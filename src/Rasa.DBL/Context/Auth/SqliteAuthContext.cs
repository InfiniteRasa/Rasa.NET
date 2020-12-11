using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Auth
{
    using Configuration;
    using Configuration.ContextSetup;
    using Structures;

    public class SqliteAuthContext : AuthContext
    {
        public SqliteAuthContext(IOptions<DatabaseConfiguration> databaseConfiguration, 
            IDbContextConfigurationService dbContextConfigurationService) 
            : base(databaseConfiguration, dbContextConfigurationService)
        {
        }

        internal SqliteAuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Id)
                .HasColumnType("integer");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .HasColumnType("tinyint(3)")
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .HasColumnType("tinyint(3)")
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.JoinDate)
                .HasDefaultValueSql("datetime('now')");
        }
    }
}