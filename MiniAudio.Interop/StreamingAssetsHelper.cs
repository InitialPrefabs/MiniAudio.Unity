using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace MiniAudio {

    internal unsafe struct CharPointer : IDisposable {

        public bool IsCreated => Ptr != null;

        public char* Ptr;
        public int Length;

        public CharPointer(string content) {
            Length = content.Length;
            fixed (char* ptr = content) {
                Ptr = (char*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<char>() * Length,
                    UnsafeUtility.AlignOf<char>(),
                    Allocator.Persistent);
                UnsafeUtility.MemCpy(Ptr, ptr, UnsafeUtility.SizeOf<char>() * Length);
            }
        }

        public char this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index >= Length || index < 0) {
                    throw new IndexOutOfRangeException(
                        $"You are attempting to access index: {index} " +
                        $"when it should be between 0 and {Length}!");
                }
#endif
                return UnsafeUtility.ReadArrayElement<char>(Ptr, index);
            }
        }

        public void Dispose() {
            if (Ptr != null) {
                UnsafeUtility.Free(Ptr, Allocator.Persistent);
                Ptr = null;
                Length = 0;
            }
        }

        public static implicit operator string(CharPointer charPointer) => 
            new(charPointer.Ptr, 0, charPointer.Length);

        public static void CopyTo(
            ref NativeArray<char> array, in CharPointer charPointer, int startOffset = 0) {
            for (int i = 0; i < charPointer.Length; i++) {
                array[startOffset + i] = charPointer[i];
            }
        }
    }

    internal class StreamingAssetsHelper {

        class Key { }

        public static readonly SharedStatic<CharPointer> Path = 
            SharedStatic<CharPointer>.GetOrCreate<StreamingAssetsHelper, Key>();

        internal static void Initialize() {
            if (Path.Data.IsCreated) {
                throw new InvalidOperationException(
                    "You are attempting to reallocate a Path which already exists!");
            }
            Path.Data = new CharPointer(Application.streamingAssetsPath);
        }

        internal static void Release() {
            Path.Data.Dispose();
        }
    }
}
