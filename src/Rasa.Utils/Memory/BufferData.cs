using System;
using System.IO;

namespace Rasa.Memory
{
    public class BufferData
    {
        public byte[] Buffer => BufferManager.Buffer;
        public int BaseOffset { get; }
        public int MaxLength => BufferManager.BlockSize;

        public int Offset { get; set; }
        public int Length { get; set; }
        public int ByteCount { get; set; }
        public int ByteOffset => BaseOffset + ByteCount;
        public int Remaining => ByteCount - Offset;
        public int Missing => Length - ByteCount;

        public BufferData(int baseOffset)
        {
            BaseOffset = baseOffset;

            Reset();
        }

        public void Reset()
        {
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

        public void CopyTo(BufferData other, int offset, int length, int destOffset = 0)
        {
            CheckConstraints(offset, length);
            CheckConstraints(destOffset, length);

            Array.Copy(Buffer, BaseOffset + offset, Buffer, other.BaseOffset + destOffset, length);
        }

        public void MoveTo(BufferData other, int destOffset = 0)
        {
            MoveTo(other, Offset, Length - Offset, destOffset);
        }

        public void MoveTo(BufferData other, int offset, int length, int destOffset = 0)
        {
            CopyTo(other, offset, length, destOffset);

            Clear(offset, length);
        }

        public BinaryReader GetReader()
        {
            return GetReader(Offset, Length - Offset);
        }

        public BinaryReader GetReader(int offset, int length)
        {
            CheckConstraints(offset, length);

            return new BinaryReader(new MemoryStream(Buffer, BaseOffset + offset, length, false));
        }

        public BinaryWriter CreateWriter()
        {
            return CreateWriter(Offset, Length - Offset);
        }

        public BinaryWriter CreateWriter(int offset, int length)
        {
            CheckConstraints(offset, length);

            return new BinaryWriter(new MemoryStream(Buffer, BaseOffset + offset, length, true));
        }

        // ReSharper disable UnusedParameter.Local
        private void CheckConstraints(int offset, int length)
        {
            if (offset < 0 || offset >= MaxLength || offset + length > MaxLength)
                throw new Exception("BufferData tried to access another BufferData's memory!");
        }
        // ReSharper restore UnusedParameter.Local
    }
}
