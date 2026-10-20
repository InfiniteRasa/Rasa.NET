using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Alia Das (foreas_concordia_wilderness, 1220) put right after a look round in game
    /// (Alia_das_touchups).
    ///
    /// Looks. The NPC clothing's "Officer 3 helmet" (NPC_Clothing_Officer_3_Helmet,
    /// npc_hum_m/f_officer_helmet_v03) is a beret, not a helmet: Lt. Saviours's "hat" and Engineer
    /// Salter's missing helmet were both that beret, and it is what Training Officer Kincaid has
    /// now, in black. The other helmets are the avatar armours' own:
    ///  - Dr. Elise Corman: the Corman's short hooded robe (CreatureSwap_Corman_Soldier_1, suit
    ///    "NPC Corman 1 Female", avatar_hum_f_npc_corman_v01) in brown, hood down. A Corman woman
    ///    with no clothing rows has no clothes at all.
    ///  - The Field Medic: the plain white uniform of the client's figures at the terminals
    ///    (ambient_f_standing_console_v01 is npc_hum_f_officer_v02: Officer_2's tunic), with
    ///    Officer_3's plain trousers and boots. No cap, so her red hair shows.
    ///  - Lt. Saviours: his light armour in #4d6080, no helmet, brown hair, and the palest skin the
    ///    client's human palette has short of grey (palette_skintone_human: 231, 224, 216).
    ///  - Supply Sergeant Caruso: her uniform in #212636, Hair 02 (swept back), and the armour
    ///    vendor's package (42), so the armour sign is over her head; she sells Supply Clerk
    ///    Bradshaw's armour as well as her own stock. Bradshaw (pool 136) is taken away.
    ///  - Supply Sergeant Maddrey: his uniform in #583627 and Hair 01 (a forelock to the front).
    ///  - Quartermaster Caufield and Arms Supplier Oliver: Clothing_Helmet_3, the helmet of the
    ///    Soldier_1 armour they wear (the suits share their mesh), in the armour's green. Caufield
    ///    moves to where the GM stood.
    ///  - Engineer Salter: the Motor Assist 03 light armour (Clothing_Armor_Light_03,
    ///    avatar_hum_m_armor_light_v03), whose trousers and boots are strapped round with a
    ///    harness, in his brown, and its helmet in silver.
    ///
    /// Places:
    ///  - The light turret on the tripod by the command tent faced the tripod's way, and in game
    ///    that is backwards for this one: it is turned half round. A second turret goes on the
    ///    empty tripod at the boargar meadow west of the outpost (660.93, 437.11; the tripod's
    ///    top at 288.91, facing as it does, 0).
    ///  - The Forean Gunner Trainee walks an 11 m circle round the flag on the ground, halfway
    ///    between the flag's ring of sandbags (out to about 5.7 m) and the tents (the hospital's
    ///    north end at 17 m, the armoury's east end at 19 m), at the terrain's height.
    ///  - Footlockers: a footlocker's box (prop_hum_footlocker.geo: local x -0.41 to 0.41, z -0.05
    ///    to 0.71) lies, as seen in game, toward (-sin r, -cos r) of its point: the one at the
    ///    cot at 869.0, 399.0 (r pi/2) lay in the dresser west of it, and the barracks one at
    ///    379.98 (r 3.18) stood out into the aisle. The barracks one between the cots at 758.1 and
    ///    761.9 goes back against the tent wall as the seed's footlocker (1) stands (379.3, r 0);
    ///    the one by the cot at 755.25, 385.0 turns parallel to the cot (the cot's 1.52 and a half
    ///    turn), 0.1 m clear of the dresser behind it; and the two in the tent behind the command
    ///    tent that were in the dressers beside them go into the middle of the gaps between
    ///    dresser and cot.
    ///  - The CELLAR Arena link has nothing to show it at either end (it is a map link: a circle
    ///    on the floor). A waypoint pad (UsableTwoStateHumWaypointV01, the Battlegrounds' team
    ///    teleporters' pad) stands on each: a teleporter row of type 1, which the server puts in
    ///    the world in its on state and nothing else. A local teleporter is gained by nobody and
    ///    opens no window, and these two have no local_teleporter_link row, so the pad takes
    ///    nobody anywhere.
    ///    Their ids, 990 and 991, are under 1000 as every placed teleporter's is: the travel
    ///    window keeps waypoints and wormholes in one list by id, and a wormhole's is its entity
    ///    id, which begins at 1000.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, every change guarded on the row as it was, so
    /// what a GM changed by hand since is left alone; Down undoes only what is still as Up left it.
    /// </summary>
    public static class AliaDasTouchups
    {
        public static readonly uint RobeBrown = Hue(0x7A, 0x58, 0x38);
        public static readonly uint SavioursBlue = Hue(0x4D, 0x60, 0x80);
        public static readonly uint CarusoBlue = Hue(0x21, 0x26, 0x36);
        public static readonly uint MaddreyBrown = Hue(0x58, 0x36, 0x27);
        public static readonly uint PalestSkin = Hue(231, 224, 216);
        public static readonly uint HairBrown = Hue(82, 52, 32);            // charactercreationwindow.py kDefaultColorHair
        public static readonly uint LightBrown = AliaDasNpcs.LightBrown;

        public const uint CormanGloves = 4384, CormanBoots = 4385, CormanTorso = 4386, CormanLegs = 4387;
        public const uint Helmet3 = 4008;
        public const uint Light03Helm = 10911, Light03Gloves = 10912, Light03Boots = 10913, Light03Legs = 10914, Light03Torso = 10915;
        public const uint Hair01 = 3672, Hair02 = 3663, Hair05 = 9781, Hair06 = 9782, Hair09 = 25255;

        public const uint Elise = 510004, FieldMedic = 640018, Saviours = 120, Bradshaw = 72, BradshawPool = 136;
        public const uint CaufieldPool = 210, LightTurret = 550017, TurretPool = 640024, MeadowTurretPool = 640025, TurretRespawn = 3000;

        /// <summary>The armour vendor's package, and Bradshaw's stock.</summary>
        public const uint ArmourPackage = 42, AmmoPackage = 104;
        public static readonly uint[] ArmourStock = { 11561, 11564, 11565, 12267, 12446, 13066, 13096, 13126, 13156, 13186 };

        private static Slot[] SeedOfficerLook() => new[]
        {
            new Slot(2, OfficerBoots, OfficerBlue), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair05, DarkHair),
            new Slot(15, OfficerTunic, OfficerBlue), new Slot(16, OfficerLegs, OfficerBlue), new Slot(17, 24008, SkinLight),
        };

        private static Slot[] Green(uint hair, uint face, uint skin) => new[]
        {
            new Slot(2, SoldierBoots, TrouserBrown), new Slot(3, SoldierGloves, AliaDasNpcs.Green), new Slot(13, PistolRedshirt, 1), new Slot(14, hair, DarkHair),
            new Slot(15, SoldierVest, AliaDasNpcs.Green), new Slot(16, OfficerLegs, TrouserBrown), new Slot(17, face, skin),
        };

        private static Slot[] WithHelmet(Slot[] look, uint helmet, uint hue) => new[] { new Slot(1, helmet, hue) }.Concat(look).ToArray();

        public static readonly Look[] Looks =
        {
            new(Elise, "Dr. Elise Corman: the Corman's short robe in brown, hood down",
                new[] { new Slot(14, Hair06, WildernessPostNpcs.BlondHair), new Slot(17, 24009, SkinLight) },
                new[] { new Slot(2, CormanBoots, RobeBrown), new Slot(3, CormanGloves, RobeBrown), new Slot(14, Hair06, WildernessPostNpcs.BlondHair),
                        new Slot(15, CormanTorso, RobeBrown), new Slot(16, CormanLegs, RobeBrown), new Slot(17, 24009, SkinLight) }),
            new(FieldMedic, "Field Medic: the terminal workers' plain white uniform",
                new[] { new Slot(2, MedicBoots, White), new Slot(3, MedicGloves, White), new Slot(14, Hair09, WildernessPostNpcs.RedHair),
                        new Slot(15, MedicTorso, White), new Slot(16, MedicLegs, WildernessPostNpcs.PaleGreen), new Slot(17, 24011, SkinLight) },
                new[] { new Slot(2, Officer3Boots, White), new Slot(14, Hair09, WildernessPostNpcs.RedHair),
                        new Slot(15, Officer2Vest, White), new Slot(16, Officer3Legs, White), new Slot(17, 24011, SkinLight) }),
            new(Saviours, "Lt. Saviours: #4d6080 light armour, no helmet, brown hair, palest skin",
                new[] { new Slot(1, Officer3Helmet, SkinMedium), new Slot(2, MedicBoots, OfficerBlue), new Slot(3, MedicGloves, OfficerBlue), new Slot(13, RifleAvatar, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, MedicTorso, OfficerBlue), new Slot(16, MedicLegs, OfficerBlue), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(2, MedicBoots, SavioursBlue), new Slot(3, MedicGloves, SavioursBlue), new Slot(13, RifleAvatar, 1),
                        new Slot(14, Hair05, HairBrown), new Slot(15, MedicTorso, SavioursBlue), new Slot(16, MedicLegs, SavioursBlue), new Slot(17, 24008, PalestSkin) }),
            new(Kincaid, "Training Officer Kincaid: a black beret",
                new[] { new Slot(2, OfficerBoots, Black), new Slot(3, Officer4Gloves, Black), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair05, DarkHair),
                        new Slot(15, Officer4Vest, Silver), new Slot(16, OfficerLegs, Black), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Officer3Helmet, Black), new Slot(2, OfficerBoots, Black), new Slot(3, Officer4Gloves, Black), new Slot(13, PistolRedshirt, 1),
                        new Slot(14, Hair05, DarkHair), new Slot(15, Officer4Vest, Silver), new Slot(16, OfficerLegs, Black), new Slot(17, 24008, SkinLight) }),
            new(Caruso, "Supply Sergeant Caruso: #212636 uniform, hair swept back", SeedOfficerLook(),
                new[] { new Slot(2, OfficerBoots, CarusoBlue), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair02, DarkHair),
                        new Slot(15, OfficerTunic, CarusoBlue), new Slot(16, OfficerLegs, CarusoBlue), new Slot(17, 24008, SkinLight) }),
            new(Maddrey, "Supply Sergeant Maddrey: #583627 uniform, a forelock to the front",
                new[] { new Slot(2, OfficerBoots, LightBrown), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair02, DarkHair),
                        new Slot(15, OfficerTunic, LightBrown), new Slot(16, OfficerLegs, LightBrown), new Slot(17, 24020, SkinMedium) },
                new[] { new Slot(2, OfficerBoots, MaddreyBrown), new Slot(13, PistolRedshirt, 1), new Slot(14, Hair01, DarkHair),
                        new Slot(15, OfficerTunic, MaddreyBrown), new Slot(16, OfficerLegs, MaddreyBrown), new Slot(17, 24020, SkinMedium) }),
            new(Caufield, "Quartermaster Caufield: a green helmet", Green(Hair05, 24008, SkinLight),
                WithHelmet(Green(Hair05, 24008, SkinLight), Helmet3, AliaDasNpcs.Green)),
            new(Oliver, "Arms Supplier Oliver: a green helmet", Green(Hair02, 24020, SkinMedium),
                WithHelmet(Green(Hair02, 24020, SkinMedium), Helmet3, AliaDasNpcs.Green)),
            new(Salter, "Engineer Salter: Motor Assist 03 light armour in brown, harnessed, silver helmet",
                new[] { new Slot(1, Officer3Helmet, Silver), new Slot(2, Officer3Boots, Brown), new Slot(3, Officer3Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, Hair05, DarkHair), new Slot(15, Officer3Vest, Brown),
                        new Slot(16, Officer3Legs, Brown), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Light03Helm, Silver), new Slot(2, Light03Boots, Brown), new Slot(3, Light03Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, Hair05, DarkHair), new Slot(15, Light03Torso, Brown),
                        new Slot(16, Light03Legs, Brown), new Slot(17, 24008, SkinLight) }),
        };

        public static readonly Move[] Moves =
        {
            new(CaufieldPool, Caufield, "Quartermaster Caufield", 764.582, 293.9688, 406.6211, 4.5945, 765.4727, 293.9688, 405.9531, 4.6165),
            // Turned only, half round.
            new(TurretPool, LightTurret, "AFS Light Turret by the command tent", 851.69, 295.44, 396.77, -1.7811, 851.69, 295.44, 396.77, 1.3605),
            new(CirclingGunnerPool, GunnerTrainee, "Forean Gunner Trainee round the flag", 786.0, 294.92, 395.5, 1.5708, 786.0, 293.82, 402.0, 1.5708),
        };

        public static readonly OldPool[] Removed =
        {
            new(BradshawPool, Bradshaw, 20, 758.96875, 293.96875, 399.7539, 5.3),
        };

        /// <summary>The meadow tripod's turret.</summary>
        public const double MeadowTurretX = 660.93, MeadowTurretY = 288.91, MeadowTurretZ = 437.11, MeadowTurretRotation = 0.0;

        /// <summary>11 m round the flag (786, 391), the same way round as before, on the terrain.</summary>
        public const double CircleRadius = 11.0;

        public static readonly Step[] CircleBeat =
        {
            new(786.0, 293.82, 402.0, null, 0),
            new(788.847, 293.82, 401.6252, null, 0),
            new(791.5, 293.82, 400.5263, null, 0),
            new(793.7782, 294.06, 398.7782, null, 0),
            new(795.5263, 294.06, 396.5, null, 0),
            new(796.6252, 294.19, 393.847, null, 0),
            new(797.0, 294.25, 391.0, null, 0),
            new(796.6252, 294.25, 388.153, null, 0),
            new(795.5263, 294.13, 385.5, null, 0),
            new(793.7782, 294.25, 383.2218, null, 0),
            new(791.5, 294.37, 381.4737, null, 0),
            new(788.847, 294.25, 380.3748, null, 0),
            new(786.0, 294.25, 380.0, null, 0),
            new(783.153, 294.25, 380.3748, null, 0),
            new(780.5, 294.13, 381.4737, null, 0),
            new(778.2218, 294.0, 383.2218, null, 0),
            new(776.4737, 294.0, 385.5, null, 0),
            new(775.3748, 294.25, 388.153, null, 0),
            new(775.0, 294.13, 391.0, null, 0),
            new(775.3748, 293.82, 393.847, null, 0),
            new(776.4737, 293.58, 396.5, null, 0),
            new(778.2218, 293.45, 398.7782, null, 0),
            new(780.5, 293.45, 400.5263, null, 0),
            new(783.153, 293.7, 401.6252, null, 0),
        };

        public sealed record LockerMove(uint Id, string Why, double FromX, double FromY, double FromZ, double FromRotation,
            double ToX, double ToY, double ToZ, double ToRotation);

        public static readonly LockerMove[] Lockers =
        {
            new(640001, "Barracks: back against the tent wall, as the seed's footlocker 1",
                760.0469, 294.2109, 379.9805, 3.1821, 759.98, 294.2109, 379.3, 0.0),
            new(640004, "Barracks: parallel to the cot at 755.25, 385.0",
                756.2461, 294.2109, 384.8359, 4.4561, 756.2461, 294.2109, 384.74, 4.6616),
            new(640017, "Cot tent: between the dresser at 866.26 and the cot at 869.0",
                867.02, 294.22, 399.0, 1.5708, 867.66, 294.22, 399.0, 1.5708),
            new(640018, "Cot tent: between the dresser at 871.55 and the cot at 874.71",
                872.73, 294.22, 399.01, 1.5708, 873.16, 294.22, 399.01, 1.5708),
        };

        public sealed record Pad(uint Id, uint MapContextId, double X, double Y, double Z, double Rotation, string Description);

        /// <summary>The waypoint pad, and the teleporter type that only shows it.</summary>
        public const uint PadClass = 25651, LocalTeleporterType = 1;

        public static readonly Pad[] Pads =
        {
            new(990, Map, 749.6406, 294.2109, 382.5547, 0.0, "CELLAR Arena link: Alia Das end"),
            new(991, 20000009, 13.1962, 36.5, -4.4361, 0.0, "CELLAR Arena link: arena end"),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Was, look.Is))
                    yield return sql;

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.FromX, m.FromY, m.FromZ, m.FromRotation, m.ToX, m.ToY, m.ToZ, m.ToRotation);

            for (var i = 0; i < CircleBeat.Length; i++)
                yield return MoveStep(i, AliaDasNpcs.CircleBeat[i], CircleBeat[i]);

            foreach (var r in Removed)
            {
                yield return $"DELETE FROM spawnpool_pose WHERE id = {r.Id} AND EXISTS (SELECT 1 FROM spawnpool p WHERE {Was(r)});";
                yield return $"DELETE FROM spawnpool WHERE {Was(r)};";
            }

            yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"SELECT {MeadowTurretPool}, 0, 0, {TurretRespawn}, {N(MeadowTurretX)}, {N(MeadowTurretY)}, {N(MeadowTurretZ)}, {N(MeadowTurretRotation)}, {Map}, " +
                $"{LightTurret}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                $"FROM creature c WHERE c.id = {LightTurret} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {MeadowTurretPool});";

            yield return $"UPDATE vendor SET package_id = {ArmourPackage} WHERE id = {Caruso} AND package_id = {AmmoPackage};";

            foreach (var template in ArmourStock)
                yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {Caruso}, {template} FROM itemtemplate t " +
                    $"WHERE t.id = {template} AND EXISTS (SELECT 1 FROM vendor v WHERE v.id = {Caruso} AND v.package_id = {ArmourPackage}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {Caruso} AND x.item_template_id = {template});";

            foreach (var l in Lockers)
                yield return MoveLocker(l, l.FromX, l.FromY, l.FromZ, l.FromRotation, l.ToX, l.ToY, l.ToZ, l.ToRotation);

            foreach (var p in Pads)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {PadClass}, {LocalTeleporterType}, {S(p.Description)}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.MapContextId} " +
                    $"FROM entityclass e WHERE e.id = {PadClass} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var p in Pads)
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {PadClass} AND type = {LocalTeleporterType} AND map_context_id = {p.MapContextId};";

            foreach (var l in Lockers)
                yield return MoveLocker(l, l.ToX, l.ToY, l.ToZ, l.ToRotation, l.FromX, l.FromY, l.FromZ, l.FromRotation);

            yield return $"DELETE FROM vendor_item WHERE id = {Caruso} AND item_template_id IN ({string.Join(", ", ArmourStock)}) " +
                $"AND EXISTS (SELECT 1 FROM vendor v WHERE v.id = {Caruso} AND v.package_id = {ArmourPackage});";

            yield return $"UPDATE vendor SET package_id = {AmmoPackage} WHERE id = {Caruso} AND package_id = {ArmourPackage};";

            yield return $"DELETE FROM spawnpool WHERE id = {MeadowTurretPool} AND creature_1_Id = {LightTurret};";

            foreach (var r in Removed)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {r.Id}, 0, 0, {r.RespawnTime}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {Map}, {r.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {r.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {r.Id});";

            for (var i = 0; i < CircleBeat.Length; i++)
                yield return MoveStep(i, CircleBeat[i], AliaDasNpcs.CircleBeat[i]);

            foreach (var m in Moves)
                yield return MovePool(m.Pool, m.Creature, m.ToX, m.ToY, m.ToZ, m.ToRotation, m.FromX, m.FromY, m.FromZ, m.FromRotation);

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Is, look.Was))
                    yield return sql;
        }

        private static string MoveStep(int step, Step from, Step to) =>
            $"UPDATE spawnpool_patrol SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE pool_id = {CirclingGunnerPool} AND step = {step} " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_y - {N(from.Y)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01;";

        private static string MoveLocker(LockerMove l, double fromX, double fromY, double fromZ, double fromRotation,
            double toX, double toY, double toZ, double toRotation) =>
            $"UPDATE footlocker SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)}, rotation = {N(toRotation)} " +
            $"WHERE id = {l.Id} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01 AND ABS(rotation - {N(fromRotation)}) < 0.01;";

        private static string Was(OldPool r) =>
            $"id = {r.Id} AND creature_1_Id = {r.Creature} AND map_context_id = {Map} " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_y - {N(r.Y)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01";
    }
}
