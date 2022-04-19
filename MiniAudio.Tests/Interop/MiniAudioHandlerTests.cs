using System;
using NUnit.Framework;

namespace MiniAudio.Interop.Tests {

    public class MiniAudioHandlerTests {

        [SetUp]
        public void SetUp() {
            ConstantImports.Initialize();
        }

#if MINIAUDIO_DEVELOP
        [Test]
        public void LoadSoundBinding() {
            var actual = LibraryHandler.GetDelegate<MiniAudioHandler.MiniAudioLoadHandler>(
                ConstantImports.MiniAudioHandle, "LoadSound");
            Assert.AreNotEqual(IntPtr.Zero, actual);
        }

        [Test]
        public void UnsafeLoadSoundBinding() {
            var actual = LibraryHandler.GetDelegate<MiniAudioHandler.UnsafeMiniAudioLoadHandler>(
                ConstantImports.MiniAudioHandle, "UnsafeLoadSound");
            Assert.AreNotEqual(IntPtr.Zero, actual);
        }
#endif

        [TearDown]
        public void TearDown() {
            ConstantImports.Release();
        }
    }
}
