using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniAudio.Entities.Demo {

    public static class UILinkExtensions {
        public static void ResolveChange(this ref UILink link) {
            if (link.DidChange) {
                link.PreviousVersion = link.Version;
            }
        }
    }

    public struct UILink {
        public bool DidChange => Version != PreviousVersion;

        public AudioState CurrentState;
        public float Volume;
        public uint Version;
        public uint PreviousVersion;
    }

    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentAuthoring : MonoBehaviour {

        public static UIDocumentAuthoring Instance { get; private set; }

        public Entity LastKnownEntity = Entity.Null;
        public AudioClip AudioClip;

        VisualElement root;
        Button playBtn;
        Button stopBtn;

        EntityCommandBufferSystem cmdBufferSystem;

        void Start() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(this);
            }

            var document = GetComponent<UIDocument>();
            root = document.rootVisualElement;

            playBtn = root.Q<Button>("play");
            playBtn.clicked += HandlePlay;

            stopBtn = root.Q<Button>("stop");
            stopBtn.clicked += HandleStop;

            cmdBufferSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        void OnDestroy() {
            playBtn.clicked -= HandlePlay;
            stopBtn.clicked -= HandleStop;
        }

        void HandlePlay() {
            if (LastKnownEntity == Entity.Null) {
                Debug.Log("Early out");
                return;
            }

            bool changed = true;
            switch (AudioClip.CurrentState) {
                case AudioState.Stopped:
                    AudioClip.CurrentState = AudioState.Playing;
                    break;
                case AudioState.Playing:
                    AudioClip.CurrentState = AudioState.Paused;
                    break;
                case AudioState.Paused:
                    AudioClip.CurrentState = AudioState.Playing;
                    break;
                default:
                    changed = false;
                    break;
            }

            if (changed) {
                Debug.Log(AudioClip.CurrentState);
                var cmdBuffer = cmdBufferSystem.CreateCommandBuffer();
                cmdBuffer.SetComponent(LastKnownEntity, AudioClip);
            }
        }

        void HandleStop() {
            if (LastKnownEntity == Entity.Null) {
                return;
            }
        }
    }
}
