using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Two Marshes talk targets moved where a player can reach and see them (to-fix rows 21 and 22, 2026-11-03; the tester in
    /// the real client).
    ///
    /// Conscript Kaav (1028's receiver, 1029's giver), row 21. Why: his place (440, 223.68, -590) is on a lower navmesh layer
    /// under the depot's chunnel floor: the walked-on top there is 224.65, a metre above him, and the chunnel couplers' walls
    /// (class 4206 arch_bane_industrial_chunnel_straight_02_coupler_16m, at 424/456, 222, -584) are 0.43 m from him (client
    /// map adv_foreas_valverde_marshes.map and the meshes' collision, read with Rasa.ClientData on 2026-11-03). In the client
    /// he stood half in a pipe, and a talk from 24 places round him gave "You cannot see your target from here" (pm 274).
    /// Now (ESTIMATE): (462, 223.68, -592), on the depot's open floor 14.5 m from the door's map link (41: outside its 6 m and
    /// the 5 m marker margin), no navmesh floor over him, at least 3.4 m from every wall of the map's statics (floors and
    /// ceilings left out), walked to from the door and from Retread City (navmesh probe), 51 m outside the nearest hostile
    /// pool's circle, 33 m from the nearest scene object (1878's crates). His 1028 marker moves with him.
    ///
    /// Lt. Kiang (1875 receiver; 1876, 1877, 1879 giver and receiver), row 22. Why: his place (-115, 230.88, -319.5) is the
    /// sentry tower's room, a navmesh island: the tower has no door or stair, and the upstream local teleporter 573 at its foot
    /// (-117.6, 217.3, -323.9) goes to 571 at Research Point, not up. The walk stopped 5.1 m from him. Retail had him in the
    /// tower room (Ten Ton Hammer's point -116, -319); the deviation is in NOTES-playtest.md (plan B there). Now (ESTIMATE):
    /// (-116.5, 216.88, -311) at the tower's foot, 8.6 m north of the room's point, 12.9 m from the pad (it takes a player
    /// within 2 m: DynamicObjectManager.LocalTeleporterWorker), at least 4 m from every wall, no floor over him, 8 m and more
    /// from the posted NPCs round the tower, walked to from Research Point and the Bane Assertion Camp (navmesh probe), 137 m
    /// outside the nearest hostile pool's circle. No mission marker stands at him (giver and receiver only).
    /// </summary>
    public static class MarshesNpcsInTheOpenV1
    {
        public static readonly (double X, double Y, double Z) KaavBefore = MarshesRegulatorNpcsV1.KaavAt;
        public static readonly (double X, double Y, double Z) KaavAt = (462, 223.68, -592);
        public static readonly (double X, double Y, double Z) KiangBefore = MarshesSentryTowerNpcsV1.KiangAt;
        public static readonly (double X, double Y, double Z) KiangAt = (-116.5, 216.88, -311);

        public static IEnumerable<string> Up()
        {
            yield return Place(MarshesRegulatorNpcsV1.Kaav, KaavAt);
            yield return KaavMarker(KaavAt);
            yield return Place(MarshesSentryTowerNpcsV1.Kiang, KiangAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return Place(MarshesSentryTowerNpcsV1.Kiang, KiangBefore);
            yield return KaavMarker(KaavBefore);
            yield return Place(MarshesRegulatorNpcsV1.Kaav, KaavBefore);
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {pool};");

        private static string KaavMarker((double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {MarshesRegulatorPartTwoV1.MissionId} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {MarshesRegulatorPartTwoV1.Tracker} AND indicator_id = 1000000001;");
    }
}
