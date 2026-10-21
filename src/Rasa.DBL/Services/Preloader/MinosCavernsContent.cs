using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Minos Caverns (adv_foreas_concordia_divide_minoscaverns, 1347): the Operation off the
    /// Divide by Delta Outpost, populated. The live servers' placements were not kept; these are
    /// reconstructed from the client's text, two walkthroughs of the live game (the Tabula Rasa
    /// wiki's and Ten Ton Hammer's), the .map's architecture and the navmesh. Levels 11-15.
    ///
    /// The caverns (the Forean crystal tunnels, north and west): warnet soldiers in groups of three
    /// or four about nine nests - two of them on the upper tunnel to the left of the main cavern,
    /// none in the tunnel from the medic or on the way to the Bane base - and miasmas at the
    /// junctions. Recon Officer Tyler sits with his broken leg in the southern vault, up the river
    /// from the Those Logos, by the flare the .map puts there. Scout Horlo stands at the corpse in
    /// the western dead end. A medical vendor stands with the field medic by the way in.
    ///
    /// The Bane base (east, Bane architecture): two security cameras flank its entrance, two more
    /// watch the crossroads and the main room; three Thrax hold the entrance hall, their pool
    /// restocking through the two teleporters there. The side room off the hall holds crates and
    /// the Thrax Technician General who carries the stolen datapad. The north door opens on the
    /// main room, with Kael at the door and in the room, and the prison beyond it to the east
    /// where Recon Officer Karik and Ranger Zola are held behind a cell door. The south door opens
    /// on the ramp up to the overlook, Kael along it, down into the control room where three Thrax
    /// Technicians work the four monitoring databanks round the Logos stone analyzer. The north
    /// and south doors are shut to players and blown open with a charge set beside each
    /// (PlacedUsables.BlowOpen), which takes one of the detonators Tyler hands out.
    ///
    /// The alarm (BaneAlarms): the cameras and the databanks bring Thrax through the base's eleven
    /// teleporters - eight reinforcement pools for the cameras, one at each teleporter from the
    /// entrance to the overlook, and three for the databanks' exit wave, at the teleporters on the
    /// way out of the control room - and light the alarm lights and the siren placed through the
    /// base. The two Thrax pools PR105 put at the control room and the north end are stood down
    /// (scripted, never woken): their teleporters are the alarm's now.
    ///
    /// Outside, on the Divide (1148): Lieutenant Sebastian in the Foreas Base command tower, who
    /// sends players after Tyler, and Corporal Foster by Delta Outpost's east gate, who sends them
    /// after the nests (MinosCavernsMissions).
    ///
    /// Ids: creatures, spawn pools, arrivals, creature actions, world objects and containers all
    /// take 568001 on, the instances' next block after the Ojasa hive's 565xxx, the Caves of
    /// Donn's 566xxx and the Devil's Den's 567xxx, and clear of the Divide's 530001-560022, the
    /// Wilderness missions' 630001-630199 and Pravus Research's 650xxx. Plain SQL, valid on SQLite
    /// and MySQL alike, every insert guarded on its id and every delete on what was inserted - so
    /// an id another migration took first would leave the row out silently, which
    /// MinosCavernsContentTests checks row by row.
    /// </summary>
    public static class MinosCavernsContent
    {
        public const uint Map = 1347;
        public const uint Divide = 1148;
        public const uint FirstId = 568001;

        #region Classes and names

        public const uint WarnetSoldierClass = 6262;       // Creature_Warnet_Soldier
        public const uint MiasmaClass = 6170;              // Creature_Miasma
        public const uint KaelClass = 4046;                // Bane_Kael_Standard
        public const uint KaelArmoredClass = 25413;        // Bane_Kael_Standard_Boss: the armoured Kael's model (3901) has no animation skeleton in the client's data
        public const uint SwapsetMale = 3846;              // NPC_Human_Swapset_Male
        public const uint SwapsetFemale = 3848;            // NPC_Human_Swapset_Female
        public const uint ForeanMale = 7034;               // Warrior Mela's class
        public const uint ServiceNpcClass = 20972;         // the vendors' and medics' class

        public const uint WarnetNestClass = 7099;          // UsableInertDestMinoswarnetnestDELETEV01
        public const uint CameraClass = 10290;             // UsableInertDestBaneCameraV01
        public const uint DatabankClass = 10191;           // UsableInertDestBaneSecurityMachineryV01
        public const uint AlarmLightClass = 9542;          // UsableTwoStateBaneAlarmLightV01
        public const uint SirenClass = 25663;              // UsableTwoStateBaneControlPointSirenV01Intrusion
        public const uint DoorClass = 9337;                // UsableDoorBaneIndustrialV01
        public const uint PrisonDoorClass = 6366;          // UsableDoorBanePrisonV01
        public const uint ChargeClass = 7870;              // UsableBombHumV01: the AFS charge with its red light
        public const uint MapChestClass = 11116;           // UsableItemDisp_item_crate_bane_v01CONVERT2TD
        public const uint LootCrateClass = 7894;           // UsableTreasureDispBaneCrateV01
        public const uint LootCrateClass2 = 7904;          // UsableTreasureDispBaneCrateV02

        /// <summary>GenItemDetonator (class 21871): the charge takes one.</summary>
        public const uint DetonatorClass = 21871;
        public const uint DetonatorTemplate = 44859;

        /// <summary>MisDivideItemStolenGalacticMap, for Stolen History (1860), which is not yet authored: the chest stands, not yet usable.</summary>
        public const uint GalacticMapTemplate = 119012;

        /// <summary>The Divide's own Thrax (PR105): the soldier and grenadier the teleporters send, and the technician.</summary>
        public const uint ThraxSoldier = 530001, ThraxGrenadier = 530002, ThraxTechnician = 530003;

        /// <summary>Bane Thrax Technician Boss (creature 48, level 15, "Thrax Technician General"): the datapad's carrier.</summary>
        public const uint TechnicianGeneral = 48;

        /// <summary>The entrance hall's Thrax: PR105's pool, kept, restocking through the entrance teleporters only.</summary>
        public const uint EntrancePool = 560008;

        /// <summary>PR105's other two Minos pools, stood down.</summary>
        public static readonly uint[] StoodDownPools = { 560006, 560007 };

        /// <summary>PR105's arrival rows of the entrance pool at the main room's two teleporters: re-pointed to the alarm pools there (<see cref="Teleporter.Row"/>).</summary>
        public static readonly uint[] EntranceArrivalsRepointed = { 36, 35 };

        /// <summary>Medical vendors' package (vendor.package_id) and the Thoria Das vendor whose stock is copied.</summary>
        public const uint MedicalVendorPackage = 87, StockFrom = 500155;

        /// <summary>wilderness_v1_thrax_10_14: the crates' loot, the level's Thrax gear.</summary>
        public const string CrateLoot = "wilderness_v1_thrax_10_14";

        #endregion

        #region Creatures

        public sealed record CreatureRow(uint Id, string Comment, uint ClassId, uint Faction, uint Level, uint MaxHp, uint NameId,
            uint RunSpeed, uint WalkSpeed, uint[] Actions, uint Body, uint Health, uint Armor, uint AppearanceFrom = 0, uint Package = 0);

        public const uint WarnetSoldier = 568001, Miasma = 568002, Kael = 568003, KaelArmored = 568004;
        public const uint Tyler = 568010, Karik = 568011, Zola = 568012, Horlo = 568013, Sebastian = 568014, Foster = 568015, MedicalVendor = 568016;

        /// <summary>Creature actions (creature_action), the Palisades warnet's, the Plateau Kael's and Bane Miasma 88's, scaled to the level.</summary>
        public const uint WarnetZap = 568001, MiasmaBite = 568002, MiasmaGasCloud = 568003, MiasmaCoalesce = 568004,
            KaelGroundPound = 568005, KaelSmash = 568006, KaelTectonicStrike = 568007;

        public sealed record ActionRow(uint Id, string Description, uint ActionId, uint ActionArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public static readonly ActionRow[] Actions =
        {
            new(WarnetZap, "Warnet Soldier Minos warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 24, 72, 13),
            new(MiasmaBite, "Miasma Minos regular", 174, 12, 0.5, 10.0, 800, 0, 24, 32, 3),
            new(MiasmaGasCloud, "Miasma Minos miasma_gas_cloud", 208, 1, 1.0, 10.0, 15000, 600, 0, 0, 1),
            new(MiasmaCoalesce, "Miasma Minos miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 27, 34, 3),
            new(KaelGroundPound, "Kael Minos kael_ground_pound", 171, 1, 0.5, 3.0, 4000, 1233, 117, 156, 1),
            new(KaelSmash, "Kael Minos kael_smash", 433, 1, 0.5, 3.0, 4000, 1033, 130, 182, 1),
            new(KaelTectonicStrike, "Kael Minos kael_tectonic_strike", 497, 1, 1.0, 20.0, 3500, 1033, 52, 78, 7)
        };

        /// <summary>
        /// The creatures. Hit points and armour sit between the Divide's level-15 Thrax (723/65)
        /// and its level-16 technician (1051/105); the NPCs are the Divide's officers' (1000/100).
        /// Looks are copied from NPCs already dressed on the Divide: Tyler and Karik Recon Officer
        /// Stevens's, Zola Agent Cicely's, Horlo Warrior Mela's, Sebastian Field Lt. Quinn's,
        /// Foster Sergeant Spencer's, the vendor the Thoria Das medical vendor's.
        /// </summary>
        public static readonly CreatureRow[] Creatures =
        {
            new(WarnetSoldier, "Warnet Soldier - Minos Caverns", WarnetSoldierClass, 0, 13, 600, 460, 9, 5, new[] { WarnetZap }, 15, 600, 50),
            new(Miasma, "Miasma - Minos Caverns", MiasmaClass, 0, 12, 500, 427, 9, 5, new[] { MiasmaBite, MiasmaGasCloud, MiasmaCoalesce }, 15, 500, 100),
            new(Kael, "Kael - Minos Caverns", KaelClass, 0, 14, 1051, 7722, 9, 5, new[] { KaelGroundPound, KaelSmash, KaelTectonicStrike }, 15, 1051, 105),
            new(KaelArmored, "Kael Armored Guard - Minos Caverns", KaelArmoredClass, 0, 15, 1500, 10370, 9, 5, new[] { KaelGroundPound, KaelSmash, KaelTectonicStrike }, 15, 1500, 150),
            new(Tyler, "Recon Officer Tyler - Minos Caverns", SwapsetMale, 1, 10, 1000, 3017, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510110, Package: 177),
            new(Karik, "Recon Officer Karik - Minos Caverns", SwapsetMale, 1, 10, 1000, 5442, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510110, Package: 523),
            new(Zola, "Ranger Zola - Minos Caverns", SwapsetFemale, 1, 10, 1000, 5443, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510098, Package: 524),
            new(Horlo, "Scout Horlo - Minos Caverns", ForeanMale, 1, 10, 1000, 10340, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510117, Package: 2380),
            new(Sebastian, "Lt. Sebastian - Foreas Base command tower", SwapsetMale, 1, 10, 1000, 125, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510106),
            new(Foster, "Corporal Foster - Delta Outpost east gate", SwapsetMale, 1, 10, 1000, 3016, 0, 0, Array.Empty<uint>(), 15, 1000, 100, AppearanceFrom: 510112),
            new(MedicalVendor, "Medical Vendor: Minos Caverns", ServiceNpcClass, 1, 5, 555, 9027, 0, 0, Array.Empty<uint>(), 15, 555, 100, AppearanceFrom: StockFrom)
        };

        #endregion

        #region Spawn pools

        public sealed record Pool(uint Id, uint MapContextId, uint Creature, uint Min, uint Max, double X, double Y, double Z, double Rotation,
            double Radius, uint Mode = 0, uint Respawn = 900, uint Pose = 0, uint Creature2 = 0, uint Min2 = 0, uint Max2 = 0, string Comment = null);

        /// <summary>Spawn pool modes (SpawnPoolManager): on its own timer, or only when something asks.</summary>
        public const uint Automatic = 0, Scripted = 2;

        /// <summary>NPC poses (NpcPose): crouched, sitting.</summary>
        public const uint Crouched = 3, Sitting = 6;

        /// <summary>
        /// A Bane teleporter of the .map, by the arrival the pool at it comes through: a spawnpool_arrival
        /// row of the pool's id, or, where PR105 already had the entrance pool arriving there, that row
        /// (<see cref="Row"/>), re-pointed to the alarm's pool.
        /// </summary>
        public sealed record Teleporter(uint Pool, double X, double Y, double Z, double Rotation, ulong EntityId, uint Row = 0);

        /// <summary>The alarm's reinforcement pools (BaneAlarms.MinosFirstCameraPool..MinosLastExitPool), one per teleporter; the pool stands beside its teleporter.</summary>
        public const uint FirstCameraPool = 568001, LastCameraPool = 568008, FirstExitPool = 568009, LastExitPool = 568011;

        public static readonly Teleporter[] Teleporters =
        {
            // The cameras' eight: the entrance hall's three, the main room's two, the north end, the chunnel turn, the overlook's first.
            new(568001, 158.32, 4.63, 39.12, 3.1195, 0x798700001364UL),
            new(568002, 158.11, 4.62, 58.76, -3.0776, 0x798700001365UL),
            new(568003, 178.97, 3.86, 63.90, 0.1139, 0x7987000012FBUL),
            new(568004, 213.77, 2.47, 106.54, 2.6506, 0x79870000131BUL, Row: 36),
            new(568005, 213.30, 2.52, 117.61, -2.6038, 0x79870000131CUL, Row: 35),
            new(568006, 156.01, 4.07, 152.07, -1.5929, 0x7987000011F5UL),
            new(568007, 186.83, 4.62, -22.64, 1.3129, 0x79870000130AUL),
            new(568008, 215.72, 12.07, -64.03, 0.0, 0x798700001303UL),
            // The databanks' three, on the way out of the control room: over it, at the overlook's end, at the chunnel turn.
            new(568009, 224.27, 12.00, -100.19, 2.8849, 0x7987000011D4UL),
            new(568010, 224.80, 12.04, -64.00, 3.1416, 0x798700001304UL),
            new(568011, 186.83, 4.62, -22.64, 1.3129, 0x79870000130AUL)
        };

        /// <summary>The reinforcement pools: scripted, Thrax soldiers (grenadiers at the first exit teleporter), at their teleporter as PR105's pools are.</summary>
        public static IEnumerable<Pool> ReinforcementPools() =>
            Teleporters.Select(t => new Pool(t.Pool, Map, t.Pool == 568009 ? ThraxGrenadier : ThraxSoldier, 2, 2,
                t.X, t.Y, t.Z, t.Rotation, 4, Scripted,
                Comment: t.Pool <= LastCameraPool ? "Minos Caverns alarm: the cameras' reinforcements" : "Minos Caverns alarm: the databanks' exit wave"));

        /// <summary>Where the nine nests stand (world_destructible 568001-568009); the warnets' pool sits on each, its radius spreading them round it.</summary>
        public static readonly (double X, double Y, double Z, string Where)[] Nests =
        {
            (-8.0, 36.02, 7.0, "upper tunnel, west end"),
            (30.0, 29.28, 9.0, "upper tunnel, east"),
            (40.0, 14.07, 30.0, "main cavern, by the offset tunnel"),
            (66.0, 15.07, 43.3, "main cavern, the dead end with the fallen soldier"),
            (84.0, 14.78, 20.0, "main cavern, the south passage"),
            (91.7, 16.27, 68.7, "main cavern, the east turn"),
            (62.0, 15.23, 62.0, "main cavern, the north turn"),
            (43.5, -2.85, -14.2, "the vault, the upper ledge"),
            (62.0, -15.24, -62.0, "the vault, by the crystal torches")
        };

        public const uint FirstNestPool = 568020;

        public static IEnumerable<Pool> NestPools() =>
            Nests.Select((n, i) => new Pool(FirstNestPool + (uint)i, Map, WarnetSoldier, 3, 4, n.X, n.Y, n.Z, 0, 6,
                Comment: $"Minos Caverns warnets at the nest: {n.Where}"));

        public static readonly Pool[] OtherPools =
        {
            // Miasmas at the junctions of the caverns.
            new(568029, Map, Miasma, 2, 2, 20.0, 14.38, 24.0, 0, 5, Comment: "Minos Caverns miasmas at the first junction"),
            new(568030, Map, Miasma, 2, 2, 57.7, 13.67, 48.1, 0, 5, Comment: "Minos Caverns miasmas at the main cavern's tee"),
            new(568031, Map, Miasma, 1, 2, -4.0, 16.5, 40.0, 0, 4, Comment: "Minos Caverns miasmas at the foot of the entrance tunnel"),
            // The Bane base's Kael, at the doors, in the rooms, along the overlook.
            new(568032, Map, Kael, 2, 2, 203.0, 1.99, 97.0, 3.1416, 3, Comment: "Minos Caverns Kael inside the north door"),
            new(568033, Map, Kael, 2, 2, 206.0, 3.14, 50.0, 0, 3, Comment: "Minos Caverns Kael at the south door"),
            new(568034, Map, Kael, 2, 2, 203.9, 1.67, 118.5, 0, 8, Creature2: KaelArmored, Min2: 1, Max2: 1, Comment: "Minos Caverns Kael in the main room"),
            new(568035, Map, Kael, 2, 2, 250.0, 3.84, 113.0, 1.5708, 4, Comment: "Minos Caverns Kael guarding the prison"),
            new(568036, Map, Kael, 2, 2, 220.0, 11.07, -30.0, 0, 6, Creature2: KaelArmored, Min2: 1, Max2: 1, Comment: "Minos Caverns Kael on the overlook, the way to the databanks"),
            new(568037, Map, KaelArmored, 2, 2, 176.0, 1.67, -100.0, 0, 6, Comment: "Minos Caverns armored Kael in the control room"),
            new(568038, Map, ThraxTechnician, 3, 3, 168.0, 1.67, -92.0, 0, 6, Comment: "Minos Caverns technicians at the databanks"),
            new(568039, Map, TechnicianGeneral, 1, 1, 127.9, 2.92, 75.9, 1.5708, 2, Comment: "Minos Caverns: the Thrax Technician General with the datapad, in the side room"),
            // The NPCs.
            new(568041, Map, Tyler, 1, 1, 58.0, -30.41, -49.0, 3.1416, 0, Pose: Sitting, Comment: "Recon Officer Tyler, sitting with his broken leg by the flare in the vault"),
            new(568042, Map, Karik, 1, 1, 294.0, 3.87, 111.0, 4.7124, 0, Pose: Crouched, Comment: "Recon Officer Karik, held in the prison"),
            new(568043, Map, Zola, 1, 1, 298.0, 3.87, 116.0, 3.1416, 0, Pose: Crouched, Comment: "Ranger Zola, held in the prison"),
            new(568044, Map, Horlo, 1, 1, -47.0, 29.95, 19.0, 1.5708, 0, Comment: "Scout Horlo at the corpse in the western dead end"),
            new(568045, Map, MedicalVendor, 1, 1, -4.0, 32.08, 113.5, 4.7124, 0, Comment: "Medical Vendor: Minos Caverns, by the field medic"),
            new(568046, Divide, Sebastian, 1, 1, -48.7, 122.01, 430.8, 0.7854, 0, Comment: "Lt. Sebastian in the Foreas Base command tower"),
            new(568047, Divide, Foster, 1, 1, 492.0, 81.89, -352.0, 1.5708, 0, Comment: "Corporal Foster by Delta Outpost's east gate, where the Minos trail leaves")
        };

        public static IEnumerable<Pool> AllPools() => ReinforcementPools().Concat(NestPools()).Concat(OtherPools);

        #endregion

        #region World objects

        public sealed record Destructible(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, string Comment);

        public const uint FirstNest = 568001, FirstCamera = 568010, FirstDatabank = 568014;

        /// <summary>Yaws, as a player's rotation: a camera faces (-sin, 0, -cos) (BaneAlarms.LensOf). The caverns lie at -x of the base, its main room at +z of the north door.</summary>
        public const double FacingMinusX = 1.5708, FacingPlusX = 4.7124, FacingMinusZ = 0.0, FacingPlusZ = 3.1416;

        public static IEnumerable<Destructible> Destructibles()
        {
            foreach (var (n, i) in Nests.Select((n, i) => (n, i)))
                yield return new Destructible(FirstNest + (uint)i, WarnetNestClass, n.X, n.Y, n.Z, 0, $"Minos Caverns warnet nest: {n.Where}");

            yield return new Destructible(FirstCamera, CameraClass, 148.14, 5.67, 45.0, FacingMinusX, "Minos Caverns security camera flanking the base entrance, watching the approach from the caverns");
            yield return new Destructible(FirstCamera + 1, CameraClass, 148.54, 5.07, 54.91, FacingMinusX, "Minos Caverns security camera flanking the base entrance, the other side, watching the approach from the caverns");
            yield return new Destructible(FirstCamera + 2, CameraClass, 206.94, 3.27, 73.31, FacingMinusX, "Minos Caverns security camera at the crossroads, watching the entrance hall");
            yield return new Destructible(FirstCamera + 3, CameraClass, 203.0, 2.17, 93.0, FacingMinusZ, "Minos Caverns security camera inside the north door, watching the door");

            yield return new Destructible(FirstDatabank, DatabankClass, 160.0, 1.87, -88.0, FacingPlusX, "Minos Caverns monitoring databank, north-west of the analyzer");
            yield return new Destructible(FirstDatabank + 1, DatabankClass, 184.0, 1.95, -88.0, FacingMinusX, "Minos Caverns monitoring databank, north-east of the analyzer");
            yield return new Destructible(FirstDatabank + 2, DatabankClass, 160.0, 1.85, -104.0, FacingPlusX, "Minos Caverns monitoring databank, south-west of the analyzer");
            yield return new Destructible(FirstDatabank + 3, DatabankClass, 184.0, 1.86, -104.0, FacingMinusX, "Minos Caverns monitoring databank, south-east of the analyzer");
        }

        public sealed record Usable(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, uint State, uint Usable_, uint ItemTemplate,
            uint ItemClass, uint ItemCount, uint Reset, uint IntervalMin, uint Radius, uint DamageMin, uint DamageMax, string Comment);

        /// <summary>
        /// A charge: takes one detonator, goes off 5 s later, and its blast reaches 5 m (the door is
        /// 3.5-4.5 m off, within PlacedUsables.BlowOpenRadius), with a blast that warns whoever stayed
        /// beside it rather than kills a level-12 (the kind's own is 300-500 over 8 m).
        /// </summary>
        public const uint ChargeFuseSeconds = 5, ChargeRadius = 5, ChargeDamageMin = 60, ChargeDamageMax = 90;

        public const uint FirstLight = 568001, SirenId = 568005, NorthDoor = 568006, NorthCharge = 568007, SouthDoor = 568008, SouthCharge = 568009,
            PrisonDoor = 568010, MapChest = 568011;

        public static readonly Usable[] Usables =
        {
            new(FirstLight, AlarmLightClass, 149.67, 4.51, 46.21, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns alarm light at the base entrance"),
            new(FirstLight + 1, AlarmLightClass, 205.74, 2.87, 69.31, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns alarm light at the crossroads"),
            new(FirstLight + 2, AlarmLightClass, 196.0, 1.67, 104.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns alarm light in the main room"),
            new(FirstLight + 3, AlarmLightClass, 164.0, 1.67, -104.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns alarm light in the control room"),
            new(SirenId, SirenClass, 202.84, 3.12, 69.43, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns intrusion siren at the crossroads"),
            // The north and south doors, shut to players, and the charge beside each: blown open, they stay open (reset 0).
            new(NorthDoor, DoorClass, 204.0, 2.27, 90.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns north security door, to the main room: blown open with the charge beside it"),
            new(NorthCharge, ChargeClass, 201.5, 2.52, 87.5, 0, 0, 1, 0, DetonatorClass, 1, 0, ChargeFuseSeconds, ChargeRadius, ChargeDamageMin, ChargeDamageMax, "Minos Caverns charge at the north security door: takes a detonator"),
            new(SouthDoor, DoorClass, 201.93, 3.38, 60.07, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns south security door, to the overlook ramp: blown open with the charge beside it"),
            new(SouthCharge, ChargeClass, 200.52, 3.32, 63.47, 0, 0, 1, 0, DetonatorClass, 1, 0, ChargeFuseSeconds, ChargeRadius, ChargeDamageMin, ChargeDamageMax, "Minos Caverns charge at the south security door: takes a detonator"),
            new(PrisonDoor, PrisonDoorClass, 229.0, 2.71, 113.0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns prison cell door, opened by hand"),
            new(MapChest, MapChestClass, 159.0, 3.77, 70.0, 0, 0, 0, GalacticMapTemplate, 0, 0, 0, 0, 0, 0, 0, "Minos Caverns: the crate with the stolen Galactic Map (Stolen History 1860, not yet authored: not usable)")
        };

        public sealed record Container(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, string Comment);

        public const uint FirstCrate = 568001;

        public static readonly Container[] Crates =
        {
            new(FirstCrate, LootCrateClass, 120.0, 2.96, 74.0, 1.5708, "Minos Caverns Bane crate in the side room off the entrance hall"),
            new(FirstCrate + 1, LootCrateClass2, 125.9, 3.05, 77.9, 3.1416, "Minos Caverns Bane crate in the side room off the entrance hall"),
            new(FirstCrate + 2, LootCrateClass, 132.0, 3.07, 73.7, 4.7124, "Minos Caverns Bane crate in the side room off the entrance hall")
        };

        #endregion

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            // An action row has nothing to hang on: the SELECT names a table only so that its
            // WHERE is one MySQL takes as well as SQLite.
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
                    $"SELECT {c.Id}, {c.Body}, {c.Body}, {c.Body}, {c.Health}, {c.Armor} FROM creature c WHERE c.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {c.Id});";

                if (c.AppearanceFrom != 0)
                    yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                        $"SELECT {c.Id}, a.slot_id, a.Class_id, a.color FROM creature_appearance a WHERE a.id = {c.AppearanceFrom} " +
                        $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {c.Id}) AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {c.Id} AND x.slot_id = a.slot_id);";

                if (c.Package != 0)
                    yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {c.Id}, {c.Package}, {S(c.Comment)} " +
                        $"FROM creature c WHERE c.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {c.Id});";
            }

            yield return $"INSERT INTO vendor (id, package_id) SELECT {MedicalVendor}, {MedicalVendorPackage} FROM creature c " +
                $"WHERE c.id = {MedicalVendor} AND NOT EXISTS (SELECT 1 FROM vendor x WHERE x.id = {MedicalVendor});";
            yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {MedicalVendor}, i.item_template_id FROM vendor_item i " +
                $"WHERE i.id = {StockFrom} AND EXISTS (SELECT 1 FROM vendor v WHERE v.id = {MedicalVendor}) " +
                $"AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {MedicalVendor} AND x.item_template_id = i.item_template_id);";

            foreach (var p in AllPools())
                yield return InsertPool(p);

            foreach (var p in AllPools().Where(p => p.Pose != 0))
                yield return $"INSERT INTO spawnpool_pose (id, pose) SELECT {p.Id}, {p.Pose} FROM spawnpool p WHERE p.id = {p.Id} AND NOT EXISTS (SELECT 1 FROM spawnpool_pose x WHERE x.id = {p.Id});";

            foreach (var t in Teleporters.Where(t => t.Row == 0))
                yield return "INSERT INTO spawnpool_arrival (id, pool_id, kind, pos_x, pos_y, pos_z, rotation, entity_id, comment) " +
                    $"SELECT {t.Pool}, {t.Pool}, 2, {N(t.X)}, {N(t.Y)}, {N(t.Z)}, {N(t.Rotation)}, {t.EntityId}, 'foreas_concordia_divide_minoscaverns teleporter (alarm)' " +
                    $"FROM spawnpool p WHERE p.id = {t.Pool} AND NOT EXISTS (SELECT 1 FROM spawnpool_arrival x WHERE x.id = {t.Pool});";

            // PR105's pools: the entrance hall's keeps the hall's three teleporters and holds three, its
            // two arrivals in the main room going to the alarm pools there; the other two stand down.
            foreach (var t in Teleporters.Where(t => t.Row != 0))
                yield return $"UPDATE spawnpool_arrival SET pool_id = {t.Pool} WHERE id = {t.Row} AND pool_id = {EntrancePool} AND entity_id = {t.EntityId} " +
                    $"AND EXISTS (SELECT 1 FROM spawnpool p WHERE p.id = {t.Pool});";
            yield return $"UPDATE spawnpool SET creature_1_min_count = 3, creature_1_max_count = 3 WHERE id = {EntrancePool} AND map_context_id = {Map} AND creature_1_Id = {ThraxSoldier} AND creature_1_min_count = 2 AND creature_1_max_count = 3;";
            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", StoodDownPools)}) AND map_context_id = {Map} AND mode = {Automatic};";

            foreach (var d in Destructibles())
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {d.Id}, {Map}, {d.ClassId}, {N(d.X)}, {N(d.Y)}, {N(d.Z)}, {N(d.Rotation)}, 0, 0, 0, 0, {S(d.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {d.ClassId} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {d.Id});";

            foreach (var u in Usables)
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {u.Id}, {Map}, {u.ClassId}, {N(u.X)}, {N(u.Y)}, {N(u.Z)}, {N(u.Rotation)}, {u.State}, {u.Usable_}, {u.ItemTemplate}, {u.ItemClass}, " +
                    $"{u.ItemCount}, {u.Reset}, {u.IntervalMin}, 0, {u.DamageMin}, {u.DamageMax}, {u.Radius}, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {u.Id});";

            foreach (var c in Crates)
                yield return "INSERT INTO world_container (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, loot_profile_key, cipher_level, reset_seconds, comment) " +
                    $"SELECT {c.Id}, {Map}, {c.ClassId}, {N(c.X)}, {N(c.Y)}, {N(c.Z)}, {N(c.Rotation)}, {S(CrateLoot)}, 0, 0, {S(c.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND EXISTS (SELECT 1 FROM loot_profile l WHERE l.profile_key = {S(CrateLoot)}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM world_container x WHERE x.id = {c.Id});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM world_container WHERE id BETWEEN {FirstCrate} AND {FirstCrate + (uint)Crates.Length - 1} AND map_context_id = {Map};";
            yield return $"DELETE FROM world_usable WHERE id BETWEEN {Usables.Min(u => u.Id)} AND {Usables.Max(u => u.Id)} AND map_context_id = {Map};";
            yield return $"DELETE FROM world_destructible WHERE id BETWEEN {Destructibles().Min(d => d.Id)} AND {Destructibles().Max(d => d.Id)} AND map_context_id = {Map};";

            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", StoodDownPools)}) AND map_context_id = {Map} AND mode = {Scripted};";
            yield return $"UPDATE spawnpool SET creature_1_min_count = 2, creature_1_max_count = 3 WHERE id = {EntrancePool} AND map_context_id = {Map} AND creature_1_Id = {ThraxSoldier} AND creature_1_min_count = 3 AND creature_1_max_count = 3;";
            foreach (var t in Teleporters.Where(t => t.Row != 0))
                yield return $"UPDATE spawnpool_arrival SET pool_id = {EntrancePool} WHERE id = {t.Row} AND pool_id = {t.Pool} AND entity_id = {t.EntityId};";
            yield return $"DELETE FROM spawnpool_arrival WHERE id BETWEEN {Teleporters.Min(t => t.Pool)} AND {Teleporters.Max(t => t.Pool)} AND pool_id = id;";
            yield return $"DELETE FROM spawnpool_pose WHERE id IN ({string.Join(", ", AllPools().Where(p => p.Pose != 0).Select(p => p.Id))});";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {AllPools().Min(p => p.Id)} AND {AllPools().Max(p => p.Id)} AND map_context_id IN ({Map}, {Divide});";

            yield return $"DELETE FROM vendor_item WHERE id = {MedicalVendor};";
            yield return $"DELETE FROM vendor WHERE id = {MedicalVendor} AND package_id = {MedicalVendorPackage};";

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
