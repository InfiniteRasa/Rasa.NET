using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The named NPCs with recorded barks who had no creature row (UnplacedNamedNpcs): Johnson,
    /// Yates, Elvers, Field Engineer Hernandez, Ryosa and Comm. Officer Jennings, with their looks
    /// and barks, and no spawn pool. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270117000000_Unplaced_named_npcs")]
    public class Unplaced_named_npcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in UnplacedNamedNpcs.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in UnplacedNamedNpcs.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
