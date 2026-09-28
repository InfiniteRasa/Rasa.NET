using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Packets.MapChannel.Client;
    using Repositories.Char.Items;
    using Repositories.UnitOfWork;
    using Structures;

    public class ItemManager
    {
        /*      Item Packets:
         *  - SetConsumable(self, isConsumable)
         *  - SetStackCount(self, count)
         *  - ItemStatus(self, currentHitPoints, maxHitPoints)
         *  - ItemModuleModified(self, moduleIds)
         */

        private static ItemManager _instance;
        private static readonly object InstanceLock = new();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public Dictionary<uint, EntityClasses> ItemTemplateItemClass = new();

        public static ItemManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ItemManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private ItemManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public Item CreateFromTemplateId(uint itemTemplateId, uint stackSize, string crafter = "")
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return null;

            var item = CreateItem(itemTemplate, stackSize, crafter);

            return item;
        }

        public Item CreateItem(ItemTemplate itemTemplate, uint stackSize, string crafter)
        {
            if (itemTemplate == null)
                return null;

            var classInfo = EntityClassManager.Instance.GetClassInfo(itemTemplate.Class);

            // dont create more then max stackSize
            if (classInfo.ItemClassInfo.StackSize < stackSize)
                stackSize = classInfo.ItemClassInfo.StackSize;

            // insert into items table to get unique ItemId
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // create physical copy of item
            var item = new Item
            {
                ItemTemplate = itemTemplate,
                ItemTemplateId = itemTemplate.ItemTemplateId,
                StackSize = stackSize,
                Crafter = crafter,
                Color = 2139062144,     // ToDo we will have to find color in game client files
                CurrentHitPoints = classInfo.ItemClassInfo.MaxHitPoints
            };
            //create item in db
            var itemId = unitOfWork.Items.CreateItem(item);

            item.Id = itemId;

            // register item
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            return item;
        }

        internal static Item StageItem(
            ItemTemplate template,
            uint stackSize,
            string crafter) => new()
        {
            ItemTemplate = template,
            ItemTemplateId = template.ItemTemplateId,
            StackSize = stackSize,
            Crafter = crafter,
            Color = 2139062144,
            CurrentHitPoints = EntityClassManager.Instance
                .GetClassInfo(template.Class).ItemClassInfo.MaxHitPoints
        };

        /// <summary>
        /// One item of a vendor's stock. Registered for the whole server and not sent anywhere:
        /// the stock is made once, but each client that opens the vendor has to be sent the
        /// items separately, so that is the caller's job rather than this one's. Sending from
        /// here meant only the player who happened to open the vendor first ever saw it.
        /// </summary>
        public Item CreateVendorItem(uint itemTemplateId)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);

            if (itemTemplate == null)
                return null;

            var classInfo = EntityClassManager.Instance.GetClassInfo(itemTemplate.Class);

            if (classInfo?.ItemClassInfo == null)
                return null;

            var item = new Item
            {
                ItemTemplate = itemTemplate,
                CurrentHitPoints = classInfo.ItemClassInfo.MaxHitPoints
            } ;

            // register item
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            return item;
        }

        public Item DuplicateItem(Client client, RequestVendorPurchasePacket packet)
        {
            var vendorItem = EntityManager.Instance.GetItem(packet.ItemEntityId);
            var item = CreateFromTemplateId(vendorItem.ItemTemplate.ItemTemplateId, packet.Quantity, "");

            item.CurrentHitPoints = vendorItem.CurrentHitPoints;
            item.Color = vendorItem.Color;

            SendItemDataToClient(client, item, false);

            return item;
        }

        public Item GetItemFromTemplateId(uint itemId, uint characterSlot, uint slotId, uint itemTemplateId, uint stackSize)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);

            if (itemTemplate == null)
                return null;

            var item = new Item
            {
                OwnerId = characterSlot,
                OwnerSlotId = slotId,
                ItemTemplate = itemTemplate,
                StackSize = stackSize,
            };
            // register item
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);

            return item;
        }
        
        public ItemTemplate GetItemTemplateById(uint itemTemplateId)
        {
            if (ItemTemplateItemClass.ContainsKey(itemTemplateId))
            {
                var classId = ItemTemplateItemClass[itemTemplateId];

                return EntityClassManager.Instance.LoadedEntityClasses[classId].ItemTemplates[itemTemplateId];
            }

            Logger.WriteLog(LogType.Error, $"Unknown itemTemplateId = {itemTemplateId}");

            return null;
        }

        public void LoadItemTemplates()
        {
            Logger.WriteLog(LogType.Initialize, "Loading ItemTemplates from db...");

            var LoadedItemTemplates = new Dictionary<uint, ItemTemplate>();
            var loaded = 0;
            var skipped = 0;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            // load item templates
            var itemTemplates = unitOfWork.Equipment.GetItemTemplateClasses();
            foreach (var itemTemplate in itemTemplates)
            {
                LoadedItemTemplates.Add(itemTemplate.ItemTemplateId, new ItemTemplate(itemTemplate));
                ItemTemplateItemClass.Add(itemTemplate.ItemTemplateId, (EntityClasses)itemTemplate.ItemClass);
            }

            // add race requirements to itemTemplate
            var itemRaceReq = unitOfWork.Equipment.GetRequirementsRace();
            foreach (var raceReq in itemRaceReq)
                LoadedItemTemplates[raceReq.Id].ItemInfo.RaceReq = raceReq.RaceId;

            // add skill requirements to itemTemplate
            var itemSkillReq = unitOfWork.Equipment.GetRequirementsSkill();
            foreach (var skillReq in itemSkillReq)
                LoadedItemTemplates[skillReq.Id].EquipableInfo = new EquipableInfo(skillReq.SkillId, skillReq.SkillLevel);

            // add resistance data to itemTemplate
            var itemTemplateResistance = unitOfWork.Equipment.GetItemResistances();
            foreach (var resistance in itemTemplateResistance)
                LoadedItemTemplates[resistance.Id].EquipableInfo.ResistList.Add(new ResistanceData((DamageType)resistance.ResistanceType, resistance.ResistanceValue));

            // Index the templates by class for the requirement table below, which is keyed by class.
            var templatesByClass = new Dictionary<EntityClasses, List<ItemTemplate>>();

            foreach (var itemTemplate in LoadedItemTemplates.Values)
            {
                if (!templatesByClass.TryGetValue(itemTemplate.Class, out var classTemplates))
                {
                    classTemplates = new List<ItemTemplate>();
                    templatesByClass.Add(itemTemplate.Class, classTemplates);
                }

                classTemplates.Add(itemTemplate);
            }

            // Add item requirements to itemTemplate. itemtemplate_requirement is a copy of the
            // client's generated.client.itemclass.reqData, whose key is the item CLASS, not the
            // template: the client looks it up as reqData.get(self.classId) in
            // client/augmentations/item.py. Reading the key as a template id dropped the 4095 rows
            // whose class id is not also a template id, and silently attached most of the rest to
            // unrelated items, because the two id spaces overlap. A requirement therefore applies
            // to every template of the class.
            var itemReqs = unitOfWork.Equipment.GetRequirementsGeneric();
            var requirementTemplates = 0;

            foreach (var itemReq in itemReqs)
            {
                if (!templatesByClass.TryGetValue((EntityClasses)itemReq.Id, out var classTemplates))
                {
                    skipped++;
                    continue;
                }

                foreach (var itemTemplate in classTemplates)
                    itemTemplate.ItemInfo.Requirements[(RequirementsType)itemReq.RequirementType] = itemReq.RequirementValue;

                requirementTemplates += classTemplates.Count;
                loaded++;
            }

            var weaponTemplates = unitOfWork.Equipment.GetWeaponItems();
            foreach (var weaponTemplate in weaponTemplates)
                LoadedItemTemplates[weaponTemplate.Id].WeaponInfo = new WeaponInfo(weaponTemplate);

            var armorTemplates = unitOfWork.Equipment.GetArmorItems();
            foreach (var armorTemplate in armorTemplates)
                LoadedItemTemplates[armorTemplate.Id].ArmorValue = armorTemplate.ArmorValue;

            var itemTemplatesData = unitOfWork.Equipment.GetItemTemplates();
            foreach (var template in itemTemplatesData)
            {
                LoadedItemTemplates[template.Id].BoundToCharacter = template.BoundToCharacterFlag != 0;
                LoadedItemTemplates[template.Id].BuyPrice = template.BuyPrice;
                LoadedItemTemplates[template.Id].HasAccountUniqueFlag = template.HasAccountUniqueFlag != 0;
                LoadedItemTemplates[template.Id].HasBoEFlag = template.HasBoEFlag != 0;
                LoadedItemTemplates[template.Id].HasCharacterUniqueFlag = template.HasCharacterUniqueFlag != 0;
                LoadedItemTemplates[template.Id].HasSellableFlag = template.HasSellableFlag != 0;
                LoadedItemTemplates[template.Id].InventoryCategory = (InventoryCategory)template.InventoryCategory;
                LoadedItemTemplates[template.Id].NotPlaceableInLockbox = template.NotPlacableInLockboxFlag != 0;
                LoadedItemTemplates[template.Id].ItemInfo.Tradable = template.NotTradableFlag != 0;
                LoadedItemTemplates[template.Id].QualityId = template.QualityId;
                LoadedItemTemplates[template.Id].SellPrice = template.SellPrice;
            }
            
            Logger.WriteLog(LogType.Initialize, $"Loaded {itemTemplatesData.Count} ItemTemplates.");

            // Most templates (schematics, components, mission items, clothing, ...) have no item_template
            // row, so their inventory category would stay 0 and AddItemToInventory would refuse them.
            // Give those a category from the class instead.
            var defaultedCategories = 0;

            // After all data is colected from db move it to EntityClass
            foreach (var entry in LoadedItemTemplates)
            {
                var itemTemplate = entry.Value;
                var entityClass = EntityClassManager.Instance.LoadedEntityClasses[itemTemplate.Class];

                if (itemTemplate.InventoryCategory == 0)
                {
                    itemTemplate.InventoryCategory = DefaultInventoryCategory(entityClass);
                    defaultedCategories++;
                }

                entityClass.ItemTemplates.Add(itemTemplate.ItemTemplateId, itemTemplate);
            }

            Logger.WriteLog(LogType.Initialize, $"Inventory category taken from the class for {defaultedCategories} ItemTemplates without item_template data.");

            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponTemplates.Count} WeaponTemplates.");
            Logger.WriteLog(LogType.Initialize, $"ItemReqs = {itemReqs.Count}, classes matched = {loaded}, classes with no template = {skipped}, templates given a requirement = {requirementTemplates}.");
        }
        
        /// <summary>
        /// The inventory tab an item belongs in when its template has no item_template row. The
        /// class augmentations decide equipment and crafting; the class name pattern decides the
        /// rest (Consumable_*, Ammo_* -> consumable; Mis*, *_Mission_* and the zone-prefixed
        /// mission items -> mission; anything else -> misc). An item_template row overrides this.
        /// </summary>
        public static InventoryCategory DefaultInventoryCategory(EntityClass entityClass)
        {
            var augmentations = entityClass.Augmentations;

            if (augmentations.Contains(AugmentationType.Recipe) || augmentations.Contains(AugmentationType.ModuleItem))
                return InventoryCategory.Crafting;

            if (augmentations.Contains(AugmentationType.Weapon) || augmentations.Contains(AugmentationType.Armor) || augmentations.Contains(AugmentationType.Equipable))
                return InventoryCategory.Equipment;

            var name = entityClass.ClassName ?? string.Empty;

            if (name.StartsWith("Component_") || name.StartsWith("Ingredient") || name.StartsWith("Resource_") || name.StartsWith("Crafting_") || IsRawMaterialClass(name))
                return InventoryCategory.Crafting;

            if (name.StartsWith("Consumable_") || name.StartsWith("Ammo_") || augmentations.Contains(AugmentationType.Customization))
                return InventoryCategory.Consumable;

            if (name.StartsWith("Mis", System.StringComparison.OrdinalIgnoreCase) || name.StartsWith("TESTMis") || name.StartsWith("MixXeno") || name.Contains("_Mission_") || IsZoneMissionItem(name))
                return InventoryCategory.Mission;

            return InventoryCategory.Misc;
        }

        // Metal1, Liquid1, Hide1, Superconductor1, Combustible1, Adhesive1, Gas1, Glass1, HardMineral1,
        // Microbes1, SoftFiber1, Solvent1, Synthetic1, Dye1: one word and a grade digit.
        private static bool IsRawMaterialClass(string name)
        {
            if (name.Length < 3 || !char.IsDigit(name[name.Length - 1]))
                return false;

            for (var i = 0; i < name.Length - 1; i++)
                if (!char.IsLetter(name[i]))
                    return false;

            return true;
        }

        private static readonly string[] ZoneMissionPrefixes =
        {
            "Burrow_", "CavesDonn_", "Cuthah_", "Dybukkar_", "Flashpoint_", "Fluxite_", "Hollow_", "Incurables_",
            "Kardash_", "Magma_", "Plains_", "Raksha_", "Rivasa_", "Runi_", "ArchBaneGenObj", "BanePlans", "ItemKey", "ItemBane"
        };

        private static bool IsZoneMissionItem(string name)
        {
            foreach (var prefix in ZoneMissionPrefixes)
                if (name.StartsWith(prefix))
                    return true;

            return false;
        }

        public void SendItemDataToClient(Client client, Item item, bool updateOnly)
        {
            // CreatePhysicalEntity
            if(!updateOnly)
                client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket( item.EntityId, item.ItemTemplate.Class));

            var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);
            // ItemInfo
            client.CallMethod(item.EntityId, new ItemInfoPacket(item, classInfo));

            // isConsumable
            client.CallMethod(item.EntityId, new SetConsumablePacket(classInfo.ItemClassInfo.IsConsumableFlag));

            if (item.ItemTemplate.WeaponInfo != null)    // weapon
            {
                // WeaponInfo
                client.CallMethod(item.EntityId, new WeaponInfoPacket(item, classInfo));

                // WeaponAmmoInfo
                client.CallMethod(item.EntityId, new WeaponAmmoInfoPacket(item.CurrentAmmo));
            }
            // ArmorInfo
            if (classInfo.ArmorClassInfo != null)
                client.CallMethod(item.EntityId, new ArmorInfoPacket(item.CurrentHitPoints, classInfo.ItemClassInfo.MaxHitPoints));
            
            // SetStackCount
            client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));
        }

        /// <summary>
        /// Tell a client an item's condition changed. Separate from SendItemDataToClient
        /// because the client acts on this one: Recv_ItemStatus is what posts
        /// UI_UPDATE_ITEM_REPAIRED, and UI_UPDATE_WEAPON_DRAWER_BROKEN_STATUS when the item
        /// crosses zero hit points in either direction. Recv_ItemInfo only stores the number.
        /// </summary>
        public void SendItemStatus(Client client, Item item, int maxHitPoints)
        {
            if (client == null || item == null)
                return;

            client.CallMethod(item.EntityId, new ItemStatusPacket(item.CurrentHitPoints, maxHitPoints));
        }

        internal void UpdateItemCurrentAmmo(IItemChange item)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Items.UpdateAmmo(item);
        }
    }
}
