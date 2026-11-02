using System;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes migrations' data rows as plain SQL (migrationBuilder.Sql), valid on SQLite and MySQL alike. An EF data
    /// operation (InsertData, UpdateData, DeleteData) without column types takes them from a model: Up from the
    /// migration's target model, Down from the one of the migration before it, and a migration with no Designer has none
    /// ("There is no entity type mapped to the table ..."). Plain SQL needs no model.
    /// </summary>
    public static class MarshesSql
    {
        public static void Insert(MigrationBuilder migration, string table, string[] columns, object[] values) =>
            migration.Sql($"INSERT INTO {table} ({string.Join(", ", columns)}) " +
                          $"VALUES ({string.Join(", ", values.Select(value => Literal(migration, value)))});");

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
