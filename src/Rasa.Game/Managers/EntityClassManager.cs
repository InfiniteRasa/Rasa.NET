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
        public Dictionary<int, EntityClass> LoadedEntityClasses = new Dictionary<int, EntityClass>();

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
                string[] augmentations = Regex.Split(entityClass.Augmentations, @"\D+");

                foreach (string value in augmentations)
                    if (int.TryParse(value, out int augmentation))
                        augList.Add((AugmentationType)augmentation);

                LoadedEntityClasses.Add(entityClass.ClassId, new EntityClass(
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
                LoadedEntityClasses[itemClass.ClassId].ItemClassInfo = new ItemClassInfo(itemClass);

            // Load ArmorClasses
            var armorClassList = ArmorClassTable.LoadArmorClasses();
            foreach (var armorClass in armorClassList)
                LoadedEntityClasses[armorClass.ClassId].ArmorClassInfo = new ArmorClassInfo(armorClass);

            // Load WeaponClasses
            var weaponClassList = WeaponClassTable.LoadWeaponClasses();
            foreach (var weaponClass in weaponClassList)
                LoadedEntityClasses[weaponClass.ClassId].WeaponClassInfo = new WeaponClassInfo(weaponClass);

            // Load WeaponClasses
            var equipableClassList = EquipableClassTable.LoadEquipableClasses();
            foreach (var equipableClass in equipableClassList)
                LoadedEntityClasses[equipableClass.ClassId].EquipableClassInfo = new EquipableClassInfo(equipableClass.SlotId);
            
            // Load ItemTemplates
            ItemManager.Instance.LoadItemTemplates();

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedEntityClasses.Count} EntityClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {itemClassList.Count} ItemClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {equipableClassList.Count} EquipableClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {armorClassList.Count} ArmorClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponClassList.Count} WeaponClasses");
        }

        public EntityClass GetClassInfo(int entityClassId)
        {
            if (LoadedEntityClasses.ContainsKey(entityClassId))
                return LoadedEntityClasses[entityClassId];

            Logger.WriteLog(LogType.Error, $"entityClassId  {entityClassId} is not present in LoadedEntityClasses");

            return null;
        }

        public ArmorClassInfo GetArmorClassInfo(Item armor)
        {
            return LoadedEntityClasses[armor.ItemTemplate.ClassId].ArmorClassInfo;
        }

        public EquipableClassInfo GetEquipableClassInfo(Item equipment)
        {
            return LoadedEntityClasses[equipment.ItemTemplate.ClassId].EquipableClassInfo;
        }

        public ItemClassInfo GetItemClassInfo(Item item)
        {
            return LoadedEntityClasses[item.ItemTemplate.ClassId].ItemClassInfo;
        }

        public WeaponClassInfo GetWeaponClassInfo(Item weapon)
        {
            return LoadedEntityClasses[weapon.ItemTemplate.ClassId].WeaponClassInfo;
        }
    }
}
