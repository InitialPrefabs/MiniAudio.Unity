using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniAudio.Interop {

#if UNITY_EDITOR_WIN
    public delegate void InitLogHandler(IntPtr log, IntPtr warn, IntPtr error);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogHandler(string message);

    public static unsafe class MiniAudioHandler {

#if UNITY_EDITOR_WIN
        internal static delegate* unmanaged[Cdecl]<bool> IsEngineInitializedHandler;
        internal static delegate* unmanaged[Cdecl]<void> InitializeEngineHandler;
        internal static delegate* unmanaged[Cdecl]<IntPtr, uint, IntPtr, uint> UnsafeLoadSoundHandler;
        internal static delegate* unmanaged[Cdecl]<uint, void> UnloadSoundHandler;
        internal static delegate* unmanaged[Cdecl]<uint, void> PlaySoundHandler;
        internal static delegate* unmanaged[Cdecl]<uint, bool, void> StopSoundHandler;
        internal static delegate* unmanaged[Cdecl]<uint, float, void> SetSoundVolumeHandler;
        internal static delegate* unmanaged[Cdecl]<uint, bool> IsSoundPlayingHandler;
        internal static delegate* unmanaged[Cdecl]<uint, bool> IsSoundFinishedHandler;
        internal static delegate* unmanaged[Cdecl]<void> ReleaseEngineHandler;
        internal static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> InitializeLoggerHandler;
        internal static delegate* unmanaged[Cdecl]<string, SoundLoadParameters, uint> LoadSoundHandler;

        const string StubName = "MiniAudio_Unity_Stub.dll";

        // Import our stub DLL
        [DllImport(StubName)]
        public static extern void InitializeEngineCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeEngineCheckCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeUnsafeLoadSoundCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeLoadSoundCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializePlaySoundCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeUnloadSoundCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeSetVolumeCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeStopSoundCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeIsSoundPlayingCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeIsSoundFinishedCallback(IntPtr callback);

        [DllImport(StubName)]
        public static extern void InitializeReleaseEngineCallback(IntPtr callback);

        [DllImport(StubName, EntryPoint = "ExecuteInitializeEngineCallback")]
        public static extern void InitializeEngine();

        [DllImport(StubName, EntryPoint = "ExecuteIsEngineInitializedCallback")]
        public static extern bool IsEngineInitialized();

        [DllImport(StubName, EntryPoint = "ExecuteUnsafeLoadSoundCallback")]
        public static extern uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParams);

        [DllImport(StubName, EntryPoint = "ExecuteLoadSoundCallback", CharSet = CharSet.Unicode)]
        public static extern uint LoadSound(string path, SoundLoadParameters loadParams);

        [DllImport(StubName, EntryPoint = "ExecutePlaySoundCallback")]
        public static extern void PlaySound(uint handle);

        [DllImport(StubName, EntryPoint = "ExecuteUnloadSoundCallback")]
        public static extern void UnloadSound(uint handle);
        
        [DllImport(StubName, EntryPoint = "ExecuteSetVolumeCallback")]
        public static extern void SetSoundVolume(uint handle, float volume);

        [DllImport(StubName, EntryPoint = "ExecuteStopSoundCallback")]
        public static extern void StopSound(uint handle, bool rewind);

        [DllImport(StubName, EntryPoint = "ExecuteIsSoundPlayingCallback")]
        public static extern bool IsSoundPlaying(uint handle);

        [DllImport(StubName, EntryPoint = "ExecuteIsSoundFinishedCallback")]
        public static extern bool IsSoundFinished(uint handle);

        [DllImport(StubName, EntryPoint = "ExecuteReleaseEngineCallback")]
        public static extern void ReleaseEngine();

        [DllImport(StubName)]
        public static extern void ReleaseAllCallbacks();
#endif

        internal static LogHandler DebugLogHandler;
        internal static LogHandler DebugWarnHandler;
        internal static LogHandler DebugErrorHandler;
        internal static IntPtr LogFunctionPtr;
        internal static IntPtr WarnFunctionPtr;
        internal static IntPtr ErrorFunctionPtr;

        [MonoPInvokeCallback(typeof(LogHandler))]
        static void Log(string msg) => Debug.Log(msg);

        [MonoPInvokeCallback(typeof(LogHandler))]
        static void Warn(string warn) => Debug.LogWarning(warn);

        [MonoPInvokeCallback(typeof(LogHandler))]
        static void Error(string error) => Debug.LogError(error);

        public static void InitializeLibrary() {
            DebugLogHandler = Log;
            DebugWarnHandler = Warn;
            DebugErrorHandler = Error;

            LogFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugLogHandler);
            WarnFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugWarnHandler);
            ErrorFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugErrorHandler);

