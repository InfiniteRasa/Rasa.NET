using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.Auth;

    public abstract class AuthContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected AuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
        : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.Auth;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupAuthAccountTable(modelBuilder);
        }

        private void SetupAuthAccountTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
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
    }
}