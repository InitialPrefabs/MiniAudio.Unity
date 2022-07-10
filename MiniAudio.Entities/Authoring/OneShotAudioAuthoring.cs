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

            dstManager.AddBuffer<FreeHandle>(entity);
            dstManager.AddBuffer<UsedHandle>(entity);

            var buffer = dstManager.AddBuffer<SoundLoadParametersElement>(entity);

            UnityEngine.Debug.Log(Size);
            for (int i = 0; i < Size; i++) {
                buffer.Add(LoadParameters);
            }

            dstManager.AddComponentData(entity, new AudioPoolDescriptor {
                ReserveCapacity = Size
            });
        }
    }
}

