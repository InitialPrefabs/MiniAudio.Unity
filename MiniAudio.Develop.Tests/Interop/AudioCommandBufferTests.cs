using MiniAudio.Entities.Systems;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace MiniAudio.Interop.Tests {

    public unsafe class AudioCommandBufferTests {

        AudioCommandBuffer audioCommandBuffer;
        const string RelativeAudioPath = "Audio/Fire.ogg";

        string path;

        float volume = 1.0f;

        static Unity.Entities.Hash128 ComputeHash(char* c, int startIndex, int length, out AtomicSafetyHandle handle) {
            var array =
                NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<char>(c, length, Allocator.None);

            handle = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, handle);
            return Hash128.Compute(array, startIndex, length);
        }

        [SetUp]
        public void SetUp() {
            path = Application.streamingAssetsPath + "/" + RelativeAudioPath;
            Assert.True(audioCommandBuffer.PlaybackIds == null);
            audioCommandBuffer = new AudioCommandBuffer(Allocator.Persistent);
            Assert.True(audioCommandBuffer.PlaybackIds != null);

            Assert.AreEqual(audioCommandBuffer.Allocator, Allocator.Persistent);

            Debug.Log(RelativeAudioPath.Length * sizeof(char));
        }

        [Test]
        public void RecordsAndStores32() {
            audioCommandBuffer.Request(new FixedString32Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = RelativeAudioPath) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = ComputeHash(ptr, 0, RelativeAudioPath.Length, out var handle);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
                AtomicSafetyHandle.Release(handle);
            }
        }

        [Test]
        public void RecordsAndStores64() {
            audioCommandBuffer.Request(new FixedString64Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = RelativeAudioPath) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = ComputeHash(ptr, 0, RelativeAudioPath.Length, out var handle);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores128() {
            audioCommandBuffer.Request(new FixedString128Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = RelativeAudioPath) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = ComputeHash(ptr, 0, RelativeAudioPath.Length, out var handle);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores512() {
            audioCommandBuffer.Request(new FixedString512Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = RelativeAudioPath) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = ComputeHash(ptr, 0, RelativeAudioPath.Length, out var handle);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores4096() {
            audioCommandBuffer.Request(new FixedString4096Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = RelativeAudioPath) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = ComputeHash(ptr, 0, RelativeAudioPath.Length, out var handle);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [TearDown]
        public void TearDown() {
            Assert.True(audioCommandBuffer.PlaybackIds != null);
            audioCommandBuffer.Dispose();
            Assert.True(audioCommandBuffer.PlaybackIds == null);
        }
    }
}
