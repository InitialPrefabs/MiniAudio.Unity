using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities.Authoring {

    public class AudioAuthoring : BaseAudioAuthoring {
        public SoundLoadParameters Parameters;
    }

    public class AudioAuthoringBaker : Baker<AudioAuthoring> {

        public override void Bake(AudioAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.ManualOverride);
            var blobAsset = BaseAudioAuthoring.CreatePathBlob(authoring.Path, authoring.IsPathStreamingAssets);
            AddBlobAsset(ref blobAsset, out _);
            AddComponent(new Path { Value = blobAsset });

            var audioClip = AudioClip.New();
            audioClip.Parameters = authoring.Parameters;

            AddComponent(entity, audioClip);
            AddComponent(entity, new AudioStateHistory { Value = AudioState.Stopped });
        }
    }
}
