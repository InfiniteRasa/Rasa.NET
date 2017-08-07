using System;
using System.IO;
using System.Text;

namespace Rasa.Memory
{
    public class BufferData
    {
        public byte[] Buffer => BufferManager.Buffer;
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

        public void Clear()
        {
            Clear(0, MaxLength);
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

            Array.Copy(source.Buffer, source.BaseOffset + sourceOffset, dest.Buffer, dest.BaseOffset + destOffset, length);
        }

        public void MoveTo(BufferData other, int destOffset = 0)
        {
            MoveTo(other, Offset, RemainingLength, destOffset);
        }

        public void MoveTo(BufferData other, int offset, int length, int destOffset = 0)
        {
            Copy(this, offset, other, destOffset, length);

            Clear(offset, length);
        }

        public BinaryReader GetReader()
        {
            return GetReader(Offset, RemainingLength, Encoding.UTF8, false);
        }

        public BinaryReader GetReader(int offset, int length)
        {
            return GetReader(offset, length, Encoding.UTF8, false);
        }

        public BinaryReader GetReader(int offset, int length, Encoding encoding)
        {
            return GetReader(offset, length, encoding, false);
        }

        public BinaryReader GetReader(int offset, int length, Encoding encoding, bool leaveOpen)
        {
            CheckConstraints(offset, length);

            return new BinaryReader(new MemoryStream(Buffer, BaseOffset + offset, length, false), encoding, leaveOpen);
        }

        public BinaryWriter CreateWriter()
        {
            return CreateWriter(Offset, RemainingLength, Encoding.UTF8, false);
        }

        public BinaryWriter CreateWriter(int offset, int length)
        {
            return CreateWriter(offset, length, Encoding.UTF8, false);
        }

        public BinaryWriter CreateWriter(int offset, int length, Encoding encoding)
        {
            return CreateWriter(offset, length, encoding, false);
        }

        public BinaryWriter CreateWriter(int offset, int length, Encoding encoding, bool leaveOpen)
        {
            CheckConstraints(offset, length);

            return new BinaryWriter(new MemoryStream(Buffer, BaseOffset + offset, length, true), encoding, leaveOpen);
        }

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

        public override string ToString()
        {
            return $"BufferData[{BaseOffset} + {Offset}, {Length}]";
        }
    }
}
