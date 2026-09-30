using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives Atropos, the Linker boss (78), the world Linkers' chest blast and hand blast:
    /// creature_action rows 67001-67002, slots 3 and 4, after its gun and ground blast.
    /// CR_LINKER_CHESTBLAST 263/1 (_SHARED - the action has no boss argument), damage x0.5 as a
    /// boss on an argument not named for one; CR_LINKER_HAND_BLAST 412/2 (_SHARED_BOSS) at x0.25,
    /// as its ground blast 264/2. Range, reuse and windup are the client's. Not the channel:
    /// it feeds another Linker, and none fights beside Atropos.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_atropos_abilities : Migration
    {
        private const uint IdMin = 67001;
        private const uint IdMax = 67999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureAtroposPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action3 = 67001 where id = 78;");   // Atropos (Linker boss)
            migrationBuilder.Sql($"update {creatures} set action4 = 67002 where id = 78;");   // Atropos (Linker boss)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 78 and action3 = 67001;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 78 and action4 = 67002;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
