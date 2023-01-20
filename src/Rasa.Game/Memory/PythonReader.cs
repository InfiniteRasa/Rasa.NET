using System;
using System.IO;
using System.Text;

namespace Rasa.Memory
{
    using Extensions;

    public enum PythonType
    {
        Structs = 0x00,
        Int = 0x10,
        Long = 0x20,
        Double = 0x30,
        String = 0x40,
        UnicodeString = 0x50,
        Dictionary = 0x60,
        List = 0x70,
        Tuple = 0x80
    }

    public enum PythonStruct
    {
        None = 0x00,
        True = 0x01,
        Zero = 0x02
    }

    public sealed class PythonReader : IDisposable
    {
        public BinaryReader Reader { get; }
        public long BeginPosition { get; }

        public PythonReader(BinaryReader reader)
        {
            Reader = reader;
            BeginPosition = Reader.BaseStream.Position;
        }

        public PythonType PeekType()
        {
            var type = Reader.ReadByte();

            --Reader.BaseStream.Position;

            return (PythonType)(type & 0xF0);
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

        public PythonStruct ReadUnkStruct()
        {
            var val = Reader.ReadByte();

            return val switch
            {
                0x00 => PythonStruct.None,
                0x01 => PythonStruct.True,
                0x02 => PythonStruct.Zero,
                _ => throw new Exception($"Expected NoneStruct, TrueStruct or ZeroStruct, found data: {val:X2}"),
            };
        }

        public bool ReadBool()
        {
            var val = Reader.ReadByte();
            return val switch
            {
                0x00 or 0x10 => false, // NoneStruct or int 0
                0x01 or 0x11 => true, // TrueStruct or int 1
                0x02 => false, // ZeroStruct
                _ => throw new Exception($"Expected 0x00, 0x10 or 0x01, 0x11. Got: {val:X2}"),
            };
        }

        public int ReadInt()
        {
            var type = Reader.ReadByte();
            if ((type & 0x10) != 0x10)
                throw new Exception($"Expected 0x1_. Got: {type:X2}");

            if (type <= 0x1C)
                return type & 0xF;

            return type switch
            {
                0x1D => Reader.ReadByte(),
                0x1E => Reader.ReadInt16(),
                0x1F => Reader.ReadInt32(),
                _ => throw new Exception($"WTF? Int type: {type:X2}"),
            };
        }

        public uint ReadUInt()
        {
            var type = Reader.ReadByte();
            if ((type & 0x10) != 0x10)
                throw new Exception($"Expected 0x1_. Got: {type:X2}");

            if (type <= 0x1C)
                return (uint)(type & 0xF);

            return type switch
            {
                0x1D => Reader.ReadByte(),
                0x1E => Reader.ReadUInt16(),
                0x1F => Reader.ReadUInt32(),
                _ => throw new Exception($"WTF? Int type: {type:X2}"),
            };
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

        public ulong ReadULong()
        {
            var type = Reader.ReadByte();
            if ((type & 0x20) != 0x20)
                throw new Exception($"Expected 0x2_. Got: {type:X2}");

            if (type == 0x20)
                return 0UL;

            if (type != 0x2F)
                throw new Exception($"WTF? Long type: {type:X2}");

            return Reader.ReadUInt64();
        }

        public double ReadDouble()
        {
            var type = Reader.ReadByte();
            if ((type & 0x30) != 0x30)
                throw new Exception($"Expected 0x3_. Got: {type:X2}");

            return type switch
            {
                0x30 => 0.0D,
                0x31 => 1.0D,
                0x3E => Reader.ReadDouble(),
                0x3F => Reader.ReadSingle(),
                _ => throw new Exception($"WTF? Double type: {type:X2}"),
            };
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

            return type switch
            {
                0x6D => Reader.ReadByte(),
                0x6E => Reader.ReadInt16(),
                0x6F => Reader.ReadInt32(),
                _ => throw new Exception($"WTF? Dictionary type: {type:X2}"),
            };
        }

        public int ReadList()
        {
            var type = Reader.ReadByte();
            if ((type & 0x70) != 0x70)
                throw new Exception($"Expected 0x7_. Got: {type:X2}");

            if (type <= 0x7C)
                return type & 0x0F;

            return type switch
            {
                0x7D => Reader.ReadByte(),
                0x7E => Reader.ReadInt16(),
                0x7F => Reader.ReadInt32(),
                _ => throw new Exception($"WTF? List type: {type:X2}"),
            };
        }

        public int ReadTuple()
        {
            var type = Reader.ReadByte();
            if ((type & 0x80) != 0x80)
                throw new Exception($"Expected 0x8_. Got: {type:X2}");

            if (type <= 0x8C)
                return type & 0x0F;

            return type switch
            {
                0x8D => Reader.ReadByte(),
                0x8E => Reader.ReadInt16(),
                0x8F => Reader.ReadInt32(),
                _ => throw new Exception($"WTF? Tuple type: {type:X2}"),
            };
        }

        public T ReadStruct<T>()
            where T : IPythonDataStruct, new()
        {
            var ret = new T();

            ret.Read(this);

            return ret;
        }

        public override string ToString()
        {
            var originalPosition = Reader.BaseStream.Position;

            Reader.BaseStream.Position = BeginPosition;

            var sb = new StringBuilder();

            while (Reader.BaseStream.Position < Reader.BaseStream.Length)
            {
                var type = Reader.ReadByte();
                if (type == 0x66)
                {
                    //if (Reader.ReadByte() == 0x2A)
                        break;

                    //--Reader.BaseStream.Position;
                }

                --Reader.BaseStream.Position;

                switch ((PythonType)(type & 0xF0))
                {
                    case PythonType.Structs:
                        sb.Append("Struct: ").AppendLine($"{ReadUnkStruct()}");
                        break;

                    case PythonType.Int:
                        sb.Append("Integer: ").AppendLine($"{ReadInt()}");
                        break;

                    case PythonType.Long:
                        sb.Append("Long: ").AppendLine($"{ReadLong()}");
                        break;

                    case PythonType.Double:
                        sb.Append("Double: ").AppendLine($"{ReadDouble()}");
                        break;

                    case PythonType.String:
                        sb.Append("String: ").AppendLine($"{ReadString()}");
                        break;

                    case PythonType.UnicodeString:
                        sb.Append("Unicode string: ").AppendLine($"{ReadUnicodeString()}");
                        break;

                    case PythonType.Dictionary:
                        sb.Append("Dictionary: Element count: ").AppendLine($"{ReadDictionary()}");
                        break;

                    case PythonType.List:
                        sb.Append("List: Element count: ").AppendLine($"{ReadList()}");
                        break;

                    case PythonType.Tuple:
                        sb.Append("Tuple: Element count: ").AppendLine($"{ReadTuple()}");
                        break;

                    default:
                        throw new Exception($"Invalid type read! Type: {type:X}");
                }
            }

            Reader.BaseStream.Position = originalPosition;

            return sb.ToString();
        }

        public void Dispose()
        {
        }
    }
}
