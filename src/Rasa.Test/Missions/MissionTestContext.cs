using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;

namespace Rasa.Test.Missions
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.Auction;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterAppearance;
    using Rasa.Repositories.Char.CharacterAbilityDrawer;
    using Rasa.Repositories.Char.CharacterInventory;
    using Rasa.Repositories.Char.CharacterLockbox;
    using Rasa.Repositories.Char.CharacterLogos;
    using Rasa.Repositories.Char.CharacterMission;
    using Rasa.Repositories.Char.CharacterMissionDeadline;
    using Rasa.Repositories.Char.CharacterMissionProgress;
    using Rasa.Repositories.Char.CharacterMissionScenario;
    using Rasa.Repositories.Char.CharacterOption;
    using Rasa.Repositories.Char.CharacterFlag;
    using Rasa.Repositories.Char.CharacterSkills;
    using Rasa.Repositories.Char.CharacterStartingExperience;
    using Rasa.Repositories.Char.CharacterTeleporter;
    using Rasa.Repositories.Char.CharacterTitle;
    using Rasa.Repositories.Char.Clan;
    using Rasa.Repositories.Char.ClanInventory;
    using Rasa.Repositories.Char.ClanLockboxLog;
    using Rasa.Repositories.Char.ClanMember;
    using Rasa.Repositories.Char.Friend;
    using Rasa.Repositories.Char.GameAccount;
    using Rasa.Repositories.Char.Ignored;
    using Rasa.Repositories.Char.Items;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Services.DbContext;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    internal sealed class MissionTestContext : IGameUnitOfWorkFactory, IDisposable
    {
        private readonly string _directory = Path.Combine(
            AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
        private readonly WorldTestContext _world;
        private readonly List<Creature> _npcs = new();
        private readonly HashSet<ulong> _originalItems = EntityManager.Instance.Items.Keys.ToHashSet();
        private readonly List<uint> _addedTemplates = new();
        private int _charUnitsCreated;
        private string Database => Path.Combine(_directory, "missions");

        internal int SaveAttempts { get; private set; }
        internal int CharUnitsCreated => _charUnitsCreated;
        internal Action<SqliteCharContext> BeforeSave { get; set; }
        internal Action<SqliteCharContext> AfterSave { get; set; }
        internal Action<SqliteCharContext> BeforeQuery { get; set; }
        internal Action<string> BeforeCommand { get; set; }
        internal Action<string> AfterCommand { get; set; }
        internal Client Client { get; }
        internal MapChannel Map => _world.Map;
        internal MissionApplication Manager { get; }
        internal Creature Receiver { get; private set; }
        internal MissionRewardDefinition Reward { get; private set; }

        internal MissionTestContext(string migration = null)
        {
            Directory.CreateDirectory(_directory);
            using var context = Open();
            context.GetService<IMigrator>().Migrate(migration);
        }

        private MissionTestContext(
            IReadOnlyDictionary<uint, Mission> definitions,
            IReadOnlyDictionary<uint, MissionRewardDefinition> rewards = null,
            Action<Item> beforeRewardItemPublication = null,
            Action<PythonPacket> beforeMissionPacketPublication = null,
            Func<DateTime> utcNow = null) : this()
        {
            SeedCharacter(1, 0, 1);
            _world = new WorldTestContext();
            Client = _world.CreateClient(factory: this);
            Client.Player.Id = 1;
            Client.Player.State = CharacterState.Idle;
            Client.Player.Level = 1;
            Client.Player.Experience = 0;
            Client.Player.Credits[CurencyType.Credits] = 100;
            Client.Player.Credits[CurencyType.Prestige] = 50;
            Client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(Client,
                new GameAccountEntry
                {
                    Id = 1,
                    SelectedSlot = 0,
                    Characters = new List<CharacterEntry>
                    {
                        new() { Id = 1, AccountId = 1, Slot = 0, Name = "Character 1", Scale = 1 }
                    }
                });
            CellManager.Instance.AddToWorld(Client);
            Drain();
            Manager = new MissionApplication(this, definitions, rewards ?? new Dictionary<uint, MissionRewardDefinition>(),
                new ManifestationManager(this), beforeRewardItemPublication,
                beforeMissionPacketPublication, utcNow: utcNow);
        }

        internal static MissionTestContext WithDefinitions(params uint[] missionIds) =>
            new(CreateDefinitions(true, missionIds));

        internal static MissionTestContext WithDatabaseDefinitions(params uint[] missionIds) =>
            new(CreateDefinitions(false, missionIds));

        internal static MissionTestContext WithRecoveredDefinitions() =>
            new(MissionDefinitionCatalog.CreateRecoveredInactiveDefinitions());

        internal static MissionTestContext WithCustomDefinitions(
            IReadOnlyDictionary<uint, Mission> definitions,
            IReadOnlyDictionary<uint, MissionRewardDefinition> rewards = null,
            Func<DateTime> utcNow = null) =>
            new(definitions, rewards, utcNow: utcNow);

        internal static MissionTestContext WithProgressMission(
            MissionProgressRule progressRule,
            uint missionId = 321,
            uint objectiveId = 1,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters = null,
            global::Rasa.Missions.Runtime.MissionCreditPolicy creditPolicy = null,
            global::Rasa.Missions.Runtime.MissionRequirement objectiveRequirement = null,
            Func<DateTime> utcNow = null,
            Action<PythonPacket> beforeMissionPacketPublication = null,
            global::Rasa.Missions.Runtime.MissionRepeatPolicy repeatPolicy = null,
            MissionRewardDefinition reward = null)
        {
            counters ??= new Dictionary<uint, MissionObjectiveCounterDefinition>();
            var counterTextIds = new uint?[3];
            foreach (var counterId in counters.Keys)
                counterTextIds[counterId] = 9000 + counterId;
            var objective = new MissionObjectiveDefinition(
                objectiveId,
                1001,
                1002,
                counterTextIds,
                0,
                MissionObjectiveState.Incomplete,
                true,
                counters,
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(),
                progressRule,
                creditPolicy: creditPolicy);
            var mission = new Mission(
                missionId,
                $"Mission {missionId}",
                missionId,
                77,
                88,
                5,
                1,
                2,
                true,
                false,
                new[] { objective },
                true,
                objectiveRequirements: objectiveRequirement == null ? null :
                    new Dictionary<uint, global::Rasa.Missions.Runtime.MissionRequirement>
                    { [objectiveId] = objectiveRequirement }, repeatPolicy: repeatPolicy);
            return new MissionTestContext(
                new Dictionary<uint, Mission> { [missionId] = mission },
                reward == null ? null : new Dictionary<uint, MissionRewardDefinition> { [missionId] = reward }, utcNow: utcNow,
                beforeMissionPacketPublication: beforeMissionPacketPublication);
        }

        private static IReadOnlyDictionary<uint, Mission> CreateDefinitions(
            bool isOperational,
            params uint[] missionIds) =>
            missionIds.ToDictionary(
                id => id,
                id => new Mission(
                    id,
                    $"Mission {id}",
                    clientNameTextId: id,
                    missionGiver: 77,
                    missionReciver: 88,
                    level: 5,
                    groupType: 1,
                    categoryId: 2,
                    shareable: true,
                    radioCompletable: false,
                    objectives: new[]
                    {
                        new MissionObjectiveDefinition(
                            objectiveId: 1,
                            clientNameTextId: 1001,
                            clientBodyTextId: 1002,
                            clientCounterTextIds: new uint?[] { null, null, null },
                            ordinal: 0,
                            initialState: MissionObjectiveState.Incomplete,
                            isRequired: true,
                            counters: new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                            itemCounters: new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                            conversations: Array.Empty<MissionObjectiveConversation>(),
                            revealedObjectiveIds: Array.Empty<uint>(),
                            activatedObjectiveIds: Array.Empty<uint>(),
                            indicators: Array.Empty<MissionIndicator>())
                    },
                    enableOperational: isOperational));

        internal static MissionTestContext WithCompletableMission(
            uint missionId,
            Action<Item> beforeRewardItemPublication = null)
        {
            var reward = new MissionRewardDefinition(
                experience: 100,
                currencies: new Dictionary<CurencyType, int>
                {
                    [CurencyType.Credits] = 7,
                    [CurencyType.Prestige] = 3
                },
                fixedItems: new[] { new MissionRewardItem(28, 3) },
                selectableItems: new[]
                {
                    new MissionRewardItem(29, 2),
                    new MissionRewardItem(29, 4)
                });
            var context = new MissionTestContext(
                CreateDefinitions(true, missionId),
                new Dictionary<uint, MissionRewardDefinition> { [missionId] = reward },
                beforeRewardItemPublication);
            context.Reward = reward;
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            context.SeedMission(context.Client.Player.Id, missionId, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            context.Receiver = context.AddNpc(88);
            context.Drain();
            return context;
        }

        internal static MissionTestContext WithItemProgressMission(
            MissionProgressEventKind kind,
            uint itemClassId,
            uint target)
        {
            var itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>
            {
                [itemClassId] = new MissionObjectiveItemCounterDefinition(
                    itemClassId, 0, target)
            };
            var objective = new MissionObjectiveDefinition(
                1, 1001, 1002, new uint?[] { null, null, null }, 0,
                MissionObjectiveState.Incomplete, true,
                new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                itemCounters,
                Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(),
                MissionProgressRule.IncrementItemCounterOnExactSubject(
                    kind, itemClassId, 0, target));
            var mission = new Mission(
                321, "Item mission", 321, 77, 88, 5, 1, 2, true, false,
                new[] { objective }, true);
            return new MissionTestContext(
                new Dictionary<uint, Mission> { [321] = mission });
        }

        internal static MissionTestContext WithObjectiveMission(
            uint missionId = 321,
            bool selectableReward = true,
            bool activateSuccessor = true,
            bool includeCompetingObjective = false,
            Action<PythonPacket> beforeMissionPacketPublication = null)
        {
            var objectives = new List<MissionObjectiveDefinition>
            {
                new MissionObjectiveDefinition(
                    objectiveId: 5,
                    clientNameTextId: 5001,
                    clientBodyTextId: 5002,
                    clientCounterTextIds: new uint?[] { 5003, null, null },
                    ordinal: 7,
                    initialState: MissionObjectiveState.Incomplete,
                    isRequired: true,
                    counters: new Dictionary<uint, MissionObjectiveCounterDefinition>
                    {
                        [0] = new MissionObjectiveCounterDefinition(0, 2, 10)
                    },
                    itemCounters: new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                    {
                        [200] = new MissionObjectiveItemCounterDefinition(200, 1, 8)
                    },
                    conversations: new[]
                    {
                        new MissionObjectiveConversation(
                            700,
                            11,
                            MissionObjectiveConversationType.Completion)
                    },
                    revealedObjectiveIds: new uint[] { 9 },
                    activatedObjectiveIds: activateSuccessor
                        ? new uint[] { 9 }
                        : Array.Empty<uint>(),
                    indicators: new[]
                    {
                        new MissionIndicator
                        {
                            Position = new Vector3(1.25f, 2.5f, 3.75f),
                            Radius = 4.5,
                            IndicatorId = 7,
                            Show3DEffect = true
                        }
                    }),
            };
            if (includeCompetingObjective)
                objectives.Add(new MissionObjectiveDefinition(
                    objectiveId: 6,
                    clientNameTextId: 6001,
                    clientBodyTextId: 6002,
                    clientCounterTextIds: new uint?[] { null, null, null },
                    ordinal: 8,
                    initialState: MissionObjectiveState.Incomplete,
                    isRequired: true,
                    counters: new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    itemCounters: new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    conversations: new[]
                    {
                        new MissionObjectiveConversation(
                            702,
                            13,
                            MissionObjectiveConversationType.Completion)
                    },
                    revealedObjectiveIds: new uint[] { 9 },
                    activatedObjectiveIds: activateSuccessor
                        ? new uint[] { 9 }
                        : Array.Empty<uint>(),
                    indicators: Array.Empty<MissionIndicator>()));
            objectives.Add(
                new MissionObjectiveDefinition(
                    objectiveId: 9,
                    clientNameTextId: 9001,
                    clientBodyTextId: 9002,
                    clientCounterTextIds: includeCompetingObjective
                        ? new uint?[] { 9003, null, null }
                        : new uint?[] { null, null, null },
                    ordinal: 8,
                    initialState: MissionObjectiveState.Inactive,
                    isRequired: true,
                    counters: includeCompetingObjective
                        ? new Dictionary<uint, MissionObjectiveCounterDefinition>
                        {
                            [0] = new MissionObjectiveCounterDefinition(0, 3, 9)
                        }
                        : new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    itemCounters: includeCompetingObjective
                        ? new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                        {
                            [201] = new MissionObjectiveItemCounterDefinition(201, 2, 8)
                        }
                        : new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    conversations: new[]
                    {
                        new MissionObjectiveConversation(
                            701,
                            12,
                            MissionObjectiveConversationType.Completion)
                    },
                    revealedObjectiveIds: Array.Empty<uint>(),
                    activatedObjectiveIds: Array.Empty<uint>(),
                    indicators: Array.Empty<MissionIndicator>())
            );
            var mission = new Mission(
                missionId,
                $"Mission {missionId}",
                clientNameTextId: missionId,
                missionGiver: 77,
                missionReciver: 88,
                level: 5,
                groupType: 1,
                categoryId: 2,
                shareable: true,
                radioCompletable: false,
                objectives,
                enableOperational: true);
            var reward = new MissionRewardDefinition(
                experience: 100,
                currencies: new Dictionary<CurencyType, int>(),
                fixedItems: Array.Empty<MissionRewardItem>(),
                selectableItems: selectableReward
                    ? new[] { new MissionRewardItem(29, 2) }
                    : Array.Empty<MissionRewardItem>());
            var context = new MissionTestContext(
                new Dictionary<uint, Mission> { [missionId] = mission },
                new Dictionary<uint, MissionRewardDefinition> { [missionId] = reward },
                beforeMissionPacketPublication: beforeMissionPacketPublication);
            context.Reward = reward;
            if (selectableReward)
                context.AddRewardTemplate(29, 3147);
            return context;
        }

        internal void SeedCharacter(uint accountId, byte slot, uint characterId, byte race = 0)
        {
            using var context = Open();
            var account = context.GameAccountEntries.Find(accountId);
            if (account == null)
            {
                account = new GameAccountEntry
                {
                    Id = accountId,
                    Email = $"account-{accountId}@example.invalid",
                    Name = $"Account {accountId}",
                    FamilyName = $"Family {accountId}"
                };
                context.GameAccountEntries.Add(account);
            }

            context.CharacterEntries.Add(new CharacterEntry
            {
                Id = characterId,
                AccountId = accountId,
                GameAccount = account,
                Slot = slot,
                Name = $"Character {characterId}",
                Race = race,
                Scale = 1,
                Level = 1,
                Credit = 100,
                Prestige = 50
            });
            context.SaveChanges();
        }

        internal void SeedMission(uint characterId, uint missionId, uint state, bool completeable)
        {
            using var context = Open();
            context.CharacterMissionEntries.Add(new CharacterMissionEntry(characterId, missionId, state)
            {
                Completeable = completeable
            });
            if (Manager != null &&
                Manager.LoadedMissions.TryGetValue(missionId, out var definition) &&
                definition.IsOperational)
                foreach (var objective in definition.Objectives.Values)
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
                    context.CharacterMissionObjectiveEntries.Add(row);
                }
            context.SaveChanges();
        }

        internal void SeedLegacyMissionWithoutObjectives(
            uint characterId,
            uint missionId,
            uint state,
            bool completeable)
        {
            using var context = Open();
            context.CharacterMissionEntries.Add(
                new CharacterMissionEntry(characterId, missionId, state)
                {
                    Completeable = completeable
                });
            context.SaveChanges();
        }

        internal int MissionCount(uint characterId)
        {
            using var context = Open();
            return context.CharacterMissionEntries.Count(entry =>
                entry.CharacterId == characterId);
        }

        internal Item CreateAuctionItem(
            uint templateId,
            uint classId,
            uint quantity,
            uint sellerId,
            uint price)
        {
            var item = CreateInventoryItem(templateId, classId, quantity);
            using var context = Open();
            var sellerAccount = new GameAccountEntry
            {
                Id = 2,
                Name = "seller",
                Email = "seller@example.invalid",
                FamilyName = "Seller"
            };
            context.GameAccountEntries.Add(sellerAccount);
            context.CharacterEntries.Add(new CharacterEntry
            {
                Id = sellerId,
                GameAccount = sellerAccount,
                Name = "Seller",
                Level = 1
            });
            context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(
                sellerAccount.Id,
                sellerId,
                (uint)InventoryType.AuctionInventory,
                0,
                item.Id));
            context.AuctionEntries.Add(new AuctionEntry(
                item.Id,
                sellerId,
                "Seller",
                price,
                0,
                12));
            context.SaveChanges();
            item.OwnerId = sellerId;
            item.OwnerSlotId = 0;
            return item;
        }

        internal bool TryCompleteMissionAggregate(uint missionId, uint objectiveId)
        {
            using var context = Open();
            using var transaction = context.Database.BeginTransaction(
                System.Data.IsolationLevel.Serializable);
            var mission = context.CharacterMissionEntries.Single(entry =>
                entry.CharacterId == Client.Player.Id &&
                entry.MissionId == missionId);
            var objective = context.CharacterMissionObjectiveEntries.Single(entry =>
                entry.CharacterId == Client.Player.Id &&
                entry.MissionId == missionId &&
                entry.ObjectiveId == objectiveId);
            mission.Completeable = true;
            objective.ObjectiveState = (byte)MissionObjectiveState.Completed;
            context.SaveChanges();
            transaction.Commit();
            return true;
        }

        internal void ReloadPlayerMissions()
        {
            using var unit = CreateChar();
            Manager.Hydrate(
                Client.Player,
                unit.CharacterMissions.Get(Client.Player.Id),
                unit.CharacterMissionProgress.Get(Client.Player.Id));
        }

        internal CharacterMissionEntry ReadMission(uint missionId)
        {
            using var context = Open();
            return context.CharacterMissionEntries.AsNoTracking().Single(entry =>
                entry.CharacterId == Client.Player.Id && entry.MissionId == missionId);
        }

        internal CharacterMissionProgressSnapshot ReadProgress(uint missionId)
        {
            using var unit = CreateChar();
            return unit.CharacterMissionProgress.Get(Client.Player.Id, missionId);
        }

        internal void SeedObjective(
            uint characterId,
            uint missionId,
            uint objectiveId,
            MissionObjectiveState state,
            IReadOnlyDictionary<uint, uint> counters = null,
            IReadOnlyDictionary<uint, uint> itemCounters = null)
        {
            using var context = Open();
            var row = new CharacterMissionObjectiveEntry(
                characterId,
                missionId,
                objectiveId,
                (byte)state);
            foreach (var counter in counters ?? new Dictionary<uint, uint>())
                row.Counters.Add(new CharacterMissionObjectiveCounterEntry(
                    characterId, missionId, objectiveId, counter.Key, counter.Value));
            foreach (var counter in itemCounters ?? new Dictionary<uint, uint>())
                row.ItemCounters.Add(new CharacterMissionObjectiveItemCounterEntry(
                    characterId, missionId, objectiveId, counter.Key, counter.Value));
            context.CharacterMissionObjectiveEntries.Add(row);
            context.SaveChanges();
        }

        internal RewardTotals ReadRewardTotals()
        {
            using var context = Open();
            var character = context.CharacterEntries.AsNoTracking().Single(entry => entry.Id == Client.Player.Id);
            var itemCount = (from inventory in context.CharacterInventoryEntries.AsNoTracking()
                join item in context.ItemEntries.AsNoTracking() on inventory.ItemId equals item.ItemId
                where inventory.CharacterId == Client.Player.Id &&
                    inventory.InventoryType == (uint)InventoryType.Personal
                select item.StackSize).Sum(value => (long)value);
            return new RewardTotals(character.Experience, character.Credit, character.Prestige, itemCount);
        }

        internal void FillRewardCategory()
        {
            var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)3147].ItemTemplates[28];
            for (uint slot = 50; slot < 100; slot++)
            {
                var item = ItemManager.StageItem(template, 50000, "");
                item.OwnerId = Client.Player.Id;
                item.OwnerSlotId = slot;
                using (var context = Open())
                {
                    item.Id = new ItemRepository(context).CreateItem(item);
                    new CharacterInventoryRepository(context).AddInvItem(
                        Client.AccountEntry.Id, Client.Player.Id, (uint)InventoryType.Personal, slot, item.Id);
                }
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                Client.Player.Inventory.PersonalInventory[(int)slot] = item.EntityId;
            }
        }

        internal Item CreateInventoryItem(
            uint templateId,
            uint classId,
            uint quantity)
        {
            AddRewardTemplate(templateId, classId);
            var item = ItemManager.StageItem(
                EntityClassManager.Instance.LoadedEntityClasses[
                    (EntityClasses)classId].ItemTemplates[templateId],
                quantity,
                "");
            item.OwnerId = Client.Player.Id;
            using (var context = Open())
                item.Id = new ItemRepository(context).CreateItem(item);
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            return item;
        }

        internal ClanEntry CreateClanForPlayer()
        {
            ClanEntry clan;
            using (var unit = CreateChar())
            {
                clan = unit.Clans.CreateClan("Mission Test Clan", false);
                if (!unit.ClanMembers.InsertClanMemberData(
                        clan.Id,
                        Client.Player.Id,
                        ClanRank.Leader,
                        ""))
                    throw new InvalidOperationException("Unable to seed mission test clan member.");
            }

            Client.Player.ClanId = clan.Id;
            ClanManager.Instance.Clans =
                new ConcurrentDictionary<uint, Lazy<ClanEntry>>(
                    new[]
                    {
                        new KeyValuePair<uint, Lazy<ClanEntry>>(
                            clan.Id,
                            new Lazy<ClanEntry>(() => clan))
                    });
            ClanManager.Instance.ClanMembers =
                new ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>>(
                    new[]
                    {
                        new KeyValuePair<uint, Lazy<List<ClanMemberEntry>>>(
                            clan.Id,
                            new Lazy<List<ClanMemberEntry>>(() =>
                                new List<ClanMemberEntry>
                                {
                                    new()
                                    {
                                        ClanId = clan.Id,
                                        CharacterId = Client.Player.Id,
                                        Rank = ClanRank.Leader,
                                        Note = ""
                                    }
                                }))
                    });
            return clan;
        }

        internal void AddRewardTemplate(uint templateId, uint classId)
        {
            var entityClass = (EntityClasses)classId;
            _world.AddClass(entityClass);
            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[entityClass];
            classInfo.ItemClassInfo ??= new ItemClassInfo(new ItemClassEntry { StackSize = 50000 });
            var template = new ItemTemplate(new ItemTemplateItemClassEntry
            {
                ItemTemplateId = templateId,
                ItemClass = classId
            }) { InventoryCategory = (InventoryCategory)2 };
            ItemManager.Instance.ItemTemplateItemClass[templateId] = entityClass;
            classInfo.ItemTemplates[templateId] = template;
            _addedTemplates.Add(templateId);
        }

        internal Creature AddNpc(
            uint dbId,
            MapChannel map = null,
            uint? npcPackageId = null,
            Vector3? position = null)
        {
            map ??= Map;
            var npc = new Creature
            {
                DbId = dbId,
                Npc = new Npc { NpcPackageId = npcPackageId ?? dbId },
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = position ?? Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            _npcs.Add(npc);
            EntityManager.Instance.RegisterEntity(npc.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(npc);
            var seed = CellManager.Instance.GetCellSeed(npc.Position);
            npc.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).CreatureList.Add(npc);
            return npc;
        }

        internal void RemoveNpcFromWorld(Creature npc)
        {
            foreach (var cell in Map.MapCellInfo.Cells.Values)
                cell.CreatureList.Remove(npc);
            EntityManager.Instance.UnregisterEntity(npc.EntityId);
            EntityManager.Instance.UnregisterCreature(npc.EntityId);
        }

        internal Client CreateCompetingClient()
        {
            return CreateCompetingClient(Manager);
        }

        internal Client CreateAdditionalClient(
            uint characterId,
            uint? accountId = null,
            MissionApplication manager = null)
        {
            var durableAccountId = accountId ?? characterId;
            SeedCharacter(durableAccountId, 0, characterId);

            var client = _world.CreateClient(factory: this);
            client.Player.Id = characterId;
            client.Player.Name = $"Player{characterId}";
            client.Player.Level = Client.Player.Level;
            client.Player.Experience = Client.Player.Experience;
            client.Player.State = CharacterState.Idle;
            client.Player.Credits[CurencyType.Credits] = Client.Player.Credits[CurencyType.Credits];
            client.Player.Credits[CurencyType.Prestige] = Client.Player.Credits[CurencyType.Prestige];
            client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new GameAccountEntry
                {
                    Id = durableAccountId,
                    SelectedSlot = 0,
                    Characters = new List<CharacterEntry>
                    {
                        new()
                        {
                            Id = characterId,
                            AccountId = durableAccountId,
                            Slot = 0,
                            Name = $"Character {characterId}",
                            Scale = 1
                        }
                    }
                });
            CellManager.Instance.AddToWorld(client);
            using var unit = CreateChar();
            (manager ?? Manager).Hydrate(
                client.Player,
                unit.CharacterMissions.Get(characterId),
                unit.CharacterMissionProgress.Get(characterId));
            Drain(client);
            return client;
        }

        internal Client CreateCompetingClient(MissionApplication manager)
        {
            foreach (var cell in Map.MapCellInfo.Cells.Values)
                foreach (var npc in _npcs)
                    cell.CreatureList.Remove(npc);
            var client = _world.CreateClient(factory: this);
            client.Player.Id = Client.Player.Id;
            client.Player.Level = Client.Player.Level;
            client.Player.Experience = Client.Player.Experience;
            client.Player.CloneCredits = Client.Player.CloneCredits;
            client.Player.Credits[CurencyType.Credits] = Client.Player.Credits[CurencyType.Credits];
            client.Player.Credits[CurencyType.Prestige] = Client.Player.Credits[CurencyType.Prestige];
            client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new GameAccountEntry { Id = Client.AccountEntry.Id, SelectedSlot = 0 });
            try
            {
                CellManager.Instance.AddToWorld(client);
            }
            finally
            {
                foreach (var npc in _npcs)
                {
                    var seed = CellManager.Instance.GetCellSeed(npc.Position);
                    CellManager.Instance.GetCell(Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(npc);
                }
            }
            using var unit = CreateChar();
            (manager ?? Manager).Hydrate(
                client.Player,
                unit.CharacterMissions.Get(client.Player.Id),
                unit.CharacterMissionProgress.Get(client.Player.Id));
            Drain(client);
            return client;
        }

        internal List<PythonPacket> Drain() => WorldTestContext.Drain(Client)
            .Select(packet => packet.Message).OfType<CallMethodMessage>()
            .Select(packet => packet.Packet).ToList();

        internal static List<PythonPacket> Drain(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message).OfType<CallMethodMessage>()
            .Select(packet => packet.Packet).ToList();

        internal static byte[] Encode(PythonPacket packet)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            using var writer = new Rasa.Memory.PythonWriter(binary);
            packet.Write(writer);
            return stream.ToArray();
        }

        public ICharUnitOfWork CreateChar()
        {
            Interlocked.Increment(ref _charUnitsCreated);
            var context = OpenWithHooks();
            context.SavingChanges += (_, _) =>
            {
                SaveAttempts++;
                BeforeSave?.Invoke(context);
            };
            context.SavedChanges += (_, _) => AfterSave?.Invoke(context);
            return new CharUnitOfWork(context,
                gameAccounts: new GameAccountRepository(context), censoredWords: null,
                characters: new CharacterRepository(context),
                characterAbilityDrawers: new CharacterAbilityDrawerRepository(context),
                characterAppearances: new CharacterAppearanceRepository(context),
                characterInventories: new CharacterInventoryRepository(context),
                characterLockboxes: new CharacterLockboxRepository(context),
                characterLogoses: new CharacterLogosRepository(context),
                characterMissions: new CharacterMissionRepository(context),
                characterMissionDeadlines: new CharacterMissionDeadlineRepository(context),
                characterMissionProgress: new CharacterMissionProgressRepository(context),
                characterMissionScenario: new CharacterMissionScenarioRepository(context),
                characterOptions: new CharacterOptionRepository(context),
                characterFlags: new CharacterFlagRepository(context),
                characterSkills: new CharacterSkillsRepository(context), characterTeleporters: new CharacterTeleporterRepository(context),
                characterStartingExperience: new CharacterStartingExperienceRepository(context),
                characterTitles: new CharacterTitleRepository(context),
                auctions: new AuctionRepository(context), clans: new ClanRepository(context),
                clanInventories: new ClanInventoryRepository(context),
                clanMembers: new ClanMemberRepository(context),
                clanLockboxLogs: new ClanLockboxLogRepository(context),
                friends: new FriendRepository(context),
                ignoreds: new IgnoredRepository(context),
                items: new ItemRepository(context), petitions: null, userOptions: null);
        }

        internal void ResetCharUnitCount() => _charUnitsCreated = 0;

        public IWorldUnitOfWork CreateWorld() =>
            throw new InvalidOperationException("Unexpected world database access.");

        internal SqliteCharContext Open() =>
            new(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = Database }
                }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());

        private SqliteCharContext OpenWithHooks() =>
            new(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = Database }
                }),
                new QueryConfiguration(this),
                new SqliteDbContextPropertyModifier());

        private sealed class QueryConfiguration(MissionTestContext owner) : IDbContextConfigurationService
        {
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
            {
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory())
                    .Configure(builder, configuration);
                builder.AddInterceptors(new QueryInterceptor(owner));
            }
        }

        private sealed class QueryInterceptor(MissionTestContext owner) : DbCommandInterceptor
        {
            public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
                System.Data.Common.DbCommand command,
                Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                InterceptionResult<System.Data.Common.DbDataReader> result)
            {
                owner.BeforeQuery?.Invoke((SqliteCharContext)eventData.Context);
                owner.BeforeCommand?.Invoke(command.CommandText);
                return result;
            }

            public override System.Data.Common.DbDataReader ReaderExecuted(
                System.Data.Common.DbCommand command,
                CommandExecutedEventData eventData,
                System.Data.Common.DbDataReader result)
            {
                owner.AfterCommand?.Invoke(command.CommandText);
                return result;
            }
        }

        public void Dispose()
        {
            foreach (var npc in _npcs)
            {
                if (EntityManager.Instance.Creatures.TryGetValue(npc.EntityId, out var current) &&
                    ReferenceEquals(current, npc))
                    EntityManager.Instance.ReleaseEntity(npc.EntityId, EntityType.Creature);
                else if (!EntityManager.Instance.RegisteredEntities.ContainsKey(npc.EntityId))
                    EntityManager.Instance.FreeEntity(npc.EntityId);
            }
            foreach (var id in EntityManager.Instance.Items.Keys.Except(_originalItems).ToArray())
                EntityManager.Instance.ReleaseEntity(id, EntityType.Item);
            foreach (var templateId in _addedTemplates)
            {
                if (ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var classId) &&
                    EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass))
                    entityClass.ItemTemplates.Remove(templateId);
                ItemManager.Instance.ItemTemplateItemClass.Remove(templateId);
            }
            _world?.Dispose();
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }

        internal readonly struct RewardTotals
        {
            internal uint Experience { get; }
            internal int Credits { get; }
            internal int Prestige { get; }
            internal long ItemCount { get; }

            internal RewardTotals(uint experience, int credits, int prestige, long itemCount)
            {
                Experience = experience;
                Credits = credits;
                Prestige = prestige;
                ItemCount = itemCount;
            }
        }
    }
}
