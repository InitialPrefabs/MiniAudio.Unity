using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities.Authoring {
    public class OneShotAudioAuthoring : BaseAudioAuthoring {

        public ushort Size = 10;
        public SoundLoadParameters LoadParameters;

        // public override void Convert(
        //     Entity entity,
        //     EntityManager dstManager,
        //     GameObjectConversionSystem conversionSystem) {
        //
        //     var blobAsset = CreatePathBlob();
        //     conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blobAsset);
        //     dstManager.AddComponentData(entity, new Path { Value = blobAsset });
        //     dstManager.AddComponentData(entity, new AliasSoundLoadParameters {
        //         EndTime   = LoadParameters.EndTime,
        //         StartTime = LoadParameters.StartTime,
        //         Volume    = LoadParameters.Volume,
        //         IsLooping = LoadParameters.IsLooping
        //     });
        //
        //     dstManager.AddBuffer<FreeHandle>(entity);
        //     dstManager.AddBuffer<UsedHandle>(entity);
        //
        //     dstManager.AddComponentData(entity, new AudioPoolDescriptor {
        //         ReserveCapacity = Size
        //     });
        //
        //     dstManager.AddBuffer<OneShotAudioState>(entity);
        // }
    }

    public class OneShotAudioBaker : Baker<OneShotAudioAuthoring> {

        public override void Bake(OneShotAudioAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            var blobAsset = authoring.CreatePathBlob();
            AddComponent(entity, new Path {
                Value = blobAsset
            });
            
            AddComponent(entity, new AliasSoundLoadParameters {
                EndTime   = authoring.LoadParameters.EndTime,
                StartTime = authoring.LoadParameters.StartTime,
                Volume    = authoring.LoadParameters.Volume,
                IsLooping = authoring.LoadParameters.IsLooping
            });

            AddBuffer<FreeHandle>(entity);
            AddBuffer<UsedHandle>(entity);
            AddBuffer<OneShotAudioState>(entity);
            
            AddComponent(entity, new AudioPoolDescriptor {
                ReserveCapacity = authoring.Size
            });
        }
    }
}

