using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the Caretakers, Thrax Technicians and Hominis Machina what they do for their own side
    /// (CreatureSupport): creature_action rows 62001-62061, each in the creature's first empty slot.
    ///
    ///  - Caretakers (the world's 13 zones, the base table's 2, the Divide's 530004):
    ///    CR_CARETAKER_HEAL 239/1 and CR_CARETAKER_REVIVE 242/1 (_1_MINION_SHARED); the Caretaker
    ///    bosses (79, 520015, 520028) the same at argument 3 (_3_BOSS_SHARED).
    ///  - Thrax Technicians (the world's 13 zones, 47, the Divide's 530003): CR_TECHNICIAN_HEAL
    ///    275/1 (_SHARED_MINION); the Thrax Technician bosses 275/2 (_SHARED_BOSS).
    ///  - Hominis Machina (8, and the bosses 520012, 520017): CR_MACHINA_REVIVE 429/1.
    ///
    /// Reuse is ours where the client gives 0 or next to it: the heal every 10 s, the repair every
    /// 8 s, the revive every 30 s. The revive's range, 40 m, is the client's and is how far it
    /// looks for a corpse; the heal and repair reach RADIUS_AROUND_SOURCE, and the rows have no
    /// range of their own (0-0), which also keeps the fighting loop off them. No damage: the
    /// amounts are HEAL_AMOUNT scaled to the caster's level at the time.
    ///
    /// Not given out: CR_TECHNICIAN_REVIVE (400), whose 1 m reach wants the Technician to walk to
    /// the body; CR_NEOBOT_REPAIR (468), no NeoBot spawns; CR_LINKER_CHANNEL (410), whose class
    /// does nothing with its hits and whose data gives damage to a friendly target - no creature
    /// uses its row (39), so it does not fire at players either.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_support : Migration
    {
        private const uint IdMin = 62001;
        private const uint IdMax = 62999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureSupportPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action2 = 62001 where id = 531004;");   // Caretaker 531004
            migrationBuilder.Sql($"update {creatures} set action3 = 62002 where id = 531004;");   // Caretaker 531004
            migrationBuilder.Sql($"update {creatures} set action2 = 62003 where id = 531011;");   // Caretaker 531011
            migrationBuilder.Sql($"update {creatures} set action3 = 62004 where id = 531011;");   // Caretaker 531011
            migrationBuilder.Sql($"update {creatures} set action2 = 62005 where id = 531021;");   // Caretaker 531021
            migrationBuilder.Sql($"update {creatures} set action3 = 62006 where id = 531021;");   // Caretaker 531021
            migrationBuilder.Sql($"update {creatures} set action2 = 62007 where id = 531027;");   // Caretaker 531027
            migrationBuilder.Sql($"update {creatures} set action3 = 62008 where id = 531027;");   // Caretaker 531027
            migrationBuilder.Sql($"update {creatures} set action2 = 62009 where id = 531032;");   // Caretaker 531032
            migrationBuilder.Sql($"update {creatures} set action3 = 62010 where id = 531032;");   // Caretaker 531032
            migrationBuilder.Sql($"update {creatures} set action2 = 62011 where id = 531043;");   // Caretaker 531043
            migrationBuilder.Sql($"update {creatures} set action3 = 62012 where id = 531043;");   // Caretaker 531043
            migrationBuilder.Sql($"update {creatures} set action2 = 62013 where id = 531048;");   // Caretaker 531048
            migrationBuilder.Sql($"update {creatures} set action3 = 62014 where id = 531048;");   // Caretaker 531048
            migrationBuilder.Sql($"update {creatures} set action2 = 62015 where id = 531058;");   // Caretaker 531058
            migrationBuilder.Sql($"update {creatures} set action3 = 62016 where id = 531058;");   // Caretaker 531058
            migrationBuilder.Sql($"update {creatures} set action2 = 62017 where id = 531066;");   // Caretaker 531066
            migrationBuilder.Sql($"update {creatures} set action3 = 62018 where id = 531066;");   // Caretaker 531066
            migrationBuilder.Sql($"update {creatures} set action2 = 62019 where id = 531077;");   // Caretaker 531077
            migrationBuilder.Sql($"update {creatures} set action3 = 62020 where id = 531077;");   // Caretaker 531077
            migrationBuilder.Sql($"update {creatures} set action2 = 62021 where id = 531085;");   // Caretaker 531085
            migrationBuilder.Sql($"update {creatures} set action3 = 62022 where id = 531085;");   // Caretaker 531085
            migrationBuilder.Sql($"update {creatures} set action2 = 62023 where id = 531092;");   // Caretaker 531092
            migrationBuilder.Sql($"update {creatures} set action3 = 62024 where id = 531092;");   // Caretaker 531092
            migrationBuilder.Sql($"update {creatures} set action2 = 62025 where id = 531104;");   // Caretaker 531104
            migrationBuilder.Sql($"update {creatures} set action3 = 62026 where id = 531104;");   // Caretaker 531104
            migrationBuilder.Sql($"update {creatures} set action2 = 62027 where id = 2;");   // Caretaker 2
            migrationBuilder.Sql($"update {creatures} set action3 = 62028 where id = 2;");   // Caretaker 2
            migrationBuilder.Sql($"update {creatures} set action2 = 62029 where id = 530004;");   // Caretaker 530004
            migrationBuilder.Sql($"update {creatures} set action3 = 62030 where id = 530004;");   // Caretaker 530004
            migrationBuilder.Sql($"update {creatures} set action2 = 62031 where id = 79;");   // Caretaker boss 79
            migrationBuilder.Sql($"update {creatures} set action3 = 62032 where id = 79;");   // Caretaker boss 79
            migrationBuilder.Sql($"update {creatures} set action2 = 62033 where id = 520015;");   // Caretaker boss 520015
            migrationBuilder.Sql($"update {creatures} set action3 = 62034 where id = 520015;");   // Caretaker boss 520015
            migrationBuilder.Sql($"update {creatures} set action2 = 62035 where id = 520028;");   // Caretaker boss 520028
            migrationBuilder.Sql($"update {creatures} set action3 = 62036 where id = 520028;");   // Caretaker boss 520028
            migrationBuilder.Sql($"update {creatures} set action2 = 62037 where id = 531003;");   // Thrax Technician 531003
            migrationBuilder.Sql($"update {creatures} set action2 = 62038 where id = 531010;");   // Thrax Technician 531010
            migrationBuilder.Sql($"update {creatures} set action2 = 62039 where id = 531020;");   // Thrax Technician 531020
            migrationBuilder.Sql($"update {creatures} set action2 = 62040 where id = 531026;");   // Thrax Technician 531026
            migrationBuilder.Sql($"update {creatures} set action2 = 62041 where id = 531031;");   // Thrax Technician 531031
            migrationBuilder.Sql($"update {creatures} set action2 = 62042 where id = 531042;");   // Thrax Technician 531042
            migrationBuilder.Sql($"update {creatures} set action2 = 62043 where id = 531047;");   // Thrax Technician 531047
            migrationBuilder.Sql($"update {creatures} set action2 = 62044 where id = 531057;");   // Thrax Technician 531057
            migrationBuilder.Sql($"update {creatures} set action2 = 62045 where id = 531065;");   // Thrax Technician 531065
            migrationBuilder.Sql($"update {creatures} set action2 = 62046 where id = 531076;");   // Thrax Technician 531076
            migrationBuilder.Sql($"update {creatures} set action2 = 62047 where id = 531084;");   // Thrax Technician 531084
            migrationBuilder.Sql($"update {creatures} set action2 = 62048 where id = 531091;");   // Thrax Technician 531091
            migrationBuilder.Sql($"update {creatures} set action2 = 62049 where id = 531103;");   // Thrax Technician 531103
            migrationBuilder.Sql($"update {creatures} set action3 = 62050 where id = 47;");   // Thrax Technician 47
            migrationBuilder.Sql($"update {creatures} set action3 = 62051 where id = 530003;");   // Thrax Technician 530003
            migrationBuilder.Sql($"update {creatures} set action4 = 62052 where id = 520009;");   // Thrax Technician boss 520009
            migrationBuilder.Sql($"update {creatures} set action4 = 62053 where id = 520010;");   // Thrax Technician boss 520010
            migrationBuilder.Sql($"update {creatures} set action4 = 62054 where id = 520037;");   // Thrax Technician boss 520037
            migrationBuilder.Sql($"update {creatures} set action4 = 62055 where id = 520041;");   // Thrax Technician boss 520041
            migrationBuilder.Sql($"update {creatures} set action4 = 62056 where id = 520042;");   // Thrax Technician boss 520042
            migrationBuilder.Sql($"update {creatures} set action4 = 62057 where id = 520045;");   // Thrax Technician boss 520045
            migrationBuilder.Sql($"update {creatures} set action4 = 62058 where id = 520052;");   // Thrax Technician boss 520052
            migrationBuilder.Sql($"update {creatures} set action2 = 62059 where id = 8;");   // Machina 8
            migrationBuilder.Sql($"update {creatures} set action2 = 62060 where id = 520012;");   // Machina 520012
            migrationBuilder.Sql($"update {creatures} set action2 = 62061 where id = 520017;");   // Machina 520017
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531004 and action2 = 62001;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531004 and action3 = 62002;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531011 and action2 = 62003;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531011 and action3 = 62004;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531021 and action2 = 62005;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531021 and action3 = 62006;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531027 and action2 = 62007;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531027 and action3 = 62008;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531032 and action2 = 62009;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531032 and action3 = 62010;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531043 and action2 = 62011;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531043 and action3 = 62012;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531048 and action2 = 62013;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531048 and action3 = 62014;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531058 and action2 = 62015;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531058 and action3 = 62016;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531066 and action2 = 62017;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531066 and action3 = 62018;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531077 and action2 = 62019;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531077 and action3 = 62020;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531085 and action2 = 62021;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531085 and action3 = 62022;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531092 and action2 = 62023;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531092 and action3 = 62024;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531104 and action2 = 62025;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531104 and action3 = 62026;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 2 and action2 = 62027;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 2 and action3 = 62028;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 530004 and action2 = 62029;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 530004 and action3 = 62030;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 79 and action2 = 62031;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 79 and action3 = 62032;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520015 and action2 = 62033;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520015 and action3 = 62034;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520028 and action2 = 62035;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520028 and action3 = 62036;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531003 and action2 = 62037;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531010 and action2 = 62038;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531020 and action2 = 62039;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531026 and action2 = 62040;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531031 and action2 = 62041;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531042 and action2 = 62042;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531047 and action2 = 62043;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531057 and action2 = 62044;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531065 and action2 = 62045;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531076 and action2 = 62046;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531084 and action2 = 62047;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531091 and action2 = 62048;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531103 and action2 = 62049;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 47 and action3 = 62050;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 530003 and action3 = 62051;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520009 and action4 = 62052;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520010 and action4 = 62053;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520037 and action4 = 62054;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520041 and action4 = 62055;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520042 and action4 = 62056;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520045 and action4 = 62057;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520052 and action4 = 62058;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 8 and action2 = 62059;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520012 and action2 = 62060;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520017 and action2 = 62061;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
