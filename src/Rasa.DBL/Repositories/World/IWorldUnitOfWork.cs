namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public interface IWorldUnitOfWork : IUnitOfWork
    {
        IActionRepository Actions { get; }
        IEquipmentRepository Equipment { get; }
        ICreatureRepository Creatures { get; }
        IEntityClassRepository EntityClasses { get; }
        IFootlockerRepository Footlockers { get; }
        ILogosRepository Logoses { get; }
        IMapInfoRepository MapInfos { get; }
        IMapLinkRepository MapLinks { get; }
        IKraftwerksRepository Kraftwerks { get; }
        IMapRegionRepository MapRegions { get; }
        IMapMarkerRepository MapMarkers { get; }
        IRecipeRepository Recipes { get; }
        INpcMissionRepository NpcMissions { get; }
        INpcMissionRewardRepository NpcMissionRewards { get; }
        IMissionContentRepository MissionContent { get; }
        INpcPackageRepository NpcPackages { get; }
        IPlayerRandomNameRepository RandomNames { get; }
        ISpawnpoolRepository Spawnpools { get; }
        ITeleporterRepository Teleporters { get; }
    }
}
