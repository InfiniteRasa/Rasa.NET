using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Structures;

    /// <summary>
    /// Same-map teleports the map has no teleporter for: a box in the world that moves whoever
    /// enters it somewhere else on the map, with no loading screen. Checked on every accepted
    /// Move (Client), against the step the player just took, so a fall fast enough to pass
    /// through a thin box between two Moves still counts. Ours: the client has no such thing,
    /// and nothing in its data says where the original ones were. Torden Abyss's are hidden,
    /// with nothing on screen to give them away; Alia Caverns's are an Eloh alcove one walks
    /// up to, and take the player as a teleporter does (<see cref="Passage.WithEffect"/>).
    ///
    /// Torden Abyss, Jump and Believe. Two Eloh obelisks stand on the lip of the Abyssal Shelf
    /// (adv_arieki_torden_abyss, around -348, 523, -535), their glyphs reading "Jump" and
    /// "Believe", and an NPC remarks on exactly that. About a hundred metres away and 340 below,
    /// sealed in the rock under the shelf, is an Eloh chamber holding the Growth logos: a room,
    /// a ramp and a short hall that ends in solid rock, with no door and no way in on foot. The
    /// leap of faith is the way in. A pane about 28 m under the lip, across the stretch of the
    /// drop below the obelisks, catches the jumper before they land anywhere and puts them in
    /// the chamber, facing the logos; the end of the chamber's hall puts them back on the shelf
    /// between the obelisks, facing the drop. A teleport ends the descent (PlaceAt), so the
    /// fall costs nothing.
    ///
    /// Concordia Wilderness, Alia Caverns (Receptive Reception, "acquire the Logos information
    /// from the shrine within"). The tunnels behind the waterfall end in sixteen metres of Eloh
    /// corridor (adv_foreas_concordia_wilderness, 835, 286, 723..739) whose far end the map
    /// leaves open on nothing: no room behind it, no map link, no instance. The shrine is 181 m
    /// further north, sealed: ten identical rooms stacked one above the other at 832, y,
    /// 920..985, 64 m apart from y 352 down to y -224, each a corridor with an open south end,
    /// a room and a Logos dispenser base at z 960. The Enhance logos is in the fourth, floor
    /// y 159.9.
    ///
    /// What closes those two open ends (<see cref="Doorways"/>), as a screenshot of the live
    /// game shows the cave's closed: an Eloh alcove, UsableTwoStateElohTemplesSpawner (22954) -
    /// a wall across the corridor with a pointed niche in the middle of it and a disc on the
    /// floor before the niche, lit in its second state by that state's effect
    /// (arch_eloh_temples_spawner_on.pkg). It is the piece the Eloh Temples map closes its own
    /// corridor ends with, set at the end's own origin and turned as the corridor is; the end
    /// of a straight has the profile of that map's tee arms to a centimetre, and so placed no
    /// line of sight from anywhere a head can be in either corridor passes it (checked against
    /// the client's meshes from 60 head positions each). The Wilderness .map leaves the two
    /// ends open and has no such piece, which is a usable and so the server's to place: it is
    /// made here as the client's map loader makes its own pieces, with the state that lights
    /// it (DynamicObjectType.Scenery).
    ///
    /// The passage is the ground before the niche: the inner half of the disc and the niche
    /// itself, 6 m across. Whoever walks up to it is taken as a teleporter takes them - the
    /// teleport's effect where they stood and where they arrive - into the shrine's corridor,
    /// facing the logos, 7 m out from the shrine's own alcove; and that alcove takes them back
    /// to the cave's corridor, facing the tunnels. The alcove is the client's to collide with:
    /// its wall stops anyone short of where the corridor's floor ends.
    /// </summary>
    public static class SecretPassages
    {
        public sealed class Passage
        {
            public Passage(string name, uint mapContextId, Vector3 min, Vector3 max, Vector3 destination, float rotation, bool withEffect = false)
            {
                Name = name;
                MapContextId = mapContextId;
                Min = Vector3.Min(min, max);
                Max = Vector3.Max(min, max);
                Destination = destination;
                Rotation = rotation;
                WithEffect = withEffect;
            }

            public string Name { get; }
            public uint MapContextId { get; }

            /// <summary>The box that triggers it, world axes, Y up.</summary>
            public Vector3 Min { get; }
            public Vector3 Max { get; }

            public Vector3 Destination { get; }

            /// <summary>The way the player faces on arrival, as Player.Rotation: 0 faces -Z.</summary>
            public float Rotation { get; }

            /// <summary>
            /// Whether it takes the player as a teleporter does, with the teleport's effect at
            /// both ends and the client's answer awaited (DynamicObjectManager.TakePassage),
            /// rather than moving them unseen.
            /// </summary>
            public bool WithEffect { get; }

            public bool Contains(Vector3 p) =>
                p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;

            /// <summary>Whether the step from one point to another touches the box anywhere along it (a slab test).</summary>
            public bool Touches(Vector3 from, Vector3 to)
            {
                var tMin = 0f;
                var tMax = 1f;
                var d = to - from;

                for (var axis = 0; axis < 3; axis++)
                {
                    var o = axis == 0 ? from.X : axis == 1 ? from.Y : from.Z;
                    var v = axis == 0 ? d.X : axis == 1 ? d.Y : d.Z;
                    var lo = axis == 0 ? Min.X : axis == 1 ? Min.Y : Min.Z;
                    var hi = axis == 0 ? Max.X : axis == 1 ? Max.Y : Max.Z;

                    if (Math.Abs(v) < 1e-6f)
                    {
                        if (o < lo || o > hi)
                            return false;

                        continue;
                    }

                    var t1 = (lo - o) / v;
                    var t2 = (hi - o) / v;

                    if (t1 > t2)
                        (t1, t2) = (t2, t1);

                    tMin = Math.Max(tMin, t1);
                    tMax = Math.Min(tMax, t2);

                    if (tMin > tMax)
                        return false;
                }

                return true;
            }
        }

        public const uint TordenAbyss = 2028;

        /// <summary>Under the Jump and Believe obelisks: the pane a jumper falls through.</summary>
        public static readonly Passage JumpAndBelieve = new Passage(
            "Torden Abyss: Jump and Believe, the leap from the Abyssal Shelf into the Growth chamber",
            TordenAbyss,
            new Vector3(-375f, 300f, -575f), new Vector3(-320f, 495f, -539f),
            new Vector3(-315f, 183.2f, -412f), 0f);

        /// <summary>The end of the Growth chamber's hall, where it runs into the rock: back to the obelisks.</summary>
        public static readonly Passage GrowthHallEnd = new Passage(
            "Torden Abyss: the end of the Growth chamber's hall, back up to the Jump and Believe obelisks",
            TordenAbyss,
            new Vector3(-325f, 150f, -344f), new Vector3(-305f, 212f, -330f),
            new Vector3(-347.8f, 523.5f, -531f), 0f);

        public const uint ConcordiaWilderness = 1220;

        /// <summary>UsableTwoStateElohTemplesSpawner: the Eloh alcove that closes a corridor's end.</summary>
        public const EntityClasses ElohAlcove = (EntityClasses)22954;

        /// <summary>How far out from an alcove's wall, into its corridor, its passage begins: the niche's arms reach 2 m, the disc 5.8 m.</summary>
        public const float AlcoveReach = 2.5f;

        /// <summary>How far to either side of the middle of an alcove its passage reaches: the niche is 2.6 m wide, between arms 5.9 m apart.</summary>
        public const float AlcoveHalfWidth = 3f;

        /// <summary>How far behind an alcove's wall its passage reaches: the niche is 1.4 m deep.</summary>
        public const float AlcoveDepth = 2f;

        /// <summary>Before the alcove at the north end of the Alia Caverns corridor (835, 286, 739; floor y 286.0, ceiling 291.5).</summary>
        public static readonly Passage AliaCavernsDoor = new Passage(
            "Concordia Wilderness: Alia Caverns, the alcove at the end of the Eloh corridor into the Enhance shrine",
            ConcordiaWilderness,
            new Vector3(835f - AlcoveHalfWidth, 280f, 739f - AlcoveReach), new Vector3(835f + AlcoveHalfWidth, 296f, 739f + AlcoveDepth),
            new Vector3(832f, 160.1f, 927f), MathF.PI, withEffect: true);

        /// <summary>Before the alcove at the south end of the Enhance shrine's corridor (832, 159.93, 920; floor y 159.9, ceiling 165.4): back into the cave.</summary>
        public static readonly Passage EnhanceShrineExit = new Passage(
            "Concordia Wilderness: the alcove at the end of the Enhance shrine's corridor, back into Alia Caverns",
            ConcordiaWilderness,
            new Vector3(832f - AlcoveHalfWidth, 154f, 920f - AlcoveDepth), new Vector3(832f + AlcoveHalfWidth, 170f, 920f + AlcoveReach),
            new Vector3(835f, 286.3f, 733f), 0f, withEffect: true);

        public static readonly Passage[] All = { JumpAndBelieve, GrowthHallEnd, AliaCavernsDoor, EnhanceShrineExit };

        /// <summary>The piece that closes the open end of a corridor a passage is at, so that it does not open on nothing.</summary>
        public sealed class Doorway
        {
            public Doorway(string name, Passage passage, EntityClasses classId, UseObjectState state, Vector3 position, float yaw, Vector3 seenFrom)
            {
                Name = name;
                Passage = passage;
                ClassId = classId;
                State = state;
                Position = position;
                Yaw = yaw;
                SeenFrom = seenFrom;
            }

            public string Name { get; }

            /// <summary>The passage that is before this piece.</summary>
            public Passage Passage { get; }

            public uint MapContextId => Passage.MapContextId;
            public EntityClasses ClassId { get; }

            /// <summary>The state the piece stands in, which is what puts that state's effect on it.</summary>
            public UseObjectState State { get; }

            /// <summary>The piece's origin: for the alcove, the middle of the corridor's end, on its floor.</summary>
            public Vector3 Position { get; }

            /// <summary>Turn about the vertical, as a DynamicObject's Rotation. At 0 the alcove's niche opens towards -Z.</summary>
            public float Yaw { get; }

            /// <summary>
            /// The place whose map cell the piece is filed in (DynamicObject.CellAnchor). A client
            /// has an object from two cells of 25.6 m around it, 51 to 77 m; filed where it stands,
            /// at the end of the way to it, the piece would come too late for someone looking down
            /// the length of that way, who would see the end open first.
            /// </summary>
            public Vector3 SeenFrom { get; }

            /// <summary>Level, out of the niche and down the corridor.</summary>
            public Vector3 Out => Vector3.Transform(new Vector3(0f, 0f, -1f), Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));
        }

        /// <summary>At the open north end of the Alia Caverns corridor (its straight's origin; the end is at z 739.14).</summary>
        public static readonly Doorway AliaCavernsDoorway = new Doorway(
            "Concordia Wilderness: the alcove at the end of the Alia Caverns corridor",
            AliaCavernsDoor, ElohAlcove, UseObjectState.TsState1,
            new Vector3(835f, 286f, 739f), 0f,
            // The tunnel runs straight at the corridor from z 647: in the cell of z 691..717, the piece is there from z 640 on.
            new Vector3(835f, 286f, 705f));

        /// <summary>At the open south end of the Enhance shrine's corridor (its straight's origin; the end is at z 919.86): the same, turned about.</summary>
        public static readonly Doorway EnhanceShrineDoorway = new Doorway(
            "Concordia Wilderness: the alcove at the end of the Enhance shrine's corridor",
            EnhanceShrineExit, ElohAlcove, UseObjectState.TsState1,
            new Vector3(832f, 159.93f, 920f), MathF.PI,
            // The room ends at z 985: in the cell of z 947..973, the piece is there throughout the shrine.
            new Vector3(832f, 160f, 960f));

        public static readonly Doorway[] Doorways = { AliaCavernsDoorway, EnhanceShrineDoorway };

        /// <summary>Puts the doorways on every loaded map that has any. Runs after MapChannelInit.</summary>
        public static void DoorwayInit()
        {
            var placed = 0;

            foreach (var mapContextId in Doorways.Select(d => d.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += PlaceDoorways(mapChannel);
            }

            Logger.WriteLog(LogType.Initialize, $"Placed {placed} of {Doorways.Length} secret passage doorways");
        }

        /// <summary>
        /// Puts this map's doorways on one channel of it - the open world's or a private copy -
        /// and tells whoever is in range. One the channel already has is left alone. Returns how
        /// many were added.
        /// </summary>
        public static int PlaceDoorways(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null)
                return 0;

            var placed = 0;

            foreach (var doorway in Doorways)
            {
                if (doorway.MapContextId != mapChannel.MapInfo.MapContextId || DoorwayObject(mapChannel, doorway) != null)
                    continue;

                // A class the server's data does not have cannot be sent: the doorway stays open, as before.
                if (EntityClassManager.Instance.GetClassInfo(doorway.ClassId) == null)
                {
                    Logger.WriteLog(LogType.Error, $"Entity class {(uint)doorway.ClassId} is not loaded; left out: {doorway.Name}");
                    continue;
                }

                CellManager.Instance.AddToWorld(mapChannel, new DynamicObject
                {
                    EntityClassId = doorway.ClassId,
                    DynamicObjectType = DynamicObjectType.Scenery,
                    StateId = doorway.State,
                    ObjectData = doorway,
                    Position = doorway.Position,
                    Rotation = doorway.Yaw,
                    CellAnchor = doorway.SeenFrom,
                    MapContextId = doorway.MapContextId,
                    TargetCategory = TargetCategory.Object,
                    Comment = doorway.Name,
                    IsInWorld = true
                });

                placed++;
            }

            return placed;
        }

        /// <summary>The object standing for a doorway on a map channel, or null.</summary>
        public static DynamicObject DoorwayObject(MapChannel mapChannel, Doorway doorway)
        {
            if (mapChannel?.MapCellInfo == null || doorway == null)
                return null;

            foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                foreach (var obj in cell.DynamicObjectList)
                    if (obj.DynamicObjectType == DynamicObjectType.Scenery && ReferenceEquals(obj.ObjectData, doorway))
                        return obj;

            return null;
        }

        /// <summary>The passage a step on this map passes through, if any.</summary>
        public static Passage Crossed(uint mapContextId, Vector3 from, Vector3 to)
        {
            foreach (var passage in All)
                if (passage.MapContextId == mapContextId && passage.Touches(from, to))
                    return passage;

            return null;
        }

        /// <summary>
        /// One accepted Move: if the step went through a passage, the player is moved to its far
        /// end and true is returned - the Move is then spent, and nothing else is to be made of it.
        /// </summary>
        public static bool OnMove(Client client, Vector3 from, Vector3 to)
        {
            var player = client?.Player;

            if (player?.MapChannel == null)
                return false;

            var passage = Crossed(player.MapContextId, from, to);

            if (passage == null)
                return false;

            Take(client, passage);

            return true;
        }

        /// <summary>The object manager a passage with an effect travels by; the game's own unless a test has put another here.</summary>
        internal static DynamicObjectManager Travel { get; set; }

        /// <summary>
        /// Moves the player to the passage's far end: the server's position, their own client,
        /// the cells they now see (the far end is out of sight of the near one, and what stands
        /// there - a logos - is to be on screen on arrival, not after the next step), and
        /// everyone who can see them there. A passage with an effect does all of that as a
        /// waypoint does, and holds the player until their client has answered the teleport;
        /// one who cannot travel now stays where they stepped, and is taken on a later step.
        /// </summary>
        public static void Take(Client client, Passage passage)
        {
            var player = client.Player;
            var movement = new Movement(passage.Destination, new Vector2(passage.Rotation, 0f));

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} took the secret passage {passage.Name}: {player.Position} -> {passage.Destination}");

            if (passage.WithEffect)
            {
                (Travel ?? DynamicObjectManager.Instance).TakePassage(client, passage.Destination, passage.Rotation);
                return;
            }

            player.PlaceAt(passage.Destination);
            player.Rotation = passage.Rotation;
            client.Movement = movement;

            client.MoveObject(player.EntityId, movement);

            if (player.MapChannel == null)
                return;

            CellManager.Instance.UpdateVisibility(client);
            client.CellMoveObject(client, new Packets.Protocol.MoveObjectMessage(player.EntityId, movement), true);
        }
    }
}
