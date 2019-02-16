using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System;

namespace Rasa.Structures
{
    public class ClanRankEntry
    {
        public uint Id { get; set; }
        public uint RankNumer { get; set; }
        public string Title { get; set; }

        public static ClanRankEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new ClanRankEntry
            {
                Id = reader.GetUInt32("id"),
                RankNumer = reader.GetUInt32("rank_number"),
                Title = reader.GetString("title"),
            };
        }
    }
}
