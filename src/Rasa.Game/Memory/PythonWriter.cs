using System;
using System.IO;
using System.Text;

namespace Rasa.Memory
{
    using Extensions;

    public sealed class PythonWriter : IDisposable
    {
        public BinaryWriter Writer { get; }
        public long BeginPositon { get; }

        public PythonWriter(BinaryWriter writer)
        {
            Writer = writer;
            BeginPositon = Writer.BaseStream.Position;
        }

        public void WriteNoneStruct()
        {
            Writer.Write((byte)PythonStruct.None);
        }

        public void WriteTrueStruct()
        {
            Writer.Write((byte)PythonStruct.True);
        }

        public void WriteZeroStruct()
        {
            Writer.Write((byte)PythonStruct.Zero);
        }

        /// <summary>
        /// A Python bool. False is the Zero struct, not None: the marshal has both, and they are
        /// different values. Python treats either as falsy, so anything the client only tests the
        /// truth of cannot tell them apart - but a value it hands to a native call can. The trade
        /// window does exactly that (tradewindow.py __ShowYourAccept passes the received
        /// confirmValue straight into SetVisible and Enable), and a None there is not a bool.
        /// ReadBool accepts None, True and Zero, so this still round-trips.
        /// </summary>
        public void WriteBool(bool value)
        {
            if (value)
                WriteTrueStruct();
            else
                WriteZeroStruct();
        }

        /// <summary>
        /// 0x10..0x1C carry the value 0..12 in the type byte itself; 0x1D, 0x1E and 0x1F escape
        /// to a one, two or four byte value.
        ///
        /// A negative number took the inline path, because it is not greater than 12. There is no
        /// room for one there: 0x10 | -1 is 0xFF after the cast, a type byte for something that is
        /// not an int at all, and the client's unmarshaller reads the rest of the packet as
        /// whatever that happens to mean.
        ///
        /// Negatives now take the four byte form. The narrow escapes are left to positive values
        /// on purpose: the client's marshaller puts -128..-1 in the one byte form (see
        /// PythonReader.ReadInt), so its unmarshaller very likely reads that byte signed too, but
        /// the four byte form is right whichever way it reads, and three bytes is not worth the
        /// assumption.
        /// </summary>
        public void WriteInt(int value)
        {
            if (value < 0)
            {
                Writer.Write((byte) 0x1F);
                Writer.Write(value);
            }
            else if (value > 0x0C)
            {
                // Kept to the signed byte range, so the value is the same whether the reader on
                // the other side treats 0x1D as signed or unsigned. 128..255 fall through to the
                // two byte form, where they are positive either way.
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

        public void WriteUInt(uint value)
        {
            WriteInt((int) value);
        }

        public void WriteLong(long value)
        {
            if (value != 0L)
            {
                Writer.Write((byte) 0x2F);
                Writer.Write(value);
            }
            else
                Writer.Write((byte) 0x20);
        }

        public void WriteULong(ulong value)
        {
            if (value != 0UL)
            {
                Writer.Write((byte) 0x2F);
                Writer.Write(value);
            }
            else
                Writer.Write((byte) 0x20);
        }

        /// <summary>
        /// The number the other side reads for one written with <see cref="WriteDouble"/>: the
        /// nearest single-precision one, unless that is 0.01 or more away. 0.05 is read as
        /// 0.0500000007..., and what the client then works out from it - a tooltip's amount,
        /// rounded up - is worked out from that. Anything the server has to work out the same
        /// way starts from this.
        /// </summary>
        public static double AsRead(double value)
        {
            return Math.Abs(value - (float) value) < 0.01D ? (float) value : value;
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

        /// <summary>
        /// The length prefix counts bytes, not characters - ReadUtf8StringOn on the other side
        /// takes it straight to ReadBytes. string.Length counts UTF-16 code units, so every
        /// character outside ASCII used to be announced short: an accented letter is one char and
        /// two bytes, so the reader stopped one byte early and took the rest of that letter as the
        /// next type byte. From there the whole packet is garbage, and an accented family name in
        /// a squad roster is enough to do it.
        ///
        /// Encoding once and measuring the result also picks the right header: a string of 200
        /// accented characters is 400 bytes and cannot be announced in the one byte form at all.
        /// </summary>
        public void WriteString(string value)
        {
            if (value == null)
            {
                Writer.Write((byte) 0x40);
                return;
            }

            WriteStringBytes(value, 0x40);
        }

        public void WriteUnicodeString(string value)
        {
            if (value == null)
            {
                Writer.Write((byte) 0x50);
                return;
            }

            WriteStringBytes(value, 0x50);
        }

        private void WriteStringBytes(string value, byte type)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            if (bytes.Length <= 0xFF)
            {
                Writer.Write((byte) (type | 0x0D));
                Writer.Write((byte) bytes.Length);
            }
            else if (bytes.Length <= 0xFFFF)
            {
                Writer.Write((byte) (type | 0x0E));
                Writer.Write((ushort) bytes.Length);
            }
            else
            {
                Writer.Write((byte) (type | 0x0F));
                Writer.Write(bytes.Length);
            }

            Writer.Write(bytes);
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

        public void WriteStruct<T>(T structure)
            where T : IPythonDataStruct
        {
            if (structure == null)
            {
                WriteNoneStruct();
                return;
            }

            structure.Write(this);
        }

        public override string ToString()
        {
            var data = new byte[Writer.BaseStream.Position - BeginPositon];

            var currentPosition = Writer.BaseStream.Position;

            Writer.BaseStream.Position = BeginPositon;
            Writer.BaseStream.Read(data, 0, data.Length);
            Writer.BaseStream.Position = currentPosition;

            using var pr = new PythonReader(new BinaryReader(new MemoryStream(data)));
            return pr.ToString();
        }

        public void Dispose()
        {
        }
    }
}
