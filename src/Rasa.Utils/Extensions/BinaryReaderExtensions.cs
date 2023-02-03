using System.IO;

namespace Rasa.Extensions;

public static class BinaryReaderExtensions
{
    public static string ReadLengthedString(this BinaryReader reader)
    {
        var len = reader.ReadInt32();
        return len == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    public static string ReadUtf8StringOn(this BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);

        var index = Array.IndexOf<byte>(bytes, 0);
        if (index == -1)
            index = bytes.Length;

        return index == 0 ? string.Empty : Encoding.UTF8.GetString(bytes, 0, index);
    }
}
