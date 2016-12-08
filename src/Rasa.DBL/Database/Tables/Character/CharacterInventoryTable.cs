using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterInventoryTable
    {
        public static readonly MySqlCommand BasicInventoryCommand = new MySqlCommand(
            "INSERT INTO character_inventory" +
            "(id, slot50item, slot50qty, slot250item, slot250qty, slot251item, slot251qty, slot252item, slot252qty, slot253item, slot253qty, " +
            "slot265item, slot265qty, slot266item, slot266qty, slot267item, slot267qty) VALUES" +
            "(@Id, 28, 100, 17131, 1, 13126, 1, 13066, 1, 13096, 1, 13186, 1, 13156, 1, 17131, 1)");

        public static void Initialize()
        {
            BasicInventoryCommand.Connection = GameDatabaseAccess.CharConnection;
            BasicInventoryCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            BasicInventoryCommand.Prepare();
        }

        public static void BasicInventory(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                BasicInventoryCommand.Parameters["@Id"].Value = id;
                BasicInventoryCommand.ExecuteNonQuery();
            }
        }
    }
}
