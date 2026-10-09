using System;
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
    using Structures.World;

    /// <summary>
    /// The client's ambient figures: people who are part of the scenery. A soldier firing down a
    /// range, two men talking on chairs, a medic at a monitor, a drill sergeant pacing. Each is
    /// an entity class of its own (UsableStatelessNPC*, 43 of them) with one model - clothes,
    /// face and what is in the hands all part of it - and its own animations, which the client
    /// plays from the class's data: usabledata.animation[(class, USE_SS_STATE_0, USE_STATE_NULL)]
    /// names animation spec 53 as a recurring one-shot, a random one of the model's ambient
    /// animations over and over (animationFamilyObject, family 277). Three have an FX package
    /// on the state as well: the firing range's shots, the drill sergeant, the trainees.
    ///
    /// They are usables with the STATELESSSWITCH augmentation, whose one state is
    /// USE_SS_STATE_0 (44). The animation starts when the entity is put in that state, which is
    /// UsableInfo's doing (Usable.Recv_UsableInfo, _SetState, UsableState.__call__): so one is
    /// created with a position, an orientation and UsableInfo(not enabled, state 44) - nothing
    /// to target and nothing to use. It has no name, no health and no faction; it is not a
    /// creature and nothing in the world acts on it.
    ///
    /// No .map places one, so they are rows of ambient_npc, put on their map when it comes up
    /// and on each private copy of it as that is made. A GM can stand one up to look at it
    /// (.ambient), which is not saved.
    ///
    /// The model's origin is at the figure's feet and it faces the way a player with the same
    /// rotation would. A seated figure has no chair: it goes on one the map has. The firing
    /// range's model (29425) is the soldier and his target, 14.25 m in front of him.
    /// </summary>
    public static class AmbientNpcs
    {
        /// <summary>What the client's ambient figures are named: UsableStatelessNPCMaleFiringRange, ...</summary>
        public const string ClassNamePrefix = "UsableStatelessNPC";

        public sealed class Placement
        {
            public uint Id { get; init; }
            public uint MapContextId { get; init; }
            public EntityClasses ClassId { get; init; }
            public Vector3 Position { get; init; }
            public double Rotation { get; init; }
            public string Comment { get; init; }
        }

        private static List<Placement> _placements = new List<Placement>();

        /// <summary>The rows that were kept: a class the client has, of the kind that can be shown.</summary>
        public static IReadOnlyList<Placement> All => _placements;

        /// <summary>Loads ambient_npc and puts each figure on its map. Runs after MapChannelInit.</summary>
        public static void Init(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();

            var rows = unitOfWork.AmbientNpcs.Get();
            var kept = Load(rows);
            var placed = 0;

            foreach (var mapContextId in _placements.Select(p => p.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += Place(mapChannel);
                else
                    Logger.WriteLog(LogType.Initialize, $"  ambient NPCs: map {mapContextId} is not loaded");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {kept} of {rows.Count} ambient NPCs, {placed} in the world");
        }

        /// <summary>
        /// Takes the rows as what is placed from here on. One whose class the server's data does
        /// not have, or that is not a figure of this kind, is logged and left out. Returns how
        /// many were kept.
        /// </summary>
        public static int Load(IEnumerable<AmbientNpcEntry> rows)
        {
            var placements = new List<Placement>();

            foreach (var row in rows ?? Enumerable.Empty<AmbientNpcEntry>())
            {
                var classInfo = ClassOf((EntityClasses)row.ClassId);

                if (!IsFigure(classInfo))
                {
                    Logger.WriteLog(LogType.Error, classInfo == null
                        ? $"ambient_npc {row.Id}: entity class {row.ClassId} is not loaded; left out"
                        : $"ambient_npc {row.Id}: entity class {row.ClassId} ({classInfo.ClassName}) is not a stateless switch; left out");
                    continue;
                }

                placements.Add(new Placement
                {
                    Id = row.Id,
                    MapContextId = row.MapContextId,
                    ClassId = (EntityClasses)row.ClassId,
                    Position = row.Position,
                    Rotation = row.Rotation,
                    Comment = row.Comment ?? ""
                });
            }

            _placements = placements;

            return placements.Count;
        }

        /// <summary>
        /// Whether a class can be shown this way: it has the STATELESSSWITCH augmentation, so the
        /// client has USE_SS_STATE_0 for it and takes UsableInfo. Sent to anything else, the
        /// state is one the class does not have, or UsableInfo a method the entity does not have.
        /// </summary>
        public static bool IsFigure(EntityClass classInfo)
        {
            return classInfo?.Augmentations != null && classInfo.Augmentations.Contains(AugmentationType.StatelessSwitch);
        }

        /// <summary>The class the server's data has under an id, or null: asked without GetClassInfo's error line for one it has not.</summary>
        public static EntityClass ClassOf(EntityClasses classId)
        {
            return EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var classInfo) ? classInfo : null;
        }

        /// <summary>The client's ambient people, by class id: the stateless switches named UsableStatelessNPC*.</summary>
        public static List<EntityClass> Figures()
        {
            return EntityClassManager.Instance.LoadedEntityClasses.Values
                .Where(c => IsFigure(c) && c.ClassName != null && c.ClassName.StartsWith(ClassNamePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.ClassId)
                .ToList();
        }

        /// <summary>A figure's name without the prefix every one of them has: MaleFiringRange.</summary>
        public static string ShortName(EntityClass classInfo)
        {
            var name = classInfo?.ClassName ?? "";

            return name.StartsWith(ClassNamePrefix, StringComparison.OrdinalIgnoreCase) ? name.Substring(ClassNamePrefix.Length) : name;
        }

        /// <summary>
        /// Puts this map's figures on one channel of it - the open world's or a private copy -
        /// and tells whoever is in range. One the channel already has is left alone. Returns how
        /// many were added.
        /// </summary>
        public static int Place(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null)
                return 0;

            var placed = 0;

            foreach (var placement in _placements)
            {
                if (placement.MapContextId != mapChannel.MapInfo.MapContextId || ObjectOf(mapChannel, placement) != null)
                    continue;

                CellManager.Instance.AddToWorld(mapChannel, Make(placement.MapContextId, placement.ClassId, placement.Position, placement.Rotation, placement.Comment, placement));
                placed++;
            }

            return placed;
        }

        /// <summary>The object standing for a row on a map channel, or null.</summary>
        public static DynamicObject ObjectOf(MapChannel mapChannel, Placement placement)
        {
            if (placement == null)
                return null;

            return OnChannel(mapChannel).FirstOrDefault(obj => obj.ObjectData is Placement of && of.Id == placement.Id);
        }

        /// <summary>Every ambient figure on a map channel: the rows' and the ones a GM stood up.</summary>
        public static List<DynamicObject> OnChannel(MapChannel mapChannel)
        {
            if (mapChannel?.MapCellInfo == null)
                return new List<DynamicObject>();

            return mapChannel.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.AmbientNpc)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Stands a figure up on a map channel that is no row and is gone when the channel is:
        /// a GM's look at one (.ambient). Null if the class cannot be shown this way.
        /// </summary>
        public static DynamicObject PutDown(MapChannel mapChannel, EntityClasses classId, Vector3 position, double rotation)
        {
            if (mapChannel?.MapInfo == null || !IsFigure(ClassOf(classId)))
                return null;

            var figure = Make(mapChannel.MapInfo.MapContextId, classId, position, rotation, "put down by a GM", null);

            CellManager.Instance.AddToWorld(mapChannel, figure);

            return figure;
        }

        /// <summary>Takes away the figures GMs stood up on a map channel; the rows' stay. Returns how many went.</summary>
        public static int TakeAwayPutDown(MapChannel mapChannel)
        {
            var putDown = OnChannel(mapChannel).Where(obj => obj.ObjectData == null).ToList();

            foreach (var figure in putDown)
                CellManager.Instance.RemoveFromWorld(mapChannel, figure);

            return putDown.Count;
        }

        /// <summary>
        /// What a client is sent to create the figure (DynamicObjectManager.CreateDynamicObjectOnClient):
        /// where it is, which way it faces, and its one state, out of service. The state is what
        /// starts its animation.
        /// </summary>
        public static List<PythonPacket> EntityData(DynamicObject obj)
        {
            return new List<PythonPacket>
            {
                new IsTargetablePacket(false),
                new WorldLocationDescriptorPacket(obj.Position, obj.Rotation),
                new UsableInfoPacket(false, UseObjectState.SsState0, 0, 0, 0)
            };
        }

        private static DynamicObject Make(uint mapContextId, EntityClasses classId, Vector3 position, double rotation, string comment, Placement placement)
        {
            return new DynamicObject
            {
                EntityClassId = classId,
                DynamicObjectType = DynamicObjectType.AmbientNpc,
                ObjectData = placement,
                Position = position,
                Rotation = rotation,
                MapContextId = mapContextId,
                TargetCategory = TargetCategory.Object,
                StateId = UseObjectState.SsState0,
                IsEnabled = false,
                Comment = comment,
                IsInWorld = true
            };
        }
    }
}
