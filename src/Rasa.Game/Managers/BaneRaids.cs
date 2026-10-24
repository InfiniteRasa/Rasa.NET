using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The raids on the open world's control points: the Bane's on the points the AFS hold, as the
    /// original servers ran them, and the AFS's on the points the Bane hold.
    ///
    /// What the original had, from the players' accounts of it: "At regular intervals, the Bane will
    /// attempt to take over the CP ... They usually come in 3-4 waves" (Wilderness Landing Zone); "as
    /// Bane are killed, even more are called in for backup ... These troops will systematically attack
    /// anything that poses a threat"; "If they manage to enter the base and reach the CP structure, they
    /// will take it over within 10 seconds unless they are interrupted by attacking them" (Control
    /// Point); a point "got increasingly difficult to defend the longer it was held". From the client:
    /// "Thrax are gathering to attack a friendly outpost. Reinforcements will arrive regularly while any
    /// Thrax are alive" (PM_BRIF_WELCOME), "THRAX REINFORCEMENTS ARE ARRIVING!!!" (PM_BRIF_REINFORCEMENTS),
    /// "AFS Reinforcements En Route - 5 minutes remaining" down to "10 seconds" (1369, 1371-1376) and
    /// "AFS Reinforcements Called Back" (1370), and the control point's own "%(faction)s claiming".
    ///
    /// What is ours, since the client has none of it:
    ///  - When: a point the AFS hold is raided every <see cref="RaidConfig.RaidMinutesMin"/> to
    ///    <see cref="RaidConfig.RaidMinutesMax"/> minutes, the first time some minutes after the server
    ///    starts, and <see cref="RaidConfig.RaidAfterCaptureMinutes"/> after the AFS take it - but a raid
    ///    begins only while a player is within <see cref="RaidConfig.RaidRange"/> of the point, and one who
    ///    is playing (not under .immune, not AFK). Nobody is there to see a point fall to nobody.
    ///  - Who: the troops of the point's own Bane garrison (the pools it has while the Bane hold it), or
    ///    the Bane troops that stand nearest it on its map (Reinforcements.TroopsNear): the ground's own
    ///    kind, at its level.
    ///  - How: each wave is one Bane dropship of <see cref="RaidConfig.RaidSquadSize"/> troops, two from the
    ///    third wave on and one more for every three players near; it comes down before a gate of the
    ///    point's outpost, each ship at the next gate and a second beside the first (Reinforcements.GateLanding),
    ///    or, for a point with no gate that has room, round it - outside the walls of a point within them,
    ///    never in its yard, never more than <see cref="Reinforcements.RaidLandingFurthest"/> out, and always
    ///    with a way on foot to it. A ship with nowhere to come down just now waits for room
    ///    (<see cref="ShipRetryMs"/>, given up after <see cref="ShipOwedMs"/>). The troops' ground is the point
    ///    itself (a pool of no table's, centred on its object): they walk to it, take on whatever they
    ///    meet on the way - a gate's shield among it - and go back to it after. The next wave is called in
    ///    when the last is worn down to a third, or after <see cref="WaveTimeoutMs"/>.
    ///  - The claim: a raider standing within <see cref="ClaimRange"/> of the point's object, not fighting
    ///    and left unhurt for <see cref="UnhurtMs"/>, claims it: the object is locked to it and shows its
    ///    contested effect, everyone in range sees it at the object, and the map is told "Bane claiming
    ///    Control Point" - no more than once in <see cref="ClaimNewsMs"/>, however often a claim is begun
    ///    again under fire. Hurt, drawn into a fight, gone from the spot or dead, it is interrupted;
    ///    standing there for <see cref="RaidConfig.RaidClaimSeconds"/> it takes the point for the Bane
    ///    through ControlPoints' own change of hands: the AFS garrison goes, and the point's own Bane
    ///    garrison is flown in (SpawnPool.ShipNextArrival), with the raiders still standing holding it
    ///    beside them until they are killed (ControlPoints.GarrisonOf); a point with no Bane garrison of its
    ///    own is given the raid's pool for one (<see cref="Garrison"/>), and a ship brings the rest of it.
    ///  - The end: every wave sent and every raider dead - the AFS held - or the point taken, or after
    ///    <see cref="MaxRaidMs"/>, when what is left of the raid goes back the way it came.
    ///  - Defence: while a raid is on, the side that holds the point fights it - whoever of it stands within
    ///    <see cref="RaidConfig.RaidDefendRange"/> of the point goes for the raiders there, the nearest few
    ///    for one claiming it (<see cref="Defend"/>). A point under attack gets no reinforcements until the
    ///    raid is over: its defending side's pools are not restocked meanwhile (SpawnPool.Besieged).
    ///  - The AFS raid the points the Bane hold (<see cref="RaidConfig.AfsRaids"/>), built as the Bane's
    ///    raids are but with less hope: "AFS Reinforcements En Route" counted down for two minutes, then
    ///    three waves of a single AFS dropship of eight soldiers, one every 40 s - the point's own AFS
    ///    soldiers or the nearest the map has - brought to three levels over the middle level of the
    ///    point's Bane (whose rows are tougher than the AFS's); near the point they go for the Bane's
    ///    healers first (<see cref="HealersFirst"/>), and a claim takes <see cref="RaidConfig.AfsRaidClaimSeconds"/>.
    ///    Beaten off, "AFS Reinforcements Called Back". Taken, the point's AFS garrison is flown in and the
    ///    soldiers still standing hold it beside it.
    ///  - While players are at a point the Bane hold and its garrison stands, AFS ships bring some of the
    ///    point's own AFS soldiers to help them take it (<see cref="Support"/>): "some AFS Dropships
    ///    swooping in to deposit NPC troops ... to assist with the assault".
    /// </summary>
    public class BaneRaids
    {
        private static BaneRaids _instance;
        private static readonly object InstanceLock = new object();

        public static BaneRaids Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new BaneRaids();

                return _instance;
            }
        }

        /// <summary>How near the point's object a raider stands to claim it, across the ground.</summary>
        public const float ClaimRange = 8f;

        /// <summary>The raiders' ground round the point's object: where they walk to and stand.</summary>
        public const float GroundRadius = 6f;

        /// <summary>How far out the raid's ships come down round a point with no gate that has room.</summary>
        public const float LandingMin = 40f;
        public const float LandingMax = 75f;

        /// <summary>How long a wave is given before the next is called in whatever is left of it.</summary>
        public const long WaveTimeoutMs = 90_000;

        /// <summary>How long a raid may last before what is left of it is taken back.</summary>
        public const long MaxRaidMs = 20 * 60_000;

        /// <summary>Who is told of a raid: the players this near its point.</summary>
        public const float AnnounceRange = 400f;

        /// <summary>How long the AFS wait between a wave being worn down and the next ships coming, when their waves are not an assault.</summary>
        public const long AfsWaveGapMs = 30_000;

        /// <summary>How often a ship that had no room is tried again, and how long after its wave it is given up (ms).</summary>
        public const long ShipRetryMs = 3000;
        public const long ShipOwedMs = 45000;

        /// <summary>How long a raider must have been left unhurt before it begins a claim.</summary>
        public const long UnhurtMs = 3000;

        /// <summary>How long after the map was told of a raid's claim another of the same raid is told.</summary>
        public const long ClaimNewsMs = 30_000;

        /// <summary>How long a garrison given to a point waits, once all of it is down, before it comes back.</summary>
        public const long GarrisonRespawnMs = 180_000;

        /// <summary>The ground a garrison given to a point stands on, round its object.</summary>
        public const float GarrisonRadius = 15f;

        /// <summary>How near a point the Bane hold players must be for the AFS to help them, and how often a ship comes.</summary>
        public const float SupportRange = 120f;
        public const long SupportEveryMs = 180_000;

        /// <summary>How many the AFS keep on the ground at a point at once, and bring in a ship.</summary>
        public const int SupportMost = 6;
        public const int SupportShip = 3;

        /// <summary>The creature rows of the AFS's light and heavy soldiers, for a raid with no soldiers of its own to draw on.</summary>
        public static readonly uint[] AfsSoldierRows = { 39, 46 };

        /// <summary>
        /// The AFS's coming, told before their first ships: how long before, and what. The client has
        /// these from five minutes; a warning longer than that tells nothing before them.
        /// </summary>
        private static readonly (long Before, PlayerMessage Message)[] Countdown =
        {
            (300_000, PlayerMessage.ARakshaAfsArrive5Min), (240_000, PlayerMessage.ARakshaAfsArrive4Min),
            (180_000, PlayerMessage.ARakshaAfsArrive3Min), (120_000, PlayerMessage.ARakshaAfsArrive2Min),
            (60_000, PlayerMessage.ARakshaAfsArrive1Min), (30_000, PlayerMessage.ARakshaAfsArrive30Sec),
            (10_000, PlayerMessage.ARakshaAfsArrive10Sec)
        };

        /// <summary>
        /// The Bane's healers, whose actions mend or raise the others: a Caretaker heals and raises the fallen
        /// within 40 m, a Technician the same nearer, a shield drone heals, the Machina raise.
        /// </summary>
        private static readonly HashSet<ActionId> BaneMenders = new HashSet<ActionId>
        {
            ActionId.CrCaretakerHeal, ActionId.CrCaretakerRevive, ActionId.CrShieldDroneHeal,
            ActionId.CrTechnicianHeal, ActionId.CrTechnicianRevive, ActionId.CrMachinaRevive
        };

        public sealed class Raid
        {
            public ControlPoints.Point Point { get; init; }
            public MapChannel Map { get; init; }

            /// <summary>Who raid: <see cref="ControlPoints.Bane"/> a point the AFS hold, <see cref="ControlPoints.Afs"/> one the Bane hold.</summary>
            public byte Side { get; init; } = ControlPoints.Bane;

            public bool ByAfs => Side == ControlPoints.Afs;

            /// <summary>The level its troops are brought to as they are set down (SpawnPool.Level); 0: as their rows are.</summary>
            public uint Level { get; init; }

            /// <summary>The next "AFS Reinforcements En Route" of <see cref="Countdown"/> to tell, before an AFS raid's next ships.</summary>
            public int News { get; set; }

            /// <summary>The defending side's pools at the point, not restocked while the raid is on (SpawnPool.Besieged).</summary>
            public List<SpawnPool> Besieged { get; } = new List<SpawnPool>();

            /// <summary>The raiders' own pool: no table's, centred on the point (Reinforcements.TransientPool).</summary>
            public SpawnPool Pool { get; init; }

            /// <summary>The creature rows its troops are drawn from, the first the most.</summary>
            public List<uint> Troops { get; init; }

            public int Waves { get; init; }
            public int Squad { get; init; }
            public long StartedAt { get; init; }

            /// <summary>Waves sent so far.</summary>
            public int Wave { get; set; }

            /// <summary>When the next wave's ships are sent; 0 while none is called for.</summary>
            public long NextWaveAt { get; set; }

            public long WaveSentAt { get; set; }

            /// <summary>The bearing round the point the next ship comes down on, in radians, for a point with no gate that has room.</summary>
            public double Bearing { get; set; }

            /// <summary>The gate of the point's outpost the next ship comes down before (an index into OutpostWalls.GatesOf; <see cref="GateLanding"/>).</summary>
            public int Gate { get; set; }

            /// <summary>Ships of the last wave that had nowhere to come down yet (the ship before them still stands there), and when one is next tried.</summary>
            public int ShipsOwed { get; set; }
            public long ShipOwedAt { get; set; }

            /// <summary>Ships of the last wave that did come down.</summary>
            public int WaveShips { get; set; }

            public Creature Claimer { get; set; }
            public long ClaimStartedAt { get; set; }

            /// <summary>The claimer's health and armour when last looked at: less, and it was hurt.</summary>
            public int ClaimToughness { get; set; }

            /// <summary>Each raider's health and armour when last looked at, and when it was last hurt: one under fire does not start a claim.</summary>
            public Dictionary<ulong, (int Toughness, long HurtAt)> Hurt { get; } = new Dictionary<ulong, (int, long)>();

            /// <summary>When the map was last told of this raid's claim: a claim begun again at once is not told again.</summary>
            public long ToldClaimAt { get; set; }
        }

        private readonly Dictionary<uint, Raid> _raids = new Dictionary<uint, Raid>();

        /// <summary>Raids that took their point: their raiders hold it with its garrison until killed or the point changes hands again.</summary>
        private readonly Dictionary<uint, Raid> _occupying = new Dictionary<uint, Raid>();

        /// <summary>The Bane garrisons given to points that have none of their own while the Bane hold them (<see cref="Garrison"/>).</summary>
        private readonly Dictionary<uint, (MapChannel Map, SpawnPool Pool)> _garrisons = new Dictionary<uint, (MapChannel, SpawnPool)>();

        /// <summary>The troops the AFS fly in to help players take a point the Bane hold, by point.</summary>
        private readonly Dictionary<uint, (MapChannel Map, SpawnPool Pool, long NextAt)> _support = new Dictionary<uint, (MapChannel, SpawnPool, long)>();

        private readonly Dictionary<uint, long> _nextAt = new Dictionary<uint, long>();
        private readonly Dictionary<uint, byte> _lastOwner = new Dictionary<uint, byte>();
        private readonly Random _random = new Random();

        /// <summary>appsettings.json's Raids. Replaced whole when the file changes.</summary>
        public RaidConfig Config { get; set; } = new RaidConfig();

        /// <summary>The clock raids run on, in milliseconds. Replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        public IReadOnlyCollection<Raid> Running => _raids.Values;

        public Raid Of(uint pointId) => _raids.TryGetValue(pointId, out var raid) ? raid : null;

        /// <summary>When the next raid on a point is due, on <see cref="Now"/>; null when none is set.</summary>
        public long? NextAt(uint pointId) => _nextAt.TryGetValue(pointId, out var at) ? at : null;

        /// <summary>Sets when the next raid on a point is due (a game master's ".cp &lt;id&gt; raid in").</summary>
        public void SetNextAt(uint pointId, long at) => _nextAt[pointId] = at;

        /// <summary>The point a pool's creatures raid or hold, if it is the pool of a Bane raid: its raiders are the point's minions (ControlPoints.CreatureKilled).</summary>
        public ControlPoints.Point PointOfPool(SpawnPool pool)
        {
            if (pool == null || !Reinforcements.IsTransient(pool))
                return null;

            foreach (var raid in _raids.Values.Concat(_occupying.Values))
                if (raid.Pool == pool && !raid.ByAfs)
                    return raid.Point;

            return null;
        }

        /// <summary>Whether a creature is one of a raid under way: what a gate's shield holds back and is struck at by.</summary>
        public bool IsRaider(Creature creature) =>
            creature?.SpawnPool != null && Reinforcements.IsTransient(creature.SpawnPool) && _raids.Values.Any(raid => raid.Pool == creature.SpawnPool);

        /// <summary>
        /// How many of the raiders of <paramref name="owner"/>'s side that took a point still stand on it: they
        /// hold it beside its garrison until they are killed (ControlPoints.GarrisonOf).
        /// </summary>
        public int Holders(MapChannel mapChannel, ControlPoints.Point point, byte owner)
        {
            return point != null && _occupying.TryGetValue(point.Id, out var raid) && raid.Map == mapChannel && raid.Side == owner
                ? Living(raid).Count
                : 0;
        }

        #region The worker

        /// <summary>Each second, from the map channel worker: the raids on a map's points, begun, run and ended.</summary>
        public void Worker(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || mapChannel.IsCopy)
                return;

            var now = Now();

            foreach (var point in ControlPoints.Instance.OnMap(mapChannel.MapInfo.MapContextId).ToList())
            {
                if (point.IsBattleground || point.Object == null || point.Map != mapChannel)
                    continue;

                Noticed(point, now);

                if (_raids.TryGetValue(point.Id, out var raid))
                {
                    Advance(raid, now);
                    continue;
                }

                if (_occupying.TryGetValue(point.Id, out var holders) && Living(holders).Count == 0)
                    _occupying.Remove(point.Id);

                // The Bane hold a point with no garrison of its own (the server started with it
                // theirs, the data gives it none): with raids on, it is given one to be raided from.
                if (!point.HeldByAfs && Config.AfsRaids && !_garrisons.ContainsKey(point.Id) && !ControlPoints.Instance.HasBaneGarrison(mapChannel, point))
                    Garrison(mapChannel, point, null);

                if (_garrisons.TryGetValue(point.Id, out var given))
                    Regroup(given.Map, given.Pool, point, LivingOf(given.Map, given.Pool), null);

                // Players at a point the Bane hold: the AFS fly troops in to help them take it.
                if (!point.HeldByAfs)
                    Support(mapChannel, point, now);

                // The side that does not hold the point raids it: the Bane a point of the AFS's, the AFS one of the Bane's.
                var side = Raider(point);

                if (!RaidsOn(side) || !_nextAt.TryGetValue(point.Id, out var due) || now < due)
                    continue;

                if (!CallersNear(mapChannel, point, Config.RaidRange))
                    continue;

                if (Begin(mapChannel, point) == null)
                    _nextAt[point.Id] = now + Between(side);
            }
        }

        /// <summary>Who would raid a point: whoever does not hold it.</summary>
        private static byte Raider(ControlPoints.Point point) => point.HeldByAfs ? ControlPoints.Bane : ControlPoints.Afs;

        private bool RaidsOn(byte side) => side == ControlPoints.Afs ? Config.AfsRaids : Config.Raids;

        /// <summary>How long after one raid of a side on a point the next comes.</summary>
        private long Between(byte side) => side == ControlPoints.Afs
            ? Minutes(Config.AfsRaidMinutesMin, Config.AfsRaidMinutesMax)
            : Minutes(Config.RaidMinutesMin, Config.RaidMinutesMax);

        /// <summary>The point is the raiding side's: taken, by the raid or otherwise.</summary>
        private static bool Taken(Raid raid) => Raider(raid.Point) != raid.Side;

        /// <summary>
        /// Keeps up with who holds a point, however it changed hands: the first raid after the server starts,
        /// and the other side's after one takes it - the Bane's raids on the AFS's points, the AFS's on the
        /// Bane's. Looked at by what is left of the other side's time there, not only by a change of owner
        /// seen between two passes: it may have changed hands twice in between.
        /// </summary>
        private void Noticed(ControlPoints.Point point, long now)
        {
            var had = _lastOwner.TryGetValue(point.Id, out var last) ? last : (byte?)null;

            _lastOwner[point.Id] = point.Owner;

            if (!point.HeldByAfs)
            {
                // The Bane have it (back): the AFS soldiers who held it go with its AFS garrison, and the
                // AFS come back for it later.
                var wasTheAfs = had != null && had != point.Owner;

                if (_occupying.TryGetValue(point.Id, out var afs) && afs.ByAfs)
                {
                    _occupying.Remove(point.Id);
                    ControlPoints.TakeOffLiving(afs.Map, new List<SpawnPool> { afs.Pool });
                    wasTheAfs = true;
                }

                if (wasTheAfs)
                    _nextAt[point.Id] = now + Math.Max(0, Config.AfsRaidAfterCaptureMinutes) * 60_000L;
                else if (had == null || !_nextAt.ContainsKey(point.Id) && !_raids.ContainsKey(point.Id))
                    _nextAt[point.Id] = now + Minutes(Config.AfsRaidFirstMinutesMin, Config.AfsRaidFirstMinutesMax);

                return;
            }

            // The AFS have it (back): whoever of a Bane raid still held it goes with the Bane garrison, a
            // garrison it was given is its no more, and those the AFS flew in to help take it make way for
            // its own garrison.
            var wasTheBanes = had != null && had != point.Owner;

            if (_occupying.TryGetValue(point.Id, out var holders) && !holders.ByAfs)
            {
                _occupying.Remove(point.Id);
                ControlPoints.TakeOffLiving(holders.Map, new List<SpawnPool> { holders.Pool });
                wasTheBanes = true;
            }

            if (_garrisons.Remove(point.Id, out var garrison))
            {
                ControlPoints.TakeOffLiving(garrison.Map, new List<SpawnPool> { garrison.Pool });
                ControlPoints.Instance.ReleaseBanePool(garrison.Map, point, garrison.Pool);
                wasTheBanes = true;
            }

            if (_support.Remove(point.Id, out var helpers))
            {
                ControlPoints.TakeOffLiving(helpers.Map, new List<SpawnPool> { helpers.Pool });
                wasTheBanes = true;
            }

            if (wasTheBanes)
                _nextAt[point.Id] = now + Math.Max(0, Config.RaidAfterCaptureMinutes) * 60_000L;
            else if (had == null || !_nextAt.ContainsKey(point.Id) && !_raids.ContainsKey(point.Id))
                _nextAt[point.Id] = now + Minutes(Config.RaidFirstMinutesMin, Config.RaidFirstMinutesMax);
        }

        private long Minutes(int min, int max)
        {
            min = Math.Max(0, min);
            max = Math.Max(min, max);

            lock (_random)
                return (long)((min + _random.NextDouble() * (max - min)) * 60_000);
        }

        #endregion

        #region A raid

        /// <summary>
        /// Begins a raid on a point now, whoever is near: by the Bane on a point the AFS hold, by the AFS on
        /// one the Bane hold; its troops chosen, the first wave's ships sent on the next pass - the AFS's after
        /// their coming is told (<see cref="RaidConfig.AfsRaidWarningSeconds"/>). Null when there is none to
        /// send - no troops are to be had for it - or one is under way already.
        /// </summary>
        public Raid Begin(MapChannel mapChannel, ControlPoints.Point point)
        {
            if (mapChannel == null || point?.Object == null || point.IsBattleground || _raids.ContainsKey(point.Id))
                return null;

            var side = Raider(point);
            var byAfs = side == ControlPoints.Afs;
            var troops = byAfs ? AfsTroopsFor(mapChannel, point) : TroopsFor(mapChannel, point);

            if (troops.Count == 0)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): no {(byAfs ? "AFS soldiers" : "Bane troops")} near enough to raid it.");
                return null;
            }

            var centre = point.Object.Position;
            var now = Now();
            var ramp = !byAfs && Config.RaidRampHours > 0 && point.ChangedAt > 0
                ? (int)Math.Clamp((ControlPoints.Instance.UtcNow() - point.ChangedAt) / (Config.RaidRampHours * 3_600_000L), 0, 3)
                : 0;
            var (wavesMin, wavesMax) = byAfs ? (Config.AfsRaidWavesMin, Config.AfsRaidWavesMax) : (Config.RaidWavesMin, Config.RaidWavesMax);
            var level = byAfs ? AfsLevelFor(mapChannel, point) : 0u;
            var warning = byAfs ? Math.Max(0, Config.AfsRaidWarningSeconds) * 1000L : 0L;
            var news = Array.FindIndex(Countdown, c => c.Before <= warning);

            int waves;
            double bearing;

            lock (_random)
            {
                waves = _random.Next(Math.Max(1, wavesMin), Math.Max(Math.Max(1, wavesMin), wavesMax) + 1);
                bearing = _random.NextDouble() * Math.PI * 2;
            }

            var pool = Reinforcements.TransientPool(mapChannel, centre, point.Object.Rotation, GroundRadius,
                troops.Select(id => new SpawnPoolSlot(id, 0, 0)));

            pool.Level = level;

            var raid = new Raid
            {
                Point = point,
                Map = mapChannel,
                Side = side,
                Level = level,
                Pool = pool,
                Troops = troops,
                Waves = waves,
                Squad = Math.Clamp(byAfs ? Config.AfsRaidSquadSize : Config.RaidSquadSize, 1, 12) + ramp,
                StartedAt = now,
                NextWaveAt = now + warning,
                Bearing = bearing,
                News = news >= 0 ? news : Countdown.Length
            };

            _raids[point.Id] = raid;
            _nextAt.Remove(point.Id);
            Besiege(raid);

            var walls = Reinforcements.Enclosed(mapChannel, centre) ? "; within walls: its ships come down outside them" : "";
            var gates = OutpostWalls.GatesOf(point).Count;

            if (byAfs)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the AFS raid it, {waves} wave(s) of {raid.Squad} a ship, soldiers {string.Join(", ", troops)}{(level > 0 ? $" at level {level}" : "")}, the first ships in {warning / 1000} s, {gates} gate(s){walls}.");

                // "AFS Reinforcements En Route - 2 minutes remaining", and so on to the ships.
                Countdowns(raid, now);
                return raid;
            }

            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the Bane raid it, {waves} wave(s) of {raid.Squad} a ship, troops {string.Join(", ", troops)}, {gates} gate(s){walls}.");

            // "Thrax are gathering to attack a friendly outpost. Reinforcements will arrive regularly
            // while any Thrax are alive, so kill as quickly as you can."
            Tell(raid, PlayerMessage.PmBRifWelcome, big: false);

            return raid;
        }

        /// <summary>A game master's end of a raid: its ships and what is alive of it go, and the next is set. Whether there was one.</summary>
        public bool Stop(uint pointId)
        {
            if (!_raids.Remove(pointId, out var raid))
                return false;

            InterruptClaim(raid);
            Relieve(raid);
            ControlPoints.TakeOffLiving(raid.Map, new List<SpawnPool> { raid.Pool });
            _nextAt[pointId] = Now() + Between(raid.Side);

            if (raid.ByAfs)
                Tell(raid, PlayerMessage.ARakshaAfsCalledBack, big: true);

            return true;
        }

        /// <summary>
        /// Forgets every raid, garrison given and help sent, as at a fresh start: for tests, and for a world
        /// being loaded again. Nothing on the maps is touched.
        /// </summary>
        public void Reset()
        {
            foreach (var raid in _raids.Values)
                Relieve(raid);

            _raids.Clear();
            _occupying.Clear();
            _garrisons.Clear();
            _support.Clear();
            _nextAt.Clear();
            _lastOwner.Clear();
        }

        /// <summary>
        /// A point under attack gets no reinforcements until the raid is over: its defending side's pools there
        /// (its garrison, a garrison it was given) are not restocked meanwhile (SpawnPool.Besieged).
        /// </summary>
        private void Besiege(Raid raid)
        {
            var point = raid.Point;
            var ids = raid.ByAfs ? point.BanePools : point.AfsPools;

            foreach (var pool in raid.Map.SpawnPools.Where(pool => pool != null && ids.Contains(pool.DbId)))
            {
                pool.Besieged = true;
                raid.Besieged.Add(pool);
            }

            if (raid.ByAfs && _garrisons.TryGetValue(point.Id, out var given) && given.Pool != null && !raid.Besieged.Contains(given.Pool))
            {
                given.Pool.Besieged = true;
                raid.Besieged.Add(given.Pool);
            }
        }

        /// <summary>The raid is over: the point's pools are restocked again.</summary>
        private static void Relieve(Raid raid)
        {
            foreach (var pool in raid.Besieged)
                pool.Besieged = false;

            raid.Besieged.Clear();
        }

        /// <summary>Calls a raid's next ships in so long; the AFS's are told of by the countdown ("En Route - 30 seconds remaining").</summary>
        private static void NextWave(Raid raid, long now, long inMs)
        {
            raid.NextWaveAt = now + Math.Max(0, inMs);

            if (!raid.ByAfs)
                return;

            var first = Array.FindIndex(Countdown, c => c.Before <= Math.Max(0, inMs));

            raid.News = first >= 0 ? first : Countdown.Length;
            Countdowns(raid, now);
        }

        /// <summary>Tells the players near an AFS raid's point how long until its next ships, as each step of <see cref="Countdown"/> comes.</summary>
        private static void Countdowns(Raid raid, long now)
        {
            var left = raid.NextWaveAt - now;
            var due = -1;

            // Only the last step reached: a pass that comes late tells no stale ones, nor any once the ships are coming.
            while (raid.News < Countdown.Length && Countdown[raid.News].Before >= left)
                due = raid.News++;

            if (due >= 0 && left > 0)
                Tell(raid, Countdown[due].Message, big: true);
        }

        private void Advance(Raid raid, long now)
        {
            var point = raid.Point;

            // Taken - by the raid's own claim, by the garrison falling, by a game master - or given
            // away: the raid is over, and those of it who stand hold the point.
            if (Taken(raid))
            {
                End(raid, taken: true);
                return;
            }

            // The AFS's coming told until their next ships.
            if (raid.ByAfs && raid.NextWaveAt != 0)
                Countdowns(raid, now);

            // A ship of the last wave that had no room: sent as soon as there is some near the point, given up after a while.
            if (raid.ShipsOwed > 0 && now >= raid.ShipOwedAt)
            {
                if (now - raid.WaveSentAt > ShipOwedMs)
                {
                    raid.ShipsOwed = 0;

                    if (raid.WaveShips == 0)
                    {
                        Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): no ship of wave {raid.Wave} could come down; the raid is called off.");
                        raid.Wave = raid.Waves;
                        raid.NextWaveAt = 0;
                    }
                }
                else
                {
                    if (SendShip(raid))
                    {
                        raid.ShipsOwed--;
                        raid.WaveShips++;
                    }

                    raid.ShipOwedAt = now + ShipRetryMs;
                }
            }

            var living = Living(raid);
            var inFlight = raid.Pool.DropshipQueue > 0 || raid.Pool.QueuedCreatures > 0 || raid.ShipsOwed > 0;
            var assault = raid.ByAfs && Config.AfsRaidWaveEverySeconds > 0;

            if (raid.NextWaveAt != 0 && now >= raid.NextWaveAt)
            {
                raid.NextWaveAt = 0;
                SendWave(raid, now);

                // The AFS's waves come one after another, an assault, not each as the last is worn down.
                if (assault && raid.Wave < raid.Waves)
                    NextWave(raid, now, Config.AfsRaidWaveEverySeconds * 1000L);
            }
            else if (!assault && raid.NextWaveAt == 0 && raid.Wave < raid.Waves && !inFlight
                     && (living.Count <= raid.Squad / 3 || now - raid.WaveSentAt >= WaveTimeoutMs))
                NextWave(raid, now, raid.ByAfs ? AfsWaveGapMs : Math.Max(0, Config.RaidWaveGapSeconds) * 1000L);

            Regroup(raid.Map, raid.Pool, raid.Point, living, raid.Claimer);
            Claim(raid, living, now);

            if (Taken(raid))
                return;

            Defend(raid, living);
            Breach(raid, living);

            if (raid.ByAfs)
                HealersFirst(raid, living);

            // Over when every wave is down - the ships of the last included, which may have just been sent.
            if (raid.Wave >= raid.Waves && raid.NextWaveAt == 0 && living.Count == 0 && raid.ShipsOwed == 0
                && raid.Pool.DropshipQueue <= 0 && raid.Pool.QueuedCreatures <= 0)
                End(raid, taken: false);
            else if (now - raid.StartedAt >= MaxRaidMs)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the {ControlPoints.FactionName(raid.Side)} raid ran out of time; {living.Count} raider(s) taken back.");
                InterruptClaim(raid);
                ControlPoints.TakeOffLiving(raid.Map, new List<SpawnPool> { raid.Pool });
                End(raid, taken: false);
            }
        }

        private void End(Raid raid, bool taken)
        {
            var point = raid.Point;

            InterruptClaim(raid);
            Relieve(raid);
            _raids.Remove(point.Id);

            // Whoever of the other side's last raid still held the point goes with the garrison it held it
            // beside (ControlPoints.ApplyPools takes that off): they are no pool of the point's.
            if (taken && _occupying.TryGetValue(point.Id, out var held) && held.Side != raid.Side)
            {
                _occupying.Remove(point.Id);
                ControlPoints.TakeOffLiving(held.Map, new List<SpawnPool> { held.Pool });
            }

            if (!taken)
            {
                _nextAt[point.Id] = Now() + Between(raid.Side);
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the {ControlPoints.FactionName(point.Owner)} held; {raid.Wave} {ControlPoints.FactionName(raid.Side)} wave(s) beaten off, {Standing(raid)} of theirs standing at it.");

                // "AFS Reinforcements Called Back".
                if (raid.ByAfs)
                    Tell(raid, PlayerMessage.ARakshaAfsCalledBack, big: true);

                return;
            }

            // Taken. The point's own garrison of the new side is flown in (ControlPoints.ApplyPools), and the
            // raiders still standing hold it beside it until they fall or it changes hands again. A point the
            // Bane took that has no Bane garrison of its own is given the raid's for one, and "a number of Bane
            // will be dropshipped in".
            if (raid.ByAfs || ControlPoints.Instance.HasBaneGarrison(raid.Map, point))
                _occupying[point.Id] = raid;
            else
                Garrison(raid.Map, point, raid);

            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}) was taken by the {ControlPoints.FactionName(raid.Side)} raid after {raid.Wave} wave(s); {Living(raid).Count} of it hold it.");
        }

        /// <summary>
        /// One wave: a ship per <see cref="Raid.Squad"/> - one, two from the third wave on (the Bane's), one
        /// more for every three players near - each down at the next gate of the point's outpost.
        /// </summary>
        private void SendWave(Raid raid, long now)
        {
            var players = CallersNearCount(raid.Map, raid.Point, AnnounceRange);
            var ships = 1 + (!raid.ByAfs && raid.Wave + 1 >= 3 ? 1 : 0) + Math.Min(2, Math.Max(0, players - 1) / 3);
            var sent = 0;

            raid.Wave++;
            raid.WaveSentAt = now;
            raid.ShipsOwed = 0;

            for (var i = 0; i < ships; i++)
            {
                if (SendShip(raid))
                    sent++;
                else
                    raid.ShipsOwed++;
            }

            raid.WaveShips = sent;
            raid.ShipOwedAt = now + ShipRetryMs;

            // A ship with nowhere to come down just now (the one before it still stands before the gate)
            // waits for room near the point (Advance) rather than being set down far from it.
            if (raid.ShipsOwed > 0)
                Logger.WriteLog(LogType.Debug, $"Control point {raid.Point.Id} ({raid.Point.Name}): {raid.ShipsOwed} ship(s) of wave {raid.Wave} wait for room near it.");

            // "THRAX REINFORCEMENTS ARE ARRIVING!!!" - the AFS's next ships were told of as they were called.
            if (sent > 0 && raid.Wave > 1 && !raid.ByAfs)
                Tell(raid, PlayerMessage.PmBRifReinforcements, big: true);
        }

        /// <summary>
        /// One ship of the raid's wave, down near the point: before a gate of the outpost, each ship at the next
        /// gate round (beside a ship that stands there already). A point with no gate that has room for a ship
        /// before it (or with no gate at all) is raided from round it instead: outside the walls of a point
        /// within them, never in its yard, and no further off than Reinforcements.RaidLandingFurthest. False
        /// when there is nowhere just now, or nobody to bring.
        /// </summary>
        private bool SendShip(Raid raid)
        {
            var map = raid.Map;
            var centre = raid.Point.Object.Position;

            lock (_random)
                raid.Bearing += 2.1 + _random.NextDouble() * 0.6;

            var spot = GateLanding(raid) ?? Reinforcements.RaidLanding(map, centre, LandingMin, LandingMax, raid.Bearing);

            if (spot == null)
            {
                Logger.WriteLog(LogType.Debug, $"Control point {raid.Point.Id} ({raid.Point.Name}): nowhere outside its walls within {Reinforcements.RaidLandingFurthest:0} m with a way on foot to it for a ship of wave {raid.Wave} just now.");
                return false;
            }

            var squad = Squad(raid);

            if (squad.Count == 0)
                return false;

            var facing = Math.Atan2(-(centre.X - spot.Value.X), -(centre.Z - spot.Value.Z));
            var ship = Reinforcements.Drop(map, raid.Pool, squad, spot.Value, facing, raid.ByAfs ? TargetCategory.Friendly : TargetCategory.Hostile);

            if (ship == null)
                return false;

            Logger.WriteLog(LogType.Debug, $"Control point {raid.Point.Id} ({raid.Point.Name}): {(raid.ByAfs ? "AFS " : "")}wave {raid.Wave}, ship {ship.EntityId} with {squad.Count} down at ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}), {Vector3.Distance(spot.Value, centre):0} m out.");

            return true;
        }

        /// <summary>
        /// Where the raid's next ship comes down before a gate of the point's outpost (Reinforcements.GateLanding):
        /// the gates taken in turn, ship after ship, so that a wave of several comes at several entrances. Null
        /// when the point has no gate with room for a ship before it.
        /// </summary>
        internal static Vector3? GateLanding(Raid raid)
        {
            var gates = OutpostWalls.GatesOf(raid.Point);
            var centre = raid.Point.Object.Position;

            for (var tried = 0; tried < gates.Count; tried++)
            {
                var gate = gates[(raid.Gate + tried) % gates.Count];
                var spot = Reinforcements.GateLanding(raid.Map, centre, gate.Middle, gate.Through);

                if (spot == null)
                    continue;

                raid.Gate = (raid.Gate + tried + 1) % gates.Count;

                return spot;
            }

            return null;
        }

        /// <summary>
        /// The point's defenders take the raid on: whoever of the side that holds it stands within
        /// <see cref="RaidConfig.RaidDefendRange"/> of it and is not fighting already goes for the nearest raider
        /// within that range of it, and the nearest few (<see cref="RaidConfig.RaidDefendersOnClaimer"/>) for a
        /// raider claiming it. Their own aggro range (a creature's, some 18 m) left a garrison standing while a
        /// raid walked past it to the point.
        /// </summary>
        private void Defend(Raid raid, List<Creature> living)
        {
            var range = Config.RaidDefendRange;
            var at = raid.Point.Object?.Position ?? raid.Point.Position;

            if (range <= 0)
                return;

            var raiders = living.Where(c => Vector3.Distance(c.Position, at) <= range).ToList();

            if (raiders.Count == 0)
                return;

            var claimer = raid.Claimer;
            var defenders = DefendersOf(raid, at, range);
            var onClaimer = claimer != null
                ? defenders.OrderBy(d => Vector3.DistanceSquared(d.Position, claimer.Position)).Take(Math.Max(0, Config.RaidDefendersOnClaimer)).ToHashSet()
                : new HashSet<Creature>();

            foreach (var defender in defenders)
            {
                if (onClaimer.Contains(defender))
                {
                    if (!IsFighting(defender) || defender.Controller.ActionFighting.TargetEntityId != claimer.EntityId)
                        BehaviorManager.Instance.TrySetActionFighting(defender, claimer.EntityId);

                    continue;
                }

                if (IsFighting(defender))
                    continue;

                var nearest = raiders.OrderBy(r => Vector3.DistanceSquared(r.Position, defender.Position)).First();

                BehaviorManager.Instance.TrySetActionFighting(defender, nearest.EntityId);
            }
        }

        /// <summary>How near the middle of a gate shield's sheet a raider it holds back stands to strike at it (metres).</summary>
        public const float BreachRange = 10f;

        /// <summary>
        /// Has a raider strike at a shield now with its own attack, through the ordinary damage path (a missile
        /// at the field's entity, ForceFields.TakeHit), its cooldowns respected; whether it struck. Ours does
        /// nothing: upstream's fields take hits only from players. The control point defences' own
        /// (ControlPointFields.Strike) is what it is meant to be. Replaceable for tests.
        /// </summary>
        public Func<MapChannel, Creature, ForceFields.Field, bool> StrikeShield { get; set; } = (map, creature, field) => false;

        /// <summary>
        /// A raid at the point's gates breaks its way in: each raider not in a fight already that stands within
        /// <see cref="BreachRange"/> of a gate shield of the point that holds it back - an AFS shield against
        /// the Bane, a Bane one against the AFS - strikes at it (<see cref="StrikeShield"/>). One in a fight
        /// strikes at a shield in its way as its fight has it.
        /// </summary>
        private void Breach(Raid raid, List<Creature> living)
        {
            if (living.Count == 0)
                return;

            var shields = (OutpostWalls.FieldsOf(raid.Point) ?? Array.Empty<ForceFields.Field>())
                .Where(field => field?.Object != null && field.Class != null && !field.IsDestroyed)
                .Select(field => (Field: field, Middle: OutpostWalls.Gate.Of(field).Middle))
                .ToList();

            if (shields.Count == 0)
                return;

            foreach (var raider in living)
            {
                if (IsFighting(raider))
                    continue;

                var held = shields
                    .Where(s => ForceFields.StopsCategory(s.Field.Side, raider.TargetCategory))
                    .Select(s => (s.Field, Distance: Vector2.Distance(new Vector2(raider.Position.X, raider.Position.Z), new Vector2(s.Middle.X, s.Middle.Z))))
                    .Where(s => s.Distance <= BreachRange)
                    .OrderBy(s => s.Distance)
                    .Select(s => s.Field)
                    .FirstOrDefault();

                if (held != null)
                    StrikeShield(raid.Map, raider, held);
            }
        }

        private static bool Mends(Creature creature) => creature.Actions.Any(action => action != null && BaneMenders.Contains(action.ActionId));

        /// <summary>
        /// An AFS raid's soldiers near the point go for the Bane's healers first, as players do: a garrison with
        /// a Caretaker in it raised its dead behind every wave, and no soldier ever stood at the point. Each
        /// soldier within <see cref="RaidConfig.RaidDefendRange"/> of the point not already fighting a healer
        /// turns on the nearest healer within that range of it.
        /// </summary>
        private void HealersFirst(Raid raid, List<Creature> living)
        {
            var range = Config.RaidDefendRange;
            var at = raid.Point.Object?.Position ?? raid.Point.Position;

            if (range <= 0 || living.Count == 0)
                return;

            var healers = DefendersOf(raid, at, range).Where(Mends).ToList();

            if (healers.Count == 0)
                return;

            foreach (var soldier in living)
            {
                if (Vector3.Distance(soldier.Position, at) > range)
                    continue;

                if (IsFighting(soldier) && healers.Any(h => h.EntityId == soldier.Controller.ActionFighting.TargetEntityId))
                    continue;

                var nearest = healers.OrderBy(h => Vector3.DistanceSquared(h.Position, soldier.Position)).First();

                BehaviorManager.Instance.TrySetActionFighting(soldier, nearest.EntityId);
            }
        }

        /// <summary>
        /// Who defends a raided point: the creatures of the side that holds it within range of it, alive and with
        /// something to fight with - for the AFS's point its soldiers, its armed staff, its turrets, soldiers who
        /// took it back; for the Bane's its garrison, and whoever of a raid holds it. No one's minion, and none of
        /// the raid's own.
        /// </summary>
        private static List<Creature> DefendersOf(Raid raid, Vector3 at, float range)
        {
            var side = raid.ByAfs ? TargetCategory.Hostile : TargetCategory.Friendly;

            return CreaturesNear(raid.Map, at, range)
                .Where(c => c != null && c.TargetCategory == side && c.SpawnPool != raid.Pool && c.MasterEntityId == 0
                            && c.State != CharacterState.Dead && c.Controller != null && c.Actions.Count > 0
                            && c.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                            && Vector3.Distance(c.Position, at) <= range
                            && (!raid.ByAfs || c.SpawnPool != null && (raid.Point.BanePools.Contains(c.SpawnPool.DbId) || Reinforcements.IsTransient(c.SpawnPool))
                                || Reinforcements.IsBaneTroop(c)))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Sends whoever of a raid (or of a garrison it left) stands about away from the point back to it: a fight
        /// on the way leaves a creature wandering where it ended, and the ground round the point is theirs, not
        /// that spot.
        /// </summary>
        private static void Regroup(MapChannel mapChannel, SpawnPool pool, ControlPoints.Point point, List<Creature> living, Creature claimer)
        {
            var at = point.Object?.Position ?? point.Position;

            foreach (var creature in living)
            {
                if (creature == claimer || creature.Controller?.CurrentAction != BehaviorManager.BehaviorActionWander
                    || creature.Controller.ActionWander.State != BehaviorManager.WanderIdle || creature.WalkSpeed < 0.01f)
                    continue;

                if (Vector3.Distance(creature.Position, at) <= pool.Radius + 3f)
                    continue;

                BehaviorManager.Instance.WalkIn(creature, SpawnPoolManager.SpawnPoint(mapChannel, pool, 1));
            }
        }

        /// <summary>One ship's troops: the raid's kinds, the first of them the most, as templates (one entry each).</summary>
        private List<Creature> Squad(Raid raid)
        {
            var squad = new List<Creature>();
            var creatures = CreatureManager.Instance.LoadedCreatures;

            for (var i = 0; i < raid.Squad; i++)
            {
                uint id;

                lock (_random)
                    id = i == 0 || raid.Troops.Count == 1 || _random.NextDouble() < 0.45 ? raid.Troops[0] : raid.Troops[1 + _random.Next(raid.Troops.Count - 1)];

                if (creatures.TryGetValue(id, out var template) && template != null)
                    squad.Add(template);
            }

            return squad;
        }

        /// <summary>
        /// Gives a point the Bane hold, that has no Bane garrison of its own, one: a pool of no table's round its
        /// object, of the troops a raid on it would bring, that comes back together like any garrison
        /// (ControlPoints.HoldsBack) and goes when the AFS take the point. Made of a raid that took the point - its
        /// raiders still standing are its first, and a ship brings the rest at once - or, with none, set down on the
        /// spawn worker's next pass.
        /// </summary>
        private void Garrison(MapChannel mapChannel, ControlPoints.Point point, Raid raid)
        {
            var troops = raid?.Troops ?? TroopsFor(mapChannel, point);

            if (troops.Count == 0 || point.Object == null)
                return;

            var counts = new (short Min, short Max)[] { (2, 3), (1, 2), (1, 1), (0, 1) };
            var slots = troops.Take(counts.Length).Select((id, i) => new SpawnPoolSlot(id, counts[i].Min, counts[i].Max)).ToList();
            var pool = raid?.Pool ?? Reinforcements.TransientPool(mapChannel, point.Object.Position, point.Object.Rotation, GarrisonRadius, slots);

            pool.SpawnSlot = slots;
            pool.Radius = GarrisonRadius;
            pool.Mode = SpawnPoolManager.ModeControlPoint;
            pool.IsGarrison = true;
            pool.Suspended = false;
            pool.RespawnTime = GarrisonRespawnMs;
            pool.HasSpawned = raid != null;
            pool.UpdateTimer = raid != null ? 0 : pool.RespawnTime;

            ControlPoints.Instance.AdoptBanePool(mapChannel, point, pool);
            _garrisons[point.Id] = (mapChannel, pool);

            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}) is given a Bane garrison of {string.Join(", ", troops)}{(raid != null ? $", {Living(raid).Count} of the raid's own in it" : "")}.");

            if (raid == null)
                return;

            var centre = point.Object.Position;

            // Before a gate or round the point outside its walls, as the raid's own ships came - never in its yard.
            var spot = GateLanding(raid) ?? Reinforcements.RaidLanding(mapChannel, centre, 30f, 60f, raid.Bearing + 1.0);

            if (spot != null)
            {
                Reinforcements.Drop(mapChannel, pool, Squad(raid), spot.Value, Math.Atan2(-(centre.X - spot.Value.X), -(centre.Z - spot.Value.Z)), TargetCategory.Hostile);
                return;
            }

            // Nowhere outside just now: the rest of it comes on the spawn worker's next pass, by ship outside the
            // walls when there is room then (Reinforcements.ArrivalFor), or beamed in.
            pool.ShipNextArrival = true;
            pool.UpdateTimer = pool.RespawnTime;
        }

        /// <summary>
        /// While players are at a point the Bane hold and its garrison stands, an AFS ship brings some of the
        /// point's own AFS soldiers every few minutes, to the point's ground, to fight with them. A point whose AFS
        /// garrison has no soldiers (only its vendors and staff) gets none.
        /// </summary>
        private void Support(MapChannel mapChannel, ControlPoints.Point point, long now)
        {
            if (!Config.AfsSupport || !Reinforcements.Config.AfsDropships || point.Object == null || _raids.ContainsKey(point.Id)
                || ControlPoints.Instance.GarrisonOf(mapChannel, point, ControlPoints.Bane) != ControlPoints.Garrison.Standing
                || !CallersNear(mapChannel, point, SupportRange))
                return;

            if (!_support.TryGetValue(point.Id, out var support))
            {
                var soldiers = SoldiersOf(mapChannel.SpawnPools.Where(pool => pool != null && point.AfsPools.Contains(pool.DbId)));

                if (soldiers.Count == 0)
                    return;

                var pool = Reinforcements.TransientPool(mapChannel, point.Object.Position, point.Object.Rotation, GroundRadius + 4f,
                    soldiers.Select(id => new SpawnPoolSlot(id, 0, 0)));

                // The first ship a little after they come: the AFS have to see them at it.
                support = (mapChannel, pool, now + 20_000);
                _support[point.Id] = support;
            }

            var helpers = LivingOf(mapChannel, support.Pool);

            Regroup(mapChannel, support.Pool, point, helpers, null);

            if (now < support.NextAt || support.Pool.DropshipQueue > 0 || helpers.Count >= SupportMost)
                return;

            _support[point.Id] = (support.Map, support.Pool, now + SupportEveryMs);

            var centre = point.Object.Position;
            double bearing;

            lock (_random)
                bearing = _random.NextDouble() * Math.PI * 2;

            // They come to take it, as a raid does: outside the walls of a point within them.
            var spot = Reinforcements.RaidLanding(mapChannel, centre, 30f, 60f, bearing);

            if (spot == null)
                return;

            var templates = CreatureManager.Instance.LoadedCreatures;
            var squad = support.Pool.SpawnSlot
                .Select(slot => templates.TryGetValue(slot.CreatureId, out var t) ? t : null)
                .Where(t => t != null)
                .ToList();

            if (squad.Count == 0)
                return;

            var troops = Enumerable.Range(0, Math.Min(SupportShip, SupportMost - helpers.Count)).Select(i => squad[i % squad.Count]).ToList();
            var ship = Reinforcements.Drop(mapChannel, support.Pool, troops, spot.Value,
                Math.Atan2(-(centre.X - spot.Value.X), -(centre.Z - spot.Value.Z)), TargetCategory.Friendly);

            if (ship != null)
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): an AFS ship {ship.EntityId} brings {troops.Count} to help take it, down at ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}).");
        }

        /// <summary>The point's own Bane garrison troops, or the Bane troops that stand nearest it.</summary>
        private static List<uint> TroopsFor(MapChannel mapChannel, ControlPoints.Point point)
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;
            var own = mapChannel.SpawnPools
                .Where(pool => pool != null && point.BanePools.Contains(pool.DbId) && pool.SpawnSlot != null && !Reinforcements.IsTransient(pool))
                .SelectMany(pool => pool.SpawnSlot)
                .Where(slot => slot != null && slot.CountMax > 0)
                .OrderByDescending(slot => slot.CountMax)
                .Select(slot => slot.CreatureId)
                .Distinct()
                .Where(id => creatures.TryGetValue(id, out var t) && Reinforcements.IsBaneTroop(t) && t.WalkSpeed > 0.01f && !Reinforcements.IsBoss(t))
                .Take(4)
                .ToList();

            return own.Count > 0 ? own : Reinforcements.TroopsNear(mapChannel, point.Object?.Position ?? point.Position, 900f, 4);
        }

        /// <summary>The AFS soldier rows of some pools, the commonest first, no boss, up to four.</summary>
        private static List<uint> SoldiersOf(IEnumerable<SpawnPool> pools)
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;

            return pools
                .Where(pool => pool?.SpawnSlot != null)
                .SelectMany(pool => pool.SpawnSlot)
                .Where(slot => slot != null && slot.CountMax > 0 && IsSoldier(creatures, slot.CreatureId))
                .GroupBy(slot => slot.CreatureId)
                .OrderByDescending(group => group.Sum(slot => slot.CountMax))
                .Select(group => group.Key)
                .Take(4)
                .ToList();
        }

        private static bool IsSoldier(IReadOnlyDictionary<uint, Creature> creatures, uint id) =>
            creatures.TryGetValue(id, out var t) && t != null && t.TargetCategory == TargetCategory.Friendly
            && Reinforcements.IsAfsTroop(t) && !Reinforcements.IsBoss(t);

        /// <summary>
        /// The AFS soldiers a raid on a point the Bane hold brings: the point's own AFS garrison's soldiers (its AFS
        /// pools), or those of the AFS soldier pools nearest it on its map, or the AFS's light and heavy soldiers.
        /// Their rows' levels do not matter: they are brought to the raid's (<see cref="AfsLevelFor"/>).
        /// </summary>
        private static List<uint> AfsTroopsFor(MapChannel mapChannel, ControlPoints.Point point)
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;
            var own = SoldiersOf(mapChannel.SpawnPools.Where(pool => pool != null && point.AfsPools.Contains(pool.DbId)));

            if (own.Count > 0)
                return own;

            var at = point.Object?.Position ?? point.Position;
            var near = SoldiersOf(mapChannel.SpawnPools
                .Where(pool => pool?.SpawnSlot != null && !Reinforcements.IsTransient(pool) && pool.Mode != SpawnPoolManager.ModeScripted
                               && pool.SpawnSlot.Any(slot => slot != null && slot.CountMax > 0 && IsSoldier(creatures, slot.CreatureId))
                               && Vector3.Distance(pool.Position, at) <= 900f)
                .OrderBy(pool => Vector3.DistanceSquared(pool.Position, at))
                .Take(8));

            return near.Count > 0 ? near : AfsSoldierRows.Where(id => IsSoldier(creatures, id)).ToList();
        }

        /// <summary>
        /// The level an AFS raid's soldiers come at: <see cref="RaidConfig.AfsRaidLevelsBelow"/> under the middle
        /// level of the point's Bane - its own Bane garrison's troops, one each of a pool's most, or the troops a
        /// Bane raid on it would bring. 0 when there are none to go by.
        /// </summary>
        public uint AfsLevelFor(MapChannel mapChannel, ControlPoints.Point point)
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;
            var levels = mapChannel.SpawnPools
                .Where(pool => pool?.SpawnSlot != null && point.BanePools.Contains(pool.DbId))
                .SelectMany(pool => pool.SpawnSlot)
                .Where(slot => slot != null && slot.CountMax > 0 && creatures.TryGetValue(slot.CreatureId, out var t) && t != null && !Reinforcements.IsBoss(t))
                .SelectMany(slot => Enumerable.Repeat((int)creatures[slot.CreatureId].Level, slot.CountMax))
                .ToList();

            if (levels.Count == 0)
                levels = TroopsFor(mapChannel, point)
                    .Where(id => creatures.TryGetValue(id, out var t) && t != null)
                    .Select(id => (int)creatures[id].Level)
                    .ToList();

            if (levels.Count == 0)
                return 0;

            levels.Sort();

            return (uint)Math.Max(1, levels[(levels.Count - 1) / 2] - Config.AfsRaidLevelsBelow);
        }

        #endregion

        #region The claim

        private void Claim(Raid raid, List<Creature> living, long now)
        {
            var obj = raid.Point.Object;

            // Who has been hurt, and when: taken from their health and armour as they go.
            foreach (var creature in living)
            {
                var toughness = Toughness(creature);

                raid.Hurt[creature.EntityId] = raid.Hurt.TryGetValue(creature.EntityId, out var was)
                    ? (toughness, toughness < was.Toughness ? now : was.HurtAt)
                    : (toughness, 0L);
            }

            foreach (var gone in raid.Hurt.Keys.Where(id => living.All(c => c.EntityId != id)).ToList())
                raid.Hurt.Remove(gone);

            if (raid.Claimer != null)
            {
                var claimer = raid.Claimer;
                var toughness = Toughness(claimer);
                var bane = raid.ByAfs ? BaneAt(raid) : null;
                var why = claimer.State == CharacterState.Dead || !living.Contains(claimer) ? "dead"
                    : obj == null || obj.UsedBy != claimer ? "the object taken by somebody else"
                    : !InReach(claimer, obj) ? "gone from the point"
                    : toughness < raid.ClaimToughness ? $"hurt ({raid.ClaimToughness} -> {toughness})"
                    : IsFighting(claimer) ? $"fighting {claimer.Controller.ActionFighting.TargetEntityId}"
                    : bane != null ? $"Bane {bane.EntityId} at the point"
                    : null;

                if (why != null)
                {
                    Logger.WriteLog(LogType.Debug, $"Control point {raid.Point.Id} ({raid.Point.Name}): claim by {(raid.ByAfs ? "soldier" : "raider")} {claimer.EntityId} interrupted after {(now - raid.ClaimStartedAt) / 1000.0:0.0} s: {why}.");
                    InterruptClaim(raid);
                    return;
                }

                raid.ClaimToughness = toughness;

                if (now - raid.ClaimStartedAt < ClaimMs(raid))
                    return;

                Release(raid, interrupted: false);
                Logger.WriteLog(LogType.Debug, $"Control point {raid.Point.Id} ({raid.Point.Name}) claimed by {(raid.ByAfs ? "soldier" : "raider")} {claimer.EntityId}.");

                // Through the point's own change of hands: the garrison that takes it over is flown in
                // (ControlPoints.ApplyPools), and the raid is over - those of it who stand hold the point.
                ControlPoints.Instance.SetOwner(raid.Point, raid.Side, null);

                if (Taken(raid))
                    End(raid, taken: true);

                return;
            }

            // Somebody else is at it - a player's own use - or it is not on the map.
            if (obj == null || !obj.IsInWorld || obj.UsedBy != null)
                return;

            // The AFS clear the point's ground before they claim it, with the rule on; the Bane do not wait.
            if (raid.ByAfs && BaneAt(raid) != null)
                return;

            var next = living.Where(c => InReach(c, obj) && !IsFighting(c) && !IsWalking(c)
                                         && (!raid.Hurt.TryGetValue(c.EntityId, out var hurt) || now - hurt.HurtAt >= UnhurtMs))
                .OrderBy(c => Vector3.DistanceSquared(c.Position, obj.Position))
                .FirstOrDefault();

            if (next != null)
                StartClaim(raid, next, now);
        }

        /// <summary>How many of the side holding a raided point stand within its defence range (for the log).</summary>
        private int Standing(Raid raid) =>
            DefendersOf(raid, raid.Point.Object?.Position ?? raid.Point.Position, Math.Max(1, Config.RaidDefendRange)).Count;

        /// <summary>How long a raider of the raid must stand at the point to take it.</summary>
        private long ClaimMs(Raid raid) =>
            Math.Max(1, raid.ByAfs ? Config.AfsRaidClaimSeconds : Config.RaidClaimSeconds) * 1000L;

        /// <summary>
        /// A Bane troop alive within <see cref="RaidConfig.AfsRaidClearRange"/> of the point's object - one of its
        /// garrison, of a raid that took it, or any Bane soldier - or null: while one stands there no AFS soldier
        /// claims the point.
        /// </summary>
        private Creature BaneAt(Raid raid)
        {
            var at = raid.Point.Object?.Position ?? raid.Point.Position;
            var range = Math.Max(0, Config.AfsRaidClearRange);

            if (range <= 0)
                return null;

            return CreaturesNear(raid.Map, at, range)
                .Where(c => c != null && c.State != CharacterState.Dead && c.TargetCategory == TargetCategory.Hostile
                            && c.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                            && Vector3.Distance(c.Position, at) <= range
                            && (c.SpawnPool != null && (raid.Point.BanePools.Contains(c.SpawnPool.DbId) || Reinforcements.IsTransient(c.SpawnPool))
                                || Reinforcements.IsBaneTroop(c)))
                .OrderBy(c => Vector3.DistanceSquared(c.Position, at))
                .FirstOrDefault();
        }

        private static void StartClaim(Raid raid, Creature claimer, long now)
        {
            var obj = raid.Point.Object;
            var map = raid.Map;

            raid.Claimer = claimer;
            raid.ClaimStartedAt = now;
            raid.ClaimToughness = Toughness(claimer);

            // It stands at it for the claim: its next stroll is a full wait away.
            if (claimer.Controller?.ActionWander != null)
                claimer.Controller.ActionWander.IdleMs = 0;

            // What a claim looks like (DynamicObjectManager's use of an object): the object locked to the
            // claimant, its contested effect on, and the claimant at it with the use's windup.
            obj.UsedBy = claimer;
            CellManager.Instance.CellCallMethod(obj, new LockToActorPacket(claimer.EntityId));
            CellManager.Instance.CellCallMethod(obj, UseInterruptible(obj, claimer, raid.Side));
            CellManager.Instance.CellCallMethod(map, claimer,
                new PerformWindupPacket(PerformType.ThreeArgs, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId, obj.EntityId));

            // "Bane claiming Control Point ..." ("AFS claiming ...") - once, for a claim begun again and again under fire.
            if (raid.ToldClaimAt == 0 || now - raid.ToldClaimAt >= ClaimNewsMs)
            {
                raid.ToldClaimAt = now;
                ControlPoints.Instance.ClaimingFor(map, raid.Point, raid.Side);
            }
        }

        private static void InterruptClaim(Raid raid)
        {
            if (raid.Claimer != null)
                Release(raid, interrupted: true);
        }

        private static void Release(Raid raid, bool interrupted)
        {
            var claimer = raid.Claimer;
            var obj = raid.Point.Object;

            raid.Claimer = null;

            if (claimer == null)
                return;

            if (claimer.State != CharacterState.Dead && claimer.RuntimeMapChannel == raid.Map)
                CellManager.Instance.CellCallMethod(raid.Map, claimer,
                    new ActionInterruptPacket(claimer.EntityId, ActionId.UseObject, DynamicObjectManager.ControlPointUseArgId));

            if (obj == null || obj.UsedBy != claimer)
                return;

            obj.UsedBy = null;

            if (!obj.IsInWorld)
                return;

            if (interrupted)
                CellManager.Instance.CellCallMethod(obj, new UseInterruptedPacket(claimer.EntityId));

            CellManager.Instance.CellCallMethod(obj, new LockToActorPacket(0));
        }

        /// <summary>UseInterruptible for a raider at a point: a clan's point is told whose raiders are at it, the Bane or the AFS.</summary>
        private static UseInterruptiblePacket UseInterruptible(DynamicObject obj, Creature claimer, byte side)
        {
            return obj.EntityClassId == ControlPoints.ClanPointClass
                ? new UseInterruptiblePacket(claimer.EntityId, side == ControlPoints.Bane ? ControlPoints.VirtualClanBane : ControlPoints.VirtualClanAfs)
                : new UseInterruptiblePacket(claimer.EntityId);
        }

        private static bool InReach(Creature creature, DynamicObject obj)
        {
            if (creature == null || obj == null)
                return false;

            var dx = creature.Position.X - obj.Position.X;
            var dz = creature.Position.Z - obj.Position.Z;

            return dx * dx + dz * dz <= ClaimRange * ClaimRange && Math.Abs(creature.Position.Y - obj.Position.Y) <= 4f;
        }

        private static bool IsFighting(Creature creature) =>
            creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting;

        private static bool IsWalking(Creature creature) =>
            creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionWander
            && creature.Controller.ActionWander.State == BehaviorManager.WanderMoving;

        private static int Toughness(Creature creature)
        {
            var health = creature.Attributes.TryGetValue(Attributes.Health, out var h) ? h.Current : 0;
            var armor = creature.Attributes.TryGetValue(Attributes.Armor, out var a) ? a.Current : 0;

            return health + armor;
        }

        #endregion

        #region Who is where

        /// <summary>For a game master: each raider alive, where it is from the point and what it is doing.</summary>
        public List<string> Describe(Raid raid)
        {
            var lines = new List<string>();

            if (raid == null)
                return lines;

            var at = raid.Point.Object?.Position ?? raid.Point.Position;

            foreach (var c in Living(raid))
            {
                var action = c.Controller?.CurrentAction switch
                {
                    BehaviorManager.BehaviorActionFighting => $"fighting {c.Controller.ActionFighting.TargetEntityId}",
                    BehaviorManager.BehaviorActionWander => c.Controller.ActionWander.State == BehaviorManager.WanderMoving
                        ? (c.Controller.ActionWander.Arriving ? "marching in" : "strolling") : "standing",
                    BehaviorManager.BehaviorActionReturning => "going home",
                    var other => $"action {other}"
                };

                lines.Add($"  {c.EntityId} {c.Name} lv{c.Level}: {Vector3.Distance(c.Position, at):0} m from the point, {action}{(c == raid.Claimer ? ", claiming" : "")}");
            }

            return lines;
        }

        /// <summary>How many of a raid's creatures are alive, and how many more are on their way in ships.</summary>
        public (int Alive, int Coming) Count(Raid raid) =>
            raid == null ? (0, 0) : (Living(raid).Count, Math.Max(0, raid.Pool.QueuedCreatures));

        /// <summary>
        /// The creatures of the map's cells within <paramref name="range"/> of a spot, by the cells' squares (the
        /// caller measures the distance): a raid's tick looks round its point, not at the whole map.
        /// </summary>
        private static IEnumerable<Creature> CreaturesNear(MapChannel mapChannel, Vector3 at, float range)
        {
            var cells = mapChannel?.MapCellInfo?.Cells;

            if (cells == null)
                yield break;

            var size = CellManager.CellSize;
            var x0 = (uint)((at.X - range) / size + CellManager.CellBias);
            var x1 = (uint)((at.X + range) / size + CellManager.CellBias);
            var z0 = (uint)((at.Z - range) / size + CellManager.CellBias);
            var z1 = (uint)((at.Z + range) / size + CellManager.CellBias);

            for (var x = x0; x <= x1; x++)
                for (var z = z0; z <= z1; z++)
                    if (cells.TryGetValue((x & 0xFFFF) | (z << 16), out var cell) && cell?.CreatureList != null)
                        foreach (var creature in cell.CreatureList)
                            yield return creature;
        }

        /// <summary>The raid's creatures that are alive, on its map.</summary>
        private static List<Creature> Living(Raid raid) => LivingOf(raid.Map, raid.Pool);

        /// <summary>A pool's creatures that are alive, on a map.</summary>
        private static List<Creature> LivingOf(MapChannel mapChannel, SpawnPool pool)
        {
            var cells = mapChannel?.MapCellInfo?.Cells;

            if (cells == null || pool == null || pool.AliveCreatures <= 0 && pool.QueuedCreatures <= 0)
                return new List<Creature>();

            return cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(c => c != null && c.SpawnPool == pool && c.State != CharacterState.Dead)
                .Distinct()
                .ToList();
        }

        private static IEnumerable<Client> ClientsNear(MapChannel mapChannel, ControlPoints.Point point, float range)
        {
            var at = point.Object?.Position ?? point.Position;

            return (mapChannel?.ClientList ?? new List<Client>()).ToArray()
                .Where(client => client?.Player != null && client.State == ClientState.Ingame
                                 && (range <= 0 || Vector2.Distance(new Vector2(client.Player.Position.X, client.Player.Position.Z), new Vector2(at.X, at.Z)) <= range));
        }

        /// <summary>
        /// The players near a point who call a raid, or the AFS's help, to it: those playing - who can be hurt (not
        /// under .immune) and are not AFK - unless <see cref="RaidConfig.RaidCallersMustBePlaying"/> is off.
        /// </summary>
        private IEnumerable<Client> CallersOf(MapChannel mapChannel, ControlPoints.Point point, float range) =>
            ClientsNear(mapChannel, point, range)
                .Where(client => !Config.RaidCallersMustBePlaying || !client.Player.ImmuneToAllDamage && !client.Player.IsAFK);

        private bool CallersNear(MapChannel mapChannel, ControlPoints.Point point, float range) =>
            CallersOf(mapChannel, point, range).Any();

        private int CallersNearCount(MapChannel mapChannel, ControlPoints.Point point, float range) =>
            CallersOf(mapChannel, point, range).Count();

        /// <summary>A raid's news to the players near its point: across the middle of the screen, or as a line of the Defense chat.</summary>
        private static void Tell(Raid raid, PlayerMessage message, bool big)
        {
            foreach (var client in ClientsNear(raid.Map, raid.Point, AnnounceRange))
            {
                if (big)
                    client.CallMethod(SysEntity.ClientMethodId, new DisplayPlayerNotificationPacket(PlayerNotificationType.Big, message));
                else
                    client.CallMethod(SysEntity.ClientMethodId, new DisplaySystemMessagePacket(message, null, MsgFilterId.Defense));
            }
        }

        #endregion
    }
}
