using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.World
{
    using Context.World;
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public WorldUnitOfWork(WorldContext dbContext,
            IArmorClassRepository armorClassRepository,
            ICreatureRepository creatureRepository,
            ICreatureActionRepository creatureActionRepository,
            ICreatureAppearanceRepository creatureAppearanceRepository,
            ICreatureStatRepository creatureStatRepository,
            IEntityClassRepository entityClassRepository,
            IEquipableClassRepository equipableClassRepository,
            IFootlockerRepository footlockerRepository,
            IItemClassRepository itemClassRepository,
            IItemTemplateArmorRepository itemTemplateArmorRepository,
            IItemTemplateRepository itemTemplateRepository,
            IItemTemplateItemClassRepository itemTemplateItemClassRepository,
            IItemTemplateRequirementRepository itemTemplateRequirementRepository,
            IItemTemplateRequirementRaceRepository itemTemplateRequirementRaceRepository,
            IItemTemplateRequirementSkillRepository itemTemplateRequirementSkillRepository,
            IItemTemplateResistanceRepository itemTemplateResistanceRepository,
            IItemTemplateWeaponRepository itemTemplateWeaponRepository,
            ILogosRepository logosRepository,
            IMapInfoRepository mapInfoRepository,
            INpcMissionRepository npcMissionRepository,
            INpcPackageRepository npcPackageRepository,
            IPlayerRandomNameRepository randomNameRepository,
            ISpawnpoolRepository spawnpoolRepository,
            ITeleporterRepository teleporterRepository,
            IVendorItemRepository vendorItemRepository,
            IVendorReposiotry vendorReposiotry,
            IWeaponClassRepository weaponClassRepository)
                : base(dbContext)
        {
            ArmorClasses = armorClassRepository;
            Creatures = creatureRepository;
            CreatureActions = creatureActionRepository;
            CreatureAppearances = creatureAppearanceRepository;
            CreatureStats = creatureStatRepository;
            EntityClasses = entityClassRepository;
            EquipableClasses = equipableClassRepository;
            Footlockers = footlockerRepository;
            ItemClasses = itemClassRepository;
            ItemTemplateArmors = itemTemplateArmorRepository;
            ItemTemplates = itemTemplateRepository;
            ItemTemplateItemClasses = itemTemplateItemClassRepository;
            ItemTemplateRequirements = itemTemplateRequirementRepository;
            ItemTemplateRequirementRaces = itemTemplateRequirementRaceRepository;
            ItemTemplateRequirementSkills = itemTemplateRequirementSkillRepository;
            ItemTemplateResistances = itemTemplateResistanceRepository;
            ItemTemplateWeapons = itemTemplateWeaponRepository;
            Logoses = logosRepository;
            MapInfos = mapInfoRepository;
            NpcMissions = npcMissionRepository;
            NpcPackages = npcPackageRepository;
            RandomNames = randomNameRepository;
            Spawnpools = spawnpoolRepository;
            Teleporters = teleporterRepository;
            VendorItems = vendorItemRepository;
            Vendors = vendorReposiotry;
            WeaponClasses = weaponClassRepository;
        }

        public IArmorClassRepository ArmorClasses { get; }
        public ICreatureRepository Creatures { get; }
        public ICreatureActionRepository CreatureActions { get; }
        public ICreatureAppearanceRepository CreatureAppearances { get; }
        public ICreatureStatRepository CreatureStats { get; }
        public IEntityClassRepository EntityClasses { get; }
        public IEquipableClassRepository EquipableClasses { get; }
        public IFootlockerRepository Footlockers { get; }
        public IItemClassRepository ItemClasses { get; }
        public IItemTemplateArmorRepository ItemTemplateArmors { get; }
        public IItemTemplateRepository ItemTemplates { get; }
        public IItemTemplateItemClassRepository ItemTemplateItemClasses { get; }
        public IItemTemplateRequirementRepository ItemTemplateRequirements { get; }
        public IItemTemplateRequirementRaceRepository ItemTemplateRequirementRaces { get; }
        public IItemTemplateRequirementSkillRepository ItemTemplateRequirementSkills { get; }
        public IItemTemplateResistanceRepository ItemTemplateResistances { get; }
        public IItemTemplateWeaponRepository ItemTemplateWeapons { get; }
        public ILogosRepository Logoses { get; }
        public IMapInfoRepository MapInfos { get; }
        public INpcMissionRepository NpcMissions { get; }
        public INpcPackageRepository NpcPackages { get; }
        public IPlayerRandomNameRepository RandomNames { get; }
        public ISpawnpoolRepository Spawnpools { get; }
        public ITeleporterRepository Teleporters { get; }
        public IVendorItemRepository VendorItems { get; }
        public IVendorReposiotry Vendors { get; }
        public IWeaponClassRepository WeaponClasses { get; }
    }
}
