extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Rasa.Missions.Content;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Rasa.Test.Missions
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context;
    using Context.World;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Game.Handlers;
    using Rasa.Game.Missions.Content;
    using Rasa.Packets;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Services.DbContext;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.Interfaces;
    using Rasa.Structures.World;

    internal static class BootcampRuntimeTestHarness
    {
        internal const uint BootcampMapContextId = 1985;
        internal const uint WildernessMapContextId = 1220;
        internal const uint MissionInitiation = 1990;
        internal const uint MissionGearingUp = 1992;

        internal const uint MajorMcAllisterCreatureId = 510203;
        internal const uint CaptainDelessioCreatureId = 510204;
        internal const uint CorporalHartmannCreatureId = 510205;
        internal const uint CorporalDeSimoneCreatureId = 510206;
        internal const uint CaptainYoungbloodCreatureId = 510207;
        internal const uint WoundedSurvivorCreatureId = 510208;
        internal const uint CorporalVanValkenbergCreatureId = 510209;
        internal const uint TizzikGiCreatureId = 510210;
        internal const uint PracticeDummyCreatureId = 510211;
        internal const uint LightningDummyCreatureId = 510212;

        internal const uint CaptainDelessioPackageId = 2560;
        internal const uint CaptainYoungbloodPackageId = 2561;
        internal const uint CorporalHartmannPackageId = 2563;
        internal const uint CorporalDeSimonePackageId = 2562;
        internal const uint WoundedSurvivorPackageId = 2584;
        internal const uint CorporalVanValkenbergPackageId = 2564;
        private const uint FreshPendingAccountId = 2;
        private const uint FreshPendingCharacterId = 2;
        private const byte FreshPendingSlot = 1;

        internal static Harness Create(bool useWorldContent = false, Action<MapChannelManager> initializeMaps = null,
            Action<IDictionary<uint, MissionSceneDefinition>> configureScenes = null)
        {
            var bootstrap = CreateBootstrap(useWorldContent, configureScenes);
            initializeMaps?.Invoke(bootstrap.Maps);
            ConfigureRuntimePlayer(bootstrap.Context.Client);
            var bootcampMap = bootstrap.Maps.GetOrCreatePrivateInstance(
                BootcampMapContextId,
                bootstrap.Context.Client.Player.Id);
            AttachClientToMap(bootstrap.Context.Client, bootcampMap);
            return new Harness(
                bootstrap.Context,
                bootstrap.WorldContext,
                bootstrap.Manager,
                bootstrap.Maps,
                bootcampMap,
                bootstrap.Singletons,
                () => bootstrap.Clock.UtcNow,
                value => bootstrap.Clock.UtcNow = value,
                bootstrap.Context.Client,
                useWorldContent);
        }

        internal static Harness CreateFromPendingSelection(bool useWorldContent = false, bool accountEntitled = false)
        {
            var bootstrap = CreateBootstrap(useWorldContent);
            SeedFreshPendingCharacter(bootstrap.Context);
            using (var unit = bootstrap.Context.CreateChar())
                unit.GameAccounts.UpdateCanSkipBootcamp(FreshPendingAccountId, accountEntitled);
            var client = CreateSelectionClient(bootstrap.Context, FreshPendingAccountId);

            bootstrap.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = FreshPendingSlot,
                    SkipBootcamp = false
                });

            ConfigureRuntimePlayer(client);
            client.Player.MapChannel.QueuedClients.Clear();
            client.Player.MapChannel.ClientList.Add(client);
            RouteMapLoaded(client);

            return new Harness(
                bootstrap.Context,
                bootstrap.WorldContext,
                bootstrap.Manager,
                bootstrap.Maps,
                client.Player.MapChannel,
                bootstrap.Singletons,
                () => bootstrap.Clock.UtcNow,
                value => bootstrap.Clock.UtcNow = value,
                client,
                useWorldContent);
        }

        internal static void AssertObjectiveStates(
            MissionLog mission,
            params (uint ObjectiveId, MissionObjectiveState State)[] expected)
        {
            var actual = expected
                .Select(entry => (entry.ObjectiveId, mission.Objectives[entry.ObjectiveId].State))
                .ToArray();
            Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert.AreEqual(expected, actual);
        }

        internal static Creature FindNpcByPackage(MapChannel map, uint npcPackageId) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .SingleOrDefault(creature => creature.Npc?.NpcPackageId == npcPackageId);

        internal static Creature FindCreature(MapChannel map, uint creatureDbId) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .SingleOrDefault(creature => creature.DbId == creatureDbId);

        internal static DynamicObject FindScenarioObject(MapChannel map, string key) =>
            map.DynamicObjects.SingleOrDefault(dynamicObject =>
                dynamicObject.SceneActorRole == key ||
                string.Equals(dynamicObject.ScenarioKey, key, StringComparison.Ordinal) ||
                dynamicObject.ScenarioKey?.EndsWith($":object:{key}", StringComparison.Ordinal) == true);

        /// <summary>
        /// Opens and fully loots the Gearing Up equipment crate, the real path by which
        /// objective 1 completes and its reward package reaches the player's inventory since
        /// the crate became a loot dispenser (see BootcampCrateLoot migrations) instead of an
        /// instant scenario grant. Callers must have already completed objective 4 so the
        /// crate has been spawned.
        /// </summary>
        internal static void LootEquipmentCrate(Harness harness)
        {
            var crate = FindScenarioObject(harness.BootcampMap, "bootcamp-equipment-crate");
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(crate);
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var loot = harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId];
            LootDispenserManager.Instance.RequestCorpseLooting(
                harness.Client,
                new Rasa.Packets.LootDispenser.Client.RequestCorpseLootingPacket { EntityId = loot.EntityId });
            LootDispenserManager.Instance.RequestLootAllFromCorpse(
                harness.Client,
                new Rasa.Packets.LootDispenser.Client.RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
        }

        internal static void AdvanceScenarioCorpseAndRespawn(
            Harness harness,
            Creature creature,
            long corpseMilliseconds,
            long respawnMilliseconds)
        {
            if (LootDispenserManager.Instance.AdvanceCorpseLifetime(
                    harness.BootcampMap,
                    creature,
                    corpseMilliseconds))
            {
                CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, creature);
            }

            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, respawnMilliseconds);
        }

        internal static void PrepareDirectDamageClient(Client client)
        {
            client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] =
                new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            client.Player.Attributes[Attributes.Power] =
                new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
        }

        internal static ActionLevelInfo LightningInfo(int primaryDamage)
        {
            var info = new ActionLevelInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Level = 1,
                MaxRange = 20
            };
            info.Properties[AbilityProperty.DamageAmountMin] = primaryDamage;
            info.Properties[AbilityProperty.DamageAmountMax] = primaryDamage;
            info.Properties[AbilityProperty.RadiusAroundTarget] = 1;
            return info;
        }

        internal static ActionInfo LightningAction(ActionLevelInfo info)
        {
            var action = new ActionInfo
            {
                ActionId = ActionId.AaRecruitLightning,
                Module = "abilities.lightning"
            };
            action.Levels[info.Level] = info;
            return action;
        }

        internal static void InvokeResolveDirectDamage(
            AbilityManager manager,
            MapChannel map,
            Client client,
            ActionInfo actionInfo,
            ActionLevelInfo info,
            ActionData action)
        {
            var method = typeof(AbilityManager).GetMethod(
                "ResolveDirectDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var arguments = new List<object>
            {
                map,
                client,
                client.Player,
                actionInfo,
                info,
                action
            };
            if (method!.GetParameters().Length == 7)
                arguments.Add(null);
            method.Invoke(manager, arguments.ToArray());
        }

        private static RasaDbContextBase CreateContext(Type contextType, string database)
        {
            var connection = new DatabaseConnectionConfiguration { Database = database };
            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = "Sqlite",
                Auth = connection,
                Char = connection,
                World = connection
            });
            return (RasaDbContextBase)Activator.CreateInstance(
                contextType,
                options,
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier())!;
        }

        private static void PrepareBootcampScenarioClasses(SqliteWorldContext world)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            foreach (var entityClassId in new uint[]
            {
                24911, 24990, 7862, 29877, 29365,
                (uint)EntityClasses.UsableTwoStateHumDropshipBeam,
                (uint)EntityClasses.UsableCrSpawnerHumDropshipV01
            })
                if (!classes.ContainsKey((EntityClasses)entityClassId))
                    classes.Add((EntityClasses)entityClassId, new EntityClass(
                        entityClassId,
                        $"scenario_object_{entityClassId}",
                        0,
                        0,
                        new List<AugmentationType>(),
                        true));

            foreach (var entry in world.EntityClassEntries.AsNoTracking()
                .Where(entry => entry.Id == 24586 || entry.Id == 21081))
                classes[(EntityClasses)entry.Id] = new EntityClass(
                    entry.Id, entry.ClassName, entry.MeshId, entry.ClassCollisionRole,
                    entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(),
                    entry.TargetFlag != 0);

            if (!classes.TryGetValue((EntityClasses)4001, out var creatureClass))
            {
                creatureClass = new EntityClass(4001, "scenario_creature", 0, 0, new List<AugmentationType>(), true);
                classes.Add((EntityClasses)4001, creatureClass);
            }

            if (!creatureClass.Augmentations.Contains(AugmentationType.Creature))
                creatureClass.Augmentations.Add(AugmentationType.Creature);
        }

        private static void PrepareBootcampRewardTemplates(MissionTestContext context)
        {
            context.AddRewardTemplate(28, 3147);
            ConfigureRewardTemplate(context, 13066, 15542, (EquipmentData)2);
            ConfigureRewardTemplate(context, 13096, 15572, (EquipmentData)3);
            ConfigureRewardTemplate(context, 13156, 15632, (EquipmentData)16);
            ConfigureRewardTemplate(context, 13186, 15662, (EquipmentData)15);
            ConfigureRewardTemplate(context, 13713, 27220, (EquipmentData)13);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)27220];
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 27220,
                WeaponTemplatId = 13713,
                AttackActionId = 1,
                AttackActionArgId = 133,
                DrawActionId = 1,
                StowActionId = 1,
                ReloadActionId = 1,
                AmmoClassId = 3147,
                ClipSize = 20,
                MinDamage = 55,
                MaxDamage = 55,
                DamageType = 1,
                WeaponAnimConditionCode = 1
            });
            weaponClass.ItemTemplates[13713].WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
            {
                Id = 13713,
                AmmoPerShot = 1,
                Refire = 800,
                ReloadTime = 1500,
                Windup = 800,
                Recovery = 1,
                Range = 80,
                ToolType = 15,
                AttackType = 2
            });
        }

        private static void ConfigureRewardTemplate(
            MissionTestContext context,
            uint templateId,
            uint classId,
            EquipmentData equipmentSlot)
        {
            context.AddRewardTemplate(templateId, classId);
            var entityClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
            entityClass.EquipableClassInfo = new EquipableClassInfo(equipmentSlot);
            if (equipmentSlot != EquipmentData.Weapon)
                entityClass.ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry());
            entityClass.ItemTemplates[templateId].InventoryCategory = InventoryCategory.Equipment;
        }

        private static void ConfigureRuntimePlayer(Client client)
        {
            client.Player.MapContextId = BootcampMapContextId;
            client.Player.Class = (uint)CharacterClass.Recruit;
            client.Player.AppearanceData ??= new Dictionary<EquipmentData, AppearanceData>();
            client.Player.Attributes[Attributes.Body] =
                new ActorAttributes(Attributes.Body, 10, 10, 10, 0, 0);
            client.Player.Attributes[Attributes.Mind] =
                new ActorAttributes(Attributes.Mind, 10, 10, 10, 0, 0);
            client.Player.Attributes[Attributes.Spirit] =
                new ActorAttributes(Attributes.Spirit, 10, 10, 10, 0, 0);
            client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Chi] =
                new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Power] =
                new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Regen] =
                new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
            client.Player.Attributes[Attributes.Armor] =
                new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
        }

        private static void AttachClientToMap(Client client, MapChannel destination)
        {
            if (client.Player.MapChannel != null)
            {
                CellManager.Instance.RemoveFromWorld(client);
                client.Player.MapChannel.ClientList.Remove(client);
            }
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            client.State = RasaGame::Rasa.Data.ClientState.Ingame;
            MissionApplication.Instance.Scenes.Resume(client);
        }

        private static Bootstrap CreateBootstrap(bool useWorldContent,
            Action<IDictionary<uint, MissionSceneDefinition>> configureScenes = null)
        {
            var databaseDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(databaseDirectory);
            var worldDatabase = Path.Combine(databaseDirectory, "world");
            var worldContext = (SqliteWorldContext)CreateContext(typeof(SqliteWorldContext), worldDatabase);
            worldContext.Initialize();
            Content.MissionContentTestSupport.ConfigureScenes(worldContext, configureScenes);

            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
            PrepareBootcampScenarioClasses(worldContext);
            PrepareBootcampRewardTemplates(context);

            var factory = new RuntimeLoadingFactory(context, worldContext);
            MissionApplication manager = null;
            CharacterManager charactersManager = null;
            var clock = new ClockState(new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc));
            var creatures = new CreatureManager(factory, new ManifestationManager(context));
            foreach (var creatureId in new[]
                     {
                         39U,
                         50U,
                         CaptainYoungbloodCreatureId,
                         WoundedSurvivorCreatureId,
                         CorporalVanValkenbergCreatureId,
                         TizzikGiCreatureId,
                         PracticeDummyCreatureId,
                         LightningDummyCreatureId,
                         510213U, 510214U, 510215U, 510227U, 510228U
                     })
            {
                creatures.LoadedCreatures[creatureId] = new Creature
                {
                    DbId = creatureId,
                    Faction = creatureId == 510227 ? Factions.AFS : Factions.Bane,
                    EntityClass = (EntityClasses)4001,
                    Npc = new Npc
                    {
                        NpcPackageId = creatureId == CaptainYoungbloodCreatureId
                            ? CaptainYoungbloodPackageId
                            : creatureId == WoundedSurvivorCreatureId
                                ? WoundedSurvivorPackageId
                            : creatureId == CorporalVanValkenbergCreatureId
                                ? CorporalVanValkenbergPackageId
                                : creatureId
                    },
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                };
            }

            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(factory, maps);
            var manifestation = new ManifestationManager(context);
            var deadlineService = new MissionDeadlineService(
                () => factory,
                () => manager,
                () => clock.UtcNow);
            var scenarioService = new MissionSceneHost(
                () => factory,
                () => manager,
                manifestation,
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => clock.UtcNow);
            maps = new MapChannelManager(
                factory,
                updateCharacter: (client, update, value) =>
                    charactersManager.UpdateCharacter(client, update, value),
                refreshStats: (_, _) => { },
                assignPlayer: _ => { },
                enterMapChannels: _ => { },
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: scenarioService);
            maps.MapChannelArray.Add(BootcampMapContextId, CreatePublicMap(BootcampMapContextId, "bootcamp_runtime"));
            maps.MapChannelArray.Add(WildernessMapContextId, CreatePublicMap(WildernessMapContextId, "alia_das_fixture"));
            objects = new DynamicObjectManager(factory, maps);
            manager = new MissionApplication(
                factory,
                new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>(),
                manifestation,
                deadlineService: deadlineService,
                scenarioService: scenarioService,
                utcNow: () => clock.UtcNow);
            var inventory = new InventoryManager(factory, manager);
            var auction = new AuctionHouseManager(factory, manager);
            var clan = Activator.CreateInstance(
                typeof(ClanManager),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { factory },
                culture: null);
            var social = Activator.CreateInstance(
                typeof(SocialManager),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { factory },
                culture: null);
            charactersManager = new CharacterManager(context, manager);
            objects = new DynamicObjectManager(
                factory,
                maps,
                missionManager: manager,
                characterManager: charactersManager);
            var report = manager.LoadMissions();
            if (report.BlocksReadiness)
                throw new InvalidOperationException(
                    string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            var singletons = new ManagerInstances(
                maps,
                objects,
                creatures,
                manager,
                inventory,
                manifestation,
                clan,
                auction,
                social,
                factory);
            LoadBootcampLootContent(worldContext);
            LoadBootcampNav(maps);
            if (useWorldContent)
                LoadBootcampWorldContent(worldContext, creatures, manager, maps);
            objects.InitTeleporters();

            return new Bootstrap(
                context,
                worldContext,
                manager,
                charactersManager,
                maps,
                singletons,
                clock);
        }

        private static void LoadBootcampLootContent(SqliteWorldContext world)
        {
            var templateIds = new uint[] { 41666, 28, 56, 44917, 41665, 11519 };
            foreach (var link in world.Set<ItemTemplateItemClassEntry>().AsNoTracking()
                         .Where(row => templateIds.Contains(row.ItemTemplateId)))
            {
                var data = world.Set<ItemTemplateEntry>().AsNoTracking().Single(row => row.Id == link.ItemTemplateId);
                var entry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == link.ItemClass);
                var classId = (EntityClasses)entry.Id;
                EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous);
                var entityClass = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                    entry.ClassCollisionRole, entry.AugList.Split(',')
                        .Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0)
                {
                    ItemClassInfo = new ItemClassInfo(world.Set<ItemClassEntry>().AsNoTracking()
                        .Single(row => row.Id == link.ItemClass)),
                    ItemTemplates = previous == null ? new Dictionary<uint, ItemTemplate>() :
                        new Dictionary<uint, ItemTemplate>(previous.ItemTemplates)
                };
                entityClass.ItemTemplates[link.ItemTemplateId] = new ItemTemplate(link)
                {
                    QualityId = data.QualityId,
                    InventoryCategory = (InventoryCategory)data.InventoryCategory,
                    HasSellableFlag = data.HasSellableFlag != 0,
                    BuyPrice = data.BuyPrice,
                    SellPrice = data.SellPrice
                };
                EntityClassManager.Instance.LoadedEntityClasses[classId] = entityClass;
                ItemManager.Instance.ItemTemplateItemClass[link.ItemTemplateId] = classId;
            }
        }

        private static void LoadBootcampWorldContent(
            SqliteWorldContext world, CreatureManager creatures, MissionApplication missions, MapChannelManager maps)
        {
            var staticSpawns = world.SpawnPoolEntries.AsNoTracking()
                .Where(pool => pool.MapContextId == BootcampMapContextId).ToArray();
            var ids = staticSpawns.SelectMany(pool => new[]
                {
                    pool.Creature1Id, pool.Creature2Id, pool.Creature3Id,
                    pool.Creature4Id, pool.Creature5Id, pool.Creature6Id
                })
                .Concat(world.MissionSpawnEntries.AsNoTracking()
                    .Where(spawn => spawn.MissionId == 1994 || spawn.MissionId == 1995 || spawn.MissionId == 2005)
                    .Select(spawn => spawn.CreatureId))
                .Where(id => id != 0).Distinct().ToArray();
            var actions = world.Set<CreatureActionEntry>().AsNoTracking().ToDictionary(action => action.Id);
            foreach (var entry in world.Set<CreatureEntry>().AsNoTracking().Where(entry => ids.Contains(entry.Id)))
            {
                var classEntry = world.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
                var augmentations = classEntry.AugList.Split(',')
                    .Select(value => (AugmentationType)uint.Parse(value)).ToList();
                EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] =
                    new EntityClass(entry.ClassId, classEntry.ClassName, classEntry.MeshId,
                        classEntry.ClassCollisionRole, augmentations, classEntry.TargetFlag != 0)
                    {
                        CreatureFlags = world.Set<CreatureClassFlagEntry>().AsNoTracking()
                            .Where(flag => flag.ClassId == entry.ClassId)
                            .Select(flag => (CreatureFlag)flag.FlagId).ToList()
                    };
                var creature = new Creature(entry)
                {
                    Npc = augmentations.Contains(AugmentationType.NPC)
                        ? new Npc
                        {
                            NpcPackageId = world.Set<NpcPackageEntry>().AsNoTracking()
                                .Where(package => package.Id == entry.Id)
                                .Select(package => package.PackageId).SingleOrDefault(),
                            NpcMissionIds = missions.LoadedMissions.Values
                                .Where(mission => mission.MissionGiver == entry.Id || mission.MissionReciver == entry.Id)
                                .Select(mission => mission.MissionId).ToList()
                        }
                        : null,
                    AppearanceData = world.Set<CreatureAppearanceEntry>().AsNoTracking()
                        .Where(appearance => appearance.Id == entry.Id)
                        .ToDictionary(appearance => (EquipmentData)appearance.SlotId,
                            appearance => new AppearanceData
                            {
                                SlotId = (EquipmentData)appearance.SlotId,
                                Class = appearance.ClassId,
                                Color = new Color(appearance.Color),
                                Hue2 = new Color(2139062144)
                            })
                };
                foreach (var actionId in new[]
                         {
                             entry.Action1, entry.Action2, entry.Action3, entry.Action4,
                             entry.Action5, entry.Action6, entry.Action7, entry.Action8
                         }.Where(id => id != 0))
                    creature.Actions.Add(new CreatureAction(actions[actionId]));
                creatures.LoadedCreatures[entry.Id] = creature;
            }

            SpawnPoolManager.Instance.SpawnPoolInit();
        }

        private static void LoadBootcampNav(MapChannelManager maps)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln")))
                root = root.Parent;
            if (root == null)
                throw new DirectoryNotFoundException("Repository root not found.");
            maps.MapChannelArray[BootcampMapContextId].NavMesh = new NavMeshQuery(NavMeshFile.Read(
                NavMeshFile.PathFor(Path.Combine(root.FullName, "navmesh"), "adv_bootcamp")));
        }

        private static void SeedFreshPendingCharacter(MissionTestContext context)
        {
            context.SeedCharacter(
                FreshPendingAccountId, FreshPendingSlot, FreshPendingCharacterId, (byte)Race.Human);
            using var unit = context.CreateChar();
            unit.CharacterStartingExperience.Add(
                new CharacterStartingExperienceEntry(
                    FreshPendingCharacterId,
                    "deployment_11",
                    CharacterStartingExperienceState.Pending));
        }

        private static Client CreateSelectionClient(MissionTestContext context, uint accountId)
        {
            using var unit = context.CreateChar();
            var client = new Client(context, new ClientPacketHandler())
            {
                State = RasaGame::Rasa.Data.ClientState.CharacterSelection
            };
            typeof(Client)
                .GetMethod("LoadGameAccountEntry", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(client, new object[] { unit, accountId });
            return client;
        }

        private static void RouteMapLoaded(Client client)
        {
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            new PacketRouter<ClientPacketHandler, GameOpcode>()
                .RoutePacket(handler, new MapLoadedPacket());
        }

        private static MapChannel CreatePublicMap(uint contextId, string name) => new()
        {
            MapInfo = new MapInfo(contextId, name, 1556, 0),
            ClientList = new List<Client>(),
            PlayerLimit = 128
        };

        internal sealed class Harness : IDisposable
        {
            private readonly Func<DateTime> _getUtcNow;
            private readonly Action<DateTime> _setUtcNow;
            private ManagerInstances _singletons;
            private readonly List<(uint CreatureId, uint? PackageId, Vector3 Position)> _npcs = new();
            private readonly bool _useWorldContent;

            internal Harness(
                MissionTestContext context,
                SqliteWorldContext worldContext,
                MissionApplication manager,
                MapChannelManager maps,
                MapChannel bootcampMap,
                IDisposable singletons,
                Func<DateTime> getUtcNow,
                Action<DateTime> setUtcNow,
                Client client,
                bool useWorldContent)
            {
                Context = context;
                WorldContext = worldContext;
                Manager = manager;
                Maps = maps;
                BootcampMap = bootcampMap;
                Client = client;
                _singletons = (ManagerInstances)singletons;
                _getUtcNow = getUtcNow;
                _setUtcNow = setUtcNow;
                _useWorldContent = useWorldContent;
            }

            internal MissionTestContext Context { get; }
            internal SqliteWorldContext WorldContext { get; }
            internal MissionApplication Manager { get; private set; }
            internal MapChannelManager Maps { get; private set; }
            internal Client Client { get; private set; }
            internal MapChannel BootcampMap { get; private set; }

            internal DateTime UtcNow
            {
                get => _getUtcNow();
                set => _setUtcNow(value);
            }

            internal IReadOnlyList<PythonPacket> Drain() =>
                MissionTestContext.Drain(Client);

            internal void ReconnectFromSelection()
            {
                var accountId = Client.AccountEntry.Id;
                var slot = Client.AccountEntry.SelectedSlot;
                var characterId = Client.Player.Id;
                DetachClientFromCurrentMap(Client);
                Maps.ReleaseOwnedPrivateInstances(characterId);
                Client = CreateSelectionClient(Context, accountId);
                new CharacterManager(Context, Manager).RequestSwitchToCharacterInSlot(
                    Client,
                    new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = slot });
                ConfigureRuntimePlayer(Client);
                RouteMapLoaded();
            }

            internal void RouteMapLoaded()
            {
                BootcampRuntimeTestHarness.RouteMapLoaded(Client);
                if (Client.Player?.MapChannel?.MapInfo?.MapContextId == BootcampMapContextId)
                    BootcampMap = Client.Player.MapChannel;
            }

            internal void SeedMission(uint characterId, uint missionId, uint state, bool completeable)
            {
                using (var unit = Context.CreateChar())
                {
                    unit.ExecuteTransaction(() =>
                    {
                        unit.CharacterMissions.Add(new CharacterMissionEntry(characterId, missionId, state)
                        {
                            Completeable = completeable
                        });
                        if (Manager.LoadedMissions.TryGetValue(missionId, out var definition) &&
                            definition.IsOperational)
                        {
                            unit.CharacterMissionProgress.AddObjectives(
                                definition.Objectives.Values.Select(objective =>
                                {
                                    var row = new CharacterMissionObjectiveEntry(
                                        characterId,
                                        missionId,
                                        objective.ObjectiveId,
                                        (byte)(completeable && objective.IsRequired.Value
                                            ? MissionObjectiveState.Completed
                                            : objective.InitialState.Value));
                                    foreach (var counter in objective.Counters)
                                        row.Counters.Add(new CharacterMissionObjectiveCounterEntry(
                                            characterId,
                                            missionId,
                                            objective.ObjectiveId,
                                            counter.Key,
                                            counter.Value.InitialValue));
                                    foreach (var counter in objective.ItemCounters)
                                        row.ItemCounters.Add(new CharacterMissionObjectiveItemCounterEntry(
                                            characterId,
                                            missionId,
                                            objective.ObjectiveId,
                                            counter.Key,
                                            counter.Value.InitialValue));
                                    return row;
                                }));
                        }

                    });
                }

                using var reload = Context.CreateChar();
                Manager.Hydrate(
                    Client.Player,
                    reload.CharacterMissions.Get(Client.Player.Id),
                    reload.CharacterMissionProgress.Get(Client.Player.Id));
            }

            internal Creature AddNpc(uint dbId, uint? npcPackageId = null, Vector3? position = null)
            {
                _npcs.Add((dbId, npcPackageId, position ?? Vector3.Zero));
                var npc = Context.AddNpc(dbId, BootcampMap, npcPackageId, position);
                BindNpcSpawn(npc);
                npc.AppearanceData ??= new Dictionary<EquipmentData, AppearanceData>();
                if (npc.Attributes.Count == 0)
                {
                    npc.State = CharacterState.Normal;
                    npc.Attributes[Attributes.Body] = new ActorAttributes(Attributes.Body, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Mind] = new ActorAttributes(Attributes.Mind, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Spirit] = new ActorAttributes(Attributes.Spirit, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                    npc.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Aware] = new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Speed] = new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0);
                    npc.Attributes[Attributes.Regen] = new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
                }
                return npc;
            }

            internal void Reconnect()
            {
                var characterId = Client.Player.Id;
                Maps.ReleaseOwnedPrivateInstances(characterId);
                var rebuilt = Maps.GetOrCreatePrivateInstance(BootcampMapContextId, characterId);
                BootcampMap = rebuilt;
                foreach (var npc in _npcs)
                    AddNpcToCurrentMap(npc.CreatureId, npc.PackageId, npc.Position);
                AttachClientToMap(Client, BootcampMap);
            }

            internal void ReconnectFresh(bool drainPackets = true)
            {
                var characterId = Client.Player.Id;
                var accountEntry = CloneAccountEntry(Client.AccountEntry);
                DetachClientFromCurrentMap(Client);
                Maps.ReleaseOwnedPrivateInstances(characterId);

                var factory = new RuntimeLoadingFactory(Context, WorldContext);
                var manifestation = new ManifestationManager(Context);
                MissionApplication manager = null;
                CharacterManager charactersManager = null;
                var creatures = new CreatureManager(factory, manifestation);
                foreach (var creatureId in new[]
                         {
                             39U,
                             50U,
                             CaptainYoungbloodCreatureId,
                             510208U,
                             CorporalVanValkenbergCreatureId,
                             TizzikGiCreatureId,
                             PracticeDummyCreatureId,
                             LightningDummyCreatureId,
                             510213U, 510214U, 510215U, 510227U, 510228U
                         })
                {
                    creatures.LoadedCreatures[creatureId] = new Creature
                    {
                        DbId = creatureId,
                        Faction = creatureId == 510227 ? Factions.AFS : Factions.Bane,
                        EntityClass = (EntityClasses)4001,
                        Npc = new Npc
                        {
                            NpcPackageId = creatureId == CaptainYoungbloodCreatureId
                                ? 2561U
                                : creatureId == 510208U
                                    ? 2584U
                                : creatureId == CorporalVanValkenbergCreatureId
                                    ? 2564U
                                    : creatureId
                        },
                        AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                    };
                }

                MapChannelManager maps = null;
                var objects = new DynamicObjectManager(factory, maps);
                var deadlineService = new MissionDeadlineService(
                    () => factory,
                    () => manager,
                    _getUtcNow);
                var scenarioService = new MissionSceneHost(
                    () => factory,
                    () => manager,
                    manifestation,
                    () => maps,
                    () => creatures,
                    () => objects,
                    () => CommunicatorManager.Instance,
                    _getUtcNow);
                maps = new MapChannelManager(
                    factory,
                    updateCharacter: (client, update, value) =>
                        charactersManager.UpdateCharacter(client, update, value),
                    refreshStats: (_, _) => { },
                    assignPlayer: _ => { },
                    enterMapChannels: _ => { },
                    privateInstances: new PrivateMapInstanceService(),
                    scenarioService: scenarioService);
                maps.MapChannelArray.Add(BootcampMapContextId, CreatePublicMap(BootcampMapContextId, "bootcamp_runtime"));
                maps.MapChannelArray.Add(WildernessMapContextId, CreatePublicMap(WildernessMapContextId, "alia_das_fixture"));
                objects = new DynamicObjectManager(factory, maps);
                manager = new MissionApplication(
                    factory,
                    new Dictionary<uint, Mission>(),
                    new Dictionary<uint, MissionRewardDefinition>(),
                    manifestation,
                    deadlineService: deadlineService,
                    scenarioService: scenarioService,
                    utcNow: _getUtcNow);
                var inventory = new InventoryManager(factory, manager);
                var auction = new AuctionHouseManager(factory, manager);
                var clan = Activator.CreateInstance(
                    typeof(ClanManager),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { factory },
                    culture: null);
                var social = Activator.CreateInstance(
                    typeof(SocialManager),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { factory },
                    culture: null);
                charactersManager = new CharacterManager(Context, manager);
                objects = new DynamicObjectManager(
                    factory,
                    maps,
                    missionManager: manager,
                    characterManager: charactersManager);
                var report = manager.LoadMissions();
                if (report.BlocksReadiness)
                    throw new InvalidOperationException(
                        string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));

                _singletons.Dispose();
                _singletons = new ManagerInstances(
                    maps,
                    objects,
                    creatures,
                    manager,
                    inventory,
                    manifestation,
                    clan,
                    auction,
                    social,
                    factory);
                LoadBootcampLootContent(WorldContext);
                LoadBootcampNav(maps);
                if (_useWorldContent)
                    LoadBootcampWorldContent(WorldContext, creatures, manager, maps);
                objects.InitTeleporters();
                Manager = manager;
                Maps = maps;
                BootcampMap = Maps.GetOrCreatePrivateInstance(BootcampMapContextId, characterId);
                foreach (var npc in _npcs)
                    AddNpcToCurrentMap(npc.CreatureId, npc.PackageId, npc.Position);

                var freshClient = Context.CreateCompetingClient(Manager);
                freshClient.Player.Id = characterId;
                typeof(Client).GetProperty(nameof(Client.AccountEntry))!
                    .SetValue(freshClient, accountEntry);
                ConfigureRuntimePlayer(freshClient);
                using (var reload = Context.CreateChar())
                {
                    var character = reload.Characters.Get(characterId);
                    freshClient.Player.Experience = character.Experience;
                    freshClient.Player.Level = character.Level;
                    freshClient.Player.CloneCredits = character.CloneCredits;
                    freshClient.Player.Credits[CurencyType.Credits] = character.Credit;
                    freshClient.Player.Credits[CurencyType.Prestige] = character.Prestige;
                    freshClient.Player.PlayerFlags = new Dictionary<uint, uint>(reload.CharacterFlags.Get(characterId));
                    Manager.HydrateAndClearInvalid(freshClient.Player, reload);
                    freshClient.Player.StartingExperienceCompleted =
                        Rasa.Game.Missions.Persistence.MissionRequirementFactsAdapter.HasCompletedStartingExperience(reload, characterId);
                }
                freshClient.Player.Class = Client.Player.Class;
                new InventoryManager(Context, Manager).InitCharacterInventory(freshClient);
                freshClient.Player.Skills = Maps.GetPlayerSkills(characterId);
                freshClient.Player.Abilities = Maps.GetPlayerAbilities(characterId);
                MoveClientToMap(freshClient, freshClient.Player.MapChannel, BootcampMap);
                freshClient.State = RasaGame::Rasa.Data.ClientState.Ingame;
                MissionApplication.Instance.Scenes.Resume(freshClient);
                Client = freshClient;
                if (drainPackets)
                    MissionTestContext.Drain(freshClient);
            }

            internal IReadOnlyDictionary<uint, int> ReadOwnedTemplateCounts(params uint[] templateIds)
            {
                var requested = new HashSet<uint>(templateIds);
                using var unit = Context.CreateChar();
                return unit.CharacterInventories
                    .GetItems(Client.AccountEntry.Id)
                    .Where(entry =>
                        entry.CharacterId == Client.Player.Id &&
                        ((InventoryType)entry.InventoryType == InventoryType.Personal ||
                         (InventoryType)entry.InventoryType == InventoryType.EquipedInventory ||
                         (InventoryType)entry.InventoryType == InventoryType.WeaponDrawerInventory))
                    .Select(entry => unit.Items.GetItem(entry.ItemId)?.ItemTemplateId)
                    .Where(templateId => templateId.HasValue && requested.Contains(templateId.Value))
                    .GroupBy(templateId => templateId!.Value)
                    .ToDictionary(group => group.Key, group => group.Count());
            }

            internal (int SkillCount, int TrayCount) ReadLightningGrantCounts()
            {
                using var unit = Context.CreateChar();
                return (
                    unit.CharacterSkills.GetCharacterSkills(Client.Player.Id)
                        .Count(entry =>
                            entry.SkillId == (uint)SkillId.Lightning &&
                            entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                            entry.SkillLevel == 1),
                    unit.CharacterAbilityDrawers.GetCharacterAbilities(Client.Player.Id)
                        .Count(entry =>
                            entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                            entry.AbilityLevel == 1));
            }

            internal void MovePlayerTo(Vector3 position)
            {
                Client.Player.Position = position;
            }

            internal void SpawnWorldNpcs()
            {
                var root = new DirectoryInfo(AppContext.BaseDirectory);
                while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln")))
                    root = root.Parent;
                if (root == null)
                    throw new DirectoryNotFoundException("Repository root not found.");

                var template = Maps.MapChannelArray[BootcampMapContextId];
                template.NavMesh = new NavMeshQuery(NavMeshFile.Read(
                    NavMeshFile.PathFor(Path.Combine(root.FullName, "navmesh"), "adv_bootcamp")));
                BootcampMap.NavMesh = template.NavMesh;

                foreach (var entry in WorldContext.Set<CreatureEntry>()
                             .Where(entry => entry.Id >= MajorMcAllisterCreatureId &&
                                             entry.Id <= CorporalDeSimoneCreatureId))
                {
                    var classId = (EntityClasses)entry.ClassId;
                    if (!EntityClassManager.Instance.LoadedEntityClasses.ContainsKey(classId))
                        EntityClassManager.Instance.LoadedEntityClasses.Add(classId, new EntityClass(
                            entry.ClassId, "bootcamp_npc", 0, 0,
                            new List<AugmentationType> { AugmentationType.Creature, AugmentationType.NPC }, true));
                    CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry)
                    {
                        Npc = new Npc
                        {
                            NpcPackageId = WorldContext.Set<NpcPackageEntry>()
                                .Where(package => package.Id == entry.Id)
                                .Select(package => package.PackageId)
                                .SingleOrDefault(),
                            NpcMissionIds = Manager.LoadedMissions.Values
                                .Where(mission => mission.MissionGiver == entry.Id ||
                                                  mission.MissionReciver == entry.Id)
                                .Select(mission => mission.MissionId).ToList()
                        },
                        AppearanceData = WorldContext.Set<CreatureAppearanceEntry>()
                            .Where(appearance => appearance.Id == entry.Id)
                            .ToDictionary(appearance => (EquipmentData)appearance.SlotId,
                                appearance => new AppearanceData
                                {
                                    SlotId = (EquipmentData)appearance.SlotId,
                                    Class = appearance.ClassId,
                                    Color = new Color(appearance.Color),
                                    Hue2 = new Color(2139062144)
                                })
                    };
                }

                var spawns = SpawnPoolManager.Instance;
                if (spawns.LoadedSpawnPools.Count == 0)
                    spawns.SpawnPoolInit();
                spawns.CloneTemplateMap(template, BootcampMap);
                BootcampMap.SpawnPools.RemoveAll(pool =>
                    pool.DbId < MajorMcAllisterCreatureId || pool.DbId > CorporalDeSimoneCreatureId);
                Manager.RebuildScenarioRuntime(Client.Player.Id, BootcampMap);
                spawns.SpawnPoolWorker(BootcampMap, 0);
            }

            internal void MovePlayerTo(IHasPosition target)
            {
                if (target != null)
                    MovePlayerTo(target.Position);
            }

            internal void BeginUseObject(
                DynamicObject dynamicObject,
                uint actionArgId = DynamicObjectManager.LogosUseArgId)
            {
                if (dynamicObject == null)
                    throw new ArgumentNullException(nameof(dynamicObject));

                MovePlayerTo(dynamicObject);
                DynamicObjectManager.Instance.RequestUseObjectPacket(
                    Client,
                    new RequestUseObjectPacket
                    {
                        ActionId = ActionId.UseObject,
                        ActionArgId = actionArgId,
                        EntityId = dynamicObject.EntityId
                    });
            }

            internal void AdvanceRecovery(long deltaMilliseconds)
            {
                ActorActionManager.Instance.DoWork(
                    Client.Player.MapChannel ?? BootcampMap,
                    deltaMilliseconds);
            }

            internal void UseObjectAndRecover(
                DynamicObject dynamicObject,
                long? deltaMilliseconds = null,
                uint actionArgId = DynamicObjectManager.LogosUseArgId)
            {
                if (dynamicObject.MissionConversation is { } conversation)
                {
                    MovePlayerTo(dynamicObject);
                    var npcs = new NpcManager(Context, Manager);
                    npcs.RequestNpcConverse(Client, new RequestNPCConversePacket { EntityId = dynamicObject.EntityId });
                    npcs.CompleteNPCObjective(Client, new CompleteNPCObjectivePacket
                    {
                        EntityId = dynamicObject.EntityId, MissionId = conversation.MissionId,
                        ObjectiveId = conversation.DialogObjectiveId, PlayerFlagId = conversation.PlayerFlagId
                    });
                    return;
                }
                BeginUseObject(dynamicObject, actionArgId);
                AdvanceRecovery(deltaMilliseconds ?? dynamicObject.WindupTime);
            }

            private void AddNpcToCurrentMap(uint dbId, uint? npcPackageId, Vector3 position)
            {
                var npc = Context.AddNpc(dbId, BootcampMap, npcPackageId, position);
                BindNpcSpawn(npc);
                npc.AppearanceData ??= new Dictionary<EquipmentData, AppearanceData>();
                if (npc.Attributes.Count == 0)
                {
                    npc.State = CharacterState.Normal;
                    npc.Attributes[Attributes.Body] = new ActorAttributes(Attributes.Body, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Mind] = new ActorAttributes(Attributes.Mind, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Spirit] = new ActorAttributes(Attributes.Spirit, 10, 10, 10, 0, 0);
                    npc.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                    npc.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Aware] = new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                    npc.Attributes[Attributes.Speed] = new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0);
                    npc.Attributes[Attributes.Regen] = new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
                }
            }

            private void BindNpcSpawn(Creature npc)
            {
                var spawn = WorldContext.SpawnPoolEntries.AsNoTracking().SingleOrDefault(entry => entry.Id == npc.DbId);
                if (spawn == null)
                    return;
                var pool = BootcampMap.SpawnPools.SingleOrDefault(entry => entry.DbId == spawn.Id);
                if (pool == null)
                {
                    pool = new SpawnPool
                    {
                        DbId = spawn.Id, MapContextId = BootcampMap.MapInfo.MapContextId,
                        RuntimeMapChannel = BootcampMap, Position = npc.Position, Rotation = npc.Rotation
                    };
                    BootcampMap.SpawnPools.Add(pool);
                }
                npc.SpawnPool = pool;
                var template = WorldContext.CreatureEntries.AsNoTracking().Single(entry => entry.Id == npc.DbId);
                npc.RunSpeed = template.RunSpeed;
                npc.WalkSpeed = template.WalkSpeed;
            }

            private static GameAccountEntry CloneAccountEntry(GameAccountEntry source) =>
                new()
                {
                    Id = source.Id,
                    Email = source.Email,
                    Name = source.Name,
                    Level = source.Level,
                    FamilyName = source.FamilyName,
                    SelectedSlot = source.SelectedSlot,
                    CanSkipBootcamp = source.CanSkipBootcamp,
                    LastIp = source.LastIp,
                    LastLogin = source.LastLogin,
                    CreatedAt = source.CreatedAt,
                    Characters = source.Characters?.ToList() ?? new List<CharacterEntry>()
                };

            private static void DetachClientFromCurrentMap(Client client)
            {
                var map = client.Player.MapChannel;
                if (map == null)
                    return;

                CellManager.Instance.RemoveFromWorld(client);
                map.ClientList.Remove(client);
            }

            private static void MoveClientToMap(
                Client client,
                MapChannel origin,
                MapChannel destination)
            {
                if (origin != null)
                {
                    CellManager.Instance.RemoveFromWorld(client);
                    origin.ClientList.Remove(client);
                }

                client.Player.MapChannel = destination;
                client.Player.RuntimeMapChannel = destination;
                client.Player.MapContextId = destination.MapInfo.MapContextId;
                destination.ClientList.Add(client);
                CellManager.Instance.AddToWorld(client);
            }

            public void Dispose()
            {
                if (!ReferenceEquals(Client, Context.Client))
                {
                    if (Client.Player?.MapChannel != null &&
                        CellManager.Instance.IsInWorld(Client))
                        CellManager.Instance.RemoveFromWorld(Client);
                    Client.Player?.MapChannel?.ClientList.Remove(Client);
                    EntityManager.Instance.UnregisterEntity(Client.Player.EntityId);
                    EntityManager.Instance.UnregisterPlayer(Client.Player.EntityId);
                    EntityManager.Instance.UnregisterActor(Client.Player.EntityId);
                    EntityManager.Instance.FreeEntity(Client.Player.EntityId);
                }

                Maps.ReleaseOwnedPrivateInstances(Client.Player.Id);
                _singletons.Dispose();
                var directory = Path.GetDirectoryName(WorldContext.Database.GetDbConnection().DataSource);
                WorldContext.Dispose();
                SqliteConnection.ClearAllPools();
                Context.Dispose();
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private sealed class Bootstrap
        {
            internal Bootstrap(
                MissionTestContext context,
                SqliteWorldContext worldContext,
                MissionApplication manager,
                CharacterManager characters,
                MapChannelManager maps,
                ManagerInstances singletons,
                ClockState clock)
            {
                Context = context;
                WorldContext = worldContext;
                Manager = manager;
                Characters = characters;
                Maps = maps;
                Singletons = singletons;
                Clock = clock;
            }

            internal MissionTestContext Context { get; }
            internal SqliteWorldContext WorldContext { get; }
            internal MissionApplication Manager { get; }
            internal CharacterManager Characters { get; }
            internal MapChannelManager Maps { get; }
            internal ManagerInstances Singletons { get; }
            internal ClockState Clock { get; }
        }

        private sealed class ClockState
        {
            internal ClockState(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            internal DateTime UtcNow { get; set; }
        }

        private sealed class RuntimeLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly SqliteWorldContext _worldContext;

            internal RuntimeLoadingFactory(
                MissionTestContext charFactory,
                SqliteWorldContext worldContext)
            {
                _charFactory = charFactory;
                _worldContext = worldContext;
            }

            public Repositories.Char.ICharUnitOfWork CreateChar() => _charFactory.CreateChar();

            public IWorldUnitOfWork CreateWorld() =>
                new RepositoryBackedWorldUnitOfWork(_worldContext);
        }

        private sealed class RepositoryBackedWorldUnitOfWork : IWorldUnitOfWork
        {
            internal RepositoryBackedWorldUnitOfWork(SqliteWorldContext context)
            {
                Actions = null;
                Equipment = new EquipmentRepository(context);
                Creatures = new CreatureRepository(context);
                EntityClasses = new EntityClassRepository(context);
                Footlockers = null;
                Logoses = null;
                MapInfos = new MapInfoRepository(context);
                MapLinks = null;
                Kraftwerks = null;
                MapRegions = null;
                MapMarkers = null;
                Recipes = null;
                NpcMissions = null;
                NpcMissionRewards = null;
                MissionContent = new MissionContentRepository(context);
                NpcPackages = new NpcPackageRepository(context);
                RandomNames = null;
                Spawnpools = new SpawnpoolRepository(context);
                Teleporters = new TeleporterRepository(context);
            }

            public IActionRepository Actions { get; }
            public IEquipmentRepository Equipment { get; }
            public ICreatureRepository Creatures { get; }
            public IEntityClassRepository EntityClasses { get; }
            public IFootlockerRepository Footlockers { get; }
            public ILogosRepository Logoses { get; }
            public IMapInfoRepository MapInfos { get; }
            public IMapLinkRepository MapLinks { get; }
            public IKraftwerksRepository Kraftwerks { get; }
            public IMapRegionRepository MapRegions { get; }
            public IMapMarkerRepository MapMarkers { get; }
            public IRecipeRepository Recipes { get; }
            public INpcMissionRepository NpcMissions { get; }
            public INpcMissionRewardRepository NpcMissionRewards { get; }
            public IMissionContentRepository MissionContent { get; }
            public INpcPackageRepository NpcPackages { get; }
            public IPlayerRandomNameRepository RandomNames { get; }
            public ISpawnpoolRepository Spawnpools { get; }
            public ITeleporterRepository Teleporters { get; }
            public void Complete() { }
            public void Reject() { }
            public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction() =>
                throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class ManagerInstances : IDisposable
        {
            private readonly FieldInfo _mapsField = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _creaturesField = typeof(CreatureManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _missionsField = typeof(MissionApplication)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _inventoryField = typeof(InventoryManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _manifestationField = typeof(ManifestationManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _clanField = typeof(ClanManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _auctionField = typeof(AuctionHouseManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _socialField = typeof(SocialManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _itemsField = typeof(ItemManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _lootField = typeof(LootDispenserManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _spawnsField = typeof(SpawnPoolManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly object _previousMaps;
            private readonly object _previousObjects;
            private readonly object _previousCreatures;
            private readonly object _previousMissions;
            private readonly object _previousInventory;
            private readonly object _previousManifestation;
            private readonly object _previousClan;
            private readonly object _previousAuction;
            private readonly object _previousSocial;
            private readonly object _previousItems;
            private readonly object _previousLoot;
            private readonly object _previousSpawns;
            private readonly Dictionary<EntityClasses, EntityClass> _previousClasses;

            internal ManagerInstances(
                MapChannelManager maps,
                DynamicObjectManager objects,
                CreatureManager creatures,
                MissionApplication missions,
                InventoryManager inventory,
                ManifestationManager manifestation,
                object clan,
                AuctionHouseManager auction,
                object social,
                IGameUnitOfWorkFactory factory)
            {
                _previousMaps = _mapsField.GetValue(null);
                _previousObjects = _objectsField.GetValue(null);
                _previousCreatures = _creaturesField.GetValue(null);
                _previousMissions = _missionsField.GetValue(null);
                _previousInventory = _inventoryField.GetValue(null);
                _previousManifestation = _manifestationField.GetValue(null);
                _previousClan = _clanField.GetValue(null);
                _previousAuction = _auctionField.GetValue(null);
                _previousSocial = _socialField.GetValue(null);
                _previousItems = _itemsField.GetValue(null);
                _previousLoot = _lootField.GetValue(null);
                _previousSpawns = _spawnsField.GetValue(null);
                _previousClasses = new Dictionary<EntityClasses, EntityClass>(
                    EntityClassManager.Instance.LoadedEntityClasses);
                _mapsField.SetValue(null, maps);
                _objectsField.SetValue(null, objects);
                _creaturesField.SetValue(null, creatures);
                _missionsField.SetValue(null, missions);
                _inventoryField.SetValue(null, inventory);
                _manifestationField.SetValue(null, manifestation);
                _clanField.SetValue(null, clan);
                _auctionField.SetValue(null, auction);
                _socialField.SetValue(null, social);
                var items = (ItemManager)Activator.CreateInstance(
                    typeof(ItemManager),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { factory },
                    culture: null)!;
                items.ItemTemplateItemClass = new Dictionary<uint, EntityClasses>(
                    ItemManager.Instance.ItemTemplateItemClass);
                _itemsField.SetValue(null, items);
                _lootField.SetValue(null, new LootDispenserManager(factory, missionManager: missions));
                _spawnsField.SetValue(null, new SpawnPoolManager(factory));
            }

            public void Dispose()
            {
                _mapsField.SetValue(null, _previousMaps);
                _objectsField.SetValue(null, _previousObjects);
                _creaturesField.SetValue(null, _previousCreatures);
                _missionsField.SetValue(null, _previousMissions);
                _inventoryField.SetValue(null, _previousInventory);
                _manifestationField.SetValue(null, _previousManifestation);
                _clanField.SetValue(null, _previousClan);
                _auctionField.SetValue(null, _previousAuction);
                _socialField.SetValue(null, _previousSocial);
                _itemsField.SetValue(null, _previousItems);
                _lootField.SetValue(null, _previousLoot);
                _spawnsField.SetValue(null, _previousSpawns);
                var classes = EntityClassManager.Instance.LoadedEntityClasses;
                classes.Clear();
                foreach (var entry in _previousClasses)
                    classes.Add(entry.Key, entry.Value);
            }
        }
    }
}
