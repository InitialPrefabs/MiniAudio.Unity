using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Authoring {
    public class OneShotAudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity {

        public bool IsPathStreamingAssets;
        public string Path;

        public void Convert(
            Entity entity, 
            EntityManager dstManager, 
            GameObjectConversionSystem conversionSystem) {

            if (string.IsNullOrEmpty(Path)) {
                Debug.LogError("Cannot convert an invalid relative path!");
                return;
            }

            var path = IsPathStreamingAssets ? $"/{Path}" : Path;
        }
    }
}

