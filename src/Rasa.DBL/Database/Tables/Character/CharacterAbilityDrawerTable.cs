using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterAbilityDrawerTable
    {
        public static readonly MySqlCommand GetCharacterAbilitiesCommand = new MySqlCommand("SELECT abilitySlotId, abilityId, abilityLevel FROM character_abilitydrawer WHERE characterId = @CharacterId");
        public static readonly MySqlCommand SetCharacterAbilityCommand = new MySqlCommand("INSERT INTO character_abilitydrawer (characterId, abilitySlotId, abilityId, abilityLevel) VALUES (@CharacterId, @AbilitySlotId, @AbilityId, @AbilityLevel)");
        public static readonly MySqlCommand UpdateCharacterAbilityCommand = new MySqlCommand("UPDATE character_abilitydrawer SET abilityId = @AbilityId, abilityLevel = @AbilityLevel WHERE characterId = @CharacterId AND abilitySlotId = @AbilitySlotId");

        public static void Initialize()
        {
            GetCharacterAbilitiesCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterAbilitiesCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetCharacterAbilitiesCommand.Prepare();

            SetCharacterAbilityCommand.Connection = GameDatabaseAccess.CharConnection;
            SetCharacterAbilityCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilitySlotId", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilityLevel", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Prepare();

            UpdateCharacterAbilityCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterAbilityCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilitySlotId", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilityLevel", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Prepare();
        }

        public static List<int> GetCharacterAbilities(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterAbilitiesCommand.Parameters["@CharacterId"].Value = characterId;
                var characterAbilities = new List<int>();
                using (var reader = GetCharacterAbilitiesCommand.ExecuteReader())
                    while (reader.Read())
                    {
                        characterAbilities.Add(reader.GetInt32("abilitySlotId"));
                        characterAbilities.Add(reader.GetInt32("abilityId"));
                        characterAbilities.Add(reader.GetInt32("abilityLevel"));
                    }
                return characterAbilities;
            }
        }

        public static void SetCharacterAbility(uint characterId, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetCharacterAbilityCommand.Parameters["@CharacterId"].Value = characterId;
                SetCharacterAbilityCommand.Parameters["@AbilitySlotId"].Value = abilitySlotId;
                SetCharacterAbilityCommand.Parameters["@AbilityId"].Value = abilityId;
                SetCharacterAbilityCommand.Parameters["@AbilityLevel"].Value = abilityLevel;
                SetCharacterAbilityCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterAbility(uint characterId, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterAbilityCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateCharacterAbilityCommand.Parameters["@AbilitySlotId"].Value = abilitySlotId;
                UpdateCharacterAbilityCommand.Parameters["@AbilityId"].Value = abilityId;
                UpdateCharacterAbilityCommand.Parameters["@AbilityLevel"].Value = abilityLevel;
                UpdateCharacterAbilityCommand.ExecuteNonQuery();
            }
        }
    }
}
