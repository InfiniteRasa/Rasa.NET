using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// What the eight "Debuff ... Resist" weapon modules do, at their five strengths: 40 rows of
    /// module_effect (ItemModuleSeed.DebuffEffects), which Add_item_modules left out because
    /// their line needs a duration and the client has none. No table changes.
    ///
    /// Down takes the 40 rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261122000000_Add_resist_debuff_module_effects")]
    public partial class Add_resist_debuff_module_effects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ItemModuleSeed.DebuffInsertStatements)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ItemModuleSeed.DebuffDeleteStatement);
        }
    }
}
