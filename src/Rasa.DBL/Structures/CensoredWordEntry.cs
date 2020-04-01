using System;

using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CensoredWordEntry
    {
        public uint Id { get; set; }
        public string Word { get; set; }

        public static CensoredWordEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new CensoredWordEntry
            {
                Id = reader.GetUInt32("id"),
                Word = reader.GetString("word")
            };            
        }
    }
}
