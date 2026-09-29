using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the Thrax Soldier bosses and the Atta harvesters the actions they perform on
    /// themselves or their own side (CreatureBuffs): creature_action rows 61001-61059 and the slots
    /// that point at them.
    ///
    ///  - The 27 Thrax Soldier bosses: CR_THRAX_RAGE 454/1 (_BOSS) in action3 and
    ///    CR_THRAX_SCOURGE 455/1 (_BOSS) in action4, beside their rifle and lightning - the
    ///    Soldier's Rage and the Commando's Scourge, the Soldier line's, as their lightning is the
    ///    Recruit's. Rage for 30 s every 30 s, the Bane within 15 m raging with it; Scourge for
    ///    15 s every 20 s on every player within 10 m.
    ///  - The Atta harvesters (Ashen Desert, Incline, Plains, Thunderhead) and Phuumz:
    ///    CR_ATTA_HARVESTER_WARCRY 477/1 in action3, calling two of their own side from up to 80 m
    ///    into the fight, once a minute at most.
    ///
    /// Ranges are ours, since the actions are TARGET_SELF / TARGET_FRIENDLY and their ranges are
    /// not about the target: Rage and the warcry are used while the target is within 40 m,
    /// Scourge within its 10 m radius. Reuse and windup from the argument. Scourge's damage is a
    /// tick's: the argument's 4-9 x 2^((level - 1) / 8) x 0.25; Rage and the warcry do none.
    ///
    /// Not given out: CR_FOREAN_CHAFF (the Foreans that spawn fight the Bane), CR_THRAX_CHAFF_TEST
    /// and CR_ATTA_QUEEN_WARCRY (no data), CR_WARDEN_BOT_TURTLE (an animation with no numbers and
    /// no effect: nothing says what the turtle does).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_buffs : Migration
    {
        private const uint IdMin = 61001;
        private const uint IdMax = 61999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureBuffPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action3 = 61001 where id = 520003;");   // Thrax Soldier boss 520003
            migrationBuilder.Sql($"update {creatures} set action4 = 61002 where id = 520003;");   // Thrax Soldier boss 520003
            migrationBuilder.Sql($"update {creatures} set action3 = 61003 where id = 520004;");   // Thrax Soldier boss 520004
            migrationBuilder.Sql($"update {creatures} set action4 = 61004 where id = 520004;");   // Thrax Soldier boss 520004
            migrationBuilder.Sql($"update {creatures} set action3 = 61005 where id = 520005;");   // Thrax Soldier boss 520005
            migrationBuilder.Sql($"update {creatures} set action4 = 61006 where id = 520005;");   // Thrax Soldier boss 520005
            migrationBuilder.Sql($"update {creatures} set action3 = 61007 where id = 520006;");   // Thrax Soldier boss 520006
            migrationBuilder.Sql($"update {creatures} set action4 = 61008 where id = 520006;");   // Thrax Soldier boss 520006
            migrationBuilder.Sql($"update {creatures} set action3 = 61009 where id = 520007;");   // Thrax Soldier boss 520007
            migrationBuilder.Sql($"update {creatures} set action4 = 61010 where id = 520007;");   // Thrax Soldier boss 520007
            migrationBuilder.Sql($"update {creatures} set action3 = 61011 where id = 520011;");   // Thrax Soldier boss 520011
            migrationBuilder.Sql($"update {creatures} set action4 = 61012 where id = 520011;");   // Thrax Soldier boss 520011
            migrationBuilder.Sql($"update {creatures} set action3 = 61013 where id = 520013;");   // Thrax Soldier boss 520013
            migrationBuilder.Sql($"update {creatures} set action4 = 61014 where id = 520013;");   // Thrax Soldier boss 520013
            migrationBuilder.Sql($"update {creatures} set action3 = 61015 where id = 520014;");   // Thrax Soldier boss 520014
            migrationBuilder.Sql($"update {creatures} set action4 = 61016 where id = 520014;");   // Thrax Soldier boss 520014
            migrationBuilder.Sql($"update {creatures} set action3 = 61017 where id = 520019;");   // Thrax Soldier boss 520019
            migrationBuilder.Sql($"update {creatures} set action4 = 61018 where id = 520019;");   // Thrax Soldier boss 520019
            migrationBuilder.Sql($"update {creatures} set action3 = 61019 where id = 520021;");   // Thrax Soldier boss 520021
            migrationBuilder.Sql($"update {creatures} set action4 = 61020 where id = 520021;");   // Thrax Soldier boss 520021
            migrationBuilder.Sql($"update {creatures} set action3 = 61021 where id = 520023;");   // Thrax Soldier boss 520023
            migrationBuilder.Sql($"update {creatures} set action4 = 61022 where id = 520023;");   // Thrax Soldier boss 520023
            migrationBuilder.Sql($"update {creatures} set action3 = 61023 where id = 520024;");   // Thrax Soldier boss 520024
            migrationBuilder.Sql($"update {creatures} set action4 = 61024 where id = 520024;");   // Thrax Soldier boss 520024
            migrationBuilder.Sql($"update {creatures} set action3 = 61025 where id = 520025;");   // Thrax Soldier boss 520025
            migrationBuilder.Sql($"update {creatures} set action4 = 61026 where id = 520025;");   // Thrax Soldier boss 520025
            migrationBuilder.Sql($"update {creatures} set action3 = 61027 where id = 520026;");   // Thrax Soldier boss 520026
            migrationBuilder.Sql($"update {creatures} set action4 = 61028 where id = 520026;");   // Thrax Soldier boss 520026
            migrationBuilder.Sql($"update {creatures} set action3 = 61029 where id = 520027;");   // Thrax Soldier boss 520027
            migrationBuilder.Sql($"update {creatures} set action4 = 61030 where id = 520027;");   // Thrax Soldier boss 520027
            migrationBuilder.Sql($"update {creatures} set action3 = 61031 where id = 520029;");   // Thrax Soldier boss 520029
            migrationBuilder.Sql($"update {creatures} set action4 = 61032 where id = 520029;");   // Thrax Soldier boss 520029
            migrationBuilder.Sql($"update {creatures} set action3 = 61033 where id = 520030;");   // Thrax Soldier boss 520030
            migrationBuilder.Sql($"update {creatures} set action4 = 61034 where id = 520030;");   // Thrax Soldier boss 520030
            migrationBuilder.Sql($"update {creatures} set action3 = 61035 where id = 520032;");   // Thrax Soldier boss 520032
            migrationBuilder.Sql($"update {creatures} set action4 = 61036 where id = 520032;");   // Thrax Soldier boss 520032
            migrationBuilder.Sql($"update {creatures} set action3 = 61037 where id = 520033;");   // Thrax Soldier boss 520033
            migrationBuilder.Sql($"update {creatures} set action4 = 61038 where id = 520033;");   // Thrax Soldier boss 520033
            migrationBuilder.Sql($"update {creatures} set action3 = 61039 where id = 520034;");   // Thrax Soldier boss 520034
            migrationBuilder.Sql($"update {creatures} set action4 = 61040 where id = 520034;");   // Thrax Soldier boss 520034
            migrationBuilder.Sql($"update {creatures} set action3 = 61041 where id = 520035;");   // Thrax Soldier boss 520035
            migrationBuilder.Sql($"update {creatures} set action4 = 61042 where id = 520035;");   // Thrax Soldier boss 520035
            migrationBuilder.Sql($"update {creatures} set action3 = 61043 where id = 520043;");   // Thrax Soldier boss 520043
            migrationBuilder.Sql($"update {creatures} set action4 = 61044 where id = 520043;");   // Thrax Soldier boss 520043
            migrationBuilder.Sql($"update {creatures} set action3 = 61045 where id = 520048;");   // Thrax Soldier boss 520048
            migrationBuilder.Sql($"update {creatures} set action4 = 61046 where id = 520048;");   // Thrax Soldier boss 520048
            migrationBuilder.Sql($"update {creatures} set action3 = 61047 where id = 520054;");   // Thrax Soldier boss 520054
            migrationBuilder.Sql($"update {creatures} set action4 = 61048 where id = 520054;");   // Thrax Soldier boss 520054
            migrationBuilder.Sql($"update {creatures} set action3 = 61049 where id = 520058;");   // Thrax Soldier boss 520058
            migrationBuilder.Sql($"update {creatures} set action4 = 61050 where id = 520058;");   // Thrax Soldier boss 520058
            migrationBuilder.Sql($"update {creatures} set action3 = 61051 where id = 520060;");   // Thrax Soldier boss 520060
            migrationBuilder.Sql($"update {creatures} set action4 = 61052 where id = 520060;");   // Thrax Soldier boss 520060
            migrationBuilder.Sql($"update {creatures} set action3 = 61053 where id = 520062;");   // Thrax Soldier boss 520062
            migrationBuilder.Sql($"update {creatures} set action4 = 61054 where id = 520062;");   // Thrax Soldier boss 520062
            migrationBuilder.Sql($"update {creatures} set action3 = 61055 where id = 531016;");   // Atta Harvester Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action3 = 61056 where id = 531038;");   // Atta Harvester Incline
            migrationBuilder.Sql($"update {creatures} set action3 = 61057 where id = 531073;");   // Atta Harvester Plains
            migrationBuilder.Sql($"update {creatures} set action3 = 61058 where id = 531099;");   // Atta Harvester Thunderhead
            migrationBuilder.Sql($"update {creatures} set action3 = 61059 where id = 520020;");   // Phuumz (Atta Harvester boss)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520003 and action3 = 61001;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520003 and action4 = 61002;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520004 and action3 = 61003;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520004 and action4 = 61004;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520005 and action3 = 61005;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520005 and action4 = 61006;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520006 and action3 = 61007;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520006 and action4 = 61008;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520007 and action3 = 61009;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520007 and action4 = 61010;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520011 and action3 = 61011;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520011 and action4 = 61012;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520013 and action3 = 61013;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520013 and action4 = 61014;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520014 and action3 = 61015;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520014 and action4 = 61016;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520019 and action3 = 61017;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520019 and action4 = 61018;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520021 and action3 = 61019;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520021 and action4 = 61020;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520023 and action3 = 61021;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520023 and action4 = 61022;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520024 and action3 = 61023;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520024 and action4 = 61024;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520025 and action3 = 61025;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520025 and action4 = 61026;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520026 and action3 = 61027;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520026 and action4 = 61028;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520027 and action3 = 61029;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520027 and action4 = 61030;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520029 and action3 = 61031;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520029 and action4 = 61032;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520030 and action3 = 61033;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520030 and action4 = 61034;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520032 and action3 = 61035;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520032 and action4 = 61036;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520033 and action3 = 61037;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520033 and action4 = 61038;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520034 and action3 = 61039;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520034 and action4 = 61040;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520035 and action3 = 61041;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520035 and action4 = 61042;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520043 and action3 = 61043;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520043 and action4 = 61044;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520048 and action3 = 61045;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520048 and action4 = 61046;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520054 and action3 = 61047;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520054 and action4 = 61048;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520058 and action3 = 61049;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520058 and action4 = 61050;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520060 and action3 = 61051;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520060 and action4 = 61052;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520062 and action3 = 61053;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520062 and action4 = 61054;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531016 and action3 = 61055;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531038 and action3 = 61056;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531073 and action3 = 61057;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531099 and action3 = 61058;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520020 and action3 = 61059;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
