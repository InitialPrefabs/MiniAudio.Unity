using System;
using System.Collections;
using MiniAudio.Entities;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniAudio.Interop.Tests {

    public partial class AudioSystemTests {

        [DisableAutoCreation]
        partial class TestAudioSystem : SystemBase {

            protected override void OnUpdate() { }

            public void AudioSystemPlaysSound() {
                foreach (var (path, entity) in SystemAPI.Query<Path>().WithEntityAccess()) {
                }
            }
        }


        [UnitySetUp]
        public IEnumerator SetUp() {
            yield return SceneManager.LoadSceneAsync("Scenes/MainSampleScene");
        }

        [UnityTest]
        public IEnumerator AudioIsLoaded() {
            throw new NotImplementedException();
        }
    }
}
