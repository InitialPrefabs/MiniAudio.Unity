using System;
using System.Collections;
using MiniAudio.Entities;
using NUnit.Framework;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using AudioClip = MiniAudio.Entities.AudioClip;
using Object = UnityEngine.Object;

namespace MiniAudio.Interop.Tests {

    public partial class AudioSystemTests {

        [DisableAutoCreation]
        partial class TestAudioSystem : SystemBase {

            protected override void OnUpdate() { }

            void AssertAudioClip(Action<AudioClip> callback) {
                foreach (var (audioClip, _) in SystemAPI
                    .Query<AudioClip, Path>()) {

                    Assert.AreNotEqual(audioClip.Handle, uint.MaxValue);
                    callback?.Invoke(audioClip);
                }
            }

            public void PlaysAudio() {
                foreach (var (audioClip, _, entity) in SystemAPI
                    .Query<AudioClip, Path>()
                    .WithEntityAccess()) {

                    Assert.AreNotEqual(audioClip.Handle, uint.MaxValue);

                    var playAudioClip = audioClip;
                    playAudioClip.CurrentState = AudioState.Playing;
                    EntityManager.SetComponentData(entity, playAudioClip);
                }
            }

            public void PauseAudio() {
                foreach (var (audioClip, _, entity) in SystemAPI
                    .Query<AudioClip, Path>().WithEntityAccess()) {

                    Assert.AreNotEqual(audioClip.Handle, uint.MaxValue);
                    var pausedAudioClip = audioClip;
                    pausedAudioClip.CurrentState = AudioState.Paused;

                    EntityManager.SetComponentData(entity, pausedAudioClip);
                }
            }

            public void StopAudio() {
                foreach (var (audioClip, _, entity) in SystemAPI
                    .Query<AudioClip, Path>().WithEntityAccess()) {

                    Assert.AreNotEqual(audioClip.Handle, uint.MaxValue);
                    var pausedAudioClip = audioClip;
                    pausedAudioClip.CurrentState = AudioState.Stopped;

                    EntityManager.SetComponentData(entity, pausedAudioClip);
                }
            }

            public void AudioIsPlaying() {
                AssertAudioClip(clip => Assert.AreEqual(AudioState.Playing, clip.CurrentState));
            }

            public void AudioIsPaused() {
                AssertAudioClip(clip => Assert.AreEqual(AudioState.Paused, clip.CurrentState));
            }

            public void AudioIsStopped() {
                AssertAudioClip(clip => Assert.AreEqual(AudioState.Stopped, clip.CurrentState));
            }
        }

        SceneSystemHelper sceneSystemHelper;
        TestAudioSystem testAudioSystem;

        [UnitySetUp]
        public IEnumerator SetUp() {
            yield return SceneManager.LoadSceneAsync("Scenes/MainSampleScene");
            sceneSystemHelper = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<SceneSystemHelper>();

            testAudioSystem = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<TestAudioSystem>();
        }

        [UnityTest]
        public IEnumerator PersistentAudioLifeCycle() {
            var subScene = Object.FindObjectOfType<SubScene>();
            SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subScene.SceneGUID);

            while (!sceneSystemHelper.AreScenesLoaded(subScene)) {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            testAudioSystem.PlaysAudio();

            yield return new WaitForSeconds(0.1f);
            testAudioSystem.AudioIsPlaying();

            yield return new WaitForSeconds(0.1f);
            testAudioSystem.PauseAudio();

            yield return new WaitForSeconds(0.1f);
            testAudioSystem.AudioIsPaused();

            yield return new WaitForSeconds(0.1f);
            testAudioSystem.StopAudio();
            
            yield return new WaitForSeconds(0.1f);
            testAudioSystem.AudioIsStopped();
        }
    }
}
