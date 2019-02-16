using MySql.Data.MySqlClient;
using System;

namespace Rasa.Structures
{
    public class ClanEntry
    {
        public uint Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
        public bool IsPvP { get; set; }
        public string RankTitle0 { get; set; }
        public string RankTitle1 { get; set; }
        public string RankTitle2 { get; set; }
        public string RankTitle3 { get; set; }

        public static ClanEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new ClanEntry
            {
                Id = reader.GetUInt32("id"),
                CreatedAt = reader.GetDateTime("created_at"),
                Name = reader.GetString("name"),
                IsPvP = reader.GetBoolean("ispvp"),
                RankTitle0 = reader.GetString("rank_title_0"),
                RankTitle1 = reader.GetString("rank_title_1"),
                RankTitle2 = reader.GetString("rank_title_2"),
                RankTitle3 = reader.GetString("rank_title_3"),
            };
        }
    }
}
