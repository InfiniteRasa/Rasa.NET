using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterSkillsTable
    {
        public static readonly MySqlCommand GetCharacterSkillsCommand = new MySqlCommand("SELECT skillId, abilityId, skillLevel FROM character_skills WHERE characterId = @CharacterId");
        public static readonly MySqlCommand SetCharacterSkillCommand = new MySqlCommand("INSERT INTO character_skills (characterId, skillId, abilityId, skillLevel) VALUES (@CharacterId, @SkillId, @AbilityId, @SkillLevel)");
        public static readonly MySqlCommand UpdateCharacterSkillCommand = new MySqlCommand("UPDATE character_skills SET skillLevel = @SkillLevel WHERE characterId = @CharacterId AND skillId = @SkillId");

        public static void Initialize()
        {
            GetCharacterSkillsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterSkillsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetCharacterSkillsCommand.Prepare();

            SetCharacterSkillCommand.Connection = GameDatabaseAccess.CharConnection;
            SetCharacterSkillCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            SetCharacterSkillCommand.Parameters.Add("@SkillId", MySqlDbType.Int32);
            SetCharacterSkillCommand.Parameters.Add("@AbilityId", MySqlDbType.Int32);
            SetCharacterSkillCommand.Parameters.Add("@SkillLevel", MySqlDbType.Int32);
            SetCharacterSkillCommand.Prepare();

            UpdateCharacterSkillCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterSkillCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateCharacterSkillCommand.Parameters.Add("@SkillId", MySqlDbType.Int32);
            UpdateCharacterSkillCommand.Parameters.Add("@SkillLevel", MySqlDbType.Int32);
            UpdateCharacterSkillCommand.Prepare();
        }

        public static List<CharacterSkillsEntry> GetCharacterSkills(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterSkillsCommand.Parameters["@CharacterId"].Value = characterId;

                var characterSkills = new List<CharacterSkillsEntry>();

                using (var reader = GetCharacterSkillsCommand.ExecuteReader())
                    while (reader.Read())
                        characterSkills.Add(CharacterSkillsEntry.Read(reader));

                return characterSkills;
            }
        }

        public static void SetCharacterSkill(uint characterId, int skillId, int abilityId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetCharacterSkillCommand.Parameters["@CharacterId"].Value = characterId;
                SetCharacterSkillCommand.Parameters["@SkillId"].Value = skillId;
                SetCharacterSkillCommand.Parameters["@AbilityId"].Value = abilityId;
                SetCharacterSkillCommand.Parameters["@SkillLevel"].Value = skillLevel;
                SetCharacterSkillCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterSkill(uint characterId, int skillId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterSkillCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateCharacterSkillCommand.Parameters["@SkillId"].Value = skillId;
                UpdateCharacterSkillCommand.Parameters["@SkillLevel"].Value = skillLevel ;
                UpdateCharacterSkillCommand.ExecuteNonQuery();
            }
        }
    }
}
