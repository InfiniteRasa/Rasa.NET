using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Char
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.Char;
    using System;

    public abstract class CharContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected CharContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService, 
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }

        public DbSet<GameAccountEntry> GameAccountEntries { get; set; }
        public DbSet<CensorWordsEntry> CensorWordsEntries { get; set; }
        public DbSet<CharacterEntry> CharacterEntries { get; set; }
        public DbSet<CharacterAbilityDrawerEntry> CharacterAbilityDrawerEntries { get; set; }
        public DbSet<CharacterAppearanceEntry> CharacterAppearanceEntries { get; set; }
        public DbSet<CharacterInventoryEntry> CharacterInventoryEntries { get; set; }
        public DbSet<CharacterLockboxEntry> CharacterLockboxEntries { get; set; }
        public DbSet<CharacterLogosEntry> CharacterLogosEntries { get; set; }
        public DbSet<CharacterMissionEntry> CharacterMissionEntries { get; set; }
        public DbSet<CharacterOptionEntry> CharacterOptionEntries { get; set; }
        public DbSet<CharacterSkillsEntry> CharacterSkillsEntries { get; set; }
        public DbSet<CharacterTeleporterEntry> CharacterTeleporterEntries { get; set; }
        public DbSet<CharacterTitleEntry> CharacterTitleEntries { get; set; }
        public DbSet<ClanEntry> ClanEntries { get; set; }
        public DbSet<ClanInventoryEntry> ClanInventoryEntries { get; set; }
        public DbSet<ClanMemberEntry> ClanMemberEntries { get; set; }
        public DbSet<FriendEntry> FriendEntries { get; set; }
        public DbSet<IgnoredEntry> IgnoredEntries { get; set; }
        public DbSet<ItemEntry> ItemEntries { get; set; }
        public DbSet<UserOptionEntry> UserOptionEntries { get; set; }
        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.Char;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupGameAccountEntryTable(modelBuilder);
            SetupCharacterAbilityDrawerTable(modelBuilder);
            SetupCharacterTable(modelBuilder);
            SetupCharacterAppearanceTable(modelBuilder);
            SetupCharacterLogosTable(modelBuilder);
            SetupCharacterSkillTable(modelBuilder);
            SetupCharacterTeleporterTable(modelBuilder);
            SetupCharacterOptionsTable(modelBuilder);
            SetupClanMemberTable(modelBuilder);
            SetupClanTable(modelBuilder);
            SetupUserOptionsTable(modelBuilder);
        }

        private void SetupGameAccountEntryTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.Level)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.FamilyName)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<GameAccountEntry>()
                .Property(e => e.SelectedSlot)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
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

        private void SetupCharacterAbilityDrawerTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterAbilityDrawerEntry>()
                .HasKey(e => new { e.CharacterId, e.AbilitySlot });
        }

        private void SetupCharacterAppearanceTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterAppearanceEntry>()
                .Property(e => e.CharacterId)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<CharacterAppearanceEntry>()
                .Property(e => e.Slot)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<CharacterAppearanceEntry>()
                .Property(e => e.Class)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterAppearanceEntry>()
                .Property(e => e.Color)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterAppearanceEntry>()
                .HasOne(e => e.Character)
                .WithMany(e => e.CharacterAppearance)
                .IsRequired()
                .HasForeignKey(nameof(CharacterAppearanceEntry.CharacterId))
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CharacterAppearanceEntry>()
                .HasKey(e => new { e.CharacterId, e.Slot });
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
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Race)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Class)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Scale)
                .AsUnsignedDouble(_dbContextPropertyModifier);
            
            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Experience)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Level)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(1);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Body)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Mind)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.Spirit)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CloneCredits)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.MapContextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.NumLogins)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.RunState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(1);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CrouchState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.LastLogin)
                .AsCurrentDateTime(_dbContextPropertyModifier);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.TotalTimePlayed)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .HasDefaultValue(0);

            modelBuilder.Entity<CharacterEntry>()
                .Property(e => e.CreatedAt)
                .AsCurrentDateTime(_dbContextPropertyModifier);

            modelBuilder.Entity<CharacterEntry>()
                .HasOne(e => e.GameAccount)
                .WithMany(e => e.Characters)
                .IsRequired()
                .HasForeignKey(nameof(CharacterEntry.AccountId))
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void SetupCharacterLogosTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterLogosEntry>()
                .HasKey(e => new { e.CharacterId, e.LogosId });
        }

        private void SetupCharacterOptionsTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterOptionEntry>()
                .HasKey(e => new { e.CharacterId, e.OptionId });
        }

        private void SetupCharacterSkillTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterSkillsEntry>()
                .HasKey(e => new { e.CharacterId, e.SkillId });
        }

        private void SetupCharacterTeleporterTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterTeleporterEntry>()
                .HasKey(e => new { e.CharacterId, e.WaypointId });
        }

        private void SetupClanMemberTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClanMemberEntry>()
                .Property(e => e.ClanId)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<ClanMemberEntry>()
                .Property(e => e.CharacterId)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<ClanMemberEntry>()
                .Property(e => e.Rank)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue(0);

            modelBuilder.Entity<ClanMemberEntry>()
                .HasOne(e => e.Character)
                .WithOne(e => e.MemberOfClan)
                .IsRequired()
                .HasForeignKey<ClanMemberEntry>(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClanMemberEntry>()
                .HasOne(e => e.Clan)
                .WithMany(e => e.Members)
                .IsRequired()
                .HasForeignKey(nameof(ClanMemberEntry.ClanId))
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClanMemberEntry>()
                .HasKey(e => new { e.ClanId, e.CharacterId});
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

        private void SetupUserOptionsTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserOptionEntry>()
                .HasKey(e => new { e.AccountId, e.OptionId });
        }
    }
}
