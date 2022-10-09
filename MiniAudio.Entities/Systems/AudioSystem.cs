using MiniAudio.Common;
using MiniAudio.Interop;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MiniAudio.Entities.Systems {

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct AudioSystem : ISystem {

        [BurstCompile]
        unsafe struct LoadSoundJob : IJobChunk {

            [ReadOnly]
            public ComponentTypeHandle<Path> PathBlobType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            public ComponentTypeHandle<IsAudioLoaded> MetadataType;

            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            NativeList<char> fullPath;

            public void Execute(
                in ArchetypeChunk chunk, 
                int unfilteredChunkIndex, 
                bool useEnabledMask, 
                in v128 chunkEnabledMask) {

                if (!fullPath.IsCreated) {
                    fullPath = new NativeList<char>(
                        StreamingAssetsHelper.Path.Data.Length,
                        Allocator.Temp);
                }

                var loadPaths = chunk.GetNativeArray(PathBlobType);
                var audioClips = chunk.GetNativeArray(AudioClipType);
                var entities = chunk.GetNativeArray(EntityType);
                var audioMetadata = chunk.GetNativeArray(MetadataType);

                for (int i = 0; i < chunk.Count; i++) {
                    ref var isLoaded = ref audioMetadata.ElementAt(i);

                    if (isLoaded.Value) {
                        continue;
                    }

                    var entity = entities[i];
                    var audioClip = audioClips[i];
                    var loadPath = loadPaths[i];

                    if (loadPath.IsStreamingAssets) {
                        fullPath.AddRangeNoResize(
                            StreamingAssetsHelper.Path.Data.Ptr,
                            StreamingAssetsHelper.Path.Data.Length);
                    }

                    ref var path = ref loadPath.Value.Value.Path;
                    fullPath.AddRange(path.GetUnsafePtr(), path.Length);

                    var handle = MiniAudioHandler.UnsafeLoadSound(
                        new IntPtr(fullPath.GetUnsafeReadOnlyPtr<char>()),
                        (uint)fullPath.Length,
                        new IntPtr(&audioClip.Parameters));

                    if (handle != uint.MaxValue) {
                        audioClip.Handle = handle;
                        CommandBuffer.SetComponent(entity, audioClip);
                        isLoaded.Value = true;
                    }
                    UnsafeUtility.MemClear(fullPath.GetUnsafePtr(), fullPath.Length * sizeof(char));
                    fullPath.Clear();
                }
            }
        }

        [BurstCompile]
        struct StopSoundJob : IJobChunk {

            [ReadOnly]
            public ComponentTypeHandle<AudioStateHistory> AudioStateHistoryType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(
                in ArchetypeChunk chunk, 
                int unfilteredChunkIndex, 
                bool useEnabledMask, 
                in v128 chunkEnabledMask) {

                var audioClips = chunk.GetNativeArray(AudioClipType);
                var stateTypes = chunk.GetNativeArray(AudioStateHistoryType);
                var entities = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++) {
                    var audioClip = audioClips[i];
                    var lastState = stateTypes[i].Value;
                    var entity = entities[i];

                    // This should only check if the entity's sound has stopped playing.
                    switch (audioClip.CurrentState) {
                        case AudioState.Playing:
                            if (MiniAudioHandler.IsSoundFinished(audioClip.Handle)) {
                                audioClip.CurrentState = AudioState.Stopped;
                                CommandBuffer.SetComponent(entity, audioClip);
                            }
                            break;
                    }
                }
            }
        }

        [BurstCompile]
        [WithChangeFilter(typeof(AudioClip))]
        struct ManageAudioStateJob : IJobChunk {

            [ReadOnly]
            public ComponentTypeHandle<AudioStateHistory> AudioStateHistoryType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            [ReadOnly]
            public ComponentTypeHandle<IsAudioLoaded> MetadataType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public uint LastSystemVersion;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(
                in ArchetypeChunk chunk, 
                int unfilteredChunkIndex, 
                bool useEnabledMask, 
                in v128 chunkEnabledMask) {

                if (!chunk.DidChange(AudioClipType, LastSystemVersion)) {
                    return;
                }

                var audioClips = chunk.GetNativeArray(AudioClipType);
                var stateTypes = chunk.GetNativeArray(AudioStateHistoryType);
                var entities = chunk.GetNativeArray(EntityType);
                var audioMetadata = chunk.GetNativeArray(MetadataType);

                for (int i = 0; i < chunk.Count; i++) {
                    var audioClip = audioClips[i];
                    var lastState = stateTypes[i].Value;
                    var metadata = audioMetadata[i];
                    var entity = entities[i];

                    MiniAudioHandler.SetSoundVolume(audioClip.Handle, audioClip.Parameters.Volume);

                    if (lastState != audioClip.CurrentState && metadata.Value) {
                        switch (audioClip.CurrentState) {
                            case AudioState.Playing:
                                MiniAudioHandler.PlaySound(audioClip.Handle);
                                break;
                            case AudioState.Stopped:
                                MiniAudioHandler.StopSound(audioClip.Handle, true);
                                break;
                            case AudioState.Paused:
                                MiniAudioHandler.StopSound(audioClip.Handle, false);
                                break;
                        }
                        CommandBuffer.SetComponent(entity, new AudioStateHistory {
                            Value = audioClip.CurrentState
                        });
                    }
                }
            }
        }

        EntityQuery soundQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            soundQuery = SystemAPI.QueryBuilder()
                .WithAll<AudioClip>()
                .WithAll<AudioStateHistory>()
                .WithAll<IsAudioLoaded>()
                .Build();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!MiniAudioHandler.IsEngineInitialized()) {
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var audioClipType = state.GetComponentTypeHandle<AudioClip>(true);
            var audioStateHistoryType = state.GetComponentTypeHandle<AudioStateHistory>(true);
            var entityType = state.GetEntityTypeHandle();

            new LoadSoundJob {
                PathBlobType = state.GetComponentTypeHandle<Path>(true),
                AudioClipType = audioClipType,
                MetadataType = state.GetComponentTypeHandle<IsAudioLoaded>(false),
                EntityType = entityType,
                CommandBuffer = commandBuffer,
            }.Run(soundQuery);
            
            new StopSoundJob {
                CommandBuffer = commandBuffer,
                AudioStateHistoryType = audioStateHistoryType,
                AudioClipType = audioClipType,
                EntityType = entityType,
            }.Run(soundQuery);
            
            new ManageAudioStateJob {
                AudioStateHistoryType = audioStateHistoryType,
                AudioClipType = audioClipType,
                MetadataType = state.GetComponentTypeHandle<IsAudioLoaded>(true),
                LastSystemVersion = state.LastSystemVersion,
                EntityType = entityType,
                CommandBuffer = commandBuffer
            }.Run(soundQuery);
        }
    }
}
