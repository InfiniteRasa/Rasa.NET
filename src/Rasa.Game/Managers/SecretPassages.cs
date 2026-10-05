using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Game;
    using Models;

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
    /// boxes are a slab across the doorway, starting just short of where the floor stops, so
    /// nobody walks off the edge; the hillside over the cave is 70 m above the first, and there
    /// is no ground at all around the second.
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
            new Vector3(826f, 280f, 738.5f), new Vector3(844f, 296f, 744f),
            new Vector3(832f, 160.1f, 927f), MathF.PI);

        /// <summary>The open south end of the Enhance shrine's corridor (floor y 159.9, ceiling 165.4, x 824..840, the floor stops at z 919.8): back into the cave.</summary>
        public static readonly Passage EnhanceShrineExit = new Passage(
            "Concordia Wilderness: the end of the Enhance shrine's corridor, back into Alia Caverns",
            ConcordiaWilderness,
            new Vector3(823f, 154f, 915f), new Vector3(841f, 170f, 920.6f),
            new Vector3(835f, 286.3f, 733f), 0f);

        public static readonly Passage[] All = { JumpAndBelieve, GrowthHallEnd, AliaCavernsDoor, EnhanceShrineExit };

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
