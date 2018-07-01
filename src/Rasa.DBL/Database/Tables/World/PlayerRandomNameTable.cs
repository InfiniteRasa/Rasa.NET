using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class PlayerRandomNameTable
    {
        public enum Gender : byte
        {
            Male    = 0,
            Female  = 1,
            Neutral = 2
        }

        public enum NameType : byte
        {
            First = 0,
            Last  = 1
        }

        private static readonly MySqlCommand GetRandomName = new MySqlCommand("SELECT `name` FROM `player_random_name` WHERE `type` = @Type AND `gender` IN (@Neutral, @Gender) ORDER BY RAND() LIMIT 1;");

        public static void Initialize()
        {
            GetRandomName.Connection = GameDatabaseAccess.WorldConnection;
            GetRandomName.Parameters.Add("@Gender", MySqlDbType.UByte);
            GetRandomName.Parameters.Add("@Type", MySqlDbType.UByte);
            GetRandomName.Parameters.Add("@Neutral", MySqlDbType.UByte);
            GetRandomName.Prepare();
        }

        public static string GetRandom(Gender gender, NameType type)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetRandomName.Parameters["@Neutral"].Value = (byte) Gender.Neutral;
                GetRandomName.Parameters["@Gender"].Value = (byte) gender;
                GetRandomName.Parameters["@Type"].Value = (byte) type;

                using (var reader = GetRandomName.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("name");
            }

            return null;
        }
    }
}
