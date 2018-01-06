using System.Collections.Generic;

namespace Rasa.Managers
{
    using System;
    using Data;
    using Game;
    using Structures;

    public class MissionManager
    {
        private static MissionManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly Dictionary<int, Mission> LoadedMissions = new Dictionary<int, Mission>();

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
                            _instance = new MissionManager();
                    }
                }

                return _instance;
            }
        }

        private MissionManager()
        {
        }

        public MissionLog FindPlayerMission(Client client, uint missionIndex)
        {
            // for now we do a simple straight forward search
            // todo: replace by binary search
            foreach (var mission in client.MapClient.Player.MissionLog)
            {
                if (mission.MissionIndex == missionIndex)
                    return mission;
            }

            return null;
        }

        public Mission GetById(int missionId)
        {
            foreach (var mission in LoadedMissions)
                if (mission.Key == missionId)
                    return mission.Value;

            return null;
        }
    }
}
