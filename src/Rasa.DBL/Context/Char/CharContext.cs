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

        public DbSet<CharacterEntry> CharacterEntries { get; set; }

        public DbSet<ClanEntry> ClanEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.Char;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupGameAccountEntryTable(modelBuilder);
            SetupCharacterTable(modelBuilder);
            SetupClanTable(modelBuilder);
        }

        private void SetupGameAccountEntryTable(ModelBuilder modelBuilder)
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
                .Property(e => e.CanSkipBootcamp)
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

        private void SetupCharacterTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.AccountId)
                .AsIdColumn(_dbContextPropertyModifier);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Slot)
                .AsTinyInt(_dbContextPropertyModifier, 3);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Race)
                .AsTinyInt(_dbContextPropertyModifier, 3);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Class)
                .AsInt(_dbContextPropertyModifier, 11);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Scale)
                .AsDouble(_dbContextPropertyModifier, true);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Experience)
                .AsInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Level)
                .AsTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(1);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Body)
                .AsInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Mind)
                .AsInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Spirit)
                .AsInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CloneCredits)
                .AsInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.MapContextId)
                .AsInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CoordX)
                .AsDouble(_dbContextPropertyModifier, false);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CoordY)
                .AsDouble(_dbContextPropertyModifier, false);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CoordZ)
                .AsDouble(_dbContextPropertyModifier, false);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.NumLogins)
                .AsInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.TotalTimePlayed)
                .AsInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CreatedAt)
                .AsCurrentDateTime(_dbContextPropertyModifier);
        }

        private void SetupClanTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClanEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<ClanEntry>()
                .Property(e => e.CreatedAt)
                .AsCurrentDateTime(_dbContextPropertyModifier);

        }
    }
}