using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class ItemManager
    {
        private static ItemManager _instance;
        private static readonly object InstanceLock = new object();
        public Dictionary<int, int> ItemTemplateItemClass = new Dictionary<int, int>();

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
        
        public Item CreateFromTemplateId(uint characterId, int slotId, int itemTemplateId, int stackSize)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return null;
            var item = CreateItem(characterId, slotId, itemTemplate, stackSize);
            return item;
        }

        public Item CreateItem(uint characterId, int slotId, ItemTemplate itemTemplate, int stackSize)
        {
            if (itemTemplate == null)
                return null;

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[itemTemplate.ClassId];

            // dont create more then max stackSize
            if (classInfo.ItemClassInfo.StackSize < stackSize)
                stackSize = classInfo.ItemClassInfo.StackSize;

            // insert into items table to get unique ItemId
            var itemId = ItemsTable.CreateItem(itemTemplate.ItemTemplateId, stackSize, classInfo.ItemClassInfo.MaxHitPoints, -2139062144);
            // insert new item into character table
            CharacterInventoryTable.AddInvItem(characterId, slotId, itemId);
            // create physical copy of item
            var item = new Item
            {
                ItemId = itemId,
                ItemTemplate = itemTemplate,
                OwnerId = characterId,
                OwnerSlotId = slotId,
                Stacksize = stackSize,
                Color = -2139062144     // ToDo we will have to find color in game client files
            };
            // register item
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);

            return item;
        }
        public Item GetItemFromTemplateId(uint itemId, uint characterId, int slotId, int itemTemplateId, int stackSize)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);

            if (itemTemplate == null)
                return null;

            var item = new Item
            {
                OwnerId = characterId,
                OwnerSlotId = slotId,
                ItemTemplate = itemTemplate,
                Stacksize = stackSize,
            };
            // register item
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);

            return item;
        }
        
        public ItemTemplate GetItemTemplateById(int itemTemplateId)
        {
            if (ItemTemplateItemClass.ContainsKey(itemTemplateId))
            {
                var classId = ItemTemplateItemClass[itemTemplateId];
                return EntityClassManager.Instance.LoadedEntityClasses[classId].ItemTemplates[itemTemplateId];
            }

            Logger.WriteLog(LogType.Error, $"Unknown temTemplateId = {itemTemplateId}");

            return null;
        }

        public void LoadItemTemplates()
        {
            Logger.WriteLog(LogType.Initialize, "Loading ItemTemplates from db...");

            var LoadedItemTemplates = new Dictionary<int, ItemTemplate>();
            
            // load item templates
            var itemTemplates = ItemTemplateItemClassTable.GetItemTemplateItemClass();
            foreach (var itemTemplate in itemTemplates)
            {
                LoadedItemTemplates.Add(itemTemplate.ItemTemplateId, new ItemTemplate(itemTemplate));
                ItemTemplateItemClass.Add(itemTemplate.ItemTemplateId, itemTemplate.ItemClassId);
            }

            // add race requirements to itemTemplate
            var itemRaceReq = ItemTemplateRaceRequiremenTable.GetItemTemplateRaceRequirement();
            foreach (var raceReq in itemRaceReq)
                LoadedItemTemplates[raceReq.ItemTemplateId].ItemInfo.RaceReq = raceReq.RaceId;

            // add skill requirements to itemTemplate
            var itemSkillReq = ItemTemplateSkillRequirementTable.GetItemTemplateSkillRequirement();
            foreach (var skillReq in itemSkillReq)
                LoadedItemTemplates[skillReq.ItemTemplateId].EquipableInfo = new EquipableInfo(skillReq.SkillId, skillReq.SkillLevel);

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
                LoadedItemTemplates[template.ItemTemplateId].InventoryCategory = template.InventoryCategory;
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
                EntityClassManager.Instance.LoadedEntityClasses[itemTemplate.ClassId].ItemTemplates.Add(itemTemplate.ItemTemplateId, itemTemplate);
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponTemplates.Count} WeaponTemplates.");
            Logger.WriteLog(LogType.Initialize, $"ItemReqs = {itemReqs.Count}, loaded = {loaded}, skipped = {skipped}");
        }
        
        public void SendItemDataToClient(MapChannelClient mapClient, Item item)
        {
            // CreatePhysicalEntity
            mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket( item.EntityId, item.ItemTemplate.ClassId));

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[item.ItemTemplate.ClassId];
            // ItemInfo
            mapClient.Player.Client.SendPacket(item.EntityId, new ItemInfoPacket
            {
                CurrentHitPoints = item.CurrentHitPoints,
                MaxHitPoints = classInfo.ItemClassInfo.MaxHitPoints,
                CrafterName = item.CrafterName,
                ItemTemplateId = item.ItemTemplate.ItemTemplateId,
                HasSellableFlag = item.ItemTemplate.HasSellableFlag,
                HasCharacterUniqueFlag = item.ItemTemplate.HasCharacterUniqueFlag,
                HasAccountUniqueFlag = item.ItemTemplate.HasAccountUniqueFlag,
                HasBoEFlag = item.ItemTemplate.HasBoEFlag,
                ClassModuleIds = item.ItemTemplate.ClassModuleIds,
                LootModuleIds = item.ItemTemplate.LootModuleIds,
                QualityId = item.ItemTemplate.QualityId,
                BoundToCharacter = item.ItemTemplate.BoundToCharacter,
                NotTradable = item.ItemTemplate.ItemInfo.Tradable,
                NotPlaceableInLockbox = item.ItemTemplate.NotPlaceableInLockbox,
                InventoryCategory = (InventoryCategory)item.ItemTemplate.InventoryCategory
            } );

            if (item.ItemTemplate.WeaponInfo != null)    // weapon
            {
                // WeaponInfo
                mapClient.Player.Client.SendPacket(item.EntityId, new WeaponInfoPacket
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
                mapClient.Player.Client.SendPacket(item.EntityId, new WeaponAmmoInfoPacket(item.CurrentAmmo));
            }
            // ArmorInfo
            if (classInfo.ArmorClassInfo != null)
                mapClient.Player.Client.SendPacket(item.EntityId, new ArmorInfoPacket(item.CurrentHitPoints, classInfo.ItemClassInfo.MaxHitPoints));
            
            // SetStackCount
            mapClient.Player.Client.SendPacket(item.EntityId, new SetStackCountPacket { StackSize = item.Stacksize });
        }
    }
}
