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
        public uint Body { get; set; }
        public uint Mind { get; set; }
        public uint Spirit { get; set; }
        public uint CloneCredits { get; set; }
        public uint MapContextId { get; set; }
        public double CoordX { get; set; }
        public double CoordY { get; set; }
        public double CoordZ { get; set; }
        public double Rotation { get; set; }
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
                Body = reader.GetUInt32("body"),
                Mind = reader.GetUInt32("mind"),
                Spirit = reader.GetUInt32("spirit"),
                CloneCredits = reader.GetUInt32("clone_credits"),
                MapContextId = reader.GetUInt32("map_context_id"),
                CoordX = reader.GetDouble("coord_x"),
                CoordY = reader.GetDouble("coord_y"),
                CoordZ = reader.GetDouble("coord_z"),
                Rotation = reader.GetDouble("rotation"),
                NumLogins = reader.GetUInt32("num_logins"),
                LastLogin = reader.IsDBNull(lastLoginOrdinal) ? (DateTime?) null : reader.GetDateTime(lastLoginOrdinal),
                TotalTimePlayed = reader.GetUInt32("total_time_played")
            };
        }
    }
}
