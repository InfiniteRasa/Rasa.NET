using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Rasa.Test.Gameplay
{
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterAppearance;
    using Rasa.Repositories.Char.CharacterInventory;
    using Rasa.Repositories.Char.Auction;
    using Rasa.Repositories.Char.Items;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    internal sealed class WeaponAmmoContext : IGameUnitOfWorkFactory, IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
        private string Database => System.IO.Path.Combine(_directory, "ammo");
        private readonly List<Item> _items = new();
        private readonly List<uint> _addedTemplates = new();
        internal WorldTestContext World { get; } = new();
        internal Client Client { get; }
        internal Item Weapon { get; }
        internal int SaveAttempts { get; private set; }
        internal Action<SqliteCharContext> BeforeSave { get; set; }
        internal Action<SqliteCharContext> AfterSave { get; set; }
        internal Action BeforeQuery { get; set; }

        internal WeaponAmmoContext(uint clip = 7, uint characterId = 1)
        {
            System.IO.Directory.CreateDirectory(_directory);
            using (var context = Open())
                context.Database.Migrate();
            Client = World.CreateClient(factory: this);
            Client.Player.Id = characterId;
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(Client,
                new GameAccountEntry { Id = 1, SelectedSlot = 1 });
            Client.Player.Inventory.PersonalInventory = Enumerable.Repeat(0UL, 250).ToList();
            Client.Player.Inventory.WeaponDrawer = Enumerable.Repeat(0UL, 5).ToList();
            Client.Player.State = CharacterState.Normal;
            Client.Player.WeaponReady = true;
            Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            using (var context = Open())
            {
                var account = new GameAccountEntry
                {
                    Id = 1, Name = "ammo", Email = "ammo@example.invalid", FamilyName = "Fixture"
                };
                context.GameAccountEntries.Add(account);
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = Client.Player.Id, GameAccount = account, Name = Client.Player.Name, Level = 1
                });
                context.SaveChanges();
            }
            // ItemTemplateItemClassPreloader: 145 -> 6048, 28 -> 3147.
            // WeaponClassPreloader 6048 and ItemTemplateWeaponPreloader 145.
            World.AddClass((EntityClasses)6048);
            World.AddClass((EntityClasses)3147);
            var weaponClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            weaponClass.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133,
                DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147,
                ClipSize = 20, MinDamage = 55, MaxDamage = 55, DamageType = 1, WeaponAnimConditionCode = 1
            });
            weaponClass.EquipableClassInfo = new EquipableClassInfo((EquipmentData)13);
            weaponClass.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1 });
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)3147].ItemClassInfo =
                new ItemClassInfo(new ItemClassEntry { StackSize = 50000, IsConsumableFlag = 1 });
            Weapon = AddWeapon(clip, 0);
            CellManager.Instance.AddToWorld(Client);
            WorldTestContext.Drain(Client);
        }

        internal Item AddWeapon(uint clip, uint slot)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 145, ItemClass = 6048 })
            {
                WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
                {
                    Id = 145, AmmoPerShot = 1, Refire = 800, ReloadTime = 1500,
                    Windup = 800, Recovery = 1, Range = 80, ToolType = 15, AttackType = 2
                })
            };
            var item = AddItem(template, 1, slot, InventoryType.WeaponDrawerInventory, clip);
            if (slot == Client.Player.ActiveWeapon)
                Client.Player.Inventory.EquippedInventory[13] = item.EntityId;
            return item;
        }

        internal Item AddAmmo(uint stack, uint slot = 50)
        {
            return AddItem(new ItemTemplate(new ItemTemplateItemClassEntry
                {
                    ItemTemplateId = 28,
                    ItemClass = 3147
                })
                {
                    InventoryCategory = (InventoryCategory)2
                },
                stack, slot, InventoryType.Personal, 0);
        }

        internal Item AddUnownedLoot(uint stack)
        {
            var slot = (uint)Client.Player.Inventory.PersonalInventory.FindIndex(50, 50, id => id == 0);
            var item = AddAmmo(stack, slot);
            item.ItemTemplate.InventoryCategory = (InventoryCategory)2;
            Client.Player.Inventory.PersonalInventory[(int)slot] = 0;
            using var context = Open();
            new CharacterInventoryRepository(context).DeleteInvItemByItemId(item.Id);
            item.OwnerId = 0;
            item.OwnerSlotId = 0;
            return item;
        }

        internal void AddCorruptPersonalInventoryRow(uint slot)
        {
            using var context = Open();
            var item = new ItemEntry
            {
                ItemId = (uint)(_items.Count + 1000),
                ItemTemplateId = 28,
                StackSize = 1,
                CrafterName = ""
            };
            context.ItemEntries.Add(item);
            context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(
                Client.AccountEntry.Id,
                Client.Player.Id,
                (uint)InventoryType.Personal,
                slot,
                item.ItemId));
            context.SaveChanges();
        }

        internal void AddAuction(Item item, uint sellerId, uint price)
        {
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
                item.Id, sellerId, "Seller", price, 0, 12));
            context.SaveChanges();
            item.OwnerId = sellerId;
            item.OwnerSlotId = 0;
        }

        internal Item AddPersonalWeapon(uint clip, uint slot = 0)
        {
            var weapon = AddWeapon(clip, 1);
            using var context = Open();
            new CharacterInventoryRepository(context).MoveInvItem(1, Client.Player.Id, (uint)InventoryType.Personal, slot, weapon.Id);
            Client.Player.Inventory.WeaponDrawer[1] = 0;
            Client.Player.Inventory.PersonalInventory[(int)slot] = weapon.EntityId;
            weapon.OwnerSlotId = slot;
            return weapon;
        }

        private Item AddItem(ItemTemplate template, uint stack, uint slot, InventoryType type, uint clip)
        {
            if (ItemManager.Instance.ItemTemplateItemClass.TryAdd(template.ItemTemplateId, template.Class))
                _addedTemplates.Add(template.ItemTemplateId);
            EntityClassManager.Instance.LoadedEntityClasses[template.Class].ItemTemplates[template.ItemTemplateId] = template;
            var item = new Item
            {
                Id = (uint)(_items.Count + 1), OwnerId = Client.Player.Id, OwnerSlotId = slot,
                ItemTemplate = template, ItemTemplateId = template.ItemTemplateId, StackSize = stack,
                CurrentAmmo = clip, Crafter = ""
            };
            _items.Add(item);
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            var slots = type == InventoryType.Personal ? Client.Player.Inventory.PersonalInventory : Client.Player.Inventory.WeaponDrawer;
            slots[(int)slot] = item.EntityId;
            using var context = Open();
            context.ItemEntries.Add(new ItemEntry(item) { ItemId = item.Id, AmmoCount = clip });
            context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(1, Client.Player.Id, (uint)type, slot, item.Id));
            context.SaveChanges();
            return item;
        }

        internal SqliteCharContext Open() => WaypointPersistenceTests.Open(Database);

        private sealed class QueryConfiguration(WeaponAmmoContext owner) :
            Rasa.Configuration.ContextSetup.IDbContextConfigurationService
        {
            public void Configure(DbContextOptionsBuilder builder, Rasa.Configuration.DatabaseConnectionConfiguration configuration)
            {
                new Rasa.Configuration.ContextSetup.SqliteDbContextConfigurationService(
                    new Rasa.Configuration.ConnectionStrings.SqliteConnectionStringFactory()).Configure(builder, configuration);
                builder.AddInterceptors(new QueryInterceptor(owner));
            }
        }

        private sealed class QueryInterceptor(WeaponAmmoContext owner) : DbCommandInterceptor
        {
            public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
                System.Data.Common.DbCommand command, CommandEventData eventData,
                InterceptionResult<System.Data.Common.DbDataReader> result)
            {
                owner.BeforeQuery?.Invoke();
                return result;
            }
        }

        internal ItemEntry Read(Item item)
        {
            using var context = Open();
            return new ItemRepository(context).GetItem(item.Id);
        }

        internal List<CharacterInventoryEntry> ReadInventory()
        {
            using var context = Open();
            return new CharacterInventoryRepository(context).GetItems(1);
        }

        internal Client Relog()
        {
            var client = World.CreateClient(factory: this);
            client.Player.Id = Client.Player.Id;
            using (var context = Open())
                client.Player.ActiveWeapon = new CharacterRepository(context).Get(client.Player.Id).ActiveWeapon;
            client.Player.Inventory = new Inventory();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, Client.AccountEntry);
            var before = EntityManager.Instance.Items.Keys.ToHashSet();
            try
            {
                new InventoryManager(this).InitCharacterInventory(client);
            }
            finally
            {
                _items.AddRange(EntityManager.Instance.Items.Where(entry => !before.Contains(entry.Key)).Select(entry => entry.Value));
            }
            return client;
        }

        public ICharUnitOfWork CreateChar()
        {
            var context = new SqliteCharContext(
                Options.Create(new Rasa.Configuration.DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new Rasa.Configuration.DatabaseConnectionConfiguration { Database = Database }
                }),
                new QueryConfiguration(this),
                new Rasa.Services.DbContext.SqliteDbContextPropertyModifier());
            context.SavingChanges += (_, _) =>
            {
                SaveAttempts++;
                BeforeSave?.Invoke(context);
            };
            context.SavedChanges += (_, _) => AfterSave?.Invoke(context);
            return new CharUnitOfWork(context,
                gameAccounts: null, censoredWords: null, characters: new CharacterRepository(context),
                characterAbilityDrawers: new Rasa.Repositories.Char.CharacterAbilityDrawer.CharacterAbilityDrawerRepository(context),
                characterAppearances: new CharacterAppearanceRepository(context),
                characterInventories: new CharacterInventoryRepository(context),
                characterLockboxes: null, characterLogoses: null, characterMissions: null,
                characterMissionDeadlines: null,
                characterMissionProgress: null,
                characterMissionScenario: null,
                characterOptions: null,
                characterFlags: null,
                characterSkills: new Rasa.Repositories.Char.CharacterSkills.CharacterSkillsRepository(context),
                characterStartingExperience: null,
                characterTeleporters: null,
                characterTitles: null, auctions: new AuctionRepository(context), clans: null, clanInventories: null, clanMembers: null,
                clanLockboxLogs: null, friends: null, ignoreds: null, items: new ItemRepository(context),
                petitions: null, userOptions: null);
        }

        public IWorldUnitOfWork CreateWorld() => throw new InvalidOperationException("Unexpected world database access.");

        public void Dispose()
        {
            ManifestationManager.Instance.RemovePlayerCharacter(Client);
            foreach (var entityId in _items.Select(item => item.EntityId).Distinct())
            {
                if (EntityManager.Instance.GetEntityType(entityId) == EntityType.Item)
                    EntityManager.Instance.ReleaseEntity(entityId, EntityType.Item);
            }
            World.Dispose();
            foreach (var id in _addedTemplates)
                ItemManager.Instance.ItemTemplateItemClass.Remove(id);
            SqliteConnection.ClearAllPools();
            System.IO.Directory.Delete(_directory, true);
        }
    }
}
