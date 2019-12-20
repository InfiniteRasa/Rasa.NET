using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class NpcManager
    {
        /* NPC augmentation. Supports dialog, mission manipulation, and vending
         * 
         *  - NPCInfo
         *  - NPCConversationStatus
         *  - Converse
         *  - Train                     => not used by client
         */

        private static NpcManager _instance;
        private static readonly object InstanceLock = new object();

        public static NpcManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new NpcManager();
                    }
                }

                return _instance;
            }
        }

        private NpcManager()
        {
        }

        #region NPC

        public void NpcInit()
        {

        }

        public void AssignNPCMission(Client client, AssignNPCMissionPacket packet)
        {
            var mission = MissionManager.Instance.LoadedMissions[packet.MissionId];

            if (client.MapClient.Player.Missions.Count > 30)
            {
                CommunicatorManager.Instance.SystemMessage(client, "Mission log is full.");
                return;
            }

            client.CallMethod(client.MapClient.Player.Actor.EntityId, new MissionGainedPacket(packet.MissionId, mission.MissionInfo));
        }

        public void CompleteNPCMission(Client client, CompleteNPCMissionPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: CompleteNPCMission");
        }

        public void RequestNpcConverse(Client client, RequestNPCConversePacket packet)
        {
            var creature = EntityManager.Instance.GetCreature((uint)packet.EntityId);

            if (creature == null)
                return;

            // ToDo create DB structures, and replace constant data with dinamic

            var convoDataDict = new Dictionary<ConversationType, object>();

            if (creature.Npc.Vendor != null)
                convoDataDict.Add(ConversationType.Vending, new List<uint> { creature.Npc.Vendor.VendorPackageId });

            if (creature.Npc.NpcMissionIds != null)
                if (creature.Npc.NpcMissionIds.Count > 0)
                {
                    var dispensableMissions = new Dictionary<uint, MissionInfo>();
                    var completeableMissions = new Dictionary<uint, RewardInfo>();
                    var completeableObjectives = new List<CompleteableObjectives>();

                    foreach (var missionId in creature.Npc.NpcMissionIds)
                    {
                        var mission = MissionManager.Instance.LoadedMissions[missionId];

                        if (mission.MissionGiver == creature.DbId)
                            dispensableMissions.Add(mission.MissionId, mission.MissionInfo);

                        if (mission.MissionReciver == creature.DbId)
                            completeableMissions.Add(mission.MissionId, mission.MissionInfo.MissionConstantData.RewardInfo);
                    }

                    // insert data into convoDataDict
                    if (dispensableMissions.Count > 0)
                        convoDataDict.Add(ConversationType.MissionDispense, dispensableMissions);

                    if (completeableMissions.Count > 0)
                        convoDataDict.Add(ConversationType.MissionComplete, completeableMissions);

                    if (completeableObjectives.Count > 0)
                        convoDataDict.Add(ConversationType.ObjectiveChoice, completeableObjectives);
                }

            // Auctioner = 14
            if (creature.Npc.NpcIsAuctioneer)
                convoDataDict.Add(ConversationType.Auctioneer, true);

            if (creature.Npc.NpcIsClanMaster)
                convoDataDict.Add(ConversationType.Clan, true);

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

            client.CallMethod(creature.Actor.EntityId, new ConversePacket(convoDataDict));
        }

        public void UpdateConversationStatus(Client client, Creature creature)
        {
            var npc = creature.Npc;
            var vendor = creature.Npc.Vendor;
            var statusSet = false;

            /* ToDo
             * implement Player=>MissionStatus
             * npc missions shold be checked with player mission status
             */

            if (npc.NpcMissionIds != null)
            {
                var availableMissions = new List<uint>();
                var completeMission = new List<uint>();

                foreach (var missionId in npc.NpcMissionIds)
                {
                    var mission = MissionManager.Instance.LoadedMissions[missionId];

                    if (mission.MissionReciver == creature.DbId)
                        completeMission.Add(missionId);

                    if (mission.MissionGiver == creature.DbId)
                        availableMissions.Add(missionId);
                }

                // if we have completable mission send it, else send available missions
                if (completeMission.Count > 0)
                    client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.ObjectivComplete, completeMission));  // complete mission
                else
                    client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Available, availableMissions));       // available missions

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
            if (vendor != null && statusSet == false)
            {
                // creature->npcData.isVendor
                client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Vending, new List<uint> { vendor.VendorPackageId })); // status - vending
                statusSet = true;
            }

            // is NPC auctioner?
            if (creature.Npc.NpcIsAuctioneer && statusSet == false)
            {
                client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Auctioneer, new List<uint>())); // status - none
                statusSet = true;
            }

            // is NPC clan master?
            if (creature.Npc.NpcIsClanMaster && statusSet == false)
            {
                client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Clan, new List<uint>())); // status - none
                statusSet = true;
            }

            // no status set yet? Send NONE conversation status
            if (statusSet == false)
            {
                // no other status, set NONE status
                client.CallMethod(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.None, new List<uint>()));// status - none

                statusSet = true;
            }
        }
        #endregion

        #region Auctioneer
        public void RequestNPCOpenAuctionHouse(Client client, long entityId)
        {
            client.CallMethod((uint)entityId, new OpenAuctionHousePacket());
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

                foreach (var itemTemplateId in creature.Npc.Vendor.VendorItems)
                {
                    var item = ItemManager.Instance.CreateVendorItem(client, itemTemplateId);

                    itemList.Add(item);
                    entityList.Add(item.EntityId);
                }

                EntityManager.Instance.RegisterVendorItem((uint)packet.EntityId, entityList);
            }

            client.CallMethod((uint)packet.EntityId, new VendPacket(itemList));
        }

        public void RequestCancelVendor(Client client, long entityId)
        {
            Logger.WriteLog(LogType.Debug, $"RequestCancelVendor => ToDo"); // ToDo
        }

        public void RequestVendorBuyback(Client client, RequestVendorBuybackPacket packet)
        {
            client.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveBuybackItemPacket((uint)packet.ItemEntityId));

            var buyBackItem = EntityManager.Instance.GetItem((uint)packet.ItemEntityId);
            var buyedItem = InventoryManager.Instance.AddItemToInventory(client, buyBackItem);
            var buyPrice = (int)buyedItem.Stacksize * buyedItem.ItemTemplate.SellPrice;

            // remove credits
            ManifestationManager.Instance.LossCredits(client, -buyPrice);
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
            var buyPrice = selectedVendorItem.ItemTemplate.BuyPrice * (int)itemQuantity;

            if (client.MapClient.Player.Credits[CurencyType.Credits] < buyPrice)
                return; // not enough credits

            // duplicate item
            var boughtItem = ItemManager.Instance.DuplicateItem(client, packet);

            if (boughtItem == null)
                return; // could not duplicate item

            var tempItem = boughtItem;
            var desiredStacksize = tempItem.Stacksize;

            boughtItem = InventoryManager.Instance.AddItemToInventory(client, boughtItem);

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

            // send player message
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmGotLootFromUnknown, new Dictionary<string, string> { { "quantity", itemQuantity.ToString() }, { "loot", boughtItem.ItemTemplate.Class.ToString() } }, MsgFilterId.LootObtained));

            // get correct buy price
            buyPrice = boughtItem.ItemTemplate.BuyPrice * (int)itemQuantity;

            // remove credits
            ManifestationManager.Instance.LossCredits(client, -buyPrice);
        }

        public void RequestVendorRepair(Client client, RequestVendorRepairPacket packet)
        {
            foreach (var itemEntityId in packet.ItemEntitesId)
            {
                var item = EntityManager.Instance.GetItem((uint)itemEntityId);
                var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);

                // calculate cost
                var cost = (double)((classInfo.ItemClassInfo.MaxHitPoints - item.CurrentHitPoints) * item.ItemTemplate.SellPrice) / 100;
                // set itemHit points to full
                item.CurrentHitPoints = classInfo.ItemClassInfo.MaxHitPoints;
                // remove player credits
                ManifestationManager.Instance.LossCredits(client, -(int)Math.Round(cost, 0));
                // updateItem on client
                ItemManager.Instance.SendItemDataToClient(client, item, true);
                // updare item in db
                ItemsTable.UpdateCurrentHitPoints(item.ItemId, item.CurrentHitPoints);
            }
        }

        public void RequestVendorSale(Client client, RequestVendorSalePacket packet)
        {
            var vendorEntityId = packet.VendorEntityId;
            var itemEntityId = packet.ItemEntityId;
            var itemQuantity = (uint)packet.Quantity;

            // note: Players can only sell items directly from their personal inventory
            //       so we only have to scan there for the item entityId
            var slotIndex = 250U;

            for (var i = 0; i < 250; i++)
            {
                if (client.MapClient.Inventory.PersonalInventory[i] == itemEntityId)
                {
                    slotIndex = (uint)i;
                    break;
                }
            }

            if (slotIndex == 250U)
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
            var sellPrice = soldItem.ItemTemplate.SellPrice * (int)realItemQuantity;
            // remove item
            // todo: Handle stacksizes correctly and only decrease item by quantity parameter
            InventoryManager.Instance.RemoveItemBySlot(client, InventoryType.Personal, slotIndex);
            CharacterInventoryTable.DeleteInvItem(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, (int)InventoryType.Personal, slotIndex);
            // add credits to player
            ManifestationManager.Instance.GainCredits(client, sellPrice);
            // add item to buyback list
            client.CallMethod(SysEntity.ClientInventoryManagerId, new AddBuybackItemPacket((uint)packet.ItemEntityId, (int)sellPrice, 1));
        }
        #endregion
    }
}
