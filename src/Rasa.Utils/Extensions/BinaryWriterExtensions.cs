using System.IO;

namespace Rasa.Extensions;

public static class BinaryWriterExtensions
{
    public static void WriteLengthedString(this BinaryWriter writer, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            writer.Write(0);
            return;
        }

        writer.Write(text.Length);
        writer.WriteUtf8StringOn(text);
    }

    public static void WriteUtf8String(this BinaryWriter writer, string text)
    {
        writer.WriteUtf8StringOn(text);
    }

    public static void WriteUtf8StringOn(this BinaryWriter writer, string text, int length = -1)
    {
        if (length == -1)
            length = text.Length;

        writer.Write(Encoding.UTF8.GetBytes(text));

        if (length <= text.Length)
            return;

        for (var i = 0; i < length - text.Length; ++i)
            writer.Write((byte) 0);
    }

    public static void WriteAt(this BinaryWriter writer, byte value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, sbyte value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, short value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, ushort value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, int value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, uint value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, long value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, ulong value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, float value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }

    public static void WriteAt(this BinaryWriter writer, double value, long position)
    {
        var currentPosition = writer.BaseStream.Position;

        writer.BaseStream.Position = position;

        writer.Write(value);

        writer.BaseStream.Position = currentPosition;
    }
}
