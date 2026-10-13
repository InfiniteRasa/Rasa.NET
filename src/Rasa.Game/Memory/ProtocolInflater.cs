using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace Rasa.Memory
{
    using Packets.Protocol;

    /// <summary>
    /// A client packet's compressed payload: raw DEFLATE, which must come out at exactly the
    /// length the packet declares, end in its final block, and have nothing after that block.
    ///
    /// DeflateStream checks none of the last two. Given a stream cut off before its final block
    /// it returns what it has and no error - with a large payload that can be every byte of the
    /// declared length - and given bytes after the final block it drops them unread. Both are
    /// read off the source it is given (<see cref="CompletionSource"/>): once it has decoded the
    /// final block it never reads from its source again, so a stream that asks past the end of
    /// its input never reached it; and with the input's last byte held back until the rest is
    /// taken, a stream that ends without asking for it had bytes after its end.
    /// </summary>
    internal static class ProtocolInflater
    {
        internal static MemoryStream Decompress(byte[] input, int expectedLength)
        {
            if (expectedLength <= 0)
                throw new InvalidDataException("Decompressed protocol size must be positive.");
            if (expectedLength > ProtocolPacket.MaxExpandedSize)
                throw new InvalidDataException(
                    $"Decompressed protocol size cannot exceed {ProtocolPacket.MaxExpandedSize} bytes.");

            var output = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                var source = new CompletionSource(input);

                using (var deflate = new DeflateStream(source, CompressionMode.Decompress))
                {
                    int count;
                    while ((count = deflate.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (count > expectedLength - output.Length)
                            throw new InvalidDataException("Decompressed payload exceeds its declared length.");

                        output.Write(buffer, 0, count);
                    }
                }

                if (source.ReadPastEnd)
                    throw new EndOfStreamException("DEFLATE payload did not reach its final block.");

                if (output.Length != expectedLength)
                    throw new EndOfStreamException("Incomplete decompressed protocol payload.");

                if (source.Consumed != input.Length)
                    throw new InvalidDataException("Compressed payload contains trailing bytes.");

                output.Position = 0;
                return output;
            }
            catch
            {
                output.Dispose();
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// The compressed bytes as DeflateStream reads them: all but the last first, the last on
        /// its own, so how many it took says whether the stream ended before the input did; and
        /// whether it asked for more once there was none.
        /// </summary>
        private sealed class CompletionSource : Stream
        {
            private readonly byte[] _input;
            private int _position;

            internal CompletionSource(byte[] input) => _input = input;

            /// <summary>How many of the input's bytes have been read.</summary>
            internal int Consumed => _position;

            /// <summary>Whether a read was asked for after the last byte had been taken.</summary>
            internal bool ReadPastEnd { get; private set; }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> destination)
            {
                if (_position >= _input.Length)
                {
                    ReadPastEnd = true;
                    return 0;
                }

                // Up to the byte before the last; then the last by itself.
                var last = _input.Length - 1;
                var available = _position < last ? last - _position : 1;
                var count = Math.Min(destination.Length, available);

                _input.AsSpan(_position, count).CopyTo(destination);
                _position += count;

                return count;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _input.Length;

            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
