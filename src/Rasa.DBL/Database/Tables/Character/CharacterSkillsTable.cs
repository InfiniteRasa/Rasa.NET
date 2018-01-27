using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterSkillsTable
    {
        public static readonly MySqlCommand GetCharacterSkillsCommand = new MySqlCommand("SELECT skillId, abilityId, skillLevel FROM character_skills WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        public static readonly MySqlCommand SetCharacterSkillCommand = new MySqlCommand("INSERT INTO character_skills (accountId, characterSlot, skillId, abilityId, skillLevel) VALUES (@AccountId, @CharacterSlot, @SkillId, @AbilityId, @SkillLevel)");
        public static readonly MySqlCommand UpdateCharacterSkillCommand = new MySqlCommand("UPDATE character_skills SET skillLevel = @SkillLevel WHERE accountId = @AccountId AND characterSlot = @CharacterSlot AND skillId = @SkillId");

        public static void Initialize()
        {
            GetCharacterSkillsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterSkillsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterSkillsCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            GetCharacterSkillsCommand.Prepare();

            SetCharacterSkillCommand.Connection = GameDatabaseAccess.CharConnection;
            SetCharacterSkillCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            SetCharacterSkillCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            SetCharacterSkillCommand.Parameters.Add("@SkillId", MySqlDbType.Int32);
            SetCharacterSkillCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            SetCharacterSkillCommand.Parameters.Add("@SkillLevel", MySqlDbType.Int32);
            SetCharacterSkillCommand.Prepare();

            UpdateCharacterSkillCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterSkillCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCharacterSkillCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateCharacterSkillCommand.Parameters.Add("@SkillId", MySqlDbType.Int32);
            UpdateCharacterSkillCommand.Parameters.Add("@SkillLevel", MySqlDbType.Int32);
            UpdateCharacterSkillCommand.Prepare();
        }

        public static List<CharacterSkillsEntry> GetCharacterSkills(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterSkillsCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterSkillsCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                var characterSkills = new List<CharacterSkillsEntry>();

                using (var reader = GetCharacterSkillsCommand.ExecuteReader())
                    while (reader.Read())
                        characterSkills.Add(CharacterSkillsEntry.Read(reader));

                return characterSkills;
            }
        }

        public static void SetCharacterSkill(uint accoutnId, uint characterSlot, int skillId, int abilityId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetCharacterSkillCommand.Parameters["@AccountId"].Value = accoutnId;
                SetCharacterSkillCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                SetCharacterSkillCommand.Parameters["@SkillId"].Value = skillId;
                SetCharacterSkillCommand.Parameters["@AbilityId"].Value = abilityId;
                SetCharacterSkillCommand.Parameters["@SkillLevel"].Value = skillLevel;
                SetCharacterSkillCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterSkill(uint accountId, uint characterSlot, int skillId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterSkillCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCharacterSkillCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateCharacterSkillCommand.Parameters["@SkillId"].Value = skillId;
                UpdateCharacterSkillCommand.Parameters["@SkillLevel"].Value = skillLevel;
                UpdateCharacterSkillCommand.ExecuteNonQuery();
            }
        }
    }
}
