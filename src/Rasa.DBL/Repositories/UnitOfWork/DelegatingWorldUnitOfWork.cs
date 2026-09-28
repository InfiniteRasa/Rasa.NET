using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Repositories.UnitOfWork
{
    using World;

    public class DelegatingWorldUnitOfWork : DelegatingUnitOfWorkBase, IWorldUnitOfWork
    {
        private readonly IWorldUnitOfWork _parent;

        public DelegatingWorldUnitOfWork(IWorldUnitOfWork parent, IServiceScope scope) : base(parent, scope)
        {
            _parent = parent;
        }

        public IActionRepository Actions => _parent.Actions;

        public IEquipmentRepository Equipment => _parent.Equipment;

        public ICreatureRepository Creatures => _parent.Creatures;

        public IEntityClassRepository EntityClasses => _parent.EntityClasses;

        public IFootlockerRepository Footlockers => _parent.Footlockers;

        public ILogosRepository Logoses => _parent.Logoses;

        public IMapInfoRepository MapInfos => _parent.MapInfos;
        public IMapLinkRepository MapLinks => _parent.MapLinks;
        public IKraftwerksRepository Kraftwerks => _parent.Kraftwerks;
        public IMapRegionRepository MapRegions => _parent.MapRegions;
        public IMapMarkerRepository MapMarkers => _parent.MapMarkers;
        public IRecipeRepository Recipes => _parent.Recipes;

        public INpcMissionRepository NpcMissions => _parent.NpcMissions;

        public INpcMissionRewardRepository NpcMissionRewards => _parent.NpcMissionRewards;

        public IMissionContentRepository MissionContent => _parent.MissionContent;

        public INpcPackageRepository NpcPackages => _parent.NpcPackages;

        public IPlayerRandomNameRepository RandomNames => _parent.RandomNames;

        public ISpawnpoolRepository Spawnpools => _parent.Spawnpools;

        public ITeleporterRepository Teleporters => _parent.Teleporters;
    }
}