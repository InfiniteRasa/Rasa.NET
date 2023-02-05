using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Rasa.Packets.Communicator.Client;
    using Structures;

    public class ChatCommandsManager
    {
        private static ChatCommandsManager _instance;
        private static readonly object InstanceLock = new object();
        private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();
        private static Client _client { get; set; }
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

        public void ProcessCommand(Client client, string command)
        {
            _client = client;
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
            RegisterCommand(".addtitle", AddTitleCommand);
            RegisterCommand(".actorstate", ActorStateCommand);
            RegisterCommand(".bark", BarkCommand);
            RegisterCommand(".comehere", ComeHereCommand);
            RegisterCommand(".createobj", CreateObjectCommand);
            RegisterCommand(".createobjonloc", CreateObjectOnLocationCommand);
            RegisterCommand(".creature", CreateCreatureCommand);
            RegisterCommand(".creatureappearance", SetCreatureAppearanceCommand);
            RegisterCommand(".creatureloc", SetCreatureLocation);
            RegisterCommand(".deleteobj", DeleteObjectCommand);
            RegisterCommand(".getdistance", GetDistanceCommand);
            RegisterCommand(".giveitem", GiveItemCommand);
            RegisterCommand(".givelogos", GiveLogosCommand);
            RegisterCommand(".givexp", GiveXpCommand);
            RegisterCommand(".gm", EnterGmModCommand);
            RegisterCommand(".forcestate", ForceStateCommand);
            RegisterCommand(".help", HelpGmCommand);
            RegisterCommand(".neer", NeerCommand);
            RegisterCommand(".npcinfo", NpcInfoCommand);
            RegisterCommand(".reloadcreatures", ReloadCreaturesCommand);
            RegisterCommand(".removeobj", RemoveObjectCommand);
            RegisterCommand(".rqs", RqsWindowCommand);
            RegisterCommand(".tele", TeleCommand);
            RegisterCommand(".teleport", TeleportCommand);
            RegisterCommand(".teleup", TeleUpCommand);
            RegisterCommand(".setkillstreak", SetKillStreakCommand);
            RegisterCommand(".setregion", SetRegionCommand);
            RegisterCommand(".speed", SpeedCommand);
            RegisterCommand(".where", WhereCommand);
        }

        #region RegularUser

        #endregion

        #region GM

        private void AddTitleCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .addtitle titleId");
                return;
            }

            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out var titleId))
                {
                    _client.CallMethod(_client.Player.EntityId, new TitleAddedPacket(titleId));
                }
            }
        }

        private void ActorStateCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .actorstate stateId speed");
                return;
            }

            if (parts.Length == 3)
            {
                if (Enum.TryParse(parts[1], out CharacterState stateId))
                    if (double.TryParse(parts[2], out var speed))
                    {
                        _client.CallMethod(_client.Player.EntityId, new ActorInfoPacket(stateId, _client.Player.TrackingTargetEntityId, _client.Movement.ViewDirection.X, speed, _client.Player.InCombatMode));
                    }
            }
        }

        private void BarkCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .bark creatureEntityId barkId");
                return;
            }

            if (parts.Length == 3)
                if (ulong.TryParse(parts[1], out var creatureEntityId))
                    if (uint.TryParse(parts[2], out var barkId))
                        _client.CallMethod(creatureEntityId, new BarkPackage(barkId));
        }

        private void ComeHereCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .comehere creatureEntityId");
                return;
            }

            if (parts.Length == 2)
                if (ulong.TryParse(parts[1], out var entityId))
                {
                    var test = new Movement(_client.Movement.Position, 6.5f, 0, _client.Movement.ViewDirection);

                    _client.MoveObject(entityId, test);
                }
        }

        private void CreateCreatureCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .creature dbId");
                return;
            }

            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint dbId))
                {
                    var creature = CreatureManager.Instance.CreateCreature(dbId, null);

                    if (creature != null)
                    {
                        CreatureManager.Instance.SetLocation(creature, _client.Movement.Position, _client.Movement.ViewDirection.X, _client.Player.MapContextId);
                        CellManager.Instance.AddToWorld(_client.Player.MapChannel, creature);
                        CommunicatorManager.Instance.SystemMessage(_client, $"Created new creature with EntityId {creature.EntityId}");
                    }
                    else
                        CommunicatorManager.Instance.SystemMessage(_client, $"Creature with dbId={dbId} isn't in database");
                }
            }

            return;
        }

        private void CreateObjectCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .createobj entityClassId");
                return;
            }
            if (parts.Length == 2)
            {
                if (Enum.TryParse(parts[1], out EntityClasses entityClassId))
                {
                    var newObject = new DynamicObject
                    {
                        Position = _client.Movement.Position,
                        Rotation = _client.Movement.ViewDirection.X,
                        MapContextId = _client.Player.MapContextId,
                        EntityClassId = entityClassId
                    };

                    CellManager.Instance.AddToWorld(_client.Player.MapChannel, newObject);
                    CommunicatorManager.Instance.SystemMessage(_client, $"Created object EntityId = {newObject.EntityId}");
                }
            }
            return;
        }

        private void CreateObjectOnLocationCommand(string[] parts)
        {
            if (parts.Length != 6)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .createobjonloc entityClassId posX posY posZ orientation");
                return;
            }

            if (Enum.TryParse(parts[1], out EntityClasses entityClassId))
                if (float.TryParse(parts[2], out var posX))
                    if (float.TryParse(parts[3], out var posY))
                        if (float.TryParse(parts[4], out var posZ))
                            if (float.TryParse(parts[5], out var orientation))
                            {
                                var newObject = new DynamicObject
                                {
                                    Position = new Vector3(posX, posY, posZ),
                                    Rotation = orientation,
                                    MapContextId = _client.Player.MapContextId,
                                    EntityClassId = entityClassId
                                };

                                CellManager.Instance.AddToWorld(_client.Player.MapChannel, newObject);
                                CommunicatorManager.Instance.SystemMessage(_client, $"Created object EntityId = {newObject.EntityId}");
                            }
            return;
        }

        private void DeleteObjectCommand(string[] parts)
        {
            if (parts.Length != 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .deleteobj entityId");
                return;
            }

            if (ulong.TryParse(parts[1], out ulong entityId))
            {
                _client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
            }

            return;
        }
        private void EnterGmModCommand(string[] parts)
        {
            _client.CallMethod(SysEntity.ClientMethodId, new SetIsGMPacket(true));
            CommunicatorManager.Instance.SystemMessage(_client, "GM Mode enabled!");
            return;
        }

        private void ForceStateCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .forcestate entityId stateId");
                return;
            }
            // if only item template, give max stack size
            if (parts.Length == 3)
                if (ulong.TryParse(parts[1], out var entityId))
                    if (Enum.TryParse(parts[2], out UseObjectState state))
                        _client.CallMethod(entityId, new ForceStatePacket(state, 100));

            return;
        }

        private void GetDistanceCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                var msg = "Distance between you and target ";

                var entityId = _client.Player.TargetEntityId;

                if (entityId == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "Please select target to use .getdistance command");
                    return;
                }

                var entityType = EntityManager.Instance.GetEntityType(entityId);

                if (entityType == EntityType.Creature)
                    msg += $"\nEntityId = {entityId} is {Vector3.Distance(_client.Movement.Position, EntityManager.Instance.GetCreature(entityId).SpawnPool.Position)}\n";

                if (entityType == EntityType.Character)
                    msg += $"\nEntityId = {entityId} is {Vector3.Distance(_client.Movement.Position, EntityManager.Instance.GetActor(entityId).Position)}\n";

                if (entityType == EntityType.Object)
                    msg = $"EntityId = {entityId} is object, ToDo\n";

                CommunicatorManager.Instance.SystemMessage(_client, msg);

                return;
            }
        }

        private void GiveItemCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .giveitem itemTemplateId quantity");
                return;
            }
            // if only item template, give max stack size
            if (parts.Length == 2)
                if (uint.TryParse(parts[1], out uint itemTemplateId))
                {
                    var classInfo = EntityClassManager.Instance.GetClassInfo(ItemManager.Instance.ItemTemplateItemClass[itemTemplateId]);
                    var item = ItemManager.Instance.CreateFromTemplateId(itemTemplateId, classInfo.ItemClassInfo.StackSize, _client.Player.FamilyName);
                    item.Crafter = _client.Player.FamilyName;
                    InventoryManager.Instance.AddItemToInventory(_client, item);
                }
            if (parts.Length == 3)
                if (uint.TryParse(parts[1], out uint itemTemplateId))
                    if (uint.TryParse(parts[2], out uint quantity))
                    {
                        var item = ItemManager.Instance.CreateFromTemplateId(itemTemplateId, quantity, _client.Player.FamilyName);
                        item.Crafter = _client.Player.FamilyName;
                        InventoryManager.Instance.AddItemToInventory(_client, item);
                    }

            return;
        }

        private void GiveLogosCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .givelogos logosId");
                return;
            }
            if (parts.Length == 2)
                if (uint.TryParse(parts[1], out uint logosId))
                    CharacterManager.Instance.UpdateCharacter(_client, CharacterUpdate.Logos, logosId);

            return;
        }

        private void GiveXpCommand(string[] parts)
        {
            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint xp))
                    ManifestationManager.Instance.GainExperience(_client, xp);
            }
            else
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .givexp ammount");

            return;
        }

        private void HelpGmCommand(string[] parts)
        {
            CommunicatorManager.Instance.SystemMessage(_client, "GM Commands List:");
            foreach (var command in Commands)
                CommunicatorManager.Instance.SystemMessage(_client, $"{command.Key}");

            return;
        }

        private void NeerCommand(string[] parts)
        {
            var listObj = new List<DynamicObject>();
            var listCreatures = new List<Creature>();

            foreach (var cellSeed in _client.Player.Cells)
            {
                var objects = _client.Player.MapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList;

                if (objects.Count > 0)
                {
                    foreach (var obj in objects)
                        Console.WriteLine($"object: entityId=> {obj.EntityId}, entityClass=> {obj.EntityClassId}, type=> {obj.DynamicObjectType}, position => {obj.Position}.");
                }
            }
            foreach (var cellSeed in _client.Player.Cells)
            {
                var creatures = _client.Player.MapChannel.MapCellInfo.Cells[cellSeed].CreatureList;
                if (creatures.Count > 0)
                    foreach (var creature in creatures)
                        Console.WriteLine($"creature: entityId=> {creature.EntityId}, entityClass=> {creature.EntityClass}, dbId=> {creature.DbId}, position => {creature.HomePos.Position}.");

            }

            Console.WriteLine();
            return;
        }

        private void NpcInfoCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                var msg = "target = (\n";

                var entityId = _client.Player.TargetEntityId;

                if (entityId == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "Please select target to use .npcinfo command");
                    return;
                }

                var entityType = EntityManager.Instance.GetEntityType(entityId);

                if (entityId != 0)
                    msg += $"EntityId = {entityId}\nEntityType = {entityType}\n";

                if (entityType == EntityType.Creature)
                {
                    var creature = EntityManager.Instance.GetCreature(entityId);

                    msg += $"CreatureDbId = {creature.DbId}\n";

                    if (creature.SpawnPool != null)
                        msg += $"SpawnPoolDbId = {creature.SpawnPool.DbId}\n";

                    msg += $"PosX = {creature.Position.X}\n";
                    msg += $"PosY = {creature.Position.Y}\n";
                    msg += $"PosZ = {creature.Position.Z}\n";
                }

                msg += ")\n";

                Logger.WriteLog(LogType.Debug, msg);
                CommunicatorManager.Instance.SystemMessage(_client, msg);

                return;
            }
        }

        private void ReloadCreaturesCommand(string[] obj)
        {
            CreatureManager.Instance.LoadedCreatures.Clear();
            CreatureManager.Instance.CreatureInit();
            CommunicatorManager.Instance.SystemMessage(_client, "Creatures Reloaded.");
        }

        private void RemoveObjectCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .removeobj entityId");
                return;
            }
            if (parts.Length == 2)
            {
                if (ulong.TryParse(parts[1], out var entityId))
                {
                    CellManager.Instance.RemoveFromWorld(_client.Player.MapChannel, entityId);
                    CommunicatorManager.Instance.SystemMessage(_client, $"Removed object EntityId = {entityId}");
                }
            }
            return;
        }

        private void RqsWindowCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                _client.CallMethod(SysEntity.ClientMethodId, new DevRQSWindowPacket());
                return;
            }
        }

        private void SetKillStreakCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .setkillstreak streakCount");
                return;
            }
            if (parts.Length == 2)
                if (int.TryParse(parts[1], out int count))
                    _client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(count));

            return;
        }

        private void TeleCommand(string[] parts)
        {
            if (parts.Length != 4)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .tele posX posY posZ");
                return;
            }

            if (float.TryParse(parts[1], out float posX))
                if (float.TryParse(parts[2], out float posY))
                    if (float.TryParse(parts[3], out float posZ))
                        _client.MoveObject(_client.Player.EntityId, new Movement(new Vector3(posX, posY, posZ), new Vector2(0f, 0f)));
        }

        private void TeleportCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .teleport posX posY posZ mapId");
                return;
            }
            if (parts.Length == 5)
            {
                if (float.TryParse(parts[1], out float posX))
                    if (float.TryParse(parts[2], out float posY))
                        if (float.TryParse(parts[3], out float posZ))
                            if (uint.TryParse(parts[4], out uint mapId))
                            {
                                // init loading screen
                                _client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
                                _client.State = ClientState.Loading;
                                // Remove player
                                MapChannelManager.Instance.RemovePlayer(_client, false);
                                // send Wonkavate
                                var mapChannel = MapChannelManager.Instance.MapChannelArray[mapId];
                                _client.LoadingMap = mapId;

                                var packet = new WonkavatePacket(
                                    mapChannel.MapInfo.MapContextId,
                                    0,                  // ToDo MapInstanceId
                                    mapChannel.MapInfo.MapVersion,
                                    new Vector3(posX, posY, posZ),
                                    _client.Movement.ViewDirection.X
                                    );

                                _client.CallMethod(SysEntity.CurrentInputStateId, packet);
                                // AddOrUpdate Db, this position will be loaded in MapLoadedPacket
                                CharacterManager.Instance.UpdateCharacter(_client, CharacterUpdate.Position, packet);
                                mapChannel.ClientList.Add(_client);
                            }

            }

            return;
        }

        private void TeleUpCommand(string[] parts)
        {
            if (parts.Length != 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .teleup posY");
                return;
            }

            if (float.TryParse(parts[1], out float posY))
                _client.MoveObject(_client.Player.EntityId, new Movement(new Vector3(_client.Movement.Position.X, posY, _client.Movement.Position.Z), _client.Movement.ViewDirection));
        }

        private void SetCreatureAppearanceCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .creatureappearance creatureEntityId slotId classId color");
                return;
            }
            if (parts.Length == 5)
            {
                if (ulong.TryParse(parts[1], out var entityId))
                    if (uint.TryParse(parts[2], out uint slotId))
                        if (uint.TryParse(parts[3], out uint classId))
                            if (uint.TryParse(parts[4], out uint color))
                            {
                                // get creature from entityId
                                var creature = EntityManager.Instance.GetCreature(entityId);
                                var appearanceData = new AppearanceData(new Structures.Char.CharacterAppearanceEntry(slotId, classId,color));
                                CreatureManager.Instance.CreateOrUpdateAppearance(creature, appearanceData);

                                Logger.WriteLog(LogType.Debug, "Creature Look updated");
                            }
            }

            return;
        }

        private void SetCreatureLocation(string[] parts)
        {
            /*if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client.MapClient, "usage: .creatureloc entityClassId, posX, posY, posZ");
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

                                var creature = CreatureManager.Instance.CreateCreature(creatureType, entityClassId, _client.MapClient.Player.AppearanceData, null);
                                CreatureManager.Instance.SetLocation(creature, position, _client.MapClient.Player.Actor.Rotation);
                                CellManager.Instance.AddToWorld(_client.MapClient.MapChannel, creature);
                                CommunicatorManager.Instance.SystemMessage(_client.MapClient, $"Created new creature with EntityId {creature.Actor.EntityId}");
                            }
            }
            return;*/
        }

        private void SetRegionCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .setregion regionId");
                return;
            }
            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint regionId))
                    _client.CallMethod(_client.Player.EntityId, new UpdateRegionsPacket { RegionIdList = regionId });
            }
            return;
        }

        private void SpeedCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .speed value");
                return;
            }
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[1], out double speed))
                    // ToDO send on cell domain
                    _client.CallMethod(_client.Player.EntityId, new MovementModChangePacket(speed));
            }
            return;
        }

        private void WhereCommand(string[] parts)
        {
            CommunicatorManager.Instance.SystemMessage(_client, $"PosX = {_client.Movement.Position.X}\nPosY = "
                + $"{_client.Movement.Position.Y}\nPosZ = {_client.Movement.Position.Z}\nOrientation = {_client.Movement.ViewDirection.X}"
                + $"\nMapId = {_client.Player.MapContextId}");
            return;
        }

        #endregion

        internal void PrivilegedCommand(Client client, PrivilegedCommandPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: PrivilegedCommand");
        }
    }
}
