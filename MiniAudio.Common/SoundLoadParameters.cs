using System;

namespace MiniAudio.Interop {

    [Serializable]
    public struct SoundLoadParameters {

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
    }
}
