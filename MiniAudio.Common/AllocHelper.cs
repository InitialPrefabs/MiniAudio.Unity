using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MiniAudio.Common {
    
    public static class AllocHelper {

        public static unsafe T* InitializePersistentPointer<T>() where T : unmanaged {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>(),
                Allocator.Persistent);
        }
    }
}
