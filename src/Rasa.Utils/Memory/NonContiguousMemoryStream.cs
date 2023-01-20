using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rasa.Memory
{
    public class NonContiguousMemoryStream : Stream
    {
        private const int WriteArrayCapacity = 256;

        private record ArrayPoolBuffer(byte[] Buffer, int Length);

        private readonly List<ArrayPoolBuffer> _buffers = new();
        private long _origin = 0;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Position { get; set; } = 0;

        public override long Length
        {
            get
            {
                return _buffers.Sum(arr => (long)arr.Length) - _origin;
            }
        }

        public NonContiguousMemoryStream()
        {
        }

        public void AddSharedPoolArray(byte[] buffer, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            _buffers.Add(new ArrayPoolBuffer(buffer, length));
        }

        public void CopyFromArray(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            CopyFromArray(buffer, 0, buffer.Length);
        }

        public void CopyFromArray(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            var newArr = ArrayPool<byte>.Shared.Rent(count);

            Buffer.BlockCopy(buffer, offset, newArr, 0, count);

            _buffers.Add(new ArrayPoolBuffer(newArr, count));
        }

        public void RemoveBytes(int count)
        {
            if (count > Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            var (index, offset) = GetIndices(count);

            if (index > 0)
            {
                for (var i = 0; i < index; ++i)
                {
                    ArrayPool<byte>.Shared.Return(_buffers[i].Buffer);
                }

                _buffers.RemoveRange(0, index);
            }

            _origin = offset;

            Position = Math.Max(0, Position - count);
        }

        private (int, int) GetIndices(long offset)
        {
            int i = 0;

            offset += _origin;

            for (; i < _buffers.Count; ++i)
            {
                if (offset < _buffers[i].Length)
                {
                    break;
                }

                offset -= _buffers[i].Length;
            }

            if (i == _buffers.Count)
                return (i, 0);

            return (i, (int)offset);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            var (indStart, offStart) = GetIndices(Position);
            var (indEnd, _) = GetIndices(Position + count);

            var currentOffset = offStart;
            var read = 0;

            for (var i = indStart; i <= indEnd && i < _buffers.Count && count > 0; ++i)
            {
                int currentCount = count;

                if (_buffers[i].Length - currentOffset < currentCount)
                    currentCount = _buffers[i].Length - currentOffset;

                Buffer.BlockCopy(_buffers[i].Buffer, currentOffset, buffer, offset, currentCount);

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

        public override void SetLength(long value)
        {
            if (value > Length)
            {
                int length = (int)(value - Length);

                _buffers.Add(new ArrayPoolBuffer(ArrayPool<byte>.Shared.Rent(length), length));
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Expand the stream with extra space to write to
            if (Length < Position + count)
            {
                SetLength(Length + Math.Max(count, WriteArrayCapacity));
            }

            var (indStart, offStart) = GetIndices(Position);
            var (indEnd, _) = GetIndices(Position + count);

            var currentOffset = offStart;
            var written = 0;

            for (var i = indStart; i <= indEnd && i < _buffers.Count && count > 0; ++i)
            {
                int currentCount = count;

                if (_buffers[i].Length - currentOffset < currentCount)
                    currentCount = _buffers[i].Length - currentOffset;

                Buffer.BlockCopy(buffer, offset, _buffers[i].Buffer, currentOffset, currentCount);

                offset += currentCount;
                count -= currentCount;

                currentOffset = 0;

                written += currentCount;
            }

            Position += written;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                foreach (var buffer in _buffers)
                {
                    ArrayPool<byte>.Shared.Return(buffer.Buffer);
                }

                _buffers.Clear();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
