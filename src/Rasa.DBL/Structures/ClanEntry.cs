using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public class ClanEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public bool IsPvP { get; set; }

        public static ClanEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new ClanEntry
            {
                Id = reader.GetUInt32("id"),
                Name = reader.GetString("name"),
            };
        }
    }
}
