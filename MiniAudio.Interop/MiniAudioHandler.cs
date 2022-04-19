using System;

namespace MiniAudio.Interop {

    public static unsafe class MiniAudioHandler {
#if UNITY_EDITOR_WIN && MINIAUDIO_DEVELOP
        #region Delegates
        public delegate bool MiniEngineInitializationCheckHandler();
        public delegate void MiniAudioEngineHandler();
        public delegate uint MiniAudioLoadHandler(string path, SoundLoadParameters loadParams);
        public delegate void MiniSoundHandler(uint handle);
        public delegate void MiniStopSoundHandler(uint handle, bool rewind);
        public delegate bool MiniSoundStateHandler(uint handle);
        public delegate void MiniSoundVolumeHandler(uint handle, float volume);
        public delegate uint UnsafeMiniAudioLoadHandler(IntPtr path, uint sizeInBytes, IntPtr loadParams);
        #endregion

        static MiniEngineInitializationCheckHandler InitializationCheckHandler;
        static MiniAudioEngineHandler InitializationHandler;
        static MiniAudioLoadHandler LoadSoundHandler;
        static UnsafeMiniAudioLoadHandler UnsafeLoadSoundHandler;
        static MiniSoundHandler PlaySoundHandler;
        static MiniStopSoundHandler StopSoundHandler;
        static MiniAudioEngineHandler ReleaseHandler;
        static MiniSoundStateHandler SoundPlayingHandler;
        static MiniSoundStateHandler SoundFinishedHandler;
        static MiniSoundVolumeHandler SoundVolumeHandler;

        public static void InitializeLibrary() {
            var library = ConstantImports.MiniAudioHandle;
            InitializationCheckHandler = LibraryHandler
                .GetDelegate<MiniEngineInitializationCheckHandler>(library, "IsEngineInitialized");
            InitializationHandler = LibraryHandler
                .GetDelegate<MiniAudioEngineHandler>(library, "InitializeEngine");
            LoadSoundHandler = LibraryHandler.GetDelegate<MiniAudioLoadHandler>(library, "LoadSound");
            UnsafeLoadSoundHandler = LibraryHandler.GetDelegate<UnsafeMiniAudioLoadHandler>(library, "UnsafeLoadSound");
            PlaySoundHandler = LibraryHandler.GetDelegate<MiniSoundHandler>(library, "PlaySound");
            StopSoundHandler = LibraryHandler.GetDelegate<MiniStopSoundHandler>(library, "StopSound");
            ReleaseHandler = LibraryHandler.GetDelegate<MiniAudioEngineHandler>(library, "ReleaseEngine");
            SoundPlayingHandler = LibraryHandler.GetDelegate<MiniSoundStateHandler>(library, "IsSoundPlaying");
            SoundFinishedHandler = LibraryHandler.GetDelegate<MiniSoundStateHandler>(library, "IsSoundFinished");
            SoundVolumeHandler = LibraryHandler.GetDelegate<MiniSoundVolumeHandler>(library, "SetSoundVolume");
        }

        public static bool IsEngineInitialized() {
            if (InitializationCheckHandler != null) {
                return InitializationCheckHandler.Invoke();
            }
            return false;
        }

        public static void InitializeEngine() {
            InitializationHandler?.Invoke();
        }

        public static uint LoadSound(string path, SoundLoadParameters loadParams) {
            if (LoadSoundHandler == null) {
                return uint.MaxValue;
            }
            return LoadSoundHandler.Invoke(path, loadParams);
        }

        public static uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParams) {
            if (UnsafeLoadSoundHandler == null) {
                return uint.MaxValue;
            }
            return UnsafeLoadSoundHandler.Invoke(path, sizeInBytes, loadParams);
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
            ReleaseHandler?.Invoke();
        }
#else
        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern bool IsEngineInitialized();

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern void InitializeEngine();

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern uint LoadSound(string path, SoundLoadParameters loadParams);

        [DllImport("MiniAudio_Unity_Bindings.dll")]
        public static extern uint UnsafeLoadSound(IntPtr path, uint sizeInBytes, IntPtr loadParameters);

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
