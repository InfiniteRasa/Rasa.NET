using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterLogosTable
    {
        private static readonly MySqlCommand GetLogosCommand = new MySqlCommand("SELECT logosId FROM character_logos WHERE accountId = @AccountId And characterSlot = @CharacterSlot");
        private static readonly MySqlCommand SetLogosCommand = new MySqlCommand("INSERT INTO character_logos (accountId, characterSlot, logosId) VALUES (@AccountId, @CharacterSlot, @LogosId)");

        public static void Initialize()
        {
            GetLogosCommand.Connection = GameDatabaseAccess.CharConnection;
            GetLogosCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetLogosCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            GetLogosCommand.Prepare();

            SetLogosCommand.Connection = GameDatabaseAccess.CharConnection;
            SetLogosCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            SetLogosCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            SetLogosCommand.Parameters.Add("@LogosId", MySqlDbType.Int32);
            SetLogosCommand.Prepare();
        }

        public static List<int> GetLogos(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var playerLogos = new List<int>();

                GetLogosCommand.Parameters["@AccountId"].Value = accountId;
                GetLogosCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                using (var reader = GetLogosCommand.ExecuteReader())
                    while (reader.Read())
                        playerLogos.Add(reader.GetInt32("logosId"));

                return playerLogos;
            }
        }

        public static void SetLogos(uint accountId, uint characterSlot, int logosId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetLogosCommand.Parameters["@AccoutnId"].Value = accountId;
                SetLogosCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                SetLogosCommand.Parameters["@LogosId"].Value = logosId;
                SetLogosCommand.ExecuteNonQuery();
            }
        }
    }
}
