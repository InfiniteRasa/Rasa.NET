using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Extensions;
    using Memory;

    public class ProtocolPacket : IBasePacket
    {
        public const int HeaderSize = 4;
        public const int MaxSize = ushort.MaxValue;
        public const int MaxExpandedSize = 4 * MaxSize;

        public ClientMessageOpcode Type { get; private set; } = ClientMessageOpcode.None;

        public ushort Size { get; private set; }
        public byte Channel { get; private set; }
        public uint SequenceNumber { get; set; }
        public bool Compress { get; private set; }
        public IClientMessage Message { get; set; }

        public ProtocolPacket()
        {
        }

        public ProtocolPacket(IClientMessage message, ClientMessageOpcode type, bool compress, byte channel)
        {
            Message = message;
            Type = type;
            Compress = compress;
            Channel = channel;
        }

        public void Read(BinaryReader br)
        {
            var available = br.BaseStream.Length - br.BaseStream.Position;
            if (available < HeaderSize)
                throw new InvalidDataException("Incomplete protocol header.");

            Size = br.ReadUInt16();
            if (Size < HeaderSize || Size > available)
                throw new InvalidDataException($"Invalid protocol size {Size}; available bytes: {available}.");

            var frame = ArrayPool<byte>.Shared.Rent(Size);
            try
            {
                frame[0] = (byte)Size;
                frame[1] = (byte)(Size >> 8);
                br.BaseStream.ReadExactly(frame, 2, Size - 2);

                using var stream = new MemoryStream(frame, 0, Size, false);
                using var reader = new BinaryReader(stream, Encoding.UTF8, true);
                ReadFrame(reader);

                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Protocol frame contains unconsumed bytes.");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(frame);
            }
        }

        private void ReadFrame(BinaryReader br)
        {
            Size = br.ReadUInt16();
            Channel = br.ReadByte();

            br.ReadByte(); // padding

            if (Channel == 0xFF) // Internal channel: Send timeout checking, ignore the packet
                return;

            if (Channel != 0) // 0 == ReliableStreamChannel (no extra data), Move message uses channels
            {
                SequenceNumber = br.ReadUInt32(); // Sequence number? if (previousValue - newValue < 0) { process packet; previousValue = newValue; }
                br.ReadInt32(); // 0xDEADBEEF
                br.ReadInt32(); // skip
            }

            var packetBeginPosition = br.BaseStream.Position;

            using (var reader = new ProtocolBufferReader(br, ProtocolBufferFlags.DontFragment))
            {
                reader.ReadProtocolFlags();


                reader.ReadPacketType(out ushort type, out bool compress);

                Type = (ClientMessageOpcode) type;
                Compress = compress;

                reader.ReadXORCheck((int) (br.BaseStream.Position - packetBeginPosition));
            }

            var xorCheckPosition = (int) br.BaseStream.Position;

            var readBr = br;

            try
            {
                if (Compress)
                {
                    var compressionType = br.ReadByte();
                    if (compressionType >= 2)
                        throw new InvalidDataException("Invalid compress type received!");

                    if (compressionType == 1)
                    {
                        var uncompressedSize = br.ReadInt32();
                        if (uncompressedSize <= 0)
                            throw new InvalidDataException("Decompressed protocol size must be positive.");
                        if (uncompressedSize > MaxExpandedSize)
                            throw new InvalidDataException(
                                $"Decompressed protocol size cannot exceed {MaxExpandedSize} bytes.");

                        var compressed = br.ReadBytesExactly((int)(br.BaseStream.Length - br.BaseStream.Position));
                        readBr = new BinaryReader(
                            ProtocolInflater.Decompress(compressed, uncompressedSize),
                            Encoding.UTF8,
                            false);
                    }
                }

                Message = Type switch
                {
                    ClientMessageOpcode.Login => new LoginMessage(),
                    ClientMessageOpcode.Move => new MoveMessage(),
                    ClientMessageOpcode.CallServerMethod => new CallServerMethodMessage(),
                    ClientMessageOpcode.Ping => new PingMessage(),
                    _ => throw new InvalidDataException($"Unsupported client packet type {Type}."),
                };

                using (var reader = new ProtocolBufferReader(readBr, ProtocolBufferFlags.DontFragment))
                {
                    reader.ReadProtocolFlags();

                    // Subtype and Message.Read()
                    reader.ReadDebugByte(41);

                    if ((Message.SubtypeFlags & ClientMessageSubtypeFlag.HasSubtype) == ClientMessageSubtypeFlag.HasSubtype)
                    {
                        Message.RawSubtype = reader.ReadByte();
                        if (Message.RawSubtype < Message.MinSubtype || Message.RawSubtype > Message.MaxSubtype)
                            throw new InvalidDataException("Invalid Subtype found!");
                    }

                    Message.Read(reader);

                    reader.ReadDebugByte(42);

                    reader.ReadXORCheck((int)br.BaseStream.Position - xorCheckPosition);
                }

                if (readBr != br && readBr.BaseStream.Position != readBr.BaseStream.Length)
                    throw new InvalidDataException("Decompressed payload contains unconsumed bytes.");
            }
            finally
            {
                if (readBr != br)
                    readBr.Dispose();
            }
        }

        public void Write(BinaryWriter bw)
        {
            var sizePosition = bw.BaseStream.Position;

            bw.Write((ushort) 0); // Size placeholder

            bw.Write(Channel);
            bw.Write((byte) 0); // padding

            if (Channel != 0)
            {
                bw.Write(SequenceNumber); // sequence num?
                bw.Write(0xDEADBEEF); // const
                bw.Write(0); // padding
            }

            var packetBeginPosition = (int) bw.BaseStream.Position;

            // TODO: find limits and maybe lower this number
            // OR: use NCMS as the target stream
            var packetBuffer = ArrayPool<byte>.Shared.Rent(0x8000);
            try
            {
                int uncompressedSize;

                using (var ms = new MemoryStream(packetBuffer, true))
                {
                    using var packetWriter = new BinaryWriter(ms, Encoding.UTF8, true);
                    using var writer = new ProtocolBufferWriter(packetWriter, ProtocolBufferFlags.DontFragment);

                    writer.WriteProtocolFlags();

                    writer.WriteDebugByte(41);

                    if ((Message.SubtypeFlags & ClientMessageSubtypeFlag.HasSubtype) == ClientMessageSubtypeFlag.HasSubtype)
                        writer.WriteByte(Message.RawSubtype);

                    Message.Write(writer);

                    writer.WriteDebugByte(42);

                    var currentPos = (int)ms.Position;

                    writer.WriteXORCheck(currentPos);

                    uncompressedSize = (int)ms.Position;
                }

                var compress = (Message.SubtypeFlags & ClientMessageSubtypeFlag.Compress) == ClientMessageSubtypeFlag.Compress && uncompressedSize > 0;

                using (var writer = new ProtocolBufferWriter(bw, ProtocolBufferFlags.DontFragment))
                {
                    writer.WriteProtocolFlags();

                    writer.WritePacketType((ushort)Message.Type, compress);

                    writer.WriteXORCheck((int)(bw.BaseStream.Position - packetBeginPosition));
                }

                int packetSize = uncompressedSize;

                if (compress)
                {
                    bw.Write((byte)0x01);
                    bw.Write(uncompressedSize);

                    var compressedBuffer = ArrayPool<byte>.Shared.Rent(uncompressedSize);
                    try
                    {
                        int compressedSize;

                        using (var compressStream = new MemoryStream(compressedBuffer, true))
                        {
                            using (var compressorStream = new DeflateStream(compressStream, CompressionMode.Compress, true))
                                compressorStream.Write(packetBuffer, 0, uncompressedSize);

                            compressedSize = (int)compressStream.Position;
                        }

                        bw.Write(compressedBuffer, 0, compressedSize);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(compressedBuffer);
                    }
                }
                else
                {
                    bw.Write(packetBuffer, 0, packetSize);
                }

                var currentPosition = bw.BaseStream.Position;

                bw.BaseStream.Position = sizePosition;

                bw.Write((ushort)(currentPosition - sizePosition));

                bw.BaseStream.Position = currentPosition;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packetBuffer);
            }
        }
    }
}
