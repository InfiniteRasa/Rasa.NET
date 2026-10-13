using System;
using System.Collections.Generic;
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
    /// A descent is a run of Moves each lower than the last. A fall is the part of it that came
    /// down at <see cref="FallSpeed"/> or faster, measured over at least <see cref="SpeedWindowMs"/>:
    /// about 1.2 seconds of free fall. Walking, or sprinting, down the steepest ground a character
    /// can walk on stays well under that, and a jump is too short to count. The descent ends - the
    /// player has landed - with the first Move that is not lower, or when no Move has come for
    /// <see cref="LandingTimeoutMs"/>.
    ///
    /// Only the fall is paid for, not the whole descent: a hill walked down to a ledge, or on down
    /// after landing, is not part of the drop. Walking down comes down at a steady pace and a fall
    /// gathers speed, so the fall is taken to start at the last Move before the descent speed began
    /// rising towards FallSpeed (<see cref="StartOf"/>), and to end with the step the ground is met
    /// in: the one after the last that came down at FallSpeed (<see cref="EndOf"/>). A fall that
    /// lands on a slope the player runs on down is paid for as soon as the descent speed has been
    /// under FallSpeed for SpeedWindowMs; two ledges with a slope between are two falls. Moves are
    /// timed by when they arrive, so packets that come unevenly blur both ends: by up to a metre
    /// or two at worst, against the whole hillside the descent used to be charged with.
    ///
    /// A fall is paid for when it drops at least <see cref="SafeDrop"/>.
    ///
    /// A fall costs <see cref="PercentPerMetre"/> of maximum health for every metre past SafeDrop,
    /// straight off health, armour or not, and is announced to everyone around as map damage of
    /// type Environmental. Landing in water costs nothing: the water planes the maps place
    /// (WaterSurfaces, generated from the client's .map files) are checked where the descent
    /// ended. A fall that takes the last of a player's health kills them (PlayerDeath).
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

        /// <summary>How long a descent is followed back with no fall under way: longer than a fall takes to gather speed.</summary>
        public const long SampleHistoryMs = 5000;

        /// <summary>The most Moves of one descent kept, whatever else: a client that never stops coming down cannot grow it.</summary>
        public const int MaxSamples = 1000;

        /// <summary>The shortest stretch a step's own descent speed is measured over - two Moves - so one packet that comes early does not make a walk a fall.</summary>
        public const long MinStepMs = 150;

        /// <summary>How much slower than the speeds after it a descent speed has to be to be part of the fall gathering speed.</summary>
        public const float RiseMargin = 0.25f;

        /// <summary>
        /// A descent speed no walk reaches, so the player is falling: running is 6.5 m/s
        /// (ManifestationManager.RunSpeed), Sprint at its top pump 160 %, and the steepest slope the
        /// navmesh walks is 50 degrees - 6.5 x 1.6 x sin 50 is about 8 m/s.
        /// </summary>
        public const float WalkingDownMax = 9f;

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
            {
                // Come down onto a slope and running on down it: the fall is over all the same.
                var fall = Descend(tracker, from, to, now);

                if (fall.HasValue)
                    landed = Charge(client, fall.Value);
            }
            else if (tracker.Descending)
                landed = Land(client, now);

            tracker.LastMoveTick = now;

            return landed;
        }

        /// <summary>
        /// Follows a descent a step further: its lowest point, how fast it is coming down, and the
        /// fall within it. Returns a fall that is over while the descent goes on - the drop and
        /// where it ended - once the descent speed has been under FallSpeed for SpeedWindowMs.
        /// </summary>
        public static (float Drop, Vector3 At)? Descend(FallTracker tracker, Vector3 from, Vector3 to, long now)
        {
            if (!tracker.Descending)
            {
                tracker.Reset();
                tracker.Descending = true;
                tracker.TopY = from.Y;
                tracker.Samples.Add((tracker.LastMoveTick != 0 ? tracker.LastMoveTick : now, from, -1f));
            }

            var speed = WindowSpeed(tracker, to.Y, now);

            tracker.Samples.Add((now, to, speed));
            tracker.Lowest = to;

            var index = tracker.Samples.Count - 1;
            (float, Vector3)? over = null;

            if (speed > tracker.PeakSpeed)
                tracker.PeakSpeed = speed;

            if (speed >= FallSpeed)
            {
                if (tracker.FirstFastIndex < 0)
                    tracker.FirstFastIndex = index;

                tracker.LastFastIndex = index;
            }
            else if (tracker.FirstFastIndex >= 0 && now - tracker.Samples[tracker.LastFastIndex].Tick >= SpeedWindowMs)
            {
                // The speed is measured over nothing of the fall any more: the player is down.
                over = Close(tracker, index);
            }

            // All of a fall under way is kept, for where it started; otherwise as much as a fall
            // could have been gathering speed over.
            while (tracker.Samples.Count > 2 && (tracker.Samples.Count > MaxSamples
                       || tracker.FirstFastIndex < 0 && now - tracker.Samples[1].Tick > SampleHistoryMs))
                tracker.DropOldest(1);

            return over;
        }

        /// <summary>The descent speed over the shortest stretch back to a Move that is at least SpeedWindowMs long, or -1 when there is none yet.</summary>
        private static float WindowSpeed(FallTracker tracker, float y, long now)
        {
            for (var i = tracker.Samples.Count - 1; i >= 0; i--)
            {
                var (tick, at, _) = tracker.Samples[i];
                var elapsed = now - tick;

                if (elapsed >= SpeedWindowMs)
                    return (at.Y - y) / (elapsed / 1000f);
            }

            return -1f;
        }

        /// <summary>The fall under way is over by the Move at <paramref name="lastIndex"/>: its drop and where it ended.</summary>
        private static (float Drop, Vector3 At) Close(FallTracker tracker, int lastIndex)
        {
            var start = StartOf(tracker, tracker.FirstFastIndex);
            var end = EndOf(tracker, tracker.FirstFastIndex, lastIndex);

            tracker.ClosedThroughIndex = end;
            tracker.FirstFastIndex = -1;
            tracker.LastFastIndex = -1;

            return (tracker.Samples[start].At.Y - tracker.Samples[end].At.Y, tracker.Samples[end].At);
        }

        /// <summary>
        /// Where the fall began: back from its first Move at FallSpeed, as long as the descent speed
        /// was rising towards it - a fall gathers speed, walking down keeps a steady one. A speed no
        /// walk reaches (<see cref="WalkingDownMax"/>) is the fall whatever comes after it. Below
        /// that, each earlier speed is set against the middle one of the next three, so one packet
        /// that came late or early neither ends the fall too soon nor carries it on up a hill. Past
        /// the start of the descent, or the end of a fall before it, it cannot go.
        /// </summary>
        public static int StartOf(FallTracker tracker, int firstFast)
        {
            var samples = tracker.Samples;
            var floor = tracker.ClosedThroughIndex;
            var i = firstFast;

            // The fall's fastest Move: after it the speed falls away with the landing in it.
            var peak = firstFast;

            for (var j = firstFast + 1; j <= Math.Max(firstFast, tracker.LastFastIndex) && j < samples.Count; j++)
                if (samples[j].Speed > samples[peak].Speed)
                    peak = j;

            while (i - 1 >= floor)
            {
                var before = samples[i - 1].Speed;

                // The descent's first moments, too short to have a speed: nothing tells a walk from
                // a fall there, and the descent is taken from its start.
                if (before < 0)
                    return floor;

                if (before < WalkingDownMax && before >= Ahead(samples, i, peak) - RiseMargin)
                    return i - 1;

                i--;
            }

            return floor;
        }

        /// <summary>The middle one of the speeds at <paramref name="i"/> and the two after it, up to the fall's fastest (fewer near it).</summary>
        private static float Ahead(List<(long Tick, Vector3 At, float Speed)> samples, int i, int peak)
        {
            var last = Math.Min(Math.Max(i, peak), i + 2);
            var count = last - i + 1;

            if (count == 1)
                return samples[i].Speed;

            if (count == 2)
                return Math.Max(samples[i].Speed, samples[i + 1].Speed);

            var a = samples[i].Speed;
            var b = samples[i + 1].Speed;
            var c = samples[i + 2].Speed;

            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        /// <summary>
        /// Where the fall ended: the Move after the last one from <paramref name="firstFast"/> to
        /// <paramref name="lastIndex"/> that came down at FallSpeed from the one before. The ground
        /// is met somewhere in that next step, which is mostly a part step for it; after it, a
        /// step down a slope is a walking pace.
        /// </summary>
        public static int EndOf(FallTracker tracker, int firstFast, int lastIndex)
        {
            for (var j = lastIndex; j >= firstFast; j--)
                if (StepSpeed(tracker, j) >= FallSpeed)
                    return Math.Min(j + 1, lastIndex);

            return tracker.LastFastIndex >= 0 ? tracker.LastFastIndex : lastIndex;
        }

        /// <summary>How fast the Move at <paramref name="index"/> came down from the one before it at least MinStepMs earlier, or -1.</summary>
        private static float StepSpeed(FallTracker tracker, int index)
        {
            var (tick, at, _) = tracker.Samples[index];

            for (var k = index - 1; k >= 0; k--)
            {
                var elapsed = tick - tracker.Samples[k].Tick;

                if (elapsed >= MinStepMs)
                    return (tracker.Samples[k].At.Y - at.Y) / (elapsed / 1000f);
            }

            return -1f;
        }

        /// <summary>The descent is over: a fall under way is paid for, unless it ended in water.</summary>
        private static (float, bool)? Land(Client client, long now)
        {
            var tracker = client.Player.Fall;
            (float, Vector3)? fall = tracker.FirstFastIndex >= 0 ? Close(tracker, tracker.Samples.Count - 1) : null;

            tracker.Reset();

            return fall.HasValue ? Charge(client, fall.Value) : null;
        }

        /// <summary>A fall that is over: paid for if it dropped far enough, and did not end in water.</summary>
        private static (float, bool)? Charge(Client client, (float Drop, Vector3 At) fall)
        {
            if (!IsFall(fall.Drop, FallSpeed))
                return null;

            var player = client.Player;
            var mapChannel = player.MapChannel;
            var water = InWater(mapChannel?.MapInfo?.MapName, fall.At);

            if (!water)
                Apply(mapChannel, player, fall.Drop);

            return (fall.Drop, water);
        }

        /// <summary>
        /// The fall's damage, off health alone, announced to everyone who can see the player. A
        /// fall that takes the last of it kills (PlayerDeath), announced as a death blow. Returns
        /// what was taken.
        /// </summary>
        public static int Apply(MapChannel mapChannel, Manifestation player, float drop)
        {
            if (player == null || player.State == CharacterState.Dead || player.State == CharacterState.Dying
                || !player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return 0;

            var taken = Math.Min(DamageFor(drop, health.CurrentMax), health.Current);

            if (taken <= 0)
                return 0;

            health.Current -= taken;

            if (health.Current <= 0 && PlayerDeath.AtZero(mapChannel, player, null))
            {
                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, player, new AnnounceMapDamagePacket(taken, deathBlow: true));

                return taken;
            }

            if (mapChannel != null)
            {
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateHealthPacket(health, 0));
                CellManager.Instance.CellCallMethod(mapChannel, player, new AnnounceMapDamagePacket(taken));
            }

            return taken;
        }

        /// <summary>
        /// .moveflags: for a GM watching, the Move packet's flags byte, its leading byte and its
        /// movement type whenever one changes, with the height and the step up or down. The type
        /// is 1 on the Move that starts a jump (MovementType.Jump); nothing in a Move marks
        /// falling or water.
        /// </summary>
        public static void ShowMoveFlags(Client client, Packets.Protocol.MoveMessage move, Vector3 from)
        {
            var tracker = client?.Player?.Fall;

            if (tracker == null || !tracker.WatchFlags || move?.Movement == null)
                return;

            var movement = move.Movement;
            var packed = (move.UnkByte << 16) | ((byte)movement.Type << 8) | movement.Flags;

            if (packed == tracker.LastFlags)
                return;

            tracker.LastFlags = packed;

            var to = movement.Position;

            CommunicatorManager.Instance.SystemMessage(client,
                $"move flags {Convert.ToString(movement.Flags, 2).PadLeft(8, '0')} (0x{movement.Flags:X2}), bytes {move.UnkByte}/{(byte)movement.Type}, "
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
