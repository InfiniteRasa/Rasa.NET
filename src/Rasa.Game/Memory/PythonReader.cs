using System;
using System.IO;

namespace Rasa.Memory
{
    using Extensions;

    public class PythonReader : IDisposable
    {
        public BinaryReader Reader { get; }

        public PythonReader(BinaryReader reader)
        {
            Reader = reader;
        }

        public void ReadNoneStruct()
        {
            var val = Reader.ReadByte();
            if (val != 0x00)
                throw new Exception($"Expected NoneStruct, found data: {val:X2}");
        }

        public void ReadTrueStruct()
        {
            var val = Reader.ReadByte();
            if (val != 0x01)
                throw new Exception($"Expected TrueStruct, found data: {val:X2}");
        }

        public void ReadZeroStruct()
        {
            var val = Reader.ReadByte();
            if (val != 0x02)
                throw new Exception($"Expected ZeroStruct, found data: {val:X2}");
        }

        public bool ReadBool()
        {
            var val = Reader.ReadByte();
            switch (val)
            {
                case 0x00: // NoneStruct
                case 0x10: // Int 0
                    return false;

                case 0x01: // TrueStruct
                case 0x11: // Int 1
                    return true;

                default:
                    throw new Exception($"Expected 0x00, 0x10 or 0x01, 0x11. Got: {val:X2}");
            }
        }

        public int ReadInt()
        {
            var type = Reader.ReadByte();
            if ((type & 0x10) != 0x10)
                throw new Exception($"Expected 0x1_. Got: {type:X2}");

            if (type <= 0x1C)
                return type & 0xF;

            switch (type)
            {
                case 0x1D:
                    return Reader.ReadByte();

                case 0x1E:
                    return Reader.ReadInt16();

                case 0x1F:
                    return Reader.ReadInt32();

                default:
                    throw new Exception($"WTF? Int type: {type:X2}");
            }
        }

        public long ReadLong()
        {
            var type = Reader.ReadByte();
            if ((type & 0x20) != 0x20)
                throw new Exception($"Expected 0x2_. Got: {type:X2}");

            if (type == 0x20)
                return 0L;

            if (type != 0x2F)
                throw new Exception($"WTF? Long type: {type:X2}");

            return Reader.ReadInt64();
        }

        public double ReadDouble()
        {
            var type = Reader.ReadByte();
            if ((type & 0x30) != 0x30)
                throw new Exception($"Expected 0x3_. Got: {type:X2}");

            switch (type)
            {
                case 0x30:
                    return 0.0D;

                case 0x31:
                    return 1.0D;

                case 0x1E:
                    return Reader.ReadDouble();

                case 0x1F:
                    return Reader.ReadSingle();

                default:
                    throw new Exception($"WTF? Double type: {type:X2}");
            }
        }

        public string ReadString()
        {
            var type = Reader.ReadByte();
            if ((type & 0x40) != 0x40)
                throw new Exception($"Expected 0x4_. Got: {type:X2}");

            int length;

            switch (type)
            {
                case 0x40:
                    return null;

                case 0x41:
                case 0x42:
                    throw new NotImplementedException();

                case 0x4D:
                    length = Reader.ReadByte();
                    break;

                case 0x4E:
                    length = Reader.ReadInt16();
                    break;

                case 0x4F:
                    length = Reader.ReadInt32();
                    break;

                default:
                    throw new Exception($"WTF? String type: {type:X2}");
            }

            return Reader.ReadUtf8StringOn(length);
        }

        public string ReadUnicodeString()
        {
            var type = Reader.ReadByte();
            if ((type & 0x50) != 0x50)
                throw new Exception($"Expected 0x5_. Got: {type:X2}");

            int length;

            switch (type)
            {
                case 0x50:
                    return null;

                case 0x52:
                    throw new NotImplementedException();

                case 0x5D:
                    length = Reader.ReadByte();
                    break;

                case 0x5E:
                    length = Reader.ReadInt16();
                    break;

                case 0x5F:
                    length = Reader.ReadInt32();
                    break;

                default:
                    throw new Exception($"WTF? String type: {type:X2}");
            }

            return Reader.ReadUtf8StringOn(length);
        }

        public int ReadDictionary()
        {
            var type = Reader.ReadByte();
            if ((type & 0x60) != 0x60)
                throw new Exception($"Expected 0x6_. Got: {type:X2}");

            if (type <= 0x6C)
                return type & 0x0F;

            switch (type)
            {
                case 0x6D:
                    return Reader.ReadByte();

                case 0x6E:
                    return Reader.ReadInt16();

                case 0x6F:
                    return Reader.ReadInt32();

                default:
                    throw new Exception($"WTF? Dictionary type: {type:X2}");
            }
        }

        public int ReadList()
        {
            var type = Reader.ReadByte();
            if ((type & 0x70) != 0x70)
                throw new Exception($"Expected 0x7_. Got: {type:X2}");

            if (type <= 0x7C)
                return type & 0x0F;

            switch (type)
            {
                case 0x7D:
                    return Reader.ReadByte();

                case 0x7E:
                    return Reader.ReadInt16();

                case 0x7F:
                    return Reader.ReadInt32();

                default:
                    throw new Exception($"WTF? List type: {type:X2}");
            }
        }

        public int ReadTuple()
        {
            var type = Reader.ReadByte();
            if ((type & 0x80) != 0x80)
                throw new Exception($"Expected 0x8_. Got: {type:X2}");

            if (type <= 0x8C)
                return type & 0x0F;

            switch (type)
            {
                case 0x8D:
                    return Reader.ReadByte();

                case 0x8E:
                    return Reader.ReadInt16();

                case 0x8F:
                    return Reader.ReadInt32();

                default:
                    throw new Exception($"WTF? Tuple type: {type:X2}");
            }
        }

        public void Dispose()
        {
            
        }
    }
}
