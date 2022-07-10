using MiniAudio.Interop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Authoring {

    public abstract class BaseAudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
        public bool IsPathStreamingAssets;
        public string Path;

        public abstract void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem);

        protected BlobAssetReference<PathBlob> CreatePathBlob() {
            if (string.IsNullOrEmpty(Path)) {
                throw new System.InvalidOperationException(
                    "Cannot convert an invalid path!");
            }

            var path = IsPathStreamingAssets ? $"/{Path}" : Path;
            var builder = new BlobBuilder(Allocator.Temp);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();
            var charArray = builder.Allocate(ref pathBlob.Path, path.Length);

            for (int i = 0; i < path.Length; i++) {
                charArray[i] = path[i];
            }

            pathBlob.IsPathStreamingAssets = IsPathStreamingAssets;
            return builder.CreateBlobAssetReference<PathBlob>(Allocator.Persistent);
        }
    }

    public class AudioAuthoring : BaseAudioAuthoring {

        public SoundLoadParameters Parameters;

        public override void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem) {

            var blobAsset = CreatePathBlob();
            conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blobAsset);
            dstManager.AddComponentData(entity, new Path { Value = blobAsset });

            var audioClip = AudioClip.New();
            audioClip.Parameters = Parameters;
            dstManager.AddComponentData(entity, audioClip);
            dstManager.AddComponentData(entity, new AudioStateHistory {
                Value = AudioState.Stopped
            });

            dstManager.AddComponentData(entity, new IsAudioLoaded {
                Value = false,
            });
        }
    }
}
