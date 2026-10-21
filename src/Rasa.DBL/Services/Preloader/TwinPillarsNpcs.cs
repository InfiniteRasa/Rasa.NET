using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// The third batch a GM placed in the Wilderness (foreas_concordia_wilderness, 1220) from
    /// gameplay video, each read off the server's .where lines (Twin_pillars_npcs).
    ///
    /// Twin Pillars:
    ///  - Its three gates (ArchHumOutpostFenceGateV01, 16 m each) closed by the AFS force field
    ///    (UsableForceFieldHumOutpostGateV01), 8 m along each gate from its origin, facing as it does:
    ///    the east gate at 31, -498, the north gate at -12.46, -398.59, the west gate at -16, -532.
    ///  - Victor Corman outside the north gate, in the Corman's own clothes (no clothing rows), walking
    ///    back and forth along the fence; he gives Quarantine, as before.
    ///  - Base Guard Byrnes, new, just inside the north gate: orange heavy armour over the light
    ///    armour's legs, which have no knee pads; he says the infirmary line (npcgreeting 555), marked.
    ///  - A squad of five, new, spread out east of the east gate: two Rocket Gunners (the NPC rocket
    ///    launcher, Weapon_Human_Redshirt_RocketLauncher_Physical: its weaponclass action 141/1), two
    ///    Riflemen and an Infantryman with a pistol.
    ///  - Recon. Spec. Ryans, new, in green armour and helmet, by the tower: the plague line (469), marked.
    ///  - The tower (ArchHumOutpostTowerV03 at -24, -488) has a teleporter pad on its floor and one on
    ///    its second floor 13.4 m straight above, and nothing working on either. A human teleporter
    ///    (ArchHumTeleporterV02) on each, linked both ways; the same in the tower south of Alia Das
    ///    (783, 192).
    ///  - Depot Commander Taylor turned, in the brown officer's uniform, no helmet, brown hair.
    ///    Receptive Liaison Standley turned, in Langerman's white uniform and beret.
    ///  - Two guards (Guardsman), new, a woman and a man, in green armour and helmet, at ease south of the infirmary (-129, -490).
    ///  - The infirmary: Medical Assistant Duncan a woman in Bio 2 armour, moved; Quarantine's analyzer
    ///    is the client's two-screen Computer Workstation (UsableNPCHumWorkstationV01, NPC-capable as
    ///    the Corman computer it replaces) where the GM stood, not the desk computer by the door; a
    ///    Field Medic, new, in teal, where the unnamed "Human" vendor (Test Vendor 3, pool 31) stood
    ///    nearby, who is taken away; Private Moore moved, in green armour and helmet with orange
    ///    trousers. Duncan and Moore say their lines once their missions are behind the player
    ///    (Managers.NpcGreetings.AfterMission), not from the start.
    ///  - The six specialty trainers (Soldier, Specialist, Commando, Ranger, Sapper, Biotechnician,
    ///    pools 501007-501012) taken away; the Class Trainer (500002) stays.
    ///  - The crafting station turned half round, its back to the wall.
    ///
    /// Elsewhere:
    ///  - Bane drop-offs south of Arms Supplier Oliver's post and by the road east of it: dropship
    ///    pools (anim_type 1) of Thrax Infantry Initiates (the pistol grunt) and Trainees (the rifle
    ///    grunt) - five level 5 Initiates every 2 minutes, five and two level 5 Trainees and two
    ///    level 4 Trainees every 4 - with the Wilderness's level 5 to 9 Thrax loot.
    ///  - Sgt. Pierre after her rescue, at the Alia Das hospital: a second Pierre, as she looks in the
    ///    cache, standing where she is found once Escape Velocity is done. She is there for everyone;
    ///    the server has no per-player spawns.
    ///  - Two Forean fire pits (UsableTwoStateForeanFirePitV01) on the Forean walkways east of
    ///    Alia Das, usable: lit and put out.
    ///  - Field Sergeant Slater in the pass to Continental Divide, at ease, his rifle on his back,
    ///    warning the player off (710), marked: on the NPC body dressed as an Infantryman, as Lt.
    ///    Saviours is, since the Infantryman's own body has no NPC augmentation and so no line.
    ///  - A Bane spotlight (UsableInertDestBaneSpotlightV01) on the trench north of Lower Eloh Creek (347.7, 279.2):
    ///    destroyed to its wreck, back later.
    ///  - Lt. Wood moved up beside it, on the swapset body (he was the Corman man's) in the brown
    ///    officer's uniform, flat brown hair; his line (157).
    ///  - Supplies On The Double's crate where the GM found it, among the dead scientists at
    ///    406, 114, and the objective's marker with it.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, every change guarded on the row as it was (as
    /// AliaDasNpcs); Down undoes only what is still as Up left it.
    /// </summary>
    public static class TwinPillarsNpcs
    {
        public const uint Map = AliaDasNpcs.Map;

        public static readonly uint Orange = WildernessPostNpcs.Orange;
        public static readonly uint Teal = WildernessPostNpcs.Teal;
        public static readonly uint BrownHair = WildernessPostNpcs.BrownHair;

        // Entity classes.
        public const uint HeavyTorso = WildernessPostNpcs.HeavyTorso, HeavyBoots = WildernessPostNpcs.HeavyBoots, HeavyGloves = WildernessPostNpcs.HeavyGloves;
        public const uint Bio2Boots = 7679, Bio2Gloves = 7680, Bio2Legs = 7681, Bio2Torso = 7682;
        public const uint Helmet3 = AliaDasTouchups.Helmet3;
        public const uint Hair01 = 3672, Hair02 = 3663, Hair05 = 9781, Hair06 = 9782;
        public const uint RocketLauncher = 6272;
        public const uint ThraxGruntPistol = 29769, ThraxGruntRifle = 29773, BanePistol = 3782, BaneRifle = 3878;
        public const uint GateField = WildernessPostNpcs.HumanGateField, Spotlight = 10192, FirePit = 6137;
        public const uint Workstation = 21087, CormanComputer = 7123;
        public const uint HumanTeleporter = ThunderheadBaseTeleporters.TeleporterClass, LocalTeleporterType = 1;

        // ---- ids ----
        public const uint Byrnes = 640101, Ryans = 640102, RocketGunner = 640103, SquadRifleman = 640104, SquadInfantryman = 640105;
        public const uint GuardWoman = 640106, GuardMan = 640107, FieldMedic = 640108, RescuedPierre = 640109, Slater = 640110;
        public const uint ThraxInitiate = 640111, ThraxTrainee = 640112, ThraxTraineeYoung = 640113;
        public const uint FirstCreature = 640101, LastCreature = 640113;

        public const uint RocketAction = 640101, ThraxPistolAction = 640102, ThraxRifleAction = 640103;

        public const uint ByrnesPool = 640101, RyansPool = 640102;
        public const uint RocketWestPool = 640103, RocketEastPool = 640104, RifleWestPool = 640105, RifleEastPool = 640106, SquadPistolPool = 640107;
        public const uint GuardWomanPool = 640108, GuardManPool = 640109, FieldMedicPool = 640110, RescuedPierrePool = 640111, SlaterPool = 640112;
        public const uint DropInitiatesPool = 640113, DropTraineesPool = 640114, DropPairPool = 640115, DropRoadPool = 640116;
        public const uint FirstPool = 640101, LastPool = 640116;

        public const uint Victor = 140, VictorPool = 218, Duncan = 125, DuncanPool = 203, Taylor = 129, TaylorPool = 207;
        public const uint Moore = 131, MoorePool = 209, Standley = 134, StandleyPool = 212, Wood = 121, WoodPool = 199;
        public const uint HumanVendor3 = 23, HumanVendor3Pool = 31, Pierre = 630070, CraftingStation = 6;

        public const uint FirstDestructible = 640101, FirstUsable = 640101, FirstTeleporter = 984;

        public const uint DropshipAnimation = 1;

        /// <summary>The Thrax's loot: the Wilderness's for Thrax of levels 5 to 9, the nearest it has for the level 4s.</summary>
        public const string ThraxLoot = "wilderness_v1_thrax_5_9";

        /// <summary>The creatures bound to <see cref="ThraxLoot"/> here.</summary>
        public static readonly uint[] LootedCreatures = { ThraxInitiate, ThraxTrainee, ThraxTraineeYoung };
        public const uint InitiateRespawn = 1200, TraineeRespawn = 2400;
        public const double DropRadius = 8.0;

        public sealed record Combatant(uint Id, string Comment, uint ClassId, uint Faction, uint Level, uint MaxHp, uint NameId, uint Action,
            uint Weapon, uint Body, uint Armor);

        public sealed record Attack(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax, uint Cooldown,
            uint MinDamage, uint MaxDamage);

        public sealed record DropOff(uint Pool, uint Creature, uint Count, uint Respawn, double X, double Y, double Z, double Rotation, string Comment);

        public sealed record Placed(uint ClassId, double X, double Y, double Z, double Rotation, uint Side, string Comment);

        public sealed record Pad(uint Id, double X, double Y, double Z, string Description);

        public sealed record Line(uint Creature, uint GreetingId, bool Important);

        public sealed record SceneMove(uint Mission, string Script, string From, string To);

        public static readonly NewCreature[] Creatures =
        {
            new(Byrnes, "Base Guard Byrnes - Twin Pillars", SwapsetMale, 10, 1000, 3081, 0, 0, 0),
            new(Ryans, "Recon. Spec. Ryans - Twin Pillars", SwapsetMale, 10, 1000, 3086, 0, 0, 0),
            new(GuardWoman, "Guardsman, green armour - Twin Pillars", SwapsetFemale, 10, 1000, 8730, 0, 0, 0),
            new(GuardMan, "Guardsman, green armour - Twin Pillars", SwapsetMale, 10, 1000, 8730, 0, 0, 0),
            new(FieldMedic, "Field Medic, teal uniform - Twin Pillars infirmary", SwapsetMale, 10, 1000, 8721, 0, 0, 0),
            // He has a line to say, so he is on the NPC body (the Infantryman's own has no NPC augmentation), dressed as one.
            new(Slater, "Field Sergeant Slater - the pass to Continental Divide", SwapsetMale, 10, 1000, 8533, 0, 0, 0),
        };

        /// <summary>The fighters: the squad and the Thrax, each with the attack it fires and, for the Thrax, its stats.</summary>
        public static readonly Combatant[] Combatants =
        {
            new(RocketGunner, "Rocket Gunner, rocket launcher - Twin Pillars squad", Infantryman, 1, 8, 600, 8724, RocketAction, RocketLauncher, 0, 0),
            new(SquadRifleman, "Rifleman, rifle - Twin Pillars squad", Infantryman, 1, 8, 600, 8717, 72003, RifleAvatar, 0, 0),
            new(SquadInfantryman, "Infantryman, pistol - Twin Pillars squad", Infantryman, 1, 8, 600, 8716, 510218, PistolRedshirt, 0, 0),
            new(ThraxInitiate, "Thrax Infantry Initiate level 5 - Wilderness drop-off", ThraxGruntPistol, 0, 5, 330, 7674, ThraxPistolAction, BanePistol, 15, 60),
            new(ThraxTrainee, "Thrax Infantry Trainee level 5 - Wilderness drop-off", ThraxGruntRifle, 0, 5, 330, 7675, ThraxRifleAction, BaneRifle, 15, 60),
            new(ThraxTraineeYoung, "Thrax Infantry Trainee level 4 - Wilderness drop-off", ThraxGruntRifle, 0, 4, 280, 7675, ThraxRifleAction, BaneRifle, 12, 48),
        };

        public static readonly Attack[] Attacks =
        {
            new(RocketAction, "AFS Rocket Gunner rocket launcher 141/1", 141, 1, 1.0, 40.0, 2000, 15, 25),
            new(ThraxPistolAction, "Thrax Infantry Initiate pistol 1/1", 1, 1, 0.5, 24.0, 1300, 9, 13),
            new(ThraxRifleAction, "Thrax Infantry Trainee rifle 1/79", 1, 79, 0.5, 30.0, 1000, 10, 15),
        };

        public static readonly NewPool[] Pools =
        {
            new(ByrnesPool, Byrnes, -20.1797, 189.207, -401.0156, 2.9106),
            new(RyansPool, Ryans, -30.75, 206.0977, -499.1992, 4.9292),
            // The squad: the pistol where the GM stood, the riflemen 3 m either side and 1.5 m back, the
            // rocket gunners 6 m out and 3 m back, on the terrain.
            new(SquadPistolPool, SquadInfantryman, 42.875, 203.5781, -494.6445, 4.2427),
            new(RifleWestPool, SquadRifleman, 42.895, 203.53, -497.999, 4.2427),
            new(RifleEastPool, SquadRifleman, 40.18, 203.44, -492.648, 4.2427),
            new(RocketWestPool, RocketGunner, 42.916, 205.13, -501.353, 4.2427),
            new(RocketEastPool, RocketGunner, 37.484, 203.19, -490.652, 4.2427),
            new(GuardWomanPool, GuardWoman, -126.2383, 220.2695, -490.1641, 0.3817, 1),
            new(GuardManPool, GuardMan, -131.832, 220.2695, -490.3789, 0.2685, 1),
            new(FieldMedicPool, FieldMedic, -118.9805, 220.7539, -468.1367, 1.3147),
            new(SlaterPool, Slater, 906.2773, 275.1016, 47.3086, 2.928, 1),
        };

        /// <summary>Sgt. Pierre after her rescue: her own row and looks, standing at the Alia Das hospital.</summary>
        public const double RescuedPierreX = 794.9766, RescuedPierreY = 294.3828, RescuedPierreZ = 378.9414, RescuedPierreRotation = 3.206;

        public static readonly DropOff[] DropOffs =
        {
            new(DropInitiatesPool, ThraxInitiate, 5, InitiateRespawn, 521.5586, 287.9141, 512.6094, 4.1438, "Bane drop-off south of Oliver's post: five Initiates"),
            new(DropTraineesPool, ThraxTrainee, 5, TraineeRespawn, 557.6406, 288.082, 463.3906, 5.4176, "Bane drop-off east of Oliver's post: five Trainees"),
            new(DropPairPool, ThraxTrainee, 2, TraineeRespawn, 531.1484, 288.0156, 482.8984, 3.3897, "Bane drop-off by Oliver's post: two Trainees"),
            new(DropRoadPool, ThraxTraineeYoung, 2, TraineeRespawn, 679.9531, 285.2188, 239.9961, 2.053, "Bane drop-off by the road: two level 4 Trainees"),
        };

        /// <summary>Victor's beat along the fence outside the north gate, 5 m either way of the GM's spot.</summary>
        public static readonly Step[] VictorBeat =
        {
            new(-24.457, 188.4023, -391.1055, null, 0),
            new(-19.582, 188.583, -390.016, null, 5000),
            new(-24.457, 188.4023, -391.1055, null, 0),
            new(-29.332, 188.957, -392.195, null, 5000),
        };

        public static readonly Move[] Moves =
        {
            new(VictorPool, Victor, "Victor Corman", -28.289062, 189.67188, -383.09766, 4.3, -24.457, 188.4023, -391.1055, 3.1353),
            new(DuncanPool, Duncan, "Medical Assistant Duncan", -127.99609, 220.76172, -466.9961, 6.14, -128.3711, 220.7539, -467.1719, 5.658),
            new(TaylorPool, Taylor, "Depot Commander Taylor", -91.77344, 220.26953, -503.13272, 5.28, -92.0977, 220.2695, -503.4609, 6.247),
            new(MoorePool, Moore, "Private Moore", -137.9961, 220.27734, -465.9961, 0.0, -138.4844, 220.2695, -466.6484, 0.9535),
            new(StandleyPool, Standley, "Receptive Liaison Standley", -109.98828, 220.26953, -494.30078, 6.23, -109.9883, 220.2695, -494.2031, 1.186),
            new(WoodPool, Wood, "Lt. Wood", 389.1289, 233.21094, 260.90625, 3.69, 356.0273, 232.3555, 293.7031, 1.0635),
        };

        public static readonly OldPool[] Removed =
        {
            new(HumanVendor3Pool, HumanVendor3, 25, -125.27734, 221.0586, -480.73047, 4.7),
            new(501007, 501007, 20, -96.0, 220.300003051758, -516.099975585938, 0.0),
            new(501008, 501008, 20, -98.5, 220.300003051758, -511.799987792969, 0.0),
            new(501009, 501009, 20, -103.5, 220.300003051758, -511.799987792969, 0.0),
            new(501010, 501010, 20, -106.0, 220.300003051758, -516.099975585938, 0.0),
            new(501011, 501011, 20, -103.25, 220.27, -521.15, 0.0),
            new(501012, 501012, 20, -98.5, 221.27, -520.400024414063, 0.0),
        };

        public static readonly Recast[] Recasts =
        {
            // Victor walks his beat.
            new(Victor, "Victor Corman", CormanMale, 3084, 0, 0, 0, "Victor Corman - outside Twin Pillars", CormanMale, 3084, 0, 2, 0),
            new(Duncan, "Medical Assistant Duncan", SwapsetMale, 3082, 0, 0, 0, "Medical Assistant Duncan - Twin Pillars infirmary", SwapsetFemale, 3082, 0, 0, 0),
            new(Wood, "AFS Officer Lt Wood", CormanMale, 3095, 0, 0, 2, "Lt. Wood - Lower Eloh Creek trenches", SwapsetMale, 3095, 0, 0, 2),
        };

        public const uint CormanMale = WildernessPostNpcs.CormanMale;

        private static Slot[] SeedOfficer() => new[]
        {
            new Slot(2, OfficerBoots, OfficerBlue), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair05, DarkHair),
            new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight),
        };

        private static Slot[] Officer(uint hue, uint hair, uint hairColor, uint face, uint skin) => new[]
        {
            new Slot(2, OfficerBoots, hue), new Slot(13, PistolRedshirt, 1), new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, hue), new Slot(16, OfficerLegs, hue), new Slot(17, face, skin),
        };

        /// <summary>The Soldier_1 armour vest, gloves and boots in green, Clothing_Helmet_3 in green, the trousers given; a pistol.</summary>
        private static Slot[] GreenArmour(uint legs, uint legsHue, uint hair, uint face, uint skin) => new[]
        {
            new Slot(1, Helmet3, Green), new Slot(2, SoldierBoots, Green), new Slot(3, SoldierGloves, Green), new Slot(13, PistolRedshirt, 1),
            new Slot(14, hair, DarkHair), new Slot(15, SoldierVest, Green), new Slot(16, legs, legsHue), new Slot(17, face, skin),
        };

        public static readonly Look[] Looks =
        {
            // A Corman man with no clothing rows wears the Corman's own (avatar_hum_m_npc_corman_v01).
            new(Victor, "Victor Corman: the Corman's own clothes",
                new[] { new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120), new Slot(15, OfficerTunic, OfficerBlue),
                        new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(17, 24008, SkinLight) }),
            new(Duncan, "Medical Assistant Duncan: a woman in Bio 2 armour", SeedOfficer(),
                new[] { new Slot(2, Bio2Boots, White), new Slot(3, Bio2Gloves, White), new Slot(14, Hair06, BrownHair),
                        new Slot(15, Bio2Torso, White), new Slot(16, Bio2Legs, White), new Slot(17, 24009, SkinLight) }),
            new(Taylor, "Depot Commander Taylor: brown officer's uniform, no helmet, brown hair", SeedOfficer(),
                Officer(Brown, Hair05, BrownHair, 24008, SkinLight)),
            new(Standley, "Receptive Liaison Standley: white uniform and beret", SeedOfficer(),
                new[] { new Slot(1, Officer2Cap, White), new Slot(2, OfficerBoots, Black), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair05, DarkHair),
                        new Slot(15, Officer2Vest, White), new Slot(16, OfficerLegs, White), new Slot(17, 24008, SkinLight) }),
            new(Moore, "Private Moore: green armour and helmet, orange trousers", SeedOfficer(),
                GreenArmour(OfficerLegs, Orange, Hair05, 24008, SkinLight)),
            new(Wood, "Lt. Wood: brown officer's uniform, flat brown hair",
                new[] { new Slot(1, Officer3Helmet, SkinMedium), new Slot(2, OfficerBoots, OfficerBlue), new Slot(3, Officer3Gloves, 22120),
                        new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                Officer(Brown, Hair01, BrownHair, 24021, SkinLight)),
            // The new ones: nothing before.
            new(Byrnes, "Base Guard Byrnes: orange heavy armour, the light armour's legs (no knee pads), pistol", new Slot[0],
                new[] { new Slot(2, HeavyBoots, Orange), new Slot(3, HeavyGloves, Orange), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair02, DarkHair),
                        new Slot(15, HeavyTorso, Orange), new Slot(16, MedicLegs, Orange), new Slot(17, 24020, SkinMedium) }),
            new(Ryans, "Recon. Spec. Ryans: green armour and helmet", new Slot[0],
                GreenArmour(OfficerLegs, Green, Hair05, 24008, SkinLight)),
            new(GuardWoman, "Guardsman: a woman in green armour and helmet", new Slot[0],
                GreenArmour(OfficerLegs, TrouserBrown, Hair06, 24009, SkinLight)),
            new(GuardMan, "Guardsman: a man in green armour and helmet", new Slot[0],
                GreenArmour(OfficerLegs, TrouserBrown, Hair02, 24021, SkinMedium)),
            new(FieldMedic, "Field Medic: teal officer's uniform", new Slot[0], Officer(Teal, Hair02, DarkHair, 24020, SkinMedium)),
            new(Slater, "Field Sergeant Slater: as an Infantryman, the blue light armour and helmet, rifle", new Slot[0],
                new[] { new Slot(1, Officer3Helmet, 4286886614), new Slot(2, MedicBoots, OfficerBlue), new Slot(3, MedicGloves, OfficerBlue), new Slot(13, RifleAvatar, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, MedicTorso, OfficerBlue), new Slot(16, MedicLegs, OfficerBlue), new Slot(17, 24008, SkinLight) }),
        };

        public static readonly Line[] Lines =
        {
            new(Byrnes, 555, true),     // "Old Man Taylor's been going on about drugs stolen from the infirmary..."
            new(Ryans, 469, true),      // "I hear those Corman hippies are carrying some kind of plague..."
            new(Slater, 710, true),     // "This path leads to Continental Divide.  Are you sure you want to head that way?..."
            new(Wood, 157, false),      // "You want to know the secret to staying alive out here?..."
        };

        public static readonly Indicator[] Indicators =
        {
            new(442, 1, 1000000001, "Quarantine: Duncan", -127.99609, 220.76172, -466.9961, -128.3711, 220.7539, -467.1719),
            new(442, 2, 1000000002, "Quarantine: the analyzer", -124.800003051758, 220.917831420898, -477.200012207031, -132.4961, 220.7539, -467.1172),
            new(623, 4, 1000000004, "Smuggler's Blues: Moore", -137.9961, 220.27734, -465.9961, -138.4844, 220.2695, -466.6484),
            new(791, 1, 1000000001, "Moore", -137.9961, 220.27734, -465.9961, -138.4844, 220.2695, -466.6484),
            new(791, 2, 1000000002, "Taylor", -91.77344, 220.26953, -503.13272, -92.0977, 220.2695, -503.4609),
            new(431, 3, 1000000003, "Lt. Wood", 389.1289, 233.21094, 260.90625, 356.0273, 232.3555, 293.7031),
            new(428, 1, 1000000001, "Supplies On The Double: the crate", 456.0, 233.899993896484, 193.0, 406.3594, 234.7891, 114.4219),
        };

        /// <summary>Quarantine's analyzer, the two-screen workstation, and where a player stands at it (1.5 m off its east side).</summary>
        public const double AnalyzerX = -132.4961, AnalyzerY = 220.7539, AnalyzerZ = -467.1172, AnalyzerRotation = 4.6762;
        public const double AnalyzerStandX = -131.0, AnalyzerStandZ = -467.1172;

        /// <summary>Scene actors moved in their missions' bindings: the analyzer (and its class) and the crate.</summary>
        public static readonly SceneMove[] SceneMoves =
        {
            new(442, "data.sequence",
                "\"templateId\":7123,\"position\":{\"x\":-124.8,\"y\":222.0481,\"z\":-479.2},\"orientation\":3.141592653589793",
                "\"templateId\":21087,\"position\":{\"x\":-132.4961,\"y\":220.7539,\"z\":-467.1172},\"orientation\":4.6762"),
            new(428, "wilderness.supply-delivery",
                "\"templateId\":26721,\"position\":{\"x\":456,\"y\":233.9,\"z\":193},\"orientation\":0",
                "\"templateId\":26721,\"position\":{\"x\":406.3594,\"y\":234.7891,\"z\":114.4219},\"orientation\":0.3786"),
        };

        /// <summary>The gates' force fields, and the spotlight (the Bane's: players may shoot it).</summary>
        public static readonly Placed[] Destructibles =
        {
            new(GateField, 31.0, 203.0, -490.0, -1.5708, 1, "Twin Pillars east gate: the AFS force field"),
            new(GateField, -20.26, 188.0, -400.33, 2.920, 1, "Twin Pillars north gate: the AFS force field"),
            new(GateField, -24.0, 220.0, -532.0, -3.1416, 1, "Twin Pillars west gate: the AFS force field"),
            new(Spotlight, 347.7109, 233.75, 279.1719, 1.1608, 0, "Bane spotlight on the trench, Lower Eloh Creek"),
        };

        public static readonly Placed[] Usables =
        {
            new(FirePit, 833.5352, 299.7891, 411.4336, 5.7366, 0, "Forean fire pit on the walkway east of Alia Das"),
            new(FirePit, 846.1953, 299.7891, 409.4414, 0.3284, 0, "Forean fire pit on the walkway east of Alia Das"),
        };

        /// <summary>The two towers' pads, floor and second floor, at the height the GM stood on them and the floor 13.4 m up.</summary>
        public static readonly Pad[] Pads =
        {
            new(FirstTeleporter, -23.9375, 207.0, -488.6836, "Local Teleporter: Twin Pillars tower, ground floor"),
            new(FirstTeleporter + 1, -23.9375, 220.4, -488.6836, "Local Teleporter: Twin Pillars tower, second floor"),
            new(FirstTeleporter + 2, 782.9688, 291.0, 192.4648, "Local Teleporter: tower south of Alia Das, ground floor"),
            new(FirstTeleporter + 3, 782.9688, 304.4, 192.4648, "Local Teleporter: tower south of Alia Das, second floor"),
        };

        public static IEnumerable<(uint From, uint To)> Links()
        {
            for (var i = 0; i < Pads.Length; i += 2)
            {
                yield return (Pads[i].Id, Pads[i + 1].Id);
                yield return (Pads[i + 1].Id, Pads[i].Id);
            }
        }

        public const double CraftingStationX = -140.2918, CraftingStationZ = -535.7525, CraftingStationWas = 0.0, CraftingStationIs = 3.1416;

        public static uint DestructibleId(int index) => FirstDestructible + (uint)index;
        public static uint UsableId(int index) => FirstUsable + (uint)index;

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var a in Attacks)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, 0, {a.MinDamage}, {a.MaxDamage} " +
                    $"FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
                yield return InsertCreature(c.Id, c.Comment, c.ClassId, 1, c.Level, c.MaxHp, c.NameId, c.RunSpeed, c.WalkSpeed, c.Action);

            foreach (var c in Combatants)
            {
                yield return InsertCreature(c.Id, c.Comment, c.ClassId, c.Faction, c.Level, c.MaxHp, c.NameId, c.Faction == 0 ? 7u : 0u, c.Faction == 0 ? 2u : 0u, c.Action);
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {c.Id}, 13, {c.Weapon}, 1 FROM creature x " +
                    $"WHERE x.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {c.Id} AND a.slot_id = 13);";

                if (c.Faction == 0)
                    yield return "INSERT INTO creature_loot_binding (map_context_id, creature_id, profile_key) " +
                        $"SELECT {Map}, {c.Id}, p.profile_key FROM loot_profile p WHERE p.profile_key = {S(ThraxLoot)} " +
                        $"AND EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id}) AND EXISTS (SELECT 1 FROM map_info m WHERE m.map_context_id = {Map}) " +
                        $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding b WHERE b.map_context_id = {Map} AND b.creature_id = {c.Id});";

                if (c.Body != 0)
                    yield return $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT {c.Id}, {c.Body}, {c.Body}, {c.Body}, {c.MaxHp}, {c.Armor} " +
                        $"FROM creature x WHERE x.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {c.Id});";
            }

            // Pierre after her rescue: her row and her looks as they are now.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"SELECT {RescuedPierre}, 'Sgt. Pierre - Alia Das hospital, after her rescue', class_id, faction, level, max_hp, name_id, 0, 0, " +
                "action1, action2, action3, action4, action5, action6, action7, action8 " +
                $"FROM creature c WHERE c.id = {Pierre} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {RescuedPierre});";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {RescuedPierre}, a.slot_id, a.Class_id, a.color " +
                $"FROM creature_appearance a WHERE a.id = {Pierre} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {RescuedPierre}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {RescuedPierre});";

            foreach (var r in Recasts)
                yield return $"UPDATE creature SET comment = {S(r.IsComment)}, class_id = {r.IsClass}, name_id = {r.IsName}, run_speed = {r.IsRun}, walk_speed = {r.IsWalk}, action1 = {r.IsAction} " +
                    $"WHERE id = {r.Creature} AND class_id = {r.WasClass} AND name_id = {r.WasName} AND run_speed = {r.WasRun} AND walk_speed = {r.WasWalk} AND action1 = {r.WasAction};";

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Was, look.Is))
                    yield return sql;

            foreach (var p in Pools)
                yield return InsertPool(p.Id, p.Creature, 0, 20, p.X, p.Y, p.Z, p.Rotation, 1, 0.0);

            yield return InsertPool(RescuedPierrePool, RescuedPierre, 0, 20, RescuedPierreX, RescuedPierreY, RescuedPierreZ, RescuedPierreRotation, 1, 0.0);

            foreach (var d in DropOffs)
                yield return InsertPool(d.Pool, d.Creature, DropshipAnimation, d.Respawn, d.X, d.Y, d.Z, d.Rotation, d.Count, DropRadius);

            foreach (var p in Pools.Where(p => p.Pose.HasValue))
                yield return AddPose(p.Id, p.Pose.Value);

            for (var i = 0; i < VictorBeat.Length; i++)
                yield return $"INSERT INTO spawnpool_patrol (pool_id, step, pos_x, pos_y, pos_z, facing, pause_ms) " +
                    $"SELECT {VictorPool}, {i}, {N(VictorBeat[i].X)}, {N(VictorBeat[i].Y)}, {N(VictorBeat[i].Z)}, NULL, {VictorBeat[i].PauseMs} " +
                    $"FROM spawnpool p WHERE p.id = {VictorPool} AND NOT EXISTS (SELECT 1 FROM spawnpool_patrol x WHERE x.pool_id = {VictorPool} AND x.step = {i});";

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.FromX, m.FromY, m.FromZ, m.FromRotation, m.ToX, m.ToY, m.ToZ, m.ToRotation);

            foreach (var r in Removed)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {r.Id} AND EXISTS (SELECT 1 FROM spawnpool p WHERE {Was(r)});";
                yield return $"DELETE FROM spawnpool WHERE {Was(r)};";
            }

            foreach (var l in Lines)
                yield return $"INSERT INTO npc_greeting (id, greeting_id, important) SELECT {l.Creature}, {l.GreetingId}, {(l.Important ? 1 : 0)} " +
                    $"FROM creature c WHERE c.id = {l.Creature} AND NOT EXISTS (SELECT 1 FROM npc_greeting x WHERE x.id = {l.Creature});";

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.FromX, i.FromY, i.FromZ, i.ToX, i.ToY, i.ToZ);

            foreach (var s in SceneMoves)
                yield return MoveSceneActor(s, s.From, s.To);

            for (var i = 0; i < Destructibles.Length; i++)
            {
                var d = Destructibles[i];
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {DestructibleId(i)}, {Map}, {d.ClassId}, {N(d.X)}, {N(d.Y)}, {N(d.Z)}, {N(d.Rotation)}, 0, 0, 0, {d.Side}, {S(d.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {d.ClassId} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {DestructibleId(i)});";
            }

            for (var i = 0; i < Usables.Length; i++)
            {
                var u = Usables[i];
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {UsableId(i)}, {Map}, {u.ClassId}, {N(u.X)}, {N(u.Y)}, {N(u.Z)}, {N(u.Rotation)}, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {UsableId(i)});";
            }

            foreach (var p in Pads)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {HumanTeleporter}, {LocalTeleporterType}, {S(p.Description)}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, 0, {Map} " +
                    $"FROM entityclass e WHERE e.id = {HumanTeleporter} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";

            foreach (var (from, to) in Links())
                yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) SELECT {from}, {to} FROM teleporter t " +
                    $"WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {to}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link l WHERE l.from_teleporter_id = {from});";

            yield return TurnStation(CraftingStationWas, CraftingStationIs);
        }

        public static IEnumerable<string> Down()
        {
            yield return TurnStation(CraftingStationIs, CraftingStationWas);

            foreach (var (from, to) in Links())
                yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";

            foreach (var p in Pads)
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {HumanTeleporter} AND type = {LocalTeleporterType} AND map_context_id = {Map};";

            yield return $"DELETE FROM world_usable WHERE id BETWEEN {FirstUsable} AND {UsableId(Usables.Length - 1)} AND class_id = {FirePit};";
            yield return $"DELETE FROM world_destructible WHERE id BETWEEN {FirstDestructible} AND {DestructibleId(Destructibles.Length - 1)};";

            foreach (var s in SceneMoves)
                yield return MoveSceneActor(s, s.To, s.From);

            foreach (var i in Indicators)
                yield return MoveIndicator(i, i.ToX, i.ToY, i.ToZ, i.FromX, i.FromY, i.FromZ);

            foreach (var l in Lines)
                yield return $"DELETE FROM npc_greeting WHERE id = {l.Creature} AND greeting_id = {l.GreetingId};";

            foreach (var r in Removed)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {r.Id}, 0, 0, {r.RespawnTime}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {Map}, {r.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {r.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {r.Id});";

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.ToX, m.ToY, m.ToZ, m.ToRotation, m.FromX, m.FromY, m.FromZ, m.FromRotation);

            yield return $"DELETE FROM spawnpool_patrol WHERE pool_id = {VictorPool};";

            yield return $"DELETE FROM spawnpool_pose WHERE id BETWEEN {FirstPool} AND {LastPool};";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool} AND creature_1_Id BETWEEN {FirstCreature} AND {LastCreature};";

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Is, look.Was))
                    yield return sql;

            foreach (var r in Recasts)
                yield return $"UPDATE creature SET comment = {S(r.WasComment)}, class_id = {r.WasClass}, name_id = {r.WasName}, run_speed = {r.WasRun}, walk_speed = {r.WasWalk}, action1 = {r.WasAction} " +
                    $"WHERE id = {r.Creature} AND class_id = {r.IsClass} AND name_id = {r.IsName} AND run_speed = {r.IsRun} AND walk_speed = {r.IsWalk} AND action1 = {r.IsAction};";

            yield return $"DELETE FROM creature_appearance WHERE id = {RescuedPierre};";
            yield return $"DELETE FROM creature WHERE id = {RescuedPierre} " +
                $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {RescuedPierre} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";

            foreach (var c in Combatants)
            {
                yield return $"DELETE FROM creature_loot_binding WHERE map_context_id = {Map} AND creature_id = {c.Id} AND profile_key = {S(ThraxLoot)};";
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {c.Id} AND slot_id = 13 AND Class_id = {c.Weapon};";
            }

            foreach (var id in Creatures.Select(c => c.Id).Concat(Combatants.Select(c => c.Id)))
                yield return $"DELETE FROM creature WHERE id = {id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";

            foreach (var a in Attacks)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature c WHERE {a.Id} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        private static string InsertCreature(uint id, string comment, uint classId, uint faction, uint level, uint maxHp, uint nameId, uint run, uint walk, uint action) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, {S(comment)}, {classId}, {faction}, {level}, {maxHp}, {nameId}, {run}, {walk}, {action}, 0, 0, 0, 0, 0, 0, 0 " +
            $"FROM entityclass e WHERE e.id = {classId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {id});";

        private static string InsertPool(uint id, uint creature, uint animType, uint respawn, double x, double y, double z, double rotation, uint count, double radius) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"SELECT {id}, 0, {animType}, {respawn}, {N(x)}, {N(y)}, {N(z)}, {N(rotation)}, {Map}, {creature}, {count}, {count}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(radius)} " +
            $"FROM creature c WHERE c.id = {creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {id});";

        private static string Was(OldPool r) =>
            $"id = {r.Id} AND creature_1_Id = {r.Creature} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_y - {N(r.Y)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01";

        private static string MoveSceneActor(SceneMove s, string from, string to) =>
            $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, {S(from)}, {S(to)}) " +
            $"WHERE mission_id = {s.Mission} AND content_revision = {S(Revision)} AND script_key = {S(s.Script)} " +
            $"AND bindings LIKE {S("%" + from + "%")};";

        private static string TurnStation(double from, double to) =>
            $"UPDATE kraftwerks SET rotation = {N(to)} WHERE id = {CraftingStation} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(CraftingStationX)}) < 0.01 AND ABS(pos_z - {N(CraftingStationZ)}) < 0.01 AND ABS(rotation - {N(from)}) < 0.01;";
    }
}
