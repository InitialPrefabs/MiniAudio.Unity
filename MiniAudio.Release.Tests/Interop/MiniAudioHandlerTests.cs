using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MiniAudio.Interop.Tests {

    public unsafe class MiniAudioHandlerTests {

        string audioPath;

        [SetUp]
        public void SetUp() {
            audioPath = Path.Combine(Application.streamingAssetsPath, "Audio", "Stronghold.mp3");
            Assert.IsTrue(File.Exists(audioPath));
        }

        [Test]
        public void BindingsDoNotThrowErrors() {
            Assert.DoesNotThrow(() => {
                DefaultMiniAudioInitializationProxy.Initialize();

                Assert.True(MiniAudioHandler.IsEngineInitialized());
                Assert.False(MiniAudioHandler.IsSoundFinished(uint.MaxValue));
                Assert.AreEqual(uint.MaxValue, MiniAudioHandler.LoadSound("", default));

                unsafe {
                    fixed (char* ptr = audioPath) {
                        SoundLoadParameters loadParams = new SoundLoadParameters {
                            Volume = 1.0f
                        };
                        uint handle = MiniAudioHandler.UnsafeLoadSound(
                            new IntPtr(ptr), 
                            (uint)(sizeof(char) * audioPath.Length), 
                            new IntPtr(&loadParams));

                        Assert.AreNotEqual(uint.MaxValue, handle);
                        Assert.AreEqual(0, handle);
                        MiniAudioHandler.UnloadSound(handle);
                    }
                }
                MiniAudioHandler.PlaySound(uint.MaxValue);
                MiniAudioHandler.StopSound(uint.MaxValue, false);
                MiniAudioHandler.SetSoundVolume(uint.MaxValue, 0.5f);
                DefaultMiniAudioInitializationProxy.Release();
            });
        }
    }
}
