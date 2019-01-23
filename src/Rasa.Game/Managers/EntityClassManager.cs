using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.World;
    using Structures;

    public class EntityClassManager
    {
        private static EntityClassManager _instance;
        private static readonly object InstanceLock = new object();
        public Dictionary<EntityClassId, EntityClass> LoadedEntityClasses = new Dictionary<EntityClassId, EntityClass>();

        public static EntityClassManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new EntityClassManager();                                                           

                    }
                }

                return _instance;
            }
        }

        private EntityClassManager()
        {
        }

        public void LoadEntityClasses()
        {
            Logger.WriteLog(LogType.Initialize, "Loading data from db ...");
            var entityClassList = EntityClassTable.LoadEntityClass();

            foreach (var entityClass in entityClassList)
            {
                // Parse AugmentationList
                var augList = new List<AugmentationType>();
                var augmentations = Regex.Split(entityClass.Augmentations, @"\D+");

                foreach (var value in augmentations)
                    if (int.TryParse(value, out var augmentation))
                        augList.Add((AugmentationType)augmentation);

                LoadedEntityClasses.Add((EntityClassId)entityClass.ClassId, new EntityClass(
                    entityClass.ClassId,
                    entityClass.ClassName,
                    entityClass.MeshId,
                    entityClass.ClassCollisionRole,
                    augList,
                    entityClass.TargetFlag
                    ));
            };

            // Load itemClasses
            var itemClassList = ItemClassTable.LoadItemClasses();
            foreach (var itemClass in itemClassList)
                LoadedEntityClasses[(EntityClassId)itemClass.ClassId].ItemClassInfo = new ItemClassInfo(itemClass);

            // Load ArmorClasses
            var armorClassList = ArmorClassTable.LoadArmorClasses();
            foreach (var armorClass in armorClassList)
                LoadedEntityClasses[(EntityClassId)armorClass.ClassId].ArmorClassInfo = new ArmorClassInfo(armorClass);

            // Load WeaponClasses
            var weaponClassList = WeaponClassTable.LoadWeaponClasses();
            foreach (var weaponClass in weaponClassList)
                LoadedEntityClasses[(EntityClassId)weaponClass.ClassId].WeaponClassInfo = new WeaponClassInfo(weaponClass);

            // Load EquipableClasses
            var equipableClassList = EquipableClassTable.LoadEquipableClasses();
            foreach (var equipableClass in equipableClassList)
                LoadedEntityClasses[(EntityClassId)equipableClass.ClassId].EquipableClassInfo = new EquipableClassInfo((EquipmentData)equipableClass.SlotId);
            
            // Load ItemTemplates
            ItemManager.Instance.LoadItemTemplates();

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedEntityClasses.Count} EntityClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {itemClassList.Count} ItemClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {equipableClassList.Count} EquipableClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {armorClassList.Count} ArmorClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponClassList.Count} WeaponClasses");
        }

        public EntityClass GetClassInfo(EntityClassId entityClassId)
        {
            if (LoadedEntityClasses.ContainsKey(entityClassId))
                return LoadedEntityClasses[entityClassId];

            Logger.WriteLog(LogType.Error, $"entityClassId  {entityClassId} is not present in LoadedEntityClasses");

            return null;
        }

        public ArmorClassInfo GetArmorClassInfo(Item armor)
        {
            return LoadedEntityClasses[armor.ItemTemplate.Class].ArmorClassInfo;
        }

        public EquipableClassInfo GetEquipableClassInfo(Item equipment)
        {
            return LoadedEntityClasses[equipment.ItemTemplate.Class].EquipableClassInfo;
        }

        public ItemClassInfo GetItemClassInfo(Item item)
        {
            return LoadedEntityClasses[item.ItemTemplate.Class].ItemClassInfo;
        }

        public WeaponClassInfo GetWeaponClassInfo(Item weapon)
        {
            return LoadedEntityClasses[weapon.ItemTemplate.Class].WeaponClassInfo;
        }
    }
}
