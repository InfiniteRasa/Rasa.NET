using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;
    using Structures.World;

    /// <summary>
    /// A Bane base's intrusion alarm: its security cameras see a player, or its databanks are
    /// smashed, and reinforcements come through its teleporters.
    ///
    /// The cameras: UsableInertDestBaneCameraV01 (10290), a destructible the server places
    /// (world_destructible, PlacedDestructibles). A standing one looks every <see cref="LookEveryMs"/>
    /// for a living player within <see cref="CameraRange"/> and <see cref="CameraHalfAngle"/>
    /// degrees of the way it faces, not hidden (Detection), and in the clear of the cover mesh
    /// from its lens (Cover.Visible). The first sighting sounds the alarm and brings a wave; a
    /// camera that has seen someone and still stands brings another every <see cref="WaveEveryMs"/>,
    /// so the way to go in quietly is to shoot the cameras first, as the walkthroughs of the live
    /// game say ("if you want to avoid a little chaos take out the two security cameras before you
    /// walk in"). A camera that is down sees nothing, and one that comes back (a respawn) starts
    /// afresh.
    ///
    /// The databanks: destructibles of the site's <see cref="Site.DatabankClasses"/>. The first one
    /// destroyed sounds the alarm and brings the site's exit wave once: Thrax on the teleporters
    /// along the way out ("Destroying the monitoring machines triggers an alarm. Assorted thrax
    /// will spawn on some of the teleporters on the way out of the base.").
    ///
    /// The alarm itself: every UsableTwoStateBaneAlarmLightV01 (9542) and
    /// UsableTwoStateBaneControlPointSirenV01Intrusion (25663) the server has placed on the map
    /// (world_usable, PlacedUsables) goes to its on state, and back off <see cref="HoldMs"/> after
    /// the last thing that sounded it or the last wave, once no camera that saw anyone still stands.
    ///
    /// A wave: creatures of the site's reinforcement pools, scripted pools (SpawnPoolManager.ModeScripted)
    /// each standing at one Bane teleporter of the .map (spawnpool_arrival, kind 2), which spawn only
    /// when a wave calls them. A camera wave takes the <see cref="Site.TeleportersPerWave"/> pools
    /// nearest the camera that saw the intruder and sends <see cref="Site.PerTeleporter"/> creatures
    /// through each; the exit wave sends <see cref="Site.ExitPerTeleporter"/> through each of the
    /// site's exit pools. They come through as any Bane arrival does (BaneArrivals), walk to their
    /// pool's ground, and hate whoever tripped the alarm (Threat.NoticedThreat), so they hunt the
    /// intruder rather than stand about. A pool whose teleporter is busy with an earlier wave is
    /// passed over for the next nearest.
    ///
    /// Each map channel - each squad's copy of the instance - has its own alarm. The figures are
    /// ours: the client has the cameras, lights, sirens and teleporters, and nothing of how the
    /// live servers ran them.
    /// </summary>
    public static class BaneAlarms
    {
        public const uint CameraClass = 10290;      // UsableInertDestBaneCameraV01
        public const uint AlarmLightClass = 9542;   // UsableTwoStateBaneAlarmLightV01
        public const uint SirenClass = 25663;       // UsableTwoStateBaneControlPointSirenV01Intrusion

        /// <summary>How far a camera sees.</summary>
        public const float CameraRange = 22f;

        /// <summary>How far either side of the way it faces.</summary>
        public const float CameraHalfAngle = 60f;

        /// <summary>Where its lens is above the row's position.</summary>
        public const float CameraEyeHeight = 2.2f;

        /// <summary>How often a camera looks, and the alarm is worked.</summary>
        public const long LookEveryMs = 1000;

        /// <summary>How long after a camera first sees someone its next wave comes, and after that one the next.</summary>
        public const long WaveEveryMs = 90_000;

        /// <summary>How long the lights and sirens stay on after the last thing that sounded them.</summary>
        public const long HoldMs = 180_000;

        /// <summary>A Bane base with an alarm: which pools answer it.</summary>
        public sealed class Site
        {
            public uint MapContextId { get; init; }

            /// <summary>The reinforcement pools a camera's wave is drawn from, by spawn pool id: one per teleporter.</summary>
            public uint[] CameraPools { get; init; } = Array.Empty<uint>();

            /// <summary>The pools of the exit wave the databanks bring.</summary>
            public uint[] ExitPools { get; init; } = Array.Empty<uint>();

            /// <summary>The destructible classes whose destruction brings the exit wave.</summary>
            public uint[] DatabankClasses { get; init; } = Array.Empty<uint>();

            public int TeleportersPerWave { get; init; } = 2;
            public int PerTeleporter { get; init; } = 2;
            public int ExitPerTeleporter { get; init; } = 2;
        }

        /// <summary>UsableInertDestBaneSecurityMachineryV01 and V02, Tall V01 and V02: the monitoring machines.</summary>
        public static readonly uint[] SecurityMachinery = { 10191, 21000, 24733, 24735 };

        /// <summary>Minos Caverns (1347): the Bane mining base in the caverns south of Delta Outpost.</summary>
        public const uint MinosCaverns = 1347;

        /// <summary>The Minos Caverns reinforcement pools: ids the placement data gives them (MinosCavernsContent).</summary>
        public const uint MinosFirstCameraPool = 568001, MinosLastCameraPool = 568008;
        public const uint MinosFirstExitPool = 568009, MinosLastExitPool = 568011;

        /// <summary>The sites, by map. A list, so that a test can add one on its own map.</summary>
        public static readonly IReadOnlyList<Site> Sites = new List<Site>
        {
            new Site
            {
                MapContextId = MinosCaverns,
                CameraPools = Range(MinosFirstCameraPool, MinosLastCameraPool),
                ExitPools = Range(MinosFirstExitPool, MinosLastExitPool),
                DatabankClasses = SecurityMachinery
            }
        };

        private static uint[] Range(uint first, uint last) => Enumerable.Range((int)first, (int)(last - first + 1)).Select(i => (uint)i).ToArray();

        /// <summary>The site of a map, or null.</summary>
        public static Site SiteOf(uint mapContextId) => Sites.FirstOrDefault(site => site.MapContextId == mapContextId);

        /// <summary>One map channel's alarm.</summary>
        public sealed class Alarm
        {
            /// <summary>Whether the lights and sirens are on.</summary>
            public bool Sounding { get; internal set; }

            /// <summary>When they go off again, while nothing keeps them on.</summary>
            public long QuietAt { get; internal set; }

            /// <summary>The cameras that have seen someone, and when each brings its next wave.</summary>
            public Dictionary<PlacedDestructibles.Live, long> Watching { get; } = new();

            /// <summary>The last player seen or heard: who the waves go for.</summary>
            public Manifestation Intruder { get; internal set; }

            /// <summary>Whether the databanks' wave has come.</summary>
            public bool ExitWaveSent { get; internal set; }

            internal long NextLookAt { get; set; }

            /// <summary>How many waves have come, in all.</summary>
            public int Waves { get; internal set; }
        }

        private static readonly ConditionalWeakTable<MapChannel, Alarm> Alarms = new();

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether a camera's lens has the player in the clear; a test's to replace.</summary>
        internal static Func<MapChannel, Vector3, Manifestation, bool> InTheClear { get; set; } = (map, eye, player) =>
            map.Cover == null || Cover.Visible(map.Cover, eye, Cover.SamplePoints(player, eye)) > 0;

        /// <summary>The map channel's alarm, made as it is first asked for.</summary>
        public static Alarm Of(MapChannel map) => Alarms.GetValue(map, _ => new Alarm());

        public static bool IsCamera(DynamicObject obj) => obj != null && (uint)obj.EntityClassId == CameraClass;

        public static bool IsDatabank(Site site, DynamicObject obj) =>
            site != null && obj != null && Array.IndexOf(site.DatabankClasses, (uint)obj.EntityClassId) >= 0;

        /// <summary>The cameras standing on the map channel.</summary>
        public static List<PlacedDestructibles.Live> StandingCameras(MapChannel map) =>
            PlacedDestructibles.On(map).Where(live => !live.IsDown && IsCamera(live.Object)).ToList();

        /// <summary>Where a camera looks from, and the way it faces, on the flat.</summary>
        public static (Vector3 Eye, Vector3 Facing) LensOf(PlacedDestructibles.Live camera)
        {
            var yaw = camera.Placement.Rotation;

            return (camera.Object.Position + new Vector3(0f, CameraEyeHeight, 0f),
                new Vector3((float)-Math.Sin(yaw), 0f, (float)-Math.Cos(yaw)));
        }

        /// <summary>Whether the camera sees the player: alive, in its cone, not hidden, and in the clear.</summary>
        public static bool Sees(MapChannel map, PlacedDestructibles.Live camera, Manifestation player)
        {
            if (map == null || camera == null || player == null || camera.IsDown ||
                player.State == CharacterState.Dead || player.State == CharacterState.Dying ||
                !ReferenceEquals(player.MapChannel, map) || Detection.IsHidden(player))
                return false;

            var (eye, facing) = LensOf(camera);

            if (!AbilityManager.InCone(camera.Object.Position, facing, player.Position, CameraRange, CameraHalfAngle))
                return false;

            return InTheClear(map, eye, player);
        }

        /// <summary>
        /// Every tick: once a second, the standing cameras look, the cameras that have seen someone
        /// bring their waves when due, and the lights go off once nothing keeps them on.
        /// </summary>
        public static void Worker(MapChannel map)
        {
            if (map?.MapInfo == null)
                return;

            var site = SiteOf(map.MapInfo.MapContextId);

            if (site == null)
                return;

            var now = Now();
            var alarm = Of(map);

            if (now < alarm.NextLookAt)
                return;

            alarm.NextLookAt = now + LookEveryMs;

            var cameras = StandingCameras(map);

            if (cameras.Count > 0 && map.ClientList.Count > 0)
            {
                var players = map.ClientList.ToList()
                    .Where(client => client?.State == ClientState.Ingame)
                    .Select(client => client.Player)
                    .Where(player => player != null)
                    .ToList();

                foreach (var camera in cameras)
                {
                    if (alarm.Watching.ContainsKey(camera))
                        continue;

                    var seen = players.FirstOrDefault(player => Sees(map, camera, player));

                    if (seen != null)
                        Trip(map, site, alarm, camera, seen, now);
                }
            }

            // A camera that is down, or gone, watches no longer; one that still stands and is due brings its wave.
            foreach (var (camera, dueAt) in alarm.Watching.ToList())
            {
                if (camera.IsDown || !cameras.Contains(camera))
                {
                    alarm.Watching.Remove(camera);
                    continue;
                }

                if (now >= dueAt)
                {
                    alarm.Watching[camera] = now + WaveEveryMs;
                    CameraWave(map, site, alarm, camera);
                }
            }

            if (alarm.Sounding && alarm.Watching.Count == 0 && now >= alarm.QuietAt)
                Quiet(map, alarm);
        }

        /// <summary>A camera has seen a player: the alarm, and the camera's first wave now.</summary>
        private static void Trip(MapChannel map, Site site, Alarm alarm, PlacedDestructibles.Live camera, Manifestation seen, long now)
        {
            Logger.WriteLog(LogType.Debug, $"Bane alarm on map {map.MapInfo.MapContextId}: {camera.Placement} saw {seen.FamilyName}.");

            alarm.Intruder = seen;
            alarm.Watching[camera] = now + WaveEveryMs;
            Sound(map, alarm, now);
            CameraWave(map, site, alarm, camera);
        }

        /// <summary>
        /// A destructible has been destroyed on the map: a databank brings the exit wave, the first
        /// time, and sounds the alarm. Called by PlacedDestructibles.Hit.
        /// </summary>
        public static void Destroyed(MapChannel map, PlacedDestructibles.Live live, Actor by)
        {
            if (map?.MapInfo == null || live == null)
                return;

            var site = SiteOf(map.MapInfo.MapContextId);

            if (site == null || !IsDatabank(site, live.Object))
                return;

            var alarm = Of(map);
            var now = Now();

            if (by is Manifestation player)
                alarm.Intruder = player;

            Sound(map, alarm, now);

            if (alarm.ExitWaveSent)
                return;

            alarm.ExitWaveSent = true;
            Logger.WriteLog(LogType.Debug, $"Bane alarm on map {map.MapInfo.MapContextId}: {live.Placement} destroyed; the exit wave comes.");

            foreach (var pool in PoolsOf(map, site.ExitPools))
                Send(map, alarm, pool, site.ExitPerTeleporter);
        }

        /// <summary>The lights and sirens on, and kept on HoldMs from now.</summary>
        private static void Sound(MapChannel map, Alarm alarm, long now)
        {
            alarm.QuietAt = now + HoldMs;

            if (alarm.Sounding)
                return;

            alarm.Sounding = true;

            foreach (var light in Lights(map))
                PlacedUsables.SetState(light, UseObjectState.TsState1, 0);
        }

        /// <summary>The lights and sirens off.</summary>
        private static void Quiet(MapChannel map, Alarm alarm)
        {
            alarm.Sounding = false;

            foreach (var light in Lights(map))
                PlacedUsables.SetState(light, UseObjectState.TsState0, 0);
        }

        /// <summary>The alarm lights and sirens placed on the map channel.</summary>
        public static List<PlacedUsables.Live> Lights(MapChannel map) =>
            PlacedUsables.On(map).Where(live => (uint)live.Placement.ClassId is AlarmLightClass or SirenClass).ToList();

        /// <summary>The site's pools that stand on this map channel, in the order given.</summary>
        private static List<SpawnPool> PoolsOf(MapChannel map, uint[] ids)
        {
            var pools = map.SpawnPools.Count > 0
                ? map.SpawnPools
                : SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == map.MapInfo.MapContextId).ToList();

            return ids.Select(id => pools.FirstOrDefault(pool => pool.DbId == id)).Where(pool => pool != null).ToList();
        }

        /// <summary>A camera's wave: PerTeleporter creatures through each of the TeleportersPerWave nearest pools that are free.</summary>
        private static void CameraWave(MapChannel map, Site site, Alarm alarm, PlacedDestructibles.Live camera)
        {
            var at = camera.Object.Position;
            var pools = PoolsOf(map, site.CameraPools)
                .Where(pool => pool.Arrivals.Count > 0 && pool.QueuedCreatures == 0 && pool.DropshipQueue == 0)
                .OrderBy(pool => Vector3.DistanceSquared(pool.Arrivals[0].Position, at))
                .Take(Math.Max(1, site.TeleportersPerWave))
                .ToList();

            foreach (var pool in pools)
                Send(map, alarm, pool, site.PerTeleporter);
        }

        /// <summary>Count creatures of the pool's first slot through its teleporter, after the intruder.</summary>
        private static void Send(MapChannel map, Alarm alarm, SpawnPool pool, int count)
        {
            var arrival = pool.Arrivals.FirstOrDefault(a => a.Kind == SpawnPoolArrivalEntry.KindTeleporter);
            var slot = pool.SpawnSlot?.FirstOrDefault(s => s != null && s.CreatureId != 0);

            if (arrival == null || slot == null || count <= 0 ||
                !CreatureManager.Instance.LoadedCreatures.TryGetValue(slot.CreatureId, out var template) || template == null)
                return;

            if (pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                return;     // its teleporter is busy with an earlier wave

            var creatures = Enumerable.Repeat(template, count).ToList();
            var intruder = alarm.Intruder;

            pool.HasSpawned = true;
            alarm.Waves++;
            alarm.QuietAt = Math.Max(alarm.QuietAt, Now() + HoldMs);     // a wave keeps the alarm on

            BaneArrivals.BeginTeleport(map, pool, arrival, creatures, spawned =>
            {
                if (intruder == null || intruder.State == CharacterState.Dead || !ReferenceEquals(intruder.MapChannel, map))
                    return;

                foreach (var creature in spawned)
                    creature.Hate.Ensure(intruder.EntityId, Threat.NoticedThreat);
            });
        }

        /// <summary>Forgets a map channel's alarm (a map reset, a test).</summary>
        internal static void Forget(MapChannel map)
        {
            if (map != null)
                Alarms.Remove(map);
        }
    }
}
