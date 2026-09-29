using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the Strider, Predator and Howler bosses their own attacks: creature_action rows
    /// 64001-64013. The Striders and Predators fought with the base table's row 1, the Thrax
    /// soldier's melee pair (1/48, 0.5-3.5 m); that slot now holds their own weapon.
    ///
    ///  - Strider bosses (Daddy Long-Legs 520031, Krammitron 520059): WEAPON_ATTACK_STRIDER 1/204
    ///    (Weapon_Creature_Strider, physical, 20 m); CR_STRIDER_EYE 269/1, CR_STRIDER_LASER_BEAM
    ///    270/1 and CR_STRIDER_GROUND_PULSE 473/1.
    ///  - Predator bosses (Alpha Class Recon Predator 520022, Iceram 520057):
    ///    WEAPON_ATTACK_PREDATOR 1/85 (Weapon_Creature_Predator, laser, 50 m); CR_PREDATOR_MISSILE
    ///    426/1 in the slot after their death bomb and scan.
    ///  - Kennilaxx (520040), the Howler boss: CR_HOWLER_SHRIEK 512/1, the action's only argument
    ///    (_EPIC_MINIBOSS), after the sonic and melee attacks it already has.
    ///
    /// Ability damage is the client's base at the argument, x2 every 8 levels, x0.5 - a boss on an
    /// argument not named for a boss, as the Predators' death bombs. The weapons, which have no
    /// base in the client, are 8-12% of a same-level player's base health, a Thrax commander's
    /// share. The client's reuse is 0 for all five abilities; ours: eye 12 s, laser 15 s, ground
    /// pulse 20 s, missile 8 s, shriek 30 s. Weapon reuse is the pair's recovery.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_boss_attacks : Migration
    {
        private const uint IdMin = 64001;
        private const uint IdMax = 64999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureBossAttackPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action1 = 64001 where id = 520031;");   // Daddy Long-Legs (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 64002 where id = 520031;");   // Daddy Long-Legs (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 64003 where id = 520031;");   // Daddy Long-Legs (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action4 = 64004 where id = 520031;");   // Daddy Long-Legs (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 64005 where id = 520059;");   // Krammitron (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action2 = 64006 where id = 520059;");   // Krammitron (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 64007 where id = 520059;");   // Krammitron (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action4 = 64008 where id = 520059;");   // Krammitron (Strider boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 64009 where id = 520022;");   // Alpha Class Recon Predator (Predator boss)
            migrationBuilder.Sql($"update {creatures} set action4 = 64010 where id = 520022;");   // Alpha Class Recon Predator (Predator boss)
            migrationBuilder.Sql($"update {creatures} set action1 = 64011 where id = 520057;");   // Iceram (Predator boss)
            migrationBuilder.Sql($"update {creatures} set action4 = 64012 where id = 520057;");   // Iceram (Predator boss)
            migrationBuilder.Sql($"update {creatures} set action3 = 64013 where id = 520040;");   // Kennilaxx (Howler boss)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520031 and action1 = 64001;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520031 and action2 = 64002;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520031 and action3 = 64003;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520031 and action4 = 64004;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520059 and action1 = 64005;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520059 and action2 = 64006;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520059 and action3 = 64007;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520059 and action4 = 64008;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520022 and action1 = 64009;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520022 and action4 = 64010;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520057 and action1 = 64011;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520057 and action4 = 64012;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 520040 and action3 = 64013;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
