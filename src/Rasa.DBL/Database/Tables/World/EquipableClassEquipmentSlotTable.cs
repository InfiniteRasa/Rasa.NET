using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class EquipableClassEquipmentSlotTable
    {
        private static readonly MySqlCommand GetSlotIdCommand = new MySqlCommand("SELECT slotId FROM equipableclass_slotid WHERE `entityId` = @EntityId");

        public static void Initialize()
        {
            GetSlotIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetSlotIdCommand.Parameters.Add("@EntityId", MySqlDbType.UInt32);
            GetSlotIdCommand.Prepare();
        }

        public static int GetSlotId(uint entityId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetSlotIdCommand.Parameters["@EntityId"].Value = entityId;

                using (var reader = GetSlotIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("slotId");
            }

            return 0;
        }
    }
}
