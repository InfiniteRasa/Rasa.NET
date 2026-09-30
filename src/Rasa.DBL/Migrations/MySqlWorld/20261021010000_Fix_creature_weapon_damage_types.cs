using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Creature weapon rows that stated physical damage for a weapon the client says deals another
    /// type now state the weapon's (generated.client.weaponclass damage type):
    ///
    ///  - the thirteen world Thrax Grenadiers' grenades (Weapon_Creature_Bane_Grenade 1/229): Fire;
    ///  - the region Mox melee (Weapon_Creature_Mox 174/13) and Beam Manta attacks
    ///    (Weapon_Creature_Beam_Manta 1/209): Electrical;
    ///  - the region Stalker's weapon (Weapon_Creature_Stalker 1/78): Laser;
    ///  - the region Swamp Grubber's melee (Weapon_Creature_Swamp_Grubber 174/14): Virulent.
    ///
    /// A row's damage_type wins over the weapon's (CreatureAttacks.DamageTypeOf), so the rows had
    /// been dealing physical damage.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Fix_creature_weapon_damage_types : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;

            migrationBuilder.Sql($"update {actions} set damage_type = 2 where id in (53003, 53014, 53029, 53039, 53047, 53062, 53070, 53086, 53099, 53116, 53128, 53139, 53157) and damage_type = 1;");
            migrationBuilder.Sql($"update {actions} set damage_type = 4 where id in (55022) and damage_type = 1;");
            migrationBuilder.Sql($"update {actions} set damage_type = 6 where id in (55003) and damage_type = 1;");
            migrationBuilder.Sql($"update {actions} set damage_type = 13 where id in (55018, 55021, 55025, 55032, 55040, 55058, 55062) and damage_type = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;

            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id in (53003, 53014, 53029, 53039, 53047, 53062, 53070, 53086, 53099, 53116, 53128, 53139, 53157) and damage_type = 2;");
            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id in (55022) and damage_type = 4;");
            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id in (55003) and damage_type = 6;");
            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id in (55018, 55021, 55025, 55032, 55040, 55058, 55062) and damage_type = 13;");
        }
    }
}
