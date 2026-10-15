using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// The second batch a GM placed in the Wilderness (foreas_concordia_wilderness, 1220) from
    /// gameplay video, each read off the server's .where lines (Wilderness_post_npcs).
    ///
    /// Who and what is where:
    ///  - The Landing Zone fort: a female Field Gunner with the red-glowing machine gun slung,
    ///    standing (186.6, -47.2); the CP Token Banker (a man now, moved down from the hill at
    ///    149, -115.7) and a new CP Prestige Vendor (a woman), both in light brown with a pistol
    ///    held ready; Medic Bond and Medical Supply Sgt. Savage, new, in teal (Savage sells the
    ///    hospital stock); four Blood Analyzation Terminals, the AFS monitor on its wheeled stand,
    ///    one where the unnamed "Human" vendor (Test Vendor 3, pool 33) stood, who is taken away.
    ///    Squad Leader Rodriguez in orange heavy armour, Outpost Commander Randolph a woman,
    ///    Dr. Ojy in silver and pale blue armour with a silver helmet, Corporal Mendelson in
    ///    the blue light armour with his rifle slung and turned to face the pad.
    ///    The new ones there and the banker join the control point's AFS garrison (control_point_link).
    ///  - The fort's gate (ArchHumOutpostFenceGateV02 at 206, -35, facing pi: its 16 m run west
    ///    to 190) is closed by the AFS force field (UsableForceFieldHumOutpostGateV01, whose sheet
    ///    is centred on its origin): 8 m along the gate from its origin, at 198, -35.
    ///  - Lower Eloh Creek (Field Sgt. Witherspoon's camp): the unnamed weapons vendor there
    ///    (creature 61, pool 119) is Supply Sergeant Sawyer, a man in brown, keeping his stock;
    ///    Witherspoon in brown, moved; Recon. Spec. Jennings a woman in brown, moved a metre.
    ///  - Alia Das: the command tent's six Computer Terminals (the same monitor and stand) and the
    ///    Dell XPS tower on its desk; Information Spec. Van Arth, new, in brown; the Rewards
    ///    Verification Officer a man in teal armour (he was the Infantryman model); Ranger Benos,
    ///    new, a Forean spearman, at ease; Elder Quillas moved; footlockers in the barracks
    ///    (753-761, 380-386), in the tent by the crafting station (794-801, 364-366) and one at
    ///    the foot of each of the nine cots of the tent behind the command tent (861.8, 402.4).
    ///  - The Alia Das hospital: Dr. Elise Corman a Corman woman in the Corman's own clothes,
    ///    moved in from the officers' tents; Lt. Saviours lying wounded in the Infantryman's blue armour; the
    ///    unnamed medical vendor (creature 31, pool 62) is Medical Supply Sgt. Solomon, a woman in
    ///    teal, keeping her stock, moved; a Field Medic in white armour and pale green trousers; a
    ///    Computer Terminal on the desk.
    ///  - The AFS light turret on the tripod by the command tent (851.7, 396.8), which the turret
    ///    patch left empty because of a hostile pool no longer there; and the two tripods south of
    ///    Alia Das turned to their mounts' facing, as the south gate's was in Alia_das_npcs.
    ///  - The Bane cache Sgt. Pierre is held in (the walled post at -273..-253, 71..103): Pierre
    ///    moved into her cell and dressed in dark blue armour and a blue cap; fifteen Bane barrels
    ///    in its yard, two where the GM stood, the rest spread over the yard either side of the
    ///    path between its two gates (z 81 to 93 kept clear), on the terrain's height.
    ///
    /// Not here: the red Bane force field in Pierre's cell door is Escape Velocity's own (its
    /// scene's "forcefield" object, at the same spot), put up for a run of the mission; a
    /// standing one would stop her on her way out. The green loot pile is the client's
    /// Sys_Default_LootProxy ("Gibblet"), which a creature's corpse leaves when it goes with loot
    /// on it: the server's to make, not a placement.
    ///
    /// Map pieces' facings are read from the .map's orientation quaternions, stored (w, x, y, z).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, every change guarded on the row as it was (as
    /// AliaDasNpcs); Down undoes only what is still as Up left it.
    /// </summary>
    public static class WildernessPostNpcs
    {
        public static readonly uint Orange = Hue(0xD0, 0x6A, 0x1C);
        public static readonly uint Teal = Hue(0x2E, 0x8C, 0x88);
        public static readonly uint PaleBlue = Hue(0xA8, 0xC8, 0xE4);
        public static readonly uint DarkBlue = Hue(0x22, 0x33, 0x66);
        public static readonly uint CapBlue = Hue(0x2E, 0x4C, 0xA0);
        public static readonly uint PaleGreen = Hue(0xB8, 0xD8, 0xB0);
        public static readonly uint RedHair = Hue(0xA0, 0x38, 0x1C);
        public static readonly uint BlondHair = Hue(0xD8, 0xBC, 0x7A);
        public static readonly uint BrownHair = Hue(0x5A, 0x3A, 0x22);

        // Entity classes.
        public const uint HeavySoldierFemale = 29432, CormanFemale = 6340, CormanMale = 6339;
        public const uint HeavyHelm = 9808, HeavyBoots = 4299, HeavyTorso = 4301, HeavyLegs = 4302, HeavyGloves = 4303;
        public const uint Hair01 = 3672, Hair02 = 3663, Hair05 = 9781, Hair06 = 9782, Hair08 = 9784, Hair09 = 25255;
        public const uint BloodAnalyzer = 9742, ComputerTerminalStand = 9478, ComputerTerminalDesk = 9479, DellTower = 26500;
        public const uint Footlocker = 21030, BaneBarrel = 9260, HumanGateField = 20000001;
        public const uint LightTurret = 550017;

        // ---- ids ----
        public const uint FieldGunnerWoman = 640012, PrestigeVendor = 640013, Bond = 640014, Savage = 640015;
        public const uint VanArth = 640016, Benos = 640017, FieldMedic = 640018;
        public const uint FirstCreature = 640012, LastCreature = 640018;

        public const uint FieldGunnerPool = 640017, PrestigeVendorPool = 640018, BondPool = 640019, SavagePool = 640020;
        public const uint VanArthPool = 640021, BenosPool = 640022, FieldMedicPool = 640023, TurretPool = 640024;
        public const uint FirstPool = 640017, LastPool = 640024;

        public const uint Rodriguez = 97, Randolph = 130, TokenBanker = 500005, Ojy = 110, Mendelson = 105, Pierre = 630070;
        public const uint Sawyer = 61, Witherspoon = 101, Jennings = 135, RewardsOfficer = 136, Quillas = 114;
        public const uint Elise = 510004, Saviours = 120, Solomon = 31, HumanVendor3 = 23, HumanVendor3Pool = 33;

        public const uint FirstUsable = 640001, FirstFootlocker = 640001, FirstDestructible = 640001;

        public sealed record Usable(uint ClassId, double X, double Y, double Z, double Rotation, string Comment);

        public sealed record Locker(string Comment, double X, double Y, double Z, double Rotation);

        public sealed record Barrel(double X, double Y, double Z, double Rotation);

        public static readonly NewCreature[] Creatures =
        {
            new(FieldGunnerWoman, "Field Gunner, machine gun - Landing Zone", HeavySoldierFemale, 8, 600, 8720, 0, 0, 510219),
            new(PrestigeVendor, "CP Prestige Vendor - Landing Zone (Control Point)", VendorFemale, 5, 555, 10504, 0, 0, 0),
            new(Bond, "Medic Bond - Landing Zone", SwapsetMale, 10, 1000, 3037, 0, 0, 0),
            new(Savage, "Medical Supply Sgt. Savage - Landing Zone", VendorMale, 6, 555, 9169, 0, 0, 2),
            new(VanArth, "Information Spec. Van Arth - Alia Das", SwapsetMale, 10, 1000, 2972, 0, 0, 0),
            new(Benos, "Ranger Benos - Alia Das", ForeanSpearman, 8, 600, 3006, 0, 0, 5),
            new(FieldMedic, "Field Medic, medical orderly - Alia Das hospital", SwapsetFemale, 6, 555, 8721, 0, 0, 0),
        };

        public static readonly NewPool[] Pools =
        {
            new(FieldGunnerPool, FieldGunnerWoman, 186.6367, 163.1602, -47.1953, 3.3975, 1),
            new(PrestigeVendorPool, PrestigeVendor, 161.0, 163.0039, -91.1367, 4.5458, 2),
            new(BondPool, Bond, 151.4023, 163.0039, -99.6719, 4.973),
            new(SavagePool, Savage, 155.3359, 163.0742, -96.5273, 4.709),
            new(VanArthPool, VanArth, 827.0938, 294.4766, 408.8281, 0.2431),
            new(BenosPool, Benos, 773.8867, 301.1133, 493.9375, 4.8426, 1),
            new(FieldMedicPool, FieldMedic, 788.7227, 294.332, 360.2188, 5.6138),
        };

        /// <summary>
        /// The light turret on the tripod by the command tent (Creature_Human_Base_Turret_Mini_Base_Tripod
        /// at 851.69, 294.40, 396.77): the gun 1.04 m over the mount, as the turret patch puts it,
        /// facing the mount's way. Its pool is a turret's: 300 s to come back.
        /// </summary>
        public const double TurretX = 851.69, TurretY = 295.44, TurretZ = 396.77, TurretRotation = -1.7811;
        public const uint TurretRespawn = 3000;

        /// <summary>Existing pools given a pose.</summary>
        public static readonly (uint Pool, uint Pose, string Who)[] Poses =
        {
            (TokenBanker, 2, "CP Token Banker"),     // his pistol held up ready
            (198, 7, "Lt. Saviours"),                // lying wounded
        };

        public static readonly Move[] Moves =
        {
            new(TokenBanker, TokenBanker, "CP Token Banker", 149.0, 174.3000030517578, -115.69999694824219, 0.0, 161.1875, 163.0039, -95.332, 4.5096),
            new(182, Mendelson, "Corporal Mendelson (turned)", 189.41016, 167.0039, -94.41016, 4.7, 189.1953, 167.0, -94.3984, 2.282),
            new(Pierre, Pierre, "Sgt. Pierre", -279.0, 170.1, 87.0, -1.42855361, -281.9688, 170.4453, 86.4844, 4.5756),
            new(119, Sawyer, "Supply Sergeant Sawyer", 495.72266, 238.35156, 215.8789, 5.14, 494.5234, 238.3047, 217.2891, 5.7896),
            new(101, Witherspoon, "Field Sgt. Witherspoon", 505.0, 238.4, 223.0, 0.8, 502.2109, 238.3398, 214.0, 2.3292),
            new(213, Jennings, "Recon. Spec. Jennings", 498.8203, 238.33984, 209.29688, 4.2, 498.918, 238.3242, 210.1289, 0.5086),
            new(192, Quillas, "Elder Quillas", 783.0, 303.317277, 130.0, 1.030376827, 774.0859, 309.0352, 104.7969, 4.7639),
            new(Elise, Elise, "Dr. Elise Corman", 810.9000244140625, 294.42, 391.70001220703125, 0.0, 788.5977, 294.332, 368.3164, 2.5021),
            new(198, Saviours, "Lt. Saviours", 784.20703, 294.54297, 367.10547, 0.29, 784.5352, 294.543, 367.9023, 3.4822),
            new(62, Solomon, "Medical Supply Sgt. Solomon", 784.98047, 294.33203, 369.6836, 3.98, 785.3203, 294.332, 358.9141, 4.5881),
            // Turned only, from pi to their tripods' facing (0): the turret patch read the mounts' quaternions as (x, y, z, w).
            new(550083, LightTurret, "AFS Light Turret south of Alia Das, west", 769.489990234375, 291.0299987792969, 185.36000061035156, 3.1415998935699463,
                769.489990234375, 291.0299987792969, 185.36000061035156, 0.0),
            new(550084, LightTurret, "AFS Light Turret south of Alia Das, east", 792.2899780273438, 291.3999938964844, 181.5399932861328, 3.1415998935699463,
                792.2899780273438, 291.3999938964844, 181.5399932861328, 0.0),
        };

        public static readonly OldPool[] Removed =
        {
            new(HumanVendor3Pool, HumanVendor3, 25, 159.10156, 163.07422, -95.578125, 1.53),
        };

        public static readonly Recast[] Recasts =
        {
            new(Randolph, "AFS Outpost Commnader Randolph", SwapsetMale, 3076, 0, 0, 0, "Outpost Commander Randolph - Landing Zone", SwapsetFemale, 3076, 0, 0, 0),
            new(TokenBanker, "prestige (Adv_Foreas_Concordia_Wilderness)", VendorFemale, 9473, 0, 0, 0,
                "CP Token Banker - Landing Zone (Control Point)", VendorMale, 9473, 0, 0, 0),
            new(Jennings, "AFS Recon Spec Jennings", SwapsetMale, 3075, 0, 0, 0, "Recon. Spec. Jennings - Lower Eloh Creek", SwapsetFemale, 3075, 0, 0, 0),
            new(RewardsOfficer, "Rewards Verification Officer", Infantryman, 9685, 0, 0, 0, "Rewards Verification Officer - Alia Das", SwapsetMale, 9685, 0, 0, 0),
            new(Elise, "Dr. Elise Corman - Alia Das", CormanMale, 2989, 0, 0, 0, "Dr. Elise Corman - Alia Das hospital", CormanFemale, 2989, 0, 0, 0),
            new(Sawyer, "General Supply Vendor Lower Eloh Creek", SwapsetFemale, 0, 0, 0, 0, "Supply Sergeant Sawyer - Lower Eloh Creek", SwapsetMale, 8510, 0, 0, 0),
            new(Solomon, "Medical Vendor Static", VendorFemale, 0, 0, 0, 2, "Medical Supply Sgt. Solomon - Alia Das hospital", VendorFemale, 8889, 0, 0, 2),
        };

        /// <summary>The seed's officer as most of these were: blue tunic, trousers and boots, a pistol, Hair_05, the Caucasian long face.</summary>
        private static Slot[] SeedOfficer() => new[]
        {
            new Slot(2, OfficerBoots, OfficerBlue), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair05, DarkHair),
            new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight),
        };

        private static Slot[] Officer(uint hue, uint hair, uint hairColor, uint face, uint skin, uint weapon = PistolRedshirt) => new[]
        {
            new Slot(2, OfficerBoots, hue), new Slot(13, weapon, 1), new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, hue), new Slot(16, OfficerLegs, hue), new Slot(17, face, skin),
        };

        /// <summary>NPC_Clothing_Medic_1, the light armour, in one colour, with the hair and face given.</summary>
        private static Slot[] LightArmour(uint hue, uint hair, uint hairColor, uint face, uint skin) => new[]
        {
            new Slot(2, MedicBoots, hue), new Slot(3, MedicGloves, hue), new Slot(14, hair, hairColor),
            new Slot(15, MedicTorso, hue), new Slot(16, MedicLegs, hue), new Slot(17, face, skin),
        };

        public static readonly Look[] Looks =
        {
            new(Rodriguez, "Squad Leader Rodriguez: orange heavy armour", SeedOfficer(),
                new[] { new Slot(1, HeavyHelm, Orange), new Slot(2, HeavyBoots, Orange), new Slot(3, HeavyGloves, Orange), new Slot(13, PistolRedshirt, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, HeavyTorso, Orange), new Slot(16, HeavyLegs, Orange), new Slot(17, 24008, SkinLight) }),
            new(Randolph, "Outpost Commander Randolph: a woman, in the blue officer's uniform", SeedOfficer(), Officer(OfficerBlue, Hair06, DarkHair, 24009, SkinLight)),
            // The seed had the tunic and trousers each in the other's slot.
            new(TokenBanker, "CP Token Banker: a man in the light brown officer's uniform, pistol",
                new[] { new Slot(2, OfficerBoots, 22120), new Slot(14, Hair01, 1), new Slot(15, OfficerLegs, 13933202), new Slot(16, OfficerTunic, 13933202),
                        new Slot(17, 20824, SkinMedium) },
                Officer(LightBrown, Hair05, DarkHair, 24008, SkinLight)),
            new(Ojy, "Dr. Ojy: silver and pale blue light armour, silver helmet",
                new[] { new Slot(2, SapperBoots, 12429948), new Slot(14, Hair01, 1440400), new Slot(15, SapperTorso, 12429948), new Slot(16, SapperLegs, 12429948),
                        new Slot(17, 3813, SkinLight) },
                new[] { new Slot(1, Officer3Helmet, Silver), new Slot(2, MedicBoots, Silver), new Slot(3, MedicGloves, Silver), new Slot(14, Hair01, 1440400),
                        new Slot(15, MedicTorso, PaleBlue), new Slot(16, MedicLegs, PaleBlue), new Slot(17, 3813, SkinLight) }),
            new(Mendelson, "Corporal Mendelson: blue light armour and helmet, rifle slung", SeedOfficer(),
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, MedicBoots, OfficerBlue), new Slot(3, MedicGloves, OfficerBlue), new Slot(13, RifleAvatar, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, MedicTorso, OfficerBlue), new Slot(16, MedicLegs, OfficerBlue), new Slot(17, 24008, SkinLight) }),
            new(Pierre, "Sgt. Pierre: dark blue light armour, blue cap",
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120),
                        new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Officer2Cap, CapBlue), new Slot(2, MedicBoots, DarkBlue), new Slot(3, MedicGloves, DarkBlue), new Slot(14, Hair06, DarkHair),
                        new Slot(15, MedicTorso, DarkBlue), new Slot(16, MedicLegs, DarkBlue), new Slot(17, 24008, SkinLight) }),
            new(Sawyer, "Supply Sergeant Sawyer: a man in the brown officer's uniform, pistol",
                new[] { new Slot(2, OfficerBoots, 266553), new Slot(14, Hair09, 266553), new Slot(15, 19296, 266553), new Slot(16, 19250, 266553),
                        new Slot(17, 24019, SkinMedium) },
                Officer(Brown, Hair02, DarkHair, 24021, SkinLight)),
            // His boots, trousers and tunic were in the gloves', tunic's and trousers' slots (3, 15, 16).
            new(Witherspoon, "Field Sgt. Witherspoon: brown officer's uniform",
                new[] { new Slot(3, OfficerBoots, 22120), new Slot(13, PistolAvatar, 1), new Slot(14, Hair01, 1), new Slot(15, OfficerLegs, 13933202),
                        new Slot(16, OfficerTunic, 13933202), new Slot(17, 20824, SkinMedium) },
                Officer(Brown, Hair01, DarkHair, 20824, SkinMedium, PistolAvatar)),
            new(Jennings, "Recon. Spec. Jennings: a woman in the brown officer's uniform, brown hair, pistol", SeedOfficer(),
                Officer(Brown, Hair06, BrownHair, 24009, SkinLight)),
            new(RewardsOfficer, "Rewards Verification Officer: teal light armour, black hair", new Slot[0],
                LightArmour(Teal, Hair05, Black, 24032, SkinLight)),
            // A Corman woman with no clothing rows wears the Corman's own (avatar_hum_f_npc_corman_v01).
            new(Elise, "Dr. Elise Corman: blond, in the Corman's own clothes",
                new[] { new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120), new Slot(15, OfficerTunic, OfficerBlue),
                        new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(14, Hair06, BlondHair), new Slot(17, 24009, SkinLight) }),
            // He gives and takes missions, so he keeps the NPC body (the Infantryman model has no NPC augmentation),
            // dressed as the Infantryman: the blue light armour and helmet, rifle.
            new(Saviours, "Lt. Saviours: blue light armour and helmet, as an Infantryman", SeedOfficer(),
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, MedicBoots, OfficerBlue), new Slot(3, MedicGloves, OfficerBlue), new Slot(13, RifleAvatar, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, MedicTorso, OfficerBlue), new Slot(16, MedicLegs, OfficerBlue), new Slot(17, 24008, SkinLight) }),
            new(Solomon, "Medical Supply Sgt. Solomon: teal officer's uniform", SeedOfficer(), Officer(Teal, Hair09, DarkHair, 24010, SkinLight)),
            // The new ones: nothing before.
            new(FieldGunnerWoman, "Field Gunner: machine gun", new Slot[0], new[] { new Slot(13, MachineGun, 1) }),
            new(PrestigeVendor, "CP Prestige Vendor: a woman in the light brown officer's uniform, pistol", new Slot[0], Officer(LightBrown, Hair06, DarkHair, 24009, SkinLight)),
            new(Bond, "Medic Bond: teal officer's uniform", new Slot[0], Officer(Teal, Hair05, DarkHair, 24032, SkinLight)),
            new(Savage, "Medical Supply Sgt. Savage: teal officer's uniform", new Slot[0], Officer(Teal, Hair02, DarkHair, 24020, SkinMedium)),
            new(VanArth, "Information Spec. Van Arth: brown officer's uniform, pistol", new Slot[0], Officer(Brown, Hair08, DarkHair, 24021, SkinLight)),
            new(Benos, "Ranger Benos: his spear", new Slot[0], new[] { new Slot(13, ForeanSpear, 1) }),
            new(FieldMedic, "Field Medic: red hair, white light armour, pale green trousers", new Slot[0],
                new[] { new Slot(2, MedicBoots, White), new Slot(3, MedicGloves, White), new Slot(14, Hair09, RedHair),
                        new Slot(15, MedicTorso, White), new Slot(16, MedicLegs, PaleGreen), new Slot(17, 24011, SkinLight) }),
        };

        public static readonly VendorStock[] Vendors =
        {
            new(PrestigeVendor, 139, new uint[0], CopyFrom: 500100),    // Iapyx's CP Prestige Vendor's stock
            new(Savage, 87, new uint[0], CopyFrom: Solomon),             // the hospital stock
        };

        /// <summary>
        /// The Landing Zone is a control point (4): its AFS garrison (control_point_link kind 2) is
        /// what stands there while the AFS hold it. The new ones there join it, and the CP Token
        /// Banker, as at the other points; the taken-away vendor's pool leaves it.
        /// </summary>
        public const uint LandingZone = 4;
        public const byte AfsGarrison = 2;
        public static readonly uint[] GarrisonJoins = { FieldGunnerPool, PrestigeVendorPool, BondPool, SavagePool, TokenBanker };
        public static readonly uint[] GarrisonLeaves = { HumanVendor3Pool };

        public static readonly Indicator[] Indicators =
        {
            new(429, 4, 1000000002, "Witherspoon", 505.0, 238.757, 223.0, 502.2109, 238.6968, 214.0),
            new(700, 2, 1000000002, "Dr. Elise Corman", 810.9, 294.71, 391.7, 788.5977, 294.622, 368.3164),
            new(820, 2, 1000000002, "Dr. Elise Corman", 810.9, 294.71, 391.7, 788.5977, 294.622, 368.3164),
        };

        public static readonly Usable[] Usables =
        {
            // The Landing Zone's medical corner: the monitor on its wheeled stand (prop_hum_monitor_v01_v01).
            new(BloodAnalyzer, 154.2656, 163.0742, -83.1914, 5.1379, "Landing Zone: blood analyzer"),
            new(BloodAnalyzer, 159.0117, 163.0742, -86.332, 0.8654, "Landing Zone: blood analyzer"),
            new(BloodAnalyzer, 159.1875, 163.0742, -90.4844, 1.4387, "Landing Zone: blood analyzer"),
            new(BloodAnalyzer, 159.0977, 163.0742, -95.3242, 1.4685, "Landing Zone: blood analyzer, where Test Vendor 3 stood"),
            // The Alia Das command tent: the same stand, a console screen (prop_hum_monitor_v01_v06).
            new(ComputerTerminalStand, 860.1602, 294.2109, 391.418, 6.2657, "Alia Das command tent: terminal, north row"),
            new(ComputerTerminalStand, 862.1602, 294.2109, 391.4258, 0.0768, "Alia Das command tent: terminal, north row"),
            new(ComputerTerminalStand, 858.7812, 294.2109, 391.457, 6.1998, "Alia Das command tent: terminal, north row"),
            new(ComputerTerminalStand, 861.1953, 294.2109, 384.3828, 2.9984, "Alia Das command tent: terminal, south row"),
            new(ComputerTerminalStand, 862.3672, 294.2109, 384.4648, 3.0722, "Alia Das command tent: terminal, south row"),
            new(ComputerTerminalStand, 863.3008, 294.2109, 384.4727, 3.1304, "Alia Das command tent: terminal, south row"),
            new(DellTower, 871.957, 295.1406, 388.0312, 1.4433, "Alia Das command tent: computer tower and monitor on the desk"),
            // The hospital: the desk monitor alone (prop_hum_monitor_v02_v05).
            new(ComputerTerminalDesk, 785.75, 295.1953, 357.9531, 3.8985, "Alia Das hospital: terminal on the desk"),
        };

        public static readonly Locker[] Footlockers =
        {
            new("Alia Das barracks", 760.0469, 294.2109, 379.9805, 3.1821),
            new("Alia Das barracks", 760.5156, 294.2109, 384.4531, 4.6195),
            new("Alia Das barracks", 757.8281, 294.2109, 385.6523, 6.2531),
            new("Alia Das barracks", 756.2461, 294.2109, 384.8359, 4.4561),
            new("Alia Das barracks", 753.6758, 294.2109, 384.6406, 6.2295),
            new("Alia Das tent by the crafting station", 801.1484, 294.3398, 364.0742, 1.4904),
            new("Alia Das tent by the crafting station", 794.5586, 294.3398, 364.1875, 4.7435),
            new("Alia Das tent by the crafting station", 794.3906, 294.3398, 365.6953, 4.6869),
            new("Alia Das tent by the crafting station", 801.1836, 294.3359, 365.6953, 1.5595),
            // The tent behind the command tent: one at the foot of each cot (ArchHumGenObjCotV01, 2.44 m by 1.06),
            // its back to the cot, the north row's heads at the wall, the south row's by the dressers.
            new("Alia Das cot tent: foot of the cot at 862.5, 405.01", 862.5, 294.22, 403.03, 0.0),
            new("Alia Das cot tent: foot of the cot at 864.8, 404.99", 864.8, 294.22, 403.01, 0.0),
            new("Alia Das cot tent: foot of the cot at 867.48, 405.02", 867.48, 294.22, 403.04, 0.0),
            new("Alia Das cot tent: foot of the cot at 869.71, 405.09", 869.71, 294.22, 403.11, 0.0),
            new("Alia Das cot tent: foot of the cot at 873.18, 405.09", 873.18, 294.22, 403.11, 0.0),
            new("Alia Das cot tent: foot of the cot at 875.48, 405.07", 875.48, 294.22, 403.09, 0.0),
            new("Alia Das cot tent: foot of the cot at 863.79, 398.96", 861.81, 294.22, 398.96, 1.5708),
            new("Alia Das cot tent: foot of the cot at 869.0, 399.0", 867.02, 294.22, 399.0, 1.5708),
            new("Alia Das cot tent: foot of the cot at 874.71, 399.01", 872.73, 294.22, 399.01, 1.5708),
        };

        /// <summary>The Bane barrels in the yard of Pierre's cache, the first two where the GM stood.</summary>
        public static readonly Barrel[] Barrels =
        {
            new(-264.1992, 170.207, 99.3047, 6.1589),
            new(-262.9531, 170.0, 75.6523, 3.0833),
            new(-266.8, 170.192, 78.17, 3.103),
            new(-260.2, 170.069, 75.22, 2.2),
            new(-261.66, 170.008, 97.49, 3.202),
            new(-255.99, 169.825, 97.42, 5.275),
            new(-269.58, 170.008, 80.69, 1.661),
            new(-264.17, 170.008, 96.55, 3.788),
            new(-258.04, 170.008, 94.58, 1.574),
            new(-266.29, 169.886, 93.59, 3.556),
            new(-257.31, 170.314, 74.86, 2.902),
            new(-263.57, 170.008, 79.53, 1.117),
            new(-269.83, 170.008, 99.19, 3.622),
            new(-255.35, 170.314, 73.33, 6.277),
            new(-262.62, 170.008, 93.26, 2.312),
        };

        /// <summary>The Landing Zone gate's force field: the AFS's, which lets players and friends through and stops the hostile.</summary>
        public const double GateFieldX = 198.0, GateFieldY = 163.0, GateFieldZ = -35.0, GateFieldRotation = 3.1416;
        public const uint SideAfs = 1;

        public static uint UsableId(int index) => FirstUsable + (uint)index;
        public static uint FootlockerId(int index) => FirstFootlocker + (uint)index;
        public static uint BarrelId(int index) => FirstDestructible + (uint)index;
        public static uint GateFieldId => FirstDestructible + (uint)Barrels.Length;

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
                yield return InsertPool(p.Id, p.Creature, 20, p.X, p.Y, p.Z, p.Rotation);

            yield return InsertPool(TurretPool, LightTurret, TurretRespawn, TurretX, TurretY, TurretZ, TurretRotation);

            foreach (var p in Pools.Where(p => p.Pose.HasValue))
                yield return AddPose(p.Id, p.Pose.Value);

            foreach (var (pool, pose, _) in Poses)
                yield return AddPose(pool, pose);

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.FromX, m.FromY, m.FromZ, m.FromRotation, m.ToX, m.ToY, m.ToZ, m.ToRotation);

            foreach (var r in Removed)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {r.Id} AND EXISTS (SELECT 1 FROM spawnpool p WHERE {Was(r)});";
                yield return $"DELETE FROM spawnpool WHERE {Was(r)};";
            }

            foreach (var v in Vendors)
            {
                yield return $"INSERT INTO vendor (id, package_id) SELECT {v.Creature}, {v.Package} FROM creature c " +
                    $"WHERE c.id = {v.Creature} AND NOT EXISTS (SELECT 1 FROM vendor x WHERE x.id = {v.Creature});";

                if (v.CopyFrom != 0)
                    yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {v.Creature}, i.item_template_id FROM vendor_item i " +
                        $"WHERE i.id = {v.CopyFrom} AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {v.Creature} AND x.item_template_id = i.item_template_id);";
            }

            foreach (var pool in GarrisonJoins)
                yield return $"INSERT INTO control_point_link (control_point_id, kind, object_id) SELECT {LandingZone}, {AfsGarrison}, {pool} " +
                    $"FROM spawnpool p WHERE p.id = {pool} AND NOT EXISTS (SELECT 1 FROM control_point_link x WHERE x.control_point_id = {LandingZone} " +
                    $"AND x.kind = {AfsGarrison} AND x.object_id = {pool});";

            foreach (var pool in GarrisonLeaves)
                yield return $"DELETE FROM control_point_link WHERE control_point_id = {LandingZone} AND kind = {AfsGarrison} AND object_id = {pool} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE p.id = {pool});";

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.FromX, i.FromY, i.FromZ, i.ToX, i.ToY, i.ToZ);

            for (var i = 0; i < Usables.Length; i++)
            {
                var u = Usables[i];
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {UsableId(i)}, {Map}, {u.ClassId}, {N(u.X)}, {N(u.Y)}, {N(u.Z)}, {N(u.Rotation)}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {UsableId(i)});";
            }

            for (var i = 0; i < Footlockers.Length; i++)
            {
                var f = Footlockers[i];
                yield return "INSERT INTO footlocker (id, class_id, map_context_id, pos_x, pos_y, pos_z, rotation, comment) " +
                    $"SELECT {FootlockerId(i)}, {Footlocker}, {Map}, {N(f.X)}, {N(f.Y)}, {N(f.Z)}, {N(f.Rotation)}, {S(f.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {Footlocker} AND NOT EXISTS (SELECT 1 FROM footlocker x WHERE x.id = {FootlockerId(i)});";
            }

            for (var i = 0; i < Barrels.Length; i++)
            {
                var b = Barrels[i];
                yield return InsertDestructible(BarrelId(i), BaneBarrel, b.X, b.Y, b.Z, b.Rotation, 0, "Bane barrel in the yard of Sgt. Pierre's cache");
            }

            yield return InsertDestructible(GateFieldId, HumanGateField, GateFieldX, GateFieldY, GateFieldZ, GateFieldRotation, SideAfs,
                "Landing Zone gate: the AFS force field");
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM world_destructible WHERE id BETWEEN {FirstDestructible} AND {GateFieldId};";
            yield return $"DELETE FROM footlocker WHERE id BETWEEN {FirstFootlocker} AND {FootlockerId(Footlockers.Length - 1)} AND class_id = {Footlocker};";
            yield return $"DELETE FROM world_usable WHERE id BETWEEN {FirstUsable} AND {UsableId(Usables.Length - 1)};";

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.ToX, i.ToY, i.ToZ, i.FromX, i.FromY, i.FromZ);

            foreach (var pool in GarrisonJoins)
                yield return $"DELETE FROM control_point_link WHERE control_point_id = {LandingZone} AND kind = {AfsGarrison} AND object_id = {pool};";

            foreach (var v in Vendors)
            {
                yield return $"DELETE FROM vendor_item WHERE id = {v.Creature};";
                yield return $"DELETE FROM vendor WHERE id = {v.Creature} AND package_id = {v.Package};";
            }

            foreach (var r in Removed)
                yield return $"INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {r.Id}, 0, 0, {r.RespawnTime}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {Map}, {r.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {r.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {r.Id});";

            foreach (var pool in GarrisonLeaves)
                yield return $"INSERT INTO control_point_link (control_point_id, kind, object_id) SELECT {LandingZone}, {AfsGarrison}, {pool} " +
                    $"FROM spawnpool p WHERE p.id = {pool} AND NOT EXISTS (SELECT 1 FROM control_point_link x WHERE x.control_point_id = {LandingZone} " +
                    $"AND x.kind = {AfsGarrison} AND x.object_id = {pool});";

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.ToX, m.ToY, m.ToZ, m.ToRotation, m.FromX, m.FromY, m.FromZ, m.FromRotation);

            foreach (var (pool, pose, _) in Poses)
                yield return $"DELETE FROM spawnpool_pose WHERE id = {pool} AND pose = {pose};";

            foreach (var p in Pools)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {p.Id};";
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Creature};";
            }

            yield return $"DELETE FROM spawnpool WHERE id = {TurretPool} AND creature_1_Id = {LightTurret};";

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

        private static string InsertPool(uint id, uint creature, uint respawn, double x, double y, double z, double rotation) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"SELECT {id}, 0, 0, {respawn}, {N(x)}, {N(y)}, {N(z)}, {N(rotation)}, {Map}, {creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
            $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {id});";

        private static string InsertDestructible(uint id, uint classId, double x, double y, double z, double rotation, uint side, string comment) =>
            "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
            $"SELECT {id}, {Map}, {classId}, {N(x)}, {N(y)}, {N(z)}, {N(rotation)}, 0, 0, 0, {side}, {S(comment)} " +
            $"FROM entityclass e WHERE e.id = {classId} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {id});";

        private static string Was(OldPool r) =>
            $"id = {r.Id} AND creature_1_Id = {r.Creature} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_y - {N(r.Y)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01";
    }
}
