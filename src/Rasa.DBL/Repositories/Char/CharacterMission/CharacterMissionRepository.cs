using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Rasa.Repositories.Char.CharacterMission
{
    using Context.Char;
    using Structures.Char;

    public class CharacterMissionRepository : ICharacterMissionRepository
    {
        private readonly CharContext _charContext;
        public MissionRuntime.MissionRuntimeRepository Runtime { get; }

        public CharacterMissionRepository(CharContext charContext)
        {
            _charContext = charContext;
            Runtime = new MissionRuntime.MissionRuntimeRepository(charContext);
        }

        public IReadOnlyList<CharacterMissionEntry> Get(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionEntries);
            return query.Where(entry => entry.CharacterId == characterId).ToList();
        }

        public List<CharacterMissionEntry> Get(
            uint accountId,
            uint characterSlot)
        {
            var characterId = _charContext.CharacterEntries
                .Where(entry => entry.AccountId == accountId && entry.Slot == characterSlot)
                .Select(entry => (uint?)entry.Id)
                .SingleOrDefault();
            return characterId.HasValue
                ? Get(characterId.Value).ToList()
                : new List<CharacterMissionEntry>();
        }

        public int Count(uint characterId) =>
            _charContext.CharacterMissionEntries.Count(entry => entry.CharacterId == characterId &&
                entry.MissionState != 4);

        [CanBeNull]
        public CharacterMissionEntry GetByCharacterAndMission(uint characterId, uint missionId)
        {
            return _charContext.CharacterMissionEntries.SingleOrDefault(
                entry => entry.CharacterId == characterId && entry.MissionId == missionId);
        }

        public void Add(CharacterMissionEntry entry)
        {
            _charContext.CharacterMissionEntries.Add(entry);
            _charContext.SaveChanges();
        }

        public void SetCompletable(uint characterId, uint missionId, bool value)
        {
            var mission = GetByCharacterAndMission(characterId, missionId);
            mission.Completeable = value;
            _charContext.SaveChanges();
        }

        public void SetState(uint characterId, uint missionId, uint state)
        {
            var mission = GetByCharacterAndMission(characterId, missionId);
            mission.MissionState = state;
            _charContext.SaveChanges();
        }

        public void Remove(uint characterId, uint missionId)
        {
            var mission = GetByCharacterAndMission(characterId, missionId);
            if (mission == null)
                return;

            if (mission.MissionState is 1 or 2 or 4)
                Runtime.Archive(mission, System.DateTime.UtcNow);
            _charContext.CharacterMissionEntries.Remove(mission);
            _charContext.SaveChanges();
        }

        public void RemoveAll(uint characterId)
        {
            var missions = _charContext.CharacterMissionEntries
                .Where(entry => entry.CharacterId == characterId);
            _charContext.CharacterMissionEntries.RemoveRange(missions);
        }
    }
}
