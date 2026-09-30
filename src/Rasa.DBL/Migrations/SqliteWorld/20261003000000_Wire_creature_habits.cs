using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the Filchers, Xanx and Predators their habits between fights (CreatureHabits):
    /// creature_action rows 63001-63011, each in the creature's first empty slot.
    ///
    ///  - Filchers (the base table's 60, the Plateau's and Howling Maw's region Filchers):
    ///    CR_FILCHER_LOOT 434/1 - they steal the loot off corpses nobody has open.
    ///  - Xanx (87, the Divide's 530008, the region Xanx of Pools, Ashen Desert and Thunderhead):
    ///    CR_XANX_DEVOUR 438/1 (_1_MINION); Arioch 438/2 (_2_BOSS) - they eat dead Xanx to heal.
    ///  - Predator bosses (520022, 520057): CR_PREDATOR_SCAN 425/1 - an idle scan ahead that
    ///    reveals cloaked players and starts a fight.
    ///
    /// Reuse is ours (the client gives 0): a loot every 10 s, a meal every 30 s, a scan every 8 s.
    /// Range is the client's where it has one (devour 3 m, scan 20 m); a Filcher reaches the
    /// corpse it flew to. No damage. The fighting loop never uses these rows.
    ///
    /// Not given out, as nothing that has them spawns: the Juggernaut's five actions (243, 244,
    /// 428, 492, 493 - the Prototype Juggernaut on the Plateau has no coordinates and no
    /// creature row), CR_THRAX_OFFICER_BOSS_LIFEFORCE_FUNNEL (279), CR_RECONSTRUCTOR_BOT_SCAN (443).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_habits : Migration
    {
        private const uint IdMin = 63001;
        private const uint IdMax = 63999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureHabitPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action2 = 63001 where id = 60;");   // Filcher 60
            migrationBuilder.Sql($"update {creatures} set action2 = 63002 where id = 540017;");   // Filcher 540017
            migrationBuilder.Sql($"update {creatures} set action2 = 63003 where id = 540038;");   // Filcher 540038
            migrationBuilder.Sql($"update {creatures} set action4 = 63004 where id = 87;");   // Xanx 87
            migrationBuilder.Sql($"update {creatures} set action4 = 63005 where id = 530008;");   // Xanx 530008
            migrationBuilder.Sql($"update {creatures} set action3 = 63006 where id = 540010;");   // Xanx 540010
            migrationBuilder.Sql($"update {creatures} set action3 = 63007 where id = 540020;");   // Xanx 540020
            migrationBuilder.Sql($"update {creatures} set action3 = 63008 where id = 540039;");   // Xanx 540039
            migrationBuilder.Sql($"update {creatures} set action4 = 63009 where id = 77;");   // Arioch (Xanx boss) 77
            migrationBuilder.Sql($"update {creatures} set action3 = 63010 where id = 520022;");   // Predator boss 520022
            migrationBuilder.Sql($"update {creatures} set action3 = 63011 where id = 520057;");   // Predator boss 520057
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 60 and action2 = 63001;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540017 and action2 = 63002;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540038 and action2 = 63003;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 87 and action4 = 63004;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 530008 and action4 = 63005;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540010 and action3 = 63006;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540020 and action3 = 63007;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540039 and action3 = 63008;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 77 and action4 = 63009;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520022 and action3 = 63010;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520057 and action3 = 63011;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
