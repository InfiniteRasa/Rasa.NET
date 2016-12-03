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
                Rotation = reader.GetDouble("rotation")
            };
        }
    }
}
