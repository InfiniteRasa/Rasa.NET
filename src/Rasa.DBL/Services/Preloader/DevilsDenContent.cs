using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Devil's Den (adv_foreas_concordia_palisades_devilsden, 1394), entered from the
    /// Palisades, as TaRapedia's walkthrough tells it: its people, its Fithik, Miasma and Bane,
    /// its usable objects and its eight missions, in the client's own names and texts.
    ///
    /// Who and what:
    ///  - Rangers Kogari, Siri and Lurori at Hightower Outpost beside Ranger Cyrida, who give
    ///    Recover the Eloh Artifacts, Recovery of Hakari's Band and Recover Luroron's Band.
    ///    Cyrida gives Find Caritas; Field Lt. Brody gives Operation Devil's Strike, Missing
    ///    Strike Team and Sever the Devil's Head; Caritas, in the Bane Detention Center, gives
    ///    Finish the Task.
    ///  - Captain Fransisco in his alcove in the tunnel to the second chamber, and Caritas behind
    ///    the detention center's laser door.
    ///  - The walkthrough's creatures at its levels, 19 to 23: the 26 pools already in the
    ///    instance move to rows of their own at 19 to 21 (the Palisades keep theirs), and the
    ///    rest are placed: Fithik Guardians, Yzub in the first chamber and Kryell at the
    ///    tunnel's junction, the Hive Master at 21; Miasma and Egg-layers with Wickip past the
    ///    Hive Master; Kael Corporals and a Centurion with Rhamshaw in the Den, Sklorax and his
    ///    guards in the detention center; Overlord Gryl in Bane Mine Control; Stalkers,
    ///    Predators, Sentinels, Shield Drones, a Juggernaut and Harmox on the Shard Impact Site;
    ///    Overseer Qraal, the Field Commander, in the Bane Communications Tower.
    ///  - What is used and destroyed: Hakari's and Luroron's corpses, which give their bands;
    ///    five Eloh artifact stones; 50 Fithik egg clusters and eight eggs; four Bane teleporters
    ///    on the Den's platform; the crate of five detonators on the platform and the three
    ///    bombs they set (at the generator up the path, the laser door and the fuel depot); the
    ///    generator; the laser door and its console; the howitzer panels in the two bunkers; the
    ///    three Thrax Machinery Power Nodes, the three landing pad machines and the fuel depot.
    ///  - The Bane Communications Tower's region (3) given its rooms.
    /// What these do beyond their rows - Fransisco's company, the teleporters' Bane, the
    /// panels, the door, the bombs and the scenario events the missions wait for - is the
    /// server's, per copy of the Den (DevilsDen).
    ///
    /// Health and damage follow the Palisades' level 23 Bane down the levels: a level's thug is
    /// 1446 x 1.0905^(level - 23), a minion three quarters of it, a lieutenant one and three
    /// quarters, a boss 2.625 times; damage scales by the same factor, a lieutenant's by a
    /// quarter more and a boss's by half as much again.
    /// </summary>
    public static class DevilsDenContent
    {
        public const string Revision = "devils_den_1_16_5";
        public const uint Den = 1394, Palisades = 1244;

        /// <summary>missioncategorylanguage: "Instance (Devil's Den)".</summary>
        public const uint Category = 10000040;

        public const uint HakarisBand = 378, LuroronsBand = 379, FindCaritas = 380, ElohArtifacts = 381;
        public const uint DevilsStrike = 669, MissingStrikeTeam = 670, FinishTheTask = 671, SeverTheHead = 672;
        public static readonly uint[] Missions = { HakarisBand, LuroronsBand, FindCaritas, ElohArtifacts, DevilsStrike, MissingStrikeTeam, FinishTheTask, SeverTheHead };

        /// <summary>The existing givers: Field Lt. Brody and Ranger Cyrida, and their conversation packages.</summary>
        public const uint Brody = 510126, Cyrida = 510131, BrodyPackage = 145, CyridaPackage = 152;

        /// <summary>The conversation packages the missions' objectives are conversations with.</summary>
        public const uint KogariPackage = 44, FransiscoPackage = 550, CaritasPackage = 557;

        /// <summary>The scenarios whose events the Den records: 669's egg clusters, 672's machines.</summary>
        public const uint EggScenario = 1, NodeScenario = 2, PadScenario = 3, DepotScenario = 4;

        public const uint Kogari = 567001, Siri = 567002, Lurori = 567003, Fransisco = 567004, Caritas = 567005;
        public const uint JasperFithik = 567010, RoseFithikYoung = 567011, FithikGuardian = 567012, Yzub = 567013, Kryell = 567014, HiveMaster = 567015;
        public const uint Miasma = 567016, EggLayer = 567017, Wickip = 567018;
        public const uint ThraxSoldier = 567020, ThraxGrenadier = 567021, ThraxHunter = 567022, ThraxTechnician = 567023, Caretaker = 567024;
        public const uint Sklorax = 567025, Harmox = 567026, Gryl = 567027, Qraal = 567028;
        public const uint KaelCorporal = 567030, KaelCenturion = 567031, Rhamshaw = 567032;
        public const uint Nitroglazer = 567033, StalkerIV = 567034, StalkerV = 567035, Predator = 567036, Sentinel = 567037, Juggernaut = 567038, ShieldDrone = 567039, Howitzer = 567040;

        /// <summary>What the Bane teleporters send: the Den's soldiers and Kael.</summary>
        public static readonly uint[] TeleportedBane = { ThraxSoldier, ThraxGrenadier, KaelCorporal };

        /// <summary>The Palisades rows the Den's pools used, and the Den's own that take their place there.</summary>
        public static readonly (uint From, uint To)[] Repointed =
        {
            (531068, JasperFithik), (531063, ThraxSoldier), (531064, ThraxGrenadier), (531067, ThraxHunter),
            (531065, ThraxTechnician), (592003, Howitzer), (520038, HiveMaster)
        };

        public const uint HakariBandClass = 7070, LuroronBandClass = 7071, ArtifactClass = 7084, DetonatorClass = 21870;
        public const uint HakariBandTemplate = 459, LuroronBandTemplate = 460, ArtifactTemplate = 463, DetonatorTemplate = 44858;

        public const uint HakariCorpseClass = 7074, LuroronCorpseClass = 7075, ArtifactStoneClass = 11093, ConsoleClass = 7468, BombClass = 10122;
        public const uint DetonatorCrateClass = 7894;
        public const uint EggClusterClass = 10180, EggClass = 20758, MachineryClass = 10287, FuelDepotClass = 12981;
        public const uint LaserDoorClass = 9572, GeneratorClass = 3857, BaneTeleporterClass = 10603;

        // Rows: pools 567201.., destructibles 567300.., usables 567400.., the crate 567500.
        public const uint FirstPool = 567201;
        public const uint FirstEggCluster = 567300, FirstEgg = 567350, FirstNode = 567360, FirstPad = 567363, FuelDepot = 567366;
        public const uint LaserDoor = 567370, GeneratorRow = 567371, FirstTeleporter = 567380;
        public const uint HakariCorpse = 567400, LuroronCorpse = 567401, FirstArtifact = 567402;
        public const uint DoorConsole = 567410, WestPanel = 567411, EastPanel = 567412;
        public const uint GeneratorBomb = 567420, DoorBomb = 567421, DepotBomb = 567422;
        public const uint DetonatorCrate = 567500;

        /// <summary>The map's Bane Communications Tower region.</summary>
        public const uint CommTowerRegion = 3;

        /// <summary>missionobjectiveindicatorlanguage: Bane Field Commander, Bane Fuel Depot, Bane Landing Pad.</summary>
        public const uint FieldCommanderMarker = 58, FuelDepotMarker = 56, LandingPadMarker = 210;

        /// <summary>The Palisades' way into the Den (map_link 20) and the Den's side of it.</summary>
        public static readonly Vector3 PalisadesEntrance = new(159.5127f, 63.3979f, -581.128f);
        public static readonly Vector3 Entrance = new(16.7733f, 122.6238f, -398.1859f);

        public static readonly Vector3 KogariAt = new(-42.5f, 154.46f, 50.5f);
        public static readonly Vector3 SiriAt = new(-48.5f, 154.25f, 50.5f);
        public static readonly Vector3 LuroriAt = new(-45.87f, 153.65f, 43.57f);
        public static readonly Vector3 CyridaAt = new(-45.5f, 153.82f, 47.6f);
        public static readonly Vector3 BrodyAt = new(-546.9f, 188.9f, 37.1f);
        public static readonly Vector3 HiveMasterAt = new(-80.3f, 169.95f, 327.5f);
        public static readonly Vector3 HatcheryAt = new(-20f, 169f, 318f);
        public static readonly Vector3 MortarWest = new(218.1f, 155.6f, 586f), MortarEast = new(250f, 155.6f, 585.8f);

        // Where things are, on the Den's navmesh (x, height, z): the walkthrough's coordinates
        // where it gives them, its rooms where it names them.
        public static readonly Vector3 FransiscoAt = new(-59.7f, 95.5f, 49.0f);
        public static readonly Vector3 CaritasAt = new(78.7f, 105.62f, 213.5f);
        public static readonly Vector3 YzubAt = new(20.0f, 97.33f, -310.0f);
        public static readonly Vector3 GuardianAAt = new(48.0f, 78.31f, -100.0f);
        public static readonly Vector3 GuardianBAt = new(56.8f, 97.82f, -29.6f);
        public static readonly Vector3 KryellAt = new(70.0f, 96.47f, -30.0f);
        public static readonly Vector3 GuardianHiveAt = new(-40.0f, 168.23f, 330.0f);
        public static readonly Vector3 RhamshawAt = new(-15.0f, 77.32f, 205.0f);
        public static readonly Vector3 KaelAAt = new(-5.0f, 83.66f, 180.0f);
        public static readonly Vector3 KaelBAt = new(0.0f, 75.45f, 225.0f);
        public static readonly Vector3 KaelCAt = new(-45.0f, 84.64f, 195.0f);
        public static readonly Vector3 CenturionAt = new(-30.0f, 76.41f, 212.0f);
        public static readonly Vector3 NitroAAt = new(-110.0f, 138.74f, 165.0f);
        public static readonly Vector3 NitroBAt = new(-20.0f, 75.16f, 225.0f);
        public static readonly Vector3 CaretakerDenAt = new(-36.24f, 78.51f, 201.0f);
        public static readonly Vector3 SkloraxAt = new(72.0f, 105.7f, 226.0f);
        public static readonly Vector3 DetentionGuardsAt = new(66.0f, 105.66f, 229.0f);
        public static readonly Vector3 MiasmaAAt = new(-118.35f, 187.92f, 414.2f);
        public static readonly Vector3 MiasmaBAt = new(-95.0f, 186.99f, 415.0f);
        public static readonly Vector3 EggLayerAAt = new(-84.64f, 187.81f, 404.2f);
        public static readonly Vector3 EggLayerBAt = new(-90.0f, 187.85f, 385.0f);
        public static readonly Vector3 WickipAt = new(-100.0f, 187.72f, 420.0f);
        public static readonly Vector3 GrylAt = new(-87.4f, 182.46f, 501.4f);
        public static readonly Vector3 MineGuardsAt = new(-70.06f, 182.86f, 490.09f);
        public static readonly Vector3 StalkerIVaAt = new(101.8f, 151.97f, 471.8f);
        public static readonly Vector3 StalkerIVbAt = new(140.0f, 140.58f, 520.0f);
        public static readonly Vector3 StalkerVAt = new(200.0f, 137.68f, 500.0f);
        public static readonly Vector3 PredatorAAt = new(175.55f, 141.62f, 530.83f);
        public static readonly Vector3 PredatorBAt = new(300.0f, 129.42f, 500.0f);
        public static readonly Vector3 SentinelAAt = new(120.0f, 149.16f, 500.0f);
        public static readonly Vector3 SentinelBAt = new(160.0f, 136.38f, 480.0f);
        public static readonly Vector3 SentinelCAt = new(230.0f, 137.82f, 520.0f);
        public static readonly Vector3 JuggernautAt = new(330.0f, 129.22f, 445.0f);
        public static readonly Vector3 DroneAAt = new(310.0f, 129.23f, 470.0f);
        public static readonly Vector3 DroneBAt = new(241.6f, 141.82f, 576.4f);
        public static readonly Vector3 HarmoxAt = new(345.0f, 129.22f, 480.0f);
        public static readonly Vector3 QraalAt = new(165.0f, 142.66f, 700.0f);
        public static readonly Vector3 TowerGuardsAt = new(172.0f, 141.82f, 690.0f);
        public static readonly Vector3 Tele1At = new(-48.4f, 93.02f, 221.2f);
        public static readonly Vector3 Tele2At = new(-48.8f, 93.02f, 242.0f);
        public static readonly Vector3 Tele3At = new(-38.0f, 93.2f, 245.2f);
        public static readonly Vector3 Tele4At = new(-37.42f, 93.22f, 218.77f);
        public static readonly Vector3 GeneratorAt = new(-118.0f, 135.72f, 160.0f);
        public static readonly Vector3 GenBombAt = new(-113.0f, 137.72f, 156.0f);
        public static readonly Vector3 DoorAt = new(80.0f, 105.81f, 219.5f);
        public static readonly Vector3 DoorBombAt = new(72.0f, 105.68f, 224.0f);
        public static readonly Vector3 ConsoleAt = new(80.0f, 105.65f, 240.0f);
        public static readonly Vector3 NodeAAt = new(198.0f, 151.02f, 644.0f);
        public static readonly Vector3 NodeBAt = new(208.0f, 151.22f, 654.0f);
        public static readonly Vector3 NodeCAt = new(122.0f, 149.18f, 498.0f);
        public static readonly Vector3 PadAAt = new(250.0f, 136.67f, 548.0f);
        public static readonly Vector3 PadBAt = new(262.0f, 137.16f, 548.0f);
        public static readonly Vector3 PadCAt = new(256.0f, 136.42f, 540.0f);
        public static readonly Vector3 FuelAt = new(296.0f, 129.37f, 470.0f);
        public static readonly Vector3 PanelWAt = new(200.0f, 150.82f, 590.0f);
        public static readonly Vector3 PanelEAt = new(270.0f, 150.82f, 590.0f);
        public static readonly Vector3 HakariAt = new(45.0f, 80.7f, -147.1f);
        public static readonly Vector3 LuroronAt = new(-56.8f, 94.57f, 38.1f);
        public static readonly Vector3 Art1At = new(-92.0f, 172.09f, 328.0f);
        public static readonly Vector3 Art2At = new(-95.0f, 185.1f, 496.4f);
        public static readonly Vector3 Art3At = new(-60.0f, 185.3f, 505.0f);
        public static readonly Vector3 Art4At = new(185.0f, 141.77f, 690.0f);
        public static readonly Vector3 Art5At = new(185.0f, 141.82f, 712.0f);
        public static readonly Vector3 CrateAt = new(-62.0f, 93.02f, 230.8f);
        public static readonly Vector3[] EggClusters =
        {
            new(-13.92f, 168.41f, 340.05f), new(2.62f, 168.71f, 316.6f), new(-20.81f, 168.77f, 330.21f), new(-13.21f, 168.18f, 324.58f),
            new(-55.58f, 168.86f, 315.77f), new(19.71f, 168.92f, 317.23f), new(24.74f, 169.15f, 288.72f), new(-3.93f, 168.29f, 325.63f),
            new(-61.96f, 168.22f, 325.28f), new(-38.24f, 170.14f, 344.54f), new(2.55f, 168.66f, 343.99f), new(-32.91f, 169.29f, 320.85f),
            new(20.58f, 169.36f, 301.9f), new(9.46f, 168.61f, 338.15f), new(-46.14f, 169.48f, 319.46f), new(-2.78f, 168.9f, 334.81f),
            new(-50.29f, 168.99f, 349.25f), new(5.2f, 169.7f, 301.18f), new(-10.66f, 169.81f, 309.03f), new(18.61f, 169.48f, 326.63f),
            new(27.47f, 168.97f, 308.31f), new(-72.77f, 169.02f, 321.02f), new(-21.9f, 169.44f, 315.24f), new(9.49f, 168.3f, 324.36f),
            new(-65.33f, 170.58f, 310.55f), new(-79.54f, 169.78f, 314.17f), new(-10.71f, 168.73f, 348.85f), new(-47.64f, 169.85f, 335.21f),
            new(-63.76f, 169.95f, 335.72f), new(28.48f, 168.51f, 296.98f), new(-42.14f, 94.2f, 39.38f), new(-31.02f, 100.62f, 71.9f),
            new(-37.29f, 109.78f, 101.63f), new(-32.82f, 96.52f, 60.5f), new(-64.67f, 95.26f, 31.94f), new(-34.03f, 96.55f, 29.69f),
            new(-25.99f, 94.78f, 23.09f), new(-27.85f, 96.65f, 38.22f), new(-33.49f, 106.55f, 91.9f), new(-25.42f, 94.93f, 55.96f),
            new(-28.69f, 105.52f, 81.33f), new(-29.13f, 108.54f, 99.73f), new(-40.12f, 75.68f, 210.08f), new(3.73f, 77.33f, 233.43f),
            new(-22.4f, 78.67f, 209.17f), new(8.13f, 80.82f, 194.03f), new(-31.31f, 83.65f, 189.4f), new(-8.5f, 75.22f, 220.95f),
            new(-20.51f, 83.57f, 189.37f), new(2.4f, 79.4f, 200.2f),
        };
        public static readonly Vector3[] Eggs =
        {
            new(14.96f, 97.76f, -335.77f), new(2.79f, 97.0f, -307.88f), new(52.39f, 95.41f, -336.34f), new(32.01f, 97.05f, -305.87f),
            new(29.52f, 94.38f, -264.13f), new(-3.72f, 96.31f, -277.04f), new(35.47f, 96.11f, -290.47f), new(14.04f, 102.6f, -356.87f),
        };

        public static readonly Act[] Actions =
        {
            new(567101, "Captain Fransisco rocket launcher 141/1", 141, 1, 1.0, 40.0, 2500, 0, 30, 45, 0),
            new(567102, "Jasper Fithik 405/1", 405, 1, 1.0, 30.0, 15000, 1200, 59, 75, 1),
            new(567103, "Jasper Fithik 201/1", 201, 1, 0.5, 5.0, 10000, 0, 45, 75, 1),
            new(567104, "Jasper Fithik 180/1", 180, 1, 0.0, 0.0, 0, 4333, 223, 267, 1),
            new(567105, "Jasper Fithik 174/34", 174, 34, 0.5, 3.0, 800, 0, 44, 66, 1),
            new(567106, "Rose Fithik Young melee 174/34", 174, 34, 0.5, 3.0, 1500, 0, 33, 49, 1),
            new(567107, "Fithik Guardian melee 174/34", 174, 34, 0.5, 3.0, 1200, 0, 72, 108, 1),
            new(567108, "Fithik Guardian acid spray 201/1", 201, 1, 1.0, 5.0, 10000, 0, 62, 102, 4),
            new(567109, "Fithik Guardian self-destruct 180/1", 180, 1, 0.0, 0.0, 0, 0, 305, 364, 1),
            new(567110, "Yzub melee 174/34", 174, 34, 0.5, 3.0, 1200, 0, 86, 129, 1),
            new(567111, "Yzub acid spray 201/1", 201, 1, 1.0, 5.0, 10000, 0, 74, 123, 4),
            new(567112, "Yzub self-destruct 180/1", 180, 1, 0.0, 0.0, 0, 0, 366, 437, 1),
            new(567113, "Kryell melee 174/34", 174, 34, 0.5, 3.0, 1200, 0, 94, 141, 1),
            new(567114, "Kryell acid spray 201/1", 201, 1, 1.0, 5.0, 10000, 0, 81, 134, 4),
            new(567115, "Kryell self-destruct 180/1", 180, 1, 0.0, 0.0, 0, 0, 399, 477, 1),
            new(567116, "Fithik Hive Master 174/34", 174, 34, 0.5, 3.0, 800, 0, 104, 155, 1),
            new(567117, "Fithik Hive Master 405/2", 405, 2, 1.0, 30.0, 15000, 1200, 124, 160, 1),
            new(567118, "Fithik Hive Master 201/2", 201, 2, 0.5, 5.0, 10000, 0, 89, 160, 1),
            new(567119, "Fithik Hive Master 180/2", 180, 2, 0.0, 0.0, 0, 4333, 318, 424, 1),
            new(567120, "Miasma 205/1", 205, 1, 0.5, 3.0, 1000, 660, 36, 71, 3),
            new(567121, "Miasma 485/1", 485, 1, 0.0, 10.0, 30000, 0, 48, 60, 3),
            new(567122, "Miasma Egg-Layer 205/1", 205, 1, 0.5, 3.0, 1000, 660, 58, 116, 3),
            new(567123, "Miasma Egg-Layer 485/1", 485, 1, 0.0, 10.0, 30000, 0, 78, 97, 3),
            new(567124, "Wickip 205/1", 205, 1, 0.5, 3.0, 1000, 660, 63, 127, 3),
            new(567125, "Wickip 485/1", 485, 1, 0.0, 10.0, 30000, 0, 85, 106, 3),
            new(567126, "Thrax Soldier 397/1", 397, 1, 0.5, 3.0, 10000, 333, 25, 32, 1),
            new(567127, "Thrax Soldier 174/46", 174, 46, 0.5, 3.5, 1300, 0, 48, 72, 1),
            new(567128, "Thrax Grenadier 1/229", 1, 229, 5.0, 15.0, 2500, 0, 48, 72, 2),
            new(567129, "Thrax Grenadier 1/79", 1, 79, 1.0, 15.0, 800, 0, 48, 72, 1),
            new(567130, "Hunter 403/1", 403, 1, 1.0, 40.0, 10000, 900, 17, 32, 13),
            new(567131, "Hunter 1/244", 1, 244, 1.0, 20.0, 1500, 0, 48, 72, 1),
            new(567132, "Thrax Technician 1/296", 1, 296, 1.0, 20.0, 800, 0, 65, 98, 5),
            new(567133, "Caretaker 395/1", 395, 1, 0.5, 10.0, 800, 0, 14, 21, 1),
            new(567134, "Sklorax 395/3", 395, 3, 1.0, 40.0, 1000, 700, 71, 106, 1),
            new(567135, "Harmox 1/229", 1, 229, 5.0, 15.0, 2500, 0, 78, 117, 2),
            new(567136, "Harmox 1/79", 1, 79, 1.0, 15.0, 800, 0, 78, 117, 1),
            new(567137, "Harmox 457/1", 457, 1, 1.0, 30.0, 15000, 500, 28, 66, 6),
            new(567138, "Harmox 458/1", 458, 1, 1.0, 40.0, 10000, 500, 134, 265, 1),
            new(567139, "Overlord Gryl 1/296", 1, 296, 1.0, 20.0, 800, 0, 117, 175, 5),
            new(567140, "Overseer Qraal 1/79", 1, 79, 0.5, 30.0, 800, 0, 19, 32, 1),
            new(567141, "Overseer Qraal 449/1", 449, 1, 1.0, 40.0, 5000, 500, 106, 210, 13),
            new(567142, "Overseer Qraal 455/1", 455, 1, 0.0, 10.0, 20000, 500, 6, 16, 1),
            new(567143, "Overseer Qraal 278/1", 278, 1, 0.0, 12.0, 15000, 500, 52, 106, 1),
            new(567144, "Overseer Qraal 451/1", 451, 1, 1.0, 40.0, 5000, 500, 106, 210, 7),
            new(567145, "Kael Corporal 171/1", 171, 1, 0.5, 3.0, 4000, 1233, 107, 143, 1),
            new(567146, "Kael Corporal 433/1", 433, 1, 0.5, 3.0, 4000, 1033, 119, 166, 1),
            new(567147, "Kael Corporal 497/1", 497, 1, 1.0, 20.0, 3500, 1033, 48, 72, 7),
            new(567148, "Kael Corporal 496/1", 496, 1, 8.0, 50.0, 15000, 0, 72, 95, 1),
            new(567149, "Kael Centurion 171/1", 171, 1, 0.5, 3.0, 4000, 1233, 146, 194, 1),
            new(567150, "Kael Centurion 433/1", 433, 1, 0.5, 3.0, 4000, 1033, 162, 227, 1),
            new(567151, "Kael Centurion 497/1", 497, 1, 1.0, 20.0, 3500, 1033, 65, 98, 7),
            new(567152, "Kael Centurion 496/1", 496, 1, 8.0, 50.0, 15000, 0, 98, 129, 1),
            new(567153, "Rhamshaw 433/1", 433, 1, 0.5, 3.0, 4000, 1033, 283, 396, 1),
            new(567154, "Rhamshaw 171/1", 171, 1, 0.5, 3.0, 4000, 1233, 254, 339, 1),
            new(567155, "Rhamshaw 497/1", 497, 1, 1.0, 20.0, 3500, 1033, 113, 170, 7),
            new(567156, "Rhamshaw 496/1", 496, 1, 8.0, 50.0, 15000, 0, 170, 226, 1),
            new(567157, "Nitroglazer weapon 1/232", 1, 232, 1.0, 30.0, 1500, 0, 48, 72, 1),
            new(567158, "Nitroglazer mortar 289/1", 289, 1, 5.0, 40.0, 12000, 0, 69, 108, 1),
            new(567159, "Class IV Stalker 1/78", 1, 78, 1.0, 60.0, 1500, 0, 82, 123, 6),
            new(567160, "Class IV Stalker 441/1", 441, 1, 0.0, 20.0, 60000, 0, 15, 30, 1),
            new(567161, "Class IV Stalker 442/1", 442, 1, 0.0, 0.0, 0, 3666, 558, 669, 5),
            new(567162, "Class V Stalker 1/78", 1, 78, 1.0, 60.0, 1500, 0, 106, 159, 6),
            new(567163, "Class V Stalker 441/1", 441, 1, 0.0, 20.0, 60000, 0, 19, 38, 1),
            new(567164, "Class V Stalker 442/1", 442, 1, 0.0, 0.0, 0, 3666, 723, 868, 5),
            new(567165, "Class V Predator 1/85", 1, 85, 1.0, 50.0, 3000, 0, 130, 194, 6),
            new(567166, "Class V Predator 407/1", 407, 1, 0.0, 0.0, 0, 0, 177, 265, 1),
            new(567167, "Class V Predator 426/1", 426, 1, 1.0, 40.0, 8000, 0, 442, 576, 1),
            new(567168, "Sentinel weapon 1/152", 1, 152, 1.0, 40.0, 1500, 0, 53, 79, 1),
            new(567169, "Sentinel scan 266/1", 266, 1, 0.0, 60.0, 20000, 0, 0, 0, 0),
            new(567170, "Juggernaut weapon 1/180", 1, 180, 1.0, 60.0, 1500, 0, 72, 108, 1),
            new(567171, "Juggernaut cannon 428/1", 428, 1, 1.0, 40.0, 8000, 0, 162, 231, 1),
            new(567172, "Shield Drone 396/1", 396, 1, 0.5, 3.0, 1000, 0, 35, 106, 1),
            new(567173, "Bane Light Howitzer 411/1", 411, 1, 0.0, 60.0, 1000, 0, 95, 143, 1)
        };

        public static readonly Beast[] Creatures =
        {
            new(Kogari, "Ranger Kogari - Hightower Outpost", 7034, TargetCategoryRow.Friendly, 20, 3000, 0, 2955, 0, 0),
            new(Siri, "Ranger Siri - Hightower Outpost", 7035, TargetCategoryRow.Friendly, 20, 3000, 0, 2956, 0, 0),
            new(Lurori, "Ranger Lurori - Hightower Outpost", 7036, TargetCategoryRow.Friendly, 20, 3000, 0, 2957, 0, 0),
            new(Fransisco, "Captain Fransisco - Devil's Den", AliaDasNpcs.SwapsetMale, TargetCategoryRow.Friendly, 17, 9000, 150, 6587, 9, 5, 567101),
            new(Caritas, "Caritas - Devil's Den detention", 7036, TargetCategoryRow.Friendly, 20, 3000, 0, 6694, 0, 0),
            new(JasperFithik, "Jasper Fithik - Devil's Den", 4313, TargetCategoryRow.Hostile, 19, 1023, 74, 7789, 9, 5, 567102, 567103, 567104, 567105),
            new(RoseFithikYoung, "Rose Fithik Young - Devil's Den eggs", 21499, TargetCategoryRow.Hostile, 19, 767, 56, 8392, 9, 5, 567106),
            new(FithikGuardian, "Fithik Guardian - Devil's Den", 24069, TargetCategoryRow.Hostile, 20, 1951, 101, 9170, 9, 5, 567107, 567108, 567109),
            new(Yzub, "Yzub - Devil's Den", 24069, TargetCategoryRow.Hostile, 20, 2927, 121, 10189, 9, 5, 567110, 567111, 567112),
            new(Kryell, "Kryell - Devil's Den", 24069, TargetCategoryRow.Hostile, 21, 3192, 132, 10192, 9, 5, 567113, 567114, 567115),
            new(HiveMaster, "Fithik Hive Master - Devil's Den", 10323, TargetCategoryRow.Hostile, 21, 3300, 200, 6690, 9, 5, 567116, 567117, 567118, 567119),
            new(Miasma, "Miasma - Devil's Den", 6170, TargetCategoryRow.Hostile, 19, 1023, 74, 426, 9, 5, 567120, 567121, 56011),
            new(EggLayer, "Radon Miasma Egg-Layer - Devil's Den", 10240, TargetCategoryRow.Hostile, 20, 2927, 300, 8441, 9, 5, 567122, 567123, 56011),
            new(Wickip, "Wickip - Devil's Den", 10240, TargetCategoryRow.Hostile, 21, 3192, 320, 10191, 9, 5, 567124, 567125, 56011),
            new(ThraxSoldier, "Thrax Soldier - Devil's Den", 20757, TargetCategoryRow.Hostile, 20, 1115, 81, 236, 9, 5, 567126, 567127),
            new(ThraxGrenadier, "Thrax Grenadier - Devil's Den", 9354, TargetCategoryRow.Hostile, 20, 1115, 81, 0, 9, 5, 567128, 567129),
            new(ThraxHunter, "Hunter - Devil's Den", 10166, TargetCategoryRow.Hostile, 20, 1115, 81, 388, 9, 5, 567130, 567131, 65035),
            new(ThraxTechnician, "Thrax Technician - Devil's Den", 7043, TargetCategoryRow.Hostile, 19, 1342, 93, 8058, 9, 5, 567132, 62045, 65014, 68079),
            new(Caretaker, "Caretaker - Devil's Den", 9244, TargetCategoryRow.Hostile, 20, 1115, 81, 366, 9, 5, 567133, 62029, 62030),
            new(Sklorax, "Sklorax - Devil's Den detention", 10321, TargetCategoryRow.Hostile, 21, 3192, 132, 10182, 9, 5, 567134, 62033, 62034),
            new(Harmox, "Harmox - Devil's Den", 21882, TargetCategoryRow.Hostile, 21, 3192, 132, 10188, 9, 5, 567135, 567136, 567137, 68059, 567138),
            new(Gryl, "Overlord Gryl - Devil's Den mine control", 10502, TargetCategoryRow.Hostile, 21, 3192, 132, 8555, 9, 5, 567139, 62045, 65014, 68079),
            new(Qraal, "Overseer Qraal - Devil's Den field commander", 10504, TargetCategoryRow.Hostile, 23, 3796, 158, 6689, 9, 5, 567140, 567141, 61017, 567142, 567143, 567144),
            new(KaelCorporal, "Kael Corporal - Devil's Den", 4046, TargetCategoryRow.Hostile, 19, 1360, 74, 7725, 9, 5, 567145, 567146, 567147, 567148),
            new(KaelCenturion, "Kael Centurion - Devil's Den", 3901, TargetCategoryRow.Hostile, 20, 1951, 101, 9846, 9, 5, 567149, 567150, 567151, 567152),
            new(Rhamshaw, "Rhamshaw - Devil's Den", 25413, TargetCategoryRow.Hostile, 21, 3192, 132, 10183, 9, 5, 567153, 567154, 567155, 567156),
            new(Nitroglazer, "Nitroglazer - Devil's Den", 7234, TargetCategoryRow.Hostile, 20, 1115, 81, 434, 9, 5, 567157, 567158),
            new(StalkerIV, "Class IV Stalker - Devil's Den", 3781, TargetCategoryRow.Hostile, 19, 1789, 93, 7693, 9, 5, 567159, 567160, 567161),
            new(StalkerV, "Class V Stalker - Devil's Den", 3781, TargetCategoryRow.Hostile, 22, 2320, 120, 7694, 9, 5, 567162, 567163, 567164),
            new(Predator, "Class V Predator - Devil's Den", 3902, TargetCategoryRow.Hostile, 21, 2128, 110, 7742, 9, 5, 567165, 567166, 63011, 567167),
            new(Sentinel, "Sentinel - Devil's Den", 7344, TargetCategoryRow.Hostile, 20, 1561, 101, 436, 9, 5, 567168, 567169),
            new(Juggernaut, "Juggernaut - Devil's Den", 7722, TargetCategoryRow.Hostile, 20, 2488, 121, 10757, 9, 5, 567170, 567171),
            new(ShieldDrone, "Shield Drone - Devil's Den", 7233, TargetCategoryRow.Hostile, 21, 1216, 88, 438, 9, 5, 567172, 45),
            new(Howitzer, "Bane Light Howitzer - Devil's Den", 7482, TargetCategoryRow.Hostile, 20, 2602, 121, 8183, 0, 0, 567173)
        };


        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1, Neutral = 3, Ignore = 6 }

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A pool: up to two kinds of creature, how many of each, and the spread they stand in.</summary>
        public sealed record Spot(uint Id, uint Map, Vector3 At, double Rotation, uint RespawnTenths, uint Creature, uint Count = 1,
            uint Creature2 = 0, uint Count2 = 0, double Radius = 0);

        /// <summary>A world_destructible row.</summary>
        public sealed record Breakable(uint Id, uint ClassId, Vector3 At, double Rotation, string Comment, uint HitPoints = 0,
            uint CreatureId = 0, uint CreatureCount = 0);

        /// <summary>A world_usable row: what a dispenser gives, what a bomb takes, and their timings.</summary>
        public sealed record Usable(uint Id, uint ClassId, Vector3 At, double Rotation, string Comment, uint ItemTemplateId = 0,
            uint ItemClassId = 0, uint ItemCount = 0, uint ResetSeconds = 0, uint DamageMin = 0, uint DamageMax = 0, double Radius = 0);

        public const uint Respawn = 1200, NamedRespawn = 3000, NpcRespawn = 300;

        public static readonly Spot[] Pools =
        {
            new(FirstPool, Palisades, KogariAt, 3.6, NpcRespawn, Kogari),
            new(FirstPool + 1, Palisades, SiriAt, 2.4, NpcRespawn, Siri),
            new(FirstPool + 2, Palisades, LuroriAt, 0.0, NpcRespawn, Lurori),
            new(FirstPool + 3, Den, FransiscoAt, 3.0, 600, Fransisco),
            new(FirstPool + 4, Den, CaritasAt, 0.0, NpcRespawn, Caritas),
            new(FirstPool + 5, Den, YzubAt, 1.0, NamedRespawn, Yzub, 1, JasperFithik, 2, 6),
            new(FirstPool + 6, Den, GuardianAAt, 2.0, Respawn, FithikGuardian, 1, JasperFithik, 2, 6),
            new(FirstPool + 7, Den, GuardianBAt, 4.0, Respawn, FithikGuardian, 1, JasperFithik, 2, 6),
            new(FirstPool + 8, Den, KryellAt, 5.0, NamedRespawn, Kryell, 1, JasperFithik, 2, 6),
            new(FirstPool + 9, Den, GuardianHiveAt, 1.5, Respawn, FithikGuardian, 1, JasperFithik, 3, 8),
            new(FirstPool + 10, Den, RhamshawAt, 0.5, NamedRespawn, Rhamshaw),
            new(FirstPool + 11, Den, KaelAAt, 2.5, Respawn, KaelCorporal),
            new(FirstPool + 12, Den, KaelBAt, 4.5, Respawn, KaelCorporal),
            new(FirstPool + 13, Den, KaelCAt, 1.0, Respawn, KaelCorporal),
            new(FirstPool + 14, Den, CenturionAt, 0.0, Respawn, KaelCenturion),
            new(FirstPool + 15, Den, NitroAAt, 3.0, Respawn, Nitroglazer, 2, Radius: 6),
            new(FirstPool + 16, Den, NitroBAt, 5.5, Respawn, Nitroglazer),
            new(FirstPool + 17, Den, CaretakerDenAt, 2.0, Respawn, Caretaker, 1, ThraxSoldier, 2, 6),
            new(FirstPool + 18, Den, SkloraxAt, 3.1, NamedRespawn, Sklorax),
            new(FirstPool + 19, Den, DetentionGuardsAt, 1.6, Respawn, ThraxSoldier, 2, ThraxTechnician, 1, 5),
            new(FirstPool + 20, Den, MiasmaAAt, 0.0, Respawn, Miasma),
            new(FirstPool + 21, Den, MiasmaBAt, 2.0, Respawn, Miasma),
            new(FirstPool + 22, Den, EggLayerAAt, 4.0, NamedRespawn, EggLayer),
            new(FirstPool + 23, Den, EggLayerBAt, 1.0, NamedRespawn, EggLayer),
            new(FirstPool + 24, Den, WickipAt, 3.0, NamedRespawn, Wickip),
            new(FirstPool + 25, Den, GrylAt, 1.6, NamedRespawn, Gryl),
            new(FirstPool + 26, Den, MineGuardsAt, 4.7, Respawn, Caretaker, 1, ThraxTechnician, 1, 5),
            new(FirstPool + 27, Den, StalkerIVaAt, 1.0, Respawn, StalkerIV),
            new(FirstPool + 28, Den, StalkerIVbAt, 2.0, Respawn, StalkerIV),
            new(FirstPool + 29, Den, StalkerVAt, 3.0, Respawn, StalkerV),
            new(FirstPool + 30, Den, PredatorAAt, 4.0, Respawn, Predator),
            new(FirstPool + 31, Den, PredatorBAt, 5.0, Respawn, Predator),
            new(FirstPool + 32, Den, SentinelAAt, 0.5, Respawn, Sentinel),
            new(FirstPool + 33, Den, SentinelBAt, 1.5, Respawn, Sentinel),
            new(FirstPool + 34, Den, SentinelCAt, 2.5, Respawn, Sentinel),
            new(FirstPool + 35, Den, JuggernautAt, 3.5, NamedRespawn, Juggernaut),
            new(FirstPool + 36, Den, DroneAAt, 4.5, Respawn, ShieldDrone, 1, ThraxSoldier, 2, 6),
            new(FirstPool + 37, Den, DroneBAt, 5.5, Respawn, ShieldDrone, 1, ThraxGrenadier, 2, 6),
            new(FirstPool + 38, Den, HarmoxAt, 4.2, NamedRespawn, Harmox, 1, ThraxGrenadier, 2, 6),
            new(FirstPool + 39, Den, QraalAt, 1.57, NamedRespawn, Qraal),
            new(FirstPool + 40, Den, TowerGuardsAt, 1.57, Respawn, Caretaker, 1, ThraxTechnician, 1, 5)
        };

        public static readonly Breakable[] Breakables = BuildBreakables();

        private static Breakable[] BuildBreakables()
        {
            var rows = new List<Breakable>();

            for (var i = 0; i < EggClusters.Length; i++)
                rows.Add(new((uint)(FirstEggCluster + i), EggClusterClass, EggClusters[i], i * 0.7 % 6.28,
                    $"Devil's Den egg cluster {i + 1} (Operation Devil's Strike)", CreatureId: RoseFithikYoung, CreatureCount: 2));

            for (var i = 0; i < Eggs.Length; i++)
                rows.Add(new((uint)(FirstEgg + i), EggClass, Eggs[i], i * 0.9 % 6.28, $"Devil's Den Fithik egg {i + 1}",
                    CreatureId: RoseFithikYoung, CreatureCount: 1));

            rows.Add(new(FirstNode, MachineryClass, NodeAAt, 1.57, "Devil's Den Thrax Machinery Power Node (tower service room)", 1500));
            rows.Add(new(FirstNode + 1, MachineryClass, NodeBAt, 4.71, "Devil's Den Thrax Machinery Power Node (tower service room)", 1500));
            rows.Add(new(FirstNode + 2, MachineryClass, NodeCAt, 3.14, "Devil's Den Thrax Machinery Power Node (west of the impact site)", 1500));
            rows.Add(new(FirstPad, MachineryClass, PadAAt, 0.0, "Devil's Den Thrax Machinery Landing Pad", 1500));
            rows.Add(new(FirstPad + 1, MachineryClass, PadBAt, 0.0, "Devil's Den Thrax Machinery Landing Pad", 1500));
            rows.Add(new(FirstPad + 2, MachineryClass, PadCAt, 3.14, "Devil's Den Thrax Machinery Landing Pad", 1500));
            rows.Add(new(FuelDepot, FuelDepotClass, FuelAt, 1.57, "Devil's Den Bane Machinery Fuel Depot (a bomb's to destroy)", 1500));
            rows.Add(new(LaserDoor, LaserDoorClass, DoorAt, 0.0, "Devil's Den detention center laser door", 3000));
            rows.Add(new(GeneratorRow, GeneratorClass, GeneratorAt, 2.8, "Devil's Den generator up the path (a bomb's to destroy)", 1500));

            var teleporters = new[] { Tele1At, Tele2At, Tele3At, Tele4At };

            for (var i = 0; i < teleporters.Length; i++)
                rows.Add(new((uint)(FirstTeleporter + i), BaneTeleporterClass, teleporters[i], i * 1.57,
                    $"Devil's Den platform Bane teleporter {i + 1}", 1500));

            return rows.ToArray();
        }

        public static readonly Usable[] Usables =
        {
            new(HakariCorpse, HakariCorpseClass, HakariAt, 1.0, "Hakari's corpse: Hakari's Band", HakariBandTemplate, ItemCount: 1, ResetSeconds: 20),
            new(LuroronCorpse, LuroronCorpseClass, LuroronAt, 2.0, "Luroron's corpse: Luroron's Band", LuroronBandTemplate, ItemCount: 1, ResetSeconds: 20),
            new(FirstArtifact, ArtifactStoneClass, Art1At, 0.0, "Forean Eloh Artifact (Hive Master Hatchery)", ArtifactTemplate, ItemCount: 1, ResetSeconds: 20),
            new(FirstArtifact + 1, ArtifactStoneClass, Art2At, 1.0, "Forean Eloh Artifact (Bane Mine Control)", ArtifactTemplate, ItemCount: 1, ResetSeconds: 20),
            new(FirstArtifact + 2, ArtifactStoneClass, Art3At, 2.0, "Forean Eloh Artifact (Bane Mine Control)", ArtifactTemplate, ItemCount: 1, ResetSeconds: 20),
            new(FirstArtifact + 3, ArtifactStoneClass, Art4At, 3.0, "Forean Eloh Artifact (Bane Communications Tower)", ArtifactTemplate, ItemCount: 1, ResetSeconds: 20),
            new(FirstArtifact + 4, ArtifactStoneClass, Art5At, 4.0, "Forean Eloh Artifact (Bane Communications Tower)", ArtifactTemplate, ItemCount: 1, ResetSeconds: 20),
            new(DoorConsole, ConsoleClass, ConsoleAt, 3.14, "Devil's Den detention center laser door console"),
            new(WestPanel, ConsoleClass, PanelWAt, 2.36, "Devil's Den west bunker howitzer panel"),
            new(EastPanel, ConsoleClass, PanelEAt, 0.79, "Devil's Den east bunker howitzer panel"),
            new(GeneratorBomb, BombClass, GenBombAt, 0.0, "Devil's Den bomb at the generator", ItemClassId: DetonatorClass, ItemCount: 1, DamageMin: 20, DamageMax: 40, Radius: 10),
            new(DoorBomb, BombClass, DoorBombAt, 0.0, "Devil's Den bomb at the detention center laser door", ItemClassId: DetonatorClass, ItemCount: 1, DamageMin: 20, DamageMax: 40, Radius: 10),
            new(DepotBomb, BombClass, FuelAt + new Vector3(4f, 0f, 0f), 0.0, "Devil's Den bomb at the fuel depot", ItemClassId: DetonatorClass, ItemCount: 1, DamageMin: 20, DamageMax: 40, Radius: 10)
        };

        /// <summary>The detonator crate on the Den's platform: five detonators.</summary>
        public const uint DetonatorCrateResetSeconds = 600;

        #region Regions

        public sealed record Volume(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const byte Box = 2;

        /// <summary>
        /// The Bane Communications Tower (region 3, priority 6, one over the Shard Impact Site's
        /// circle about it): the command center (arch_bane_industrial_room_control_24m and its
        /// overlooks, 138 to 206 east), the teleporter hall east of it, and the service wing
        /// with its rooms (y 150) to the south. The Bane Detention Center has no region of the
        /// client's, and gets none.
        /// </summary>
        public static readonly Volume[] Volumes =
        {
            Of(CommTowerRegion, 136, 206, 656, 744, 136, 164, "Bane Communications Tower (command center)"),
            Of(CommTowerRegion, 206, 246, 664, 712, 136, 160, "Bane Communications Tower (teleporter hall)"),
            Of(CommTowerRegion, 188, 264, 622, 664, 144, 166, "Bane Communications Tower (service wing)")
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(Den, region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof,
                "foreas_concordia_palisades_devilsden - " + comment);

        #endregion

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            // An action row has nothing to hang on: the SELECT names a table only so that its
            // WHERE is one MySQL takes as well as SQLite.
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {AnyClass} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = c.Actions.Concat(Enumerable.Repeat(0U, 8)).Take(8).ToArray();

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            // Kogari carries Cyrida's spear; Fransisco wears Brody's AFS kit, with the rocket gunner's launcher for Brody's rifle.
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {Kogari}, a.slot_id, a.Class_id, a.color " +
                $"FROM creature_appearance a WHERE a.id = {Cyrida} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Kogari}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {Kogari});";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                $"SELECT {Fransisco}, a.slot_id, CASE WHEN a.slot_id = 13 THEN {RocketLauncher} ELSE a.Class_id END, a.color " +
                $"FROM creature_appearance a WHERE a.id = {Brody} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Fransisco}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {Fransisco});";

            // The Hunter's shield and the howitzer's launcher, in their weapon slots as the Palisades' rows have them.
            foreach (var (creature, from) in LooksLike)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {creature}, a.slot_id, a.Class_id, a.color " +
                    $"FROM creature_appearance a WHERE a.id = {from} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {creature}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {creature});";

            foreach (var (creature, package, who) in Packages)
                yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {creature}, {package}, {S(who)} " +
                    $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {creature});";

            foreach (var p in Pools)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Id}, 0, 0, {p.RespawnTenths}, {N(p.At.X)}, {N(p.At.Y)}, {N(p.At.Z)}, {N(p.Rotation)}, {p.Map}, " +
                    $"{p.Creature}, {p.Count}, {p.Count}, {p.Creature2}, {p.Count2}, {p.Count2}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {to} WHERE map_context_id = {Den} AND creature_{slot}_Id = {from} " +
                        $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {to});";

            foreach (var b in Breakables)
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {b.Id}, {Den}, {b.ClassId}, {N(b.At.X)}, {N(b.At.Y)}, {N(b.At.Z)}, {N(b.Rotation)}, {b.HitPoints}, {b.CreatureId}, {b.CreatureCount}, 0, {S(b.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {Den} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {b.Id});";

            foreach (var u in Usables)
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {u.Id}, {Den}, {u.ClassId}, {N(u.At.X)}, {N(u.At.Y)}, {N(u.At.Z)}, {N(u.Rotation)}, 0, 1, {u.ItemTemplateId}, {u.ItemClassId}, " +
                    $"{u.ItemCount}, {u.ResetSeconds}, 0, 0, {u.DamageMin}, {u.DamageMax}, {N(u.Radius)}, 0, {S(u.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {Den} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {u.Id});";

            yield return "INSERT INTO world_container (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, loot_profile_key, cipher_level, reset_seconds, comment) " +
                $"SELECT {DetonatorCrate}, {Den}, {DetonatorCrateClass}, {N(CrateAt.X)}, {N(CrateAt.Y)}, {N(CrateAt.Z)}, 1.57, NULL, 0, {DetonatorCrateResetSeconds}, " +
                $"'Devil''s Den platform crate: five detonators' FROM map_info m WHERE m.map_context_id = {Den} " +
                $"AND NOT EXISTS (SELECT 1 FROM world_container x WHERE x.id = {DetonatorCrate});";
            yield return $"INSERT INTO world_container_item (container_id, item_template_id, quantity) SELECT {DetonatorCrate}, {DetonatorTemplate}, 5 " +
                $"FROM world_container c WHERE c.id = {DetonatorCrate} AND NOT EXISTS (SELECT 1 FROM world_container_item x WHERE x.container_id = {DetonatorCrate});";

            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {v.MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {v.MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";

            yield return $"DELETE FROM world_container_item WHERE container_id = {DetonatorCrate};";
            yield return $"DELETE FROM world_container WHERE id = {DetonatorCrate};";
            yield return $"DELETE FROM world_usable WHERE id BETWEEN {HakariCorpse} AND {DepotBomb};";
            yield return $"DELETE FROM world_destructible WHERE id BETWEEN {FirstEggCluster} AND {FirstTeleporter + 3};";

            // The Den's own pools out first: the rest of its pools are then the ones that had the Palisades' rows.
            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Creature};";

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {from} WHERE map_context_id = {Den} AND creature_{slot}_Id = {to};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            yield return $"DELETE FROM creature_appearance WHERE id IN ({Kogari}, {Fransisco}, {string.Join(", ", LooksLike.Select(look => look.Creature))});";

            foreach (var c in Creatures)
            {
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id};";
        }

        /// <summary>The entity class the creature_action inserts select from (NPC_Forean_Spearman): a row that is always there.</summary>
        private const uint AnyClass = 7034;

        /// <summary>Weapon_Human_Redshirt_RocketLauncher_Physical, as the AFS Rocket Gunner holds it.</summary>
        public const uint RocketLauncher = 6272;

        /// <summary>New rows that hold what the Palisades row they replace holds: the Hunter's shield, the howitzer's launcher.</summary>
        public static readonly (uint Creature, uint From)[] LooksLike = { (ThraxHunter, 531067), (Howitzer, 592003) };

        public static readonly (uint Creature, uint Package, string Who)[] Packages =
        {
            (Kogari, KogariPackage, "Ranger Kogari"), (Fransisco, FransiscoPackage, "Captain Fransisco"), (Caritas, CaritasPackage, "Caritas")
        };

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {v.MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string N(float value) => ((double)value).ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => InstanceMissionSql.S(value);

        #region Missions

        private const string Walkthrough = "https://tabularasa.fandom.com/wiki/Devil%27s_Den";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            HakarisBandRows(sql);
            LuroronsBandRows(sql);
            FindCaritasRows(sql);
            ElohArtifactsRows(sql);
            DevilsStrikeRows(sql);
            MissingStrikeTeamRows(sql);
            FinishTheTaskRows(sql);
            SeverTheHeadRows(sql);
            return sql;
        }

        private static InstanceMissionSql.RewardItem Item(uint template, uint module = 0) => new(template, 1, module);

        private static void Marker(InstanceMissionSql mission, uint objective, Vector3 at, double radius = 8, uint indicator = 0) =>
            mission.Indicator(objective, indicator != 0 ? indicator : MissionIndicator.UnnamedFrom + objective, at.X, at.Y, at.Z, radius);

        /// <summary>Hakari's corpse in the tunnel to the second chamber gives his band; Siri takes it.</summary>
        private static void HakarisBandRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, HakarisBand, "Recovery of Hakari's Band", 424, Siri, Siri, 19, Category);
            mission.Reward(19000, 2850, selectableItems: new[] { Item(29161), Item(18633), Item(35439) });
            mission.Objective(1, 428, 7629, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, HakariBandClass, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 1, HakariAt);
            Evidence(mission, "Hakari's corpse7074 lies in the tunnel from the first chamber (the walkthrough's 47, 80, -145); searched, it " +
                "gives Hakari's Band (class7070/template459), handed in to Ranger Siri at Hightower Outpost beside Cyrida.");
            mission.Enable(Collected("band", HakariBandTemplate, 1));
        }

        /// <summary>Luroron's corpse by Fransisco's alcove gives his band; Lurori takes it.</summary>
        private static void LuroronsBandRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LuroronsBand, "Recover Luroron's Band", 429, Lurori, Lurori, 19, Category);
            mission.Reward(19000, 2850, selectableItems: new[] { Item(122949), Item(122999), Item(122959) });
            mission.Objective(1, 433, 7226, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, LuroronBandClass, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 1, LuroronAt);
            Evidence(mission, "Luroron's corpse7075 lies near Captain Fransisco (the walkthrough's -54, 94, 42); searched, it gives " +
                "Luroron's Band (class7071/template460), handed in to Ranger Lurori. The level 5 armor mod schematics are the modules themselves.");
            mission.Enable(Collected("band", LuroronBandTemplate, 1));
        }

        /// <summary>Cyrida sends the player to Caritas, in the Bane Detention Center behind its laser door.</summary>
        private static void FindCaritasRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, FindCaritas, "Find Caritas in the Devil's Den Cave", 434, Cyrida, Caritas, 19, Category);
            mission.Reward(19000, 1900);
            mission.Objective(1, 438, 7241, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, CaritasPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 1, CaritasAt);
            Evidence(mission, "Caritas (package557) is held in the Bane Detention Center (77, 106, 213), guarded by Sklorax, behind a laser door " +
                "its console, a bomb or gunfire opens.");
            mission.Enable(Scene());
        }

        /// <summary>Five Eloh artifact stones: the Hive Master's, two in Bane Mine Control, two in the tower; back to Kogari.</summary>
        private static void ElohArtifactsRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ElohArtifacts, "Recover the Eloh Artifacts", 441, Kogari, Kogari, 20, Category);
            mission.Reward(38000, 3800, selectableItems: new[] { Item(17514), Item(13717), Item(18213) });
            mission.Objective(1, 457, 7631, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 6020, 7632, 2);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, ArtifactClass, target: 5);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, KogariPackage);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 1, Art1At, 12);
            Marker(mission, 2, KogariAt);
            Evidence(mission, "The walkthrough's five artifacts: one in the Hive Master's room, two in the Bane machinery room (Bane Mine " +
                "Control), two in the command center. Each is an artifact stone11093 giving a Forean Eloh Artifact (class7084/template463).");
            mission.Enable(Collected("artifacts", ArtifactTemplate, 5));
        }

        /// <summary>The Hive Master dead and 50 egg clusters destroyed: every squad member in the copy is credited with each cluster.</summary>
        private static void DevilsStrikeRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, DevilsStrike, "Operation Devil's Strike", 2701, Brody, Brody, 21, Category);
            mission.Reward(34000, 3400, selectableItems: new[] { Item(28564, 100049), Item(20717, 100029), Item(39125, 100049) });
            mission.Objective(1, 2717, 8062, 1, MissionObjectiveState.Incomplete, counterTextId: 2719);
            mission.Objective(2, 2720, 8063, 2, MissionObjectiveState.Incomplete, counterTextId: 2721);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, HiveMaster, counter: 0, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2);
            var events = Enumerable.Range(1, EggClusters.Length).Select(i => (uint)i).ToArray();
            foreach (var e in events)
                mission.Progress(2, MissionProgressEventKind.ScenarioEvent, e, counter: EggScenario, target: (uint)events.Length, trigger: e);
            mission.Scenario(EggScenario, "The Den's egg clusters", events);
            Marker(mission, 1, HiveMasterAt, 15);
            Marker(mission, 2, HatcheryAt, 60);
            Evidence(mission, "The Hive Master (Hive Master Hatchery, -81, 170, 326) at level 21, and 50 Fithik egg clusters10180 - 30 in its " +
                "hatchery, 20 in the tunnel and the Den - each destroyed recording its own event of scenario1 for every squad member in the copy. " +
                "Pulsar, Luminar and Pulsar pieces at level 21 with their brand's module[2].");
            mission.Enable(Scene(events: true));
        }

        /// <summary>Fransisco found in his alcove: he joins the one who found him (DevilsDen) until Brody hears of it.</summary>
        private static void MissingStrikeTeamRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, MissingStrikeTeam, "Missing Strike Team", 2706, Brody, Brody, 20, Category);
            mission.Reward(40000, 4000, selectableItems: new[] { Item(32741, 100033), Item(17607, 900231), Item(38867, 100037) });
            mission.Objective(1, 2712, 8059, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 2974, 8060, 2);
            mission.Transition(1);
            mission.Conversation(1, FransiscoPackage);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, BrodyPackage);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 1, FransiscoAt);
            Marker(mission, 2, BrodyAt);
            Evidence(mission, "Captain Fransisco (package550) in the tunnel to the second chamber (-59, 96, 50). Found, he follows the one who " +
                "found him through the rest of the Den, fighting at their side, and is put beside them where they have gone on without him; " +
                "killed, he is back in his alcove, and follows again whoever on the mission comes to him.");
            mission.Enable(Scene());
        }

        /// <summary>Caritas's task: the Hive Master dead, then back to her, then to Cyrida.</summary>
        private static void FinishTheTaskRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, FinishTheTask, "Finish the Task", 2747, Caritas, Cyrida, 21, Category);
            mission.Reward(41000, 3800, selectableItems: new[] { Item(16673, 900216), Item(16932, 900216), Item(17859, 900206) });
            mission.Prerequisite(FindCaritas);
            mission.Objective(8, 4461, 7237, 1, MissionObjectiveState.Incomplete, counterTextId: 4462);
            mission.Objective(3, 2806, 7238, 2);
            mission.Objective(7, 3241, 7239, 3);
            mission.Transition(8);
            mission.Progress(8, MissionProgressEventKind.CreatureKilled, HiveMaster, counter: 0, target: 1);
            mission.RevealAndActivate(8, 3);
            mission.Transition(3);
            mission.Conversation(3, CaritasPackage);
            mission.RevealAndActivate(3, 7);
            mission.Transition(7);
            mission.Conversation(7, CyridaPackage);
            mission.Action(7, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, 8, HiveMasterAt, 15);
            Marker(mission, 3, CaritasAt);
            Marker(mission, 7, CyridaAt);
            Evidence(mission, "Given by Caritas once found; the client's objectives 8, 3 and 7 in that order. Teleract and ChiTech weapons at " +
                "level 21 with their brand's module[2].");
            mission.Enable(Scene());
        }

        /// <summary>
        /// The Field Commander (Overseer Qraal) dead, three power nodes and three landing pad
        /// machines destroyed, and the fuel depot if the player likes: each destroyed one is its
        /// event for every squad member in the copy.
        /// </summary>
        private static void SeverTheHeadRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, SeverTheHead, "Sever the Devil's Head", 2786, Brody, Brody, 23, Category);
            mission.Reward(38000, 3800, selectableItems: new[] { Item(18424, 100025), Item(38737, 100041), Item(29023, 100041) });
            mission.Objective(1, 2790, 7221, 1, MissionObjectiveState.Incomplete, counterTextId: 2791);
            mission.Objective(3, 2902, 7222, 2, MissionObjectiveState.Incomplete, counterTextId: 2903);
            mission.Objective(4, 3189, 7223, 3, MissionObjectiveState.Incomplete, counterTextId: 3190);
            mission.Objective(5, 3191, 7224, 4, MissionObjectiveState.Incomplete, required: false, counterTextId: 3192);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, Qraal, counter: 0, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            foreach (var (objective, scenario, count, name) in new[]
                     {
                         (3U, NodeScenario, 3U, "Thrax Machinery Power Nodes"), (4U, PadScenario, 3U, "Thrax Machinery Landing Pad"),
                         (5U, DepotScenario, 1U, "Bane Machinery Fuel Depot")
                     })
            {
                mission.Transition(objective);
                var events = Enumerable.Range(1, (int)count).Select(i => (uint)i).ToArray();
                foreach (var e in events)
                    mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, e, counter: scenario, target: count, trigger: e);
                mission.Scenario(scenario, name, events);
            }
            Marker(mission, 1, QraalAt, 15, FieldCommanderMarker);
            Marker(mission, 3, NodeAAt, 15);
            Marker(mission, 4, PadAAt, 15, LandingPadMarker);
            Marker(mission, 5, FuelAt, 10, FuelDepotMarker);
            Evidence(mission, "Overseer Qraal is the Field Commander, in the command center (165, 142, 700) at level 23. Thrax Machinery10287: " +
                "two power nodes in the service room right of the command center and one on the impact site's west side; three machines on " +
                "the landing pad by the crate stacks. The fuel depot12981 is no target: one of the platform crate's detonators sets the bomb " +
                "beside it. Audiodyne and CryoGen pieces at level 23 with their brand's module[2].");
            mission.Enable(Scene(events: true));
        }

        /// <summary>A mission's binding: for the bootcamp's graduates; one whose events the Den records has a scenario, which a mission has only with a script.</summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        /// <summary>A mission whose items are picked up in the Den and handed in: held ones count, and the turn-in takes them.</summary>
        private static MissionSceneDefinition Collected(string key, uint template, uint quantity)
        {
            var scene = Scene();
            scene.ExistingFactObjectiveIds = new() { 1 };
            scene.Items = new List<MissionItemBinding>
            {
                new(key, template, MissionItemScope.CharacterOwned, quantity,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: quantity)
            };
            return scene;
        }

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Walkthrough, "TaRapedia's Devil's Den: walkthrough, bestiary and NPCs.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "DevilsDenContent", reconstruction);
        }

        #endregion
    }
}
