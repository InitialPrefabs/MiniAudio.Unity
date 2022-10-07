using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities.Authoring {
    
    public class OneShotAudioAuthoring : BaseAudioAuthoring {

        public ushort Size = 10;
        public SoundLoadParameters LoadParameters;
    }

    public class OneShotAudioBaker : Baker<OneShotAudioAuthoring> {

        public override void Bake(OneShotAudioAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.ManualOverride);
            var blobAsset = authoring.CreatePathBlob();
            
            AddBlobAsset(ref blobAsset, out _);
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

