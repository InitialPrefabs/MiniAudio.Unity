using System.Collections;
using System.Collections.Generic;
using InitialPrefabs.NimGui.Loop;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniAudio.Entities.Demo {

    public class CameraAuthoring : MonoBehaviour {

        public CameraEvent CameraEvent;
        public Camera Camera;

        void OnEnable() {
            DefaultImGuiInitialization.SetupCamera(Camera, CameraEvent);
        }

        void OnDisable() {
            DefaultImGuiInitialization.TearDownCamera(Camera, CameraEvent);
        }

        void Update() {

        }
    }
}
