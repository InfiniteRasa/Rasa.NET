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
    /// Pravus Research Facility (adv_foreas_concordia_wilderness_pravusresearch, 1430), entered
    /// from the Wilderness, as the Tabula Rasa wiki's walkthrough tells it: its people, its Bane
    /// and Machina, and its six missions, in the client's own names and texts.
    ///
    /// Who and what:
    ///  - Ranger Nylla at the Forean encampment by the way in, who takes The Escapist (given by
    ///    Lt. Cmdr. Parsons outside) and gives The Means of Production; Nylla again in the
    ///    facility's overlook hall once the fueling capsule is down (PravusResearch), who takes
    ///    it and gives A Higher Calling, handed in to Council Elder Baruhi at the Memory Tree.
    ///  - Cmd. Sgt. Price north of the way in, who gives Assemble With Lieutenant Perkins: fifteen
    ///    Machina at the Frontlines Entrance, where three AFS riflemen hold the line; Field Lt.
    ///    Perkins at the north west fortification takes it.
    ///  - Information Spec. Johnson by the main pathway (the unplaced row 400401), who gives and
    ///    takes Pirate Radio: three Living Infestations on each of the three communication dishes.
    ///  - Forean Ranger Maulis in a cell of the prison, the bonus of The Means of Production.
    ///  - Logos: Communication, Control, Machine, given and taken by Receptive Liaison Standley at
    ///    Twin Pillars, as the Wilderness' other Logos missions are.
    ///  - Machina on the hills, Thrax at the fortifications on the way up, two Predators and a
    ///    Stalker before the Bane wall, Thrax guarding its gate (the gate's Grenadier carries the
    ///    Level 1 Keypass), Thrax about the research HQ and in the facility, Linkers in the prison.
    ///    Level 10 to 12, for a squad of level 10: health 10000 x 2^((level - 42) / 8), the curve
    ///    the Wilderness rows sit on, a minion three quarters of it, a lieutenant one and three
    ///    quarters, the Stalker four times; armour ten a level less twenty.
    /// The gate's force field and its keypass scanner, the fueling capsule and the endless
    /// Machina it feeds, the ambush after it, the infestations, the Machina at the frontline and
    /// the scenario events the missions wait for are the server's, per copy of the facility
    /// (PravusResearch).
    /// </summary>
    public static class PravusResearchContent
    {
        public const string Revision = "pravus_1_16_5";
        public const uint Pravus = 1430, Wilderness = 1220;

        /// <summary>missioncategorylanguage: "Instance (Pravus Research)".</summary>
        public const uint Category = 10000048;

        public const uint Escapist = 593, MeansOfProduction = 575, AssembleWithPerkins = 321, PirateRadio = 323,
            CommunicationControlMachine = 924, HigherCalling = 1075;
        public static readonly uint[] Missions = { Escapist, MeansOfProduction, AssembleWithPerkins, PirateRadio, CommunicationControlMachine, HigherCalling };

        /// <summary>Machinations, which Parsons takes before he sends you to Nylla.</summary>
        public const uint Machinations = 574;

        // The NPC packages the missions' conversations are in.
        public const uint NyllaPackage = 420, MaulisPackage = 468, PricePackage = 105, JohnsonPackage = 106, BaruhiPackage = 227;

        // The people already in the world.
        public const uint Parsons = 119, Baruhi = 106, Standley = 134, Johnson = UnplacedNamedNpcs.Johnson;

        public const uint NyllaCamp = 650001, NyllaControl = 650002, Price = 650003, Perkins = 650004, Maulis = 650005, Rifleman = 650006;
        public const uint Machina = 650010, ProductionMachina = 650011;
        public const uint Thrax = 650020, Grenadier = 650021, GateGrenadier = 650022, AmbushThrax = 650023, Linker = 650024;
        public const uint Predator = 650025, Stalker = 650026;

        /// <summary>Every Bane and Machina row of the facility: what PravusResearch counts as its hostiles.</summary>
        public static readonly uint[] Hostiles = { Machina, ProductionMachina, Thrax, Grenadier, GateGrenadier, AmbushThrax, Linker, Predator, Stalker };

        // The scenarios whose events PravusResearch records, and their events.
        public const uint ChamberScenario = 1, SourceFound = 1, CapsuleDestroyed = 2;
        public const uint AmbushScenario = 2, AmbushCount = 6;
        public const uint NorthDishScenario = 1, NorthwestDishScenario = 2, SouthDishScenario = 3, InfestationsPerDish = 3;

        // Pirate Radio's objectives, one a dish.
        public const uint NorthDish = 309, NorthwestDish = 310, SouthDish = 311;

        // The Means of Production's objectives.
        public const uint FindTheSource = 1, DestroyTheCapsule = 2, ObtainTheKeypass = 3, SpeakWithMaulis = 4, Ambush = 5;

        /// <summary>Assemble With Lieutenant Perkins' one objective, and how many Machina.</summary>
        public const uint DefeatTheMachina = 310, MachinaToKill = 15;

        /// <summary>The Level 1 Keypass: "Pravus Research Facility: Level 1 Keypass".</summary>
        public const uint KeypassClass = 20000013, KeypassTemplate = 11505;

        /// <summary>Ranger Nylla's Armband.</summary>
        public const uint ArmbandClass = 22953, ArmbandTemplate = 45439;

        // The objects PravusResearch puts down.
        public const uint ForceFieldClass = 9572, KeypassScannerClass = 29046, CapsuleClass = 9272, InfestationClass = 6225;
        public const uint CapsuleHitPoints = 3000;

        // Logos ids of the three shrines (LogosPreloader).
        public const uint CommunicationLogos = 51, ControlLogos = 41, MachineLogos = 34;

        /// <summary>
        /// missionobjectiveindicatorlanguage: Frontlines Entrance, Ranger Maulis Holding Cell,
        /// Machina Fueling Capsule, Pravus Research Facility Entrance, and the south, north and
        /// west fortifications' comm. dishes.
        /// </summary>
        public const uint FrontlinesMarker = 17, MaulisCellMarker = 52, CapsuleMarker = 53, FacilityEntranceMarker = 54,
            SouthDishMarker = 78, NorthDishMarker = 79, WestDishMarker = 80;

        // Where things are, on the facility's navmesh (x, height, z).
        public static readonly Vector3 Arrival = new(-193.5236f, 9.9638f, -257.405f);
        public static readonly Vector3 WildernessDoor = new(-786.2202f, 233.6148f, 71.5645f);
        public static readonly Vector3 NyllaAtCamp = new(-185f, 5.14f, -123f);
        public static readonly Vector3 PriceSpot = new(-268f, 7.16f, -127f);
        public static readonly Vector3 PerkinsSpot = new(-77f, 32.02f, 176f);
        public static readonly Vector3 JohnsonSpot = new(-83f, 33.47f, -132f);
        public static readonly Vector3 MaulisCell = new(321f, 11.48f, 45.5f);
        public static readonly Vector3 Frontline = new(-116.4f, 26.0f, 1.6f);
        public static readonly Vector3[] Riflemen = { new(-120f, 26.67f, 0f), new(-112f, 26.5f, 6f), new(-124f, 28.69f, -8f) };
        public static readonly Vector3[] FrontlineSpawns = { new(-80f, 23.93f, 0f), new(-84f, 24.47f, 12f), new(-84f, 25.54f, -12f) };

        /// <summary>The Bane wall's major entrance (the .map's arch_bane_outpost_wall_major_entrance_v01), facing the way up.</summary>
        public static readonly Vector3 Gate = new(74f, 54f, -40f);
        public const float GateYaw = 1.5707964f;

        /// <summary>The keypass scanner, to the right of the field as it is come at.</summary>
        public static readonly Vector3 Scanner = new(69f, 55.5f, -58f);
        public const float ScannerYaw = -1.5707964f;

        public static readonly Vector3 GateGuards = new(65f, 58.3f, -40f);
        public static readonly Vector3 FacilityEntrance = new(176f, 41.81f, -8f);

        /// <summary>The control room's floor, north of its central pillar: the capsule.</summary>
        public static readonly Vector3 Capsule = new(224f, 8.01f, 50f);
        public static readonly Vector3 Chamber = new(224f, 8.0f, 40f);
        public static readonly Vector3[] ProductionSpawns = { new(218f, 7.74f, 46f), new(230f, 7.74f, 46f) };
        public static readonly Vector3[] AmbushSpawns =
        {
            new(220f, 8.14f, 18f), new(228f, 8.14f, 18f), new(224f, 8.8f, 14f),
            new(220f, 10.14f, 62f), new(228f, 10.07f, 62f), new(224f, 10.14f, 64f)
        };

        /// <summary>The overlook hall, the Bane teleporters about it: where Nylla is once the capsule is down.</summary>
        public static readonly Vector3 ControlCentre = new(224f, 8.57f, 168f);

        /// <summary>The communication dishes (the .map's prop_hum_radar_v01): north, north west, south.</summary>
        public static readonly Vector3 NorthDishSpot = new(-2.9f, 47.4f, 298.7f);
        public static readonly Vector3 NorthwestDishSpot = new(-54.6f, 32.9f, 163.0f);
        public static readonly Vector3 SouthDishSpot = new(-47.3f, 36.4f, -224.1f);

        /// <summary>How far from a dish's middle its infestations are, at a third of a turn from each other.</summary>
        public const float InfestationRadius = 5f;

        public static readonly Vector3 CommunicationShrine = new(-178.58203f, 7.4375f, -86.91016f);
        public static readonly Vector3 ControlShrine = new(-183.82031f, 59.8242188f, 132.21875f);
        public static readonly Vector3 MachineShrine = new(-54.960938f, 43.996094f, -317.3164f);

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
        public const uint MachinaWeapon = 630103, MachinaRevive = 62059;
        public const uint ThraxRifle = 70031, ThraxMelee = 3;
        public const uint GrenadierGrenade = 19, GrenadierRifle = 20;
        public const uint LinkerBeam = 36, LinkerChannel = 66001;
        public const uint AfsRifle = 72003;

        public static readonly Act[] Actions =
        {
            new(650101, "Predator - Pravus predator weapon 1/85", 1, 85, 1, 50, 3000, 0, 40, 60, 6),
            new(650102, "Stalker - Pravus 1/78", 1, 78, 1, 60, 1500, 0, 50, 75, 6)
        };

        public static readonly Beast[] Creatures =
        {
            new(NyllaCamp, "Ranger Nylla - Pravus encampment", ForeanSpearman, TargetCategoryRow.Friendly, 12, 3000, 0, 4843, 0, 0),
            new(NyllaControl, "Ranger Nylla - Pravus control centre", ForeanSpearman, TargetCategoryRow.Friendly, 12, 3000, 0, 4843, 0, 0),
            new(Price, "Cmd. Sgt. Price - Pravus", SwapsetMale, TargetCategoryRow.Friendly, 12, 3000, 0, 205, 0, 0),
            new(Perkins, "Field Lt. Perkins - Pravus north west fortification", SwapsetMale, TargetCategoryRow.Friendly, 12, 3000, 0, 204, 0, 0),
            new(Maulis, "Forean Ranger Maulis - Pravus prison", ForeanSpearman, TargetCategoryRow.Friendly, 10, 1500, 0, 5286, 0, 0),
            new(Rifleman, "AFS Infantryman - Pravus frontline", Infantryman, TargetCategoryRow.Friendly, 10, 1000, 60, 8716, 0, 0, AfsRifle),
            new(Machina, "Forean Machina - Pravus", 6236, TargetCategoryRow.Hostile, 10, 625, 80, 0, 9, 5, MachinaWeapon, MachinaRevive),
            new(ProductionMachina, "Forean Machina - Pravus production", 6236, TargetCategoryRow.Hostile, 10, 470, 80, 0, 9, 5, MachinaWeapon, MachinaRevive),
            new(Thrax, "Thrax Soldier - Pravus", 20757, TargetCategoryRow.Hostile, 10, 625, 80, 235, 9, 5, ThraxRifle, ThraxMelee),
            new(Grenadier, "Thrax Grenadier - Pravus", 9354, TargetCategoryRow.Hostile, 11, 680, 90, 0, 9, 5, GrenadierGrenade, GrenadierRifle),
            new(GateGrenadier, "Thrax Grenadier - Pravus gate, the keypass", 9354, TargetCategoryRow.Hostile, 11, 680, 90, 0, 9, 5, GrenadierGrenade, GrenadierRifle),
            new(AmbushThrax, "Thrax Soldier - Pravus ambush", 20757, TargetCategoryRow.Hostile, 10, 625, 80, 235, 9, 5, ThraxRifle, ThraxMelee),
            new(Linker, "Bane Linker - Pravus prison", 9751, TargetCategoryRow.Hostile, 10, 625, 80, 419, 9, 5, LinkerBeam, LinkerChannel),
            new(Predator, "Predator - Pravus approach", 3902, TargetCategoryRow.Hostile, 11, 1190, 100, 0, 9, 5, 650101),
            new(Stalker, "Stalker - Pravus approach", 3781, TargetCategoryRow.Hostile, 12, 3000, 100, 0, 9, 5, 650102)
        };

        /// <summary>Price and Perkins wear the brown officer's uniform Johnson does (UnplacedNamedNpcs).</summary>
        public static readonly Slot[] PriceLook = Officer(UnplacedNamedNpcs.FaceCaucasianRound1, UnplacedNamedNpcs.HairBald, DarkHair, SkinMedium);
        public static readonly Slot[] PerkinsLook = Officer(UnplacedNamedNpcs.FaceCaucasianStandard2, WildernessPostNpcs.Hair06, WildernessPostNpcs.BrownHair, SkinLight);

        /// <summary>The frontline's riflemen look as the Alia Das infantryman at ease does.</summary>
        public const uint RiflemanLooksLike = GuardInfantryman;

        private static Slot[] Officer(uint face, uint hair, uint hairColor, uint skin) => new[]
        {
            new Slot(2, OfficerBoots, Brown),
            new Slot(13, PistolRedshirt, 1),
            new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, Brown),
            new Slot(16, OfficerLegs, Brown),
            new Slot(17, face, skin)
        };

        public const uint NyllaPool = 650201, PricePool = 650202, PerkinsPool = 650203, JohnsonPool = 650204, MaulisPool = 650205,
            FirstRiflemanPool = 650206;

        public const uint NpcRespawn = 200, HostileRespawn = 3000;

        public static readonly Spot[] Pools =
        {
            // The people.
            new(NyllaPool, Pravus, NyllaAtCamp.X, NyllaAtCamp.Y, NyllaAtCamp.Z, 0.6, NpcRespawn, 0, (NyllaCamp, 1, 1)),
            new(PricePool, Pravus, PriceSpot.X, PriceSpot.Y, PriceSpot.Z, 1.4, NpcRespawn, 0, (Price, 1, 1)),
            new(PerkinsPool, Pravus, PerkinsSpot.X, PerkinsSpot.Y, PerkinsSpot.Z, 2.2, NpcRespawn, 0, (Perkins, 1, 1)),
            new(JohnsonPool, Pravus, JohnsonSpot.X, JohnsonSpot.Y, JohnsonSpot.Z, 1.6, NpcRespawn, 0, (Johnson, 1, 1)),
            new(MaulisPool, Pravus, MaulisCell.X, MaulisCell.Y, MaulisCell.Z, -1.5708, NpcRespawn, 0, (Maulis, 1, 1)),
            new(FirstRiflemanPool, Pravus, Riflemen[0].X, Riflemen[0].Y, Riflemen[0].Z, 1.5708, NpcRespawn, 0, (Rifleman, 1, 1)),
            new(FirstRiflemanPool + 1, Pravus, Riflemen[1].X, Riflemen[1].Y, Riflemen[1].Z, 1.5708, NpcRespawn, 0, (Rifleman, 1, 1)),
            new(FirstRiflemanPool + 2, Pravus, Riflemen[2].X, Riflemen[2].Y, Riflemen[2].Z, 1.5708, NpcRespawn, 0, (Rifleman, 1, 1)),

            // Machina on the hills between the frontline and the fortifications.
            new(650210, Pravus, -60, 24.87, 60, 1.6, HostileRespawn, 6, (Machina, 2, 3)),
            new(650211, Pravus, -50, 24.28, -60, 1.6, HostileRespawn, 6, (Machina, 2, 3)),
            new(650212, Pravus, -30, 23.09, 0, 1.6, HostileRespawn, 6, (Machina, 2, 3)),
            new(650213, Pravus, -30, 28.31, 100, 1.6, HostileRespawn, 6, (Machina, 2, 2)),
            new(650214, Pravus, -20, 24.19, -120, 1.6, HostileRespawn, 6, (Machina, 2, 2)),

            // The Thrax fortifications on the way up.
            new(650220, Pravus, 5, 33.68, -15, -1.6, HostileRespawn, 5, (Thrax, 2, 3), (Grenadier, 1, 1)),
            new(650221, Pravus, -5, 25.34, 35, -1.6, HostileRespawn, 5, (Thrax, 2, 3)),
            new(650222, Pravus, 12, 25.94, 125, -1.6, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(650223, Pravus, 10, 41.14, -50, -1.6, HostileRespawn, 5, (Thrax, 2, 3)),

            // Before the Bane wall: the Predators, the Stalker, and the gate's guards.
            new(650225, Pravus, 35, 52.74, -20, -1.6, HostileRespawn, 4, (Predator, 1, 1)),
            new(650226, Pravus, 35, 46.19, -60, -1.6, HostileRespawn, 4, (Predator, 1, 1)),
            new(650227, Pravus, 48, 58.58, -40, -1.6, HostileRespawn, 0, (Stalker, 1, 1)),
            new(650228, Pravus, 63, 60.8, -35, -1.6, HostileRespawn, 0, (Thrax, 1, 1)),
            new(650229, Pravus, 63, 58.4, -45, -1.6, HostileRespawn, 0, (Thrax, 1, 1)),
            new(650230, Pravus, GateGuards.X, GateGuards.Y, GateGuards.Z, -1.6, HostileRespawn, 0, (GateGrenadier, 1, 1)),

            // The research HQ and the compound about it.
            new(650231, Pravus, 180, 41.74, -8, -1.6, HostileRespawn, 5, (Thrax, 2, 2), (Grenadier, 1, 1)),
            new(650232, Pravus, 150, 32.94, 30, -1.6, HostileRespawn, 5, (Thrax, 2, 2)),
            new(650233, Pravus, 150, 33.14, -50, -1.6, HostileRespawn, 5, (Thrax, 2, 2)),

            // Inside: the tunnels' cross, the side rooms, and the prison.
            new(650240, Pravus, 224, 9.89, -24, 0, HostileRespawn, 4, (Thrax, 2, 2)),
            new(650241, Pravus, 128, 17.37, 96, 0, HostileRespawn, 4, (Thrax, 2, 2)),
            new(650242, Pravus, 152, 17.14, 72, 0, HostileRespawn, 4, (Thrax, 1, 1), (Grenadier, 1, 1)),
            new(650243, Pravus, 136, 17.19, 120, 0, HostileRespawn, 4, (Thrax, 2, 2)),
            new(650244, Pravus, 272, 17.26, -32, 0, HostileRespawn, 4, (Thrax, 2, 2)),
            new(650245, Pravus, 309, 9.81, 50, 0, HostileRespawn, 2, (Linker, 1, 1)),
            new(650246, Pravus, 309, 9.74, 66, 3.1, HostileRespawn, 2, (Linker, 1, 1), (Thrax, 1, 1))
        };

        /// <summary>The loot each hostile row drops on the facility's map: the Wilderness' own profiles for its kind.</summary>
        public static readonly (uint Creature, string Profile)[] Loot =
        {
            (Machina, "wilderness_v1_machina_5_9"), (ProductionMachina, "wilderness_v1_machina_5_9"),
            (Thrax, "wilderness_v1_thrax_10_14"), (Grenadier, "wilderness_v1_thrax_10_14"),
            (GateGrenadier, "wilderness_v1_thrax_10_14"), (AmbushThrax, "wilderness_v1_thrax_10_14"),
            (Linker, "wilderness_v1_thrax_10_14"), (Predator, "wilderness_v1_predator_10_14"),
            (Stalker, "wilderness_v1_thrax_10_14_boss")
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

            foreach (var sql in Change(Price, new Slot[0], PriceLook))
                yield return sql;
            foreach (var sql in Change(Perkins, new Slot[0], PerkinsLook))
                yield return sql;

            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {Rifleman}, a.slot_id, a.Class_id, a.color " +
                $"FROM creature_appearance a WHERE a.id = {RiflemanLooksLike} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Rifleman}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {Rifleman});";

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
                    $"SELECT {Pravus}, {creature}, {S(profile)} FROM creature c WHERE c.id = {creature} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding b WHERE b.map_context_id = {Pravus} AND b.creature_id = {creature});";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var (creature, profile) in Loot)
                yield return $"DELETE FROM creature_loot_binding WHERE map_context_id = {Pravus} AND creature_id = {creature} AND profile_key = {S(profile)};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Slots[0].Creature};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            yield return $"DELETE FROM creature_appearance WHERE id IN ({Price}, {Perkins}, {Rifleman});";

            foreach (var c in Creatures)
            {
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id};";
        }

        /// <summary>Who speaks which package: the facility's people, and Johnson, whose row had none.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (NyllaCamp, NyllaPackage, "Ranger Nylla - Pravus encampment"),
            (NyllaControl, NyllaPackage, "Ranger Nylla - Pravus control centre"),
            (Price, PricePackage, "Cmd. Sgt. Price"),
            (Maulis, MaulisPackage, "Forean Ranger Maulis"),
            (Johnson, JohnsonPackage, "Information Spec. Johnson")
        };

        #region Missions

        private const string Guide = "https://tabularasa.fandom.com/wiki/Pravus_Research_Facility";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            EscapistRows(sql);
            MeansOfProductionRows(sql);
            AssembleWithPerkinsRows(sql);
            PirateRadioRows(sql);
            LogosRows(sql);
            HigherCallingRows(sql);
            return sql;
        }

        // Rewards: the guide's credits; the experience is ours. Each piece of gear is the nearest
        // level 9 to 13 uncommon of its kind, with the module its name's brand prefix is: Olympia
        // Resist Physical, Pulsar Resist EMP, AccuMax Crit Hit Bonus, Eclipse Threat Reduction,
        // Vitalius Steal Health, each [1] as the Wilderness rewards have them (WildernessRewardModules).
        public const uint ResistPhysical = 100020, ResistEmp = 100048, CritHitBonus = 100056, ThreatReduction = 100052, StealHealth = 900265;
        public const uint VitaliusEmpRifle = 17382, PulsarHazmatHelmet = 20558, OlympiaReflectiveVest = 36093,
            PulsarMotorAssistLegs = 13744, AccuMaxLeechGun = 42397, EclipsePulseShotgun = 17704;
        public const uint EmpGrenade = 111022, StandardMedPack = 44918;

        private static InstanceMissionSql.RewardItem Item(uint template, uint quantity = 1, uint module = 0) => new(template, quantity, module);

        /// <summary>Parsons, outside, sends you to Nylla at the encampment (package 420).</summary>
        private static void EscapistRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Escapist, "The Escapist", 1871, Parsons, NyllaCamp, 10, Category);
            mission.Reward(8000, 1200);
            mission.Objective(1, 1875, 6721, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, NyllaPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, FacilityEntranceMarker, WildernessDoor.X, WildernessDoor.Y, WildernessDoor.Z);
            Evidence(mission, "Lt. Cmdr. Parsons119 near the Pravus entrance gives it once Machinations574 is done; Ranger Nylla at the " +
                "Forean encampment by the way in (package420) takes it. The marker is on the Wilderness' door to Pravus. Machinations " +
                "is the Wilderness' (another content revision), so it is the binding's requirement, not a prerequisite row.");
            var scene = Scene();
            scene.Requirement = new AllRequirements(new MissionRequirement[]
            {
                scene.Requirement, new MissionStateRequirement(Machinations, MissionState.Completed)
            });
            mission.Enable(scene);
        }

        /// <summary>
        /// Nylla's: through the Bane gate with the keypass, the source found in the Machina chamber,
        /// its fueling capsule destroyed, the ambush that comes after, and Nylla in the overlook hall.
        /// </summary>
        private static void MeansOfProductionRows(List<string> sql)
        {
            // Not shareable: a mission whose events the facility records has a script. The events
            // are everyone's on the objective, so the squad does it together all the same.
            var mission = new InstanceMissionSql(sql, Revision, MeansOfProduction, "The Means of Production", 1756, NyllaCamp, NyllaControl, 10,
                Category);
            mission.Reward(15000, 2000, selectableItems: new[] { Item(VitaliusEmpRifle, 1, StealHealth), Item(PulsarHazmatHelmet, 1, ResistEmp) });
            mission.Prerequisite(Escapist);
            mission.Objective(FindTheSource, 1865, 6636, 1, MissionObjectiveState.Incomplete);
            mission.Objective(DestroyTheCapsule, 1867, 6637, 2);
            mission.Objective(ObtainTheKeypass, 20000025, 6640, 3, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(SpeakWithMaulis, 20000028, 6638, 4, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Ambush, 20000033, 6639, 5, counterTextId: 20000034);

            mission.Transition(FindTheSource);
            mission.Progress(FindTheSource, MissionProgressEventKind.ScenarioEvent, SourceFound, counter: ChamberScenario);
            mission.RevealAndActivate(FindTheSource, DestroyTheCapsule);

            mission.Transition(DestroyTheCapsule);
            mission.Progress(DestroyTheCapsule, MissionProgressEventKind.ScenarioEvent, CapsuleDestroyed, counter: ChamberScenario);
            mission.RevealAndActivate(DestroyTheCapsule, Ambush);

            mission.Transition(ObtainTheKeypass);
            mission.Progress(ObtainTheKeypass, MissionProgressEventKind.ItemAcquired, KeypassClass, target: 1);

            mission.Transition(SpeakWithMaulis);
            mission.Conversation(SpeakWithMaulis, MaulisPackage);

            mission.Transition(Ambush);
            for (uint ambusher = 1; ambusher <= AmbushCount; ambusher++)
                mission.Progress(Ambush, MissionProgressEventKind.ScenarioEvent, ambusher, counter: AmbushScenario, target: AmbushCount, trigger: ambusher);
            mission.Action(Ambush, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Scenario(ChamberScenario, "The Machina chamber", SourceFound, CapsuleDestroyed);
            mission.Scenario(AmbushScenario, "The ambush", Enumerable.Range(1, (int)AmbushCount).Select(i => (uint)i).ToArray());

            mission.Indicator(FindTheSource, FacilityEntranceMarker, FacilityEntrance.X, FacilityEntrance.Y, FacilityEntrance.Z);
            mission.Indicator(DestroyTheCapsule, CapsuleMarker, Capsule.X, Capsule.Y, Capsule.Z);
            mission.Indicator(ObtainTheKeypass, MissionIndicator.UnnamedFrom + ObtainTheKeypass, GateGuards.X, GateGuards.Y, GateGuards.Z, 15);
            mission.Indicator(SpeakWithMaulis, MaulisCellMarker, MaulisCell.X, MaulisCell.Y, MaulisCell.Z);
            mission.Indicator(Ambush, MissionIndicator.UnnamedFrom + Ambush, Chamber.X, Chamber.Y, Chamber.Z, 20);

            Evidence(mission, "The Bane wall's gate has a force field shots do nothing to; the keypass scanner to its right, used with the " +
                "Level 1 Keypass (class20000013, template11505: the gate's Thrax Grenadier650022 drops it to whoever is on the keypass " +
                "objective), takes it down for the whole copy. Coming within 25 m of the fueling capsule9272 in the control room is " +
                "the source found (event1 of scenario1); the capsule destroyed (3000 hit points) is event2, for everyone on the " +
                "objective. Until then the chamber turns out Machina without end. Six Bane come at the chamber then; each dead is an " +
                "event of scenario2, its counter Bane Remaining. Nylla650002 is in the overlook hall from then on and takes it. " +
                "Ranger Maulis (package468) in a cell of the prison is the bonus. The keypass objective is not required, as a squad " +
                "member who did not loot it still goes through the opened gate. Vitalius EMP Rifle17382 with Steal Health[1]900265 " +
                "and Pulsar Hazmat Armor Helmet20558 with Resist EMP[1]100048.");

            var scene = Scene(events: true);
            scene.Items = new List<MissionItemBinding>
            {
                // A drop is the character's own: kept, whatever becomes of the mission.
                new("keypass", KeypassTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    Drop: new MissionItemDropDefinition(new[] { GateGrenadier }, ObtainTheKeypass, Pravus, 100, 1))
            };
            mission.Enable(scene);
        }

        /// <summary>Price's: fifteen Machina at the frontline, handed in to Perkins at the north west fortification.</summary>
        private static void AssembleWithPerkinsRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, AssembleWithPerkins, "Assemble With Lieutenant Perkins", 15, Price, Perkins, 10,
                Category, shareable: true);
            mission.Reward(8000, 1200, selectableItems: new[] { Item(OlympiaReflectiveVest, 1, ResistPhysical), Item(PulsarMotorAssistLegs, 1, ResistEmp) });
            mission.Objective(DefeatTheMachina, 115, 6522, 1, MissionObjectiveState.Incomplete, counterTextId: 1711);
            mission.Transition(DefeatTheMachina);
            mission.Progress(DefeatTheMachina, MissionProgressEventKind.CreatureKilled, Machina, counter: 0, target: MachinaToKill);
            mission.Action(DefeatTheMachina, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(DefeatTheMachina, FrontlinesMarker, Frontline.X, Frontline.Y, Frontline.Z, 30);
            Evidence(mission, "Cmd. Sgt. Price (package105) north of the way in gives it; Field Lt. Perkins at the north west " +
                "fortification takes it. A player on it at the frontline brings the Machina on, six at a time, beside three AFS " +
                "riflemen; fifteen Forean Machina650010 killed, the hills' or the frontline's. Shareable, as the walkthrough has " +
                "the first to speak to Price bring the squad in. Olympia Reflective Armor Vest36093 with Resist Physical[1]100020 " +
                "and Pulsar Motor Assist Armor Legs13744 with Resist EMP[1]100048.");
            mission.Enable(Scene());
        }

        /// <summary>Johnson's: three Living Infestations on each of the three communication dishes.</summary>
        private static void PirateRadioRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, PirateRadio, "Pirate Radio", 51, Johnson, Johnson, 10, Category);
            mission.Reward(8000, 1200, selectableItems: new[] { Item(AccuMaxLeechGun, 1, CritHitBonus), Item(EclipsePulseShotgun, 1, ThreatReduction) });
            mission.Prerequisite(AssembleWithPerkins);

            uint ordinal = 1;
            foreach (var (objective, name, body, counter, scenario, marker, spot) in Dishes)
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Transition(objective);
                for (uint infestation = 1; infestation <= InfestationsPerDish; infestation++)
                    mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, infestation, counter: scenario,
                        target: InfestationsPerDish, trigger: infestation);
                mission.Scenario(scenario, $"The infestations of the {(objective == NorthDish ? "north" : objective == NorthwestDish ? "north west" : "south")} dish",
                    Enumerable.Range(1, (int)InfestationsPerDish).Select(i => (uint)i).ToArray());
                mission.Indicator(objective, marker, spot.X, spot.Y, spot.Z, 15);
            }
            mission.Action(NorthDish, 1, MissionActionKind.GrantReward, reward: 1);

            Evidence(mission, "Information Spec. Johnson (400401, package106) by the main pathway gives and takes it, after Perkins. " +
                "Three Living Infestations6225 (300 hit points) stand about each dish; each destroyed is an event of the dish's " +
                "scenario for everyone on the dish's objective, its counter Infestation Remaining. Destroyed, they stay down in the " +
                "copy. AccuMax Leech Gun42397 with Crit Hit Bonus[1]100056 and Eclipse Pulse Shotgun17704 with Threat Reduction[1]100052.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>Pirate Radio's dishes: objective, its texts, the counter's text, its scenario, its marker, where.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, uint Scenario, uint Marker, Vector3 Spot)[] Dishes =
        {
            (NorthDish, 20000050, 7394, 20000051, NorthDishScenario, NorthDishMarker, NorthDishSpot),
            (NorthwestDish, 20000053, 7395, 20000054, NorthwestDishScenario, WestDishMarker, NorthwestDishSpot),
            (SouthDish, 20000056, 7396, 20000057, SouthDishScenario, SouthDishMarker, SouthDishSpot)
        };

        /// <summary>Standley's, as his other Logos missions are: the facility's three shrines.</summary>
        private static void LogosRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CommunicationControlMachine, "Logos: Communication, Control, Machine", 4934,
                Standley, Standley, 10, Category);
            mission.Reward(12000, 3000);
            uint ordinal = 1;
            foreach (var (objective, name, body, logos) in new (uint, uint, uint, uint)[]
                     {
                         (6, 4938, 7598, CommunicationLogos), (7, 4939, 7599, ControlLogos), (8, 4940, 7600, MachineLogos)
                     })
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.LogosAcquired, logos);
            }
            mission.Action(6, 1, MissionActionKind.GrantReward, reward: 1);
            Evidence(mission, "Receptive Liaison Standley134 (package2049) at Twin Pillars gives and takes it, as the Wilderness' other " +
                "Logos missions. Communication51 behind a tree on the eastern lakeshore of the encampment, Control41 by the Forean " +
                "huts of the cave path west of the northern route, Machine34 on the hill behind the southern AFS bunker. Logos " +
                "already held count. No markers: the text says the place is small enough to scout.");
            var scene = Scene();
            scene.ExistingFactObjectiveIds = new List<uint> { 6, 7, 8 };
            mission.Enable(scene);
        }

        /// <summary>Nylla's armband, from her in the overlook hall to Baruhi at the Memory Tree.</summary>
        private static void HigherCallingRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, HigherCalling, "A Higher Calling", 6063, NyllaControl, Baruhi, 10, Category);
            mission.Reward(6000, 800, fixedItems: new[] { Item(EmpGrenade, 3), Item(StandardMedPack) });
            mission.Prerequisite(MeansOfProduction);
            mission.Objective(1, 6067, 6644, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, BaruhiPackage);
            mission.Action(1, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("baruhi-takes-armband", HigherCalling, "armband", 1, MissionItemScope.AssignmentIssued));
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, MissionIndicator.UnnamedFrom + 1, WildernessDoor.X, WildernessDoor.Y, WildernessDoor.Z);
            Evidence(mission, "Nylla650002 in the overlook hall gives Ranger Nylla's Armband45439 on acceptance; Council Elder Baruhi106 " +
                "(package227) at the Memory Tree takes it. The marker is on Pravus' door out to the Wilderness, as the elder is there.");
            var scene = Scene();
            scene.Items = new List<MissionItemBinding> { Issued("armband", ArmbandTemplate) };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("nylla-armband", HigherCalling, "armband", ArmbandTemplate, 1)
            };
            mission.Enable(scene);
        }

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
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide, "The Pravus Research Facility walkthrough: route, people and order.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "PravusResearchContent", reconstruction);
        }

        #endregion
    }
}
