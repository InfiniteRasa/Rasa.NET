using System;
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
            // insert into items table to get unique ItemId
            var itemId = ItemsTable.CreateItem(itemTemplate.ItemTemplateId, stackSize, -2139062144);
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

        public void GetItemArmorData()
        {
            var armorTemplates = ArmorTemplateTable.GetArmorTemplates();
            foreach (var template in armorTemplates)
            {
                // update LoadedItemTemplates
                LoadedItemTemplates[template.ItemTemplateId].Armor = new ArmorTupple
                {
                    ArmorValue = template.ArmorValue,
                    DamageAbsorbed = template.DamageAbsorbed,
                    RegenRate = template.RegenRate
                };
            }
            Console.WriteLine($"Loaded {armorTemplates.Count} ArmorTemplates.");
        }

        public void GetItemEquipmentData()
        {
            var equipmentTemplates = EquipmentTemplateTable.GetEquipmentTemplates();

            foreach (var template in equipmentTemplates)
            {
                // update LoadedItemTemplates
                LoadedItemTemplates[template.ItemTemplateId].Equipment = new EquipmentTupple
                {
                    EquiptmentSlotType = template.SlotType,
                    RequiredSkillId = template.RequiredSkillId,
                    RequiredSkillMinVal = template.RequiredSkillMinVal
                };
            }
            Console.WriteLine($"Loaded {equipmentTemplates.Count} EquipmentTemplates.");
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
            if (LoadedItemTemplates.ContainsKey(itemTemplateId))
                return LoadedItemTemplates[itemTemplateId];
            else
                return null;
        }

        public void GetItemTemplates()
        {
            Console.WriteLine("Loading ItemTemplates from db...");
            var itemTemplates = ItemTemplateTable.GetItemTemplates();
            foreach (var template in itemTemplates)
                LoadedItemTemplates.Add(template.ItemTemplateId, new ItemTemplate(template));

            Console.WriteLine($"Loaded {itemTemplates.Count} ItemTemplates.");
        }

        public void GetItemWeaponData()
        {
            var weaponTemplates = WeaponTemplateTable.GetWeaponTemplates();
            foreach (var template in weaponTemplates)
            {
                // update LoadedItemTemplates
                LoadedItemTemplates[template.ItemTemplateId].Weapon = new WeaponTupple
                {
                    ClipSize = template.ClipSize,
                    AimRate = template.AimRate,
                    ReloadTime = template.ReloadTime,
                    AltActionId = template.AltActionId,
                    AltActionArg = template.AltActionArg,
                    AeType = template.AeType,
                    AeRadius = template.AeRadius,
                    RecoilAmount = template.RecoilAmount,
                    ReuseOverride = template.ReuseOverride,
                    CoolRate = template.CoolRate,
                    HeatPerShot = template.HeatPerShot,
                    ToolType = template.ToolType,
                    AmmoPerShot = template.AmmoPerShot,
                    MinDamage = template.MinDamage,
                    MaxDamage = template.MaxDamage,
                    AmmoClassId = template.AmmoClassId,
                    DamageType = template.DamageType,
                    WindupTime = template.WindupTime,
                    RecoveryTime = template.RecoveryTime,
                    RefireTime = template.RefireTime,
                    Range = template.Range,
                    AltMaxDamage = template.AltMaxDamage,
                    AltDamageType = template.AltDamageType,
                    AltRange = template.AltRange,
                    AltAERadius = template.AltAERadius,
                    AltAEType = template.AltAEType,
                    AttackType = template.AttackType
                };
            }
            Console.WriteLine($"Loaded {weaponTemplates.Count} WeaponTemplates.\n");
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
                    CurrentAmmo = item.CurrentAmmo,
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
                mapClient.Player.Client.SendPacket(item.EntityId, new WeaponAmmoInfoPacket { AmmoInfo = item.CurrentAmmo });
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
    }
}
