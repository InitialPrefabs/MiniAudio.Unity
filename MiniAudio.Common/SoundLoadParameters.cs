using System;

namespace MiniAudio.Interop {

    [Serializable]
    public struct SoundLoadParameters : IEquatable<SoundLoadParameters> {

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
        
        public bool Equals(SoundLoadParameters other) {
            return IsLooping == other.IsLooping && Volume.Equals(other.Volume) && StartTime == other.StartTime && EndTime == other.EndTime;
        }
        
        public override int GetHashCode() {
            return IsLooping.GetHashCode() ^ Volume.GetHashCode() ^ StartTime.GetHashCode() ^ EndTime.GetHashCode();
        }
    }
}
