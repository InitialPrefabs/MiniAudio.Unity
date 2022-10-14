using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniAudio.Interop.Tests {

    public class MiniAudioHandlerTests {

        string audioPath;

        [UnitySetUp]
        public IEnumerator SetUp() {
            audioPath = Path.Combine(Application.streamingAssetsPath, "Audio", "Stronghold.mp3");
            Assert.IsTrue(File.Exists(audioPath));

            var initializedProxy = Object.FindObjectOfType<DefaultMiniAudioInitializationProxy>();
            if (initializedProxy != null) {
                Object.Destroy(initializedProxy.gameObject);
                yield return null;
            }

            yield return null;
            DefaultMiniAudioInitializationProxy.Setup();
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            var initializedProxy = Object.FindObjectOfType<DefaultMiniAudioInitializationProxy>();
            Assert.IsNotNull(initializedProxy);
            Object.Destroy(initializedProxy);

            yield return null;
            Assert.False(StreamingAssetsHelper.Path.Data.IsCreated);
        }

        [UnityTest]
        public IEnumerator ValidLifeCycleRuntimeTest() {
            yield return new WaitForSeconds(1);
            uint handle = uint.MaxValue;
            Assert.DoesNotThrow(() => {
                CommonSetUpAsserts();

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
                CommonSetUpAsserts();

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

        void CommonSetUpAsserts() {
            Assert.True(MiniAudioHandler.IsEngineInitialized());
            Assert.False(MiniAudioHandler.IsSoundFinished(uint.MaxValue));
            Assert.True(StreamingAssetsHelper.Path.Data.IsCreated);
        }
    }
}
