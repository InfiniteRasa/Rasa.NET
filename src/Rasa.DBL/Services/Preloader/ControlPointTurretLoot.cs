namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Wilderness Bane turret's ordinary loot (Bind_control_point_turret_loot).
    ///
    /// Wilderness_ordinary_loot_v1 requires a loot profile for every hostile, rewarding creature
    /// in a spawnpool or a control point's garrison on map 1220 (loot_map_policy
    /// require_profiles; OrdinaryLootCoverage), and the Game server does not start while one is
    /// missing. It binds the 36 that existed when it was written. Add_control_point_turrets puts
    /// the Bane turret 551001 on six Wilderness spots (pools 551122..551132, control point 4),
    /// and nothing bound it.
    ///
    /// It takes the Bane mortars' profile (wilderness_v1_mortar_5_9, creatures 630076..630079):
    /// the turret fires the same Weapon_Creature_Bane_Mortar_Launcher, and there is no turret
    /// profile. The statements are guarded: a database given the binding by hand is left as it
    /// is, and one without the profile or the turret is not touched. Plain SQL, valid on SQLite
    /// and MySQL alike.
    ///
    /// The other zones' Bane turrets (551002..551014) are on maps no policy requires yet.
    /// </summary>
    public static class ControlPointTurretLoot
    {
        public const uint Map = 1220;
        public const uint Creature = 551001;
        public const string Profile = "wilderness_v1_mortar_5_9";

        public static readonly string Up =
            "INSERT INTO creature_loot_binding (map_context_id, creature_id, profile_key) " +
            $"SELECT {Map}, {Creature}, p.profile_key FROM loot_profile p " +
            $"WHERE p.profile_key = '{Profile}' " +
            $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Creature}) " +
            $"AND EXISTS (SELECT 1 FROM map_info m WHERE m.map_context_id = {Map}) " +
            $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding b WHERE b.map_context_id = {Map} AND b.creature_id = {Creature});";

        public static readonly string Down =
            "DELETE FROM creature_loot_binding " +
            $"WHERE map_context_id = {Map} AND creature_id = {Creature} AND profile_key = '{Profile}';";
    }
}
