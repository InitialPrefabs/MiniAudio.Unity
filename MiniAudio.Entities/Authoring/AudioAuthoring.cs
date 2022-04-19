using MiniAudio.Interop;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Authoring {

    public class AudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity {

        public bool IsPathStreamingAssets;
        public string Path;
        public SoundLoadParameters Parameters;

        public unsafe void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            if (string.IsNullOrEmpty(Path)) {
                return;
            }

            var path = IsPathStreamingAssets ? $"/{Path}" : Path;

            var buffer = dstManager.AddBuffer<LoadPath>(entity);
            buffer.ResizeUninitialized(path.Length);
            fixed (char* head = path) {
                UnsafeUtility.MemCpy(buffer.GetUnsafePtr(), head, sizeof(char) * path.Length);
            }

            var audioClip = AudioClip.New();
            audioClip.Parameters = Parameters;
            dstManager.AddComponentData(entity, audioClip);
            dstManager.AddComponentData(entity, new AudioStateHistory {
                Value = AudioState.Stopped
            });

            if (IsPathStreamingAssets) {
                dstManager.AddComponentData(entity, new StreamingPathTag { });
            }
        }
    }
}
