using System;
using System.IO;

namespace Rasa.Memory
{
    using Extensions;

    public class PythonWriter : IDisposable
    {
        public BinaryWriter Writer { get; }

        public PythonWriter(BinaryWriter writer)
        {
            Writer = writer;
        }

        public void WriteNoneStruct()
        {
            Writer.Write((byte) 0x00);
        }

        public void WriteTrueStruct()
        {
            Writer.Write((byte) 0x01);
        }

        public void WriteZeroStruct()
        {
            Writer.Write((byte) 0x02);
        }

        public void WriteBool(bool value) // TODO: Maybe send 0 and 1 as int
        {
            if (value)
                WriteTrueStruct();
            else
                WriteNoneStruct();
        }

        public void WriteInt(int value)
        {
            if (value > 0x0C)
            {
                if ((sbyte) value == value)
                {
                    Writer.Write((byte) 0x1D);
                    Writer.Write((byte) value);
                }
                else if ((short) value == value)
                {
                    Writer.Write((byte) 0x1E);
                    Writer.Write((short) value);
                }
                else
                {
                    Writer.Write((byte) 0x1F);
                    Writer.Write(value);
                }
            }
            else
                Writer.Write((byte) (0x10 | value));
        }

        public void WriteLong(long value)
        {
            if (value != 0)
            {
                Writer.Write((byte) 0x2F);
                Writer.Write(value);
            }
            else
                Writer.Write((byte) 0x20);
        }

        public void WriteDouble(double value)
        {
            if (Math.Abs(value) < double.Epsilon)
                Writer.Write((byte) 0x30);
            else if (Math.Abs(value - 1.0D) < double.Epsilon)
                Writer.Write((byte) 0x31);
            else if (Math.Abs(value - (float) value) < 0.01D) // Using 0.01D as an epsilon, as the client does
            {
                Writer.Write((byte) 0x3F);
                Writer.Write((float) value);
            }
            else
            {
                Writer.Write((byte) 0x3E);
                Writer.Write(value);
            }
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                Writer.Write((byte) 0x40);
                return;
            }

            if (value.Length <= 0xFF)
            {
                Writer.Write((byte) 0x4D);
                Writer.Write((byte) value.Length);
            }
            else if (value.Length <= 0xFFFF)
            {
                Writer.Write((byte) 0x4E);
                Writer.Write((ushort) value.Length);
            }
            else
            {
                Writer.Write((byte) 0x4F);
                Writer.Write(value.Length);
            }

            Writer.WriteUtf8String(value);
        }

        public void WriteUnicodeString(string value)
        {
            if (value == null)
            {
                Writer.Write((byte) 0x50);
                return;
            }

            if (value.Length <= 0xFF)
            {
                Writer.Write((byte) 0x5D);
                Writer.Write((byte) value.Length);
            }
            else if (value.Length <= 0xFFFF)
            {
                Writer.Write((byte) 0x5E);
                Writer.Write((ushort) value.Length);
            }
            else
            {
                Writer.Write((byte) 0x5F);
                Writer.Write(value.Length);
            }

            Writer.WriteUtf8String(value);
        }

        public void WriteDictionary(int elementCount)
        {
            if (elementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be zero or greater!");

            if (elementCount <= 0x0C)
                Writer.Write((byte) (0x60 | elementCount));
            else if ((byte) elementCount == elementCount)
            {
                Writer.Write((byte) 0x6D);
                Writer.Write((byte) elementCount);
            }
            else if ((ushort) elementCount == elementCount)
            {
                Writer.Write((byte) 0x6E);
                Writer.Write((ushort) elementCount);
            }
            else
            {
                Writer.Write((byte) 0x6F);
                Writer.Write(elementCount);
            }
        }

        public void WriteList(int elementCount)
        {
            if (elementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be zero or greater!");

            if (elementCount <= 0x0C)
                Writer.Write((byte) (0x70 | elementCount));
            else if ((byte) elementCount == elementCount)
            {
                Writer.Write((byte) 0x7D);
                Writer.Write((byte) elementCount);
            }
            else if ((ushort) elementCount == elementCount)
            {
                Writer.Write((byte) 0x7E);
                Writer.Write((ushort) elementCount);
            }
            else
            {
                Writer.Write((byte) 0x7F);
                Writer.Write(elementCount);
            }
        }

        public void WriteTuple(int elementCount)
        {
            if (elementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be zero or greater!");

            if (elementCount <= 0x0C)
                Writer.Write((byte) (0x80 | elementCount));
            else if ((byte) elementCount == elementCount)
            {
                Writer.Write((byte) 0x8D);
                Writer.Write((byte) elementCount);
            }
            else if ((ushort) elementCount == elementCount)
            {
                Writer.Write((byte) 0x8E);
                Writer.Write((ushort) elementCount);
            }
            else
            {
                Writer.Write((byte) 0x8F);
                Writer.Write(elementCount);
            }
        }

        public void Dispose()
        {

        }
    }
}
