using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Falling damage. The client announces it (Actor.Recv_AnnounceMapDamage, "Map damaged this
    /// actor (falling)") but never reports a fall - its OnStartFalling/OnStopFalling only switch its
    /// own falling and landing states - so the server works falls out from the positions it is sent.
    ///
    /// A descent is a run of Moves each lower than the last. It is a fall when it drops at least
    /// <see cref="SafeDrop"/> and at some point came down at <see cref="FallSpeed"/> or faster,
    /// measured over at least <see cref="SpeedWindowMs"/>: about 1.2 seconds of free fall. Walking,
    /// or sprinting, down the steepest ground a character can walk on stays well under that, and a
    /// jump is too short to count. The descent ends - the player has landed - with the first Move
    /// that is not lower, or when no Move has come for <see cref="LandingTimeoutMs"/>.
    ///
    /// A fall costs <see cref="PercentPerMetre"/> of maximum health for every metre past SafeDrop,
    /// straight off health, armour or not, and is announced to everyone around as map damage of
    /// type Environmental. Landing in water costs nothing: the water planes the maps place
    /// (WaterSurfaces, generated from the client's .map files) are checked where the descent
    /// ended. Player death is not wired yet (ActorManager.Damage), so a fall that would kill leaves
    /// the player at 1 health rather than announcing a death the server does not carry out.
    ///
    /// Anything that puts a player somewhere (Manifestation.PlaceAt: a teleport, a map change, a
    /// dropship) ends the descent, so no relocation is ever taken for a fall. None of the numbers
    /// come from the client or its data; nothing there says when a fall hurts or by how much.
    /// </summary>
    public static class FallDamage
    {
        /// <summary>Metres a player may fall without harm.</summary>
        public const float SafeDrop = 10f;

        /// <summary>The descent speed, metres a second, that marks a fall rather than a steep walk down.</summary>
        public const float FallSpeed = 12f;

        /// <summary>The shortest stretch a descent speed is measured over, so one early or late packet cannot fake one.</summary>
        public const long SpeedWindowMs = 400;

        /// <summary>How long a descent is followed back, for its speed.</summary>
        public const long SampleHistoryMs = 2000;

        /// <summary>Percent of maximum health per metre fallen past SafeDrop.</summary>
        public const int PercentPerMetre = 5;

        /// <summary>A descent with no Move for this long has landed: a player who stops dead sends nothing.</summary>
        public const long LandingTimeoutMs = 600;

        /// <summary>A step down smaller than this is level ground, not a descent.</summary>
        public const float LevelTolerance = 0.05f;

        /// <summary>How far above a water surface a landing still counts as in the water (a swimmer's reported height).</summary>
        public const float WaterAbove = 1.5f;

        /// <summary>How far below a water surface a landing still counts as in the water, so a cave under a pond does not.</summary>
        public const float WaterBelow = 25f;

        /// <summary>Metres beyond a water plane's edge that still count as in it.</summary>
        public const float WaterEdge = 1f;

        /// <summary>A descent of this drop and peak speed: a fall?</summary>
        public static bool IsFall(float drop, float peakSpeed) => drop >= SafeDrop && peakSpeed >= FallSpeed;

        /// <summary>Health a fall of this drop takes from a player with this maximum health.</summary>
        public static int DamageFor(float drop, int maxHealth)
        {
            if (drop <= SafeDrop || maxHealth <= 0)
                return 0;

            return (int)Math.Ceiling(maxHealth * (double)PercentPerMetre * (drop - SafeDrop) / 100.0);
        }

        /// <summary>Whether a point on the map is in one of its water planes.</summary>
        public static bool InWater(string mapName, Vector3 position)
        {
            if (mapName == null || !WaterSurfaces.ByMap.TryGetValue(mapName, out var surfaces))
                return false;

            foreach (var surface in surfaces)
                if (position.Y <= surface.SurfaceY + WaterAbove && position.Y >= surface.SurfaceY - WaterBelow
                    && surface.Covers(position.X, position.Z, WaterEdge))
                    return true;

            return false;
        }

        /// <summary>
        /// One accepted Move, from where the player was to where they are now. Returns the fall
        /// that ended with it, if one did: the drop and whether it was harmless for the water.
        /// </summary>
        public static (float Drop, bool Water)? OnMove(Client client, Vector3 from, Vector3 to, long now)
        {
            var player = client?.Player;

            if (player == null)
                return null;

            var tracker = player.Fall;
            (float, bool)? landed = null;

            if (to.Y < from.Y - LevelTolerance)
                Descend(tracker, from, to, now);
            else if (tracker.Descending)
                landed = Land(client, now);

            tracker.LastMoveTick = now;

            return landed;
        }

        /// <summary>Follows a descent a step further: its lowest point and how fast it is coming down.</summary>
        public static void Descend(FallTracker tracker, Vector3 from, Vector3 to, long now)
        {
            if (!tracker.Descending)
            {
                tracker.Reset();
                tracker.Descending = true;
                tracker.TopY = from.Y;
                tracker.Samples.Add((tracker.LastMoveTick != 0 ? tracker.LastMoveTick : now, from.Y));
            }

            tracker.Samples.Add((now, to.Y));
            tracker.Lowest = to;

            // The speed over the shortest stretch back that is at least the window long.
            for (var i = tracker.Samples.Count - 2; i >= 0; i--)
            {
                var (tick, y) = tracker.Samples[i];
                var elapsed = now - tick;

                if (elapsed < SpeedWindowMs)
                    continue;

                var speed = (y - to.Y) / (elapsed / 1000f);

                if (speed > tracker.PeakSpeed)
                    tracker.PeakSpeed = speed;

                break;
            }

            // Only as much history as a speed is ever measured over.
            while (tracker.Samples.Count > 2 && now - tracker.Samples[1].Tick > SampleHistoryMs)
                tracker.Samples.RemoveAt(0);
        }

        /// <summary>The descent is over: a fall is paid for, unless it ended in water.</summary>
        private static (float, bool)? Land(Client client, long now)
        {
            var player = client.Player;
            var tracker = player.Fall;
            var drop = tracker.TopY - tracker.Lowest.Y;
            var peak = tracker.PeakSpeed;
            var lowest = tracker.Lowest;

            tracker.Reset();

            if (!IsFall(drop, peak))
                return null;

            var mapChannel = player.MapChannel;
            var water = InWater(mapChannel?.MapInfo?.MapName, lowest);

            if (!water)
                Apply(mapChannel, player, drop);

            return (drop, water);
        }

        /// <summary>
        /// The fall's damage, off health alone, announced to everyone who can see the player.
        /// Leaves them at 1 at least. Returns what was taken.
        /// </summary>
        public static int Apply(MapChannel mapChannel, Manifestation player, float drop)
        {
            if (player == null || player.State == CharacterState.Dead || player.State == CharacterState.Dying
                || !player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 1)
                return 0;

            var taken = Math.Min(DamageFor(drop, health.CurrentMax), health.Current - 1);

            if (taken <= 0)
                return 0;

            health.Current -= taken;

            if (mapChannel != null)
            {
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateHealthPacket(health, 0));
                CellManager.Instance.CellCallMethod(mapChannel, player, new AnnounceMapDamagePacket(taken));
            }

            return taken;
        }

        /// <summary>
        /// .moveflags: for a GM watching, the Move packet's flags byte and leading byte whenever
        /// either changes, with the height and the step up or down - to find whether the client
        /// marks being in the air or in water there, which nothing has decoded.
        /// </summary>
        public static void ShowMoveFlags(Client client, Packets.Protocol.MoveMessage move, Vector3 from)
        {
            var tracker = client?.Player?.Fall;

            if (tracker == null || !tracker.WatchFlags || move?.Movement == null)
                return;

            var movement = move.Movement;
            var packed = (move.UnkByte << 16) | (movement.UnknownByte << 8) | movement.Flags;

            if (packed == tracker.LastFlags)
                return;

            tracker.LastFlags = packed;

            var to = movement.Position;

            CommunicatorManager.Instance.SystemMessage(client,
                $"move flags {Convert.ToString(movement.Flags, 2).PadLeft(8, '0')} (0x{movement.Flags:X2}), bytes {move.UnkByte}/{movement.UnknownByte}, "
                + $"y {to.Y:0.00} ({to.Y - from.Y:+0.00;-0.00}), water here {(InWater(client.Player.MapChannel?.MapInfo?.MapName, to) ? "yes" : "no")}");
        }

        /// <summary>Lands whoever has stopped sending Moves in the middle of a descent: they are on the ground and standing still.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var tracker = client?.Player?.Fall;

                if (tracker != null && tracker.Descending && now - tracker.LastMoveTick >= LandingTimeoutMs)
                    Land(client, now);
            }
        }
    }
}
