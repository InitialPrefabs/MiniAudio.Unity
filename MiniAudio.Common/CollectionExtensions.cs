using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MiniAudio.Common {

    internal static class CollectionExtensions {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T ElementAt<T>(this ref NativeArray<T> array, int i) where T : unmanaged {
            return ref UnsafeUtility.ArrayElementAsRef<T>((T*)array.GetUnsafePtr(), i);
        }
    }
}
