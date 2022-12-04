using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using System;
    using Structures;

    public class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand("INSERT INTO `character` (`account_id`, `slot`, `name`, `race`, `class`, `scale`, `gender`, `experience`, `level`, `body`, `mind`, `spirit`, `map_context_id`, `coord_x`, `coord_y`, `coord_z`, `orientation`) VALUES (@AccountId, @Slot, @Name, @Race, @Class, @Scale, @Gender, @Experience, @Level, @Body, @Mind, @Spirit, @MapContextId, @CoordX, @CoordY, @CoordZ, @Orientation)");
        private static readonly MySqlCommand GetCharacterCommand = new MySqlCommand("SELECT * FROM `character` WHERE `account_id` = @AccountId AND slot = @Slot");
        private static readonly MySqlCommand GetCharacterByNameCommand = new MySqlCommand("SELECT * FROM `character` WHERE `account_id` = @AccountId AND name = @Name");
        private static readonly MySqlCommand GetCharacterByIdCommand = new MySqlCommand("SELECT * FROM `character` WHERE `id` = @CharacterId");
        private static readonly MySqlCommand GetCharactersByAccountIdCommand = new MySqlCommand("SELECT * FROM `character` WHERE `account_id` = @AccountId");
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand("DELETE FROM `character` WHERE `id` = @Id");
        private static readonly MySqlCommand ListCharactersCommand = new MySqlCommand("SELECT * FROM `character` WHERE `account_id` = @AccountId");
        private static readonly MySqlCommand UpdateCharacterAttributesCommand = new MySqlCommand("UPDATE `character` SET `body` = @Body, `mind` = @Mind, `spirit` = @Spirit WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterActiveWeaponCommand = new MySqlCommand("UPDATE `character` SET `active_weapon` = @ActiveWeapon WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterCloneCreditsCommand = new MySqlCommand("UPDATE `character` SET `clone_credits` = @CloneCredits WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterCreditsCommand = new MySqlCommand("UPDATE `character` SET `credits` = @Credits WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterExperienceCommand = new MySqlCommand("UPDATE `character` SET `experience` = @Experience WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterLevelCommand = new MySqlCommand("UPDATE `character` SET `level` = @Level WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterLocationCommand = new MySqlCommand("UPDATE `character` SET `map_context_id` = @MapContextId, `coord_x` = @CoordX, `coord_y` = @CoordY, `coord_z` = @CoordZ, `orientation` = @Orientation WHERE `id` = @Id");
        private static readonly MySqlCommand UpdateCharacterLoginCommand = new MySqlCommand("UPDATE `character` SET `num_logins` = @NumLogins, `last_login` = NOW(), `total_time_played` = @TotalTimePlayed WHERE `id` = @Id");
        
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
            CreateCharacterCommand.Parameters.Add("@CoordX", MySqlDbType.Float);
            CreateCharacterCommand.Parameters.Add("@CoordY", MySqlDbType.Float);
            CreateCharacterCommand.Parameters.Add("@CoordZ", MySqlDbType.Float);
            CreateCharacterCommand.Parameters.Add("@Orientation", MySqlDbType.Float);
            CreateCharacterCommand.Prepare();

            ListCharactersCommand.Connection = GameDatabaseAccess.CharConnection;
            ListCharactersCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            ListCharactersCommand.Prepare();

            GetCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterCommand.Parameters.Add("@Slot", MySqlDbType.UInt32);
            GetCharacterCommand.Prepare();

            GetCharacterByNameCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterByNameCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterByNameCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            GetCharacterByNameCommand.Prepare();

            GetCharacterByIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterByIdCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);            
            GetCharacterByIdCommand.Prepare();

            GetCharactersByAccountIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharactersByAccountIdCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharactersByAccountIdCommand.Prepare();

            DeleteCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            DeleteCharacterCommand.Prepare();

            UpdateCharacterActiveWeaponCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterActiveWeaponCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterActiveWeaponCommand.Parameters.Add("@ActiveWeapon", MySqlDbType.UByte);
            UpdateCharacterActiveWeaponCommand.Prepare();

            UpdateCharacterAttributesCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterAttributesCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterAttributesCommand.Parameters.Add("@Body", MySqlDbType.Int32);
            UpdateCharacterAttributesCommand.Parameters.Add("@Mind", MySqlDbType.Int32);
            UpdateCharacterAttributesCommand.Parameters.Add("@Spirit", MySqlDbType.Int32);
            UpdateCharacterAttributesCommand.Prepare();

            UpdateCharacterCloneCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterCloneCreditsCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterCloneCreditsCommand.Parameters.Add("@CloneCredits", MySqlDbType.UInt32);
            UpdateCharacterCloneCreditsCommand.Prepare();

            UpdateCharacterCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterCreditsCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterCreditsCommand.Parameters.Add("@Credits", MySqlDbType.Int32);
            UpdateCharacterCreditsCommand.Prepare();

            UpdateCharacterExperienceCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterExperienceCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterExperienceCommand.Parameters.Add("@Experience", MySqlDbType.Int32);
            UpdateCharacterExperienceCommand.Prepare();

            UpdateCharacterLevelCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterLevelCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterLevelCommand.Parameters.Add("@Level", MySqlDbType.Int32);
            UpdateCharacterLevelCommand.Prepare();

            UpdateCharacterLocationCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterLocationCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterLocationCommand.Parameters.Add("@MapContextId", MySqlDbType.UInt32);
            UpdateCharacterLocationCommand.Parameters.Add("@CoordX", MySqlDbType.Float);
            UpdateCharacterLocationCommand.Parameters.Add("@CoordY", MySqlDbType.Float);
            UpdateCharacterLocationCommand.Parameters.Add("@CoordZ", MySqlDbType.Float);
            UpdateCharacterLocationCommand.Parameters.Add("@Orientation", MySqlDbType.Float);
            UpdateCharacterLocationCommand.Prepare();

            UpdateCharacterLoginCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterLoginCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Parameters.Add("@NumLogins", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Parameters.Add("@TotalTimePlayed", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Prepare();
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
                    CreateCharacterCommand.Parameters["@MapContextId"].Value = entry.MapContextId;
                    CreateCharacterCommand.Parameters["@CoordX"].Value = entry.CoordX;
                    CreateCharacterCommand.Parameters["@CoordY"].Value = entry.CoordY;
                    CreateCharacterCommand.Parameters["@CoordZ"].Value = entry.CoordZ;
                    CreateCharacterCommand.Parameters["@Orientation"].Value = entry.Orientation;
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

        public static CharacterEntry GetCharacter(uint accountId, byte slot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterCommand.Parameters["@Slot"].Value = slot;

                using (var reader = GetCharacterCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static CharacterEntry GetCharacterByName(uint accountId, string name)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterByNameCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterByNameCommand.Parameters["@Name"].Value = name;

                using (var reader = GetCharacterByNameCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static CharacterEntry GetCharacterById(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterByIdCommand.Parameters["@CharacterId"].Value = characterId;                

                using (var reader = GetCharacterByIdCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static List<CharacterEntry> GetCharactersByAccountId(uint accountId)
        {
            var characters = new List<CharacterEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                ListCharactersCommand.Parameters["@AccountId"].Value = accountId;

                using (var reader = ListCharactersCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var charEntry = CharacterEntry.Read(reader, false);

                        characters.Add(charEntry);
                    }
                }
            }

            return characters;
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

        public static void UpdateCharacterPosition(uint characterId, float coordX, float coordY, float coordZ, float orientation, uint mapContextId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterLocationCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterLocationCommand.Parameters["@CoordX"].Value = coordX;
                UpdateCharacterLocationCommand.Parameters["@CoordY"].Value = coordY;
                UpdateCharacterLocationCommand.Parameters["@CoordZ"].Value = coordZ;
                UpdateCharacterLocationCommand.Parameters["@Orientation"].Value = orientation;
                UpdateCharacterLocationCommand.Parameters["@MapContextId"].Value = mapContextId;
                UpdateCharacterLocationCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterActiveWeapon(uint characterId, byte activeWeapon)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterActiveWeaponCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterActiveWeaponCommand.Parameters["@ActiveWeapon"].Value = activeWeapon;
                UpdateCharacterActiveWeaponCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterAttributes(uint characterId, int body, int mind, int spirit)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterAttributesCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterAttributesCommand.Parameters["@Body"].Value = body;
                UpdateCharacterAttributesCommand.Parameters["@Mind"].Value = mind;
                UpdateCharacterAttributesCommand.Parameters["@Spirit"].Value = spirit;
                UpdateCharacterAttributesCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterCloneCredits(uint characterId, uint cloneCredits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterCloneCreditsCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterCloneCreditsCommand.Parameters["@CloneCredits"].Value = cloneCredits;
                UpdateCharacterCloneCreditsCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterCredits(uint characterId, int credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterCreditsCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterCreditsCommand.Parameters["@Credits"].Value = credits;
                UpdateCharacterCreditsCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterExpirience(uint characterId, int experience)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterExperienceCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterExperienceCommand.Parameters["@Experience"].Value = experience;
                UpdateCharacterExperienceCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterLevel(uint characterId, byte level)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterLevelCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterLevelCommand.Parameters["@Level"].Value = level;
                UpdateCharacterLevelCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterLogin(uint characterId, uint totalTimePlayed, uint numLogins)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterLoginCommand.Parameters["@Id"].Value = characterId;
                UpdateCharacterLoginCommand.Parameters["@TotalTimePlayed"].Value = totalTimePlayed;
                UpdateCharacterLoginCommand.Parameters["@NumLogins"].Value = numLogins;
                UpdateCharacterLoginCommand.ExecuteNonQuery();
            }
        }
    }
}
