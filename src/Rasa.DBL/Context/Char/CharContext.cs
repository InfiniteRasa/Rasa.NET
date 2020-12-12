using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Char
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services;
    using Structures;

    public abstract class CharContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected CharContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService, IDbContextPropertyModifier dbContextPropertyModifier) 
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }

        public DbSet<GameAccountEntry> GameAccountEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.Char;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.Level)
                .AsTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.SelectedSlot)
                .AsTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.SelectedSlot)
                .HasDefaultValue(false);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.LastIp)
                .HasDefaultValue("0.0.0.0");

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.LastLogin)
                .AsCurrentDateTime(_dbContextPropertyModifier);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.CreatedAt)
                .AsCurrentDateTime(_dbContextPropertyModifier);
        }
    }
}