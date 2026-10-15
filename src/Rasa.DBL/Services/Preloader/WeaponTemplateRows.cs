using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The weapon templates that had no itemtemplate_weapon row (Weapon_template_rows).
    ///
    /// The seed has 2440 rows for the 30225 item templates, and 2683 templates of a weapon class
    /// (one of WeaponTemplateStats.Types: guns, launchers, staves, blades) were left without
    /// one - most of them the other templates of a class whose first template has its row, and
    /// the rest the retired ("RECYCLE_") classes, none of whose templates has one. A template
    /// without the row has no WeaponInfo on the server, so none is sent to the client
    /// (ItemManager.SendItemDataToClient). The client's weapon (weapon.py) has defaults for most
    /// of what WeaponInfo carries, but not its recoil or its reload time: the first shot reads
    /// the one (baseweaponattack.py, weapon.recoilAmount) and a reload the other
    /// (weaponreload.py, GetReloadTimeMs), and each raises. The Cryogenic Shotgun 49286
    /// (RECYCLE_Weapon_Avatar_Shotgun_Ice_12_to_to_16) is one.
    ///
    /// Each is given the row of a template that has one:
    ///  - another template of its own class (2300): the same weapon, row for row;
    ///  - else a template of a class with the same attack action, attack argument and animation
    ///    condition (374) - the same attack, so the same reach, timing, heat, tool type and
    ///    rounds a shot - with its melee (alt) damage worked out for its own class as
    ///    Retune_weapon_template_stats did: max_damage times its type's ratio.
    /// Ten have no such template and keep none: seven TEST_ classes and a DELETEME_ one, an NPC's
    /// injection gun, and a creature's spear (<see cref="WithoutDonor"/>). The Loper spear and
    /// the TEST_ Loper spear share an attack, but neither has a row to copy.
    ///
    /// Tools (the Snowball Launcher, the salvage and tissue tools and the rest) are not weapon
    /// types and are left as they are: they are used through ToolActionManager.
    ///
    /// A retired class is still what the client's own data has: the Cryogenic Shotgun's class
    /// has no ammunition class and no damage there, so it can be drawn and its tooltip read,
    /// but there is nothing to load it with and nothing it would deal.
    /// </summary>
    public static class WeaponTemplateRows
    {
        /// <summary>The weapon row the Bootcamp pistol (17131) is given outside the seed (BootcampMissionDataV1).</summary>
        public static readonly uint[] RowsOutsideTheSeed = { 17131 };

        public static readonly string[] Columns =
        {
            "aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type", "ae_radius", "recoil_amount",
            "reuse_override", "cool_rate", "heat_per_shot", "tool_type", "ammo_per_shot", "windup", "recovery", "refire",
            "`range`", "alt_max_damage", "alt_damage_type", "alt_range", "alt_ae_radius", "alt_ae_type", "attack_type"
        };

        /// <summary>A template given a row, the template whose row it copies, and whether that is one of its own class.</summary>
        public sealed record Target(uint Template, uint ItemClass, uint Donor, bool SameClass, double MeleeRatio);

        private static readonly Lazy<(IReadOnlyList<Target> Targets, IReadOnlyList<uint> WithoutDonor)> Worked = new(Work);

        public static IReadOnlyList<Target> Targets => Worked.Value.Targets;

        public static IReadOnlyList<uint> WithoutDonor => Worked.Value.WithoutDonor;

        private static (IReadOnlyList<Target>, IReadOnlyList<uint>) Work()
        {
            var types = WeaponTemplateStats.Types.ToDictionary(type => (type.AttackActionId, type.AnimCode), type => type.MeleeRatio);

            // weaponclass: id, weapon_template_id, attack_action_id, attack_action_arg_id, ... weapon_anim_condition_code (13).
            var classes = new WeaponClassPreloader().Rows().ToDictionary(
                row => Convert.ToUInt32(row[0]),
                row => (Action: Convert.ToUInt32(row[2]), Arg: Convert.ToUInt32(row[3]), Anim: Convert.ToInt32(row[13])));

            var links = new ItemTemplateItemClassPreloader().Rows()
                .Select(row => (Template: Convert.ToUInt32(row[0]), Class: Convert.ToUInt32(row[1])))
                .ToList();

            var seeded = new ItemTemplateWeaponPreloader().Rows().Select(row => Convert.ToUInt32(row[0]))
                .Concat(RowsOutsideTheSeed).ToHashSet();

            var withRow = links.Where(link => seeded.Contains(link.Template) && classes.ContainsKey(link.Class)).ToList();
            var byClass = withRow.GroupBy(link => link.Class).ToDictionary(group => group.Key, group => group.Min(link => link.Template));
            var byAttack = withRow.GroupBy(link => classes[link.Class]).ToDictionary(group => group.Key, group => group.Min(link => link.Template));

            var targets = new List<Target>();
            var withoutDonor = new List<uint>();

            foreach (var (template, itemClass) in links.OrderBy(link => link.Template))
            {
                if (seeded.Contains(template) || !classes.TryGetValue(itemClass, out var attack)
                    || !types.TryGetValue((attack.Action, attack.Anim), out var ratio))
                    continue;

                if (byClass.TryGetValue(itemClass, out var sibling))
                    targets.Add(new Target(template, itemClass, sibling, true, ratio));
                else if (byAttack.TryGetValue(attack, out var donor))
                    targets.Add(new Target(template, itemClass, donor, false, ratio));
                else
                    withoutDonor.Add(template);
            }

            return (targets, withoutDonor);
        }

        /// <summary>
        /// The statements, one each, as MigrationBuilder.Sql takes them: one insert per donor and kind.
        /// A template that has a row already is left out by the join against its own row - in the
        /// FROM clause, where MySQL allows the table an INSERT ... SELECT writes to, not a subquery.
        /// </summary>
        public static IEnumerable<string> Up()
        {
            var weapon = ItemTemplateWeaponEntry.TableName;

            foreach (var group in Targets.GroupBy(target => (target.Donor, target.SameClass, target.MeleeRatio)).OrderBy(group => group.Key.Donor))
            {
                var (donor, sameClass, ratio) = group.Key;
                var values = Columns.Select(column => column == "alt_max_damage" && !sameClass
                    ? $"round(k.max_damage * {ratio.ToString("0.###", CultureInfo.InvariantCulture)})"
                    : $"w.{column}");

                yield return $"insert into {weapon} (id, {string.Join(", ", Columns)}) " +
                    $"select c.itemTemplateId, {string.Join(", ", values)} " +
                    $"from {ItemTemplateItemClassEntry.TableName} c join {WeaponClassEntry.TableName} k on k.id = c.itemClassId " +
                    $"join {weapon} w on w.id = {donor} left join {weapon} x on x.id = c.itemTemplateId " +
                    $"where c.itemTemplateId in ({string.Join(", ", group.Select(target => target.Template))}) and x.id is null;";
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var chunk in Targets.Select(target => target.Template).Chunk(500))
                yield return $"delete from {ItemTemplateWeaponEntry.TableName} where id in ({string.Join(", ", chunk)});";
        }
    }
}
