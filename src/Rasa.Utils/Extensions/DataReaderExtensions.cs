using MySql.Data.MySqlClient;

namespace Rasa.Extensions
{
    public static class DataReaderExtensions
    {
        public static ushort GetUInt16(this MySqlDataReader reader, int column)
        {
            return (ushort) reader.GetInt16(column);
        }

        public static ushort GetUInt16(this MySqlDataReader reader, string column)
        {
            return (ushort) reader.GetInt16(column);
        }

        public static uint GetUInt32(this MySqlDataReader reader, int column)
        {
            return (uint) reader.GetInt32(column);
        }

        public static uint GetUInt32(this MySqlDataReader reader, string column)
        {
            return (uint) reader.GetInt32(column);
        }

        public static ulong GetUInt64(this MySqlDataReader reader, int column)
        {
            return (ulong) reader.GetInt64(column);
        }

        public static ulong GetUInt64(this MySqlDataReader reader, string column)
        {
            return (ulong) reader.GetInt64(column);
        }
    }
}
