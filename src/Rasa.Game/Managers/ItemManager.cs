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

        public Item CreateVendorItem(Client client, uint itemTemplateId)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);
            var classInfo = EntityClassManager.Instance.GetClassInfo(itemTemplate.Class);

            var item = new Item
            {
                ItemTemplate = itemTemplate,
                CurrentHitPoints = classInfo.ItemClassInfo.MaxHitPoints
            } ;

            // register item
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            SendItemDataToClient(client, item, false);

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

            // add item requirements to itemTemplate
            var itemReqs = unitOfWork.Equipment.GetRequirementsGeneric();

            foreach (var itemReq in itemReqs)
            {

                if (LoadedItemTemplates.ContainsKey(itemReq.Id))
                {
                    LoadedItemTemplates[itemReq.Id].ItemInfo.Requirements.Add((RequirementsType)itemReq.RequirementType, itemReq.RequirementValue);
                    loaded++;
                }
                else
                    skipped++;
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


            // After all data is colected from db move it to EntityClass
            foreach (var entry in LoadedItemTemplates)
            {
                var itemTemplate = entry.Value;
                EntityClassManager.Instance.LoadedEntityClasses[itemTemplate.Class].ItemTemplates.Add(itemTemplate.ItemTemplateId, itemTemplate);
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponTemplates.Count} WeaponTemplates.");
            Logger.WriteLog(LogType.Initialize, $"ItemReqs = {itemReqs.Count}, loaded = {loaded}, skipped = {skipped}");
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

        internal void UpdateItemCurrentAmmo(IItemChange item)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Items.UpdateAmmo(item);
        }
    }
}
