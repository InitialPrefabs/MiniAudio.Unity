using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities.Authoring {
    public class OneShotAudioAuthoring : BaseAudioAuthoring {

        public ushort Size = 10;
        public SoundLoadParameters LoadParameters;

        public override void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem) {

            var blobAsset = CreatePathBlob();
            conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blobAsset);
            dstManager.AddComponentData(entity, new Path { Value = blobAsset });
            dstManager.AddComponentData(entity, new AliasSoundLoadParameters {
                EndTime   = LoadParameters.EndTime,
                StartTime = LoadParameters.StartTime,
                Volume    = LoadParameters.Volume,
                IsLooping = LoadParameters.IsLooping
            });

            dstManager.AddBuffer<FreeHandle>(entity);
            dstManager.AddBuffer<UsedHandle>(entity);

            dstManager.AddComponentData(entity, new AudioPoolDescriptor {
                ReserveCapacity = Size
            });

            dstManager.AddBuffer<OneShotAudioState>(entity);
        }
    }
}

