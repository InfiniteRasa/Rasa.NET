using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Every zone's control point plans by dropship - the Plateau's (PlateauDynamicDropshipsContent) -
    /// and what a plan is made of.
    /// </summary>
    public static class DynamicDropshipsContent
    {
        public readonly struct Troop
        {
            public uint Creature { get; }
            public int Min { get; }
            public int Max { get; }
            public Troop(uint creature, int min, int max) { Creature = creature; Min = min; Max = max; }
        }

        public sealed class Assault
        {
            public uint MapId { get; init; }
            public uint PointId { get; init; }
            /// <summary>From the server start, or from the AFS taking the point, to the first assault.</summary>
            public int FirstDelaySeconds { get; init; }
            public int PeriodSeconds { get; init; }
            public int WaveSeconds { get; init; }
            public int LifetimeSeconds { get; init; }
            public LandingZoneAssaultContent.Drop[] Drops { get; init; }
            /// <summary>Per wave, per ship: what the ship brings.</summary>
            public Troop[][][] Waves { get; init; }
            /// <summary>Comes with one ship of the last wave and takes the point if the defence falls.</summary>
            public uint Boss { get; init; }
            /// <summary>From the assault's start to the AFS relief ship.</summary>
            public int ReliefSeconds { get; init; } = 480;
            public LandingZoneAssaultContent.Drop ReliefPad { get; init; }
            public Vector3 ReliefPost { get; init; }
            public Troop[] Relief { get; init; }
            /// <summary>While the Bane hold the point: one AFS ship with this squad now and then (AfsSorties).</summary>
            public Troop[] Sortie { get; init; } = Array.Empty<Troop>();
            public int SortieFirstDelaySeconds { get; init; }
            public int SortiePeriodSeconds { get; init; } = 1800;
            public int SortieLifetimeSeconds { get; init; } = 600;
            /// <summary>
            /// Each assault the AFS hold off makes the next one bigger by this share of its troops
            /// (LandingZoneAssaults), until the point falls; it starts again from nothing when the
            /// point changes hands. 0: no growth.
            /// </summary>
            public double EscalationStep { get; init; } = Escalation;
            /// <summary>Held-off assaults that count towards the growth: at most twice the troops.</summary>
            public int MaxEscalation { get; init; } = 1;
            /// <summary>One of each, however big the assault grows: a named creature.</summary>
            public uint[] NotEscalated { get; init; } = Array.Empty<uint>();
        }

        // Each zone's escalation (tuned on the Plateau). "Allowing the Bane to control these points may give them the means to drop an ever
        // increasing number of troops" (Ten Ton Hammer, E3 2005); "as Bane are killed, even more are
        // called in for backup" (TaRapedia, Control Point); "The game had a built in 'ramp up' for the
        // number of Bane that would spawn in and assault so that EVENTUALLY the Control Point would get
        // overrun" (a retail player, City of Titans forum, "CONTROL POINTS ... how Tabula Rasa did it").
        // 50% more troops (Commander and Kael one each) for each assault held off: ESTIMATE, so that a
        // point no player defends falls in its second or third assault and one player helping can hold
        // it. It was 25% (live run without Shield Drones: Northwest and a captured Southwest both fell in
        // the third), but the garrison was set down again during an assault then; live run 2026-11-05,
        // Research Point: the first assault (8, 10, 13 Bane, 3 min apart) killed 1 of 18 AFS. Now the
        // garrison stays down while an assault runs (LandingZoneAssaults.HoldsGarrison), the waves come
        // 90 s apart (was 180 s, ESTIMATE: a wave lands while the last still fights), and each held off
        // counts for more. Capped at twice the troops (ESTIMATE; was 4 x 25%) so that a point players
        // keep defending does not grow without end. Live runs 2026-11-05 at 50% (2 held off for twice):
        // every natural assault was held off (Northeast lost 1 AFS, Retread Outpost 7, Bane Assertion
        // Camp 6). Now twice the troops from the first assault held off on (100%, MaxEscalation 1,
        // ESTIMATE), with no relief for a garrison down to half (LandingZoneAssaults) and troops that
        // give up a fight that gets them nowhere (LandingZoneAssaults.FightWatch).
        public const double Escalation = 1.0;

        /// <summary>
        /// The key of each table a dropship migration inserts into: the primary key of its entity in
        /// WorldContext's model, or, for the keyless creature_appearance, the columns of its index
        /// creature_appearance_index_creature_slot. <see cref="Guarded"/> skips a row whose key is there.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Keys = new Dictionary<string, string[]>
        {
            ["creature_action"] = new[] { "id" },
            ["creature"] = new[] { "id" },
            ["creature_stat"] = new[] { "id" },
            ["creature_appearance"] = new[] { "id", "slot_id" },
            ["spawnpool"] = new[] { "id" },
            ["control_point_link"] = new[] { "control_point_id", "kind", "object_id" }
        };

        /// <summary>
        /// <paramref name="statements"/> safe to run twice: each row of an
        /// <c>INSERT INTO t (columns) VALUES (...), (...)</c> its own
        /// <c>INSERT INTO t (columns) SELECT ... FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM t g WHERE g.key = value ...)</c>,
        /// valid on SQLite and MySQL 8. Not INSERT OR IGNORE / INSERT IGNORE: those hide every other
        /// error too. Any other statement comes back as it is.
        /// </summary>
        public static IEnumerable<string> Guarded(IEnumerable<string> statements)
        {
            foreach (var sql in statements)
            {
                var match = Regex.Match(sql.Trim(), @"^INSERT\s+INTO\s+(\w+)\s*\(([^()]*)\)\s*VALUES\s*(.*?);?$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!match.Success)
                {
                    if (sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException($"Not an INSERT INTO table (columns) VALUES ...: {sql}", nameof(statements));
                    yield return sql;
                    continue;
                }
                var table = match.Groups[1].Value;
                var columns = match.Groups[2].Value.Split(',').Select(c => c.Trim()).ToArray();
                if (!Keys.TryGetValue(table, out var keys))
                    throw new ArgumentException($"No key for table {table} in DynamicDropshipsContent.Keys: {sql}", nameof(statements));
                var list = string.Join(",", columns);
                foreach (var row in Items(match.Groups[3].Value))
                {
                    if (!row.StartsWith("(") || !row.EndsWith(")"))
                        throw new ArgumentException($"Not a row: {row}", nameof(statements));
                    var inner = row.Substring(1, row.Length - 2);
                    var values = Items(inner);
                    if (values.Count != columns.Length)
                        throw new ArgumentException($"{columns.Length} columns, {values.Count} values: {row}", nameof(statements));
                    var key = string.Join(" AND ", keys.Select(name =>
                    {
                        var at = Array.FindIndex(columns, c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (at < 0) throw new ArgumentException($"{table}: the insert sets no {name}: {sql}", nameof(statements));
                        return $"g.{name} = {values[at]}";
                    }));
                    yield return $"INSERT INTO {table} ({list}) SELECT {inner} FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM {table} g WHERE {key});";
                }
            }
        }

        /// <summary>The comma-separated items of <paramref name="text"/> outside quoted strings and parentheses, trimmed.</summary>
        private static List<string> Items(string text)
        {
            var items = new List<string>();
            int depth = 0, start = 0;
            var quoted = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quoted)
                {
                    // '' is a quote inside the string; a lone ' ends it.
                    if (c == '\'' && i + 1 < text.Length && text[i + 1] == '\'') i++;
                    else if (c == '\'') quoted = false;
                    continue;
                }
                if (c == '\'') quoted = true;
                else if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0) { items.Add(text.Substring(start, i - start).Trim()); start = i + 1; }
            }
            items.Add(text.Substring(start).Trim());
            return items;
        }

        public static readonly Assault[] Assaults =
            PlateauDynamicDropshipsContent.Assaults.ToArray();
    }
}
