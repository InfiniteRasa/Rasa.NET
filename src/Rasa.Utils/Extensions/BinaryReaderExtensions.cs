using System;
using System.IO;
using System.Text;

namespace Rasa.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static string ReadLengthedString(this BinaryReader reader)
        {
            var len = reader.ReadInt32();
            return len == 0 ? "" : Encoding.UTF8.GetString(reader.ReadBytesExactly(len));
        }

        public static byte[] ReadBytesExactly(this BinaryReader reader, int length)
        {
            if (length < 0)
                throw new InvalidDataException("Payload length cannot be negative.");

            if (reader.BaseStream.CanSeek && length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new EndOfStreamException("Payload length exceeds the available data.");

            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException("Incomplete payload.");

            return bytes;
        }

        /// <summary>
        /// Returns <paramref name="length"/> when that many bytes can still be read from the
        /// reader's stream, and throws otherwise. Every length in the client protocols is
        /// client-controlled; used before an allocation sized by one, it keeps a 30-bit
        /// count in a 20-byte packet from asking for a gigabyte on the main loop.
        /// </summary>
        public static int CheckedLength(this BinaryReader reader, long length, string what)
        {
            if (length < 0)
                throw new InvalidDataException($"Negative {what} length {length}.");

            var stream = reader.BaseStream;

            if (stream.CanSeek && length > stream.Length - stream.Position)
                throw new InvalidDataException($"{what} length {length} exceeds the {stream.Length - stream.Position} bytes remaining.");

            return (int)length;
        }

        public static void EnsureFullyConsumed(this BinaryReader reader, string what)
        {
            var stream = reader.BaseStream;
            if (stream.CanSeek && stream.Position != stream.Length)
                throw new InvalidDataException($"{what} contains trailing bytes.");
        }

        public static string ReadUtf8StringOn(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytesExactly(length);

            var index = Array.IndexOf<byte>(bytes, 0);
            if (index == -1)
                index = bytes.Length;

            return index == 0 ? "" : Encoding.UTF8.GetString(bytes, 0, index);
        }
    }
}
