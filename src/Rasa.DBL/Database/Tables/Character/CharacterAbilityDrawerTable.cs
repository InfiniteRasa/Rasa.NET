using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterAbilityDrawerTable
    {
        public static List<int> GetCharacterAbilities(uint accountId, uint characterSlot)
        {
            var charAbilities = new List<int>();
            lock (GameDatabaseAccess.CharLock)
            {
                var abilities = (from charAbility in GameDatabaseAccess.CharConnection.CharacterAbilitydrawer
                        where charAbility.AccountId == accountId && charAbility.CharacterSlot == characterSlot
                        select new []
                        {
                            charAbility.AbilitySlotId,
                            charAbility.AbilityId,
                            charAbility.AbilityLevel
                        });

                foreach (var ab in abilities)
                {
                    charAbilities.Add(ab[0]);
                    charAbilities.Add(ab[1]);
                    charAbilities.Add(ab[2]);
                }
            }

            return charAbilities;
        }

        public static void SetCharacterAbility(uint accountId, uint characterSlot, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterAbilitydrawer.Add(
                    new Structures.CharacterAbilitydrawerEntry
                    {
                        AccountId = accountId,
                        CharacterSlot = characterSlot,
                        AbilitySlotId = abilitySlotId,
                        AbilityId = abilityId,
                        AbilityLevel = abilityLevel
                    });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterAbility(uint accountId, uint characterSlot, int abilitySlotId, int abilityId, int abilityLevel)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var charAbility = GameDatabaseAccess.CharConnection.CharacterAbilitydrawer.First(
                    ca => ca.AccountId == accountId
                          && ca.CharacterSlot == characterSlot
                          && ca.AbilitySlotId == abilitySlotId
                );
                charAbility.AbilityId = abilityId;
                charAbility.AbilityLevel = abilityLevel;

                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
        
    }
}
