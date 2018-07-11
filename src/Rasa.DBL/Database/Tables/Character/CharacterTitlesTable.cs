using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterTitlesTable
    {
        private static readonly MySqlCommand CharacterTitles = new MySqlCommand("SELECT titleId FROM character_titles WHERE accountId = @AccountId AND characterSlot = @CharacterSlot;");

        public static void Initialize()
        {
            CharacterTitles.Connection = GameDatabaseAccess.CharConnection;
            CharacterTitles.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            CharacterTitles.Parameters.Add("@CharacterSlot", MySqlDbType.Byte);
            CharacterTitles.Prepare();
        }

        public static List<uint> GetCharacterTitles(uint accountId, byte characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CharacterTitles.Parameters["@AccountId"].Value = accountId;
                CharacterTitles.Parameters["@CharacterSlot"].Value = characterSlot;

                var titles = new List<uint>();

                using (var reader = CharacterTitles.ExecuteReader())
                    while (reader.Read())
                        titles.Add(reader.GetUInt32("titleId"));

                return titles;
            }
        }
    }
}