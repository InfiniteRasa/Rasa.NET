namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public interface IWorldUnitOfWork : IUnitOfWork
    {
        IArmorClassRepository ArmorClasses { get; }
        ICreatureRepository Creatures { get; }
        ICreatureActionRepository CreatureActions { get; }
        ICreatureAppearanceRepository CreatureAppearances { get; }
        ICreatureStatRepository CreatureStats { get; }
        IEntityClassRepository EntityClasses { get; }
        IEquipableClassRepository EquipableClasses { get; }
        IFootlockerRepository Footlockers { get; }
        IItemClassRepository ItemClasses { get; }
        IItemTemplateArmorRepository ItemTemplateArmors { get; }
        IItemTemplateItemClassRepository ItemTemplateItemClasses { get; }
        IItemTemplateRepository ItemTemplates { get; }
        IItemTemplateRequirementRepository ItemTemplateRequirements { get; }
        IItemTemplateRequirementRaceRepository ItemTemplateRequirementRaces { get; }
        IItemTemplateRequirementSkillRepository ItemTemplateRequirementSkills { get; }
        IItemTemplateResistanceRepository ItemTemplateResistances { get; }
        IItemTemplateWeaponRepository ItemTemplateWeapons { get; }
        ILogosRepository Logoses { get; }
        IMapInfoRepository MapInfos { get; }
        INpcMissionRepository NpcMissions { get; }
        INpcPackageRepository NpcPackages { get; }
        IPlayerRandomNameRepository RandomNames { get; }
        ISpawnpoolRepository Spawnpools { get; }
        ITeleporterRepository Teleporters { get; }
        IVendorItemRepository VendorItems { get; }
        IVendorReposiotry Vendors { get; }
        IWeaponClassRepository WeaponClasses { get; }
    }
}