using System.Collections.Generic;
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
    using static AliaDasNpcs;

    /// <summary>
    /// The Kardash Atta Colony (adv_arieki_torden_plains_attacolony, 1773), entered from the
    /// Torden Plains west of Irendas, as Ten Ton Hammer's Kardash Atta Colony guide tells it: its
    /// people, its Atta, the Bane in the Brann outposts and its ten missions, in the client's own
    /// names and texts. It is what opens the Phanin Research Facility.
    ///
    /// Who and what:
    ///  - In Irendas: Captain Reyko outside the command tower, Agent Zim and Dr. Finobee in the
    ///    hospital, Researcher Erodan outside the large building and Engineer Tralos south of it,
    ///    at the guide's coordinates. The four already placed stood together on a patch of ground
    ///    joined to nothing (383, 433, -100), at a map marker: they are moved, and Tralos, a Brann,
    ///    is given the Brann body and look the other Brann NPCs have. Erodan had no row.
    ///  - In the colony: Lieutenant Donners just inside, off the hospital medic she stood in;
    ///    Surveyor Miras, Researcher Kranix and Engineer Reshi in Research Outpost Alpha, with the
    ///    console Into the Hive is reported from beside Kranix.
    ///  - The colony's Atta at the instance's levels, 20 to 23, the Queen 25: its 42 pools move from
    ///    the Torden Plains' level 35 and 36 rows to Atta of their own, as the Ojasa hive's did.
    ///    The guide's named Atta: Champion Boritak by the Eloh cache, Foreman Exicorium in the
    ///    Inner Nest, Foreman Akikorik at the Royal Nest's teleporter, Champion Olirex on the Royal
    ///    Nest's west side, and Queen Kardash on her throne with Escort Rendash and Nurse Trendal.
    ///  - Lightbender Infiltrators at the five Brann outposts, an Elite Infiltrator at four of
    ///    them: Arkshaz at Beta, Morkone at Gamma, Dargoth at Delta, Kresht at Epsilon.
    /// The five Atta Mind Control Transducers, the console, the radio offer of Atta Control, the
    /// Bane at the Queen and the scenario events the missions wait for are the server's, per copy
    /// of the colony (KardashAttaColony).
    ///
    /// Health and damage follow the Ojasa hive's Atta down the levels: a level's thug is
    /// 7071 x 1.0905^(level - 38), a minion three quarters of it, a lieutenant one and three
    /// quarters, and damage by the same factor; the Bane follow Pravus: 10000 x 2^((level - 42) / 8),
    /// armour ten a level less twenty.
    /// </summary>
    public static class KardashAttaColonyContent
    {
        public const string Revision = "kardash_1_16_5";
        public const uint Colony = 1773, Plains = 1764;

        /// <summary>missioncategorylanguage: "Instance (Kardash Atta Colony)".</summary>
        public const uint Category = 10000029;

        public const uint IntoTheHive = 1112, AttaControl = 1113, BugHunt = 1115, ADividedPeople = 1118, TaleOfTwoBrothers = 1119,
            UnexpectedGuests = 1120, JustToBeCertain = 1121, KeyInformation = 1122, SecurityThreat = 1125, BugHugger = 1939;

        /// <summary>Reyko's Eloh artifact, put in later under this revision (KardashWaypointsAndMysteries).</summary>
        public const uint UndergroundMysteries = 1186;

        /// <summary>The ten this migration puts in.</summary>
        public static readonly uint[] OwnMissions =
        {
            IntoTheHive, AttaControl, BugHunt, ADividedPeople, TaleOfTwoBrothers, UnexpectedGuests, JustToBeCertain, KeyInformation,
            SecurityThreat, BugHugger
        };

        /// <summary>Every mission under the colony's revision: its own ten and Underground Mysteries.</summary>
        public static readonly uint[] Missions = OwnMissions.Append(UndergroundMysteries).ToArray();

        /// <summary>The server event Atta Control is offered by radio on (KardashAttaColony).</summary>
        public const string AttaControlOfferKey = "kardash.atta-control";

        // The NPC packages the missions' conversations are in (objectiveconversation, and Trick or
        // Treat!'s for Erodan).
        public const uint ReykoPackage = 1200, ZimPackage = 1201, ErodanPackage = 1202, MirasPackage = 1203, TralosPackage = 1204,
            ReshiPackage = 1207, ConsolePackage = 1213, DonnersPackage = 2457;

        // The people already in the world (MissionNpcCreaturePreloader; pool id is the row's).
        public const uint Zim = 510082, Reyko = 510083, Finobee = 510086, Tralos = 510088, Donners = 510096;

        // New people.
        public const uint Miras = 781001, Kranix = 781002, Reshi = 781003, Erodan = 781004;

        // The colony's Atta and Bane.
        public const uint Soldier = 781010, Worker = 781011, Guardian = 781012, Grub = 781013;
        public const uint Boritak = 781020, Olirex = 781021, Akikorik = 781022, Exicorium = 781023, Queen = 781024, Rendash = 781025,
            Trendal = 781026;
        public const uint Infiltrator = 781030, QueenRaider = 781031;
        public const uint Arkshaz = 781032, Morkone = 781033, Dargoth = 781034, Kresht = 781035;

        public static readonly uint[] KardashAtta = { Soldier, Worker, Guardian, Grub };
        public static readonly uint[] Elites = { Arkshaz, Morkone, Dargoth, Kresht };

        /// <summary>The Torden Plains rows the colony's pools stood on, and what they go over to.</summary>
        public const uint PlainsAttaHarvester = 531073, PlainsAttaSoldier = 531080, PlainsAttaGrub = 531081;
        public static readonly (uint From, uint To)[] Repointed =
        {
            (PlainsAttaSoldier, Soldier), (PlainsAttaHarvester, Worker), (PlainsAttaGrub, Grub)
        };

        /// <summary>The Brann NPCs' one look (Viddea Scientist Eugin's, and every other Brann NPC's).</summary>
        public const uint BrannLooksLike = 510036;
        public const uint BrannSwapsetMale = 7776;

        /// <summary>UsableNPCBrannKeyboardV01: the console Into the Hive is reported from.</summary>
        public const uint ConsoleClass = 24660;

        /// <summary>UsableInertDestBrannMachineryv01: a transducer, beside each outpost's mind control console.</summary>
        public const uint TransducerClass = 24050;
        public const uint TransducerHitPoints = 1500;

        // Mission items (class, template).
        public const uint AttaBrainClass = 24720, AttaBrainTemplate = 50307;
        public const uint BoritakBrainClass = 24721, BoritakBrainTemplate = 50306;
        public const uint OlirexBrainClass = 26443, OlirexBrainTemplate = 111188;
        public const uint AkikorikBrainClass = 26444, AkikorikBrainTemplate = 111186;
        public const uint ExicoriumBrainClass = 26445, ExicoriumBrainTemplate = 111187;
        public const uint QueenBrainClass = 26719, QueenBrainTemplate = 117044;
        public const uint DataDiscClass = 24722, DataDiscTemplate = 50309;
        public const uint PackageTemplate = 50308;
        public const uint KeycardClass = 24724, KeycardTemplate = 50310;

        /// <summary>How many Atta brains Zim wants, and how many plain infiltrators Miras wants dead.</summary>
        public const uint BrainsWanted = 10, InfiltratorsWanted = 12;

        /// <summary>How often an Atta carries a brain sample worth having, and a Bane infiltrator a data disc (percent).</summary>
        public const int BrainChance = 33, DataDiscChance = 35;

        // The scenarios the colony records, and their events.
        public const uint ColonyScenario = 1, ColonyFound = 1;
        public const uint TransducerScenario = 1, AllTransducersScenario = 2, AllTransducersDown = 1;
        public const uint ExploreScenario = 1, HarvestGrotto = 1, InnerNest = 2, RoyalNest = 3, QueensChamber = 4, QueenProtected = 5;
        public const uint HuggerDoneScenario = 2, HuggerDone = 1;

        // Objectives the Game watches.
        public const uint FindColony = 3, SpeakWithDonners = 10;
        public const uint TransducerCounter = 6, ReportToReyko = 7;
        public const uint ExploreGrotto = 3, ExploreInnerNest = 4, ExploreRoyalNest = 5, ExploreQueensChamber = 6, ProtectQueen = 7,
            TalkToDonners = 8, HuggerCounter = 9, TalkToReshi = 10;

        /// <summary>missionobjectiveindicatorlanguage: Kardash Queens Chamber.</summary>
        public const uint QueensChamberMarker = 409;

        // Where things are (x, height, z), on the navmesh.
        public static readonly Vector3 Arrival = new(-25.749f, 312.1235f, 184.0261f);
        public static readonly Vector3 DonnersAt = new(-20f, 314.27f, 176f);
        public static readonly Vector3 MirasAt = new(8f, 272.63f, -208f);
        public static readonly Vector3 KranixAt = new(20f, 272.73f, -198f);
        public static readonly Vector3 ReshiAt = new(-6f, 272.63f, -198f);
        public static readonly Vector3 Console = new(22f, 272.7f, -202.5f);
        public const float ConsoleYaw = -1.5707964f;

        public static readonly Vector3 ErodanAt = new(254f, 433.37f, -145.4f);

        /// <summary>A transducer: where it stands (beside the outpost's PropBrannComputerAttaMindControlV01) and its outpost.</summary>
        public sealed record Transducer(uint Objective, string Outpost, Vector3 At);

        public static readonly Transducer[] Transducers =
        {
            new(1, "Research Outpost Alpha", new Vector3(24.6f, 272.71f, -199.2f)),
            new(2, "Research Outpost Beta", new Vector3(447.5f, 188.63f, 300.5f)),
            new(3, "Research Outpost Gamma", new Vector3(-158f, 224.63f, 227.2f)),
            new(4, "Research Outpost Delta", new Vector3(332.47f, 100.63f, -438.22f)),
            new(5, "Research Outpost Epsilon", new Vector3(-229.94f, 110.23f, -47.06f))
        };

        /// <summary>
        /// The places Bug Hugger sends a player to, as the map's regions have them (map_region 163,
        /// 171, 169 and 20412), with the height of the cavern each is: the regions themselves reach
        /// every height, and the Harvest Grotto's circle and the Inner Nest's overlap one above the
        /// other.
        /// </summary>
        public sealed record Place(uint Event, string Name, Vector3 Middle, float Radius, float HalfX, float HalfZ, float MinY, float MaxY)
        {
            public bool Contains(Vector3 at) =>
                at.Y >= MinY && at.Y <= MaxY && (Radius > 0
                    ? Vector2.Distance(new Vector2(at.X, at.Z), new Vector2(Middle.X, Middle.Z)) <= Radius
                    : System.Math.Abs(at.X - Middle.X) <= HalfX && System.Math.Abs(at.Z - Middle.Z) <= HalfZ);
        }

        public static readonly Place[] Places =
        {
            new(HarvestGrotto, "the Harvest Grotto", new Vector3(351.2f, 191.3f, -6.6f), 176f, 0, 0, 160f, 240f),
            new(InnerNest, "the Inner Nest", new Vector3(419.3f, 108.2f, -413.7f), 200f, 0, 0, 85f, 150f),
            new(RoyalNest, "the Royal Nest", new Vector3(-297.8f, 44.3f, -314.1f), 200f, 0, 0, 0f, 100f),
            new(QueensChamber, "the Queen's Chamber", new Vector3(-318.5f, 30f, 198f), 0f, 125.5f, 135f, 30f, 110f)
        };

        public static readonly Vector3 Throne = new(-349f, 45.18f, 239f);
        public static readonly Vector3 RendashAt = new(-330f, 39.42f, 220f);
        public static readonly Vector3 TrendalAt = new(-339.69f, 45.63f, 232f);

        /// <summary>Where the Bane troubling the Queen come in for Bug Hugger: the chamber floor before her throne.</summary>
        public static readonly Vector3[] RaiderSpots = { new(-340f, 41.13f, 210f), new(-334f, 40.5f, 214f), new(-346f, 41.5f, 206f), new(-338f, 41f, 204f) };

        public static readonly Vector3 BoritakAt = new(-100f, 192.03f, 324f);
        public static readonly Vector3 ExicoriumAt = new(403f, 131.85f, -364f);
        public static readonly Vector3 AkikorikAt = new(-82.41f, 67.16f, -281.13f);
        public static readonly Vector3 OlirexAt = new(-401f, 50.83f, -292f);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1, Neutral = 3 }

        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A spawn pool: its creatures (row, least, most), where and which way, and its respawn time in tenths of a second.</summary>
        public sealed record Spot(uint Id, uint Map, double X, double Y, double Z, double Rotation, uint RespawnTenths, double Radius,
            params (uint Creature, uint Min, uint Max)[] Slots);

        // Existing actions the new Atta keep: the Incline harvesters' pheromone and war cry, which do no damage.
        public const uint HarvesterPheromone = 56007, HarvesterWarcry = 61056, GrubCocoon = 65039;

        private const uint Melee = 478, Spit = 479, Rock = 292, HarvesterSpit = 475, RockWindup = 1366;
        private const uint BeamAttack = 1, BeamArg = 149;

        public static readonly Act[] Actions =
        {
            new(781101, "Kardash Soldier atta_soldier_melee", Melee, 1, 0.5, 4, 1000, 0, 77, 154, 1),
            new(781102, "Kardash Soldier atta_soldier_acid_spit", Spit, 1, 1, 10, 5000, 0, 34, 69, 4),
            new(781103, "Kardash Soldier atta_soldier_rock_throw", Rock, 1, 1, 50, 3000, RockWindup, 77, 154, 1),
            new(781104, "Kardash Worker atta_harvester_acid_spit", HarvesterSpit, 1, 1, 20, 5000, 0, 35, 49, 4),
            new(781117, "Kardash Grub atta_grub_melee", 499, 1, 0.5, 2, 1000, 433, 39, 78, 1),
            new(781105, "Kardash Guardian atta_soldier_melee", Melee, 1, 0.5, 4, 1000, 0, 84, 168, 1),
            new(781106, "Kardash Guardian atta_soldier_acid_spit", Spit, 1, 1, 10, 5000, 0, 37, 76, 4),
            new(781107, "Kardash Guardian atta_soldier_rock_throw", Rock, 1, 1, 50, 3000, RockWindup, 84, 168, 1),
            new(781108, "Kardash Champion atta_soldier_melee", Melee, 1, 0.5, 4, 1000, 0, 115, 229, 1),
            new(781109, "Kardash Champion atta_soldier_acid_spit", Spit, 1, 1, 10, 5000, 0, 50, 104, 4),
            new(781110, "Kardash Champion atta_soldier_rock_throw", Rock, 1, 1, 50, 3000, RockWindup, 115, 229, 1),
            new(781111, "Kardash Foreman atta_harvester_acid_spit", HarvesterSpit, 1, 1, 20, 4000, 0, 53, 74, 4),
            new(781112, "Kardash Nurse atta_harvester_acid_spit", HarvesterSpit, 1, 1, 20, 4000, 0, 58, 80, 4),
            new(781113, "Queen Kardash acid spit", HarvesterSpit, 1, 1, 30, 4000, 0, 86, 120, 4),
            new(781114, "Queen Kardash rock throw", Rock, 1, 1, 50, 6000, RockWindup, 150, 300, 1),
            new(781115, "Lightbender Infiltrator beam 1/149", BeamAttack, BeamArg, 0.5, 40, 2500, 0, 60, 90, 6),
            new(781116, "Elite Infiltrator beam 1/149", BeamAttack, BeamArg, 0.5, 40, 2500, 0, 75, 110, 6)
        };

        private const uint SoldierClass = 7081, HarvesterClass = 7078, SoldierBossClass = 22627, HarvesterBossClass = 24592,
            QueenClass = 7079, LightbenderClass = 7120, LightbenderBossClass = 10333;

        public static readonly Beast[] Creatures =
        {
            new(Miras, "Surveyor Miras - Kardash Research Outpost Alpha", BrannSwapsetMale, TargetCategoryRow.Friendly, 22, 3000, 0, 9010, 0, 0),
            new(Kranix, "Researcher Kranix - Kardash Research Outpost Alpha", BrannSwapsetMale, TargetCategoryRow.Friendly, 22, 3000, 0, 9012, 0, 0),
            new(Reshi, "Engineer Reshi - Kardash Research Outpost Alpha", BrannSwapsetMale, TargetCategoryRow.Friendly, 22, 3000, 0, 9014, 0, 0),
            new(Erodan, "Researcher Erodan - Irendas Colony", BrannSwapsetMale, TargetCategoryRow.Friendly, 22, 3000, 0, 9009, 0, 0),
            new(Soldier, "Kardash Soldier", SoldierClass, TargetCategoryRow.Hostile, 22, 1769, 80, 9055, 9, 5, 781101, 781102, 781103),
            new(Worker, "Kardash Worker", HarvesterClass, TargetCategoryRow.Hostile, 21, 1217, 50, 9054, 9, 5, 781104, HarvesterPheromone, HarvesterWarcry),
            new(Grub, "Kardash Grub", 29089, TargetCategoryRow.Hostile, 20, 1115, 45, 0, 9, 5, 781117, GrubCocoon),
            new(Guardian, "Kardash Guardian", SoldierClass, TargetCategoryRow.Hostile, 23, 1929, 85, 9050, 9, 5, 781105, 781106, 781107),
            new(Boritak, "Champion Boritak - Kardash", SoldierBossClass, TargetCategoryRow.Hostile, 24, 3680, 110, 9053, 9, 5, 781108, 781109, 781110),
            new(Olirex, "Champion Olirex - Kardash", SoldierBossClass, TargetCategoryRow.Hostile, 24, 3680, 110, 9575, 9, 5, 781108, 781109, 781110),
            new(Akikorik, "Foreman Akikorik - Kardash", HarvesterBossClass, TargetCategoryRow.Hostile, 23, 3376, 105, 9573, 9, 5, 781111, HarvesterPheromone, HarvesterWarcry),
            new(Exicorium, "Foreman Exicorium - Kardash", HarvesterBossClass, TargetCategoryRow.Hostile, 23, 3376, 105, 9574, 9, 5, 781111, HarvesterPheromone, HarvesterWarcry),
            new(Queen, "Queen Kardash", QueenClass, TargetCategoryRow.Hostile, 25, 12000, 150, 9057, 0, 0, 781113, 781114),
            new(Rendash, "Escort Rendash - Kardash", SoldierBossClass, TargetCategoryRow.Hostile, 24, 3680, 110, 9576, 9, 5, 781108, 781109, 781110),
            new(Trendal, "Nurse Trendal - Kardash", HarvesterBossClass, TargetCategoryRow.Hostile, 24, 3680, 100, 9577, 9, 5, 781112, HarvesterPheromone, HarvesterWarcry),
            new(Infiltrator, "Lightbender Infiltrator - Kardash", LightbenderClass, TargetCategoryRow.Hostile, 21, 1621, 190, 9073, 9, 5, 781115),
            new(QueenRaider, "Lightbender Infiltrator - Kardash Queen", LightbenderClass, TargetCategoryRow.Hostile, 21, 1621, 190, 9073, 9, 5, 781115),
            new(Arkshaz, "Elite Infiltrator Arkshaz - Kardash Beta", LightbenderBossClass, TargetCategoryRow.Hostile, 22, 3094, 200, 9074, 9, 5, 781116),
            new(Morkone, "Elite Infiltrator Morkone - Kardash Gamma", LightbenderBossClass, TargetCategoryRow.Hostile, 22, 3094, 200, 9075, 9, 5, 781116),
            new(Dargoth, "Elite Infiltrator Dargoth - Kardash Delta", LightbenderBossClass, TargetCategoryRow.Hostile, 22, 3094, 200, 9077, 9, 5, 781116),
            new(Kresht, "Elite Infiltrator Kresht - Kardash Epsilon", LightbenderBossClass, TargetCategoryRow.Hostile, 22, 3094, 200, 9076, 9, 5, 781116)
        };

        public const uint NpcRespawn = 200, HostileRespawn = 3000, NamedRespawn = 2700;

        public const uint MirasPool = 781201, KranixPool = 781202, ReshiPool = 781203, ErodanPool = 781204;

        public static readonly Spot[] Pools =
        {
            new(MirasPool, Colony, MirasAt.X, MirasAt.Y, MirasAt.Z, 0, NpcRespawn, 0, (Miras, 1, 1)),
            new(KranixPool, Colony, KranixAt.X, KranixAt.Y, KranixAt.Z, -1.5708, NpcRespawn, 0, (Kranix, 1, 1)),
            new(ReshiPool, Colony, ReshiAt.X, ReshiAt.Y, ReshiAt.Z, 1.5708, NpcRespawn, 0, (Reshi, 1, 1)),
            new(ErodanPool, Plains, ErodanAt.X, ErodanAt.Y, ErodanAt.Z, 0, NpcRespawn, 0, (Erodan, 1, 1)),

            // The named Atta.
            new(781210, Colony, BoritakAt.X, BoritakAt.Y, BoritakAt.Z, 1.6, NamedRespawn, 5, (Boritak, 1, 1)),
            new(781211, Colony, ExicoriumAt.X, ExicoriumAt.Y, ExicoriumAt.Z, 0.6, NamedRespawn, 4, (Exicorium, 1, 1), (Soldier, 2, 2)),
            new(781212, Colony, AkikorikAt.X, AkikorikAt.Y, AkikorikAt.Z, 2.2, NamedRespawn, 4, (Akikorik, 1, 1), (Guardian, 2, 2)),
            new(781213, Colony, OlirexAt.X, OlirexAt.Y, OlirexAt.Z, 0.8, NamedRespawn, 4, (Olirex, 1, 1)),
            new(781214, Colony, Throne.X, Throne.Y, Throne.Z, 2.4, NamedRespawn, 0, (Queen, 1, 1)),
            new(781215, Colony, RendashAt.X, RendashAt.Y, RendashAt.Z, 2.4, NamedRespawn, 0, (Rendash, 1, 1), (Guardian, 2, 2)),
            new(781216, Colony, TrendalAt.X, TrendalAt.Y, TrendalAt.Z, 2.4, NamedRespawn, 0, (Trendal, 1, 1)),

            // The Bane at the outposts: Alpha's on the bridge before its door, the rest inside.
            new(781220, Colony, 4, 272.06, -146, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781221, Colony, 11.98, 272.03, -145.64, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781222, Colony, 8, 272.03, -150, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781223, Colony, 428, 188.63, 290, 1.6, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781224, Colony, 444, 188.63, 290, -1.6, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781225, Colony, 436, 188.63, 300, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781226, Colony, 428, 188.63, 295, 1.6, HostileRespawn, 0, (Arkshaz, 1, 1)),
            new(781227, Colony, -165, 224.63, 210, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781228, Colony, -172, 224.63, 220, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781229, Colony, -160, 224.63, 225, 3.1, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781230, Colony, -172, 224.76, 185, 0, HostileRespawn, 0, (Morkone, 1, 1)),
            new(781231, Colony, 344, 100.63, -430, 1.6, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781232, Colony, 336, 100.63, -436, 1.6, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781233, Colony, 344, 100.63, -446, 1.6, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781234, Colony, 352, 100.63, -436, 1.6, HostileRespawn, 0, (Dargoth, 1, 1)),
            new(781235, Colony, -222, 110.23, -62, 2.4, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781236, Colony, -230.49, 110.23, -47.6, 2.4, HostileRespawn, 0, (Infiltrator, 1, 1)),
            new(781237, Colony, -225.69, 110.23, -58, 2.4, HostileRespawn, 0, (Kresht, 1, 1))
        };

        /// <summary>
        /// The people already in the world who stood where nothing is: (pool, creature, from, to).
        /// Each pool's id is its creature's (MissionNpcCreaturePreloader).
        /// </summary>
        public static readonly (uint Pool, Vector3 From, Vector3 To, double Rotation)[] Moved =
        {
            (Reyko, new Vector3(387.6f, 433.2f, -100.8f), new Vector3(323.9f, 433.54f, -60.5f), 3.1),
            (Zim, new Vector3(382.3f, 433.2f, -103.9f), new Vector3(209f, 425.6f, -212f), 1.6),
            (Finobee, new Vector3(383f, 433.2f, -98.5f), new Vector3(211f, 425.6f, -228f), 1.6),
            (Tralos, new Vector3(377f, 433.4f, -100.8f), new Vector3(264.6f, 433.2f, -236.1f), 0),
            (Donners, new Vector3(-12.9f, 313.45f, 181.3f), DonnersAt, 0.6)
        };

        /// <summary>Tralos's human swapset class and look, as MissionNpcCreaturePreloader seeded them.</summary>
        public const uint HumanSwapsetMale = 3846;

        public static readonly Slot[] TralosHumanLook =
        {
            new(2, 4021, 4294934528), new(13, 6271, 1), new(14, 9781, 4278655809), new(15, 4023, 4294934528),
            new(16, 4022, 4294934528), new(17, 24008, 4286690539)
        };

        public static readonly Slot[] BrannLook =
        {
            new(2, 7259, 4286611584), new(3, 7258, 4286611584), new(15, 7256, 4286611584), new(16, 7257, 4286611584), new(17, 7690, 1)
        };

        /// <summary>Who speaks which package.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Miras, MirasPackage, "Surveyor Miras"),
            (Reshi, ReshiPackage, "Engineer Reshi"),
            (Erodan, ErodanPackage, "Researcher Erodan")
        };

        /// <summary>The people this puts in the world, who take the Brann look.</summary>
        public static readonly uint[] NewPeople = { Miras, Kranix, Reshi, Erodan };

        #region Up and Down

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {SoldierClass} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = c.Actions.Concat(Enumerable.Repeat(0U, 8)).Take(8).ToArray();
                var actionsThere = string.Concat(c.Actions.Select(id => $" AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {id})"));

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId}{actionsThere} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            foreach (var person in NewPeople)
                foreach (var sql in Change(person, new Slot[0], BrannLook))
                    yield return sql;

            // Tralos is Brann: the Brann body and look, while he has the human ones he was seeded with.
            yield return $"UPDATE creature SET class_id = {BrannSwapsetMale} WHERE id = {Tralos} AND class_id = {HumanSwapsetMale};";
            foreach (var sql in Change(Tralos, TralosHumanLook, BrannLook))
                yield return sql;

            foreach (var (creature, package, comment) in Packages)
                yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {creature}, {package}, {S(comment)} " +
                    $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {creature});";

            foreach (var p in Pools)
            {
                var slots = p.Slots.Concat(Enumerable.Repeat((Creature: 0U, Min: 0U, Max: 0U), 6)).Take(6).ToArray();
                var there = string.Concat(p.Slots.Skip(1).Select(s => $" AND EXISTS (SELECT 1 FROM creature x WHERE x.id = {s.Creature})"));

                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Id}, 0, 0, {p.RespawnTenths}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.Map}, " +
                    string.Join(", ", slots.Select(s => $"{s.Creature}, {s.Min}, {s.Max}")) + $", {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Slots[0].Creature}{there} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";
            }

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {to} WHERE map_context_id = {Colony} AND creature_{slot}_Id = {from} " +
                        $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {to});";

            // Each moves only from where it was seeded: one a game master has moved stays.
            foreach (var (pool, from, to, rotation) in Moved)
                yield return $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, rotation = {N(rotation)} " +
                    $"WHERE id = {pool} AND {Near(from)};";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var (pool, from, to, _) in Moved)
                yield return $"UPDATE spawnpool SET pos_x = {N(from.X)}, pos_y = {N(from.Y)}, pos_z = {N(from.Z)}, rotation = 0 " +
                    $"WHERE id = {pool} AND {Near(to)};";

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {from} WHERE map_context_id = {Colony} AND creature_{slot}_Id = {to};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Slots[0].Creature};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            foreach (var sql in Change(Tralos, BrannLook, TralosHumanLook))
                yield return sql;
            yield return $"UPDATE creature SET class_id = {HumanSwapsetMale} WHERE id = {Tralos} AND class_id = {BrannSwapsetMale};";

            yield return $"DELETE FROM creature_appearance WHERE id IN ({string.Join(", ", NewPeople)});";

            foreach (var c in Creatures)
            {
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id};";
        }

        /// <summary>A pool still at a spot, to the centimetre.</summary>
        private static string Near(Vector3 at) =>
            $"ABS(pos_x - {N(at.X)}) < 0.05 AND ABS(pos_y - {N(at.Y)}) < 0.05 AND ABS(pos_z - {N(at.Z)}) < 0.05";

        #endregion

        #region Missions

        private const string Guide = "https://www.tentonhammer.com/guides/kardash-atta-colony-guide";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            IntoTheHiveRows(sql);
            AttaControlRows(sql);
            BugHuntRows(sql);
            BugHuggerRows(sql);
            ADividedPeopleRows(sql);
            TaleOfTwoBrothersRows(sql);
            UnexpectedGuestsRows(sql);
            JustToBeCertainRows(sql);
            KeyInformationRows(sql);
            SecurityThreatRows(sql);
            return sql;
        }

        // Rewards: the guide's credits and gear where it names them, the experience ours. Each
        // piece is the uncommon of its kind nearest level 22, with the module its brand names at
        // [3], the band of level 21-30 gear: CryoGen Resist Ice, Teleract Armor Absorption,
        // ChiTech Power Regen Bonus, Vextronics the debuff of the weapon's own damage.
        public const uint ResistIce = 100042, ArmorAbsorption = 900182, PowerRegen = 900202, DebuffFire = 900242, DebuffPhysical = 900222;
        public const uint StealthLegs = 39141;           // Armor_T3_Stealth_V02_UNC_Legs_20_to_24
        public const uint IncendiaryLeechGun = 47720;    // Weapon_Avatar_LeechGun_Fire_UNC_24_to_28: no fire leech gun is nearer
        public const uint MotorAssistGloves = 29483;     // Armor_T1_MotorAssist_V05_UNC_Gloves_18_to_22
        public const uint BioVest = 19216;               // Armor_T3_Bio_V02_UNC_Vest_20_to_24
        public const uint GravitonHelmet = 23758;        // Armor_T3_Graviton_V02_UNC_Helmet_20_to_24
        public const uint Shotgun = 120204;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_22_to_26

        private static InstanceMissionSql.RewardItem Item(uint template, uint quantity = 1, uint module = 0) => new(template, quantity, module);

        /// <summary>Reyko's: the console in Research Outpost Alpha (package 1213), then the report by radio.</summary>
        private static void IntoTheHiveRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, IntoTheHive, "Into The Hive", 7645, Reyko, null, 20, Category);
            mission.Reward(12000, 1800);
            mission.RadioTurnIn();
            mission.Objective(1, 8539, 8540, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            // An object's conversation is the object's interaction (MissionApplication.TryApplyDialogue).
            mission.Progress(1, MissionProgressEventKind.InteractionUsed, ConsoleClass);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Marker(mission, scene, 1, Colony, Console, 4);
            Evidence(mission, "Captain Reyko1200 outside the command tower gives it. The console beside Kranix in Research Outpost Alpha " +
                "(UsableNPCBrannKeyboardV01, package1213: 'This console appears to have an active communications system') is the " +
                "report; its finishing text is the console's upload, so it is turned in by radio. Atta Control is offered by radio " +
                "next, while the player is in the colony (KardashAttaColony).");
            mission.Enable(scene);
        }

        /// <summary>The five transducers, then Reyko (package 1200).</summary>
        private static void AttaControlRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, AttaControl, "Atta Control", 7650, null, Reyko, 20, Category);
            mission.Reward(20000, 3000);
            mission.Prerequisite(IntoTheHive);
            var names = new uint[] { 9092, 12338, 12340, 12342, 12344 };
            var bodies = new uint[] { 9093, 12339, 12341, 12343, 12345 };
            var scene = Scene(events: true);

            foreach (var t in Transducers)
            {
                mission.Objective(t.Objective, names[t.Objective - 1], bodies[t.Objective - 1], t.Objective, MissionObjectiveState.Incomplete);
                mission.Transition(t.Objective);
                mission.Progress(t.Objective, MissionProgressEventKind.ScenarioEvent, t.Objective, counter: TransducerScenario);
                Marker(mission, scene, t.Objective, Colony, t.At, 8);
            }

            mission.Objective(TransducerCounter, 12738, 12739, 6, MissionObjectiveState.Incomplete);
            mission.Objective(ReportToReyko, 12741, 12742, 7);
            mission.Transition(TransducerCounter);
            mission.Progress(TransducerCounter, MissionProgressEventKind.ScenarioEvent, AllTransducersDown, counter: AllTransducersScenario);
            mission.RevealAndActivate(TransducerCounter, ReportToReyko);

            mission.Transition(ReportToReyko);
            mission.Conversation(ReportToReyko, ReykoPackage);
            mission.Action(ReportToReyko, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Scenario(TransducerScenario, "The Atta Mind Control Transducers", 1, 2, 3, 4, 5);
            mission.Scenario(AllTransducersScenario, "Every transducer down", AllTransducersDown);
            Evidence(mission, "Offered by radio once Into the Hive is done, to a player in the colony. One Atta Mind Control Transducer " +
                "(UsableInertDestBrannMachineryv01, 1500 hit points) beside the mind control console of each Brann outpost - Alpha, " +
                "Beta, Gamma, Delta, Epsilon, the guide's #1-#5; each destroyed is its objective for everyone on it, and stays down in " +
                "the copy. Objective6 is the client's hidden counter: all five done, it shows the report to Reyko1200.");
            scene.HiddenObjectiveIds = new List<uint> { TransducerCounter };
            mission.Channels(MissionChannel.Radio, MissionChannel.Npc,
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, AttaControlOfferKey, Colony));
            mission.Enable(scene);
        }

        /// <summary>The bosses of Zim's list, each with its brain item, and the brains of ten more.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Boss, uint ItemClass, uint ItemTemplate, string Key)[] BossBrains =
        {
            (5, 13902, 13903, Boritak, BoritakBrainClass, BoritakBrainTemplate, "boritak-brain"),
            (6, 13904, 13905, Olirex, OlirexBrainClass, OlirexBrainTemplate, "olirex-brain"),
            (7, 13906, 13907, Exicorium, ExicoriumBrainClass, ExicoriumBrainTemplate, "exicorium-brain"),
            (8, 13908, 13909, Akikorik, AkikorikBrainClass, AkikorikBrainTemplate, "akikorik-brain"),
            (9, 17332, 17333, Queen, QueenBrainClass, QueenBrainTemplate, "queen-brain")
        };

        /// <summary>
        /// Zim's: the colony found, ten Atta brains and the five named Atta's, and Donners, who asks
        /// the player to give it up: kept to (choice 1), or given up for Bug Hugger (choice 2).
        /// </summary>
        private static void BugHuntRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, BugHunt, "Bug Hunt", 7660, Zim, Zim, 22, Category);
            mission.Reward(22000, 3300);
            var scene = Scene(events: true);

            mission.Objective(FindColony, 12744, 12745, 1, MissionObjectiveState.Incomplete);
            mission.Transition(FindColony);
            mission.Progress(FindColony, MissionProgressEventKind.ScenarioEvent, ColonyFound, counter: ColonyScenario);
            Marker(mission, scene, FindColony, Colony, Arrival, 20);
            mission.Scenario(ColonyScenario, "The colony found", ColonyFound);

            mission.Objective(4, 13900, 13901, 2, MissionObjectiveState.Incomplete);
            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.ItemAcquired, AttaBrainClass, target: BrainsWanted);

            var ordinal = 3U;
            foreach (var b in BossBrains)
            {
                mission.Objective(b.Objective, b.Name, b.Body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(b.Objective);
                mission.Progress(b.Objective, MissionProgressEventKind.ItemAcquired, b.ItemClass, target: 1);
            }
            Marker(mission, scene, 5, Colony, BoritakAt, 15);
            Marker(mission, scene, 6, Colony, OlirexAt, 15);
            Marker(mission, scene, 7, Colony, ExicoriumAt, 15);
            Marker(mission, scene, 8, Colony, AkikorikAt, 15);
            Marker(mission, scene, 9, Colony, Throne, 25, QueensChamberMarker);

            // Donners: kept to, the objective is done; given up, it fails, and the mission with it.
            mission.Objective(SpeakWithDonners, 20254, 20255, ordinal, MissionObjectiveState.Incomplete);
            mission.Transition(SpeakWithDonners, 1, sequence: 1);
            mission.Conversation(SpeakWithDonners, DonnersPackage, transition: 1);
            mission.Transition(SpeakWithDonners, 2, MissionObjectiveState.Failed, sequence: 2);
            mission.Conversation(SpeakWithDonners, DonnersPackage, transition: 2);
            Marker(mission, scene, SpeakWithDonners, Colony, DonnersAt, 4);
            mission.Action(SpeakWithDonners, 1, MissionActionKind.GrantReward, reward: 1);

            Evidence(mission, "Agent Zim1201 in the hospital gives and takes it. Coming into the colony finds it. A Kardash Atta Brain " +
                "Sample50307 from a third of the colony's Atta (Soldier, Worker, Guardian, Grub); Boritak's50306, Olirex's111188, Exicorium's111187, Akikorik's111186 " +
                "and Queen Kardash's117044 from each. All are handed over to Zim. Lieutenant Donners2457 just inside asks the player " +
                "to give it up: choice1 keeps to it, choice2 fails it - and Donners then has Bug Hugger, which takes its place.");

            scene.Items = new List<MissionItemBinding>
            {
                Drop("atta-brains", AttaBrainTemplate, BrainsWanted, 4, BrainChance, KardashAtta)
            };
            foreach (var b in BossBrains)
                scene.Items.Add(Drop(b.Key, b.ItemTemplate, 1, b.Objective, 100, b.Boss));
            scene.Dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(SpeakWithDonners, DonnersPackage, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 })
            };
            scene.Requirement = new AllRequirements(new MissionRequirement[]
            {
                scene.Requirement,
                new NotRequirement(new MissionStateRequirement(BugHugger)),
                new NotRequirement(new MissionStateRequirement(BugHugger, MissionState.Active))
            });
            mission.Enable(scene);
        }

        /// <summary>
        /// Donners': the four places, the Bane at the Queen, Engineer Reshi (package 1207), then
        /// Donners again (2457), whose report goes to Zim.
        /// </summary>
        private static void BugHuggerRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, BugHugger, "Bug Hugger", 20249, Donners, Zim, 22, Category);
            mission.Reward(22000, 3300);
            var scene = Scene(events: true);

            var ordinal = 1U;
            foreach (var (objective, name, body, place) in new (uint, uint, uint, Place)[]
                     {
                         (ExploreGrotto, 20293, 20294, Places[0]), (ExploreInnerNest, 20295, 20296, Places[1]),
                         (ExploreRoyalNest, 20297, 20298, Places[2]), (ExploreQueensChamber, 20299, 20300, Places[3])
                     })
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, place.Event, counter: ExploreScenario);
                Marker(mission, scene, objective, Colony, place == Places[3] ? Throne : place.Middle, place == Places[3] ? 40 : 60,
                    place == Places[3] ? QueensChamberMarker : null);
            }

            mission.Objective(ProtectQueen, 20301, 20302, ordinal++, MissionObjectiveState.Incomplete);
            mission.Transition(ProtectQueen);
            mission.Progress(ProtectQueen, MissionProgressEventKind.ScenarioEvent, QueenProtected, counter: ExploreScenario);
            Marker(mission, scene, ProtectQueen, Colony, RaiderSpots[0], 20, QueensChamberMarker);

            mission.Objective(TalkToReshi, 20311, 20312, ordinal++, MissionObjectiveState.Incomplete);
            mission.Transition(TalkToReshi);
            mission.Conversation(TalkToReshi, ReshiPackage);
            Marker(mission, scene, TalkToReshi, Colony, ReshiAt, 4);

            mission.Objective(HuggerCounter, 20305, 20306, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(TalkToDonners, 20303, 20304, ordinal);
            mission.Transition(HuggerCounter);
            mission.Progress(HuggerCounter, MissionProgressEventKind.ScenarioEvent, HuggerDone, counter: HuggerDoneScenario);
            mission.RevealAndActivate(HuggerCounter, TalkToDonners);

            mission.Transition(TalkToDonners);
            mission.Conversation(TalkToDonners, DonnersPackage);
            mission.Action(TalkToDonners, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, scene, TalkToDonners, Colony, DonnersAt, 4);

            mission.Scenario(ExploreScenario, "The colony explored and the Queen protected", HarvestGrotto, InnerNest, RoyalNest, QueensChamber, QueenProtected);
            mission.Scenario(HuggerDoneScenario, "Everything but Donners done", HuggerDone);

            Evidence(mission, "Lieutenant Donners2457 just inside gives it, to a player who has not done Bug Hunt; Agent Zim takes her " +
                "report. Coming into the Harvest Grotto, the Inner Nest, the Royal Nest and the Queen's Chamber (the map's regions, at " +
                "each cavern's height) explores each. In the chamber four Lightbender Infiltrators come at the Queen: all dead is her " +
                "protection. Engineer Reshi1207 in Outpost Alpha uploads the Brann's data. Objective9 is the client's hidden counter: " +
                "everything but Donners done, it shows Talk to Lieutenant Donners.");

            scene.HiddenObjectiveIds = new List<uint> { HuggerCounter };
            scene.Requirement = new AllRequirements(new MissionRequirement[]
            {
                scene.Requirement,
                new NotRequirement(new MissionStateRequirement(BugHunt)),
                new NotRequirement(new MissionStateRequirement(BugHunt, MissionState.Active))
            });
            mission.Enable(scene);
        }

        /// <summary>Erodan sends the player to Tralos (package 1204).</summary>
        private static void ADividedPeopleRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ADividedPeople, "A Divided People", 7675, Erodan, Tralos, 20, Category);
            mission.Reward(6000, 900);
            mission.Objective(1, 7711, 7712, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, TralosPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Marker(mission, scene, 1, Plains, Moved[3].To, 4);
            Evidence(mission, "Researcher Erodan outside the large building in Irendas (254, 433, -145) gives it; Engineer Tralos1204 " +
                "(264.6, 433, -236) takes it.");
            mission.Enable(scene);
        }

        /// <summary>Tralos sends the player to his brother Miras in the colony (package 1203).</summary>
        private static void TaleOfTwoBrothersRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TaleOfTwoBrothers, "A Tale of Two Brothers", 7703, Tralos, Miras, 20, Category);
            mission.Reward(8000, 1200);
            mission.Prerequisite(ADividedPeople);
            mission.Objective(1, 8536, 8537, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, MirasPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Marker(mission, scene, 1, Colony, MirasAt, 4);
            Evidence(mission, "Engineer Tralos gives it after A Divided People; Surveyor Miras1203, in the back room of Research Outpost " +
                "Alpha, takes it.");
            mission.Enable(scene);
        }

        /// <summary>Miras': the Lightbender Infiltrators and the four Elite Infiltrators.</summary>
        private static void UnexpectedGuestsRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, UnexpectedGuests, "Unexpected Guests", 7718, Miras, Miras, 22, Category, shareable: true);
            mission.Reward(20000, 5000, selectableItems: new[]
            {
                Item(StealthLegs, 1, ResistIce), Item(IncendiaryLeechGun, 1, DebuffFire), Item(MotorAssistGloves, 1, ArmorAbsorption)
            });
            mission.Prerequisite(TaleOfTwoBrothers);
            var scene = Scene();

            mission.Objective(1, 8667, 8668, 1, MissionObjectiveState.Incomplete, counterTextId: 8669);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, Infiltrator, counter: 0, target: InfiltratorsWanted);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            var ordinal = 2U;
            foreach (var (objective, name, body, elite, outpost) in new (uint, uint, uint, uint, int)[]
                     {
                         (2, 8670, 8671, Arkshaz, 1), (3, 8672, 8673, Dargoth, 3), (4, 8674, 8675, Kresht, 4), (5, 8676, 8677, Morkone, 2)
                     })
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, elite);
                Marker(mission, scene, objective, Colony, Transducers[outpost].At, 20);
            }

            Evidence(mission, $"Surveyor Miras gives and takes it, after A Tale of Two Brothers. {InfiltratorsWanted} Lightbender " +
                "Infiltrators at the Brann outposts (Alpha's bridge, Beta, Gamma, Delta, Epsilon), and the guide's Elite Infiltrators: " +
                "Arkshaz at Beta, Morkone at Gamma, Dargoth at Delta, Kresht at Epsilon. Shareable: a squad clears the outposts together. " +
                "5000 credits and CryoGen Stealth Armor Legs (Resist Ice[3]), Vextronics Pyrogenic Leechgun - no fire leech gun nearer " +
                "level 22 than Incendiary 24-28 - (Debuff Fire Resist[3]) or Teleract Motor Assist Armor Gloves (Armor Absorption[3]), the guide's.");
            mission.Enable(scene);
        }

        /// <summary>Kranix's: a Brann Data Disc from the infiltrators.</summary>
        private static void JustToBeCertainRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, JustToBeCertain, "Just to be Certain", 7723, Kranix, Kranix, 22, Category);
            mission.Reward(15000, 5000, selectableItems: new[]
            {
                Item(BioVest, 1, ArmorAbsorption), Item(GravitonHelmet, 1, PowerRegen), Item(Shotgun, 1, DebuffPhysical)
            });
            mission.Prerequisite(TaleOfTwoBrothers);
            mission.Objective(1, 8691, 8692, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, DataDiscClass, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Evidence(mission, "Researcher Kranix gives and takes it, after A Tale of Two Brothers. A Brann Data Disc50309 from the " +
                "Lightbender Infiltrators and Elite Infiltrators, one time in three, handed over to " +
                "Kranix. 5000 credits and Teleract Bio Armor (Armor Absorption[3]), ChiTech Gravitron Armor Helmet (Power Regen Bonus[3]) " +
                "or Vextronics Shotgun (Debuff Physical Resist[3]), the guide's.");
            scene.Items = new List<MissionItemBinding> { Drop("data-disc", DataDiscTemplate, 1, 1, DataDiscChance, Elites.Prepend(Infiltrator).ToArray()) };
            mission.Enable(scene);
        }

        /// <summary>Miras' package to Tralos (package 1204), who gives the keycard.</summary>
        private static void KeyInformationRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, KeyInformation, "Key Information", 7728, Miras, Tralos, 22, Category);
            mission.Reward(10000, 1500, fixedItems: new[] { Item(KeycardTemplate) });
            mission.Prerequisite(UnexpectedGuests);
            mission.Objective(1, 8693, 8694, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, TralosPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Marker(mission, scene, 1, Plains, Moved[3].To, 4);
            Evidence(mission, "Surveyor Miras gives the Package from Miras50308 on acceptance, once Unexpected Guests is done; Engineer " +
                "Tralos1204 takes it and gives the Phanin Research Facility Keycard50310 ('He has provided me an Authorization Keycard'), " +
                "which is the player's to keep.");
            scene.Items = new List<MissionItemBinding>
            {
                new("miras-package", PackageTemplate, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
            };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("miras-issue-package", KeyInformation, "miras-package", PackageTemplate, 1)
            };
            mission.Enable(scene);
        }

        /// <summary>Tralos sends the player to Reyko with what Miras gave (package 1200).</summary>
        private static void SecurityThreatRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, SecurityThreat, "Security Threat", 7746, Tralos, Reyko, 22, Category);
            mission.Reward(8000, 1200);
            mission.Prerequisite(KeyInformation);
            mission.Objective(1, 8132, 8133, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, ReykoPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            Marker(mission, scene, 1, Plains, Moved[0].To, 4);
            Evidence(mission, "Engineer Tralos gives it once Key Information is done; Captain Reyko1200 takes it, and has Into the " +
                "Facility next (PhaninResearchContent).");
            mission.Enable(scene);
        }

        /// <summary>A brain or disc off a corpse: the character's own, handed in.</summary>
        private static MissionItemBinding Drop(string key, uint template, uint count, uint objective, int chance, params uint[] creatures) =>
            new(key, template, MissionItemScope.CharacterOwned, count,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                TurnInQuantity: count, Drop: new MissionItemDropDefinition(creatures, objective, Colony, chance, 1));

        /// <summary>An objective's marker, on the map it is on.</summary>
        private static void Marker(InstanceMissionSql mission, MissionSceneDefinition scene, uint objective, uint map, Vector3 at, double radius,
            uint? named = null)
        {
            var id = named ?? MissionIndicator.UnnamedFrom + objective;
            mission.Indicator(objective, id, at.X, at.Y, at.Z, radius);
            scene.IndicatorMaps ??= new Dictionary<uint, Dictionary<uint, uint>>();
            if (!scene.IndicatorMaps.TryGetValue(objective, out var maps))
                scene.IndicatorMaps[objective] = maps = new Dictionary<uint, uint>();
            maps[id] = map;
        }

        /// <summary>
        /// A mission's binding: for the bootcamp's graduates. One whose events the colony records
        /// has a scenario, which a mission has only with a script; the data sequence with no
        /// sequences is one that does nothing.
        /// </summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide, "The Kardash Atta Colony walkthrough: route, people and rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "KardashAttaColonyContent", reconstruction);
        }

        #endregion
    }
}
