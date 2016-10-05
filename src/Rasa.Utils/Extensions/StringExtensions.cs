using System;

namespace Rasa.Utils.Extensions
{
    public static class StringExtensions
    {
        public static byte[] GetByteArrayFromString(this string val)
        {
            var split = val.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var key = new byte[split.Length];

            for (var i = 0; i < split.Length; ++i)
                key[i] = byte.Parse(split[i]);

            return key;
        }
    }
}
