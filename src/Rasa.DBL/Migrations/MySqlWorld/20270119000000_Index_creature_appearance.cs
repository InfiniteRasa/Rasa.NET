using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// creature_appearance (id, slot_id). The table has no key (CreatureAppearanceEntry is
    /// [Keyless]) and had no index, so a creature's look (CreatureRepository.GetCreatureAppearances,
    /// the API's creature details) and a GM's change to one slot of it (CreateOrUpdateAppearance,
    /// .creatureappearance) read every row. Not unique: with no key, nothing has kept a creature
    /// to one row a slot. No data changes.
    ///
    /// Down drops it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270119000000_Index_creature_appearance")]
    public partial class Index_creature_appearance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "creature_appearance_index_creature_slot",
                table: "creature_appearance",
                columns: new[] { "id", "slot_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "creature_appearance_index_creature_slot", table: "creature_appearance");
        }
    }
}
