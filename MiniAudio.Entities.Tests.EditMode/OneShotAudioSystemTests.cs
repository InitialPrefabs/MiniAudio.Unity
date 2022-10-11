using MiniAudio.Entities.Systems;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;

namespace MiniAudio.Entities.Tests.EditMode {

    [DisableAutoCreation]
    public partial class OneShotAudioSystemV2TestRunner : SystemBase {

        SystemHandle systemHandle;
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery blobQuery;
        EntityQuery uninitializedAudioQuery;
        EntityQuery initializedAudioQuery;

        protected override void OnCreate() {
            entityCommandBufferSystem =
                World.CreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
            systemHandle = World.CreateSystem<OneShotAudioSystemV2>();

            blobQuery = SystemAPI.QueryBuilder()
                .WithAll<Path>()
                .Build();

            uninitializedAudioQuery = SystemAPI.QueryBuilder()
                .WithAll<Path>()
                .WithNone<InitializedAudioTag>()
                .Build();

            initializedAudioQuery = SystemAPI.QueryBuilder()
                .WithAll<Path>()
                .WithAll<FreeHandle>()
                .WithAll<InitializedAudioTag>()
                .Build();
        }

        protected override void OnUpdate() { }

        public void InitializesPooledAudioEntities() {
            CreateUninitializedPooledAudioEntity();

            Assert.AreEqual(1, uninitializedAudioQuery.CalculateEntityCount());
            Assert.AreEqual(0, initializedAudioQuery.CalculateEntityCount());

            systemHandle.Update(World.Unmanaged);
            entityCommandBufferSystem.Update();

            foreach (var (poolDescriptor, entity) in SystemAPI.Query<AudioPoolDescriptor>().WithEntityAccess()) {
                var freeHandles = EntityManager.GetBuffer<FreeHandle>(entity);
                Assert.AreEqual(poolDescriptor.ReserveCapacity, freeHandles.Length);
            }
            
            Assert.AreEqual(0, uninitializedAudioQuery.CalculateEntityCount());
            Assert.AreEqual(1, initializedAudioQuery.CalculateEntityCount());
        }

        void CreateUninitializedPooledAudioEntity() {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new AudioPoolDescriptor {
                ReserveCapacity = 10
            });
            EntityManager.AddComponentData(entity, new AliasSoundLoadParameters {
                Volume = 1.0f
            });
            EntityManager.AddBuffer<FreeHandle>(entity);
            EntityManager.AddBuffer<UsedHandle>(entity);
            EntityManager.AddBuffer<OneShotAudioState>(entity);

            var path = $"/Audio/Fire.ogg";
            using var builder = new BlobBuilder(World.UpdateAllocator.ToAllocator);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();
            var charArray = builder.Allocate(ref pathBlob.Path, path.Length);

            for (int i = 0; i < charArray.Length; i++) {
                charArray[i] = path[i];
            }

            pathBlob.IsPathStreamingAssets = true;
            pathBlob.ID = UnityEngine.Hash128.Compute(path);
            var blobAssetReference = builder
                .CreateBlobAssetReference<PathBlob>(Allocator.Persistent);

            EntityManager.AddComponentData(entity, new Path {
                Value = blobAssetReference
            });
        }

        public void TearDown() {
            foreach (var pathBlob in SystemAPI.Query<Path>()) {
                pathBlob.Value.Dispose();
            }
        }
    }

    public class OneShotAudioSystemV2Tests : ECSTestsFixture {

        OneShotAudioSystemV2TestRunner testRunner;
        [SetUp]
        public override void Setup() {
            base.Setup();
            DefaultMiniAudioInitializationProxy.Initialize();
            testRunner = World.CreateSystemManaged<OneShotAudioSystemV2TestRunner>();
        }

        [TearDown]
        public override void TearDown() {
            testRunner.TearDown();
            base.TearDown();
            DefaultMiniAudioInitializationProxy.Release();
        }

        [Test]
        public void InitializesPooledAudioEntities() {
            testRunner.InitializesPooledAudioEntities();
        }
    }

}
