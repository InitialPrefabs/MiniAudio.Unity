using System;
using System.IO;
using MiniAudio.Entities.Systems;
using MiniAudio.Interop;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MiniAudio.Entities.Tests.EditMode {

    public unsafe class OneShotAudioSystemTests : ECSTestsFixture {

        static readonly string path = Application.streamingAssetsPath + "/Audio/Fire.ogg";

        [BurstCompile]
        struct FillCmdBufferJob : IJobFor {

            public AudioCommandBuffer CommandBuffer;
            public FixedString512Bytes StringBytes;

            public void Execute(int index) {
                CommandBuffer.Request(StringBytes, 0);
            }
        }

        OneShotAudioSystem oneShotAudioSystem;
        EntityCommandBufferSystem entityCommandBufferSystem;

        EntityQuery initializedAudioQuery;

        [SetUp]
        public override void Setup() {
            base.Setup();
            DefaultMiniAudioInitializationProxy.Initialize();
            Assert.True(MiniAudioHandler.IsEngineInitialized());

            entityCommandBufferSystem = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
            oneShotAudioSystem = World.CreateSystemManaged<OneShotAudioSystem>();

            initializedAudioQuery = EmptySystem.GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<AudioPoolDescriptor>(),
                    ComponentType.ReadWrite<AudioPoolID>(),
                    ComponentType.ReadWrite<FreeHandle>(),
                    ComponentType.ReadWrite<UsedHandle>()
                }
            });
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
            DefaultMiniAudioInitializationProxy.Release();
        }

        [Test]
        public void BlobFileExists() {
            var entity = m_Manager.CreateEntity();
            var builder = new BlobBuilder(Allocator.Temp);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();
            var charArray = builder.Allocate<char>(ref pathBlob.Path, path.Length);

            for (int i = 0; i < path.Length; i++) {
                charArray[i] = path[i];
            }

            pathBlob.IsPathStreamingAssets = true;

            var pathComp = new Path {
                Value = builder.CreateBlobAssetReference<PathBlob>(Allocator.Persistent)
            };

            m_Manager.AddComponentData(entity, pathComp);

            bool tested = false;

            foreach (var entry in SystemAPI.Query<Path>().WithEntityAccess()) {
                ref var pathArray = ref entry.Item1.Value.Value.Path;
                void* head = pathArray.GetUnsafePtr();
                var loadParams = new AliasSoundLoadParameters {
                    Volume = 1.0f
                };
                var handle = MiniAudioHandler.UnsafeLoadSound(
                    new IntPtr(head),
                    (uint)pathArray.Length,
                    new IntPtr(&loadParams));
                Assert.AreNotEqual(uint.MaxValue, handle);
                MiniAudioHandler.UnloadSound(handle);
                tested = true;
            }
            Assert.True(tested);
        }

        [Test]
        public void FileExists() {
            Assert.True(File.Exists(path));
            fixed(char* ptr = path) {
                var param = new AliasSoundLoadParameters();
                var handle = MiniAudioHandler.UnsafeLoadSound(
                    new IntPtr(ptr),
                    (uint)path.Length * sizeof(char),
                    new IntPtr(&param));

                Assert.AreNotEqual(uint.MaxValue, handle);
                MiniAudioHandler.UnloadSound(handle);
            }
        }

        [Test]
        public void InitializesAudioPool() {
            CreateAudioUninitializedAudioEntity();

            Assert.True(initializedAudioQuery.IsEmpty);

            oneShotAudioSystem.Update();
            entityCommandBufferSystem.Update();

            Assert.AreEqual(1, initializedAudioQuery.CalculateEntityCount());

            foreach (var entry in SystemAPI
                .Query<DynamicBuffer<FreeHandle>, AudioPoolDescriptor, AudioPoolID, Path>()
                .WithEntityAccess()) {
                var entity = entry.Item5;
                Assert.True(entry.Item2.IsLoaded);

                fixed(char* ptr = path) {
                    var expectedID = math.hash(ptr, sizeof(char) * path.Length);
                    Assert.AreEqual(expectedID, entry.Item3.Value);
                }

                ref var pathArray = ref entry.Item4.Value.Value.Path;
                var builtPath = new string((char*)pathArray.GetUnsafePtr(), 0, pathArray.Length);
                Assert.AreEqual(10, entry.Item1.Length, $"Failed to load {builtPath}");
            }
        }

        [Test]
        public void AudioCommandBufferPlaysSound() {
            InitializesAudioPool();

            var commandBuffer = oneShotAudioSystem.CreateCommandBuffer();

            new FillCmdBufferJob {
                CommandBuffer = commandBuffer,
                StringBytes = new FixedString512Bytes(path)
            }.Run(1);

            oneShotAudioSystem.Update();

            bool tested = false;

            SystemAPI.QueryBuilder().WithAll<AudioPoolDescriptor>().Build();

            // Entities.ForEach((
            //     ref AudioPoolDescriptor desc,
            //     DynamicBuffer<FreeHandle> freeHandles,
            //     DynamicBuffer<UsedHandle> usedHandles) => {
            //
            //         Assert.AreEqual(freeHandles.Length + usedHandles.Length, desc.ReserveCapacity);
            //         Assert.True(MiniAudioHandler.IsSoundPlaying(usedHandles[0].Value));
            //         tested = true;
            //     });

            Assert.True(tested);
        }

        void CreateAudioUninitializedAudioEntity() {
            var entity = m_Manager.CreateEntity();

            var builder = new BlobBuilder(Allocator.Temp);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();
            var charArray = builder.Allocate<char>(ref pathBlob.Path, path.Length);

            for (int i = 0; i < path.Length; i++) {
                charArray[i] = path[i];
            }

            m_Manager.AddComponentData(entity, new Path {
                Value = builder.CreateBlobAssetReference<PathBlob>(Allocator.Persistent)
            });

            m_Manager.AddBuffer<FreeHandle>(entity);
            m_Manager.AddBuffer<UsedHandle>(entity);

            var buffer = m_Manager.AddComponentData(entity, new AliasSoundLoadParameters {
                Volume = 1.0f
            });

            m_Manager.AddComponentData(entity, new AudioPoolDescriptor {
                ReserveCapacity = 10
            });

            m_Manager.AddBuffer<OneShotAudioState>(entity);
        }
    }
}
