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
            // Outpost_tower_teleporters inserts descriptions longer than varchar(64).
            // Keep the authored text intact; SQLite does not enforce length limits.
            modelBuilder.Entity<TeleporterEntry>()
                .Property(teleporter => teleporter.Description)
                .HasColumnType("text");
            // Proving_grounds_front_squads authors a 57-character creature comment.
            // Keep the historic SQLite schema unchanged; MySQL widens before that seed.
            modelBuilder.Entity<CreatureEntry>()
                .Property(creature => creature.Comment)
                .HasColumnType("text");
        }
    }
}