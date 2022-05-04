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

            void Execute(Entity entity, in AudioClip audioClip) {
                Clips.AddNoResize(audioClip);
                AssociatedEntities.AddNoResize(entity);
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

            // commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate() {
            var audioHandles = new NativeList<AudioClip>(
                audioQuery.CalculateEntityCount(),
                Allocator.TempJob);

            var entities = new NativeList<Entity>(
                audioQuery.CalculateEntityCount(),
                Allocator.TempJob);

            new AudioQueryJob() {
                Clips = audioHandles,
                AssociatedEntities = entities
            }.Run(audioQuery);

            for (int i = 0; UIDocumentAuthoring.Instance != null && i < audioHandles.Length; i++) {
                var audioHandle = audioHandles[i];
                var entity = entities[i];
                UIDocumentAuthoring.Instance.LastKnownEntity = entity;
                UIDocumentAuthoring.Instance.AudioClip = audioHandle;
            }

            // commandBufferSystem.AddJobHandleForProducer(Dependency);
            audioHandles.Dispose();
            entities.Dispose();
        }
    }
}
