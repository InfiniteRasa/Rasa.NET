using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateRaceRequirementEntry
    {
        public int ItemTemplateId { get; set; }
        public short RaceId { get; set; }

        public static ItemTemplateRaceRequirementEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateRaceRequirementEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateid"),
                RaceId = reader.GetInt16("raceId")
            };
        }
    }
}
