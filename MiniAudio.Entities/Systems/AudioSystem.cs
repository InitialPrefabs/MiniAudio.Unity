using MiniAudio.Common;
using MiniAudio.Interop;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

#if !UNITY_EDITOR_WIN
using Unity.Burst;
#endif

namespace MiniAudio.Entities.Systems {

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class AudioSystem : SystemBase {

#if !UNITY_EDITOR_WIN
        [BurstCompile]
#endif
        unsafe struct LoadSoundJob : IJobEntityBatch {

            [ReadOnly]
            public BufferTypeHandle<LoadPath> LoadPathType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            public ComponentTypeHandle<AudioMetadata> MetadataType;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public NativeArray<char> StreamingPath;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                var loadParams = batchInChunk.GetBufferAccessor(LoadPathType);
                var audioClips = batchInChunk.GetNativeArray(AudioClipType);
                var entities = batchInChunk.GetNativeArray(EntityType);
                var audioMetadata = batchInChunk.GetNativeArray(MetadataType);

                var fullPath = new NativeList<char>(StreamingPath.Length, Allocator.Temp);
                for (int i = 0; i < batchInChunk.Count; i++) {
                    ref var metadata = ref audioMetadata.ElementAt(i);

                    if (metadata.IsLoaded) {
                        continue;
                    }

                    var entity = entities[i];
                    var audioClip = audioClips[i];
                    var pathBuffer = loadParams[i];

                    fullPath.AddRangeNoResize(StreamingPath.GetUnsafeReadOnlyPtr(), StreamingPath.Length);

                    if (metadata.IsStreamingAssets) {
                        fullPath.AddRange(pathBuffer.GetUnsafeReadOnlyPtr(), pathBuffer.Length);
                    }

                    SoundLoadParameters* loadParameters = &audioClip.Parameters;

                    var handle = MiniAudioHandler.UnsafeLoadSound(
                        new IntPtr(fullPath.GetUnsafeReadOnlyPtr<char>()),
                        (uint)fullPath.Length,
                        new IntPtr(loadParameters));

                    if (handle != uint.MaxValue) {
                        audioClip.Handle = handle;
                        CommandBuffer.SetComponent(entity, audioClip);
                        metadata.IsLoaded = true;
                    }
                    fullPath.Clear();
                }
            }
        }

#if !UNITY_EDITOR_WIN
        [BurstCompile]
#endif
        struct StopSoundJob : IJobEntityBatch {

            [ReadOnly]
            public ComponentTypeHandle<AudioStateHistory> AudioStateHistoryType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                var audioClips = batchInChunk.GetNativeArray(AudioClipType);
                var stateTypes = batchInChunk.GetNativeArray(AudioStateHistoryType);
                var entities = batchInChunk.GetNativeArray(EntityType);

                for (int i = 0; i < batchInChunk.Count; i++) {
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

#if !UNITY_EDITOR_WIN
        [BurstCompile]
#endif
        [WithChangeFilter(typeof(AudioClip))]
        struct ManageAudioStateJob : IJobEntityBatch {

            [ReadOnly]
            public ComponentTypeHandle<AudioStateHistory> AudioStateHistoryType;

            [ReadOnly]
            public ComponentTypeHandle<AudioClip> AudioClipType;

            [ReadOnly]
            public ComponentTypeHandle<AudioMetadata> MetadataType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public uint LastSystemVersion;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                if (!batchInChunk.DidChange(AudioClipType, LastSystemVersion)) {
                    return;
                }
                var audioClips = batchInChunk.GetNativeArray(AudioClipType);
                var stateTypes = batchInChunk.GetNativeArray(AudioStateHistoryType);
                var entities = batchInChunk.GetNativeArray(EntityType);
                var audioMetadata = batchInChunk.GetNativeArray(MetadataType);

                for (int i = 0; i < batchInChunk.Count; i++) {
                    var audioClip = audioClips[i];
                    var lastState = stateTypes[i].Value;
                    var metadata = audioMetadata[i];
                    var entity = entities[i];

                    MiniAudioHandler.SetSoundVolume(audioClip.Handle, audioClip.Parameters.Volume);

                    if (lastState != audioClip.CurrentState && metadata.IsLoaded) {
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
        EntityCommandBufferSystem commandBufferSystem;
        NativeArray<char> fixedStreamingPath;

        protected override void OnCreate() {
            soundQuery = GetEntityQuery(new EntityQueryDesc() {
                All = new[] {
                    ComponentType.ReadOnly<AudioClip>(),
                    ComponentType.ReadOnly<AudioStateHistory>(),
                    ComponentType.ReadOnly<AudioMetadata>()
                },
            });

            var streamingPath = Application.streamingAssetsPath;
            fixedStreamingPath = new NativeArray<char>(streamingPath.Length, Allocator.Persistent);

            for (int i = 0; i < streamingPath.Length; i++) {
                fixedStreamingPath[i] = streamingPath[i];
            }
            commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy() {
            if (fixedStreamingPath.IsCreated) {
                fixedStreamingPath.Dispose();
            }
        }

        protected override void OnUpdate() {
            if (!MiniAudioHandler.IsEngineInitialized()) {
                return;
            }

            var commandBuffer = commandBufferSystem.CreateCommandBuffer();
            new LoadSoundJob {
                LoadPathType = GetBufferTypeHandle<LoadPath>(true),
                AudioClipType = GetComponentTypeHandle<AudioClip>(true),
                EntityType = GetEntityTypeHandle(),
                MetadataType = GetComponentTypeHandle<AudioMetadata>(false),
                CommandBuffer = commandBuffer,
                StreamingPath = fixedStreamingPath
            }.Run(soundQuery);

            new StopSoundJob {
                AudioStateHistoryType = GetComponentTypeHandle<AudioStateHistory>(true),
                AudioClipType = GetComponentTypeHandle<AudioClip>(true),
                CommandBuffer = commandBuffer,
                EntityType = GetEntityTypeHandle()
            }.Run(soundQuery);

            new ManageAudioStateJob() {
                AudioStateHistoryType = GetComponentTypeHandle<AudioStateHistory>(true),
                AudioClipType = GetComponentTypeHandle<AudioClip>(true),
                MetadataType = GetComponentTypeHandle<AudioMetadata>(true),
                CommandBuffer = commandBuffer,
                LastSystemVersion = LastSystemVersion,
                EntityType = GetEntityTypeHandle()
            }.Run(soundQuery);
        }
    }
}
