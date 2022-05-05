using MiniAudio.Interop;
using UnityEngine;

namespace MiniAudio {

    [AddComponentMenu("")]
    [DisallowMultipleComponent]
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

        internal static void Initialize() {
            ConstantImports.Initialize();
            MiniAudioHandler.InitializeLibrary();
            MiniAudioHandler.InitializeEngine();
        }

        internal static void Release() {
            MiniAudioHandler.ReleaseEngine();
            MiniAudioHandler.ReleaseLibrary();
            ConstantImports.Release();
        }

        void Start() {
            Initialize();
        }

        void OnDestroy() {
            Release();
        }
    }
}
