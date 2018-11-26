using System.Collections.Generic;

using MySql.Data.MySqlClient;


namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterOptionsTable
    {
        private static readonly MySqlCommand DeleteCharacterOptionsCommand = new MySqlCommand("DELETE FROM character_options WHERE character_id = @CharacterId");
        private static readonly MySqlCommand GetCharacterOptionsCommand = new MySqlCommand("SELECT * FROM character_options WHERE character_id = @CharacterId");

        public static void Initialize()
        {
            GetCharacterOptionsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterOptionsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetCharacterOptionsCommand.Prepare();

            DeleteCharacterOptionsCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterOptionsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            DeleteCharacterOptionsCommand.Prepare();
        }

        public static void AddCharacterOption(string value)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                MySqlCommand AddCharacterOptionsCommand = new MySqlCommand("INSERT INTO character_options (character_id, option_id, value) VALUES" + value)
                {
                    Connection = GameDatabaseAccess.CharConnection
                };
                AddCharacterOptionsCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteCharacterOptions(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterOptionsCommand.Parameters["@CharacterId"].Value = characterId;
                DeleteCharacterOptionsCommand.ExecuteNonQuery();
            }
        }

        public static List<UserOptionsEntry> GetCharacterOptions(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterOptionsCommand.Parameters["@CharacterId"].Value = characterId;

                var characterOptions = new List<UserOptionsEntry>();

                using (var reader = GetCharacterOptionsCommand.ExecuteReader())
                    while (reader.Read())
                        characterOptions.Add(UserOptionsEntry.Read(reader));

                return characterOptions;
            }
        }

    }
}
