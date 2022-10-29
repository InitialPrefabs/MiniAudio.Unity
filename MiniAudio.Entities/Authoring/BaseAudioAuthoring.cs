using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MiniAudio.Entities.Authoring {
    
    public abstract class BaseAudioAuthoring : MonoBehaviour {

        public bool IsPathStreamingAssets;
        public string Path;

        public static BlobAssetReference<PathBlob> CreatePathBlob(string path, bool isPathStreamingAssets) {
            if (string.IsNullOrEmpty(path)) {
                throw new System.InvalidOperationException(
                    "Cannot convert an invalid path!");
            }

            var adjustedPath = isPathStreamingAssets ? $"/{path}" : path;
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var pathBlob = ref builder.ConstructRoot<PathBlob>();

            var charArray = builder.Allocate(ref pathBlob.Path, adjustedPath.Length);

            for (int i = 0; i < adjustedPath.Length; i++) {
                charArray[i] = adjustedPath[i];
            }

            pathBlob.ID = BakeUtils.ComputeHash(path);
            pathBlob.IsPathStreamingAssets = isPathStreamingAssets;
            return builder.CreateBlobAssetReference<PathBlob>(Allocator.Persistent);
        }
    }
}
