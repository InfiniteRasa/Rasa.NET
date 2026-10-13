namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Wilderness Bane mortars' launcher in their weapon slot (Wilderness_mortar_launchers).
    ///
    /// The four reconstructed mortars of WildernessWaveAPopulationV1 (630076..630079,
    /// Emplacement_Bane_Turret_Standard) were made with no creature_appearance at all, and
    /// Fix_creature_attack_animations gave them Weapon_Creature_Bane_Mortar_Launcher's attack,
    /// WEAPON_GROUNDTARGET 1, without the launcher. A creature with nothing in the weapon slot
    /// (13) is unarmed to the server and to the clients: it is never sent WeaponReady
    /// (CreatureWeaponDraw), and the client has no weapon to fire the shell from, so the shot
    /// landed with no shell, no blast and no recoil. The Bane Mortar and Bane Light Mortar of
    /// WreckCreatures and the control points' Bane turrets (ControlPointTurrets) carry it, colour 1;
    /// these get the same row.
    ///
    /// Plain SQL for both providers. Guarded: a creature that already has a weapon slot keeps it.
    /// creature_appearance has no key, so a second row for the slot would be a duplicate.
    /// </summary>
    public static class WildernessMortarLaunchers
    {
        public static readonly uint[] Mortars = { 630076, 630077, 630078, 630079 };
        public const uint WeaponSlot = 13;
        public const uint Launcher = ControlPointTurrets.BaneMortarLauncher;
        public const uint Color = 1;

        private static string Ids => string.Join(", ", Mortars);

        public static readonly string Up =
            "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
            $"SELECT c.id, {WeaponSlot}, {Launcher}, {Color} FROM creature c " +
            $"WHERE c.id IN ({Ids}) " +
            $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id AND a.slot_id = {WeaponSlot});";

        public static readonly string Down =
            "DELETE FROM creature_appearance " +
            $"WHERE id IN ({Ids}) AND slot_id = {WeaponSlot} AND Class_id = {Launcher};";

        /// <summary>The id the migration first had, 20261217000000, which Fithik_bites shares.</summary>
        public const string OldId = "20261217000000_Wilderness_mortar_launchers";

        /// <summary>The history row a World that ran it under OldId still has.</summary>
        public static readonly string ForgetOldId =
            $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{OldId}';";
    }
}
