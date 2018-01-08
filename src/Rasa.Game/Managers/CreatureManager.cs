using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
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
        public readonly Dictionary<uint, CreatureType> LoadedCreatureTypes = new Dictionary<uint, CreatureType>();           // list of loaded Creatures
        public readonly Dictionary<uint, Creature> LoadedCreatures = new Dictionary<uint, Creature>();

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

        public Creature CreateCreature(uint dbId, SpawnPool spawnPool)
        {
            // check is creature in database
            if (!LoadedCreatures.ContainsKey(dbId))
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId}, isn't in database");
                return null;
            }

            // crate creature
            var creature = new Creature
            {
                Actor = new Actor(),
                AppearanceData = LoadedCreatures[dbId].AppearanceData,
                CreatureType = LoadedCreatures[dbId].CreatureType,
                DbId = LoadedCreatures[dbId].DbId,
                Faction = LoadedCreatures[dbId].Faction,
                Level = LoadedCreatures[dbId].Level,
                MaxHitPoints = LoadedCreatures[dbId].MaxHitPoints,
                NameId = LoadedCreatures[dbId].NameId,
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

            mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(creature.Actor.EntityId, creature.CreatureType.ClassId, entityData));

            // NPC augmentation
            if (creature.CreatureType.NpcData != null)
            {
                /*
             * NPCInfo
             * NPCConversationStatus
             * Converse
             * Train
             */
                UpdateConversationStatus(mapClient.Client, creature);
                mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new NPCInfoPacket(726));
                //mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new ConversePacket());
                //mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationType.Vending, new List<int> { 10 }));
            }
            /*if (creature->actor.entityClassId == 25580) // pistol
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 3782);
            }
            if (creature->actor.entityClassId == 25581) // rifle
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 3878);
            }
            if (creature->actor.entityClassId == 6163) // staff
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 6164);
            }
            if (creature->actor.entityClassId == 6043) // spear
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 6042);
            }
            */
            /*if (creature->actor.entityClassId == 29765) // pistol
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 6443);
                // weapon ready
                pym_init(&pms);
                pym_tuple_begin(&pms);
                pym_addBool(&pms, true);
                pym_tuple_end(&pms);
                netMgr_pythonAddMethodCallRaw(client->cgm, creature->actor.entityId, 575, pym_getData(&pms), pym_getLen(&pms));
            }
            if (creature->actor.entityClassId == 29423)
            {
                creature_updateAppearance(client->cgm, creature->actor.entityId, 6443);
                // weapon ready
                pym_init(&pms);
                pym_tuple_begin(&pms);
                pym_addBool(&pms, true);
                pym_tuple_end(&pms);
                netMgr_pythonAddMethodCallRaw(client->cgm, creature->actor.entityId, 575, pym_getData(&pms), pym_getLen(&pms));
            }*/

            /*if (creature->actor.stats.healthCurrent <= 0)
            {
                creature->actor.state = ACTOR_STATE_DEAD;
                // dead!
                pym_init(&pms);
                pym_tuple_begin(&pms);
                pym_list_begin(&pms);
                pym_addInt(&pms, 5); // dead
                pym_list_end(&pms);
                pym_tuple_end(&pms);
                netMgr_pythonAddMethodCallRaw(client->cgm, creature->actor.entityId, 206, pym_getData(&pms), pym_getLen(&pms));
                // fix health
                creature->actor.stats.healthCurrent = 0;
            }*/
            /*if (creature->type->npcData)
            {
                npc_creature_updateConversationStatus(client, creature);
                // send Recv_NPCInfo (only npcPackageId)
                // the only reason to send this is because the language lookup for mission objectives needs it...
                if (creature->type->npcData->npcPackageId != 0)
                {
                    pym_init(&pms);
                    pym_tuple_begin(&pms);
                    pym_addInt(&pms, creature->type->npcData->npcPackageId); // the glorious npcPackageId
                    pym_tuple_end(&pms);
                    netMgr_pythonAddMethodCallRaw(client->cgm, creature->actor.entityId, 490, pym_getData(&pms), pym_getLen(&pms));
                }
            }*/
        }

        public Creature FindCreature(uint creatureId)
        {
            return LoadedCreatures[creatureId];
        }

        public void CreatureInit()
        {
            var creatureList = CreatureTable.LoadCreatures();

            foreach (var data in creatureList)
            {
                var appearanceData = CreatureAppearenceTable.GetCreatureAppearence(data.DbId);
                var tempAppearnece = new Dictionary<EquipmentSlots, AppearanceData>();
                if (appearanceData != null)
                {
                    tempAppearnece.Add(EquipmentSlots.Helmet, new AppearanceData { SlotId = 1, ClassId = appearanceData.Helmet, Color = new Color(appearanceData.HelmetHue) });
                    tempAppearnece.Add(EquipmentSlots.Shoes, new AppearanceData { SlotId = 2, ClassId = appearanceData.Shoes, Color = new Color(appearanceData.ShoesHue) });
                    tempAppearnece.Add(EquipmentSlots.Gloves, new AppearanceData { SlotId = 3, ClassId = appearanceData.Gloves, Color = new Color(appearanceData.GlovesHue) });
                    tempAppearnece.Add(EquipmentSlots.Weapon, new AppearanceData { SlotId = 13, ClassId = appearanceData.Weapon, Color = new Color(appearanceData.WeaponHue) });
                    tempAppearnece.Add(EquipmentSlots.Hair, new AppearanceData { SlotId = 14, ClassId = appearanceData.Hair, Color = new Color(appearanceData.HairHue) });
                    tempAppearnece.Add(EquipmentSlots.Torso, new AppearanceData { SlotId = 15, ClassId = appearanceData.Torso, Color = new Color(appearanceData.TorsoHue) });
                    tempAppearnece.Add(EquipmentSlots.Legs, new AppearanceData { SlotId = 16, ClassId = appearanceData.Legs, Color = new Color(appearanceData.LegsHue) });
                    tempAppearnece.Add(EquipmentSlots.Face, new AppearanceData { SlotId = 17, ClassId = appearanceData.Face, Color = new Color(appearanceData.FaceHue) });
                    tempAppearnece.Add(EquipmentSlots.EyeWear, new AppearanceData { SlotId = 19, ClassId = appearanceData.EyeWear, Color = new Color(appearanceData.EyeWearHue) });
                    tempAppearnece.Add(EquipmentSlots.Beard, new AppearanceData { SlotId = 20, ClassId = appearanceData.Beard, Color = new Color(appearanceData.BeardHue) });
                    tempAppearnece.Add(EquipmentSlots.Mask, new AppearanceData { SlotId = 21, ClassId = appearanceData.Mask, Color = new Color(appearanceData.MaskHue) });
                }

                var creature = new Creature
                {
                    DbId = data.DbId,
                    CreatureType = LoadedCreatureTypes[data.CreatureType],
                    Faction = data.Faction,
                    Level = data.Level,
                    MaxHitPoints = data.MaxHitPoints,
                    NameId = data.NameId,
                    AppearanceData = tempAppearnece

                };

                LoadedCreatures.Add(creature.DbId, creature);
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedCreatures.Count} Creatures");
        }

        public void CreatureTypesInit()
        {
            var creatureTypeData = CreatureTypesTable.LoadCreatureTypes();

            foreach (var data in creatureTypeData)
            {
                var creatureType = new CreatureType
                {
                    DbId = data.DbId,
                    ClassId = data.ClassId
                };

                /*  ToDo
                if (creatureData.IsAuctioner > 0)
                    creature.CreatureType.AuvtionerData = new AuctionerData();
                */

                if (data.IsHarvestable > 0)
                    creatureType.LootData = new CreatureLootData();

                if (data.IsNpc > 0)
                    creatureType.NpcData = new CreatureNpcData();

                if (data.IsVendor > 0)
                    creatureType.VendorData = new CreatureVendorData();

                LoadedCreatureTypes.Add(creatureType.DbId, creatureType);
            }
            
            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedCreatureTypes.Count} CreatureTypes");

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

        public void RequestNpcConverse(Client client, RequestNPCConversePacket packet)
        {
            var creature = EntityManager.Instance.GetCreature((uint)packet.EntityId);

            if (creature == null)
                return;
            /*
            var npcData = creature.NpcData;

            if (npcData == null)
                return;

            if (npcData.RelatedMissionCount > 64)
            {
                Logger.WriteLog(LogType.Debug, "NPC has more than 64 missions! Truncating list...");
                npcData.RelatedMissionCount = 64;
            }
            // collect player state info about provided missions
            var missionState = new List<int>();     // -2 means mission completed, -1 means not accepted
            /*
            foreach (var mission in npcData.RelatedMissions)
            {
                //missionState.Add(MISSION_STATE_NOTACCEPTED;
                var missionLog = MissionManager.Instance.FindPlayerMission(client, mission.MissionIndex);

                if (missionLog != null)
                {
                    // check specific state of mission
                    var getMission = MissionManager.Instance.GetById(missionLog.MissionIndex);
                    if (getMission == null)
                    {
                        missionState[i] = MISSION_STATE_COMPLETED;
                        continue;
                    }
                    if (missionLogEntry->state >= (mission->stateCount - 1))
                    {
                        missionState[i] = MISSION_STATE_COMPLETEABLE;
                        continue;
                    }
                    // check specific state of mission
                    missionState[i] = missionLogEntry->state;
                }
                else if (mission_isCompletedByPlayer(client, npcData->relatedMissions[i].missionIndex) == true)
                {
                    missionState[i] = MISSION_STATE_COMPLETED;
                }
            }

            // count mission types
            sint32 countMissionAvailable = 0;
            sint32 countMissionCompletable = 0;
            sint32 countMissionObjectiveCompletable = 0;
            for (uint32 i = 0; i < npcData->relatedMissionCount; i++)
            {
                if (missionState[i] == MISSION_STATE_COMPLETED)
                    ; // completed mission not available
                else if (missionState[i] == MISSION_STATE_NOTACCEPTED)
                    countMissionAvailable++;
                else if (missionState[i] == MISSION_STATE_COMPLETEABLE)
                    countMissionCompletable++;
                else if (missionState[i] > 0)
                    countMissionObjectiveCompletable++;
            }
            */
            client.SendPacket(creature.Actor.EntityId, new ConversePacket());
            /*
            // build info about available missions
            if (countMissionAvailable > 0)
            {
                pym_addInt(&pms, 2); // key: CONVO_TYPE_MISSIONDISPENSE
                pym_dict_begin(&pms); // mission list
                for (uint32 i = 0; i < npcData->relatedMissionCount; i++)
                {
                    if (missionState[i] == MISSION_STATE_NOTACCEPTED)
                    {
                        mission_t* mission = mission_getByIndex(npcData->relatedMissions[i].missionIndex);
                        if (mission == NULL)
                            continue;
                        pym_addInt(&pms, mission->missionId); // missionID
                        pym_tuple_begin(&pms);  // mission info
                        pym_addInt(&pms, 1);    // level
                        mission_buildRewardInfoTuple(mission, &pms);
                        pym_addNoneStruct(&pms); // offerVOAudioSetId (NoneStruct for no-audio)
                        pym_list_begin(&pms);   // itemsRequired
                        pym_list_end(&pms);
                        pym_list_begin(&pms);   // objectives
                        pym_list_end(&pms);
                        pym_addInt(&pms, MISSION_GROUPTYPE_SOLO); // groupType
                        pym_tuple_end(&pms);
                    }
                }
                pym_dict_end(&pms);
            }
            // build info about objectives
            if (countMissionObjectiveCompletable > 0)
            {
                pym_addInt(&pms, 6); // key: CONVO_TYPE_OBJECTIVECOMPLETE
                pym_list_begin(&pms); // mission list
                for (uint32 i = 0; i < npcData->relatedMissionCount; i++)
                {
                    if (missionState[i] <= 0)
                        continue;
                    mission_t* mission = mission_getByIndex(npcData->relatedMissions[i].missionIndex);
                    sint32 scriptlineStart = mission->stateMapping[missionState[i]];
                    sint32 scriptlineEnd = mission->stateMapping[missionState[i] + 1];
                    for (sint32 l = scriptlineStart; l < scriptlineEnd; l++)
                    {
                        missionScriptLine_t* scriptline = mission->scriptLines + l;
                        if (scriptline->command == M_OP_COMPLETEOBJECTIVE)
                        {
                            if (creature->type->typeId == scriptline->value1)
                            {
                                pym_tuple_begin(&pms);  // mission info
                                pym_addInt(&pms, mission->missionId); // missionID
                                pym_addInt(&pms, scriptline->value2); // objectiveId
                                pym_addInt(&pms, scriptline->value3); // playerFlagId
                                pym_tuple_end(&pms);
                            }
                        }
                    }
                }
                pym_list_end(&pms);
            }
            // build info about completable missions
            if (countMissionCompletable > 0)
            {
                pym_addInt(&pms, 3); // key: CONVO_TYPE_MISSIONCOMPLETE
                pym_dict_begin(&pms); // mission list
                for (uint32 i = 0; i < npcData->relatedMissionCount; i++)
                {
                    if (missionState[i] == MISSION_STATE_COMPLETEABLE)
                    {
                        mission_t* mission = mission_getByIndex(npcData->relatedMissions[i].missionIndex);
                        if (mission == NULL)
                            continue;
                        pym_addInt(&pms, mission->missionId); // missionID
                        mission_buildRewardInfoTuple(mission, &pms);
                    }
                }
                pym_dict_end(&pms);
            }
            // build info about vendor data
            if (creature->type->vendorData)
            {
                pym_addInt(&pms, 11); // key: CONVO_TYPE_VENDING
                pym_list_begin(&pms); // vendor packageId list
                pym_addInt(&pms, creature->type->vendorData->vendorPackageId); // vendorPackageId (why do some parts support more than one vendorPackageId?)
                pym_list_end(&pms);
            }

            //mission_t *missionAvailableList[16];
            //sint32 missionAvailableCount; 
            //// send greeting
            //pym_addInt(&pms, 0); // key: CONVO_TYPE_GREETING
            ////pym_tuple_begin(&pms); // greeting data
            //pym_addInt(&pms, 6); // blah

            //missionAvailableCount = 16; // limit
            //if( mission_completeableAvailableForClient(npc->missionList, client, npc, missionAvailableList, &missionAvailableCount) )
            //{
            //	if( missionAvailableCount > 0 )
            //	{
            //		// CONVO_TYPE_MISSIONCOMPLETE (3)
            //		pym_addInt(&pms, 3); // key: CONVO_TYPE_MISSIONCOMPLETE
            //		pym_dict_begin(&pms); // mission list
            //		for(sint32 i=0; i<missionAvailableCount; i++)
            //		{
            //			mission_t *mission = missionAvailableList[i];
            //			if( !mission )
            //				continue;
            //			pym_addInt(&pms, mission->missionId); // missionID
            //			pym_tuple_begin(&pms); // rewardInfo
            //			  pym_tuple_begin(&pms); // fixed redward
            //			     pym_list_begin(&pms); // fixedReward-credits
            //			     pym_list_end(&pms); // fixedReward-credits
            //			     pym_list_begin(&pms); // fixedReward-items
            //			     pym_list_end(&pms); // fixedReward-items
            //			  pym_tuple_end(&pms); // fixed redward
            //		      pym_list_begin(&pms); // selectionList
            //			  pym_list_end(&pms);
            //			pym_tuple_end(&pms); 
            //			/*
            //				rewardInfo(T):
            //					fixedReward(T)
            //						credits(List)
            //							Number of credits, can have multiple or no entry?
            //						fixedItems(List)
            //							items (	'itemTemplateId'
            //									'itemClassId'
            //									'quantity'
            //									'hue'
            //									'moduleIds'
            //									'qualityId' )
            //					selectionList(List)
            //							items (see above)
            //			*/
            //		}
            //		pym_dict_end(&pms); // mission list
            //	}
            //}
            /*
            pym_dict_end(&pms);
            pym_tuple_end(&pms);
            netMgr_pythonAddMethodCallRaw(client->cgm, creature->actor.entityId, 433, pym_getData(&pms), pym_getLen(&pms));
            */
        }
        
        public void UpdateConversationStatus(Client client, Creature creature)
        {
            var npcData = creature.CreatureType.NpcData;
            var statusSet = false;

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
                            if (creature.CreatureType.DbId == scriptline.Value1) // same NPC?
                            {
                                // objective already completed?
                                if (missionLogEntry.MissionData[scriptline.StorageIndex] == 1)
                                    continue;

                                // send objective completable flag
                                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.ObjectivComplete, new List<int>())); // status - complete objective

                                statusSet = true;

                                break;
                            }
                            else if (scriptline.Command == MissionScriptCommand.Collector)
                            {
                                if (creature.CreatureType.DbId == scriptline.Value1) // same NPC?
                                {
                                    // mission already completed?
                                    if (missionLogEntry.State != (mission.StateCount - 1))
                                        continue;

                                    // send mission completable flag
                                    client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.MissionComplete, new List<int>())); // status - complete objective

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
                        client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Available, new List<int>())); // status - available

                        statusSet = true;

                        break;
                    }
                }
            }
            // is NPC vendor?
            if (creature.CreatureType.VendorData != null && statusSet == false )
            {
                // creature->npcData.isVendor
                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.Available, new List<int> { (creature.CreatureType.VendorData.VendorPackageId) })); // status - vending

                statusSet = true;
            }
            // no status set yet? Send NONE conversation status
            if (statusSet == false )
            {
                // no other status, set NONE status
                client.SendPacket(creature.Actor.EntityId, new NPCConversationStatusPacket(ConversationStatus.None, new List<int>()));// status - none

                statusSet = true;
            }
        }
        #endregion
    }
}
