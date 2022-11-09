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

        /// <summary>
        /// Creates a command buffer to record audio commands.
        /// </summary>
        /// <param name="singleton">The Singleton component data that is aliased to the pendingBuffers.</param>
        /// <param name="allocator">The lifetime of the Allocator</param>
        /// <returns>An instance of a CommandBuffer that is tracked by the <see cref="OneShotAudioSystem"/></returns>
        public static AudioCommandBuffer CreateCommandBuffer(
            this ref OneShotAudioSystem.Singleton singleton, Allocator allocator = Allocator.TempJob) {

            var commandBuffer = new AudioCommandBuffer(allocator);
            singleton.PendingBuffers->Add(commandBuffer);
            return commandBuffer;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public unsafe partial struct OneShotAudioSystem : ISystem {

        public struct Singleton : IComponentData {
            internal UnsafeList<AudioCommandBuffer>* PendingBuffers;
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
                    if (EntityLookUp.TryGetValue(payload.Hash128, out var entity)) {
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
                    ref var pathID = ref path.HashedPath();
                    ref var pathBlob = ref path.PathBlobArray();
                    var poolDesc = poolDescriptors[i];

                    // Construct the path
                    absolutePath.Clear();
                    absolutePath.AddRangeNoResize(
                        StreamingAssetsHelper.Path.Data.Ptr, StreamingAssetsHelper.Path.Data.Length);
                    absolutePath.AddRange(pathBlob.GetUnsafePtr(), pathBlob.Length);

                    CommandBuffer.AddComponent(entity, new AudioPoolID {
                        Value = pathID
                    });
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

        [BurstCompile]
        struct RemoveDeadAudioPoolJob : IJobChunk {

            [ReadOnly]
            public ComponentTypeHandle<AudioPoolID> AudioPoolIDType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [WriteOnly]
            public NativeParallelHashMap<Hash128, Entity> EntityLookUp;

            public EntityCommandBuffer CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var audioPoolIds = chunk.GetNativeArray(AudioPoolIDType);
                var entities = chunk.GetNativeArray(EntityType);
                for (int i = 0; i < chunk.Count; i++) {
                    var audioPoolId = audioPoolIds[i];

                    if (EntityLookUp.Remove(audioPoolId.Value)) {
                        var entity = entities[i];
                        CommandBuffer.RemoveComponent<AudioPoolID>(entity);
                        CommandBuffer.DestroyEntity(entity);
                    }
                }
            }
        }

        [BurstCompile]
        struct RecycleUsedHandlesJob : IJobChunk {

            public BufferTypeHandle<UsedHandle> UsedHandleType;

            public BufferTypeHandle<FreeHandle> FreeHandleType;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var usedHandleAccessor = chunk.GetBufferAccessor(UsedHandleType);
                var freeHandleAccessor = chunk.GetBufferAccessor(FreeHandleType);
                for (int i = 0; i < chunk.Count; i++) {
                    var usedHandles = usedHandleAccessor[i];
                    var freeHandles = freeHandleAccessor[i];

                    for (int m = usedHandles.Length - 1; m >= 0; m--) {
                        var usedHandle = usedHandles[m];

                        if (MiniAudioHandler.IsSoundFinished(usedHandle)) {
                            usedHandles.RemoveAt(m);
                            freeHandles.Add(usedHandle);
                        }
                    }
                }
            }
        }

        const int InitialBufferSize = 64;

        NativeParallelHashMap<Hash128, Entity> entityLookUp;
        UnsafeList<AudioCommandBuffer>* pendingBuffers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RegisterSingletonBuffer(
            ref SystemState state,
            UnsafeList<AudioCommandBuffer>* playbackBuffer) {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new Singleton {
                PendingBuffers = playbackBuffer,
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
            entityLookUp = new NativeParallelHashMap<Hash128, Entity>(InitialBufferSize, Allocator.Persistent);
            pendingBuffers = AllocHelper.InitializePersistentPointer<UnsafeList<AudioCommandBuffer>>();
            *pendingBuffers = new UnsafeList<AudioCommandBuffer>(InitialBufferSize, Allocator.Persistent);

            RegisterSingletonBuffer(ref state, pendingBuffers);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {
            if (entityLookUp.IsCreated) {
                entityLookUp.Dispose();
            }

            if (pendingBuffers != null) {
                pendingBuffers->Dispose();
                UnsafeUtility.Free(pendingBuffers, Allocator.Persistent);
                pendingBuffers = null;
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!MiniAudioHandler.IsEngineInitialized()) {
                return;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (pendingBuffers == null) {
                Debug.LogError("The PendingBuffers were not initialized!");
                return;
            }
#endif
            var _ = SystemAPI.GetSingletonRW<Singleton>();
            var uninitializedPoolQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioPoolDescriptor>()
                .WithAllRW<FreeHandle>()
                .WithAll<UsedHandle>()
                .WithAll<AliasSoundLoadParameters>()
                .WithNone<AudioPoolID>()
                .Build();

            var freeHandles = SystemAPI.GetBufferLookup<FreeHandle>();
            var usedHandles = SystemAPI.GetBufferLookup<UsedHandle>();
            new PlaybackPendingBuffersJob {
                PendingBuffers = pendingBuffers,
                EntityLookUp = entityLookUp,
                FreeHandles = freeHandles,
                UsedHandles = usedHandles
            }.Run(pendingBuffers->Length);

            FlushPendingBuffers(pendingBuffers);

            var initializedPoolQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioPoolDescriptor>()
                .WithAllRW<FreeHandle>()
                .WithAll<UsedHandle>()
                .WithAll<AliasSoundLoadParameters>()
                .WithAll<AudioPoolID>()
                .Build();

            var usedTypeHandle = state.GetBufferTypeHandle<UsedHandle>(false);
            var freeTypeHandle = state.GetBufferTypeHandle<FreeHandle>(false);
            
            new RecycleUsedHandlesJob {
                UsedHandleType = usedTypeHandle,
                FreeHandleType = freeTypeHandle
            }.Run(initializedPoolQuery);
            
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var entityType = state.GetEntityTypeHandle();
            var audioPoolIDType = state.GetComponentTypeHandle<AudioPoolID>(true);

            var deadPooledAudioQuery = SystemAPI.QueryBuilder()
                .WithAll<AudioPoolID>()
                .WithNone<FreeHandle>()
                .WithNone<UsedHandle>()
                .WithNone<AliasSoundLoadParameters>()
                .Build();
            
            new RemoveDeadAudioPoolJob {
                EntityType = entityType,
                AudioPoolIDType = audioPoolIDType,
                CommandBuffer = commandBuffer,
                EntityLookUp = entityLookUp
            }.Run(deadPooledAudioQuery);

            var pathType = state.GetComponentTypeHandle<Path>(true);
            var soundParamsType = state.GetComponentTypeHandle<AliasSoundLoadParameters>(true);
            var audioPoolDescType = state.GetComponentTypeHandle<AudioPoolDescriptor>(true);
            var freeHandlesType = state.GetBufferTypeHandle<FreeHandle>(false);

            new InitializePooledAudioJob {
                EntityLookUp = entityLookUp,
                PathType = pathType,
                EntityType = entityType,
                SoundLoadParamType = soundParamsType,
                PoolDescriptorType = audioPoolDescType,
                FreeHandleType = freeHandlesType,
                CommandBuffer = commandBuffer
            }.Run(uninitializedPoolQuery);
        }
    }
}
