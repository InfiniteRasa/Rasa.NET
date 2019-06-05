using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;
    using Timer;

    public class MapChannelManager
    {

        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        public const int MapChannel_PlayerQueue = 32;
        private readonly Dictionary<uint, MapChannel> MapChannelsByContextId = new Dictionary<uint, MapChannel>();     // list of maps that need to be loaded
        public readonly Dictionary<uint, MapChannel> MapChannelArray = new Dictionary<uint, MapChannel>();           // list of loaded maps
        public readonly Timer Timer = new Timer();

        public static MapChannelManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapChannelManager();
                    }
                }

                return _instance;
            }
        }

        private MapChannelManager()
        {
        }

        public void CharacterLogout(Client client)
        {
            if (client.MapClient.LogoutActive == false)
                return;

            client.MapClient.RemoveFromMap = true;
            client.State = ClientState.LoggedIn;
        }

        public Manifestation CreatePlayerCharacter(Client client)
        {
            var character = CharacterTable.GetCharacter(client.AccountEntry.Id, client.AccountEntry.SelectedSlot);
            var lockboxInfo = CharacterLockboxTable.GetLockboxInfo(client.AccountEntry.Id);
            var appearances = CharacterAppearanceTable.GetAppearances(character.Id);
            var missions = CharacterMissionsTable.GetMissions(client.AccountEntry.Id, client.AccountEntry.SelectedSlot);
            var appearanceData = new Dictionary<EquipmentData, AppearanceData>();
            var missionData = new Dictionary<int, MissionLog>();
            foreach (var appearanceEntry in appearances)
            {
                var appearance = new AppearanceData(appearanceEntry.Value);

                appearanceData.Add(appearance.SlotId, appearance);
            }

            foreach (var mission in missions)
                missionData.Add(mission.MissionId, new MissionLog { MissionId = mission.MissionId, MissionState = mission.MissionState });

            var player = new Manifestation(character, appearanceData)
            {
                Actor = new Actor
                {
                    EntityClassId = character.Gender == 0 ? EntityClassId.HumanBaseMale : EntityClassId.HumanBaseFemale,
                    Name = character.Name,
                    FamilyName = client.AccountEntry.FamilyName,
                    Position = new Vector3(character.CoordX, character.CoordY, character.CoordZ),
                    Orientation = character.Orientation,
                    MapContextId = character.MapContextId,
                    IsRunning = true,
                    InCombatMode = false,
                    Attributes = new Dictionary<Attributes, ActorAttributes>
                    {
                        { Attributes.Body, new ActorAttributes(Attributes.Body, 0, 0, 0, 0, 0) },
                        { Attributes.Mind, new ActorAttributes(Attributes.Mind, 0, 0, 0, 0, 0) },
                        { Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 0, 0, 0, 0, 0) },
                        { Attributes.Health, new ActorAttributes(Attributes.Health, 0, 0, 0, 0, 0) },
                        { Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0) },
                        { Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0) },
                        { Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0) },
                        { Attributes.Armor, new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0) },
                        { Attributes.Speed, new ActorAttributes(Attributes.Speed, 0, 0, 0, 0, 0) },
                        { Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0) }
                    }
                },
                //ClanId = data.ClanId,
                //ClanName = data.ClanName,
                GainedWaypoints = CharacterTeleportersTable.GetTeleporters(character.Id),
                LockboxCredits = lockboxInfo[0],
                LockboxTabs = lockboxInfo[1],
                Skills = GetPlayerSkills(client),
                Titles = CharacterTitlesTable.GetCharacterTitles(client.AccountEntry.Id, client.AccountEntry.SelectedSlot),
                //CurrentTitle = data.CurrentTitle,
                Abilities = GetPlayerAbilities(client.AccountEntry.Id, client.AccountEntry.SelectedSlot),
                //CurrentAbilityDrawer = data.CurrentAbilityDrawer,
                Missions = missionData,
                LoginTime = DateTime.Now,
                Logos = CharacterLogosTable.GetLogos(client.AccountEntry.Id, client.AccountEntry.SelectedSlot),
                FamilyName = client.AccountEntry.FamilyName
            };

            return player;
        }

        public MapChannel FindByContextId(uint contextId)
        {
            return MapChannelArray[contextId];
        }

        public Dictionary<int, AbilityDrawerData> GetPlayerAbilities(uint accountId, uint characterSlot)
        {
            var abilities = new Dictionary<int, AbilityDrawerData>();
            var abilitiesData = CharacterAbilityDrawerTable.GetCharacterAbilities(accountId, characterSlot);

            for (var i = 0; i < 25; i++)
                if (abilitiesData[i * 3 + 1] > 0)   // don't insert if there is no ablility in slot
                    abilities.Add(abilitiesData[i * 3], new AbilityDrawerData { AbilitySlotId = abilitiesData[i * 3], AbilityId = abilitiesData[i * 3 + 1], AbilityLevel = abilitiesData[i * 3 + 2] });

            return abilities;
        }

        public Dictionary<int, SkillsData> GetPlayerSkills(Client client)
        {
            var skills = new Dictionary<int, SkillsData>();
            var skillsData = CharacterSkillsTable.GetCharacterSkills(client.AccountEntry.Id, client.AccountEntry.SelectedSlot);

            foreach (var skill in skillsData)
                skills.Add(skill.SkillId, new SkillsData(skill.SkillId, skill.AbilityId, skill.SkillLevel));

            return skills;
        }

        public void MapChannelInit()
        {
            MapChannelsByContextId.Add(1985, new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 0) });
            MapChannelsByContextId.Add(1220, new MapChannel { MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0) });
            MapChannelsByContextId.Add(1148, new MapChannel { MapInfo = new MapInfo(1148, "adv_foreas_concordia_divide", 1584, 0) });

            foreach (var t in MapChannelsByContextId)
            {
                var id = t.Key;
                // load all maps
                var newMapChannel = new MapChannel
                {
                    MapInfo = MapChannelsByContextId[id].MapInfo,
                    //TimerClientEffectUpdate = Environment.TickCount,
                    //TimerMissileUpdate = Environment.TickCount,
                    //TimerDynObjUpdate = Environment.TickCount,
                    //TimerGeneralTimer = Environment.TickCount,
                    //TimerController = Environment.TickCount,
                    //TimerPlayerUpdate = Environment.TickCount,
                    //PlayerCount = 0,
                    PlayerLimit = 128,
                    ClientList = new List<Client>()
                };
                // register mapChannel
                MapChannelArray.Add(id, newMapChannel);
            }

            foreach (var t in MapChannelArray)
            {
                var mapChannel = t.Value;
                //navmesh_initForMapChannel(mapChannel);
                //dynamicObject_init(mapChannel);
                //mission_initForChannel(mapChannel);
                //missile_initForMapchannel(mapChannel);
                //SpawnPoolManager.Instance.InitForMapChannel(mapChannel);
                //controller_initForMapChannel(mapChannel);
                //teleporter_initForMapChannel(mapChannel); //---load teleporters
                //logos_initForMapChannel(mapChannel); // logos world objects
                Console.WriteLine("Map: {0} loaded", mapChannel.MapInfo.MapName);
            }

            Console.WriteLine("\nMapChannels Started...");

            Timer.Add("AutoFire", 100, true, null);
            Timer.Add("CheckForLogingClients", 1000, true, null);
            Timer.Add("CheckForObjects", 1000, true, null);
            Timer.Add("ClientEffectUpdate", 500, true, null);
            Timer.Add("CellUpdateVisibility", 1000, true, null);
            Timer.Add("CheckForCreatures", 1000, true, null);
        }

        public void MapChannelWorker(long delta)
        {
            Timer.Update(delta);

            foreach (var t in MapChannelArray)
            {
                var mapChannel = t.Value;

                mapChannel.MapChannelElapsed += delta;

                if (Timer.IsTriggered("CheckForLogingClients"))
                    if (mapChannel.QueuedClients.Count > 0)
                    {
                        // create new mapClient
                        var dequedClient = mapChannel.QueuedClients.Dequeue();
                        var mapClient = new MapChannelClient { MapChannel = mapChannel };

                        dequedClient.MapClient = mapClient;

                        // add it to list
                        mapChannel.ClientList.Add(dequedClient);
                    }

                if (mapChannel.ClientList.Count > 0)
                {
                    ActorActionManager.Instance.DoWork(mapChannel, delta);
                    MissileManager.Instance.DoWork(mapChannel, delta);
                    BehaviorManager.Instance.MapChannelThink(mapChannel, delta);

                    // check forAutoFIre
                    if (Timer.IsTriggered("AutoFire"))
                        ManifestationManager.Instance.AutoFireTimerDoWork(delta);

                    // CellManager worker
                    if (Timer.IsTriggered("CellUpdateVisibility"))
                        CellManager.Instance.DoWork(mapChannel);

                    // check for objects
                    if (Timer.IsTriggered("CheckForObjects"))
                        DynamicObjectManager.Instance.DynamicObjectWorker(mapChannel, delta);
                    
                    // check for creatures
                    if (Timer.IsTriggered("CheckForCreatures"))
                        SpawnPoolManager.Instance.SpawnPoolWorker(mapChannel, delta);

                    // check for effects (buffs)
                    if (Timer.IsTriggered("ClientEffectUpdate"))
                        GameEffectManager.Instance.DoWork(mapChannel, delta);

                    // chack for player LogOut
                    foreach (var client in mapChannel.ClientList)
                        if (client != null)
                            if (client.MapClient.RemoveFromMap == true)
                            {
                                RemovePlayer(client, true);
                                break;
                            }

                    // ToDo check for timers
                    //missile_check(mapChannel, 100);
                    // do other work

                    // check timers

                    //gameEffect_checkForPlayers(mapChannel->playerList, mapChannel->playerCount, 500);
                    //Debugger.Break();
                    //if (Timer.IsTriggered("MissileCheck"))
                    //missile_check(mapChannel, 100);
                    /*if (currentTime - mapChannel.TimerMissileUpdate >= 100)
                    {
                        //missile_check(mapChannel, 100);
                        mapChannel.TimerMissileUpdate += 100;
                    }
                    if (currentTime - mapChannel.TimerDynObjUpdate >= 100)
                    {
                        //dynamicObject_check(mapChannel, 100);
                        mapChannel.TimerDynObjUpdate += 100;
                    }
                    if (currentTime - mapChannel.TimerController >= 250)
                    {
                        //mapteleporter_checkForEntityInRange(mapChannel);
                        //controller_mapChannelThink(mapChannel);
                        mapChannel.TimerController += 250;
                    }
                    if ((currentTime - mapChannel.TimerPlayerUpdate) >= 1000)
                    {
                        var playerUpdateTick = currentTime - mapChannel.TimerPlayerUpdate;
                        mapChannel.TimerPlayerUpdate = currentTime;
                        for (var i = 0; i < mapChannel.PlayerCount; i++)
                        {
                            //manifestation_updatePlayer(mapChannel->playerList[i], playerUpdateTick);
                        }
                    }*/
                    /*if (currentTime - mapChannel.TimerGeneralTimer >= 100)
                    {
                        var timePassed = 100;
                        // queue for deleting map timers
                        std::vector<mapChannelTimer_t*> queue_timerDeletion;
                        // parse through all timers
                        mapChannel_check_AutoFireTimers(mapChannel);
                        std::vector<mapChannelTimer_t*>::iterator timer = mapChannel->timerList.begin();
                        while (timer != mapChannel->timerList.end())
                        {
                            (*timer)->timeLeft -= timePassed;
                            if ((*timer)->timeLeft <= 0)
                            {
                                sint32 objTimePassed = (*timer)->period - (*timer)->timeLeft;
                                (*timer)->timeLeft += (*timer)->period;
                                // trigger object
                                bool remove = (*timer)->cb(mapChannel, (*timer)->param, objTimePassed);
                                if (remove == false)
                                    queue_timerDeletion.push_back(*timer);
                            }
                            timer++;
                        }
                        // parse deletion queue
                        if (queue_timerDeletion.empty() != true)
                        {
                            mapChannelTimer_t** timerList = &queue_timerDeletion[0];
                            sint32 timerCount = queue_timerDeletion.size();
                            for (sint32 f = 0; f < timerCount; f++)
                            {
                                mapChannelTimer_t* toBeDeletedTimer = timerList[f];
                                // remove from timer list
                                std::vector<mapChannelTimer_t*>::iterator itr = mapChannel->timerList.begin();
                                while (itr != mapChannel->timerList.end())
                                {
                                    if ((*itr) == toBeDeletedTimer)
                                    {
                                        mapChannel.TimerList.erase(itr);
                                        break;
                                    }
                                    ++itr;
                                }
                            }
                        }
                        mapChannel.TimerGeneralTimer += 100;
                    }*/
                }
            }
        }

        public void MapLoaded(Client client)
        {
            client.State = ClientState.Ingame;
            client.MapClient.Player = CreatePlayerCharacter(client);
            InventoryManager.Instance.InitForClient(client);
            ManifestationManager.Instance.UpdateStatsValues(client, true);

            // register new Player
            EntityManager.Instance.RegisterEntity(client.MapClient.Player.Actor.EntityId, EntityType.Player);
            EntityManager.Instance.RegisterPlayer(client.MapClient.Player.Actor.EntityId, client.MapClient);
            EntityManager.Instance.RegisterActor(client.MapClient.Player.Actor.EntityId, client.MapClient.Player.Actor);
            CommunicatorManager.Instance.LoginOk(client);
            CellManager.Instance.AddToWorld(client); // will introduce the player to all clients, including the current owner
            ManifestationManager.Instance.AssignPlayer(client);
            CommunicatorManager.Instance.RegisterPlayer(client);

            // Must be called after AssignPlayer and RegisterPlayer so that IsOnline status can be accurately checked
            ClanManager.Instance.InitializePlayerClanData(client);
            InventoryManager.Instance.InitClanInventory(client);
            CommunicatorManager.Instance.PlayerEnterMap(client);
            //mission_initForClient(cm);
        }

        public void PassClientToMapChannel(Client client, MapChannel mapChannel)
        {
            client.State = ClientState.Loading;
            mapChannel.QueuedClients.Enqueue(client);
        }

        public void PassClientToCharacterSelection(Client client)
        {
            // ToDo
            /*if (ClientsGameMainCount >= MAX_GAMEMAIN_CLIENTS)
            {
                // force disconnect
                closesocket(cgm->socket);
                //free(cgm);
                return;
            }*/
            CharacterManager.Instance.StartCharacterSelection(client);
            //Increase count and return struct
            //ClientsGameMainCount++;
        }

        public void Ping(Client client, double ping)
        {
            client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(ping));
        }

        public void RemovePlayer(Client client, bool logout)
        {
            // unregister Communicator
            CommunicatorManager.Instance.PlayerExitMap(client);
            // unregister mapChannelClient
            EntityManager.Instance.UnregisterEntity(client.MapClient.Player.Actor.EntityId);
            EntityManager.Instance.UnregisterPlayer(client.MapClient.Player.Actor.EntityId);
            EntityManager.Instance.UnregisterActor(client.MapClient.Player.Actor.EntityId);

            // unregister character Inventory
            foreach (var entityId in client.MapClient.Inventory.EquippedInventory)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            foreach (var entityId in client.MapClient.Inventory.HomeInventory)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            foreach (var entityId in client.MapClient.Inventory.PersonalInventory)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            foreach (var entityId in client.MapClient.Inventory.WeaponDrawer)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            // unregister from chat
            CommunicatorManager.Instance.UnregisterPlayer(client);
            CellManager.Instance.RemoveFromWorld(client);
            ManifestationManager.Instance.RemovePlayerCharacter(client);
            ClanManager.Instance.RemovePlayer(client);

            if (logout)
                if (client.MapClient.Disconected == false)
                {
                    PassClientToCharacterSelection(client);
                    client.MapClient.Disconected = true;
                }

            // remove from list
            for (var i = 0; i < client.MapClient.MapChannel.ClientList.Count; i++)
            {
                if (client == client.MapClient.MapChannel.ClientList[i])
                {
                    client.MapClient.MapChannel.ClientList.RemoveAt(i);
                    //mapClient.MapChannel.PlayerCount--;
                    break;
                }
            }

        }

        public void RequestLogout(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket());
            client.MapClient.LogoutActive = true;
        }
    }
}