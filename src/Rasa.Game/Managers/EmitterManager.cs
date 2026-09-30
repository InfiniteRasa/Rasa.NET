using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// FX packages played at spots on a map - a smoke column, a fire, a looping ambient sound -
    /// through the client's FXPackageEmitter (entity class 2873, the one class carrying the
    /// FXPACKAGEEMITTER augmentation). The client's side is small: the entity is an invisible
    /// proxy mesh (Emitter.__init__), Recv_EmitterInfo(on, packageId) gives it its package and
    /// turns it on or off, and Recv_TurnOn / Recv_TurnOff start and stop it; a package id the
    /// client's string table does not have raises inside the client, so only ids in
    /// <see cref="FxPackages"/> are sent.
    ///
    /// No .map places one, and nothing in the client's data does either: they were server world
    /// data, gone with the original server. So they are rows in map_emitter, authored in game
    /// with the .emitter commands and loaded onto their maps at start-up. The row holds whether
    /// the emitter plays when the map comes up; <see cref="TurnOn"/> and <see cref="TurnOff"/>
    /// switch one without touching the row, for whatever comes to script them - a mission
    /// step, a trigger, a control point changing hands.
    /// </summary>
    public class EmitterManager
    {
        /// <summary>FXPackageEmitter.</summary>
        public const EntityClasses EmitterClassId = (EntityClasses)2873;

        private static EmitterManager _instance;
        private static readonly object InstanceLock = new object();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Dictionary<uint, MapEmitter> _emitters = new Dictionary<uint, MapEmitter>();

        public static EmitterManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new EmitterManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private EmitterManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public bool TryGet(uint id, out MapEmitter emitter)
        {
            return _emitters.TryGetValue(id, out emitter);
        }

        /// <summary>Loads map_emitter and puts each emitter on its map. Runs after MapChannelInit.</summary>
        public void EmitterInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entries = unitOfWork.MapEmitters.GetMapEmitters();
            var spawned = 0;

            foreach (var entry in entries)
            {
                var emitter = MapEmitter.FromEntry(entry);

                _emitters[emitter.Id] = emitter;

                if (!FxPackages.Names.ContainsKey(emitter.PackageId))
                {
                    Logger.WriteLog(LogType.Initialize, $"  emitter #{emitter.Id} names FX package {emitter.PackageId}, which the client does not have; left out");
                    continue;
                }

                if (Spawn(emitter))
                    spawned++;
                else
                    Logger.WriteLog(LogType.Initialize, $"  emitter #{emitter.Id}: map {emitter.MapContextId} is not loaded");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {entries.Count} FX emitters, {spawned} in the world");
        }

        /// <summary>The emitters on a map, nearest to the position first.</summary>
        public List<MapEmitter> OnMap(uint mapContextId, Vector3 position)
        {
            return _emitters.Values
                .Where(e => e.MapContextId == mapContextId)
                .OrderBy(e => Vector3.Distance(e.Position, position))
                .ThenBy(e => e.Id)
                .ToList();
        }

        #region The entity

        /// <summary>What a client is sent to create the emitter: where it is and what it plays (DynamicObjectManager.CreateDynamicObjectOnClient).</summary>
        public static List<PythonPacket> EntityData(DynamicObject obj, EntityClass classInfo)
        {
            var emitter = obj.ObjectData as MapEmitter;

            return new List<PythonPacket>
            {
                new IsTargetablePacket(classInfo.TargetFlag),
                new WorldLocationDescriptorPacket(obj.Position, obj.Rotation),
                new EmitterInfoPacket(emitter?.IsOn ?? false, emitter?.PackageId ?? 0)
            };
        }

        /// <summary>Puts the emitter's entity on its map, on every client in range. False when the map is not loaded.</summary>
        private static bool Spawn(MapEmitter emitter)
        {
            var mapChannel = MapChannelManager.Instance.FindByContextId(emitter.MapContextId);

            if (mapChannel == null)
                return false;

            emitter.Object = new DynamicObject
            {
                EntityClassId = EmitterClassId,
                DynamicObjectType = DynamicObjectType.Emitter,
                ObjectData = emitter,
                Position = emitter.Position,
                Rotation = emitter.Rotation,
                MapContextId = emitter.MapContextId,
                TargetCategory = TargetCategory.Object,
                Comment = emitter.Comment,
                IsInWorld = true
            };

            CellManager.Instance.AddToWorld(mapChannel, emitter.Object);

            return true;
        }

        /// <summary>Takes the emitter's entity off its map and off every client that had it.</summary>
        private static void Despawn(MapEmitter emitter)
        {
            if (emitter.Object == null)
                return;

            var mapChannel = MapChannelManager.Instance.FindByContextId(emitter.MapContextId);

            if (mapChannel != null)
                CellManager.Instance.RemoveFromWorld(mapChannel, emitter.Object);

            emitter.Object = null;
        }

        private static void Tell(MapEmitter emitter, PythonPacket packet)
        {
            if (emitter.Object != null)
                CellManager.Instance.CellCallMethod(emitter.Object, packet);
        }

        /// <summary>
        /// Plays a package at a spot for as long as the caller keeps it: an emitter that is not a
        /// row and not in the emitter list, on the given map channel (a private instance too).
        /// Taken away with <see cref="RemoveTemporary"/>.
        /// </summary>
        public MapEmitter PlayTemporary(MapChannel mapChannel, uint mapContextId, Vector3 position, double rotation, uint packageId, string comment)
        {
            var emitter = new MapEmitter
            {
                MapContextId = mapContextId,
                Position = position,
                Rotation = rotation,
                PackageId = packageId,
                IsOn = true,
                Comment = comment ?? ""
            };

            emitter.Object = new DynamicObject
            {
                EntityClassId = EmitterClassId,
                DynamicObjectType = DynamicObjectType.Emitter,
                ObjectData = emitter,
                Position = position,
                Rotation = rotation,
                MapContextId = mapContextId,
                TargetCategory = TargetCategory.Object,
                Comment = emitter.Comment,
                IsInWorld = true
            };

            CellManager.Instance.AddToWorld(mapChannel, emitter.Object);

            return emitter;
        }

        /// <summary>Takes a <see cref="PlayTemporary"/> emitter off its map channel and off every client that had it.</summary>
        public void RemoveTemporary(MapChannel mapChannel, MapEmitter emitter)
        {
            if (emitter?.Object == null)
                return;

            CellManager.Instance.RemoveFromWorld(mapChannel, emitter.Object);
            emitter.Object = null;
        }

        #endregion

        #region Switching

        /// <summary>Starts the package playing, for everyone in range. The row is left as it is.</summary>
        public void TurnOn(MapEmitter emitter)
        {
            if (emitter == null || emitter.IsOn)
                return;

            emitter.IsOn = true;
            Tell(emitter, new EmitterTurnOnPacket());
        }

        /// <summary>Stops the package, for everyone in range. The row is left as it is.</summary>
        public void TurnOff(MapEmitter emitter)
        {
            if (emitter == null || !emitter.IsOn)
                return;

            emitter.IsOn = false;
            Tell(emitter, new EmitterTurnOffPacket());
        }

        /// <summary>
        /// Plays another package. Sent as EmitterInfo, which the client applies by stopping the
        /// old package before starting the new one, or leaving it stopped if the emitter is off.
        /// </summary>
        public void SetPackage(MapEmitter emitter, uint packageId)
        {
            emitter.PackageId = packageId;
            Tell(emitter, new EmitterInfoPacket(emitter.IsOn, emitter.PackageId));
        }

        /// <summary>Moves the emitter: its entity is taken away and made again where it now stands.</summary>
        public void MoveTo(MapEmitter emitter, uint mapContextId, Vector3 position, double rotation)
        {
            Despawn(emitter);

            emitter.MapContextId = mapContextId;
            emitter.Position = position;
            emitter.Rotation = rotation;

            Spawn(emitter);
        }

        #endregion

        #region Rows

        /// <summary>Stores a new emitter and puts it in the world. Null when the row could not be written.</summary>
        public MapEmitter Add(MapEmitter emitter)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = emitter.ToEntry();
            entry.Id = 0;

            var id = unitOfWork.MapEmitters.AddMapEmitter(entry);

            if (id == 0)
                return null;

            emitter.Id = id;
            _emitters[id] = emitter;
            Spawn(emitter);

            return emitter;
        }

        /// <summary>Writes the emitter's current fields to its row.</summary>
        public bool Save(MapEmitter emitter)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            return unitOfWork.MapEmitters.UpdateMapEmitter(emitter.ToEntry());
        }

        public bool Delete(MapEmitter emitter)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!unitOfWork.MapEmitters.DeleteMapEmitter(emitter.Id))
                return false;

            Despawn(emitter);
            _emitters.Remove(emitter.Id);

            return true;
        }

        #endregion
    }
}
