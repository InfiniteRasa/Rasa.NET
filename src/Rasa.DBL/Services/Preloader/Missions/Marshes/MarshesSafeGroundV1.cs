using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes quest NPCs out of the hostile pools' reach (to-fix rows 1 and 4, 2026-11-03), and Warrior Dikah's stats.
    ///
    /// Why: SpawnPoolManager.ValidatePools reports a hostile pool whose circle comes within SafeClearance (15 m) of a friendly
    /// NPC's pool ("which should be safe ground"). Upstream has no flag that keeps a public spawn-pool quest giver alive (a
    /// mission's protected public actor is only a replacement's; ActorGameplayPolicy is for scene and instance actors), so the
    /// upstream way is the ground: each NPC moves to the nearest walked-to spot at least 16 m outside every hostile pool's
    /// circle on its map (navmesh probe 2026-11-03, a complete path from the old spot). In play a Maw of upstream pool 540037
    /// (radius 15, at -525.4, 222, -189.9) surfaced 2.5 m from Dikah and killed him with one hit.
    ///  - 760317 Warrior Dikah (1875 giver), Far West Watch: TaRapedia's /loc (-521, 222, -179), TTH (-523, 222, -176), 11.7 m
    ///    from 540037; now (-514, 222.52, -161), 19 m north-east of TaRapedia's (ESTIMATE).
    ///  - Village Ruins (1743), the Swamp Grubbers of upstream 540085 (radius 15) in P'reo Das (100.8, 57.8, -236.8) and the
    ///    Maws of 540088 (radius 15) at He'kal Das (135, 65.5, 3): Qunle 760373 (95, -230) to (82, 63.47, -212), Kirii 760377
    ///    (106, -242) to (132, 57.77, -235), Rakla 760378 (90, -244) to (75, 58.97, -254), the Forean Elder 760374 (130, -18)
    ///    to (127, 66.52, -27) (all ESTIMATE). The markers at Kirii (891), Rakla (892) and the elder (893) move with them.
    ///  - Dikah had no creature_stat row, so the server gave him its default 100 hit points (CreatureManager; the creature
    ///    row's max_hp is not read). Now the stats of upstream Ranger Anjuhi (98, the same NPC_Forean_Spearman class, friendly):
    ///    body, mind, spirit 15, 4500 hit points, armour 300; max_hp 4500 to match.
    /// </summary>
    public static class MarshesSafeGroundV1
    {
        public const uint DikahHitPoints = 4500, DikahArmor = 300, DikahOldMaxHp = 1000;

        /// <summary>Pool, the place before, the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (MarshesSentryTowerNpcsV1.Dikah, MarshesSentryTowerNpcsV1.DikahAt, (-514, 222.52, -161)),
            (MarshesPreoDasNpcsV1.Qunle, MarshesPreoDasNpcsV1.QunleAt, (82, 63.47, -212)),
            (MarshesPreoDasNpcsV1.Elder, MarshesPreoDasNpcsV1.ElderAt, (127, 66.52, -27)),
            (MarshesPreoDasVillageNpcsV1.Kirii, MarshesPreoDasVillageNpcsV1.KiriiAt, (132, 57.77, -235)),
            (MarshesPreoDasVillageNpcsV1.Rakla, MarshesPreoDasVillageNpcsV1.RaklaAt, (75, 58.97, -254))
        };

        /// <summary>The markers standing at a moved NPC: mission, objective, indicator, the NPC's pool.</summary>
        public static readonly (uint Mission, uint Objective, uint Indicator, uint Pool)[] Markers =
        {
            (MarshesNeedleInABaneCampV1.MissionId, 1, 10000003, MarshesPreoDasNpcsV1.Elder),
            (MarshesNeverSurrenderV1.MissionId, 1, 1000000002, MarshesPreoDasVillageNpcsV1.Kirii),
            (MarshesPickingUpThePiecesV1.MissionId, 1, 1000000003, MarshesPreoDasVillageNpcsV1.Rakla)
        };

        /// <summary>Where a moved NPC's pool stands now.</summary>
        public static (double X, double Y, double Z) At(uint pool) => Moves.Single(move => move.Pool == pool).Now;

        public static IEnumerable<string> Up()
        {
            yield return Stat(MarshesSentryTowerNpcsV1.Dikah, DikahHitPoints, DikahArmor);
            yield return $"UPDATE creature SET max_hp = {DikahHitPoints} WHERE id = {MarshesSentryTowerNpcsV1.Dikah};";
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
            foreach (var marker in Markers)
                yield return Marker(marker, At(marker.Pool));
        }

        /// <summary>
        /// The moved markers of one mission, as Up writes them (Marshes_playtest_fixes: a mission written again by its V1 class
        /// takes its moved marker back).
        /// </summary>
        public static IEnumerable<string> MarkersOf(uint mission) =>
            Markers.Where(marker => marker.Mission == mission).Select(marker => Marker(marker, At(marker.Pool)));

        public static IEnumerable<string> Down()
        {
            foreach (var marker in Markers)
                yield return Marker(marker, Moves.Single(move => move.Pool == marker.Pool).Before);
            foreach (var (pool, before, _) in Moves)
                yield return Place(pool, before);
            yield return $"UPDATE creature SET max_hp = {DikahOldMaxHp} WHERE id = {MarshesSentryTowerNpcsV1.Dikah};";
            yield return $"DELETE FROM creature_stat WHERE id = {MarshesSentryTowerNpcsV1.Dikah};";
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) => FormattableString(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {pool};");

        private static string Marker((uint Mission, uint Objective, uint Indicator, uint Pool) marker, (double X, double Y, double Z) at) =>
            FormattableString($"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {marker.Mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {marker.Objective} AND indicator_id = {marker.Indicator};");

        private static string FormattableString(System.FormattableString sql) => System.FormattableString.Invariant(sql);
    }
}
