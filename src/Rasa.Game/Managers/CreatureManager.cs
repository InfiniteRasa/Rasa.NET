using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Game;
    using Packets;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class CreatureManager
    {
        private static CreatureManager _instance;
        private static readonly object InstanceLock = new object();
        public const int CreatureLocationUpdateTime = 1500;
        public readonly Dictionary<int, Creature> LoadedCreatures = new Dictionary<int, Creature>();

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
        public void CellIntroduceCreatureToClients(MapChannel mapChannel, Creature creature, List<MapChannelClient> playerList)
        {
            foreach (var player in playerList)
                CreateCreatureOnClient(player, creature);
        }

        // n creatures to 1 client
        public void CellIntroduceCreaturesToClient(MapChannel mapChannel, MapChannelClient mapClient, List<Creature> creaturList)
        {
            foreach (var creature in creaturList)
                CreateCreatureOnClient(mapClient, creature);
        }

        public Creature CreateCreature(int dbId, SpawnPool spawnPool)
        {
            // check is creature in database
            if (!LoadedCreatures.ContainsKey(dbId))
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId}, isn't in database");
                return null;
            }
            var isCreature = false;
            // check if classId have creature Augmentation
            foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[dbId].ClassId].Augmentations)
                if (aug == AugmentationType.Creature)
                    isCreature = true;
            if(!isCreature)
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId}, don't have creature Augmentation");
                return null;
            }

            // crate creature
            var creature = new Creature
            {
                Actor = new Actor(),
                AppearanceData = LoadedCreatures[dbId].AppearanceData,
                DbId = LoadedCreatures[dbId].DbId,
                ClassId = LoadedCreatures[dbId].ClassId,
                Faction = LoadedCreatures[dbId].Faction,
                Level = LoadedCreatures[dbId].Level,
                MaxHitPoints = LoadedCreatures[dbId].MaxHitPoints,
                NameId = LoadedCreatures[dbId].NameId,
                NpcData = LoadedCreatures[dbId].NpcData,
                SpawnPool = spawnPool,
                VendorData = LoadedCreatures[dbId].VendorData
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

        public void CreateCreatureOnClient(MapChannelClient mapClient, Creature creature)

        {
            if (creature == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new WorldLocationDescriptorPacket(creature.Actor.Position, creature.Actor.Rotation),
                new IsTargetablePacket(true),
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

            mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(creature.Actor.EntityId, creature.ClassId, entityData));

            // NPC  & Vendor augmentation
            if (creature.NpcData != null || creature.VendorData != null)
                UpdateConversationStatus(mapClient.Client, creature);
        }

        public Creature FindCreature(int creatureId)
        {
            return LoadedCreatures[creatureId];
        }

        public void CreatureInit()
        {
            var creatureList = CreatureTable.LoadCreatures();
            foreach (var data in creatureList)
            {
                var appearanceData = CreatureAppearanceTable.GetCreatureAppearance(data.DbId);
                var tempAppearanceData = new Dictionary<EquipmentSlots, AppearanceData>();

                if (appearanceData != null)
                    foreach (var t in appearanceData)
                        tempAppearanceData.Add((EquipmentSlots)t.SlotId, new AppearanceData { SlotId = (EquipmentSlots)t.SlotId, ClassId = t.ClassId, Color = new Color(t.Color) });

                var creature = new Creature
                {
                    DbId = data.DbId,
                    ClassId = data.ClassId,
                    Faction = data.Faction,
                    Level = data.Level,
                    MaxHitPoints = data.MaxHitPoints,
                    NameId = data.NameId,
                    AppearanceData = tempAppearanceData

                };
                // add mission data to npc's
                foreach (var entry in MissionManager.Instance.LoadedMissions)
                {
                    var mission = entry.Value;
                    if (mission.MissionGiver == data.DbId || mission.MissionReciver == data.DbId)
                    {
                        if (creature.NpcData != null)
                        {
                            if (!creature.NpcData.Missions.ContainsKey(mission.MissionId))
                                creature.NpcData.Missions.Add(mission.MissionId, mission);
                        }
                        else
                        {
                            creature.NpcData = new CreatureNpcData();
                            creature.NpcData.Missions.Add(mission.MissionId, mission);
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
                    foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[package.CreatureDbId].ClassId].Augmentations)
                        if (aug == AugmentationType.NPC)
                            if (LoadedCreatures[package.CreatureDbId].NpcData == null)
                            {
                                LoadedCreatures[package.CreatureDbId].NpcData = new CreatureNpcData();
                                LoadedCreatures[package.CreatureDbId].NpcData.NpcPackageIds.Add(package.NpcPackageId);
                                break;
                            }
                            else
                            {
                                LoadedCreatures[package.CreatureDbId].NpcData.NpcPackageIds.Add(package.NpcPackageId);
                                break;
                            }
                }
                else
                    Logger.WriteLog(LogType.Error, $"LoadNPCPackages: unknown cratueDbId = {package.CreatureDbId}");
            }

            var vendorsList = VendorsTable.LoadVendors();
            foreach (var vendor in vendorsList)
            {
                if (LoadedCreatures.ContainsKey(vendor.CreatureDbId))
                {
                    foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[vendor.CreatureDbId].ClassId].Augmentations)
                        if (aug == AugmentationType.Vendor || aug == AugmentationType.NPC)
                            LoadedCreatures[vendor.CreatureDbId].VendorData = new CreatureVendorData { VendorPackageId = vendor.PackageId };
                }
                else
                    Logger.WriteLog(LogType.Error, $"LoadVendors: unknown cratueDbId = {vendor.CreatureDbId}");
            }

            var vendorItemList = VendorItemsTable.LoadVendorItems();
            foreach (var vendorItem in vendorItemList)
            {
                if (LoadedCreatures.ContainsKey(vendorItem.DbId))
                    LoadedCreatures[vendorItem.DbId].VendorData.SellItemList.Add(vendorItem.ItemTemplateId);
                else
                    Logger.WriteLog(LogType.Error, $"LoadVendorItems: unknown cratueDbId = {vendorItem.DbId}");
            }
        }

        public void SetLocation(Creature creature, Position position, Quaternion rotation)
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

        #region NPC

        public void NpcInit()
        {

        }

        public void AssignNPCMission(Client client, AssignNPCMissionPacket packet)
        {
            var mission = EntityManager.Instance.GetCreature((uint)packet.NpcEntityId).NpcData.Missions[packet.MissionId];

            if (client.MapClient.Player.Missions.Count > 30)
            {
                CommunicatorManager.Instance.SystemMessage(client.MapClient, "Mission log is full.");
                return;
            }

            client.SendPacket(client.MapClient.Player.Actor.EntityId, new MissionGainedPacket(mission.MissionId, mission.MissionInfo));
        }

        public void RequestNpcConverse(Client client, RequestNPCConversePacket packet)
        {
            var creature = EntityManager.Instance.GetCreature((uint)packet.EntityId);

            if (creature == null)
                return;

            // ToDo create DB structures, and replace constant data with dinamic

            var testConvoDataDict = new Dictionary<ConversationType, ConvoData>();

            if (creature.VendorData != null)
                testConvoDataDict.Add(ConversationType.Vending, new ConvoData { VendorConverse = new List<int> { creature.VendorData.VendorPackageId } });

            if (creature.NpcData != null)
                if (creature.NpcData.Missions.Count > 0)
                {
                    var dispensableMissions = new List<DispensableMissions>();
                    var completeableMissions = new List<CompleteableMissions>();
                    var completeableObjectives = new List<CompleteableObjectives>();

                    foreach (var entry in creature.NpcData.Missions)
                    {
                        var mission = entry.Value;

                        if (mission.MissionGiver == creature.DbId)
                            dispensableMissions.Add(new DispensableMissions(mission.MissionId, mission.MissionInfo ));

                        if (mission.MissionReciver == creature.DbId)
                            completeableMissions.Add(new CompleteableMissions(mission.MissionId, mission.MissionInfo.MissionConstantData.RewardInfo));
                    }

                    testConvoDataDict.Add(ConversationType.MissionDispense, new ConvoData { DispensableMissions = dispensableMissions });
                    testConvoDataDict.Add(ConversationType.MissionComplete, new ConvoData { CompleteableMissions = completeableMissions });
                }
                    
            /*
            // Greeting = 0
            var greetingId = 19;

            // ForceTopic = 1
            var forceTopicType = new ForceTopic(ConversationType.MissionReward, 429);

            // DispensableMissions = 2
            var missionId = 429;
            var missionLevel = 1;
            var groupType = 1;
            var credits = new List<Curency>
            {
                new Curency(CurencyType.Credits, 200),
                new Curency(CurencyType.Prestige, 100)
            };
            var fixedItems = new List<RewardItem>
            {
                new RewardItem(17131, 27120, 1, new List<int>{900620 }, 2),
                new RewardItem(17131, 27120, 1, new List<int>{900007 }, 2)
            };
            var selectableRewards = new List<RewardItem>
            {
                new RewardItem(28, 3147, 20, new List<int>(), 1),
                new RewardItem(28, 3147, 50, new List<int>(), 1)
            };
            var fixedReward = new FixedReward(credits, fixedItems);
            var selectableReward = new List<RewardItem>(selectableRewards);
            var rewardInfo = new RewardInfo(fixedReward, selectableReward);
            var missionObjectives = new List<MissionObjectives> { new MissionObjectives(4), new MissionObjectives(5) };
            var itemRequired = new List<RewardItem> { new RewardItem(26544) };
            var missionInfo = new MissionInfo(missionLevel, rewardInfo, missionObjectives, itemRequired, groupType);
            var dispensableMissions = new List<DispensableMissions>
            {
                new DispensableMissions(missionId, missionInfo),
                new DispensableMissions(298, missionInfo)
            };

            // CompletableMissions = 3
            var completeableMissions = new List<CompleteableMissions>
            {
                new CompleteableMissions(missionId, rewardInfo),
                new CompleteableMissions(298, rewardInfo)
            };

            // MissionReminder = 4
            var remindableMissions = new List<int> { 298, 429, 430 };

            // ObjectiveAmbient = 5
            var ambientObjectives = new List<AmbientObjectives>
            {
                new AmbientObjectives(missionId, 5, 1),
                new AmbientObjectives(missionId, 4, 1)
            };

            // ObjectiveComplete = 6
            var objectiveComplete = new List<CompleteableObjectives>
            {
                new CompleteableObjectives(missionId, 5, 1),
                new CompleteableObjectives(missionId, 4, 1)
            };

            // RewardableMission = 7 (mission without objectives ???)
            var revardableMissions = new List<RewardableMissions>
            {
                new RewardableMissions(298, rewardInfo),
                new RewardableMissions(missionId, rewardInfo),
            };

            // ObjectiveChoice = 8,
            // EndConversation = 9,
            // Training = 10,
            var training = new TrainingConverse(true, 1);
            // Vending = 11,
            var vendor = new ConvoDataDict
            {
                VendorConverse = new List<int> { 142 }
            };
            // ImportantGreering = 12,
            // Clan = 13,
            // Auctioner = 14,
            var auctioneer = new ConvoDataDict
            {
                IsAuctioneer = true
            };
            // ForcedByScript = 15

            var testConvoDataDict = new Dictionary<ConversationType, ConvoDataDict>
            {
                { ConversationType.MissionDispense, new ConvoDataDict(dispensableMissions) },
                { ConversationType.ObjectiveComplete, new ConvoDataDict(objectiveComplete) },
                { ConversationType.MissionComplete, new ConvoDataDict(completeableMissions) },
                //{ ConversationType.MissionReward, new ConvoDataDict(revardableMissions) },
                { ConversationType.Greeting, new ConvoDataDict(greetingId) },
                //{ ConversationType.MissionReminder, new ConvoDataDict(remindableMissions) },
                //{ ConversationType.ObjectiveAmbient, new ConvoDataDict(ambientObjectives) },
                //{ ConversationType.Training, new ConvoDataDict(training) }
                { ConversationType.Auctioneer, auctioneer },
                { ConversationType.Vending, vendor }
            };
            */

            client.SendPacket(creature.Actor.EntityId, new ConversePacket(testConvoDataDict));
        }

        public void UpdateConversationStatus(Client client, Creature creature)
        {
            var npcData = creature.NpcData;
            var statusSet = false;

            if (npcData != null)
                if (npcData.Missions != null)
                    if (npcData.Missions.Count > 0)
                    {

                    }

            if (npcData != null)
            {
                if (npcData.NpcPackageIds.Count > 0)
                    foreach (var packageId in npcData.NpcPackageIds)
                        client.SendPacket(creature.Actor.EntityId, new NPCInfoPacket(packageId));

                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.ObjectivComplete, new List<int>())); // complete objective
                statusSet = true;
            }
            /*

            foreach (var entry in npcData.RelatedMissions)
            {
                var missionLogEntry = MissionManager.Instance.FindPlayerMission(client, entry.MissionIndex);
                var mission = MissionManager.Instance.GetById(missionLogEntry.MissionIndex);

                if (missionLogEntry != null)
                {
                    if (mission == null)
                        continue;

                    if (missionLogEntry.State >= mission.StateCount)
                        continue;

                    // search for objective or mission related updates
                    var scriptlineStart = mission.StateMapping[missionLogEntry.State];
                    var scriptlineEnd = mission.StateMapping[missionLogEntry.State + 1];

                    for (var i = scriptlineStart; i < scriptlineEnd; i++)
                    {
                        var scriptline = mission.ScriptLines[i];

                        if (scriptline.Command == MissionScriptCommand.CompleteObjective)
                        {
                            if (creature.DbId == scriptline.Value1) // same NPC?
                            {
                                // objective already completed?
                                if (missionLogEntry.MissionData[scriptline.StorageIndex] == 1)
                                    continue;

                                // send objective completable flag
                                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.ObjectivComplete, new List<int> { })); // status - complete objective

                                statusSet = true;

                                break;
                            }
                            else if (scriptline.Command == MissionScriptCommand.Collector)
                            {
                                if (creature.DbId == scriptline.Value1) // same NPC?
                                {
                                    // mission already completed?
                                    if (missionLogEntry.State != (mission.StateCount - 1))
                                        continue;

                                    // send mission completable flag
                                    client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.MissionComplete, new List<int> { })); // status - complete objective

                                    statusSet = true;

                                    break;
                                }
                            }
                        }
                    }
                }
                else if (MissionManager.Instance.IsCompletedByPlayer(client, mission.MissionIndex) == false)
                {
                    // check if the npc is actually the mission dispenser and not only a objective related npc
                    if (MissionManager.Instance.IsCreatureMissionDispenser(MissionManager.Instance.GetByIndex(mission.MissionIndex), creature))
                    {
                        // mission available overwrites any other converse state
                        client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Available, new List<int> { })); // status - available

                        statusSet = true;

                        break;
                    }
                }
            }*/
            // is NPC vendor?
            if (creature.VendorData != null && statusSet == false)
            {
                // creature->npcData.isVendor
                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Vending, new List<int> { creature.VendorData.VendorPackageId })); // status - vending
                statusSet = true;
            }
            // no status set yet? Send NONE conversation status
            if (statusSet == false)
            {
                // no other status, set NONE status
                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.None, null));// status - none

                statusSet = true;
            }
        }
        #endregion

        #region Auctioneer
        public void RequestNPCOpenAuctionHouse(Client client, long entityId)
        {
            client.SendPacket((uint)entityId, new OpenAuctionHousePacket());
        }
        #endregion

        #region Vendor
        public void RequestNPCVending(Client client, RequestNPCVendingPacket packet)
        {
            var itemList = new List<Item>();
            var entityList = new List<uint>();

            if (EntityManager.Instance.VendorItems.ContainsKey((uint)packet.EntityId))
            {
                // store was opened before
                entityList = EntityManager.Instance.VendorItems[(uint)packet.EntityId];

                foreach (var entityId in entityList)
                    itemList.Add(EntityManager.Instance.GetItem(entityId));
            }
            else
            {
                // opening store first Time, create item
                var creature = EntityManager.Instance.GetCreature((uint)packet.EntityId);

                foreach (var itemTemplateId in creature.VendorData.SellItemList)
                {
                    var item = ItemManager.Instance.CreateVendorItem(client, itemTemplateId);

                    itemList.Add(item);
                    entityList.Add(item.EntityId);
                }

                EntityManager.Instance.RegisterVendorItem((uint)packet.EntityId, entityList);
            }

            client.SendPacket((uint)packet.EntityId, new VendPacket(itemList));
        }

        public void RequestCancelVendor(Client client, long entityId)
        {
            // ToDo
        }

        public void RequestVendorBuyback(Client client, RequestVendorBuybackPacket packet)
        {
            client.SendPacket(9, new RemoveBuybackItemPacket((uint)packet.ItemEntityId));

            var buyBackItem = EntityManager.Instance.GetItem((uint)packet.ItemEntityId);
            var buyedItem = InventoryManager.Instance.AddItemToInventory(client.MapClient, buyBackItem);
            var buyPrice = buyedItem.Stacksize * buyedItem.ItemTemplate.SellPrice;
            // remove credits
            PlayerManager.Instance.GainCredits(client, -buyPrice);
        }

        public void RequestVendorPurchase(Client client, RequestVendorPurchasePacket packet)
        {
            var itemQuantity = packet.Quantity;
            if (itemQuantity <= 0)
                return;

            // get the instance of the bought item
            var selectedVendorItem = EntityManager.Instance.GetItem((uint)packet.ItemEntityId);

            if (selectedVendorItem == null)
            {
                Logger.WriteLog(LogType.Error, "RequestVendorPurchase: The item instance does not exist");
                return;
            }
            // has the player enough credits?
            var buyPrice = selectedVendorItem.ItemTemplate.BuyPrice * itemQuantity;
            
            if (client.MapClient.Player.Credits < buyPrice)
                return; // not enough credits

            // duplicate item
            var boughtItem = ItemManager.Instance.DuplicateItem(client, packet);

            if (boughtItem == null)
                return; // could not duplicate item

            var tempItem = boughtItem;
            var desiredStacksize = tempItem.Stacksize;

            boughtItem = InventoryManager.Instance.AddItemToInventory(client.MapClient, boughtItem);

            if (boughtItem == null)
            {
                // item could not be added to inventory
                var appliedRestStackSize = tempItem.Stacksize;
                if (appliedRestStackSize == desiredStacksize)
                {
                    // not even 1x item could be added to the inventory
                    EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                    return;
                }
                // we were able to at least get a part of the stack into the inventory
                // destroy the rest and update the stacksize for the price
                EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                itemQuantity = (desiredStacksize - appliedRestStackSize);
            }
            // get correct buy price
            buyPrice = boughtItem.ItemTemplate.BuyPrice * itemQuantity;
            // remove credits
            PlayerManager.Instance.GainCredits(client, -buyPrice);
        }
        
        public void RequestVendorRepair(Client client, RequestVendorRepairPacket packet)
        {
            foreach( var itemEntityId in packet.ItemEntitesId)
            {
                var item = EntityManager.Instance.GetItem((uint)itemEntityId);
                var classInfo = EntityClassManager.Instance.LoadedEntityClasses[item.ItemTemplate.ClassId];

                // calculate cost
                var cost = (double)((classInfo.ItemClassInfo.MaxHitPoints - item.CurrentHitPoints) * item.ItemTemplate.SellPrice) / 100;
                // set itemHit points to full
                item.CurrentHitPoints = classInfo.ItemClassInfo.MaxHitPoints;
                // remove player credits
                PlayerManager.Instance.GainCredits(client, -(int)Math.Round(cost, 0));
                // updateItem on client
                ItemManager.Instance.SendItemDataToClient(client.MapClient, item, true);
                // updare item in db
                ItemsTable.UpdateCurrentHitPoints(item.ItemId, item.CurrentHitPoints);
            }
        }

        public void RequestVendorSale(Client client, RequestVendorSalePacket packet)
        {
            var vendorEntityId = packet.VendorEntityId;
            var itemEntityId = packet.ItemEntityId;
            var itemQuantity = packet.Quantity;

            // note: Players can only sell items directly from their personal inventory
            //       so we only have to scan there for the item entityId
            var slotIndex = -1;
            for (var i = 0; i < 250; i++)
            {
                if (client.MapClient.Inventory.PersonalInventory[i] == itemEntityId)
                {
                    slotIndex = i;
                    break;
                }
            }

            if (slotIndex == -1)
            {
                Logger.WriteLog(LogType.Error, "RequestVendorSale: Item entity not found in player's inventory\n");
                return;
            }
            
            // get item handle
            var soldItem = EntityManager.Instance.GetItem((uint)itemEntityId);

            if (soldItem == null)
            {
                Logger.WriteLog(LogType.Error, "RequestVendorSale: Item reference found but item instance does not exist\n");
                return;
            }

            // get sell price
            var realItemQuantity = Math.Min(itemQuantity, soldItem.Stacksize);
            var sellPrice = soldItem.ItemTemplate.SellPrice * realItemQuantity;
            // remove item
            // todo: Handle stacksizes correctly and only decrease item by quantity parameter
            InventoryManager.Instance.RemoveItemBySlot(client, InventoryType.Personal, slotIndex);
            CharacterInventoryTable.DeleteInvItem(client.MapClient.Player.CharacterId, slotIndex);
            // add credits to player
            PlayerManager.Instance.GainCredits(client, (int)sellPrice);
            // add item to buyback list
            client.SendPacket(9, new AddBuybackItemPacket((uint)packet.ItemEntityId, (int)sellPrice, 1));
        }
        #endregion
    }
}
