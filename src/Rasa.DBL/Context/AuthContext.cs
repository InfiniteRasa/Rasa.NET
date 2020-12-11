using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context
{
    using Configuration;
    using Configuration.ContextSetup;
    using Structures;

    public class AuthContext : DbContext
    {
        private readonly IOptions<DatabaseConnectionConfiguration> _databaseConnectionConfiguration;
        private readonly IDbContextConfigurationService _dbContextConfigurationService;

        /// <summary>
        /// Constructor for DI
        /// </summary>
        public AuthContext(IOptions<DatabaseConnectionConfiguration> databaseConnectionConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService)
        {
            _databaseConnectionConfiguration = databaseConnectionConfiguration;
            _dbContextConfigurationService = dbContextConfigurationService;
        }

        /// <summary>
        /// Constructor for migration tools
        /// </summary>
        internal AuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            _dbContextConfigurationService.Configure(optionsBuilder, _databaseConnectionConfiguration.Value);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastIp)
                .HasDefaultValue("0.0.0.0");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.JoinDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Locked)
                .HasDefaultValue(false);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Validated)
                .HasDefaultValue(false);
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }
    }
}