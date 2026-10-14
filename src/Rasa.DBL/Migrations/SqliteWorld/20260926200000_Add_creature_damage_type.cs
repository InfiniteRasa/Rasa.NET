using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// A damage type on every creature attack.
    ///
    /// Nothing in the client marks a creature with a damage type: the type belongs to the weapon
    /// an attack plays, and an attack names its weapon by the action and action argument it is
    /// performed with. generated.client.weaponclass carries both, so every row here is set from
    /// the weapon class its pair belongs to - the AFS mini turret and the Mox's energy attack are
    /// electrical, the Miasma is ice, the Thrax grenadier is fire, the Lightbender and the Bane
    /// pistols are laser, the boargar's stunning swipe is sonic, and the rest are physical.
    ///
    /// A row left at 0 is worked out by the server instead (Managers.CreatureAttacks): the
    /// action's own DAMAGE_TYPE property first, for the creature abilities that carry one, then
    /// the same weapon class table (Data.CreatureWeaponDamage), then physical.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_creature_damage_type : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "damage_type",
                table: CreatureActionEntry.TableName,
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            // physical
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 1 where id in (1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 13, 14, 15, 16, 17, 20, 21, 22, 24, 26, 27, 28, 31, 32, 34, 35, 36, 37, 38, 39, 40, 41, 42, 44);");

            // fire
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 2 where id in (19);");

            // ice
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 3 where id in (43);");

            // laser
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 6 where id in (18, 29, 30, 33);");

            // sonic
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 7 where id in (25);");

            // electrical
            migrationBuilder.Sql($"update {CreatureActionEntry.TableName} set damage_type = 13 where id in (8, 12, 23);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "damage_type",
                table: CreatureActionEntry.TableName);
        }
    }
}
