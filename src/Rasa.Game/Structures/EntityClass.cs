using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class EntityClass
    {
        public int ClassId { get; set; }                            // entityClass.pyo
        public string ClassName { get; set; }                       // entityClass.pyo
        public int MeshId { get; set; }                             // entityClass.pyo
        public int LodMeshId { get; set; }                          // entityClass.pyo for all entityClasses LodMeshId is none (null)
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

        public Dictionary<int, ItemTemplate> ItemTemplates = new Dictionary<int, ItemTemplate>();
        public ItemClassInfo ItemClassInfo { get; set; }
        public ArmorClassInfo ArmorClassInfo { get; set; }
        public EquipableClassInfo EquipableClassInfo { get; set; }
        public WeaponClassInfo WeaponClassInfo { get; set; }

        public EntityClass(int classId, string className, int meshId, short classCollisionRole, List<AugmentationType> augList, bool targetFlag)
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
        //public int ClassId { get; set; }
        public int MinDamageAbsorbed { get; set; }
        public int MaxDamageAbsorbed { get; set; }
        public int RegenRate { get; set; }

        public ArmorClassInfo(ArmorClassEntry armorClass)
        {
            //ClassId = armorClass.ClassId;
            MinDamageAbsorbed = armorClass.MinDamageAbsorbed;
            MaxDamageAbsorbed = armorClass.MaxDamageAbsorbed;
            RegenRate = armorClass.RegenRate;
        }
    }

    public class EquipableClassInfo
    {
        public EquipmentSlots EquipmentSlotId { get; set; }

        public EquipableClassInfo(int slotId)
        {
            EquipmentSlotId = (EquipmentSlots)slotId;
        }
    }

    public class ItemClassInfo
    {
        public int InventoryIconStringId { get; set; }
        public int LootValue { get; set; }
        public bool HiddenInventoryFlag { get; set; }
        public bool IsConsumableFlag { get; set; }
        public int MaxHitPoints { get; set; }
        public int StackSize { get; set; }
        public int DragAudioSetId { get; set; }
        public int DropAudioSetId { get; set; }

        public ItemClassInfo(ItemClassEntry itemClassInfo)
        {
            InventoryIconStringId = itemClassInfo.InventoryIconStringId;
            LootValue = itemClassInfo.LootValue;
            HiddenInventoryFlag = itemClassInfo.HiddenInventoryFlag;
            IsConsumableFlag = itemClassInfo.IsConsumableFlag;
            MaxHitPoints = itemClassInfo.MaxHitPoints;
            StackSize = itemClassInfo.StackSize;
            DragAudioSetId = itemClassInfo.DragAudioSetId;
            DropAudioSetId = itemClassInfo.DropAudioSetId;
        }
    }

    public class WeaponClassInfo
    {
        //public int ClassId { get; set; }
        public short WeaponTemplateid { get; set; }
        public ActionId WeaponAttackActionId { get; set; }
        public short WeaponAttackArgId { get; set; }
        public short DrawActionId { get; set; }
        public short StowActionId { get; set; }
        public short ReloadActionId { get; set; }
        public int AmmoClassId { get; set; }
        public short ClipSize { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public short DamageType { get; set; }
        public short Velocity { get; set; }
        public short WeaponAnimConditionCode { get; set; }
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
        public short UnkArg6 { get; set; }
        public bool UnkArg7 { get; set; }
        public short UnkArg8 { get; set; }

        public WeaponClassInfo(WeaponClassEntry weaponInfo)
        {
            WeaponTemplateid = weaponInfo.WeaponTemplateid;
            WeaponAttackActionId = (ActionId)weaponInfo.WeaponAttackActionId;
            WeaponAttackArgId = weaponInfo.WeaponAttackArgId;
            DrawActionId = weaponInfo.DrawActionId;
            StowActionId = weaponInfo.StowActionId;
            ReloadActionId = weaponInfo.ReloadActionId;
            AmmoClassId = weaponInfo.AmmoClassId;
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
            UnkArg1 = weaponInfo.UnkArg1;
            UnkArg2 = weaponInfo.UnkArg2;
            UnkArg3 = weaponInfo.UnkArg3;
            UnkArg4 = weaponInfo.UnkArg4;
            UnkArg5 = weaponInfo.UnkArg5;
            UnkArg6 = weaponInfo.UnkArg6;
            UnkArg7 = weaponInfo.UnkArg7;
            UnkArg8 = weaponInfo.UnkArg8;
        }
    }
}
