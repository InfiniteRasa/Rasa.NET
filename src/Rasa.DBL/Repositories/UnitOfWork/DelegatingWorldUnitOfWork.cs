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

        public IArmorClassRepository ArmorClasses => _parent.ArmorClasses;

        public ICreatureRepository Creatures => _parent.Creatures;

        public ICreatureActionRepository CreatureActions => _parent.CreatureActions;

        public ICreatureAppearanceRepository CreatureAppearances => _parent.CreatureAppearances;

        public ICreatureStatRepository CreatureStats => _parent.CreatureStats;

        public IEntityClassRepository EntityClasses => _parent.EntityClasses;

        public IEquipableClassRepository EquipableClasses => _parent.EquipableClasses;

        public IFootlockerRepository Footlockers => _parent.Footlockers;

        public IItemClassRepository ItemClasses => _parent.ItemClasses;

        public IItemTemplateArmorRepository ItemTemplateArmors => _parent.ItemTemplateArmors;

        public IItemTemplateItemClassRepository ItemTemplateItemClasses => _parent.ItemTemplateItemClasses;

        public IItemTemplateRepository ItemTemplates => _parent.ItemTemplates;

        public IItemTemplateRequirementRepository ItemTemplateRequirements => _parent.ItemTemplateRequirements;

        public IItemTemplateRequirementRaceRepository ItemTemplateRequirementRaces => _parent.ItemTemplateRequirementRaces;

        public IItemTemplateRequirementSkillRepository ItemTemplateRequirementSkills => _parent.ItemTemplateRequirementSkills;

        public IItemTemplateResistanceRepository ItemTemplateResistances => _parent.ItemTemplateResistances;

        public IItemTemplateWeaponRepository ItemTemplateWeapons => _parent.ItemTemplateWeapons;

        public ILogosRepository Logoses => _parent.Logoses;

        public IMapInfoRepository MapInfos => _parent.MapInfos;

        public INpcMissionRepository NpcMissions => _parent.NpcMissions;

        public INpcMissionRewardRepository NpcMissionRewards => _parent.NpcMissionRewards;

        public INpcPackageRepository NpcPackages => _parent.NpcPackages;

        public IPlayerRandomNameRepository RandomNames => _parent.RandomNames;

        public ISpawnpoolRepository Spawnpools => _parent.Spawnpools;

        public ITeleporterRepository Teleporters => _parent.Teleporters;

        public IVendorItemRepository VendorItems => _parent.VendorItems;

        public IVendorReposiotry Vendors => _parent.Vendors;

        public IWeaponClassRepository WeaponClasses => _parent.WeaponClasses;
    }
}