namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Bane Hunters' shield in their weapon slot (Hunter_shields).
    ///
    /// A Hunter's weapon is its shield, Weapon_Creature_Hunter (10171), the class whose attack the
    /// Hunters fire (WEAPON_ATTACK 1/244). Only Bane Hunter Boss 81 carried it in the weapon slot
    /// (13); the other Hunters had no creature_appearance at all, so they were unarmed: never sent
    /// WeaponReady (CreatureWeaponDraw), no shield drawn, and no HUNTER_SHIELD to reflect with
    /// (Managers.HunterShield). These get the boss's row: (id, 13, 10171, 1).
    ///
    /// Plain SQL for both providers. Guarded: a Hunter that already has a weapon slot keeps it.
    /// creature_appearance has no key, so a second row for the slot would be a duplicate.
    /// </summary>
    public static class HunterShields
    {
        public static readonly uint[] Hunters =
        {
            41, 45, 49, 530007, 530009, 531028, 531051, 531054, 531061, 531062, 531067, 531095, 531096
        };

        public const uint WeaponSlot = 13;
        public const uint Shield = 10171;
        public const uint Color = 1;

        private static string Ids => string.Join(", ", Hunters);

        public static readonly string Up =
            "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
            $"SELECT c.id, {WeaponSlot}, {Shield}, {Color} FROM creature c " +
            $"WHERE c.id IN ({Ids}) " +
            $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id AND a.slot_id = {WeaponSlot});";

        /// <summary>The id the migration first had, 20261218000000, which Wilderness_mortar_launchers shares.</summary>
        public const string OldId = "20261218000000_Hunter_shields";

        /// <summary>The history row a World that ran it under OldId still has.</summary>
        public static readonly string ForgetOldId =
            $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{OldId}';";

        public static readonly string Down =
            "DELETE FROM creature_appearance " +
            $"WHERE id IN ({Ids}) AND slot_id = {WeaponSlot} AND Class_id = {Shield};";
    }
}
