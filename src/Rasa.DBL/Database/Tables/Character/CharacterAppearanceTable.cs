using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterAppearanceTable
    {
        private static readonly MySqlCommand GetCharacterAppearancesCommand = new MySqlCommand("SELECT * FROM `character_appearance` WHERE `character_id` = @CharacterId");
        private static readonly MySqlCommand AddCharacterAppearanceCommand = new MySqlCommand("INSERT INTO `character_appearance` (`character_id`, `slot`, `class`, `color`) VALUE (@CharacterId, @Slot, @Class, @Color)");
        private static readonly MySqlCommand DeleteCharacterAppearancesCommand = new MySqlCommand("DELETE FROM `character_appearance` WHERE `character_id` = @CharacterId");

        public static void Initialize()
        {
            GetCharacterAppearancesCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterAppearancesCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetCharacterAppearancesCommand.Prepare();

            AddCharacterAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            AddCharacterAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            AddCharacterAppearanceCommand.Parameters.Add("@Slot", MySqlDbType.UInt32);
            AddCharacterAppearanceCommand.Parameters.Add("@Class", MySqlDbType.UInt32);
            AddCharacterAppearanceCommand.Parameters.Add("@Color", MySqlDbType.UInt32);
            AddCharacterAppearanceCommand.Prepare();

            DeleteCharacterAppearancesCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterAppearancesCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            DeleteCharacterAppearancesCommand.Prepare();
        }

        public static Dictionary<uint, CharacterAppearanceEntry> GetAppearances(uint characterId)
        {
            var dict = new Dictionary<uint, CharacterAppearanceEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterAppearancesCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetCharacterAppearancesCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var appearanceEntry = CharacterAppearanceEntry.Read(reader, false);

                        dict.Add(appearanceEntry.Slot, appearanceEntry);
                    }
                }
            }

            return dict;
        }

        public static bool AddAppearance(uint characterId, CharacterAppearanceEntry entry)
        {
            try
            {
                lock (GameDatabaseAccess.CharLock)
                {
                    AddCharacterAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                    AddCharacterAppearanceCommand.Parameters["@Slot"].Value = entry.Slot;
                    AddCharacterAppearanceCommand.Parameters["@Class"].Value = entry.Class;
                    AddCharacterAppearanceCommand.Parameters["@Color"].Value = entry.Color;
                    AddCharacterAppearanceCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static void DeleteCharacterAppearances(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterAppearancesCommand.Parameters["@CharacterId"].Value = characterId;
                DeleteCharacterAppearancesCommand.ExecuteNonQuery();
            }
        }
    }
}