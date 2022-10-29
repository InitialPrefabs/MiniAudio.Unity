using MiniAudio.Entities.Authoring;
using MiniAudio.Entities.Systems;
using MiniAudio.Interop;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniAudio.Entities.Tests.EditMode {

    [DisableAutoCreation]
    public partial class OneShotAudioSystemV2TestRunner : SystemBase {

        const string RelativePath = "Audio/Fire.ogg";

        SystemHandle systemHandle;
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery uninitializedAudioQuery;
        EntityQuery initializedAudioQuery;

        protected override void OnCreate() {
            entityCommandBufferSystem =
                World.CreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
            systemHandle = World.CreateSystem<OneShotAudioSystem>();

            uninitializedAudioQuery = SystemAPI.QueryBuilder()
                .WithAll<Path>()
                .WithNone<InitializedAudioTag>()
                .Build();

            initializedAudioQuery = SystemAPI.QueryBuilder()
                .WithAll<Path>()
                .WithAll<FreeHandle>()
                .WithAll<UsedHandle>()
                .WithAll<InitializedAudioTag>()
                .Build();
        }

        protected override void OnUpdate() { }

        public unsafe void SingletonCommandBufferExists() {
            var query = SystemAPI.QueryBuilder().WithAll<OneShotAudioSystem.Singleton>().Build();
            Assert.AreEqual(1, query.CalculateEntityCount());
            foreach (var singleton in SystemAPI.Query<OneShotAudioSystem.Singleton>()) {
                Assert.True(singleton.PendingBuffers != null);
            }
        }

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

        public unsafe void CommandBufferRecordsAndPlaysBack() {
            InitializesPooledAudioEntities();

            var audioEcbSingleton = SystemAPI.GetSingleton<OneShotAudioSystem.Singleton>();
            var audioCommandBuffer = audioEcbSingleton.CreateCommandBuffer();
            audioCommandBuffer.Request(
                new FixedString128Bytes(RelativePath), 
                1.0f);
            
            Assert.AreEqual(1, audioEcbSingleton.PendingBuffers->Length);

            systemHandle.Update(World.Unmanaged);
            
            Assert.AreEqual(0, audioEcbSingleton.PendingBuffers->Length);
            Assert.AreEqual(1, initializedAudioQuery.CalculateEntityCount());

            bool tested = false;
            foreach (var (_, poolDesc, entity) in SystemAPI
                .Query<Path, AudioPoolDescriptor>().WithEntityAccess()) {
                var freeHandles = EntityManager.GetBuffer<FreeHandle>(entity);
                var usedHandles = EntityManager.GetBuffer<UsedHandle>(entity);
                Assert.AreEqual(poolDesc.ReserveCapacity - 1, freeHandles.Length);

                Assert.AreEqual(1, usedHandles.Length);

                uint handle = usedHandles[0];
                Assert.AreNotEqual(uint.MaxValue, handle);
                Assert.True(MiniAudioHandler.IsSoundPlaying(handle));
                tested = true;
            }
            Assert.True(tested);
        }

        public void TearDown() {
            foreach (var pathBlob in SystemAPI.Query<Path>()) {
                pathBlob.Value.Dispose();
            }
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

            var blobAssetReference = BaseAudioAuthoring.CreatePathBlob(RelativePath, true);
            EntityManager.AddComponentData(entity, new Path {
                Value = blobAssetReference
            });
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
        public void SingletonCommandBufferExists() {
            testRunner.SingletonCommandBufferExists();
        }

        [Test]
        public void CommandBufferCompletesDependencies() {
            testRunner.CommandBufferRecordsAndPlaysBack();
        }

        [Test]
        public void InitializesPooledAudioEntities() {
            testRunner.InitializesPooledAudioEntities();
        }

        [Test]
        public void OneShotAudioSystemTestsErrors() {
            var system = new OneShotAudioSystem();
            var systemState = new SystemState();
            
            system.OnUpdate(ref systemState);
            LogAssert.Expect(LogType.Error, "The PendingBuffers were not initialized!");
        }
    }
}
