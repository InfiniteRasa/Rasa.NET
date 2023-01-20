using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    public class MissionManager
    {
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

            foreach (var line in missions)
            {
                if (!LoadedMissions.ContainsKey(line.Id))
                    LoadedMissions.Add(line.Id, new Mission(line.Id));

                switch ((MissionScriptCommand)line.Command)
                {
                    case MissionScriptCommand.Dispenser:
                        LoadedMissions[line.Id].MissionGiver = line.Var1;
                        break;
                    case MissionScriptCommand.Collector:
                        LoadedMissions[line.Id].MissionReciver = line.Var1;
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

                            LoadedMissions[line.Id].MissionInfo.MissionConstantData.RewardInfo.FixedReward.FixedItems.Add(rewardItem);
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
