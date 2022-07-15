using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MiniAudio.Entities.Systems {

    public struct AudioCommandBuffer : IDisposable {

        internal struct Payload {
            public uint ID;
            public float Volume;
        }

        [NativeDisableUnsafePtrRestriction]
        unsafe internal UnsafeList<Payload>* PlaybackIds;
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
        internal unsafe static void RequestInternal<T>(
            this ref AudioCommandBuffer buffer,
            T path,
            float volume) where T : unmanaged, IUTF8Bytes, INativeList<byte> {

            byte* head = path.GetUnsafePtr();

            var size = path.Length * 2;
            char* c = stackalloc char[size];
            Unicode.Utf8ToUtf16(head, path.Length, c, out int utf16Length, size);
            var id = math.hash(c, size);
            buffer.PlaybackIds->Add(new AudioCommandBuffer.Payload {
                ID = id,
                Volume = volume 
            });
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
