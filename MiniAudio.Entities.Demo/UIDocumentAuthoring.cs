using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniAudio.Entities.Demo {

    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentAuthoring : MonoBehaviour {

        public static UIDocumentAuthoring Instance { get; private set; }

        public Entity LastKnownEntity = Entity.Null;
        public AudioClip AudioClip;
        public string Name;

        VisualElement root;
        Button playBtn;
        Button stopBtn;
        Slider volumeSlider;
        Label clipName;

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

            volumeSlider = root.Q<Slider>("vol");
            volumeSlider.RegisterValueChangedCallback(ApplyVolume);

            clipName = root.Q<Label>("clip-name");

            cmdBufferSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        void OnDestroy() {
            playBtn.clicked -= HandlePlay;
            stopBtn.clicked -= HandleStop;
            volumeSlider.UnregisterValueChangedCallback(ApplyVolume);
        }

        void Update() {
            clipName.text = Name;
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
                    playBtn.text = "Pause Audio";
                    break;
                case AudioState.Playing:
                    AudioClip.CurrentState = AudioState.Paused;
                    playBtn.text = "Resume Audio";
                    break;
                case AudioState.Paused:
                    AudioClip.CurrentState = AudioState.Playing;
                    playBtn.text = "Pause Audio";
                    break;
                default:
                    changed = false;
                    break;
            }

            if (changed) {
                var cmdBuffer = cmdBufferSystem.CreateCommandBuffer();
                cmdBuffer.SetComponent(LastKnownEntity, AudioClip);
            }
        }

        void HandleStop() {
            if (LastKnownEntity == Entity.Null) {
                return;
            }

            bool changed = false;
            if (AudioClip.CurrentState != AudioState.Stopped) {
                AudioClip.CurrentState = AudioState.Stopped;
                playBtn.text = "Play Audio";
                changed = true;
            }

            if (changed) {
                var cmdBuffer = cmdBufferSystem.CreateCommandBuffer();
                cmdBuffer.SetComponent(LastKnownEntity, AudioClip);
            }
        }

        void ApplyVolume(ChangeEvent<float> changeEvent) {
            if (changeEvent.newValue != changeEvent.previousValue) {
                var ratio = changeEvent.newValue / 100f;
                AudioClip.Parameters.Volume = Mathf.Pow(ratio, 2);
                var cmdBuffer = cmdBufferSystem.CreateCommandBuffer();
                cmdBuffer.SetComponent(LastKnownEntity, AudioClip);
            }
        }
    }
}
