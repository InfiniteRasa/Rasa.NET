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
    /// Crater Lake Research Facility (adv_foreas_concordia_wilderness_clrf, 1721), entered from
    /// the Wilderness, as the Ten Ton Hammer walkthrough tells it: its people, its Bane and its
    /// ten missions, in the client's own names and texts.
    ///
    /// Who and what:
    ///  - Dr. Franja Corman at Ranja Gorge (creature 107), who gives The Dead Live (Casper is
    ///    missing), takes Ghost in the Machine (his dogtags) and gives Mending A Broken Heart (her
    ///    photographs to Council Elder Baruhi at the Memory Tree, and back to her).
    ///  - Operations Chief Sten Corman at Ranja Gorge (128), who gives and takes Centrifuge Fuel:
    ///    five crystals out of the facility's old storage crates.
    ///  - Receptive Liaison Standley at Twin Pillars (134): Logos: Movement, Around, Chaos, as the
    ///    Wilderness' other Logos missions are.
    ///  - Captain Velns in the staging post in the cave by the way in (package 23), who takes The
    ///    Dead Live and gives We Be Jammin' (the Bane jammer, then the AFS GPS beacon by the
    ///    radar dish; radioed in) and Destroying the Evidence (the Corman data terminals of the
    ///    Processing Center, the Biological Lab and the Observation Center, and the Bane
    ///    equipment on the fortification's platform; radioed in).
    ///  - Headquarters by radio: Ghost in the Machine once We Be Jammin' is in (Casper, now a
    ///    Machina, on the fortification's platform: creature 520012), Reclaiming What's Ours once
    ///    Destroying the Evidence is (the detonators, the generator and the base's force fields,
    ///    the Overseer, the supply pen's panel and its two Logos), and Return to Sender once that
    ///    is (Daniel to Velns).
    ///  - Daniel Corman in the supply pen, who takes Reclaiming What's Ours and gives Bending the
    ///    Rules (out of the facility). He or Velns: taking one of the two shuts the other.
    ///  - The Bane, levels 10 to 12, for a squad of level 10: Thrax on the trail, at the destroyed
    ///    buildings, the transmitter clearing, the fortification, the crater floor and in the
    ///    stronghold's courtyard; a Predator outside the cave and more on the crater floor; the
    ///    Overseer in the courtyard, whose shield the walkthrough has Sonic and Lightning take
    ///    down. Health 10000 x 2^((level - 42) / 8), the curve the Wilderness rows sit on, a
    ///    lieutenant one and three quarters of it; armour ten a level less twenty. The mortars on
    ///    the map's three pads are BaneMortars'.
    /// The force fields, generators, jammer, beacon site, terminals, crates, cache, the Overseer's
    /// shield and drop pods, the teleporters that beam Bane in, the waves that come behind a squad
    /// that lingers, Daniel and the scenario events the missions wait for are the server's, per
    /// copy of the facility (CraterLakeFacility).
    /// </summary>
    public static class CraterLakeContent
    {
        public const string Revision = "clrf_1_16_5";
        public const uint CraterLake = 1721, Wilderness = 1220;

        /// <summary>missioncategorylanguage: "Instance (Crater Lake Research Facility)", and the Wilderness battlefield's.</summary>
        public const uint Category = 10000046, WildernessCategory = 10000044;

        public const uint TheDeadLive = 450, WeBeJammin = 1054, DestroyingTheEvidence = 1055, GhostInTheMachine = 1059,
            MendingABrokenHeart = 446, CentrifugeFuel = 489, LogosMovementAroundChaos = 960, ReclaimingWhatsOurs = 1056,
            ReturnToSender = 711, BendingTheRules = 1065;

        public static readonly uint[] Missions =
        {
            TheDeadLive, WeBeJammin, DestroyingTheEvidence, GhostInTheMachine, MendingABrokenHeart, CentrifugeFuel,
            LogosMovementAroundChaos, ReclaimingWhatsOurs, ReturnToSender, BendingTheRules
        };

        // The NPC packages of the missions' conversations (objectiveconversation): Velns, Franja,
        // Baruhi, and the objects': the three Corman terminals, the Bane equipment, the pen's panel.
        public const uint VelnsPackage = 23, FranjaPackage = 118, BaruhiPackage = 227;
        public const uint ProcessingPackage = 603, BiologicalPackage = 604, ObservationPackage = 649, BaneDataPackage = 1121, PanelPackage = 2581;

        // The people already in the world.
        public const uint Franja = 107, Sten = 128, Baruhi = 106, Standley = 134, Casper = 520012;

        public const uint Velns = 675001, Daniel = 675002;
        public const uint Thrax = 675010, Grenadier = 675011, Hunter = 675012, Predator = 675013, Overseer = 675014;

        /// <summary>Every Bane row of ours in the facility.</summary>
        public static readonly uint[] Hostiles = { Thrax, Grenadier, Hunter, Predator, Overseer };

        // The scenario of each mission whose events the facility records (one each), and its events.
        public const uint Scenario = 1;
        public const uint JammerDown = 1, BeaconPlaced = 2;
        public const uint BaseOpen = 1, OverseerDown = 2;
        public const uint DanielDelivered = 1;
        public const uint DanielOut = 1;

        // Objectives.
        public const uint DestroyJammers = 1, PlaceBeacon = 2;
        public const uint ProcessingCenter = 1, BiologicalLab = 2, ObservationCenter = 3, BaneInformation = 4;
        public const uint EngageCasper = 1, ReturnDogtags = 2;
        public const uint AroundFromPen = 1, ChaosFromPen = 2, KillOverseer = 3, UsePanel = 4, GatherBombs = 5, BlowGenerator = 6;
        public const uint EscortToVelns = 1, EscortOut = 3, FindCrystals = 1, SpeakToVelns = 4;
        public const uint DeliverPhotos = 1, ReturnToFranja = 2;
        public const uint BombsWanted = 2, CrystalsWanted = 5;

        // Items.
        public const uint BeaconClass = 22123, BeaconTemplate = 45036;              // AFS GPS Beacon
        public const uint DetonatorClass = 20650, DetonatorTemplate = 11487;        // Generator Detonator
        public const uint CrystalClass = 7597, CrystalTemplate = 626;               // Centrifuge Crystal
        public const uint DogtagsClass = 7596, DogtagsTemplate = 625;               // Casper's Dogtags
        public const uint PhotosClass = 9254, PhotosTemplate = 1760;                // Franja Corman's Photographs

        // The objects CraterLakeFacility puts down.
        public const uint BaseFieldClass = 20000003;    // UsableForceFieldBaneOutpostWallMinorV01: the wall's minor entrances
        public const uint GeneratorClass = 3857;        // UsableInertDestBaneOutpostGenerator
        public const uint JammerClass = 10282;          // UsableInertDestBaneSonicTowerV01: the large transmitter
        public const uint TeleporterClass = 10603;      // UsableInertDestBaneTeleporterV01DELETEDUPE
        public const uint BombClass = 10122;            // UsableBombForeanV01
        public const uint TerminalClass = 7123;         // UsableNPCCormanComputerV01
        public const uint KeyboardClass = 10519;        // UsableNPCBaneKeyboard
        public const uint CacheClass = 7894;            // UsableTreasureDispBaneCrateV01: the weapons cache
        public const uint CrateClass = 7863;            // UsableTreasureDispHumCrateV02: the mesh the client's CLRF crystal crate is
        public const uint BeaconSiteClass = 9474;       // UsableItemAccHumObjGPSBeaconV01
        public const uint RadarDishClass = 29709;       // PropHumRadarV02
        public const uint GeneratorHitPoints = 20000, JammerHitPoints = 3000, TeleporterHitPoints = 2500;

        // Logos ids of the three shrines (LogosPreloader).
        public const uint MovementLogos = 50, AroundLogos = 45, ChaosLogos = 4;

        /// <summary>missionobjectiveindicatorlanguage: GPS Beacon Location, Bane Equipment, Bane Generator.</summary>
        public const uint BeaconMarker = 48, BaneEquipmentMarker = 72, GeneratorMarker = 243;

        // Where things are, on the facility's navmesh (x, height, z).
        public static readonly Vector3 Arrival = new(8.5096f, 140.14f, 158.2927f);
        public static readonly Vector3 WildernessDoor = new(-408.3123f, 275.6469f, -749.348f);
        public static readonly Vector3 FranjaAtRanja = new(-715.9961f, 169.97266f, -324.9961f);
        public static readonly Vector3 BaruhiAtMemoryTree = new(-45.804688f, 207.33594f, 69.21484f);

        /// <summary>The staging post's room, its folding table: Velns.</summary>
        public static readonly Vector3 VelnsSpot = new(34f, 139.44f, 197f);

        /// <summary>The supply pen, between the Chaos and Around shrines.</summary>
        public static readonly Vector3 DanielInPen = new(18.1f, 71.85f, -11.8f);

        /// <summary>The stronghold's courtyard, among its lights: the Overseer.</summary>
        public static readonly Vector3 Courtyard = new(-35f, 71.58f, -5f);

        /// <summary>Where the drop pods put their Hunters down: the courtyard's middle.</summary>
        public static readonly Vector3[] DropSpots = { new(-50f, 71.62f, -30f), new(-20f, 71.58f, 10f), new(-45f, 71.6f, 15f) };

        /// <summary>
        /// The wall's minor entrances (ArchBaneOutpostWallMinorEntranceV01) the fields stand in, at
        /// each gate's origin and yaw (2 atan2(qy, qw) of the map's rotation): the stronghold's
        /// west and north ways in, and the supply pen's.
        /// </summary>
        public static readonly Vector3 WestGate = new(-98.46f, 72.19f, -24.88f), NorthGate = new(-27.82f, 72.19f, 39.12f),
            PenGate = new(8.65f, 72.19f, -20.5f);
        public const float WestGateYaw = 1.5707964f, NorthGateYaw = 3.1415927f, PenGateYaw = 1.5707964f;

        /// <summary>The generator by the fortification's pipes (the walkthrough's), and the second in the courtyard.</summary>
        public static readonly Vector3 OuterGenerator = new(-253.8f, 166.19f, -102.5f), InnerGenerator = new(-2f, 71.49f, -6f);
        public static readonly Vector3 OuterBomb = new(-251f, 165.75f, -98f), InnerBomb = new(-5f, 71.55f, -9f);

        /// <summary>The ring of Bane lights in the clearing west along the ridge: the transmitter.</summary>
        public static readonly Vector3 Jammer = new(-59f, 98.66f, -169f);

        /// <summary>The GPS beacon's place (the walkthrough's), and the dish beside it.</summary>
        public static readonly Vector3 BeaconSite = new(-169.5f, 141.25f, -109.5f), RadarDish = new(-172f, 142.24f, -113f);

        /// <summary>
        /// The data terminals: the research HQ's upper floor (the walkthrough's first), the
        /// greenhouse with the lab counters (its second), the housing on the ridge with its
        /// microscope and computer (its third). The Bane equipment by the fortification platform's consoles.
        /// </summary>
        public static readonly Vector3 ProcessingTerminal = new(124.3f, 103.91f, -27.3f), BiologicalTerminal = new(102.3f, 118.94f, -89.6f),
            ObservationTerminal = new(-113f, 154.04f, -170f), BaneData = new(-270.5f, 169.04f, -66f);

        /// <summary>The panel on the courtyard's catwalk, by its terminal.</summary>
        public static readonly Vector3 Panel = new(-80f, 74.84f, 6f);

        /// <summary>The weapons caches on the fortification's platform.</summary>
        public static readonly Vector3[] Caches = { new(-283f, 169.04f, -52f), new(-283f, 169.04f, -64f) };

        /// <summary>The storage crates in the destroyed buildings.</summary>
        public static readonly Vector3[] Crates =
        {
            new(118f, 99.58f, -40f), new(126f, 104.46f, -33f), new(100f, 120.29f, -110f), new(90f, 118.93f, -118f),
            new(8f, 119.09f, -160f), new(20f, 119.22f, -145f), new(-125f, 154.04f, -185f)
        };

        /// <summary>The Bane teleporters: on the trail below the research HQ, and on the crater floor north of the stronghold.</summary>
        public static readonly Vector3[] Teleporters = { new(130f, 100.95f, -60f), new(-24f, 71.23f, 70f) };

        public static readonly Vector3 MovementShrine = new(0.93359375f, 90.92969f, 115.21094f);
        public static readonly Vector3 AroundShrine = new(24.070312f, 74.84766f, -9.0f);
        public static readonly Vector3 ChaosShrine = new(23.980469f, 74.828125f, -20.121094f);
        public static readonly Vector3 CasperSpot = new(-272f, 168f, -54f);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1 }

        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A spawn pool: its creatures (row, least, most), where and which way, and its respawn time in tenths of a second.</summary>
        public sealed record Spot(uint Id, uint Map, double X, double Y, double Z, double Rotation, uint RespawnTenths, double Radius,
            params (uint Creature, uint Min, uint Max)[] Slots);

        // Existing actions the rows keep.
        public const uint ThraxRifle = 70031, ThraxMelee = 3;
        public const uint GrenadierGrenade = 19, GrenadierRifle = 20;
        public const uint HunterRange = 9, HunterMelee = 10;
        public const uint OverseerAttack = 34, OverseerForceBlast = 76050, OverseerLightning = 76051, OverseerRage = 61001;

        public static readonly Act[] Actions =
        {
            new(675101, "Predator - Crater Lake predator weapon 1/85", 1, 85, 1, 50, 3000, 0, 40, 60, 6)
        };

        public static readonly Beast[] Creatures =
        {
            new(Velns, "Captain Velns - Crater Lake staging post", SwapsetMale, TargetCategoryRow.Friendly, 12, 3000, 0, 8871, 0, 0),
            new(Daniel, "Daniel Corman - Crater Lake supply pen", WildernessPostNpcs.CormanMale, TargetCategoryRow.Friendly, 10, 1000, 0, 3103, 6, 2),
            new(Thrax, "Thrax Soldier - Crater Lake", 20757, TargetCategoryRow.Hostile, 10, 625, 80, 235, 9, 5, ThraxRifle, ThraxMelee),
            new(Grenadier, "Thrax Grenadier - Crater Lake", 9354, TargetCategoryRow.Hostile, 11, 680, 90, 0, 9, 5, GrenadierGrenade, GrenadierRifle),
            new(Hunter, "Bane Hunter - Crater Lake, beamed in", 10166, TargetCategoryRow.Hostile, 10, 625, 80, 0, 9, 5, HunterRange, HunterMelee),
            new(Predator, "Predator - Crater Lake", 3902, TargetCategoryRow.Hostile, 11, 1190, 100, 0, 9, 5, 675101),
            new(Overseer, "Overseer Tyryd - Crater Lake stronghold", 10504, TargetCategoryRow.Hostile, 12, 4500, 140, 7002, 9, 5,
                OverseerAttack, OverseerForceBlast, OverseerLightning, OverseerRage)
        };

        /// <summary>Velns wears the brown officer's uniform of the Wilderness' officers.</summary>
        public static readonly Slot[] VelnsLook =
        {
            new Slot(2, OfficerBoots, Brown),
            new Slot(13, PistolRedshirt, 1),
            new Slot(14, WildernessPostNpcs.Hair09, DarkHair),
            new Slot(15, OfficerTunic, Brown),
            new Slot(16, OfficerLegs, Brown),
            new Slot(17, UnplacedNamedNpcs.FaceCaucasianSquare2, SkinLight)
        };

        /// <summary>Daniel dresses as George Corman, the Wilderness' Corman civilian, does.</summary>
        public const uint DanielLooksLike = 510005;

        public const uint VelnsPool = 675201;
        public const uint NpcRespawn = 200, HostileRespawn = 3000;

        public static readonly Spot[] Pools =
        {
            new(VelnsPool, CraterLake, VelnsSpot.X, VelnsSpot.Y, VelnsSpot.Z, -1.5708, NpcRespawn, 0, (Velns, 1, 1)),

            // The trail down from the cave: a Predator outside it, patrols on the way.
            new(675210, CraterLake, 90, 140.75, 82, 2.6, HostileRespawn, 3, (Predator, 1, 1)),
            new(675211, CraterLake, 108, 136.97, 58, 2.8, HostileRespawn, 5, (Thrax, 2, 2)),
            new(675212, CraterLake, 105, 101.15, 8, 3.0, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),

            // The destroyed buildings: the research HQ, the greenhouses, the Corman HQ, the housing on the ridge.
            new(675213, CraterLake, 115, 99.7, -45, -1.6, HostileRespawn, 5, (Thrax, 2, 2)),
            new(675214, CraterLake, 95, 119.44, -105, 0.5, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(675215, CraterLake, 5, 118.91, -150, 1.6, HostileRespawn, 5, (Thrax, 2, 2)),
            new(675216, CraterLake, -120, 154.05, -175, 1.6, HostileRespawn, 5, (Thrax, 2, 2)),

            // The transmitter's clearing and the beacon's ridge.
            new(675217, CraterLake, -45, 99.93, -165, -1.6, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(675218, CraterLake, -160, 137.43, -120, 0.8, HostileRespawn, 5, (Thrax, 2, 2)),

            // The fortification, a dozen or so with Casper (520012's pool) on the platform.
            new(675219, CraterLake, -250, 165.93, -80, -1.6, HostileRespawn, 5, (Thrax, 3, 3)),
            new(675220, CraterLake, -240, 166.04, -60, -1.6, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(675221, CraterLake, -283, 169.04, -70, 1.6, HostileRespawn, 4, (Thrax, 2, 2), (Grenadier, 1, 1)),

            // The crater floor about the stronghold's walls and trenches, wandering Predators.
            new(675222, CraterLake, -115, 71.46, -21, -1.6, HostileRespawn, 5, (Thrax, 2, 2), (Predator, 1, 1)),
            new(675223, CraterLake, -24, 71.06, 60, 3.1, HostileRespawn, 5, (Thrax, 2, 2)),
            new(675224, CraterLake, 0, 70.84, 60, 3.1, HostileRespawn, 3, (Predator, 1, 1)),
            new(675225, CraterLake, -110, 71.71, 30, -1.6, HostileRespawn, 5, (Thrax, 2, 2)),

            // The courtyard and its catwalk.
            new(675226, CraterLake, -60, 71.64, -40, 0.8, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(675227, CraterLake, -30, 71.52, 20, 3.1, HostileRespawn, 5, (Thrax, 2, 2)),
            new(675228, CraterLake, -75, 74.84, 8, 1.6, HostileRespawn, 2, (Thrax, 1, 1), (Grenadier, 1, 1))
        };

        /// <summary>The loot each hostile row drops on the facility's map: the Wilderness' own profiles for its kind.</summary>
        public static readonly (uint Creature, string Profile)[] Loot =
        {
            (Thrax, "wilderness_v1_thrax_10_14"), (Grenadier, "wilderness_v1_thrax_10_14"), (Hunter, "wilderness_v1_hunter_5_9"),
            (Predator, "wilderness_v1_predator_10_14"), (Overseer, "wilderness_v1_thrax_10_14_boss")
        };

        /// <summary>Who speaks which package: Velns, and Franja, whose row had none.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Velns, VelnsPackage, "Captain Velns - Crater Lake"),
            (Franja, FranjaPackage, "Dr. Franja Corman")
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            // An action row has nothing to hang on: the SELECT names a table only so that its
            // WHERE is one MySQL takes as well as SQLite.
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {SwapsetMale} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = c.Actions.Concat(Enumerable.Repeat(0U, 8)).Take(8).ToArray();

                // Every action it names has to be there, or the creature is not.
                var actionsThere = string.Concat(c.Actions.Select(id => $" AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {id})"));

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId}{actionsThere} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            foreach (var sql in Change(Velns, new Slot[0], VelnsLook))
                yield return sql;

            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {Daniel}, a.slot_id, a.Class_id, a.color " +
                $"FROM creature_appearance a WHERE a.id = {DanielLooksLike} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Daniel}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {Daniel});";

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

            foreach (var (creature, profile) in Loot)
                yield return "INSERT INTO creature_loot_binding (map_context_id, creature_id, profile_key) " +
                    $"SELECT {CraterLake}, {creature}, {S(profile)} FROM creature c WHERE c.id = {creature} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding b WHERE b.map_context_id = {CraterLake} AND b.creature_id = {creature});";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var (creature, profile) in Loot)
                yield return $"DELETE FROM creature_loot_binding WHERE map_context_id = {CraterLake} AND creature_id = {creature} AND profile_key = {S(profile)};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Slots[0].Creature};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            yield return $"DELETE FROM creature_appearance WHERE id IN ({Velns}, {Daniel});";

            foreach (var c in Creatures)
            {
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id};";
        }

        #region Missions

        private const string Guide = "https://www.tentonhammer.com/tabula-rasa/guides/crater-lake-research-facility";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            TheDeadLiveRows(sql);
            WeBeJamminRows(sql);
            DestroyingTheEvidenceRows(sql);
            GhostInTheMachineRows(sql);
            MendingABrokenHeartRows(sql);
            CentrifugeFuelRows(sql);
            LogosRows(sql);
            ReclaimingWhatsOursRows(sql);
            ReturnToSenderRows(sql);
            BendingTheRulesRows(sql);
            return sql;
        }

        // Rewards: the walkthrough's choices; the experience and the credits it does not give are
        // ours, beside the Wilderness' instances'. Each piece of gear is the nearest level 9 to 13
        // uncommon of its kind, with the module its name's brand prefix is: Pulsar Resist EMP,
        // Eclipse Threat Reduction, each [1] as the Wilderness rewards have them
        // (WildernessRewardModules). Teleract names more than one kind of module and has none;
        // the helmets and the chaingun are named without a prefix.
        public const uint ResistEmp = 100048, ThreatReduction = 100052;
        public const uint StandardMedPack = 44918, EmpBomb = 111247;
        public const uint PulsarHazmatBoots = 20260, TeleractPhotonicShotgun = 17843;
        public const uint ReflectiveHelmet = 35794, HazmatHelmet = 20559;
        public const uint EclipseLaserRifle = 17337, PulsarReflectiveGloves = 35645;
        public const uint PulsarHazmatVest = 20856, LaserChaingun = 42440;

        private static InstanceMissionSql.RewardItem Item(uint template, uint quantity = 1, uint module = 0) => new(template, quantity, module);

        /// <summary>Franja's: Casper is missing; Velns at the staging post looks him up.</summary>
        private static void TheDeadLiveRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TheDeadLive, "The Dead Live", 825, Franja, Velns, 9, WildernessCategory);
            mission.Reward(4000, 800, selectableItems: new[] { Item(StandardMedPack, 3), Item(EmpBomb, 3) });
            mission.Objective(SpeakToVelns, 4748, 6764, 1, MissionObjectiveState.Incomplete);
            mission.Transition(SpeakToVelns);
            mission.Conversation(SpeakToVelns, VelnsPackage);
            mission.Action(SpeakToVelns, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(SpeakToVelns, MissionIndicator.UnnamedFrom + SpeakToVelns, WildernessDoor.X, WildernessDoor.Y, WildernessDoor.Z);
            Evidence(mission, "Dr. Franja Corman107 at Ranja Gorge asks after Casper (the opening text is hers); Captain Velns675001 in " +
                "the staging post by the way in (package23, the native objective4 conversation) takes it. The marker is on the " +
                "Wilderness' door to the facility. The walkthrough's reward: a Class I Standard Med Pack44918 or a Class I EMP " +
                "Bomb111247, three of either.");
            mission.Enable(Scene());
        }

        /// <summary>Velns's: the jammer on the ridge, then the GPS beacon by the radar dish; HQ is radioed.</summary>
        private static void WeBeJamminRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, WeBeJammin, "We Be Jammin'", 5923, Velns, null, 10, Category);
            mission.Reward(10000, 1500, selectableItems: new[] { Item(PulsarHazmatBoots, 1, ResistEmp), Item(TeleractPhotonicShotgun) });
            mission.Prerequisite(TheDeadLive);
            mission.Objective(DestroyJammers, 5979, 7759, 1, MissionObjectiveState.Incomplete);
            mission.Objective(PlaceBeacon, 5980, 7760, 2);
            mission.Transition(DestroyJammers);
            mission.Progress(DestroyJammers, MissionProgressEventKind.ScenarioEvent, JammerDown, counter: Scenario);
            mission.RevealAndActivate(DestroyJammers, PlaceBeacon);
            mission.Transition(PlaceBeacon);
            mission.Progress(PlaceBeacon, MissionProgressEventKind.ScenarioEvent, BeaconPlaced, counter: Scenario);
            mission.Action(PlaceBeacon, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("velns-beacon-placed", WeBeJammin, "beacon", 1, MissionItemScope.AssignmentIssued));
            mission.Action(PlaceBeacon, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "We Be Jammin'", JammerDown, BeaconPlaced);
            mission.Indicator(DestroyJammers, MissionIndicator.UnnamedFrom + DestroyJammers, Jammer.X, Jammer.Y, Jammer.Z, 15);
            mission.Indicator(PlaceBeacon, BeaconMarker, BeaconSite.X, BeaconSite.Y, BeaconSite.Z);
            mission.RadioTurnIn();
            Evidence(mission, "Velns675001 gives it after The Dead Live450, with an AFS GPS Beacon45036. The large transmitter " +
                "(UsableInertDestBaneSonicTowerV01) stands in the ring of Bane lights in the clearing west along the ridge; down, " +
                "it is event1 for everyone in the copy on the objective. The beacon used on its place by the radar dish at the " +
                "walkthrough's (-169.5, -109.5), the jammer down, is event2 and takes the beacon; the finishing text is HQ's, so " +
                "it is radioed in, and Ghost in the Machine1059 follows by radio. Pulsar Hazmat Armor Boots20260 with Resist " +
                "EMP[1]100048 or the Teleract Photonic Shotgun17843.");
            var scene = Scene(events: true);
            scene.Items = new List<MissionItemBinding> { Issued("beacon", BeaconTemplate) };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("velns-beacon", WeBeJammin, "beacon", BeaconTemplate, 1)
            };
            mission.Enable(scene);
        }

        /// <summary>Velns's: the three Corman terminals and the Bane equipment; HQ is radioed.</summary>
        private static void DestroyingTheEvidenceRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, DestroyingTheEvidence, "Destroying the Evidence", 5927, Velns, null, 10, Category);
            mission.Reward(12000, 2000, selectableItems: new[] { Item(ReflectiveHelmet), Item(HazmatHelmet) });
            mission.Prerequisite(TheDeadLive);
            uint ordinal = 1;
            foreach (var (objective, name, body, classId, spot, marker) in Terminals)
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                // An object's conversation is the object's interaction (MissionApplication.TryApplyDialogue).
                mission.Progress(objective, MissionProgressEventKind.InteractionUsed, classId);
                mission.Indicator(objective, marker, spot.X, spot.Y, spot.Z);
            }
            mission.Action(BaneInformation, 1, MissionActionKind.GrantReward, reward: 1);
            mission.RadioTurnIn();
            Evidence(mission, "Velns675001 gives it after The Dead Live450. Each terminal is an object conversation of the client's " +
                "objectiveconversation: the Processing Center's (package603) on the research HQ's upper floor at the walkthrough's " +
                "(124.3, -27.3), the Biological Lab's (604) in the greenhouse at (102.3, -89.6), the Observation Center's (649) " +
                "in the housing on the ridge (the walkthrough's third, east of the beacon), UsableNPCCormanComputerV01; the Bane " +
                "equipment (1121, UsableNPCBaneKeyboard) by the fortification platform's consoles. The finishing text is HQ's, so " +
                "it is radioed in, and Reclaiming What's Ours1056 follows by radio. A Reflective Armor Helmet35794 or a Hazmat " +
                "Armor Helmet20559, as the walkthrough names them.");
            mission.Enable(Scene());
        }

        /// <summary>Destroying the Evidence's objects: objective, its texts, the class, where, its marker.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint ClassId, Vector3 Spot, uint Marker)[] Terminals =
        {
            (ProcessingCenter, 5983, 6956, TerminalClass, ProcessingTerminal, MissionIndicator.UnnamedFrom + ProcessingCenter),
            (BiologicalLab, 5985, 6957, TerminalClass, BiologicalTerminal, MissionIndicator.UnnamedFrom + BiologicalLab),
            (ObservationCenter, 5987, 6958, TerminalClass, ObservationTerminal, MissionIndicator.UnnamedFrom + ObservationCenter),
            (BaneInformation, 5989, 6959, KeyboardClass, BaneData, BaneEquipmentMarker)
        };

        /// <summary>The package of each of Destroying the Evidence's objects, by objective.</summary>
        public static uint PackageOf(uint objective) => objective switch
        {
            ProcessingCenter => ProcessingPackage,
            BiologicalLab => BiologicalPackage,
            ObservationCenter => ObservationPackage,
            _ => BaneDataPackage
        };

        /// <summary>HQ's, by radio after We Be Jammin': Casper, found a Machina, and his dogtags to Franja.</summary>
        private static void GhostInTheMachineRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, GhostInTheMachine, "Ghost in the Machine", 5944, null, Franja, 10, Category);
            mission.Reward(8000, 1500);
            mission.Prerequisite(WeBeJammin);
            mission.Objective(EngageCasper, 5981, 7391, 1, MissionObjectiveState.Incomplete);
            // Both open from the start: the dogtags drop to whoever has the second open when Casper dies.
            mission.Objective(ReturnDogtags, 6004, 7392, 2, MissionObjectiveState.Incomplete);
            mission.Transition(EngageCasper);
            mission.Progress(EngageCasper, MissionProgressEventKind.CreatureKilled, Casper);
            mission.Transition(ReturnDogtags);
            mission.Progress(ReturnDogtags, MissionProgressEventKind.ItemAcquired, DogtagsClass, target: 1);
            mission.Action(ReturnDogtags, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(EngageCasper, MissionIndicator.UnnamedFrom + EngageCasper, CasperSpot.X, CasperSpot.Y, CasperSpot.Z, 15);
            mission.Indicator(ReturnDogtags, MissionIndicator.UnnamedFrom + ReturnDogtags, Arrival.X, Arrival.Y, Arrival.Z);
            mission.Channels(MissionChannel.Radio, MissionChannel.Npc,
                MissionOfferSourceDefinition.MissionRewarded(WeBeJammin),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, CraterLake));
            Evidence(mission, "HQ's scan after We Be Jammin'1054 (its opening is HQ's): offered by radio at its turn-in, and again on " +
                "coming into the facility. Hominis Machina Lt. Casper520012 on the fortification's platform (BossSpawnpoolPreloader) " +
                "dead is objective1, and drops Casper's Dogtags625 to whoever has objective2 open; the dogtags held are it (both open " +
                "from the start, as a drop is the open objective's), " +
                "and Dr. Franja Corman107 (her finishing text) takes them with the mission. The marker of the second is on the " +
                "facility's door out, as she is in Ranja Gorge. The hidden objective3 is not made.");
            var scene = Scene();
            scene.ExistingFactObjectiveIds = new List<uint> { ReturnDogtags };
            scene.Items = new List<MissionItemBinding>
            {
                new("dogtags", DogtagsTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1, Drop: new MissionItemDropDefinition(new[] { Casper }, ReturnDogtags, CraterLake, 100, 1))
            };
            mission.Enable(scene);
        }

        /// <summary>Franja's: her photographs to Baruhi at the Memory Tree, and back to her.</summary>
        private static void MendingABrokenHeartRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, MendingABrokenHeart, "Mending A Broken Heart", 809, Franja, Franja, 10, WildernessCategory);
            mission.Reward(5000, 1000);
            mission.Prerequisite(GhostInTheMachine);
            mission.Objective(DeliverPhotos, 1647, 6734, 1, MissionObjectiveState.Incomplete);
            mission.Objective(ReturnToFranja, 1649, 6735, 2);
            mission.Transition(DeliverPhotos);
            mission.Conversation(DeliverPhotos, BaruhiPackage);
            mission.Action(DeliverPhotos, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("baruhi-takes-photos", MendingABrokenHeart, "photos", 1, MissionItemScope.AssignmentIssued));
            mission.RevealAndActivate(DeliverPhotos, ReturnToFranja, 2);
            mission.Transition(ReturnToFranja);
            mission.Conversation(ReturnToFranja, FranjaPackage);
            mission.Action(ReturnToFranja, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(DeliverPhotos, MissionIndicator.UnnamedFrom + DeliverPhotos, BaruhiAtMemoryTree.X, BaruhiAtMemoryTree.Y, BaruhiAtMemoryTree.Z);
            mission.Indicator(ReturnToFranja, MissionIndicator.UnnamedFrom + ReturnToFranja, FranjaAtRanja.X, FranjaAtRanja.Y, FranjaAtRanja.Z);
            Evidence(mission, "Franja107 gives it once Ghost in the Machine1059 is done (the instance's own completion), with Franja " +
                "Corman's Photographs1760; Council Elder Baruhi106 (package227) at the Memory Tree takes them, and Franja " +
                "(package118, given her row here) takes the mission. Both conversations are the client's (446.1, 446.2).");
            var scene = Scene();
            scene.Items = new List<MissionItemBinding> { Issued("photos", PhotosTemplate) };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("franja-photos", MendingABrokenHeart, "photos", PhotosTemplate, 1)
            };
            mission.Enable(scene);
        }

        /// <summary>Sten Corman's: five crystals out of the facility's old storage crates.</summary>
        private static void CentrifugeFuelRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CentrifugeFuel, "Centrifuge Fuel", 1166, Sten, Sten, 9, WildernessCategory);
            mission.Reward(6000, 1200);
            mission.Objective(FindCrystals, 1177, 6652, 1, MissionObjectiveState.Incomplete);
            mission.Transition(FindCrystals);
            mission.Progress(FindCrystals, MissionProgressEventKind.ItemAcquired, CrystalClass, target: CrystalsWanted);
            mission.Action(FindCrystals, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(FindCrystals, MissionIndicator.UnnamedFrom + FindCrystals, WildernessDoor.X, WildernessDoor.Y, WildernessDoor.Z);
            Evidence(mission, "Operations Chief Sten Corman128 at Ranja Gorge gives and takes it. The storage crates " +
                "(UsableTreasureDispHumCrateV02, the mesh of the client's MisTreasureDispenserCLRFCrystals) in the destroyed " +
                "buildings give a Centrifuge Crystal626 each to whoever is on it and are empty a minute or two (the walkthrough). " +
                "Five, which the turn-in takes. The marker is on the Wilderness' door to the facility.");
            var scene = Scene();
            scene.ExistingFactObjectiveIds = new List<uint> { FindCrystals };
            scene.Items = new List<MissionItemBinding>
            {
                new("crystals", CrystalTemplate, MissionItemScope.CharacterOwned, CrystalsWanted,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: CrystalsWanted)
            };
            mission.Enable(scene);
        }

        /// <summary>Standley's, as his other Logos missions are: the facility's three shrines.</summary>
        private static void LogosRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LogosMovementAroundChaos, "Logos: Movement, Around, Chaos", 5217,
                Standley, Standley, 10, WildernessCategory);
            mission.Reward(12000, 3000);
            uint ordinal = 1;
            foreach (var (objective, name, body, logos) in new (uint, uint, uint, uint)[]
                     {
                         (4, 5226, 7609, MovementLogos), (5, 5227, 7610, AroundLogos), (6, 5228, 7611, ChaosLogos)
                     })
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.LogosAcquired, logos);
            }
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);
            Evidence(mission, "Receptive Liaison Standley134 at Twin Pillars gives and takes it, as the Wilderness' other Logos " +
                "missions. Movement50 on the north side of the crater, Around45 and Chaos4 in the Bane supply pen. Logos already " +
                "held count. No markers: the text sends the player to Velns.");
            var scene = Scene();
            scene.ExistingFactObjectiveIds = new List<uint> { 4, 5, 6 };
            mission.Enable(scene);
        }

        /// <summary>
        /// HQ's, by radio after Destroying the Evidence: the detonators from the weapons cache, the
        /// generator that keeps the base's force fields up, the Overseer, the pen's panel, the two
        /// Logos in the pen; Daniel takes it.
        /// </summary>
        private static void ReclaimingWhatsOursRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ReclaimingWhatsOurs, "Reclaiming What's Ours", 5931, null, Daniel, 11, Category);
            mission.Reward(15000, 2500, selectableItems: new[] { Item(EclipseLaserRifle, 1, ThreatReduction), Item(PulsarReflectiveGloves, 1, ResistEmp) });
            mission.Prerequisite(DestroyingTheEvidence);
            mission.Objective(GatherBombs, 5991, 6990, 1, MissionObjectiveState.Incomplete);
            mission.Objective(BlowGenerator, 5997, 6991, 2);
            mission.Objective(KillOverseer, 5994, 6994, 3);
            mission.Objective(UsePanel, 5995, 6995, 4);
            mission.Objective(AroundFromPen, 5992, 6992, 5);
            mission.Objective(ChaosFromPen, 5993, 6993, 6);

            mission.Transition(GatherBombs);
            mission.Progress(GatherBombs, MissionProgressEventKind.ItemAcquired, DetonatorClass, target: BombsWanted);
            mission.RevealAndActivate(GatherBombs, BlowGenerator);

            mission.Transition(BlowGenerator);
            mission.Progress(BlowGenerator, MissionProgressEventKind.ScenarioEvent, BaseOpen, counter: Scenario);
            mission.RevealAndActivate(BlowGenerator, KillOverseer);

            mission.Transition(KillOverseer);
            mission.Progress(KillOverseer, MissionProgressEventKind.ScenarioEvent, OverseerDown, counter: Scenario);
            mission.RevealAndActivate(KillOverseer, UsePanel);

            mission.Transition(UsePanel);
            mission.Progress(UsePanel, MissionProgressEventKind.InteractionUsed, KeyboardClass);
            mission.RevealAndActivate(UsePanel, AroundFromPen, 1);
            mission.RevealAndActivate(UsePanel, ChaosFromPen, 3);

            mission.Transition(AroundFromPen);
            mission.Progress(AroundFromPen, MissionProgressEventKind.LogosAcquired, AroundLogos);
            mission.Action(AroundFromPen, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(ChaosFromPen);
            mission.Progress(ChaosFromPen, MissionProgressEventKind.LogosAcquired, ChaosLogos);

            mission.Scenario(Scenario, "Reclaiming What's Ours", BaseOpen, OverseerDown);

            mission.Indicator(GatherBombs, MissionIndicator.UnnamedFrom + GatherBombs, Caches[0].X, Caches[0].Y, Caches[0].Z);
            mission.Indicator(BlowGenerator, GeneratorMarker, OuterGenerator.X, OuterGenerator.Y, OuterGenerator.Z);
            mission.Indicator(KillOverseer, MissionIndicator.UnnamedFrom + KillOverseer, Courtyard.X, Courtyard.Y, Courtyard.Z, 25);
            mission.Indicator(UsePanel, MissionIndicator.UnnamedFrom + UsePanel, Panel.X, Panel.Y, Panel.Z);
            mission.Indicator(AroundFromPen, MissionIndicator.UnnamedFrom + AroundFromPen, AroundShrine.X, AroundShrine.Y, AroundShrine.Z);
            mission.Indicator(ChaosFromPen, MissionIndicator.UnnamedFrom + ChaosFromPen, ChaosShrine.X, ChaosShrine.Y, ChaosShrine.Z);

            mission.Channels(MissionChannel.Radio, MissionChannel.Npc,
                MissionOfferSourceDefinition.MissionRewarded(DestroyingTheEvidence),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, CraterLake));

            Evidence(mission, "HQ's (the opening reads the Bane database's intel): offered by radio at the turn-in of Destroying the " +
                "Evidence1055 and again on coming into the facility. The weapons caches on the fortification's platform give two " +
                "Generator Detonators11487 each (two held is objective5). A detonator set at the generator by the fortification's " +
                "pipes (the walkthrough's (-253.8, -102.5)) blows it, and the force fields in the stronghold's west and north ways " +
                "in go down for the copy: event1. The Overseer in the courtyard - his shield up save while Sonic or Lightning breaks " +
                "it, and for a short while every so often; drop pods of Hunters while he fights - dead is event2. The pen's panel " +
                "(package2581, 'Access Granted.') on the courtyard's catwalk answers once the second generator, in the courtyard, " +
                "is blown too; used, the pen's field goes down, and its Around45 and Chaos4 shrines are the last two. Daniel " +
                "Corman675002 in the pen takes it (his finishing text). An Eclipse Laser Rifle17337 with Threat Reduction[1]100052 " +
                "or Pulsar Reflective Armor Gloves35645 with Resist EMP[1]100048.");
            var scene = Scene(events: true);
            scene.ExistingFactObjectiveIds = new List<uint> { GatherBombs, AroundFromPen, ChaosFromPen };
            mission.Enable(scene);
        }

        /// <summary>HQ's, by radio after Reclaiming What's Ours: Daniel walked to Velns; not with Bending the Rules.</summary>
        private static void ReturnToSenderRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ReturnToSender, "Return to Sender", 3337, null, Velns, 11, Category);
            mission.Reward(12000, 1800, selectableItems: new[] { Item(PulsarHazmatVest, 1, ResistEmp), Item(LaserChaingun) });
            mission.Prerequisite(ReclaimingWhatsOurs);
            mission.Objective(EscortToVelns, 3344, 6915, 1, MissionObjectiveState.Incomplete);
            mission.Transition(EscortToVelns);
            mission.Progress(EscortToVelns, MissionProgressEventKind.ScenarioEvent, DanielDelivered, counter: Scenario);
            mission.Action(EscortToVelns, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Return to Sender", DanielDelivered);
            mission.Indicator(EscortToVelns, MissionIndicator.UnnamedFrom + EscortToVelns, VelnsSpot.X, VelnsSpot.Y, VelnsSpot.Z);
            mission.Channels(MissionChannel.Radio, MissionChannel.Npc,
                MissionOfferSourceDefinition.MissionRewarded(ReclaimingWhatsOurs),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, CraterLake));
            Evidence(mission, "The orders are HQ's ('Your orders are to return all research materials'): offered by radio at the turn-in " +
                "of Reclaiming What's Ours1056. Daniel675002 follows whoever is on it; within reach of Velns it is event1, and " +
                "Velns (package23, his finishing text) takes it. Not for one who has taken Bending the Rules1065, as Daniel's " +
                "plea and HQ's orders are the walkthrough's choice. The walkthrough calls its reward like Bending the Rules': " +
                "1800 credits, Pulsar Hazmat Armor Vest20856 with Resist EMP[1]100048 or the Laser Chaingun42440.");
            var scene = Scene(events: true);
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, NotTaken(BendingTheRules) });
            mission.Enable(scene);
        }

        /// <summary>Daniel's plea: out of the facility; not with Return to Sender.</summary>
        private static void BendingTheRulesRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, BendingTheRules, "Bending the Rules", 5998, Daniel, Daniel, 11, Category);
            mission.Reward(12000, 1800, selectableItems: new[] { Item(PulsarHazmatVest, 1, ResistEmp), Item(LaserChaingun) });
            mission.Prerequisite(ReclaimingWhatsOurs);
            mission.Objective(EscortOut, 6235, 6913, 1, MissionObjectiveState.Incomplete);
            mission.Transition(EscortOut);
            mission.Progress(EscortOut, MissionProgressEventKind.ScenarioEvent, DanielOut, counter: Scenario);
            mission.Action(EscortOut, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Bending the Rules", DanielOut);
            mission.Indicator(EscortOut, MissionIndicator.UnnamedFrom + EscortOut, Arrival.X, Arrival.Y, Arrival.Z);
            Evidence(mission, "Daniel675002 asks it himself (the opening is his) once Reclaiming What's Ours1056 is done. He follows " +
                "whoever is on it; at the facility's door out it is event1, and he takes it there (his finishing text). Not for " +
                "one who has taken Return to Sender711. The walkthrough's reward: 1800 credits and a Pulsar Hazmat Armor " +
                "Vest20856 with Resist EMP[1]100048 or the Laser Chaingun42440.");
            var scene = Scene(events: true);
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, NotTaken(ReturnToSender) });
            mission.Enable(scene);
        }

        /// <summary>Neither on the mission now nor done with it.</summary>
        private static MissionRequirement NotTaken(uint missionId) =>
            new NotRequirement(new AnyRequirement(new MissionRequirement[]
            {
                new MissionStateRequirement(missionId, Accepted: true), new MissionStateRequirement(missionId)
            }));

        /// <summary>
        /// A mission's binding: for the bootcamp's graduates. One whose events the facility records
        /// has a scenario, which a mission has only with a script; the data sequence with no
        /// sequences is one that does nothing.
        /// </summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static MissionItemBinding Issued(string key, uint template) =>
            new(key, template, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove);

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide,
                "The Crater Lake Research Facility walkthrough: route, coordinates, order and rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "CraterLakeContent", reconstruction);
        }

        #endregion
    }
}
