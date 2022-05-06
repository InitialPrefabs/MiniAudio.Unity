using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniAudio.Interop {

#if MINIAUDIO_DEVELOP && UNITY_EDITOR_WIN
    public delegate bool InitCheckHandler();
    public delegate void VoidHandler();
    public delegate uint LoadHandler(string path, SoundLoadParameters loadParams);
    public delegate void SoundHandler(uint handle);
    public delegate void StopSoundHandler(uint handle, bool rewind);
    public delegate bool BoolHandler(uint handle);
    public delegate void VolumeHandler(uint handle, float volume);
    public delegate uint UnsafeLoadHandler(IntPtr path, uint sizeInBytes, IntPtr loadParams);
    public delegate void InitLogHandler(IntPtr log, IntPtr warn, IntPtr error);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogHandler(string message);

    public static unsafe class MiniAudioHandler {

#if MINIAUDIO_DEVELOP && UNITY_EDITOR_WIN
        internal static InitCheckHandler  InitCheckHandler;
        internal static VoidHandler       InitEngineHandler;
        internal static LoadHandler       LoadSoundHandler;
        internal static UnsafeLoadHandler UnsafeLoadHandler;
        internal static SoundHandler      UnloadSoundHandler;
        internal static SoundHandler      PlaySoundHandler;
        internal static StopSoundHandler  StopSoundHandler;
        internal static VoidHandler       ReleaseEngineHandler;
        internal static BoolHandler       SoundPlayingHandler;
        internal static BoolHandler       SoundFinishedHandler;
        internal static VolumeHandler     SoundVolumeHandler;
        internal static InitLogHandler    InitLoggerHandler;
#endif

        internal static LogHandler DebugLogHandler;
        internal static LogHandler DebugWarnHandler;
        internal static LogHandler DebugErrorHandler;
        internal static IntPtr LogFunctionPtr;
        internal static IntPtr WarnFunctionPtr;
        internal static IntPtr ErrorFunctionPtr;

        static void Log(string msg) => Debug.Log(msg);
        static void Warn(string warn) => Debug.LogWarning(warn);
        static void Error(string error) => Debug.LogError(error);

        public static void InitializeLibrary() {
            DebugLogHandler   = Log;
            DebugWarnHandler  = Warn;
            DebugErrorHandler = Error;

            LogFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugLogHandler);
            WarnFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugWarnHandler);
            ErrorFunctionPtr = Marshal.GetFunctionPointerForDelegate(DebugErrorHandler);

#if MINIAUDIO_DEVELOP && UNITY_EDITOR_WIN
            InitCheckHandler     = null;
            InitEngineHandler    = null;
            LoadSoundHandler     = null;
            UnsafeLoadHandler    = null;
            UnloadSoundHandler   = null;
            PlaySoundHandler     = null;
            StopSoundHandler     = null;
            ReleaseEngineHandler = null;
            SoundPlayingHandler  = null;
            SoundFinishedHandler = null;
            SoundVolumeHandler   = null;
            InitLoggerHandler    = null;

            var library          = ConstantImports.MiniAudioHandle;
            InitCheckHandler     += LibraryHandler.GetDelegate<InitCheckHandler>(library, "IsEngineInitialized");
            InitEngineHandler    += LibraryHandler.GetDelegate<VoidHandler>(library, "InitializeEngine");
            LoadSoundHandler     += LibraryHandler.GetDelegate<LoadHandler>(library, "LoadSound");
            UnsafeLoadHandler    += LibraryHandler.GetDelegate<UnsafeLoadHandler>(library, "UnsafeLoadSound");
            UnloadSoundHandler   += LibraryHandler.GetDelegate<SoundHandler>(library, "UnloadSound");
            PlaySoundHandler     += LibraryHandler.GetDelegate<SoundHandler>(library, "PlaySound");
            StopSoundHandler     += LibraryHandler.GetDelegate<StopSoundHandler>(library, "StopSound");
            ReleaseEngineHandler += LibraryHandler.GetDelegate<VoidHandler>(library, "ReleaseEngine");
            SoundPlayingHandler  += LibraryHandler.GetDelegate<BoolHandler>(library, "IsSoundPlaying");
            SoundFinishedHandler += LibraryHandler.GetDelegate<BoolHandler>(library, "IsSoundFinished");
            SoundVolumeHandler   += LibraryHandler.GetDelegate<VolumeHandler>(library, "SetSoundVolume");
            InitLoggerHandler    += LibraryHandler.GetDelegate<InitLogHandler>(library, "InitializeLogger");
#endif
            InitializeLogger(LogFunctionPtr, WarnFunctionPtr, ErrorFunctionPtr);
        }

        public static void ReleaseLibrary() {
#if MINIAUDIO_DEVELOP && UNITY_EDITOR_WIN
            var library          = ConstantImports.MiniAudioHandle;
            InitCheckHandler     -= LibraryHandler.GetDelegate<InitCheckHandler>(library, "IsEngineInitialized");
            InitEngineHandler    -= LibraryHandler.GetDelegate<VoidHandler>(library, "InitializeEngine");
            LoadSoundHandler     -= LibraryHandler.GetDelegate<LoadHandler>(library, "LoadSound");
            UnsafeLoadHandler    -= LibraryHandler.GetDelegate<UnsafeLoadHandler>(library, "UnsafeLoadSound");
            UnloadSoundHandler   -= LibraryHandler.GetDelegate<SoundHandler>(library, "UnloadSound");
            PlaySoundHandler     -= LibraryHandler.GetDelegate<SoundHandler>(library, "PlaySound");
            StopSoundHandler     -= LibraryHandler.GetDelegate<StopSoundHandler>(library, "StopSound");
            ReleaseEngineHandler -= LibraryHandler.GetDelegate<VoidHandler>(library, "ReleaseEngine");
            SoundPlayingHandler  -= LibraryHandler.GetDelegate<BoolHandler>(library, "IsSoundPlaying");
            SoundFinishedHandler -= LibraryHandler.GetDelegate<BoolHandler>(library, "IsSoundFinished");
            SoundVolumeHandler   -= LibraryHandler.GetDelegate<VolumeHandler>(library, "SetSoundVolume");
            InitLoggerHandler    -= LibraryHandler.GetDelegate<InitLogHandler>(library, "InitializeLogger");

            InitCheckHandler     = null;
            InitEngineHandler    = null;
            LoadSoundHandler     = null;
            UnsafeLoadHandler    = null;
            UnloadSoundHandler   = null;
            PlaySoundHandler     = null;
            StopSoundHandler     = null;
            ReleaseEngineHandler = null;
            SoundPlayingHandler  = null;
            SoundFinishedHandler = null;
            SoundVolumeHandler   = null;
            InitLoggerHandler    = null;
#endif
        }

