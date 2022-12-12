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
            SetLogosCommand.Parameters.Add("@LogosId", MySqlDbType.UInt32);
            SetLogosCommand.Prepare();
        }

        public static List<uint> GetLogos(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var playerLogos = new List<uint>();

                GetLogosCommand.Parameters["@AccountId"].Value = accountId;
                GetLogosCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                using (var reader = GetLogosCommand.ExecuteReader())
                    while (reader.Read())
                        playerLogos.Add(reader.GetUInt32("logosId"));

                return playerLogos;
            }
        }

        public static void SetLogos(uint accountId, uint characterSlot, uint logosId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetLogosCommand.Parameters["@AccountId"].Value = accountId;
                SetLogosCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                SetLogosCommand.Parameters["@LogosId"].Value = logosId;
                SetLogosCommand.ExecuteNonQuery();
            }
        }
    }
}
