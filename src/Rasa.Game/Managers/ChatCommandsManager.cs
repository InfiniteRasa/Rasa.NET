using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Database.Tables.Character;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class ChatCommandsManager
    {
        private static ChatCommandsManager _instance;
        private static readonly object InstanceLock = new object();
        private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();
        private static MapChannelClient _mapClient { get; set; }
        public static ChatCommandsManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ChatCommandsManager();
                    }
                }

                return _instance;
            }
        }

        private ChatCommandsManager()
        {
        }

        public void ProcessCommand(MapChannelClient mapClient, string command)
        {
            _mapClient = mapClient;
            if (string.IsNullOrWhiteSpace(command))
                return;

            var parts = command.Split(' ');

            if (Commands.ContainsKey(parts[0]))
            {
                Commands[parts[0]](parts);
                return;
            }

            Logger.WriteLog(LogType.Command, $"Invalid command: {command}");
        }

        public void RegisterCommand(string name, Action<string[]> handler)
        {
            Commands.Add(name, handler);
        }

        public void RemoveCommand(string name)
        {
            if (Commands.ContainsKey(name))
                Commands.Remove(name);
        }

        public void RegisterChatCommands()
        {
            RegisterCommand(".createobj", CreateObjectCommand);
            RegisterCommand(".creature", CreateCreatureCommand);
            RegisterCommand(".creatureloc", SetCreatureLocation);
            RegisterCommand(".giveitem", GiveItemCommand);
            RegisterCommand(".givelogos", GiveLogosCommand);
            RegisterCommand(".gm", EnterGmModCommand);
            RegisterCommand(".help", HelpGmCommand);
            RegisterCommand(".teleport", TeleportCommand);
            RegisterCommand(".setregion", SetRegionCommand);
            RegisterCommand(".speed", SpeedCommand);
            RegisterCommand(".testmove", TestMoveCommand);
            RegisterCommand(".where", WhereCommand);
        }

        #region RegularUser

        #endregion

        #region GM
        private void CreateCreatureCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .creature dbId");
                return;
            }

            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint dbId))
                {
                    var creature = CreatureManager.Instance.CreateCreature(dbId, null);

                    if (creature != null)
                    {
                        CreatureManager.Instance.SetLocation(creature, _mapClient.Player.Actor.Position, _mapClient.Player.Actor.Rotation);
                        CellManager.Instance.AddToWorld(_mapClient.MapChannel, creature);
                        CommunicatorManager.Instance.SystemMessage(_mapClient, $"Created new creature with EntityId {creature.Actor.EntityId}");
                    }
                    else
                        CommunicatorManager.Instance.SystemMessage(_mapClient, $"Creature with dbId={dbId} isn't in database");
                }
            }

            return;
        }

        private void CreateObjectCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .createobj entityClassId");
                return;
            }
            if (parts.Length == 2)
            {
                var _entityId = EntityManager.Instance.GetEntityId;
                int entityClassId;
                if (int.TryParse(parts[1], out entityClassId))
                {
                    // create object entity
                    _mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(_entityId, entityClassId));
                    // set position
                    _mapClient.Player.Client.SendPacket(_entityId, new WorldLocationDescriptorPacket(_mapClient.Player.Actor.Position, _mapClient.Player.Actor.Rotation));
                    CommunicatorManager.Instance.SystemMessage(_mapClient, "Created object");
                }
            }
            return;
        }

        private void EnterGmModCommand(string[] parts)
        {
            _mapClient.Player.Client.SendPacket(5, new SetIsGMPacket(true));
            CommunicatorManager.Instance.SystemMessage(_mapClient, "GM Mode enabled!");
            return;
        }

        private void GiveItemCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .giveitem itemTemplateId quantity");
                return;
            }
            if (parts.Length == 3)
            {
                int itemTemplateId, quantity;
                if (int.TryParse(parts[1], out itemTemplateId))
                    if (int.TryParse(parts[2], out quantity))
                    {
                        InventoryManager.Instance.AddItemToInventory(_mapClient, itemTemplateId, quantity);
                    }
            }
            return;
        }

        private void GiveLogosCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .givelogos logosId");
                return;
            }
            if (parts.Length == 2)
            {
                int logosId;
                if (int.TryParse(parts[1], out logosId))
                    {
                        PlayerManager.Instance.GiveLogos(_mapClient, logosId);
                    }
            }
            return;
        }

        private void HelpGmCommand(string[] parts)
        {
            CommunicatorManager.Instance.SystemMessage(_mapClient, "GM Commands List:");
            foreach (var command in Commands)
                CommunicatorManager.Instance.SystemMessage(_mapClient, $"{command.Key}");
            return;
        }

        private void TeleportCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .teleport posX posY posZ mapId");
                return;
            }
            if (parts.Length == 5)
            {
                double posX, posY, posZ;
                int mapId;
                if (double.TryParse(parts[1], out posX))
                    if (double.TryParse(parts[2], out posY))
                        if (double.TryParse(parts[3], out posZ))
                            if (int.TryParse(parts[4], out mapId))
                            {
                                // init loading screen
                                _mapClient.Player.Client.SendPacket(5, new PreWonkavatePacket());
                                // Remove player
                                MapChannelManager.Instance.RemovePlayer(_mapClient, false);
                                // send Wonkavate
                                var mapChannel = MapChannelManager.Instance.MapChannelArray[mapId];
                                _mapClient.Player.Client.LoadingMap = mapId;
                                _mapClient.Player.Client.SendPacket(6, new WonkavatePacket
                                {
                                    MapContextId = mapChannel.MapInfo.MapId,
                                    MapInstanceId = 0,                  // ToDo MapInstanceId
                                    MapVersion = mapChannel.MapInfo.MapVersion,
                                    Position = new Position(posX, posY, posZ),
                                    Rotation = 1
                                });
                                // Update Db, this position will be loaded in MapLoadedPacket
                                CharacterTable.UpdateCharacterPos(_mapClient.Player.CharacterId, posX, posY, posZ, 1, mapId);
                                mapChannel.PlayerList.Add(_mapClient );
                            }
            }
            return;
        }

        private void TestMoveCommand(string[] parts)
        {
            //CommunicatorManager.Instance.SystemMessage(_mapClient, "Place hodler");
            var netMovement = new NetCompressedMovement();
            netMovement.EntityId = _mapClient.Player.Actor.EntityId;
            //netMovement.Flag = 0;
            netMovement.PosX24b = (uint)_mapClient.Player.Actor.Position.PosX * 256;
            netMovement.PosY24b = (uint)_mapClient.Player.Actor.Position.PosY * 256;
            netMovement.PosZ24b = (uint)_mapClient.Player.Actor.Position.PosZ * 256;
            _mapClient.Player.Client.SendMovement(_mapClient.Player.Actor.EntityId, netMovement);
            return;
        }

        private void SetCreatureLocation(string[] parts)
        {
            /*if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .creatureloc entityClassId, posX, posY, posZ");
                return;
            }
            if (parts.Length == 5)
            {
                double posX, posY, posZ;
                int entityClassId;
                if (int.TryParse(parts[1], out entityClassId))
                    if (double.TryParse(parts[2], out posX))
                        if (double.TryParse(parts[3], out posY))
                            if (double.TryParse(parts[4], out posZ))
                            {
                                var creatureType = new CreatureType();
                                var position = new Position { PosX = posX, PosY = posY, PosZ = posZ };
                                
                                creatureType.NameId = 0;
                                creatureType.Name = "test Npc";

                                var creature = CreatureManager.Instance.CreateCreature(creatureType, entityClassId, _mapClient.Player.AppearanceData, null);
                                CreatureManager.Instance.SetLocation(creature, position, _mapClient.Player.Actor.Rotation);
                                CellManager.Instance.AddToWorld(_mapClient.MapChannel, creature);
                                CommunicatorManager.Instance.SystemMessage(_mapClient, $"Created new creature with EntityId {creature.Actor.EntityId}");
                            }
            }
            return;*/
        }

        private void SetRegionCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .setregion regionId");
                return;
            }
            if (parts.Length == 2)
            {
                int regionId;
                if (int.TryParse(parts[1], out regionId))
                    _mapClient.Player.Client.SendPacket(_mapClient.Player.Actor.EntityId, new UpdateRegionsPacket { RegionIdList = regionId });
            }
            return;
        }

        private void SpeedCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_mapClient, "usage: .speed value");
                return;
            }
            if (parts.Length == 2)
            {
                double speed;
                if (double.TryParse(parts[1], out speed))
                    // ToDO send on cell domain
                    _mapClient.Player.Client.SendPacket(_mapClient.Player.Actor.EntityId, new MovementModChangePacket(speed) );
            }
            return;
        }

        private void WhereCommand(string[] parts)
        {
            CommunicatorManager.Instance.SystemMessage(_mapClient, $"PosX = {_mapClient.Player.Actor.Position.PosX}\nPosY = "
                +$"{_mapClient.Player.Actor.Position.PosY}\nPosZ = {_mapClient.Player.Actor.Position.PosZ}\nMapId = {_mapClient.Player.Actor.MapContextId}");
            return;
        }
        
        #endregion
    }
}
