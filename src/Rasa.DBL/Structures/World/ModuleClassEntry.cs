using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// An item module: what a weapon, a piece of armor or a tool carries in one of its four
    /// module slots, and what an item's ItemInfo names in lootModuleIds. A row of the client's
    /// moduleClassTable (generated/shared/crafting), which is where the 867 of them come from
    /// (ItemModuleSeed); what a module does is in module_effect.
    ///
    /// The game server reads the id and the level. The other columns are the client's own
    /// crafting data for the module - which items take it, the module item it is as a thing in
    /// a pack, what the crafting station charges - kept with the row they belong to; nothing
    /// reads them yet, because the station's module pages are not answered yet.
    ///
    /// 0 is "none" in every column but the id: no id of the client's is 0, and a cost of 0 is
    /// the client's own way of saying a module cannot be extracted or upgraded.
    /// </summary>
    [Table(TableName)]
    public class ModuleClassEntry : IHasId
    {
        public const string TableName = "module_class";

        /// <summary>ModuleClassId: the id an item carries.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>
        /// ModuleVariantId, 1 to 54: the kind of bonus (Body, Fire Resistance, Steal Health).
        /// An item takes one module of a variant. 0 for the sets and the others with none.
        /// </summary>
        [Column("variant_id")]
        [Required]
        public uint VariantId { get; set; }

        /// <summary>
        /// The strength, 1 (Basic) to 5 (Exceptional): the ModuleStrengthTypeId of the module's
        /// item, which is where the client reads it (craftingnew.GetModuleStrengthByModuleId)
        /// and what it prints before the bonus, "[3]". 0 for a module with no item.
        /// </summary>
        [Column("level")]
        [Required]
        public uint Level { get; set; }

        /// <summary>
        /// ClassSetId: the item classes the module goes into - 212 armor, 1212 weapons, 1273
        /// tools, 1274 what is only salvaged. 0 for a module the station does not handle.
        /// </summary>
        [Column("class_set_id")]
        [Required]
        public uint ClassSetId { get; set; }

        /// <summary>ModuleItemTemplateId: the module as an item in a pack, 0 for none.</summary>
        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>ModuleItemEntityClassId: that item's class, 0 for none.</summary>
        [Column("item_class_id")]
        [Required]
        public uint ItemClassId { get; set; }

        /// <summary>MimeogelCostToExtract; 0, it cannot be taken out again.</summary>
        [Column("extract_cost")]
        [Required]
        public uint ExtractCost { get; set; }

        /// <summary>MimeogelCostToIntegrate, before the client's formula scales it.</summary>
        [Column("integrate_cost")]
        [Required]
        public uint IntegrateCost { get; set; }

        /// <summary>MimeogelGainFromSalvage.</summary>
        [Column("salvage_gain")]
        [Required]
        public uint SalvageGain { get; set; }

        /// <summary>MimeogelCostToUpgrade; 0, it cannot be upgraded.</summary>
        [Column("upgrade_cost")]
        [Required]
        public uint UpgradeCost { get; set; }

        /// <summary>UpgradeModuleClassId: the module an upgrade makes of this one, 0 for none.</summary>
        [Column("upgrade_module_id")]
        [Required]
        public uint UpgradeModuleId { get; set; }

        /// <summary>
        /// What the client calls it, for whoever reads the table or searches it (".module
        /// find"): a set's name ("Set: Scout Suit Mk I"), else the module item's ("Armor Module:
        /// Body Bonus [1]"), else the pattern it names an item by ("Titan %(ClassName)s").
        /// Empty for a module the client has no words for.
        /// </summary>
        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = "";
    }
}
