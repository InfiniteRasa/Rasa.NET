using System.Collections.Generic;

namespace Rasa.Repositories.Char.Character
{
    using Structures.Char;

    public interface ICharacterRepository
    {
        CharacterEntry Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender);
        CharacterEntry Get(uint id);

        /// <summary>Like Get(uint), but null when the character does not exist.</summary>
        CharacterEntry Find(uint id);

        IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId);
        CharacterEntry GetByAccountId(uint accountEntryId, byte slot);
        void Delete(uint id);
        void UpdateLoginData(uint id);
        void SaveCharacter(ICharacterChange characterChange);
        void UpdateCharacterAttributes(uint id, int spentBody, int spentMind, int spentSpirit);
        void UpdateCharacterClass(uint id, uint classId);
        void UpdateCharacterCloneCredits(uint id, uint cloneCredits);
        void UpdateCharacterCredits(uint id, int credits);
        void UpdateCharacterCurrencies(uint id, int credits, int prestige);
        void UpdateCharacterPrestige(uint id, int prestige);
        void UpdateCharacterExpirience(uint id, uint experience);
        void UpdateCharacterProgression(uint id, uint experience, byte level);
        void ReconcileBootcampCharacter(
            uint id,
            uint experience,
            byte level,
            uint classId,
            double x,
            double y,
            double z,
            double rotation,
            uint mapContextId);
        void UpdateCharacterLevel(uint id, byte level);
        void UpdateCharacterLogin(uint id, uint totalTimePlayed, uint numLogins);
        void UpdateCharacterPosition(uint id, double x, double y, double z, double rotation, uint mapContextId);
        void UpdateCharacterActiveWeapon(uint id, byte activeWeapon);
        void UpdateCharacterAbilitySlot(uint id, byte slot);
        void UpdateCharacterName(uint id, string name);

        /// <summary>Whether another character already has this name, matched case-insensitively.</summary>
        bool IsCharacterNameTaken(string name, uint exceptCharacterId);
    }
}