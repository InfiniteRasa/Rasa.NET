using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using System;
    using Data;
    using Database.Tables.World;
    using Game;
    using Packets;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class CreatureManager
    {
        /* Actor: AI bodies     (CreatureClass)
         * 
         *  - CreatureInfo
         *  - BattlecryNotification
         *  - Bark
         *  - UpdateEscortStatus
         */

        private static CreatureManager _instance;
        private static readonly object InstanceLock = new object();
        public const int CreatureLocationUpdateTime = 1500;
        public readonly Dictionary<uint, Creature> LoadedCreatures = new Dictionary<uint, Creature> { { 0, new Creature() } };

        public static CreatureManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CreatureManager();
                    }
                }

                return _instance;
            }
        }

        private CreatureManager()
        {
        }

        // 1 creature to n client's
        public void CellIntroduceCreatureToClients(MapChannel mapChannel, Creature creature, List<Client> clientList)
        {
            foreach (var client in clientList)
                CreateCreatureOnClient(client, creature);
        }

        // n creatures to 1 client
        public void CellIntroduceCreaturesToClient(Client client, List<Creature> creaturList)
        {
            foreach (var creature in creaturList)
                CreateCreatureOnClient(client, creature);
        }

        public Creature CreateCreature(uint dbId, SpawnPool spawnPool)
        {
            // check is creature in database
            if (!LoadedCreatures.ContainsKey(dbId))
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId}, isn't in database");
                return null;
            }

            var isCreature = false;
            // check if classId have creature Augmentation
            foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[dbId].EntityClassId].Augmentations)
                if (aug == AugmentationType.Creature)
                    isCreature = true;

            if (!isCreature)
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId = {dbId}, don't have creature Augmentation");
                return null;
            }

            // crate creature
            var creature = new Creature
            {
                Actor = new Actor(),
                AppearanceData = LoadedCreatures[dbId].AppearanceData,
                DbId = LoadedCreatures[dbId].DbId,
                EntityClassId = LoadedCreatures[dbId].EntityClassId,
                Faction = LoadedCreatures[dbId].Faction,
                Level = LoadedCreatures[dbId].Level,
                MaxHitPoints = LoadedCreatures[dbId].MaxHitPoints,
                NameId = LoadedCreatures[dbId].NameId,
                Npc = LoadedCreatures[dbId].Npc,
                SpawnPool = spawnPool
            };
            // set creature stats
            var creatureStats = CreatureStatsTable.GetCreatureStats(dbId);
            if (creatureStats != null)
            {
                creature.Actor.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, creatureStats.Body, creatureStats.Body, creatureStats.Body, 5, 1000));
                creature.Actor.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, creatureStats.Mind, creatureStats.Mind, creatureStats.Mind, 5, 1000));
                creature.Actor.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, creatureStats.Spirit, creatureStats.Spirit, creatureStats.Spirit, 5, 1000));
                creature.Actor.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, creatureStats.Health, creatureStats.Health, creatureStats.Health, 5, 1000));
                creature.Actor.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
                creature.Actor.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
                creature.Actor.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
                creature.Actor.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, creatureStats.Armor, creatureStats.Armor, creatureStats.Armor, 5, 1000));
                creature.Actor.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Actor.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
            }

            if (spawnPool != null)
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(spawnPool);

            return creature;
        }

        public void CreateCreatureOnClient(Client client, Creature creature)
        {
            if (creature == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo((EntityClassId)EntityManager.Instance.GetEntityClassId(creature.Actor.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(creature.Actor.Position, creature.Actor.Rotation),
                // Creature augmentation
                new CreatureInfoPacket(creature.NameId, false, new List<int>()),    // ToDo add creature flags
                // Actor augmentation
                new AppearanceDataPacket(creature.AppearanceData),
                new LevelPacket(creature.Level),
                new AttributeInfoPacket(creature.Actor.Attributes),
                new TargetCategoryPacket(creature.Faction),
                new UpdateAttributesPacket(creature.Actor.Attributes, 0),
                new IsRunningPacket(false)
            };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(creature.Actor.EntityId, creature.EntityClassId, entityData));

            // NPC  & Vendor augmentation
            if (creature.Npc != null)
                NpcManager.Instance.UpdateConversationStatus(client, creature);
        }

        public Creature FindCreature(uint creatureId)
        {
            return LoadedCreatures[creatureId];
        }

        public void CreatureInit()
        {
            var creatureList = CreatureTable.LoadCreatures();
            var vendorsList = VendorsTable.LoadVendors();
            var vendorItemList = VendorItemsTable.LoadVendorItems();

            foreach (var data in creatureList)
            {
                var appearanceData = CreatureAppearanceTable.GetCreatureAppearance(data.DbId);
                var tempAppearanceData = new Dictionary<EquipmentData, AppearanceData>();
                var augmentationsList = EntityClassManager.Instance.LoadedEntityClasses[(EntityClassId)data.ClassId].Augmentations;
                Npc isNpc = null;
                var isVendor = false;
                var isAuctioneer = false;
                //var isClanManager = false;

                foreach (var aug in augmentationsList)
                {
                    switch (aug)
                    {
                        case AugmentationType.Creature:
                            break;
                        case AugmentationType.NPC:
                            isNpc = new Npc();
                            break;
                        case AugmentationType.Vendor:
                            isVendor = true;
                            break;
                        case AugmentationType.Auctioneer:
                            isAuctioneer = true;
                            break;
                        case AugmentationType.Harvestable:
                            /* can be looted???
                             * ToDo
                             */
                            break;
                        default:
                            Logger.WriteLog(LogType.Error, $"Unsuported Augmentation {aug}");
                            break;
                    }
                }

                if (appearanceData != null && appearanceData.Count > 0)
                    foreach (var t in appearanceData)
                        tempAppearanceData.Add((EquipmentData)t.Slot, new AppearanceData { SlotId = (EquipmentData)t.Slot, Class = (EntityClassId)t.Class, Color = new Color(t.Color) });

                var creature = new Creature
                {
                    DbId = data.DbId,
                    EntityClassId = (EntityClassId)data.ClassId,
                    Faction = data.Faction,
                    Level = data.Level,
                    MaxHitPoints = data.MaxHitPoints,
                    NameId = data.NameId,
                    AppearanceData = tempAppearanceData
                };

                if (isNpc != null)
                    creature.Npc = isNpc;

                if (isAuctioneer)
                    creature.Npc.NpcIsAuctioneer = true;

                if (isVendor)
                {
                    // Load vendorPackageId
                    foreach (var vendor in vendorsList)
                        if (vendor.CreatureDbId == data.DbId)
                        {
                            creature.Npc.Vendor = new Vendor(vendor.PackageId);
                            break;
                        }

                    // Load vendorItems
                    foreach (var vendorItem in vendorItemList)
                        if (vendorItem.DbId == data.DbId)
                            creature.Npc.Vendor.VendorItems.Add(vendorItem.ItemTemplateId);
                }

                // add mission data to npc's
                foreach (var entry in MissionManager.Instance.LoadedMissions)
                {
                    var mission = entry.Value;

                    if (mission.MissionGiver == data.DbId || mission.MissionReciver == data.DbId)
                    {
                        if (creature.Npc.NpcMissionIds != null)
                        {
                            creature.Npc.NpcMissionIds.Add(mission.MissionId);
                        }
                        else
                        {
                            creature.Npc.NpcMissionIds = new List<uint>
                                {
                                    mission.MissionId
                                };
                        }
                    }
                }

                LoadedCreatures.Add(creature.DbId, creature);
            }

            var npcPackages = NPCPackagesTable.LoadNPCPackages();
            foreach (var package in npcPackages)
            {
                if (LoadedCreatures.ContainsKey(package.CreatureDbId))
                {
                    foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[package.CreatureDbId].EntityClassId].Augmentations)
                    {
                        if (aug == AugmentationType.NPC)
                        {
                            LoadedCreatures[package.CreatureDbId].Npc.NpcPackageId = package.NpcPackageId;
                            break;
                        }
                    }
                }
                else
                    Logger.WriteLog(LogType.Error, $"LoadNPCPackages: unknown cratueDbId = {package.CreatureDbId}");
            }

            
            
        }

        public void CellDiscardCreaturesToClient(Client client, List<Creature> discardCreatures)
        {
            foreach (var creature in discardCreatures)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.Actor.EntityId));
        }

        public void SetLocation(Creature creature, Vector3 position, Quaternion rotation)
        {
            // set spawnlocation
            creature.Actor.Position = position;
            creature.Actor.Rotation = rotation;
            //allocate pathnodes
            //creature->pathnodes = (baseBehavior_baseNode*)malloc(sizeof(baseBehavior_baseNode));
            //memset(creature->pathnodes, 0x00, sizeof(baseBehavior_baseNode));
            //creature->lastattack = GetTickCount();
            //creature->lastresttime = GetTickCount();
        }
    }
}
