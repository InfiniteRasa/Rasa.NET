using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Structures;

    public class MissionManager
    {
        private static MissionManager _instance;
        private static readonly object InstanceLock = new object();
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
                            _instance = new MissionManager();
                    }
                }

                return _instance;
            }
        }

        private MissionManager()
        {
        }

        public void LoadMissions()
        {
            var missions = NpcMissionsTable.GetNpcMissions();

            foreach (var line in missions)
            {
                if (!LoadedMissions.ContainsKey(line.MissionId))
                    LoadedMissions.Add(line.MissionId, new Mission(line.MissionId));

                switch ((MissionScriptCommand)line.Command)
                {
                    case MissionScriptCommand.Dispenser:
                        LoadedMissions[line.MissionId].MissionGiver = line.Var1;
                        break;
                    case MissionScriptCommand.Collector:
                        LoadedMissions[line.MissionId].MissionReciver = line.Var1;
                        break;
                    case MissionScriptCommand.RewardItem:
                        {
                            var itemTemplate = ItemManager.Instance.GetItemTemplateById((uint)line.Var1);

                            var rewardItem = new RewardItem
                            {
                                ItemTemplateId = (uint)line.Var1,
                                Class = itemTemplate.Class,
                                Quantity = line.Var2,
                                ModuleIds = itemTemplate.ModuleIds,
                                QualityId = itemTemplate.QualityId
                            };

                            LoadedMissions[line.MissionId].MissionInfo.MissionConstantData.RewardInfo.FixedReward.FixedItems.Add(rewardItem);
                            break;
                        }
                    default:
                        Logger.WriteLog(LogType.Error, $"LoadMissions: Unknown command {line.Command}");
                        break;
                }
            }
        }
    }
}
