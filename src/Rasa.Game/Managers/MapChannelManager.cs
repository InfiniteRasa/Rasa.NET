using System;
using System.Collections.Generic;
using System.Threading;
using Rasa.Packets.MapChannel.Server;

namespace Rasa.Managers
{
    using Database.Tables.Character;
    using Game;
    using Structures;

    public class MapChannelManager
    {
        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        private static readonly Dictionary<int,MapChannel> MapChannelsByContextId = new Dictionary<int, MapChannel>();     // list of maps that need to be loaded
        private static readonly Dictionary<int, MapChannel> MapChannelArray = new Dictionary<int, MapChannel>();           // list of loaded maps
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

        public void AddNewPlayer(MapChannel mapChannel, MapChannelClient mapClient)
        {
            mapChannel.PlayerList.Add(mapClient);
            mapChannel.PlayerCount++;

            // register mapChannelClient
            EntityManager.RegisterEntity(mapClient.ClientEntityId, mapClient);
        }

        public void CreatePlayerCharacter(MapChannel mapChannel, MapChannelClient mapClient)
        {
            mapClient.MapChannel = mapChannel;
            mapClient.ClientEntityId = EntityManager.GetFreeEntityIdForPlayer();
            AddNewPlayer(mapChannel, mapClient);
            CellManager.AddToWorld(mapClient);
            PlayerManager.AssignPlayer(mapChannel, mapClient);

        }

        public void CharacterLogout(Client client)
        {            
            if (client.MapClient.LogoutActive == false)
                return;
            client.MapClient.RemoveFromMap = true;
        }

        public static void MapChannelInit()
        {
            MapChannelsByContextId.Add(1220, new MapChannel { MapInfo = new MapInfo { BaseRegionId = 0, MapId = 1220, MapName = "adv_foreas_concordia_wilderness", MapVersion = 1556 } });
            MapChannelsByContextId.Add(1148, new MapChannel { MapInfo = new MapInfo { BaseRegionId = 10, MapId = 1148, MapName = "adv_foreas_concordia_divide", MapVersion = 1584 } });
            
            foreach (var t in MapChannelsByContextId)
            {
                var id = t.Key;
                // load all maps
                var newMapChannel = new MapChannel
                {
                    MapInfo = MapChannelsByContextId[id].MapInfo,
                    SocketToClient = new Dictionary<int, MapChannelClient>(),
                    TimerClientEffectUpdate = Environment.TickCount,
                    TimerMissileUpdate = Environment.TickCount,
                    TimerDynObjUpdate = Environment.TickCount,
                    TimerGeneralTimer = Environment.TickCount,
                    TimerController = Environment.TickCount,
                    TimerPlayerUpdate = Environment.TickCount,
                    PlayerCount = 0,
                    PlayerLimit = 128,
                    PlayerList = new List<MapChannelClient>()
                };
                // register mapChannel
                MapChannelArray.Add(id, newMapChannel);
            }
            var test = new Thread(MapChannelWorker);
            test.Start();
        }

