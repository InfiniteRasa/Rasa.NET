using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterAppearanceTable
    {
        public static Dictionary<uint, CharacterAppearanceEntry> GetAppearances(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.CharacterAppearance.Where(ca => ca.CharacterId == characterId)
                    .ToDictionary(charAppearance => charAppearance.Slot);
            }
        }

        public static bool AddAppearance(uint characterId, CharacterAppearanceEntry entry)
        {
            try
            {
                lock (GameDatabaseAccess.CharLock)
                {
                    GameDatabaseAccess.CharConnection.CharacterAppearance.Add(new CharacterAppearanceEntry(
                        characterId,
                        entry.Slot,
                        entry.Class,
                        entry.Color
                    ));
                    GameDatabaseAccess.CharConnection.SaveChanges();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static void DeleteCharacterAppearances(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterAppearance = GameDatabaseAccess.CharConnection.CharacterAppearance.First(ca => ca.CharacterId == characterId);
                GameDatabaseAccess.CharConnection.Remove(characterAppearance);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterAppearance(uint characterId, uint slot, uint classId, uint color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterAppearance =
                    GameDatabaseAccess.CharConnection.CharacterAppearance.First(ca =>
                        ca.CharacterId == characterId && ca.Slot == slot);
                characterAppearance.Class = classId;
                characterAppearance.Color = color;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}