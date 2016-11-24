using System;

namespace Rasa.Structures
{
    using Memory;

    public class Color : IPythonDataStruct
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte Alpha { get; set; }

        public int Hue => (Alpha << 24) | (Blue << 16) | (Green << 8) | Red;

        public void Read(PythonReader pr)
        {
            var size = pr.ReadTuple();
            if (size != 4)
                throw new Exception("Invalid Color tuple parameter count!");

            switch (pr.PeekType())
            {
                case PythonType.Int:
                    Red = (byte) pr.ReadInt();
                    Green = (byte) pr.ReadInt();
                    Blue = (byte) pr.ReadInt();
                    Alpha = (byte) pr.ReadInt();
                    break;

                case PythonType.Long:
                    Red = (byte) pr.ReadLong();
                    Green = (byte) pr.ReadLong();
                    Blue = (byte) pr.ReadLong();
                    Alpha = (byte) pr.ReadLong();
                    break;

                default:
                    throw new Exception("Invalid peeked type at Color reading!");
            }
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Red);
            pw.WriteInt(Green);
            pw.WriteInt(Blue);
            pw.WriteInt(Alpha);
        }
    }
}
