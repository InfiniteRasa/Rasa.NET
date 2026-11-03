using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Magma Caverns (adv_arieki_ligo_burningsteps_magmacaverns, 1977), off Torden Abyss, as the
    /// Ten Ton Hammer Deployment 8 dev diary (Jason Ford, Michael Hutchison) tells it and the
    /// 1.16.5 client's texts name it: the abandoned Brann research station where Lieutenant Kane
    /// asks a squad whose side it is on, the Rebels' camp past the Sakra Oasis and their front
    /// lines in the Magmonix Grounds, Penumbra's landing zones in Agni's Cauldron, and Ember's
    /// den. Five missions:
    ///  - Consensus (1942), Kane's: the choice, repeatable, no reward. Each member in the copy
    ///    votes; the majority decides for the copy, a tie goes to the first to vote. The decision
    ///    is flag <see cref="SideFlag"/>, which offers one of the two arcs.
    ///  - Final Chance (1945), the Rebels': Salyard, Sudevi, two Penumbra assaults through
    ///    portable wormholes, Nelby, an optional Rebel wormhole, the landing zones, the two GPS
    ///    beacons, Dama and Mezen.
    ///  - Executioner (1944), Penumbra's: Mezen, Dama, the Rebel assault on the landing zones,
    ///    the two blocked doors, Nelby, Sudevi, the camp, Salyard, his laptop, and the Thrax
    ///    research subjects.
    ///  - Creature Strike (1457), Sudevi's, for the Rebels' side: the Beam Mantas and Barb Ticks
    ///    from the oasis against the camp's defenses and their turret.
    ///  - Lava Beast (1474), Kane's, whichever side: Ember's lair, and Ember - the client's
    ///    Creature_Magmonix_Boss, which shrugs off bullets and fire as every Magmonix does
    ///    (IMMUNE_PHYSICAL, IMMUNE_FIRE, IMMUNE_SONIC): ice, lasers and the rest hurt him.
    /// Both sides' rows stand in the map Neutral; what a copy's side makes them, who takes harm,
    /// the doors, wormholes, waves, beacons, laptop, cells, teleporter and Ember's treasure are
    /// MagmaCaverns's, per copy. Every position is on the map's navmesh. What the client does not
    /// hold - the levels, health, looks, soldier mixes, wave sizes and the arcs' rewards - is
    /// marked ESTIMATE: health 10000 x 2^((level - 42) / 8) as Crucible's rows sit, an officer
    /// one and three quarters of it and a leader two and a half; armour ten a level less twenty.
    /// </summary>
    public static class MagmaCavernsContent
    {
        public const string Revision = "magma_1_16_5";
        public const uint MagmaCaverns = 1977;

        /// <summary>missioncategorylanguage: "Instance (Magma Caverns)".</summary>
        public const uint Category = 10000020;

        public const uint Consensus = 1942, Executioner = 1944, FinalChance = 1945, CreatureStrike = 1457, LavaBeast = 1474;
        public static readonly uint[] Missions = { Consensus, Executioner, FinalChance, CreatureStrike, LavaBeast };

        /// <summary>A member's vote (Kane's choice), and the copy's decision as it reached them: <see cref="PenumbraSide"/> or <see cref="RebelSide"/>.</summary>
        public const uint VoteFlag = 1942001, SideFlag = 1942002;
        public const uint PenumbraSide = 1, RebelSide = 2;

        // The packages of the people's conversations (objectiveconversation).
        public const uint KanePackage = 2458, SalyardPackage = 1760, SudeviPackage = 1759, NelbyPackage = 1751,
            MezenPackage = 1763, DamaPackage = 1761;

        // The people.
        public const uint Kane = 678001, Salyard = 678002, Sudevi = 678003, Nelby = 678004, Mezen = 678005, Dama = 678006, Ember = 678007;

        // The Rebels and the Thrax they keep.
        public const uint Guerrilla = 678010, Assassin = 678011, Marine = 678012, ConvertedThrax = 678013, Defender = 678014,
            RebelMedic = 678015, PacifiedThrax = 678016;

        // Penumbra.
        public const uint BlackOps = 678020, ShockTrooper = 678021, Spy = 678022, SpecOps = 678023, PenumbraMedic = 678024;

        // The creatures.
        public const uint BeamManta = 678030, BarbTick = 678031, Magmonix = 678032;

        /// <summary>The AFS light turrets already at the Rebel camp's defenses (pools 550008 to 550010): the Rebels'.</summary>
        public const uint Turret = 550003;

        /// <summary>Crucible's Flaregashers (pools 540128 to 540134).</summary>
        public const uint Flaregasher = 540030;

        public static readonly uint[] RebelRows = { Salyard, Sudevi, Nelby, Guerrilla, Assassin, Marine, ConvertedThrax, Defender, RebelMedic, Turret };
        public static readonly uint[] PenumbraRows = { Mezen, Dama, BlackOps, ShockTrooper, SpecOps, Spy, PenumbraMedic };
        public static readonly uint[] Officers = { Salyard, Sudevi, Nelby, Mezen, Dama };

        // The hospitals' teleporters: the entrance's, the Rebel camp's and the landing zone's.
        public const uint EntranceHospital = 597, RebelHospital = 485, PenumbraHospital = 596;

        // The scenario of each mission whose events MagmaCaverns records (one each), and its events.
        public const uint Scenario = 1;

        // Consensus.
        public const uint SidedPenumbra = 1, SidedRebel = 2, Crossed = 3, UsedTeleporter = 4, UsedBackDoor = 5;
        public const uint SpeakToKane = 1, WaitForSquad = 2, Hidden = 10, TakeTeleporter = 11, TakeBackDoor = 12;

        // Final Chance: objectives, and events.
        public const uint FcSalyard = 7, FcSudevi = 1, FcShutWormhole1 = 8, FcHoldCamp = 2, FcBonus1 = 16, FcNelby = 5,
            FcShutWormhole2 = 9, FcHoldDoor = 6, FcBonus2 = 17, FcRebelWormhole = 14, FcInfiltrate = 10, FcBeacon1 = 11,
            FcBeacon2 = 12, FcDama = 15, FcMezen = 13;
        public const uint FcWormhole1Shut = 1, FcAssault1Over = 2, FcBonus1Won = 3, FcBonus1Lost = 4, FcWormhole2Shut = 5,
            FcAssault2Over = 6, FcBonus2Won = 7, FcBonus2Lost = 8, FcRebelWormholeSet = 9, FcInfiltrated = 10,
            FcBeacon1Placed = 11, FcBeacon2Placed = 12;

        // Executioner: objectives, and events.
        public const uint ExMezen = 1, ExDama = 2, ExStopAssault = 3, ExBonus1 = 15, ExBonus2 = 16, ExShutWormhole = 4,
            ExWormhole1 = 5, ExFrontLine = 6, ExDoor1 = 17, ExNelby = 7, ExWormhole2 = 8, ExDoor2 = 18, ExStronghold = 19,
            ExSudevi = 9, ExCamp = 10, ExSalyard = 11, ExLaptop = 12, ExRelease = 13, ExSubjects = 14;
        public const uint ExAssaultOver = 1, ExBonus1Won = 2, ExBonus1Lost = 3, ExBonus2Won = 4, ExBonus2Lost = 5,
            ExWormholeShut = 6, ExWormhole1Set = 7, ExFrontFound = 8, ExDoor1Blown = 9, ExWormhole2Set = 10, ExDoor2Blown = 11,
            ExStrongholdFound = 12, ExCampEntered = 13, ExLaptopRead = 14, ExSubjectsReleased = 15, ExSubjectsDead = 16;
        public const uint SubjectsWanted = 4;

        // Creature Strike and Lava Beast.
        public const uint StrikeDefeat = 2, StrikeOver = 1;
        public const uint FindLair = 2, KillEmber = 1, LairFound = 1;

        // The objects MagmaCaverns puts down.
        public const uint TeleporterClass = 7895;       // UsableTwoStateHumTeleporter
        public const uint WormholeClass = 25408;        // UsableTwoStateHumWormhole
        public const uint DoorClass = 11144;            // UsableDoorHumCorridorV01: the corridors' doorjambs' door
        public const uint BeaconClass = 9474;           // UsableItemAccHumObjGPSBeaconV01
        public const uint LaptopClass = 26498;          // UsableTwoStateHumComputerDellXpsNotebookV01
        public const uint CellsClass = 21486;           // the prison block's forcefield
        public const uint TreasureClass = 7931;         // a crate: Ember's hoard

        // Where things are, on the map's navmesh (x, height, z).
        public static readonly Vector3 Entrance = new(-368.1f, 206.6f, -441.9f);

        /// <summary>Arkhara Research Station: Kane, its teleporter pad, where the pad puts one down, and the airlock out the back.</summary>
        public static readonly Vector3 KaneSpot = new(-381f, 187.65f, -321f), StationTeleporter = new(-380f, 187.65f, -285f),
            StationArrival = new(-380f, 187.65f, -292f), BackDoor = new(-396f, 195.8f, -252f);

        /// <summary>The Rebel camp: its teleporter pad and where it puts one down, Salyard, his laptop, the prison block's cells.</summary>
        public static readonly Vector3 CampTeleporter = new(-389f, 140.07f, 588f), Camp = new(-374.4f, 129.99f, 558f),
            SalyardSpot = new(-318f, 136.05f, 548f), Laptop = new(-307.6f, 136.05f, 547f), Cells = new(-302f, 132.05f, 551f);

        public static readonly Vector3[] CellSpots = { new(-304f, 132.05f, 554f), new(-300f, 132.05f, 554f) };

        /// <summary>The camp's defenses, below the tunnel from the oasis: Sudevi, and the turret of pool 550010.</summary>
        public static readonly Vector3 SudeviSpot = new(-360.2f, 134.23f, 439.6f), Defenses = new(-372f, 134.67f, 432f);
        public static readonly Vector3 TunnelOasisEnd = new(-344f, 147.65f, 466f), TunnelCampEnd = new(-385f, 139.68f, 508f);

        /// <summary>The Sakra Oasis, and its north end past the west door, where Penumbra's first wormhole stands.</summary>
        public static readonly Vector3 Sakra = new(-372f, 133.96f, 346f), OasisNorth = new(-398f, 132.66f, 214f);

        /// <summary>The corridors' doorjambs (ArchHumCorridorDoorjamb018mV01): the west's into the oasis, the east's into the Grounds.</summary>
        public static readonly Vector3 WestDoor = new(-303.5f, 141.5f, 184f), EastDoor = new(-87.3f, 131.9f, 174.6f);
        public static readonly Vector3 WestDoorWay = new(-300f, 145.95f, 183f), EastDoorWay = new(-88f, 137.47f, 150f);

        /// <summary>The Magmonix Grounds' front lines: Nelby, and Penumbra's second wormhole by the east door.</summary>
        public static readonly Vector3 NelbySpot = new(-200f, 132.25f, 170f), GroundsWormhole = new(-110f, 136.55f, 210f),
            GroundsSetup = new(-262f, 131.89f, 158.8f);

        /// <summary>The east corridor up from the long tunnel: the Rebels' front, as Penumbra locate it.</summary>
        public static readonly Vector3 EastCorridor = new(-80f, 138.85f, 95f);

        /// <summary>Agni's Cauldron: the landing zones, Dama by the first and Mezen by the second, the Rebels' way in.</summary>
        public static readonly Vector3 LandingZone1 = new(-512.3f, 168.43f, -21.5f), LandingZone2 = new(-396f, 169.1f, -93f),
            DamaSpot = new(-505f, 168.67f, -30f), MezenSpot = new(-398f, 169.09f, -97f), RebelWormhole = new(-416f, 157.9f, 45f),
            RebelApproach = new(-424f, 158.93f, 23f), CauldronRim = new(-488f, 177.5f, -103f), CauldronSetup = new(-470f, 171.45f, -6f);

        /// <summary>Ember's den in the Grounds' graveyard cave, and his hoard's place.</summary>
        public static readonly Vector3 Lair = new(-117.6f, 143.05f, 317.6f), Treasure = new(-112f, 143.59f, 318f);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1, Neutral = 3 }

        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A spawn pool: its creatures (row, least, most), where and which way, its respawn time in tenths of a second.</summary>
        public sealed record Spot(uint Id, double X, double Y, double Z, double Rotation, uint RespawnTenths, double Radius,
            params (uint Creature, uint Min, uint Max)[] Slots);

        // Actions: the soldiers' (ESTIMATE, at Crucible's damage) and the creatures' own (Crucible and Thunderhead rows).
        public const uint Rifle = 678101, Pistol = 678102, MachineGunFire = 678103, EmberSlam = 678104, MagmonixSlam = 678105;
        public const uint GrenadierGrenade = 53029, GrenadierRifle = 53030, MantaBeam = 55062, MantaLightning = 76027,
            TickBite = 55043, FlaregasherFireball = 55047;

        public static readonly Act[] Actions =
        {
            new(Rifle, "Magma Caverns soldier rifle 1/134", 1, 134, 1, 30, 1500, 0, 380, 570, 1),
            new(Pistol, "Magma Caverns officer pistol 1/133", 1, 133, 0.5, 25, 1300, 0, 300, 450, 1),
            new(MachineGunFire, "Magma Caverns heavy machine gun 149/1", 149, 1, 1, 25, 1000, 0, 260, 390, 1),
            new(EmberSlam, "Ember - Magma Caverns slam 1/1", 1, 1, 0.5, 6, 2500, 0, 1500, 2200, 1),
            new(MagmonixSlam, "Magmonix - Magma Caverns slam 1/1", 1, 1, 0.5, 4, 1600, 0, 600, 850, 1)
        };

        // Levels and health (ESTIMATE): 10000 x 2^((level - 42) / 8).
        private const uint Hp44 = 11892, Hp45 = 12968, Hp46 = 14142, Hp47 = 15422;

        public static readonly Beast[] Creatures =
        {
            new(Kane, "Lieutenant Kane - Magma Caverns, Arkhara Research Station", SwapsetMale, TargetCategoryRow.Friendly, 46, Hp46, 440, 10472, 0, 0),
            new(Salyard, "Rebel Leader Salyard - Magma Caverns, Rebel camp", SwapsetMale, TargetCategoryRow.Neutral, 47, Hp47 * 5 / 2, 450, 9679, 9, 5, Pistol),
            new(Sudevi, "Lieutenant Sudevi - Magma Caverns, the camp's defenses", SwapsetFemale, TargetCategoryRow.Neutral, 46, Hp46 * 7 / 4, 440, 9678, 9, 5, Pistol),
            new(Nelby, "Lieutenant Nelby - Magma Caverns, the front lines", SwapsetMale, TargetCategoryRow.Neutral, 46, Hp46 * 7 / 4, 440, 9665, 9, 5, Rifle),
            new(Mezen, "Penumbra Assault Leader Mezen - Magma Caverns, landing zone 2", SwapsetMale, TargetCategoryRow.Neutral, 47, Hp47 * 5 / 2, 450, 9680, 9, 5, Pistol),
            new(Dama, "Lieutenant Dama - Magma Caverns, landing zone 1", SwapsetFemale, TargetCategoryRow.Neutral, 46, Hp46 * 7 / 4, 440, 9682, 9, 5, Rifle),
            new(Ember, "Ember - Magma Caverns, the graveyard cave", 24088, TargetCategoryRow.Hostile, 47, Hp47 * 5, 450, 10491, 7, 4, EmberSlam, FlaregasherFireball),

            new(Guerrilla, "Rebel Guerrilla - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 45, Hp45, 430, 9648, 9, 5, Rifle),
            new(Assassin, "Rebel Assassin - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 45, Hp45, 430, 9649, 9, 5, Pistol),
            new(Marine, "Rebel Marine - Magma Caverns", HeavySoldier, TargetCategoryRow.Neutral, 45, Hp45, 430, 9650, 9, 5, MachineGunFire),
            new(ConvertedThrax, "Converted Thrax - Magma Caverns", 9354, TargetCategoryRow.Neutral, 45, Hp45, 430, 10501, 9, 5, GrenadierGrenade, GrenadierRifle),
            new(Defender, "Rebel Defender - Magma Caverns", HeavySoldier, TargetCategoryRow.Neutral, 45, Hp45, 430, 10502, 9, 5, MachineGunFire),
            new(RebelMedic, "Rebel Field Medic - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 44, Hp44, 420, 10512, 9, 5, Pistol),
            new(PacifiedThrax, "Pacified Thrax - Magma Caverns, the research subjects", 20757, TargetCategoryRow.Neutral, 44, Hp44, 420, 9653, 9, 5, GrenadierRifle),

            new(BlackOps, "Penumbra Black Ops - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 45, Hp45, 430, 9645, 9, 5, Rifle),
            new(ShockTrooper, "Penumbra Shock Trooper - Magma Caverns", HeavySoldier, TargetCategoryRow.Neutral, 45, Hp45, 430, 9646, 9, 5, MachineGunFire),
            new(Spy, "Penumbra Spy - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 45, Hp45, 430, 9647, 9, 5, Pistol),
            new(SpecOps, "Penumbra Spec Ops - Magma Caverns", HeavySoldier, TargetCategoryRow.Neutral, 45, Hp45, 430, 10449, 9, 5, Rifle),
            new(PenumbraMedic, "Penumbra Field Medic - Magma Caverns", Infantryman, TargetCategoryRow.Neutral, 44, Hp44, 420, 10511, 9, 5, Pistol),

            new(BeamManta, "Beam Manta Soarer - Magma Caverns, Raintree Oasis", 6441, TargetCategoryRow.Hostile, 44, Hp44, 420, 8027, 9, 5, MantaBeam, MantaLightning),
            new(BarbTick, "Platinum Barb Tick - Magma Caverns, Raintree Oasis", 7236, TargetCategoryRow.Hostile, 44, Hp44 * 3 / 4, 420, 8147, 9, 5, TickBite),
            new(Magmonix, "Quicksilver Magmonix - Magma Caverns, Ember's den", 6338, TargetCategoryRow.Hostile, 45, Hp45, 430, 8791, 7, 4, MagmonixSlam)
        };

        /// <summary>Who speaks which package.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Kane, KanePackage, "Lieutenant Kane - Magma Caverns"),
            (Salyard, SalyardPackage, "Rebel Leader Salyard - Magma Caverns"),
            (Sudevi, SudeviPackage, "Lieutenant Sudevi - Magma Caverns"),
            (Nelby, NelbyPackage, "Lieutenant Nelby - Magma Caverns"),
            (Mezen, MezenPackage, "Penumbra Assault Leader Mezen - Magma Caverns"),
            (Dama, DamaPackage, "Lieutenant Dama - Magma Caverns")
        };

        // Looks (ESTIMATE): Kane in the AFS officers' brown, the Rebels' officers in khaki, Penumbra's in black.
        public const uint FemaleFace = 24009, FemaleHair = 3672;

        private static Slot[] Uniform(uint hue, uint hair, uint hairColor, uint face, uint skin, uint weapon) => new[]
        {
            new Slot(2, OfficerBoots, hue), new Slot(13, weapon, 1), new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, hue), new Slot(16, OfficerLegs, hue), new Slot(17, face, skin)
        };

        public static readonly (uint Creature, Slot[] Look)[] Looks =
        {
            (Kane, Uniform(Brown, 9782, DarkHair, 24032, SkinMedium, PistolRedshirt)),
            (Salyard, Uniform(Khaki, 3663, DarkHair, 24020, SkinMedium, PistolRedshirt)),
            (Sudevi, Uniform(Khaki, FemaleHair, DarkHair, FemaleFace, SkinLight, PistolRedshirt)),
            (Nelby, Uniform(Khaki, 9784, DarkHair, 24032, SkinLight, RifleAvatar)),
            (Mezen, Uniform(Black, WildernessPostNpcs.Hair09, DarkHair, UnplacedNamedNpcs.FaceCaucasianSquare2, SkinLight, PistolRedshirt)),
            (Dama, Uniform(Black, FemaleHair, DarkHair, FemaleFace, SkinMedium, RifleAvatar)),
            (Guerrilla, new[] { new Slot(13, RifleAvatar, 1) }),
            (Assassin, new[] { new Slot(13, PistolRedshirt, 1) }),
            (Marine, new[] { new Slot(13, MachineGun, 1) }),
            (Defender, new[] { new Slot(13, MachineGun, 1) }),
            (RebelMedic, new[] { new Slot(13, PistolRedshirt, 1) }),
            (BlackOps, new[] { new Slot(13, RifleAvatar, 1) }),
            (ShockTrooper, new[] { new Slot(13, MachineGun, 1) }),
            (Spy, new[] { new Slot(13, PistolRedshirt, 1) }),
            (SpecOps, new[] { new Slot(13, RifleLaser, 1) }),
            (PenumbraMedic, new[] { new Slot(13, PistolRedshirt, 1) })
        };

        public const uint KanePool = 678201, SalyardPool = 678202, SudeviPool = 678203, NelbyPool = 678204, MezenPool = 678205,
            DamaPool = 678206, SubjectsPool = 678207;
        public const uint NpcRespawn = 200, OfficerRespawn = 6000, SoldierRespawn = 3000, CreatureRespawn = 1200;

        public static readonly Spot[] Pools =
        {
            new(KanePool, KaneSpot.X, KaneSpot.Y, KaneSpot.Z, 3.1, NpcRespawn, 0, (Kane, 1, 1)),
            new(SalyardPool, SalyardSpot.X, SalyardSpot.Y, SalyardSpot.Z, -1.6, OfficerRespawn, 0, (Salyard, 1, 1)),
            new(SudeviPool, SudeviSpot.X, SudeviSpot.Y, SudeviSpot.Z, 3.1, OfficerRespawn, 0, (Sudevi, 1, 1)),
            new(NelbyPool, NelbySpot.X, NelbySpot.Y, NelbySpot.Z, 1.2, OfficerRespawn, 0, (Nelby, 1, 1)),
            new(MezenPool, MezenSpot.X, MezenSpot.Y, MezenSpot.Z, 0.6, OfficerRespawn, 0, (Mezen, 1, 1)),
            new(DamaPool, DamaSpot.X, DamaSpot.Y, DamaSpot.Z, 0.9, OfficerRespawn, 0, (Dama, 1, 1)),
            new(SubjectsPool, CellSpots[0].X, CellSpots[0].Y, CellSpots[0].Z, 3.1, SoldierRespawn, 2, (PacifiedThrax, 4, 4)),

            // The Rebel camp, the camp's defenses, the oasis's sandbags and the front lines.
            new(678210, -340.5, 133.95, 551.9, -1.6, SoldierRespawn, 3, (Guerrilla, 2, 2), (RebelMedic, 1, 1)),
            new(678211, -356, 129.45, 564.4, -0.4, SoldierRespawn, 3, (Marine, 1, 1), (ConvertedThrax, 1, 1)),
            new(678212, Defenses.X, Defenses.Y, Defenses.Z, 3.1, SoldierRespawn, 3, (Defender, 2, 2), (Marine, 1, 1)),
            new(678213, -350, 136.92, 448, 3.1, SoldierRespawn, 3, (Assassin, 1, 1), (ConvertedThrax, 1, 1)),
            new(678214, -430, 144.18, 230, 2.8, SoldierRespawn, 3, (Guerrilla, 2, 2)),
            new(678215, -420, 132.22, 177.6, 1.6, SoldierRespawn, 3, (Marine, 1, 1), (Assassin, 1, 1)),
            new(678216, -195, 133.07, 178, 1.2, SoldierRespawn, 3, (Defender, 2, 2), (RebelMedic, 1, 1)),
            new(678217, -210, 132.97, 162, 1.2, SoldierRespawn, 3, (Marine, 1, 1), (ConvertedThrax, 1, 1)),

            // Penumbra's landing zones.
            new(678220, -410, 169.82, -110, 0.6, SoldierRespawn, 3, (BlackOps, 2, 2), (PenumbraMedic, 1, 1)),
            new(678221, -405, 169.51, -80, 2.6, SoldierRespawn, 3, (ShockTrooper, 1, 1), (Spy, 1, 1)),
            new(678222, -516.2, 169.22, -12.5, 0.9, SoldierRespawn, 3, (ShockTrooper, 2, 2)),
            new(678223, -498, 169.39, -18, 0.9, SoldierRespawn, 3, (SpecOps, 1, 1), (BlackOps, 1, 1)),

            // Ember's den: aggressive Magmonix in the cave's mouth, and Flaregashers living beside them.
            new(678230, -168, 144.9, 298, 0.4, CreatureRespawn, 5, (Magmonix, 2, 2)),
            new(678231, Lair.X, Lair.Y, Lair.Z, 0, CreatureRespawn, 6, (Flaregasher, 2, 2))
        };

        /// <summary>
        /// Crucible's Flaregasher pools, out of Penumbra's landing zones and the Rebels' front (and
        /// one off the navmesh by the east door): along the long tunnel between them and up to
        /// Ember's den. Their spots, radius 15, and ours, radius 6.
        /// </summary>
        public static readonly (uint Pool, Vector3 From, Vector3 To)[] FlaregasherMoves =
        {
            (540128, new(-472f, 171.1f, -6f), new(-300f, 154.5f, 15f)),
            (540129, new(-458.8f, 171.1f, -94.7f), new(-223f, 155.9f, 5f)),
            (540130, new(-437.2f, 171.1f, 76.6f), new(-156f, 152.5f, 7f)),
            (540131, new(-382.4f, 171.1f, -9.8f), new(-175f, 143.9f, 284f)),
            (540132, new(-172f, 134.3f, 183f), new(-158f, 144.9f, 309f)),
            (540133, new(-239f, 134.3f, 206f), new(-188f, 135.3f, 237f)),
            (540134, new(-122.9f, 134.3f, 131.8f), new(-110f, 147.9f, 26f))
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {SwapsetMale} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

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

            foreach (var (creature, look) in Looks)
                foreach (var sql in Change(creature, new Slot[0], look))
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
                    $"SELECT {p.Id}, 0, 0, {p.RespawnTenths}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {MagmaCaverns}, " +
                    string.Join(", ", slots.Select(s => $"{s.Creature}, {s.Min}, {s.Max}")) + $", {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Slots[0].Creature}{there} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";
            }

            foreach (var (pool, from, to) in FlaregasherMoves)
                yield return $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, radius = 6 " +
                    $"WHERE id = {pool} AND creature_1_Id = {Flaregasher} AND map_context_id = {MagmaCaverns} AND radius = 15;";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var (pool, from, _) in FlaregasherMoves)
                yield return $"UPDATE spawnpool SET pos_x = {N(from.X)}, pos_y = {N(from.Y)}, pos_z = {N(from.Z)}, radius = 15 " +
                    $"WHERE id = {pool} AND creature_1_Id = {Flaregasher} AND map_context_id = {MagmaCaverns} AND radius = 6;";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Slots[0].Creature};";

            foreach (var (creature, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {creature} AND package_id = {package};";

            yield return $"DELETE FROM creature_appearance WHERE id IN ({string.Join(", ", Looks.Select(look => look.Creature))});";

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

        private const string Diary = "https://www.tentonhammer.com/tabula-rasa/deployment-8-dev-diary";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            ConsensusRows(sql);
            FinalChanceRows(sql);
            ExecutionerRows(sql);
            CreatureStrikeRows(sql);
            LavaBeastRows(sql);
            return sql;
        }

        // Rewards (ESTIMATE): the arcs about a level-45 battlefield mission's run of four; each
        // arc's three choices the nearest level-45 uncommon of their kind. The bonus objectives'
        // credits and experience are the client's (PM_MAGMA_*_BONUS_*), MagmaCaverns's to grant.
        public const uint LaserMachineGun = 12023, MotorAssistVest = 30034, GravitonHelmet = 23849;
        public const uint FireGrenadeLauncher = 11997, PhysicalLeechGun = 12053, MotorAssistHelmet = 28960;
        public const uint ArcExperience = 160000, ArcCredits = 16000;
        public const int BonusCredits = 50000;
        public const uint BonusExperience = 200000;

        private static InstanceMissionSql.RewardItem Item(uint template) => new(template, 1);

        /// <summary>Kane's choice, the squad's vote, and the way to the side chosen.</summary>
        private static void ConsensusRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Consensus, "Consensus", 20337, Kane, null, 44, Category);
            mission.Reward(0, 0);
            mission.Objective(SpeakToKane, 20342, 20343, 1, MissionObjectiveState.Incomplete);
            mission.Objective(WaitForSquad, 20348, 20349, 2);
            mission.Objective(Hidden, 20730, 20731, 3);
            mission.Objective(TakeTeleporter, 20738, 20739, 4, required: false);
            mission.Objective(TakeBackDoor, 20740, 20741, 5, required: false);

            // Kane's two answers: the vote.
            foreach (var (transition, side) in new[] { (1U, PenumbraSide), (2U, RebelSide) })
            {
                mission.Transition(SpeakToKane, transition, sequence: transition);
                mission.Conversation(SpeakToKane, KanePackage, 1, transition);
                mission.SetFlag(SpeakToKane, 1, VoteFlag, side, transition);
                mission.RevealAndActivate(SpeakToKane, WaitForSquad, 2, transition);
            }

            // The copy's decision: the flag, and its way out of the station.
            foreach (var (transition, side, sided, way) in new[]
                     {
                         (1U, PenumbraSide, SidedPenumbra, TakeBackDoor), (2U, RebelSide, SidedRebel, TakeTeleporter)
                     })
            {
                mission.Transition(WaitForSquad, transition, sequence: transition);
                mission.Progress(WaitForSquad, MissionProgressEventKind.ScenarioEvent, sided, counter: Scenario, transition: transition);
                mission.SetFlag(WaitForSquad, 1, SideFlag, side, transition);
                mission.RevealAndActivate(WaitForSquad, way, 2, transition);
                mission.RevealAndActivate(WaitForSquad, Hidden, 4, transition);
            }

            mission.Transition(Hidden);
            mission.Progress(Hidden, MissionProgressEventKind.ScenarioEvent, Crossed, counter: Scenario);
            mission.Action(Hidden, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(TakeTeleporter);
            mission.Progress(TakeTeleporter, MissionProgressEventKind.ScenarioEvent, UsedTeleporter, counter: Scenario);
            mission.Transition(TakeBackDoor);
            mission.Progress(TakeBackDoor, MissionProgressEventKind.ScenarioEvent, UsedBackDoor, counter: Scenario);

            mission.Scenario(Scenario, "Consensus", SidedPenumbra, SidedRebel, Crossed, UsedTeleporter, UsedBackDoor);
            mission.Indicator(SpeakToKane, MissionIndicator.UnnamedFrom + SpeakToKane, KaneSpot.X, KaneSpot.Y, KaneSpot.Z);
            mission.Indicator(TakeTeleporter, MissionIndicator.UnnamedFrom + TakeTeleporter, StationTeleporter.X, StationTeleporter.Y, StationTeleporter.Z);
            mission.Indicator(TakeBackDoor, MissionIndicator.UnnamedFrom + TakeBackDoor, BackDoor.X, BackDoor.Y, BackDoor.Z);
            mission.RadioTurnIn();
            mission.RepeatImmediately();
            Evidence(mission, "Lieutenant Kane678001 in Arkhara Research Station gives it (package2458: body20344, choice1 'I've got " +
                "my orders' is Penumbra, choice2 'You're right' the Rebels). Each answer is the member's vote, flag1942001. " +
                "MagmaCaverns counts the votes of the members in the copy once every one on Consensus has voted: the majority, a " +
                "tie the first to vote; a copy already decided decides for whoever votes later. The decision is event1 " +
                "(Penumbra) or 2 (Rebels), which sets flag1942002 and opens the back door's or the teleporter's objective; " +
                "through it is event3 for the hidden objective10 and event4 or 5. Objective9 (the squad accepting) is not made. " +
                "Repeatable, no reward (an empty one, which a turn-in wants), radioed in (the mission notes).");
            var scene = Scene(events: true);
            scene.HiddenObjectiveIds = new List<uint> { Hidden };
            scene.Dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(SpeakToKane, KanePackage, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 })
            };
            mission.Enable(scene);
        }

        /// <summary>The Rebels' arc.</summary>
        private static void FinalChanceRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, FinalChance, "Final Chance", 20369, null, null, 45, Category,
                radioCompleteable: true);
            mission.Prerequisite(Consensus);
            mission.Reward(ArcExperience, ArcCredits, selectableItems: new[] { Item(LaserMachineGun), Item(MotorAssistVest), Item(GravitonHelmet) });

            uint ordinal = 1;
            void Objective(uint id, uint name, uint body, bool start = false, bool required = true) =>
                mission.Objective(id, name, body, ordinal++, start ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive, required);

            Objective(FcSalyard, 20567, 20568, start: true);
            Objective(FcSudevi, 20543, 20544);
            Objective(FcShutWormhole1, 20574, 20575);
            Objective(FcHoldCamp, 20546, 20547);
            Objective(FcBonus1, 20662, 20663, required: false);
            Objective(FcNelby, 20553, 20554);
            Objective(FcShutWormhole2, 20576, 20577);
            Objective(FcHoldDoor, 20556, 20557);
            Objective(FcBonus2, 20664, 20665, required: false);
            Objective(FcRebelWormhole, 20589, 20590, required: false);
            Objective(FcInfiltrate, 20578, 20579);
            Objective(FcBeacon1, 20580, 20581);
            Objective(FcBeacon2, 20582, 20583);
            Objective(FcDama, 20591, 20592);
            Objective(FcMezen, 20584, 20585);

            mission.Transition(FcSalyard);
            mission.Conversation(FcSalyard, SalyardPackage);
            mission.RevealAndActivate(FcSalyard, FcSudevi);

            mission.Transition(FcSudevi);
            mission.Conversation(FcSudevi, SudeviPackage);
            Open(mission, FcSudevi, FcShutWormhole1, FcHoldCamp, FcBonus1);

            Event(mission, FcShutWormhole1, FcWormhole1Shut);
            Event(mission, FcHoldCamp, FcAssault1Over);
            mission.RevealAndActivate(FcHoldCamp, FcNelby);
            Bonus(mission, FcBonus1, FcBonus1Won, FcBonus1Lost);

            mission.Transition(FcNelby);
            mission.Conversation(FcNelby, NelbyPackage);
            Open(mission, FcNelby, FcShutWormhole2, FcHoldDoor, FcBonus2);

            Event(mission, FcShutWormhole2, FcWormhole2Shut);
            Event(mission, FcHoldDoor, FcAssault2Over);
            Open(mission, FcHoldDoor, FcRebelWormhole, FcInfiltrate);
            Bonus(mission, FcBonus2, FcBonus2Won, FcBonus2Lost);

            Event(mission, FcRebelWormhole, FcRebelWormholeSet);
            Event(mission, FcInfiltrate, FcInfiltrated);
            mission.RevealAndActivate(FcInfiltrate, FcBeacon1);
            Event(mission, FcBeacon1, FcBeacon1Placed);
            mission.RevealAndActivate(FcBeacon1, FcBeacon2);
            Event(mission, FcBeacon2, FcBeacon2Placed);
            Open(mission, FcBeacon2, FcDama, FcMezen);

            mission.Transition(FcDama);
            mission.Progress(FcDama, MissionProgressEventKind.CreatureKilled, Dama);
            mission.Transition(FcMezen);
            mission.Progress(FcMezen, MissionProgressEventKind.CreatureKilled, Mezen);
            mission.Action(FcMezen, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Scenario(Scenario, "Final Chance", FcWormhole1Shut, FcAssault1Over, FcBonus1Won, FcBonus1Lost, FcWormhole2Shut,
                FcAssault2Over, FcBonus2Won, FcBonus2Lost, FcRebelWormholeSet, FcInfiltrated, FcBeacon1Placed, FcBeacon2Placed);

            Marker(mission, FcSalyard, SalyardSpot);
            Marker(mission, FcSudevi, SudeviSpot);
            Marker(mission, FcShutWormhole1, OasisNorth);
            Marker(mission, FcHoldCamp, Sakra, 30);
            Marker(mission, FcNelby, NelbySpot);
            Marker(mission, FcShutWormhole2, GroundsWormhole);
            Marker(mission, FcHoldDoor, WestDoorWay, 20);
            Marker(mission, FcRebelWormhole, GroundsSetup);
            Marker(mission, FcInfiltrate, LandingZone2, 40);
            Marker(mission, FcBeacon1, LandingZone1);
            Marker(mission, FcBeacon2, LandingZone2);
            Marker(mission, FcDama, DamaSpot, 15);
            Marker(mission, FcMezen, MezenSpot, 15);

            mission.Channels(MissionChannel.Radio, MissionChannel.Radio, MissionOfferSourceDefinition.MissionRewarded(Consensus),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, MagmaCaverns));
            Evidence(mission, "Offered by radio once Consensus1942 is turned in with the Rebels chosen (flag1942002 = 2), and on " +
                "coming into the caverns; radioed in (its texts are the briefing's). Salyard678002 (package1760) and Sudevi678003 " +
                "(1759) at the camp, Nelby678004 (1751) at the front. Penumbra's first assault comes through the wormhole at the " +
                "oasis's north end by the Sakra Oasis, the camp's defenses and the tunnel to the camp; the second through the " +
                "wormhole by the east door in the Grounds to the west door. Each wormhole shut is its objective's event, each " +
                "assault broken (shut and none left) its own; a bonus is won with the assault broken before any of it reaches the " +
                "camp or the door, lost when one does, +50,000 credits and +200,000 experience (the client's messages). The " +
                "optional wormhole stands in the Grounds and puts the squad by the Rebels' way into the Cauldron. The landing " +
                "zones reached, the GPS beacons at each, then Dama678006 and Mezen678005 dead. Reward ESTIMATE.");
            var scene = Scene(events: true);
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, new FlagRequirement(SideFlag, RebelSide) });
            mission.Enable(scene);
        }

        /// <summary>Penumbra's arc.</summary>
        private static void ExecutionerRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Executioner, "Executioner", 20364, null, null, 45, Category,
                radioCompleteable: true);
            mission.Prerequisite(Consensus);
            mission.Reward(ArcExperience, ArcCredits, selectableItems: new[] { Item(FireGrenadeLauncher), Item(PhysicalLeechGun), Item(MotorAssistHelmet) });

            uint ordinal = 1;
            void Objective(uint id, uint name, uint body, bool start = false, bool required = true) =>
                mission.Objective(id, name, body, ordinal++, start ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive, required);

            Objective(ExMezen, 20594, 20595, start: true);
            Objective(ExDama, 20597, 20598);
            Objective(ExStopAssault, 20600, 20601);
            Objective(ExBonus1, 20666, 20667, required: false);
            Objective(ExBonus2, 20668, 20669, required: false);
            Objective(ExShutWormhole, 20603, 20604);
            Objective(ExWormhole1, 20605, 20606, required: false);
            Objective(ExFrontLine, 20607, 20608);
            Objective(ExDoor1, 20732, 20733);
            Objective(ExNelby, 20609, 20610);
            Objective(ExWormhole2, 20612, 20613, required: false);
            Objective(ExDoor2, 20736, 20737);
            Objective(ExStronghold, 20734, 20735);
            Objective(ExSudevi, 20614, 20615);
            Objective(ExCamp, 20617, 20618);
            Objective(ExSalyard, 20619, 20620);
            Objective(ExLaptop, 20622, 20623);
            Objective(ExRelease, 20624, 20625);
            Objective(ExSubjects, 20626, 20627);

            mission.Transition(ExMezen);
            mission.Conversation(ExMezen, MezenPackage);
            mission.RevealAndActivate(ExMezen, ExDama);

            mission.Transition(ExDama);
            mission.Conversation(ExDama, DamaPackage);
            Open(mission, ExDama, ExStopAssault, ExBonus1, ExBonus2, ExShutWormhole, ExWormhole1);

            Event(mission, ExStopAssault, ExAssaultOver);
            mission.RevealAndActivate(ExStopAssault, ExFrontLine);
            Bonus(mission, ExBonus1, ExBonus1Won, ExBonus1Lost);
            Bonus(mission, ExBonus2, ExBonus2Won, ExBonus2Lost);
            Event(mission, ExShutWormhole, ExWormholeShut);
            Event(mission, ExWormhole1, ExWormhole1Set);

            Event(mission, ExFrontLine, ExFrontFound);
            mission.RevealAndActivate(ExFrontLine, ExDoor1);
            Event(mission, ExDoor1, ExDoor1Blown);
            Open(mission, ExDoor1, ExNelby, ExWormhole2);

            mission.Transition(ExNelby);
            mission.Progress(ExNelby, MissionProgressEventKind.CreatureKilled, Nelby);
            mission.RevealAndActivate(ExNelby, ExDoor2);
            Event(mission, ExWormhole2, ExWormhole2Set);

            Event(mission, ExDoor2, ExDoor2Blown);
            mission.RevealAndActivate(ExDoor2, ExStronghold);
            Event(mission, ExStronghold, ExStrongholdFound);
            mission.RevealAndActivate(ExStronghold, ExSudevi);

            mission.Transition(ExSudevi);
            mission.Progress(ExSudevi, MissionProgressEventKind.CreatureKilled, Sudevi);
            mission.RevealAndActivate(ExSudevi, ExCamp);
            Event(mission, ExCamp, ExCampEntered);
            mission.RevealAndActivate(ExCamp, ExSalyard);

            mission.Transition(ExSalyard);
            mission.Progress(ExSalyard, MissionProgressEventKind.CreatureKilled, Salyard);
            mission.RevealAndActivate(ExSalyard, ExLaptop);
            Event(mission, ExLaptop, ExLaptopRead);
            mission.RevealAndActivate(ExLaptop, ExRelease);
            Event(mission, ExRelease, ExSubjectsReleased);
            mission.RevealAndActivate(ExRelease, ExSubjects);

            Event(mission, ExSubjects, ExSubjectsDead);
            mission.Action(ExSubjects, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Scenario(Scenario, "Executioner", ExAssaultOver, ExBonus1Won, ExBonus1Lost, ExBonus2Won, ExBonus2Lost,
                ExWormholeShut, ExWormhole1Set, ExFrontFound, ExDoor1Blown, ExWormhole2Set, ExDoor2Blown, ExStrongholdFound,
                ExCampEntered, ExLaptopRead, ExSubjectsReleased, ExSubjectsDead);

            Marker(mission, ExMezen, MezenSpot);
            Marker(mission, ExDama, DamaSpot);
            Marker(mission, ExStopAssault, LandingZone1, 30);
            Marker(mission, ExShutWormhole, RebelWormhole);
            Marker(mission, ExWormhole1, CauldronSetup);
            Marker(mission, ExFrontLine, EastCorridor, 20);
            Marker(mission, ExDoor1, EastDoorWay);
            Marker(mission, ExNelby, NelbySpot, 15);
            Marker(mission, ExWormhole2, GroundsSetup);
            Marker(mission, ExDoor2, WestDoorWay);
            Marker(mission, ExStronghold, SudeviSpot, 30);
            Marker(mission, ExSudevi, SudeviSpot, 15);
            Marker(mission, ExCamp, Camp, 25);
            Marker(mission, ExSalyard, SalyardSpot, 15);
            Marker(mission, ExLaptop, Laptop);
            Marker(mission, ExRelease, Cells);
            Marker(mission, ExSubjects, Cells, 10);

            mission.Channels(MissionChannel.Radio, MissionChannel.Radio, MissionOfferSourceDefinition.MissionRewarded(Consensus),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, MagmaCaverns));
            Evidence(mission, "Offered by radio once Consensus1942 is turned in with Penumbra chosen (flag1942002 = 1), and on " +
                "coming into the caverns; radioed in. Mezen678005 (package1763) by the second landing zone through the back " +
                "door, Dama678006 (1761) by the first. The Rebel assault comes through the wormhole on the Rebels' way into the " +
                "Cauldron to the first landing zone and on to the second; a bonus is won with it broken before it reaches each " +
                "(its GPS beacon placed), +50,000 credits and +200,000 experience. The optional wormholes stand by the first " +
                "landing zone (to the east corridor) and in the Grounds (to the oasis's north end). The front found at the east " +
                "corridor; each blocked door blown by one on its objective; Nelby678004, Sudevi678003 and Salyard678002 dead; the " +
                "stronghold the camp's defenses, the camp its middle; Salyard's laptop; the prison block's forcefield opened, and " +
                "the Pacified Thrax678016 it held, every one loosed, dead (event16). Reward ESTIMATE.");
            var scene = Scene(events: true);
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, new FlagRequirement(SideFlag, PenumbraSide) });
            mission.Enable(scene);
        }

        /// <summary>Sudevi's, for the Rebels' side: the Mantas and Ticks from the oasis.</summary>
        private static void CreatureStrikeRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CreatureStrike, "Creature Strike", 12979, Sudevi, Sudevi, 44, Category);
            mission.Reward(60000, 6000);
            mission.Objective(StrikeDefeat, 20572, 20573, 1, MissionObjectiveState.Incomplete);
            Event(mission, StrikeDefeat, StrikeOver);
            mission.Action(StrikeDefeat, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Creature Strike", StrikeOver);
            Marker(mission, StrikeDefeat, Defenses, 25);
            Evidence(mission, "Lieutenant Sudevi678003 at the camp's defenses gives and takes it, to one on the Rebels' side " +
                "(flag1942002 = 2). MagmaCaverns sends Beam Mantas678030 and Barb Ticks678031 from the Raintree (Sakra) Oasis at " +
                "the defenses and their turret (pool550010) once one in the copy is on it; all dead is event1. Reward ESTIMATE.");
            var scene = Scene(events: true);
            scene.Requirement = new AllRequirements(new MissionRequirement[] { scene.Requirement, new FlagRequirement(SideFlag, RebelSide) });
            mission.Enable(scene);
        }

        /// <summary>Kane's, whichever side: Ember's lair, and Ember.</summary>
        private static void LavaBeastRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LavaBeast, "Lava Beast", 13111, Kane, null, 46, Category);
            mission.Reward(80000, 8000);
            mission.Objective(FindLair, 20570, 20571, 1, MissionObjectiveState.Incomplete);
            mission.Objective(KillEmber, 13116, 13117, 2);
            Event(mission, FindLair, LairFound);
            mission.RevealAndActivate(FindLair, KillEmber);
            mission.Transition(KillEmber);
            mission.Progress(KillEmber, MissionProgressEventKind.CreatureKilled, Ember);
            mission.Action(KillEmber, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Lava Beast", LairFound);
            Marker(mission, FindLair, Lair, 30);
            Marker(mission, KillEmber, Lair, 15);
            mission.RadioTurnIn();
            Evidence(mission, "Lieutenant Kane678001 gives it, whichever side the copy took ('regardless of the side'); radioed in, " +
                "his finishing text13114 read over it. The graveyard cave off the Magmonix Grounds within 30 m is event1; Ember678007 " +
                "dead the second. His death sets the den's bones alight and shows his hoard (PM_MAGMA_EMBER_TREASURE_BOX_REVEALED), " +
                "MagmaCaverns's. Reward ESTIMATE.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>The objective's transition reveals and activates each of the others.</summary>
        private static void Open(InstanceMissionSql mission, uint objective, params uint[] next)
        {
            uint action = 1;
            foreach (var id in next)
            {
                mission.RevealAndActivate(objective, id, action);
                action += 2;
            }
        }

        /// <summary>The objective done by its scenario event.</summary>
        private static void Event(InstanceMissionSql mission, uint objective, uint eventId)
        {
            mission.Transition(objective);
            mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: Scenario);
        }

        /// <summary>A bonus: won by one event, failed by the other.</summary>
        private static void Bonus(InstanceMissionSql mission, uint objective, uint won, uint lost)
        {
            Event(mission, objective, won);
            mission.Transition(objective, 2, MissionObjectiveState.Failed, 2);
            mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, lost, counter: Scenario, transition: 2);
        }

        private static void Marker(InstanceMissionSql mission, uint objective, Vector3 at, double radius = 8) =>
            mission.Indicator(objective, MissionIndicator.UnnamedFrom + objective, at.X, at.Y, at.Z, radius);

        /// <summary>A mission's binding: for the bootcamp's graduates; one whose events MagmaCaverns records has a scenario.</summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Diary,
                "The Deployment 8 dev diary (Jason Ford, Michael Hutchison): the choice, the vote, both arcs, the assaults, Ember.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "MagmaCavernsContent", reconstruction);
        }

        #endregion
    }
}
