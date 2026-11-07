using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible migrations' data rows as plain SQL (migrationBuilder.Sql), valid on SQLite and MySQL alike. An EF data
    /// operation (InsertData, UpdateData, DeleteData) without column types takes them from a model: Up from the
    /// migration's target model, Down from the one of the migration before it, and a migration with no Designer has none
    /// ("There is no entity type mapped to the table ..."). Plain SQL needs no model. Every insert goes through
    /// <see cref="Guard"/>, so each Up and Down is safe to run twice.
    /// </summary>
    public static class CrucibleSql
    {
        /// <summary>
        /// The key of each table a Crucible migration inserts into: the primary key of its entity in WorldContext's model
        /// (CrucibleGuardTests pins it), or, for the keyless creature_appearance, the columns of its index
        /// creature_appearance_index_creature_slot (one row per creature and slot). <see cref="Guard"/> skips a row whose
        /// key is already there.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Keys = new Dictionary<string, string[]>
        {
            ["creature"] = new[] { "id" },
            ["creature_action"] = new[] { "id" },
            ["creature_appearance"] = new[] { "id", "slot_id" },
            ["creature_stat"] = new[] { "id" },
            ["npc_package"] = new[] { "id" },
            ["spawnpool"] = new[] { "id" },
            ["teleporter"] = new[] { "id" },
            ["local_teleporter_link"] = new[] { "from_teleporter_id" },
            ["npc_squad_type"] = new[] { "id" },
            ["npc_squad_member"] = new[] { "type_id", "slot" },
            ["npc_squad_path"] = new[] { "id" },
            ["npc_squad_path_point"] = new[] { "path_id", "step" },
            ["npc_squad_spawn"] = new[] { "id" },
            ["mission_content_definition"] = new[] { "mission_id", "content_revision" },
            ["mission_channel_policy"] = new[] { "mission_id", "content_revision" },
            ["mission_repeat_policy"] = new[] { "mission_id", "content_revision" },
            ["mission_scene_binding"] = new[] { "mission_id", "content_revision" },
            ["mission_evidence"] = new[] { "mission_id", "content_revision", "evidence_id" },
            ["mission_prerequisite"] = new[] { "mission_id", "content_revision", "prerequisite_id" },
            ["mission_objective_definition"] = new[] { "mission_id", "content_revision", "objective_id" },
            ["mission_objective_transition"] = new[] { "mission_id", "content_revision", "objective_id", "transition_id" },
            ["mission_trigger"] = new[] { "mission_id", "content_revision", "objective_id", "transition_id", "trigger_id" },
            ["mission_action"] = new[] { "mission_id", "content_revision", "objective_id", "transition_id", "action_id" },
            ["mission_indicator"] = new[] { "mission_id", "content_revision", "objective_id", "indicator_id" },
            ["mission_area"] = new[] { "mission_id", "content_revision", "area_id" },
            ["mission_scenario"] = new[] { "mission_id", "content_revision", "scenario_id" },
            ["mission_scenario_step"] = new[] { "mission_id", "content_revision", "scenario_id", "step_id" },
            ["mission_spawn_group"] = new[] { "mission_id", "content_revision", "spawn_group_id" },
            ["mission_spawn"] = new[] { "mission_id", "content_revision", "spawn_group_id", "spawn_id" },
            ["mission_reward_definition"] = new[] { "mission_id", "content_revision", "reward_id" },
            ["mission_reward_item"] = new[] { "mission_id", "content_revision", "reward_id", "item_id" }
        };

        public static void Insert(MigrationBuilder migration, string table, string[] columns, object[] values) =>
            migration.Sql(Guard($"INSERT INTO {table} ({string.Join(", ", columns)}) " +
                                $"VALUES ({string.Join(", ", values.Select(value => Literal(migration, value)))});"));

        /// <summary>
        /// <paramref name="sql"/> safe to run twice: the same rows when their keys (<see cref="Keys"/>) are not there yet,
        /// none when they are. An <c>INSERT INTO t (columns) VALUES (...)</c> of one row becomes upstream's form
        /// <c>INSERT INTO t (columns) SELECT ... FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM t g WHERE g.key = value ...)</c>
        /// (as TwinPillarsNpcs); an <c>INSERT INTO t (columns) SELECT ...</c> (a copy of other rows) becomes
        /// <c>INSERT INTO t (columns) WITH s (columns) AS (SELECT ...) SELECT columns FROM s WHERE NOT EXISTS (SELECT 1 FROM t g WHERE g.key = s.key ...)</c>.
        /// Both are valid on SQLite and MySQL 8. Not INSERT OR IGNORE / INSERT IGNORE: those hide every other error too.
        /// Any other statement (UPDATE, DELETE) comes back as it is: it is safe to run twice already.
        /// </summary>
        public static string Guard(string sql)
        {
            var text = sql.Trim();
            if (!text.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return sql;
            var header = Regex.Match(text, @"^INSERT\s+INTO\s+(\w+)\s*\(([^()]*)\)\s*", RegexOptions.IgnoreCase);
            if (!header.Success)
                throw new ArgumentException($"Not an INSERT INTO table (columns) ...: {sql}", nameof(sql));
            var table = header.Groups[1].Value;
            var columns = header.Groups[2].Value.Split(',').Select(column => column.Trim()).ToArray();
            if (!Keys.TryGetValue(table, out var keys))
                throw new ArgumentException($"No key for table {table} in CrucibleSql.Keys: {sql}", nameof(sql));
            var missing = keys.Where(key => !columns.Contains(key, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (missing.Length > 0)
                throw new ArgumentException($"{table}: the insert sets no {string.Join(", ", missing)}: {sql}", nameof(sql));

            var body = text.Substring(header.Length).TrimEnd(';', ' ', '\r', '\n', '\t');
            var list = string.Join(", ", columns);
            if (Regex.IsMatch(body, @"^VALUES\s*\(", RegexOptions.IgnoreCase))
            {
                var open = body.IndexOf('(');
                var close = Closing(body, open);
                if (close != body.Length - 1)
                    throw new ArgumentException($"Only one row per INSERT ... VALUES: {sql}", nameof(sql));
                var inner = body.Substring(open + 1, close - open - 1);
                var values = Split(inner);
                if (values.Count != columns.Length)
                    throw new ArgumentException($"{columns.Length} columns, {values.Count} values: {sql}", nameof(sql));
                var key = string.Join(" AND ", keys.Select(name =>
                    $"g.{name} = {values[Array.FindIndex(columns, column => column.Equals(name, StringComparison.OrdinalIgnoreCase))]}"));
                return $"INSERT INTO {table} ({list}) SELECT {inner} FROM (SELECT 1 AS one) d " +
                       $"WHERE NOT EXISTS (SELECT 1 FROM {table} g WHERE {key});";
            }
            if (!Regex.IsMatch(body, @"^SELECT\s", RegexOptions.IgnoreCase))
                throw new ArgumentException($"Neither VALUES nor SELECT: {sql}", nameof(sql));
            var match = string.Join(" AND ", keys.Select(name => $"g.{name} = s.{name}"));
            return $"INSERT INTO {table} ({list}) WITH s ({list}) AS ({body}) " +
                   $"SELECT {list} FROM s WHERE NOT EXISTS (SELECT 1 FROM {table} g WHERE {match});";
        }

        /// <summary>The comma-separated items of <paramref name="text"/> outside quoted strings and parentheses, trimmed.</summary>
        private static List<string> Split(string text)
        {
            var items = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                i = SkipString(text, i);
                if (text[i] == '(')
                    depth++;
                else if (text[i] == ')')
                    depth--;
                else if (text[i] == ',' && depth == 0)
                {
                    items.Add(text.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            items.Add(text.Substring(start).Trim());
            return items;
        }

        /// <summary>At a quote, the index of the quote that ends its string ('' is a quote in one); elsewhere <paramref name="i"/>.</summary>
        private static int SkipString(string text, int i)
        {
            if (text[i] != '\'')
                return i;
            for (i++; i < text.Length; i++)
            {
                if (text[i] != '\'')
                    continue;
                if (i + 1 < text.Length && text[i + 1] == '\'')
                    i++;
                else
                    return i;
            }
            throw new ArgumentException($"Unclosed string: {text}", nameof(text));
        }

        /// <summary>The index of the parenthesis that closes the one at <paramref name="open"/>, outside quoted strings ('' is a quote in one).</summary>
        private static int Closing(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                i = SkipString(text, i);
                if (text[i] == '(')
                    depth++;
                else if (text[i] == ')' && --depth == 0)
                    return i;
            }
            throw new ArgumentException($"Unbalanced parentheses: {text}", nameof(text));
        }

        /// <summary>Sets <paramref name="column"/> of one mission's row of <paramref name="table"/>.</summary>
        public static void Update(MigrationBuilder migration, string table, uint missionId, string revision, string column, object value) =>
            migration.Sql($"UPDATE {table} SET {column} = {Literal(migration, value)} " +
                          $"WHERE mission_id = {missionId} AND content_revision = {Literal(migration, revision)};");

        /// <summary>A value as an SQL literal: NULL, 0/1 for a bool, an invariant number, a quoted string.</summary>
        public static string Literal(MigrationBuilder migration, object value) => value switch
        {
            null => "NULL",
            bool flag => flag ? "1" : "0",
            string text => Quote(migration, text),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => ((double)number).ToString("R", CultureInfo.InvariantCulture),
            Enum member => Convert.ToInt64(member, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"No SQL literal for {value.GetType()}", nameof(value))
        };

        /// <summary>A quoted string; MySQL also reads a backslash as an escape, SQLite does not.</summary>
        private static string Quote(MigrationBuilder migration, string text)
        {
            if (migration.ActiveProvider?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
                text = text.Replace("\\", "\\\\");
            return "'" + text.Replace("'", "''") + "'";
        }
    }
}
