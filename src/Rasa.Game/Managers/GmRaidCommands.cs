using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;
    using Structures.World;

    /// <summary>
    /// A game master's commands for the raids on the control points (BaneRaids) and the troops that come by
    /// dropship or beam in (Reinforcements): to drive them and to watch them.
    ///
    ///  - .cp raids [all]: every point on this map (or every map) - who holds it, a raid on it under way
    ///    (its side, wave, raiders alive and on their way, a claim) or when the next is due, its gates, and
    ///    whoever of a raid holds it;
    ///  - .cp &lt;id&gt; raid: a raid on the point now, by whoever does not hold it, whoever is near;
    ///    .cp &lt;id&gt; raid stop: the raid ends, what is alive of it goes; .cp &lt;id&gt; raid in &lt;minutes&gt;:
    ///    the next one is due then (it still waits for a player near); .cp &lt;id&gt; raid who: each raider
    ///    alive, where it is and what it is doing;
    ///  - .dropship &lt;creature id&gt; [count] [bane|afs] [here]: a ship of that side comes down with that many
    ///    of the creature, on open ground some 30 m ahead (or round you, with here), and they stay there;
    ///    .dropship pool &lt;pool id&gt;: how that pool's next spawn would come, without sending it.
    /// </summary>
    public static class GmRaidCommands
    {
        /// <summary>How far ahead of the game master a .dropship comes down, and how far round that it may look for open sky.</summary>
        public const float DropAhead = 30f;
        public const float DropSearch = 25f;

        /// <summary>The most a .dropship may bring.</summary>
        public const int DropMost = 12;

        private static void Say(Client client, string text) => CommunicatorManager.Instance.SystemMessage(client, text);

        #region .cp raids, .cp <id> raid

        /// <summary>.cp raids [all]: the state of the raids on this map's points, or on every map's.</summary>
        public static void Status(Client client, bool all)
        {
            var player = client.Player;
            var raids = BaneRaids.Instance;
            var points = ControlPoints.Instance;
            var now = raids.Now();
            var list = (all ? points.Points : points.OnMap(player.MapContextId))
                .Where(p => !p.IsBattleground)
                .OrderBy(p => p.MapContextId).ThenBy(p => p.Id).ToList();

            if (list.Count == 0)
            {
                Say(client, all ? "There are no control points." : $"No control points on map {player.MapContextId}.");
                return;
            }

            var config = raids.Config;

            Say(client, $"Raids: Bane {(config.Raids ? "on" : "off")}, AFS {(config.AfsRaids ? "on" : "off")}; ships: Bane {(Reinforcements.Config.BaneDropships ? "on" : "off")}, AFS {(Reinforcements.Config.AfsDropships ? "on" : "off")}, beam-in {(Reinforcements.Config.BeamIn ? "on" : "off")}. {raids.Running.Count} raid(s) under way.");

            foreach (var point in list)
            {
                var raid = raids.Of(point.Id);
                var holder = ControlPoints.FactionName(point.Owner);
                var map = point.Map;
                var gates = OutpostWalls.GatesOf(point).Count;
                var held = map == null ? 0 : raids.Holders(map, point, point.Owner);
                string state;

                if (raid != null)
                {
                    var (alive, coming) = raids.Count(raid);
                    var next = raid.NextWaveAt != 0 ? $", next ships in {Math.Max(0, raid.NextWaveAt - now) / 1000} s" : "";
                    var owed = raid.ShipsOwed > 0 ? $", {raid.ShipsOwed} ship(s) waiting for room" : "";
                    var claim = raid.Claimer != null ? $", {raid.Claimer.EntityId} claiming for {(now - raid.ClaimStartedAt) / 1000} s" : "";

                    state = $"{ControlPoints.FactionName(raid.Side)} raid, wave {raid.Wave}/{raid.Waves} of {raid.Squad} a ship, {alive} alive, {coming} on the way{next}{owed}{claim}, {(now - raid.StartedAt) / 1000} s in";
                }
                else
                {
                    var due = raids.NextAt(point.Id);

                    state = due == null ? "no raid due yet" : $"next {ControlPoints.FactionName(point.HeldByAfs ? ControlPoints.Bane : ControlPoints.Afs)} raid in {Clock(Math.Max(0, due.Value - now))}";
                }

                var where = all || map == null ? $"map {point.MapContextId}" : $"{Vector3.Distance(point.Object?.Position ?? point.Position, player.Position):0} m";

                Say(client, $"#{point.Id} {point.Name}: {holder}, {state}; {gates} gate(s){(held > 0 ? $", {held} raider(s) hold it" : "")}; {where}");
            }
        }

        /// <summary>.cp &lt;id&gt; raid [stop | in &lt;minutes&gt; | who].</summary>
        public static void Raid(Client client, ControlPoints.Point point, string[] args)
        {
            var raids = BaneRaids.Instance;
            var player = client.Player;

            if (point.IsBattleground)
            {
                Say(client, $"Control point #{point.Id} {point.Name} is a battleground's: nobody raids it.");
                return;
            }

            switch (args.Length == 0 ? "" : args[0])
            {
                case "":
                    if (point.Map == null || point.Object == null)
                    {
                        Say(client, $"Control point #{point.Id} {point.Name} is not set down on a map yet.");
                        return;
                    }

                    if (raids.Of(point.Id) != null)
                    {
                        Say(client, $"Control point #{point.Id} {point.Name} is being raided already; .cp {point.Id} raid stop ends it.");
                        return;
                    }

                    var raid = raids.Begin(point.Map, point);

                    Say(client, raid == null
                        ? $"No raid on control point #{point.Id} {point.Name}: no {(point.HeldByAfs ? "Bane troops" : "AFS soldiers")} near enough to send."
                        : $"The {ControlPoints.FactionName(raid.Side)} raid control point #{point.Id} {point.Name}: {raid.Waves} wave(s) of {raid.Squad} a ship, troops {string.Join(", ", raid.Troops)}{(raid.Level > 0 ? $" at level {raid.Level}" : "")}, the first ships in {Math.Max(0, raid.NextWaveAt - raids.Now()) / 1000} s.");

                    Logger.WriteLog(LogType.Command, $"{player.FamilyName} began a raid on control point {point.Id} ({point.Name}).");
                    return;

                case "stop":
                    Say(client, raids.Stop(point.Id)
                        ? $"The raid on control point #{point.Id} {point.Name} is over; what was alive of it has gone."
                        : $"Control point #{point.Id} {point.Name} is not being raided.");
                    return;

                case "in" when args.Length == 2 && double.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var minutes) && minutes >= 0:
                    raids.SetNextAt(point.Id, raids.Now() + (long)(minutes * 60_000));
                    Say(client, $"The next raid on control point #{point.Id} {point.Name} is due in {Clock((long)(minutes * 60_000))}, once a player is near.");
                    return;

                case "who":
                    var running = raids.Of(point.Id);

                    if (running == null)
                    {
                        Say(client, $"Control point #{point.Id} {point.Name} is not being raided.");
                        return;
                    }

                    var lines = raids.Describe(running);

                    Say(client, $"{lines.Count} of the {ControlPoints.FactionName(running.Side)} raid on #{point.Id} {point.Name} alive:");

                    foreach (var line in lines.Take(20))
                        Say(client, line);

                    return;

                default:
                    Say(client, "usage: .cp <id> raid | raid stop | raid in <minutes> | raid who");
                    return;
            }
        }

        private static string Clock(long ms) => $"{ms / 60_000}:{ms / 1000 % 60:00}";

        #endregion

        #region .dropship

        /// <summary>.dropship &lt;creature id&gt; [count] [bane|afs] [here] | .dropship pool &lt;pool id&gt;.</summary>
        public static void Dropship(Client client, string[] parts)
        {
            const string usage = "usage: .dropship <creature id> [count 1-12] [bane|afs] [here] | .dropship pool <pool id>";
            var player = client.Player;
            var map = player.MapChannel;

            if (map == null)
                return;

            if (parts.Length == 3 && parts[1] == "pool")
            {
                Explain(client, map, parts[2]);
                return;
            }

            if (parts.Length < 2 || !uint.TryParse(parts[1], out var creatureId))
            {
                Say(client, usage);
                return;
            }

            if (!CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var template) || template == null)
            {
                Say(client, $"There is no creature {creatureId}.");
                return;
            }

            var count = 1;
            var side = template.TargetCategory == TargetCategory.Friendly ? TargetCategory.Friendly : TargetCategory.Hostile;
            var here = false;

            foreach (var arg in parts.Skip(2))
            {
                if (int.TryParse(arg, out var n))
                    count = Math.Clamp(n, 1, DropMost);
                else if (arg == "bane")
                    side = TargetCategory.Hostile;
                else if (arg == "afs")
                    side = TargetCategory.Friendly;
                else if (arg == "here")
                    here = true;
                else
                {
                    Say(client, usage);
                    return;
                }
            }

            var at = here ? player.Position : player.Position + AbilityManager.FacingOf(player) * DropAhead;
            var spot = Reinforcements.LandingSpot(map, at, DropSearch);

            if (spot == null)
            {
                Say(client, $"No open sky for a ship within {DropSearch:0} m of ({at.X:0.#}, {at.Y:0.#}, {at.Z:0.#}): under a roof, in a cave, or another ship is there.");
                return;
            }

            var pool = Reinforcements.TransientPool(map, spot.Value, player.Rotation, 8f, new[] { new SpawnPoolSlot(creatureId, 0, 0) });
            var ship = Reinforcements.Drop(map, pool, Enumerable.Repeat(template, count).ToList(), spot.Value, player.Rotation + Math.PI, side);

            Say(client, ship == null
                ? "The ship could not be sent; see the server log."
                : $"A{(side == TargetCategory.Friendly ? "n AFS" : " Bane")} dropship ({ship.EntityId}) brings {count} of creature {creatureId} down at ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}), {Vector3.Distance(spot.Value, player.Position):0} m from you.");

            Logger.WriteLog(LogType.Command, $"{player.FamilyName} sent a dropship of {count} x creature {creatureId} to ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}) on map {map.MapInfo.MapContextId}.");
        }

        /// <summary>How a pool of this map would restock next, by its rows or by Reinforcements; nothing is sent.</summary>
        private static void Explain(Client client, MapChannel map, string id)
        {
            if (!uint.TryParse(id, out var poolId) || map.SpawnPools.FirstOrDefault(p => p != null && p.DbId == poolId) is not { } pool)
            {
                Say(client, $"No spawn pool {id} on this map.");
                return;
            }

            var creatures = SpawnPoolManager.CreateListOfCreatures(pool, CreatureManager.Instance.LoadedCreatures, new Random());

            if (pool.Arrivals.Count > 0)
            {
                Say(client, $"SpawnPool {poolId} arrives through its {pool.Arrivals.Count} arrival row(s): {string.Join(", ", pool.Arrivals.Select(a => $"kind {a.Kind} at ({a.PosX:0.#}, {a.PosY:0.#}, {a.PosZ:0.#})"))}.");
                return;
            }

            if (pool.AnimType != 0)
            {
                Say(client, $"SpawnPool {poolId} has anim_type {pool.AnimType}: its own {(pool.AnimType == 1 ? "Bane" : "AFS")} ship on its ground.");
                return;
            }

            // As a restock (the world's first stocking is always plain).
            var spawned = pool.HasSpawned;
            pool.HasSpawned = true;

            SpawnPoolArrivalEntry arrival;

            try
            {
                arrival = Reinforcements.ArrivalFor(map, pool, creatures);
            }
            finally
            {
                pool.HasSpawned = spawned;
            }

            var what = arrival == null ? "plainly, on its ground"
                : arrival.Kind == SpawnPoolArrivalEntry.KindBeamIn ? "beamed in on its ground"
                : $"by {(arrival.Kind == SpawnPoolArrivalEntry.KindAfsDropship ? "AFS" : "Bane")} dropship at ({arrival.PosX:0.#}, {arrival.PosY:0.#}, {arrival.PosZ:0.#}), {Vector3.Distance(arrival.Position, pool.Position):0} m from its ground";

            Say(client, $"SpawnPool {poolId} ({creatures.Count} of {string.Join(", ", creatures.Select(c => c.DbId).Distinct())}) would restock {what}{(OutpostWalls.Within(map, pool.Position) ? "; its ground is within an outpost's walls" : "")}.");
        }

        #endregion
    }
}
