using System;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Database
{
    using Structures;

    public partial class WorldContext : DbContext
    {
        public WorldContext()
        {
        }

        public WorldContext(DbContextOptions<WorldContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ArmorClassEntry> ArmorClass { get; set; }
        public virtual DbSet<CreatureActionEntry> CreatureAction { get; set; }
        public virtual DbSet<CreatureAppearanceEntry> CreatureAppearance { get; set; }
        public virtual DbSet<CreatureStatsEntry> CreatureStats { get; set; }
        public virtual DbSet<CreaturesEntry> Creatures { get; set; }
        public virtual DbSet<EntityClassEntry> EntityClass { get; set; }
        public virtual DbSet<EquipableClassEntry> Equipableclass { get; set; }
        public virtual DbSet<FootlockerEntry> Footlockers { get; set; }
        public virtual DbSet<ItemClassEntry> ItemClass { get; set; }
        public virtual DbSet<ItemTemplateEntry> Itemtemplate { get; set; }
        public virtual DbSet<ArmorTemplateEntry> ItemtemplateArmor { get; set; }
        public virtual DbSet<ItemTemplateItemClassEntry> ItemtemplateItemclass { get; set; }
        public virtual DbSet<ItemTemplateRaceRequirementEntry> ItemtemplateRacerequirement { get; set; }
        public virtual DbSet<ItemTemplateRequirementsEntry> ItemtemplateRequirements { get; set; }
        public virtual DbSet<ItemTemplateResistanceEntry> ItemtemplateResistance { get; set; }
        public virtual DbSet<ItemTemplateSkillRequirementEntry> ItemtemplateSkillrequirement { get; set; }
        public virtual DbSet<WeaponTemplateEntry> ItemtemplateWeapon { get; set; }
        public virtual DbSet<NpcMissionEntry> NpcMissions { get; set; }
        public virtual DbSet<NPCPackagesEntry> NpcPackages { get; set; }
        public virtual DbSet<PlayerExpForLevelEntry> PlayerExpForLevel { get; set; }
        public virtual DbSet<PlayerRandomNameEntry> PlayerRandomName { get; set; }
        public virtual DbSet<SpawnPoolEntry> Spawnpool { get; set; }
        public virtual DbSet<StarterItemsEntry> StarterItems { get; set; }
        public virtual DbSet<TeleporterEntry> Teleporter { get; set; }
        public virtual DbSet<VendorItemsEntry> VendorItems { get; set; }
        public virtual DbSet<VendorsEntry> Vendors { get; set; }
        public virtual DbSet<WeaponClassEntry> WeaponClass { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldContext).Assembly,
                t => t.Namespace?.StartsWith("Rasa.Database.Configurations.World") ?? false);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        [DbFunction]
        public float Rand()
        {
            throw new NotImplementedException();
        }
    }
}
