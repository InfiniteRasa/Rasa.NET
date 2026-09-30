using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the creatures that spawn the abilities their species has in the client and they
    /// were never given: creature_action rows 68001-68095, one creature row to summon (570003).
    ///
    ///  - The Thrax Soldier bosses (the 27 with the Thrax lightning, rage and scourge):
    ///    CR_THRAX_SHRAPNEL 278/1 (_BOSS, all around it, 12 m) and CR_THRAX_FORCE_BLAST 451/1
    ///    (_BOSS_SONIC - the one argument that knocks back). Thraxus Machina (520001): those two
    ///    and CR_THRAX_TECTONIC_STRIKE 458/1 (_BOSS).
    ///  - The Thrax Grenadier bosses (Karem Zul, Orax): the tectonic strike, and
    ///    CR_THRAX_NECROMITE 488/1, which summons 570003 - an Ability_Bane_Necromite (7528) with
    ///    Weapon_Creature_Necromite (7077) in its weapon slot, its bite 1/109 and
    ///    CR_NECROMITE_SELF_DESTRUCT 489/1.
    ///  - Every Thrax Technician that spawns: CR_TECHNICIAN_REVIVE 400 - 400/1 (SHARED_MINION),
    ///    the Technician bosses 400/2 (SHARED_BOSS).
    ///  - The Forean shaman (52): CR_FOREAN_LIFEFORCE_FUNNEL 255/1 and CR_FOREAN_DECAY 448/1
    ///    (_REDSHIRT). The Forean gunner (51): CR_FOREAN_CHAFF 204/1.
    ///  - Every Miasma that spawns: CR_MIASMA_COALESCE 485/1.
    ///  - The AFS soldiers (39, 97): CR_HUMAN_RUSHING_BLOW 504/1 (SHARED_MINION).
    ///
    /// Damage is the client base x2 every 8 levels x0.25 - a boss on a boss argument, a minion
    /// on a minion's - and the Necromite's at level 42, scaled to its Thrax's when it comes; its
    /// bite is a minion weapon's 4-6% of a same-level player's base health. Range, reuse and
    /// windup are the client's but where the client gives none or one the fighting loop cannot
    /// use: shrapnel's reach is its radius, the coalesce's its implosion's; the revive reaches
    /// 20 m (the client's 1 m would have the Technician walk to the body); reuse for the revive
    /// 30 s, the funnel 10 s, decay 6 s, chaff 60 s (its duration), the coalesce 30 s, the
    /// Necromite 30 s.
    ///
    /// CR_AMOEBOID_EXPIRE 435 needs no row: it is how an Amoeboid Spawn's life ends
    /// (AmoeboidVomit).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_missing_abilities : Migration
    {
        private const uint ActionIdMin = 68001;
        private const uint ActionIdMax = 68999;
        private const uint NecromiteId = 570003;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureMissingAbilityActionPreloader().Preload(migrationBuilder);
            new CreatureNecromitePreloader().Preload(migrationBuilder);
            new CreatureNecromiteStatsPreloader().Preload(migrationBuilder);
            new CreatureNecromiteAppearancePreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action5 = 68001 where id = 520003;");   // Thrax Soldier boss 520003
            migrationBuilder.Sql($"update {creatures} set action6 = 68002 where id = 520003;");   // Thrax Soldier boss 520003
            migrationBuilder.Sql($"update {creatures} set action5 = 68003 where id = 520004;");   // Thrax Soldier boss 520004
            migrationBuilder.Sql($"update {creatures} set action6 = 68004 where id = 520004;");   // Thrax Soldier boss 520004
            migrationBuilder.Sql($"update {creatures} set action5 = 68005 where id = 520005;");   // Thrax Soldier boss 520005
            migrationBuilder.Sql($"update {creatures} set action6 = 68006 where id = 520005;");   // Thrax Soldier boss 520005
            migrationBuilder.Sql($"update {creatures} set action5 = 68007 where id = 520006;");   // Thrax Soldier boss 520006
            migrationBuilder.Sql($"update {creatures} set action6 = 68008 where id = 520006;");   // Thrax Soldier boss 520006
            migrationBuilder.Sql($"update {creatures} set action5 = 68009 where id = 520007;");   // Thrax Soldier boss 520007
            migrationBuilder.Sql($"update {creatures} set action6 = 68010 where id = 520007;");   // Thrax Soldier boss 520007
            migrationBuilder.Sql($"update {creatures} set action5 = 68011 where id = 520011;");   // Thrax Soldier boss 520011
            migrationBuilder.Sql($"update {creatures} set action6 = 68012 where id = 520011;");   // Thrax Soldier boss 520011
            migrationBuilder.Sql($"update {creatures} set action5 = 68013 where id = 520013;");   // Thrax Soldier boss 520013
            migrationBuilder.Sql($"update {creatures} set action6 = 68014 where id = 520013;");   // Thrax Soldier boss 520013
            migrationBuilder.Sql($"update {creatures} set action5 = 68015 where id = 520014;");   // Thrax Soldier boss 520014
            migrationBuilder.Sql($"update {creatures} set action6 = 68016 where id = 520014;");   // Thrax Soldier boss 520014
            migrationBuilder.Sql($"update {creatures} set action5 = 68017 where id = 520019;");   // Thrax Soldier boss 520019
            migrationBuilder.Sql($"update {creatures} set action6 = 68018 where id = 520019;");   // Thrax Soldier boss 520019
            migrationBuilder.Sql($"update {creatures} set action5 = 68019 where id = 520021;");   // Thrax Soldier boss 520021
            migrationBuilder.Sql($"update {creatures} set action6 = 68020 where id = 520021;");   // Thrax Soldier boss 520021
            migrationBuilder.Sql($"update {creatures} set action5 = 68021 where id = 520023;");   // Thrax Soldier boss 520023
            migrationBuilder.Sql($"update {creatures} set action6 = 68022 where id = 520023;");   // Thrax Soldier boss 520023
            migrationBuilder.Sql($"update {creatures} set action5 = 68023 where id = 520024;");   // Thrax Soldier boss 520024
            migrationBuilder.Sql($"update {creatures} set action6 = 68024 where id = 520024;");   // Thrax Soldier boss 520024
            migrationBuilder.Sql($"update {creatures} set action5 = 68025 where id = 520025;");   // Thrax Soldier boss 520025
            migrationBuilder.Sql($"update {creatures} set action6 = 68026 where id = 520025;");   // Thrax Soldier boss 520025
            migrationBuilder.Sql($"update {creatures} set action5 = 68027 where id = 520026;");   // Thrax Soldier boss 520026
            migrationBuilder.Sql($"update {creatures} set action6 = 68028 where id = 520026;");   // Thrax Soldier boss 520026
            migrationBuilder.Sql($"update {creatures} set action5 = 68029 where id = 520027;");   // Thrax Soldier boss 520027
            migrationBuilder.Sql($"update {creatures} set action6 = 68030 where id = 520027;");   // Thrax Soldier boss 520027
            migrationBuilder.Sql($"update {creatures} set action5 = 68031 where id = 520029;");   // Thrax Soldier boss 520029
            migrationBuilder.Sql($"update {creatures} set action6 = 68032 where id = 520029;");   // Thrax Soldier boss 520029
            migrationBuilder.Sql($"update {creatures} set action5 = 68033 where id = 520030;");   // Thrax Soldier boss 520030
            migrationBuilder.Sql($"update {creatures} set action6 = 68034 where id = 520030;");   // Thrax Soldier boss 520030
            migrationBuilder.Sql($"update {creatures} set action5 = 68035 where id = 520032;");   // Thrax Soldier boss 520032
            migrationBuilder.Sql($"update {creatures} set action6 = 68036 where id = 520032;");   // Thrax Soldier boss 520032
            migrationBuilder.Sql($"update {creatures} set action5 = 68037 where id = 520033;");   // Thrax Soldier boss 520033
            migrationBuilder.Sql($"update {creatures} set action6 = 68038 where id = 520033;");   // Thrax Soldier boss 520033
            migrationBuilder.Sql($"update {creatures} set action5 = 68039 where id = 520034;");   // Thrax Soldier boss 520034
            migrationBuilder.Sql($"update {creatures} set action6 = 68040 where id = 520034;");   // Thrax Soldier boss 520034
            migrationBuilder.Sql($"update {creatures} set action5 = 68041 where id = 520035;");   // Thrax Soldier boss 520035
            migrationBuilder.Sql($"update {creatures} set action6 = 68042 where id = 520035;");   // Thrax Soldier boss 520035
            migrationBuilder.Sql($"update {creatures} set action5 = 68043 where id = 520043;");   // Thrax Soldier boss 520043
            migrationBuilder.Sql($"update {creatures} set action6 = 68044 where id = 520043;");   // Thrax Soldier boss 520043
            migrationBuilder.Sql($"update {creatures} set action5 = 68045 where id = 520048;");   // Thrax Soldier boss 520048
            migrationBuilder.Sql($"update {creatures} set action6 = 68046 where id = 520048;");   // Thrax Soldier boss 520048
            migrationBuilder.Sql($"update {creatures} set action5 = 68047 where id = 520054;");   // Thrax Soldier boss 520054
            migrationBuilder.Sql($"update {creatures} set action6 = 68048 where id = 520054;");   // Thrax Soldier boss 520054
            migrationBuilder.Sql($"update {creatures} set action5 = 68049 where id = 520058;");   // Thrax Soldier boss 520058
            migrationBuilder.Sql($"update {creatures} set action6 = 68050 where id = 520058;");   // Thrax Soldier boss 520058
            migrationBuilder.Sql($"update {creatures} set action5 = 68051 where id = 520060;");   // Thrax Soldier boss 520060
            migrationBuilder.Sql($"update {creatures} set action6 = 68052 where id = 520060;");   // Thrax Soldier boss 520060
            migrationBuilder.Sql($"update {creatures} set action5 = 68053 where id = 520062;");   // Thrax Soldier boss 520062
            migrationBuilder.Sql($"update {creatures} set action6 = 68054 where id = 520062;");   // Thrax Soldier boss 520062
            migrationBuilder.Sql($"update {creatures} set action2 = 68055 where id = 520001;");   // Thraxus Machina 520001
            migrationBuilder.Sql($"update {creatures} set action3 = 68056 where id = 520001;");   // Thraxus Machina 520001
            migrationBuilder.Sql($"update {creatures} set action4 = 68057 where id = 520001;");   // Thraxus Machina 520001
            migrationBuilder.Sql($"update {creatures} set action3 = 68058 where id = 520008;");   // Thrax Grenadier boss 520008
            migrationBuilder.Sql($"update {creatures} set action4 = 68059 where id = 520008;");   // Thrax Grenadier boss 520008
            migrationBuilder.Sql($"update {creatures} set action3 = 68060 where id = 520061;");   // Thrax Grenadier boss 520061
            migrationBuilder.Sql($"update {creatures} set action4 = 68061 where id = 520061;");   // Thrax Grenadier boss 520061
            migrationBuilder.Sql($"update {creatures} set action5 = 68062 where id = 47;");   // Thrax Technician 47
            migrationBuilder.Sql($"update {creatures} set action6 = 68063 where id = 520009;");   // Thrax Technician boss 520009
            migrationBuilder.Sql($"update {creatures} set action6 = 68064 where id = 520010;");   // Thrax Technician boss 520010
            migrationBuilder.Sql($"update {creatures} set action6 = 68065 where id = 520037;");   // Thrax Technician boss 520037
            migrationBuilder.Sql($"update {creatures} set action6 = 68066 where id = 520041;");   // Thrax Technician boss 520041
            migrationBuilder.Sql($"update {creatures} set action6 = 68067 where id = 520042;");   // Thrax Technician boss 520042
            migrationBuilder.Sql($"update {creatures} set action6 = 68068 where id = 520045;");   // Thrax Technician boss 520045
            migrationBuilder.Sql($"update {creatures} set action6 = 68069 where id = 520052;");   // Thrax Technician boss 520052
            migrationBuilder.Sql($"update {creatures} set action5 = 68070 where id = 530003;");   // Thrax Technician 530003
            migrationBuilder.Sql($"update {creatures} set action4 = 68071 where id = 531003;");   // Thrax Technician 531003
            migrationBuilder.Sql($"update {creatures} set action4 = 68072 where id = 531010;");   // Thrax Technician 531010
            migrationBuilder.Sql($"update {creatures} set action4 = 68073 where id = 531020;");   // Thrax Technician 531020
            migrationBuilder.Sql($"update {creatures} set action4 = 68074 where id = 531026;");   // Thrax Technician 531026
            migrationBuilder.Sql($"update {creatures} set action4 = 68075 where id = 531031;");   // Thrax Technician 531031
            migrationBuilder.Sql($"update {creatures} set action4 = 68076 where id = 531042;");   // Thrax Technician 531042
            migrationBuilder.Sql($"update {creatures} set action4 = 68077 where id = 531047;");   // Thrax Technician 531047
            migrationBuilder.Sql($"update {creatures} set action4 = 68078 where id = 531057;");   // Thrax Technician 531057
            migrationBuilder.Sql($"update {creatures} set action4 = 68079 where id = 531065;");   // Thrax Technician 531065
            migrationBuilder.Sql($"update {creatures} set action4 = 68080 where id = 531076;");   // Thrax Technician 531076
            migrationBuilder.Sql($"update {creatures} set action4 = 68081 where id = 531084;");   // Thrax Technician 531084
            migrationBuilder.Sql($"update {creatures} set action4 = 68082 where id = 531091;");   // Thrax Technician 531091
            migrationBuilder.Sql($"update {creatures} set action4 = 68083 where id = 531103;");   // Thrax Technician 531103
            migrationBuilder.Sql($"update {creatures} set action1 = 68084 where id = 52;");   // Forean Shaman 52
            migrationBuilder.Sql($"update {creatures} set action2 = 68085 where id = 52;");   // Forean Shaman 52
            migrationBuilder.Sql($"update {creatures} set action2 = 68086 where id = 51;");   // Forean Gunner 51
            migrationBuilder.Sql($"update {creatures} set action3 = 68087 where id = 88;");   // Miasma 88
            migrationBuilder.Sql($"update {creatures} set action3 = 68088 where id = 531035;");   // Miasma 531035
            migrationBuilder.Sql($"update {creatures} set action2 = 68089 where id = 540004;");   // Miasma 540004
            migrationBuilder.Sql($"update {creatures} set action2 = 68090 where id = 540015;");   // Miasma 540015
            migrationBuilder.Sql($"update {creatures} set action2 = 68091 where id = 540034;");   // Miasma 540034
            migrationBuilder.Sql($"update {creatures} set action2 = 68092 where id = 39;");   // AFS Soldier 39
            migrationBuilder.Sql($"update {creatures} set action2 = 68093 where id = 97;");   // AFS Soldier 97
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520003 and action5 = 68001;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520003 and action6 = 68002;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520004 and action5 = 68003;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520004 and action6 = 68004;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520005 and action5 = 68005;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520005 and action6 = 68006;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520006 and action5 = 68007;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520006 and action6 = 68008;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520007 and action5 = 68009;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520007 and action6 = 68010;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520011 and action5 = 68011;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520011 and action6 = 68012;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520013 and action5 = 68013;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520013 and action6 = 68014;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520014 and action5 = 68015;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520014 and action6 = 68016;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520019 and action5 = 68017;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520019 and action6 = 68018;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520021 and action5 = 68019;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520021 and action6 = 68020;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520023 and action5 = 68021;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520023 and action6 = 68022;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520024 and action5 = 68023;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520024 and action6 = 68024;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520025 and action5 = 68025;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520025 and action6 = 68026;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520026 and action5 = 68027;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520026 and action6 = 68028;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520027 and action5 = 68029;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520027 and action6 = 68030;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520029 and action5 = 68031;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520029 and action6 = 68032;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520030 and action5 = 68033;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520030 and action6 = 68034;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520032 and action5 = 68035;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520032 and action6 = 68036;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520033 and action5 = 68037;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520033 and action6 = 68038;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520034 and action5 = 68039;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520034 and action6 = 68040;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520035 and action5 = 68041;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520035 and action6 = 68042;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520043 and action5 = 68043;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520043 and action6 = 68044;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520048 and action5 = 68045;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520048 and action6 = 68046;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520054 and action5 = 68047;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520054 and action6 = 68048;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520058 and action5 = 68049;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520058 and action6 = 68050;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520060 and action5 = 68051;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520060 and action6 = 68052;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520062 and action5 = 68053;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520062 and action6 = 68054;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520001 and action2 = 68055;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520001 and action3 = 68056;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520001 and action4 = 68057;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520008 and action3 = 68058;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520008 and action4 = 68059;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520061 and action3 = 68060;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520061 and action4 = 68061;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 47 and action5 = 68062;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520009 and action6 = 68063;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520010 and action6 = 68064;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520037 and action6 = 68065;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520041 and action6 = 68066;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520042 and action6 = 68067;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520045 and action6 = 68068;");
            migrationBuilder.Sql($"update {creatures} set action6 = 0 where id = 520052 and action6 = 68069;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 530003 and action5 = 68070;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531003 and action4 = 68071;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531010 and action4 = 68072;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531020 and action4 = 68073;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531026 and action4 = 68074;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531031 and action4 = 68075;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531042 and action4 = 68076;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531047 and action4 = 68077;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531057 and action4 = 68078;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531065 and action4 = 68079;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531076 and action4 = 68080;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531084 and action4 = 68081;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531091 and action4 = 68082;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531103 and action4 = 68083;");
            migrationBuilder.Sql($"update {creatures} set action1 = 0 where id = 52 and action1 = 68084;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 52 and action2 = 68085;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 51 and action2 = 68086;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 88 and action3 = 68087;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531035 and action3 = 68088;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540004 and action2 = 68089;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540015 and action2 = 68090;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540034 and action2 = 68091;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 39 and action2 = 68092;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 97 and action2 = 68093;");

            migrationBuilder.Sql($"delete from {CreatureAppearanceEntry.TableName} where id = {NecromiteId};");
            migrationBuilder.Sql($"delete from {CreatureStatEntry.TableName} where id = {NecromiteId};");
            migrationBuilder.Sql($"delete from {creatures} where id = {NecromiteId};");
            migrationBuilder.Sql($"delete from {actions} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}
