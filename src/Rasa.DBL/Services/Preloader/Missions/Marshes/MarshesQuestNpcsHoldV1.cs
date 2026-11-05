using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes quest NPCs survive a fight beside them (to-fix row 17, 2026-11-03).
    ///
    /// Why: Forean Elder Kirii (891's receiver) had 100 hit points in play, and an area attack (ability 229) of one of 891's own
    /// wave Grenadiers killed him, so the turn-in failed (row 4 had the same with Dikah). A creature with no creature_stat row
    /// gets the server's default 100 hit points (CreatureManager; the creature row's max_hp is not read).
    ///  - Hit points: no engine flag keeps a public pool NPC alive (ActorGameplayPolicy.Invulnerable is for scene actors and
    ///    for actors in a private instance, MissionSceneDefinition.ActorPolicies, as upstream Bootcamp uses it; the Village Ruins
    ///    is a squad instance, and the others stand on the Marshes), so the upstream way is a stat row. Each friendly or neutral
    ///    Marshes pool NPC of ours without one (33, listed below) gets Warrior Dikah's (row 4: upstream Ranger Anjuhi's): body,
    ///    mind, spirit 15, 4500 hit points, armour 300; max_hp 4500 to match. Checked 2026-11-03 against the scene fighters
    ///    of all Marshes missions: in a fight's path stand Kirii, Qunle (891's waves, 16 m), Lieutenant Delaio and Lieutenant
    ///    Shaw (920's waves come through the eastern guard station), Karl and Foster (the Retread Arena's fighters, 23-31 m),
    ///    Copilot Ortega (664's waves) and the Forean Elder (894's He'kal Das guards); the others get the same row so that no
    ///    Marshes quest NPC is left at the default (ESTIMATE: the retail hit points are not known).
    ///  - Kirii's place: he stood 18 m from a wave spawn (140, -206.8) and 12-14 m from the waves' way to P'reo Das. Now on the
    ///    nearest walked-to ground (navmesh probe 2026-11-03, a complete path from the door) at least 22 m (the row 15 rule,
    ///    MarshesNpcsStandStillV1.ReachClearance + 2) from every hostile scene actor's spawn on the map, from each 891 spawn's
    ///    straight way to the village's centre and outside every hostile pool's circle by ReachClearance: (165, 55.15, -228),
    ///    27 m east of the 2200 place, 31.5 m from the nearest spawn and way (ESTIMATE). His 891 marker moves with him.
    /// </summary>
    public static class MarshesQuestNpcsHoldV1
    {
        public const uint HitPoints = 4500, Armor = 300;
        public const float FightClearance = MarshesNpcsStandStillV1.ReachClearance + 2;

        /// <summary>The creature, its max_hp before (the row's, not read by the server).</summary>
        public static readonly (uint Creature, uint OldMaxHp)[] Hold =
        {
            (MarshesPaludosNpcsV1.Siva, 1000), (MarshesPaludosNpcsV1.Veska, 1000), (MarshesFalconHoldNpcsV1.Horsh, 1000),
            (MarshesHarvestersNpcsV1.Brazos, 1000), (MarshesRegulatorNpcsV1.Wilson, 1000), (MarshesSentryTowerNpcsV1.McDonald, 1000),
            (MarshesSentryTowerNpcsV1.Kiang, 1000), (MarshesSentryTowerTwoNpcsV1.Thoung, 1000), (MarshesSentryTowerTwoNpcsV1.Brady, 1000),
            (MarshesSentryTowerTwoNpcsV1.Zui, 1000), (MarshesSentryTowerTwoNpcsV1.Dahn, 1000), (MarshesSentryTowerTwoNpcsV1.Nez, 1000),
            (MarshesStalkerWoodsNpcsV1.Jeska, 1000), (MarshesStalkerWoodsNpcsV1.Dudley, 1000), (MarshesStalkerWoodsNpcsV1.Duvall, 1000),
            (MarshesGunControlNpcsV1.Lou, 1000), (MarshesGunControlNpcsV1.Masai, 400), (MarshesEscortsNpcsV1.Belfast, 1000),
            (MarshesEscortsNpcsV1.Delaio, 1000), (MarshesEscortsNpcsV1.Urqhart, 1000), (MarshesRetreadCityNpcsV1.Figgins, 1000),
            (MarshesRetreadCityNpcsV1.Holloway, 1000), (MarshesRetreadArenaNpcsV1.Karl, 1000), (MarshesRetreadArenaNpcsV1.Foster, 1000),
            (MarshesEastAndNorthNpcsV1.Bargas, 1000), (MarshesEastAndNorthNpcsV1.Shaw, 1000), (MarshesLogosResearchNpcsV1.Jumna, 1000),
            (MarshesPreoDasNpcsV1.Qunle, 1000), (MarshesPreoDasNpcsV1.Elder, 1000), (MarshesPreoDasVillageNpcsV1.Tola, 1000),
            (MarshesPreoDasVillageNpcsV1.Kirii, 1000), (MarshesPreoDasVillageNpcsV1.Rakla, 1000), (MarshesBaneSupplyDepotNpcsV1.Ortega, 1000)
        };

        public static readonly (double X, double Y, double Z) KiriiBefore = MarshesNpcsStandStillV1.At(MarshesPreoDasVillageNpcsV1.Kirii);
        public static readonly (double X, double Y, double Z) KiriiAt = (165, 55.15, -228);

        private static readonly (uint Mission, uint Objective, uint Indicator, uint Pool) KiriiMarker =
            MarshesSafeGroundV1.Markers.Single(marker => marker.Pool == MarshesPreoDasVillageNpcsV1.Kirii);

        public static IEnumerable<string> Up()
        {
            foreach (var (creature, _) in Hold)
                yield return Stat(creature, HitPoints, Armor);
            yield return $"UPDATE creature SET max_hp = {HitPoints} WHERE id IN ({string.Join(", ", Hold.Select(npc => npc.Creature))});";
            yield return Place(MarshesPreoDasVillageNpcsV1.Kirii, KiriiAt);
            yield return Marker(KiriiAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return Marker(KiriiBefore);
            yield return Place(MarshesPreoDasVillageNpcsV1.Kirii, KiriiBefore);
            foreach (var group in Hold.GroupBy(npc => npc.OldMaxHp))
                yield return $"UPDATE creature SET max_hp = {group.Key} WHERE id IN ({string.Join(", ", group.Select(npc => npc.Creature))});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({string.Join(", ", Hold.Select(npc => npc.Creature))});";
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE spawnpool SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE id = {pool};");

        private static string Marker((double X, double Y, double Z) at) => System.FormattableString.Invariant(
            $"UPDATE mission_indicator SET pos_x = {at.X}, pos_y = {at.Y}, pos_z = {at.Z} WHERE mission_id = {KiriiMarker.Mission} AND content_revision = '{MarshesMissionDataV1.Revision}' AND objective_id = {KiriiMarker.Objective} AND indicator_id = {KiriiMarker.Indicator};");
    }
}
