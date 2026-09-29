using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Equipment condition: how it wears, what wear costs, and what a repair costs.
    ///
    /// The strategy guide (p18): "Weapons and armor degrade over time ... The more times you
    /// attack and the more hits you endure, the more your equipment's durability decreases ...
    /// Weapons deliver far less damage when their durability is low, and armor cannot absorb
    /// damage as efficiently. Equipment that fully bottoms out is only half as effective as its
    /// properly maintained counterpart."
    ///
    /// From the client (shared/gameconstants.py, written for the server that is missing):
    ///
    ///  - The effectiveness curve, DURABILITYMOD_*: full strength at 75% condition and above,
    ///    sliding linearly to half strength at 25%, and half strength below that.
    ///  - Broken at 0%. The client's BROKEN_EQUIPMENT tutorial: "You will no longer gain any
    ///    benefit from the equipment until it is repaired." It refuses to equip or reload an item
    ///    at 0 (equipable.py, weaponreload.py), paints it red in the weapon drawer, and has
    ///    WeaponBroken and ArmorBroken for the moment it happens. So "bottoms out" in the guide is
    ///    the 25% floor of the curve, and 0 is past it: no damage, no armour.
    ///  - Condition is the client's GetCondition, 100 * current / max in integer division.
    ///  - The repair price, vendorwindow._GetRepairPrice, "duplicated code from the server":
    ///    buyback x (100 - condition)% x REPAIR_GLOBAL_MODIFIER, at least 1.
    ///  - EQUIPMENT_DAMAGE_PER_DEATH -10: a death costs every equipped piece 10% of its maximum,
    ///    confirmed by a 2008 player write-up ("All of your equipment is damaged by 10%"). Kept
    ///    here for the death system, which does not exist yet.
    ///  - MIN_HEAT_FOR_DURABILITY_LOSS 500 (WeaponHeat.MinHeatForDurabilityLoss): a weapon
    ///    attack wears the weapon only while its barrel is at or above 500 of the 1000 it jams
    ///    at. Pacing yourself costs nothing; holding the trigger down does - the guide's
    ///    "especially for those who frequently engage in heavy combat".
    ///
    /// Not in the client, and chosen here: the pace. How much a shot or a hit took lived on the
    /// 2009 server. It is set so that <see cref="HoursToPenaltyLine"/> hours of heavy fighting
    /// take a piece from 100% to 75%, where the penalty starts - the guide's advice is to repair
    /// "when a weapon drops down to 80% or lower", so that is roughly one repair trip per session.
    /// Both rates are derived from the pace constants below, which are the ones to tune.
    ///
    /// Wear is a percentage of the item's own maximum, the same way the death penalty is, since
    /// maximums run from 100 (most weapons) to thousands (armour). A fraction of a hit point is
    /// carried on the item until it adds up to a whole one (<see cref="Item.WearCarry"/>). Only
    /// what is equipped wears - the guide's "unequip items to protect them from further wear" -
    /// and only weapon attacks wear a weapon: tools (the healing disc, the repair tool, harvest
    /// and cipher tools) do not.
    /// </summary>
    public static class Durability
    {
        /// <summary>DURABILITYMOD_MAX: at or above this condition an item is at full strength.</summary>
        public const int FullStrengthAt = 75;

        /// <summary>DURABILITYMOD_MAX_AMT.</summary>
        public const double FullStrength = 1.0;

        /// <summary>DURABILITYMOD_MIN: at or below this condition (and above 0) an item is at half strength.</summary>
        public const int HalfStrengthAt = 25;

        /// <summary>DURABILITYMOD_MIN_AMT.</summary>
        public const double HalfStrength = 0.5;

        /// <summary>REPAIR_GLOBAL_MODIFIER.</summary>
        public const double RepairGlobalModifier = 1.0;

        /// <summary>EQUIPMENT_DAMAGE_PER_DEATH, as a positive percentage of maximum: for the death system.</summary>
        public const double DeathWearPercent = 10.0;

        /// <summary>Ours: hours of heavy fighting that take a piece from 100% to <see cref="FullStrengthAt"/>.</summary>
        public const double HoursToPenaltyLine = 2.0;

        /// <summary>Ours: the share of heavy fighting spent with the trigger down - the rest is moving, reloading, looting.</summary>
        public const double HeavyFightingFiringShare = 0.5;

        /// <summary>
        /// Ours, and the one to revisit when the heat numbers are corrected: the share of that
        /// firing done with the barrel at or above MIN_HEAT_FOR_DURABILITY_LOSS, which is all of
        /// the firing that wears. It is a property of the heat numbers - how fast each weapon heats
        /// and cools against its rate of fire - and 0.5 is what a weapon gets that climbs steadily
        /// from cold to its jam and is cleared: the second half of every climb is above the line.
        ///
        /// At the heat numbers every weapon carries today (heat_per_shot 10, cool_rate 100 a
        /// second, placeholders) no weapon fires fast enough to reach 500, so no weapon wears yet.
        /// That is the heat rule doing what it says; the pace comes right when the heat does.
        /// </summary>
        public const double HotShareOfFiring = 0.5;

        /// <summary>Ours: hits taken a minute in heavy fighting - one every three seconds.</summary>
        public const double HeavyFightingHitsPerMinute = 20.0;

        /// <summary>
        /// Percent of maximum a weapon loses per second of hot firing - firing with the barrel at
        /// or above MIN_HEAT_FOR_DURABILITY_LOSS. A hot attack costs this times its own refire, so
        /// every weapon wears at the same rate per second held down hot whatever its rate of fire -
        /// a pistol's many small shots and a rocket launcher's few large ones alike. Attacks made
        /// below the line cost nothing.
        /// </summary>
        public static readonly double WeaponWearPercentPerHotSecond =
            (100.0 - FullStrengthAt) / (HoursToPenaltyLine * 3600.0 * HeavyFightingFiringShare * HotShareOfFiring);

        /// <summary>Percent of maximum each equipped piece of armour loses per hit its wearer takes.</summary>
        public static readonly double ArmorWearPercentPerHit =
            (100.0 - FullStrengthAt) / (HoursToPenaltyLine * 60.0 * HeavyFightingHitsPerMinute);

        /// <summary>
        /// Items with a maximum below this do not wear: a hit point would be too large a share of
        /// them. Weapons start at 100 and armour in the hundreds; the few classes below are test and
        /// placeholder items.
        /// </summary>
        public const int MinWearableHitPoints = 10;

        /// <summary>Equipment slots 1 to 20, the weapon's excepted - the armour UpdateStatsValues sums.</summary>
        private const int FirstArmorSlot = 1;
        private const int LastArmorSlot = 20;
        private const int WeaponSlot = 13;

        public static int MaxHitPointsOf(Item item)
        {
            if (item?.ItemTemplate == null)
                return 0;

            return EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class)?.ItemClassInfo?.MaxHitPoints ?? 0;
        }

        /// <summary>The client's GetCondition: 100 x current / maximum in whole percent, rounded down; 100 with no maximum.</summary>
        public static int Condition(int current, int max)
        {
            if (max <= 0)
                return 100;

            return (int)(100L * Math.Clamp(current, 0, max) / max);
        }

        public static int ConditionOf(Item item) => Condition(item?.CurrentHitPoints ?? 0, MaxHitPointsOf(item));

        /// <summary>Broken: an item with a maximum, at 0%. It gives nothing until repaired.</summary>
        public static bool IsBroken(Item item) => MaxHitPointsOf(item) > 0 && ConditionOf(item) == 0;

        /// <summary>What an item in this condition is worth, 0 to 1.</summary>
        public static double Effectiveness(int condition)
        {
            if (condition <= 0)
                return 0;

            if (condition <= HalfStrengthAt)
                return HalfStrength;

            if (condition >= FullStrengthAt)
                return FullStrength;

            return HalfStrength + (FullStrength - HalfStrength) * (condition - HalfStrengthAt) / (FullStrengthAt - HalfStrengthAt);
        }

        public static double EffectivenessOf(Item item) => Effectiveness(ConditionOf(item));

        /// <summary>A damage roll from this weapon, as its condition leaves it. Never less than 1 from a weapon that is not broken.</summary>
        public static int ScaleDamage(Item weapon, int damage)
        {
            var effectiveness = EffectivenessOf(weapon);

            if (effectiveness >= FullStrength || damage <= 0)
                return damage;

            return effectiveness <= 0 ? 0 : Math.Max(1, (int)Math.Round(damage * effectiveness));
        }

        /// <summary>
        /// What a vendor charges to bring this item back to full: the client's own formula, so the
        /// price the repair window shows is the price paid. The buyback price is the template's
        /// sell price, which is what ItemInfo sends the client as buyback.
        /// </summary>
        public static int RepairCost(Item item)
        {
            var buyback = Math.Max(item?.ItemTemplate?.SellPrice ?? 0, 0);
            var price = (int)(buyback * (100.0 - ConditionOf(item)) * 0.01);

            return Math.Max((int)(price * RepairGlobalModifier), 1);
        }

        /// <summary>
        /// A weapon attack has been made with this weapon, one whose next may come
        /// <paramref name="cycleMs"/> later, with its barrel at <paramref name="heat"/>. It wears
        /// only if the barrel is at or above MIN_HEAT_FOR_DURABILITY_LOSS.
        /// </summary>
        public static void WearWeapon(Client client, Item weapon, long cycleMs, double heat)
        {
            if (cycleMs <= 0 || heat < WeaponHeat.MinHeatForDurabilityLoss)
                return;

            Wear(client, weapon, WeaponWearPercentPerHotSecond * cycleMs / 1000.0, isArmor: false);
        }

        /// <summary>A player has taken a hit: each piece of armour they have on wears.</summary>
        public static void WearArmor(MapChannel mapChannel, Manifestation player)
        {
            var client = mapChannel?.ClientList.FirstOrDefault(c => c?.Player == player);
            var equipped = player?.Inventory?.EquippedInventory;

            if (client == null || equipped == null)
                return;

            for (var slot = FirstArmorSlot; slot <= LastArmorSlot && slot < equipped.Count; slot++)
            {
                if (slot == WeaponSlot || equipped[slot] == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(equipped[slot]);

                if (item?.ItemTemplate != null && EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class)?.ArmorClassInfo != null)
                    Wear(client, item, ArmorWearPercentPerHit, isArmor: true);
            }
        }

        /// <summary>
        /// Takes percentOfMax of the item's maximum off it, carrying the fraction. The client is
        /// told, and the database written, only when the whole percentage it shows changes - a
        /// hit point off a 3,600-point chest piece is not worth a packet and a write, and what is
        /// lost to a zone change before the next one is less than a percent, in the player's
        /// favour.
        /// </summary>
        private static void Wear(Client client, Item item, double percentOfMax, bool isArmor)
        {
            var max = MaxHitPointsOf(item);

            if (client == null || max < MinWearableHitPoints || percentOfMax <= 0)
                return;

            // A maximum lowered since the item was made can leave it above it.
            var current = Math.Min(item.CurrentHitPoints, max);

            if (current <= 0)
                return;

            item.WearCarry += max * percentOfMax / 100.0;

            var whole = (int)item.WearCarry;

            if (whole <= 0)
                return;

            item.WearCarry -= whole;

            var before = Condition(current, max);

            item.CurrentHitPoints = Math.Max(0, current - whole);

            var after = Condition(item.CurrentHitPoints, max);

            if (after == before)
                return;

            ItemManager.Instance.SendItemStatus(client, item, max);
            ItemManager.Instance.SaveHitPoints(item);

            if (after == 0)
                client.CallMethod(item.EntityId, isArmor ? (PythonPacket)new ArmorBrokenPacket() : new WeaponBrokenPacket());

            // The armour bar is summed from the pieces as their condition leaves them.
            if (isArmor && Effectiveness(after) != Effectiveness(before))
                ManifestationManager.Instance.RefreshStats(client.Player);
        }
    }
}
