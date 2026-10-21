using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Torcastra Prison (adv_foreas_concordia_divide_torcastraprison, map 1349), peopled and fitted
    /// out as the wiki's walkthrough tells it (Torcastra_prison). The missions it is the
    /// stage of are TorcastraMissionsV1's; what the place does - the trip wire's alarm and its
    /// air strike, the bunkers, the gate, the cell consoles - is the Game's TorcastraPrison.
    ///
    /// From the client: the map (the trip wire's emitters across the bridge, four pillboxes each
    /// with a mortar base on its roof, the prison wall's gate, the prison: cell corridors, an
    /// information extraction room, two control rooms, a research ward with an extraction chair
    /// and a data room); the names (Lt. Cisco, Ranger Ferme, Airman Hamilton, Gatekeeper Xsic,
    /// the Cell Block Warden, Overseer Torqua, Forean prisoners); the gate key, access keys and
    /// datapad, and the creature, weapon and usable classes. From the wiki: mob levels 13-16; a
    /// security beacon past the trip wire whose alarm calls a Stalker unless it is shot first;
    /// Thrax, Hominis Machina and sludges at each bunker and a turret on its roof; the Gatekeeper
    /// on the hill opposite the gate; Cisco moving up to the gate with a hospital beside him once
    /// it is open, and a Juggernaut at the prison door; the Cell Block Warden's pass key (which
    /// the Game gives); five cell blocks, each opened by a sequence of seven panels that blink
    /// it, two of which call Bane security; Ranger Ferme being interrogated; Hamilton in the
    /// research ward.
    ///
    /// Ours, where neither says: where everything stands (on the navmesh, measured against the
    /// map's own pieces), the creatures' numbers, the classes standing in for retail objects whose
    /// placements were lost (a Bane security machine for the beacon, lab machines and console
    /// brains for the data storage, datablade consoles for the terminals and the key slots, Bane
    /// console switches for the cell panels), and Field Sgt. O'Toole at Foreas Base and Base
    /// Cmdr. Matlin at Cumbria Research, who had no rows.
    ///
    /// The Divide seed's 21 placeholder Thrax pools here are left asleep rather than deleted: the
    /// 20 of them that were automatic are set to scripted (mode 2), which nothing wakes; the
    /// courtyard's medic and vendor wait for the gate (mode 2, woken by the Game when it opens);
    /// Hamilton moves from the vendor's spot to the research ward and takes his own package
    /// (1526, not the terminal's 1527). The bunkers' mortars are the four BaneMortars put on
    /// the pillboxes' pads, the Divide's (level 18), and are left as they are.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, and a
    /// row changed only from what it was; Down takes out and puts back only as Up left them.
    /// </summary>
    public static class TorcastraPrisonContent
    {
        public const uint Map = 1349;
        public const uint Divide = 1148;
        public const uint Palisades = 1244;

        // ---- Creatures --------------------------------------------------------------------

        public const uint CiscoAtEntrance = 660001, CiscoAtGate = 660002, Ferme = 660003, OToole = 660005, Matlin = 660006;
        public const uint Hamilton = 510120;

        public const uint ThraxSoldier = 660010, ThraxGrenadier = 660011, HominisMachina = 660012, Amoeboid = 660013,
            Hunter = 660014, Stalker = 660016, Juggernaut = 660017, GatekeeperXsic = 660018,
            CellBlockWarden = 660019, OverseerTorqua = 660020, Interrogator = 660021, ForeanPrisoner = 660022,
            ForeanPrisonerLeader = 660023;

        public const uint CiscoPackage = 1156, FermePackage = 193, HamiltonPackage = 1526, TerminalPackage = 1527, MatlinPackage = 1214;

        /// <summary>Lt. Cisco, Ranger Ferme and the rest; Hamilton is the existing 510120.</summary>
        public sealed record Npc(uint Id, string Comment, uint ClassId, uint Level, uint NameId, uint LookFrom, uint? Package);

        public static readonly Npc[] Npcs =
        {
            new(CiscoAtEntrance, "Lt. Cisco - Torcastra Prison entrance", 3846, 16, 5272, 400403, CiscoPackage),
            new(CiscoAtGate, "Lt. Cisco - Torcastra Prison gate", 3846, 16, 5272, 400403, CiscoPackage),
            new(Ferme, "Ranger Ferme - Torcastra Prison", 3848, 16, 3034, 400402, FermePackage),
            new(OToole, "Field Sgt. O'Toole - Foreas Base", 3846, 16, 129, 510100, null),
            new(Matlin, "Base Cmdr. Matlin - Cumbria Research Facility", 3848, 20, 9029, 510069, MatlinPackage),
        };

        public sealed record Attack(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax, uint Cooldown, uint MinDamage, uint MaxDamage, uint DamageType);

        public static readonly Attack[] Attacks =
        {
            new(660001, "Thrax Soldier - Torcastra pistol 1/1", 1, 1, 1, 20, 800, 12, 18, 6),
            new(660002, "Hominis Machina - Torcastra weapon 1/97", 1, 97, 1, 20, 800, 14, 22, 1),
            new(660003, "Stalker - Torcastra 1/78", 1, 78, 1, 60, 1500, 55, 85, 6),
            new(660004, "Juggernaut - Torcastra 1/180", 1, 180, 1, 40, 1500, 60, 95, 1),
            new(660005, "Cell Block Warden pistol 1/1", 1, 1, 1, 20, 800, 18, 28, 6),
        };

        /// <summary>A Bane creature (faction 0) or a prisoner (1): its class, numbers, attacks, and whose look it wears.</summary>
        public sealed record Combatant(uint Id, string Comment, uint ClassId, uint Faction, uint Level, uint MaxHp, uint NameId,
            uint Run, uint Walk, uint Action1, uint Action2, uint LookFrom, uint Action3 = 0);

        public static readonly Combatant[] Combatants =
        {
            new(ThraxSoldier, "Thrax Soldier - Torcastra", 20757, 0, 14, 650, 236, 9, 5, 660001, 0, 640111),
            new(ThraxGrenadier, "Thrax Grenadier - Torcastra", 9354, 0, 15, 723, 0, 9, 5, 19, 20, 0),
            new(HominisMachina, "Hominis Machina - Torcastra", 3868, 0, 14, 700, 0, 9, 5, 660002, 62059, 8),
            new(Amoeboid, "Sludge - Torcastra", 6032, 0, 13, 600, 0, 9, 5, 12, 76016, 0, Action3: 76017),
            new(Hunter, "Hunter - Torcastra", 10166, 0, 15, 723, 388, 9, 5, 9, 10, 530007),
            new(Stalker, "Stalker - Torcastra alarm", 3781, 0, 16, 3000, 0, 9, 5, 660003, 0, 0),
            new(Juggernaut, "Juggernaut - Torcastra prison door", 7722, 0, 16, 4500, 0, 9, 5, 660004, 0, 0),
            new(GatekeeperXsic, "Gatekeeper Xsic - Torcastra", 10504, 0, 16, 3000, 8628, 9, 5, 41, 76033, 0),
            new(CellBlockWarden, "Cell Block Warden - Torcastra", 10503, 0, 15, 2200, 9409, 9, 5, 660005, 0, 0),
            new(OverseerTorqua, "Overseer Torqua - Torcastra", 10504, 0, 16, 3200, 10332, 9, 5, 34, 76033, 0),
            new(Interrogator, "Thrax Interrogator - Torcastra", 11320, 0, 14, 650, 8058, 9, 5, 70016, 70017, 0),
            new(ForeanPrisoner, "Forean Prisoner - Torcastra", 7034, 1, 10, 800, 5289, 9, 5, 0, 0, 0),
            new(ForeanPrisonerLeader, "Forean Prisoner Leader - Torcastra", 7035, 1, 11, 900, 5311, 9, 5, 0, 0, 0),
        };

        // ---- Spawn pools ----------------------------------------------------------------------

        public const short Automatic = 0, Scripted = 2;

        public sealed record Pool(uint Id, uint Creature, short Mode, uint Respawn, double X, double Y, double Z, double Rotation,
            uint Count, double Radius, uint MapContextId, string Comment);

        // Ids: 6600xx people, 6601xx bunkers, 6602xx the plateau, 6603xx the prison.
        public const uint CiscoEntrancePool = 660001, CiscoGatePool = 660002, FermePool = 660003, OTpoolePool = 660005, MatlinPool = 660006;
        public const uint StalkerPool = 660201, GatekeeperPool = 660202, JuggernautPool = 660203;
        public const uint WardenPool = 660310, TorquaPool = 660311, InterrogatorPool = 660312;

        /// <summary>The four bunkers: their pillbox, the mortar pool on its roof (BaneMortars'), and the pools of its crew.</summary>
        public sealed record Bunker(string Name, uint Objective, uint Event, double X, double Y, double Z, uint MortarPool, uint[] CrewPools);

        public static readonly Bunker[] Bunkers =
        {
            new("NE", 1, 1, -27.70, 50.39, -69.01, 592135, new uint[] { 660104, 660105, 660106 }),
            new("NW", 2, 2, -104.32, 49.88, -67.75, 592134, new uint[] { 660101, 660102, 660103 }),
            new("SE", 10, 3, -3.27, 93.90, -268.12, 592136, new uint[] { 660110, 660111, 660112 }),
            new("SW", 11, 4, -148.25, 93.77, -238.61, 592133, new uint[] { 660107, 660108, 660109 }),
        };

        /// <summary>The five cell blocks: where the corridor narrows at their door, which wall the panels are on, and their pools.</summary>
        public sealed record CellBlock(uint Number, double X, double FloorY, double Z, int Side, uint SecurityPool, uint[] PrisonerPools);

        public static readonly CellBlock[] CellBlocks =
        {
            new(1, -216, 101.633, -500, -1, 660321, new uint[] { 660331, 660332 }),
            new(2, -88, 101.604, -500, -1, 660322, new uint[] { 660333, 660334 }),
            new(3, -88, 101.681, -564, -1, 660323, new uint[] { 660335, 660336 }),
            new(4, 40, 101.672, -468, 1, 660324, new uint[] { 660337, 660338 }),
            new(5, 40, 101.745, -564, 1, 660325, new uint[] { 660339, 660340 }),
        };

        public static readonly Pool[] Pools = BuildPools().ToArray();

        private static IEnumerable<Pool> BuildPools()
        {
            // People.
            yield return new(CiscoEntrancePool, CiscoAtEntrance, Automatic, 20, -258.0, 13.47, 306.0, -1.9644, 1, 0, Map, "Lt. Cisco at the AFS camp by the entrance");
            yield return new(CiscoGatePool, CiscoAtGate, Scripted, 10, -90.0, 96.54, -268.0, 0.8921, 1, 0, Map, "Lt. Cisco inside the gate, once it is open");
            yield return new(FermePool, Ferme, Automatic, 20, -139.5, 99.47, -556.0, -0.245, 1, 0, Map, "Ranger Ferme in the information extraction room");
            yield return new(OTpoolePool, OToole, Automatic, 20, -51.3, 116.47, 470.0, 0, 1, 0, Divide, "Field Sgt. O'Toole at Foreas Base");
            yield return new(MatlinPool, Matlin, Automatic, 20, -509.0, 141.13, 705.0, 0, 1, 0, Palisades, "Base Cmdr. Matlin at Cumbria Research");

            // The bunkers' crews: inside the pillbox, and on either side of it.
            yield return new(660101, ThraxSoldier, Automatic, 3000, -104.32, 50.67, -67.75, 0, 3, 5, Map, "NW bunker: Thrax inside");
            yield return new(660102, HominisMachina, Automatic, 3000, -92.32, 51.80, -55.75, 0, 2, 4, Map, "NW bunker: Hominis Machina");
            yield return new(660103, Amoeboid, Automatic, 3000, -116.32, 50.68, -79.75, 0, 2, 4, Map, "NW bunker: sludges");
            yield return new(660104, ThraxSoldier, Automatic, 3000, -27.70, 51.27, -69.01, 0, 3, 5, Map, "NE bunker: Thrax inside");
            yield return new(660105, ThraxGrenadier, Automatic, 3000, -15.70, 50.31, -57.01, 0, 2, 4, Map, "NE bunker: Grenadiers");
            yield return new(660106, HominisMachina, Automatic, 3000, -39.70, 54.03, -81.01, 0, 2, 4, Map, "NE bunker: Hominis Machina");
            yield return new(660107, ThraxSoldier, Automatic, 3000, -148.25, 94.67, -238.61, 0, 3, 5, Map, "SW bunker: Thrax inside");
            yield return new(660108, Amoeboid, Automatic, 3000, -136.25, 93.83, -226.61, 0, 2, 4, Map, "SW bunker: sludges");
            yield return new(660109, Hunter, Automatic, 3000, -160.25, 94.52, -250.61, 0, 2, 4, Map, "SW bunker: Hunters");
            yield return new(660110, ThraxSoldier, Automatic, 3000, -3.27, 94.67, -268.12, 0, 3, 5, Map, "SE bunker: Thrax inside");
            yield return new(660111, ThraxGrenadier, Automatic, 3000, 8.73, 94.01, -256.12, 0, 2, 4, Map, "SE bunker: Grenadiers");
            yield return new(660112, HominisMachina, Automatic, 3000, -15.27, 94.47, -280.12, 0, 2, 4, Map, "SE bunker: Hominis Machina");

            // The path up from the camp, and the plateau's others.
            yield return new(660211, ThraxSoldier, Automatic, 3000, -154.4, 10.47, 285.306, 0, 2, 6, Map, "Approach: by the camp");
            yield return new(660212, Amoeboid, Automatic, 3000, -173.9, 13.21, 328.9, 0, 2, 5, Map, "Approach: sludges by the camp");
            yield return new(660213, ThraxSoldier, Automatic, 3000, -138.8, 15.07, 311.706, 0, 2, 5, Map, "Approach: on the path east");
            yield return new(660214, HominisMachina, Automatic, 3000, -66.985, 25.56, 165.398, 0, 2, 5, Map, "Approach: climbing north");
            yield return new(660215, ThraxGrenadier, Automatic, 3000, -66.365, 21.93, 187.409, 0, 2, 5, Map, "Approach: climbing north");
            yield return new(660216, ThraxSoldier, Automatic, 3000, -68.1, 42.03, 85.9, 0, 2, 5, Map, "Approach: below the bridge");
            yield return new(660217, Hunter, Automatic, 3000, -78.073, 46.51, 10.433, 0, 2, 4, Map, "The bridge: north end, west");
            yield return new(660218, ThraxSoldier, Automatic, 3000, -42.5, 50.62, 6.506, 0, 2, 4, Map, "The bridge: north end, east");
            yield return new(660219, ThraxSoldier, Automatic, 3000, -246.0, 63.07, -132.7, 0, 2, 6, Map, "Plateau: west, by the Predator pad");
            yield return new(660220, Hunter, Automatic, 3000, -203.428, 62.11, -89.302, 0, 2, 5, Map, "Plateau: west");
            yield return new(660221, HominisMachina, Automatic, 3000, -162.0, 57.67, -103.095, 0, 2, 5, Map, "Plateau: west of the tower");
            yield return new(660222, ThraxSoldier, Automatic, 3000, 84.8, 73.27, -131.9, 0, 2, 6, Map, "Plateau: east");
            yield return new(660223, Amoeboid, Automatic, 3000, 51.2, 94.87, -191.1, 0, 2, 5, Map, "Plateau: east, high");
            yield return new(660224, ThraxGrenadier, Automatic, 3000, 1.387, 91.27, -193.219, 0, 2, 5, Map, "Plateau: south of the tower");
            yield return new(660225, ThraxSoldier, Automatic, 3000, 29.9, 96.9, -233.3, 0, 2, 5, Map, "Plateau: south east");
            yield return new(StalkerPool, Stalker, Scripted, 10, -66.0, 48.39, -26.0, 0, 1, 0, Map, "The alarm's Stalker, past the trip wire");
            yield return new(GatekeeperPool, GatekeeperXsic, Automatic, 3000, -80.0, 116.95, -184.0, 3.1092, 1, 0, Map, "Gatekeeper Xsic on the hill opposite the gate");
            yield return new(660204, ThraxSoldier, Automatic, 3000, -72.0, 114.87, -183.0, 0, 2, 3, Map, "The Gatekeeper's guard");
            yield return new(JuggernautPool, Juggernaut, Scripted, 10, -80.0, 100.35, -360.0, 0, 1, 0, Map, "The Juggernaut at the prison door, once the gate is open");

            // The prison.
            yield return new(660301, ThraxSoldier, Automatic, 3000, -80.0, 101.85, -420.0, 0, 2, 4, Map, "Prison: entrance hall");
            yield return new(660302, ThraxGrenadier, Automatic, 3000, -120.0, 101.47, -420.0, 0, 2, 4, Map, "Prison: west hall");
            yield return new(660303, Hunter, Automatic, 3000, -30.0, 101.47, -420.0, 0, 2, 4, Map, "Prison: east hall");
            yield return new(660304, ThraxSoldier, Automatic, 3000, -120.0, 100.8, -468.0, 0, 2, 4, Map, "Prison: by the prisoner computer banks");
            yield return new(660305, ThraxSoldier, Automatic, 3000, -88.0, 101.67, -548.0, 0, 2, 4, Map, "Prison: central hub");
            yield return new(660306, Hunter, Automatic, 3000, 40.0, 101.69, -484.0, 0, 2, 4, Map, "Prison: east hall");
            yield return new(660307, ThraxSoldier, Automatic, 3000, -14.0, 99.51, -556.0, 0, 2, 3, Map, "Prison: data storage centre");
            yield return new(660308, HominisMachina, Automatic, 3000, -14.0, 99.47, -478.0, 0, 2, 3, Map, "Prison: north control room");
            yield return new(660309, HominisMachina, Automatic, 3000, -66.4, 99.47, -668.0, 0, 2, 3, Map, "Prison: research ward");
            yield return new(WardenPool, CellBlockWarden, Automatic, 3000, -92.0, 101.61, -540.0, 0, 1, 0, Map, "The Cell Block Warden, in the central hub");
            yield return new(TorquaPool, OverseerTorqua, Automatic, 3000, 40.0, 99.47, -652.0, 0, 1, 0, Map, "Overseer Torqua in the data room");
            yield return new(660313, ThraxSoldier, Automatic, 3000, 46.0, 99.6, -664.0, 0, 2, 3, Map, "Overseer Torqua's guard");
            yield return new(InterrogatorPool, Interrogator, Automatic, 3000, -144.0, 99.56, -538.0, 0, 3, 4, Map, "Ranger Ferme's interrogators");
            yield return new(660314, Hunter, Automatic, 3000, -232.0, 108.87, -572.0, 0, 2, 3, Map, "Prison: west service room");

            // Each cell block's Bane security, called by a wrong panel, and its prisoners in their cells.
            var prisoners = new (double X, double Y, double Z)[]
            {
                (-222.0, 100.94, -516.5), (-210.0, 103.37, -510.5),
                (-94.0, 100.94, -516.5), (-82.0, 103.18, -510.5),
                (-94.0, 100.94, -580.5), (-82.0, 103.18, -574.5),
                (34.0, 100.94, -452.5), (46.0, 102.74, -446.5),
                (34.0, 100.94, -580.5), (46.0, 103.37, -574.5)
            };
            var security = new (double X, double Y, double Z)[]
            {
                (-216.0, 101.47, -516.0), (-88.0, 101.47, -516.0), (-88.0, 101.47, -580.0), (40.0, 101.54, -452.0), (40.0, 101.47, -580.0)
            };
            for (var b = 0; b < CellBlocks.Length; b++)
            {
                var block = CellBlocks[b];
                yield return new(block.SecurityPool, ThraxSoldier, Scripted, 10, security[b].X, security[b].Y, security[b].Z, 0, 3, 3, Map,
                    $"Cell block {block.Number}: Bane security");
                for (var p = 0; p < 2; p++)
                {
                    var spot = prisoners[b * 2 + p];
                    var creature = b == 0 && p == 0 ? ForeanPrisonerLeader : ForeanPrisoner;
                    yield return new(block.PrisonerPools[p], creature, Automatic, 3000, spot.X, spot.Y, spot.Z, 0, p == 0 ? 1u : 2u, 0.8, Map,
                        $"Cell block {block.Number}: prisoners");
                }
            }
        }

        // ---- Existing rows changed ------------------------------------------------------------

        /// <summary>The Divide seed's placeholder Thrax pools here: set scripted, so nothing wakes them.</summary>
        public static readonly uint[] PlaceholderPools =
            Enumerable.Range(530059, 21).Select(id => (uint)id).Where(id => id != 530060).ToArray();

        /// <summary>The courtyard's field medic and vendor: there once the gate is open.</summary>
        public static readonly uint[] CourtyardPools = { 500169, 500170 };

        public const double HamiltonWasX = -97.5, HamiltonWasY = 96.81, HamiltonWasZ = -272.100006103515625;
        public const double HamiltonX = -72.0, HamiltonY = 99.63, HamiltonZ = -648.0, HamiltonRotation = 0.0;

        // ---- Usable and destructible objects ----------------------------------------------------

        public const uint TripWireClass = 10415, AlarmLightClass = 9542, PanelClass = 7468, DoorSwitchClass = 7467,
            DatabladeClass = 7888, CommLinkClass = 11131, BeaconClass = 10191, GateFieldClass = 9572,
            StorageUnitClass = 20643, StoragePanelClass = 7502;

        public const uint GateKeyClass = 21718, GateKeyTemplate = 42634, AccessKeyClass = 25319, AccessKeyTemplate = 97391,
            DatapadClass = 7175, DatapadTemplate = 483, CoordinatesClass = 28657, CoordinatesTemplate = 119013,
            GalacticMapClass = 28655, MapCipherClass = 28654;

        public const uint TripWire = 660001, GateKeySlot = 660002, WormConsole = 660003, PrimaryTerminal = 660004,
            SecondaryTerminal = 660005, FermeCellConsole = 660006, ResearchWardConsole = 660007;
        public static readonly uint[] AlarmLights = { 660011, 660012, 660013, 660014, 660015, 660016 };

        /// <summary>The first panel of each block: panel n of block b is FirstPanel + (b - 1) * 10 + n - 1.</summary>
        public const uint FirstPanel = 660021;

        public static uint PanelId(uint block, uint panel) => FirstPanel + (block - 1) * 10 + panel - 1;

        public const uint SecurityBeacon = 660001, GateField = 660002, StorageUnitA = 660003, StorageUnitB = 660004,
            StoragePanelA = 660005, StoragePanelB = 660006;

        public sealed record Usable(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, uint State, bool IsUsable,
            uint ItemClassId, uint ItemCount, uint ResetSeconds, string Comment);

        public sealed record Destructible(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, uint HitPoints, uint Side, string Comment);

        public const uint TsOff = 55, TsOn = 56, IaActive = 119;

        /// <summary>
        /// The seven panels of a cell block, numbered as the wiki does from the player's side - left
        /// to right, top to bottom: two on top, three across the middle, two below. Offsets across
        /// the panel (to the player's right) and up, in metres.
        /// </summary>
        public static readonly (double Across, double Up)[] PanelLayout =
        {
            (-0.16, 0.30), (0.16, 0.30), (-0.32, 0), (0, 0), (0.32, 0), (-0.16, -0.30), (0.16, -0.30)
        };

        /// <summary>How far from the corridor's middle the panels stand, and how high over the floor their middle is.</summary>
        public const double PanelOffset = 3.2, PanelHeight = 1.35;

        /// <summary>Where panel <paramref name="panel"/> (1-7) of a block is, and which way it faces.</summary>
        public static (double X, double Y, double Z, double Rotation) PanelAt(CellBlock block, uint panel)
        {
            var (across, up) = PanelLayout[panel - 1];

            // On the west wall facing east the player's right is north (-Z); on the east wall, south (+Z).
            var rightZ = block.Side < 0 ? -1.0 : 1.0;

            return (block.X + block.Side * PanelOffset, block.FloorY + PanelHeight + up, block.Z + across * rightZ,
                block.Side < 0 ? Math.PI / 2 : -Math.PI / 2);
        }

        public static readonly Usable[] Usables = BuildUsables().ToArray();

        private static IEnumerable<Usable> BuildUsables()
        {
            yield return new(TripWire, TripWireClass, -62.265, 43.49, -1.465, -0.0066, TsOn, false, 0, 0, 0,
                "The trip wire across the bridge, between the map's two emitters");
            yield return new(GateKeySlot, DatabladeClass, -90.0, 98.35, -252.0, 0, IaActive, true, GateKeyClass, 1, 0,
                "Torcastra gate: where the Gatekeeper's key goes");
            yield return new(WormConsole, DatabladeClass, -16.0, 100.67, -550.4, 0, IaActive, true, DatapadClass, 0, 0,
                "Data storage centre: the control console the worm is uploaded to");
            yield return new(PrimaryTerminal, DatabladeClass, -16.0, 100.67, -486.3, 0, IaActive, true, DatapadClass, 0, 0,
                "North control room: the primary data terminal");
            yield return new(SecondaryTerminal, DatabladeClass, 39.96, 101.07, -627.3, 0, IaActive, true, DatapadClass, 0, 0,
                "Data room: the secondary data terminal");
            yield return new(FermeCellConsole, DoorSwitchClass, -141.0, 100.92, -552.0, 0.245, TsOff, true, 0, 0, 0,
                "Information extraction room: Ranger Ferme's cell console");
            yield return new(ResearchWardConsole, DatabladeClass, -63.0, 100.69, -648.0, -1.5708, IaActive, true, AccessKeyClass, 1, 0,
                "Research ward: Airman Hamilton's restraints, opened by the Cell Block Warden's pass key");

            var lights = new (double X, double Y, double Z, string Where)[]
            {
                (-73.98, 46.47, 4.101, "the bridge, west"), (-51.6, 48.27, 4.506, "the bridge, east"),
                (-92.3, 51.8, -55.75, "the NW bunker"), (-15.7, 50.31, -57.0, "the NE bunker"),
                (-136.25, 93.83, -226.6, "the SW bunker"), (8.7, 94.0, -256.1, "the SE bunker")
            };
            for (var i = 0; i < lights.Length; i++)
                yield return new(AlarmLights[i], AlarmLightClass, lights[i].X, lights[i].Y, lights[i].Z, 0, TsOff, false, 0, 0, 0,
                    $"Torcastra alarm light: {lights[i].Where}");

            foreach (var block in CellBlocks)
                for (uint panel = 1; panel <= 7; panel++)
                {
                    var at = PanelAt(block, panel);
                    yield return new(PanelId(block.Number, panel), PanelClass, at.X, at.Y, at.Z, at.Rotation, TsOff, true, 0, 0, 0,
                        $"Cell block {block.Number} console: panel {panel}");
                }
        }

        public static readonly Destructible[] Destructibles =
        {
            new(SecurityBeacon, BeaconClass, -74.0, 47.94, -12.0, 1.5708, 2500, 0, "The security beacon past the trip wire"),
            new(GateField, GateFieldClass, -77.64, 98.0, -257.98, Math.PI, 0, 1, "Torcastra's gate: the Bane force field across it"),
            new(StorageUnitA, StorageUnitClass, -27.0, 99.99, -548.0, 1.5708, 1500, 0, "Data storage centre: main storage bank A"),
            new(StorageUnitB, StorageUnitClass, -5.0, 99.92, -548.0, -1.5708, 1500, 0, "Data storage centre: main storage bank B"),
            new(StoragePanelA, StoragePanelClass, -16.0, 99.59, -539.0, Math.PI, 800, 0, "Data storage centre: second storage panel, north"),
            new(StoragePanelB, StoragePanelClass, -16.0, 99.57, -557.0, 0, 800, 0, "Data storage centre: second storage panel, south"),
        };

        // ---- The SQL ---------------------------------------------------------------------------

        private const string Slots =
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count";

        private const string CreatureColumns = "id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8";

        private static string InPool(uint creature) =>
            $"{creature} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id)";

        public static IEnumerable<string> Up()
        {
            foreach (var a in Attacks)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, 0, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var n in Npcs)
            {
                yield return $"INSERT INTO creature ({CreatureColumns}) " +
                    $"SELECT {n.Id}, {S(n.Comment)}, {n.ClassId}, 1, {n.Level}, 1000, {n.NameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {n.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {n.Id});";
                yield return CopyLook(n.Id, n.LookFrom);
                if (n.Package.HasValue)
                    yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {n.Id}, {n.Package.Value}, {S(Short(n.Comment))} " +
                        $"FROM creature c WHERE c.id = {n.Id} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {n.Id});";
            }

            foreach (var c in Combatants)
            {
                yield return $"INSERT INTO creature ({CreatureColumns}) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {c.Faction}, {c.Level}, {c.MaxHp}, {c.NameId}, {c.Run}, {c.Walk}, {c.Action1}, {c.Action2}, {c.Action3}, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";
                yield return $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT {c.Id}, 15, 15, 15, {c.MaxHp}, 65 " +
                    $"FROM creature x WHERE x.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {c.Id});";
                if (c.LookFrom != 0)
                    yield return CopyLook(c.Id, c.LookFrom);
            }

            foreach (var p in Pools)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " + Slots + ", radius) " +
                    $"SELECT {p.Id}, {p.Mode}, 0, {p.Respawn}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.MapContextId}, " +
                    $"{p.Creature}, {p.Count}, {p.Count}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", PlaceholderPools)}) AND map_context_id = {Map} AND mode = {Automatic};";
            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", CourtyardPools)}) AND map_context_id = {Map} AND mode = {Automatic};";
            yield return MoveHamilton(HamiltonWasX, HamiltonWasY, HamiltonWasZ, HamiltonX, HamiltonY, HamiltonZ);
            yield return $"UPDATE npc_package SET package_id = {HamiltonPackage} WHERE id = {Hamilton} AND package_id = {TerminalPackage};";

            foreach (var u in Usables)
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {u.Id}, {Map}, {u.ClassId}, {N(u.X)}, {N(u.Y)}, {N(u.Z)}, {N(u.Rotation)}, {u.State}, {(u.IsUsable ? 1 : 0)}, 0, {u.ItemClassId}, " +
                    $"{u.ItemCount}, {u.ResetSeconds}, 0, 0, 0, 0, 0, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {u.Id});";

            foreach (var d in Destructibles)
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {d.Id}, {Map}, {d.ClassId}, {N(d.X)}, {N(d.Y)}, {N(d.Z)}, {N(d.Rotation)}, {d.HitPoints}, 0, 0, {d.Side}, {S(d.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {d.ClassId} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {d.Id});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var d in Destructibles)
                yield return $"DELETE FROM world_destructible WHERE id = {d.Id} AND map_context_id = {Map} AND class_id = {d.ClassId};";

            foreach (var u in Usables)
                yield return $"DELETE FROM world_usable WHERE id = {u.Id} AND map_context_id = {Map} AND class_id = {u.ClassId};";

            yield return $"UPDATE npc_package SET package_id = {TerminalPackage} WHERE id = {Hamilton} AND package_id = {HamiltonPackage};";
            yield return MoveHamilton(HamiltonX, HamiltonY, HamiltonZ, HamiltonWasX, HamiltonWasY, HamiltonWasZ);
            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", CourtyardPools)}) AND map_context_id = {Map} AND mode = {Scripted};";
            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", PlaceholderPools)}) AND map_context_id = {Map} AND mode = {Scripted};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND map_context_id = {p.MapContextId} AND creature_1_Id = {p.Creature};";

            foreach (var c in Combatants)
            {
                if (c.LookFrom != 0)
                    yield return $"DELETE FROM creature_appearance WHERE id = {c.Id};";
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {InPool(c.Id)});";
            }

            foreach (var n in Npcs)
            {
                if (n.Package.HasValue)
                    yield return $"DELETE FROM npc_package WHERE id = {n.Id} AND package_id = {n.Package.Value};";
                yield return $"DELETE FROM creature_appearance WHERE id = {n.Id};";
                yield return $"DELETE FROM creature WHERE id = {n.Id} AND name_id = {n.NameId} AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {InPool(n.Id)});";
            }

            foreach (var a in Attacks)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id} " +
                    "AND NOT EXISTS (SELECT 1 FROM creature c WHERE " +
                    $"{a.Id} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        private static string CopyLook(uint creature, uint from) =>
            $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {creature}, a.slot_id, a.Class_id, a.color " +
            $"FROM creature_appearance a WHERE a.id = {from} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {creature}) " +
            $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {creature});";

        private static string MoveHamilton(double fromX, double fromY, double fromZ, double toX, double toY, double toZ) =>
            $"UPDATE spawnpool SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)} " +
            $"WHERE id = {Hamilton} AND map_context_id = {Map} AND creature_1_Id = {Hamilton} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01;";

        private static string Short(string comment) => comment.Length <= 50 ? comment : comment.Substring(0, 50);

        internal static string S(string value) => "'" + value.Replace("'", "''") + "'";

        internal static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
