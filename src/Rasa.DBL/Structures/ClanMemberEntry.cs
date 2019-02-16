using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rasa.Structures
{
    public class ClanMemberEntry
    {
        public uint CharacterId { get; set; }
        public uint ClanId { get; set; }
        public uint Rank { get; set; }
        public string Note { get; set; }

        public static ClanMemberEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new ClanMemberEntry
            {
                CharacterId = reader.GetUInt32("character_id"),
                ClanId = reader.GetUInt32("clan_id"),
                Rank = reader.GetUInt32("rank"),
                Note = reader.GetString("note"),
            };
        }
    }
}
