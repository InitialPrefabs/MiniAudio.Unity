using System.Text;
using InitialPrefabs.NimGui;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Screen = UnityEngine.Screen;

namespace MiniAudio.Entities {
    
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
                All = new [] {
                    ComponentType.ReadOnly<AudioClip>() 
                }
            });

            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate() {
            if (ImGui.Button("Quit")) {
                UnityEngine.Application.Quit();
            }

            var audioHandles = new NativeList<AudioClip>(
                audioQuery.CalculateEntityCount(), 
                Allocator.TempJob);

            var entities=  new NativeList<Entity>(
                audioQuery.CalculateEntityCount(), 
                Allocator.TempJob);

            new AudioQueryJob() {
                Clips = audioHandles,
                AssociatedEntities = entities
            }.Run(audioQuery);

            var center = new float2(Screen.width / 2f, Screen.height / 2f);
            var commandBuffer = commandBufferSystem.CreateCommandBuffer();

            using (var pane = new ImPane("Audio Handles", center, 500, ImPaneFlags.Pinned)) {
                if (pane.IsVisible) {
                    for (int i = 0; i < audioHandles.Length; i++) {
                        var audioHandle = audioHandles[i];
                        StringBuilder.Clear().Append("Audio Handle: ").Append(audioHandle.Handle);
                        ImGui.Label(StringBuilder);

                        switch (audioHandle.CurrentState) {
                            case AudioState.Stopped:
                                if (ImGui.Button("Play")) {
                                    audioHandle.CurrentState = AudioState.Playing;
                                    commandBuffer.SetComponent(entities[i], audioHandle);
                                }
                                break;
                            case AudioState.Playing:
                                var volume = ImGui.Slider("Volume", 0f, 1f, audioHandle.Parameters.Volume);

                                if (volume != audioHandle.Parameters.Volume) {
                                    audioHandle.Parameters.Volume = volume;
                                    commandBuffer.SetComponent(entities[i], audioHandle);
                                }

                                if (ImGui.Button("Stop")) {
                                    audioHandle.CurrentState = AudioState.Stopped;
                                    commandBuffer.SetComponent(entities[i], audioHandle);
                                } else if (ImGui.Button("Pause")) {
                                    audioHandle.CurrentState = AudioState.Paused;
                                    commandBuffer.SetComponent(entities[i], audioHandle);
                                }
                                break;
                            case AudioState.Paused:
                                if (ImGui.Button("Resume")) {
                                    audioHandle.CurrentState = AudioState.Playing;
                                    commandBuffer.SetComponent(entities[i], audioHandle);
                                }
                                break;
                        }
                    }
                }
            }

            commandBufferSystem.AddJobHandleForProducer(Dependency);

            audioHandles.Dispose();
            entities.Dispose();
        }
    }
}
