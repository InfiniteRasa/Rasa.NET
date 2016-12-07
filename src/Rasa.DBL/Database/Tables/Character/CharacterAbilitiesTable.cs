using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterAbilitiesTable
    {
        public static readonly MySqlCommand BasicEntryCommand = new MySqlCommand("INSERT INTO character_abilities (id) VALUES (@Id)");

        public static void Initialize()
        {
            BasicEntryCommand.Connection = GameDatabaseAccess.CharConnection;
            BasicEntryCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            BasicEntryCommand.Prepare();
        }

        public static void BasicEntry(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                BasicEntryCommand.Parameters["@Id"].Value = id;
                BasicEntryCommand.ExecuteNonQuery();
            }
        }
    }
}
