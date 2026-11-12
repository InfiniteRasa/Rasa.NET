using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Forean Elder Kirii (891's receiver, 894's giver) in the Village Ruins seen from the west (review list, tester, real client
    /// 2026-11-04: at (165, 55.15, -228) he talked only from his east side; from (162, -226) the client said "You cannot see
    /// your target from here").
    ///
    /// Why: MarshesQuestNpcsHoldV1's place is at the east foot of a rock pile (class 20812 terra_foreas_val_rock_pile01_v01 at
    /// 154.5, 62, -237.25 in the instance's .map): its collision is 0.12 m over his place and stands between him and every
    /// place west and south-west of him, the side the player comes from (P'reo Das is west). The client files' line from a
    /// player's eye at (162, -226) to his middle meets the rock (the probe of MarshesClientFloorTests' sight check); from 7
    /// of 16 places 3 m round him the line is clear, all east of him.
    ///
    /// Now (ESTIMATE, client floor, sight and navmesh probe 2026-11-04): 3.2 m north-east, (166, 55.01, -225), on the navmesh
    /// (the client floor 54.93), clear of the rock: seen from (162, -226) and from 15 of 16 places 3 m round him. He stays
    /// MarshesQuestNpcsHoldV1.FightClearance (22 m) from 891's waves: 30.6 m from the nearest spawn and its way into the
    /// village. His 891 marker moves with him.
    /// </summary>
    public static class MarshesKiriiInSightV1
    {
        public static readonly (double X, double Y, double Z) Before = MarshesQuestNpcsHoldV1.KiriiAt;
        public static readonly (double X, double Y, double Z) Now = (166, 55.01, -225);

        private static readonly (uint Mission, uint Objective, uint Indicator, uint Pool) KiriiMarker =
            MarshesSafeGroundV1.Markers.Single(marker => marker.Pool == MarshesPreoDasVillageNpcsV1.Kirii);

        public static IEnumerable<string> Up()
        {
            yield return Place(Now);
            yield return Marker(Now);
        }

        public static IEnumerable<string> Down()
        {
            yield return Marker(Before);
            yield return Place(Before);
        }

        private static string Place((double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {MarshesPreoDasVillageNpcsV1.Kirii};");

        private static string Marker((double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {KiriiMarker.Mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {KiriiMarker.Objective} AND indicator_id = {KiriiMarker.Indicator};");
    }
}
