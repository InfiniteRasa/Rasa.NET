using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
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
        public Dictionary<uint, Creature> LoadedCreatures = new();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
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
                            _instance = new CreatureManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private CreatureManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
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

        private void GiveWeapon(Creature creature)
        {
            var haveWeapon = creature.AppearanceData.ContainsKey(EquipmentData.Weapon);

            if (!haveWeapon)
            {
                var weapon = new AppearanceData
                {
                    SlotId = EquipmentData.Weapon,
                    Color = Color.RandomColor(),
                    Hue2 = Color.RandomColor()
                };

                switch (creature.EntityClass)
                {
                    case (EntityClasses)20757:
                        weapon.Class = 3878;
                        break;
                    case (EntityClasses)9244:
                        weapon.Class = 3782;
                        break;
                    case (EntityClasses)3846:
                        weapon.Class = 27131;
                        break;
                    case (EntityClasses)3848:
                        weapon.Class = 6443;
                        break;
                    default:
                        return;
                }

                creature.AppearanceData.Add(EquipmentData.Weapon, weapon);
                UpdateCreatureAppearance(creature);
            }
        }

        internal void HandleCreatureKill(MapChannel mapChannel, Creature creature, Actor killedBy)
        {
            if (creature.State == CharacterState.Dead)
                return; // creature already dead

            // kill creature
            var stateIds = new List<CharacterState> { CharacterState.Dead };

            creature.State = CharacterState.Dead;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new StateChangePacket(stateIds));

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
                    if (tempClient.Player == killedBy)
                    {
                        client = tempClient;
                        break;
                    }

            if (client != null)
            {
                // give experience
                var experience = creature.Level * 100; // base experience
                var experienceRange = creature.Level * 10;
                experience += (uint)(new Random().Next() % (experienceRange * 2 + 1)) - experienceRange;

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
            foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[dbId].EntityClass].Augmentations)
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
            var creatureEntry = LoadedCreatures[dbId];
            var creature = (Creature)creatureEntry.Clone();

            creature.SpawnPool = spawnPool;

            creature.State = CharacterState.Idle;
            creature.Name = EntityClassManager.Instance.LoadedEntityClasses[creature.EntityClass].ClassName;

            // set creature stats
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var creatureStats = unitOfWork.CreatureStats.GetCreatureStats(dbId);

            if (creatureStats != null)
            {
                creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, creatureStats.Body, creatureStats.Body, creatureStats.Body, 5, 1000));
                creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, creatureStats.Mind, creatureStats.Mind, creatureStats.Mind, 5, 1000));
                creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, creatureStats.Spirit, creatureStats.Spirit, creatureStats.Spirit, 5, 1000));
                creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, creatureStats.Health, creatureStats.Health, creatureStats.Health, 5, 1000));
                creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, creatureStats.Armor, creatureStats.Armor, creatureStats.Armor, 5, 1000));
                creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
            }
            else
            {
                creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 10, 1000));
                creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 100, 100, 100, 5, 1000));
                creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
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
            var oldCellMatrix = creature.Cells;
            var newCellMatrix = CellManager.Instance.CreateCellMatrix(mapChannel, newLocX, newLocZ);

            // get info about cell we need to update
            var needUpdate = new List<uint>();
            var needDelete = new List<uint>();

            CellManager.Instance.GetCellMatrixDiff(oldCellMatrix, newCellMatrix, out needUpdate, out needDelete);

            mapChannel.MapCellInfo.Cells[oldCellMatrix[2, 2]].CreatureList.Remove(creature);

            // remove creature for player that are not in visibility range anymore
            foreach (var cellSeed in needDelete)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));

            // add creature to new cell
            mapChannel.MapCellInfo.Cells[newCellMatrix[2, 2]].CreatureList.Add(creature);

            // set new creature visibility
            creature.Cells = newCellMatrix;

            // notify clients about creature
            foreach (var cellSeed in needUpdate)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    CreateCreatureOnClient(client, creature);
        }

        public void CreateCreatureOnClient(Client client, Creature creature)
        {
            if (creature == null)
                return;

            // random colors for now
            var hue = Color.RandomColor();
            var hue2 = Color.RandomColor();

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(EntityManager.Instance.GetEntityClassId(creature.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(creature.Position, creature.Rotation),
                new BodyAttributesPacket(creature.Scale, hue, 0, 0, hue2),
                // Creature augmentation
                new CreatureInfoPacket(creature.NameId, false, new List<int>()),    // ToDo add creature flags
                // Actor augmentation
                new AppearanceDataPacket(creature.AppearanceData),
                new LevelPacket(creature.Level),
                new AttributeInfoPacket(creature.Attributes),
                new TargetCategoryPacket(creature.Faction),
                new UpdateAttributesPacket(creature.Attributes, 0),
                new IsRunningPacket(false)
            };

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(creature.EntityId, creature.EntityClass, entityData));

            // NPC  & Vendor augmentation
            if (creature.Npc != null)
                NpcManager.Instance.UpdateConversationStatus(client, creature);

            // send inital movement packet
            var movementData = new Movement(creature.Position, new Vector2((float)creature.Rotation, 0f));

            // give some weapon to creature's
            GiveWeapon(creature);

            client.MoveObject(creature.EntityId, movementData);
        }

        public void CreatureInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var creatureList = unitOfWork.Creatures.Get();
            var vendorsList = unitOfWork.Vendors.Get();
            var vendorItemList = unitOfWork.VendorItems.Get();
            var creatureActions = unitOfWork.CreatureActions.Get();

            foreach (var data in creatureList)
            {
                var appearanceData = unitOfWork.CreatureAppearances.GetCreatureAppearances(data.Id);
                var tempAppearanceData = new Dictionary<EquipmentData, AppearanceData>();
                var augmentationsList = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)data.ClassId].Augmentations;
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
                        tempAppearanceData.Add((EquipmentData)t.SlotId, new AppearanceData { SlotId = (EquipmentData)t.SlotId, Class = t.ClassId, Color = new Color(t.Color), Hue2 = new Color(2139062144) });

                var creature = new Creature(data)
                {
                    AppearanceData = tempAppearanceData,
                };

                // load Creature Actions
                if (data.Action1 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action1]));
                if (data.Action2 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action2]));
                if (data.Action3 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action3]));
                if (data.Action4 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action4]));
                if (data.Action5 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action5]));
                if (data.Action6 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action6]));
                if (data.Action7 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action7]));
                if (data.Action8 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action8]));

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
                        if (vendor.Id == data.Id)
                        {
                            creature.Npc.Vendor = new Vendor(vendor.PackageId);
                            break;
                        }

                    // Load vendorItems
                    foreach (var vendorItem in vendorItemList)
                        if (vendorItem.Id == data.Id)
                            creature.Npc.Vendor.VendorItems.Add(vendorItem.ItemTemplateId);
                }

                // add mission data to npc's
                foreach (var entry in MissionManager.Instance.LoadedMissions)
                {
                    var mission = entry.Value;

                    if (mission.MissionGiver == data.Id || mission.MissionReciver == data.Id)
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

            var npcPackages = unitOfWork.NpcPackages.Get();
            foreach (var package in npcPackages)
            {
                if (LoadedCreatures.ContainsKey(package.Id))
                {
                    foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[package.Id].EntityClass].Augmentations)
                    {
                        if (aug == AugmentationType.NPC)
                        {
                            LoadedCreatures[package.Id].Npc.NpcPackageId = package.PackageId;
                            break;
                        }
                    }
                }
                else
                    Logger.WriteLog(LogType.Error, $"LoadNPCPackages: unknown cratueDbId = {package.Id}");
            }
        }

        public void CellDiscardCreaturesToClient(Client client, List<Creature> discardCreatures)
        {
            foreach (var creature in discardCreatures)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
        }

        public void SetLocation(Creature creature, Vector3 position, double orientation, uint mapContextId)
        {
            // set spawnlocation
            creature.Position = position;
            creature.Rotation = orientation;
            creature.MapContextId = mapContextId;
            // set home location
            creature.HomePos.Position = position;
            creature.HomePos.MapContextid = mapContextId;
            //allocate pathnodes
            //creature->pathnodes = (baseBehavior_baseNode*)malloc(sizeof(baseBehavior_baseNode));
            //memset(creature->pathnodes, 0x00, sizeof(baseBehavior_baseNode));
            //creature->lastattack = GetTickCount();
            //creature->lastresttime = GetTickCount();
        }

        public void UpdateCreatureAppearance(Creature creature)
        {
            var mapChannel = MapChannelManager.Instance.MapChannelArray[creature.MapContextId];
            CellManager.Instance.CellCallMethod(mapChannel, creature, new AppearanceDataPacket(creature.AppearanceData));
        }

        internal void CreateOrUpdateAppearance(Creature creature, AppearanceData appearanceData)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!creature.AppearanceData.ContainsKey(appearanceData.SlotId))
                creature.AppearanceData.Add(appearanceData.SlotId, appearanceData);

            creature.AppearanceData[appearanceData.SlotId].Color = appearanceData.Color;

            if (appearanceData.Class > 0)
                creature.AppearanceData[appearanceData.SlotId].Class = appearanceData.Class;

            // update creature appearance on client's
            CellManager.Instance.CellCallMethod(creature, new AppearanceDataPacket(creature.AppearanceData));

            // update creature in database
            unitOfWork.CreatureAppearances.CreateOrUpdate(creature.DbId, (uint)appearanceData.SlotId, appearanceData.Class, appearanceData.Color.Hue);

            Logger.WriteLog(LogType.Debug, "Creature Look updated");
        }
    }
}
