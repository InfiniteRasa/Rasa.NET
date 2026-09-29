using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives spawning creatures the attacks that put a game effect on a player
    /// (CreatureEffectAttacks): creature_action rows 56001-56032 and the slots that point at them.
    ///
    ///  - Treebacks (Marshes, Palisades): CR_TREEBACK_SWARM 191/1, a DECAY of 10 ticks a second
    ///    apart.
    ///  - Xanx: CR_XANX_V1_WEB 238/1 (_1_MINION) on the Xanx, CR_XANX_V2_WEB 440/2 (_2_BOSS) on
    ///    Arioch - which of V1 and V2 is which Xanx is ours; their data differ only by argument.
    ///    The web holds the player for DURATION (5 s, the boss's 10).
    ///  - Atta harvesters: CR_ATTA_HARVESTER_PHEROMONE 476, argument 1 (_SHARED_MINION) in the
    ///    world, 2 (_SHARED_BOSS) on Phuumz: physical resistance down for three minutes on
    ///    everyone within 10 m - the soldiers' blows land harder after it. Used only within those
    ///    10 m (range 0.5-10): the action's area is around the harvester, not its target.
    ///  - Miasma: CR_MIASMA_GAS_CLOUD 208/1, ice resistance down 25 for 15 s within 10 m of its
    ///    target.
    ///  - Thrax Technician bosses: CR_THRAX_DECAY 450/1 (_BOSS) and CR_THRAX_POLARITY_FIELD
    ///    456/5 (_BOSS_PHYSICAL, so its own rifle is what the vulnerability is to). Thrax
    ///    Grenadier bosses: CR_THRAX_EXPLOSIVE_NANITES 457/1 (_BOSS). The three are the Thrax
    ///    boss versions of the Specialist's Ruin, the Spy's Polarity Field and the
    ///    Demolitionist's Explosive Nanites; which Thrax gets which is ours, by class line.
    ///  - Bane Hunters with no net yet (41, 45, the Divide's 530007 and 530009): CR_HUNTER_NET
    ///    403/1, held for 5 s. The world's Hunters have had it since the world spawn areas.
    ///
    /// Arguments are picked by the names the client gives them (_MINION, _BOSS). Numbers follow
    /// the rows before them: range, reuse and windup from the argument, damage the argument's
    /// DAMAGE_AMOUNT_MIN/MAX x 2^((level - 1) / 8) x 0.25 - a boss argument already carries the
    /// boss's share. For a damage over time the row's damage is a tick's, for the nanites an
    /// explosion's; the pheromone, the gas cloud and the polarity field do no damage (0-0).
    ///
    /// Not given out: CR_FOREAN_DECAY (448), since the Foreans that spawn fight the Bane and
    /// these effects are only for players; CR_BRANN_DECAY (453), since no Brann spawns.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_effect_attacks : Migration
    {
        private const uint IdMin = 56001;
        private const uint IdMax = 56999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureEffectAttackPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action3 = 56001 where id = 531053;");   // Treeback Marshes
            migrationBuilder.Sql($"update {creatures} set action3 = 56002 where id = 531069;");   // Treeback Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 56003 where id = 87;");   // Bane Xanx
            migrationBuilder.Sql($"update {creatures} set action3 = 56004 where id = 530008;");   // Xanx Divide
            migrationBuilder.Sql($"update {creatures} set action3 = 56005 where id = 77;");   // Arioch (Xanx boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 56006 where id = 531016;");   // Atta Harvester Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action2 = 56007 where id = 531038;");   // Atta Harvester Incline
            migrationBuilder.Sql($"update {creatures} set action2 = 56008 where id = 531073;");   // Atta Harvester Plains
            migrationBuilder.Sql($"update {creatures} set action2 = 56009 where id = 531099;");   // Atta Harvester Thunderhead
            migrationBuilder.Sql($"update {creatures} set action2 = 56010 where id = 520020;");   // Phuumz (Atta Harvester boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 56011 where id = 88;");   // Bane Miasma
            migrationBuilder.Sql($"update {creatures} set action2 = 56012 where id = 531035;");   // Miasma Howling Maw
            migrationBuilder.Sql($"update {creatures} set action2 = 56013 where id = 520009;");   // Thrax Technician boss 520009
            migrationBuilder.Sql($"update {creatures} set action3 = 56014 where id = 520009;");   // Thrax Technician boss 520009
            migrationBuilder.Sql($"update {creatures} set action2 = 56015 where id = 520010;");   // Thrax Technician boss 520010
            migrationBuilder.Sql($"update {creatures} set action3 = 56016 where id = 520010;");   // Thrax Technician boss 520010
            migrationBuilder.Sql($"update {creatures} set action2 = 56017 where id = 520037;");   // Thrax Technician boss 520037
            migrationBuilder.Sql($"update {creatures} set action3 = 56018 where id = 520037;");   // Thrax Technician boss 520037
            migrationBuilder.Sql($"update {creatures} set action2 = 56019 where id = 520041;");   // Thrax Technician boss 520041
            migrationBuilder.Sql($"update {creatures} set action3 = 56020 where id = 520041;");   // Thrax Technician boss 520041
            migrationBuilder.Sql($"update {creatures} set action2 = 56021 where id = 520042;");   // Thrax Technician boss 520042
            migrationBuilder.Sql($"update {creatures} set action3 = 56022 where id = 520042;");   // Thrax Technician boss 520042
            migrationBuilder.Sql($"update {creatures} set action2 = 56023 where id = 520045;");   // Thrax Technician boss 520045
            migrationBuilder.Sql($"update {creatures} set action3 = 56024 where id = 520045;");   // Thrax Technician boss 520045
            migrationBuilder.Sql($"update {creatures} set action2 = 56025 where id = 520052;");   // Thrax Technician boss 520052
            migrationBuilder.Sql($"update {creatures} set action3 = 56026 where id = 520052;");   // Thrax Technician boss 520052
            migrationBuilder.Sql($"update {creatures} set action2 = 56027 where id = 520008;");   // Thrax Grenadier boss 520008
            migrationBuilder.Sql($"update {creatures} set action2 = 56028 where id = 520061;");   // Thrax Grenadier boss 520061
            migrationBuilder.Sql($"update {creatures} set action3 = 56029 where id = 41;");   // Bane Hunter 41
            migrationBuilder.Sql($"update {creatures} set action3 = 56030 where id = 45;");   // Bane Hunter 45
            migrationBuilder.Sql($"update {creatures} set action3 = 56031 where id = 530007;");   // Bane Hunter 530007
            migrationBuilder.Sql($"update {creatures} set action3 = 56032 where id = 530009;");   // Bane Hunter 530009
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531053 and action3 = 56001;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531069 and action3 = 56002;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 87 and action3 = 56003;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 530008 and action3 = 56004;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 77 and action3 = 56005;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531016 and action2 = 56006;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531038 and action2 = 56007;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531073 and action2 = 56008;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531099 and action2 = 56009;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520020 and action2 = 56010;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 88 and action2 = 56011;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531035 and action2 = 56012;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520009 and action2 = 56013;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520009 and action3 = 56014;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520010 and action2 = 56015;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520010 and action3 = 56016;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520037 and action2 = 56017;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520037 and action3 = 56018;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520041 and action2 = 56019;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520041 and action3 = 56020;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520042 and action2 = 56021;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520042 and action3 = 56022;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520045 and action2 = 56023;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520045 and action3 = 56024;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520052 and action2 = 56025;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520052 and action3 = 56026;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520008 and action2 = 56027;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520061 and action2 = 56028;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 41 and action3 = 56029;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 45 and action3 = 56030;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 530007 and action3 = 56031;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 530009 and action3 = 56032;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
