using MiniAudio.Interop;
using UnityEngine;

namespace MiniAudio {

    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal class DefaultMiniAudioInitializationProxy : MonoBehaviour {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Setup() {
            var go = new GameObject("MiniAudio Proxy") {
#if !UNITY_EDITOR
                hideFlags = HideFlags.HideInHierarchy
#endif
            };
            go.AddComponent<DefaultMiniAudioInitializationProxy>();
            DontDestroyOnLoad(go);
        }

        internal static void Initialize() {
            ConstantImports.Initialize();
            MiniAudioHandler.InitializeLibrary();
            MiniAudioHandler.InitializeEngine();
            StreamingAssetsHelper.Initialize();
        }

        internal static void Release() {
            MiniAudioHandler.ReleaseEngine();
            MiniAudioHandler.ReleaseLibrary();
            ConstantImports.Release();
            StreamingAssetsHelper.Release();
        }

        void Start() {
            Initialize();
        }

        void OnDestroy() {
            Release();
        }
    }
}
