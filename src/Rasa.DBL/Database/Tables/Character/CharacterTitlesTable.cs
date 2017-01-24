using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterTitlesTable
    {
        private static readonly MySqlCommand CharacterTitles = new MySqlCommand("SELECT titleId FROM character_titles WHERE characterId = @CharacterId;");

        public static void Initialize()
        {
            CharacterTitles.Connection = GameDatabaseAccess.CharConnection;
            CharacterTitles.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            CharacterTitles.Prepare();
        }

        public static List<int> GetCharacterTitles(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CharacterTitles.Parameters["@CharacterId"].Value = characterId;
                var titles = new List<int>();
                using (var reader = CharacterTitles.ExecuteReader())
                    while (reader.Read())
                        titles.Add(reader.GetInt32("titleId"));
                return titles;
            }
        }
    }
}