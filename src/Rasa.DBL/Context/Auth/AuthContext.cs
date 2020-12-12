using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Initialization;
    using Services;
    using Structures;

    public abstract class AuthContext : DbContext, IInitializable
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextConfigurationService _dbContextConfigurationService;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected AuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextConfigurationService = dbContextConfigurationService;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _dbContextConfigurationService.Configure(optionsBuilder, _databaseConfiguration.Value.Auth);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .AsTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .AsTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.JoinDate)
                .AsCurrentDateTime(_dbContextPropertyModifier);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastIp)
                .HasDefaultValue("0.0.0.0");
            
            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Locked)
                .HasDefaultValue(false);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Validated)
                .HasDefaultValue(false);
        }
        public void Initialize()
        {
            if (_databaseConfiguration.Value.GetDatabaseProvider() == DatabaseProvider.Sqlite)
            {
                this.Database.Migrate();
            }
        }
    }
}