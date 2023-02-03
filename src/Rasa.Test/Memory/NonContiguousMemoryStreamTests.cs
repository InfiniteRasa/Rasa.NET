using System;
using System.Buffers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Memory;

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

        stream.Write(buffer1);
        stream.Write(buffer2);
        stream.Write(buffer3, 0, buffer3.Length - 10);

        Assert.AreEqual(stream.WritePosition, buffer1.Length + buffer2.Length + buffer3.Length - 10);
    }

    [TestMethod]
    public void TestRead()
    {
        var buffer1 = new byte[20];
        for (var i = 0; i < buffer1.Length; ++i)
            buffer1[i] = (byte)i;

        var buffer2 = new byte[40];
        for (var i = 0; i < buffer2.Length; ++i)
            buffer2[i] = (byte)(buffer1.Length + i);

        var buffer3 = new byte[60];
        for (var i = 0; i < buffer3.Length; ++i)
            buffer3[i] = (byte)(buffer1.Length + buffer2.Length + i);

        using var stream = new NonContiguousMemoryStream();

        stream.Write(buffer1);
        stream.Write(buffer2);
        stream.Write(buffer3);

        var read1 = new byte[30];
        var readCount1 = stream.Read(read1, 0, read1.Length);

        var read2 = new byte[20];
        var readCount2 = stream.Read(read2, 0, read2.Length);

        var read3 = new byte[70];
        var readCount3 = stream.Read(read3, 0, read3.Length);

        Assert.AreEqual(readCount1, 30);
        Assert.AreEqual(readCount2, 20);
        Assert.AreEqual(readCount3, 70);
        Assert.AreEqual(stream.Position, stream.WritePosition);

        // Validate read1
        for (var i = 0; i < 20; ++i)
            Assert.AreEqual(read1[i], buffer1[i]);

        for (var i = 0; i < 10; ++i)
            Assert.AreEqual(read1[20 + i], buffer2[i]);

        // Validate read2
        for (var i = 0; i < 20; ++i)
            Assert.AreEqual(read2[i], buffer2[10 + i]);

        // Validate read3
        for (var i = 0; i < 10; ++i)
            Assert.AreEqual(read3[i], buffer2[30 + i]);

        for (var i = 0; i < 60; ++i)
            Assert.AreEqual(read3[10 + i], buffer3[i]);
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

        stream.Write(buffer1);
        stream.Write(buffer2);

        var throwAwayData = new byte[2];
        stream.Read(throwAwayData, 0, 2);

        stream.RemoveBytes(3);

        var data = new byte[7];
        stream.Read(data, 0, data.Length);

        Assert.AreEqual(data[0], 3);
        Assert.AreEqual(data[1], 4);
        Assert.AreEqual(data[2], 0);
        Assert.AreEqual(data[3], 2);
        Assert.AreEqual(data[4], 4);
        Assert.AreEqual(data[5], 6);
        Assert.AreEqual(data[6], 8);
        Assert.AreEqual(stream.Position, stream.WritePosition);
    }

    [TestMethod]
    public void TestRemoveMoreBytes()
    {
        var buffer = new byte[5];

        using var stream = new NonContiguousMemoryStream();

        stream.Write(buffer);
        stream.Write(buffer);

        var throwAwayData = new byte[6];
        stream.Read(throwAwayData, 0, 6);

        stream.RemoveBytes(9);

        Assert.AreEqual(stream.Position, 0);
        Assert.AreEqual(stream.WritePosition, 1);
    }

    [TestMethod]
    public void TestRemoveAllBytes()
    {
        var buffer = new byte[5];

        using var stream = new NonContiguousMemoryStream();

        stream.Write(buffer);
        stream.Write(buffer);

        var throwAwayData = new byte[6];
        stream.Read(throwAwayData, 0, 6);

        stream.RemoveBytes(10);

        Assert.AreEqual(stream.Position, 0);
        Assert.AreEqual(stream.WritePosition, 0);
    }

    [TestMethod]
    public void TestRemoveTooManyBytes()
    {
        var buffer = new byte[5];

        using var stream = new NonContiguousMemoryStream();

        stream.Write(buffer);
        stream.Write(buffer);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => stream.RemoveBytes((int)stream.Length + 1));
    }

    [TestMethod]
    public void TestCopyToWithCount()
    {
        var buffer = new byte[5];

        using var stream1 = new NonContiguousMemoryStream();
        using var stream2 = new NonContiguousMemoryStream();

        stream1.Write(buffer);
        stream1.CopyToWithCount(stream2, 10);

        Assert.AreEqual(5, stream1.Position);
        Assert.AreEqual(5, stream1.WritePosition);
        Assert.AreEqual(0, stream2.Position);
        Assert.AreEqual(5, stream2.WritePosition);
    }
}
