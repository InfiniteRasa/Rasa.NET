using System;

using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterEntry
    {
        public uint Id { get; set; }
        public uint AccountId { get; set; }
        public byte Slot { get; set; }
        public string Name { get; set; }
        public byte Race { get; set; }
        public uint Class { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public uint Experience { get; set; }
        public byte Level { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public uint CloneCredits { get; set; }
        public uint MapContextId { get; set; }
        public float CoordX { get; set; }
        public float CoordY { get; set; }
        public float CoordZ { get; set; }
        public float Rotation { get; set; }
        public uint NumLogins { get; set; }
        public DateTime? LastLogin { get; set; }
        public uint TotalTimePlayed { get; set; }

        public static CharacterEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            var lastLoginOrdinal = reader.GetOrdinal("last_login");

            return new CharacterEntry
            {
                Id = reader.GetUInt32("id"),
                AccountId = reader.GetUInt32("account_id"),
                Slot = reader.GetByte("slot"),
                Name = reader.GetString("name"),
                Race = reader.GetByte("race"),
                Class = reader.GetUInt32("class"),
                Gender = (byte) (reader.GetBoolean("gender") ? 1 : 0),
                Scale = reader.GetDouble("scale"),
                Experience = reader.GetUInt32("experience"),
                Level = reader.GetByte("level"),
                Body = reader.GetInt32("body"),
                Mind = reader.GetInt32("mind"),
                Spirit = reader.GetInt32("spirit"),
                CloneCredits = reader.GetUInt32("clone_credits"),
                MapContextId = reader.GetUInt32("map_context_id"),
                CoordX = reader.GetFloat("coord_x"),
                CoordY = reader.GetFloat("coord_y"),
                CoordZ = reader.GetFloat("coord_z"),
                Rotation = reader.GetFloat("rotation"),
                NumLogins = reader.GetUInt32("num_logins"),
                LastLogin = reader.IsDBNull(lastLoginOrdinal) ? (DateTime?) null : reader.GetDateTime(lastLoginOrdinal),
                TotalTimePlayed = reader.GetUInt32("total_time_played")
            };
        }
    }
}
