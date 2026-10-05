using System;
using System.Buffers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Memory
{
    using Rasa.Memory;

    [TestClass]
    public class NonContiguousMemoryStreamTests
    {
        [TestMethod]
        public void TestLength()
        {
            var buffer1 = new byte[10];
            var buffer2 = new byte[20];
            var buffer3 = new byte[30];

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);
            stream.CopyFromArray(buffer3, 0, buffer3.Length - 10);

            Assert.AreEqual(stream.Length, buffer1.Length + buffer2.Length + buffer3.Length - 10);
        }

        [TestMethod]
        public void TestLengthManualPoolArray()
        {
            var buffer1 = new byte[10];
            var buffer2 = new byte[20];
            var buffer3 = ArrayPool<byte>.Shared.Rent(20);

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);
            stream.AddSharedPoolArray(buffer3, 20);

            Assert.AreEqual(stream.Length, buffer1.Length + buffer2.Length + 20);
        }

        [TestMethod]
        public void TestRead()
        {
            var buffer1 = new byte[20];
            for (var i = 0; i < buffer1.Length; ++i)
            {
                buffer1[i] = (byte)i;
            }

            var buffer2 = new byte[40];
            for (var i = 0; i < buffer2.Length; ++i)
            {
                buffer2[i] = (byte)(buffer1.Length + i);
            }

            var buffer3 = new byte[60];
            for (var i = 0; i < buffer3.Length; ++i)
            {
                buffer3[i] = (byte)(buffer1.Length + buffer2.Length + i);
            }

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);
            stream.CopyFromArray(buffer3);

            var read1 = new byte[30];
            var readCount1 = stream.Read(read1, 0, read1.Length);

            var read2 = new byte[20];
            var readCount2 = stream.Read(read2, 0, read2.Length);

            var read3 = new byte[70];
            var readCount3 = stream.Read(read3, 0, read3.Length);

            Assert.AreEqual(30, readCount1);
            Assert.AreEqual(20, readCount2);
            Assert.AreEqual(70, readCount3);
            Assert.AreEqual(stream.Position, stream.Length);

            // Validate read1
            for (var i = 0; i < 20; ++i)
            {
                Assert.AreEqual(read1[i], buffer1[i]);
            }

            for (var i = 0; i < 10; ++i)
            {
                Assert.AreEqual(read1[20 + i], buffer2[i]);
            }

            // Validate read2
            for (var i = 0; i < 20; ++i)
            {
                Assert.AreEqual(read2[i], buffer2[10 + i]);
            }

            // Validate read3
            for (var i = 0; i < 10; ++i)
            {
                Assert.AreEqual(read3[i], buffer2[30 + i]);
            }

            for (var i = 0; i < 60; ++i)
            {
                Assert.AreEqual(read3[10 + i], buffer3[i]);
            }
        }

        [TestMethod]
        public void TestRemoveBytes()
        {
            var buffer1 = new byte[5];
            var buffer2 = new byte[5];

            for (var i = 0; i < 5; ++i)
            {
                buffer1[i] = (byte)i;
                buffer2[i] = (byte)(i * 2);
            }

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);

            var throwAwayData = new byte[2];
            Assert.AreEqual(2, stream.Read(throwAwayData, 0, 2));

            stream.RemoveBytes(3);

            var data = new byte[7];
            Assert.AreEqual(data.Length, stream.Read(data, 0, data.Length));

            Assert.AreEqual(3, data[0]);
            Assert.AreEqual(4, data[1]);
            Assert.AreEqual(0, data[2]);
            Assert.AreEqual(2, data[3]);
            Assert.AreEqual(4, data[4]);
            Assert.AreEqual(6, data[5]);
            Assert.AreEqual(8, data[6]);
            Assert.AreEqual(stream.Length, buffer1.Length + buffer2.Length - 3);
            Assert.AreEqual(stream.Position, stream.Length);
        }

        [TestMethod]
        public void TestRemoveMoreBytes()
        {
            var buffer1 = new byte[5];
            var buffer2 = new byte[5];

            for (var i = 0; i < 5; ++i)
            {
                buffer1[i] = (byte)i;
                buffer2[i] = (byte)(i * 2);
            }

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);

            var throwAwayData = new byte[6];
            Assert.AreEqual(6, stream.Read(throwAwayData, 0, 6));

            stream.RemoveBytes(9);

            Assert.AreEqual(1, stream.Length);
            Assert.AreEqual(0, stream.Position);
        }

        [TestMethod]
        public void TestRemoveAllBytes()
        {
            var buffer1 = new byte[5];
            var buffer2 = new byte[5];

            for (var i = 0; i < 5; ++i)
            {
                buffer1[i] = (byte)i;
                buffer2[i] = (byte)(i * 2);
            }

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);

            var throwAwayData = new byte[6];
            Assert.AreEqual(6, stream.Read(throwAwayData, 0, 6));

            stream.RemoveBytes(10);

            Assert.AreEqual(0, stream.Length);
            Assert.AreEqual(0, stream.Position);
        }

        [TestMethod]
        public void TestRemoveTooManyBytes()
        {
            var buffer1 = new byte[5];
            var buffer2 = new byte[5];

            for (var i = 0; i < 5; ++i)
            {
                buffer1[i] = (byte)i;
                buffer2[i] = (byte)(i * 2);
            }

            using var stream = new NonContiguousMemoryStream();

            stream.CopyFromArray(buffer1);
            stream.CopyFromArray(buffer2);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                stream.RemoveBytes(11);
            });
        }
    }
}
