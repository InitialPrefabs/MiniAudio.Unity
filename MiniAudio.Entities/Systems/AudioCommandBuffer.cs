using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MiniAudio.Entities.Systems {

    public struct AudioCommandBuffer : IDisposable {

        unsafe internal UnsafeList<uint>* PlaybackIds;
        internal readonly Allocator Allocator;

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
        internal unsafe static void RequestInternal<T>(
            this ref AudioCommandBuffer buffer, T path) where T : unmanaged, IUTF8Bytes, INativeList<byte> {
            byte* head = path.GetUnsafePtr();
            var id = math.hash(head, path.Length);
            buffer.PlaybackIds->Add(in id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(this ref AudioCommandBuffer buffer, FixedString32Bytes path) {
            buffer.RequestInternal(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(this ref AudioCommandBuffer buffer, FixedString64Bytes path) {
            buffer.RequestInternal(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(this ref AudioCommandBuffer buffer, FixedString128Bytes path) {
            buffer.RequestInternal(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(this ref AudioCommandBuffer buffer, FixedString512Bytes path) {
            buffer.RequestInternal(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Request(this ref AudioCommandBuffer buffer, FixedString4096Bytes path) {
            buffer.RequestInternal(path);
        }
    }
}
