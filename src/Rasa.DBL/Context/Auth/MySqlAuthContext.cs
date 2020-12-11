using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Structures;

    public class MySqlAuthContext : AuthContext
    {
        public MySqlAuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService) 
            : base(databaseConfiguration, dbContextConfigurationService)
        {
        }

        internal MySqlAuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Id)
                .HasColumnType("int(11) unsigned");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .HasColumnType("tinyint(3) unsigned")
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .HasColumnType("tinyint(3) unsigned")
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.JoinDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}