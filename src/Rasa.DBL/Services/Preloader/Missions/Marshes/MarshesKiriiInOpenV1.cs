using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Forean Elder Kirii (891's receiver, 894's giver) in the Village Ruins in the open, seen from the way in from the village
    /// (tester, real client 2026-11-05: at MarshesKiriiInSightV1's (166, 55.01, -225) the client's walk to him stopped at
    /// (163.8, -227.8), at talk range, and from there and from (162-163, -226 to -227) it said "You cannot see your target
    /// from here" (pm 274); only from the east (170, -225) did he talk).
    ///
    /// Why: the client decides the talk's sight itself (converse.pyo CheckAction: world.InLineOfSight(actor.body, npc.body),
    /// tabula_rasa.exe 0x544590 -> 0x921c60 -> ClientCollisionManager 0x942e10). It tests five lines, from the player's capsule
    /// middle and top to Kirii's capsule middle, top and quarter height (MarshesClientFloorTests.ClientSees), and the rock pile
    /// (class 20812) of the .map reaches round his south-west side at those heights. MarshesKiriiInSightV1 had checked one
    /// line, eye 1.6 m to middle 1.5 m, from one spot.
    ///
    /// Now (ESTIMATE, client geometry probe with the client's rule, 2026-11-05): (166, 55.2, -220), 5 m north, on the server
    /// navmesh (55.20; the client floor 54.96) and on the navmesh's way in from the village centre (along z -221). Seen by
    /// the client's rule from every place 2, 3, 4 and 5 m round him that has a client floor (64 of 64), each with the
    /// middle-to-middle line clear too. 29 m from 891's nearest wave spawn (140, -206.8); MarshesQuestNpcsHoldV1.FightClearance
    /// is 22 m. His 891 marker moves with him.
    /// </summary>
    public static class MarshesKiriiInOpenV1
    {
        public static readonly (double X, double Y, double Z) Before = MarshesKiriiInSightV1.Now;
        public static readonly (double X, double Y, double Z) Now = (166, 55.2, -220);

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
