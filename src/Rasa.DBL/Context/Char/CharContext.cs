using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace Rasa.Context.Char
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.Char;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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
        public DbSet<CharacterMissionOfferEntry> CharacterMissionOfferEntries { get; set; }
        public DbSet<CharacterMissionItemEntry> CharacterMissionItemEntries { get; set; }
        public DbSet<CharacterMissionItemReceiptEntry> CharacterMissionItemReceiptEntries { get; set; }
        public DbSet<CharacterMissionItemQuarantineEntry> CharacterMissionItemQuarantineEntries { get; set; }
        public DbSet<CharacterMissionDeadlineEntry> CharacterMissionDeadlineEntries { get; set; }
        public DbSet<CharacterMissionObjectiveEntry> CharacterMissionObjectiveEntries { get; set; }
        public DbSet<CharacterMissionObjectiveCounterEntry> CharacterMissionObjectiveCounterEntries { get; set; }
        public DbSet<CharacterMissionObjectiveItemCounterEntry> CharacterMissionObjectiveItemCounterEntries { get; set; }
        public DbSet<CharacterMissionScenarioStepEntry> CharacterMissionScenarioStepEntries { get; set; }
        public DbSet<CharacterOptionEntry> CharacterOptionEntries { get; set; }
        public DbSet<CharacterFlagEntry> CharacterFlagEntries { get; set; }
        public DbSet<CharacterSkillsEntry> CharacterSkillsEntries { get; set; }
        public DbSet<CharacterStartingExperienceEntry> CharacterStartingExperienceEntries { get; set; }
        public DbSet<CharacterTeleporterEntry> CharacterTeleporterEntries { get; set; }
        public DbSet<CharacterTitleEntry> CharacterTitleEntries { get; set; }
        public DbSet<ClanEntry> ClanEntries { get; set; }
        public DbSet<AuctionEntry> AuctionEntries { get; set; }

        public DbSet<ClanInventoryEntry> ClanInventoryEntries { get; set; }
        public DbSet<ClanMemberEntry> ClanMemberEntries { get; set; }
        public DbSet<ClanLockboxLogEntry> ClanLockboxLogEntries { get; set; }
        public DbSet<FriendEntry> FriendEntries { get; set; }
        public DbSet<IgnoredEntry> IgnoredEntries { get; set; }
        public DbSet<ItemEntry> ItemEntries { get; set; }
        public DbSet<PetitionEntry> PetitionEntries { get; set; }
        public DbSet<UserOptionEntry> UserOptionEntries { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AdvanceMissionVersions();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AdvanceMissionVersions();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AdvanceMissionVersions()
        {
            var changed = ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted)
                .Select(entry => entry.Entity switch
                {
                    CharacterMissionEntry mission => ((uint, uint)?)(mission.CharacterId, mission.MissionId),
                    CharacterMissionObjectiveEntry objective => (objective.CharacterId, objective.MissionId),
                    CharacterMissionObjectiveCounterEntry counter => (counter.CharacterId, counter.MissionId),
                    CharacterMissionObjectiveItemCounterEntry counter => (counter.CharacterId, counter.MissionId),
                    _ => null
                }).Where(key => key.HasValue).Select(key => key.Value).Distinct().ToArray();
            foreach (var (characterId, missionId) in changed)
            {
                var mission = CharacterMissionEntries.Find(characterId, missionId);
                if (mission == null)
                    continue;
                var tracked = Entry(mission);
                if (tracked.State is EntityState.Added or EntityState.Deleted ||
                    tracked.Property(entry => entry.Version).IsModified)
                    continue;
                mission.Version = checked(mission.Version + 1);
            }
        }

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
            SetupCharacterMissionTable(modelBuilder);
            SetupCharacterMissionObjectiveTables(modelBuilder);
            SetupCharacterMissionDurabilityTables(modelBuilder);
            MissionRuntimeModel.Configure(modelBuilder);
            MissionItemModel.Configure(modelBuilder);
            SetupCharacterSkillTable(modelBuilder);
            SetupCharacterStartingExperienceTables(modelBuilder);
            SetupCharacterFlagTable(modelBuilder);
            SetupCharacterTeleporterTable(modelBuilder);
            SetupCharacterOptionsTable(modelBuilder);
            SetupClanMemberTable(modelBuilder);
            SetupClanTable(modelBuilder);
            SetupFriendTable(modelBuilder);
            SetupIgnoredTable(modelBuilder);
            SetupPetitionTable(modelBuilder);
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

        private void SetupCharacterMissionTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterMissionEntry>()
                .HasKey(e => new { e.CharacterId, e.MissionId });
            modelBuilder.Entity<CharacterMissionEntry>()
                .HasOne<CharacterEntry>()
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterMissionEntry>()
                .Property(e => e.Completeable)
                .HasDefaultValue(false);
        }

        private void SetupCharacterMissionObjectiveTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterMissionObjectiveEntry>()
                .HasKey(entry => new { entry.CharacterId, entry.MissionId, entry.ObjectiveId });
            modelBuilder.Entity<CharacterMissionObjectiveEntry>()
                .HasOne(entry => entry.Mission)
                .WithMany(mission => mission.Objectives)
                .HasForeignKey(entry => new { entry.CharacterId, entry.MissionId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterMissionObjectiveEntry>()
                .Property(entry => entry.ObjectiveState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .IsConcurrencyToken();

            modelBuilder.Entity<CharacterMissionObjectiveCounterEntry>()
                .HasKey(entry => new
                {
                    entry.CharacterId,
                    entry.MissionId,
                    entry.ObjectiveId,
                    entry.CounterId
                });
            modelBuilder.Entity<CharacterMissionObjectiveCounterEntry>()
                .HasOne(entry => entry.Objective)
                .WithMany(objective => objective.Counters)
                .HasForeignKey(entry => new { entry.CharacterId, entry.MissionId, entry.ObjectiveId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterMissionObjectiveCounterEntry>()
                .Property(entry => entry.CounterId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<CharacterMissionObjectiveCounterEntry>()
                .Property(entry => entry.CounterValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .IsConcurrencyToken();

            modelBuilder.Entity<CharacterMissionObjectiveItemCounterEntry>()
                .HasKey(entry => new
                {
                    entry.CharacterId,
                    entry.MissionId,
                    entry.ObjectiveId,
                    entry.ItemClassId
                });
            modelBuilder.Entity<CharacterMissionObjectiveItemCounterEntry>()
                .HasOne(entry => entry.Objective)
                .WithMany(objective => objective.ItemCounters)
                .HasForeignKey(entry => new { entry.CharacterId, entry.MissionId, entry.ObjectiveId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterMissionObjectiveItemCounterEntry>()
                .Property(entry => entry.ItemClassId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<CharacterMissionObjectiveItemCounterEntry>()
                .Property(entry => entry.CounterValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .IsConcurrencyToken();
        }

        private void SetupCharacterMissionDurabilityTables(ModelBuilder modelBuilder)
        {
            var utcDateTime = new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            modelBuilder.Entity<CharacterMissionDeadlineEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_character_mission_deadline_state",
                    "state IN (1, 2, 3, 4)"));
            modelBuilder.Entity<CharacterMissionDeadlineEntry>()
                .HasKey(entry => new { entry.CharacterId, entry.MissionId });
            modelBuilder.Entity<CharacterMissionDeadlineEntry>()
                .HasOne(entry => entry.Mission)
                .WithOne(mission => mission.Deadline)
                .HasForeignKey<CharacterMissionDeadlineEntry>(entry =>
                    new { entry.CharacterId, entry.MissionId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterMissionDeadlineEntry>()
                .Property(entry => entry.State)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<CharacterMissionDeadlineEntry>()
                .Property(entry => entry.DueAtUtc)
                .HasConversion(utcDateTime);

            modelBuilder.Entity<CharacterMissionScenarioStepEntry>()
                .HasKey(entry => new { entry.CharacterId, entry.MissionId, entry.StepKey });
            modelBuilder.Entity<CharacterMissionScenarioStepEntry>()
                .HasOne(entry => entry.Mission)
                .WithMany(mission => mission.ScenarioSteps)
                .HasForeignKey(entry => new { entry.CharacterId, entry.MissionId })
                .OnDelete(DeleteBehavior.Cascade);
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

        private void SetupCharacterStartingExperienceTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterStartingExperienceEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_character_starting_experience_state",
                    "state IN (1, 2, 3, 4, 5)"));
            modelBuilder.Entity<CharacterStartingExperienceEntry>()
                .HasKey(entry => entry.CharacterId);
            modelBuilder.Entity<CharacterStartingExperienceEntry>()
                .HasOne(entry => entry.Character)
                .WithOne()
                .HasForeignKey<CharacterStartingExperienceEntry>(entry => entry.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterStartingExperienceEntry>()
                .Property(entry => entry.State)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

        }

        private void SetupCharacterFlagTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterFlagEntry>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint("CK_character_flag_id", "flag_id BETWEEN 1 AND 4294967295");
                    table.HasCheckConstraint("CK_character_flag_value", "value BETWEEN 0 AND 4294967295");
                });
            modelBuilder.Entity<CharacterFlagEntry>()
                .HasKey(entry => new { entry.CharacterId, entry.FlagId });
            modelBuilder.Entity<CharacterFlagEntry>()
                .HasOne(entry => entry.Character)
                .WithMany()
                .HasForeignKey(entry => entry.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CharacterFlagEntry>()
                .Property(entry => entry.FlagId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11)
                .ValueGeneratedNever();
            modelBuilder.Entity<CharacterFlagEntry>()
                .Property(entry => entry.Value)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
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

        // One row per (owner, contact). These were keyed on account_id alone, which capped
        // every account at a single friend and a single ignored player.
        private void SetupFriendTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FriendEntry>()
                .HasKey(e => new { e.AccountId, e.FriendAccountId });
        }

        private void SetupIgnoredTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IgnoredEntry>()
                .HasKey(e => new { e.AccountId, e.IgnoredAccountId });
        }

        private void SetupPetitionTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.AccountId)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.CharacterId)
                .AsIdColumn(_dbContextPropertyModifier);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.Type)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue((byte)0);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.MapContextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.Status)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3)
                .HasDefaultValue((byte)0);

            modelBuilder.Entity<PetitionEntry>()
                .Property(e => e.Resolution)
                .HasDefaultValue(string.Empty);

            // created_at carries no SQL default on purpose. The other tables use
            // CURRENT_TIMESTAMP, which MySQL rejects as a default for a datetime(6) column
            // unless the fractional precision is spelled out; the timestamp is set in
            // PetitionEntry's constructor instead, in UTC, like the rest of the server.
        }

        private void SetupUserOptionsTable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserOptionEntry>()
                .HasKey(e => new { e.AccountId, e.OptionId });
        }
    }
}
