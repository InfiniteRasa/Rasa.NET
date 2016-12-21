using System;
using System.Collections.Generic;
using System.Threading;

namespace Rasa.Managers
{
    using Game;
    using Structures;

    public class MapChannelManager
    {
        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        private static readonly Dictionary<int,MapChannel> MapChannelsByContextId = new Dictionary<int, MapChannel>();     // list of maps that need to be loaded
        private static readonly Dictionary<int, MapChannel> MapChannelArray = new Dictionary<int, MapChannel>();           // listo of loaded maps
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

        public void MapLoaded(Client client)
        {
            var mapChannel = MapChannelArray[1220];
            var owner = new MapChannelClient();
            var characterData = new CharacterData // basic constant data to enter world, read from db later
            {
                PosX = 894.9,
                PosY = 347.1,
                PosZ = 307.9,
                MapContextId = 1220,
                Gender = 0
            };
            CreatePlayerCharacter(client, mapChannel, owner, characterData);
        }

        public void CreatePlayerCharacter(Client client, MapChannel mapChannel, MapChannelClient owner, CharacterData characterData)
        {
            var player = new PlayerData
            {
                Actor = new Actor
                {
                    EntityId = EntityManager.GetFreeEntityIdForPlayer(),
                    PosX = characterData.PosX,
                    PosY = characterData.PosY,
                    PosZ = characterData.PosZ,
                    MapContextId = characterData.MapContextId,
                    EntityClassId = characterData.Gender == 0 ? (uint) 692 : 691
                }
            };

            owner.Player = player;
            owner.Client = client;
            owner.MapChannel = mapChannel;
            owner.ClientEntityId = EntityManager.GetFreeEntityIdForPlayer();
            AddNewPlayer(mapChannel, client);
            CellManager.AddToWorld(owner);
            PlayerManager.AssignPlayer(mapChannel, owner, player);

        }

        public void AddNewPlayer(MapChannel mapChannel, Client client)
        {
            var mapChanelClient = new MapChannelClient
            {
                Client = client,
                ClientEntityId = EntityManager.GetFreeEntityIdForPlayer(),
                MapChannel = mapChannel,
                Player = null
            };
            mapChannel.PlayerList[mapChannel.PlayerCount] = mapChanelClient;
            mapChannel.PlayerCount++;

            // register mapChannelClient
            EntityManager.RegisterEntity(mapChanelClient.ClientEntityId, mapChanelClient);
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
                    PlayerList = new MapChannelClient[128]
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
                            sint32 timePassed = 100;
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
                            }*/
                    mapChannel.TimerGeneralTimer += 100;
                }

                Thread.Sleep(5000);
            }
        }

        public void Ping(Client client)
        {
            
        }
    }
}