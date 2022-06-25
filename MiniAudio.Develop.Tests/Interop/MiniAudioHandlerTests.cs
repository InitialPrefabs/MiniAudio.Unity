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
        }

        void AreNotNull() {
        }
    }
}
