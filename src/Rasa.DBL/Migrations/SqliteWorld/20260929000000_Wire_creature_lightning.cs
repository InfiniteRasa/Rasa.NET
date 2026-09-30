using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the lightning attacks (CreatureLightning) to the creatures that spawn without them:
    /// creature_action rows 57001-57033 and the slots that point at them.
    ///
    ///  - The Mox boss (520053): CR_MOX_ENERGY_ATTACK 209/2 (_SHARED_BOSS) beside its tail swipe -
    ///    the guide's "lightning sock at range", as the rank and file already have it.
    ///  - The Warnet Queen bosses (520016, 520046): CR_WARNET_QUEEN 210/1 (_1_NORMAL), a bolt
    ///    that carries half of itself again and arcs to a second player within 15 m (stun 20%,
    ///    2 s), and CR_WARNET_ZAP 189/1 at 20 m, in place of the Thrax soldier melee weapon
    ///    they were given. A flying zapper: no melee.
    ///  - The Beam Manta boss (520049): CR_BEAMMANTA_LIGHTNING 248/2 (_SHARED_BOSS), in place of
    ///    the Thrax melee. Its argument gives a range of 0 and a reuse of 0; 20 m, the Beam
    ///    Manta weapon's range, and 4 s, about its windup and recovery, are ours.
    ///  - The Thrax Soldier bosses (27 of them): CR_THRAX_LIGHTNING 449/1 (_BOSS) beside their
    ///    rifle - the Recruit's Lightning, a Thrax boss's version.
    ///
    /// Numbers as the rows before them: range, reuse and windup from the argument, damage the
    /// argument's DAMAGE_AMOUNT_MIN/MAX x 2^((level - 1) / 8) x 0.25, a boss not on a boss
    /// argument x 0.5 (the Warnet Queen's _1_NORMAL and the zap's _1_SHARED).
    ///
    /// Not given out: CR_BRANN_LIGHTNING (452), no Brann spawns; the Warnet Queen's 210 on
    /// ordinary Warnets, whose spawns have the zap.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_lightning : Migration
    {
        private const uint IdMin = 57001;
        private const uint IdMax = 57999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureLightningPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action2 = 57001 where id = 520053;");   // Mox boss 520053
            migrationBuilder.Sql($"update {creatures} set action1 = 57002 where id = 520016;");   // Warnet Queen boss 520016
            migrationBuilder.Sql($"update {creatures} set action2 = 57003 where id = 520016;");   // Warnet Queen boss 520016
            migrationBuilder.Sql($"update {creatures} set action1 = 57004 where id = 520046;");   // Warnet Queen boss 520046
            migrationBuilder.Sql($"update {creatures} set action2 = 57005 where id = 520046;");   // Warnet Queen boss 520046
            migrationBuilder.Sql($"update {creatures} set action1 = 57006 where id = 520049;");   // Beam Manta boss 520049
            migrationBuilder.Sql($"update {creatures} set action2 = 57007 where id = 520003;");   // Thrax Soldier boss 520003
            migrationBuilder.Sql($"update {creatures} set action2 = 57008 where id = 520004;");   // Thrax Soldier boss 520004
            migrationBuilder.Sql($"update {creatures} set action2 = 57009 where id = 520005;");   // Thrax Soldier boss 520005
            migrationBuilder.Sql($"update {creatures} set action2 = 57010 where id = 520006;");   // Thrax Soldier boss 520006
            migrationBuilder.Sql($"update {creatures} set action2 = 57011 where id = 520007;");   // Thrax Soldier boss 520007
            migrationBuilder.Sql($"update {creatures} set action2 = 57012 where id = 520011;");   // Thrax Soldier boss 520011
            migrationBuilder.Sql($"update {creatures} set action2 = 57013 where id = 520013;");   // Thrax Soldier boss 520013
            migrationBuilder.Sql($"update {creatures} set action2 = 57014 where id = 520014;");   // Thrax Soldier boss 520014
            migrationBuilder.Sql($"update {creatures} set action2 = 57015 where id = 520019;");   // Thrax Soldier boss 520019
            migrationBuilder.Sql($"update {creatures} set action2 = 57016 where id = 520021;");   // Thrax Soldier boss 520021
            migrationBuilder.Sql($"update {creatures} set action2 = 57017 where id = 520023;");   // Thrax Soldier boss 520023
            migrationBuilder.Sql($"update {creatures} set action2 = 57018 where id = 520024;");   // Thrax Soldier boss 520024
            migrationBuilder.Sql($"update {creatures} set action2 = 57019 where id = 520025;");   // Thrax Soldier boss 520025
            migrationBuilder.Sql($"update {creatures} set action2 = 57020 where id = 520026;");   // Thrax Soldier boss 520026
            migrationBuilder.Sql($"update {creatures} set action2 = 57021 where id = 520027;");   // Thrax Soldier boss 520027
            migrationBuilder.Sql($"update {creatures} set action2 = 57022 where id = 520029;");   // Thrax Soldier boss 520029
            migrationBuilder.Sql($"update {creatures} set action2 = 57023 where id = 520030;");   // Thrax Soldier boss 520030
            migrationBuilder.Sql($"update {creatures} set action2 = 57024 where id = 520032;");   // Thrax Soldier boss 520032
            migrationBuilder.Sql($"update {creatures} set action2 = 57025 where id = 520033;");   // Thrax Soldier boss 520033
            migrationBuilder.Sql($"update {creatures} set action2 = 57026 where id = 520034;");   // Thrax Soldier boss 520034
            migrationBuilder.Sql($"update {creatures} set action2 = 57027 where id = 520035;");   // Thrax Soldier boss 520035
            migrationBuilder.Sql($"update {creatures} set action2 = 57028 where id = 520043;");   // Thrax Soldier boss 520043
            migrationBuilder.Sql($"update {creatures} set action2 = 57029 where id = 520048;");   // Thrax Soldier boss 520048
            migrationBuilder.Sql($"update {creatures} set action2 = 57030 where id = 520054;");   // Thrax Soldier boss 520054
            migrationBuilder.Sql($"update {creatures} set action2 = 57031 where id = 520058;");   // Thrax Soldier boss 520058
            migrationBuilder.Sql($"update {creatures} set action2 = 57032 where id = 520060;");   // Thrax Soldier boss 520060
            migrationBuilder.Sql($"update {creatures} set action2 = 57033 where id = 520062;");   // Thrax Soldier boss 520062
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520053 and action2 = 57001;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520016 and action1 = 57002;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520016 and action2 = 57003;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520046 and action1 = 57004;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520046 and action2 = 57005;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520049 and action1 = 57006;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520003 and action2 = 57007;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520004 and action2 = 57008;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520005 and action2 = 57009;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520006 and action2 = 57010;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520007 and action2 = 57011;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520011 and action2 = 57012;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520013 and action2 = 57013;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520014 and action2 = 57014;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520019 and action2 = 57015;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520021 and action2 = 57016;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520023 and action2 = 57017;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520024 and action2 = 57018;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520025 and action2 = 57019;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520026 and action2 = 57020;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520027 and action2 = 57021;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520029 and action2 = 57022;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520030 and action2 = 57023;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520032 and action2 = 57024;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520033 and action2 = 57025;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520034 and action2 = 57026;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520035 and action2 = 57027;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520043 and action2 = 57028;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520048 and action2 = 57029;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520054 and action2 = 57030;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520058 and action2 = 57031;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520060 and action2 = 57032;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520062 and action2 = 57033;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
