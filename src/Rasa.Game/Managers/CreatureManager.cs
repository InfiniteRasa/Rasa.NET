using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
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
        public const long CreatureLocationUpdateTime = 1500;
        public readonly Dictionary<uint, Creature> LoadedCreatures = new Dictionary<uint, Creature>();
        public readonly Dictionary<uint, CreatureAction> CreatureActions = new Dictionary<uint, CreatureAction>();

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

        internal void HandleCreatureKill(MapChannel mapChannel, Creature creature, Actor killedBy)
        {
            if (creature.Actor.State == CharacterState.Dead)
                return; // creature already dead

            // kill creature
            var stateIds = new List<CharacterState> { CharacterState.Dead };

            creature.Actor.State = CharacterState.Dead;
            CellManager.Instance.CellCallMethod(mapChannel, creature.Actor, new StateChangePacket(stateIds));

            // tell spawnpool if set
            if (creature.SpawnPool != null)
            {
                SpawnPoolManager.Instance.DecreaseAliveCreatureCount(mapChannel, creature.SpawnPool);
                SpawnPoolManager.Instance.IncreaseDeadCreatureCount(creature.SpawnPool);
            }

            // todo: How were credits and experience calculated when multiple players attacked the same creature? Did only the player with the first strike get experience?

            Client client = null;

            // get client if it's killed by player
            foreach (var cellSeed in killedBy.Cells)
                foreach (var tempClient in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    if (tempClient.MapClient.Player.Actor == killedBy)
                    {
                        client = tempClient;
                        break;
                    }

            if (client != null)
            {
                // give experience
                var experience = creature.Level * 100; // base experience
                var experienceRange = creature.Level * 10;
                experience += ((new Random().Next() % (experienceRange * 2 + 1)) - experienceRange);

                // todo: Depending on level difference reduce experience
                ManifestationManager.Instance.GainExperience(client, experience);
            }

            // spawn loot
            if (killedBy != null && client != null)
                LootDispenserManager.Instance.Loot(client, creature);
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
                {
                    isCreature = true;
                    break;
                }

            if (!isCreature)
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId = {dbId}, don't have creature Augmentation");
                return null;
            }

            // crate creature
            var creature = new Creature(LoadedCreatures[dbId])
            {
                SpawnPool = spawnPool
            };

            creature.Actor.State = CharacterState.Idle;
            creature.Actor.Name = EntityClassManager.Instance.LoadedEntityClasses[(EntityClassId)creature.EntityClassId].ClassName;

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

            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            creature.Controller.ActionWander.State = BehaviorManager.WanderIdle; //wanderstate: calc new position
            
            if (spawnPool != null)
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(spawnPool);

            return creature;
        }

        internal void CellUpdateLocation(MapChannel mapChannel, Creature creature, uint newLocX, uint newLocZ)
        {
            // get old and new cell matrix
            var oldCellMatrix = creature.Actor.Cells;
            var newCellMatrix = CellManager.Instance.CreateCellMatrix(mapChannel, newLocX, newLocZ);
            
            // get info about cell we need to update
            var needUpdate = new List<uint>();
            var needDelete = new List<uint>();

            CellManager.Instance.GetCellMatrixDiff(oldCellMatrix, newCellMatrix, out needUpdate, out needDelete);

            mapChannel.MapCellInfo.Cells[oldCellMatrix[2, 2]].CreatureList.Remove(creature);
            
            // remove creature for player that are not in visibility range anymore
            foreach (var cellSeed in needDelete)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.Actor.EntityId));

            // add creature to new cell
            mapChannel.MapCellInfo.Cells[newCellMatrix[2, 2]].CreatureList.Add(creature);
            
            // set new creature visibility
            creature.Actor.Cells = newCellMatrix;
            
            // notify clients about creature
            foreach (var cellSeed in needUpdate)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    CreateCreatureOnClient(client, creature);
        }

        public void CreateCreatureOnClient(Client client, Creature creature)
        {
            if (creature == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo((EntityClassId)EntityManager.Instance.GetEntityClassId(creature.Actor.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(creature.Actor.Position, creature.Actor.Orientation),
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

            // send inital movement packet
            var movementData = new Memory.MovementData(creature.Actor.Position.X + 1, creature.Actor.Position.Y, creature.Actor.Position.Z + 1, creature.Actor.Orientation);

            client.MoveObject(creature.Actor.EntityId, movementData);
        }

        public Creature FindCreature(uint creatureId)
        {
            return LoadedCreatures[creatureId];
        }

        public void CreatureInit()
        {
            var creatureList = CreatureTable.LoadCreatures();
            var cratureActions = CreatureActionTable.GetCreatureActions();
            var vendorsList = VendorsTable.LoadVendors();
            var vendorItemList = VendorItemsTable.LoadVendorItems();

            foreach (var action in cratureActions)
                CreatureActions.Add(action.Id, new CreatureAction(action));

            foreach (var data in creatureList)
            {
                var appearanceData = CreatureAppearanceTable.GetCreatureAppearance(data.DbId);
                var tempAppearanceData = new Dictionary<EquipmentData, AppearanceData>();
                var augmentationsList = EntityClassManager.Instance.LoadedEntityClasses[(EntityClassId)data.ClassId].Augmentations;
                var actions = new List<CreatureAction>();
                Npc isNpc = null;
                var isVendor = false;
                var isAuctioneer = false;
                var isClanManager = data.NameId == 8838 ? true : false;

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

                // load Creature Actions
                if (data.Action1 != 0)
                    actions.Add(CreatureActions[data.Action1]);
                if (data.Action2 != 0)
                    actions.Add(CreatureActions[data.Action2]);
                if (data.Action3 != 0)
                    actions.Add(CreatureActions[data.Action3]);
                if (data.Action4 != 0)
                    actions.Add(CreatureActions[data.Action4]);
                if (data.Action5 != 0)
                    actions.Add(CreatureActions[data.Action5]);
                if (data.Action6 != 0)
                    actions.Add(CreatureActions[data.Action6]);
                if (data.Action7 != 0)
                    actions.Add(CreatureActions[data.Action7]);
                if (data.Action8 != 0)
                    actions.Add(CreatureActions[data.Action8]);

                var creature = new Creature(data)
                {
                    AppearanceData = tempAppearanceData,
                    Actions = actions
                };

                if (isNpc != null)
                    creature.Npc = isNpc;

                if (isAuctioneer)
                    creature.Npc.NpcIsAuctioneer = true;

                if (isClanManager)
                    creature.Npc.NpcIsClanMaster = true;

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

        public void SetLocation(Creature creature, Vector3 position, float orientation, uint mapContextId)
        {
            // set spawnlocation
            creature.Actor.Position = position;
            creature.Actor.Orientation = orientation;
            creature.Actor.MapContextId = mapContextId;
            // set home location
            creature.HomePos.Position = position;
            creature.HomePos.MapContextid = mapContextId;
            //allocate pathnodes
            //creature->pathnodes = (baseBehavior_baseNode*)malloc(sizeof(baseBehavior_baseNode));
            //memset(creature->pathnodes, 0x00, sizeof(baseBehavior_baseNode));
            //creature->lastattack = GetTickCount();
            //creature->lastresttime = GetTickCount();
        }
    }
}
