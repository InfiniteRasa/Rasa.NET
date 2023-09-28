namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public interface IWorldUnitOfWork : IUnitOfWork
    {
        IEquipmentRepository Equipment { get; }
        ICreatureRepository Creatures { get; }
        IEntityClassRepository EntityClasses { get; }
        IFootlockerRepository Footlockers { get; }
        ILogosRepository Logoses { get; }
        IMapInfoRepository MapInfos { get; }
        INpcMissionRepository NpcMissions { get; }
        INpcMissionRewardRepository NpcMissionRewards { get; }
        INpcPackageRepository NpcPackages { get; }
        IPlayerRandomNameRepository RandomNames { get; }
        ISpawnpoolRepository Spawnpools { get; }
        ITeleporterRepository Teleporters { get; }
    }
}
