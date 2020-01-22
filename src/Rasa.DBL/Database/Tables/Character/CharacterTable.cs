using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand("INSERT INTO `character` (`account_id`, `slot`, `name`, `race`, `class`, `scale`, `gender`, `experience`, `level`, `body`, `mind`, `spirit`, `map_context_id`, `coord_x`, `coord_y`, `coord_z`, `rotation`) VALUES (@AccountId, @Slot, @Name, @Race, @Class, @Scale, @Gender, @Experience, @Level, @Body, @Mind, @Spirit, @MapContextId, @CoordX, @CoordY, @CoordZ, @Rotation)");
        private static readonly MySqlCommand ListCharactersCommand = new MySqlCommand("SELECT * FROM `character` WHERE `account_id` = @AccountId");
        private static readonly MySqlCommand GetCharacterCommand = new MySqlCommand("SELECT * FROM `character` WHERE `id` = @Id");
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand("DELETE FROM `character` WHERE `id` = @Id");


        public static void Initialize()
        {
            CreateCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Slot", MySqlDbType.UByte);
            CreateCharacterCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            CreateCharacterCommand.Parameters.Add("@Race", MySqlDbType.UByte);
            CreateCharacterCommand.Parameters.Add("@Class", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Scale", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@Gender", MySqlDbType.Bit);
            CreateCharacterCommand.Parameters.Add("@Experience", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Level", MySqlDbType.UByte);
            CreateCharacterCommand.Parameters.Add("@Body", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Mind", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Spirit", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@MapContextId", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@CoordX", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@CoordY", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@CoordZ", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@Rotation", MySqlDbType.Double);
            CreateCharacterCommand.Prepare();

            ListCharactersCommand.Connection = GameDatabaseAccess.CharConnection;
            ListCharactersCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            ListCharactersCommand.Prepare();

            GetCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            GetCharacterCommand.Prepare();

            DeleteCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            DeleteCharacterCommand.Prepare();
        }

        public static bool CreateCharacter(CharacterEntry entry)
        {
            try
            {
                lock (GameDatabaseAccess.CharLock)
                {
                    CreateCharacterCommand.Parameters["@AccountId"].Value = entry.AccountId;
                    CreateCharacterCommand.Parameters["@Slot"].Value = entry.Slot;
                    CreateCharacterCommand.Parameters["@Name"].Value = entry.Name;
                    CreateCharacterCommand.Parameters["@Race"].Value = entry.Race;
                    CreateCharacterCommand.Parameters["@Class"].Value = entry.Class;
                    CreateCharacterCommand.Parameters["@Scale"].Value = entry.Scale;
                    CreateCharacterCommand.Parameters["@Gender"].Value = entry.Gender;
                    CreateCharacterCommand.Parameters["@Experience"].Value = entry.Experience;
                    CreateCharacterCommand.Parameters["@Level"].Value = entry.Level;
                    CreateCharacterCommand.Parameters["@Body"].Value = entry.Body;
                    CreateCharacterCommand.Parameters["@Mind"].Value = entry.Mind;
                    CreateCharacterCommand.Parameters["@Spirit"].Value = entry.Spirit;
                    CreateCharacterCommand.Parameters["@MapContextId"].Value = entry.ContextId;
                    CreateCharacterCommand.Parameters["@CoordX"].Value = entry.CoordX;
                    CreateCharacterCommand.Parameters["@CoordY"].Value = entry.CoordY;
                    CreateCharacterCommand.Parameters["@CoordZ"].Value = entry.CoordZ;
                    CreateCharacterCommand.Parameters["@Rotation"].Value = entry.Rotation;
                    CreateCharacterCommand.ExecuteNonQuery();

                    entry.Id = (uint) CreateCharacterCommand.LastInsertedId;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static Dictionary<byte, CharacterEntry> ListCharactersBySlot(uint accountId)
        {
            var dict = new Dictionary<byte, CharacterEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                ListCharactersCommand.Parameters["@AccountId"].Value = accountId;

                using (var reader = ListCharactersCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var charEntry = CharacterEntry.Read(reader, false);

                        dict.Add(charEntry.Slot, charEntry);
                    }
                }
            }

            return dict;
        }

        public static CharacterEntry GetCharacter(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterCommand.Parameters["@Id"].Value = characterId;

                using (var reader = GetCharacterCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static void DeleteCharacter(uint characterId)
        {
            CharacterAppearanceTable.DeleteCharacterAppearances(characterId);

            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterCommand.Parameters["@Id"].Value = characterId;
                DeleteCharacterCommand.ExecuteNonQuery();
            }
        }
    }
}
