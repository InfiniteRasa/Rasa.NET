using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Warnet Caverns (adv_foreas_concordia_palisades_warnetcaverns, 1384), the Palisades
    /// instance north-east of River-Base Krimm, peopled and fitted out for its eight missions
    /// (WarnetCavernsMissions; the Game's WarnetCaverns runs what the place does). Levels 17-19.
    ///
    /// From the client: the map (the entrance tunnel and its sandbags, the two upper chambers, the
    /// big cavern with the Research Base Camp's platforms, turrets, cots and monitor, the Dig Site
    /// with Corman's drills and sandbags, Bug City, and the Nest round its lake); the link marker
    /// on the Palisades map (uimapmarker 1402 to 1541) that had no reciprocal, which is why no
    /// map_link joined the caverns to the Palisades; the names (Information Specs. Sides,
    /// Brumley and Gooding, Field Medic Grant, Captain LaFontaine, Irate Warnet Soldiers, Miasma,
    /// the Queen Warnet Matriarch, Chelliak, Tozid, Joahg, Brazix, Lightbender Infiltrators); the
    /// item classes (the metal container, scent glands, digestive fluids, the queen lure, the
    /// goo covered object, Brazix's orders, the Lake Elinor and caverns trophies, Miasma essence
    /// and meat); the usable classes (the Scientific Remote Research Station, the Metal
    /// Container, the Queen Lure, the Pheromone Canister, the Hanging Hive, crates).
    ///
    /// From the Tabula Rasa wiki's walkthrough and Ten Ton Hammer's guides to the caverns and to
    /// River-Base Krimm: the specialists met at the sandbags by the entrance; the Research Base
    /// Camp cleared, Chelliak (L19) in it, and its three stations rebooted; Brumley at the camp's
    /// lower sandbags (68, 87, -31) and walked through the Lower Passage (172, 66, 124) to the Dig
    /// Site; the metal container by the sandbags there (142, 36, 49) beside a crate (140, 36, 52);
    /// Gooding at the camp (35, 97, -83); Joahg's twins (L18 and L19) at (148, 74, 116); Tozid
    /// (286, 49, 51), out once the Hanging Hives in Bug City are down; the lure at the red staff
    /// by the Nest's mouth, the Queen (L19) at (275, 1, 334), an attack at the lure and another at
    /// her death; Brazix (L19) and his Lightbenders at (236, 28, 255) and the bio-bomb at
    /// (164, 33, 298); the crates at (19, 97, -86), (140, 36, 50), (286, 51, 16), (280, 4.5, 394);
    /// LaFontaine in the middle of Krimm. Warnets immune to electric are the class's own.
    ///
    /// Ours: where everything stands (on the navmesh rebuilt with a 1.5 m climb), the numbers -
    /// health and damage down the Palisades' level-23 warnet by 1.0905 a level, a lieutenant one
    /// and three quarters, the Queen 2.625 times, as DevilsDenContent does - the specialists'
    /// and LaFontaine's looks (Information Spec. Johnson's, Field Lt. Yates's, Outpost Commander
    /// Randolph's officer's uniform), Grant's stock (the caverns' medical vendor's), the hives'
    /// spots (where Bug City's roof is 7-8 m up, low enough to shoot and to stir), and the lure's
    /// waves: 24 scripted pools of one level-19 warnet round the Nest, woken as many as the
    /// squad in the cavern wants (WarnetCaverns.WaveSize).
    ///
    /// The two AFS turrets on the camp's platforms stand down until the camp is cleared (they
    /// would clear it themselves).
    ///
    /// Ids: 571001 on for creatures, creature actions, spawn pools, world objects, crates and the
    /// two map links - the instances' next block after Timora Mines' 569xxx. Plain SQL, valid on SQLite and MySQL alike: every insert is
    /// guarded on its id, every change on what it was, and Down takes out and puts back only
    /// what Up did.
    /// </summary>
    public static class WarnetCavernsContent
    {
        public const uint Map = 1384;
        public const uint Palisades = 1244;
        public const uint FirstId = 571001;

        #region Classes and names

        public const uint WarnetClass = 6262;              // Creature_Warnet_Soldier
        public const uint QueenClass = 6263;               // Creature_Warnet_Queen_Boss
        public const uint MiasmaClass = 6170;              // Creature_Miasma
        public const uint LightbenderClass = 7120;         // Bane_Lightbender_Standard
        public const uint LightbenderBossClass = 10333;    // Bane_Lightbender_Standard_Boss
        public const uint SwapsetMale = 3846, SwapsetFemale = 3848;
        public const uint ServiceNpcClass = 20972;

        public const uint StationClass = 7123;             // UsableNPCCormanComputerV01, "Scientific Remote Research Station"
        public const uint MetalContainerDispenserClass = 7130; // UsableItemDispMetalcontainer
        public const uint LureClass = 7131;                // UsableItemAccQueenlure, "Queen Lure"
        public const uint BioBombClass = 24918;            // UsableItemAccBanePheromoneCanisterV01INCOMPLETE, "Pheromone Canister"
        public const uint HiveClass = 21805;               // UsableCrSpawnerDestWarnetHiveV02, "Hanging Hive"
        public const uint CrateClass = 7861, CrateClass2 = 7863; // UsableTreasureDispHumCrateV01/V02, "Crate"

        /// <summary>Names (creaturenamelanguage).</summary>
        public const uint IrateWarnetName = 7957, MiasmaName = 426, QueenName = 8581, ChelliakName = 10222, TozidName = 10111,
            JoahgName = 10224, BrazixName = 10223, InfiltratorName = 9073, SidesName = 2943, BrumleyName = 2944, GoodingName = 2945,
            GrantName = 10162, LaFontaineName = 10219;

        /// <summary>The mission items: class and the template the client files them under.</summary>
        public const uint WarnetGlandsClass = 7125, WarnetGlandsTemplate = 471;
        public const uint DigestiveFluidsClass = 7127, DigestiveFluidsTemplate = 472;
        public const uint MetalContainerClass = 7128, MetalContainerTemplate = 473;
        public const uint QueenLureClass = 7129, QueenLureTemplate = 474;
        public const uint GooObjectClass = 7132, GooObjectTemplate = 475;
        public const uint OrdersClass = 28441, OrdersTemplate = 118797;
        public const uint ElinorTrophyClass = 28442, ElinorTrophyTemplate = 118798;
        public const uint CavernsTrophyClass = 28443, CavernsTrophyTemplate = 118799;
        public const uint MiasmaEssenceClass = 28444, MiasmaEssenceTemplate = 118800;
        public const uint MiasmaMeatClass = 28445, MiasmaMeatTemplate = 118801;

        /// <summary>The Palisades' warnets (PR105's level-23 row): Lake Elinor's, for Trophy Case.</summary>
        public const uint PalisadesWarnet = 540005;

        /// <summary>The caverns' own first aid stations, medical vendor and the camp's two turrets (ServiceNpcSpawnpoolPreloader, the turrets' seed).</summary>
        public const uint CavernsVendor = 500194;
        public const uint MedicalVendorPackage = 87;
        public static readonly uint[] CampTurretPools = { 550080, 550081 };

        /// <summary>Brumley's conversation package (objectiveconversation 394.2); the stations' (393.3-5).</summary>
        public const uint BrumleyPackage = 143;
        public static readonly uint[] StationPackages = { 789, 790, 791 };

        /// <summary>LaFontaine's line: Mullen keeps it too ("We need to do whatever it takes to hold on to River Base Krimm.").</summary>
        public const uint LaFontaineGreeting = 1474;

        /// <summary>wilderness_v1_thrax_15_19_boss: the crates' kit, the level's gear.</summary>
        public const string CrateLoot = "wilderness_v1_thrax_15_19_boss";

        #endregion

        #region Creatures

        public sealed record ActionRow(uint Id, string Description, uint ActionId, uint ActionArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public const uint ZapL17 = 571001, ZapL18 = 571002, ZapL19 = 571003, ZapLieutenant = 571004, QueenCall = 571005, QueenZap = 571006,
            MiasmaBiteL17 = 571007, MiasmaCoalesceL17 = 571008, MiasmaBiteL18 = 571009, MiasmaCoalesceL18 = 571010,
            JoahgBite = 571011, JoahgCoalesce = 571012, LightbenderRifle = 571013, LightbenderQuill = 571014, BrazixRifle = 571015, BrazixQuill = 571016;

        /// <summary>Bane Miasma's gas cloud (creature_action 56011), as every Miasma row has it.</summary>
        public const uint MiasmaGasCloud = 56011;

        /// <summary>
        /// The Palisades' warnet zap (55006, 42-126 at level 23) and its Queen's (57004/57005 at 28),
        /// Devil's Den's level-19 Miasma (567120/567121), the Lightbender's rifle (30) and Hygax's
        /// quill (54013 at 28), scaled to the level by 1.0905 a level.
        /// </summary>
        public static readonly ActionRow[] Actions =
        {
            new(ZapL17, "Irate Warnet Soldier L17 warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 25, 75, 13),
            new(ZapL18, "Irate Warnet Soldier L18 warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 27, 82, 13),
            new(ZapL19, "Irate Warnet Soldier L19 warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 30, 89, 13),
            new(ZapLieutenant, "Chelliak and Tozid warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 37, 111, 13),
            new(QueenCall, "Queen Warnet Matriarch warnet_queen", 210, 1, 1.0, 40.0, 4000, 1200, 119, 357, 13),
            new(QueenZap, "Queen Warnet Matriarch warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 60, 179, 13),
            new(MiasmaBiteL17, "Miasma Warnet Caverns L17 205/1", 205, 1, 0.5, 3.0, 1000, 660, 30, 60, 3),
            new(MiasmaCoalesceL17, "Miasma Warnet Caverns L17 485/1", 485, 1, 0.0, 10.0, 30000, 0, 40, 50, 3),
            new(MiasmaBiteL18, "Miasma Warnet Caverns L18 205/1", 205, 1, 0.5, 3.0, 1000, 660, 33, 65, 3),
            new(MiasmaCoalesceL18, "Miasma Warnet Caverns L18 485/1", 485, 1, 0.0, 10.0, 30000, 0, 44, 55, 3),
            new(JoahgBite, "Joahg 205/1", 205, 1, 0.5, 3.0, 1000, 660, 45, 89, 3),
            new(JoahgCoalesce, "Joahg 485/1", 485, 1, 0.0, 10.0, 30000, 0, 60, 75, 3),
            new(LightbenderRifle, "Lightbender Infiltrator 1/149", 1, 149, 0.5, 30.0, 4433, 0, 20, 32, 6),
            new(LightbenderQuill, "Lightbender Infiltrator lightbender_quill", 202, 1, 1.0, 10.0, 4000, 2266, 19, 37, 1),
            new(BrazixRifle, "Brazix 1/149", 1, 149, 0.5, 30.0, 4433, 0, 25, 40, 6),
            new(BrazixQuill, "Brazix lightbender_quill", 202, 1, 1.0, 10.0, 4000, 2266, 26, 50, 1)
        };

        public sealed record CreatureRow(uint Id, string Comment, uint ClassId, uint Faction, uint Level, uint MaxHp, uint NameId,
            uint RunSpeed, uint WalkSpeed, uint[] Actions, uint Armor, uint AppearanceFrom = 0, uint Package = 0);

        public const uint WarnetL17 = 571001, WarnetL18 = 571002, WarnetL19 = 571003, MiasmaL17 = 571004, MiasmaL18 = 571005,
            Queen = 571006, Chelliak = 571007, Tozid = 571008, Joahg = 571009, JoahgTwin = 571010, Brazix = 571011, Lightbender = 571012;
        public const uint Sides = 571020, Brumley = 571021, Gooding = 571022, Grant = 571023, LaFontaine = 571024;

        /// <summary>Every warnet of the caverns: what drops their trophies, glands and fluids.</summary>
        public static readonly uint[] CavernsWarnets = { WarnetL17, WarnetL18, WarnetL19, Chelliak, Tozid };

        /// <summary>Every Miasma of the caverns: what drops its essence.</summary>
        public static readonly uint[] CavernsMiasmas = { MiasmaL17, MiasmaL18, Joahg, JoahgTwin };

        /// <summary>
        /// The creatures: health 1446 x 1.0905^(level - 23) for a thug, 1.75 times for a lieutenant,
        /// 2.625 for the Queen; armour as Devil's Den's at the level. The specialists walk where the
        /// story takes them (WarnetCaverns), at the Minos captives' speeds; LaFontaine and Grant stand.
        /// </summary>
        public static readonly CreatureRow[] Creatures =
        {
            new(WarnetL17, "Irate Warnet Soldier L17 - Warnet Caverns", WarnetClass, 0, 17, 860, IrateWarnetName, 9, 5, new[] { ZapL17 }, 62),
            new(WarnetL18, "Irate Warnet Soldier L18 - Warnet Caverns", WarnetClass, 0, 18, 938, IrateWarnetName, 9, 5, new[] { ZapL18 }, 68),
            new(WarnetL19, "Irate Warnet Soldier L19 - Warnet Caverns Nest", WarnetClass, 0, 19, 1023, IrateWarnetName, 9, 5, new[] { ZapL19 }, 74),
            new(MiasmaL17, "Miasma L17 - Warnet Caverns", MiasmaClass, 0, 17, 860, MiasmaName, 9, 5, new[] { MiasmaBiteL17, MiasmaCoalesceL17, MiasmaGasCloud }, 62),
            new(MiasmaL18, "Miasma L18 - Warnet Caverns", MiasmaClass, 0, 18, 938, MiasmaName, 9, 5, new[] { MiasmaBiteL18, MiasmaCoalesceL18, MiasmaGasCloud }, 68),
            new(Queen, "Queen Warnet Matriarch - Warnet Caverns Nest", QueenClass, 0, 19, 2685, QueenName, 9, 5, new[] { QueenCall, QueenZap }, 180),
            new(Chelliak, "Chelliak - Warnet Caverns Research Base Camp", WarnetClass, 0, 19, 1790, ChelliakName, 9, 5, new[] { ZapLieutenant }, 101),
            new(Tozid, "Tozid - Warnet Caverns Bug City", WarnetClass, 0, 19, 1790, TozidName, 9, 5, new[] { ZapLieutenant }, 101),
            new(Joahg, "Joahg - Warnet Caverns (the larger twin)", MiasmaClass, 0, 19, 1790, JoahgName, 9, 5, new[] { JoahgBite, JoahgCoalesce, MiasmaGasCloud }, 101),
            new(JoahgTwin, "Joahg - Warnet Caverns (the smaller twin)", MiasmaClass, 0, 18, 1641, JoahgName, 9, 5, new[] { MiasmaBiteL18, MiasmaCoalesceL18, MiasmaGasCloud }, 92),
            new(Brazix, "Brazix - Warnet Caverns Lightbender squad leader", LightbenderBossClass, 0, 19, 1790, BrazixName, 9, 5, new[] { BrazixRifle, BrazixQuill }, 101),
            new(Lightbender, "Lightbender Infiltrator - Warnet Caverns", LightbenderClass, 0, 18, 938, InfiltratorName, 9, 5, new[] { LightbenderRifle, LightbenderQuill }, 68),
            new(Sides, "Information Spec. Sides - Warnet Caverns", SwapsetMale, 1, 20, 3000, SidesName, 7, 3, Array.Empty<uint>(), 120, AppearanceFrom: 400401),
            new(Brumley, "Information Spec. Brumley - Warnet Caverns", SwapsetMale, 1, 20, 3000, BrumleyName, 7, 3, Array.Empty<uint>(), 120, AppearanceFrom: 400401, Package: BrumleyPackage),
            new(Gooding, "Information Spec. Gooding - Warnet Caverns", SwapsetFemale, 1, 20, 3000, GoodingName, 7, 3, Array.Empty<uint>(), 120, AppearanceFrom: 400402),
            new(Grant, "Field Medic Grant - Warnet Caverns entrance", ServiceNpcClass, 1, 20, 3000, GrantName, 0, 0, Array.Empty<uint>(), 120, AppearanceFrom: 400201),
            new(LaFontaine, "Captain LaFontaine - River-base Krimm", SwapsetFemale, 1, 20, 3000, LaFontaineName, 0, 0, Array.Empty<uint>(), 120, AppearanceFrom: 130)
        };

        #endregion

        #region Places

        /// <summary>A spot on the caverns' navmesh (x, floor height, z).</summary>
        public sealed record Spot(double X, double Y, double Z);

        /// <summary>The map links: the Palisades' cave mouth (uimapmarker 1402's to 1541) and the caverns' way out, each with its arrival clear of the other's trigger.</summary>
        public const uint EntryLink = 571001, ExitLink = 571002;
        public static readonly Spot PalisadesMouth = new(769.5072, 155.2076, 797.6298);
        public static readonly Spot PalisadesArrival = new(762.32, 153.75, 791.09);
        public static readonly Spot CavernsMouth = new(-138.06, 98.18, 23.06);
        public static readonly Spot CavernsArrival = new(-132.2, 101.7, 42.0);
        public const double PalisadesArrivalYaw = 0.767, CavernsArrivalYaw = -1.5708, LinkRadius = 6;

        /// <summary>The specialists' three places: the entrance sandbags, the camp's mouth, their posts in the camp.</summary>
        public static readonly Spot SidesAtEntrance = new(-120.0, 97.54, 48.0), BrumleyAtEntrance = new(-122.0, 98.15, 46.0), GoodingAtEntrance = new(-118.1, 97.22, 40.89);
        public static readonly Spot SidesAtMouth = new(41.8, 87.55, 24.8), BrumleyAtMouth = new(39.0, 85.84, 22.0), GoodingAtMouth = new(45.0, 86.61, 21.0);
        public static readonly Spot SidesAtPost = new(32.82, 97.95, -90.29), BrumleyAtPost = new(68.0, 87.95, -31.0), GoodingAtPost = new(36.0, 98.0, -80.0);

        /// <summary>The three stations of the Research Base Camp (Reset the Research Stations), and Remote Station 3 at the Dig Site (Escort to Remote Station 3).</summary>
        public static readonly Spot[] Stations = { new(29.5, 98.2, -88.5), new(82.5, 98.36, -56.5), new(59.0, 88.11, -37.2) };
        public static readonly double[] StationYaws = { 1.5708, -2.3562, 3.1416 };
        public static readonly Spot RemoteStation = new(155.0, 33.75, 29.5);

        /// <summary>Where the Queen Lure goes (the wiki's red staff by the Nest's mouth) and the bio-bomb (Ten Ton Hammer's 164, 33, 298).</summary>
        public static readonly Spot LureStand = new(262.0, 28.35, 240.0);
        public static readonly Spot BioBomb = new(163.84, 34.18, 298.1);
        public static readonly Spot QueenSpot = new(275.0, 0.75, 334.0);
        public static readonly Spot MetalContainerSpot = new(142.0, 36.76, 49.0);
        public static readonly Spot JoahgSpot = new(148.0, 75.09, 116.0);
        public static readonly Spot BrazixSpot = new(236.0, 28.66, 255.0);
        public static readonly Spot LaFontaineSpot = new(213.0, 110.47, 342.0);

        #endregion

        #region Spawn pools

        public sealed record Pool(uint Id, uint MapContextId, uint Creature, uint Min, uint Max, double X, double Y, double Z, double Rotation,
            double Radius, uint Mode = 0, uint Respawn = 9000, uint Creature2 = 0, uint Min2 = 0, uint Max2 = 0, string Comment = null);

        public const uint Automatic = 0, Scripted = 2;

        /// <summary>Respawn, in tenths of a second: the caverns' creatures 15 minutes; the people at once.</summary>
        public const uint CreatureRespawn = 9000, PersonRespawn = 300;

        public const uint GrantPool = 571001, SidesPool = 571002, BrumleyPool = 571003, GoodingPool = 571004, LaFontainePool = 571005;

        /// <summary>"Clear the way to the Research Site": the entrance tunnel, the first chamber and the way down to the camp's mouth.</summary>
        public static readonly uint[] WayPools = { 571010, 571011, 571012, 571013, 571014 };

        /// <summary>"Clear the Research Site": the big cavern's pools and Chelliak.</summary>
        public static readonly uint[] CampPools = { 571020, 571021, 571022, 571023, 571024, 571025 };
        public const uint ChelliakPool = 571025;

        public const uint JoahgPool = 571032, JoahgTwinPool = 571033, TozidPool = 571054, BrazixPool = 571062, LightbenderPool = 571063, QueenPool = 571064;

        /// <summary>The lure's attack (the lure placed) and the Queen's (her death): a level-19 warnet each, scripted, woken as many as the squad wants.</summary>
        public const uint FirstLureWavePool = 571070, FirstDeathWavePool = 571082, WavePools = 12;

        private static readonly (double X, double Y, double Z)[] LureWave =
        {
            (226, 6.62, 279), (238, 5.13, 267), (238, 6.82, 291), (274, 3.03, 291), (286, 3.04, 267), (286, 8.15, 279),
            (286, 8.73, 291), (298, 9.57, 291), (298, 3.81, 303), (298, 3.02, 315), (310, 8.46, 303), (310, 7.33, 315)
        };

        private static readonly (double X, double Y, double Z)[] DeathWave =
        {
            (226, 12.38, 327), (226, 10.64, 339), (238, 7.81, 327), (238, 7.1, 339), (238, 8.87, 351), (250, 4.03, 327),
            (250, 3.1, 339), (250, 3.09, 351), (274, 4.63, 375), (274, 5.83, 387), (286, 3.55, 387), (298, 7.67, 375)
        };

        public static IEnumerable<Pool> WavePoolRows() =>
            LureWave.Select((w, i) => new Pool(FirstLureWavePool + (uint)i, Map, WarnetL19, 1, 1, w.X, w.Y, w.Z, 0, 2, Scripted,
                    Comment: $"Warnet Caverns Nest: the lure's attack, warnet {i + 1}"))
                .Concat(DeathWave.Select((w, i) => new Pool(FirstDeathWavePool + (uint)i, Map, WarnetL19, 1, 1, w.X, w.Y, w.Z, 0, 2, Scripted,
                    Comment: $"Warnet Caverns Nest: the Queen's death, warnet {i + 1}")));

        public static readonly Pool[] PlacedPools =
        {
            // The people: Grant and the specialists at the entrance sandbags, LaFontaine in the middle of Krimm.
            new(GrantPool, Map, Grant, 1, 1, -118.0, 96.89, 43.0, 2.3562, 0, Respawn: PersonRespawn, Comment: "Field Medic Grant at the entrance sandbags"),
            new(SidesPool, Map, Sides, 1, 1, SidesAtEntrance.X, SidesAtEntrance.Y, SidesAtEntrance.Z, 2.3562, 0, Respawn: PersonRespawn, Comment: "Information Spec. Sides at the entrance sandbags"),
            new(BrumleyPool, Map, Brumley, 1, 1, BrumleyAtEntrance.X, BrumleyAtEntrance.Y, BrumleyAtEntrance.Z, 2.3562, 0, Respawn: PersonRespawn, Comment: "Information Spec. Brumley at the entrance sandbags"),
            new(GoodingPool, Map, Gooding, 1, 1, GoodingAtEntrance.X, GoodingAtEntrance.Y, GoodingAtEntrance.Z, 2.3562, 0, Respawn: PersonRespawn, Comment: "Information Spec. Gooding at the entrance sandbags"),
            new(LaFontainePool, Palisades, LaFontaine, 1, 1, LaFontaineSpot.X, LaFontaineSpot.Y, LaFontaineSpot.Z, 4.7124, 0, Respawn: PersonRespawn, Comment: "Captain LaFontaine in the middle of River-base Krimm"),

            // The way to the camp.
            new(571010, Map, WarnetL17, 2, 2, -60.86, 88.9, 44.75, 0, 5, Comment: "Warnet Caverns: warnets in the entrance tunnel"),
            new(571011, Map, WarnetL17, 2, 2, -27.8, 74.55, 41.4, 0, 5, Comment: "Warnet Caverns: warnets at the foot of the entrance tunnel"),
            new(571012, Map, WarnetL17, 3, 3, 12.4, 73.55, 70.6, 0, 6, Comment: "Warnet Caverns: warnets in the first chamber"),
            new(571013, Map, WarnetL17, 2, 2, 43.6, 83.75, 67.2, 0, 5, Comment: "Warnet Caverns: warnets on the first chamber's ramp"),
            new(571014, Map, WarnetL17, 2, 2, 41.0, 89.77, 30.0, 0, 4, Comment: "Warnet Caverns: warnets at the mouth of the Research Base Camp"),

            // The Research Base Camp.
            new(571020, Map, WarnetL17, 3, 3, 64.6, 83.95, -1.8, 0, 6, Comment: "Warnet Caverns Research Base Camp: warnets below the mouth"),
            new(571021, Map, WarnetL18, 2, 2, 80.0, 85.38, -10.0, 0, 5, Comment: "Warnet Caverns Research Base Camp: warnets on the east slope"),
            new(571022, Map, WarnetL18, 2, 2, 100.4, 93.81, -16.4, 0, 5, Comment: "Warnet Caverns Research Base Camp: warnets on the east ledge"),
            new(571023, Map, WarnetL18, 2, 2, 60.0, 94.55, -120.0, 0, 5, Comment: "Warnet Caverns Research Base Camp: warnets at the south wall"),
            new(571024, Map, WarnetL17, 2, 2, 18.8, 87.51, -60.3, 0, 5, Comment: "Warnet Caverns Research Base Camp: warnets under the west platform"),
            new(ChelliakPool, Map, Chelliak, 1, 1, 55.0, 98.15, -82.0, 0, 0, Comment: "Warnet Caverns Research Base Camp: Chelliak"),

            // The second chamber, the passages.
            new(571030, Map, WarnetL18, 2, 2, 103.8, 64.35, 118.2, 0, 5, Comment: "Warnet Caverns: warnets in the second chamber"),
            new(571031, Map, MiasmaL17, 1, 1, 120.4, 61.75, 131.6, 0, 3, Comment: "Warnet Caverns: a Miasma in the second chamber"),
            new(JoahgPool, Map, Joahg, 1, 1, JoahgSpot.X, JoahgSpot.Y, JoahgSpot.Z, 0, 0, Comment: "Warnet Caverns: Joahg, the larger twin, where the Miasma emanations are"),
            new(JoahgTwinPool, Map, JoahgTwin, 1, 1, 150.0, 74.66, 120.0, 0, 0, Comment: "Warnet Caverns: Joahg, the smaller twin"),
            new(571034, Map, WarnetL18, 2, 2, 190.8, 59.15, 158.0, 0, 5, Comment: "Warnet Caverns Upper Passages: warnets past the sandbags"),
            new(571035, Map, WarnetL18, 2, 2, 190.0, 54.95, 129.6, 0, 5, Comment: "Warnet Caverns Lower Passage: warnets"),
            new(571036, Map, MiasmaL18, 1, 1, 211.6, 43.75, 113.4, 0, 3, Comment: "Warnet Caverns Lower Passage: the first Miasma"),

            // The Dig Site.
            new(571040, Map, WarnetL18, 3, 3, 182.6, 33.75, 41.0, 0, 6, Comment: "Warnet Caverns Dig Site: warnets"),
            new(571041, Map, WarnetL18, 2, 2, 207.0, 34.26, 40.0, 0, 5, Comment: "Warnet Caverns Dig Site: warnets in the east room"),

            // Bug City.
            new(571050, Map, WarnetL18, 2, 2, 248.4, 45.35, 79.4, 0, 5, Comment: "Warnet Caverns Bug City: warnets at the top of the ramp"),
            new(571051, Map, WarnetL18, 3, 3, 297.6, 48.95, 79.6, 0, 6, Comment: "Warnet Caverns Bug City: warnets under the hives"),
            new(571052, Map, WarnetL18, 3, 3, 307.4, 40.35, 100.0, 0, 6, Comment: "Warnet Caverns Bug City: warnets on the east terrace"),
            new(571053, Map, WarnetL18, 2, 2, 274.0, 33.69, 120.0, 0, 5, Creature2: MiasmaL18, Min2: 1, Max2: 1, Comment: "Warnet Caverns Bug City: warnets and a Miasma on the way to the Nest"),
            new(TozidPool, Map, Tozid, 1, 1, 286.0, 49.66, 51.0, 0, 0, Scripted, Comment: "Warnet Caverns Bug City: Tozid, out once the Hanging Hives are down"),

            // The Nest.
            new(571060, Map, WarnetL19, 3, 3, 270.0, 29.44, 250.0, 0, 5, Comment: "Warnet Caverns Nest: warnets at the mouth"),
            new(571061, Map, WarnetL19, 2, 2, 247.96, 29.70, 237.97, 0, 5, Comment: "Warnet Caverns Nest: warnets at the mouth, west"),
            new(BrazixPool, Map, Brazix, 1, 1, BrazixSpot.X, BrazixSpot.Y, BrazixSpot.Z, 0, 0, Comment: "Warnet Caverns Nest: Brazix with the squad's orders"),
            new(LightbenderPool, Map, Lightbender, 3, 3, 240.0, 29.03, 250.0, 0, 4, Comment: "Warnet Caverns Nest: Brazix's Lightbender squad"),
            new(QueenPool, Map, Queen, 1, 1, QueenSpot.X, QueenSpot.Y, QueenSpot.Z, 0, 0, Scripted, Comment: "Warnet Caverns Nest: the Queen Warnet Matriarch, drawn out by the lure")
        };

        public static IEnumerable<Pool> AllPools() => PlacedPools.Concat(WavePoolRows());

        #endregion

        #region World objects

        public sealed record Hive(uint Id, double X, double Y, double Z, double Floor, string Where);

        public const uint FirstHive = 571001;

        /// <summary>The Hanging Hives (arch_forean_warnethive_v02: 6.5 m from its stalk to its tip), hung under Bug City's roof 6.9-7.7 m over the floor: low enough to shoot, and to stir.</summary>
        public static readonly Hive[] Hives =
        {
            new(FirstHive, 263.0, 52.0, 69.0, 44.58, "the north-west chamber"),
            new(FirstHive + 1, 287.0, 59.6, 75.0, 51.91, "the high terrace"),
            new(FirstHive + 2, 306.2, 49.45, 97.0, 41.75, "the east terrace"),
            new(FirstHive + 3, 317.0, 53.3, 87.0, 46.07, "the east alcove"),
            new(FirstHive + 4, 262.94, 51.5, 91.22, 44.55, "the west terrace"),
            new(FirstHive + 5, 311.0, 55.9, 51.0, 48.24, "the south-east terrace")
        };

        /// <summary>What a hive sends out when stirred, and how many.</summary>
        public const uint HiveBrood = WarnetL18, HiveBroodCount = 2;

        public sealed record Usable(uint Id, uint ClassId, Spot At, double Rotation, uint State, uint ItemTemplate, uint ItemClass, uint ItemCount, uint Reset, string Comment);

        public const uint MetalContainerDispenser = 571001, LureAcceptor = 571002;

        /// <summary>USE_ID_STATE_ACTIVE and USE_IA_STATE_ACTIVE.</summary>
        public const uint IdActive = 81, IaActive = 119;

        /// <summary>The lure stand takes the lure again this long after it was last placed: the Queen comes back to a fresh lure.</summary>
        public const uint LureResetSeconds = 120;

        public static readonly Usable[] Usables =
        {
            new(MetalContainerDispenser, MetalContainerDispenserClass, MetalContainerSpot, 0, IdActive, MetalContainerTemplate, 0, 1, 10,
                "Warnet Caverns Dig Site: the metal container by the sandbags (Create the Queen Lure)"),
            new(LureAcceptor, LureClass, LureStand, 0, IaActive, 0, QueenLureClass, 1, LureResetSeconds,
                "Warnet Caverns Nest: where the Queen Lure is placed (Kill the Queen)")
        };

        public sealed record Container(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, string Comment);

        public const uint FirstCrate = 571001;

        public static readonly Container[] Crates =
        {
            new(FirstCrate, CrateClass, 19.0, 97.95, -86.0, 1.5708, "Warnet Caverns Research Base Camp: crate"),
            new(FirstCrate + 1, CrateClass2, 140.0, 37.37, 52.0, 0, "Warnet Caverns Dig Site: crate at the back of the cave"),
            new(FirstCrate + 2, CrateClass, 286.0, 51.38, 16.0, 0, "Warnet Caverns Bug City: crate in the south chamber"),
            new(FirstCrate + 3, CrateClass2, 280.0, 4.82, 394.0, 3.1416, "Warnet Caverns Nest: crate in the north")
        };

        #endregion

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ActionArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {SwapsetMale} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = Enumerable.Range(0, 8).Select(i => i < c.Actions.Length ? c.Actions[i] : 0u).ToArray();

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {c.Faction}, {c.Level}, {c.MaxHp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.MaxHp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {c.Id});";

                if (c.AppearanceFrom != 0)
                    yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                        $"SELECT {c.Id}, a.slot_id, a.Class_id, a.color FROM creature_appearance a WHERE a.id = {c.AppearanceFrom} " +
                        $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {c.Id}) AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {c.Id} AND x.slot_id = a.slot_id);";

                if (c.Package != 0)
                    yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {c.Id}, {c.Package}, {S(c.Comment)} " +
                        $"FROM creature c WHERE c.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {c.Id});";
            }

            yield return $"INSERT INTO vendor (id, package_id) SELECT {Grant}, {MedicalVendorPackage} FROM creature c " +
                $"WHERE c.id = {Grant} AND NOT EXISTS (SELECT 1 FROM vendor x WHERE x.id = {Grant});";
            yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {Grant}, i.item_template_id FROM vendor_item i " +
                $"WHERE i.id = {CavernsVendor} AND EXISTS (SELECT 1 FROM vendor v WHERE v.id = {Grant}) " +
                $"AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {Grant} AND x.item_template_id = i.item_template_id);";

            yield return $"INSERT INTO npc_greeting (id, greeting_id, important) SELECT {LaFontaine}, {LaFontaineGreeting}, 0 FROM creature c " +
                $"WHERE c.id = {LaFontaine} AND NOT EXISTS (SELECT 1 FROM npc_greeting x WHERE x.id = {LaFontaine});";

            foreach (var p in AllPools())
                yield return InsertPool(p);

            // The camp's turrets wait for the camp to be cleared (WarnetCaverns wakes them).
            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", CampTurretPools)}) AND map_context_id = {Map} AND mode = {Automatic};";

            foreach (var h in Hives)
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {h.Id}, {Map}, {HiveClass}, {N(h.X)}, {N(h.Y)}, {N(h.Z)}, 0, 0, {HiveBrood}, {HiveBroodCount}, 0, {S($"Warnet Caverns Bug City: Hanging Hive in {h.Where}")} " +
                    $"FROM creature c WHERE c.id = {HiveBrood} AND EXISTS (SELECT 1 FROM entityclass e WHERE e.id = {HiveClass}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {h.Id});";

            foreach (var u in Usables)
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {u.Id}, {Map}, {u.ClassId}, {N(u.At.X)}, {N(u.At.Y)}, {N(u.At.Z)}, {N(u.Rotation)}, {u.State}, 1, {u.ItemTemplate}, {u.ItemClass}, " +
                    $"{u.ItemCount}, {u.Reset}, 0, 0, 0, 0, 0, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {u.Id});";

            foreach (var c in Crates)
                yield return "INSERT INTO world_container (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, loot_profile_key, cipher_level, reset_seconds, comment) " +
                    $"SELECT {c.Id}, {Map}, {c.ClassId}, {N(c.X)}, {N(c.Y)}, {N(c.Z)}, {N(c.Rotation)}, {S(CrateLoot)}, 0, 0, {S(c.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND EXISTS (SELECT 1 FROM loot_profile l WHERE l.profile_key = {S(CrateLoot)}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM world_container x WHERE x.id = {c.Id});";

            yield return MapLink(EntryLink, Palisades, PalisadesMouth, Map, CavernsArrival, CavernsArrivalYaw, "palisades -> palisades_warnetcaverns");
            yield return MapLink(ExitLink, Map, CavernsMouth, Palisades, PalisadesArrival, PalisadesArrivalYaw, "palisades_warnetcaverns -> palisades");
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM map_link WHERE id IN ({EntryLink}, {ExitLink}) AND (map_context_id = {Map} OR dest_map_context_id = {Map});";
            yield return $"DELETE FROM world_container WHERE id BETWEEN {FirstCrate} AND {FirstCrate + (uint)Crates.Length - 1} AND map_context_id = {Map};";
            yield return $"DELETE FROM world_usable WHERE id BETWEEN {Usables.Min(u => u.Id)} AND {Usables.Max(u => u.Id)} AND map_context_id = {Map};";
            yield return $"DELETE FROM world_destructible WHERE id BETWEEN {FirstHive} AND {FirstHive + (uint)Hives.Length - 1} AND map_context_id = {Map};";

            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", CampTurretPools)}) AND map_context_id = {Map} AND mode = {Scripted};";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {AllPools().Min(p => p.Id)} AND {AllPools().Max(p => p.Id)} AND map_context_id IN ({Map}, {Palisades});";

            yield return $"DELETE FROM npc_greeting WHERE id = {LaFontaine} AND greeting_id = {LaFontaineGreeting};";
            yield return $"DELETE FROM vendor_item WHERE id = {Grant};";
            yield return $"DELETE FROM vendor WHERE id = {Grant} AND package_id = {MedicalVendorPackage};";

            foreach (var c in Creatures.Reverse())
            {
                yield return $"DELETE FROM npc_package WHERE id = {c.Id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {c.Id};";
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            yield return $"DELETE FROM creature_action WHERE id BETWEEN {Actions.Min(a => a.Id)} AND {Actions.Max(a => a.Id)};";
        }

        private static string MapLink(uint id, uint map, Spot at, uint dest, Spot arrival, double yaw, string comment) =>
            "INSERT INTO map_link (id, map_context_id, pos_x, pos_y, pos_z, radius, dest_map_context_id, dest_pos_x, dest_pos_y, dest_pos_z, dest_rotation, kind, enabled, comment) " +
            $"SELECT {id}, {map}, {N(at.X)}, {N(at.Y)}, {N(at.Z)}, {N(LinkRadius)}, {dest}, {N(arrival.X)}, {N(arrival.Y)}, {N(arrival.Z)}, {N(yaw)}, 1, 1, {S(comment)} " +
            $"FROM map_info m WHERE m.map_context_id = {Map} AND NOT EXISTS (SELECT 1 FROM map_link x WHERE x.id = {id}) " +
            $"AND NOT EXISTS (SELECT 1 FROM map_link y WHERE y.map_context_id = {map} AND y.dest_map_context_id = {dest});";

        private static string InsertPool(Pool p) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"SELECT {p.Id}, {p.Mode}, 0, {p.Respawn}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.MapContextId}, " +
            $"{p.Creature}, {p.Min}, {p.Max}, {p.Creature2}, {p.Min2}, {p.Max2}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(p.Radius)} " +
            $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
