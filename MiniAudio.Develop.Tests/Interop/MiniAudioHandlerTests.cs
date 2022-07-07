using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MiniAudio.Interop.Tests {

    // TODO: Add a cc0 audio clip for runtime tests
    public unsafe class MiniAudioHandlerTests {

        uint handle = uint.MaxValue;

        [SetUp]
        public void SetUp() {
            Assert.DoesNotThrow(() => DefaultMiniAudioInitializationProxy.Initialize());

            Assert.True(MiniAudioHandler.IsEngineInitializedHandler != null);
            Assert.True(MiniAudioHandler.InitializeEngineHandler != null);
            Assert.True(MiniAudioHandler.UnsafeLoadSoundHandler != null);
            Assert.True(MiniAudioHandler.UnloadSoundHandler != null);
            Assert.True(MiniAudioHandler.PlaySoundHandler != null);
            Assert.True(MiniAudioHandler.StopSoundHandler != null);
            Assert.True(MiniAudioHandler.SetSoundVolumeHandler != null);
            Assert.True(MiniAudioHandler.IsSoundPlayingHandler != null);
            Assert.True(MiniAudioHandler.IsSoundFinishedHandler != null);
            Assert.True(MiniAudioHandler.ReleaseEngineHandler != null);
            Assert.True(MiniAudioHandler.InitializeLoggerHandler != null);
        }

        [TearDown]
        public void TearDown() {
            Assert.DoesNotThrow(() => DefaultMiniAudioInitializationProxy.Release());
            Assert.False(MiniAudioHandler.IsEngineInitializedHandler != null);
            Assert.False(MiniAudioHandler.InitializeEngineHandler != null);
            Assert.False(MiniAudioHandler.UnsafeLoadSoundHandler != null);
            Assert.False(MiniAudioHandler.UnloadSoundHandler != null);
            Assert.False(MiniAudioHandler.PlaySoundHandler != null);
            Assert.False(MiniAudioHandler.StopSoundHandler != null);
            Assert.False(MiniAudioHandler.SetSoundVolumeHandler != null);
            Assert.False(MiniAudioHandler.IsSoundPlayingHandler != null);
            Assert.False(MiniAudioHandler.IsSoundFinishedHandler != null);
            Assert.False(MiniAudioHandler.ReleaseEngineHandler != null);
            Assert.False(MiniAudioHandler.InitializeLoggerHandler != null);
        }

        [Test]
        public void SoundLifeCycleTests() {
            var targetPath = Application.streamingAssetsPath + "/Audio/Stronghold.mp3";
            Assert.True(File.Exists(targetPath));
            handle = MiniAudioHandler.LoadSound(targetPath, new SoundLoadParameters { Volume = 1f });

            Assert.AreNotEqual(uint.MaxValue, handle);
            Assert.AreEqual(0, handle);

            Assert.DoesNotThrow(() => MiniAudioHandler.PlaySound(handle));
            Assert.True(MiniAudioHandler.IsSoundPlaying(handle));
            Assert.False(MiniAudioHandler.IsSoundFinished(handle));
            
            Assert.DoesNotThrow(() => MiniAudioHandler.StopSound(handle, false));
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));
        }

        [Test]
        public void InvalidSoundLifeCycleTests() {
            var targetPath = string.Empty;
            Assert.False(File.Exists(targetPath));
            handle = MiniAudioHandler.LoadSound(targetPath, new SoundLoadParameters { Volume = 1f });

            Assert.AreEqual(uint.MaxValue, handle);

            Assert.DoesNotThrow(() => MiniAudioHandler.PlaySound(handle));
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));
            Assert.False(MiniAudioHandler.IsSoundFinished(handle));
            
            Assert.DoesNotThrow(() => MiniAudioHandler.StopSound(handle, false));
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));
        }
    }
}
