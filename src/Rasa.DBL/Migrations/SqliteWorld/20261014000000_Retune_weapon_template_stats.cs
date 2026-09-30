using System.Globalization;

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Weapon templates' range, attack type and alt (melee) damage (WeaponTemplateStats), which
    /// were the same placeholders on all 2440 rows: 80 m, ranged, 25. Range is the weapon
    /// class's attack action's max_range; blades are melee; alt_max_damage is the class's
    /// max_damage times its type's melee : ranged ratio from the strategy guide.
    ///
    /// Only the 2240 weapons (weaponclass rows with a type in WeaponTemplateStats.Types); the
    /// 200 tools keep theirs. Only rows still holding a placeholder are touched; Down puts the
    /// placeholders back on the rows still holding what this set. `range` is quoted: MySQL 8
    /// reserves the word.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Retune_weapon_template_stats : Migration
    {
        private static string Weapon => ItemTemplateWeaponEntry.TableName;

        private static string TemplatesOf(uint actionId, int animCode) =>
            $"select c.itemTemplateId from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} k on k.id = c.itemClassId"
            + $" where k.attack_action_id = {actionId} and k.weapon_anim_condition_code = {animCode}";

        private static string Of(string select, string join = "") =>
            $"(select {select} from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} k on k.id = c.itemClassId{join}"
            + $" where c.itemTemplateId = {Weapon}.id)";

        private static string Reach =>
            Of("l.max_range", $" join {ActionLevelEntry.TableName} l on l.action_id = k.attack_action_id and l.level = k.attack_action_arg_id");

        private static string AltDamage(double ratio) =>
            Of($"round(k.max_damage * {ratio.ToString("0.###", CultureInfo.InvariantCulture)})");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (_, actionId, animCode, ratio) in WeaponTemplateStats.Types)
            {
                var templates = TemplatesOf(actionId, animCode);

                migrationBuilder.Sql($"update {Weapon} set `range` = {Reach} where `range` = {WeaponTemplateStats.PlaceholderRange} and id in ({templates})"
                    + $" and exists (select 1 from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} k on k.id = c.itemClassId"
                    + $" join {ActionLevelEntry.TableName} l on l.action_id = k.attack_action_id and l.level = k.attack_action_arg_id where c.itemTemplateId = {Weapon}.id);");

                migrationBuilder.Sql($"update {Weapon} set alt_max_damage = {AltDamage(ratio)} where alt_max_damage = {WeaponTemplateStats.PlaceholderAltMaxDamage} and id in ({templates});");

                if (actionId == WeaponTemplateStats.BladeActionId)
                    migrationBuilder.Sql($"update {Weapon} set attack_type = {WeaponTemplateStats.Melee} where attack_type = {WeaponTemplateStats.PlaceholderAttackType} and id in ({templates});");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (_, actionId, animCode, ratio) in WeaponTemplateStats.Types)
            {
                var templates = TemplatesOf(actionId, animCode);

                if (actionId == WeaponTemplateStats.BladeActionId)
                    migrationBuilder.Sql($"update {Weapon} set attack_type = {WeaponTemplateStats.PlaceholderAttackType} where attack_type = {WeaponTemplateStats.Melee} and id in ({templates});");

                migrationBuilder.Sql($"update {Weapon} set alt_max_damage = {WeaponTemplateStats.PlaceholderAltMaxDamage} where alt_max_damage = {AltDamage(ratio)} and id in ({templates});");
                migrationBuilder.Sql($"update {Weapon} set `range` = {WeaponTemplateStats.PlaceholderRange} where `range` = {Reach} and id in ({templates});");
            }
        }
    }
}
