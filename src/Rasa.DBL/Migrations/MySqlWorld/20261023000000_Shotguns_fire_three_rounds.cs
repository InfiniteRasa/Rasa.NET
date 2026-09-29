using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// A shotgun fires three rounds a shot. The strategy guide's shotgun table gives every level
    /// the same clip, "45 (3/shot)", and every other weapon in it one round a shot; ammo_per_shot
    /// was 1 on every row, so a shotgun's clip lasted three times as long as it should.
    ///
    /// The shotguns are the 268 templates with tool_type 9 (TOOLTYPE Shotgun), which are exactly
    /// the templates whose weapon class fires WEAPON_ATTACK with anim condition 3. Their clips are
    /// 30, 45, 60 and 135, all whole numbers of three-round shots.
    ///
    /// Nothing else needs to change: the server already takes ammo_per_shot off the clip and
    /// asks for a reload when fewer are left, and it is sent to the client in WeaponInfo and the
    /// tooltip. The client counts itself out of ammo on the same rule (weapon.py OutOfAmmo) and
    /// shows an ammo-per-shot line for anything above 1 (ID_TOOLTIP_WEAPON_AMMO_PER_SHOT).
    ///
    /// Only rows still at 1; Down puts back those still at 3.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Shotguns_fire_three_rounds : Migration
    {
        private const int Shotgun = 9;
        private const int OneRound = 1;
        private const int ThreeRounds = 3;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set ammo_per_shot = {ThreeRounds} where tool_type = {Shotgun} and ammo_per_shot = {OneRound};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set ammo_per_shot = {OneRound} where tool_type = {Shotgun} and ammo_per_shot = {ThreeRounds};");
        }
    }
}
