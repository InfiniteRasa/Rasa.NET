using System.IO;

namespace Rasa.Memory;

public class BufferData
{
    public static byte[] Buffer => BufferManager.Buffer;
    public int BaseOffset { get; set; }
    public int RealBaseOffset { get; }
    public int MaxLength => BufferManager.BlockSize - (BaseOffset - RealBaseOffset);

    public int Offset { get; set; }
    public int Length { get; set; }
    public int ByteCount { get; set; }
    public int RemainingLength => Length - Offset;

    public bool Free { get; set; } = true;

    public string StrData => ByteData();

    public BufferData(int baseOffset)
    {
        RealBaseOffset = baseOffset;

        Reset();
    }

    public byte this[int off]
    {
        get { return Buffer[BaseOffset + off]; }
        set { Buffer[BaseOffset + off] = value; }
    }

    public void Reset()
    {
        BaseOffset = RealBaseOffset;
        Length = MaxLength;
        Offset = ByteCount = 0;

        Clear();
    }

    public int Append(BufferData source)
    {
        var bytesAvailable = MaxLength - Length;
        var bytesInSource = source.RemainingLength;

        var bytesToCopy = (bytesInSource > bytesAvailable) ? bytesAvailable : bytesInSource;

        if (bytesToCopy > 0)
        {
            Array.Copy(Buffer, source.BaseOffset + source.Offset, Buffer, BaseOffset + Length, bytesToCopy);
            source.Offset += bytesToCopy;
            Length += bytesToCopy;
        }

        return bytesToCopy;
    }

    public int ShiftProcessedData()
    {
        if (Offset > 0)
        {
            var pos = Offset;

            Array.Copy(Buffer, BaseOffset + pos, Buffer, BaseOffset + 0, RemainingLength);
            
            ByteCount -= pos;
            Length -= pos;
            Offset = 0;

            return pos;
        }

        return 0;
    }

    public void Clear(int offset, int length)
    {
        CheckConstraints(offset, length);

        Array.Clear(Buffer, BaseOffset + offset, length);
    }

    public static void Copy(BufferData source, int sourceOffset, BufferData dest, int destOffset, int length)
    {
        source.CheckConstraints(sourceOffset, length);
        dest.CheckConstraints(destOffset, length);

        Array.Copy(Buffer, source.BaseOffset + sourceOffset, Buffer, dest.BaseOffset + destOffset, length);
    }

    public MemoryStream GetStream(int offset, int length, bool writable)
    {
        CheckConstraints(offset, length);

        return new MemoryStream(Buffer, BaseOffset + offset, length, writable);
    }

    public void Clear() => Clear(0, MaxLength);
    public MemoryStream GetStream(bool writable) => GetStream(Offset, RemainingLength, writable);
    public BinaryReader GetReader() => GetReader(Offset, RemainingLength, Encoding.UTF8, false);
    public BinaryReader GetReader(int offset, int length) => GetReader(offset, length, Encoding.UTF8, false);
    public BinaryReader GetReader(int offset, int length, Encoding encoding, bool leaveOpen) => new(GetStream(offset, length, false), encoding, leaveOpen);
    public BinaryWriter CreateWriter() => CreateWriter(Offset, RemainingLength, Encoding.UTF8, false);
    public BinaryWriter CreateWriter(int offset, int length) => CreateWriter(offset, length, Encoding.UTF8, false);
    public BinaryWriter CreateWriter(int offset, int length, Encoding encoding, bool leaveOpen) => new(GetStream(offset, length, true), encoding, leaveOpen);
    public override string ToString() => $"BufferData[{BaseOffset} + {Offset}, {Length}]";

    private void CheckConstraints(int offset, int length)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (offset + length > MaxLength)
            throw new Exception("BufferData tried to access another BufferData's memory!");

        if (BaseOffset < RealBaseOffset || BaseOffset > RealBaseOffset + BufferManager.BlockSize)
            throw new Exception("The BaseOffset is in another buffer's area!");
    }

    public string ByteData()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < RemainingLength; ++i)
        {
            if (i > 0)
                sb.Append(", ");

            sb.Append($"0x{this[Offset + i]:X2}");
        }

        return sb.ToString();
    }
}
