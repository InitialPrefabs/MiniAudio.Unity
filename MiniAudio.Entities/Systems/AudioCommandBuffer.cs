using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MiniAudio.Entities.Systems {

    public struct AudioCommandBuffer : IDisposable {

        [NativeDisableUnsafePtrRestriction]
        unsafe internal UnsafeList<uint>* PlaybackIds;
        internal readonly Allocator Allocator;

        public AudioCommandBuffer(Allocator allocator) {
            Allocator = allocator;
            unsafe {
                PlaybackIds = (UnsafeList<uint>*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<UnsafeList<uint>>(),
                    UnsafeUtility.AlignOf<UnsafeList<uint>>(),
                    allocator);

                *PlaybackIds = new UnsafeList<uint>(10, allocator);
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

        public static ref uint ElementAt(this ref AudioCommandBuffer buffer, int i) {
            unsafe {
                return ref buffer.PlaybackIds->ElementAt(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void RequestInternal<T>(this ref AudioCommandBuffer buffer, T path)
                where T : unmanaged, IUTF8Bytes, INativeList<byte> {
            byte* head = path.GetUnsafePtr();

            var size = path.Length * 2;
            char* c = stackalloc char[size];
            Unicode.Utf8ToUtf16(head, path.Length, c, out int utf16Length, size);
            var id = math.hash(c, path.Length * 2);
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
