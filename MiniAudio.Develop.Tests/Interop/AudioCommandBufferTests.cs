using MiniAudio.Entities.Systems;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace MiniAudio.Interop.Tests {

    public unsafe class AudioCommandBufferTests {

        AudioCommandBuffer audioCommandBuffer;
        const string PathParam = "Test";

        SoundLoadParameters soundParams = new SoundLoadParameters {
            Volume = 1.0f
        };

        [SetUp]
        public void SetUp() {
            Assert.True(audioCommandBuffer.PlaybackIds == null);
            audioCommandBuffer = new AudioCommandBuffer(Allocator.Persistent);
            Assert.True(audioCommandBuffer.PlaybackIds != null);

            Assert.AreEqual(audioCommandBuffer.Allocator, Allocator.Persistent);
        }

        [Test]
        public void RecordsAndStores32() {
            audioCommandBuffer.Request(new FixedString32Bytes(PathParam), soundParams);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = PathParam) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = math.hash(ptr, sizeof(char) * PathParam.Length);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores64() {
            audioCommandBuffer.Request(new FixedString64Bytes(PathParam), soundParams);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = PathParam) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = math.hash(ptr, sizeof(char) * PathParam.Length);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores128() {
            audioCommandBuffer.Request(new FixedString128Bytes(PathParam), soundParams);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = PathParam) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = math.hash(ptr, sizeof(char) * PathParam.Length);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores512() {
            audioCommandBuffer.Request(new FixedString512Bytes(PathParam), soundParams);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = PathParam) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = math.hash(ptr, sizeof(char) * PathParam.Length);
                Assert.AreEqual(expected, actual.ID, "Mismatch hashes");
            }
        }

        [Test]
        public void RecordsAndStores4096() {
            audioCommandBuffer.Request(new FixedString4096Bytes(PathParam), soundParams);
            Assert.AreEqual(1, audioCommandBuffer.PlaybackIds->Length);

            fixed(char* ptr = PathParam) {
                var actual = audioCommandBuffer.PlaybackIds->ElementAt(0);
                var expected = math.hash(ptr, sizeof(char) * PathParam.Length);
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

