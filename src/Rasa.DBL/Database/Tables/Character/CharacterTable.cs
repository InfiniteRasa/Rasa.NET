using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterTable
    {
        public static bool CreateCharacter(CharacterEntry entry)
        {
            try
            {
                lock (GameDatabaseAccess.CharLock)
                {
                    GameDatabaseAccess.CharConnection.Character.Add(entry);
                    GameDatabaseAccess.CharConnection.SaveChanges();
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
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.Character.Where(character => character.AccountId == accountId)
                    .ToDictionary(character => character.Slot);
            }
        }

        public static CharacterEntry GetCharacter(uint accountId, byte slot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.Character.First(character =>
                    character.AccountId == accountId && character.Slot == slot);
            }
        }

        public static void DeleteCharacter(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character =
                    GameDatabaseAccess.CharConnection.Character.First(@char => @char.Id == characterId);
                GameDatabaseAccess.CharConnection.Remove(character);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterPosition(uint characterId, float coordX, float coordY, float coordZ, float orientation, uint mapContextId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.CoordX = coordX;
                character.CoordY = coordY;
                character.CoordZ = coordZ;
                character.Orientation = orientation;
                character.MapContextId = mapContextId;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterActiveWeapon(uint characterId, byte activeWeapon)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.ActiveWeapon = activeWeapon;
            }
        }

        public static void UpdateCharacterAttributes(uint characterId, int body, int mind, int spirit)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.Body = body;
                character.Mind = mind;
                character.Spirit = spirit;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterCloneCredits(uint characterId, uint cloneCredits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.CloneCredits = cloneCredits;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterCredits(uint characterId, int credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.Credits = credits;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCharacterLogin(uint characterId, uint totalTimePlayed, uint numLogins)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var character = GameDatabaseAccess.CharConnection.Character.First(@char =>
                    @char.Id == characterId);
                character.TotalTimePlayed = totalTimePlayed;
                character.NumLogins = numLogins;
                character.LastLogin = DateTime.Now;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
