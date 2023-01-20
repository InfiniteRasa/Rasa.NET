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

        public DbSet<ExperienceForLevelEntry> ExperienceForLevel { get; set; }

        public DbSet<RandomNameEntry> RandomNameEntries { get; set; }

        public DbSet<ItemTemplateItemClassEntry> ItemTemplateItemClassEntries { get; set; }

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