using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The three Personal Waypoints for sale at every medical vendor.
    ///
    /// The client's mission text (Traitor on the Run): "you will need to equip yourself with a
    /// personal waypoint generator before you leave. You can find one for sale at any medical
    /// vendor." No vendor sold one: the item templates were in the data with their prices
    /// (1000, 2500 and 5000 credits) and nothing gave them out.
    ///
    /// A vendor is a medical vendor when its package is one the client's
    /// vendordata.vendorpackages types MEDICAL_SUPPLIES (3). In the seed data that is 182
    /// vendors: 181 on package 87 and one on 86.
    ///
    /// Only pairs a vendor does not already have are added. Down removes the three from the
    /// medical vendors; no vendor had them before.
    /// </summary>
    public static class PersonalWaypointStock
    {
        /// <summary>The client's MEDICAL_SUPPLIES vendor packages.</summary>
        public static readonly uint[] MedicalPackages =
        {
            84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 116, 159
        };

        /// <summary>One-Way Personal Waypoint, Two-Way Personal Waypoint and Two-Way Squad Waypoint, in the order they are listed at the counter.</summary>
        public static readonly uint[] Items = { 118781, 118782, 118783 };

        private static string Packages => string.Join(", ", MedicalPackages);

        public static readonly string[] Up =
        {
            $"insert into {VendorItemEntry.TableName} (id, item_template_id)"
            + $" select v.id, a.template_id from {VendorEntry.TableName} v cross join ("
            + string.Join(" union all ", Items.Select((template, n) => $"select {template} as template_id, {n} as n")) + ") a"
            + $" where v.package_id in ({Packages})"
            + $" and not exists (select 1 from {VendorItemEntry.TableName} i where i.id = v.id and i.item_template_id = a.template_id)"
            + " order by v.id, a.n;"
        };

        public static readonly string[] Down =
        {
            $"delete from {VendorItemEntry.TableName} where item_template_id in ({string.Join(", ", Items)})"
            + $" and id in (select id from {VendorEntry.TableName} where package_id in ({Packages}));"
        };
    }
}
