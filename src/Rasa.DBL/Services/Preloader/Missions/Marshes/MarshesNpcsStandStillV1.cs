using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes quest NPCs stand still and out of hostile reach (to-fix row 15, 2026-11-03).
    ///
    /// Why: Warrior Dikah was killed again at his Marshes_safe_ground place. His row (a copy of a Forean row) had run_speed 9
    /// and walk_speed 5, where upstream givers have 0 and 0 (Ranger Anjuhi, McDonald, Kiang and 146 more). A creature that can
    /// move is not one that never moves (BehaviorManager.NeverMoves), so it wanders up to 10 m round its pool
    /// (WanderZoneFallbackRadius), scans its AggroRange (18 m by default, Creature.AggroRange) as it goes and runs at a foe
    /// it sees. Maw pool 540037's circle was 16.1 m from his place, inside that reach (the Maws' own lurk scan is 18 m too).
    ///  - Speeds: the ten friendly or neutral Marshes pool NPCs that had 9 and 5 now have 0 and 0, as upstream givers: Shaman
    ///    Siva 760301, Warrior Veska 760302, Conscript Kaav 760314 (the Bane traitor, a contact), Dikah 760317, Archer Thoung
    ///    760320, Archer Zui 760322, Warrior Dahn 760323 and Elder Nez 760324 (posted along the sentry tower's patrol), Tola
    ///    760376, Rakla 760378. Unchanged: the followers (497, 888, 889 and 1023 escorts, 894's prisoners, 1033's mech, 542's
    ///    prisoner leader), Ranger Alia's beat (917), the scene fighters, bosses and waves, and the hostile pools.
    ///  - Places: each friendly Marshes pool NPC stands at least ReachClearance (AggroRange + 2 m) outside every hostile
    ///    automatic pool's circle on its map. Five were 16.0-16.3 m out and move 8-11 m to the nearest walked-to ground at
    ///    least 22 m out (ESTIMATE, navmesh probe 2026-11-03, a complete path from the old place): Dikah (from 540037), Qunle,
    ///    Kirii and Rakla (from Grubber pool 540085), the Forean Elder (from Maw pool 540088). Their markers move with them.
    ///    Trapped!'s crew moves too (MarshesTrappedV3).
    /// </summary>
    public static class MarshesNpcsStandStillV1
    {
        public const float AggroRange = 18, ReachClearance = AggroRange + 2;
        public const uint OldRunSpeed = 9, OldWalkSpeed = 5;

        public static readonly uint[] StandStill =
        {
            MarshesPaludosNpcsV1.Siva, MarshesPaludosNpcsV1.Veska, MarshesRegulatorNpcsV1.Kaav, MarshesSentryTowerNpcsV1.Dikah,
            MarshesSentryTowerTwoNpcsV1.Thoung, MarshesSentryTowerTwoNpcsV1.Zui, MarshesSentryTowerTwoNpcsV1.Dahn, MarshesSentryTowerTwoNpcsV1.Nez,
            MarshesPreoDasVillageNpcsV1.Tola, MarshesPreoDasVillageNpcsV1.Rakla
        };

        /// <summary>Pool, the place before (Marshes_safe_ground's), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (MarshesSentryTowerNpcsV1.Dikah, MarshesSafeGroundV1.At(MarshesSentryTowerNpcsV1.Dikah), (-514, 221.85, -153)),
            (MarshesPreoDasNpcsV1.Qunle, MarshesSafeGroundV1.At(MarshesPreoDasNpcsV1.Qunle), (82, 63.17, -204)),
            (MarshesPreoDasNpcsV1.Elder, MarshesSafeGroundV1.At(MarshesPreoDasNpcsV1.Elder), (127, 66.23, -35)),
            (MarshesPreoDasVillageNpcsV1.Kirii, MarshesSafeGroundV1.At(MarshesPreoDasVillageNpcsV1.Kirii), (138, 57.31, -224.6)),
            (MarshesPreoDasVillageNpcsV1.Rakla, MarshesSafeGroundV1.At(MarshesPreoDasVillageNpcsV1.Rakla), (67, 59.17, -254))
        };

        /// <summary>Where a moved NPC's pool stands now.</summary>
        public static (double X, double Y, double Z) At(uint pool) => Moves.Single(move => move.Pool == pool).Now;

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE creature SET run_speed = 0, walk_speed = 0 WHERE id IN ({string.Join(", ", StandStill)});";
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
            foreach (var marker in MarshesSafeGroundV1.Markers)
                yield return Marker(marker, At(marker.Pool));
        }

        public static IEnumerable<string> Down()
        {
            foreach (var marker in MarshesSafeGroundV1.Markers)
                yield return Marker(marker, Moves.Single(move => move.Pool == marker.Pool).Before);
            foreach (var (pool, before, _) in Moves)
                yield return Place(pool, before);
            yield return $"UPDATE creature SET run_speed = {OldRunSpeed}, walk_speed = {OldWalkSpeed} WHERE id IN ({string.Join(", ", StandStill)});";
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {pool};");

        private static string Marker((uint Mission, uint Objective, uint Indicator, uint Pool) marker, (double X, double Y, double Z) at) =>
            System.FormattableString.Invariant($"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {marker.Mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {marker.Objective} AND indicator_id = {marker.Indicator};");
    }
}
