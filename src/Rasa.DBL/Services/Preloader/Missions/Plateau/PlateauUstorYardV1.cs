using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Ustor Yard (squad instance 1502, client map adv_foreas_valverde_plateau_ustoryard) for Field Lieutenant
    /// Rapp's three missions, Silent But Deadly (1187), Interception! (1189) and Monkey Wrench (1188): the door
    /// from the Plateau, Rapp, and the Bane the missions fight. The server had no door into the map, no Rapp
    /// and none of these Bane.
    ///
    /// Sourced: TaRapedia Field Lieutenant Rapp: human, female, AFS, level 30, "with a medical vendor
    /// immediately outside the east instance entrance" at (356, 168, -14); creaturenamelanguage 9162 "Field
    /// Lieutenant Rapp"; her missions' package 1303 (the client's mission giver). The client's Plateau map has
    /// the Ustor door frame ArchBaneIndustrialTunnelJambInstance (class 6973, as the Maligo door's) at (356, 432,
    /// -556), TaRapedia's Plateau map the Ustor Yard entrance at x 359; the Ustor map the same frame at its east
    /// entrance (412, 168, -8), the hospital (teleporter 584) and the two Field Medics at (356, 169, 1-5).
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - The door: map links <see cref="IntoUstor"/> (in the Plateau frame, the tunnel runs east-west on the
    ///    navmesh) and <see cref="OutOfUstor"/> (in the east frame), 6 m as the instance doors. Each sets the
    ///    player down 10-14 m outside the other's circle, facing away from the door (yaw atan2(-dx, -dz)). The
    ///    west exit to the Maligo bridge is not here (One Way Trip, 877).
    ///  - Rapp: class 3848 NPC_Human_Swapset_Female in Lieutenant Liu's outfit (creature 510091), at
    ///    TaRapedia's spot on the navmesh's ground.
    ///  - The Bane: Thrax Soldiers (class 20757) and Kael (4046), their attacks the server's Thrax Soldier
    ///    and Kael rows' (Pools/Marshes Thrax for level 30-33, the Plateau Kael's). Hit points and armor
    ///    between the server's level 30 (2652, 140) and 33 (3439, 155) Thrax: 2914/145 at level 31, 3177/150
    ///    at 32; the Kael 1.45 times the Thrax, as the Plateau's (2973 against a level 28 Thrax's 2190).
    ///     - <see cref="Security"/>: level 31, normal; the guards, the station patrol (1187) and the first
    ///       patrol (1189).
    ///     - <see cref="PatrolThrax"/> and <see cref="PatrolKael"/>: level 32 at twice the hit points; the
    ///       second patrol (1189), "significantly harder" (the user).
    ///     - <see cref="Officer"/>: the Bane Security Officer of Monkey Wrench, a boss (the user): level 31,
    ///       three times the hit points, the level 33 Thrax's attacks; named 7682 "Thrax Infantry Warrant
    ///       Officer" (the client has no name for him; a patch note says the Maligo one got a unique name
    ///       only later). He stands in the centre's console room (Yard Regulation and Control) with three
    ///       guards (<see cref="OfficerPool"/>, slot 2), respawn 600 s.
    ///
    /// Ids: creatures 750061-750065, pools 750061 (Rapp's, as her creature id) and 750067, map links 750068-750069,
    /// free before. Plain SQL, valid on SQLite and MySQL; down takes out only
    /// these rows.
    /// </summary>
    public static class PlateauUstorYardV1
    {
        public const uint MapContextId = 1502;
        public const uint Plateau = PlateauMissionDataV1.MapContextId;

        public const uint Rapp = 750061, RappPool = 750061, RappPackage = 1303, RappName = 9162, RappLevel = 30;
        public const uint FemaleClass = 3848;           // NPC_Human_Swapset_Female
        public const uint OutfitOf = 510091;            // Lieutenant Liu - Eir Crater

        public const uint ThraxClass = 20757, KaelClass = 4046;
        public const uint ThraxName = 237, KaelName = 7725, OfficerName = 7682;
        public const uint Security = 750062, Officer = 750063, PatrolThrax = 750064, PatrolKael = 750065;
        public const uint OfficerPool = 750067, Guards = 3, OfficerRespawnSeconds = 600;

        public const uint SecurityLevel = 31, SecurityHitPoints = 2914, SecurityArmor = 145;
        public const uint OfficerHitPoints = SecurityHitPoints * 3;
        public const uint PatrolLevel = 32, PatrolThraxHitPoints = 3177 * 2, PatrolThraxArmor = 150;
        public const uint PatrolKaelHitPoints = 4313 * 2, PatrolKaelArmor = 210;
        public const uint ThraxKick30 = 53137, ThraxWeapon30 = 53138;   // Thrax Soldier - Pools (level 30)
        public const uint ThraxKick33 = 53068, ThraxWeapon33 = 53069;   // Thrax Soldier - Marshes (level 33)
        public const uint KaelPound = 53135, KaelSmash = 53136, KaelStrike = 54011;   // Kael - Plateau

        public const uint IntoUstor = 750068, OutOfUstor = 750069;
        public const double DoorRadius = 6;

        /// <summary>
        /// Rapp by the hospital, 2 m east of the first guess (356, 169.4, -14): there upstream's Bane pool 531659 (radius
        /// 9.1 at 333.1, -6.5) came 15.0 m short of her, under SpawnPoolManager.SafeClearance (to-fix #7).
        /// </summary>
        public static readonly (double X, double Y, double Z) RappAt = (358, 169.41, -14);
        public static readonly (double X, double Y, double Z) OfficerAt = (64.4, 178.8, -12);

        /// <summary>The Plateau's Ustor frame and where Ustor's door sets a player down, facing east.</summary>
        public static readonly (double X, double Y, double Z) PlateauDoor = (356, 433.88, -556), PlateauArrival = (370, 433.85, -556);
        public const double PlateauArrivalYaw = -1.5708;

        /// <summary>Ustor's east frame and where the Plateau's door sets a player down, facing west to Rapp.</summary>
        public static readonly (double X, double Y, double Z) UstorDoor = (412, 169.96, -8), UstorArrival = (400, 170, -8);
        public const double UstorArrivalYaw = 1.5708;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Rapp}, 'Field Lieutenant Rapp - Ustor Yard', {FemaleClass}, 1, {RappLevel}, 1000, {RappName}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Rapp}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Rapp}, {RappPackage}, 'Field Lieutenant Rapp');";
            yield return Pool(RappPool, RappAt.X, RappAt.Y, RappAt.Z, MapContextId, Rapp, 1, 1, 20, 0);

            yield return Creature(Security, "Bane Security - Ustor Yard", ThraxClass, SecurityLevel, SecurityHitPoints, ThraxName, ThraxKick30, ThraxWeapon30);
            yield return Stat(Security, SecurityHitPoints, SecurityArmor);
            yield return Creature(Officer, "Bane Security Officer - Ustor Yard, Yard Regulation and Control", ThraxClass, SecurityLevel,
                OfficerHitPoints, OfficerName, ThraxKick33, ThraxWeapon33);
            yield return Stat(Officer, OfficerHitPoints, SecurityArmor);
            yield return Creature(PatrolThrax, "Bane Secondary Patrol Thrax - Ustor Yard", ThraxClass, PatrolLevel, PatrolThraxHitPoints, ThraxName,
                ThraxKick33, ThraxWeapon33);
            yield return Stat(PatrolThrax, PatrolThraxHitPoints, PatrolThraxArmor);
            yield return Creature(PatrolKael, "Bane Secondary Patrol Kael - Ustor Yard", KaelClass, PatrolLevel, PatrolKaelHitPoints, KaelName,
                KaelPound, KaelSmash, KaelStrike);
            yield return Stat(PatrolKael, PatrolKaelHitPoints, PatrolKaelArmor);

            yield return PoolSeconds(OfficerPool, OfficerAt.X, OfficerAt.Y, OfficerAt.Z, MapContextId, Officer, 1, 1, OfficerRespawnSeconds, 4);
            yield return $"UPDATE spawnpool SET creature_2_Id = {Security}, creature_2_min_count = {Guards}, creature_2_max_count = {Guards} " +
                         $"WHERE id = {OfficerPool};";

            yield return Link(IntoUstor, Plateau, PlateauDoor, MapContextId, UstorArrival, UstorArrivalYaw, "plateau -> plateau_ustoryard");
            yield return Link(OutOfUstor, MapContextId, UstorDoor, Plateau, PlateauArrival, PlateauArrivalYaw, "plateau_ustoryard -> plateau");
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM map_link WHERE id IN ({IntoUstor}, {OutOfUstor});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({RappPool}, {OfficerPool});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Security}, {Officer}, {PatrolThrax}, {PatrolKael});";
            yield return $"DELETE FROM npc_package WHERE id = {Rapp};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Rapp};";
            yield return $"DELETE FROM creature WHERE id IN ({Rapp}, {Security}, {Officer}, {PatrolThrax}, {PatrolKael});";
        }

        private static string Link(uint id, uint map, (double X, double Y, double Z) at, uint destination,
            (double X, double Y, double Z) arrival, double yaw, string comment) =>
            "INSERT INTO map_link (id, map_context_id, pos_x, pos_y, pos_z, radius, dest_map_context_id, dest_pos_x, dest_pos_y, " +
            "dest_pos_z, dest_rotation, kind, enabled, comment) " +
            $"VALUES ({id}, {map}, {F(at.X)}, {F(at.Y)}, {F(at.Z)}, {F(DoorRadius)}, {destination}, {F(arrival.X)}, {F(arrival.Y)}, " +
            $"{F(arrival.Z)}, {yaw.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}, 1, 1, '{comment}');";
    }
}
