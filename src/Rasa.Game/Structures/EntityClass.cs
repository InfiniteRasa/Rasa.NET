using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using World;

    public class EntityClass
    {
        public uint ClassId { get; set; }                           // entityClass.pyo
        public string ClassName { get; set; }                       // entityClass.pyo
        public int MeshId { get; set; }                             // entityClass.pyo
        public uint LodMeshId { get; set; }                         // entityClass.pyo for all entityClasses LodMeshId is none (null)
        public double LodScreenPercentage { get; set; }             // entityClass.pyo for all entityClasses LodScreenPercentage is none (null)
        public short ClassCollisionRole { get; set; }               // entityClass.pyo
        public List<AugmentationType> Augmentations { get; set; }   // entityClass.pyo
        public bool TargetFlag { get; set; }                        // entityClass.pyo

        public int SkeletonId { get; set; }
        public int CameraCollideFlag { get; set; }
        public int CullingLayerType { get; set; }
        public int CombinedGroupId { get; set; }
        public int DiscardCombined { get; set; }
        public int CastsShadowFlag { get; set; }
        public int PickableFlag { get; set; }
        public int TargetPickOverride { get; set; }
        public int HasServerSkeleton { get; set; }

        public Dictionary<uint, ItemTemplate> ItemTemplates = new Dictionary<uint, ItemTemplate>();
        public ItemClassInfo ItemClassInfo { get; set; }
        public ArmorClassInfo ArmorClassInfo { get; set; }
        public EquipableClassInfo EquipableClassInfo { get; set; }
        public WeaponClassInfo WeaponClassInfo { get; set; }

        public EntityClass(uint classId, string className, uint meshId, short classCollisionRole, List<AugmentationType> augList, bool targetFlag)
        {
            ClassId = classId;
            ClassName = className;
            MeshId = MeshId;
            ClassCollisionRole = classCollisionRole;
            Augmentations = augList;
            TargetFlag = targetFlag;
        }
    }

    public class ArmorClassInfo
    {
        public uint MinDamageAbsorbed { get; set; }
        public uint MaxDamageAbsorbed { get; set; }
        public int RegenRate { get; set; }

        public ArmorClassInfo(ArmorClassEntry armorClass)
        {
            MinDamageAbsorbed = armorClass.MinDamageAbsorbed;
            MaxDamageAbsorbed = armorClass.MaxDamageAbsorbed;
            RegenRate = armorClass.RegenRate;
        }
    }

    public class EquipableClassInfo
    {
        public EquipmentData EquipmentSlotId { get; set; }

        public EquipableClassInfo(EquipmentData slotId)
        {
            EquipmentSlotId = slotId;
        }
    }

    public class ItemClassInfo
    {
        public uint InventoryIconStringId { get; set; }
        public uint LootValue { get; set; }
        public byte HidenInventoryFlag { get; set; }
        public byte IsConsumableFlag { get; set; }
        public int MaxHitPoints { get; set; }
        public uint StackSize { get; set; }
        public uint DragAudioSetId { get; set; }
        public uint DropAudioSetId { get; set; }

        public ItemClassInfo(ItemClassEntry itemClassInfo)
        {
            InventoryIconStringId = itemClassInfo.InventoryIconStringId;
            LootValue = itemClassInfo.LootValue;
            HidenInventoryFlag = itemClassInfo.HidenInventoryFlag;
            IsConsumableFlag = itemClassInfo.IsConsumableFlag;
            MaxHitPoints = itemClassInfo.MaxHitPoints;
            StackSize = itemClassInfo.StackSize;
            DragAudioSetId = itemClassInfo.DragAudioSetId;
            DropAudioSetId = itemClassInfo.DropAudioSetId;
        }
    }

    public class WeaponClassInfo
    {
        public uint WeaponTemplateid { get; set; }
        public ActionId WeaponAttackActionId { get; set; }
        public uint WeaponAttackArgId { get; set; }
        public byte DrawActionId { get; set; }
        public byte StowActionId { get; set; }
        public byte ReloadActionId { get; set; }
        public EntityClasses AmmoClassId { get; set; }
        public uint ClipSize { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public byte DamageType { get; set; }
        public int Velocity { get; set; }
        public uint WeaponAnimConditionCode { get; set; }
        public bool WindupOverride { get; set; }
        public bool RecoveryOverride { get; set; }
        public bool ReuseOverride { get; set; }
        public bool ReloadOverride { get; set; }
        public bool RangeType { get; set; }
        public bool UnkArg1 { get; set; }
        public bool UnkArg2 { get; set; }
        public bool UnkArg3 { get; set; }
        public bool UnkArg4 { get; set; }
        public bool UnkArg5 { get; set; }
        public uint UnkArg6 { get; set; }
        public bool UnkArg7 { get; set; }
        public uint UnkArg8 { get; set; }

        public WeaponClassInfo(WeaponClassEntry weaponInfo)
        {
            WeaponTemplateid = weaponInfo.Id;
            WeaponAttackActionId = (ActionId)weaponInfo.AttackActionId;
            WeaponAttackArgId = weaponInfo.AttackActionArgId;
            DrawActionId = weaponInfo.DrawActionId;
            StowActionId = weaponInfo.StowActionId;
            ReloadActionId = weaponInfo.ReloadActionId;
            AmmoClassId = (EntityClasses)weaponInfo.AmmoClassId;
            ClipSize = weaponInfo.ClipSize;
            MinDamage = weaponInfo.MinDamage;
            MaxDamage = weaponInfo.MaxDamage;
            DamageType = weaponInfo.DamageType;
            Velocity = weaponInfo.Velocity;
            WeaponAnimConditionCode = weaponInfo.WeaponAnimConditionCode;
            WindupOverride = weaponInfo.WindupOverride;
            RecoveryOverride = weaponInfo.RecoveryOverride;
            ReuseOverride = weaponInfo.ReuseOverride;
            ReloadOverride = weaponInfo.ReloadOverride;
            RangeType = weaponInfo.RangeType;
            UnkArg1 = weaponInfo.UnknownArg1;
            UnkArg2 = weaponInfo.UnknownArg2;
            UnkArg3 = weaponInfo.UnknownArg3;
            UnkArg4 = weaponInfo.UnknownArg4;
            UnkArg5 = weaponInfo.UnknownArg5;
            UnkArg6 = weaponInfo.UnknownArg6;
            UnkArg7 = weaponInfo.UnknownArg7;
            UnkArg8 = weaponInfo.UnknownArg8;
        }
    }
}
