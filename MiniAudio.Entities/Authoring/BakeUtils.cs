using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MiniAudio.Entities {

    public static class BakeUtils {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hash128 ComputeHash(string contents) {
            var fs = new FixedString4096Bytes(contents);
            return ComputeHash(fs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Hash128 ComputeHash<T>(T path) where T : unmanaged, IUTF8Bytes, INativeList<byte> {
            var utf16Capacity = path.Length * 2;
            var utf16Buffer = new NativeArray<char>(utf16Capacity, Allocator.Temp);
            Unicode.Utf8ToUtf16(
                path.GetUnsafePtr(),
                path.Length,
                (char*)utf16Buffer.GetUnsafePtr(),
                out var utf16Length,
                utf16Capacity);
            return UnityEngine.Hash128.Compute(utf16Buffer, 0, utf16Length);
        }
    }
}
