using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Every armour item template's armour value: its class's itemclass.max_hp, into
    /// itemtemplate_armor, which the server sums over the pieces worn for the Armor maximum
    /// (ManifestationManager.UpdateStatsValues). That is the figure the client's armour tooltip
    /// shows as Protection (the piece's hit points, whose share of max_hp is its Condition), and
    /// the strategy guide's per-piece "absorption" column: the level 1 Motor Assist set there,
    /// 54/36/72/90/107, is Armor_T1_MotorAssist_V01_CMN_*_03_to_07's 54/36/72/90/108.
    /// armorclass.max_damage_absorbed is ten times it in every row and read by nothing.
    ///
    /// The table had 5 rows, typed in for the level 1-2 Motor Assist set, the last three a row out
    /// (helmet 59, legs 70, vest 0 where the classes say 47, 59, 70); every other armour template
    /// counted 0. This adds a row for each of the other 14,828 templates whose class has an
    /// armorclass row - body armour, clothing, uniques, and the creature, NPC and vehicle shields
    /// (a creature's own armour comes from creature_stat, not these) - and corrects the three.
    /// Down takes out the rows still holding what this set and puts the three back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_armor_values : Migration
    {
        /// <summary>The hand-entered rows this corrects: template, value it had, value its class gives.</summary>
        private static readonly (uint Template, int Was, int Is)[] Corrected =
        {
            (13126, 59, 47),    // Armor_T1_MotorAssist_V01_CMN_Helmet_01_to_02
            (13156, 70, 59),    // Armor_T1_MotorAssist_V01_CMN_Legs_01_to_02
            (13186, 0, 70),     // Armor_T1_MotorAssist_V01_CMN_Vest_01_to_02
        };

        /// <summary>The five rows the table already had, which Down keeps.</summary>
        private const string Original = "13066, 13096, 13126, 13156, 13186";

        private static string MaxHpOf(string templateId) =>
            $"(select i.max_hp from {ItemTemplateItemClassEntry.TableName} c join {ItemClassEntry.TableName} i on i.id = c.itemClassId where c.itemTemplateId = {templateId})";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"insert into {ItemTemplateArmorEntry.TableName} (id, armor_value)"
                + $" select c.itemTemplateId, i.max_hp from {ItemTemplateItemClassEntry.TableName} c"
                + $" join {ArmorClassEntry.TableName} a on a.class_id = c.itemClassId"
                + $" join {ItemClassEntry.TableName} i on i.id = c.itemClassId"
                // The target in the FROM clause rather than a subquery, which MySQL refuses for an INSERT ... SELECT.
                + $" left join {ItemTemplateArmorEntry.TableName} t on t.id = c.itemTemplateId"
                + $" where t.id is null;");

            foreach (var (template, was, @is) in Corrected)
                migrationBuilder.Sql($"update {ItemTemplateArmorEntry.TableName} set armor_value = {@is} where id = {template} and armor_value = {was};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (template, was, @is) in Corrected)
                migrationBuilder.Sql($"update {ItemTemplateArmorEntry.TableName} set armor_value = {was} where id = {template} and armor_value = {@is};");

            migrationBuilder.Sql($"delete from {ItemTemplateArmorEntry.TableName} where id not in ({Original}) and armor_value = {MaxHpOf(ItemTemplateArmorEntry.TableName + ".id")};");
        }
    }
}