        public static void MapChannelWorker()
        {
            foreach (var t in MapChannelArray)
            {
                var mapChannel = t.Value;
                //navmesh_initForMapChannel(mapChannel);
                //dynamicObject_init(mapChannel);
                //mission_initForChannel(mapChannel);
                //missile_initForMapchannel(mapChannel);
                //spawnPool_initForMapChannel(mapChannel);
                //controller_initForMapChannel(mapChannel);
                //teleporter_initForMapChannel(mapChannel); //---load teleporters
                //logos_initForMapChannel(mapChannel); // logos world objects
                Console.WriteLine("Map: {0} loaded", mapChannel.MapInfo.MapName);
            }

            Console.WriteLine("\nMapChannels Started...");

            while (true)
            {
                foreach (var t in MapChannelArray)
                {
                    var mapChannel = t.Value;

                    if (mapChannel.PlayerCount <= 0)
                        continue;
                    // do other work
                    CellManager.DoWork(mapChannel);
                    // check timers
                    var currentTime = Environment.TickCount;
                    if (currentTime - mapChannel.TimerClientEffectUpdate >= 500)
                    {
                        //gameEffect_checkForPlayers(mapChannel->playerList, mapChannel->playerCount, 500);
                        mapChannel.TimerClientEffectUpdate += 500;
                    }
                    if (currentTime - mapChannel.TimerMissileUpdate >= 100)
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
                    }
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

                    for (var i = 0; i < mapChannel.PlayerList.Count; i++)
                    {
                        var player = mapChannel.PlayerList[i];
                        if (player != null)
                        {
                            if (player.RemoveFromMap == true)
                            {

                                CommunicatorManager.PlayerExitMap(player.Client);
                                RemovePlayer(player.Client);
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        public void MapLoaded(Client client)
        {
            var mapClient = new MapChannelClient();
            var data = CharacterTable.GetCharacterData(client.Entry.Id, client.LastCharPlayed);
            var equipment = CharacterEquipmentTable.GetEquipment(data.Id);
            var player = new PlayerData
            {
                Actor = new Actor
                {
                    EntityId = EntityManager.GetFreeEntityIdForPlayer(),
                    EntityClassId = data.Gender == 0 ? (uint)692 : 691,
                    Name = data.Name,
                    FamilyName = data.FamilyName,
                    PosX = data.PosX,
                    PosY = data.PosY,
                    PosZ = data.PosZ,
                    Rotation = data.Rotation,
                    MapContextId = data.MapContextId,
                    IsRunning = true,
                    InCombatMode = false,
                    Stats = new ActorStats
                    {
                        Level = data.Level
                    },
                },
                AppearanceData = new Dictionary<int, AppearanceData>
                    {
                        { 1, new AppearanceData{ SlotId = 1, ClassId = equipment.Helmet, Color = new Color(equipment.HelmetHue) } },
                        { 2, new AppearanceData{ SlotId = 2, ClassId = equipment.Shoes, Color = new Color(equipment.ShoesHue) } },
                        { 3, new AppearanceData{ SlotId = 3, ClassId = equipment.Gloves, Color = new Color(equipment.GlovesHue) } },
                        { 4, new AppearanceData{ SlotId = 4, ClassId = equipment.Slot4,  Color = new Color(equipment.Slot4Hue) } },
                        { 5, new AppearanceData{ SlotId = 5, ClassId = equipment.Slot5, Color = new Color(equipment.Slot5Hue) } },
                        { 6, new AppearanceData{ SlotId = 6, ClassId = equipment.Slot6, Color = new Color(equipment.Slot6Hue) } },
                        { 7, new AppearanceData{ SlotId = 7, ClassId = equipment.Slot7, Color = new Color(equipment.Slot7Hue) } },
                        { 8, new AppearanceData{ SlotId = 8, ClassId = equipment.Slot8, Color = new Color(equipment.Slot8Hue) } },
                        { 9, new AppearanceData{ SlotId = 9, ClassId = equipment.Slot9, Color = new Color(equipment.Slot9Hue) } },
                        { 10, new AppearanceData{ SlotId = 10, ClassId = equipment.Slot10, Color = new Color(equipment.Slot10Hue) } },
                        { 11, new AppearanceData{ SlotId = 11, ClassId = equipment.Slot11, Color = new Color(equipment.Slot11Hue) } },
                        { 12, new AppearanceData{ SlotId = 12, ClassId = equipment.Slot12, Color = new Color(equipment.Slot12Hue) } },
                        { 13, new AppearanceData{ SlotId = 13, ClassId = equipment.Weapon, Color = new Color(equipment.WeaponHue) } },
                        { 14, new AppearanceData{ SlotId = 14, ClassId = equipment.Hair, Color = new Color(equipment.HairHue) } },
                        { 15, new AppearanceData{ SlotId = 15, ClassId = equipment.Torso, Color = new Color(equipment.TorsoHue) } },
                        { 16, new AppearanceData{ SlotId = 16, ClassId = equipment.Legs, Color = new Color(equipment.LegsHue) } },
                        { 17, new AppearanceData{ SlotId = 17, ClassId = equipment.Face, Color = new Color(equipment.FaceHue) } },
                        { 18, new AppearanceData{ SlotId = 18, ClassId = equipment.Wing, Color = new Color(equipment.Wing) } },
                        { 19, new AppearanceData{ SlotId = 19, ClassId = equipment.EyeWeare, Color = new Color(equipment.EyeWeareHue) } },
                        { 20, new AppearanceData{ SlotId = 20, ClassId = equipment.Beard, Color = new Color(equipment.BeardHue) } },
                        { 21, new AppearanceData{ SlotId = 21, ClassId = equipment.Mask, Color = new Color(equipment.MaskHue) } }
                    },
                CharacterId = data.Id,
                AccountId = data.AccountId,
                SlotId = data.SlotId,
                Gender = data.Gender,
                Scale = data.Scale,
                RaceId = data.RaceId,
                ClassId = data.ClassId,
                Experience = data.Experience,
                Body = data.Body,
                Mind = data.Mind,
                Spirit = data.Spirit,
                CloneCredits = data.CloneCredits,
                NumLogins = data.NumLogins + 1,
                TotalTimePlayed = data.TotalTimePlayed, // 21
                TimeSinceLastPlayed = data.TimeSinceLastPlayed,
                ClanId = data.ClanId,
                ClanName = data.ClanName,
                Credits = data.Credits,            // ToDo
                Prestige = data.Prestige,
                Skills = new[] { 0 },
                AbilityDrawer = new[] { 0 },
                AbilityLvDrawer = new[] { 0 },
                CurrentAbilityDrawer = 0,
                MissionStateCount = 0,
                // MissionStateData = new CharacterMissionData(),
                LoginTime = 0,
                Logos = new List<byte>(),
            };
            var mapChannel = MapChannelArray[data.MapContextId];
            mapClient.Player = player;
            mapClient.Player.Client = client;
            mapClient.Client = client;
            client.MapClient = mapClient;
            CreatePlayerCharacter(mapChannel, mapClient);
            CommunicatorManager.RegisterPlayer(mapClient);
            CommunicatorManager.PlayerEnterMap(mapClient);
            InventoryManager.InitForClient(mapClient);
        }

        public static void PassClientToCharacterSelection(Client client)
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

        public void Ping(Client client)
        {
            // ToDo
        }

        public void RadialChat(Client client, string textMsg)
        {
            CommunicatorManager.Recv_RadialChat(client, textMsg);
        }

        public static void RemovePlayer(Client client)
        {
            var mapClient = client.MapClient;
            // unregister mapChannelClient
            EntityManager.UnregisterEntity(mapClient.ClientEntityId);
            CommunicatorManager.UnregisterPlayer(mapClient);            
            CellManager.RemoveFromWorld(client);
            PlayerManager.RemovePlayerCharacter(mapClient.MapChannel, mapClient);
            if (mapClient.Disconected == false)
                PassClientToCharacterSelection(client);
            // remove from list
            for (var i = 0; i < mapClient.MapChannel.PlayerCount; i++)
            {
                if (mapClient == mapClient.MapChannel.PlayerList[i])
                {
                    mapClient.MapChannel.PlayerList.RemoveAt(i);
                    mapClient.MapChannel.PlayerCount--;
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
        
        //
    }
}