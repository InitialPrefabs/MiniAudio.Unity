using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniAudio.Interop.Tests {

    public unsafe class MiniAudioHandlerTests {

        string audioPath;

        [SetUp]
        public void SetUp() {
            audioPath = Path.Combine(Application.streamingAssetsPath, "Audio", "Stronghold.mp3");
            Assert.IsTrue(File.Exists(audioPath));
        }

        [UnityTest]
        public IEnumerator ValidLifeCycleRuntimeTest() {
            yield return new WaitForSeconds(1);
            uint handle = uint.MaxValue;
            Assert.DoesNotThrow(() => {
                Assert.True(MiniAudioHandler.IsEngineInitialized());
                Assert.False(MiniAudioHandler.IsSoundFinished(uint.MaxValue));

                handle = MiniAudioHandler.LoadSound(audioPath, new SoundLoadParameters {
                    Volume = 1.0f,
                });

                Assert.AreEqual(0, handle);

                MiniAudioHandler.PlaySound(handle);
            });

            yield return new WaitForSeconds(0.5f);
            Assert.True(MiniAudioHandler.IsSoundPlaying(handle));

            Assert.DoesNotThrow(() => {
                MiniAudioHandler.SetSoundVolume(uint.MaxValue, 0.5f);
                MiniAudioHandler.StopSound(handle, false);
            });

            yield return new WaitForSeconds(0.5f);
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));
            MiniAudioHandler.UnloadSound(handle);
        }

        [UnityTest]
        public IEnumerator InvalidLifeCycleRuntimeTest() {
            yield return new WaitForSeconds(1);
            uint handle = uint.MaxValue;
            Assert.DoesNotThrow(() => {
                Assert.True(MiniAudioHandler.IsEngineInitialized());
                Assert.False(MiniAudioHandler.IsSoundFinished(uint.MaxValue));

                handle = MiniAudioHandler.LoadSound(string.Empty, new SoundLoadParameters {
                    Volume = 1.0f,
                });

                Assert.AreEqual(uint.MaxValue, handle);

                MiniAudioHandler.PlaySound(handle);
            });

            yield return new WaitForSeconds(0.5f);
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));

            Assert.DoesNotThrow(() => {
                MiniAudioHandler.SetSoundVolume(uint.MaxValue, 0.5f);
                MiniAudioHandler.StopSound(handle, false);
            });

            yield return new WaitForSeconds(0.5f);
            Assert.False(MiniAudioHandler.IsSoundPlaying(handle));
        }
    }
}
