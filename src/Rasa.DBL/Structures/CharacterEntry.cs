using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public uint AccountId { get; set; }
        public int SlotId { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int RaceId { get; set; }
        public int ClassId { get; set; }
        public int MapContextId { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public int CloneCredits { get; set; }
        public int NumLogins { get; set; }
        public int TotalTimePlayed { get; set; }
        public string TimeSinceLastPlayed { get; set; }
        /* ToDo
         * public int ClanId { get; set; }
         * public int ClanName { get; set; }
         */


        public static CharacterEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new CharacterEntry
            {
                Id = reader.GetUInt32("id"),
                Name = reader.GetString("name"),
                FamilyName = reader.GetString("familyName"),
                AccountId = reader.GetUInt32("accountId"),
                SlotId = reader.GetInt32("slotId"),
                Gender = reader.GetInt32("gender"),
                Scale = reader.GetDouble("scale"),
                RaceId = reader.GetInt32("raceId"),
                ClassId = reader.GetInt32("classId"),
                MapContextId = reader.GetInt32("mapContextId"),
                PosX = reader.GetDouble("posX"),
                PosY = reader.GetDouble("posY"),
                PosZ = reader.GetDouble("posZ"),
                Rotation = reader.GetDouble("rotation"),
                Experience = reader.GetInt32("experience"),
                Level = reader.GetInt32("level"),
                Body = reader.GetInt32("body"),
                Mind = reader.GetInt32("mind"),
                Spirit = reader.GetInt32("spirit"),
                CloneCredits = reader.GetInt32("cloneCredits"),
                NumLogins = reader.GetInt32("numLogins"),
                TotalTimePlayed = reader.GetInt32("totalTimePlayed"),
                TimeSinceLastPlayed = reader.GetString("timeSinceLastPlayed")
            };
        }

        public static CharacterEntry GetData(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;
            
         return new CharacterEntry
            {
                Id = reader.GetUInt32("id"),
                Name = reader.GetString("name"),
                FamilyName = reader.GetString("familyName"),
                SlotId = reader.GetInt32("slotId"),
                Gender = reader.GetInt32("gender"),
                Scale = reader.GetDouble("scale"),
                MapContextId = reader.GetInt32("mapContextId"),
                ClassId = reader.GetInt32("classId"),
                RaceId = reader.GetInt32("raceId"),
            };
        }
    }
}
