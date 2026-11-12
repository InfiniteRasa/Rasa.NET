using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052): Samuel's three Retreads hold a wave against one player who kills a Thrax per 15 s (tester, real
    /// client 2026-11-05: the first Retread died 31 s after wave 2, with the depot's Thrax in the fight;
    /// MarshesTrappedClearOfDepotV1 takes those out of it).
    ///
    /// Why: with the spare (to-fix 29) each member of a wave goes for its own Retread (MarshesDefenceScene.Attackers: the
    /// members in turn on the living Retreads). A Retread (760343) had the escort's 4000 hit points; a Marshes Thrax does
    /// 147-220 a hit, a soldier every 1.3 s, a grenadier every 0.8 s (creature_action 53069, 53071): 4000 lasts 15-30 s
    /// under one Thrax. A player killing one per 15 s kills a three-Thrax wave's last member 45 s after it comes, so that
    /// member's Retread was down before it, and every Retread down sends the waves on, Samuel last.
    ///
    /// Now (ESTIMATE, no source gives the crew's strength): the Retreads twice the escort's, 8000 (as Head East's station
    /// soldiers, MarshesGuardStationHoldsV1): 30-60 s under one Thrax. Samuel's stand-in keeps his 12000. creature_stat and
    /// max_hp, both ways.
    /// </summary>
    public static class MarshesTrappedRetreadsHoldV1
    {
        public const uint RetreadHitPoints = 2 * MarshesRetreadCityNpcsV1.EscortHealth;

        public static void Up(MigrationBuilder migration) => Health(migration, RetreadHitPoints);

        public static void Down(MigrationBuilder migration) => Health(migration, MarshesRetreadCityNpcsV1.EscortHealth);

        private static void Health(MigrationBuilder migration, uint health)
        {
            migration.Sql($"UPDATE creature_stat SET health = {health} WHERE id = {MarshesRetreadCityNpcsV1.Retread};");
            migration.Sql($"UPDATE creature SET max_hp = {health} WHERE id = {MarshesRetreadCityNpcsV1.Retread};");
        }
    }
}
