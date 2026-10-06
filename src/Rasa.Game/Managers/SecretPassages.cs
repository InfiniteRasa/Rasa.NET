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
    /// Hidden same-map teleports: a box in the world that moves whoever enters it somewhere else
    /// on the map, with no loading screen and nothing on screen to give it away. Checked on every
    /// accepted Move (Client), against the step the player just took, so a fall fast enough to
    /// pass through a thin box between two Moves still counts. Ours: the client has no such
    /// thing, and nothing in its data says where the original ones were.
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
    /// corridor (adv_foreas_concordia_wilderness, 835, 286, 723..739) whose far doorway opens on
    /// nothing: no room behind it, no map link, no instance. The shrine is 181 m further north,
    /// sealed: ten identical rooms stacked one above the other at 832, y, 920..985, 64 m apart
    /// from y 352 down to y -224, each a corridor with an open south end, a room and a Logos
    /// dispenser base at z 960. The Enhance logos is in the fourth, floor y 159.9. The cave's
    /// doorway is the way in to that one, and that room's corridor end is the way back. Both
    /// boxes are a slab across the doorway, starting 1.7 m short of where the floor stops, so
    /// nobody walks off the edge or into what stands in the doorway (below) first; the hillside
    /// over the cave is 70 m above the first, and there is no ground at all around the second.
    ///
    /// What stands in those two doorways (<see cref="Doorways"/>). Open, each shows the void the
    /// map ends in. The client's map marks six ways out of the Wilderness - the two passes to
    /// the Divide and four instance doors - with one piece, TerraForeasCavernInstance (6977): an
    /// 8 m stub of cave tunnel, 14.3 m wide and 10.7 m high, plugged with rock at the back and
    /// lined just inside its mouth with the swirling band of a transition (the mesh's own
    /// animation, terra_foreas_cavern_instance.anm, so it plays wherever the mesh is). The maps
    /// put it against Eloh cavern pieces elsewhere on Foreas (Palisades, Valverde Descent). One
    /// stands behind each doorway here, mouth to the corridor: the opening in the corridor's end
    /// is an oval 12.6 m by 5.5 m, the band is 12.6 m across, so it shows down both sides and
    /// along the floor. The stub's axis is 4.5 m over the corridor floor and its mouth 0.44 m
    /// inside the corridor's end: placed so, no line of sight from anywhere a head can be in the
    /// corridor reaches past the rock (checked against the client's meshes from 77 head
    /// positions each; 0.5 m higher and slivers of void show at the corners). The server makes
    /// them as the client's map loader makes its own pieces (DynamicObjectType.Scenery). The
    /// stub's rock has collision like any piece of a map, so each passage's slab starts 1.2 m
    /// ahead of its stub's mouth, the whole width of the corridor: whoever walks at the doorway
    /// is through before they touch it.
    /// </summary>
    public static class SecretPassages
    {
        public sealed class Passage
        {
            public Passage(string name, uint mapContextId, Vector3 min, Vector3 max, Vector3 destination, float rotation)
            {
                Name = name;
                MapContextId = mapContextId;
                Min = Vector3.Min(min, max);
                Max = Vector3.Max(min, max);
                Destination = destination;
                Rotation = rotation;
            }

            public string Name { get; }
            public uint MapContextId { get; }

            /// <summary>The box that triggers it, world axes, Y up.</summary>
            public Vector3 Min { get; }
            public Vector3 Max { get; }

            public Vector3 Destination { get; }

            /// <summary>The way the player faces on arrival, as Player.Rotation: 0 faces -Z.</summary>
            public float Rotation { get; }

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

        /// <summary>The open north end of the Alia Caverns corridor (floor y 286.0, ceiling 291.5, x 827..843, the floor stops at z 739.2).</summary>
        public static readonly Passage AliaCavernsDoor = new Passage(
            "Concordia Wilderness: Alia Caverns, the doorway at the end of the Eloh corridor into the Enhance shrine",
            ConcordiaWilderness,
            new Vector3(826f, 280f, 737.5f), new Vector3(844f, 296f, 744f),
            new Vector3(832f, 160.1f, 927f), MathF.PI);

        /// <summary>The open south end of the Enhance shrine's corridor (floor y 159.9, ceiling 165.4, x 824..840, the floor stops at z 919.8): back into the cave.</summary>
        public static readonly Passage EnhanceShrineExit = new Passage(
            "Concordia Wilderness: the end of the Enhance shrine's corridor, back into Alia Caverns",
            ConcordiaWilderness,
            new Vector3(823f, 154f, 915f), new Vector3(841f, 170f, 921.5f),
            new Vector3(835f, 286.3f, 733f), 0f);

        public static readonly Passage[] All = { JumpAndBelieve, GrowthHallEnd, AliaCavernsDoor, EnhanceShrineExit };

        /// <summary>A piece of scenery that fills the doorway a passage is in, so that it does not open on nothing.</summary>
        public sealed class Doorway
        {
            public Doorway(string name, Passage passage, EntityClasses classId, Vector3 position, float yaw, Vector3 seenFrom)
            {
                Name = name;
                Passage = passage;
                ClassId = classId;
                Position = position;
                Yaw = yaw;
                SeenFrom = seenFrom;
            }

            public string Name { get; }

            /// <summary>The passage whose doorway this fills.</summary>
            public Passage Passage { get; }

            public uint MapContextId => Passage.MapContextId;
            public EntityClasses ClassId { get; }

            /// <summary>The piece's origin: for the cave stub, on its axis at the plugged end.</summary>
            public Vector3 Position { get; }

            /// <summary>Turn about the vertical, as a DynamicObject's Rotation. At 0 the cave stub's mouth is 8.3 m towards -Z.</summary>
            public float Yaw { get; }

            /// <summary>
            /// The place whose map cell the piece is filed in (DynamicObject.CellAnchor). A client
            /// has an object from two cells of 25.6 m around it, 51 to 77 m; filed where it stands,
            /// behind the doorway, the piece would come too late for someone looking down the
            /// length of the way to it, who would see the doorway open first.
            /// </summary>
            public Vector3 SeenFrom { get; }

            /// <summary>The middle of the cave stub's mouth, at floor height: 8.3 m along its axis from the origin and 4.5 m down.</summary>
            public Vector3 Mouth => Position + Vector3.Transform(new Vector3(0f, -CavernTransitionFloor, -CavernTransitionLength), Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));
        }

        /// <summary>TerraForeasCavernInstance: the cave mouth with the transition's swirl that the Wilderness map puts at every way out of it.</summary>
        public const EntityClasses CavernTransition = (EntityClasses)6977;

        /// <summary>From the cave stub's origin to its mouth, along its axis (the mesh's bounds).</summary>
        public const float CavernTransitionLength = 8.3f;

        /// <summary>How far under the cave stub's axis a corridor's floor is put.</summary>
        public const float CavernTransitionFloor = 4.5f;

        /// <summary>Behind the open north end of the Alia Caverns corridor (its end at z 739.14, floor y 286.0).</summary>
        public static readonly Doorway AliaCavernsDoorway = new Doorway(
            "Concordia Wilderness: the transition in the Alia Caverns corridor's doorway",
            AliaCavernsDoor, CavernTransition,
            new Vector3(835f, 290.5f, 747f), 0f,
            // The tunnel runs straight at the doorway from z 647: in the cell of z 691..717, the piece is there from z 640 on.
            new Vector3(835f, 286f, 705f));

        /// <summary>Behind the open south end of the Enhance shrine's corridor (its end at z 919.86, floor y 159.93): the same, turned about.</summary>
        public static readonly Doorway EnhanceShrineDoorway = new Doorway(
            "Concordia Wilderness: the transition in the Enhance shrine corridor's doorway",
            EnhanceShrineExit, CavernTransition,
            new Vector3(832f, 164.43f, 912f), MathF.PI,
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

        /// <summary>
        /// Moves the player to the passage's far end: the server's position, their own client,
        /// the cells they now see (the far end is out of sight of the near one, and what stands
        /// there - a logos - is to be on screen on arrival, not after the next step), and
        /// everyone who can see them there.
        /// </summary>
        public static void Take(Client client, Passage passage)
        {
            var player = client.Player;
            var movement = new Movement(passage.Destination, new Vector2(passage.Rotation, 0f));

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} took the secret passage {passage.Name}: {player.Position} -> {passage.Destination}");

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
