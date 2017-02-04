using System.Collections.Generic;

namespace Rasa.Managers
{
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class CreatureManager
    {
        private static CreatureManager _instance;
        private static readonly object InstanceLock = new object();
        public const int CreatureLocationUpdateTime = 1500;

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

        public Creature CreateCreature(CreatureType creatureType, SpawnPool spawnPool)
        {
            // allocate and init creature
            var creature = new Creature();
            creature.CreatureType = creatureType; // direct pointer for fast access to type info
            creature.Actor = new Actor
            {
                EntityClassId = creatureType.EntityClassId,
                Name = creatureType.Name,
                Stats = creatureType.CreatureStats
            };
            creature.Level = 1; // test
            creature.UpdatePositionCounter = CreatureLocationUpdateTime;
            creature.SpawnPool = spawnPool;

            // Register Actor
            EntityManager.Instance.RegisterActor(creature.Actor.EntityId, creature.Actor);

            // set AI state
            //creature->controller.currentAction = BEHAVIOR_ACTION_WANDER;
            //creature->controller.actionWander.state = 0; //wanderstate: calc new position
            //set wander boundaries
            //creature->wander_dist = 11.12f;
            // update spawnpool
            if (creature.SpawnPool != null)
            {
                // TODO: check for dead-spawned creatures (We need this for the mission River Recon for example)
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(creature.SpawnPool);
            }

            return creature;
        }
        
        public void CreateCreatureOnClient(MapChannelClient mapClient, Creature creature)

        {
            if (creature == null)
                return;

            mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(creature.Actor.EntityId, creature.Actor.EntityClassId));
            // set attributes - Recv_AttributeInfo (29)
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new AttributeInfoPacket { ActorStats = creature.Actor.Stats });
            // set level
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new LevelPacket { Level = creature.Level });
            // set creature info (439)
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new CreatureInfoPacket { CreatureNameId = creature.CreatureType.NameId, IsFlyer = false, CreatureFlags = new List<int>() });    // ToDo add creature flags
            // set running
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new IsRunningPacket { IsRunning = false });
            // Recv_WorldLocationDescriptor (243)
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new WorldLocationDescriptorPacket
            {
                Position = creature.Actor.Position,
                RotationX = 0.0D,
                RotationY = 0.0D,
                RotationZ = 0.0D,
                RotationW = 1.0D
            });
            // TargetCategory
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new TargetCategoryPacket { TargetCategory = creature.CreatureType.Faction });  // HOSTILE
            // Recv_IsTargetable
            mapClient.Player.Client.SendPacket(creature.Actor.EntityId, new IsTargetablePacket { IsTargetable = true });

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
           /* if (creature->type->npcData)
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

        public void SetLocation(Creature creature, Position position, Quaternion rotation)
        {
            // set spawnlocation
            creature.Actor.Position = position;
            // set homelocation
            creature.HomePosition = position;
            //allocate pathnodes
            //creature->pathnodes = (baseBehavior_baseNode*)malloc(sizeof(baseBehavior_baseNode));
            //memset(creature->pathnodes, 0x00, sizeof(baseBehavior_baseNode));
            //creature->lastattack = GetTickCount();
            //creature->lastresttime = GetTickCount();
        }
    }
}
