using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class PlayerRandomNameTable
    {
        private static readonly MySqlCommand GetRandomName = new MySqlCommand("SELECT `name` FROM `player_random_name` WHERE `type` = @Type AND (`gender` = 'neutral' OR `gender` = @Gender) ORDER BY RAND() LIMIT 1;");

        public static void Initialize()
        {
            GetRandomName.Connection = GameDatabaseAccess.WorldConnection;
            GetRandomName.Parameters.Add("@Gender", MySqlDbType.VarChar);
            GetRandomName.Parameters.Add("@Type", MySqlDbType.VarChar);
            GetRandomName.Prepare();
        }

        public static string GetRandom(string gender, string type)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetRandomName.Parameters["@Gender"].Value = gender;
                GetRandomName.Parameters["@Type"].Value = type;

                using (var reader = GetRandomName.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("name");
            }

            return null;
        }
    }
}
