using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateRaceRequirementEntry
    {
        public uint ItemTemplateId { get; set; }
        public short RaceId { get; set; }

        public static ItemTemplateRaceRequirementEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateRaceRequirementEntry
            {
                ItemTemplateId = reader.GetUInt32("itemTemplateid"),
                RaceId = reader.GetInt16("raceId")
            };
        }
    }
}
