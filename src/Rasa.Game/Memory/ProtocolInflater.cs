using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using Org.BouncyCastle.Utilities.Zlib;

namespace Rasa.Memory
{
    using Packets.Protocol;

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
                using (var source = new MemoryStream(input, false))
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

                if (output.Length != expectedLength)
                    throw new EndOfStreamException("Incomplete decompressed protocol payload.");

                ValidateCompletion(input, expectedLength, buffer);
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

        private static void ValidateCompletion(byte[] input, int expectedLength, byte[] buffer)
        {
            var inflater = new ZStream();
            var initialized = false;
            try
            {
                if (inflater.inflateInit(true) != JZlib.Z_OK)
                    throw new InvalidOperationException("Unable to initialize the protocol inflater.");

                initialized = true;

                var lookahead = new byte[input.Length + 1];
                input.CopyTo(lookahead, 0);
                inflater.next_in = lookahead;
                inflater.avail_in = lookahead.Length;

                while (true)
                {
                    var previousInput = inflater.total_in;
                    inflater.next_out = buffer;
                    inflater.next_out_index = 0;
                    inflater.avail_out = buffer.Length;
                    var result = inflater.inflate(JZlib.Z_NO_FLUSH);
                    var count = buffer.Length - inflater.avail_out;

                    if (inflater.total_out > expectedLength)
                        throw new InvalidDataException("Decompressed payload exceeds its declared length.");

                    if (result == JZlib.Z_STREAM_END)
                    {
                        if (inflater.total_in > input.Length)
                            throw new EndOfStreamException("DEFLATE payload consumed synthetic lookahead.");

                        if (inflater.total_in != input.Length)
                            throw new InvalidDataException("Compressed payload contains trailing bytes.");

                        if (inflater.total_out != expectedLength)
                            throw new EndOfStreamException("Incomplete decompressed protocol payload.");

                        return;
                    }

                    if (result == JZlib.Z_DATA_ERROR || result == JZlib.Z_NEED_DICT)
                        throw new InvalidDataException($"Invalid DEFLATE payload: {inflater.msg}");

                    if (result != JZlib.Z_OK && result != JZlib.Z_BUF_ERROR)
                        throw new InvalidOperationException($"Protocol inflater failed with status {result}.");

                    if (count == 0 && inflater.total_in == previousInput)
                    {
                        if (inflater.avail_in == 0)
                            throw new EndOfStreamException("DEFLATE payload did not reach its final block.");

                        throw new InvalidDataException("DEFLATE payload cannot be decoded.");
                    }
                }
            }
            finally
            {
                if (initialized)
                    inflater.inflateEnd();

                inflater.free();
            }
        }
    }
}
