using MiniAudio.Interop;
using System;
using Unity.Entities;

namespace MiniAudio.Entities {
    
    [InternalBufferCapacity(32)]
    public struct FreeHandle : IBufferElementData {
        public uint Value;

        public static implicit operator UsedHandle(FreeHandle handle) {
            return new UsedHandle { Value = handle.Value };
        }

        public static implicit operator FreeHandle(uint value) {
            return new FreeHandle { Value = value };
        }
    }

    [InternalBufferCapacity(32)]
    public struct UsedHandle : IBufferElementData {
        public uint Value;

        public static implicit operator FreeHandle(UsedHandle handle) {
            return new FreeHandle { Value = handle.Value };
        }

        public static implicit operator uint(UsedHandle value) => value.Value;
    }

    [InternalBufferCapacity(32)]
    public struct OneShotAudioState : IBufferElementData {
        public AudioState Value;

        public static implicit operator OneShotAudioState(AudioState state) {
            return new OneShotAudioState {
                Value = state
            };
        }
    }

    /// <summary>
    /// A 1:1 mapping of <see cref="MiniAudio.Interop.SoundLoadParameters"/>.
    /// </summary>
    public struct AliasSoundLoadParameters : IComponentData {
        /// <summary>
        /// Is the AudioClip looping?
        /// </summary>
        public bool IsLooping;

        /// <summary>
        /// What is the Volume of the AudioClip? Typically between 1 and 0, inclusively.
        /// </summary>
        public float Volume;

        /// <summary>
        /// The start time of the AudioClip in milliseconds.
        /// </summary>
        public uint StartTime;

        /// <summary>
        /// The end time of the AudioClip in milliseconds.
        /// </summary>
        public uint EndTime;

        public static implicit operator AliasSoundLoadParameters(SoundLoadParameters value) {
            return new AliasSoundLoadParameters {
                EndTime = value.EndTime,
                IsLooping = value.IsLooping,
                StartTime = value.StartTime,
                Volume = value.Volume
            };
        }
    }

    public struct AudioPoolDescriptor : IComponentData {
        public ushort ReserveCapacity;
    }

    [Obsolete]
    public struct AudioPoolID : ICleanupComponentData {
        public uint Value;
    }
}

