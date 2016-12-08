using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterSkillsTable
    {
        public static readonly MySqlCommand BasicEntrycCommand = new MySqlCommand("INSERT INTO character_skills (id) VALUES (@Id)");

        public static void Initialize()
        {
            BasicEntrycCommand.Connection = GameDatabaseAccess.CharConnection;
            BasicEntrycCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            BasicEntrycCommand.Prepare();
        }

        public static void BasicEntry(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                BasicEntrycCommand.Parameters["@Id"].Value = id;
                BasicEntrycCommand.ExecuteNonQuery();
            }
        }
    }
}
