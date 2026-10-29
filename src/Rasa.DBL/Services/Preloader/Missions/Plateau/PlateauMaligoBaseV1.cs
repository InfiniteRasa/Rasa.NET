using System.Collections.Generic;
using System.Globalization;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Maligo Base (squad instance 1830, client map adv_foreas_valverde_plateau_maligobasev3) and the Ustor
    /// Bridge for Showdown (876), Coup (878) and One Way Trip (877): the way from Ustor Yard's west exit onto
    /// the bridge, the teleporters between Maligo's levels, the AFS prisoners and the two Bane officers with
    /// the keypasses. The server had none of these.
    ///
    /// Sourced:
    ///  - Names (creaturenamelanguage): Downed Prisoner 8590, Squad Leader Altera 8591, Information Spec.
    ///    Neuman 8592, Demolitionist Maybauer 8593, Mind Controlled Soldier 8621, Lieutenant Homburg 10134,
    ///    Overseer Kythura 10258, Watchman Zyrutha 10295. Packages (objectiveconversation): Altera 980 (878),
    ///    Neuman 981 (877 6 and 11, 878 6), Maybauer 982 (877 16), Homburg 2220 (877 12).
    ///  - TaRapedia NPC pages: Downed Prisoner female, level 34; Squad Leader Alter male, level 45; Information
    ///    Spec. Neuman female, level 42, at the "Hospital". Ten Ton Hammer (Maligo upper level, 2008-03): the
    ///    prisoner lies on the ground up the ramp from the arrival room; Maybauer "pops in" when the powercube
    ///    is placed; Homburg teleports in after the bomb. Watchman Zyrutha (Level 1 keypass) stands behind the
    ///    first laser beams; Overseer Kythura (Level 3 keypass) south of the second beams.
    ///  - Client 876 text 4593: the keypass "provides entry to a local teleporter somewhere to the West" (Level 1).
    ///    Client 877 text 17350 (Homburg): "I've had my men rewire this local teleporter and you should be able to
    ///    return safely."
    ///  - Client Plateau map: the bridge ArchBaneOutpostBridgeV01 at (39, 422.5, -617) beside the Maligo door
    ///    frame (0, 422, -617); the server has waypoint 155 "Ustor Bridge" there. Client Ustor map: the west
    ///    exit frame ArchBaneIndustrialTunnelJambInstance at (-188, 159, 2).
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23):
    ///  - The navmesh has Level 1 (y -132, the entry) and Level 2 (y -100..-88) as two separate areas, and the
    ///    Special Projects chamber (y -104.3, x -277..-215) as a third. The 2008 guides' coordinates do not fit
    ///    this map (rebuilt later, "maligobasev3"): every place here is an estimate from the client map's rooms,
    ///    to be checked in the playtest.
    ///  - The keypass teleporter: a pair of local teleporters (<see cref="Level1Pad"/>, <see cref="Level2Pad"/>),
    ///    open to everybody. CHANGE LATER: in 2008 only a player with the Level 1 keypass (876) could use it; the
    ///    server has no local teleporter that checks an item or a mission.
    ///  - The chamber's way out: a third local teleporter (<see cref="ChamberPad"/>), one way to Level 2, always
    ///    there (Homburg's "rewired local teleporter", and a way out for a player whose mission failed in the
    ///    chamber). The way in is One Way Trip's terminal (PlateauOneWayTripV1).
    ///  - The Ustor west exit: map link <see cref="OntoBridge"/> to the bridge on the Plateau, and
    ///    <see cref="BackIntoUstor"/> from the bridge's east end back in. Open to everybody. CHANGE LATER: in 2008
    ///    the Ustor chain (Monkey Wrench) probably opened the west exit (ESTIMATE, from Rapp's text).
    ///  - The keypass officers: single named Thrax (class 20757) as Ustor Yard's officer, level 32/33 at three
    ///    times the hit points, respawn 600 s. Their keypasses are personal drops (the missions' item bindings).
    ///    CHANGE LATER: in 2008 the keypasses dropped for every player in Maligo (patch note 2008-02-26).
    ///  - The humans: Rapp's outfit (Lieutenant Liu's) for the women, Peterson's officer uniform for the men;
    ///    the Downed Prisoner lies down (pose 7). The mind-controlled prisoners: Clean Slate's Light Soldier model.
    ///
    /// Ids: creatures 750082-750089, pools 750082-750084 and 750088-750089 (as their creatures), map links
    /// 750090-750091, teleporters 900-902 (waypoint ids stay below 1000), free before. Plain SQL, valid on SQLite and MySQL; down takes
    /// out only these rows.
    /// </summary>
    public static class PlateauMaligoBaseV1
    {
        public const uint MapContextId = 1830;
        public const uint Plateau = PlateauMissionDataV1.MapContextId;
        public const uint Ustor = PlateauUstorYardV1.MapContextId;

        public const uint MaleClass = 3846;             // NPC_Human_Swapset_Male
        public const uint FemaleClass = PlateauUstorYardV1.FemaleClass;
        public const uint SoldierClass = PlateauPerduNpcsV1.SoldierClass;
        public const uint WomenOutfitOf = PlateauUstorYardV1.OutfitOf;

        public const uint DownedPrisoner = 750082, DownedPrisonerName = 8590, DownedPrisonerLevel = 34;
        public const uint Altera = 750083, AlteraName = 8591, AlteraPackage = 980, AlteraLevel = 45;
        public const uint Neuman = 750084, NeumanName = 8592, NeumanPackage = 981, NeumanLevel = 42;
        public const uint Maybauer = 750085, MaybauerName = 8593, MaybauerPackage = 982;
        public const uint Homburg = 750086, HomburgName = 10134, HomburgPackage = 2220;
        public const uint MindControlled = 750087, MindControlledName = 8621;
        public const uint Zyrutha = 750088, ZyruthaName = 10295, ZyruthaLevel = 32;
        public const uint Kythura = 750089, KythuraName = 10258, KythuraLevel = 33;
        // OfficerRespawnSeconds: then PlateauMaligoOfficerSquadRespawnV1, in the same migration, sets 60 s (to-fix #18).
        public const uint HumanLevel = 32, LyingDown = 7, OfficerRespawnSeconds = 600;

        public const uint ZyruthaHitPoints = 3177 * 3, ZyruthaArmor = 150;
        public const uint KythuraHitPoints = 3439 * 3, KythuraArmor = 155;

        public const uint OntoBridge = 750090, BackIntoUstor = 750091;
        public const double DoorRadius = PlateauUstorYardV1.DoorRadius;
        public const uint Level1Pad = 900, Level2Pad = 901, ChamberPad = 902;
        public const uint PadClass = 29648;             // ArchHumTeleporterV02, as the local teleporters at Ashoka
        public const uint LocalTeleporter = 1;          // teleporter.type

        // Level 2 (ESTIMATE places, on the navmesh).
        // The prisoner and Altera stand on safe ground (SpawnPoolManager.SafeClearance, to-fix #7): first guessed at
        // (130, -90.27, 127) and (-118, -100.28, 128), inside upstream's Bane pools 531631 (radius 40 at 100, 128.5) and
        // 531633 (radius 5 at -111.6, 129.1); moved to the nearest navmesh points on the same floor clear of every pool.
        public static readonly (double X, double Y, double Z) DownedPrisonerAt = (155, -90.28, 127);
        public static readonly (double X, double Y, double Z) AlteraAt = (-131, -100.28, 123);
        public static readonly (double X, double Y, double Z) NeumanAt = (-20, -90.28, 70);
        public static readonly (double X, double Y, double Z) KythuraAt = (-168, -90.2, 120);

        // Level 1: Zyrutha behind the laser beams west of the entry (client ArchBaneTripWireEmitter, x 108.5).
        public static readonly (double X, double Y, double Z) ZyruthaAt = (96, -132, 128);

        /// <summary>Ustor's west exit frame, and where the bridge's link sets a player down, facing east, away from the exit.</summary>
        public static readonly (double X, double Y, double Z) UstorWestDoor = (-188, 160.96, 2), UstorWestArrival = (-172, 161.05, 2);
        public const double UstorWestArrivalYaw = -1.5708;

        /// <summary>The bridge's east end (its link back into Ustor), and where Ustor's west exit sets a player down, facing west to the Maligo door.</summary>
        public static readonly (double X, double Y, double Z) BridgeEastDoor = (64, 424.6, -617), BridgeArrival = (52, 424.2, -617);
        public const double BridgeArrivalYaw = 1.5708;

        /// <summary>The pads: Level 1's west end, Level 2's control room (the massacre room), the chamber's east side.</summary>
        public static readonly (double X, double Y, double Z) Level1PadAt = (-214, -130.13, 128), Level2PadAt = (90, -100.08, 128),
            ChamberPadAt = (-222, -104.28, 140);

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Human(DownedPrisoner, "Downed Prisoner - Maligo Base", FemaleClass, DownedPrisonerName, DownedPrisonerLevel, null, DownedPrisonerAt))
                yield return sql;
            yield return $"INSERT INTO spawnpool_pose (id, pose) VALUES ({DownedPrisoner}, {LyingDown});";
            foreach (var sql in Human(Altera, "Squad Leader Altera - Maligo Base", MaleClass, AlteraName, AlteraLevel, AlteraPackage, AlteraAt))
                yield return sql;
            foreach (var sql in Human(Neuman, "Information Spec. Neuman - Maligo Base", FemaleClass, NeumanName, NeumanLevel, NeumanPackage, NeumanAt))
                yield return sql;
            foreach (var sql in Human(Maybauer, "Demolitionist Maybauer - Maligo Base (877 scene)", MaleClass, MaybauerName, HumanLevel, MaybauerPackage, null))
                yield return sql;
            foreach (var sql in Human(Homburg, "Lieutenant Homburg - Maligo Base (877 scene)", MaleClass, HomburgName, HumanLevel, HomburgPackage, null))
                yield return sql;

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({MindControlled}, 'Mind Controlled Soldier - Maligo Base (One Way Trip scene)', {SoldierClass}, 1, 30, 1000, " +
                         $"{MindControlledName}, 7, 2, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({MindControlled}, 13, 6271, 1);";

            yield return Creature(Zyrutha, "Watchman Zyrutha - Maligo Base, Level 1 keypass", PlateauUstorYardV1.ThraxClass, ZyruthaLevel,
                ZyruthaHitPoints, ZyruthaName, PlateauUstorYardV1.ThraxKick33, PlateauUstorYardV1.ThraxWeapon33);
            yield return Stat(Zyrutha, ZyruthaHitPoints, ZyruthaArmor);
            yield return Pool(Zyrutha, ZyruthaAt.X, ZyruthaAt.Y, ZyruthaAt.Z, MapContextId, Zyrutha, 1, 1, OfficerRespawnSeconds, 0);
            yield return Creature(Kythura, "Overseer Kythura - Maligo Base, Level 3 keypass", PlateauUstorYardV1.ThraxClass, KythuraLevel,
                KythuraHitPoints, KythuraName, PlateauUstorYardV1.ThraxKick33, PlateauUstorYardV1.ThraxWeapon33);
            yield return Stat(Kythura, KythuraHitPoints, KythuraArmor);
            yield return Pool(Kythura, KythuraAt.X, KythuraAt.Y, KythuraAt.Z, MapContextId, Kythura, 1, 1, OfficerRespawnSeconds, 0);

            yield return Link(OntoBridge, Ustor, UstorWestDoor, Plateau, BridgeArrival, BridgeArrivalYaw, "plateau_ustoryard west exit -> plateau ustor bridge");
            yield return Link(BackIntoUstor, Plateau, BridgeEastDoor, Ustor, UstorWestArrival, UstorWestArrivalYaw, "plateau ustor bridge -> plateau_ustoryard west exit");

            yield return Pad(Level1Pad, "Local Teleporter: Maligo Base Level 01 (keypass teleporter)", Level1PadAt, 3.1416);
            yield return Pad(Level2Pad, "Local Teleporter: Maligo Base Level 02", Level2PadAt, 1.5708);
            yield return Pad(ChamberPad, "Local Teleporter: Maligo Base Special Projects (out)", ChamberPadAt, -1.5708);
            yield return PadLink(Level1Pad, Level2Pad);
            yield return PadLink(Level2Pad, Level1Pad);
            yield return PadLink(ChamberPad, Level2Pad);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({Level1Pad}, {Level2Pad}, {ChamberPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({Level1Pad}, {Level2Pad}, {ChamberPad});";
            yield return $"DELETE FROM map_link WHERE id IN ({OntoBridge}, {BackIntoUstor});";
            yield return $"DELETE FROM spawnpool_pose WHERE id = {DownedPrisoner};";
            yield return $"DELETE FROM spawnpool WHERE id IN ({DownedPrisoner}, {Altera}, {Neuman}, {Zyrutha}, {Kythura});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Zyrutha}, {Kythura});";
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {DownedPrisoner} AND {Kythura};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {DownedPrisoner} AND {Kythura};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {DownedPrisoner} AND {Kythura};";
        }

        /// <summary>Peterson's officer uniform (slot, class, color), as PlateauPerduNpcsV1's men wear.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] MenOutfit =
        {
            (2, 4021, 4294934528),      // NPC_Clothing_Officer_1_Boots
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (14, 9781, 4278655809),     // AvatarSwap hair
            (15, 4023, 4294934528),     // NPC_Clothing_Officer_1_Torso
            (16, 4022, 4294934528),     // NPC_Clothing_Officer_1_Legs
            (17, 24008, 4286690539),    // AvatarSwap face, Caucasian
        };

        /// <summary>A friendly human; with a pool at <paramref name="at"/>, or a template for a scene with none.</summary>
        private static IEnumerable<string> Human(uint id, string comment, uint classId, uint nameId, uint level, uint? package,
            (double X, double Y, double Z)? at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {classId}, 1, {level}, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            if (classId == FemaleClass)
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {WomenOutfitOf};";
            else
                foreach (var (slot, cls, color) in MenOutfit)
                    yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, {slot}, {cls}, {color});";
            if (package.HasValue)
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            if (at.HasValue)
                yield return Pool(id, at.Value.X, at.Value.Y, at.Value.Z, MapContextId, id, 1, 1, 20, 0);
        }

        private static string Link(uint id, uint map, (double X, double Y, double Z) at, uint destination,
            (double X, double Y, double Z) arrival, double yaw, string comment) =>
            "INSERT INTO map_link (id, map_context_id, pos_x, pos_y, pos_z, radius, dest_map_context_id, dest_pos_x, dest_pos_y, " +
            "dest_pos_z, dest_rotation, kind, enabled, comment) " +
            $"VALUES ({id}, {map}, {F(at.X)}, {F(at.Y)}, {F(at.Z)}, {F(DoorRadius)}, {destination}, {F(arrival.X)}, {F(arrival.Y)}, " +
            $"{F(arrival.Z)}, {yaw.ToString("0.####", CultureInfo.InvariantCulture)}, 1, 1, '{comment}');";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at, double rotation) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporter}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, " +
            $"{rotation.ToString("0.####", CultureInfo.InvariantCulture)}, {MapContextId});";

        private static string PadLink(uint from, uint to) =>
            $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({from}, {to});";
    }
}
