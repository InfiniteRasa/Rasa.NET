using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// What a vendor is paid in, by its package (vendor.package_id): the client's own table,
    /// generated/client/vendordata.vendorpackages, which gives each package a vendor type, a
    /// currency item and a credit type (constant/credittype: FUND 1, PRESTIGE 2).
    ///
    /// The vendor window takes all three from there, not from the server: a package of credit
    /// type PRESTIGE shows its prices as prestige with the prestige icon, checks them against
    /// the player's prestige, and asks before every purchase (ConfirmPrestigePurchases). Those
    /// are the thirteen PRESTIGE_POINTS packages below - the "Prestige Vendor" and "Prestige
    /// Supply Vendor" NPCs among them - and a purchase at one is paid in prestige.
    ///
    /// A counter not paid in credits only sells: the window has no selling to it, no buying
    /// back and no repairs (vendorwindow.py, bVendingOnly).
    ///
    /// Not done: three packages are paid in an item rather than a credit type - 106 in care
    /// package vouchers (template 42410), 136 and 137 in the control points' assault and
    /// defence tokens (121935, 121936). Those still charge credits.
    /// </summary>
    public static class VendorPackages
    {
        /// <summary>The packages of credit type PRESTIGE: all of vendor type PRESTIGE_POINTS (9).</summary>
        public static readonly HashSet<uint> Prestige = new HashSet<uint>
        {
            138, 139, 144, 145, 146, 147, 148, 149, 150, 151, 158, 160, 10000001
        };

        /// <summary>What a purchase at a vendor of this package is paid in.</summary>
        public static CurencyType CurrencyOf(uint packageId) =>
            Prestige.Contains(packageId) ? CurencyType.Prestige : CurencyType.Credits;
    }
}
