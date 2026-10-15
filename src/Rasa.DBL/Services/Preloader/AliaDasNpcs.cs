using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Alia Das (foreas_concordia_wilderness, 1220) and three other Wilderness posts as a GM placed
    /// them from gameplay video, each read off the server's .where lines (Alia_das_npcs).
    ///
    /// Who is where:
    ///  - The command tent (ArchHumTentCommunicationV01 at 857.11, 388.05): Outpost Commander Rogers
    ///    at its middle, two Infantrymen at ease either side of its opening, Captain Spaulding,
    ///    Brigadier General Beacham and Receptive Liaison Langerman, Clan Registrar Kolchak by it,
    ///    and two of the client's ambient figures in grey at its terminal and computer.
    ///  - Major Bonham up by the landing pad; Dropship Mechanic Shramek walking between the
    ///    latrine tent and the pad, along the navmesh's path there and back.
    ///  - The barracks, armoury and supply tents: Training Officer Kincaid where the client's map
    ///    pins "Class Trainer: Alia Das" (766, 386.1), Supply Sergeants Caruso (the ammunition
    ///    vendor) and Maddrey (the weapons vendor, where the client pins "Weapons Vendor: Alia
    ///    Das"), Quartermaster Caufield, Engineer Salter holding his rifle ready.
    ///  - By the hospital and the crafting tent: Supply Sergeants Harrington and North, selling
    ///    the crafting components the Kraftwerks takes.
    ///  - Forean trainees: a Gunner Trainee walking round the flagpole's ring of sandbags
    ///    (786, 391); three Gunner Trainees behind a Shaman Trainee walking up and down the north
    ///    road where it meets the outpost (the road texture's centre line, x 838.5 to 840.5).
    ///  - The Forean council: Elders Moawi and Solis and Warrior Apirka moved, Apirka a spearman
    ///    now (he was a gunner with a goo gun), at ease with his spear.
    ///  - Arms Supplier Oliver's post (533.9, 517.2): an Infantryman with a pistol, two Field
    ///    Gunners with machine guns (red-glowing: physical; the laser machine gun glows purple)
    ///    and a Rifleman with the purple-glowing laser rifle.
    ///  - Pinhole Falls: Mining Coordinator Richards and Mining Technician Maxwell moved to the
    ///    caverns' mouth (341.2, 477.8).
    ///
    /// Taken away: the six class trainers Add_class_trainers stood at Alia Das (501001-501006;
    /// Kincaid, a trainer of every class, stands where they were pinned) and the unnamed "Human"
    /// weapons vendor (Test Vendor 5, pool 36), whose stock is Maddrey's now. Turned: the AFS light
    /// turret by the south gate 180 degrees, and the crafting station a quarter turn clockwise,
    /// which stands its 4 m length along the tent instead of through its south wall: the station's
    /// back (prop_hum_craftstation_v01.geo local x 0.49) then meets the wall's inner face
    /// (z 359.2, the navmesh's inside edge 359.8 less its 0.6 m erosion) where it stands.
    ///
    /// Looks: NPC_Clothing_Officer_1's tunic has its shoulder pads coloured in the texture
    /// (npc_hum_m/f_officer_v01_df: maroon), so the "pinkish shoulders" are that uniform, and the
    /// colour asked for is its hue. Brown is the Proving Grounds quest NPCs' #68562c. "Green armour
    /// with brown trousers" is the Soldier_1 armour vest and gloves over the officer's trousers;
    /// Kincaid's black and silver is Officer_4 (npc_hum_mf_officer_v04_df: black, silver trim);
    /// Langerman's white uniform and beret Officer_2's vest and cap; Salter's strapped uniform and
    /// silver helmet Officer_3; Lieutenant Colonel Cimoch's blue armour NPC_Clothing_Medic_1, the
    /// light armour (avatar_hum_mf_armor_light_v07_v08) nearest the Infantryman's blue and white,
    /// with his rifle slung. He stays on his swapset body: the Infantryman's own model
    /// (Redshirt_Human_Soldier_Light_Male) has no NPC augmentation, and a mission giver needs one.
    /// None of these were in the client: each is the nearest thing in its NPC clothing, and
    /// .creatureappearance changes it.
    ///
    /// The missions follow: the map indicators of the objectives that send the player to Rogers,
    /// Beacham, Salter, Moawi, Solis and Apirka mark where they stand now, and Too Close For
    /// Comfort's escort ends at Solis's new spot.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Every change is guarded on the row as it was
    /// (a pool moves only from where it stood, a look slot changes only while it holds what it
    /// held), so what a GM changed by hand since is left alone; Down undoes only what is still as
    /// Up left it.
    /// </summary>
    public static class AliaDasNpcs
    {
        public const uint Map = 1220;

        /// <summary>A colour as the game server's Color.Hue stores it: alpha, blue, green, red from the high byte down.</summary>
        public static uint Hue(uint red, uint green, uint blue) => 0xFF000000u | (blue << 16) | (green << 8) | red;

        public static readonly uint Brown = Hue(0x68, 0x56, 0x2C);        // the Proving Grounds quest NPCs' brown
        public static readonly uint LightBrown = Hue(0x9A, 0x7F, 0x4E);
        public static readonly uint Yellow = Hue(0xB8, 0xA2, 0x50);
        public static readonly uint Green = Hue(0x4E, 0x6B, 0x34);
        public static readonly uint TrouserBrown = Hue(0x5E, 0x4A, 0x2E);
        public static readonly uint Black = Hue(0x2A, 0x2A, 0x2E);
        public static readonly uint Silver = Hue(0xC8, 0xCC, 0xD0);
        public static readonly uint White = Hue(0xED, 0xED, 0xED);
        public static readonly uint Khaki = Hue(0x7A, 0x73, 0x5F);
        public const uint OfficerBlue = 4294934528;                          // what the seed's officers wear: (0, 128, 255)
        public const uint DarkHair = 4278655809;
        public const uint SkinLight = 4286690539;
        public const uint SkinMedium = 4286886614;

        // Entity classes.
        public const uint SwapsetMale = 3846, SwapsetFemale = 3848, VendorMale = 20975, VendorFemale = 20972;
        public const uint Infantryman = 29423, HeavySoldier = 29433, ForeanSpearman = 7034, ForeanShaman = 7035, ForeanGunner = 6239;
        public const uint OfficerBoots = 4021, OfficerLegs = 4022, OfficerTunic = 4023;
        public const uint SoldierVest = 3809, SoldierBoots = 3810, SoldierGloves = 3986;
        public const uint MedicBoots = 4025, MedicGloves = 4026, MedicTorso = 4027, MedicLegs = 4028;
        public const uint SapperTorso = 3942, SapperLegs = 3943, SapperBoots = 3941, SapperGloves = 3985;
        public const uint Officer2Vest = 29794, Officer2Cap = 29795;
        public const uint Officer3Boots = 26672, Officer3Gloves = 26673, Officer3Legs = 26674, Officer3Vest = 26675, Officer3Helmet = 26677;
        public const uint Officer4Vest = 30040, Officer4Gloves = 30041;
        public const uint PistolRedshirt = 6271, PistolAvatar = 27120, RifleAvatar = 27220, RifleElectric = 20550, RifleLaser = 10537;
        public const uint MachineGun = 20535, ForeanSpear = 10532, ForeanStaff = 10533, ForeanGooGun = 6238, ForeanNpcGooGun = 10530;

        public sealed record Slot(uint Id, uint ClassId, uint Color);

        /// <summary>A creature's look: its creature_appearance rows before and after.</summary>
        public sealed record Look(uint Creature, string Who, Slot[] Was, Slot[] Is);

        /// <summary>A spawn pool moved and turned. From is the pool as the database holds it.</summary>
        public sealed record Move(uint Pool, uint Creature, string Who, double FromX, double FromY, double FromZ, double FromRotation,
            double ToX, double ToY, double ToZ, double ToRotation);

        public sealed record NewCreature(uint Id, string Comment, uint ClassId, uint Level, uint MaxHp, uint NameId, uint RunSpeed, uint WalkSpeed, uint Action);

        public sealed record NewPool(uint Id, uint Creature, double X, double Y, double Z, double Rotation, uint? Pose = null);

        /// <summary>A pool taken away, as the database held it, for Down to put back.</summary>
        public sealed record OldPool(uint Id, uint Creature, uint RespawnTime, double X, double Y, double Z, double Rotation);

        /// <summary>A creature's own columns changed: name, entity class, speeds, first action and comment.</summary>
        public sealed record Recast(uint Creature, string WasComment, uint WasClass, uint WasName, uint WasRun, uint WasWalk, uint WasAction,
            string IsComment, uint IsClass, uint IsName, uint IsRun, uint IsWalk, uint IsAction);

        public sealed record Step(double X, double Y, double Z, double? Facing, uint PauseMs);

        public sealed record Ambient(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, string Comment);

        public sealed record Greeting(uint Creature, uint? Was, uint Is);

        public sealed record VendorStock(uint Creature, uint Package, uint[] Templates, uint CopyFrom = 0);

        /// <summary>A mission objective's map indicator that marked an NPC where it stood, moved with it.</summary>
        public sealed record Indicator(uint Mission, uint Objective, uint IndicatorId, string Who,
            double FromX, double FromY, double FromZ, double ToX, double ToY, double ToZ);

        // ---- ids ----
        public const uint Shramek = 640001;
        public const uint GuardInfantryman = 640002;
        public const uint Spaulding = 640003;
        public const uint GunnerTrainee = 640004;
        public const uint ShamanTrainee = 640005;
        public const uint Maddrey = 640006;
        public const uint Harrington = 640007;
        public const uint North = 640008;
        public const uint PistolInfantryman = 640009;
        public const uint FieldGunner = 640010;
        public const uint Rifleman = 640011;
        public const uint FirstCreature = 640001, LastCreature = 640011;

        public const uint ShramekPool = 640001;
        public const uint GuardNorthPool = 640002, GuardSouthPool = 640003;
        public const uint SpauldingPool = 640004;
        public const uint CirclingGunnerPool = 640005;
        public const uint RoadShamanPool = 640006, RoadGunnerWestPool = 640007, RoadGunnerMiddlePool = 640008, RoadGunnerEastPool = 640009;
        public const uint MaddreyPool = 640010, HarringtonPool = 640011, NorthPool = 640012;
        public const uint OliverPistolPool = 640013, OliverGunnerEastPool = 640014, OliverGunnerWestPool = 640015, OliverRiflemanPool = 640016;
        public const uint FirstPool = 640001, LastPool = 640016;

        public const uint Rogers = 100, Bonham = 122, Beacham = 510002, Langerman = 133, Kolchak = 30, Cimoch = 118, Kincaid = 510006;
        public const uint Caruso = 73, Caufield = 132, Salter = 115, Oliver = 103, Richards = 126, Maxwell = 127;
        public const uint Moawi = 38, Solis = 42, Apirka = 43;
        public const uint HumanVendor = 25, HumanVendorPool = 36;
        public const uint SouthGateTurretPool = 550082, CraftingStation = 4;

        /// <summary>The flagpole's ring of sandbags the Gunner Trainee walks round.</summary>
        public const double FlagX = 786.0, FlagZ = 391.0, CircleRadius = 4.5;

        public static readonly NewCreature[] Creatures =
        {
            new(Shramek, "Dropship Mechanic Shramek - Alia Das", SwapsetMale, 10, 1000, 4667, 0, 2, 0),
            new(GuardInfantryman, "Infantryman at ease, rifle - Alia Das", Infantryman, 10, 1000, 8716, 0, 0, 72003),
            new(Spaulding, "Captain Spaulding - Alia Das", SwapsetMale, 10, 1000, 2971, 0, 0, 0),
            new(GunnerTrainee, "Forean Gunner Trainee - Alia Das", ForeanGunner, 5, 350, 7940, 0, 1, 0),
            new(ShamanTrainee, "Forean Shaman Trainee - Alia Das", ForeanShaman, 5, 350, 7892, 0, 1, 0),
            new(Maddrey, "Supply Sergeant Maddrey - Alia Das", VendorMale, 6, 555, 8508, 0, 0, 2),
            new(Harrington, "Supply Sergeant Harrington - Alia Das", VendorMale, 6, 555, 9628, 0, 0, 2),
            new(North, "Supply Sergeant North - Alia Das", VendorFemale, 6, 555, 9629, 0, 0, 2),
            new(PistolInfantryman, "Infantryman, pistol - Oliver's post", Infantryman, 8, 600, 8716, 0, 0, 510218),
            new(FieldGunner, "Field Gunner, machine gun - Oliver's post", HeavySoldier, 8, 600, 8720, 0, 0, 510219),
            new(Rifleman, "Rifleman, laser rifle - Oliver's post", Infantryman, 8, 600, 8717, 0, 0, 72003),
        };

        public static readonly NewPool[] Pools =
        {
            new(ShramekPool, Shramek, 755.7422, 294.257, 363.7305, 6.2029),
            new(GuardNorthPool, GuardInfantryman, 855.1562, 294.1406, 384.8477, 1.4577, 1),
            new(GuardSouthPool, GuardInfantryman, 855.0781, 294.1406, 391.1406, 1.4906, 1),
            new(SpauldingPool, Spaulding, 858.2695, 294.2109, 385.3398, 3.8075),
            new(CirclingGunnerPool, GunnerTrainee, 786.0, 294.92, 395.5, 1.5708),
            new(RoadShamanPool, ShamanTrainee, 840.5, 294.266, 430.0, 0.0),
            new(RoadGunnerWestPool, GunnerTrainee, 839.1116, 294.559, 431.8874, 0.0),
            new(RoadGunnerMiddlePool, GunnerTrainee, 840.6089, 294.289, 431.7967, 0.0),
            new(RoadGunnerEastPool, GunnerTrainee, 842.1061, 294.239, 431.706, 0.0),
            new(MaddreyPool, Maddrey, 752.6211, 293.9688, 396.9688, 4.7076),
            new(HarringtonPool, Harrington, 800.2852, 294.2617, 368.0117, 2.9544),
            new(NorthPool, North, 795.2539, 294.2617, 368.0, 3.3943),
            new(OliverPistolPool, PistolInfantryman, 536.3125, 290.4102, 500.6797, 0.3313),
            new(OliverGunnerEastPool, FieldGunner, 532.4219, 289.9648, 503.7461, 0.2197),
            new(OliverGunnerWestPool, FieldGunner, 526.293, 287.9141, 503.4531, 5.8322),
            new(OliverRiflemanPool, Rifleman, 521.1094, 287.9141, 502.7305, 6.0003),
        };

        /// <summary>Existing pools that are given a pose (the new ones carry theirs in <see cref="Pools"/>).</summary>
        public static readonly (uint Pool, uint Pose, string Who)[] Poses =
        {
            (100, 1, "Rogers"),          // he walked about the outpost; now he stands at his post in the command tent
            (193, 2, "Salter"),          // his rifle held ready across his chest
            (219, 1, "Apirka"),          // at ease with his spear
        };

        public static readonly Move[] Moves =
        {
            new(200, Bonham, "Major Bonham", 885.22266, 305.98438, 347.34375, 4.5, 886.3125, 304.6562, 355.1797, 5.8669),
            new(100, Rogers, "Outpost Commander Rogers", 870.0, 294.21, 385.5, 0.8, 857.4023, 294.2109, 388.1133, 1.5205),
            new(510002, Beacham, "Brigadier General Beacham", 813.4, 294.52, 387.95, 0.0, 871.082, 294.2109, 390.6562, 1.2299),
            new(211, Langerman, "Receptive Liaison Langerman", 864.7, 294.31787, 390.0, 1.8, 870.9766, 294.2109, 385.2539, 2.2258),
            new(30, Kolchak, "Clan Registrar Kolchak", 810.12, 294.11, 376.28, 4.4, 858.8828, 294.2617, 396.0469, 2.843),
            new(510006, Kincaid, "Training Officer Kincaid", 774.5, 293.88, 393.0, 0.0, 765.0, 294.1406, 385.8047, 4.5898),
            new(116, Caruso, "Supply Sergeant Caruso", 760.71484, 293.96875, 393.78516, 4.2, 761.7578, 293.9688, 395.207, 4.0651),
            new(210, Caufield, "Quartermaster Caufield", 756.8828, 293.96875, 407.07422, 4.7, 764.582, 293.9688, 406.6211, 4.5945),
            new(193, Salter, "Engineer Salter", 764.0, 293.97656, 409.0, 0.0, 763.6562, 293.9688, 409.5586, 5.0091),
            new(64, Moawi, "Council Elder Moawi", 812.0, 302.09375, 505.1953, 6.23, 810.6328, 302.0977, 500.875, 6.2107),
            new(184, Solis, "Council Elder Solis", 784.7, 287.33997, 581.1, 3.0, 789.8906, 287.2812, 583.3438, 1.5361),
            new(219, Apirka, "Warrior Apirka", 826.85156, 301.39062, 502.27734, 0.0, 826.7852, 301.2227, 500.1484, 0.0281),
            new(204, Richards, "Mining Coord. Richards", 372.84375, 213.69922, 517.8008, 6.14, 329.0195, 228.3945, 473.0195, 0.2669),
            new(205, Maxwell, "Mining Tech. Maxwell", 338.90234, 197.80469, 595.6719, 3.2, 332.4727, 228.3203, 470.8984, 0.757),
            // Turned only: 180 degrees, from facing +z to facing -z.
            new(SouthGateTurretPool, 550017, "AFS Light Turret by the south gate", 746.47998046875, 294.6300048828125, 283.9700012207031, 3.1415998935699463,
                746.47998046875, 294.6300048828125, 283.9700012207031, 0.0),
        };

        public static readonly OldPool[] Removed =
        {
            new(HumanVendorPool, HumanVendor, 25, 752.45703, 293.96875, 397.08203, 4.7),
            new(501001, 501001, 20, 771.0, 294.1, 386.1, 0.0),
            new(501002, 501002, 20, 768.5, 294.1, 390.4, 0.0),
            new(501003, 501003, 20, 763.5, 293.88, 390.4, 0.0),
            new(501004, 501004, 20, 761.0, 294.12, 386.85, 0.0),
            new(501005, 501005, 20, 763.5, 294.1, 381.8, 0.0),
            new(501006, 501006, 20, 768.5, 294.1, 381.8, 0.0),
        };

        public static readonly Recast[] Recasts =
        {
            new(Kolchak, "Clan Master", SwapsetMale, 8838, 0, 0, 2, "Clan Registrar Kolchak - Alia Das", SwapsetFemale, 8997, 0, 0, 2),
            new(Caruso, "Ammo Supply Vendor Alia Das", VendorFemale, 10790, 0, 0, 2, "Supply Sergeant Caruso - Alia Das", VendorFemale, 8509, 0, 0, 2),
            new(Rogers, "Outpost Commander Rogers", SwapsetMale, 2973, 9, 5, 2, "Outpost Commander Rogers", SwapsetMale, 2973, 0, 0, 2),
            // The gunner's goo gun attack (72009) has no animation on the warrior's skeleton: the spearman's thrust (5).
            new(Apirka, "Warrior Aprika", 22637, 2969, 0, 0, 72009, "Warrior Apirka", ForeanSpearman, 2969, 0, 0, 5),
        };

        /// <summary>The seed's officer: blue tunic, trousers and boots, a pistol, Hair_05 and the Caucasian long face.</summary>
        private static Slot[] SeedOfficer() => new[]
        {
            new Slot(2, OfficerBoots, OfficerBlue), new Slot(13, PistolRedshirt, 1), new Slot(14, 9781, DarkHair),
            new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight),
        };

        private static Slot[] Officer(uint hue, uint hair, uint hairColor, uint face, uint skin, uint weapon = PistolRedshirt) => new[]
        {
            new Slot(2, OfficerBoots, hue), new Slot(13, weapon, 1), new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, hue), new Slot(16, OfficerLegs, hue), new Slot(17, face, skin),
        };

        private static Slot[] GreenArmour(uint hair, uint hairColor, uint face, uint skin, uint weapon = PistolRedshirt) => new[]
        {
            new Slot(2, SoldierBoots, TrouserBrown), new Slot(3, SoldierGloves, Green), new Slot(13, weapon, 1), new Slot(14, hair, hairColor),
            new Slot(15, SoldierVest, Green), new Slot(16, OfficerLegs, TrouserBrown), new Slot(17, face, skin),
        };

        public static readonly Look[] Looks =
        {
            new(Bonham, "Major Bonham: brown officer's uniform, no helmet",
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120),
                        new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(2, OfficerBoots, Brown), new Slot(14, 9781, DarkHair), new Slot(15, OfficerTunic, Brown),
                        new Slot(16, OfficerLegs, Brown), new Slot(17, 24008, SkinLight) }),
            // His boots, trousers and tunic were in the helmet's, tunic's and trousers' slots (3, 15, 16).
            new(Rogers, "Outpost Commander Rogers: brown officer's uniform",
                new[] { new Slot(3, OfficerBoots, 22120), new Slot(13, PistolAvatar, 1), new Slot(14, 3672, 14404004),
                        new Slot(15, OfficerLegs, 933202), new Slot(16, OfficerTunic, 13933202), new Slot(17, 24019, SkinMedium) },
                new[] { new Slot(2, OfficerBoots, Brown), new Slot(13, PistolAvatar, 1), new Slot(14, 3672, 14404004),
                        new Slot(15, OfficerTunic, Brown), new Slot(16, OfficerLegs, Brown), new Slot(17, 24019, SkinMedium) }),
            new(Beacham, "Brigadier General Beacham: brown officer's uniform", SeedOfficer(), Officer(Brown, 9781, DarkHair, 24008, SkinLight)),
            new(Langerman, "Receptive Liaison Langerman: white uniform and beret", SeedOfficer(),
                new[] { new Slot(1, Officer2Cap, White), new Slot(2, OfficerBoots, Black), new Slot(13, PistolRedshirt, 1), new Slot(14, 9781, DarkHair),
                        new Slot(15, Officer2Vest, White), new Slot(16, OfficerLegs, White), new Slot(17, 24008, SkinLight) }),
            new(Kolchak, "Clan Registrar Kolchak: a woman in the brown officer's uniform", SeedOfficer(), Officer(Brown, 9782, DarkHair, 24009, SkinLight)),
            new(Cimoch, "Lieutenant Colonel Cimoch: blue light armour, rifle slung",
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120),
                        new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, MedicBoots, OfficerBlue), new Slot(3, MedicGloves, OfficerBlue),
                        new Slot(13, RifleAvatar, 1), new Slot(15, MedicTorso, OfficerBlue), new Slot(16, MedicLegs, OfficerBlue), new Slot(17, 24008, SkinLight) }),
            new(Kincaid, "Training Officer Kincaid: black and silver uniform", SeedOfficer(),
                new[] { new Slot(2, OfficerBoots, Black), new Slot(3, Officer4Gloves, Black), new Slot(13, PistolRedshirt, 1), new Slot(14, 9781, DarkHair),
                        new Slot(15, Officer4Vest, Silver), new Slot(16, OfficerLegs, Black), new Slot(17, 24008, SkinLight) }),
            new(Caufield, "Quartermaster Caufield: green armour, brown trousers", SeedOfficer(), GreenArmour(9781, DarkHair, 24008, SkinLight)),
            new(Salter, "Engineer Salter: brown strapped uniform, silver helmet, electric rifle", SeedOfficer(),
                new[] { new Slot(1, Officer3Helmet, Silver), new Slot(2, Officer3Boots, Brown), new Slot(3, Officer3Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, 9781, DarkHair), new Slot(15, Officer3Vest, Brown),
                        new Slot(16, Officer3Legs, Brown), new Slot(17, 24008, SkinLight) }),
            new(Oliver, "Arms Supplier Oliver: green armour, brown trousers", SeedOfficer(), GreenArmour(3663, DarkHair, 24020, SkinMedium)),
            new(Richards, "Mining Coord. Richards: green armour, brown trousers", SeedOfficer(), GreenArmour(9784, DarkHair, 24032, SkinLight)),
            new(Apirka, "Warrior Apirka: his spear", new[] { new Slot(13, ForeanNpcGooGun, 1) }, new[] { new Slot(13, ForeanSpear, 1) }),
            // The new ones: nothing before.
            new(Shramek, "Dropship Mechanic Shramek: khaki work suit", new Slot[0],
                new[] { new Slot(2, SapperBoots, Khaki), new Slot(3, SapperGloves, Khaki), new Slot(14, 3672, DarkHair),
                        new Slot(15, SapperTorso, Khaki), new Slot(16, SapperLegs, Khaki), new Slot(17, 24033, SkinLight) }),
            new(GuardInfantryman, "Infantryman: rifle", new Slot[0], new[] { new Slot(13, RifleAvatar, 1) }),
            new(Spaulding, "Captain Spaulding: green armour, brown trousers", new Slot[0], GreenArmour(9782, DarkHair, 24032, SkinMedium)),
            new(GunnerTrainee, "Forean Gunner Trainee: goo gun", new Slot[0], new[] { new Slot(13, ForeanGooGun, 1) }),
            new(ShamanTrainee, "Forean Shaman Trainee: staff", new Slot[0], new[] { new Slot(13, ForeanStaff, 1) }),
            new(Maddrey, "Supply Sergeant Maddrey: lighter brown officer's uniform", new Slot[0], Officer(LightBrown, 3663, DarkHair, 24020, SkinMedium)),
            new(Harrington, "Supply Sergeant Harrington: yellowish officer's uniform", new Slot[0], Officer(Yellow, 9783, DarkHair, 24016, SkinMedium)),
            new(North, "Supply Sergeant North: yellowish officer's uniform", new Slot[0], Officer(Yellow, 25255, DarkHair, 24010, SkinLight)),
            new(PistolInfantryman, "Infantryman: pistol", new Slot[0], new[] { new Slot(13, PistolRedshirt, 1) }),
            new(FieldGunner, "Field Gunner: machine gun", new Slot[0], new[] { new Slot(13, MachineGun, 1) }),
            new(Rifleman, "Rifleman: laser rifle", new Slot[0], new[] { new Slot(13, RifleLaser, 1) }),
        };

        public static readonly Greeting[] Greetings =
        {
            new(Rogers, 1620, 147),     // "We had a Bane Mothership in geosynchronous orbit above the Concordia continent for months..."
            new(Langerman, null, 1307), // "I'm here to help you discover the power you possess."
            new(Cimoch, null, 1199),    // "You're AFS, and that means you're unstoppable!..."
        };

        /// <summary>The crafting components the Kraftwerks takes (recipe_input), split between the two sergeants.</summary>
        public static readonly uint[] HarringtonStock =
        {
            1765, 45102, 45103, 45104, 45105,          // scrap metal to integrated circuits
            638, 45106, 45107, 45108, 45109,           // micromech: spore to medical grade
            1766, 42272, 42273, 42274, 42275,          // nucleotides: pyrimidines to DNA
            110908, 110909, 110910,                    // pharmaceuticals: high, select, elite grade
        };

        public static readonly uint[] NorthStock =
        {
            110985,                                    // bonding agent
            110980, 110981, 110982, 110983, 110984,    // pigments: magenta, cyan, yellow, black, white
        };

        public static readonly VendorStock[] Vendors =
        {
            new(Maddrey, 10, new uint[0], CopyFrom: HumanVendor),   // the "Human" weapons vendor's package and stock
            new(Harrington, 128, HarringtonStock),
            new(North, 129, NorthStock),
        };

        public static readonly Ambient[] Ambients =
        {
            new(22, 26417, 861.6484, 294.2109, 386.6562, 3.1495, "Alia Das command tent: man in grey at the terminal"),
            new(23, 26422, 863.5, 294.2109, 389.3086, 0.0848, "Alia Das command tent: woman in grey at the computer"),
        };

        public const string Revision = "wilderness_1_16_5";

        public static readonly Indicator[] Indicators =
        {
            new(508, 8, 1000000006, "Survey Says: report to Rogers", 870.0, 294.21, 385.5, 857.4023, 294.2109, 388.1133),
            new(701, 6, 1000000003, "Orders From High Command: Beacham", 812.9, 294.69, 388.2, 871.082, 294.2109, 390.6562),
            new(623, 1, 1000000001, "Salter", 764.0, 293.97656, 409.0, 763.6562, 293.9688, 409.5586),
            new(1407, 1, 1000000001, "Too Close For Comfort: Moawi", 812.0, 302.11786, 505.1953, 810.6328, 302.0977, 500.875),
            new(1407, 10, 1000000002, "Too Close For Comfort: Solis", 784.7, 287.33997, 581.1, 789.8906, 287.2812, 583.3438),
            new(1069, 2, 1000000002, "Receptive Reception: Solis", 784.7, 287.33997, 581.1, 789.8906, 287.2812, 583.3438),
            new(1069, 3, 1000000003, "Receptive Reception: Apirka", 826.85156, 301.51782, 502.27734, 826.7852, 301.2227, 500.1484),
        };

        /// <summary>
        /// Too Close For Comfort's escort (SolisEscort, mission_scene_binding of 1407): the ranger's
        /// route ended on Solis's old spot. It ends 1.5 m in front of him where he stands now, facing
        /// him, by way of a corner the navmesh's path takes there.
        /// </summary>
        public const string SolisRouteEndWas =
            "{\"position\":{\"x\":784.7,\"y\":287.33997,\"z\":581.1},\"orientation\":3,\"pauseMilliseconds\":0}";
        public const string SolisRouteEndIs =
            "{\"position\":{\"x\":785.857,\"y\":287.318,\"z\":579},\"orientation\":0,\"pauseMilliseconds\":0}," +
            "{\"position\":{\"x\":788.39,\"y\":287.2812,\"z\":583.29},\"orientation\":4.7124,\"pauseMilliseconds\":0}";

        /// <summary>The crafting station's quarter turn clockwise.</summary>
        public const double CraftingStationX = 797.8405, CraftingStationY = 294.3154, CraftingStationZ = 359.7303;
        public const double CraftingStationWas = 0.0, CraftingStationIs = 1.5708;

        public static readonly Step[] ShramekBeat =
        {
            new(755.7422, 294.257, 363.7305, 6.2029, 10000),   // in the latrine tent
            new(760.4209, 294.198, 364.6479, null, 0),
            new(765.0996, 294.198, 365.5652, null, 0),
            new(769.7783, 294.198, 366.4826, null, 0),
            new(774.457, 294.348, 367.4, null, 0),
            new(778.057, 294.348, 368.0, null, 0),
            new(779.057, 294.348, 366.4, null, 0),
            new(782.657, 294.348, 356.4, null, 0),
            new(800.257, 294.285, 355.12, null, 0),
            new(804.657, 294.148, 354.8, null, 0),
            new(808.9713, 292.998, 354.1429, null, 0),
            new(813.2856, 292.998, 353.4857, null, 0),
            new(817.5999, 293.849, 352.8286, null, 0),
            new(821.9141, 294.617, 352.1714, null, 0),
            new(826.2284, 294.757, 351.5143, null, 0),
            new(830.5427, 295.697, 350.8571, null, 0),
            new(834.857, 295.748, 350.2, null, 0),
            new(837.857, 295.348, 350.2, null, 0),
            new(841.857, 294.905, 351.9, null, 0),
            new(845.857, 294.442, 353.6, null, 0),
            new(849.857, 294.341, 355.3, null, 0),
            new(853.857, 294.693, 357.0, null, 0),
            new(857.857, 295.033, 358.7, null, 0),
            new(861.857, 295.148, 360.4, null, 0),
            new(864.557, 295.398, 360.5, null, 0),
            new(867.257, 296.348, 360.6, null, 0),
            new(868.257, 296.748, 360.6, null, 0),
            new(876.5903, 300.702, 358.6, null, 0),
            new(880.757, 302.345, 357.6, null, 0),
            new(884.9237, 304.38, 356.6, null, 0),
            new(889.0903, 305.543, 355.6, null, 0),
            new(893.257, 305.948, 354.6, null, 0),
            new(894.657, 305.948, 353.8, null, 0),
            new(896.6723, 306.194, 349.8629, null, 0),
            new(898.6875, 306.0695, 345.9258, 0.5482, 10000),  // on the landing pad
            new(896.6723, 306.194, 349.8629, null, 0),
            new(894.657, 305.948, 353.8, null, 0),
            new(893.257, 305.948, 354.6, null, 0),
            new(889.0903, 305.543, 355.6, null, 0),
            new(884.9237, 304.38, 356.6, null, 0),
            new(880.757, 302.345, 357.6, null, 0),
            new(876.5903, 300.702, 358.6, null, 0),
            new(868.257, 296.748, 360.6, null, 0),
            new(867.257, 296.348, 360.6, null, 0),
            new(864.557, 295.398, 360.5, null, 0),
            new(861.857, 295.148, 360.4, null, 0),
            new(857.857, 295.033, 358.7, null, 0),
            new(853.857, 294.693, 357.0, null, 0),
            new(849.857, 294.341, 355.3, null, 0),
            new(845.857, 294.442, 353.6, null, 0),
            new(841.857, 294.905, 351.9, null, 0),
            new(837.857, 295.348, 350.2, null, 0),
            new(834.857, 295.748, 350.2, null, 0),
            new(830.5427, 295.697, 350.8571, null, 0),
            new(826.2284, 294.757, 351.5143, null, 0),
            new(821.9141, 294.617, 352.1714, null, 0),
            new(817.5999, 293.849, 352.8286, null, 0),
            new(813.2856, 292.998, 353.4857, null, 0),
            new(808.9713, 292.998, 354.1429, null, 0),
            new(804.657, 294.148, 354.8, null, 0),
            new(800.257, 294.285, 355.12, null, 0),
            new(782.657, 294.348, 356.4, null, 0),
            new(779.057, 294.348, 366.4, null, 0),
            new(778.057, 294.348, 368.0, null, 0),
            new(774.457, 294.348, 367.4, null, 0),
            new(769.7783, 294.198, 366.4826, null, 0),
            new(765.0996, 294.198, 365.5652, null, 0),
            new(760.4209, 294.198, 364.6479, null, 0),
        };

        public static readonly Step[] CircleBeat =
        {
            new(786.0, 294.92, 395.5, null, 0),
            new(787.1647, 294.996, 395.3467, null, 0),
            new(788.25, 295.105, 394.8971, null, 0),
            new(789.182, 295.134, 394.182, null, 0),
            new(789.8971, 295.106, 393.25, null, 0),
            new(790.3467, 295.131, 392.1647, null, 0),
            new(790.5, 295.134, 391.0, null, 0),
            new(790.3467, 295.128, 389.8353, null, 0),
            new(789.8971, 295.109, 388.75, null, 0),
            new(789.182, 294.842, 387.818, null, 0),
            new(788.25, 295.0, 387.1029, null, 0),
            new(787.1647, 294.911, 386.6533, null, 0),
            new(786.0, 294.979, 386.5, null, 0),
            new(784.8353, 294.878, 386.6533, null, 0),
            new(783.75, 294.857, 387.1029, null, 0),
            new(782.818, 294.843, 387.818, null, 0),
            new(782.1029, 294.824, 388.75, null, 0),
            new(781.6533, 294.844, 389.8353, null, 0),
            new(781.5, 294.86, 391.0, null, 0),
            new(781.6533, 294.909, 392.1647, null, 0),
            new(782.1029, 295.051, 393.25, null, 0),
            new(782.818, 295.119, 394.182, null, 0),
            new(783.75, 295.181, 394.8971, null, 0),
            new(784.8353, 294.973, 395.3467, null, 0),
        };

        /// <summary>
        /// The road party: four parallel lines 1.5 m apart across the road, the shaman's 1.8 m
        /// further south, so walking south he leads and walking north he follows. The same
        /// length and pauses keep them abreast. Each line is walked north to south and back,
        /// 5 s at each end.
        /// </summary>
        public static readonly Step[] RoadShamanBeat =
        {
            new(840.5, 294.266, 430.0, null, 5000),
            new(840.2143, 293.914, 425.2857, null, 0),
            new(839.9286, 293.611, 420.5714, null, 0),
            new(839.6429, 293.499, 415.8571, null, 0),
            new(839.3571, 293.398, 411.1429, null, 0),
            new(839.0714, 293.485, 406.4286, null, 0),
            new(838.5, 293.601, 397.0, null, 5000),
            new(839.0714, 293.485, 406.4286, null, 0),
            new(839.3571, 293.398, 411.1429, null, 0),
            new(839.6429, 293.499, 415.8571, null, 0),
            new(839.9286, 293.611, 420.5714, null, 0),
            new(840.2143, 293.914, 425.2857, null, 0),
        };

        public static readonly Step[] RoadGunnerWestBeat =
        {
            new(839.1116, 294.559, 431.8874, null, 5000),
            new(838.8259, 294.309, 427.1732, null, 0),
            new(838.5402, 293.794, 422.4589, null, 0),
            new(838.2545, 293.638, 417.7446, null, 0),
            new(837.9688, 293.398, 413.0303, null, 0),
            new(837.6831, 293.431, 408.316, null, 0),
            new(837.3974, 293.604, 403.6017, null, 0),
            new(837.1116, 293.62, 398.8874, null, 5000),
            new(837.3974, 293.604, 403.6017, null, 0),
            new(837.6831, 293.431, 408.316, null, 0),
            new(837.9688, 293.398, 413.0303, null, 0),
            new(838.2545, 293.638, 417.7446, null, 0),
            new(838.5402, 293.794, 422.4589, null, 0),
            new(838.8259, 294.309, 427.1732, null, 0),
        };

        public static readonly Step[] RoadGunnerMiddleBeat =
        {
            new(840.6089, 294.289, 431.7967, null, 5000),
            new(840.0375, 293.6, 422.3681, null, 0),
            new(839.7517, 293.559, 417.6538, null, 0),
            new(839.466, 293.401, 412.9396, null, 0),
            new(839.1803, 293.424, 408.2253, null, 0),
            new(838.8946, 293.649, 403.511, null, 0),
            new(838.6089, 293.655, 398.7967, null, 5000),
            new(838.8946, 293.649, 403.511, null, 0),
            new(839.1803, 293.424, 408.2253, null, 0),
            new(839.466, 293.401, 412.9396, null, 0),
            new(839.7517, 293.559, 417.6538, null, 0),
            new(840.0375, 293.6, 422.3681, null, 0),
        };

        public static readonly Step[] RoadGunnerEastBeat =
        {
            new(842.1061, 294.239, 431.706, null, 5000),
            new(841.8204, 293.958, 426.9917, null, 0),
            new(841.5347, 293.709, 422.2774, null, 0),
            new(841.249, 293.705, 417.5631, null, 0),
            new(840.9633, 293.417, 412.8488, null, 0),
            new(840.6776, 293.417, 408.1345, null, 0),
            new(840.3919, 293.719, 403.4202, null, 0),
            new(840.1061, 293.758, 398.706, null, 5000),
            new(840.3919, 293.719, 403.4202, null, 0),
            new(840.6776, 293.417, 408.1345, null, 0),
            new(840.9633, 293.417, 412.8488, null, 0),
            new(841.249, 293.705, 417.5631, null, 0),
            new(841.5347, 293.709, 422.2774, null, 0),
            new(841.8204, 293.958, 426.9917, null, 0),
        };

        public static IEnumerable<(uint Pool, Step[] Steps)> Beats()
        {
            yield return (ShramekPool, ShramekBeat);
            yield return (CirclingGunnerPool, CircleBeat);
            yield return (RoadShamanPool, RoadShamanBeat);
            yield return (RoadGunnerWestPool, RoadGunnerWestBeat);
            yield return (RoadGunnerMiddlePool, RoadGunnerMiddleBeat);
            yield return (RoadGunnerEastPool, RoadGunnerEastBeat);
        }

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var c in Creatures)
                yield return $"INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, 1, {c.Level}, {c.MaxHp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {c.Action}, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

            foreach (var r in Recasts)
                yield return $"UPDATE creature SET comment = {S(r.IsComment)}, class_id = {r.IsClass}, name_id = {r.IsName}, run_speed = {r.IsRun}, walk_speed = {r.IsWalk}, action1 = {r.IsAction} " +
                    $"WHERE id = {r.Creature} AND class_id = {r.WasClass} AND name_id = {r.WasName} AND run_speed = {r.WasRun} AND walk_speed = {r.WasWalk} AND action1 = {r.WasAction};";

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Was, look.Is))
                    yield return sql;

            foreach (var p in Pools)
                yield return $"INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Id}, 0, 0, 20, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {Map}, {p.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            foreach (var p in Pools.Where(p => p.Pose.HasValue))
                yield return AddPose(p.Id, p.Pose.Value);

            foreach (var (pool, pose, _) in Poses)
                yield return AddPose(pool, pose);

            foreach (var (pool, steps) in Beats())
                for (var i = 0; i < steps.Length; i++)
                    yield return $"INSERT INTO spawnpool_patrol (pool_id, step, pos_x, pos_y, pos_z, facing, pause_ms) " +
                        $"SELECT {pool}, {i}, {N(steps[i].X)}, {N(steps[i].Y)}, {N(steps[i].Z)}, {(steps[i].Facing.HasValue ? N(steps[i].Facing.Value) : "NULL")}, {steps[i].PauseMs} " +
                        $"FROM spawnpool p WHERE p.id = {pool} AND NOT EXISTS (SELECT 1 FROM spawnpool_patrol x WHERE x.pool_id = {pool} AND x.step = {i});";

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.FromX, m.FromY, m.FromZ, m.FromRotation, m.ToX, m.ToY, m.ToZ, m.ToRotation);

            foreach (var r in Removed)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {r.Id} AND EXISTS (SELECT 1 FROM spawnpool p WHERE {Was(r)});";
                yield return $"DELETE FROM spawnpool WHERE {Was(r)};";
            }

            foreach (var g in Greetings)
            {
                if (g.Was.HasValue)
                    yield return $"UPDATE npc_greeting SET greeting_id = {g.Is} WHERE id = {g.Creature} AND greeting_id = {g.Was.Value};";
                else
                    yield return $"INSERT INTO npc_greeting (id, greeting_id, important) SELECT {g.Creature}, {g.Is}, 0 " +
                        $"FROM creature c WHERE c.id = {g.Creature} AND NOT EXISTS (SELECT 1 FROM npc_greeting x WHERE x.id = {g.Creature});";
            }

            foreach (var v in Vendors)
            {
                yield return $"INSERT INTO vendor (id, package_id) SELECT {v.Creature}, {v.Package} FROM creature c " +
                    $"WHERE c.id = {v.Creature} AND NOT EXISTS (SELECT 1 FROM vendor x WHERE x.id = {v.Creature});";

                if (v.CopyFrom != 0)
                    yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {v.Creature}, i.item_template_id FROM vendor_item i " +
                        $"WHERE i.id = {v.CopyFrom} AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {v.Creature} AND x.item_template_id = i.item_template_id);";

                foreach (var template in v.Templates)
                    yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {v.Creature}, {template} FROM itemtemplate t " +
                        $"WHERE t.id = {template} AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {v.Creature} AND x.item_template_id = {template});";
            }

            foreach (var a in Ambients)
                yield return $"INSERT INTO ambient_npc (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, comment) " +
                    $"SELECT {a.Id}, {Map}, {a.ClassId}, {N(a.X)}, {N(a.Y)}, {N(a.Z)}, {N(a.Rotation)}, {S(a.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {a.ClassId} AND NOT EXISTS (SELECT 1 FROM ambient_npc x WHERE x.id = {a.Id});";

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.FromX, i.FromY, i.FromZ, i.ToX, i.ToY, i.ToZ);

            yield return RerouteEscort(SolisRouteEndWas, SolisRouteEndIs);

            yield return TurnStation(CraftingStationWas, CraftingStationIs);
        }

        public static IEnumerable<string> Down()
        {
            yield return TurnStation(CraftingStationIs, CraftingStationWas);

            yield return RerouteEscort(SolisRouteEndIs, SolisRouteEndWas);

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.ToX, i.ToY, i.ToZ, i.FromX, i.FromY, i.FromZ);

            foreach (var a in Ambients)
                yield return $"DELETE FROM ambient_npc WHERE id = {a.Id} AND class_id = {a.ClassId};";

            foreach (var v in Vendors)
            {
                yield return $"DELETE FROM vendor_item WHERE id = {v.Creature};";
                yield return $"DELETE FROM vendor WHERE id = {v.Creature} AND package_id = {v.Package};";
            }

            foreach (var g in Greetings)
            {
                if (g.Was.HasValue)
                    yield return $"UPDATE npc_greeting SET greeting_id = {g.Was.Value} WHERE id = {g.Creature} AND greeting_id = {g.Is};";
                else
                    yield return $"DELETE FROM npc_greeting WHERE id = {g.Creature} AND greeting_id = {g.Is};";
            }

            foreach (var r in Removed)
                yield return $"INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {r.Id}, 0, 0, {r.RespawnTime}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {Map}, {r.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {r.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {r.Id});";

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.ToX, m.ToY, m.ToZ, m.ToRotation, m.FromX, m.FromY, m.FromZ, m.FromRotation);

            foreach (var (pool, _) in Beats())
                yield return $"DELETE FROM spawnpool_patrol WHERE pool_id = {pool};";

            foreach (var (pool, pose, _) in Poses)
                yield return $"DELETE FROM spawnpool_pose WHERE id = {pool} AND pose = {pose};";

            foreach (var p in Pools)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {p.Id};";
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Creature};";
            }

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Is, look.Was))
                    yield return sql;

            foreach (var r in Recasts)
                yield return $"UPDATE creature SET comment = {S(r.WasComment)}, class_id = {r.WasClass}, name_id = {r.WasName}, run_speed = {r.WasRun}, walk_speed = {r.WasWalk}, action1 = {r.WasAction} " +
                    $"WHERE id = {r.Creature} AND class_id = {r.IsClass} AND name_id = {r.IsName} AND run_speed = {r.IsRun} AND walk_speed = {r.IsWalk} AND action1 = {r.IsAction};";

            foreach (var c in Creatures)
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
        }

        /// <summary>
        /// A look changed from one set of rows to another, slot by slot, each guarded on what the
        /// slot holds: a slot in both is updated while it still holds the old class and colour, a
        /// slot only in the old is deleted if it is as it was, and a slot only in the new is
        /// filled if empty.
        /// </summary>
        internal static IEnumerable<string> Change(uint creature, Slot[] from, Slot[] to)
        {
            foreach (var old in from)
            {
                var next = to.FirstOrDefault(s => s.Id == old.Id);

                if (next == null)
                    yield return $"DELETE FROM creature_appearance WHERE id = {creature} AND slot_id = {old.Id} AND Class_id = {old.ClassId} AND color = {old.Color};";
                else if (next != old)
                    yield return $"UPDATE creature_appearance SET Class_id = {next.ClassId}, color = {next.Color} " +
                        $"WHERE id = {creature} AND slot_id = {old.Id} AND Class_id = {old.ClassId} AND color = {old.Color};";
            }

            foreach (var next in to.Where(s => from.All(old => old.Id != s.Id)))
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {creature}, {next.Id}, {next.ClassId}, {next.Color} " +
                    $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {creature} AND a.slot_id = {next.Id});";
        }

        internal static string AddPose(uint pool, uint pose) =>
            $"INSERT INTO spawnpool_pose (id, pose) SELECT {pool}, {pose} FROM spawnpool p " +
            $"WHERE p.id = {pool} AND NOT EXISTS (SELECT 1 FROM spawnpool_pose x WHERE x.id = {pool});";

        internal static string MovePool(uint pool, uint creature, double fromX, double fromY, double fromZ, double fromRotation,
            double toX, double toY, double toZ, double toRotation) =>
            $"UPDATE spawnpool SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)}, rotation = {N(toRotation)} " +
            $"WHERE id = {pool} AND creature_1_Id = {creature} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01 AND ABS(rotation - {N(fromRotation)}) < 0.01;";

        private static string Was(OldPool r) =>
            $"id = {r.Id} AND creature_1_Id = {r.Creature} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_y - {N(r.Y)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01";

        internal static string MoveIndicator(Indicator i, double fromX, double fromY, double fromZ, double toX, double toY, double toZ) =>
            $"UPDATE mission_indicator SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)} " +
            $"WHERE mission_id = {i.Mission} AND content_revision = {S(Revision)} AND objective_id = {i.Objective} AND indicator_id = {i.IndicatorId} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01;";

        /// <summary>The route's last point replaced in the binding's JSON, only where it is still the old one.</summary>
        private static string RerouteEscort(string from, string to) =>
            $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, {S(from)}, {S(to)}) " +
            $"WHERE mission_id = 1407 AND content_revision = {S(Revision)} AND script_key = 'wilderness.solis-escort' " +
            $"AND bindings LIKE {S("%" + from + "%")};";

        private static string TurnStation(double from, double to) =>
            $"UPDATE kraftwerks SET rotation = {N(to)} WHERE id = {CraftingStation} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(CraftingStationX)}) < 0.01 AND ABS(pos_z - {N(CraftingStationZ)}) < 0.01 AND ABS(rotation - {N(from)}) < 0.01;";

        internal static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        internal static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
