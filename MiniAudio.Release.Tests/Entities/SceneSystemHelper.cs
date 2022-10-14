using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;

namespace MiniAudio.Interop.Tests {

    [DisableAutoCreation]
    public partial class SceneSystemHelper : SystemBase {

        protected override void OnUpdate() { }

        Entity[] GetSceneGuidEntities(params SubScene[] subScenes) {
            var entities = new List<Entity>(subScenes.Length);
            foreach (var subScene in subScenes) {
                foreach (var (sceneReference, entity) in SystemAPI
                    .Query<SceneReference>().WithEntityAccess()) {

                    if (sceneReference.SceneGUID == subScene.SceneGUID) {
                        entities.Add(entity);
                    }
                }
            }
            return entities.ToArray();
        }

        public bool AreScenesLoaded(params SubScene[] subScenes) {
            var entities = GetSceneGuidEntities(subScenes);
            var isLoaded = true;
            for (int i = 0; i < entities.Length; i++) {
                var guid = subScenes[i].SceneGUID;
                isLoaded &= SceneSystem.IsSceneLoaded(World.Unmanaged, entities[i]);
            }
            return isLoaded;
        }
    }
}
