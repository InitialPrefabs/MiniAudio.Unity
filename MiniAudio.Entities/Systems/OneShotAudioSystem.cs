using System;
using MiniAudio.Common;
using MiniAudio.Interop;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MiniAudio.Entities.Systems {

    public partial class OneShotAudioSystem : SystemBase {

        [BurstCompile]
        unsafe struct InitializePooledAudioJob : IJobEntityBatch {

            [WriteOnly]
            public NativeParallelHashMap<uint, Entity> EntityLookUp;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public NativeArray<char> StreamingPath;

            [ReadOnly]
            public ComponentTypeHandle<Path> PathType;

            [ReadOnly]
            public BufferTypeHandle<SoundLoadParametersElement> SoundLoadParamsType;

            public ComponentTypeHandle<AudioPoolDescriptor> PoolDescriptorType;

            public BufferTypeHandle<OneShotAudioState> OneShotAudioStateType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;

            [NativeDisableContainerSafetyRestriction]
            NativeList<char> fullPath;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                if (!fullPath.IsCreated) {
                    fullPath = new NativeList<char>(StreamingPath.Length, Allocator.Temp);
                }

                var freeHandles = batchInChunk.GetBufferAccessor(FreeHandleType);
                var soundLoadParams = batchInChunk.GetBufferAccessor(SoundLoadParamsType);
                var poolDescriptors = batchInChunk.GetNativeArray(PoolDescriptorType);
                var oneshotAudioStates = batchInChunk.GetBufferAccessor(OneShotAudioStateType);
                var paths = batchInChunk.GetNativeArray(PathType);
                var entities = batchInChunk.GetNativeArray(EntityType);

                for (int i = 0; i < batchInChunk.Count; i++) {
                    ref var poolDescriptor = ref poolDescriptors.ElementAt(i);
                    if (poolDescriptor.IsLoaded) {
                        continue;
                    }

                    var loadPath = paths[i];
                    var freeHandleBuffer = freeHandles[i];
                    var oneShotAudioStateBuffer = oneshotAudioStates[i];

                    oneShotAudioStateBuffer.Clear();
                    freeHandleBuffer.Clear();

                    if (loadPath.IsStreamingAssets) {
                        fullPath.AddRangeNoResize(
                            StreamingPath.GetUnsafeReadOnlyPtr(),
                            StreamingPath.Length);
                    }

                    ref var path = ref loadPath.Value.Value.Path;
                    fullPath.AddRange(path.GetUnsafePtr(), path.Length);

                    // Hash the path so we can store the entities and do quick lookup.
                    poolDescriptor.ID = math.hash(
                        fullPath.GetUnsafePtr(),
                        path.Length * sizeof(char));

                    EntityLookUp.TryAdd(poolDescriptor.ID, entities[i]);

                    var soundLoadParamArray = soundLoadParams[i].AsNativeArray();
                    for (int j = 0; j < poolDescriptor.ReserveCapacity; j++) {
                        var soundLoadParam = soundLoadParamArray[j];
                        var handle = MiniAudioHandler.UnsafeLoadSound(
                            new IntPtr(fullPath.GetUnsafeReadOnlyPtr<char>()),
                            (uint)fullPath.Length,
                            new IntPtr(&soundLoadParam));

                        if (handle != uint.MaxValue) {
                            freeHandleBuffer.Add(new FreeHandle { Value = handle });
                            oneShotAudioStateBuffer.Add(AudioState.Stopped);
                        }
                    }

                    poolDescriptor.IsLoaded = true;
                    fullPath.Clear();
                }
            }
        }

        [BurstCompile]
        struct RemoveTrackedPooledEntityJob : IJobEntityBatch {

            [WriteOnly]
            public NativeParallelHashMap<uint, Entity> EntityLookUp;

            [ReadOnly]
            public ComponentTypeHandle<AudioPoolDescriptor> AudioPoolDescriptorType;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                var audioPoolDescriptors = batchInChunk.GetNativeArray(AudioPoolDescriptorType);
                for (int i = 0; i < batchInChunk.Count; i++) {
                    EntityLookUp.Remove(audioPoolDescriptors[i].ID);
                }
            }
        }

        EntityQuery uninitializeAudioPoolQuery;
        EntityQuery cleanUpEntityQuery;
        NativeArray<char> fixedStreamingPath;
        NativeParallelHashMap<uint, Entity> entityLookUp;

        EntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate() {
            uninitializeAudioPoolQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<AudioPoolDescriptor>(),
                    ComponentType.ReadWrite<FreeHandle>(),
                    ComponentType.ReadOnly<UsedHandle>(),
                    ComponentType.ReadOnly<SoundLoadParametersElement>(),
                    ComponentType.ReadWrite<OneShotAudioState>()
                }
            });

            cleanUpEntityQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<AudioPoolDescriptor>()
                },
                None = new[] {
                    ComponentType.ReadWrite<FreeHandle>(),
                    ComponentType.ReadOnly<UsedHandle>(),
                    ComponentType.ReadOnly<SoundLoadParametersElement>(),
                    ComponentType.ReadWrite<OneShotAudioState>()
                }
            });

            var streamingPath = Application.streamingAssetsPath;
            fixedStreamingPath = new NativeArray<char>(streamingPath.Length, Allocator.Persistent);

            for (int i = 0; i < streamingPath.Length; i++) {
                fixedStreamingPath[i] = streamingPath[i];
            }

            entityLookUp = new NativeParallelHashMap<uint, Entity>(10, Allocator.Persistent);
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy() {
            if (fixedStreamingPath.IsCreated) {
                fixedStreamingPath.Dispose();
            }

            if (entityLookUp.IsCreated) {
                entityLookUp.Dispose();
            }
        }

        protected override void OnUpdate() {
            new InitializePooledAudioJob {
                PathType = GetComponentTypeHandle<Path>(true),
                SoundLoadParamsType = GetBufferTypeHandle<SoundLoadParametersElement>(true),
                FreeHandleType = GetBufferTypeHandle<FreeHandle>(false),
                PoolDescriptorType = GetComponentTypeHandle<AudioPoolDescriptor>(false),
                OneShotAudioStateType = GetBufferTypeHandle<OneShotAudioState>(false),
                EntityType = GetEntityTypeHandle(),
                StreamingPath = fixedStreamingPath,
                EntityLookUp = entityLookUp,
            }.Run(uninitializeAudioPoolQuery);

            if (!cleanUpEntityQuery.IsEmpty) {
                new RemoveTrackedPooledEntityJob {
                    AudioPoolDescriptorType = GetComponentTypeHandle<AudioPoolDescriptor>(true),
                    EntityLookUp = entityLookUp
                }.Run(cleanUpEntityQuery);

                var commandBuffer = commandBufferSystem.CreateCommandBuffer();
                commandBuffer.RemoveComponentForEntityQuery<AudioPoolDescriptor>(cleanUpEntityQuery);
            }
        }
    }
}

