using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterAbilityDrawerTable
    {
        private static readonly MySqlCommand GetCharacterAbilitiesCommand = new MySqlCommand("SELECT abilitySlotId, abilityId, abilityLevel FROM character_abilitydrawer WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand SetCharacterAbilityCommand = new MySqlCommand("INSERT INTO character_abilitydrawer (accountId, characterSlot, abilitySlotId, abilityId, abilityLevel) VALUES (@AccountId, @CharacterSlot, @AbilitySlotId, @AbilityId, @AbilityLevel)");
        private static readonly MySqlCommand UpdateCharacterAbilityCommand = new MySqlCommand("UPDATE character_abilitydrawer SET abilityId = @AbilityId, abilityLevel = @AbilityLevel WHERE accountId = @AccountId AND characterSlot = @CharacterSlot AND abilitySlotId = @AbilitySlotId");

        public static void Initialize()
        {
            GetCharacterAbilitiesCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterAbilitiesCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterAbilitiesCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            GetCharacterAbilitiesCommand.Prepare();

            SetCharacterAbilityCommand.Connection = GameDatabaseAccess.CharConnection;
            SetCharacterAbilityCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            SetCharacterAbilityCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilitySlotId", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Parameters.Add("@AbilityLevel", MySqlDbType.Int32);
            SetCharacterAbilityCommand.Prepare();

            UpdateCharacterAbilityCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterAbilityCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCharacterAbilityCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilitySlotId", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Parameters.Add("@AbilityLevel", MySqlDbType.Int32);
            UpdateCharacterAbilityCommand.Prepare();
        }
        
        public static List<int> GetCharacterAbilities(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterAbilitiesCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterAbilitiesCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                var characterAbilities = new List<int>();
                using (var reader = GetCharacterAbilitiesCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        characterAbilities.Add(reader.GetInt32("abilitySlotId"));
                        characterAbilities.Add(reader.GetInt32("abilityId"));
                        characterAbilities.Add(reader.GetInt32("abilityLevel"));
                    }
                }

                return characterAbilities;
            }
        }

        public static void SetCharacterAbility(uint accountId, uint characterSlot, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetCharacterAbilityCommand.Parameters["@AccountId"].Value = accountId;
                SetCharacterAbilityCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                SetCharacterAbilityCommand.Parameters["@AbilitySlotId"].Value = abilitySlotId;
                SetCharacterAbilityCommand.Parameters["@AbilityId"].Value = abilityId;
                SetCharacterAbilityCommand.Parameters["@AbilityLevel"].Value = abilityLevel;
                SetCharacterAbilityCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterAbility(uint accountId, uint characterSlot, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterAbilityCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCharacterAbilityCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateCharacterAbilityCommand.Parameters["@AbilitySlotId"].Value = abilitySlotId;
                UpdateCharacterAbilityCommand.Parameters["@AbilityId"].Value = abilityId;
                UpdateCharacterAbilityCommand.Parameters["@AbilityLevel"].Value = abilityLevel;
                UpdateCharacterAbilityCommand.ExecuteNonQuery();
            }
        }
        
    }
}
