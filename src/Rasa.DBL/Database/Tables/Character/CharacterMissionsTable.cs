using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterMissionsTable
    {
        public static void AddMission(uint accountId, int characterSlot, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterMissions.Add(new CharacterMissionsEntry
                {
                    AccountId = accountId,
                    CharacterSlot = characterSlot,
                    MissionId = missionId,
                    MissionState = missionState
                });
            }
        }

        public static List<CharacterMissionsEntry> GetMissions(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.CharacterMissions.Where(charMission =>
                    charMission.AccountId == accountId && charMission.CharacterSlot == characterSlot).ToList();
            }
        }

        public static void UpdateMission(uint accountId, uint characterSlot, int missionId, short missionState)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterMission = GameDatabaseAccess.CharConnection.CharacterMissions.First(charMission =>
                    charMission.AccountId == accountId
                    && charMission.CharacterSlot == characterSlot
                    && charMission.MissionId == missionId);
                characterMission.MissionState = missionState;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
