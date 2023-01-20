using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Managers
{
    using Data;
    using Rasa.Game;
    using Rasa.Repositories.UnitOfWork;
    using Structures;

    public class EntityClassManager
    {
        private static EntityClassManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        public Dictionary<EntityClasses, EntityClass> LoadedEntityClasses = new Dictionary<EntityClasses, EntityClass>();

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
                            _instance = new EntityClassManager(Server.GameUnitOfWorkFactory);                                                           

                    }
                }

                return _instance;
            }
        }

        private EntityClassManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void LoadEntityClasses()
        {
            Logger.WriteLog(LogType.Initialize, "Loading data from db ...");
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entityClassList = unitOfWork.EntityClasses.Get();

            foreach (var entityClass in entityClassList)
            {
                // Parse AugmentationList
                var augList = new List<AugmentationType>();
                var augmentations = Regex.Split(entityClass.AugList, @"\D+");

                foreach (var value in augmentations)
                    if (int.TryParse(value, out var augmentation))
                        augList.Add((AugmentationType)augmentation);

                LoadedEntityClasses.Add((EntityClasses)entityClass.Id, new EntityClass(
                    entityClass.Id,
                    entityClass.ClassName,
                    entityClass.MeshId,
                    entityClass.ClassCollisionRole,
                    augList,
                    entityClass.TargetFlag != 0
                    ));
            };

            // Load itemClasses
            var itemClassList = unitOfWork.ItemClasses.Get();
            foreach (var itemClass in itemClassList)
                LoadedEntityClasses[(EntityClasses)itemClass.Id].ItemClassInfo = new ItemClassInfo(itemClass);

            // Load ArmorClasses
            var armorClassList = unitOfWork.ArmorClasses.Get();
            foreach (var armorClass in armorClassList)
                LoadedEntityClasses[(EntityClasses)armorClass.Id].ArmorClassInfo = new ArmorClassInfo(armorClass);

            // Load WeaponClasses
            var weaponClassList = unitOfWork.WeaponClasses.Get();
            foreach (var weaponClass in weaponClassList)
                LoadedEntityClasses[(EntityClasses)weaponClass.Id].WeaponClassInfo = new WeaponClassInfo(weaponClass);

            // Load EquipableClasses
            var equipableClassList = unitOfWork.EquipableClasses.Get();
            foreach (var equipableClass in equipableClassList)
                LoadedEntityClasses[(EntityClasses)equipableClass.Id].EquipableClassInfo = new EquipableClassInfo((EquipmentData)equipableClass.SlotId);

            // Load ItemTemplates
            ItemManager.Instance.LoadItemTemplates();

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedEntityClasses.Count} EntityClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {itemClassList.Count} ItemClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {equipableClassList.Count} EquipableClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {armorClassList.Count} ArmorClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponClassList.Count} WeaponClasses");
        }

        public EntityClass GetClassInfo(EntityClasses entityClassId)
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
