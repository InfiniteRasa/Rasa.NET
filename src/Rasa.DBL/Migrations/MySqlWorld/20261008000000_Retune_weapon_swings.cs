using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Each weapon's own melee swing as its alt attack, and its own rate of fire (WeaponSwings).
    /// itemtemplate_weapon had the same placeholders on all 2440 rows: alt 1/133 at 80 m - a
    /// pistol shot, which a pistol's client took for its primary fire - and refire 800 ms.
    ///
    /// Alt: WEAPON_MELEE 174 at the weapon class's stance (weapon_anim_condition_code), reach
    /// from that action_level row's max_range. Refire: the weapon class's attack action_level
    /// row, windup_ms + recovery_ms + reuse_ms; the 348 constant-fire weapons keep theirs. 2240
    /// weapon templates take the swing and 1892 their attack's timing (1856 change: 36 rifles'
    /// is 800 already); the 200 tools keep both.
    ///
    /// Only rows still holding a placeholder are touched; Down puts the placeholders back on the
    /// rows still holding what this set.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Retune_weapon_swings : Migration
    {
        private static string TemplatesOf(string where) =>
            $"select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} w on w.id = c.itemClassId where {where}";

        private static string Paced =>
            TemplatesOf($"w.attack_action_id not in ({string.Join(", ", WeaponSwings.ConstantFireActionIds)}) and w.weapon_anim_condition_code in ({string.Join(", ", WeaponSwings.SwingByAnimCode.Keys)})"
                + $" and exists (select 1 from {ActionLevelEntry.TableName} l where l.action_id = w.attack_action_id and l.level = w.attack_action_arg_id)");

        private static string Cycle =>
            $"(select l.windup_ms + l.recovery_ms + l.reuse_ms from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} w on w.id = c.itemClassId"
            + $" join {ActionLevelEntry.TableName} l on l.action_id = w.attack_action_id and l.level = w.attack_action_arg_id where c.itemTemplateId = {ItemTemplateWeaponEntry.TableName}.id)";

        private static string Reach(uint swing) =>
            $"(select max_range from {ActionLevelEntry.TableName} where action_id = {WeaponSwings.MeleeActionId} and level = {swing})";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (code, swing) in WeaponSwings.SwingByAnimCode.OrderBy(s => s.Key))
                migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set alt_action_id = {WeaponSwings.MeleeActionId}, alt_action_arg_id = {swing}, alt_range = {Reach(swing)}"
                    + $" where alt_action_id = {WeaponSwings.PlaceholderAltActionId} and alt_action_arg_id = {WeaponSwings.PlaceholderAltActionArgId} and alt_range = {WeaponSwings.PlaceholderAltRange}"
                    + $" and id in ({TemplatesOf($"w.weapon_anim_condition_code = {code}")});");

            migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set refire = {Cycle} where refire = {WeaponSwings.PlaceholderRefire} and id in ({Paced});");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set refire = {WeaponSwings.PlaceholderRefire} where refire = {Cycle} and id in ({Paced});");

            foreach (var (code, swing) in WeaponSwings.SwingByAnimCode.OrderBy(s => s.Key))
                migrationBuilder.Sql($"update {ItemTemplateWeaponEntry.TableName} set alt_action_id = {WeaponSwings.PlaceholderAltActionId}, alt_action_arg_id = {WeaponSwings.PlaceholderAltActionArgId}, alt_range = {WeaponSwings.PlaceholderAltRange}"
                    + $" where alt_action_id = {WeaponSwings.MeleeActionId} and alt_action_arg_id = {swing} and alt_range = {Reach(swing)}"
                    + $" and id in ({TemplatesOf($"w.weapon_anim_condition_code = {code}")});");
        }
    }
}
