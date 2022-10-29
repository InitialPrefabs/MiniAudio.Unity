using System;
using System.Threading;
using MiniAudio.Interop;
using Unity.Entities;

namespace MiniAudio.Entities {

    public static class PathExtensions {
        public static ref bool IsStreamingAssets(this ref Path path) =>
            ref path.Value.Value.IsPathStreamingAssets;
        public static ref BlobArray<char> PathBlobArray(this ref Path path) =>
            ref path.Value.Value.Path;

        public static ref Hash128 HashedPath(this ref Path path) => ref path.Value.Value.ID;
    }

    /// <summary>
    /// Describes the current state of the Audio.
    /// </summary>
    public enum AudioState : byte {
        Stopped,
        Playing,
        Paused
    }

    /// <summary>
    /// Stores readonly metadata describing where the audio file lives 
    /// and whether or not it is a relative/absolute path.
    /// </summary>
    public struct PathBlob {
        public bool IsPathStreamingAssets;
        public Hash128 ID;
        public BlobArray<char> Path;
    }

    /// <summary>
    /// The Entity component version of the PathBlob.
    /// </summary>
    public struct Path : IComponentData {

        public bool IsStreamingAssets => Value.Value.IsPathStreamingAssets;
        public BlobAssetReference<PathBlob> Value;
    }

    /// <summary>
    /// A stand in to describe the audio clip. When DOTS 1.0 rolls out with 
    /// Enable/Disable components, this will be moved into the AudioClip.
    /// </summary>
    // public struct IsAudioLoaded : IComponentData {
    //     public bool Value;
    // }
    public struct AudioClip : IComponentData, IEquatable<AudioClip> {

        /// <summary>
        /// Stores the index in which MiniAudio allocated the sound.
        /// </summary>
        public uint Handle;

        /// <summary>
        /// Stores the current AudioState.
        /// </summary>
        public AudioState CurrentState;

        /// <summary>
        /// Stores the SoundLoadParameters needed to initially load the sound.
        /// </summary>
        public SoundLoadParameters Parameters;

        public static AudioClip New() {
            return new AudioClip {
                Handle = uint.MaxValue,
                CurrentState = AudioState.Stopped,
                Parameters = new SoundLoadParameters {
                    Volume = 1.0f,
                }
            };
        }
        public bool Equals(AudioClip other) {
            return Handle == other.Handle && CurrentState == other.CurrentState && Parameters.Equals(other.Parameters);
        }

        public override int GetHashCode() {
            return Handle.GetHashCode() ^ CurrentState.GetHashCode() ^ Parameters.GetHashCode();
        }

        public static bool operator ==(AudioClip lhs, AudioClip rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(AudioClip lhs, AudioClip rhs) {
            return !lhs.Equals(rhs);
        }
    }

    /// <summary>
    /// Stores the last known state of the AudioClip.
    /// </summary>
    internal struct AudioStateHistory : IComponentData {
        public AudioState Value;
    }

    internal struct InitializedAudioTag : ICleanupComponentData {
    }
}
