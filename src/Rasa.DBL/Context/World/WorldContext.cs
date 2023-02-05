using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.World;

    public abstract class WorldContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected WorldContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }
        public DbSet<ArmorClassEntry> ArmorClassEntries { get; set; }
        public DbSet<CreatureEntry> CreatureEntries { get; set; }
        public DbSet<CreatureActionEntry> CreatureActionEntries { get; set; }
        public DbSet<CreatureAppearanceEntry> CreatureAppearanceEntries { get; set; }
        public DbSet<CreatureStatEntry> CreatureStatEntries { get; set; }
        public DbSet<ExperienceForLevelEntry> ExperienceForLevelEntries { get; set; }
        public DbSet<EntityClassEntry> EntityClassEntries { get; set; }
        public DbSet<EquipableClassEntry> EquipableClassEntries { get; set; }
        public DbSet<FootlockerEntry> FootlockerEntries { get; set; }
        public DbSet<ItemClassEntry> ItemClassEntries { get; set; }
        public DbSet<ItemTemplateEntry> ItemTemplateEntries { get; set; }
        public DbSet<ItemTemplateArmorEntry> ItemTemplateArmorEntries { get; set; }
        public DbSet<ItemTemplateItemClassEntry> ItemTemplateItemClassEntries { get; set; }
        public DbSet<ItemTemplateRequirementEntry> ItemTemplateRequirementEntries { get; set; }
        public DbSet<ItemTemplateRequirementRaceEntry> ItemTemplateRequirementRaceEntries { get; set; }
        public DbSet<ItemTemplateRequirementSkillEntry> ItemTemplateRequirementSkillEntries { get; set; }
        public DbSet<ItemTemplateResistanceEntry> ItemTemplateResistanceEntries { get; set; }
        public DbSet<ItemTemplateWeaponEntry> ItemTemplateWeaponEntries { get; set; }
        public DbSet<LogosEntry> LogosEntries { get; set; }
        public DbSet<MapInfoEntry> MapInfoEntries { get; set; }
        public DbSet<NpcMissionEntry> NpcMissionEntries { get; set; }
        public DbSet<NpcMissionRewardEntry> NpcMissionRewardEntries { get; set; }
        public DbSet<NpcPackageEntry> NpcPackageEntries { get; set; }
        public DbSet<RandomNameEntry> RandomNameEntries { get; set; }
        public DbSet<SpawnPoolEntry> SpawnPoolEntries { get; set; }
        public DbSet<TeleporterEntry> TeleporterEntries { get; set; }
        public DbSet<VendorEntry> VendorEntries { get; set; }
        public DbSet<VendorItemEntry> VendorItemEntries { get; set; }
        public DbSet<WeaponClassEntry> WeaponClassEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.World;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupExperienceForLevel(modelBuilder);
            SetupRandomName(modelBuilder);
            SetupItemTemplateItemClass(modelBuilder);
        }

        private void SetupExperienceForLevel(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Level)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Experience)
                .AsUnsignedBigInt(_dbContextPropertyModifier, 20);
        }

        private void SetupRandomName(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Type)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Gender)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>().HasKey(e => new { e.Name, e.Type, e.Gender });
        }

        private void SetupItemTemplateItemClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemTemplateId)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemClass)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
        }
    }
}
