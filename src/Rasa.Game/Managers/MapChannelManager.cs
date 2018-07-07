using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;
    using Timer;

    public class MapChannelManager
    {

        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        public const int MapChannel_PlayerQueue = 32;
        private readonly Dictionary<int,MapChannel> MapChannelsByContextId = new Dictionary<int, MapChannel>();     // list of maps that need to be loaded
        public readonly Dictionary<int, MapChannel> MapChannelArray = new Dictionary<int, MapChannel>();           // list of loaded maps
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
        }

        public void CreatePlayerCharacter(Client client)
        {
            var data = CharacterTable.GetCharacterData(client.Entry.Id, client.LoadingSlot);
            var lockboxInfo = CharacterLockboxTable.GetLockboxInfo(client.Entry.Id);
            var appearances = CharacterAppearanceTable.GetAppearance(client.Entry.Id, client.LoadingSlot);
            var missions = CharacterMissionsTable.GetMissions(client.Entry.Id, client.LoadingSlot);

            var appearanceData = new Dictionary<EquipmentSlots, AppearanceData>();
            var missionData = new Dictionary<int, MissionLog>();

            foreach (var appearance in appearances)
                appearanceData.Add((EquipmentSlots)appearance.SlotId, new AppearanceData { SlotId = (EquipmentSlots)appearance.SlotId, ClassId = appearance.ClassId, Color = new Color(appearance.Color) });

            foreach (var mission in missions)
                missionData.Add(mission.MissionId, new MissionLog{ MissionId = mission.MissionId, MissionState = mission.MissionState });
            
            var player = new PlayerData
            {
                Actor = new Actor
                {
                    EntityClassId = data.Gender == 0 ? 692 : 691,
                    Name = data.Name,
                    FamilyName = data.FamilyName,
                    Position = new Position(data.PosX, data.PosY, data.PosZ),
                    Rotation = new Quaternion(0D, 0D, 0D, 0D),    // ToDo
                    MapContextId = data.MapContextId,
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
                ControllerUser = client.MapClient,
                AppearanceData = appearanceData,
                AccountId = client.Entry.Id,
                CharacterSlot = client.LoadingSlot,
                Gender = data.Gender,
                Scale = data.Scale,
                RaceId = data.RaceId,
                ClassId = data.ClassId,
                Experience = data.Experience,
                Level = data.Level,
                Body = data.Body,
                Mind = data.Mind,
                Spirit = data.Spirit,
                CloneCredits = data.CloneCredits,
                NumLogins = data.NumLogins + 1,
                TotalTimePlayed = data.TotalTimePlayed,
                TimeSinceLastPlayed = data.TimeSinceLastPlayed,
                ClanId = data.ClanId,
                ClanName = data.ClanName,
                LockboxCredits = lockboxInfo[0],
                LockboxTabs = lockboxInfo[1],
                Credits = data.Credits,
                Prestige = data.Prestige,
                Skills = GetPlayerSkills(client),
                Titles = CharacterTitlesTable.GetCharacterTitles(client.Entry.Id, client.LoadingSlot),
                Abilities = GetPlayerAbilities(client.Entry.Id, client.LoadingSlot),
                CurrentAbilityDrawer = data.CurrentAbilityDrawer,
                Missions = missionData,
                LoginTime = 0,
                Logos = CharacterLogosTable.GetLogos(client.Entry.Id, client.LoadingSlot)
            };
            // register new Player
            EntityManager.Instance.RegisterEntity(player.Actor.EntityId, EntityType.Player);
            EntityManager.Instance.RegisterActor(player.Actor.EntityId, player.Actor);
            client.MapClient.Player = player;
            CommunicatorManager.Instance.LoginOk(client);
            CellManager.Instance.AddToWorld(client); // will introduce the player to all clients, including the current owner
            PlayerManager.Instance.AssignPlayer(client);
        }
        
        public MapChannel FindByContextId(int contextId)
        {
            return MapChannelArray[contextId];
        }

        public Dictionary<int, AbilityDrawerData> GetPlayerAbilities(uint accountId, uint characterSlot)
        {
            var abilities = new Dictionary<int, AbilityDrawerData>();
            var abilitiesData = CharacterAbilityDrawerTable.GetCharacterAbilities(accountId, characterSlot);

            for (var i = 0; i < 25; i++)
                if (abilitiesData[i * 3 + 1] > 0)   // don't insert if there is no ablility in slot
                    abilities.Add(abilitiesData[i * 3], new AbilityDrawerData { AbilitySlotId = abilitiesData[i * 3],  AbilityId = abilitiesData[i * 3 + 1], AbilityLevel = abilitiesData[i * 3 + 2] });

            return abilities;
        }

        public Dictionary<int, SkillsData> GetPlayerSkills(Client client)
        {
            var skills = new Dictionary<int, SkillsData>();
            var skillsData = CharacterSkillsTable.GetCharacterSkills(client.Entry.Id, client.LoadingSlot);

            foreach (var skill in skillsData)
                skills.Add(skill.SkillId, new SkillsData(skill.SkillId, skill.AbilityId, skill.SkillLevel));

            return skills;
        }

        public void MapChannelInit()
        {
            MapChannelsByContextId.Add(1220, new MapChannel { MapInfo = new MapInfo( 1220, "adv_foreas_concordia_wilderness", 1556, 0) });
            MapChannelsByContextId.Add(1148, new MapChannel { MapInfo = new MapInfo( 1148, "adv_foreas_concordia_divide", 1584 , 0) });
            
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
            Timer.Add("ClientEffectUpdate", 500, true, null);
            Timer.Add("CellUpdateVisibility", 300, true, null);
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
                        var mapClient = new MapChannelClient{ MapChannel = mapChannel };

                        dequedClient.MapClient = mapClient;
                       
                        // add it to list
                        mapChannel.ClientList.Add(dequedClient);
                    }

                if (mapChannel.ClientList.Count > 0)
                {
                    ActorActionManager.Instance.DoWork(mapChannel, delta);
                    MissileManager.Instance.DoWork(delta);
                    PlayerManager.Instance.AutoFireTimerDoWork(delta);

                    // CellManager worker
                    if (Timer.IsTriggered("CellUpdateVisibility"))
                        CellManager.Instance.DoWork(mapChannel);

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
            CreatePlayerCharacter(client);
            CommunicatorManager.Instance.RegisterPlayer(client);
            CommunicatorManager.Instance.PlayerEnterMap(client);
            InventoryManager.Instance.InitForClient(client);
            PlayerManager.Instance.UpdateStatsValues(client, true);
            //mission_initForClient(cm);
        }

        public void PassClientToMapChannel(Client client, MapChannel mapChannel)
        {
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
            client.SendPacket(5, new AckPingPacket(ping));
        }

        public void RegisterAutoFireTimer(Client client)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client.MapClient);

            if (weapon == null || weapon.ItemTemplate.WeaponInfo == null)
                return; // invalid entity or incorrect item type

            Timer.Add("CheckForLogingClients", 1000, true, null);
        }

        public void RemovePlayer(Client client, bool logout)
        {
            // unregister Communicator
            CommunicatorManager.Instance.PlayerExitMap(client);
            // unregister mapChannelClient
            EntityManager.Instance.UnregisterEntity(client.MapClient.Player.Actor.EntityId);
            EntityManager.Instance.UnregisterPlayer(client.MapClient.Player.Actor.EntityId);
            // unregister Actor
            EntityManager.Instance.UnregisterEntity(client.MapClient.Player.Actor.EntityId);
            EntityManager.Instance.UnregisterActor(client.MapClient.Player.Actor.EntityId);
            // unregister character Inventory
            for (var i = 0; i < 250; i++)
                if (client.MapClient.Inventory.PersonalInventory[i] != 0)
                {
                    EntityManager.Instance.DestroyPhysicalEntity(client, client.MapClient.Inventory.PersonalInventory[i], EntityType.Item);
                    client.MapClient.Inventory.PersonalInventory[i] = 0;
                }

            foreach (var entry in client.MapClient.Inventory.EquippedInventory)
                if (entry.Value != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entry.Value, EntityType.Item);

            for (var i = 0; i < 5; i++)
                if (client.MapClient.Inventory.WeaponDrawer[i] != 0)
                {
                    EntityManager.Instance.DestroyPhysicalEntity(client, client.MapClient.Inventory.WeaponDrawer[i], EntityType.Item);
                    client.MapClient.Inventory.WeaponDrawer[i] = 0;
                }

            // unregister from chat
            CommunicatorManager.Instance.UnregisterPlayer(client);
            CellManager.Instance.RemoveFromWorld(client);
            PlayerManager.Instance.RemovePlayerCharacter(client);
            if (logout)
                if (client.MapClient.Disconected == false)
                    PassClientToCharacterSelection(client);
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
            client.SendPacket(5, new LogoutTimeRemainingPacket());
            client.MapClient.LogoutRequestedLast = Environment.TickCount;
            client.MapClient.LogoutActive = true;
        }
    }
}