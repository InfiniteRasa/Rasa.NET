using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The item modules: the rows of Add_item_modules.
    ///
    /// Classes is the client's moduleClassTable (generated/shared/crafting.pyo, 1.16.5.0), all
    /// 867 rows of it, with each module's strength taken from its module item
    /// (moduleItemTemplateTable) and its comment from the client's own text. By what they are:
    ///  - 376 bonus modules, the ones with a variant: 330 that are an item a player can hold and
    ///    a crafting station can put in - 66 kinds, armor, weapon and tool, at five strengths -
    ///    and 46 that are not;
    ///  - 227 sets;
    ///  - 60 that only give a salvage value, 5 to 300;
    ///  - 204 others, named at most: the built-in modules of items, named one-offs, and the
    ///    developers' tests.
    ///
    /// Effects is what the client's data says a module does, and that is less than all of them.
    /// The values were never the client's to know - the server sent them, in ModuleTooltipInfo -
    /// but each of the 330 module items says its own in its description ("Base Bonus: 1 / Level
    /// Bonus: 0.05 per item level", "Extra Bonus: 8 (doubles every 8 item levels)"), and those
    /// are the three terms of the amount the client works out: flat, per level, and doubling.
    /// The effect each is shown with is the one of the client's game effects whose tooltip is
    /// that bonus ("Body: %(amount)s", "Resist: $damageType%(arg1)s %(amount)s", "Steal
    /// Health: %(amount)s"): read off the text, not stated by the data.
    ///
    /// So 290 rows, one each for 58 of the 66 kinds. Left without a row:
    ///  - the eight "Debuff ... Resist" weapon modules (40): their line is "Reduce Resist:
    ///    Fire by %(amount)s for %(arg2)s sec", and nothing in the client says how long;
    ///  - the 46 bonus modules that are no item, the sets and the rest: the client has their
    ///    names and nothing of what they did.
    /// A module with no row is still a module: an item carries it, is named by it, and shows no
    /// line for it.
    /// </summary>
    public static class ItemModuleSeed
    {
        private static ModuleClassEntry C(uint id, uint variantId, uint level, uint classSetId, uint itemTemplateId, uint itemClassId,
            uint extractCost, uint integrateCost, uint salvageGain, uint upgradeCost, uint upgradeModuleId, string comment) => new()
        {
            Id = id,
            VariantId = variantId,
            Level = level,
            ClassSetId = classSetId,
            ItemTemplateId = itemTemplateId,
            ItemClassId = itemClassId,
            ExtractCost = extractCost,
            IntegrateCost = integrateCost,
            SalvageGain = salvageGain,
            UpgradeCost = upgradeCost,
            UpgradeModuleId = upgradeModuleId,
            Comment = comment
        };

        private static ModuleEffectEntry E(uint id, uint moduleId, uint effectId, double flat, double linear, double exp, int? arg1 = null) => new()
        {
            Id = id,
            ModuleId = moduleId,
            EffectId = effectId,
            FlatValue = flat,
            LinearValue = linear,
            ExpValue = exp,
            Arg1 = arg1
        };

        /// <summary>
        /// (id, variant, level, class set, item template, item class, extract cost, integrate
        /// cost, salvage gain, upgrade cost, upgrade module, comment).
        /// </summary>
        public static readonly IReadOnlyList<ModuleClassEntry> Classes = new[]
        {
            C(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Grey Market %(ClassName)s"),
            C(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "C:%(ClassName)s T:%(TemplateName)s"),
            C(100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Test Set #100"),
            C(101, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Test Set #101"),
            C(100001, 4, 2, 212, 122969, 29879, 2, 2, 4, 5, 100002, "Armor Module: Health Bonus [2]"),
            C(100002, 4, 3, 212, 122970, 29879, 3, 3, 6, 20, 100003, "Armor Module: Health Bonus [3]"),
            C(100003, 4, 4, 212, 122971, 29879, 4, 4, 8, 100, 900034, "Armor Module: Health Bonus [4]"),
            C(100004, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Bonus: Wellcare %(ClassName)s"),
            C(100005, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Bonus: Wellcare %(ClassName)s"),
            C(100006, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Bonus: Wellcare %(ClassName)s"),
            C(100007, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Bonus: Wellcare %(ClassName)s"),
            C(100008, 5, 1, 212, 122978, 29879, 1, 1, 2, 1, 100009, "Armor Module: Power Bonus [1]"),
            C(100009, 5, 2, 212, 122979, 29879, 2, 2, 4, 5, 100010, "Armor Module: Power Bonus [2]"),
            C(100010, 5, 3, 212, 122980, 29879, 3, 3, 6, 20, 100011, "Armor Module: Power Bonus [3]"),
            C(100011, 5, 4, 212, 122981, 29879, 4, 4, 8, 100, 900038, "Armor Module: Power Bonus [4]"),
            C(100012, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Power Bonus: Dynamo %(ClassName)s"),
            C(100013, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Power Bonus: Dynamo %(ClassName)s"),
            C(100014, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Power Bonus: Dynamo %(ClassName)s"),
            C(100015, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Power Bonus: Dynamo %(ClassName)s"),
            C(100016, 6, 1, 212, 122983, 29879, 1, 1, 2, 1, 100017, "Armor Module: Regen Bonus [1]"),
            C(100017, 6, 2, 212, 122984, 29879, 2, 2, 4, 5, 100018, "Armor Module: Regen Bonus [2]"),
            C(100018, 6, 3, 212, 122985, 29879, 3, 3, 6, 20, 100019, "Armor Module: Regen Bonus [3]"),
            C(100019, 6, 4, 212, 122986, 29879, 4, 4, 8, 100, 900040, "Armor Module: Regen Bonus [4]"),
            C(100020, 7, 1, 212, 123043, 29879, 1, 1, 2, 1, 100021, "Armor Module: Resist Physical [1]"),
            C(100021, 7, 2, 212, 123044, 29879, 2, 2, 4, 5, 100022, "Armor Module: Resist Physical [2]"),
            C(100022, 7, 3, 212, 123045, 29879, 3, 3, 6, 20, 100023, "Armor Module: Resist Physical [3]"),
            C(100023, 7, 4, 212, 123046, 29879, 4, 4, 8, 100, 900048, "Armor Module: Resist Physical [4]"),
            C(100024, 13, 1, 212, 123058, 29879, 1, 1, 2, 1, 100025, "Armor Module: Resist Sonic [1]"),
            C(100025, 13, 2, 212, 123059, 29879, 2, 2, 4, 5, 100026, "Armor Module: Resist Sonic [2]"),
            C(100026, 13, 3, 212, 123060, 29879, 3, 3, 6, 20, 100027, "Armor Module: Resist Sonic [3]"),
            C(100027, 13, 4, 212, 123061, 29879, 4, 4, 8, 100, 900047, "Armor Module: Resist Sonic [4]"),
            C(100028, 12, 1, 212, 123038, 29879, 1, 1, 2, 1, 100029, "Armor Module: Resist Photonic [1]"),
            C(100029, 12, 2, 212, 123039, 29879, 2, 2, 4, 5, 100030, "Armor Module: Resist Photonic [2]"),
            C(100030, 12, 3, 212, 123040, 29879, 3, 3, 6, 20, 100031, "Armor Module: Resist Photonic [3]"),
            C(100031, 12, 4, 212, 123041, 29879, 4, 4, 8, 100, 900049, "Armor Module: Resist Photonic [4]"),
            C(100032, 14, 1, 212, 123013, 29879, 1, 1, 2, 1, 100033, "Armor Module: Resist Electric [1]"),
            C(100033, 14, 2, 212, 123014, 29879, 2, 2, 4, 5, 100034, "Armor Module: Resist Electric [2]"),
            C(100034, 14, 3, 212, 123015, 29879, 3, 3, 6, 20, 100035, "Armor Module: Resist Electric [3]"),
            C(100035, 14, 4, 212, 123016, 29879, 4, 4, 8, 100, 900043, "Armor Module: Resist Electric [4]"),
            C(100036, 8, 1, 212, 123023, 29879, 1, 1, 2, 1, 100037, "Armor Module: Resist Fire [1]"),
            C(100037, 8, 2, 212, 123024, 29879, 2, 2, 4, 5, 100038, "Armor Module: Resist Fire [2]"),
            C(100038, 8, 3, 212, 123025, 29879, 3, 3, 6, 20, 100039, "Armor Module: Resist Fire [3]"),
            C(100039, 8, 4, 212, 123026, 29879, 4, 4, 8, 100, 900045, "Armor Module: Resist Fire [4]"),
            C(100040, 9, 1, 212, 123028, 29879, 1, 1, 2, 1, 100041, "Armor Module: Resist Ice [1]"),
            C(100041, 9, 2, 212, 123029, 29879, 2, 2, 4, 5, 100042, "Armor Module: Resist Ice [2]"),
            C(100042, 9, 3, 212, 123030, 29879, 3, 3, 6, 20, 100043, "Armor Module: Resist Ice [3]"),
            C(100043, 9, 4, 212, 123031, 29879, 4, 4, 8, 100, 900046, "Armor Module: Resist Ice [4]"),
            C(100044, 10, 1, 212, 123068, 29879, 1, 1, 2, 1, 100045, "Armor Module: Resist Virulent [1]"),
            C(100045, 10, 2, 212, 123069, 29879, 2, 2, 4, 5, 100046, "Armor Module: Resist Virulent [2]"),
            C(100046, 10, 3, 212, 123070, 29879, 3, 3, 6, 20, 100047, "Armor Module: Resist Virulent [3]"),
            C(100047, 10, 4, 212, 123071, 29879, 4, 4, 8, 100, 900050, "Armor Module: Resist Virulent [4]"),
            C(100048, 11, 1, 212, 123018, 29879, 1, 1, 2, 1, 100049, "Armor Module: Resist EMP [1]"),
            C(100049, 11, 2, 212, 123019, 29879, 2, 2, 4, 5, 100050, "Armor Module: Resist EMP [2]"),
            C(100050, 11, 3, 212, 123020, 29879, 3, 3, 6, 20, 100051, "Armor Module: Resist EMP [3]"),
            C(100051, 11, 4, 212, 123021, 29879, 4, 4, 8, 100, 900044, "Armor Module: Resist EMP [4]"),
            C(100052, 16, 1, 1212, 123253, 29879, 1, 1, 2, 1, 100053, "Weapon Module: Threat Reduction [1]"),
            C(100053, 16, 2, 1212, 123254, 29879, 2, 2, 4, 5, 100054, "Weapon Module: Threat Reduction [2]"),
            C(100054, 16, 3, 1212, 123255, 29879, 3, 3, 6, 20, 100055, "Weapon Module: Threat Reduction [3]"),
            C(100055, 16, 4, 1212, 123256, 29879, 4, 4, 8, 100, 900052, "Weapon Module: Threat Reduction [4]"),
            C(100056, 15, 1, 1212, 123158, 29879, 1, 1, 2, 1, 100057, "Weapon Module: Crit Hit Bonus [1]"),
            C(100057, 15, 2, 1212, 123159, 29879, 2, 2, 4, 5, 100058, "Weapon Module: Crit Hit Bonus [2]"),
            C(100058, 15, 3, 1212, 123160, 29879, 3, 3, 6, 20, 100059, "Weapon Module: Crit Hit Bonus [3]"),
            C(100059, 15, 4, 1212, 123161, 29879, 4, 4, 8, 100, 900051, "Weapon Module: Crit Hit Bonus [4]"),
            C(100060, 1, 1, 212, 122963, 29879, 1, 1, 2, 1, 100061, "Armor Module: Body Bonus [1]"),
            C(100061, 1, 2, 212, 122964, 29879, 2, 2, 4, 5, 100062, "Armor Module: Body Bonus [2]"),
            C(100062, 1, 3, 212, 122965, 29879, 3, 3, 6, 20, 100063, "Armor Module: Body Bonus [3]"),
            C(100063, 1, 4, 212, 122966, 29879, 4, 4, 8, 100, 900032, "Armor Module: Body Bonus [4]"),
            C(100064, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Body Bonus: Titan %(ClassName)s"),
            C(100065, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Body Bonus: Titan %(ClassName)s"),
            C(100066, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Body Bonus: Titan %(ClassName)s"),
            C(100067, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Body Bonus: Titan %(ClassName)s"),
            C(100068, 2, 1, 212, 122973, 29879, 1, 1, 2, 1, 100069, "Armor Module: Mind Bonus [1]"),
            C(100069, 2, 2, 212, 122974, 29879, 2, 2, 4, 5, 100070, "Armor Module: Mind Bonus [2]"),
            C(100070, 2, 3, 212, 122975, 29879, 3, 3, 6, 20, 100071, "Armor Module: Mind Bonus [3]"),
            C(100071, 2, 4, 212, 122976, 29879, 4, 4, 8, 100, 900036, "Armor Module: Mind Bonus [4]"),
            C(100072, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mind Bonus: Prodigy %(ClassName)s"),
            C(100073, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mind Bonus: Prodigy %(ClassName)s"),
            C(100074, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mind Bonus: Prodigy %(ClassName)s"),
            C(100075, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mind Bonus: Prodigy %(ClassName)s"),
            C(100076, 3, 1, 212, 122988, 29879, 1, 1, 2, 1, 100077, "Armor Module: Spirit Bonus [1]"),
            C(100077, 3, 2, 212, 122989, 29879, 2, 2, 4, 5, 100078, "Armor Module: Spirit Bonus [2]"),
            C(100078, 3, 3, 212, 122990, 29879, 3, 3, 6, 20, 100079, "Armor Module: Spirit Bonus [3]"),
            C(100079, 3, 4, 212, 122991, 29879, 4, 4, 8, 100, 900041, "Armor Module: Spirit Bonus [4]"),
            C(100080, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Spirit Bonus: Astra %(ClassName)s"),
            C(100081, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Spirit Bonus: Astra %(ClassName)s"),
            C(100082, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Spirit Bonus: Astra %(ClassName)s"),
            C(100083, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Spirit Bonus: Astra %(ClassName)s"),
            C(100084, 4, 1, 212, 122968, 29879, 1, 1, 2, 1, 100001, "Armor Module: Health Bonus [1]"),
            C(600000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600005, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(600006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(700000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Karem Zul's Vest"),
            C(700001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Prodigy %(ClassName)s"),
            C(700002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Astra %(ClassName)s"),
            C(700003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "\"Louise\""),
            C(700004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Olympia %(ClassName)s"),
            C(700005, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Hellstrom %(ClassName)s"),
            C(700006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Treeback Bio Boots"),
            C(900000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mestemaker Industries Helmet Lamp"),
            C(900001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "StunTest %(ClassName)s"),
            C(900002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "KnockbackTest %(ClassName)s"),
            C(900003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Speedy %(ClassName)s"),
            C(900004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "AlwaysCrit %(ClassName)s"),
            C(900006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "\"Sally\""),
            C(900007, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Physical"),
            C(900008, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Physical"),
            C(900009, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Fire"),
            C(900010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Fire"),
            C(900011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Ice"),
            C(900012, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Ice"),
            C(900013, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Virulent"),
            C(900014, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Virulent"),
            C(900015, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Laser"),
            C(900016, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Laser"),
            C(900017, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Sonic"),
            C(900018, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Sonic"),
            C(900019, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - EMP"),
            C(900020, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - EMP"),
            C(900021, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Res - Electric"),
            C(900022, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QA Dmg Imm - Electric"),
            C(900023, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "adfadf"),
            C(900024, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "\"Big Bertha\""),
            C(900025, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mooch's Fuzzy Tinfoil Hat"),
            C(900026, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Recruit Issue %(ClassName)s"),
            C(900027, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armageddon Cannon"),
            C(900028, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Sally Beefy Gun"),
            C(900029, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Cheapshot Revolver"),
            C(900030, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s of the Carebear"),
            C(900031, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Marksman's Rifle"),
            C(900032, 1, 5, 212, 122967, 29879, 0, 5, 10, 0, 0, "Armor Module: Body Bonus [5]"),
            C(900033, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Body Bonus: Customized %(ClassName)s"),
            C(900034, 4, 5, 212, 122972, 29879, 0, 5, 10, 0, 0, "Armor Module: Health Bonus [5]"),
            C(900035, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Bonus: Customized %(ClassName)s"),
            C(900036, 2, 5, 212, 122977, 29879, 0, 5, 10, 0, 0, "Armor Module: Mind Bonus [5]"),
            C(900037, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Mind Bonus: Customized %(ClassName)s"),
            C(900038, 5, 5, 212, 122982, 29879, 0, 5, 10, 0, 0, "Armor Module: Power Bonus [5]"),
            C(900039, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Power Bonus: Customized %(ClassName)s"),
            C(900040, 6, 5, 212, 122987, 29879, 0, 5, 10, 0, 0, "Armor Module: Regen Bonus [5]"),
            C(900041, 3, 5, 212, 122992, 29879, 0, 5, 10, 0, 0, "Armor Module: Spirit Bonus [5]"),
            C(900042, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Spirit Bonus: Customized %(ClassName)s"),
            C(900043, 14, 5, 212, 123017, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Electric [5]"),
            C(900044, 11, 5, 212, 123022, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist EMP [5]"),
            C(900045, 8, 5, 212, 123027, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Fire [5]"),
            C(900046, 9, 5, 212, 123032, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Ice [5]"),
            C(900047, 13, 5, 212, 123062, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Sonic [5]"),
            C(900048, 7, 5, 212, 123047, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Physical [5]"),
            C(900049, 12, 5, 212, 123042, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Photonic [5]"),
            C(900050, 10, 5, 212, 123072, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Virulent [5]"),
            C(900051, 15, 5, 1212, 123162, 29879, 0, 5, 10, 0, 0, "Weapon Module: Crit Hit Bonus [5]"),
            C(900052, 16, 5, 1212, 123257, 29879, 0, 5, 10, 0, 0, "Weapon Module: Threat Reduction [5]"),
            C(900053, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Modification Schematic"),
            C(900054, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massive Armor Regen %(ClassName)s"),
            C(900055, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massive Health Regen %(ClassName)s"),
            C(900056, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massive Power Regen %(ClassName)s"),
            C(900057, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Physical Resist Debuff Proc"),
            C(900058, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Health Vamp"),
            C(900059, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Vamp Armor"),
            C(900060, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Vamp Chi"),
            C(900061, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Vamp Power"),
            C(900062, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "New Body Amt"),
            C(900063, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "New Body Percent"),
            C(900160, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900161, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900162, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900163, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900164, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900165, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900166, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900167, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900168, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900169, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Bleedthrough Reduction: FortiMax %(ClassName)s"),
            C(900170, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Piercing: Shinobi %(ClassName)s"),
            C(900171, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Piercing: Shinobi %(ClassName)s"),
            C(900172, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Piercing: Shinobi %(ClassName)s"),
            C(900173, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Piercing: Shinobi %(ClassName)s"),
            C(900174, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Piercing: Shinobi %(ClassName)s"),
            C(900175, 18, 1, 1212, 123143, 29879, 1, 1, 2, 1, 900176, "Weapon Module: Armor Piercing [1]"),
            C(900176, 18, 2, 1212, 123144, 29879, 2, 2, 4, 5, 900177, "Weapon Module: Armor Piercing [2]"),
            C(900177, 18, 3, 1212, 123145, 29879, 3, 3, 6, 20, 900178, "Weapon Module: Armor Piercing [3]"),
            C(900178, 18, 4, 1212, 123146, 29879, 4, 4, 8, 100, 900179, "Weapon Module: Armor Piercing [4]"),
            C(900179, 18, 5, 1212, 123147, 29879, 0, 5, 10, 0, 0, "Weapon Module: Armor Piercing [5]"),
            C(900180, 19, 1, 212, 122948, 29879, 1, 1, 2, 1, 900181, "Armor Module: Armor Absorption [1]"),
            C(900181, 19, 2, 212, 122949, 29879, 2, 2, 4, 5, 900182, "Armor Module: Armor Absorption [2]"),
            C(900182, 19, 3, 212, 122950, 29879, 3, 3, 6, 20, 900183, "Armor Module: Armor Absorption [3]"),
            C(900183, 19, 4, 212, 122951, 29879, 4, 4, 8, 100, 900184, "Armor Module: Armor Absorption [4]"),
            C(900184, 19, 5, 212, 122952, 29879, 0, 5, 10, 0, 0, "Armor Module: Armor Absorption [5]"),
            C(900185, 19, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Absorption: Teleract %(ClassName)s"),
            C(900186, 19, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Absorption: Teleract %(ClassName)s"),
            C(900187, 19, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Absorption: Teleract %(ClassName)s"),
            C(900188, 19, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Absorption: Teleract %(ClassName)s"),
            C(900189, 19, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Absorption: Teleract %(ClassName)s"),
            C(900190, 20, 1, 212, 122998, 29879, 1, 1, 2, 1, 900191, "Armor Module: Health Regen Bonus [1]"),
            C(900191, 20, 2, 212, 122999, 29879, 2, 2, 4, 5, 900192, "Armor Module: Health Regen Bonus [2]"),
            C(900192, 20, 3, 212, 123000, 29879, 3, 3, 6, 20, 900193, "Armor Module: Health Regen Bonus [3]"),
            C(900193, 20, 4, 212, 123001, 29879, 4, 4, 8, 100, 900194, "Armor Module: Health Regen Bonus [4]"),
            C(900194, 20, 5, 212, 123002, 29879, 0, 5, 10, 0, 0, "Armor Module: Health Regen Bonus [5]"),
            C(900195, 44, 1, 1212, 123203, 29879, 1, 1, 2, 1, 900196, "Weapon Module: Health Regen Bonus [1]"),
            C(900196, 44, 2, 1212, 123204, 29879, 2, 2, 4, 5, 900197, "Weapon Module: Health Regen Bonus [2]"),
            C(900197, 44, 3, 1212, 123205, 29879, 3, 3, 6, 20, 900198, "Weapon Module: Health Regen Bonus [3]"),
            C(900198, 44, 4, 1212, 123206, 29879, 4, 4, 8, 100, 900199, "Weapon Module: Health Regen Bonus [4]"),
            C(900199, 44, 5, 1212, 123207, 29879, 0, 5, 10, 0, 0, "Weapon Module: Health Regen Bonus [5]"),
            C(900200, 21, 1, 212, 123003, 29879, 1, 1, 2, 1, 900201, "Armor Module: Power Regen Bonus [1]"),
            C(900201, 21, 2, 212, 123004, 29879, 2, 2, 4, 5, 900202, "Armor Module: Power Regen Bonus [2]"),
            C(900202, 21, 3, 212, 123005, 29879, 3, 3, 6, 20, 900203, "Armor Module: Power Regen Bonus [3]"),
            C(900203, 21, 4, 212, 123006, 29879, 4, 4, 8, 100, 900204, "Armor Module: Power Regen Bonus [4]"),
            C(900204, 21, 5, 212, 123007, 29879, 0, 5, 10, 0, 0, "Armor Module: Power Regen Bonus [5]"),
            C(900205, 45, 1, 1212, 123208, 29879, 1, 1, 2, 1, 900206, "Weapon Module: Power Regen Bonus [1]"),
            C(900206, 45, 2, 1212, 123209, 29879, 2, 2, 4, 5, 900207, "Weapon Module: Power Regen Bonus [2]"),
            C(900207, 45, 3, 1212, 123210, 29879, 3, 3, 6, 20, 900208, "Weapon Module: Power Regen Bonus [3]"),
            C(900208, 45, 4, 1212, 123211, 29879, 4, 4, 8, 100, 900209, "Weapon Module: Power Regen Bonus [4]"),
            C(900209, 45, 5, 1212, 123212, 29879, 0, 5, 10, 0, 0, "Weapon Module: Power Regen Bonus [5]"),
            C(900210, 22, 1, 212, 122958, 29879, 1, 1, 2, 1, 900211, "Armor Module: Armor Recharge [1]"),
            C(900211, 22, 2, 212, 122959, 29879, 2, 2, 4, 5, 900212, "Armor Module: Armor Recharge [2]"),
            C(900212, 22, 3, 212, 122960, 29879, 3, 3, 6, 20, 900213, "Armor Module: Armor Recharge [3]"),
            C(900213, 22, 4, 212, 122961, 29879, 4, 4, 8, 100, 900214, "Armor Module: Armor Recharge [4]"),
            C(900214, 22, 5, 212, 122962, 29879, 0, 5, 10, 0, 0, "Armor Module: Armor Recharge [5]"),
            C(900215, 46, 1, 1212, 123148, 29879, 1, 1, 2, 1, 900216, "Weapon Module: Armor Recharge [1]"),
            C(900216, 46, 2, 1212, 123149, 29879, 2, 2, 4, 5, 900217, "Weapon Module: Armor Recharge [2]"),
            C(900217, 46, 3, 1212, 123150, 29879, 3, 3, 6, 20, 900218, "Weapon Module: Armor Recharge [3]"),
            C(900218, 46, 4, 1212, 123151, 29879, 4, 4, 8, 100, 900219, "Weapon Module: Armor Recharge [4]"),
            C(900219, 46, 5, 1212, 123152, 29879, 0, 5, 10, 0, 0, "Weapon Module: Armor Recharge [5]"),
            C(900220, 32, 1, 1212, 123188, 29879, 1, 1, 2, 1, 900221, "Weapon Module: Debuff Physical Resist [1]"),
            C(900221, 32, 2, 1212, 123189, 29879, 2, 2, 4, 5, 900222, "Weapon Module: Debuff Physical Resist [2]"),
            C(900222, 32, 3, 1212, 123190, 29879, 3, 3, 6, 20, 900223, "Weapon Module: Debuff Physical Resist [3]"),
            C(900223, 32, 4, 1212, 123191, 29879, 4, 4, 8, 100, 900224, "Weapon Module: Debuff Physical Resist [4]"),
            C(900224, 32, 5, 1212, 123192, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Physical Resist [5]"),
            C(900225, 33, 1, 1212, 123193, 29879, 1, 1, 2, 1, 900226, "Weapon Module: Debuff Sonic Resist [1]"),
            C(900226, 33, 2, 1212, 123194, 29879, 2, 2, 4, 5, 900227, "Weapon Module: Debuff Sonic Resist [2]"),
            C(900227, 33, 3, 1212, 123195, 29879, 3, 3, 6, 20, 900228, "Weapon Module: Debuff Sonic Resist [3]"),
            C(900228, 33, 4, 1212, 123196, 29879, 4, 4, 8, 100, 900229, "Weapon Module: Debuff Sonic Resist [4]"),
            C(900229, 33, 5, 1212, 123197, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Sonic Resist [5]"),
            C(900230, 23, 1, 1212, 123163, 29879, 1, 1, 2, 1, 900231, "Weapon Module: Debuff Electric Resist [1]"),
            C(900231, 23, 2, 1212, 123164, 29879, 2, 2, 4, 5, 900232, "Weapon Module: Debuff Electric Resist [2]"),
            C(900232, 23, 3, 1212, 123165, 29879, 3, 3, 6, 20, 900233, "Weapon Module: Debuff Electric Resist [3]"),
            C(900233, 23, 4, 1212, 123166, 29879, 4, 4, 8, 100, 900234, "Weapon Module: Debuff Electric Resist [4]"),
            C(900234, 23, 5, 1212, 123167, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Electric Resist [5]"),
            C(900235, 31, 1, 1212, 123183, 29879, 1, 1, 2, 1, 900236, "Weapon Module: Debuff Photonic Resist [1]"),
            C(900236, 31, 2, 1212, 123184, 29879, 2, 2, 4, 5, 900237, "Weapon Module: Debuff Photonic Resist [2]"),
            C(900237, 31, 3, 1212, 123185, 29879, 3, 3, 6, 20, 900238, "Weapon Module: Debuff Photonic Resist [3]"),
            C(900238, 31, 4, 1212, 123186, 29879, 4, 4, 8, 100, 900239, "Weapon Module: Debuff Photonic Resist [4]"),
            C(900239, 31, 5, 1212, 123187, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Photonic Resist [5]"),
            C(900240, 29, 1, 1212, 123173, 29879, 1, 1, 2, 1, 900241, "Weapon Module: Debuff Fire Resist [1]"),
            C(900241, 29, 2, 1212, 123174, 29879, 2, 2, 4, 5, 900242, "Weapon Module: Debuff Fire Resist [2]"),
            C(900242, 29, 3, 1212, 123175, 29879, 3, 3, 6, 20, 900243, "Weapon Module: Debuff Fire Resist [3]"),
            C(900243, 29, 4, 1212, 123176, 29879, 4, 4, 8, 100, 900244, "Weapon Module: Debuff Fire Resist [4]"),
            C(900244, 29, 5, 1212, 123177, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Fire Resist [5]"),
            C(900245, 30, 1, 1212, 123178, 29879, 1, 1, 2, 1, 900246, "Weapon Module: Debuff Ice Resist [1]"),
            C(900246, 30, 2, 1212, 123179, 29879, 2, 2, 4, 5, 900247, "Weapon Module: Debuff Ice Resist [2]"),
            C(900247, 30, 3, 1212, 123180, 29879, 3, 3, 6, 20, 900248, "Weapon Module: Debuff Ice Resist [3]"),
            C(900248, 30, 4, 1212, 123181, 29879, 4, 4, 8, 100, 900249, "Weapon Module: Debuff Ice Resist [4]"),
            C(900249, 30, 5, 1212, 123182, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Ice Resist [5]"),
            C(900250, 34, 1, 1212, 123198, 29879, 1, 1, 2, 1, 900251, "Weapon Module: Debuff Virulent Resist [1]"),
            C(900251, 34, 2, 1212, 123199, 29879, 2, 2, 4, 5, 900252, "Weapon Module: Debuff Virulent Resist [2]"),
            C(900252, 34, 3, 1212, 123200, 29879, 3, 3, 6, 20, 900253, "Weapon Module: Debuff Virulent Resist [3]"),
            C(900253, 34, 4, 1212, 123201, 29879, 4, 4, 8, 100, 900254, "Weapon Module: Debuff Virulent Resist [4]"),
            C(900254, 34, 5, 1212, 123202, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff Virulent Resist [5]"),
            C(900255, 28, 1, 1212, 123168, 29879, 1, 1, 2, 1, 900256, "Weapon Module: Debuff EMP Resist [1]"),
            C(900256, 28, 2, 1212, 123169, 29879, 2, 2, 4, 5, 900257, "Weapon Module: Debuff EMP Resist [2]"),
            C(900257, 28, 3, 1212, 123170, 29879, 3, 3, 6, 20, 900258, "Weapon Module: Debuff EMP Resist [3]"),
            C(900258, 28, 4, 1212, 123171, 29879, 4, 4, 8, 100, 900259, "Weapon Module: Debuff EMP Resist [4]"),
            C(900259, 28, 5, 1212, 123172, 29879, 0, 5, 10, 0, 0, "Weapon Module: Debuff EMP Resist [5]"),
            C(900260, 24, 1, 1212, 123263, 29879, 1, 1, 2, 1, 900261, "Weapon Module: Steal Armor [1]"),
            C(900261, 24, 2, 1212, 123264, 29879, 2, 2, 4, 5, 900262, "Weapon Module: Steal Armor [2]"),
            C(900262, 24, 3, 1212, 123265, 29879, 3, 3, 6, 20, 900263, "Weapon Module: Steal Armor [3]"),
            C(900263, 24, 4, 1212, 123266, 29879, 4, 4, 8, 100, 900264, "Weapon Module: Steal Armor [4]"),
            C(900264, 24, 5, 1212, 123267, 29879, 0, 5, 10, 0, 0, "Weapon Module: Steal Armor [5]"),
            C(900265, 26, 1, 1212, 123268, 29879, 1, 1, 2, 1, 900266, "Weapon Module: Steal Health [1]"),
            C(900266, 26, 2, 1212, 123269, 29879, 2, 2, 4, 5, 900267, "Weapon Module: Steal Health [2]"),
            C(900267, 26, 3, 1212, 123270, 29879, 3, 3, 6, 20, 900268, "Weapon Module: Steal Health [3]"),
            C(900268, 26, 4, 1212, 123271, 29879, 4, 4, 8, 100, 900269, "Weapon Module: Steal Health [4]"),
            C(900269, 26, 5, 1212, 123272, 29879, 0, 5, 10, 0, 0, "Weapon Module: Steal Health [5]"),
            C(900270, 27, 1, 1212, 123273, 29879, 1, 1, 2, 1, 900271, "Weapon Module: Steal Power [1]"),
            C(900271, 27, 2, 1212, 123274, 29879, 2, 2, 4, 5, 900272, "Weapon Module: Steal Power [2]"),
            C(900272, 27, 3, 1212, 123275, 29879, 3, 3, 6, 20, 900273, "Weapon Module: Steal Power [3]"),
            C(900273, 27, 4, 1212, 123276, 29879, 4, 4, 8, 100, 900274, "Weapon Module: Steal Power [4]"),
            C(900274, 27, 5, 1212, 123277, 29879, 0, 5, 10, 0, 0, "Weapon Module: Steal Power [5]"),
            C(900275, 25, 1, 1212, 123258, 29879, 1, 1, 2, 1, 900276, "Weapon Module: Steal Adrenaline [1]"),
            C(900276, 25, 2, 1212, 123259, 29879, 2, 2, 4, 5, 900277, "Weapon Module: Steal Adrenaline [2]"),
            C(900277, 25, 3, 1212, 123260, 29879, 3, 3, 6, 20, 900278, "Weapon Module: Steal Adrenaline [3]"),
            C(900278, 25, 4, 1212, 123261, 29879, 4, 4, 8, 100, 900279, "Weapon Module: Steal Adrenaline [4]"),
            C(900279, 25, 5, 1212, 123262, 29879, 0, 5, 10, 0, 0, "Weapon Module: Steal Adrenaline [5]"),
            C(900280, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "MASSIVE Health"),
            C(900281, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Dobeck's Armor"),
            C(900282, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Teleract %(ClassName)s"),
            C(900283, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Teleract %(ClassName)s"),
            C(900284, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Hellstrom %(ClassName)s"),
            C(900285, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Olympia %(ClassName)s"),
            C(900286, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Teleract %(ClassName)s"),
            C(900287, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Titan %(ClassName)s"),
            C(900288, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Astra %(ClassName)s"),
            C(900289, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Prodigy %(ClassName)s"),
            C(900290, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Shinobi %(ClassName)s"),
            C(900291, 43, 1, 1212, 123213, 29879, 1, 1, 2, 1, 900292, "Weapon Module: Resist Electric [1]"),
            C(900292, 43, 2, 1212, 123214, 29879, 2, 2, 4, 5, 900293, "Weapon Module: Resist Electric [2]"),
            C(900293, 43, 3, 1212, 123215, 29879, 3, 3, 6, 20, 900294, "Weapon Module: Resist Electric [3]"),
            C(900294, 43, 4, 1212, 123216, 29879, 4, 4, 8, 100, 900295, "Weapon Module: Resist Electric [4]"),
            C(900295, 43, 5, 1212, 123217, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Electric [5]"),
            C(900296, 40, 1, 1212, 123218, 29879, 1, 1, 2, 1, 900297, "Weapon Module: Resist EMP [1]"),
            C(900297, 40, 2, 1212, 123219, 29879, 2, 2, 4, 5, 900298, "Weapon Module: Resist EMP [2]"),
            C(900298, 40, 3, 1212, 123220, 29879, 3, 3, 6, 20, 900299, "Weapon Module: Resist EMP [3]"),
            C(900299, 40, 4, 1212, 123221, 29879, 4, 4, 8, 100, 900300, "Weapon Module: Resist EMP [4]"),
            C(900300, 40, 5, 1212, 123222, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist EMP [5]"),
            C(900301, 37, 1, 1212, 123223, 29879, 1, 1, 2, 1, 900302, "Weapon Module: Resist Fire [1]"),
            C(900302, 37, 2, 1212, 123224, 29879, 2, 2, 4, 5, 900303, "Weapon Module: Resist Fire [2]"),
            C(900303, 37, 3, 1212, 123225, 29879, 3, 3, 6, 20, 900304, "Weapon Module: Resist Fire [3]"),
            C(900304, 37, 4, 1212, 123226, 29879, 4, 4, 8, 100, 900305, "Weapon Module: Resist Fire [4]"),
            C(900305, 37, 5, 1212, 123227, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Fire [5]"),
            C(900306, 38, 1, 1212, 123228, 29879, 1, 1, 2, 1, 900307, "Weapon Module: Resist Ice [1]"),
            C(900307, 38, 2, 1212, 123229, 29879, 2, 2, 4, 5, 900308, "Weapon Module: Resist Ice [2]"),
            C(900308, 38, 3, 1212, 123230, 29879, 3, 3, 6, 20, 900309, "Weapon Module: Resist Ice [3]"),
            C(900309, 38, 4, 1212, 123231, 29879, 4, 4, 8, 100, 900310, "Weapon Module: Resist Ice [4]"),
            C(900310, 38, 5, 1212, 123232, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Ice [5]"),
            C(900311, 41, 1, 1212, 123233, 29879, 1, 1, 2, 1, 900312, "Weapon Module: Resist Photonic [1]"),
            C(900312, 41, 2, 1212, 123234, 29879, 2, 2, 4, 5, 900313, "Weapon Module: Resist Photonic [2]"),
            C(900313, 41, 3, 1212, 123235, 29879, 3, 3, 6, 20, 900314, "Weapon Module: Resist Photonic [3]"),
            C(900314, 41, 4, 1212, 123236, 29879, 4, 4, 8, 100, 900315, "Weapon Module: Resist Photonic [4]"),
            C(900315, 41, 5, 1212, 123237, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Photonic [5]"),
            C(900316, 36, 1, 1212, 123238, 29879, 1, 1, 2, 1, 900317, "Weapon Module: Resist Physical [1]"),
            C(900317, 36, 2, 1212, 123239, 29879, 2, 2, 4, 5, 900318, "Weapon Module: Resist Physical [2]"),
            C(900318, 36, 3, 1212, 123240, 29879, 3, 3, 6, 20, 900319, "Weapon Module: Resist Physical [3]"),
            C(900319, 36, 4, 1212, 123241, 29879, 4, 4, 8, 100, 900320, "Weapon Module: Resist Physical [4]"),
            C(900320, 36, 5, 1212, 123242, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Physical [5]"),
            C(900321, 42, 1, 1212, 123243, 29879, 1, 1, 2, 1, 900322, "Weapon Module: Resist Sonic [1]"),
            C(900322, 42, 2, 1212, 123244, 29879, 2, 2, 4, 5, 900323, "Weapon Module: Resist Sonic [2]"),
            C(900323, 42, 3, 1212, 123245, 29879, 3, 3, 6, 20, 900324, "Weapon Module: Resist Sonic [3]"),
            C(900324, 42, 4, 1212, 123246, 29879, 4, 4, 8, 100, 900325, "Weapon Module: Resist Sonic [4]"),
            C(900325, 42, 5, 1212, 123247, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Sonic [5]"),
            C(900326, 39, 1, 1212, 123248, 29879, 1, 1, 2, 1, 900327, "Weapon Module: Resist Virulent [1]"),
            C(900327, 39, 2, 1212, 123249, 29879, 2, 2, 4, 5, 900328, "Weapon Module: Resist Virulent [2]"),
            C(900328, 39, 3, 1212, 123250, 29879, 3, 3, 6, 20, 900329, "Weapon Module: Resist Virulent [3]"),
            C(900329, 39, 4, 1212, 123251, 29879, 4, 4, 8, 100, 900330, "Weapon Module: Resist Virulent [4]"),
            C(900330, 39, 5, 1212, 123252, 29879, 0, 5, 10, 0, 0, "Weapon Module: Resist Virulent [5]"),
            C(900331, 35, 1, 1212, 123153, 29879, 1, 1, 2, 1, 900332, "Weapon Module: Regen Bonus [1]"),
            C(900332, 35, 2, 1212, 123154, 29879, 2, 2, 4, 5, 900333, "Weapon Module: Regen Bonus [2]"),
            C(900333, 35, 3, 1212, 123155, 29879, 3, 3, 6, 20, 900334, "Weapon Module: Regen Bonus [3]"),
            C(900334, 35, 4, 1212, 123156, 29879, 4, 4, 8, 100, 900335, "Weapon Module: Regen Bonus [4]"),
            C(900335, 35, 5, 1212, 123157, 29879, 0, 5, 10, 0, 0, "Weapon Module: Regen Bonus [5]"),
            C(900336, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Ability Damage Test"),
            C(900337, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Ranged Damage Test"),
            C(900338, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Melee Damage"),
            C(900339, 35, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Regen Bonus: %(TemplateName)s"),
            C(900340, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: The Avatar"),
            C(900341, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST AE when hit %(ClassName)s"),
            C(900342, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Fast Reload %(ClassName)s"),
            C(900344, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST XP %(ClassName)s"),
            C(900345, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: The Embodiment"),
            C(900346, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST AE Heal+Repair %(ClassName)s"),
            C(900347, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: The Vessel"),
            C(900348, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk I"),
            C(900349, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk I"),
            C(900350, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk I"),
            C(900351, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk I"),
            C(900352, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk I"),
            C(900353, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk I"),
            C(900354, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk I"),
            C(900355, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Power Cost %(ClassName)s"),
            C(900356, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set Module: Royal Flush"),
            C(900364, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Specific Ability Damage %(ClassName)s"),
            C(900366, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Praetorian"),
            C(900367, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Panzer Tank"),
            C(900368, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set Module: Stormcaller"),
            C(900369, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mercurial"),
            C(900370, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: La R\u00e9sistance"),
            C(900371, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: The Leviathan"),
            C(900372, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Aegis Armor"),
            C(900373, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Ragnarok"),
            C(900374, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Boargar Hide"),
            C(900375, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Iron Sentient"),
            C(900376, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set Module: The Ultimatum"),
            C(900377, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: \"Lucky\" Owens' Gear"),
            C(900378, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set Module: Protector"),
            C(900379, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Resist %(TemplateName)s"),
            C(900380, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST KillStreak %(TemplateName)s"),
            C(900381, 49, 1, 212, 123008, 29879, 1, 1, 2, 1, 900382, "Armor Module: Resist Blinding [1]"),
            C(900382, 49, 2, 212, 123009, 29879, 2, 2, 4, 5, 900383, "Armor Module: Resist Blinding [2]"),
            C(900383, 49, 3, 212, 123010, 29879, 3, 3, 6, 20, 900384, "Armor Module: Resist Blinding [3]"),
            C(900384, 49, 4, 212, 123011, 29879, 4, 4, 8, 100, 900385, "Armor Module: Resist Blinding [4]"),
            C(900385, 49, 5, 212, 123012, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Blinding [5]"),
            C(900386, 50, 1, 212, 123033, 29879, 1, 1, 2, 1, 900387, "Armor Module: Resist Knockback [1]"),
            C(900387, 50, 2, 212, 123034, 29879, 2, 2, 4, 5, 900388, "Armor Module: Resist Knockback [2]"),
            C(900388, 50, 3, 212, 123035, 29879, 3, 3, 6, 20, 900389, "Armor Module: Resist Knockback [3]"),
            C(900389, 50, 4, 212, 123036, 29879, 4, 4, 8, 100, 900390, "Armor Module: Resist Knockback [4]"),
            C(900390, 50, 5, 212, 123037, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Knockback [5]"),
            C(900391, 53, 1, 212, 123063, 29879, 1, 1, 2, 1, 900392, "Armor Module: Resist Stun [1]"),
            C(900392, 53, 2, 212, 123064, 29879, 2, 2, 4, 5, 900393, "Armor Module: Resist Stun [2]"),
            C(900393, 53, 3, 212, 123065, 29879, 3, 3, 6, 20, 900394, "Armor Module: Resist Stun [3]"),
            C(900394, 53, 4, 212, 123066, 29879, 4, 4, 8, 100, 900395, "Armor Module: Resist Stun [4]"),
            C(900395, 53, 5, 212, 123067, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Stun [5]"),
            C(900396, 51, 1, 212, 123048, 29879, 1, 1, 2, 1, 900397, "Armor Module: Resist Root [1]"),
            C(900397, 51, 2, 212, 123049, 29879, 2, 2, 4, 5, 900398, "Armor Module: Resist Root [2]"),
            C(900398, 51, 3, 212, 123050, 29879, 3, 3, 6, 20, 900399, "Armor Module: Resist Root [3]"),
            C(900399, 51, 4, 212, 123051, 29879, 4, 4, 8, 100, 900400, "Armor Module: Resist Root [4]"),
            C(900400, 51, 5, 212, 123052, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Root [5]"),
            C(900401, 54, 1, 212, 123073, 29879, 1, 1, 2, 1, 900402, "Armor Module: Experience Bonus [1]"),
            C(900402, 54, 2, 212, 123074, 29879, 2, 2, 4, 5, 900403, "Armor Module: Experience Bonus [2]"),
            C(900403, 54, 3, 212, 123075, 29879, 3, 3, 6, 20, 900404, "Armor Module: Experience Bonus [3]"),
            C(900404, 54, 4, 212, 123076, 29879, 4, 4, 8, 100, 900405, "Armor Module: Experience Bonus [4]"),
            C(900405, 54, 5, 212, 123077, 29879, 0, 5, 10, 0, 0, "Armor Module: Experience Bonus [5]"),
            C(900406, 52, 1, 212, 123053, 29879, 1, 1, 2, 1, 900407, "Armor Module: Resist Snare [1]"),
            C(900407, 52, 2, 212, 123054, 29879, 2, 2, 4, 5, 900408, "Armor Module: Resist Snare [2]"),
            C(900408, 52, 3, 212, 123055, 29879, 3, 3, 6, 20, 900409, "Armor Module: Resist Snare [3]"),
            C(900409, 52, 4, 212, 123056, 29879, 4, 4, 8, 100, 900410, "Armor Module: Resist Snare [4]"),
            C(900410, 52, 5, 212, 123057, 29879, 0, 5, 10, 0, 0, "Armor Module: Resist Snare [5]"),
            C(900411, 48, 1, 212, 122993, 29879, 1, 1, 2, 1, 900412, "Armor Module: Move Speed Bonus [1]"),
            C(900412, 48, 2, 212, 122994, 29879, 2, 2, 4, 5, 900413, "Armor Module: Move Speed Bonus [2]"),
            C(900413, 48, 3, 212, 122995, 29879, 3, 3, 6, 20, 900414, "Armor Module: Move Speed Bonus [3]"),
            C(900414, 48, 4, 212, 122996, 29879, 4, 4, 8, 100, 900415, "Armor Module: Move Speed Bonus [4]"),
            C(900415, 48, 5, 212, 122997, 29879, 0, 5, 10, 0, 0, "Armor Module: Move Speed Bonus [5]"),
            C(900416, 47, 1, 212, 122953, 29879, 1, 1, 2, 1, 900417, "Armor Module: Armor Piercing [1]"),
            C(900417, 47, 2, 212, 122954, 29879, 2, 2, 4, 5, 900418, "Armor Module: Armor Piercing [2]"),
            C(900418, 47, 3, 212, 122955, 29879, 3, 3, 6, 20, 900419, "Armor Module: Armor Piercing [3]"),
            C(900419, 47, 4, 212, 122956, 29879, 4, 4, 8, 100, 900420, "Armor Module: Armor Piercing [4]"),
            C(900420, 47, 5, 212, 122957, 29879, 0, 5, 10, 0, 0, "Armor Module: Armor Piercing [5]"),
            C(900421, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Movement %(TemplateName)s"),
            C(900422, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Proc DD Sonic"),
            C(900423, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Life and Death"),
            C(900424, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Jolene's Rainbow"),
            C(900425, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Dismantler"),
            C(900426, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Unity"),
            C(900427, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Bang and Blame"),
            C(900428, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Stabilization"),
            C(900429, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Caustic"),
            C(900430, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Corporal Ziskey"),
            C(900431, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Fire and Ice"),
            C(900432, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Omega Forces"),
            C(900433, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Pvt. Dan's Forest Gear"),
            C(900434, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Tactical Analysis"),
            C(900435, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Panzer Armor"),
            C(900436, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Remembrance"),
            C(900437, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Sgt. Hulka's Armor"),
            C(900438, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Pvt. Oxburger's Armor"),
            C(900439, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Thunder and Lightning"),
            C(900440, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Anger and Rot"),
            C(900441, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Fast Track"),
            C(900442, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Grunt's Armor"),
            C(900443, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Shock and Awe"),
            C(900444, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Reinforced"),
            C(900445, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Flasharmor"),
            C(900446, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Major Servo's Duds"),
            C(900447, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900448, 46, 1, 1273, 123078, 29879, 1, 1, 2, 1, 900449, "Tool Module: Armor Absorption [1]"),
            C(900449, 46, 2, 1273, 123079, 29879, 2, 2, 4, 5, 900450, "Tool Module: Armor Absorption [2]"),
            C(900450, 46, 3, 1273, 123080, 29879, 3, 3, 6, 20, 900451, "Tool Module: Armor Absorption [3]"),
            C(900451, 46, 4, 1273, 123081, 29879, 4, 4, 8, 100, 900452, "Tool Module: Armor Absorption [4]"),
            C(900452, 46, 5, 1273, 123082, 29879, 0, 5, 10, 0, 0, "Tool Module: Armor Absorption [5]"),
            C(900453, 35, 1, 1273, 123083, 29879, 1, 1, 2, 1, 900454, "Tool Module: Regen Bonus [1]"),
            C(900454, 35, 2, 1273, 123084, 29879, 2, 2, 4, 5, 900455, "Tool Module: Regen Bonus [2]"),
            C(900455, 35, 3, 1273, 123085, 29879, 3, 3, 6, 20, 900456, "Tool Module: Regen Bonus [3]"),
            C(900456, 35, 4, 1273, 123086, 29879, 4, 4, 8, 100, 900457, "Tool Module: Regen Bonus [4]"),
            C(900457, 35, 5, 1273, 123087, 29879, 0, 5, 10, 0, 0, "Tool Module: Regen Bonus [5]"),
            C(900458, 44, 1, 1273, 123088, 29879, 1, 1, 2, 1, 900459, "Tool Module: Health Regen Bonus [1]"),
            C(900459, 44, 2, 1273, 123089, 29879, 2, 2, 4, 5, 900460, "Tool Module: Health Regen Bonus [2]"),
            C(900460, 44, 3, 1273, 123090, 29879, 3, 3, 6, 20, 900461, "Tool Module: Health Regen Bonus [3]"),
            C(900461, 44, 4, 1273, 123091, 29879, 4, 4, 8, 100, 900462, "Tool Module: Health Regen Bonus [4]"),
            C(900462, 44, 5, 1273, 123092, 29879, 0, 5, 10, 0, 0, "Tool Module: Health Regen Bonus [5]"),
            C(900463, 45, 1, 1273, 123093, 29879, 1, 1, 2, 1, 900464, "Tool Module: Power Regen Bonus [1]"),
            C(900464, 45, 2, 1273, 123094, 29879, 2, 2, 4, 5, 900465, "Tool Module: Power Regen Bonus [2]"),
            C(900465, 45, 3, 1273, 123095, 29879, 3, 3, 6, 20, 900466, "Tool Module: Power Regen Bonus [3]"),
            C(900466, 45, 4, 1273, 123096, 29879, 4, 4, 8, 100, 900467, "Tool Module: Power Regen Bonus [4]"),
            C(900467, 45, 5, 1273, 123097, 29879, 0, 5, 10, 0, 0, "Tool Module: Power Regen Bonus [5]"),
            C(900468, 43, 1, 1273, 123098, 29879, 1, 1, 2, 1, 900469, "Tool Module: Resist Electric [1]"),
            C(900469, 43, 2, 1273, 123099, 29879, 2, 2, 4, 5, 900470, "Tool Module: Resist Electric [2]"),
            C(900470, 43, 3, 1273, 123100, 29879, 3, 3, 6, 20, 900471, "Tool Module: Resist Electric [3]"),
            C(900471, 43, 4, 1273, 123101, 29879, 4, 4, 8, 100, 900472, "Tool Module: Resist Electric [4]"),
            C(900472, 43, 5, 1273, 123102, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Electric [5]"),
            C(900473, 40, 1, 1273, 123103, 29879, 1, 1, 2, 1, 900474, "Tool Module: Resist EMP [1]"),
            C(900474, 40, 2, 1273, 123104, 29879, 2, 2, 4, 5, 900475, "Tool Module: Resist EMP [2]"),
            C(900475, 40, 3, 1273, 123105, 29879, 3, 3, 6, 20, 900476, "Tool Module: Resist EMP [3]"),
            C(900476, 40, 4, 1273, 123106, 29879, 4, 4, 8, 100, 900477, "Tool Module: Resist EMP [4]"),
            C(900477, 40, 5, 1273, 123107, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist EMP [5]"),
            C(900478, 37, 1, 1273, 123108, 29879, 1, 1, 2, 1, 900479, "Tool Module: Resist Fire [1]"),
            C(900479, 37, 2, 1273, 123109, 29879, 2, 2, 4, 5, 900480, "Tool Module: Resist Fire [2]"),
            C(900480, 37, 3, 1273, 123110, 29879, 3, 3, 6, 20, 900481, "Tool Module: Resist Fire [3]"),
            C(900481, 37, 4, 1273, 123111, 29879, 4, 4, 8, 100, 900482, "Tool Module: Resist Fire [4]"),
            C(900482, 37, 5, 1273, 123112, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Fire [5]"),
            C(900483, 38, 1, 1273, 123113, 29879, 1, 1, 2, 1, 900484, "Tool Module: Resist Ice [1]"),
            C(900484, 38, 2, 1273, 123114, 29879, 2, 2, 4, 5, 900485, "Tool Module: Resist Ice [2]"),
            C(900485, 38, 3, 1273, 123115, 29879, 3, 3, 6, 20, 900486, "Tool Module: Resist Ice [3]"),
            C(900486, 38, 4, 1273, 123116, 29879, 4, 4, 8, 100, 900487, "Tool Module: Resist Ice [4]"),
            C(900487, 38, 5, 1273, 123117, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Ice [5]"),
            C(900488, 41, 1, 1273, 123118, 29879, 1, 1, 2, 1, 900489, "Tool Module: Resist Photonic [1]"),
            C(900489, 41, 2, 1273, 123119, 29879, 2, 2, 4, 5, 900490, "Tool Module: Resist Photonic [2]"),
            C(900490, 41, 3, 1273, 123120, 29879, 3, 3, 6, 20, 900491, "Tool Module: Resist Photonic [3]"),
            C(900491, 41, 4, 1273, 123121, 29879, 4, 4, 8, 100, 900492, "Tool Module: Resist Photonic [4]"),
            C(900492, 41, 5, 1273, 123122, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Photonic [5]"),
            C(900493, 36, 1, 1273, 123123, 29879, 1, 1, 2, 1, 900494, "Tool Module: Resist Physical [1]"),
            C(900494, 36, 2, 1273, 123124, 29879, 2, 2, 4, 5, 900495, "Tool Module: Resist Physical [2]"),
            C(900495, 36, 3, 1273, 123125, 29879, 3, 3, 6, 20, 900496, "Tool Module: Resist Physical [3]"),
            C(900496, 36, 4, 1273, 123126, 29879, 4, 4, 8, 100, 900497, "Tool Module: Resist Physical [4]"),
            C(900497, 36, 5, 1273, 123127, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Physical [5]"),
            C(900498, 42, 1, 1273, 123128, 29879, 1, 1, 2, 1, 900499, "Tool Module: Resist Sonic [1]"),
            C(900499, 42, 2, 1273, 123129, 29879, 2, 2, 4, 5, 900500, "Tool Module: Resist Sonic [2]"),
            C(900500, 42, 3, 1273, 123130, 29879, 3, 3, 6, 20, 900501, "Tool Module: Resist Sonic [3]"),
            C(900501, 42, 4, 1273, 123131, 29879, 4, 4, 8, 100, 900502, "Tool Module: Resist Sonic [4]"),
            C(900502, 42, 5, 1273, 123132, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Sonic [5]"),
            C(900503, 39, 1, 1273, 123133, 29879, 1, 1, 2, 1, 900504, "Tool Module: Resist Virulent [1]"),
            C(900504, 39, 2, 1273, 123134, 29879, 2, 2, 4, 5, 900505, "Tool Module: Resist Virulent [2]"),
            C(900505, 39, 3, 1273, 123135, 29879, 3, 3, 6, 20, 900506, "Tool Module: Resist Virulent [3]"),
            C(900506, 39, 4, 1273, 123136, 29879, 4, 4, 8, 100, 900507, "Tool Module: Resist Virulent [4]"),
            C(900507, 39, 5, 1273, 123137, 29879, 0, 5, 10, 0, 0, "Tool Module: Resist Virulent [5]"),
            C(900508, 16, 1, 1273, 123138, 29879, 1, 1, 2, 1, 900509, "Tool Module: Threat Reduction [1]"),
            C(900509, 16, 2, 1273, 123139, 29879, 2, 2, 4, 5, 900510, "Tool Module: Threat Reduction [2]"),
            C(900510, 16, 3, 1273, 123140, 29879, 3, 3, 6, 20, 900511, "Tool Module: Threat Reduction [3]"),
            C(900511, 16, 4, 1273, 123141, 29879, 4, 4, 8, 100, 900512, "Tool Module: Threat Reduction [4]"),
            C(900512, 16, 5, 1273, 123142, 29879, 0, 5, 10, 0, 0, "Tool Module: Threat Reduction [5]"),
            C(900513, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Purifier"),
            C(900514, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set Module: Field Agent"),
            C(900515, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900516, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900517, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900518, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900519, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900520, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900521, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900522, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900523, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900524, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900525, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900526, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900527, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900528, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900529, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900530, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900531, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900532, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900533, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900534, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900535, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900536, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900537, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900538, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900539, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900540, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900541, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900542, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900543, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900544, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900545, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900546, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900547, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900548, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900549, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900550, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900551, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900552, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900553, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900554, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900556, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900557, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900558, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk II"),
            C(900559, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk III"),
            C(900560, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk IV"),
            C(900561, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk V"),
            C(900562, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk VI"),
            C(900563, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk VII"),
            C(900564, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Evasion Suit Mk VIII"),
            C(900565, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900566, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900567, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk II"),
            C(900568, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk III"),
            C(900569, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk IV"),
            C(900570, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk V"),
            C(900571, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk VI"),
            C(900572, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk VII"),
            C(900573, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Mechanic Suit Mk VIII"),
            C(900574, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk II"),
            C(900575, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk III"),
            C(900576, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk IV"),
            C(900577, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk V"),
            C(900578, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk VI"),
            C(900579, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk VII"),
            C(900580, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Recoil Suit Mk VIII"),
            C(900581, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk II"),
            C(900582, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900583, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk III"),
            C(900584, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk IV"),
            C(900585, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mv V"),
            C(900586, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk VI"),
            C(900587, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk VII"),
            C(900588, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Scout Suit Mk VIII"),
            C(900589, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk II"),
            C(900590, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900591, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk III"),
            C(900592, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk IV"),
            C(900593, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk V"),
            C(900594, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk VI"),
            C(900595, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk VII"),
            C(900596, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Steadfast Suit Mk VIII"),
            C(900597, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk II"),
            C(900599, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk III"),
            C(900600, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk IV"),
            C(900601, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk V"),
            C(900602, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk VI"),
            C(900603, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk VII"),
            C(900604, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Support Suit Mk VIII"),
            C(900605, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk II"),
            C(900607, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk III"),
            C(900608, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk IV"),
            C(900609, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk V"),
            C(900610, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk VI"),
            C(900611, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk VII"),
            C(900612, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Survival Suit Mk VIII"),
            C(900613, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900614, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900615, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900616, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900617, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk I"),
            C(900618, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk I"),
            C(900619, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk I"),
            C(900620, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk I"),
            C(900621, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Shinobi Assassin Mk I"),
            C(900622, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Accelera Tachyon Mk I"),
            C(900623, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: AccuMax Marksman Mk I"),
            C(900624, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Astra Harmony Mk I"),
            C(900625, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk I"),
            C(900626, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk I"),
            C(900627, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Proc DD Physical"),
            C(900628, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Physical"),
            C(900629, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Proc Heal Armor AE"),
            C(900630, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Proc Heal Health AE"),
            C(900657, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD Fire"),
            C(900658, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD Ice"),
            C(900659, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD Virulent"),
            C(900660, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD Light"),
            C(900661, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD Energy"),
            C(900662, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc DD EMP"),
            C(900663, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE EMP"),
            C(900664, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Virulent"),
            C(900665, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Sonic"),
            C(900666, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Energy"),
            C(900667, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Fire"),
            C(900668, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Ice"),
            C(900669, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE Light"),
            C(900670, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Physical"),
            C(900671, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Virulent"),
            C(900672, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Sonic"),
            C(900673, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Light"),
            C(900674, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Ice"),
            C(900675, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Fire"),
            C(900676, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit Energy"),
            C(900677, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc AE When Hit EMP"),
            C(900678, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc Knockback When Hit"),
            C(900679, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc Stun When Hit"),
            C(900680, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc Knockback"),
            C(900681, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Proc Stun"),
            C(900682, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Ability"),
            C(900683, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Melee"),
            C(900684, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Ranged"),
            C(900685, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage EMP"),
            C(900686, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Energy"),
            C(900687, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Ice"),
            C(900688, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Fire"),
            C(900689, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Physical"),
            C(900690, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Sonic"),
            C(900691, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Virulent"),
            C(900692, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Damage Light"),
            C(900693, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Lightning Damage"),
            C(900694, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Lightning Power"),
            C(900695, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Vamp Armor"),
            C(900696, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Vamp Health"),
            C(900697, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Vamp Power"),
            C(900698, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Vamp Adrenaline"),
            C(900699, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Mod Kill Streak"),
            C(900700, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Mod XP Gain"),
            C(900701, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Virulent"),
            C(900702, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Sonic"),
            C(900703, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Physical"),
            C(900704, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Light"),
            C(900705, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Ice"),
            C(900706, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Fire"),
            C(900707, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist Energy"),
            C(900708, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Debuff Resist EMP"),
            C(900709, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Test Resist Thrax"),
            C(900710, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk II"),
            C(900711, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk III"),
            C(900712, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk IV"),
            C(900713, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk V"),
            C(900714, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk VI"),
            C(900715, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk VII"),
            C(900716, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk VIII"),
            C(900717, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Harmony Suit Mk IX"),
            C(900718, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk II"),
            C(900719, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk III"),
            C(900720, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk IV"),
            C(900721, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk V"),
            C(900722, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk VI"),
            C(900723, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk VII"),
            C(900724, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk VIII"),
            C(900725, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Wellspring Suit Mk IX"),
            C(900726, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk II"),
            C(900727, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk III"),
            C(900728, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk IV"),
            C(900729, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk V"),
            C(900730, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk VI"),
            C(900731, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk VII"),
            C(900732, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk VIII"),
            C(900733, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Immortal Suit Mk IX"),
            C(900734, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk II"),
            C(900735, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk III"),
            C(900736, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk IV"),
            C(900737, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk V"),
            C(900738, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk VI"),
            C(900739, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk VII"),
            C(900740, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk VIII"),
            C(900741, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Psyche Suit Mk IX"),
            C(900742, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk II"),
            C(900743, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk III"),
            C(900744, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk IV"),
            C(900745, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk V"),
            C(900746, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk VI"),
            C(900747, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk VII"),
            C(900748, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk VIII"),
            C(900749, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Concussion Suit Mk IX"),
            C(900750, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk II"),
            C(900751, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk III"),
            C(900752, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk IV"),
            C(900753, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk V"),
            C(900754, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk VI"),
            C(900755, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk VII"),
            C(900756, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk VIII"),
            C(900757, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Atlas Suit Mk IX"),
            C(900758, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk II"),
            C(900759, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk III"),
            C(900760, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk IV"),
            C(900761, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk V"),
            C(900762, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk VI"),
            C(900763, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk VII"),
            C(900764, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk VIII"),
            C(900765, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Longevity Suit Mk IX"),
            C(900766, 0, 1, 1274, 50211, 24579, 0, 0, 5, 0, 0, ""),
            C(900767, 0, 0, 1274, 0, 0, 0, 0, 105, 0, 0, ""),
            C(900768, 0, 1, 1274, 110771, 25734, 0, 0, 10, 0, 0, ""),
            C(900769, 0, 1, 1274, 42370, 20951, 0, 0, 15, 0, 0, ""),
            C(900770, 0, 2, 1274, 110788, 25751, 0, 0, 20, 0, 0, ""),
            C(900771, 0, 1, 1274, 42347, 20928, 0, 0, 25, 0, 0, ""),
            C(900772, 0, 3, 1274, 110805, 25768, 0, 0, 30, 0, 0, ""),
            C(900773, 0, 1, 1274, 42340, 20921, 0, 0, 35, 0, 0, ""),
            C(900774, 0, 4, 1274, 110822, 25787, 0, 0, 40, 0, 0, ""),
            C(900775, 0, 1, 1274, 42352, 20933, 0, 0, 45, 0, 0, ""),
            C(900776, 0, 1, 1274, 42353, 20934, 0, 0, 50, 0, 0, ""),
            C(900777, 0, 1, 1274, 42369, 20950, 0, 0, 55, 0, 0, ""),
            C(900778, 0, 1, 1274, 110414, 25304, 0, 0, 60, 0, 0, ""),
            C(900779, 0, 0, 1274, 0, 0, 0, 0, 65, 0, 0, ""),
            C(900780, 0, 1, 1274, 42348, 20929, 0, 0, 70, 0, 0, ""),
            C(900781, 0, 1, 1274, 42310, 20891, 0, 0, 75, 0, 0, ""),
            C(900782, 0, 1, 1274, 42349, 20930, 0, 0, 80, 0, 0, ""),
            C(900783, 0, 0, 1274, 0, 0, 0, 0, 85, 0, 0, ""),
            C(900784, 0, 2, 1274, 110447, 25304, 0, 0, 90, 0, 0, ""),
            C(900785, 0, 0, 1274, 0, 0, 0, 0, 95, 0, 0, ""),
            C(900786, 0, 1, 1274, 42320, 20901, 0, 0, 100, 0, 0, ""),
            C(900787, 0, 0, 1274, 0, 0, 0, 0, 110, 0, 0, ""),
            C(900788, 0, 0, 1274, 0, 0, 0, 0, 115, 0, 0, ""),
            C(900789, 0, 2, 1274, 110448, 25304, 0, 0, 120, 0, 0, ""),
            C(900790, 0, 0, 1274, 0, 0, 0, 0, 125, 0, 0, ""),
            C(900791, 0, 0, 1274, 0, 0, 0, 0, 130, 0, 0, ""),
            C(900792, 0, 0, 1274, 0, 0, 0, 0, 135, 0, 0, ""),
            C(900793, 0, 0, 1274, 0, 0, 0, 0, 140, 0, 0, ""),
            C(900794, 0, 0, 1274, 0, 0, 0, 0, 145, 0, 0, ""),
            C(900795, 0, 3, 1274, 110449, 25304, 0, 0, 150, 0, 0, ""),
            C(900796, 0, 0, 1274, 0, 0, 0, 0, 155, 0, 0, ""),
            C(900797, 0, 0, 1274, 0, 0, 0, 0, 160, 0, 0, ""),
            C(900798, 0, 0, 1274, 0, 0, 0, 0, 165, 0, 0, ""),
            C(900799, 0, 0, 1274, 0, 0, 0, 0, 170, 0, 0, ""),
            C(900800, 0, 0, 1274, 0, 0, 0, 0, 175, 0, 0, ""),
            C(900801, 0, 3, 1274, 110450, 25304, 0, 0, 180, 0, 0, ""),
            C(900802, 0, 0, 1274, 0, 0, 0, 0, 185, 0, 0, ""),
            C(900803, 0, 0, 1274, 0, 0, 0, 0, 190, 0, 0, ""),
            C(900804, 0, 0, 1274, 0, 0, 0, 0, 195, 0, 0, ""),
            C(900805, 0, 0, 1274, 0, 0, 0, 0, 200, 0, 0, ""),
            C(900806, 0, 0, 1274, 0, 0, 0, 0, 205, 0, 0, ""),
            C(900807, 0, 4, 1274, 110451, 25304, 0, 0, 210, 0, 0, ""),
            C(900808, 0, 0, 1274, 0, 0, 0, 0, 215, 0, 0, ""),
            C(900809, 0, 0, 1274, 0, 0, 0, 0, 220, 0, 0, ""),
            C(900810, 0, 0, 1274, 0, 0, 0, 0, 225, 0, 0, ""),
            C(900811, 0, 0, 1274, 0, 0, 0, 0, 230, 0, 0, ""),
            C(900812, 0, 0, 1274, 0, 0, 0, 0, 235, 0, 0, ""),
            C(900813, 0, 4, 1274, 110452, 25304, 0, 0, 240, 0, 0, ""),
            C(900814, 0, 0, 1274, 0, 0, 0, 0, 245, 0, 0, ""),
            C(900815, 0, 0, 1274, 0, 0, 0, 0, 250, 0, 0, ""),
            C(900816, 0, 0, 1274, 0, 0, 0, 0, 255, 0, 0, ""),
            C(900817, 0, 0, 1274, 0, 0, 0, 0, 260, 0, 0, ""),
            C(900818, 0, 0, 1274, 0, 0, 0, 0, 265, 0, 0, ""),
            C(900819, 0, 5, 1274, 110453, 25304, 0, 0, 270, 0, 0, ""),
            C(900820, 0, 0, 1274, 0, 0, 0, 0, 275, 0, 0, ""),
            C(900821, 0, 0, 1274, 0, 0, 0, 0, 280, 0, 0, ""),
            C(900822, 0, 0, 1274, 0, 0, 0, 0, 285, 0, 0, ""),
            C(900823, 0, 0, 1274, 0, 0, 0, 0, 290, 0, 0, ""),
            C(900824, 0, 0, 1274, 0, 0, 0, 0, 295, 0, 0, ""),
            C(900825, 0, 5, 1274, 110454, 25304, 0, 0, 300, 0, 0, ""),
            C(900826, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900827, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900828, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900829, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900830, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(900831, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Range Brawler Suit"),
            C(900832, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Range Marksman Suit"),
            C(900833, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Range Receptive Suit"),
            C(900834, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Stalwart Suit"),
            C(900835, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Skirmisher Suit"),
            C(900836, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: Savant Suit"),
            C(900859, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Set: AFS Shocktrooper Suit"),
            C(900860, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900861, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900862, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900863, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900864, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900865, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900866, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900867, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900868, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900869, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900870, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900871, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900872, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900873, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900874, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900875, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900876, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900877, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900878, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900879, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900880, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900881, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900882, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900883, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900884, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900885, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900886, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900887, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900888, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900889, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900890, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900891, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900892, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900893, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900894, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900895, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900896, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900897, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900898, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900899, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900900, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900901, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900902, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900903, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900904, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900905, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900906, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(ClassName)s"),
            C(900907, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s"),
            C(10000001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massive Armor Absorb Amount"),
            C(10000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massive Armor Absorb Percent"),
            C(10000003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Pierce Amount"),
            C(10000004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Pierce Percent"),
            C(10000005, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Bleed Amount"),
            C(10000006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Armor Bleed Percent"),
            C(10000007, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST CreatureFlag %(ClassName)s"),
            C(10000008, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST DD Proc %(ClassName)s"),
            C(10000009, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST AE Proc %(ClassName)s"),
            C(10000010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "TEST Defensive %(ClassName)s"),
            C(10000011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Elemental Boosted %(TemplateName)s"),
            C(10000012, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Boxing %(TemplateName)s"),
            C(20000001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "%(TemplateName)s")
        };

        /// <summary>(id, module, effect, flat, per level, doubling[, arg1]).</summary>
        public static readonly IReadOnlyList<ModuleEffectEntry> Effects = new[]
        {
            E(1, 100001, 336, 0, 0, 16), // Armor Module: Health Bonus [2]
            E(2, 100002, 336, 0, 0, 24), // Armor Module: Health Bonus [3]
            E(3, 100003, 336, 0, 0, 32), // Armor Module: Health Bonus [4]
            E(4, 100008, 338, 1, 0.2, 0), // Armor Module: Power Bonus [1]
            E(5, 100009, 338, 2, 0.4, 0), // Armor Module: Power Bonus [2]
            E(6, 100010, 338, 3, 0.6, 0), // Armor Module: Power Bonus [3]
            E(7, 100011, 338, 4, 0.8, 0), // Armor Module: Power Bonus [4]
            E(8, 100016, 340, 0, 0.2, 0), // Armor Module: Regen Bonus [1]
            E(9, 100017, 340, 0, 0.4, 0), // Armor Module: Regen Bonus [2]
            E(10, 100018, 340, 0, 0.6, 0), // Armor Module: Regen Bonus [3]
            E(11, 100019, 340, 0, 0.8, 0), // Armor Module: Regen Bonus [4]
            E(12, 100020, 413, 3, 0, 0, 1), // Armor Module: Resist Physical [1]
            E(13, 100021, 413, 6, 0, 0, 1), // Armor Module: Resist Physical [2]
            E(14, 100022, 413, 9, 0, 0, 1), // Armor Module: Resist Physical [3]
            E(15, 100023, 413, 12, 0, 0, 1), // Armor Module: Resist Physical [4]
            E(16, 100024, 413, 3, 0, 0, 7), // Armor Module: Resist Sonic [1]
            E(17, 100025, 413, 6, 0, 0, 7), // Armor Module: Resist Sonic [2]
            E(18, 100026, 413, 9, 0, 0, 7), // Armor Module: Resist Sonic [3]
            E(19, 100027, 413, 12, 0, 0, 7), // Armor Module: Resist Sonic [4]
            E(20, 100028, 413, 3, 0, 0, 6), // Armor Module: Resist Photonic [1]
            E(21, 100029, 413, 6, 0, 0, 6), // Armor Module: Resist Photonic [2]
            E(22, 100030, 413, 9, 0, 0, 6), // Armor Module: Resist Photonic [3]
            E(23, 100031, 413, 12, 0, 0, 6), // Armor Module: Resist Photonic [4]
            E(24, 100032, 413, 3, 0, 0, 13), // Armor Module: Resist Electric [1]
            E(25, 100033, 413, 6, 0, 0, 13), // Armor Module: Resist Electric [2]
            E(26, 100034, 413, 9, 0, 0, 13), // Armor Module: Resist Electric [3]
            E(27, 100035, 413, 12, 0, 0, 13), // Armor Module: Resist Electric [4]
            E(28, 100036, 413, 3, 0, 0, 2), // Armor Module: Resist Fire [1]
            E(29, 100037, 413, 6, 0, 0, 2), // Armor Module: Resist Fire [2]
            E(30, 100038, 413, 9, 0, 0, 2), // Armor Module: Resist Fire [3]
            E(31, 100039, 413, 12, 0, 0, 2), // Armor Module: Resist Fire [4]
            E(32, 100040, 413, 3, 0, 0, 3), // Armor Module: Resist Ice [1]
            E(33, 100041, 413, 6, 0, 0, 3), // Armor Module: Resist Ice [2]
            E(34, 100042, 413, 9, 0, 0, 3), // Armor Module: Resist Ice [3]
            E(35, 100043, 413, 12, 0, 0, 3), // Armor Module: Resist Ice [4]
            E(36, 100044, 413, 3, 0, 0, 4), // Armor Module: Resist Virulent [1]
            E(37, 100045, 413, 6, 0, 0, 4), // Armor Module: Resist Virulent [2]
            E(38, 100046, 413, 9, 0, 0, 4), // Armor Module: Resist Virulent [3]
            E(39, 100047, 413, 12, 0, 0, 4), // Armor Module: Resist Virulent [4]
            E(40, 100048, 413, 3, 0, 0, 5), // Armor Module: Resist EMP [1]
            E(41, 100049, 413, 6, 0, 0, 5), // Armor Module: Resist EMP [2]
            E(42, 100050, 413, 9, 0, 0, 5), // Armor Module: Resist EMP [3]
            E(43, 100051, 413, 12, 0, 0, 5), // Armor Module: Resist EMP [4]
            E(44, 100052, 151, -10, 0, 0), // Weapon Module: Threat Reduction [1]
            E(45, 100053, 151, -20, 0, 0), // Weapon Module: Threat Reduction [2]
            E(46, 100054, 151, -30, 0, 0), // Weapon Module: Threat Reduction [3]
            E(47, 100055, 151, -40, 0, 0), // Weapon Module: Threat Reduction [4]
            E(48, 100056, 166, 1, 0, 0), // Weapon Module: Crit Hit Bonus [1]
            E(49, 100057, 166, 2, 0, 0), // Weapon Module: Crit Hit Bonus [2]
            E(50, 100058, 166, 3, 0, 0), // Weapon Module: Crit Hit Bonus [3]
            E(51, 100059, 166, 4, 0, 0), // Weapon Module: Crit Hit Bonus [4]
            E(52, 100060, 330, 1, 0.05, 0), // Armor Module: Body Bonus [1]
            E(53, 100061, 330, 2, 0.1, 0), // Armor Module: Body Bonus [2]
            E(54, 100062, 330, 3, 0.15, 0), // Armor Module: Body Bonus [3]
            E(55, 100063, 330, 4, 0.2, 0), // Armor Module: Body Bonus [4]
            E(56, 100068, 332, 1, 0.05, 0), // Armor Module: Mind Bonus [1]
            E(57, 100069, 332, 2, 0.1, 0), // Armor Module: Mind Bonus [2]
            E(58, 100070, 332, 3, 0.15, 0), // Armor Module: Mind Bonus [3]
            E(59, 100071, 332, 4, 0.2, 0), // Armor Module: Mind Bonus [4]
            E(60, 100076, 334, 1, 0.05, 0), // Armor Module: Spirit Bonus [1]
            E(61, 100077, 334, 2, 0.1, 0), // Armor Module: Spirit Bonus [2]
            E(62, 100078, 334, 3, 0.15, 0), // Armor Module: Spirit Bonus [3]
            E(63, 100079, 334, 4, 0.2, 0), // Armor Module: Spirit Bonus [4]
            E(64, 100084, 336, 0, 0, 8), // Armor Module: Health Bonus [1]
            E(65, 900032, 330, 5, 0.25, 0), // Armor Module: Body Bonus [5]
            E(66, 900034, 336, 0, 0, 40), // Armor Module: Health Bonus [5]
            E(67, 900036, 332, 5, 0.25, 0), // Armor Module: Mind Bonus [5]
            E(68, 900038, 338, 5, 1, 0), // Armor Module: Power Bonus [5]
            E(69, 900040, 340, 0, 1, 0), // Armor Module: Regen Bonus [5]
            E(70, 900041, 334, 5, 0.25, 0), // Armor Module: Spirit Bonus [5]
            E(71, 900043, 413, 15, 0, 0, 13), // Armor Module: Resist Electric [5]
            E(72, 900044, 413, 15, 0, 0, 5), // Armor Module: Resist EMP [5]
            E(73, 900045, 413, 15, 0, 0, 2), // Armor Module: Resist Fire [5]
            E(74, 900046, 413, 15, 0, 0, 3), // Armor Module: Resist Ice [5]
            E(75, 900047, 413, 15, 0, 0, 7), // Armor Module: Resist Sonic [5]
            E(76, 900048, 413, 15, 0, 0, 1), // Armor Module: Resist Physical [5]
            E(77, 900049, 413, 15, 0, 0, 6), // Armor Module: Resist Photonic [5]
            E(78, 900050, 413, 15, 0, 0, 4), // Armor Module: Resist Virulent [5]
            E(79, 900051, 166, 5, 0, 0), // Weapon Module: Crit Hit Bonus [5]
            E(80, 900052, 151, -50, 0, 0), // Weapon Module: Threat Reduction [5]
            E(81, 900175, 10000052, 2, 0, 0), // Weapon Module: Armor Piercing [1]
            E(82, 900176, 10000052, 4, 0, 0), // Weapon Module: Armor Piercing [2]
            E(83, 900177, 10000052, 6, 0, 0), // Weapon Module: Armor Piercing [3]
            E(84, 900178, 10000052, 8, 0, 0), // Weapon Module: Armor Piercing [4]
            E(85, 900179, 10000052, 10, 0, 0), // Weapon Module: Armor Piercing [5]
            E(86, 900180, 10000049, 0, 0, 8), // Armor Module: Armor Absorption [1]
            E(87, 900181, 10000049, 0, 0, 16), // Armor Module: Armor Absorption [2]
            E(88, 900182, 10000049, 0, 0, 24), // Armor Module: Armor Absorption [3]
            E(89, 900183, 10000049, 0, 0, 32), // Armor Module: Armor Absorption [4]
            E(90, 900184, 10000049, 0, 0, 40), // Armor Module: Armor Absorption [5]
            E(91, 900190, 111, 0, 0, 1), // Armor Module: Health Regen Bonus [1]
            E(92, 900191, 111, 0, 0, 2), // Armor Module: Health Regen Bonus [2]
            E(93, 900192, 111, 0, 0, 3), // Armor Module: Health Regen Bonus [3]
            E(94, 900193, 111, 0, 0, 4), // Armor Module: Health Regen Bonus [4]
            E(95, 900194, 111, 0, 0, 5), // Armor Module: Health Regen Bonus [5]
            E(96, 900195, 111, 0, 0, 2), // Weapon Module: Health Regen Bonus [1]
            E(97, 900196, 111, 0, 0, 4), // Weapon Module: Health Regen Bonus [2]
            E(98, 900197, 111, 0, 0, 6), // Weapon Module: Health Regen Bonus [3]
            E(99, 900198, 111, 0, 0, 8), // Weapon Module: Health Regen Bonus [4]
            E(100, 900199, 111, 0, 0, 10), // Weapon Module: Health Regen Bonus [5]
            E(101, 900200, 110, 1, 0, 0), // Armor Module: Power Regen Bonus [1]
            E(102, 900201, 110, 2, 0, 0), // Armor Module: Power Regen Bonus [2]
            E(103, 900202, 110, 3, 0, 0), // Armor Module: Power Regen Bonus [3]
            E(104, 900203, 110, 4, 0, 0), // Armor Module: Power Regen Bonus [4]
            E(105, 900204, 110, 5, 0, 0), // Armor Module: Power Regen Bonus [5]
            E(106, 900205, 110, 2, 0, 0), // Weapon Module: Power Regen Bonus [1]
            E(107, 900206, 110, 4, 0, 0), // Weapon Module: Power Regen Bonus [2]
            E(108, 900207, 110, 6, 0, 0), // Weapon Module: Power Regen Bonus [3]
            E(109, 900208, 110, 8, 0, 0), // Weapon Module: Power Regen Bonus [4]
            E(110, 900209, 110, 10, 0, 0), // Weapon Module: Power Regen Bonus [5]
            E(111, 900210, 108, 0, 0, 1), // Armor Module: Armor Recharge [1]
            E(112, 900211, 108, 0, 0, 2), // Armor Module: Armor Recharge [2]
            E(113, 900212, 108, 0, 0, 3), // Armor Module: Armor Recharge [3]
            E(114, 900213, 108, 0, 0, 4), // Armor Module: Armor Recharge [4]
            E(115, 900214, 108, 0, 0, 5), // Armor Module: Armor Recharge [5]
            E(116, 900215, 108, 0, 0, 2), // Weapon Module: Armor Recharge [1]
            E(117, 900216, 108, 0, 0, 4), // Weapon Module: Armor Recharge [2]
            E(118, 900217, 108, 0, 0, 6), // Weapon Module: Armor Recharge [3]
            E(119, 900218, 108, 0, 0, 8), // Weapon Module: Armor Recharge [4]
            E(120, 900219, 108, 0, 0, 10), // Weapon Module: Armor Recharge [5]
            E(121, 900260, 323, 6, 0, 5), // Weapon Module: Steal Armor [1]
            E(122, 900261, 323, 6, 0, 10), // Weapon Module: Steal Armor [2]
            E(123, 900262, 323, 6, 0, 15), // Weapon Module: Steal Armor [3]
            E(124, 900263, 323, 6, 0, 20), // Weapon Module: Steal Armor [4]
            E(125, 900264, 323, 6, 0, 25), // Weapon Module: Steal Armor [5]
            E(126, 900265, 320, 0, 0, 2), // Weapon Module: Steal Health [1]
            E(127, 900266, 320, 0, 0, 4), // Weapon Module: Steal Health [2]
            E(128, 900267, 320, 0, 0, 6), // Weapon Module: Steal Health [3]
            E(129, 900268, 320, 0, 0, 8), // Weapon Module: Steal Health [4]
            E(130, 900269, 320, 0, 0, 10), // Weapon Module: Steal Health [5]
            E(131, 900270, 321, 0, 0.2, 0), // Weapon Module: Steal Power [1]
            E(132, 900271, 321, 0, 0.4, 0), // Weapon Module: Steal Power [2]
            E(133, 900272, 321, 0, 0.6, 0), // Weapon Module: Steal Power [3]
            E(134, 900273, 321, 0, 0.8, 0), // Weapon Module: Steal Power [4]
            E(135, 900274, 321, 0, 1, 0), // Weapon Module: Steal Power [5]
            E(136, 900275, 322, 10, 0, 0), // Weapon Module: Steal Adrenaline [1]
            E(137, 900276, 322, 20, 0, 0), // Weapon Module: Steal Adrenaline [2]
            E(138, 900277, 322, 30, 0, 0), // Weapon Module: Steal Adrenaline [3]
            E(139, 900278, 322, 40, 0, 0), // Weapon Module: Steal Adrenaline [4]
            E(140, 900279, 322, 50, 0, 0), // Weapon Module: Steal Adrenaline [5]
            E(141, 900291, 413, 6, 0, 0, 13), // Weapon Module: Resist Electric [1]
            E(142, 900292, 413, 12, 0, 0, 13), // Weapon Module: Resist Electric [2]
            E(143, 900293, 413, 18, 0, 0, 13), // Weapon Module: Resist Electric [3]
            E(144, 900294, 413, 24, 0, 0, 13), // Weapon Module: Resist Electric [4]
            E(145, 900295, 413, 30, 0, 0, 13), // Weapon Module: Resist Electric [5]
            E(146, 900296, 413, 6, 0, 0, 5), // Weapon Module: Resist EMP [1]
            E(147, 900297, 413, 12, 0, 0, 5), // Weapon Module: Resist EMP [2]
            E(148, 900298, 413, 18, 0, 0, 5), // Weapon Module: Resist EMP [3]
            E(149, 900299, 413, 24, 0, 0, 5), // Weapon Module: Resist EMP [4]
            E(150, 900300, 413, 30, 0, 0, 5), // Weapon Module: Resist EMP [5]
            E(151, 900301, 413, 6, 0, 0, 2), // Weapon Module: Resist Fire [1]
            E(152, 900302, 413, 12, 0, 0, 2), // Weapon Module: Resist Fire [2]
            E(153, 900303, 413, 18, 0, 0, 2), // Weapon Module: Resist Fire [3]
            E(154, 900304, 413, 24, 0, 0, 2), // Weapon Module: Resist Fire [4]
            E(155, 900305, 413, 30, 0, 0, 2), // Weapon Module: Resist Fire [5]
            E(156, 900306, 413, 6, 0, 0, 3), // Weapon Module: Resist Ice [1]
            E(157, 900307, 413, 12, 0, 0, 3), // Weapon Module: Resist Ice [2]
            E(158, 900308, 413, 18, 0, 0, 3), // Weapon Module: Resist Ice [3]
            E(159, 900309, 413, 24, 0, 0, 3), // Weapon Module: Resist Ice [4]
            E(160, 900310, 413, 30, 0, 0, 3), // Weapon Module: Resist Ice [5]
            E(161, 900311, 413, 6, 0, 0, 6), // Weapon Module: Resist Photonic [1]
            E(162, 900312, 413, 12, 0, 0, 6), // Weapon Module: Resist Photonic [2]
            E(163, 900313, 413, 18, 0, 0, 6), // Weapon Module: Resist Photonic [3]
            E(164, 900314, 413, 24, 0, 0, 6), // Weapon Module: Resist Photonic [4]
            E(165, 900315, 413, 30, 0, 0, 6), // Weapon Module: Resist Photonic [5]
            E(166, 900316, 413, 6, 0, 0, 1), // Weapon Module: Resist Physical [1]
            E(167, 900317, 413, 12, 0, 0, 1), // Weapon Module: Resist Physical [2]
            E(168, 900318, 413, 18, 0, 0, 1), // Weapon Module: Resist Physical [3]
            E(169, 900319, 413, 24, 0, 0, 1), // Weapon Module: Resist Physical [4]
            E(170, 900320, 413, 30, 0, 0, 1), // Weapon Module: Resist Physical [5]
            E(171, 900321, 413, 6, 0, 0, 7), // Weapon Module: Resist Sonic [1]
            E(172, 900322, 413, 12, 0, 0, 7), // Weapon Module: Resist Sonic [2]
            E(173, 900323, 413, 18, 0, 0, 7), // Weapon Module: Resist Sonic [3]
            E(174, 900324, 413, 24, 0, 0, 7), // Weapon Module: Resist Sonic [4]
            E(175, 900325, 413, 30, 0, 0, 7), // Weapon Module: Resist Sonic [5]
            E(176, 900326, 413, 6, 0, 0, 4), // Weapon Module: Resist Virulent [1]
            E(177, 900327, 413, 12, 0, 0, 4), // Weapon Module: Resist Virulent [2]
            E(178, 900328, 413, 18, 0, 0, 4), // Weapon Module: Resist Virulent [3]
            E(179, 900329, 413, 24, 0, 0, 4), // Weapon Module: Resist Virulent [4]
            E(180, 900330, 413, 30, 0, 0, 4), // Weapon Module: Resist Virulent [5]
            E(181, 900331, 340, 0, 0.5, 0), // Weapon Module: Regen Bonus [1]
            E(182, 900332, 340, 0, 1, 0), // Weapon Module: Regen Bonus [2]
            E(183, 900333, 340, 0, 1.5, 0), // Weapon Module: Regen Bonus [3]
            E(184, 900334, 340, 0, 2, 0), // Weapon Module: Regen Bonus [4]
            E(185, 900335, 340, 0, 2.5, 0), // Weapon Module: Regen Bonus [5]
            E(186, 900381, 413, 3, 0, 0, 20), // Armor Module: Resist Blinding [1]
            E(187, 900382, 413, 6, 0, 0, 20), // Armor Module: Resist Blinding [2]
            E(188, 900383, 413, 9, 0, 0, 20), // Armor Module: Resist Blinding [3]
            E(189, 900384, 413, 12, 0, 0, 20), // Armor Module: Resist Blinding [4]
            E(190, 900385, 413, 15, 0, 0, 20), // Armor Module: Resist Blinding [5]
            E(191, 900386, 413, 3, 0, 0, 8), // Armor Module: Resist Knockback [1]
            E(192, 900387, 413, 6, 0, 0, 8), // Armor Module: Resist Knockback [2]
            E(193, 900388, 413, 9, 0, 0, 8), // Armor Module: Resist Knockback [3]
            E(194, 900389, 413, 12, 0, 0, 8), // Armor Module: Resist Knockback [4]
            E(195, 900390, 413, 15, 0, 0, 8), // Armor Module: Resist Knockback [5]
            E(196, 900391, 413, 3, 0, 0, 9), // Armor Module: Resist Stun [1]
            E(197, 900392, 413, 6, 0, 0, 9), // Armor Module: Resist Stun [2]
            E(198, 900393, 413, 9, 0, 0, 9), // Armor Module: Resist Stun [3]
            E(199, 900394, 413, 12, 0, 0, 9), // Armor Module: Resist Stun [4]
            E(200, 900395, 413, 15, 0, 0, 9), // Armor Module: Resist Stun [5]
            E(201, 900396, 413, 3, 0, 0, 15), // Armor Module: Resist Root [1]
            E(202, 900397, 413, 6, 0, 0, 15), // Armor Module: Resist Root [2]
            E(203, 900398, 413, 9, 0, 0, 15), // Armor Module: Resist Root [3]
            E(204, 900399, 413, 12, 0, 0, 15), // Armor Module: Resist Root [4]
            E(205, 900400, 413, 15, 0, 0, 15), // Armor Module: Resist Root [5]
            E(206, 900401, 402, 1, 0, 0), // Armor Module: Experience Bonus [1]
            E(207, 900402, 402, 2, 0, 0), // Armor Module: Experience Bonus [2]
            E(208, 900403, 402, 3, 0, 0), // Armor Module: Experience Bonus [3]
            E(209, 900404, 402, 4, 0, 0), // Armor Module: Experience Bonus [4]
            E(210, 900405, 402, 5, 0, 0), // Armor Module: Experience Bonus [5]
            E(211, 900406, 413, 3, 0, 0, 14), // Armor Module: Resist Snare [1]
            E(212, 900407, 413, 6, 0, 0, 14), // Armor Module: Resist Snare [2]
            E(213, 900408, 413, 9, 0, 0, 14), // Armor Module: Resist Snare [3]
            E(214, 900409, 413, 12, 0, 0, 14), // Armor Module: Resist Snare [4]
            E(215, 900410, 413, 15, 0, 0, 14), // Armor Module: Resist Snare [5]
            E(216, 900411, 227, 1, 0, 0), // Armor Module: Move Speed Bonus [1]
            E(217, 900412, 227, 2, 0, 0), // Armor Module: Move Speed Bonus [2]
            E(218, 900413, 227, 3, 0, 0), // Armor Module: Move Speed Bonus [3]
            E(219, 900414, 227, 4, 0, 0), // Armor Module: Move Speed Bonus [4]
            E(220, 900415, 227, 5, 0, 0), // Armor Module: Move Speed Bonus [5]
            E(221, 900416, 10000052, 1, 0, 0), // Armor Module: Armor Piercing [1]
            E(222, 900417, 10000052, 2, 0, 0), // Armor Module: Armor Piercing [2]
            E(223, 900418, 10000052, 3, 0, 0), // Armor Module: Armor Piercing [3]
            E(224, 900419, 10000052, 4, 0, 0), // Armor Module: Armor Piercing [4]
            E(225, 900420, 10000052, 5, 0, 0), // Armor Module: Armor Piercing [5]
            E(226, 900448, 108, 0, 0, 2), // Tool Module: Armor Absorption [1]
            E(227, 900449, 108, 0, 0, 4), // Tool Module: Armor Absorption [2]
            E(228, 900450, 108, 0, 0, 6), // Tool Module: Armor Absorption [3]
            E(229, 900451, 108, 0, 0, 8), // Tool Module: Armor Absorption [4]
            E(230, 900452, 108, 0, 0, 10), // Tool Module: Armor Absorption [5]
            E(231, 900453, 340, 0, 0.5, 0), // Tool Module: Regen Bonus [1]
            E(232, 900454, 340, 0, 1, 0), // Tool Module: Regen Bonus [2]
            E(233, 900455, 340, 0, 1.5, 0), // Tool Module: Regen Bonus [3]
            E(234, 900456, 340, 0, 2, 0), // Tool Module: Regen Bonus [4]
            E(235, 900457, 340, 0, 2.5, 0), // Tool Module: Regen Bonus [5]
            E(236, 900458, 111, 0, 0, 2), // Tool Module: Health Regen Bonus [1]
            E(237, 900459, 111, 0, 0, 4), // Tool Module: Health Regen Bonus [2]
            E(238, 900460, 111, 0, 0, 6), // Tool Module: Health Regen Bonus [3]
            E(239, 900461, 111, 0, 0, 8), // Tool Module: Health Regen Bonus [4]
            E(240, 900462, 111, 0, 0, 10), // Tool Module: Health Regen Bonus [5]
            E(241, 900463, 110, 2, 0, 0), // Tool Module: Power Regen Bonus [1]
            E(242, 900464, 110, 4, 0, 0), // Tool Module: Power Regen Bonus [2]
            E(243, 900465, 110, 6, 0, 0), // Tool Module: Power Regen Bonus [3]
            E(244, 900466, 110, 8, 0, 0), // Tool Module: Power Regen Bonus [4]
            E(245, 900467, 110, 10, 0, 0), // Tool Module: Power Regen Bonus [5]
            E(246, 900468, 413, 6, 0, 0, 13), // Tool Module: Resist Electric [1]
            E(247, 900469, 413, 12, 0, 0, 13), // Tool Module: Resist Electric [2]
            E(248, 900470, 413, 18, 0, 0, 13), // Tool Module: Resist Electric [3]
            E(249, 900471, 413, 24, 0, 0, 13), // Tool Module: Resist Electric [4]
            E(250, 900472, 413, 30, 0, 0, 13), // Tool Module: Resist Electric [5]
            E(251, 900473, 413, 6, 0, 0, 5), // Tool Module: Resist EMP [1]
            E(252, 900474, 413, 12, 0, 0, 5), // Tool Module: Resist EMP [2]
            E(253, 900475, 413, 18, 0, 0, 5), // Tool Module: Resist EMP [3]
            E(254, 900476, 413, 24, 0, 0, 5), // Tool Module: Resist EMP [4]
            E(255, 900477, 413, 30, 0, 0, 5), // Tool Module: Resist EMP [5]
            E(256, 900478, 413, 6, 0, 0, 2), // Tool Module: Resist Fire [1]
            E(257, 900479, 413, 12, 0, 0, 2), // Tool Module: Resist Fire [2]
            E(258, 900480, 413, 18, 0, 0, 2), // Tool Module: Resist Fire [3]
            E(259, 900481, 413, 24, 0, 0, 2), // Tool Module: Resist Fire [4]
            E(260, 900482, 413, 30, 0, 0, 2), // Tool Module: Resist Fire [5]
            E(261, 900483, 413, 6, 0, 0, 3), // Tool Module: Resist Ice [1]
            E(262, 900484, 413, 12, 0, 0, 3), // Tool Module: Resist Ice [2]
            E(263, 900485, 413, 18, 0, 0, 3), // Tool Module: Resist Ice [3]
            E(264, 900486, 413, 24, 0, 0, 3), // Tool Module: Resist Ice [4]
            E(265, 900487, 413, 30, 0, 0, 3), // Tool Module: Resist Ice [5]
            E(266, 900488, 413, 6, 0, 0, 6), // Tool Module: Resist Photonic [1]
            E(267, 900489, 413, 12, 0, 0, 6), // Tool Module: Resist Photonic [2]
            E(268, 900490, 413, 18, 0, 0, 6), // Tool Module: Resist Photonic [3]
            E(269, 900491, 413, 24, 0, 0, 6), // Tool Module: Resist Photonic [4]
            E(270, 900492, 413, 30, 0, 0, 6), // Tool Module: Resist Photonic [5]
            E(271, 900493, 413, 6, 0, 0, 1), // Tool Module: Resist Physical [1]
            E(272, 900494, 413, 12, 0, 0, 1), // Tool Module: Resist Physical [2]
            E(273, 900495, 413, 18, 0, 0, 1), // Tool Module: Resist Physical [3]
            E(274, 900496, 413, 24, 0, 0, 1), // Tool Module: Resist Physical [4]
            E(275, 900497, 413, 30, 0, 0, 1), // Tool Module: Resist Physical [5]
            E(276, 900498, 413, 6, 0, 0, 7), // Tool Module: Resist Sonic [1]
            E(277, 900499, 413, 12, 0, 0, 7), // Tool Module: Resist Sonic [2]
            E(278, 900500, 413, 18, 0, 0, 7), // Tool Module: Resist Sonic [3]
            E(279, 900501, 413, 24, 0, 0, 7), // Tool Module: Resist Sonic [4]
            E(280, 900502, 413, 30, 0, 0, 7), // Tool Module: Resist Sonic [5]
            E(281, 900503, 413, 6, 0, 0, 4), // Tool Module: Resist Virulent [1]
            E(282, 900504, 413, 12, 0, 0, 4), // Tool Module: Resist Virulent [2]
            E(283, 900505, 413, 18, 0, 0, 4), // Tool Module: Resist Virulent [3]
            E(284, 900506, 413, 24, 0, 0, 4), // Tool Module: Resist Virulent [4]
            E(285, 900507, 413, 30, 0, 0, 4), // Tool Module: Resist Virulent [5]
            E(286, 900508, 151, -10, 0, 0), // Tool Module: Threat Reduction [1]
            E(287, 900509, 151, -20, 0, 0), // Tool Module: Threat Reduction [2]
            E(288, 900510, 151, -30, 0, 0), // Tool Module: Threat Reduction [3]
            E(289, 900511, 151, -40, 0, 0), // Tool Module: Threat Reduction [4]
            E(290, 900512, 151, -50, 0, 0), // Tool Module: Threat Reduction [5]
        };

        private static string Text(string value) => "'" + value.Replace("'", "''") + "'";

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "null";

        public static IEnumerable<string> InsertStatements =>
            Classes.Select(row =>
                    $"insert into {ModuleClassEntry.TableName} (id, variant_id, level, class_set_id, item_template_id, item_class_id, extract_cost, integrate_cost, salvage_gain, upgrade_cost, upgrade_module_id, comment) " +
                    $"values ({row.Id}, {row.VariantId}, {row.Level}, {row.ClassSetId}, {row.ItemTemplateId}, {row.ItemClassId}, {row.ExtractCost}, {row.IntegrateCost}, {row.SalvageGain}, {row.UpgradeCost}, {row.UpgradeModuleId}, {Text(row.Comment)});")
                .Concat(Effects.Select(row =>
                    $"insert into {ModuleEffectEntry.TableName} (id, module_id, effect_id, set_level, flat_value, linear_value, exp_value, arg1, arg2, arg3, arg4) " +
                    $"values ({row.Id}, {row.ModuleId}, {row.EffectId}, {row.SetLevel}, {Number(row.FlatValue)}, {Number(row.LinearValue)}, {Number(row.ExpValue)}, {Number(row.Arg1)}, {Number(row.Arg2)}, {Number(row.Arg3)}, {Number(row.Arg4)});"));
    }
}
