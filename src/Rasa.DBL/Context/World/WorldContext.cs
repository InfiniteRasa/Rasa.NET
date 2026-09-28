using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.World;

    public abstract class WorldContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected WorldContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }
        public DbSet<ActionEntry> ActionEntries { get; set; }
        public DbSet<ActionLevelEntry> ActionLevelEntries { get; set; }
        public DbSet<ActionCostEntry> ActionCostEntries { get; set; }
        public DbSet<ActionPropertyEntry> ActionPropertyEntries { get; set; }
        public DbSet<ActionItemRequirementEntry> ActionItemRequirementEntries { get; set; }
        public DbSet<ArmorClassEntry> ArmorClassEntries { get; set; }
        public DbSet<CreatureEntry> CreatureEntries { get; set; }
        public DbSet<CreatureActionEntry> CreatureActionEntries { get; set; }
        public DbSet<CreatureAppearanceEntry> CreatureAppearanceEntries { get; set; }
        public DbSet<CreatureStatEntry> CreatureStatEntries { get; set; }
        public DbSet<CreatureClassFlagEntry> CreatureClassFlagEntries { get; set; }
        public DbSet<SkillCharacterEntry> SkillCharacterEntries { get; set; }
        public DbSet<ExperienceForLevelEntry> ExperienceForLevelEntries { get; set; }
        public DbSet<EntityClassEntry> EntityClassEntries { get; set; }
        public DbSet<EquipableClassEntry> EquipableClassEntries { get; set; }
        public DbSet<FootlockerEntry> FootlockerEntries { get; set; }
        public DbSet<ItemClassEntry> ItemClassEntries { get; set; }
        public DbSet<ItemTemplateEntry> ItemTemplateEntries { get; set; }
        public DbSet<ItemTemplateActionEntry> ItemTemplateActionEntries { get; set; }
        public DbSet<ItemTemplateArmorEntry> ItemTemplateArmorEntries { get; set; }
        public DbSet<ItemTemplateItemClassEntry> ItemTemplateItemClassEntries { get; set; }
        public DbSet<ItemTemplateRequirementEntry> ItemTemplateRequirementEntries { get; set; }
        public DbSet<ItemTemplateRequirementRaceEntry> ItemTemplateRequirementRaceEntries { get; set; }
        public DbSet<ItemTemplateRequirementSkillEntry> ItemTemplateRequirementSkillEntries { get; set; }
        public DbSet<ItemTemplateResistanceEntry> ItemTemplateResistanceEntries { get; set; }
        public DbSet<ItemTemplateWeaponEntry> ItemTemplateWeaponEntries { get; set; }
        public DbSet<LogosEntry> LogosEntries { get; set; }
        public DbSet<MapInfoEntry> MapInfoEntries { get; set; }
        public DbSet<MapLinkEntry> MapLinkEntries { get; set; }
        public DbSet<KraftwerksEntry> KraftwerksEntries { get; set; }
        public DbSet<MapRegionEntry> MapRegionEntries { get; set; }
        public DbSet<MapMarkerEntry> MapMarkerEntries { get; set; }
        public DbSet<RecipeEntry> RecipeEntries { get; set; }
        public DbSet<RecipeInputEntry> RecipeInputEntries { get; set; }
        public DbSet<NpcMissionEntry> NpcMissionEntries { get; set; }
        public DbSet<NpcMissionRewardEntry> NpcMissionRewardEntries { get; set; }
        public DbSet<MissionContentDefinitionEntry> MissionContentDefinitionEntries { get; set; }
        public DbSet<MissionPrerequisiteEntry> MissionPrerequisiteEntries { get; set; }
        public DbSet<MissionObjectiveDefinitionEntry> MissionObjectiveDefinitionEntries { get; set; }
        public DbSet<MissionObjectiveTransitionEntry> MissionObjectiveTransitionEntries { get; set; }
        public DbSet<MissionTriggerEntry> MissionTriggerEntries { get; set; }
        public DbSet<MissionActionEntry> MissionActionEntries { get; set; }
        public DbSet<MissionRewardDefinitionEntry> MissionRewardDefinitionEntries { get; set; }
        public DbSet<MissionRewardItemEntry> MissionRewardItemEntries { get; set; }
        public DbSet<MissionIndicatorEntry> MissionIndicatorEntries { get; set; }
        public DbSet<MissionAreaEntry> MissionAreaEntries { get; set; }
        public DbSet<MissionSpawnGroupEntry> MissionSpawnGroupEntries { get; set; }
        public DbSet<MissionSpawnEntry> MissionSpawnEntries { get; set; }
        public DbSet<MissionScenarioEntry> MissionScenarioEntries { get; set; }
        public DbSet<MissionScenarioStepEntry> MissionScenarioStepEntries { get; set; }
        public DbSet<MissionEvidenceEntry> MissionEvidenceEntries { get; set; }
        public DbSet<NpcPackageEntry> NpcPackageEntries { get; set; }
        public DbSet<RandomNameEntry> RandomNameEntries { get; set; }
        public DbSet<SpawnPoolEntry> SpawnPoolEntries { get; set; }
        public DbSet<TeleporterEntry> TeleporterEntries { get; set; }
        public DbSet<VendorEntry> VendorEntries { get; set; }
        public DbSet<VendorItemEntry> VendorItemEntries { get; set; }
        public DbSet<WeaponClassEntry> WeaponClassEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.World;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupExperienceForLevel(modelBuilder);
            SetupRandomName(modelBuilder);
            SetupItemTemplateItemClass(modelBuilder);
            SetupMapMarker(modelBuilder);
            SetupCreatureClassFlag(modelBuilder);
            SetupSkillCharacter(modelBuilder);
            SetupMissionContent(modelBuilder);
            modelBuilder.Entity<MissionSceneBindingEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision });
            modelBuilder.Entity<MissionExperienceBindingEntry>()
                .HasKey(entry => entry.ExperienceKey);
            modelBuilder.Entity<MissionSceneBindingEntry>().HasOne<MissionContentDefinitionEntry>().WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision }).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionRepeatPolicyEntry>().HasKey(entry => new { entry.MissionId, entry.ContentRevision });
            modelBuilder.Entity<MissionChannelPolicyEntry>().HasKey(entry => new { entry.MissionId, entry.ContentRevision });
            modelBuilder.Entity<MissionChannelPolicyEntry>().HasOne<MissionContentDefinitionEntry>().WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision }).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionChannelPolicyEntry>().ToTable(table => table.HasCheckConstraint(
                "CK_mission_channel_policy_channels", "acceptance_channel IN (1, 2, 3) AND completion_channel IN (1, 2, 3)"));
            modelBuilder.Entity<MissionRepeatPolicyEntry>().HasOne<MissionContentDefinitionEntry>().WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision }).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionRepeatPolicyEntry>().ToTable(table => table.HasCheckConstraint(
                "CK_mission_repeat_policy_parameters",
                "(repeat_kind IN (0, 1) AND cooldown_seconds IS NULL AND reset_second_utc IS NULL) OR " +
                "(repeat_kind = 2 AND cooldown_seconds IS NOT NULL AND cooldown_seconds > 0 AND reset_second_utc IS NULL) OR " +
                "(repeat_kind = 3 AND cooldown_seconds IS NULL AND reset_second_utc IS NOT NULL AND reset_second_utc BETWEEN 0 AND 86399)"));
        }

        /// <summary>
        /// A marker is identified by its id *and* its map. The client reuses one marker id across
        /// a zone and its wargame variant, where the server has two different objects, so the id
        /// alone is not a key - and the read is always one map's worth anyway.
        /// </summary>
        private static void SetupMapMarker(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MapMarkerEntry>()
                .HasKey(e => new { e.MarkerEntityId, e.MapContextId });
        }

        /// <summary>
        /// A class carries several flags, so the row is the pair. Composite keys cannot be
        /// declared with [Key] attributes - EF refuses the model outright - so it is set here.
        /// </summary>
        private static void SetupCreatureClassFlag(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CreatureClassFlagEntry>()
                .HasKey(e => new { e.ClassId, e.FlagId });
        }

        private void SetupExperienceForLevel(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Level)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Experience)
                .AsUnsignedBigInt(_dbContextPropertyModifier, 20);
        }

        private void SetupRandomName(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Type)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Gender)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>().HasKey(e => new { e.Name, e.Type, e.Gender });
        }

        /// <summary>
        /// The skill id is the client's own and is inserted as it stands, so it is a key rather
        /// than something the database hands out.
        /// </summary>
        private void SetupSkillCharacter(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SkillCharacterEntry>()
                .Property(e => e.Id)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
        }

        private void SetupItemTemplateItemClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemTemplateId)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemClass)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
        }

        private void SetupMissionContent(ModelBuilder modelBuilder)
        {
            const string triggerParameterSetConstraint =
                "(kind IN (1, 2, 3, 4, 5)) " +
                "AND (kind <> 1 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL " +
                "AND related_objective_id IS NULL AND related_state IS NULL " +
                "AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL " +
                "AND initial_value IS NULL AND target_value IS NULL " +
                "AND area_id IS NULL AND duration_seconds IS NULL " +
                "AND source_spawn_resolved IS NULL)) " +
                "AND (kind <> 2 OR (event_kind IS NOT NULL AND subject_id IS NOT NULL " +
                "AND related_objective_id IS NULL AND related_state IS NULL " +
                "AND area_id IS NULL AND duration_seconds IS NULL " +
                "AND npc_package_id IS NULL AND player_flag_id IS NULL)) " +
                "AND (kind <> 3 OR (related_objective_id IS NOT NULL AND related_state IS NOT NULL " +
                "AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL " +
                "AND initial_value IS NULL AND target_value IS NULL " +
                "AND area_id IS NULL AND duration_seconds IS NULL " +
                "AND npc_package_id IS NULL AND player_flag_id IS NULL " +
                "AND source_spawn_resolved IS NULL)) " +
                "AND (kind <> 4 OR (area_id IS NOT NULL " +
                "AND related_objective_id IS NULL AND related_state IS NULL " +
                "AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL " +
                "AND initial_value IS NULL AND target_value IS NULL " +
                "AND duration_seconds IS NULL AND npc_package_id IS NULL " +
                "AND player_flag_id IS NULL AND source_spawn_resolved IS NULL)) " +
                "AND (kind <> 5 OR (duration_seconds IS NOT NULL " +
                "AND related_objective_id IS NULL AND related_state IS NULL " +
                "AND event_kind IS NULL AND subject_id IS NULL AND counter_id IS NULL " +
                "AND initial_value IS NULL AND target_value IS NULL " +
                "AND area_id IS NULL AND npc_package_id IS NULL " +
                "AND player_flag_id IS NULL AND source_spawn_resolved IS NULL))";
            const string actionParameterSetConstraint =
                "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)) " +
                "AND (kind >= 10 OR item_intent IS NULL) " +
                "AND (kind < 10 OR (item_intent IS NOT NULL AND target_objective_id IS NULL " +
                "AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL " +
                "AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_id IS NULL " +
                "AND player_flag_value IS NULL AND npc_package_id IS NULL)) " +
                "AND (kind <> 1 OR (target_objective_id IS NOT NULL AND objective_state IS NULL " +
                "AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 2 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL " +
                "AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 3 OR (target_objective_id IS NOT NULL AND objective_state IS NOT NULL " +
                "AND reward_id IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 4 OR (reward_id IS NOT NULL AND target_objective_id IS NULL " +
                "AND objective_state IS NULL AND spawn_group_id IS NULL AND scenario_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 5 OR (scenario_id IS NOT NULL AND target_objective_id IS NULL " +
                "AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 6 OR (spawn_group_id IS NOT NULL AND target_objective_id IS NULL " +
                "AND objective_state IS NULL AND reward_id IS NULL AND scenario_id IS NULL " +
                "AND indicator_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 7 OR (indicator_id IS NOT NULL AND target_objective_id IS NULL " +
                "AND objective_state IS NULL AND reward_id IS NULL AND spawn_group_id IS NULL " +
                "AND scenario_id IS NULL AND player_flag_id IS NULL AND player_flag_value IS NULL " +
                "AND npc_package_id IS NULL)) " +
                "AND (kind <> 8 OR (player_flag_id IS NOT NULL AND player_flag_value IS NOT NULL " +
                "AND target_objective_id IS NULL AND objective_state IS NULL " +
                "AND reward_id IS NULL AND spawn_group_id IS NULL " +
                "AND scenario_id IS NULL AND indicator_id IS NULL AND npc_package_id IS NULL)) " +
                "AND (kind <> 9 OR (npc_package_id IS NOT NULL AND player_flag_id IS NOT NULL " +
                "AND target_objective_id IS NULL AND objective_state IS NULL " +
                "AND reward_id IS NULL AND spawn_group_id IS NULL " +
                "AND scenario_id IS NULL AND indicator_id IS NULL AND player_flag_value IS NULL))";
            const string rewardSelectionCountConstraint = "selection_count IN (0, 1)";
            const string rewardItemKindConstraint = "kind IN (1, 2)";
            string ScenarioStepNullColumns(params string[] allowedColumns)
            {
                var allColumns = new[]
                {
                    "target_objective_id",
                    "reward_id",
                    "spawn_group_id",
                    "spawn_id",
                    "dynamic_object_key",
                    "entity_class_id",
                    "target_scenario_id",
                    "delay_milliseconds",
                    "skill_id",
                    "ability_id",
                    "skill_level",
                    "ability_slot",
                    "tutorial_id",
                    "audio_set_id",
                    "attempt_key",
                    "scenario_event_id",
                    "map_context_id",
                    "pos_x",
                    "pos_y",
                    "pos_z",
                    "orientation",
                    "initial_interaction_enabled",
                    "qualification_key",
                    "qualification_value",
                    "account_skip_entitlement"
                };
                return string.Join(
                    " AND ",
                    allColumns
                        .Where(column => !allowedColumns.Contains(column, StringComparer.Ordinal))
                        .Select(column => $"{column} IS NULL"));
            }

            var scenarioStepParameterSetConstraint =
                "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23)) " +
                $"AND (kind <> 1 OR (spawn_group_id IS NOT NULL AND {ScenarioStepNullColumns("spawn_group_id")})) " +
                $"AND (kind <> 2 OR (spawn_group_id IS NOT NULL AND {ScenarioStepNullColumns("spawn_group_id")})) " +
                $"AND (kind <> 3 OR ((((entity_class_id IS NOT NULL AND spawn_group_id IS NULL AND spawn_id IS NULL) " +
                    $"OR (entity_class_id IS NULL AND spawn_group_id IS NOT NULL AND spawn_id IS NOT NULL)) " +
                    $"AND {ScenarioStepNullColumns("entity_class_id", "spawn_group_id", "spawn_id")}))) " +
                $"AND (kind <> 4 OR ((((entity_class_id IS NOT NULL AND spawn_group_id IS NULL AND spawn_id IS NULL) " +
                    $"OR (entity_class_id IS NULL AND spawn_group_id IS NOT NULL AND spawn_id IS NOT NULL)) " +
                    $"AND {ScenarioStepNullColumns("entity_class_id", "spawn_group_id", "spawn_id")}))) " +
                $"AND (kind <> 5 OR (target_objective_id IS NOT NULL AND {ScenarioStepNullColumns("target_objective_id")})) " +
                $"AND (kind <> 6 OR (target_objective_id IS NOT NULL AND {ScenarioStepNullColumns("target_objective_id")})) " +
                $"AND (kind <> 7 OR (target_objective_id IS NOT NULL AND {ScenarioStepNullColumns("target_objective_id")})) " +
                $"AND (kind <> 8 OR (target_objective_id IS NOT NULL AND {ScenarioStepNullColumns("target_objective_id")})) " +
                $"AND (kind <> 9 OR (delay_milliseconds IS NOT NULL AND {ScenarioStepNullColumns("delay_milliseconds")})) " +
                $"AND (kind <> 10 OR ({ScenarioStepNullColumns()})) " +
                $"AND (kind <> 11 OR (reward_id IS NOT NULL AND {ScenarioStepNullColumns("reward_id")})) " +
                $"AND (kind <> 12 OR (skill_id IS NOT NULL AND ability_id IS NOT NULL AND skill_level IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("skill_id", "ability_id", "skill_level", "ability_slot")})) " +
                $"AND (kind <> 13 OR (tutorial_id IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("tutorial_id", "audio_set_id")})) " +
                $"AND (kind <> 14 OR (target_scenario_id IS NOT NULL AND delay_milliseconds IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("target_scenario_id", "delay_milliseconds")})) " +
                $"AND (kind <> 15 OR ((((target_scenario_id IS NOT NULL AND attempt_key IS NULL) " +
                    $"OR (target_scenario_id IS NULL AND attempt_key IS NOT NULL AND attempt_key <> '')) " +
                    $"AND {ScenarioStepNullColumns("target_scenario_id", "attempt_key")}))) " +
                $"AND (kind <> 16 OR (scenario_event_id IS NOT NULL AND {ScenarioStepNullColumns("scenario_event_id")})) " +
                $"AND (kind <> 17 OR (map_context_id IS NOT NULL AND pos_x IS NOT NULL AND pos_y IS NOT NULL " +
                    $"AND pos_z IS NOT NULL AND orientation IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("map_context_id", "pos_x", "pos_y", "pos_z", "orientation")})) " +
                $"AND (kind <> 18 OR (qualification_key IS NOT NULL AND qualification_value IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("qualification_key", "qualification_value")})) " +
                $"AND (kind <> 19 OR (account_skip_entitlement IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("account_skip_entitlement")})) " +
                $"AND (kind <> 20 OR (dynamic_object_key IS NOT NULL AND dynamic_object_key <> '' " +
                    $"AND entity_class_id IS NOT NULL AND pos_x IS NOT NULL AND pos_y IS NOT NULL AND pos_z IS NOT NULL AND orientation IS NOT NULL " +
                    $"AND {ScenarioStepNullColumns("dynamic_object_key", "entity_class_id", "pos_x", "pos_y", "pos_z", "orientation", "initial_interaction_enabled", "delay_milliseconds")})) " +
                $"AND (kind <> 21 OR (dynamic_object_key IS NOT NULL AND dynamic_object_key <> '' " +
                    $"AND {ScenarioStepNullColumns("dynamic_object_key")})) " +
                $"AND (kind <> 22 OR (spawn_group_id IS NOT NULL AND {ScenarioStepNullColumns("spawn_group_id")})) " +
                $"AND (kind <> 23 OR ({ScenarioStepNullColumns()}))";

            var scenarioStepNumericBoundsConstraint =
                "(target_objective_id IS NULL OR target_objective_id > 0) " +
                "AND (reward_id IS NULL OR reward_id > 0) " +
                "AND (spawn_group_id IS NULL OR spawn_group_id > 0) " +
                "AND (spawn_id IS NULL OR spawn_id > 0) " +
                "AND (entity_class_id IS NULL OR entity_class_id > 0) " +
                "AND (target_scenario_id IS NULL OR target_scenario_id > 0) " +
                $"AND (delay_milliseconds IS NULL OR (delay_milliseconds >= 1 AND delay_milliseconds <= {MissionScenarioStepEntry.MaxDelayMilliseconds})) " +
                "AND (skill_id IS NULL OR skill_id > 0) " +
                $"AND (ability_id IS NULL OR (ability_id >= 1 AND ability_id <= {MissionScenarioStepEntry.MaxAbilityId})) " +
                $"AND (skill_level IS NULL OR (skill_level >= 1 AND skill_level <= {MissionScenarioStepEntry.MaxSkillLevel})) " +
                $"AND (ability_slot IS NULL OR ability_slot <= {MissionScenarioStepEntry.MaxAbilitySlot}) " +
                "AND (tutorial_id IS NULL OR tutorial_id > 0) " +
                "AND (audio_set_id IS NULL OR audio_set_id > 0) " +
                "AND (attempt_key IS NULL OR attempt_key <> '') " +
                "AND (dynamic_object_key IS NULL OR dynamic_object_key <> '') " +
                "AND (scenario_event_id IS NULL OR scenario_event_id > 0) " +
                "AND (map_context_id IS NULL OR map_context_id > 0) " +
                $"AND (qualification_key IS NULL OR (qualification_key >= {MissionScenarioStepEntry.MinimumQualificationKey} AND qualification_key <= {MissionScenarioStepEntry.MaximumQualificationKey})) " +
                $"AND (qualification_value IS NULL OR (qualification_value >= {MissionScenarioStepEntry.RemovedQualificationValue} AND qualification_value <= {MissionScenarioStepEntry.GrantedQualificationValue}))";

            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision });
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.Enabled).HasColumnName("enabled").HasDefaultValue(false);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.MissionId)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.AbandonmentPolicy)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.ClientNameTextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.GiverId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.ReceiverId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.Level)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.GroupType)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionContentDefinitionEntry>()
                .Property(entry => entry.CategoryId)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.PrerequisiteId });
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Prerequisites)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.PrerequisiteId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.Kind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.RequiredMissionId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.RequiredMissionState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.RequiredLevel)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.PlayerFlagId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionPrerequisiteEntry>()
                .Property(entry => entry.PlayerFlagValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ObjectiveId });
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Objectives)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ObjectiveId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ClientNameTextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ClientBodyTextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ClientCounter0TextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ClientCounter1TextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.ClientCounter2TextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.Ordinal)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveDefinitionEntry>()
                .Property(entry => entry.InitialState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .HasKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.TransitionId
                });
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .HasOne(entry => entry.Objective)
                .WithMany(objective => objective.Transitions)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ObjectiveId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .Property(entry => entry.TransitionId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .Property(entry => entry.Sequence)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .Property(entry => entry.FromState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionObjectiveTransitionEntry>()
                .Property(entry => entry.ToState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionTriggerEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_mission_trigger_kind_parameter_set",
                    triggerParameterSetConstraint))
                .HasKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.TransitionId,
                    entry.TriggerId
                });
            modelBuilder.Entity<MissionTriggerEntry>()
                .HasOne(entry => entry.Transition)
                .WithMany(transition => transition.Triggers)
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.TransitionId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionTriggerEntry>()
                .HasOne<MissionObjectiveDefinitionEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.RelatedObjectiveId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionTriggerEntry>()
                .HasOne<MissionAreaEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.AreaId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.AreaId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.TriggerId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.Kind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.Sequence)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.RelatedObjectiveId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.RelatedState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.EventKind)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.SubjectId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.CounterId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.InitialValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.TargetValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.AreaId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.DurationSeconds)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.NpcPackageId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionTriggerEntry>()
                .Property(entry => entry.PlayerFlagId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.ItemIntentJson).HasColumnName("item_intent");
            modelBuilder.Entity<MissionActionEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_mission_action_kind_parameter_set",
                    actionParameterSetConstraint))
                .HasKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.TransitionId,
                    entry.ActionId
                });
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne(entry => entry.Transition)
                .WithMany(transition => transition.Actions)
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.TransitionId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne<MissionObjectiveDefinitionEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.TargetObjectiveId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne<MissionRewardDefinitionEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.RewardId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.RewardId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne<MissionSpawnGroupEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.SpawnGroupId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.SpawnGroupId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne<MissionScenarioEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ScenarioId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ScenarioId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .HasOne<MissionIndicatorEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.IndicatorId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.ObjectiveId,
                    entry.IndicatorId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.ActionId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.Kind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.Sequence)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.TargetObjectiveId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.ObjectiveState)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.RewardId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.SpawnGroupId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.ScenarioId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.IndicatorId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.PlayerFlagId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.PlayerFlagValue)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionActionEntry>()
                .Property(entry => entry.NpcPackageId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.RewardId });
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Rewards)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_mission_reward_definition_selection_count",
                    rewardSelectionCountConstraint));
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.RewardId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.Experience)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.Credits)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.Prestige)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardDefinitionEntry>()
                .Property(entry => entry.SelectionCount)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionRewardItemEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.RewardId, entry.ItemId });
            modelBuilder.Entity<MissionRewardItemEntry>()
                .HasOne(entry => entry.Reward)
                .WithMany(reward => reward.Items)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.RewardId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MissionRewardItemEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_mission_reward_item_kind",
                    rewardItemKindConstraint));
            modelBuilder.Entity<MissionRewardItemEntry>()
                .Property(entry => entry.ItemId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardItemEntry>()
                .Property(entry => entry.Kind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionRewardItemEntry>()
                .Property(entry => entry.ItemTemplateId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionRewardItemEntry>()
                .Property(entry => entry.Quantity)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionIndicatorEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ObjectiveId, entry.IndicatorId });
            modelBuilder.Entity<MissionIndicatorEntry>()
                .HasOne(entry => entry.Objective)
                .WithMany(objective => objective.Indicators)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ObjectiveId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionIndicatorEntry>()
                .Property(entry => entry.IndicatorId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionIndicatorEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionAreaEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.AreaId });
            modelBuilder.Entity<MissionAreaEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Areas)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionAreaEntry>()
                .Property(entry => entry.AreaId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionAreaEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionAreaEntry>()
                .Property(entry => entry.MapContextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionAreaEntry>()
                .Property(entry => entry.Shape)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.SpawnGroupId });
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.SpawnGroups)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .HasOne<MissionAreaEntry>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.AreaId
                })
                .HasPrincipalKey(entry => new
                {
                    entry.MissionId,
                    entry.ContentRevision,
                    entry.AreaId
                })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.SpawnGroupId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.AreaId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.MapContextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.SpawnPolicy)
                .HasDefaultValue(MissionSpawnGroupPolicy.OrdinaryRespawn)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionSpawnGroupEntry>()
                .Property(entry => entry.RespawnSeconds)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionSpawnEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.SpawnGroupId, entry.SpawnId });
            modelBuilder.Entity<MissionSpawnEntry>()
                .HasOne(entry => entry.SpawnGroup)
                .WithMany(group => group.Spawns)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.SpawnGroupId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MissionSpawnEntry>()
                .Property(entry => entry.SpawnId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionSpawnEntry>()
                .Property(entry => entry.CreatureId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionSpawnEntry>()
                .Property(entry => entry.Quantity)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);

            modelBuilder.Entity<MissionScenarioEntry>()
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ScenarioId });
            modelBuilder.Entity<MissionScenarioEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Scenarios)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioEntry>()
                .Property(entry => entry.ScenarioId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioEntry>()
                .Property(entry => entry.StartPolicy)
                .HasDefaultValue(MissionScenarioStartPolicy.Automatic)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionScenarioStepEntry>()
                .ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_mission_scenario_step_kind_parameter_set",
                        scenarioStepParameterSetConstraint);
                    table.HasCheckConstraint(
                        "CK_mission_scenario_step_numeric_bounds",
                        scenarioStepNumericBoundsConstraint);
                })
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ScenarioId, entry.StepId });
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne(entry => entry.Scenario)
                .WithMany(scenario => scenario.Steps)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.ScenarioId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne<MissionObjectiveDefinitionEntry>()
                .WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.TargetObjectiveId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne<MissionRewardDefinitionEntry>()
                .WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.RewardId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne<MissionSpawnGroupEntry>()
                .WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.SpawnGroupId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne<MissionSpawnEntry>()
                .WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.SpawnGroupId, entry.SpawnId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .HasOne<MissionScenarioEntry>()
                .WithMany()
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision, entry.TargetScenarioId })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.StepId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.Requirement)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.Kind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.Sequence)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.TargetObjectiveId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.RewardId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.SpawnGroupId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.SpawnId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.DynamicObjectKey)
                .HasMaxLength(64);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.EntityClassId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.TargetScenarioId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.DelayMilliseconds)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.SkillId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.AbilityId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.SkillLevel)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.AbilitySlot)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.TutorialId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.AudioSetId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.ScenarioEventId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.MapContextId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.QualificationKey)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionScenarioStepEntry>()
                .Property(entry => entry.QualificationValue)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<MissionEvidenceEntry>()
                .ToTable(table => table.HasCheckConstraint(
                    "CK_mission_evidence_source_location",
                    "source_uri IS NOT NULL OR local_client_path IS NOT NULL"))
                .HasKey(entry => new { entry.MissionId, entry.ContentRevision, entry.EvidenceId });
            modelBuilder.Entity<MissionEvidenceEntry>()
                .HasOne(entry => entry.Content)
                .WithMany(content => content.Evidence)
                .HasForeignKey(entry => new { entry.MissionId, entry.ContentRevision })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MissionEvidenceEntry>()
                .Property(entry => entry.EvidenceId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionEvidenceEntry>()
                .Property(entry => entry.OwnerKind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionEvidenceEntry>()
                .Property(entry => entry.OwnerId)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
            modelBuilder.Entity<MissionEvidenceEntry>()
                .Property(entry => entry.SourceKind)
                .HasConversion<byte>()
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);
            modelBuilder.Entity<MissionEvidenceEntry>()
                .Property(entry => entry.Confidence)
                .AsUnsignedDouble(_dbContextPropertyModifier);
        }
    }
}
