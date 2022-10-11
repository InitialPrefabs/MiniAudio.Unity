using System;
using System.Runtime.CompilerServices;
using MiniAudio.Common;
using MiniAudio.Interop;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace MiniAudio.Entities.Systems {
    
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    public partial struct OneShotAudioSystemV2 : ISystem {

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

            public unsafe void Execute(
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
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state) {
            if (entityLookUp.IsCreated) {
                entityLookUp.Dispose();
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
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
