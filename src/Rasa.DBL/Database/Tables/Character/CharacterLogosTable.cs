using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterLogosTable
    {
        private static readonly MySqlCommand GetLogosCommand = new MySqlCommand("SELECT logosId FROM character_logos WHERE characterId = @CharacterId");
        private static readonly MySqlCommand SetLogosCommand = new MySqlCommand("INSERT INTO character_logos (characterId, logosId) VALUES (@CharacterId, @LogosId)");

        public static void Initialize()
        {
            GetLogosCommand.Connection = GameDatabaseAccess.CharConnection;
            GetLogosCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetLogosCommand.Prepare();

            SetLogosCommand.Connection = GameDatabaseAccess.CharConnection;
            SetLogosCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            SetLogosCommand.Parameters.Add("@LogosId", MySqlDbType.Int32);
            SetLogosCommand.Prepare();
        }

        public static List<int> GetLogos(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var playerLogos = new List<int>();

                GetLogosCommand.Parameters["@CharacterId"].Value = characterId;
                using (var reader = GetLogosCommand.ExecuteReader())
                    while (reader.Read())
                        playerLogos.Add(reader.GetInt32("logosId"));

                return playerLogos;
            }
        }

        public static void SetLogos(uint characterId, int logosId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetLogosCommand.Parameters["@CharacterId"].Value = characterId;
                SetLogosCommand.Parameters["@LogosId"].Value = logosId;
                SetLogosCommand.ExecuteNonQuery();
            }
        }
    }
}
