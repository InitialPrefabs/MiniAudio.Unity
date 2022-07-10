using System;
using MiniAudio.Common;
using MiniAudio.Interop;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Systems {

    public partial class OneShotAudioSystem : SystemBase {

        [BurstCompile]
        unsafe struct InitializePooledAudioJob : IJobEntityBatch {

            [ReadOnly]
            public NativeArray<char> StreamingPath;

            [ReadOnly]
            public ComponentTypeHandle<Path> PathType;

            [ReadOnly]
            public BufferTypeHandle<SoundLoadParametersElement> SoundLoadParamsType;

            public ComponentTypeHandle<AudioPoolDescriptor> PoolDescriptorType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;

            public BufferTypeHandle<UsedHandle> UsedHandleType;

            [NativeDisableContainerSafetyRestriction]
            NativeList<char> fullPath;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex) {
                if (!fullPath.IsCreated) {
                    fullPath = new NativeList<char>(StreamingPath.Length, Allocator.Temp);
                }

                var freeHandles = batchInChunk.GetBufferAccessor(FreeHandleType);
                var usedHandles = batchInChunk.GetBufferAccessor(UsedHandleType);
                var soundLoadParams = batchInChunk.GetBufferAccessor(SoundLoadParamsType);
                var poolDescriptors = batchInChunk.GetNativeArray(PoolDescriptorType);
                var paths = batchInChunk.GetNativeArray(PathType);

                for (int i = 0; i < batchInChunk.Count; i++) {
                    ref var poolDescriptor = ref poolDescriptors.ElementAt(i);
                    if (poolDescriptor.IsLoaded) {
                        continue;
                    }

                    var loadPath = paths[i];
                    var freeHandleBuffer = freeHandles[i];
                    var usedHandleBuffer = usedHandles[i];

                    freeHandleBuffer.ResizeUninitialized(poolDescriptors.Length);

                    if (loadPath.IsStreamingAssets) {
                        fullPath.AddRangeNoResize(
                            StreamingPath.GetUnsafeReadOnlyPtr(),
                            StreamingPath.Length);
                    }

                    ref var path = ref loadPath.Value.Value.Path;
                    fullPath.AddRange(path.GetUnsafePtr(), path.Length);

                    var soundLoadParamArray = soundLoadParams[i].AsNativeArray();

                    for (int j = 0; j < soundLoadParamArray.Length; j++) {
                        var soundLoadParam = soundLoadParamArray[j];
                        var handle = MiniAudioHandler.UnsafeLoadSound(
                            new IntPtr(fullPath.GetUnsafeReadOnlyPtr<char>()),
                            (uint)fullPath.Length,
                            new IntPtr(&soundLoadParam));

                        if (handle != uint.MaxValue) {
                            freeHandleBuffer.Add(new FreeHandle { Value = handle });
                        }
                    }

                    poolDescriptor.IsLoaded = true;
                    fullPath.Clear();
                }
            }
        }

        EntityQuery uninitializeAudioPoolQuery;
        NativeArray<char> fixedStreamingPath;

        protected override void OnCreate() {
            uninitializeAudioPoolQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<AudioPoolDescriptor>(),
                    ComponentType.ReadOnly<FreeHandle>(),
                    ComponentType.ReadOnly<UsedHandle>(),
                    ComponentType.ReadOnly<SoundLoadParametersElement>()
                }
            });

            var streamingPath = Application.streamingAssetsPath;
            fixedStreamingPath = new NativeArray<char>(streamingPath.Length, Allocator.Persistent);

            for (int i = 0; i < streamingPath.Length; i++) {
                fixedStreamingPath[i] = streamingPath[i];
            }
        }

        protected override void OnDestroy() {
            if (fixedStreamingPath.IsCreated) {
                fixedStreamingPath.Dispose();
            }
        }

        protected override void OnUpdate() {
            new InitializePooledAudioJob {
                FreeHandleType = GetBufferTypeHandle<FreeHandle>(false),
                UsedHandleType = GetBufferTypeHandle<UsedHandle>(false),
                PathType = GetComponentTypeHandle<Path>(true),
                PoolDescriptorType = GetComponentTypeHandle<AudioPoolDescriptor>(false),
                SoundLoadParamsType = GetBufferTypeHandle<SoundLoadParametersElement>(true),
                StreamingPath = fixedStreamingPath
            }.Run(uninitializeAudioPoolQuery);
        }
    }
}

