using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ContextSetup;
    using Services.DbContext;
    using Structures.World;

    public class MySqlWorldContext : WorldContext
    {
        public MySqlWorldContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService, dbContextPropertyModifier)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // The original varchar(50) cannot hold authored boss action descriptions.
            // Keep the SQLite model unchanged; only MySQL needs the wider column.
            modelBuilder.Entity<CreatureActionEntry>()
                .Property(action => action.Description)
                .HasColumnType("text");
        }
    }
}