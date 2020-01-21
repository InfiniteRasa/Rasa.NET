using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterSkillsTable
    {
        public static List<CharacterSkillsEntry> GetCharacterSkills(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.CharacterSkills.Where(charSkill =>
                    charSkill.AccountId == accountId && charSkill.CharacterSlot == characterSlot).ToList();
            }
        }

        public static void SetCharacterSkill(uint accoutnId, uint characterSlot, int skillId, int abilityId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterSkills.Add(new CharacterSkillsEntry
                {
                    AccountId = accoutnId,
                    CharacterSlot = characterSlot,
                    SkillId = skillId,
                    AbilityId = abilityId,
                    SkillLevel = skillLevel
                });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterSkill(uint accountId, uint characterSlot, int skillId, int skillLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterSkill = GameDatabaseAccess.CharConnection.CharacterSkills.First(charSkill =>
                    charSkill.AccountId == accountId
                    && charSkill.CharacterSlot == characterSlot
                    && charSkill.SkillId == skillId);
                characterSkill.SkillLevel = skillLevel;
            }
        }
    }
}
