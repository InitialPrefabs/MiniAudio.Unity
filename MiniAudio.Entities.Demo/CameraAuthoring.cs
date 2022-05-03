using InitialPrefabs.NimGui.Loop;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniAudio.Entities.Demo {

    [System.Obsolete]
    public class CameraAuthoring : MonoBehaviour {

        public CameraEvent CameraEvent;
        public Camera Camera;

        void OnEnable() {
#if !URP_ENABLED
            DefaultImGuiInitialization.SetupCamera(Camera, CameraEvent);
#endif
        }

        void OnDisable() {
#if !URP_ENABLED
            DefaultImGuiInitialization.TearDownCamera(Camera, CameraEvent);
#endif
        }

        void Update() {

        }
    }
}
