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

        public uint Hue => (uint) ((Alpha << 24) | (Blue << 16) | (Green << 8) | Red);

        /// <summary>
        /// The colour an item is made in until it is painted: white, (255, 255, 255, 255). It is
        /// the client's own for an unpainted piece: the creation window dresses its mannequin's
        /// recruit boots, vest and legs in kDefaultColorArmor (charactercreationwindow.py), and
        /// the tier preview a slot with nothing in it (tierselect.py). It was 2139062144
        /// (0x7F7F7F80), a half-alpha mid-grey that nothing in the client uses.
        /// </summary>
        public const uint ItemDefault = 0xFFFFFFFF;

        public Color()
        {
        }

        public Color(uint hue)
            : this((byte) (hue & 0xFF), (byte) ((hue >> 8) & 0xFF), (byte) ((hue >> 16) & 0xFF), (byte) ((hue >> 24) & 0xFF))
        {
        }

        public Color(byte red, byte green, byte blue, byte alpha = 0xFF)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public static Color RandomColor()
        {
            return new Color((byte)Random.Shared.Next(0,255), (byte)Random.Shared.Next(0, 255), (byte)Random.Shared.Next(0, 255));
        }

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

        public static void WriteEmpty(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteNoneStruct();
            pw.WriteNoneStruct();
            pw.WriteNoneStruct();
            pw.WriteNoneStruct();
        }
    }
}
