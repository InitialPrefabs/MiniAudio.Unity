using MiniAudio.Interop;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MiniAudio.Entities.Systems {

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
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

                for (int i = 0; i < chunk.Count; i++) {
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
                        CommandBuffer.AddComponent<InitializedAudioTag>(entity);
                    }
                    UnsafeUtility.MemClear(fullPath.GetUnsafePtr(), fullPath.Length * sizeof(char));
                    fullPath.Clear();
                }
            }
        }

        [BurstCompile]
        struct StopSoundJob : IJobChunk {

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
                var entities = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++) {
                    var audioClip = audioClips[i];
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
            public EntityTypeHandle EntityType;

            public uint LastSystemVersion;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask) {

                if (chunk.GetChangeVersion(AudioClipType) == LastSystemVersion) {
                    return;
                }

                var audioClips = chunk.GetNativeArray(AudioClipType);
                var stateTypes = chunk.GetNativeArray(AudioStateHistoryType);
                var entities = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++) {
                    var audioClip = audioClips[i];
                    var lastState = stateTypes[i].Value;
                    var entity = entities[i];

                    MiniAudioHandler.SetSoundVolume(audioClip.Handle, audioClip.Parameters.Volume);

                    if (lastState != audioClip.CurrentState) {
                        switch (audioClip.CurrentState) {
                            case AudioState.Playing:
                                MiniAudioHandler.PlaySound(audioClip.Handle);
                                break;
                            case AudioState.Stopped:
                                MiniAudioHandler.StopSound(audioClip.Handle, true);
                                break;
                            case AudioState.Paused:
                                // Debug.Log($"Pausing clip: {audioClip.Handle}");
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

        ComponentTypeHandle<AudioClip> audioClipType;
        ComponentTypeHandle<AudioStateHistory> audioStateHistoryType;
        ComponentTypeHandle<Path> pathBlobType;
        EntityTypeHandle entityType;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            audioClipType = state.GetComponentTypeHandle<AudioClip>(true);
            audioStateHistoryType = state.GetComponentTypeHandle<AudioStateHistory>(true);
            pathBlobType = state.GetComponentTypeHandle<Path>(true);
            entityType = state.GetEntityTypeHandle();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!MiniAudioHandler.IsEngineInitialized()) {
                return;
            }

            var uninitializedSoundQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioClip>()
                .WithAllRW<AudioStateHistory>()
                .WithNone<InitializedAudioTag>()
                .Build();

            var initializedSoundQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioClip>()
                .WithAllRW<AudioStateHistory>()
                .WithAll<InitializedAudioTag>()
                .Build();

            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            audioClipType.Update(ref state);
            audioStateHistoryType.Update(ref state);
            pathBlobType.Update(ref state);
            entityType.Update(ref state);

            new LoadSoundJob {
                PathBlobType = pathBlobType,
                AudioClipType = audioClipType,
                EntityType = entityType,
                CommandBuffer = commandBuffer,
            }.Run(uninitializedSoundQuery);

            new StopSoundJob {
                CommandBuffer = commandBuffer,
                AudioClipType = audioClipType,
                EntityType = entityType,
            }.Run(initializedSoundQuery);

            new ManageAudioStateJob {
                AudioStateHistoryType = audioStateHistoryType,
                AudioClipType = audioClipType,
                LastSystemVersion = state.LastSystemVersion,
                EntityType = entityType,
                CommandBuffer = commandBuffer
            }.Run(initializedSoundQuery);
        }
    }
}