using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EquipmentTemplateTable
    {
        private static readonly MySqlCommand GetDbRowsCommand = new MySqlCommand("SELECT COUNT(*) FROM itemtemplate_equipment");
        private static readonly MySqlCommand GetEquipmentTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_equipment LIMIT @Row, 1");
        //private static readonly MySqlCommand GetItemTemplatesCommand = new MySqlCommand("SELECT itemTemplateId, slotType, requiredSkillId, requiredSkillMinVal FROM itemtemplate_equipment");

        public static void Initialize()
        {
            GetDbRowsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetDbRowsCommand.Prepare();

            GetEquipmentTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetEquipmentTemplatesCommand.Parameters.Add("@Row", MySqlDbType.Int32);
            GetEquipmentTemplatesCommand.Prepare();
        }

        public static long GetDbRows()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return (long)GetDbRowsCommand.ExecuteScalar();
            }
        }

        public static EquipmentTemplateEntry GetEquipmentTemplates(int row)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetEquipmentTemplatesCommand.Parameters["@Row"].Value = row;

                using (var reader = GetEquipmentTemplatesCommand.ExecuteReader())
                    return EquipmentTemplateEntry.Read(reader);
            }
        }
    }
}
