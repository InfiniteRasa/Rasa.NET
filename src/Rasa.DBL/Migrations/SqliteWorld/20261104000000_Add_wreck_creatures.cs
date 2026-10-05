using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The creatures that leave a wreck (WreckCreatures): ALT_MESH on the five turret
    /// classes and ALT_MESH_DELAYED_3500 on the Predator and the Ravager in creature_class_flag,
    /// and creature rows for the four of them that had none - the Bane Mortar, the Bane Light
    /// Mortar, the Brann Turret and the Ravager (590001-590004), each with its gun (71001-71004),
    /// stats and weapon appearance. No spawn pool has the new rows.
    ///
    /// Down deletes the seven flag rows and the closed id ranges.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261104000000_Add_wreck_creatures")]
    public partial class Add_wreck_creatures : Migration
    {
        private static readonly string[] Tables =
        {
            CreatureAppearanceEntry.TableName,
            CreatureStatEntry.TableName,
            CreatureEntry.TableName
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var insert in WreckCreatures.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"delete from {table} where id between {WreckCreatures.FirstId} and {WreckCreatures.LastId};");

            migrationBuilder.Sql($"delete from {CreatureActionEntry.TableName} where id between {WreckCreatures.FirstActionId} and {WreckCreatures.LastActionId};");
            migrationBuilder.Sql($"delete from {CreatureClassFlagEntry.TableName} where {WreckCreatures.ClassFlagRows};");
        }
    }
}
