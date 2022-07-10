using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities {

    public struct FreeHandle : IBufferElementData {
        public uint Value;

        public static implicit operator UsedHandle(FreeHandle handle) {
            return new UsedHandle { Value = handle.Value };
        }
    }

    public struct UsedHandle : IBufferElementData {
        public uint Value;

        public static implicit operator FreeHandle(UsedHandle handle) {
            return new FreeHandle { Value = handle.Value };
        }
    }

    /// <summary>
    /// A 1:1 mapping of <see cref="MiniAudio.Interop.SoundLoadParameters"/>.
    /// </summary>
    public struct SoundLoadParametersElement : IBufferElementData {
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

        public static implicit operator SoundLoadParametersElement(SoundLoadParameters value) {
            return new SoundLoadParametersElement {
                EndTime = value.EndTime,
                IsLooping = value.IsLooping,
                StartTime = value.StartTime,
                Volume = value.Volume
            };
        }
    }

    /// <summary>
    /// Describes that the <see cref="MiniAudio.Entities.Systems.OneShotAudioSystem"/> 
    /// must initialize the <see cref="FreeHandle"/>.
    /// </summary>
    public struct InitializePoolTag : IComponentData { }

    public struct AudioPoolDescriptor : IComponentData {
        public ushort ReserveCapacity;
        public bool IsLoaded;
    }
}
