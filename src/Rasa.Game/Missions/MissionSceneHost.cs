using System;

namespace Rasa.Managers
{
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    public interface IMissionSceneHost
    {
        bool TryExecute(Client client, uint missionId, uint scenarioId);
        bool TryExecuteFailureTransition(Client client, uint missionId, uint scenarioId);
        bool TryActivateSpawnGroup(Client client, uint missionId, uint spawnGroupId);
        void OnMissionAccepted(Client client, uint missionId);
        bool Tick(Client client);
        void TickMap(MapChannel map);
        void Rebuild(uint characterId, MapChannel map);
        void Release(uint characterId, MapChannel map);
        void Detach(Client client, MapChannel map);
        void Resume(Client client);
    }

    internal sealed class MissionSceneHost : IMissionSceneHost
    {
        private readonly Func<MissionApplication> _missions;

        internal MissionSceneHost(
            Func<IGameUnitOfWorkFactory> factory, Func<MissionApplication> missions, ManifestationManager manifestations,
            Func<MapChannelManager> maps = null, Func<CreatureManager> creatures = null,
            Func<DynamicObjectManager> objects = null, Func<CommunicatorManager> communicator = null,
            Func<DateTime> utcNow = null)
        {
            _missions = missions ?? (() => MissionApplication.Instance);
        }

        public bool TryExecute(Client client, uint missionId, uint scenarioId) =>
            _missions().Scenes.Execute(client, missionId, scenarioId);
        public bool TryExecuteFailureTransition(Client client, uint missionId, uint scenarioId) =>
            _missions().Scenes.Execute(client, missionId, scenarioId);
        public bool TryActivateSpawnGroup(Client client, uint missionId, uint spawnGroupId) =>
            _missions().Scenes.ExecuteNamed(client, missionId, $"spawn-group-{spawnGroupId}");
        public void OnMissionAccepted(Client client, uint missionId)
        {
            if (_missions().Scenes.Owns(missionId))
                _missions().Scenes.Execute(client, missionId, 0, started: true);
        }
        public bool Tick(Client client)
        {
            var map = client?.Player?.MapChannel;
            if (map == null)
                return false;
            var missions = _missions();
            var changed = missions.Scenes.Tick(map, Game.Missions.SceneTickScope.Scripts);
            missions.RetryCharacterFlagPublications(map);
            return changed;
        }
        public void TickMap(MapChannel map)
        {
            var missions = _missions();
            missions.Scenes.Tick(map);
            missions.RetryCharacterFlagPublications(map);
        }
        public void Rebuild(uint characterId, MapChannel map) => _missions().Scenes.Rebuild(characterId, map);
        public void Release(uint characterId, MapChannel map) => _missions().Scenes.Detach(characterId, map);
        public void Detach(Client client, MapChannel map) => _missions().Scenes.Detach(client, map);
        public void Resume(Client client) => _missions().Scenes.Resume(client);
        internal void RecordScenarioCreatureDeath(SpawnPool pool)
        {
            if (pool?.ScenarioKey != null && pool.SceneRunId == null)
                Logger.WriteLog(LogType.Error, $"Unbound legacy scenario actor {pool.ScenarioKey} cannot write scene state.");
        }
    }
}
