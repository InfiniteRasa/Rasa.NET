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
    /// The Energy Weapon Center (adv_arieki_torden_mires_energyweaponcenter, 2107; the Mires'
    /// "Casso Weapons Facility"), entered from the Bane headquarters in the Mires, as the Tabula
    /// Rasa wiki's walkthrough tells it: its people, its Bane and its five missions, in the
    /// client's own names and texts.
    ///
    /// Who and what:
    ///  - Engineer Brandenburg outside, below the way in, who gives Incursion Investigation: the
    ///    team he sent in, of whom Sergeant Wolfe is what is left.
    ///  - Sergeant Wolfe inside the way in (package 1455), beside Field Medic Casey and the
    ///    hospital, who takes it and gives It's All In the Timing (radioed in): through the two
    ///    reactor tubes, whose plasma reactors fire down them by turns, to the primary generator
    ///    at their far end. Headquarters, by radio, then gives Tesla Inversion (the datablade in
    ///    the research lab, the Tesla Control Console at its window), Look Before You Sabotage
    ///    (the security monitors, Demolitionist Engelsen's charges, the warehouse's fuel tanks)
    ///    and The Strongest Link (Linker Commander Casso's minions, then Casso), which Wolfe takes.
    ///  - The Bane, levels 28 to 30: Thrax, technicians, grenadiers, a Kael and shield drones
    ///    about the facility, Nitroglazers in the reactor tubes and the Nitroglazer Generator
    ///    Guardian at the generator, Overseer Xirnon in the research lab, Caretaker Sergeant
    ///    Zixno and the storeroom caretakers in the warehouse, Casso and his Linkers and
    ///    caretakers in the command chamber. Outside the lab's window, in the yard nobody can
    ///    reach, the Tesla coil prototypes' test range: Thrax and a Stalker. Health 10000 x
    ///    2^((level - 42) / 8), the curve the world's rows sit on, a minion three quarters of it,
    ///    a lieutenant one and three quarters, Casso three times; armour as the Plateau's and the
    ///    Pools' rows of the kind at that level.
    /// The force fields (and the one shutting the yard), the generator, the reactors' beams, the datablade, the console and its
    /// coils, the monitors, the demolitionist, the fuel tanks and their charges, Casso's link to
    /// his minions and the scenario events the missions wait for are the server's, per copy of the
    /// facility (EnergyWeaponCenter).
    ///
    /// The seed's four level 36 Thrax pools here (the Mires' own row, 531055) are left asleep:
    /// scripted (mode 2), which nothing wakes; Down sets them going again.
    /// </summary>
    public static class EnergyWeaponCenterContent
    {
        public const string Revision = "ewc_1_16_5";
        public const uint Center = 2107, Mires = 1759;

        /// <summary>missioncategorylanguage: "Instance (Energy Weapon Center)".</summary>
        public const uint Category = 3;

        public const uint IncursionInvestigation = 1271, AllInTheTiming = 1253, TeslaInversion = 1260, LookBeforeYouSabotage = 1296,
            TheStrongestLink = 1307;

        public static readonly uint[] Missions = { IncursionInvestigation, AllInTheTiming, TeslaInversion, LookBeforeYouSabotage, TheStrongestLink };

        /// <summary>Wolfe's package: the client's objectiveconversation (1271, 1, 1455).</summary>
        public const uint WolfePackage = 1455;

        public const uint Wolfe = 710001, Brandenburg = 710002;
        public const uint Thrax = 710010, Technician = 710011, Grenadier = 710012, ShieldDrone = 710013, Kael = 710014,
            Nitroglazer = 710015, GeneratorGuardian = 710016, StoreroomCaretaker = 710017, FacilityGuard = 710018,
            Xirnon = 710019, Zixno = 710020, Linker = 710021, Caretaker = 710022, Casso = 710023, Stalker = 710024;

        /// <summary>Every Bane row of ours in the facility.</summary>
        public static readonly uint[] Hostiles =
        {
            Thrax, Technician, Grenadier, ShieldDrone, Kael, Nitroglazer, GeneratorGuardian, StoreroomCaretaker, FacilityGuard,
            Xirnon, Zixno, Linker, Caretaker, Casso, Stalker
        };

        /// <summary>The seed's level 36 Thrax pools in the facility, put to sleep.</summary>
        public static readonly uint[] SeedPools = { 531435, 531436, 531437, 531438 };
        public const uint SeedThrax = 531055;
        public const short Automatic = 0, Scripted = 2;

        // The scenario of each mission whose events the facility records (one each), and its events.
        public const uint Scenario = 1;
        public const uint ReactorsDown = 1;
        public const uint Inverted = 1;
        public const uint NetworkAccessed = 1, ChargesFound = 2;

        /// <summary>Look Before You Sabotage's second: a charge set at fuel tank n is its event n, the counter's own 1..3.</summary>
        public const uint TanksScenario = 2, FirstTank = 1;
        public const uint CassoDefeated = 1;

        // Objectives.
        public const uint DetermineFate = 1;
        public const uint ShutDownReactors = 1;
        public const uint FindDatablade = 1, TriggerInversion = 2;
        public const uint AccessNetwork = 1, RetrieveCharges = 4, PlaceCharges = 5;
        public const uint DefeatCasso = 1;
        public const uint Tanks = 3;

        // Items.
        public const uint DatabladeClass = 25470, DatabladeTemplate = 97472;    // Mis_TordenMiresIns_EWC_Datablade
        public const uint ChargeClass = 25499, ChargeTemplate = 97497;          // Mis_TordenMiresIns_EWC_ExplosiveCharge

        // The objects EnergyWeaponCenter puts down.
        public const uint FieldClass = 20000003;        // UsableForceFieldBaneOutpostWallMinorV01
        public const uint GeneratorClass = 3857;        // UsableInertDestBaneOutpostGenerator
        public const uint TankClass = 12981;            // UsableInertDestBaneFuelDepotTankV01
        public const uint CoilClass = 3899;             // UsableTeslaCoilBaneV01
        public const uint BombClass = 7870;             // UsableBombHumV01: a charge set on a tank
        public const uint DispenserClass = 11081;       // UsableItemDispBaneDataBladeV01: the testing station
        public const uint ConsoleClass = 7888;          // UsableItemAccBaneConsoleV01: the Tesla Control Console
        public const uint CorpseClass = 7654;           // UsableItemDispGuardPromCorpse: Demolitionist Engelsen
        public const uint AlarmLightClass = 9542;       // UsableTwoStateBaneAlarmLightV01: a reactor charging
        public const uint ReactorFireClass = 20604;     // RandomExplosionPravusReactorFlamingChunks: a reactor firing
        public static readonly uint[] MonitorClasses = { 12986, 12987, 12988 };   // UsableTwoStateBaneConsoleMonitorCameraLgV01-03
        public const uint GeneratorHitPoints = 20000, TankHitPoints = 2500;

        /// <summary>
        /// missionobjectiveindicatorlanguage: Plasma Generator, Research Center, Tesla Control
        /// Console, Demolitionist Engelsen, Fuel Tanks, Linker Commander Casso, Security Control Station.
        /// </summary>
        public const uint GeneratorMarker = 164, ResearchCenterMarker = 165, ConsoleMarker = 167, EngelsenMarker = 169,
            TanksMarker = 170, CassoMarker = 172, SecurityMarker = 181;

        // Where things are, on the facility's navmesh (x, height, z).
        public static readonly Vector3 Arrival = new(346.8038f, 97.781f, -0.0524f);

        /// <summary>The Mires' side of the way in: the Bane headquarters' tunnel door, and the open ground below its ramp.</summary>
        public static readonly Vector3 MiresDoor = new(-280.1593f, 221.7631f, 340.4434f);
        public static readonly Vector3 BrandenburgSpot = new(-277f, 220.96f, 311f);

        /// <summary>Inside the way in, across the tunnel from Casey and the hospital: Wolfe.</summary>
        public static readonly Vector3 WolfeSpot = new(330f, 97.71f, -2.2f);

        /// <summary>
        /// The force fields (the Bane wall's minor entrance), each across a way the walkthrough has
        /// taken another: the tunnel west of the cross, to the research lab; the service corridor
        /// from the generator room up to the lab's south gallery; the cross's north branch, to the
        /// command chamber. A field's sheet runs from -10.85 to 2.85 m across its origin, so the
        /// origin stands 4 m off the way's middle, each the way its yaw has the sheet run.
        /// </summary>
        public static readonly Vector3 TunnelField = new(272f, 97.71f, -4f), LabField = new(128f, 105.13f, -52f), CommandField = new(300f, 97.71f, 40f);
        public const float TunnelFieldYaw = 1.5707964f, LabFieldYaw = 1.5707964f, CommandFieldYaw = 0f;

        /// <summary>
        /// The fourth, which never fails: across the corridor out to the facility's north west
        /// door (arch_bane_industrial_chunnel_entrance_v01 at (56, 88)), the way into the yard. The
        /// walkthrough has the yard's Stalker seen through the lab's window and not reached.
        /// </summary>
        public static readonly Vector3 YardField = new(46f, 97.87f, 84f);
        public const float YardFieldYaw = 1.5707964f;

        /// <summary>The reactor tubes (arch_bane_industrial_circuit_*): their axes, and the plasma reactor in each.</summary>
        public const float NorthTubeZ = -24f, SouthTubeZ = -72f, TubeWest = 132f, TubeEast = 258f;
        public static readonly Vector3 NorthReactor = new(186f, 100.5f, -24f), SouthReactor = new(217f, 100.5f, -72f);

        /// <summary>The octagonal service room at the tubes' west end, where the corridor climbs to the lab: the primary generator.</summary>
        public static readonly Vector3 Generator = new(152f, 97.11f, -48f);

        /// <summary>The research lab: its south gallery's testing station, the console at its window, the north gallery's monitors.</summary>
        public static readonly Vector3 LabCentre = new(88f, 95.79f, 0f);
        public static readonly Vector3 Datablade = new(90f, 105.71f, -52f);
        public static readonly Vector3 Console = new(42f, 105.71f, 0f);
        public static readonly Vector3[] Monitors = { new(84f, 105.71f, 72f), new(88f, 105.71f, 72f), new(92f, 105.71f, 72f) };

        /// <summary>The yard outside the window, which nobody gets into (YardField): the coil prototypes' test range.</summary>
        public static readonly Vector3[] Coils = { new(14f, 97.3f, -14f), new(14f, 96.78f, 14f), new(2f, 97.02f, -12f), new(2f, 96.88f, 12f) };
        public static readonly Vector3 Yard = new(8f, 97.04f, 0f);

        /// <summary>The warehouse: Engelsen on the crate stacks' top, the three fuel tanks and the charges set at them.</summary>
        public static readonly Vector3 Engelsen = new(215f, 105.31f, 46f);
        public static readonly Vector3[] TankSpots = { new(148f, 96.17f, 100f), new(238f, 96.18f, 100f), new(192f, 96.11f, 127f) };
        public static readonly Vector3[] ChargeSpots = { new(151f, 96.16f, 100f), new(235f, 96.18f, 100f), new(192f, 96.11f, 124f) };
        public static readonly Vector3 WarehouseCentre = new(192f, 96.91f, 100f);

        /// <summary>The command chamber (the north control room): Casso.</summary>
        public static readonly Vector3 CommandChamber = new(296f, 95.71f, 88f);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1 }

        /// <summary>A creature row, its stats and its actions (as its creature_action ids), and whose look it wears (0 none).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, uint LookFrom, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A spawn pool: its creatures (row, least, most), where and which way, and its respawn time in tenths of a second.</summary>
        public sealed record Spot(uint Id, uint Map, double X, double Y, double Z, double Rotation, uint RespawnTenths, double Radius,
            params (uint Creature, uint Min, uint Max)[] Slots);

        // Existing actions the rows keep: the Plateau's and the Pools' rows of the kind at these levels.
        public const uint ThraxKick = 53137, ThraxRifle = 53138;
        public const uint TechnicianWeapon = 70028, TechnicianHeal = 62047, TechnicianTurret = 65016;
        public const uint GrenadierGrenade = 53139, GrenadierRifle = 53140;
        public const uint DroneAttack = 53134, DroneHealAura = 45;
        public const uint KaelPound = 53135, KaelSmash = 53136, KaelStrike = 54011;
        public const uint CaretakerAttack = 53131, CaretakerHeal = 62021, CaretakerRevive = 62022;
        public const uint OverseerKick = 53132, OverseerRifle = 53133;
        public const uint LinkerChannel = 66001;
        public const uint StalkerWeapon = 750019, StalkerOvulate = 750020, StalkerEggDrop = 750021;

        /// <summary>
        /// The Nitroglazers' and Linkers', which have no rows at these levels: the Devil's Den's
        /// Nitroglazer (20) and the Pools' Linker (35) scaled along the same curve, 2^(levels / 8),
        /// the bosses' by a quarter more.
        /// </summary>
        public static readonly Act[] Actions =
        {
            new(710001, "Nitroglazer - EWC weapon 1/232", 1, 232, 1, 30, 1500, 0, 96, 144, 1),
            new(710002, "Nitroglazer - EWC mortar 289/1", 289, 1, 5, 40, 12000, 0, 138, 216, 1),
            new(710003, "Nitroglazer Generator Guardian weapon 1/232", 1, 232, 1, 30, 1500, 0, 143, 214, 1),
            new(710004, "Nitroglazer Generator Guardian mortar 289/1", 289, 1, 5, 40, 12000, 0, 205, 321, 1),
            new(710005, "Linker - EWC linker_chestblast", 263, 1, 1, 40, 20000, 7333, 306, 458, 1),
            new(710006, "Linker - EWC linker_hand_blast", 412, 1, 1, 40, 5000, 566, 115, 152, 1),
            new(710007, "Linker - EWC linker_groundblast", 264, 1, 1, 40, 5000, 2633, 152, 229, 1),
            new(710008, "Linker Commander Casso linker_chestblast", 263, 1, 1, 40, 20000, 7333, 416, 624, 1),
            new(710009, "Linker Commander Casso linker_hand_blast", 412, 1, 1, 40, 5000, 566, 156, 207, 1),
            new(710010, "Linker Commander Casso linker_groundblast", 264, 1, 1, 40, 5000, 2633, 207, 312, 1)
        };

        // Level 28, 29 and 30: 2973, 3242 and 3536 by the curve.
        public static readonly Beast[] Creatures =
        {
            new(Wolfe, "Sergeant Wolfe - Energy Weapon Center", SwapsetMale, TargetCategoryRow.Friendly, 30, 5000, 0, 9341, 0, 0, FieldSergeantLook),
            new(Brandenburg, "Engineer Brandenburg - Mires, by the EWC", SwapsetMale, TargetCategoryRow.Friendly, 30, 5000, 0, 8680, 0, 0, EngineerLook),
            new(Thrax, "Thrax Soldier - EWC", 20757, TargetCategoryRow.Hostile, 28, 2230, 140, 9224, 9, 5, 3, ThraxKick, ThraxRifle),
            new(Technician, "Thrax Technician - EWC", 7043, TargetCategoryRow.Hostile, 28, 2973, 195, 9106, 9, 5, 0,
                TechnicianWeapon, TechnicianHeal, TechnicianTurret),
            new(Grenadier, "Thrax Grenadier - EWC", 9354, TargetCategoryRow.Hostile, 29, 2432, 140, 7074, 9, 5, 0, GrenadierGrenade, GrenadierRifle),
            new(ShieldDrone, "Shield Drone - EWC", 7233, TargetCategoryRow.Hostile, 28, 2973, 195, 438, 9, 5, 0, DroneAttack, DroneHealAura),
            new(Kael, "Kael Master Sergeant - EWC", 4046, TargetCategoryRow.Hostile, 29, 3242, 205, 7728, 9, 5, 0, KaelPound, KaelSmash, KaelStrike),
            new(Nitroglazer, "Nitroglazer - EWC reactor tubes", 7234, TargetCategoryRow.Hostile, 28, 2973, 195, 434, 9, 5, 0, 710001, 710002),
            new(GeneratorGuardian, "Nitroglazer Generator Guardian - EWC", 10337, TargetCategoryRow.Hostile, 30, 6188, 280, 9376, 9, 5, 0, 710003, 710004),
            new(StoreroomCaretaker, "Storeroom Caretaker - EWC warehouse", 9244, TargetCategoryRow.Hostile, 29, 3242, 205, 9553, 9, 5, 0,
                CaretakerAttack, CaretakerHeal, CaretakerRevive),
            new(FacilityGuard, "Casso Facility Guard - EWC", 20757, TargetCategoryRow.Hostile, 29, 2432, 150, 8736, 9, 5, 3, ThraxKick, ThraxRifle),
            new(Xirnon, "Overseer Xirnon - EWC research lab", 10504, TargetCategoryRow.Hostile, 30, 6188, 280, 9516, 9, 5, 0, OverseerKick, OverseerRifle),
            new(Zixno, "Caretaker Sergeant Zixno - EWC warehouse", 10321, TargetCategoryRow.Hostile, 30, 6188, 280, 9515, 9, 5, 0,
                CaretakerAttack, CaretakerHeal, CaretakerRevive),
            new(Linker, "Linker - EWC command chamber", 9751, TargetCategoryRow.Hostile, 29, 3242, 205, 412, 9, 5, 0, 710005, 710006, 710007, LinkerChannel),
            new(Caretaker, "Caretaker - EWC command chamber", 9244, TargetCategoryRow.Hostile, 29, 3242, 205, 366, 9, 5, 0,
                CaretakerAttack, CaretakerHeal, CaretakerRevive),
            new(Casso, "Linker Commander Casso - EWC command chamber", 10335, TargetCategoryRow.Hostile, 30, 10608, 280, 9380, 9, 5, 0,
                710008, 710009, 710010),
            new(Stalker, "Stalker - EWC yard, the coils' test range", 3781, TargetCategoryRow.Hostile, 30, 9000, 220, 0, 9, 5, 0,
                StalkerWeapon, StalkerOvulate, StalkerEggDrop)
        };

        /// <summary>Wolfe dresses as Field Sgt. Witherspoon does, Brandenburg as AFS Engineer Salter.</summary>
        public const uint FieldSergeantLook = 101, EngineerLook = 115;

        public const uint WolfePool = 710001, BrandenburgPool = 710002;
        public const uint XirnonPool = 710303, ZixnoPool = 710402, CassoPool = 710501, YardStalkerPool = 710601;

        /// <summary>The Casso Facility Guards the alarm brings to the lab, and the fuel tanks' blast to the warehouse: scripted, woken once.</summary>
        public const uint LabAlarmPool = 710309, WarehouseAlarmPool = 710409;

        /// <summary>Casso's minions: the command chamber's Linkers and caretakers. He is not to be hurt while one stands.</summary>
        public static readonly uint[] MinionPools = { 710502, 710503, 710504, 710505 };

        public const uint NpcRespawn = 200, HostileRespawn = 3000;

        public static readonly Spot[] Pools =
        {
            // The people.
            new(WolfePool, Center, WolfeSpot.X, WolfeSpot.Y, WolfeSpot.Z, 1.5708, NpcRespawn, 0, (Wolfe, 1, 1)),
            new(BrandenburgPool, Mires, BrandenburgSpot.X, BrandenburgSpot.Y, BrandenburgSpot.Z, 0, NpcRespawn, 0, (Brandenburg, 1, 1)),

            // The Tesla Control Room, south of the cross: the first room the walkthrough clears.
            new(710101, Center, 300, 97.31, -40, 3.1, HostileRespawn, 3, (Thrax, 2, 2), (Technician, 1, 1)),
            new(710102, Center, 312, 96.11, -50, -1.6, HostileRespawn, 3, (Thrax, 1, 1), (Kael, 1, 1)),
            new(710103, Center, 288, 96.18, -64, 1.6, HostileRespawn, 2, (Technician, 1, 1), (Grenadier, 1, 1)),

            // The reactor tubes, the Nitroglazers keeping the reactors cool, and the generator room.
            new(710201, Center, 244, 99.0, -24, -1.6, HostileRespawn, 1, (Technician, 1, 1), (Nitroglazer, 1, 1)),
            new(710202, Center, 244, 99.0, -72, -1.6, HostileRespawn, 1, (Nitroglazer, 2, 2)),
            new(710203, Center, 200, 97.53, -48, 0, HostileRespawn, 1, (Nitroglazer, 2, 2)),
            new(710204, Center, 168, 96.83, -58, 0, HostileRespawn, 1, (Technician, 1, 1), (Nitroglazer, 1, 1)),
            new(710205, Center, 232, 97.33, -58, 0, HostileRespawn, 1, (Technician, 1, 1)),
            new(710206, Center, 152, 97.11, -44.5, 3.1, HostileRespawn, 0, (GeneratorGuardian, 1, 1)),
            new(710207, Center, 152, 97.11, -51.5, 0, HostileRespawn, 1.5, (Nitroglazer, 2, 2)),

            // The research lab: its floor, the south gallery with Xirnon, the overlook at the window, the north gallery.
            new(710301, Center, LabCentre.X, LabCentre.Y, LabCentre.Z, 1.6, HostileRespawn, 5, (Thrax, 2, 2), (Technician, 1, 1)),
            new(710302, Center, 96, 95.8, -6, 1.6, HostileRespawn, 0, (ShieldDrone, 1, 1)),
            new(XirnonPool, Center, 84, 105.71, -44, 0, HostileRespawn, 0, (Xirnon, 1, 1)),
            new(710304, Center, 88, 105.71, -48, 0, HostileRespawn, 3, (Thrax, 2, 2)),
            new(710305, Center, 50, 105.81, 0, -1.6, HostileRespawn, 3, (Thrax, 1, 1), (Technician, 1, 1)),
            new(710306, Center, 88, 105.71, 48, 3.1, HostileRespawn, 4, (Thrax, 2, 2), (ShieldDrone, 1, 1)),
            new(710307, Center, 88, 105.71, 66, 3.1, HostileRespawn, 1, (Technician, 1, 1)),
            new(710308, Center, 110, 97.71, 88, -1.6, HostileRespawn, 2, (Grenadier, 1, 1), (Thrax, 1, 1)),

            // The warehouse: Zixno and the storeroom's caretakers, the patrols the depot's text warns of.
            new(710401, Center, 160, 96.11, 70, 0.8, HostileRespawn, 4, (StoreroomCaretaker, 2, 2)),
            new(ZixnoPool, Center, WarehouseCentre.X, WarehouseCentre.Y, WarehouseCentre.Z, 3.1, HostileRespawn, 0, (Zixno, 1, 1)),
            new(710403, Center, 200, 96.28, 70, 3.1, HostileRespawn, 5, (Grenadier, 2, 2), (Thrax, 1, 1)),
            new(710404, Center, 226, 96.18, 108, -2.4, HostileRespawn, 2, (Kael, 1, 1), (Thrax, 1, 1)),
            new(710405, Center, 158, 96.16, 108, 2.4, HostileRespawn, 2, (StoreroomCaretaker, 1, 1), (Thrax, 2, 2)),
            new(710406, Center, 205, 96.91, 85, 3.1, HostileRespawn, 3, (Thrax, 2, 2)),

            // The command chamber: Casso, his minions, and the guards at its door from the warehouse.
            new(CassoPool, Center, 300, 95.71, 92, -2.4, HostileRespawn, 0, (Casso, 1, 1)),
            new(710502, Center, 288, 95.71, 80, 0.8, HostileRespawn, 0, (Linker, 1, 1)),
            new(710503, Center, 305, 95.89, 78, -0.8, HostileRespawn, 0, (Caretaker, 1, 1)),
            new(710504, Center, 310, 96.15, 88, -1.6, HostileRespawn, 0, (Linker, 1, 1)),
            new(710505, Center, 300, 95.71, 100, 3.1, HostileRespawn, 0, (Caretaker, 1, 1)),
            new(710506, Center, 276, 96.23, 88, -1.6, HostileRespawn, 2, (FacilityGuard, 2, 2)),

            // The yard outside the lab's window, the coils' test range: out of everyone's reach.
            new(YardStalkerPool, Center, Yard.X, Yard.Y, Yard.Z, 1.6, HostileRespawn, 0, (Stalker, 1, 1)),
            new(710602, Center, 12, 96.95, 8, 1.6, HostileRespawn, 2, (Thrax, 2, 2)),
            new(710603, Center, 12, 97.1, -8, 1.6, HostileRespawn, 2, (Technician, 1, 1), (Thrax, 1, 1))
        };

        /// <summary>The scripted pools EnergyWeaponCenter wakes: scripted (mode 2), so nothing else does.</summary>
        public static readonly Spot[] AlarmPools =
        {
            new(LabAlarmPool, Center, LabCentre.X, LabCentre.Y, LabCentre.Z, 3.1, 10, 4, (FacilityGuard, 3, 3)),
            new(WarehouseAlarmPool, Center, WarehouseCentre.X, WarehouseCentre.Y, WarehouseCentre.Z, 3.1, 10, 6, (FacilityGuard, 4, 4))
        };

        /// <summary>Who speaks which package: Wolfe.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Wolfe, WolfePackage, "Sergeant Wolfe - Energy Weapon Center")
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

                if (c.LookFrom != 0)
                    yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {c.Id}, a.slot_id, a.Class_id, a.color " +
                        $"FROM creature_appearance a WHERE a.id = {c.LookFrom} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {c.Id}) " +
                        $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {c.Id});";
            }

            foreach (var (creature, package, comment) in Packages)
                yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {creature}, {package}, {S(comment)} " +
                    $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {creature});";

            foreach (var p in Pools)
                yield return Pool(p, Automatic);

            foreach (var p in AlarmPools)
                yield return Pool(p, Scripted);

            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", SeedPools)}) AND map_context_id = {Center} " +
                $"AND creature_1_Id = {SeedThrax} AND mode = {Automatic};";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", SeedPools)}) AND map_context_id = {Center} " +
                $"AND creature_1_Id = {SeedThrax} AND mode = {Scripted};";

            foreach (var p in Pools.Concat(AlarmPools))
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND map_context_id = {p.Map} AND creature_1_Id = {p.Slots[0].Creature};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            foreach (var c in Creatures)
            {
                if (c.LookFrom != 0)
                    yield return $"DELETE FROM creature_appearance WHERE id = {c.Id};";
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id} " +
                    "AND NOT EXISTS (SELECT 1 FROM creature c WHERE " +
                    $"{a.Id} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        private static string Pool(Spot p, short mode)
        {
            var slots = p.Slots.Concat(Enumerable.Repeat((Creature: 0U, Min: 0U, Max: 0U), 6)).Take(6).ToArray();
            var there = string.Concat(p.Slots.Skip(1).Select(s => $" AND EXISTS (SELECT 1 FROM creature x WHERE x.id = {s.Creature})"));

            return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"SELECT {p.Id}, {mode}, 0, {p.RespawnTenths}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.Map}, " +
                string.Join(", ", slots.Select(s => $"{s.Creature}, {s.Min}, {s.Max}")) + $", {N(p.Radius)} " +
                $"FROM creature c WHERE c.id = {p.Slots[0].Creature}{there} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";
        }

        #region Missions

        private const string Guide = "https://tabularasa.fandom.com/wiki/Energy_Weapon_Center";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            IncursionInvestigationRows(sql);
            AllInTheTimingRows(sql);
            TeslaInversionRows(sql);
            LookBeforeYouSabotageRows(sql);
            TheStrongestLinkRows(sql);
            return sql;
        }

        // Rewards: the experience and credits of the Plateau's level 28 to 30 missions, and each
        // choice of gear one of theirs at the level: Canyon Sweep's (1045), Blueprint For
        // Victory's (1046), Toxic Detox's (1053) and Incommunicado's (1040).
        public static readonly uint[] TimingChoices = { 15582, 15105, 49168, 14933 };
        public static readonly uint[] InversionChoices = { 46028, 25229, 34225, 40353 };
        public static readonly uint[] SabotageChoices = { 120434, 48788, 14610, 49652 };
        public static readonly uint[] StrongestLinkChoices = { 48256, 15507, 120384, 48708 };

        private static InstanceMissionSql.RewardItem[] Choice(uint[] templates) =>
            templates.Select(template => new InstanceMissionSql.RewardItem(template, 1)).ToArray();

        /// <summary>Brandenburg's, outside: what became of his team; Wolfe inside the way in tells it.</summary>
        private static void IncursionInvestigationRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, IncursionInvestigation, "Incursion Investigation", 10202, Brandenburg, Wolfe, 28, Category);
            mission.Reward(25000, 3100);
            mission.Objective(DetermineFate, 10211, 10212, 1, MissionObjectiveState.Incomplete);
            mission.Transition(DetermineFate);
            mission.Conversation(DetermineFate, WolfePackage);
            mission.Action(DetermineFate, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(DetermineFate, MissionIndicator.UnnamedFrom + DetermineFate, MiresDoor.X, MiresDoor.Y, MiresDoor.Z);
            Evidence(mission, "Engineer Brandenburg8680 gives it (his opening: 'I lost contact with the team I sent to the Energy Weapon " +
                "Center'); he has no place in the client's data, and stands outside, below the ramp from the Bane headquarters' door " +
                "into the facility. Sergeant Wolfe9341, inside the way in (package1455, the objective's conversation and its " +
                "finishing text: 'We got slaughtered!'), takes it. The marker is on the Mires' door into the facility, as the " +
                "giver's map is the Mires.");
            var scene = Scene();
            scene.IndicatorMaps = new Dictionary<uint, Dictionary<uint, uint>>
            {
                [DetermineFate] = new() { [MissionIndicator.UnnamedFrom + DetermineFate] = Mires }
            };
            mission.Enable(scene);
        }

        /// <summary>Wolfe's: through the reactor tubes to the primary generator; radioed in.</summary>
        private static void AllInTheTimingRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, AllInTheTiming, "It's All In the Timing", 10041, Wolfe, null, 28, Category);
            mission.Reward(55000, 5100, selectableItems: Choice(TimingChoices));
            mission.Objective(ShutDownReactors, 10047, 10048, 1, MissionObjectiveState.Incomplete);
            mission.Transition(ShutDownReactors);
            mission.Progress(ShutDownReactors, MissionProgressEventKind.ScenarioEvent, ReactorsDown, counter: Scenario);
            mission.Action(ShutDownReactors, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "It's All In the Timing", ReactorsDown);
            mission.Indicator(ShutDownReactors, GeneratorMarker, Generator.X, Generator.Y, Generator.Z, 10);
            mission.RadioTurnIn();
            Evidence(mission, "Wolfe710001 gives it (his opening). Left at the cross is the Tesla Control Room, and west of it the two " +
                "reactor tubes, whose plasma reactors fire down them by turns ('wait for an opening before darting through the " +
                "chamber'); the generator (UsableInertDestBaneOutpostGenerator, 20000 hit points, Lightning three times as hard) " +
                "in the service room at their far end, the Nitroglazer Generator Guardian9376 by it. Down, it is event1 for " +
                "everyone in the copy on the objective, the reactors are off and the fields in the tunnel and the corridor up to " +
                "the lab fail ('That should open a few doors'). The walkthrough has it rewarded by radio. Not after Incursion " +
                "Investigation: Wolfe is by the way in for whoever comes. Canyon Sweep's level 30 choice.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>HQ's, by radio after It's All In the Timing: the datablade, and the Tesla Control Console.</summary>
        private static void TeslaInversionRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TeslaInversion, "Tesla Inversion", 10105, null, null, 29, Category, radioCompleteable: true);
            mission.Reward(55000, 5100, selectableItems: Choice(InversionChoices));
            mission.Prerequisite(AllInTheTiming);
            mission.Objective(FindDatablade, 10129, 10130, 1, MissionObjectiveState.Incomplete);
            mission.Objective(TriggerInversion, 10134, 10135, 2);
            mission.Transition(FindDatablade);
            mission.Progress(FindDatablade, MissionProgressEventKind.ItemAcquired, DatabladeClass, target: 1);
            mission.RevealAndActivate(FindDatablade, TriggerInversion);
            mission.Transition(TriggerInversion);
            mission.Progress(TriggerInversion, MissionProgressEventKind.ScenarioEvent, Inverted, counter: Scenario);
            mission.Action(TriggerInversion, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Tesla Inversion", Inverted);
            mission.Indicator(FindDatablade, ResearchCenterMarker, Datablade.X, Datablade.Y, Datablade.Z, 6);
            mission.Indicator(TriggerInversion, ConsoleMarker, Console.X, Console.Y, Console.Z, 4);
            mission.Channels(MissionChannel.Radio, MissionChannel.Radio, Offers(AllInTheTiming));
            Evidence(mission, "HQ's ('I can reprogram it remotely'): offered by radio at the turn-in of It's All In the Timing1253, and " +
                "again on coming into the facility. The datablade (Mis_TordenMiresIns_EWC_Datablade, template97472) from the " +
                "testing station in the research lab's south gallery, Overseer Xirnon9516 by it; held, it is objective1. The " +
                "Tesla Control Console at the lab's window takes it: event2's alarm, and the coil prototypes on the test range " +
                "outside turn on the Thrax and the Stalker there until they burn out ('That must be quite a show!'). Radioed in. " +
                "Blueprint For Victory's level 30 choice.");
            var scene = Scene(events: true);
            scene.MapContextId = Center;
            scene.ExistingFactObjectiveIds = new List<uint> { FindDatablade };
            mission.Enable(scene);
        }

        /// <summary>HQ's, by radio after Tesla Inversion: the monitors, Engelsen's charges, the fuel tanks.</summary>
        private static void LookBeforeYouSabotageRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LookBeforeYouSabotage, "Look Before You Sabotage", 10454, null, null, 29, Category,
                radioCompleteable: true);
            mission.Reward(55000, 5100, selectableItems: Choice(SabotageChoices));
            mission.Prerequisite(TeslaInversion);
            mission.Objective(AccessNetwork, 10467, 10468, 1, MissionObjectiveState.Incomplete);
            mission.Objective(RetrieveCharges, 10561, 10562, 2);
            mission.Objective(PlaceCharges, 10563, 10564, 3, counterTextId: 10565);
            mission.Transition(AccessNetwork);
            mission.Progress(AccessNetwork, MissionProgressEventKind.ScenarioEvent, NetworkAccessed, counter: Scenario);
            mission.RevealAndActivate(AccessNetwork, RetrieveCharges);
            mission.Transition(RetrieveCharges);
            mission.Progress(RetrieveCharges, MissionProgressEventKind.ScenarioEvent, ChargesFound, counter: Scenario);
            mission.RevealAndActivate(RetrieveCharges, PlaceCharges);
            mission.Transition(PlaceCharges);
            for (uint tank = 0; tank < Tanks; tank++)
                mission.Progress(PlaceCharges, MissionProgressEventKind.ScenarioEvent, FirstTank + tank, counter: TanksScenario, target: Tanks,
                    trigger: tank + 1);
            mission.Action(PlaceCharges, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Look Before You Sabotage", NetworkAccessed, ChargesFound);
            mission.Scenario(TanksScenario, "Look Before You Sabotage: the fuel tanks", FirstTank, FirstTank + 1, FirstTank + 2);
            mission.Indicator(AccessNetwork, SecurityMarker, Monitors[1].X, Monitors[1].Y, Monitors[1].Z, 6);
            mission.Indicator(RetrieveCharges, EngelsenMarker, Engelsen.X, Engelsen.Y, Engelsen.Z, 4);
            mission.Indicator(PlaceCharges, TanksMarker, WarehouseCentre.X, WarehouseCentre.Y, WarehouseCentre.Z, 40);
            mission.Channels(MissionChannel.Radio, MissionChannel.Radio, Offers(TeslaInversion));
            Evidence(mission, "HQ's ('head north from their primary research hub'): offered by radio at the turn-in of Tesla " +
                "Inversion1260, and again on coming into the facility. A security monitor of the bank in the research lab's north " +
                "gallery used is event1, for everyone in the copy on it; its feed shows Demolitionist Engelsen on the warehouse's " +
                "crate stacks, whose body gives three Explosive Charges (Mis_TordenMiresIns_EWC_ExplosiveCharge, template97497): " +
                "event2. A charge set on each of the three fuel tanks is event1 to 3 of scenario2 (counter10565 Charges Placed); each goes off 15 s " +
                "later and takes its tank with it, and the three blown bring the facility guards. The hidden objective6 'Obtain 3 " +
                "explosive charges' is not made: objective4 is it. Radioed in ('I heard that from out here!'). Toxic Detox's level 30 choice.");
            var scene = Scene(events: true);
            scene.MapContextId = Center;
            mission.Enable(scene);
        }

        /// <summary>HQ's, by radio after Look Before You Sabotage: Casso's minions, then Casso; Wolfe takes it.</summary>
        private static void TheStrongestLinkRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TheStrongestLink, "The Strongest Link", 10603, null, Wolfe, 30, Category);
            mission.Reward(100000, 6600, selectableItems: Choice(StrongestLinkChoices));
            mission.Prerequisite(LookBeforeYouSabotage);
            mission.Objective(DefeatCasso, 10608, 10609, 1, MissionObjectiveState.Incomplete);
            mission.Transition(DefeatCasso);
            mission.Progress(DefeatCasso, MissionProgressEventKind.ScenarioEvent, CassoDefeated, counter: Scenario);
            mission.Action(DefeatCasso, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "The Strongest Link", CassoDefeated);
            mission.Indicator(DefeatCasso, CassoMarker, CommandChamber.X, CommandChamber.Y, CommandChamber.Z, 20);
            mission.Channels(MissionChannel.Radio, MissionChannel.Npc, Offers(LookBeforeYouSabotage));
            Evidence(mission, "HQ's ('I just intercepted a Bane transmission'): offered by radio at the turn-in of Look Before You " +
                "Sabotage1296, and again on coming into the facility. Linker Commander Casso9380 in the command chamber (the north " +
                "control room, west of the warehouse) is tied into the base's network: nothing hurts him while one of his Linkers " +
                "and caretakers stands. Dead, he is event1 for everyone in the copy on it, and the field in the cross's north " +
                "branch fails, the way back to Sergeant Wolfe, who takes it (his finishing text). Incommunicado's level 30 choice.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>A radio mission's offers: at the turn-in of the one before, and on coming into the facility.</summary>
        private static MissionOfferSourceDefinition[] Offers(uint after) => new[]
        {
            MissionOfferSourceDefinition.MissionRewarded(after),
            new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Center)
        };

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

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide, "The Energy Weapon Center walkthrough: route, people and order.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "EnergyWeaponCenterContent", reconstruction);
        }

        #endregion
    }
}
