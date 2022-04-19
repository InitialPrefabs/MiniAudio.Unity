using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniAudio.Interop {

    public static class LibraryHandler {
#if UNITY_EDITOR_WIN
        [DllImport("kernel32")]
        static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32")]
        static extern IntPtr GetProcAddress(IntPtr libraryHandle, string symbolName);

        [DllImport("kernel32")]
        static extern bool FreeLibrary(IntPtr libraryHandle);

        public static IntPtr InitializeLibrary(string path) {
            IntPtr handle = LoadLibrary(path);

            if (handle == IntPtr.Zero) {
                throw new Exception("Couldn't open native library: " + path);
            }
            return handle;
        }

        public static void ReleaseLibrary(IntPtr libraryPtr) {
            Debug.Log("Closing external library");
            FreeLibrary(libraryPtr);
        }

        public static T GetDelegate<T>(IntPtr libraryPtr, string functionName) where T : class {
            IntPtr symbol = GetProcAddress(libraryPtr, functionName);

            if (symbol == IntPtr.Zero) {
                Debug.LogError($"Could not find function: {functionName}");
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(symbol, typeof(T)) as T;
        }
#endif
    }
}
