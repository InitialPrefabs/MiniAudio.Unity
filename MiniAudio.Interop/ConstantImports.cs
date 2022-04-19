using System;
using UnityEngine;

namespace MiniAudio.Interop {

    public static class ConstantImports {

        public const string MiniAudioLibPath = "/MiniAudio/Plugins/MiniAudio_Unity_Bindings.dll";
        public static IntPtr MiniAudioHandle => LibraryHandleInternal;

        static IntPtr LibraryHandleInternal;

        public static void Initialize() {
#if UNITY_EDITOR_WIN
            LibraryHandleInternal = LibraryHandler.InitializeLibrary(Application.dataPath + MiniAudioLibPath);
#endif
        }

        public static void Release() {
#if UNITY_EDITOR_WIN
            LibraryHandler.ReleaseLibrary(MiniAudioHandle);
#endif
        }
    }
}
