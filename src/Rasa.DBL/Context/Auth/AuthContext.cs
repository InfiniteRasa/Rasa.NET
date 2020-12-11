using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Initialization;
    using Structures;

    public abstract class AuthContext : DbContext, IInitializable
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextConfigurationService _dbContextConfigurationService;

        /// <summary>
        /// Constructor for DI
        /// </summary>
        protected AuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextConfigurationService = dbContextConfigurationService;
        }

        /// <summary>
        /// Constructor for migration tools
        /// </summary>
        internal AuthContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            _dbContextConfigurationService.Configure(optionsBuilder, _databaseConfiguration.Value.Auth);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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