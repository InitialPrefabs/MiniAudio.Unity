using System;
using MiniAudio.Common;
using MiniAudio.Interop;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MiniAudio.Entities.Systems {

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(AudioSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class OneShotAudioSystem : SystemBase {

        [BurstCompile]
        unsafe struct InitializePooledAudioJob : IJobChunk {

            [WriteOnly]
            public NativeParallelHashMap<uint, Entity> EntityLookUp;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public NativeArray<char> StreamingPath;

            [ReadOnly]
            public ComponentTypeHandle<Path> PathType;

            [ReadOnly]
            public ComponentTypeHandle<AliasSoundLoadParameters> SoundLoadParamsType;

            public ComponentTypeHandle<AudioPoolDescriptor> PoolDescriptorType;

            public BufferTypeHandle<OneShotAudioState> OneShotAudioStateType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;

            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            NativeList<char> fullPath;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                if (!fullPath.IsCreated) {
                    fullPath = new NativeList<char>(StreamingPath.Length, Allocator.Temp);
                }

                var freeHandles = chunk.GetBufferAccessor(FreeHandleType);
                var soundLoadParams = chunk.GetNativeArray(SoundLoadParamsType);
                var poolDescriptors = chunk.GetNativeArray(PoolDescriptorType);
                var oneshotAudioStates = chunk.GetBufferAccessor(OneShotAudioStateType);
                var paths = chunk.GetNativeArray(PathType);
                var entities = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++) {
                    ref var poolDescriptor = ref poolDescriptors.ElementAt(i);
                    if (poolDescriptor.IsLoaded) {
                        continue;
                    }

                    var loadPath = paths[i];
                    var freeHandleBuffer = freeHandles[i];
                    var oneShotAudioStateBuffer = oneshotAudioStates[i];
                    var entity = entities[i];

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
                    var poolId = new AudioPoolID {
                        Value = math.hash(path.GetUnsafePtr(), path.Length * sizeof(char))
                    };
                    CommandBuffer.AddComponent(entity, poolId);

                    EntityLookUp.TryAdd(poolId.Value, entities[i]);
                    var soundLoadParam = soundLoadParams[i];

                    for (int j = 0; j < poolDescriptor.ReserveCapacity; j++) {
                        var handle = MiniAudioHandler.UnsafeLoadSound(
                            new IntPtr(fullPath.GetUnsafeReadOnlyPtr()),
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
        struct RemoveTrackedPooledEntityJob : IJobChunk {

            [WriteOnly]
            public NativeParallelHashMap<uint, Entity> EntityLookUp;

            [ReadOnly]
            public ComponentTypeHandle<AudioPoolID> AudioPoolDescriptorType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var audioPoolDescriptors = chunk.GetNativeArray(AudioPoolDescriptorType);
                for (int i = 0; i < chunk.Count; i++) {
                    EntityLookUp.Remove(audioPoolDescriptors[i].Value);
                }
            }
        }

        // TODO: Finish implementing.
        [BurstCompile]
        unsafe struct PlaybackCommandBufferJob : IJobFor {

            [ReadOnly]
            public NativeArray<AudioCommandBuffer> AudioCommandBuffers;

            [ReadOnly]
            public NativeParallelHashMap<uint, Entity> EntityLookUp;

            public BufferLookup<FreeHandle> FreeHandles;

            public BufferLookup<UsedHandle> UsedHandles;

            public void Execute(int index) {
                var commandBuffer = AudioCommandBuffers[index];
                for (int i = 0; i < commandBuffer.PlaybackIds->Length; i++) {
                    var payload = commandBuffer.PlaybackIds->ElementAt(i);
                    if (EntityLookUp.TryGetValue(payload.ID, out Entity entity)) {
                        var soundFreeHandles = FreeHandles[entity];
                        var soundInPlayHandles = UsedHandles[entity];

                        if (soundFreeHandles.Length > 0) {
                            var last = soundFreeHandles.Length - 1;
                            var freeHandle = soundFreeHandles[last];

                            // Play the sound
                            MiniAudioHandler.PlaySound(freeHandle.Value);
                            MiniAudioHandler.SetSoundVolume(freeHandle.Value, payload.Volume);
                            soundFreeHandles.RemoveAt(soundFreeHandles.Length - 1);

                            soundInPlayHandles.Add(freeHandle);
                        } // TODO: Load a sound
                    }
                }
            }
        }

        [BurstCompile]
        struct ManageOneShotAudioJob : IJobChunk {

            public BufferTypeHandle<UsedHandle> UsedHandleType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;

            [NativeDisableContainerSafetyRestriction]
            NativeList<int> cleanUp;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                if (!cleanUp.IsCreated) {
                    cleanUp = new NativeList<int>(10, Allocator.Temp);
                }

                var usedHandlesAccessor = chunk.GetBufferAccessor(UsedHandleType);
                var freeHandlesAccessor = chunk.GetBufferAccessor(FreeHandleType);
                for (int i = 0; i < chunk.Count; i++) {
                    var usedHandles = usedHandlesAccessor[i];
                    var freeHandles = freeHandlesAccessor[i];

                    for (int j = usedHandles.Length - 1; j >= 0; j--) {
                        var handle = usedHandles[j];
                        if (MiniAudioHandler.IsSoundFinished(usedHandles[j].Value)) {
                            // Stop the sound so we can rewind it
                            MiniAudioHandler.StopSound(handle.Value, true);

                            // The value will be stored into the FreeHandle buffer so that it can 
                            // be reused.
                            freeHandles.Add(handle);

                            // Collect the index so we can remove it from the UsedHandle
                            cleanUp.Add(j);
                        }
                    }

                    for (int j = 0; j < cleanUp.Length; j++) {
                        usedHandles.RemoveAtSwapBack(cleanUp[j]);
                    }

                    cleanUp.Clear();
                }
            }
        }

        NativeParallelHashMap<uint, Entity> entityLookUp;
        EntityQuery uninitializeAudioPoolQuery;
        EntityQuery cleanUpEntityQuery;
        EntityQuery oneShotAudioQuery;

        NativeArray<char> fixedStreamingPath;
        NativeList<AudioCommandBuffer> audioCommandBuffers;

        EntityCommandBufferSystem commandBufferSystem;
        JobHandle frameDependency;

        protected override void OnCreate() {
            uninitializeAudioPoolQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<AudioPoolDescriptor>(),
                    ComponentType.ReadWrite<FreeHandle>(),
                    ComponentType.ReadWrite<OneShotAudioState>(),
                    ComponentType.ReadOnly<UsedHandle>(),
                    ComponentType.ReadOnly<AliasSoundLoadParameters>(),
                },
                None = new[] {
                    ComponentType.ReadWrite<AudioPoolID>()
                }
            });

            cleanUpEntityQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<AudioPoolID>()
                },
                None = new[] {
                    ComponentType.ReadWrite<AudioPoolDescriptor>(),
                    ComponentType.ReadOnly<FreeHandle>(),
                    ComponentType.ReadOnly<UsedHandle>(),
                    ComponentType.ReadOnly<AliasSoundLoadParameters>(),
                    ComponentType.ReadOnly<OneShotAudioState>()
                }
            });

            oneShotAudioQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadWrite<FreeHandle>(), ComponentType.ReadWrite<UsedHandle>()
                }
            });

            var streamingPath = Application.streamingAssetsPath;
            fixedStreamingPath = new NativeArray<char>(streamingPath.Length, Allocator.Persistent);

            for (int i = 0; i < streamingPath.Length; i++) {
                fixedStreamingPath[i] = streamingPath[i];
            }

            entityLookUp = new NativeParallelHashMap<uint, Entity>(10, Allocator.Persistent);
            audioCommandBuffers = new NativeList<AudioCommandBuffer>(10, Allocator.Persistent);
            commandBufferSystem = World.GetExistingSystemManaged<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy() {
            if (fixedStreamingPath.IsCreated) {
                fixedStreamingPath.Dispose();
            }

            if (entityLookUp.IsCreated) {
                entityLookUp.Dispose();
            }

            if (audioCommandBuffers.IsCreated) {
                audioCommandBuffers.Dispose();
            }
        }

        protected override void OnUpdate() {
            frameDependency.Complete();
            frameDependency = default;

            var commandBuffer = commandBufferSystem.CreateCommandBuffer();

            new InitializePooledAudioJob {
                PathType = GetComponentTypeHandle<Path>(true),
                SoundLoadParamsType = GetComponentTypeHandle<AliasSoundLoadParameters>(true),
                FreeHandleType = GetBufferTypeHandle<FreeHandle>(false),
                PoolDescriptorType = GetComponentTypeHandle<AudioPoolDescriptor>(false),
                OneShotAudioStateType = GetBufferTypeHandle<OneShotAudioState>(false),
                EntityType = GetEntityTypeHandle(),
                StreamingPath = fixedStreamingPath,
                EntityLookUp = entityLookUp,
                CommandBuffer = commandBuffer
            }.Run(uninitializeAudioPoolQuery);

            new ManageOneShotAudioJob {
                FreeHandleType = GetBufferTypeHandle<FreeHandle>(false),
                UsedHandleType = GetBufferTypeHandle<UsedHandle>(false),
            }.Run(oneShotAudioQuery);

            if (audioCommandBuffers.Length > 0) {
                new PlaybackCommandBufferJob {
                    AudioCommandBuffers = audioCommandBuffers.AsArray(),
                    EntityLookUp = entityLookUp,
                    FreeHandles = GetBufferLookup<FreeHandle>(false),
                    UsedHandles = GetBufferLookup<UsedHandle>(false),
                }.Run(audioCommandBuffers.Length);

                // Perform the clean up
                for (int i = 0; i < audioCommandBuffers.Length; i++) {
                    var audioCommandBuffer = audioCommandBuffers[i];
                    audioCommandBuffer.Dispose();
                }
                audioCommandBuffers.Clear();
            }

            if (!cleanUpEntityQuery.IsEmpty) {
                new RemoveTrackedPooledEntityJob {
                    AudioPoolDescriptorType = GetComponentTypeHandle<AudioPoolID>(true),
                    EntityLookUp = entityLookUp
                }.Run(cleanUpEntityQuery);

                commandBuffer.RemoveComponentForEntityQuery<AudioPoolDescriptor>(cleanUpEntityQuery);
            }

            commandBufferSystem.AddJobHandleForProducer(Dependency);
            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        public AudioCommandBuffer CreateCommandBuffer(Allocator allocator = Allocator.TempJob) {
            var cmdBuffer = new AudioCommandBuffer(allocator);
            audioCommandBuffers.Add(cmdBuffer);
            return cmdBuffer;
        }

        public void AddJobHandleForProducer(JobHandle jobHandle) {
            JobHandle.CombineDependencies(jobHandle, frameDependency);
        }
    }
}
