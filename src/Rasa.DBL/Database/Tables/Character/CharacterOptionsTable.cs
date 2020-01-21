using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterOptionsTable
    {
        public static void AddCharacterOption(IEnumerable<CharacterOptionsEntry> characterOptions)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterOptions.AddRange(characterOptions);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void DeleteCharacterOptions(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterOptions = GameDatabaseAccess.CharConnection.CharacterOptions.Where(charOption =>
                    charOption.CharacterId == characterId);
                GameDatabaseAccess.CharConnection.RemoveRange(characterOptions);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static List<UserOptionsEntry> GetCharacterOptions(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return (from characterOptions in GameDatabaseAccess.CharConnection.CharacterOptions
                    where characterOptions.CharacterId == characterId
                    select new UserOptionsEntry
                    {
                        AccountId = characterOptions.CharacterId,
                        OptionId = (uint)characterOptions.OptionId,
                        Value = characterOptions.Value
                    }).ToList();
            }
        }

    }
}
