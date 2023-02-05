using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    public class MissionManager
    {
        /*      Mission Packets:
         * - MissionStatusInfo(self, missionStatusDict)
         * - MissionCompleteable(self, missionId, bCompleteable)
         * - MissionCompleted(self, missionId)
         * - MissionRewarded(self, missionId)
         * - MissionFailed(self, missionId)
         * - MissionDiscarded(self, missionId)
         * - MissionCleared(self, missionId)
         * - MissionGained(self, missionId, missionInfo)
         * - ObjectiveRevealed(self, missionId, objectiveId, missionInfo)
         * - ObjectiveActivated(self, missionId, objectiveId)
         * - ObjectiveCompleted(self, missionId, objectiveId)
         * - ObjectiveFailed(self, missionId, objectiveId)
         * - UpdateObjectiveCounter(self, missionId, objectiveId, counterId, counterVal, initialVal, targetVal)
         * - UpdateObjectiveItemCounter(self, missionId, objectiveId, itemClassId, counterVal, targetVal)
         * - DispenseSharedMission(self, actorId, missionId, missionInfo)
         * - DispenseRadioMission(self, missionId, missionInfo, bForceDialog)
         * 
         */

        private static MissionManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public readonly Dictionary<uint, Mission> LoadedMissions = new Dictionary<uint, Mission>();

        public static MissionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MissionManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private MissionManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void LoadMissions()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var missions = unitOfWork.NpcMissions.Get();

            foreach (var mission in missions)
            {
                LoadedMissions.Add(mission.Id, new Mission(mission));

            }
        }
    }
}