#if UNITY_EDITOR_WIN
            var library = ConstantImports.MiniAudioHandle;

            IsEngineInitializedHandler = (delegate* unmanaged[Cdecl]<bool>)LibraryHandler.GetFunctionPointer(library, "IsEngineInitialized");
            InitializeEngineHandler    = (delegate* unmanaged[Cdecl]<void>)LibraryHandler.GetFunctionPointer(library, "InitializeEngine");
            UnsafeLoadSoundHandler     = (delegate* unmanaged[Cdecl]<IntPtr, uint, IntPtr, uint>)LibraryHandler.GetFunctionPointer(library, "UnsafeLoadSound");
            LoadSoundHandler           = (delegate* unmanaged[Cdecl]<string, SoundLoadParameters, uint>)LibraryHandler.GetFunctionPointer(library, "LoadSound");
            UnloadSoundHandler         = (delegate* unmanaged[Cdecl]<uint, void>)LibraryHandler.GetFunctionPointer(library, "UnloadSound");
            PlaySoundHandler           = (delegate* unmanaged[Cdecl]<uint, void>)LibraryHandler.GetFunctionPointer(library, "PlaySound");
            StopSoundHandler           = (delegate* unmanaged[Cdecl]<uint, bool, void>)LibraryHandler.GetFunctionPointer(library, "StopSound");
            SetSoundVolumeHandler      = (delegate* unmanaged[Cdecl]<uint, float, void>)LibraryHandler.GetFunctionPointer(library, "SetSoundVolume");
            IsSoundPlayingHandler      = (delegate* unmanaged[Cdecl]<uint, bool>)LibraryHandler.GetFunctionPointer(library, "IsSoundPlaying");
            IsSoundFinishedHandler     = (delegate* unmanaged[Cdecl]<uint, bool>)LibraryHandler.GetFunctionPointer(library, "IsSoundFinished");
            ReleaseEngineHandler       = (delegate* unmanaged[Cdecl]<void>)LibraryHandler.GetFunctionPointer(library, "ReleaseEngine");
            InitializeLoggerHandler    = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)LibraryHandler.GetFunctionPointer(library, "InitializeLogger");

            InitializeEngineCallback((IntPtr)InitializeEngineHandler);
            InitializeEngineCheckCallback((IntPtr)IsEngineInitializedHandler);
            InitializeUnsafeLoadSoundCallback((IntPtr)UnsafeLoadSoundHandler);
            InitializeLoadSoundCallback((IntPtr)LoadSoundHandler);
            InitializeUnloadSoundCallback((IntPtr)UnloadSoundHandler);
            InitializePlaySoundCallback((IntPtr)PlaySoundHandler);
            InitializeStopSoundCallback((IntPtr)StopSoundHandler);
            InitializeSetVolumeCallback((IntPtr)SetSoundVolumeHandler);
            InitializeStopSoundCallback((IntPtr)StopSoundHandler);
            InitializeIsSoundPlayingCallback((IntPtr)IsSoundPlayingHandler);
            InitializeIsSoundFinishedCallback((IntPtr)IsSoundFinishedHandler);
            InitializeReleaseEngineCallback((IntPtr)ReleaseEngineHandler);
#endif
            InitializeLogger(LogFunctionPtr, WarnFunctionPtr, ErrorFunctionPtr);
        }

        public static void ReleaseLibrary() {
#if UNITY_EDITOR_WIN
            IsEngineInitializedHandler = null;
            InitializeEngineHandler = null;
            UnsafeLoadSoundHandler = null;
            LoadSoundHandler = null;
            UnloadSoundHandler = null;
            PlaySoundHandler = null;
            StopSoundHandler = null;
            SetSoundVolumeHandler = null;
            IsSoundPlayingHandler = null;
            IsSoundFinishedHandler = null;
            ReleaseEngineHandler = null;
            InitializeLoggerHandler = null;

            ReleaseAllCallbacks();
#endif
        }

#if UNITY_EDITOR_WIN
        static void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error) {
            InitializeLoggerHandler(log, warn, error);
        }
        
        /*
        public static bool IsEngineInitialized() {
            if (IsEngineInitializedHandler != null) {
                return IsEngineInitializedHandler();
            }
            return false;
        }

        public static void InitializeEngine() {
            if (InitializeEngineHandler != null) {
                InitializeEngineHandler();
            }
        }

        public static uint LoadSound(string path, SoundLoadParameters loadParams) {
            if (UnsafeLoadSoundHandler == null) {
                return uint.MaxValue;
            }

            unsafe {
                var loadParamsPtr = new IntPtr(&loadParams);
                fixed (char* head = path) {
                    var pathPtr = new IntPtr(head);
                    return UnsafeLoadSound(
                        pathPtr,
                        (uint)(path.Length * sizeof(char)),
                        loadParamsPtr);
                }
            }
        }

        public static uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParams) {
            if (UnsafeLoadSoundHandler == null) {
                return uint.MaxValue;
            }
            return UnsafeLoadSoundHandler(path, sizeInBytes, loadParams);
        }

        public static void UnloadSound(uint handle) {
            if (UnloadSoundHandler != null) {
                UnloadSoundHandler(handle);
            }
        }

        public static void PlaySound(uint handle) {
            if (PlaySoundHandler != null) {
                PlaySoundHandler(handle);
            }
        }

        public static void StopSound(uint handle, bool rewind) {
            if (StopSoundHandler != null) {
                StopSoundHandler(handle, rewind);
            }
        }

        public static void SetSoundVolume(uint handle, float volume) {
            if (SetSoundVolumeHandler != null) {
                SetSoundVolumeHandler(handle, volume);
            }
        }

        public static bool IsSoundPlaying(uint handle) {
            if (IsSoundPlayingHandler == null) {
                return false;
            }
            return IsSoundPlayingHandler(handle);
        }

        public static bool IsSoundFinished(uint handle) {
            if (IsSoundFinishedHandler != null) {
                return false;
            }
            return IsSoundFinishedHandler(handle);
        }

        public static void ReleaseEngine() {
            if (ReleaseEngineHandler != null) {
                ReleaseEngineHandler();
            }
        }
        */
#else
        [DllImport("MiniAudio_Unity_Bindings.dll")]
        static extern void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern bool IsEngineInitialized();

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void InitializeEngine();

        [DllImport("MiniAudio_Unity_Bindings.dll", CharSet = CharSet.Unicode)]
        public static extern uint LoadSound(string path, SoundLoadParameters loadParams);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParameters);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void UnloadSound(uint handle);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void PlaySound(uint handle);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void StopSound(uint handle, bool rewind);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void SetSoundVolume(uint handle, float volume);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern bool IsSoundPlaying(uint handle);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern bool IsSoundFinished(uint handle);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void ReleaseEngine();
#endif
    }
}
