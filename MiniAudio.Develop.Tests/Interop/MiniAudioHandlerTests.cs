using NUnit.Framework;

namespace MiniAudio.Interop.Tests {

    public class MiniAudioHandlerTests {

        [Test]
        public void DelegatesInitialized() {
            AreNull();
            DefaultMiniAudioInitializationProxy.Initialize();
            AreNotNull();
            DefaultMiniAudioInitializationProxy.Release();
            AreNull();
        }

        void AreNull() {


            Assert.True(MiniAudioHandler.InitCheckHandler == null);
            Assert.True(MiniAudioHandler.InitEngineHandler == null);
            Assert.True(MiniAudioHandler.LoadSoundHandler == null);
            Assert.True(MiniAudioHandler.UnsafeLoadHandler == null);
            Assert.True(MiniAudioHandler.PlaySoundHandler == null);
            Assert.True(MiniAudioHandler.StopSoundHandler == null);
            Assert.True(MiniAudioHandler.ReleaseEngineHandler == null);
            Assert.True(MiniAudioHandler.SoundPlayingHandler == null);
            Assert.True(MiniAudioHandler.SoundFinishedHandler == null);
            Assert.True(MiniAudioHandler.SoundVolumeHandler == null);
            Assert.True(MiniAudioHandler.InitLoggerHandler == null);
        }

        void AreNotNull() {
            Assert.IsNotNull(MiniAudioHandler.InitCheckHandler);
            Assert.IsNotNull(MiniAudioHandler.InitEngineHandler);
            Assert.IsNotNull(MiniAudioHandler.LoadSoundHandler);
            Assert.IsNotNull(MiniAudioHandler.UnsafeLoadHandler);
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
