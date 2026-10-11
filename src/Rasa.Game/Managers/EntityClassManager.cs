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
                LoadedEntityClasses.Add((EntityClasses)entityClass.Id, new EntityClass(
                    entityClass.Id,
                    entityClass.ClassName,
                    entityClass.MeshId,
                    entityClass.ClassCollisionRole,
                    ParseAugmentations(entityClass.AugList),
                    entityClass.TargetFlag != 0
                    ));
            };

            // Load itemClasses
            var itemClassList = unitOfWork.Equipment.GetItemClasses();
            foreach (var itemClass in itemClassList)
                LoadedEntityClasses[(EntityClasses)itemClass.Id].ItemClassInfo = new ItemClassInfo(itemClass);

            // Load ArmorClasses
            var armorClassList = unitOfWork.Equipment.GetArmorClasses();
            foreach (var armorClass in armorClassList)
                LoadedEntityClasses[(EntityClasses)armorClass.Id].ArmorClassInfo = new ArmorClassInfo(armorClass);

            // Load WeaponClasses
            var weaponClassList = unitOfWork.Equipment.GetWeaponClasses();
            foreach (var weaponClass in weaponClassList)
                LoadedEntityClasses[(EntityClasses)weaponClass.Id].WeaponClassInfo = new WeaponClassInfo(weaponClass);

            // Load EquipableClasses
            var equipableClassList = unitOfWork.Equipment.GetEquipableClasses();
            foreach (var equipableClass in equipableClassList)
                LoadedEntityClasses[(EntityClasses)equipableClass.Id].EquipableClassInfo = new EquipableClassInfo((EquipmentData)equipableClass.SlotId);

            // Load creature flags. Keyed by class, and a class that is not a creature simply
            // has none - so this is a plain fold rather than a lookup per creature.
            var creatureFlagList = unitOfWork.Creatures.GetClassFlags();
            foreach (var flag in creatureFlagList)
                if (LoadedEntityClasses.TryGetValue((EntityClasses)flag.ClassId, out var entityClass))
                    entityClass.CreatureFlags.Add((CreatureFlag)flag.FlagId);

            // Load ItemTemplates
            ItemManager.Instance.LoadItemTemplates();

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedEntityClasses.Count} EntityClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {itemClassList.Count} ItemClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {equipableClassList.Count} EquipableClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {armorClassList.Count} ArmorClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {weaponClassList.Count} WeaponClasses");
            Logger.WriteLog(LogType.Initialize, $"Loaded {creatureFlagList.Count} CreatureClassFlags");
        }

        public EntityClass GetClassInfo(EntityClasses entityClassId)
        {
            if (LoadedEntityClasses.ContainsKey(entityClassId))
                return LoadedEntityClasses[entityClassId];

            Logger.WriteLog(LogType.Error, $"entityClassId  {entityClassId} is not present in LoadedEntityClasses");

            return null;
        }

        internal static List<AugmentationType> ParseAugmentations(string augmentationList)
        {
            var result = new List<AugmentationType>();
            foreach (var value in Regex.Split(augmentationList, @"\D+"))
                if (int.TryParse(value, out var augmentation))
                    result.Add((AugmentationType)augmentation);
            return result;
        }

        public ArmorClassInfo GetArmorClassInfo(Item armor)
        {
            return LoadedEntityClasses[armor.ItemTemplate.Class].ArmorClassInfo;
        }

        /// <summary>
        /// Which equipment slot an item is worn in, or null for no item, a class that was never
        /// loaded, and a class that is not equipment at all - a consumable, an ammo stack, a
        /// crafting part. The last is an ordinary answer rather than a fault: the equip handlers
        /// ask precisely to find out, and every caller tests the result.
        /// </summary>
        public EquipableClassInfo GetEquipableClassInfo(Item equipment)
        {
            if (equipment?.ItemTemplate == null)
                return null;

            if (LoadedEntityClasses.TryGetValue(equipment.ItemTemplate.Class, out var entityClass))
                return entityClass.EquipableClassInfo;

            Logger.WriteLog(LogType.Error, $"entityClassId  {equipment.ItemTemplate.Class} is not present in LoadedEntityClasses");

            return null;
        }

        public ItemClassInfo GetItemClassInfo(Item item)
        {
            return LoadedEntityClasses[item.ItemTemplate.Class].ItemClassInfo;
        }

        /// <summary>
        /// The weapon class of an item, or null for no item and for a class that was never
        /// loaded. Every live caller already tests the result for null, and used to get a
        /// NullReferenceException or a KeyNotFoundException in place of that null - thrown on
        /// the world loop, where it costs the tick rather than the shot.
        /// </summary>
        public WeaponClassInfo GetWeaponClassInfo(Item weapon)
        {
            if (weapon?.ItemTemplate == null)
                return null;

            if (LoadedEntityClasses.TryGetValue(weapon.ItemTemplate.Class, out var entityClass))
                return entityClass.WeaponClassInfo;

            Logger.WriteLog(LogType.Error, $"entityClassId  {weapon.ItemTemplate.Class} is not present in LoadedEntityClasses");

            return null;
        }
    }
}
