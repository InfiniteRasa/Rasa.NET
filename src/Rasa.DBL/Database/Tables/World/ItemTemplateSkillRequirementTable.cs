using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateSkillRequirementTable
    {
        private static readonly MySqlCommand GetItemTemplateSkillRequirementCommand = new MySqlCommand("SELECT * FROM itemtemplate_skillrequirement");

        public static void Initialize()
        {

            GetItemTemplateSkillRequirementCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateSkillRequirementCommand.Prepare();
        }

        public static List<ItemTemplateSkillRequirementEntry> GetItemTemplateSkillRequirement()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemTemplateSkillRequirement = new List<ItemTemplateSkillRequirementEntry>();

                using (var reader = GetItemTemplateSkillRequirementCommand.ExecuteReader())
                    while (reader.Read())
                        itemTemplateSkillRequirement.Add(ItemTemplateSkillRequirementEntry.Read(reader));

                return itemTemplateSkillRequirement;
            }
        }
    }
}
