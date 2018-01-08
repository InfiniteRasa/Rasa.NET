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
            foreach (var entry in client.MapClient.Player.MissionLog)
            {
                var mission = entry.Value;
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

        public Mission GetByIndex(int missionIndex)
        {
            var missionEnv = new MissionEnv();
            if (missionIndex >= missionEnv.MissionCount)
                return null;

            return missionEnv.Missions[missionIndex];
        }

        public bool IsCreatureMissionDispenser(Mission mission, Creature creatureOrNPC)
        {
            var scriptLineTo = mission.StateMapping[1];
            for (var i = 0; i < scriptLineTo; i++)
            {
                var scriptline = mission.ScriptLines[i];

                if (scriptline.Command == MissionScriptCommand.Dispenser)
                    if (creatureOrNPC.CreatureType.DbId == scriptline.Value1)
                        return true;
            }

            return false;
        }

        public bool IsCompletedByPlayer(Client client, int missionIndex)
        {
            if (client.MapClient.Player.MissionLog.ContainsKey(missionIndex))
                if (client.MapClient.Player.MissionLog[missionIndex].State == 2)
                    return true;

            return false;
        }
    }
}
