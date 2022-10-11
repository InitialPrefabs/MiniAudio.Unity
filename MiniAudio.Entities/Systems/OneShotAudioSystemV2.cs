using System;
using System.Runtime.CompilerServices;
using MiniAudio.Common;
using MiniAudio.Interop;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace MiniAudio.Entities.Systems {

    public static unsafe class OneShotAudioSystemExtensions {
        public static AudioCommandBuffer CreateCommandBuffer(
            this OneShotAudioSystemV2.Singleton singleton, Allocator allocator) {

            var commandBuffer = new AudioCommandBuffer(allocator);
            singleton.PendingBuffers->Add(commandBuffer);
            return commandBuffer;
        }

        public static void AddJobHandleForProducer(
            this ref OneShotAudioSystemV2.Singleton singleton, JobHandle inputDeps) {
            *singleton.DependencyHandle = JobHandle.CombineDependencies(*singleton.DependencyHandle, inputDeps);
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    public unsafe partial struct OneShotAudioSystemV2 : ISystem {

        public struct Singleton : IComponentData {
            internal UnsafeList<AudioCommandBuffer>* PendingBuffers;
            internal JobHandle* DependencyHandle;
        }

        [BurstCompile]
        struct PlaybackPendingBuffersJob : IJobFor {

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<AudioCommandBuffer>* PendingBuffers;

            [ReadOnly]
            public NativeParallelHashMap<Hash128, Entity> EntityLookUp;

            public BufferLookup<FreeHandle> FreeHandles;

            public BufferLookup<UsedHandle> UsedHandles;

            public void Execute(int index) {
                ref readonly var commandBuffer = ref PendingBuffers->ElementAt(index);
                for (int i = 0; i < commandBuffer.PlaybackIds->Length; i++) {
                    ref readonly var payload = ref commandBuffer.PlaybackIds->ElementAt(i);
                    if (EntityLookUp.TryGetValue(payload.ID, out var entity)) {
                        var freeHandles = FreeHandles[entity];
                        var usedHandles = UsedHandles[entity];

                        if (freeHandles.Length > 0) {
                            var last = freeHandles.Length - 1;
                            var freeHandle = freeHandles[last];
                            MiniAudioHandler.PlaySound(freeHandle.Value);
                            MiniAudioHandler.SetSoundVolume(freeHandle.Value, payload.Volume);

                            freeHandles.RemoveAt(last);
                            usedHandles.Add(freeHandle);
                        }
                    } // TODO: allow a grow command
                }
            }
        }

        [BurstCompile]
        struct InitializePooledAudioJob : IJobChunk {

            [WriteOnly]
            public NativeParallelHashMap<Hash128, Entity> EntityLookUp;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Path> PathType;

            [ReadOnly]
            public ComponentTypeHandle<AliasSoundLoadParameters> SoundLoadParamType;

            [ReadOnly]
            public ComponentTypeHandle<AudioPoolDescriptor> PoolDescriptorType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;

            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            NativeList<char> absolutePath;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask) {

                if (!absolutePath.IsCreated) {
                    absolutePath = new NativeList<char>(100, Allocator.Temp);
                }

                var entities = chunk.GetNativeArray(EntityType);
                var paths = chunk.GetNativeArray(PathType);
                var freeHandlesAccessor = chunk.GetBufferAccessor(FreeHandleType);
                var poolDescriptors = chunk.GetNativeArray(PoolDescriptorType);
                var aliasSoundParams = chunk.GetNativeArray(SoundLoadParamType);

                for (int i = 0; i < chunk.Count; i++) {
                    var entity = entities[i];
                    var path = paths[i];
                    var aliasSoundLoadParam = aliasSoundParams[i];
                    var freeHandles = freeHandlesAccessor[i];
                    ref var pathID = ref path.Value.Value.ID;
                    ref var pathBlob = ref path.Value.Value.Path;
                    var poolDesc = poolDescriptors[i];

                    // Construct the path
                    absolutePath.Clear();
                    absolutePath.AddRangeNoResize(
                        StreamingAssetsHelper.Path.Data.Ptr, StreamingAssetsHelper.Path.Data.Length);
                    absolutePath.AddRange(pathBlob.GetUnsafePtr(), pathBlob.Length);

                    CommandBuffer.AddComponent<InitializedAudioTag>(entity);
                    // Track the Entity in a ParallelHashMap.
                    EntityLookUp.TryAdd(pathID, entity);

                    var fullPathPointer = new IntPtr(absolutePath.GetUnsafePtr());

                    for (int j = 0; j < poolDesc.ReserveCapacity; j++) {
                        var handle = MiniAudioHandler.UnsafeLoadSound(
                            fullPathPointer,
                            (uint)absolutePath.Length,
                            new IntPtr(&aliasSoundLoadParam));

                        if (handle != uint.MaxValue) {
                            freeHandles.Add(handle);
                        }
                    }
                }
            }
        }

        EntityQuery uninitializedPoolQuery;
        NativeParallelHashMap<Hash128, Entity> entityLookUp;

        UnsafeList<AudioCommandBuffer>* playbackBuffers;
        JobHandle* dependencyHandle;

        static T* InitializePointer<T>() where T : unmanaged {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>(),
                Allocator.Persistent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RegisterSingletonBuffer(
            ref SystemState state,
            JobHandle* handle,
            UnsafeList<AudioCommandBuffer>* playbackBuffer) {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new Singleton {
                PendingBuffers = playbackBuffer,
                DependencyHandle = handle
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FlushPendingBuffers(UnsafeList<AudioCommandBuffer>* playbackBuffers) {
            for (int i = 0; i < playbackBuffers->Length; i++) {
                ref var cmdBuffer = ref playbackBuffers->ElementAt(i);
                cmdBuffer.Dispose();
            }
            playbackBuffers->Clear();
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            uninitializedPoolQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioPoolDescriptor>()
                .WithAllRW<OneShotAudioState>()
                .WithAllRW<FreeHandle>()
                .WithAll<UsedHandle>()
                .WithAll<AliasSoundLoadParameters>()
                .WithNone<InitializedAudioTag>()
                .Build();

            entityLookUp = new NativeParallelHashMap<Hash128, Entity>(64, Allocator.Persistent);
            playbackBuffers = InitializePointer<UnsafeList<AudioCommandBuffer>>();
            *playbackBuffers = new UnsafeList<AudioCommandBuffer>(64, Allocator.Persistent);
            
            dependencyHandle = InitializePointer<JobHandle>();
            *dependencyHandle = default;

            RegisterSingletonBuffer(ref state, dependencyHandle, playbackBuffers);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {
            if (entityLookUp.IsCreated) {
                entityLookUp.Dispose();
            }

            if (playbackBuffers != null) {
                playbackBuffers->Dispose();
                UnsafeUtility.Free(playbackBuffers, Allocator.Persistent);
                playbackBuffers = null;
            }

            if (dependencyHandle != null) {
                UnsafeUtility.Free(dependencyHandle, Allocator.Persistent);
                dependencyHandle = null;
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (dependencyHandle == null || playbackBuffers == null) {
                Debug.LogError("The dependencyHandle and playbackBuffers were not initialized!");
                return;
            }
#endif
            dependencyHandle->Complete();

            // TODO: Clear the pending buffers list.
            new PlaybackPendingBuffersJob {
                PendingBuffers = playbackBuffers,
                EntityLookUp = entityLookUp,
                FreeHandles = SystemAPI.GetBufferLookup<FreeHandle>(),
                UsedHandles = SystemAPI.GetBufferLookup<UsedHandle>()
            }.Run(playbackBuffers->Length);

            FlushPendingBuffers(playbackBuffers);

            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new InitializePooledAudioJob {
                EntityLookUp = entityLookUp,
                PathType = state.GetComponentTypeHandle<Path>(true),
                EntityType = state.GetEntityTypeHandle(),
                SoundLoadParamType = state.GetComponentTypeHandle<AliasSoundLoadParameters>(true),
                PoolDescriptorType = state.GetComponentTypeHandle<AudioPoolDescriptor>(true),
                FreeHandleType = state.GetBufferTypeHandle<FreeHandle>(),
                CommandBuffer = commandBuffer
            }.Run(uninitializedPoolQuery);
        }
    }
}