#if MINIAUDIO_DEVELOP && UNITY_EDITOR_WIN
        static void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error) {
            InitLoggerHandler?.Invoke(log, warn, error);
        }

        public static bool IsEngineInitialized() {
            if (InitCheckHandler != null) {
                return InitCheckHandler.Invoke();
            }
            return false;
        }

        public static void InitializeEngine() {
            InitEngineHandler?.Invoke();
        }

        public static uint LoadSound(string path, SoundLoadParameters loadParams) {
            if (LoadSoundHandler == null) {
                return uint.MaxValue;
            }
            return LoadSoundHandler.Invoke(path, loadParams);
        }

        public static uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParams) {
            if (UnsafeLoadHandler == null) {
                return uint.MaxValue;
            }
            return UnsafeLoadHandler.Invoke(path, sizeInBytes, loadParams);
        }

        public static void UnloadSound(uint handle) {
            UnloadSoundHandler?.Invoke(handle);
        }

        public static void PlaySound(uint handle) {
            PlaySoundHandler?.Invoke(handle);
        }

        public static void StopSound(uint handle, bool rewind) {
            StopSoundHandler?.Invoke(handle, rewind);
        }

        public static void SetSoundVolume(uint handle, float volume) {
            SoundVolumeHandler?.Invoke(handle, volume);
        }

        public static bool IsSoundPlaying(uint handle) {
            if (SoundPlayingHandler == null) {
                return false;
            }
            return SoundPlayingHandler.Invoke(handle);
        }

        public static bool IsSoundFinished(uint handle) {
            if (SoundFinishedHandler == null) {
                return false;
            }
            return SoundFinishedHandler.Invoke(handle);
        }

        public static void ReleaseEngine() {
            ReleaseEngineHandler?.Invoke();
        }
#else
        [DllImport("MiniAudio_Unity_Bindings.dll")]
        static extern void InitializeLogger(IntPtr log, IntPtr warn, IntPtr error);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern bool IsEngineInitialized();

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void InitializeEngine();

        [DllImport("MiniAudio_Unity_Bindings.dll")]
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
