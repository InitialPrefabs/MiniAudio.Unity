using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace MiniAudio.Entities.Demo {

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AudioDrawingSystem : SystemBase {

        static readonly StringBuilder StringBuilder = new StringBuilder(256);

        [BurstCompile]
        partial struct AudioQueryJob : IJobEntity {

            [WriteOnly]
            public NativeList<MiniAudio.Entities.AudioClip> Clips;

            [WriteOnly]
            public NativeList<Entity> AssociatedEntities;

            [WriteOnly]
            public NativeList<FixedString512Bytes> Names;

            void Execute(Entity entity, DynamicBuffer<LoadPath> loadPath, in AudioClip audioClip) {
                Clips.AddNoResize(audioClip);
                AssociatedEntities.AddNoResize(entity);
                var fixedString = new FixedString512Bytes();
                for (int i = 0; i < loadPath.Length; i++) {
                    fixedString.Append(loadPath[i].Value);
                }
                Names.AddNoResize(fixedString);
            }
        }

        EntityQuery audioQuery;
        EntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate() {
            audioQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<AudioClip>()
                }
            });
        }

        protected override void OnUpdate() {
            var audioHandles = new NativeList<AudioClip>(
                audioQuery.CalculateEntityCount(),
                Allocator.TempJob);

            var entities = new NativeList<Entity>(
                audioQuery.CalculateEntityCount(),
                Allocator.TempJob);

            var paths = new NativeList<FixedString512Bytes>(
                audioQuery.CalculateEntityCount(),
                Allocator.TempJob
            );

            new AudioQueryJob() {
                Clips = audioHandles,
                AssociatedEntities = entities,
                Names = paths
            }.Run(audioQuery);

            for (int i = 0; UIDocumentAuthoring.Instance != null && i < audioHandles.Length; i++) {
                var audioHandle = audioHandles[i];
                var entity = entities[i];
                var path = paths[i];
                UIDocumentAuthoring.Instance.LastKnownEntity = entity;
                UIDocumentAuthoring.Instance.AudioClip = audioHandle;
                UIDocumentAuthoring.Instance.Name = path.ToString();
            }

            audioHandles.Dispose();
            entities.Dispose();
        }
    }
}
