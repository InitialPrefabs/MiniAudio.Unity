using NUnit.Framework;

namespace MiniAudio.Interop.Tests {

    public class MiniAudioHandlerTests {

        [Test]
        public void DelegatesInitializedAndReleased() {
            AreNull();
            DefaultMiniAudioInitializationProxy.Initialize();
            AreNotNull();
            DefaultMiniAudioInitializationProxy.Release();
            AreNull();
        }

        void AreNull() {
            Assert.IsNull(MiniAudioHandler.InitCheckHandler);
            Assert.IsNull(MiniAudioHandler.InitEngineHandler);
            Assert.IsNull(MiniAudioHandler.LoadSoundHandler);
            Assert.IsNull(MiniAudioHandler.UnsafeLoadHandler);
            Assert.IsNull(MiniAudioHandler.UnloadSoundHandler);
            Assert.IsNull(MiniAudioHandler.PlaySoundHandler);
            Assert.IsNull(MiniAudioHandler.StopSoundHandler);
            Assert.IsNull(MiniAudioHandler.ReleaseEngineHandler);
            Assert.IsNull(MiniAudioHandler.SoundPlayingHandler);
            Assert.IsNull(MiniAudioHandler.SoundFinishedHandler);
            Assert.IsNull(MiniAudioHandler.SoundVolumeHandler);
            Assert.IsNull(MiniAudioHandler.InitLoggerHandler);
        }

        void AreNotNull() {
            Assert.IsNotNull(MiniAudioHandler.InitCheckHandler);
            Assert.IsNotNull(MiniAudioHandler.InitEngineHandler);
            Assert.IsNotNull(MiniAudioHandler.LoadSoundHandler);
            Assert.IsNotNull(MiniAudioHandler.UnsafeLoadHandler);
            Assert.IsNotNull(MiniAudioHandler.UnloadSoundHandler);
            Assert.IsNotNull(MiniAudioHandler.PlaySoundHandler);
            Assert.IsNotNull(MiniAudioHandler.StopSoundHandler);
            Assert.IsNotNull(MiniAudioHandler.ReleaseEngineHandler);
            Assert.IsNotNull(MiniAudioHandler.SoundPlayingHandler);
            Assert.IsNotNull(MiniAudioHandler.SoundFinishedHandler);
            Assert.IsNotNull(MiniAudioHandler.SoundVolumeHandler);
            Assert.IsNotNull(MiniAudioHandler.InitLoggerHandler);
        }
    }
}
