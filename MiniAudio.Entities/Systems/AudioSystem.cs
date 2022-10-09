using MiniAudio.Common;
using MiniAudio.Interop;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Systems {

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct AudioSystem : ISystem {

        [BurstCompile]
        unsafe struct LoadSoundJob : IJobChunk {

            [ReadOnly]
            public NativeArray<char> StreamingPath;

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
                        StreamingPath.Length, 
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
                            StreamingPath.GetUnsafeReadOnlyPtr(),
                            StreamingPath.Length);
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
        NativeArray<char> fixedStreamingPath;

        public void OnCreate(ref SystemState state) {
            soundQuery = SystemAPI.QueryBuilder()
                .WithAll<AudioClip>()
                .WithAll<AudioStateHistory>()
                .WithAll<IsAudioLoaded>()
                .Build();

            var streamingPath = Application.streamingAssetsPath;
            fixedStreamingPath = new NativeArray<char>(streamingPath.Length, Allocator.Persistent);

            for (int i = 0; i < streamingPath.Length; i++) {
                fixedStreamingPath[i] = streamingPath[i];
            }
            
            // commandBufferSystem = World.GetExistingSystemManaged<EndInitializationEntityCommandBufferSystem>();
        }

        public void OnDestroy(ref SystemState state) {
            if (fixedStreamingPath.IsCreated) {
                fixedStreamingPath.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state) {
            if (!MiniAudioHandler.IsEngineInitialized()) {
                return;
            }
            
            // var commandBuffer = commandBufferSystem.CreateCommandBuffer();
            
            // new LoadSoundJob {
            //     PathBlobType = GetComponentTypeHandle<Path>(true),
            //     AudioClipType = GetComponentTypeHandle<AudioClip>(true),
            //     MetadataType = GetComponentTypeHandle<IsAudioLoaded>(false),
            //     EntityType = GetEntityTypeHandle(),
            //     CommandBuffer = commandBuffer,
            //     StreamingPath = fixedStreamingPath
            // }.Run(soundQuery);

            // new StopSoundJob {
            //     AudioStateHistoryType = GetComponentTypeHandle<AudioStateHistory>(true),
            //     AudioClipType = GetComponentTypeHandle<AudioClip>(true),
            //     CommandBuffer = commandBuffer,
            //     EntityType = GetEntityTypeHandle()
            // }.Run(soundQuery);

            // new ManageAudioStateJob() {
            //     AudioStateHistoryType = GetComponentTypeHandle<AudioStateHistory>(true),
            //     AudioClipType = GetComponentTypeHandle<AudioClip>(true),
            //     MetadataType = GetComponentTypeHandle<IsAudioLoaded>(true),
            //     CommandBuffer = commandBuffer,
            //     LastSystemVersion = LastSystemVersion,
            //     EntityType = GetEntityTypeHandle()
            // }.Run(soundQuery);
            
            // commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
