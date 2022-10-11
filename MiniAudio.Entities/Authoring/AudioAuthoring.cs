using MiniAudio.Interop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Authoring {

    public abstract class BaseAudioAuthoring : MonoBehaviour {
        
        public bool IsPathStreamingAssets;
        public string Path;

        public virtual BlobAssetReference<PathBlob> CreatePathBlob() {
            if (string.IsNullOrEmpty(Path)) {
                throw new System.InvalidOperationException(
                    "Cannot convert an invalid path!");
            }

            var path = IsPathStreamingAssets ? $"/{Path}" : Path;
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();
            
            var charArray = builder.Allocate(ref pathBlob.Path, path.Length);

            for (int i = 0; i < path.Length; i++) {
                charArray[i] = path[i];
            }

            pathBlob.IsPathStreamingAssets = IsPathStreamingAssets;
            pathBlob.ID = UnityEngine.Hash128.Compute(Path);
            return builder.CreateBlobAssetReference<PathBlob>(Allocator.Persistent);
        }
    }

    public class AudioAuthoring : BaseAudioAuthoring {
        public SoundLoadParameters Parameters;
    }

    public class AudioAuthoringBaker : Baker<AudioAuthoring> {

        public override void Bake(AudioAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.ManualOverride);
            var blobAsset = authoring.CreatePathBlob();
            AddBlobAsset(ref blobAsset, out _);
            AddComponent(new Path { Value = blobAsset });
            
            var audioClip = AudioClip.New();
            audioClip.Parameters = authoring.Parameters;
            
            AddComponent(entity, audioClip);
            AddComponent(entity, new AudioStateHistory { Value = AudioState.Stopped });
        }
    }
}
