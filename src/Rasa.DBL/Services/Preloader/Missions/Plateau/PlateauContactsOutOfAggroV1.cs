using System.Collections.Generic;
using Rasa.Structures;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// To-fix #16: the contacts of the Ustor Yard (1502) and Maligo Base (1830) squad copies out of the copy's Bane aggro
    /// range. To-fix #7 put Rapp, the Downed Prisoner and Altera 15 m (SpawnPoolManager.SafeClearance) from the nearest
    /// Bane pool's ground, but a Bane notices a FRIENDLY NPC at 18 m (Creature.AggroRange), and a squad copy's pools do not
    /// come back: the playtest bot found Altera and Rapp killed, and their missions blocked.
    ///
    /// A pool's ground is its circle (centre and radius); a pool of radius 0 wanders 10 m around its spawn point
    /// (BehaviorManager.WanderZoneFallbackRadius). Each contact moves to the nearest walkable navmesh point on its floor,
    /// walked to from its old place, with 2 m of floor around it, that is at least <see cref="Clearance"/> (flat) from
    /// every hostile pool's ground of its map. Neither map has hostile patrols or NPC squads (spawnpool_patrol,
    /// npc_squad_spawn). Margin 7 m over the aggro range: ESTIMATE.
    ///  - Rapp (750061): (358, -14) to (369, -13), 11 m east: 27.4 m from Bane pool 531659 (radius 9.1 at 333.1, -6.5).
    ///  - The Downed Prisoner (750082): (155, 127) to (169, 106), 25 m south-east, down the ramp: 32.6 m from Bane pool
    ///    531631 (radius 40 at 100, 128.5).
    ///  - Altera (750083): (-131, 123) to (-139, 137), 16 m north-west: 23.5 m from Bane pool 531633 (radius 5 at
    ///    -111.6, 129.1) and 23.6 m from Overseer Kythura's ground (750089, -168, 120). Short of 25 m: that pool and Kythura
    ///    hold her on both sides, and no point within 45 m of her room has more (the nearest with 25 m is 62 m west, past
    ///    Kythura). 5.5 m over the aggro range: ESTIMATE.
    ///  - Information Spec. Neuman (750084) stays: 51 m from the nearest hostile ground (592253).
    /// The markers on Rapp (One Way Trip 877, objective 10) and on Altera (Coup 878, objectives 1 and 5) move with them.
    /// Plain SQL, the same on SQLite and MySQL (as PlateauWeaponsTraffickingMarkersV1). Down puts back the places of
    /// PlateauUstorYardV1 and PlateauMaligoBaseV1.
    /// </summary>
    public static class PlateauContactsOutOfAggroV1
    {
        public const double Clearance = 25;

        public static readonly (double X, double Y, double Z) RappAt = (369, 169.6, -13);
        public static readonly (double X, double Y, double Z) DownedPrisonerAt = (169, -92.08, 106);
        public static readonly (double X, double Y, double Z) AlteraAt = (-139, -96.78, 137);

        /// <summary>The markers on these NPCs: mission, indicator (above MissionIndicator.UnnamedFrom).</summary>
        public static readonly (uint Mission, uint Indicator)[] RappMarkers = { (PlateauOneWayTripV1.MissionId, 3) };
        public static readonly (uint Mission, uint Indicator)[] AlteraMarkers = { (PlateauCoupV1.MissionId, 1), (PlateauCoupV1.MissionId, 9) };

        public static IEnumerable<string> Up()
        {
            yield return Move(PlateauUstorYardV1.RappPool, RappAt);
            yield return Move(PlateauMaligoBaseV1.DownedPrisoner, DownedPrisonerAt);
            yield return Move(PlateauMaligoBaseV1.Altera, AlteraAt);
            foreach (var (mission, indicator) in RappMarkers)
                yield return Mark(mission, indicator, RappAt);
            foreach (var (mission, indicator) in AlteraMarkers)
                yield return Mark(mission, indicator, AlteraAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return Move(PlateauUstorYardV1.RappPool, PlateauUstorYardV1.RappAt);
            yield return Move(PlateauMaligoBaseV1.DownedPrisoner, PlateauMaligoBaseV1.DownedPrisonerAt);
            yield return Move(PlateauMaligoBaseV1.Altera, PlateauMaligoBaseV1.AlteraAt);
            foreach (var (mission, indicator) in RappMarkers)
                yield return Mark(mission, indicator, PlateauUstorYardV1.RappAt);
            foreach (var (mission, indicator) in AlteraMarkers)
                yield return Mark(mission, indicator, PlateauMaligoBaseV1.AlteraAt);
        }

        private static string Move(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} WHERE id = {pool};";

        private static string Mark(uint mission, uint indicator, (double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} WHERE mission_id = {mission} " +
            $"AND content_revision = '{PlateauMissionDataV1.Revision}' AND indicator_id = {MissionIndicator.UnnamedFrom + indicator};";
    }
}
