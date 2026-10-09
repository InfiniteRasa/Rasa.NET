using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The sky's clock: SetSkyTime, the one thing about a map's sky the server decides.
    ///
    /// A client's map file gives the map a day of so many seconds and the second of it at which
    /// the sky starts (gamemap.py _LoadTimeSettings1), and from there the client runs the sky
    /// itself. What it cannot know is how long the sky has been running, so the server says:
    /// Recv_SetSkyTime(runningTime) is "the number of seconds the sky has been running on the
    /// server", added to the map's starting second; the sum over the day's length is the day,
    /// and what is left over is the time of day (0 midnight, half the day noon).
    ///
    /// The sky of a map channel starts when the channel is made: with the server for a map's
    /// own channel, and for a copy of a map - a private, shared or squad instance - when that
    /// copy is. A channel's players are all sent the same clock, on arriving, and it is not
    /// sent again: the client keeps time between. So after a restart every map is at its own
    /// starting hour, and a fresh instance always opens at its own.
    ///
    /// <see cref="Days"/> is each map's day as its map file has it, by the map's name. The
    /// server needs it only to say a time of day or to be told one (.skytime, .setskytime);
    /// the seconds it sends need no table.
    /// </summary>
    public static class SkyClock
    {
        /// <summary>A map's day: its length, and the second of it at which its sky starts.</summary>
        public sealed record Day(int Length, int Start);

        /// <summary>The tick the clock reads; a test puts its own here.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Each client map's time settings (data/maps/&lt;name&gt;/&lt;name&gt;.map, client 1.16.5.0), in seconds.</summary>
        public static readonly IReadOnlyDictionary<string, Day> Days = new Dictionary<string, Day>(StringComparer.OrdinalIgnoreCase)
        {
            ["adv_afs_arena"] = new Day(3600, 0),
            ["adv_arieki_ligo_ashendesert"] = new Day(10800, 3750),
            ["adv_arieki_ligo_ashendesert_avernusoutpost"] = new Day(10800, 0),
            ["adv_arieki_ligo_ashendesert_baneconscriptfacility"] = new Day(10800, 8100),
            ["adv_arieki_ligo_ashendesert_indracaverns"] = new Day(10800, 0),
            ["adv_arieki_ligo_burningsteps"] = new Day(10800, 3700),
            ["adv_arieki_ligo_burningsteps_magmacaverns"] = new Day(10800, 5400),
            ["adv_arieki_ligo_crucible_incurablesward"] = new Day(10800, 0),
            ["adv_arieki_ligo_crucible_wbfacility"] = new Day(3600, 0),
            ["adv_arieki_ligo_staaljunkyard"] = new Day(10800, 0),
            ["adv_arieki_ligo_thunderhead"] = new Day(10800, 3375),
            ["adv_arieki_ligo_thunderhead_faultlever"] = new Day(10800, 10500),
            ["adv_arieki_ligo_thunderhead_quassostation"] = new Day(10800, 5400),
            ["adv_arieki_ligo_thunderhead_rivasaattacolony"] = new Day(3600, 0),
            ["adv_arieki_torden_abyss"] = new Day(10800, 0),
            ["adv_arieki_torden_abyss_dybukkar"] = new Day(10800, 0),
            ["adv_arieki_torden_abyss_omegalabs"] = new Day(3600, 0),
            ["adv_arieki_torden_abyss_tampei"] = new Day(3600, 0),
            ["adv_arieki_torden_incline"] = new Day(10800, 2400),
            ["adv_arieki_torden_incline_commtower"] = new Day(10800, 0),
            ["adv_arieki_torden_incline_ojasaattahive"] = new Day(10800, 0),
            ["adv_arieki_torden_incline_wardenbotfactory"] = new Day(100000, 60000),
            ["adv_arieki_torden_mires"] = new Day(10800, 4000),
            ["adv_arieki_torden_mires_banefluxitemines"] = new Day(10800, 0),
            ["adv_arieki_torden_mires_energyweaponcenter"] = new Day(3600, 0),
            ["adv_arieki_torden_mires_tahrendrabase"] = new Day(3600, 0),
            ["adv_arieki_torden_plains"] = new Day(10800, 3036),
            ["adv_arieki_torden_plains_attacolony"] = new Day(3600, 0),
            ["adv_arieki_torden_plains_brannwaterrefinery"] = new Day(10800, 0),
            ["adv_arieki_torden_plains_penalresearch"] = new Day(3600, 0),
            ["adv_bootcamp"] = new Day(10800, 0),
            ["adv_earth_unitedstates_manhattan_01"] = new Day(10800, 1350),
            ["adv_earth_unitedstates_manhattan_01_shared"] = new Day(10800, 1350),
            ["adv_foreas_concordia_divide"] = new Day(10800, 3450),
            ["adv_foreas_concordia_divide_minoscaverns"] = new Day(10800, 0),
            ["adv_foreas_concordia_divide_purgasstation2"] = new Day(10800, 3375),
            ["adv_foreas_concordia_divide_timoramines"] = new Day(10800, 0),
            ["adv_foreas_concordia_divide_torcastraprison"] = new Day(10800, 3375),
            ["adv_foreas_concordia_elohcommtower2"] = new Day(10800, 4500),
            ["adv_foreas_concordia_palisades"] = new Day(10800, 3300),
            ["adv_foreas_concordia_palisades_devilsden"] = new Day(10800, 4500),
            ["adv_foreas_concordia_palisades_elohtemples"] = new Day(360000, 0),
            ["adv_foreas_concordia_palisades_treebackcamp"] = new Day(36000, 12000),
            ["adv_foreas_concordia_palisades_warnetcaverns"] = new Day(3600, 0),
            ["adv_foreas_concordia_wilderness"] = new Day(10800, 3375),
            ["adv_foreas_concordia_wilderness_cavesofdonn02"] = new Day(10800, 0),
            ["adv_foreas_concordia_wilderness_cavesofdonn_epic"] = new Day(10800, 0),
            ["adv_foreas_concordia_wilderness_clrf"] = new Day(10800, 5400),
            ["adv_foreas_concordia_wilderness_guardianprom"] = new Day(10800, 3300),
            ["adv_foreas_concordia_wilderness_pravusresearch"] = new Day(10800, 3365),
            ["adv_foreas_howlingmaw1"] = new Day(10800, 7000),
            ["adv_foreas_howlingmaw_cuthahbase"] = new Day(10800, 5400),
            ["adv_foreas_howlingmaw_deathburrow"] = new Day(3600, 0),
            ["adv_foreas_valverde_descent"] = new Day(10800, 3375),
            ["adv_foreas_valverde_descent_inferno_outpost"] = new Day(3600, 0),
            ["adv_foreas_valverde_descent_therefuge"] = new Day(3600, 0),
            ["adv_foreas_valverde_marshes"] = new Day(10800, 1500),
            ["adv_foreas_valverde_marshes_banesupplydepot"] = new Day(10800, 5400),
            ["adv_foreas_valverde_marshes_logosresearchfacility"] = new Day(10800, 0),
            ["adv_foreas_valverde_marshes_villageruins"] = new Day(10800, 7800),
            ["adv_foreas_valverde_marshes_wetlandrefinery"] = new Day(10800, 90),
            ["adv_foreas_valverde_plateau"] = new Day(10800, 3375),
            ["adv_foreas_valverde_plateau_maligobasev3"] = new Day(10800, 0),
            ["adv_foreas_valverde_plateau_sanctusgrotto"] = new Day(10800, 5600),
            ["adv_foreas_valverde_plateau_temporalchamber"] = new Day(10800, 3375),
            ["adv_foreas_valverde_plateau_ustoryard"] = new Day(10800, 1125),
            ["adv_foreas_valverde_pools"] = new Day(10800, 2500),
            ["adv_foreas_valverde_pools_livetargetpensv2"] = new Day(10800, 0),
            ["adv_foreas_valverde_pools_retread_caves"] = new Day(3600, 0),
            ["adv_foreas_valverde_pools_test_weapons_center"] = new Day(10800, 0),
            ["adv_wargame_edmundrange2"] = new Day(10800, 0),
            ["adv_wargame_indoorarena"] = new Day(3600, 0),
            ["adv_wargame_provinggroundsv002"] = new Day(10800, 0),
            ["adv_zepic_pve_arena"] = new Day(3600, 0),
            ["characterselection"] = new Day(3600, 0),
            ["test_nexus2"] = new Day(9999, 5000),
            ["test_pvpcontrolpoint01"] = new Day(100000, 50000),
        };

        /// <summary>How long a map channel's sky has been running, in whole seconds.</summary>
        public static int RunningSeconds(MapChannel map)
        {
            if (map == null)
                return 0;

            return (int)Math.Clamp((Now() - map.SkyStartedTick) / 1000L, 0L, int.MaxValue);
        }

        /// <summary>Tells one client how long the sky of the map it is on has been running.</summary>
        public static void Send(Client client)
        {
            var map = client?.Player?.MapChannel;

            if (map == null)
                return;

            client.CallMethod(SysEntity.ClientGameMapId, new SetSkyTimePacket { RunningTime = RunningSeconds(map) });
        }

        /// <summary>Puts a map channel's sky at so many seconds of running, and tells everyone on it.</summary>
        public static void SetRunningSeconds(MapChannel map, int seconds)
        {
            map.SkyStartedTick = Now() - Math.Max(0, seconds) * 1000L;

            foreach (var client in map.ClientList.ToArray())
                if (client?.Player != null && ReferenceEquals(client.Player.MapChannel, map))
                    Send(client);
        }

        /// <summary>The map's day, when its map file is one the table has.</summary>
        public static bool TryGetDay(MapChannel map, out Day day)
        {
            day = null;

            return map?.MapInfo?.MapName != null && Days.TryGetValue(map.MapInfo.MapName, out day) && day.Length > 0;
        }

        /// <summary>The time of day on a map channel now, as a fraction of its day: 0 midnight, 0.5 noon.</summary>
        public static bool TryGetTimeOfDay(MapChannel map, out double timeOfDay, out Day day)
        {
            timeOfDay = 0;

            if (!TryGetDay(map, out day))
                return false;

            timeOfDay = (double)(((long)day.Start + RunningSeconds(map)) % day.Length) / day.Length;

            return true;
        }

        /// <summary>
        /// Runs a map channel's sky on to the next time it is at this time of day (a fraction of
        /// its day), and tells everyone on it. On, never back: a sky does not run backwards.
        /// </summary>
        public static bool TrySetTimeOfDay(MapChannel map, double timeOfDay)
        {
            if (!TryGetDay(map, out var day) || !double.IsFinite(timeOfDay) || timeOfDay < 0 || timeOfDay >= 1)
                return false;

            var running = RunningSeconds(map);
            var now = ((long)day.Start + running) % day.Length;
            var wanted = (long)Math.Round(timeOfDay * day.Length) % day.Length;
            var ahead = ((wanted - now) % day.Length + day.Length) % day.Length;

            SetRunningSeconds(map, (int)Math.Min(int.MaxValue, running + ahead));

            return true;
        }

        /// <summary>A fraction of a day as hours and minutes of a 24-hour clock.</summary>
        public static string ClockText(double timeOfDay)
        {
            var minutes = (int)Math.Floor(timeOfDay * 24 * 60 + 1e-6) % (24 * 60);

            return $"{minutes / 60:00}:{minutes % 60:00}";
        }
    }
}
