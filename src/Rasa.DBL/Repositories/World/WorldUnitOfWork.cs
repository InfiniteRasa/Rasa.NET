using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.World
{
    using Context.World;
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public WorldUnitOfWork(WorldContext dbContext,
            IEquipmentRepository equipmentRepository,
            ICreatureRepository creatureRepository,
            IEntityClassRepository entityClassRepository,
            IFootlockerRepository footlockerRepository,
            ILogosRepository logosRepository,
            IMapInfoRepository mapInfoRepository,
            INpcMissionRepository npcMissionRepository,
            INpcMissionRewardRepository npcMissionRewardRepository,
            INpcPackageRepository npcPackageRepository,
            IPlayerRandomNameRepository randomNameRepository,
            ISpawnpoolRepository spawnpoolRepository,
            ITeleporterRepository teleporterRepository)
                : base(dbContext)
        {
            Equipment = equipmentRepository;
            Creatures = creatureRepository;
            EntityClasses = entityClassRepository;
            Footlockers = footlockerRepository;
            Logoses = logosRepository;
            MapInfos = mapInfoRepository;
            NpcMissions = npcMissionRepository;
            NpcMissionRewards = npcMissionRewardRepository;
            NpcPackages = npcPackageRepository;
            RandomNames = randomNameRepository;
            Spawnpools = spawnpoolRepository;
            Teleporters = teleporterRepository;
        }

        public IEquipmentRepository Equipment { get; }
        public ICreatureRepository Creatures { get; }
        public IEntityClassRepository EntityClasses { get; }
        public IFootlockerRepository Footlockers { get; }
        public ILogosRepository Logoses { get; }
        public IMapInfoRepository MapInfos { get; }
        public INpcMissionRepository NpcMissions { get; }
        public INpcMissionRewardRepository NpcMissionRewards { get; }
        public INpcPackageRepository NpcPackages { get; }
        public IPlayerRandomNameRepository RandomNames { get; }
        public ISpawnpoolRepository Spawnpools { get; }
        public ITeleporterRepository Teleporters { get; }
    }
}
