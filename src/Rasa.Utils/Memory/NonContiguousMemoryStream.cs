using System.Buffers;
using System.IO;

namespace Rasa.Memory;

public class NonContiguousMemoryStream : Stream
{
    private const int WriteArrayCapacity = 256;

    private record ArrayPoolBuffer(byte[] Buffer, int Length);

    private List<ArrayPoolBuffer> Buffers { get; } = new();
    private long Origin { get; set; }

    public long WritePosition { get; set; }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Position { get; set; }
    public override long Length => Buffers.Sum(arr => (long)arr.Length) - Origin;

    public void RemoveBytes(int count)
    {
        if (count > Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        var (index, offset) = GetIndices(count);

        if (index > 0)
        {
            for (var i = 0; i < index; ++i)
                ArrayPool<byte>.Shared.Return(Buffers[i].Buffer);

            Buffers.RemoveRange(0, index);
        }

        Origin = offset;

        Position = Math.Max(0, Position - count);
        WritePosition = Math.Max(0, WritePosition - count);
    }

    private (int, int) GetIndices(long offset)
    {
        int i = 0;

        offset += Origin;

        for (; i < Buffers.Count; ++i)
        {
            if (offset < Buffers[i].Length)
                break;

            offset -= Buffers[i].Length;
        }

        if (i == Buffers.Count)
            return (i, 0);

        return (i, (int)offset);
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count > buffer.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (Position + count > WritePosition)
            count = (int)(WritePosition - Position);

        var (indStart, offStart) = GetIndices(Position);
        var (indEnd, _) = GetIndices(Position + count);

        var currentOffset = offStart;
        var read = 0;

        for (var i = indStart; i <= indEnd && i < Buffers.Count && count > 0; ++i)
        {
            int currentCount = count;

            if (Buffers[i].Length - currentOffset < currentCount)
                currentCount = Buffers[i].Length - currentOffset;

            Buffer.BlockCopy(Buffers[i].Buffer, currentOffset, buffer, offset, currentCount);

            offset += currentCount;
            count -= currentCount;

            currentOffset = 0;

            read += currentCount;
        }

        Position += read;

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;

            case SeekOrigin.Current:
                Position += offset;
                break;

            case SeekOrigin.End:
                Position = Length + offset;
                break;
        }

        return Position;
    }

    public long SeekWrite(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                WritePosition = offset;
                break;

            case SeekOrigin.Current:
                WritePosition += offset;
                break;

            case SeekOrigin.End:
                WritePosition = Length + offset;
                break;
        }

        return WritePosition;
    }

    public override void SetLength(long value)
    {
        if (value > Length)
        {
            var newArr = ArrayPool<byte>.Shared.Rent((int)(value - Length));

            Buffers.Add(new ArrayPoolBuffer(newArr, newArr.Length));
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count > buffer.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(count));

        // Expand the stream with extra space to write to
        if (WritePosition + count > Length)
            SetLength(Length + Math.Max(count, WriteArrayCapacity));

        var (indStart, offStart) = GetIndices(WritePosition);
        var (indEnd, _) = GetIndices(WritePosition + count);

        for (var i = indStart; i <= indEnd && i < Buffers.Count && count > 0; ++i)
        {
            int currentCount = count;

            if (Buffers[i].Length - offStart < currentCount)
                currentCount = Buffers[i].Length - offStart;

            Buffer.BlockCopy(buffer, offset, Buffers[i].Buffer, offStart, currentCount);

            offset += currentCount;
            count -= currentCount;

            offStart = 0;

            WritePosition += currentCount;
        }
    }

    public void CopyToWithCount(Stream destination, int count)
    {
        if (count == 0)
            return;

        var buffer = ArrayPool<byte>.Shared.Rent(WriteArrayCapacity);

        try
        {
            while (count > 0)
            {
                var toReadSize = Math.Min(count, buffer.Length);

                var bytesRead = Read(buffer, 0, toReadSize);
                if (bytesRead == 0)
                    break;

                destination.Write(buffer, 0, bytesRead);

                count -= bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void Optimize()
    {
        // TODO: check the buffers and if the buffer.Buffer.Length is greater than buffer.Length
        // then grab the next buffer, move the first bytes to the end of
        // then return every buffer that 
    }

    public override void Close()
    {
        foreach (var buffer in Buffers)
            ArrayPool<byte>.Shared.Return(buffer.Buffer);

        Buffers.Clear();

        base.Close();
    }
}
