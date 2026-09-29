using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the creature abilities the client plays as plain hits to the creatures that spawn
    /// with them: 30 creature_action rows, 54001-54030, and the slots that point at them.
    ///
    /// Every action here is one whose client class (client/actions/abilities/ai) resolves each
    /// entry of the recovery's hit list as a damage rawInfo - CreateDamageInfo then
    /// AnnounceDamage - which is exactly what a creature's attack sends. The ones with an area
    /// (Kael smash and ground pound, tectonic strike, Howler sonic attack, Lightbender quill)
    /// hit everyone in it through CreatureAreaAttacks; knockback and stun in the action data
    /// land through PlayerCrowdControl as before.
    ///
    /// Who gets what:
    ///  - Flaregasher boss (Cracked Tooth): CR_FLAREGASHER_MELEE and CR_FLAREGASHER_FIREBALL, in
    ///    place of the Thrax soldier's melee weapon it was given.
    ///  - Howler boss (Kennilaxx): CR_HOWLER_MELEE_ATTACK and CR_HOWLER_SONIC_ATTACK (a 45 degree
    ///    cone, 10% to stun for 1 s), in place of a Bane pistol.
    ///  - Kael bosses (Goliath, Painrox): CR_KAEL_SMASH, CR_KAEL_GROUND_POUND and
    ///    CR_KAEL_TECTONIC_STRIKE, in place of the Thrax melee weapon; the Plateau's Kael add the
    ///    tectonic strike to the smash and ground pound they have.
    ///  - Lightbender bosses (Proctor Fulgor, Hygax): CR_LIGHTBENDER_QUILL beside their gun. The
    ///    quill also carries LIGHTBENDER_QUILL_FLASH (game effect 388), which is not attached yet.
    ///  - Atta soldier bosses (Imperial Guard, Red Kraken, Tiamox): CR_ATTA_SOLDIER_MELEE,
    ///    _ACID_SPIT and _ROCK_THROW, in place of the Thrax melee weapon; the world's Atta soldiers
    ///    add the rock throw to the spit and melee they have. The Atta harvester boss (Phuumz)
    ///    gets CR_ATTA_HARVESTER_ACID_SPIT, as its world kin have it.
    ///  - Xanx (Arioch, the Bane Xanx, the Divide's Xanx): CR_XANX_MELEE beside the Xanx weapon.
    ///
    /// Numbers follow the world rows (WorldCreatureActionPreloader): range, reuse and windup from
    /// the action's argument 1, reuse at least a second, the client's DAMAGE_TYPE or physical,
    /// and damage the argument's DAMAGE_AMOUNT_MIN/MAX x 2^((level - 1) / 8) x 0.25 - a boss
    /// 0.5, twice what its kin hit for. Two departures: the Howler's melee reaches 3 m, not the
    /// client's 2, because a chasing creature stops at 3 m and would never swing; and the
    /// Flaregasher's fireball reuses every 6 s, not every 1, so it opens with it and closes in
    /// for its bite rather than standing at 40 m throwing fire.
    ///
    /// Also takes the Xanx weapon row (31, Weapon_Creature_Xanx 174/24) down from 30 m to the
    /// weapon's own 4: it is a melee swing.
    ///
    /// Left for later, as they are not plain hits: Kael rushing blow (its class reads the hit
    /// list as (target, rawInfo) pairs), CR_TREEBACK_BANE_HOWL (no ability data), and the human
    /// mech's ground pound and missiles (an allied AFS vehicle whose pools spawn none).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_attacks : Migration
    {
        private const uint IdMin = 54001;
        private const uint IdMax = 54999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            new CreatureAttackPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action1 = 54001 where id = 520047;");   // Cracked Tooth (Flaregasher boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54002 where id = 520047;");   // Cracked Tooth (Flaregasher boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54003 where id = 520040;");   // Kennilaxx (Howler boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54004 where id = 520040;");   // Kennilaxx (Howler boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54005 where id = 520051;");   // Goliath (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54006 where id = 520051;");   // Goliath (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54007 where id = 520051;");   // Goliath (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54008 where id = 520063;");   // Painrox (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54009 where id = 520063;");   // Painrox (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54010 where id = 520063;");   // Painrox (Kael boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54011 where id = 531088;");   // Kael Plateau
            migrationBuilder.Sql($"update {creatures} set action2 = 54012 where id = 76;");   // Proctor Fulgor (Lightbender boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54013 where id = 520039;");   // Hygax (Lightbender boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54014 where id = 520002;");   // Atta Imperial Guard (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54015 where id = 520002;");   // Atta Imperial Guard (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54016 where id = 520002;");   // Atta Imperial Guard (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54017 where id = 520050;");   // The Red Kraken (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54018 where id = 520050;");   // The Red Kraken (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54019 where id = 520050;");   // The Red Kraken (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54020 where id = 520064;");   // Tiamox (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54021 where id = 520064;");   // Tiamox (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54022 where id = 520064;");   // Tiamox (Atta Soldier boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 54023 where id = 520020;");   // Phuumz (Atta Harvester boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 54024 where id = 531015;");   // Atta Soldier Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action3 = 54025 where id = 531037;");   // Atta Soldier Incline
            migrationBuilder.Sql($"update {creatures} set action3 = 54026 where id = 531080;");   // Atta Soldier Plains
            migrationBuilder.Sql($"update {creatures} set action3 = 54027 where id = 531098;");   // Atta Soldier Thunderhead
            migrationBuilder.Sql($"update {creatures} set action2 = 54028 where id = 77;");   // Arioch (Xanx boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 54029 where id = 87;");   // Bane Xanx
            migrationBuilder.Sql($"update {creatures} set action2 = 54030 where id = 530008;");   // Xanx Divide

            // The Xanx weapon is a melee swing: Weapon_Creature_Xanx reaches 4, not 30.
            migrationBuilder.Sql($"update {actions} set range_max = 4.0 where id = 31;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520047 and action1 = 54001;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520047 and action2 = 54002;");
            migrationBuilder.Sql($"update {creatures} set action1 = 33 where id = 520040 and action1 = 54003;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520040 and action2 = 54004;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520051 and action1 = 54005;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520051 and action2 = 54006;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520051 and action3 = 54007;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520063 and action1 = 54008;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520063 and action2 = 54009;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520063 and action3 = 54010;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531088 and action3 = 54011;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 76 and action2 = 54012;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520039 and action2 = 54013;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520002 and action1 = 54014;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520002 and action2 = 54015;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520002 and action3 = 54016;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520050 and action1 = 54017;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520050 and action2 = 54018;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520050 and action3 = 54019;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520064 and action1 = 54020;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520064 and action2 = 54021;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520064 and action3 = 54022;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520020 and action1 = 54023;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531015 and action3 = 54024;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531037 and action3 = 54025;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531080 and action3 = 54026;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531098 and action3 = 54027;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 77 and action2 = 54028;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 87 and action2 = 54029;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 530008 and action2 = 54030;");

            migrationBuilder.Sql($"update {actions} set range_max = 30.0 where id = 31;");
            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
