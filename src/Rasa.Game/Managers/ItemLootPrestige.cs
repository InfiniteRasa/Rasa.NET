using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Prestige for taking a rare item off a creature's corpse. The client's help names the
    /// source ("by looting rare items"; the prestige tooltip's "Rare item acquirement off
    /// hostiles") and has the two messages for it (ReceivedItemLootPrestigePacket), for quality 4
    /// (RARE, shown as Experimental) and 5 (EPIC, shown as Prototype) and no other.
    ///
    /// The amounts are not in the client. They are the Tabula Rasa wiki's Prestige table: 17 for
    /// an Experimental item and 30 for a Prototype, the same whatever the looter has wagered. The
    /// same table agrees with the client where the client has a number: the kill streak's 1
    /// (MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS) and the wager's 20, 35 and 50 percent
    /// (quality.wageringPrestigeBonus).
    ///
    /// Ours:
    ///  - it is paid to whoever takes the item, once, when it leaves the corpse
    ///    (LootDispenserManager.Claim), in the write that gives them the item;
    ///  - a row of the corpse is one item, whatever its stack;
    ///  - nothing else that gives an item pays it: a mission's reward crate, a vendor, a trade,
    ///    the mail, the auction house, a recipe.
    /// </summary>
    public static class ItemLootPrestige
    {
        /// <summary>Prestige for looting an Experimental item (quality 4).</summary>
        public const int Experimental = 17;

        /// <summary>Prestige for looting a Prototype item (quality 5).</summary>
        public const int Prototype = 30;

        /// <summary>What looting an item of this quality is worth; 0 for a quality that gives none.</summary>
        public static int Amount(int qualityId)
        {
            return (LootQuality)qualityId switch
            {
                LootQuality.Rare => Experimental,
                LootQuality.Epic => Prototype,
                _ => 0
            };
        }

        /// <summary>
        /// What taking these rows off this dispenser is worth, row by row: nothing from a
        /// mission's reward object, which is not a corpse.
        /// </summary>
        internal static List<(LootItem Row, int QualityId, int Amount)> Awards(LootDispenser loot, IEnumerable<LootItem> items)
        {
            var awards = new List<(LootItem, int, int)>();

            if (loot == null || loot.AttachedObject != null || items == null)
                return awards;

            foreach (var row in items)
            {
                var qualityId = row?.Item?.ItemTemplate?.QualityId ?? 0;
                var amount = Amount(qualityId);

                if (amount > 0)
                    awards.Add((row, qualityId, amount));
            }

            return awards;
        }
    }
}
