using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterEntry
    {
        public uint CharacterId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public uint AccountId { get; set; }
        public uint SlotId { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int RaceId { get; set; }
        public int ClassId { get; set; }
        public int GameContextId { get; set; }
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
        public int TimeSinceLastPlayed { get; set; }
        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public int Credits { get; set; }
        public int Prestige { get; set; }
        public string Logos { get; set; }
        public string Skills { get; set; }
        public int CurrentAbilityDrawer { get; set; }

        public static CharacterEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new CharacterEntry
            {
                CharacterId = reader.GetUInt32("characterId"),
                Name = reader.GetString("name"),
                FamilyName = reader.GetString("familyName"),
                AccountId = reader.GetUInt32("accountId"),
                SlotId = reader.GetUInt32("slotId"),
                Gender = reader.GetInt32("gender"),
                Scale = reader.GetDouble("scale"),
                RaceId = reader.GetInt32("raceId"),
                ClassId = reader.GetInt32("classId"),
                GameContextId = reader.GetInt32("gameContextId"),
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
                TimeSinceLastPlayed = reader.GetInt32("timeSinceLastPlayed"),
                ClanId = reader.GetInt32("clanId"),
                ClanName = reader.GetString("clanName"),
                Credits = reader.GetInt32("credits"),
                Prestige = reader.GetInt32("prestige"),
                CurrentAbilityDrawer = reader.GetInt32("currentAbilityDrawer"),
                Logos = reader.GetString("logos")
            };
        }
    }
}
