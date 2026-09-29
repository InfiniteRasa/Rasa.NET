using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the summoners their summons (CreatureSummons, CreatureBombs): creature_action rows
    /// 65001-65043 and two creature rows to summon, 570001-570002.
    ///
    ///  - Thrax Technicians - the base table's, the Divide's, the world's thirteen and the seven
    ///    Technician bosses: CR_TECHNICIAN_TURRET 276/1 (_STANDARD). What it sets down is
    ///    570001, an Ability_Bane_Turret (20359) with Weapon_Creature_Ability_Bane_Turret (20508)
    ///    in its weapon slot, firing WEAPON_ATTACK_ABILITYTURRET_TECHNICIAN 1/245 (65001).
    ///  - Bane Hunters - the base table's two, the Divide's two, the world's eight:
    ///    CR_HUNTER_PET 277/1. What comes is 570002, a Bane_Howler (7336) with the Howler's melee
    ///    402/1 (65002) and sonic attack 200/1 (65003).
    ///  - Atta Grubs (Ashen Desert, Incline, Plains, Thunderhead): CR_ATTA_GRUB_COCOON 500/1
    ///    (_SOLDIER) - they come out Atta Soldiers.
    ///  - The Marshes' Stalker (540002): CR_STALKER_OVULATE 441/1 and CR_STALKER_EGG_DROP 442/1.
    ///
    /// The two creature rows are written at level 42 - health half a same-level minion's,
    /// attacks a minion's share (the turret's 4-6% of a same-level player's base health, the
    /// Howler's the client base x2 every 8 levels x0.25) - and scaled to the summoner's level
    /// when they come. The Stalker's rows are its level's: the charge's tick and the egg's blast
    /// from EFFECT_DAMAGE and DAMAGE_AMOUNT the same way. Reuse, the client giving 0 for three of
    /// the four: turret 60 s (the client's), pet 30 s, ovulate 60 s; the cocoon and the egg drop
    /// have a range of 0, which the fighting loop never uses - the cocoon goes at half health,
    /// the egg when the charge is done.
    ///
    /// Not given out: CR_THRAX_NECROMITE 488 (no Thrax that spawns is named for it), and the
    /// summons of creatures that do not spawn - CR_SENTINEL_SUMMON 294, CR_RAVAGER_SPAWN_ESCORT
    /// 521, CR_RECONSTRUCTOR_BOT_CLONE 444.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_summons : Migration
    {
        private const uint ActionIdMin = 65001;
        private const uint ActionIdMax = 65999;
        private const uint IdMin = 570001;
        private const uint IdMax = 570999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureSummonActionPreloader().Preload(migrationBuilder);
            new CreatureSummonPreloader().Preload(migrationBuilder);
            new CreatureSummonStatsPreloader().Preload(migrationBuilder);
            new CreatureSummonAppearancePreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action4 = 65004 where id = 47;");   // Bane Thrax Technician
            migrationBuilder.Sql($"update {creatures} set action4 = 65005 where id = 530003;");   // Thrax Technician - Divide
            migrationBuilder.Sql($"update {creatures} set action3 = 65006 where id = 531003;");   // Thrax Technician - Abyss
            migrationBuilder.Sql($"update {creatures} set action3 = 65007 where id = 531010;");   // Thrax Technician - Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action3 = 65008 where id = 531020;");   // Thrax Technician - Crucible
            migrationBuilder.Sql($"update {creatures} set action3 = 65009 where id = 531026;");   // Thrax Technician - Descent
            migrationBuilder.Sql($"update {creatures} set action3 = 65010 where id = 531031;");   // Thrax Technician - Howling Maw
            migrationBuilder.Sql($"update {creatures} set action3 = 65011 where id = 531042;");   // Thrax Technician - Incline
            migrationBuilder.Sql($"update {creatures} set action3 = 65012 where id = 531047;");   // Thrax Technician - Marshes
            migrationBuilder.Sql($"update {creatures} set action3 = 65013 where id = 531057;");   // Thrax Technician - Mires
            migrationBuilder.Sql($"update {creatures} set action3 = 65014 where id = 531065;");   // Thrax Technician - Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 65015 where id = 531076;");   // Thrax Technician - Plains
            migrationBuilder.Sql($"update {creatures} set action3 = 65016 where id = 531084;");   // Thrax Technician - Plateau
            migrationBuilder.Sql($"update {creatures} set action3 = 65017 where id = 531091;");   // Thrax Technician - Pools
            migrationBuilder.Sql($"update {creatures} set action3 = 65018 where id = 531103;");   // Thrax Technician - Thunderhead
            migrationBuilder.Sql($"update {creatures} set action5 = 65019 where id = 520009;");   // The Collector - Bootcamp
            migrationBuilder.Sql($"update {creatures} set action5 = 65020 where id = 520010;");   // The Dissector - Bootcamp
            migrationBuilder.Sql($"update {creatures} set action5 = 65021 where id = 520037;");   // Davinx - Palisades
            migrationBuilder.Sql($"update {creatures} set action5 = 65022 where id = 520041;");   // Lawfoid - Palisades
            migrationBuilder.Sql($"update {creatures} set action5 = 65023 where id = 520042;");   // Mirtanz - Palisades
            migrationBuilder.Sql($"update {creatures} set action5 = 65024 where id = 520045;");   // Sinatrix - Palisades
            migrationBuilder.Sql($"update {creatures} set action5 = 65025 where id = 520052;");   // Inquisitor Krakatus - Plateau
            migrationBuilder.Sql($"update {creatures} set action4 = 65026 where id = 41;");   // Bane Hunter Invasion
            migrationBuilder.Sql($"update {creatures} set action4 = 65027 where id = 45;");   // Bane Hunter Invasion
            migrationBuilder.Sql($"update {creatures} set action4 = 65028 where id = 530007;");   // Hunter - Divide
            migrationBuilder.Sql($"update {creatures} set action4 = 65029 where id = 530009;");   // Hunter Lieutenant - Divide
            migrationBuilder.Sql($"update {creatures} set action3 = 65030 where id = 531028;");   // Hunter - Descent
            migrationBuilder.Sql($"update {creatures} set action3 = 65031 where id = 531051;");   // Hunter - Marshes
            migrationBuilder.Sql($"update {creatures} set action3 = 65032 where id = 531054;");   // Hunter Lieutenant - Marshes
            migrationBuilder.Sql($"update {creatures} set action3 = 65033 where id = 531061;");   // Hunter - Mires
            migrationBuilder.Sql($"update {creatures} set action3 = 65034 where id = 531062;");   // Hunter Lieutenant - Mires
            migrationBuilder.Sql($"update {creatures} set action3 = 65035 where id = 531067;");   // Hunter - Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 65036 where id = 531095;");   // Hunter - Pools
            migrationBuilder.Sql($"update {creatures} set action3 = 65037 where id = 531096;");   // Hunter Lieutenant - Pools
            migrationBuilder.Sql($"update {creatures} set action2 = 65038 where id = 531017;");   // Atta Grub - Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action2 = 65039 where id = 531039;");   // Atta Grub - Incline
            migrationBuilder.Sql($"update {creatures} set action2 = 65040 where id = 531081;");   // Atta Grub - Plains
            migrationBuilder.Sql($"update {creatures} set action2 = 65041 where id = 531100;");   // Atta Grub - Thunderhead
            migrationBuilder.Sql($"update {creatures} set action2 = 65042 where id = 540002;");   // Stalker - Marshes
            migrationBuilder.Sql($"update {creatures} set action3 = 65043 where id = 540002;");   // Stalker - Marshes
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 47 and action4 = 65004;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 530003 and action4 = 65005;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531003 and action3 = 65006;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531010 and action3 = 65007;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531020 and action3 = 65008;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531026 and action3 = 65009;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531031 and action3 = 65010;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531042 and action3 = 65011;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531047 and action3 = 65012;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531057 and action3 = 65013;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531065 and action3 = 65014;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531076 and action3 = 65015;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531084 and action3 = 65016;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531091 and action3 = 65017;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531103 and action3 = 65018;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520009 and action5 = 65019;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520010 and action5 = 65020;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520037 and action5 = 65021;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520041 and action5 = 65022;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520042 and action5 = 65023;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520045 and action5 = 65024;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520052 and action5 = 65025;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 41 and action4 = 65026;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 45 and action4 = 65027;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 530007 and action4 = 65028;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 530009 and action4 = 65029;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531028 and action3 = 65030;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531051 and action3 = 65031;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531054 and action3 = 65032;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531061 and action3 = 65033;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531062 and action3 = 65034;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531067 and action3 = 65035;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531095 and action3 = 65036;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531096 and action3 = 65037;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531017 and action2 = 65038;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531039 and action2 = 65039;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531081 and action2 = 65040;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 531100 and action2 = 65041;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 540002 and action2 = 65042;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540002 and action3 = 65043;");

            migrationBuilder.Sql($"delete from {CreatureAppearanceEntry.TableName} where id between {IdMin} and {IdMax};");
            migrationBuilder.Sql($"delete from {CreatureStatEntry.TableName} where id between {IdMin} and {IdMax};");
            migrationBuilder.Sql($"delete from {creatures} where id between {IdMin} and {IdMax};");
            migrationBuilder.Sql($"delete from {actions} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}
