using MiniAudio.Interop;
using UnityEngine;

namespace MiniAudio {

    [AddComponentMenu("")]
    internal class DefaultMiniAudioInitializationProxy : MonoBehaviour {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Setup() {
            var go = new GameObject("MiniAudio Proxy") {
#if !UNITY_EDITOR
                hideFlags = HideFlags.HideInHierarchy
#endif
            };
            go.AddComponent<DefaultMiniAudioInitializationProxy>();
            Object.DontDestroyOnLoad(go);
        }

        void Start() {
            ConstantImports.Initialize();
            DefaultLogInitialization.InitializeLibrary();
#if UNITY_EDITOR_WIN && MINIAUDIO_DEVELOP
            MiniAudioHandler.InitializeLibrary();
#endif

            MiniAudioHandler.InitializeEngine();
        }

        void OnDestroy() {
            MiniAudioHandler.ReleaseEngine();
            ConstantImports.Release();
        }
    }
}
