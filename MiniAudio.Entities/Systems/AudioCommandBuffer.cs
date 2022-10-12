// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Hash128 = Unity.Entities.Hash128;

namespace MiniAudio.Entities.Systems {

    public struct AudioCommandBuffer : IDisposable {

        internal struct Payload {
            public Hash128 Hash128;
            public float Volume;
        }

        [NativeDisableUnsafePtrRestriction]
        internal unsafe UnsafeList<Payload>* PlaybackIds;
        internal readonly Allocator Allocator;

        public AudioCommandBuffer(Allocator allocator) {
            Allocator = allocator;
            unsafe {
                PlaybackIds = (UnsafeList<Payload>*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<UnsafeList<uint>>(),
                    UnsafeUtility.AlignOf<UnsafeList<uint>>(),
                    allocator);

                *PlaybackIds = new UnsafeList<Payload>(10, allocator);
            }
        }

        public void Dispose() {
            unsafe {
                if (PlaybackIds != null) {
                    if (PlaybackIds->IsCreated) {
                        PlaybackIds->Dispose();
                    }
                    UnsafeUtility.Free(PlaybackIds, Allocator);
                    PlaybackIds = null;
                }
            }
        }
    }

    public static class AudioCommandBufferExtensions {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void RequestInternal<T>(
            this ref AudioCommandBuffer buffer,
            T path,
            float volume) where T : unmanaged, IUTF8Bytes, INativeList<byte> {
            
            var hash128 = BakeUtils.ComputeHash(path);
            buffer.PlaybackIds->Add(new AudioCommandBuffer.Payload {
                Hash128 = hash128,
                Volume = volume
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            in Hash128 hash128,
            float volume) {

            unsafe {
                buffer.PlaybackIds->Add(new AudioCommandBuffer.Payload {
                    Hash128 = hash128,
                    Volume = volume
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            FixedString32Bytes path,
            float volume) {
            buffer.RequestInternal(path, volume);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            FixedString64Bytes path,
            float volume) {
            buffer.RequestInternal(path, volume);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            FixedString128Bytes path,
            float volume) {
            buffer.RequestInternal(path, volume);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            FixedString512Bytes path,
            float volume) {
            buffer.RequestInternal(path, volume);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(
            this ref AudioCommandBuffer buffer,
            FixedString4096Bytes path,
            float volume) {
            buffer.RequestInternal(path, volume);
        }
    }
}
