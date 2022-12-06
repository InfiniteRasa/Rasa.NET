using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Rasa.Packets.MapChannel.Client;
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
        private static readonly object InstanceLock = new object();
        public Dictionary<uint, EntityClassId> ItemTemplateItemClass = new Dictionary<uint, EntityClassId>();

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
                            _instance = new ItemManager();
                    }
                }

                return _instance;
            }
        }

        private ItemManager()
        {
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
            var itemId = 0U;

            if (crafter != "")
                itemId = ItemsTable.CraftItem(itemTemplate.ItemTemplateId, stackSize, classInfo.ItemClassInfo.MaxHitPoints, crafter, 2139062144);
            else
                itemId = ItemsTable.CreateItem(itemTemplate.ItemTemplateId, stackSize, classInfo.ItemClassInfo.MaxHitPoints, 2139062144);
            
            // create physical copy of item
            var item = new Item
            {
                ItemId = itemId,
                ItemTemplate = itemTemplate,
                Stacksize = stackSize,
                Color = 2139062144     // ToDo we will have to find color in game client files
            };
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
                Stacksize = stackSize,
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
            
            // load item templates
            var itemTemplates = ItemTemplateItemClassTable.GetItemTemplateItemClass();
            foreach (var itemTemplate in itemTemplates)
            {
                LoadedItemTemplates.Add(itemTemplate.ItemTemplateId, new ItemTemplate(itemTemplate));
                ItemTemplateItemClass.Add(itemTemplate.ItemTemplateId, (EntityClassId)itemTemplate.ItemClass);
            }

            // add race requirements to itemTemplate
            var itemRaceReq = ItemTemplateRaceRequiremenTable.GetItemTemplateRaceRequirement();
            foreach (var raceReq in itemRaceReq)
                LoadedItemTemplates[raceReq.ItemTemplateId].ItemInfo.RaceReq = raceReq.RaceId;

            // add skill requirements to itemTemplate
            var itemSkillReq = ItemTemplateSkillRequirementTable.GetItemTemplateSkillRequirement();
            foreach (var skillReq in itemSkillReq)
                LoadedItemTemplates[skillReq.ItemTemplateId].EquipableInfo = new EquipableInfo(skillReq.SkillId, skillReq.SkillLevel);

            // add resistance data to itemTemplate
            var itemTemplateResistance = ItemTemplateResistanceTable.GetItemTemplateResistance();
            foreach (var resistance in itemTemplateResistance)
                LoadedItemTemplates[resistance.ItemTemplateId].EquipableInfo.ResistList.Add(new ResistanceData((DamageType)resistance.ResistanceType, resistance.ResistanceValue));

            // add item requirements to itemTemplate
            var itemReqs = ItemTemplateRequirementsTable.GetItemTemplateRequirements();
            var loaded = 0;
            var skipped = 0;

            foreach (var itemReq in itemReqs)
            {

                if (LoadedItemTemplates.ContainsKey(itemReq.ItemTemplateId))
                {
                    LoadedItemTemplates[itemReq.ItemTemplateId].ItemInfo.Requirements.Add((RequirementsType)itemReq.ReqType, itemReq.ReqValue);
                    loaded++;
                }
                else
                    skipped++;
            }

            var weaponTemplates = WeaponTemplateTable.GetWeaponTemplates();
            foreach (var weaponTemplate in weaponTemplates)
                LoadedItemTemplates[weaponTemplate.ItemTemplateId].WeaponInfo = new WeaponInfo(weaponTemplate);

            var armorTemplates = ArmorTemplateTable.GetArmorTemplates();
            foreach (var armorTemplate in armorTemplates)
                LoadedItemTemplates[armorTemplate.ItemTemplateId].ArmorValue = armorTemplate.ArmorValue;

            var itemTemplatesData = ItemTemplateTable.GetItemTemplates();
            foreach (var template in itemTemplatesData)
            {
                LoadedItemTemplates[template.ItemTemplateId].BoundToCharacter = template.BoundToCharacterFlag;
                LoadedItemTemplates[template.ItemTemplateId].BuyPrice = template.BuyPrice;
                LoadedItemTemplates[template.ItemTemplateId].HasAccountUniqueFlag = template.HasAccountUniqueFlag;
                LoadedItemTemplates[template.ItemTemplateId].HasBoEFlag = template.HasBoEFlag;
                LoadedItemTemplates[template.ItemTemplateId].HasCharacterUniqueFlag = template.HasCharacterUniqueFlag;
                LoadedItemTemplates[template.ItemTemplateId].HasSellableFlag = template.HasSellableFlag;
                LoadedItemTemplates[template.ItemTemplateId].InventoryCategory = (InventoryCategory)template.InventoryCategory;
                LoadedItemTemplates[template.ItemTemplateId].NotPlaceableInLockbox = template.NotPlaceableInLockBoxFlag;
                LoadedItemTemplates[template.ItemTemplateId].ItemInfo.Tradable = template.NotTradableFlag;
                LoadedItemTemplates[template.ItemTemplateId].QualityId = template.QualityId;
                LoadedItemTemplates[template.ItemTemplateId].SellPrice = template.SellPrice;
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
            client.CallMethod(item.EntityId, new ItemInfoPacket
            {
                CurrentHitPoints = item.CurrentHitPoints,
                MaxHitPoints = classInfo.ItemClassInfo.MaxHitPoints,
                CrafterName = item.CrafterName,
                ItemTemplateId = item.ItemTemplate.ItemTemplateId,
                HasSellableFlag = item.ItemTemplate.HasSellableFlag,
                HasCharacterUniqueFlag = item.ItemTemplate.HasCharacterUniqueFlag,
                HasAccountUniqueFlag = item.ItemTemplate.HasAccountUniqueFlag,
                HasBoEFlag = item.ItemTemplate.HasBoEFlag,
                ClassModuleIds = item.ItemTemplate.ModuleIds,
                LootModuleIds = item.ItemTemplate.LootModuleIds,
                QualityId = item.ItemTemplate.QualityId,
                BoundToCharacter = item.ItemTemplate.BoundToCharacter,
                NotTradable = item.ItemTemplate.ItemInfo.Tradable,
                NotPlaceableInLockbox = item.ItemTemplate.NotPlaceableInLockbox,
                InventoryCategory = item.ItemTemplate.InventoryCategory
            } );

            // isConsumable
            client.CallMethod(item.EntityId, new SetConsumablePacket(classInfo.ItemClassInfo.IsConsumableFlag));

            if (item.ItemTemplate.WeaponInfo != null)    // weapon
            {
                // WeaponInfo
                client.CallMethod(item.EntityId, new WeaponInfoPacket
                {
                    WeaponName = item.ItemTemplate.WeaponInfo.WeaponName,
                    ClipSize = classInfo.WeaponClassInfo.ClipSize,
                    CurrentAmmo = item.CurrentAmmo,
                    AimRate = item.ItemTemplate.WeaponInfo.AimRate,
                    ReloadTime = item.ItemTemplate.WeaponInfo.ReloadTime,
                    AltActionId = item.ItemTemplate.WeaponInfo.AltActionId,
                    AltActionArg = item.ItemTemplate.WeaponInfo.AltActionArg,
                    AeType = item.ItemTemplate.WeaponInfo.AEType,
                    AeRadius = item.ItemTemplate.WeaponInfo.AERadius,
                    RecoilAmount = item.ItemTemplate.WeaponInfo.RecoilAmount,
                    ReuseOverride = item.ItemTemplate.WeaponInfo.ReuseOverride,
                    CoolRate = item.ItemTemplate.WeaponInfo.CoolRate,
                    HeatPerShot = item.ItemTemplate.WeaponInfo.HeatPerShot,
                    ToolType = item.ItemTemplate.WeaponInfo.ToolType,
                    IsJammed = item.IsJammed,
                    AmmoPerShot = item.ItemTemplate.WeaponInfo.AmmoPerShot,
                    CammeraProfile = item.CammeraProfile
                } );

                // WeaponAmmoInfo
                client.CallMethod(item.EntityId, new WeaponAmmoInfoPacket(item.CurrentAmmo));
            }
            // ArmorInfo
            if (classInfo.ArmorClassInfo != null)
                client.CallMethod(item.EntityId, new ArmorInfoPacket(item.CurrentHitPoints, classInfo.ItemClassInfo.MaxHitPoints));
            
            // SetStackCount
            client.CallMethod(item.EntityId, new SetStackCountPacket(item.Stacksize));
        }
    }
}
