using MiniAudio.Entities;
using MiniAudio.Entities.Systems;
using NUnit.Framework;
using Unity.Collections;

namespace MiniAudio.Interop.Tests {

    public unsafe class AudioCommandBufferTests {

        AudioCommandBuffer audioCommandBuffer;
        const string RelativeAudioPath = "Audio/Fire.ogg";
        float volume = 1.0f;

        [SetUp]
        public void SetUp() {
            Assert.True(audioCommandBuffer.PlaybackIds == null);
            audioCommandBuffer = new AudioCommandBuffer(Allocator.Persistent);
            Assert.True(audioCommandBuffer.PlaybackIds != null);
            Assert.AreEqual(audioCommandBuffer.Allocator, Allocator.Persistent);
        }
        
        [TearDown]
        public void TearDown() {
            Assert.True(audioCommandBuffer.PlaybackIds != null);
            audioCommandBuffer.Dispose();
            Assert.True(audioCommandBuffer.PlaybackIds == null);
        }

        [Test]
        public void RecordsAndStores32() {
            var expected = BakeUtils.ComputeHash(RelativeAudioPath);
            audioCommandBuffer.Request(new FixedString32Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);
            var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
            Assert.AreEqual(expected, actual.Hash128, "Mismatch hashes");
        }

        [Test]
        public void RecordsAndStores64() {
            var expected = BakeUtils.ComputeHash(RelativeAudioPath);
            audioCommandBuffer.Request(new FixedString64Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);
            var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
            Assert.AreEqual(expected, actual.Hash128, "Mismatch hashes");
        }

        [Test]
        public void RecordsAndStores128() {
            var expected = BakeUtils.ComputeHash(RelativeAudioPath);
            audioCommandBuffer.Request(new FixedString128Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);
            var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
            Assert.AreEqual(expected, actual.Hash128, "Mismatch hashes");
        }

        [Test]
        public void RecordsAndStores512() {
            var expected = BakeUtils.ComputeHash(RelativeAudioPath);
            audioCommandBuffer.Request(new FixedString512Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);
            var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
            Assert.AreEqual(expected, actual.Hash128, "Mismatch hashes");
        }

        [Test]
        public void RecordsAndStores4096() {
            var expected = BakeUtils.ComputeHash(RelativeAudioPath);
            audioCommandBuffer.Request(new FixedString4096Bytes(RelativeAudioPath), volume);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);
            var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
            Assert.AreEqual(expected, actual.Hash128, "Mismatch hashes");
        }

    }
}
