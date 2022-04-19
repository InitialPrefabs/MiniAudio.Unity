using System;
using UnityEngine;

namespace MiniAudio.Interop {

    public static class ConstantImports {

        public static readonly string[] Paths = new [] { 
            "/MiniAudio.Unity/Plugins/MiniAudio_Unity_Bindings.dll",
            "/Scripts/MiniAudio.Unity/Plugins/MiniAudio_Unity_Bindings.dll"
        };
        public static IntPtr MiniAudioHandle => LibraryHandleInternal;

        static IntPtr LibraryHandleInternal;

        public static void Initialize() {
#if UNITY_EDITOR_WIN
            for (int i = 0; i < Paths.Length; i++) {
                var handle = LibraryHandler.InitializeLibrary(Application.dataPath + Paths[i]);

                if (handle != IntPtr.Zero) {
                    LibraryHandleInternal = handle;
                    break;
                }
            }
#endif
        }

        public static void Release() {
#if UNITY_EDITOR_WIN
            LibraryHandler.ReleaseLibrary(MiniAudioHandle);
#endif
        }
    }
}
