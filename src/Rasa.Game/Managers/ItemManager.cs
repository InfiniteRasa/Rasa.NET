using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Database.Tables.Character;
    using Database.Tables.World;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class ItemManager
    {
        private static ItemManager _instance;
        private static readonly object InstanceLock = new object();
        public Dictionary<int, ItemTemplate> LoadedItemTemplates = new Dictionary<int, ItemTemplate>();

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
        
        
        public Item CreateFromTemplateId(uint ownerId, int ownerSlotId, int itemTemplateId, int stackSize)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return null;
            var item = CreateItem(ownerId, ownerSlotId, itemTemplate, stackSize);
            return item;
        }

        public Item CreateItem(uint ownerId, int ownerSlotId, ItemTemplate itemTemplate, int stackSize)
        {
            if (itemTemplate == null)
                return null;
            var item = new Item();
            var id = ItemsTable.CreateItem(ownerId, ownerSlotId, itemTemplate.ItemTemplateId, stackSize);
            //item.EntityId = id;
            item.ItemTemplate = itemTemplate;
            item.OwnerId = ownerId;
            item.OwnerSlotId = ownerSlotId;
            item.Stacksize = stackSize;
            // register item
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            return item;
        }

        public void GetItemArmorData()
        {
            var rows = ArmorTemplateTable.GetDbRows();
            for (var i = 0; i < rows; i++)
            {
                // get data from db
                var data = ArmorTemplateTable.GetArmorTemplates(i);
                // update LoadedItemTemplates
                LoadedItemTemplates[data.ItemTemplateId].Armor = new ArmorTupple
                {
                    ArmorValue = data.ArmorValue,
                    DamageAbsorbed = data.DamageAbsorbed,
                    RegenRate = data.RegenRate
                };
            }
            Console.WriteLine("Loaded {0} ArmorTemplates.", rows);
        }

        public void GetItemEquipmentData()
        {
            var rows = EquipmentTemplateTable.GetDbRows();
            for (var i = 0; i < rows; i++)
            {
                // get data from db
                var data = EquipmentTemplateTable.GetEquipmentTemplates(i);
                // update LoadedItemTemplates
                LoadedItemTemplates[data.ItemTemplateId].Equipment = new EquipmentTupple
                {
                    EquiptmentSlotType = data.SlotType,
                    RequiredSkillId = data.RequiredSkillId,
                    RequiredSkillMinVal = data.RequiredSkillMinVal
                };
            }
            Console.WriteLine("Loaded {0} EquipmentTemplates.", rows);
        }

        public Item GetItemFromTemplateId(uint itemId, uint ownerId, int ownerSlot, int itemTemplateId, int stackSize)
        {
            var itemTemplate = GetItemTemplateById(itemTemplateId);
            if (itemTemplate == null)
                return null;
            var item = new Item
            {
                //EntityId = itemId,
                OwnerId = ownerId,
                OwnerSlotId = ownerSlot,
                ItemTemplate = itemTemplate,
                Stacksize = stackSize,
            };
            // register item
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            return item;
        }
        
        public ItemTemplate GetItemTemplateById(int itemTemplateId)
        {
            ItemTemplate itemTemplate = null;
            if (LoadedItemTemplates.ContainsKey(itemTemplateId))
                itemTemplate = LoadedItemTemplates[itemTemplateId];
            return itemTemplate;
        }

        public void GetItemTemplates()
        {
            var rows = ItemTemplateTable.GetDbRows();
            Console.WriteLine("Loading ItemTemplates from db...");
            for (var i = 0; i < rows; i++)
            {
                var data = ItemTemplateTable.GetItemTemplates(i);
                LoadedItemTemplates.Add(data.ItemTemplateId, new ItemTemplate
                {
                    ItemTemplateId = data.ItemTemplateId,
                    ClassId = data.ClassId,
                    QualityId = data.QualityId,
                    ItemType = data.ItemType,
                    HasSellableFlag = data.HasSellableFlag,
                    NotTradable = data.NotTradeableFlag,
                    HasCharacterUniqueFlag = data.HasCharacterUniqueFlag,
                    HasAccountUniqueFlag = data.HasAccountUniqueFlag,
                    HasBoEFlag = data.HasBoEFlag,
                    BoundToCharacter = data.BoundToCharacterFlag,
                    NotPlaceableInLockbox = data.NotPlaceableInLockBoxFlag,
                    InventoryCategory = data.InventoryCategory,
                    ReqLevel = data.ReqLevel,
                    BuyPrice = data.BuyPrice,
                    SellPrice = data.SellPrice,
                    Stacksize = data.StackSize
                });
            }
            Console.WriteLine("Loaded {0} ItemTemplates.", rows);
        }

        public void GetItemWeaponData()
        {
            var rows = WeaponTemplateTable.GetDbRows();
            for (var i = 0; i < rows; i++)
            {
                // get data from db
                var data = WeaponTemplateTable.GetWeaponTemplates(i);
                // update LoadedItemTemplates
                LoadedItemTemplates[data.ItemTemplateId].Weapon = new WeaponTupple
                {
                    ClipSize = data.ClipSize,
                    CurrentAmmo = data.CurrentAmmo,
                    AimRate = data.AimRate,
                    ReloadTime = data.ReloadTime,
                    AltActionId = data.AltActionId,
                    AltActionArg = data.AltActionArg,
                    AeType = data.AeType,
                    AeRadius = data.AeRadius,
                    RecoilAmount = data.RecoilAmount,
                    ReuseOverride = data.ReuseOverride,
                    CoolRate = data.CoolRate,
                    HeatPerShot = data.HeatPerShot,
                    ToolType = data.ToolType,
                    AmmoPerShot = data.AmmoPerShot,
                    MinDamage = data.MinDamage,
                    MaxDamage = data.MaxDamage,
                    AmmoClassId = data.AmmoClassId,
                    DamageType = data.DamageType,
                    WindupTime = data.WindupTime,
                    RecoveryTime = data.RecoveryTime,
                    RefireTime = data.RefireTime,
                    Range = data.Range,
                    AltMaxDamage = data.AltMaxDamage,
                    AltDamageType = data.AltDamageType,
                    AltRange = data.AltRange,
                    AltAERadius = data.AltAERadius,
                    AltAEType = data.AltAEType,
                    AttackType = data.AttackType
                };
            }
            Console.WriteLine("Loaded {0} WeaponTemplates.\n", rows);
        }

        public void LoadItems()
        {
            // Load ItemTemplates from DB
            GetItemTemplates();            
            // load additional equipment info data from db
            GetItemEquipmentData();
            // load additional armor data
            GetItemArmorData();
            // load additional weapon data
            GetItemWeaponData();
            // load additional misc data
            // todo
        }
        
        public void SendItemDataToClient(MapChannelClient mapClient, Item item)
        {
            // CreatePhysicalEntity
            mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket( (int)item.EntityId, item.ItemTemplate.ClassId));
            // ItemInfo
            mapClient.Player.Client.SendPacket(item.EntityId, new ItemInfoPacket
            {
                CurrentHitPoints = item.ItemTemplate.CurrentHitPoints,
                MaxHitPoints = item.ItemTemplate.MaxHitPoints,
                CrafterName = item.ItemTemplate.CrafterName,
                ItemTemplateId = item.ItemTemplate.ItemTemplateId,
                HasSellableFlag = item.ItemTemplate.HasSellableFlag,
                HasCharacterUniqueFlag = item.ItemTemplate.HasCharacterUniqueFlag,
                HasAccountUniqueFlag = item.ItemTemplate.HasAccountUniqueFlag,
                HasBoEFlag = item.ItemTemplate.HasBoEFlag,
                ClassModuleIds = item.ItemTemplate.ClassModuleIds,
                LootModuleIds = item.ItemTemplate.LootModuleIds,
                QualityId = item.ItemTemplate.QualityId,
                BoundToCharacter = item.ItemTemplate.BoundToCharacter,
                NotTradable = item.ItemTemplate.NotTradable,
                NotPlaceableInLockbox = item.ItemTemplate.NotPlaceableInLockbox,
                InventoryCategory = item.ItemTemplate.InventoryCategory
            } );

            if (item.ItemTemplate.ItemType == 1)    // weapon
            {
                // WeaponInfo
                mapClient.Player.Client.SendPacket(item.EntityId, new WeaponInfoPacket
                {
                    WeaponName = item.ItemTemplate.Weapon.WeaponName,
                    ClipSize = item.ItemTemplate.Weapon.ClipSize,
                    CurrentAmmo = item.ItemTemplate.Weapon.CurrentAmmo,
                    AimRate = item.ItemTemplate.Weapon.AimRate,
                    ReloadTime = item.ItemTemplate.Weapon.ReloadTime,
                    AltActionId = item.ItemTemplate.Weapon.AltActionId,
                    AltActionArg = item.ItemTemplate.Weapon.AltActionArg,
                    AeType = item.ItemTemplate.Weapon.AeType,
                    AeRadius = item.ItemTemplate.Weapon.AeRadius,
                    RecoilAmount = item.ItemTemplate.Weapon.RecoilAmount,
                    ReuseOverride = item.ItemTemplate.Weapon.ReuseOverride,
                    CoolRate = item.ItemTemplate.Weapon.CoolRate,
                    HeatPerShot = item.ItemTemplate.Weapon.HeatPerShot,
                    ToolType = item.ItemTemplate.Weapon.ToolType,
                    IsJammed = item.ItemTemplate.Weapon.IsJammed,
                    AmmoPerShot = item.ItemTemplate.Weapon.AmmoPerShot,
                    CammeraProfile = item.ItemTemplate.Weapon.CammeraProfile
                } );
                // WeaponAmmoInfo
                mapClient.Player.Client.SendPacket(item.EntityId, new WeaponAmmoInfoPacket { AmmoInfo = item.WeaponAmmoCount });
            }

            if (item.ItemTemplate.ItemType == 2)    // armor
            {
                // ArmorInfo
                // we dont need to send this, it is just an empty method on the client which has no effect
                //mapClient.Player.Client.SendPacket(item.EntityId, new ArmorInfoPacket { CurrentHitPoints = item.ItemTemplate.CurrentHitPoints, MaxHitPoints = item.ItemTemplate.MaxHitPoints});
            }
            // SetStackCount
            mapClient.Player.Client.SendPacket(item.EntityId, new SetStackCountPacket { StackSize = item.Stacksize });
        }

        public void SendItemDestruction(MapChannelClient mapClient, Item item)
        { 
            EntityManager.Instance.FreeEntity(item.EntityId);
            mapClient.Player.Client.SendPacket(5, new DestroyPhysicalEntityPacket {EntityId = item.EntityId});
        }
    }
}
