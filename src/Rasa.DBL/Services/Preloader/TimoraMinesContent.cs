using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Timora Mines (adv_foreas_concordia_divide_timoramines, map 1348), the Divide's Bane fuel
    /// mine, peopled and fitted out as the walkthroughs tell it (Timora_mines). The missions it is
    /// the stage of are TimoraMinesMissionsV1's; what the place does - the drills' charges, the
    /// fuel lakes and the Main Fuel Pump's security, the escort, the Eloh Shelter's ambush - is the
    /// Game's TimoraMines.
    ///
    /// From the client: the map (the entrance camp and its field hospital, the Expeditionary
    /// Excavation with its pillboxes and pit, the Predator Bay, the industrial complex down to the
    /// Fuel Bore Grotto and its pool, the Supply Storage, the Fuel Bore Cavern's climb and bridge
    /// and its pool below, the Eloh Shelter's projector, the Fuel Egress's basin, and the Timora
    /// Command Center with its three consoles); the names (Cmd. Sgt. Simpson, Base Cmdr. Jayjack,
    /// Field Rangers Kearney, Morrow and Sanchez, Sergeant Pastre, the Timora Overseer, Wardmaster
    /// and Warden); the detonators, the Map Cipher, and the creature and object classes - the
    /// articulated drill, the raw fuel pump house, and the fuel depot's pump, fuel, fire and force
    /// field. From the wiki and Ten Ton Hammer's walkthrough: mob levels 18-20; Simpson in the
    /// Foreas Base command centre (-34.6, 121.1, 426.8); Kearney at the entrance (-136.9, 205.6,
    /// 518.2) with Pastre behind him and Morrow across from Pastre; the drills' charges at
    /// (-214.8, 201.5, 456.6), (-77.3, 199.6, 393.7), (-90.2, 194.6, 325.0), in the centre pit and
    /// at (118.8, 210.0, 111.6); a named Bane in the bunker at (-241.9, 217.9, 289.3); the Map
    /// Cipher in a crate across the Predator Bay at (-417, 220, 283); five fuel pumps in the
    /// grotto's pool and four in the cavern's below the bridge, with the Timora Overseer at the
    /// bottom; Sanchez in the Supply Storage (56.6, 145.6, 50.0); waves of Caretakers and of
    /// Technicians on the climb, and snipers above its first turn; the Eloh information in the
    /// cave right of the bridge; the Main Fuel Pump at (312.2, 204.9, -267.5) with three pools
    /// round it, a force field over a lake when security is on, and one console per lake in the
    /// Warden's lair; the Timora Warden at the Command Center, beyond the entrance at (414.8,
    /// 231.6, -206.1), and a transporter there back to the entrance.
    ///
    /// Ours, where neither says: where the rest stands (on the navmesh), the creatures' numbers,
    /// the pumps on the pools' five smoke vents in the grotto, the Wardmaster on the Fuel Egress's
    /// mining control terminal, Jayjack beside Simpson, the drills beside their charges, the lakes
    /// in the basin round the pump, and the classes: the Overseers Thrax rifle bosses, the
    /// Wardmaster a Thrax Technician boss, the Warden a Caretaker boss, the escort AFS infantry.
    ///
    /// The Divide seed's eight placeholder pools here (530051-530058) are left asleep (scripted,
    /// mode 2), as Torcastra's were. Kearney and Pastre move from the hospital, where Pastre stood
    /// on the medic.
    ///
    /// Ids: 569001 on - creatures and their actions 5690xx, pools 5691xx, usables 5692xx,
    /// destructibles 5693xx - the instances' next block after the Minos Caverns' 568xxx; the
    /// transporter's pads are teleporters 946 and 947. Plain SQL, valid on SQLite and MySQL alike.
    /// A row goes in only where its id is free, and a row changed only from what it was; Down
    /// takes out and puts back only as Up left them.
    /// </summary>
    public static class TimoraMinesContent
    {
        public const uint Map = 1348;
        public const uint Divide = 1148;

        // ---- Creatures --------------------------------------------------------------------

        public const uint Simpson = 569001, Jayjack = 569002, Morrow = 569003, Sanchez = 569004;
        public const uint Kearney = 510118, Pastre = 510119;

        public const uint ThraxSoldier = 569010, ThraxGrenadier = 569011, ThraxTechnician = 569012, Caretaker = 569013,
            ShieldDrone = 569014, Hunter = 569015, Predator = 569016;
        public const uint TimoraOverseer = 569020, TimoraWardmaster = 569021, TimoraWarden = 569022;
        public const uint EscortSoldier = 569030;

        /// <summary>The conversation package Kearney already has: Report to him, and his escort's briefing.</summary>
        public const uint KearneyPackage = 158;

        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, uint Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, uint LookFrom, params uint[] Actions);

        public const uint Hostile = 0, Friendly = 1;

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public static readonly Act[] Actions =
        {
            new(569001, "Thrax Soldier - Timora 1/1", 1, 1, 1.0, 20.0, 800, 0, 38, 57, 6),
            new(569002, "Thrax Soldier - Timora 397/1", 397, 1, 0.5, 3.0, 10000, 333, 23, 30, 1),
            new(569003, "Thrax Grenadier - Timora 1/229", 1, 229, 5.0, 15.0, 2500, 0, 43, 65, 2),
            new(569004, "Thrax Grenadier - Timora 1/79", 1, 79, 1.0, 15.0, 800, 0, 43, 65, 1),
            new(569005, "Thrax Technician - Timora 1/296", 1, 296, 1.0, 20.0, 800, 0, 58, 88, 5),
            new(569006, "Caretaker - Timora 395/1", 395, 1, 0.5, 10.0, 800, 0, 13, 19, 1),
            new(569007, "Hunter - Timora 1/244", 1, 244, 1.0, 20.0, 1500, 0, 43, 65, 1),
            new(569008, "Hunter - Timora 403/1", 403, 1, 1.0, 40.0, 10000, 900, 15, 29, 13),
            new(569009, "Shield Drone - Timora 396/1", 396, 1, 0.5, 3.0, 1000, 0, 32, 95, 1),
            new(569010, "Predator - Timora 1/85", 1, 85, 1.0, 50.0, 3000, 0, 117, 175, 6),
            new(569011, "Predator - Timora 407/1", 407, 1, 0.0, 0.0, 0, 0, 160, 240, 1),
            new(569012, "Timora Overseer 1/79", 1, 79, 0.5, 30.0, 800, 0, 17, 29, 1),
            new(569013, "Timora Overseer 449/1", 449, 1, 1.0, 40.0, 5000, 500, 95, 190, 13),
            new(569014, "Timora Wardmaster 1/296", 1, 296, 1.0, 20.0, 800, 0, 105, 158, 5),
            new(569015, "Timora Warden 395/3", 395, 3, 1.0, 40.0, 1000, 700, 85, 128, 1),
            new(569016, "AFS escort - Timora rifle 1/134", 1, 134, 1.0, 20.0, 800, 0, 30, 45, 1),
        };

        // The shared ability rows the Divide's and the Den's Bane already use: a Caretaker's heal and
        // revive (62029, 62030), a boss Caretaker's (62033, 62034), a Technician's heal, turret and
        // revive (62045, 65014, 68079), a Predator's scan (63011), a Hunter's pet (65035), a Shield
        // Drone's aura (45).
        public static readonly Beast[] Creatures =
        {
            new(Simpson, "Cmd. Sgt. Simpson - Foreas Base", 3846, Friendly, 22, 1000, 0, 127, 0, 0, 510063),
            new(Jayjack, "Base Cmdr. Jayjack - Foreas Base", 3846, Friendly, 25, 1000, 0, 124, 0, 0, 510052),
            new(Morrow, "Field Ranger Morrow - Timora Mines", 3848, Friendly, 19, 1000, 0, 3047, 0, 0, 510091),
            new(Sanchez, "Field Ranger Sanchez - Timora Mines", 3846, Friendly, 19, 1000, 0, 3046, 0, 0, 510110),

            new(ThraxSoldier, "Thrax Soldier - Timora", 20757, Hostile, 18, 1000, 75, 236, 9, 5, 0, 569001, 569002),
            new(ThraxGrenadier, "Thrax Grenadier - Timora", 9354, Hostile, 18, 1000, 75, 0, 9, 5, 0, 569003, 569004),
            new(ThraxTechnician, "Thrax Technician - Timora", 7043, Hostile, 19, 1250, 88, 8058, 9, 5, 0, 569005, 62045, 65014, 68079),
            new(Caretaker, "Caretaker - Timora", 9244, Hostile, 19, 1050, 78, 366, 9, 5, 0, 569006, 62029, 62030),
            new(ShieldDrone, "Shield Drone - Timora", 7233, Hostile, 18, 1100, 80, 438, 9, 5, 0, 569009, 45),
            new(Hunter, "Hunter - Timora", 10166, Hostile, 19, 1050, 78, 388, 9, 5, 0, 569007, 569008, 65035),
            new(Predator, "Predator - Timora Predator Bay", 3902, Hostile, 20, 2000, 105, 0, 9, 5, 0, 569010, 569011, 63011),

            new(TimoraOverseer, "Timora Overseer", 10504, Hostile, 20, 2600, 120, 20000011, 9, 5, 0, 569012, 569013),
            new(TimoraWardmaster, "Timora Wardmaster", 10502, Hostile, 20, 3200, 132, 20000013, 9, 5, 0, 569014, 62045, 65014, 68079),
            new(TimoraWarden, "Timora Warden - Command Center", 10321, Hostile, 22, 6400, 160, 20000012, 9, 5, 0, 569015, 62033, 62034),

            new(EscortSoldier, "AFS Infantryman - Timora escort", 29423, Friendly, 19, 1400, 80, 8716, 7, 5, 0, 569016),
        };

        // ---- Where things are ------------------------------------------------------------------

        public static readonly Vector3 SimpsonAt = new(-34.6f, 121.2f, 426.8f);
        public static readonly Vector3 JayjackAt = new(-40.0f, 121.28f, 426.0f);
        public static readonly Vector3 KearneyAt = new(-136.9f, 205.74f, 518.2f);
        public static readonly Vector3 PastreAt = new(-142.0f, 205.74f, 517.5f);
        public static readonly Vector3 MorrowAt = new(-142.0f, 205.74f, 508.0f);
        public static readonly Vector3 EscortAt = new(-132.0f, 205.74f, 509.0f);
        public static readonly Vector3 SanchezAt = new(56.6f, 145.74f, 50.0f);
        public static readonly Vector3 HologramAt = new(-48.0f, 180.92f, -56.0f);
        public static readonly Vector3 PumpAt = new(319.0f, 204.14f, -266.0f);
        public static readonly Vector3 WardenAt = new(486.0f, 231.87f, -206.0f);
        public static readonly Vector3 WardmasterAt = new(316.0f, 217.14f, -102.0f);
        public static readonly Vector3 OverseerBunkerAt = new(-241.9f, 218.18f, 289.3f);
        public static readonly Vector3 OverseerPoolAt = new(84.0f, 158.74f, -68.0f);
        public static readonly Vector3 CrateAt = new(-417.0f, 220.64f, 283.0f);

        /// <summary>The Kearney and Pastre spots the client seed gave them, by the field hospital, as the rows hold them.</summary>
        public static readonly (double X, double Y, double Z) KearneyWasAt = (-125.0999984741211, 205.74000549316406, 517.5999755859375),
            PastreWasAt = (-128.8000030517578, 205.74000549316406, 517.5999755859375);
        public const double KearneyRotation = Math.PI, PastreRotation = -2.38;

        // ---- Spawn pools ----------------------------------------------------------------------

        public const short Automatic = 0, Scripted = 2;
        public const uint Respawn = 1200, NamedRespawn = 3000, NpcRespawn = 300, OnceRespawn = 10;

        /// <summary>A pool: up to two kinds of creature, how many of each, and the spread they stand in.</summary>
        public sealed record Spot(uint Id, uint Map, Vector3 At, double Rotation, short Mode, uint Respawn, uint Creature, uint Count = 1,
            uint Creature2 = 0, uint Count2 = 0, double Radius = 0, string Comment = "");

        public const uint SimpsonPool = 569101, JayjackPool = 569102, MorrowPool = 569103, SanchezPool = 569104, EscortPool = 569105;
        public const uint OverseerBunkerPool = 569120, OverseerCavernPool = 569121, WardmasterPool = 569122, WardenPool = 569123;
        public const uint FirstAmbushPool = 569160, AmbushWaves = 3, PumpAmbushPool = 569178;

        /// <summary>The Eloh Shelter's three ambush waves, one pool each.</summary>
        public static uint AmbushPool(uint wave) => FirstAmbushPool + wave - 1;

        public static readonly Spot[] Pools =
        {
            // People.
            new(SimpsonPool, Divide, SimpsonAt, 1.98, Automatic, NpcRespawn, Simpson, Comment: "Cmd. Sgt. Simpson in the Foreas Base command centre"),
            new(JayjackPool, Divide, JayjackAt, 2.51, Automatic, NpcRespawn, Jayjack, Comment: "Base Cmdr. Jayjack in the Foreas Base command centre"),
            new(MorrowPool, Map, MorrowAt, -Math.PI / 2, Automatic, NpcRespawn, Morrow, Comment: "Field Ranger Morrow at the entrance camp, across from Pastre"),
            new(SanchezPool, Map, SanchezAt, 2.2, Automatic, NpcRespawn, Sanchez, Comment: "Field Ranger Sanchez in the Supply Storage"),
            new(EscortPool, Map, EscortAt, Math.PI, Scripted, OnceRespawn, EscortSoldier, 4, Radius: 2, Comment: "Central Bound Patrol: Kearney's escort, called by the Game"),

            // The Expeditionary Excavation, the bunker and the Predator Bay.
            new(569110, Map, new(-119.7f, 202.94f, 452.2f), 0, Automatic, Respawn, ThraxSoldier, 3, Radius: 5, Comment: "Excavation: the way down from the camp"),
            new(569111, Map, new(-170.0f, 201.71f, 458.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ShieldDrone, 1, 5, "Excavation: below the tunnel mouth"),
            new(569112, Map, new(-200.0f, 201.74f, 440.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Hunter, 1, 5, "Excavation: by the first drill"),
            new(569113, Map, new(-88.6f, 197.95f, 392.7f), 0, Automatic, Respawn, ThraxGrenadier, 2, ShieldDrone, 1, 5, "Excavation: by the second drill"),
            new(569114, Map, new(-73.3f, 187.94f, 293.1f), 0, Automatic, Respawn, ThraxTechnician, 2, ThraxSoldier, 1, 5, "Excavation: by the third drill"),
            new(569115, Map, new(-195.8f, 194.54f, 371.5f), 0, Automatic, Respawn, Caretaker, 1, ThraxSoldier, 2, 5, "Excavation: the west slope"),
            new(569116, Map, new(-140.0f, 159.92f, 372.0f), 0, Automatic, Respawn, Caretaker, 2, ThraxTechnician, 1, 6, "Excavation: the centre pit"),
            new(569117, Map, new(-170.0f, 187.28f, 310.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ThraxGrenadier, 1, 5, "Excavation: below the bunker"),
            new(OverseerBunkerPool, Map, OverseerBunkerAt, 0.6, Automatic, NamedRespawn, TimoraOverseer, Comment: "The Timora Overseer in the bunker up the hill"),
            new(569118, Map, new(-236.0f, 217.19f, 295.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Radius: 3, Comment: "The bunker's guard"),
            new(569130, Map, new(-330.0f, 216.14f, 300.0f), 0, Automatic, Respawn, Predator, Comment: "Predator Bay: by the launch pad"),
            new(569131, Map, new(-370.0f, 216.14f, 310.0f), 0, Automatic, Respawn, Predator, 1, ThraxSoldier, 2, 6, "Predator Bay: the far side"),
            new(569132, Map, new(-345.0f, 216.14f, 260.0f), 0, Automatic, Respawn, Predator, 1, ThraxGrenadier, 1, 6, "Predator Bay: south of the pad"),
            new(569133, Map, new(-405.0f, 223.28f, 295.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Hunter, 1, 4, "Predator Bay: by the crates across it"),

            // The industrial complex down to the grotto, and the grotto.
            new(569140, Map, new(-143.0f, 187.94f, 260.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ThraxTechnician, 1, 4, "The complex: its north hall"),
            new(569141, Map, new(-138.0f, 188.29f, 225.0f), 0, Automatic, Respawn, ThraxTechnician, 2, Caretaker, 1, 4, "The complex: its middle"),
            new(569142, Map, new(-112.0f, 187.74f, 182.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ThraxGrenadier, 1, 4, "The complex: its south end"),
            new(569143, Map, new(-85.6f, 145.74f, 112.8f), 0, Automatic, Respawn, ThraxSoldier, 3, ThraxTechnician, 1, 6, "Fuel Bore Grotto: among the pumps"),
            new(569144, Map, new(-120.0f, 145.74f, 70.0f), 0, Automatic, Respawn, Caretaker, 2, Radius: 5, Comment: "Fuel Bore Grotto: the west side"),
            new(569145, Map, new(-60.0f, 145.74f, 90.0f), 0, Automatic, Respawn, ThraxGrenadier, 2, ShieldDrone, 1, 5, "Fuel Bore Grotto: the east side"),

            // The Supply Storage.
            new(569150, Map, new(7.8f, 147.68f, 23.7f), 0, Automatic, Respawn, ThraxSoldier, 3, Radius: 5, Comment: "Supply Storage: the south hall"),
            new(569151, Map, new(13.0f, 145.74f, 97.0f), 0, Automatic, Respawn, ThraxTechnician, 2, ThraxSoldier, 1, 5, "Supply Storage: the north hall"),
            new(569152, Map, new(76.0f, 147.74f, 120.0f), 0, Automatic, Respawn, ThraxGrenadier, 2, Caretaker, 1, 5, "Supply Storage: the way out to the cavern"),

            // The Fuel Bore Cavern: the climb's waves, the snipers, the bridge, the pool below.
            new(569153, Map, new(154.0f, 148.18f, 51.2f), 0, Automatic, Respawn, Caretaker, 5, Radius: 6, Comment: "The climb: Caretakers"),
            new(569154, Map, new(177.6f, 161.54f, 14.0f), 0, Automatic, Respawn, ThraxTechnician, 5, Radius: 6, Comment: "The climb: Technicians"),
            new(569155, Map, new(195.2f, 177.94f, 45.6f), 0, Automatic, Respawn, Caretaker, 6, Radius: 6, Comment: "The climb: Caretakers"),
            new(569156, Map, new(178.0f, 206.34f, 103.6f), 0, Automatic, Respawn, ThraxTechnician, 6, Radius: 6, Comment: "The climb: Technicians, at the top"),
            new(569157, Map, new(190.4f, 199.14f, 89.6f), 0, Automatic, Respawn, Hunter, 2, Radius: 3, Comment: "The climb: snipers above the first turn"),
            new(569158, Map, new(129.0f, 201.54f, -25.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Hunter, 1, 5, "The cavern's bridge"),
            new(OverseerCavernPool, Map, OverseerPoolAt, 1.0, Automatic, NamedRespawn, TimoraOverseer, Comment: "The Timora Overseer at the bottom of the left path"),
            new(569159, Map, new(88.0f, 159.79f, -60.0f), 0, Automatic, Respawn, ThraxTechnician, 2, Radius: 3, Comment: "The Overseer's guard"),

            // The Eloh Shelter's ambush: three waves the Game calls one after the other.
            new(AmbushPool(1), Map, new(-20.0f, 179.94f, -56.0f), Math.PI / 2, Scripted, OnceRespawn, ThraxSoldier, 3, ThraxTechnician, 1, 4, "Eloh Shelter ambush: the first wave"),
            new(AmbushPool(2), Map, new(-40.0f, 179.94f, -44.0f), Math.PI, Scripted, OnceRespawn, Caretaker, 2, ThraxGrenadier, 2, 4, "Eloh Shelter ambush: the second wave"),
            new(AmbushPool(3), Map, new(-40.0f, 179.94f, -68.0f), 0, Scripted, OnceRespawn, Hunter, 2, ThraxSoldier, 2, 4, "Eloh Shelter ambush: the third wave"),

            // The Fuel Egress, the Wardmaster, the pump's guard and the ambush its blast calls.
            new(569171, Map, new(300.0f, 207.34f, -136.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ThraxGrenadier, 1, 5, "Fuel Egress: the upper floor, west"),
            new(569172, Map, new(330.0f, 207.44f, -150.0f), 0, Automatic, Respawn, ShieldDrone, 1, ThraxSoldier, 2, 5, "Fuel Egress: the upper floor, east"),
            new(569173, Map, new(290.0f, 202.88f, -196.0f), 0, Automatic, Respawn, Caretaker, 1, ThraxTechnician, 2, 5, "Fuel Egress: the trench, west"),
            new(569174, Map, new(316.0f, 202.85f, -208.0f), 0, Automatic, Respawn, ThraxSoldier, 3, Radius: 5, Comment: "Fuel Egress: the trench, east"),
            new(WardmasterPool, Map, WardmasterAt, Math.PI, Automatic, NamedRespawn, TimoraWardmaster, Comment: "The Timora Wardmaster at the mining control terminal"),
            new(569175, Map, new(324.0f, 217.24f, -98.0f), 0, Automatic, Respawn, ThraxTechnician, 2, Radius: 3, Comment: "The Wardmaster's guard"),
            new(569176, Map, new(290.0f, 207.84f, -280.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Caretaker, 1, 5, "The Main Fuel Pump's guard, west"),
            new(569177, Map, new(340.0f, 208.03f, -292.0f), 0, Automatic, Respawn, Hunter, 2, Radius: 4, Comment: "The Main Fuel Pump's guard, south"),
            new(PumpAmbushPool, Map, new(300.0f, 207.31f, -262.0f), 0, Scripted, OnceRespawn, ThraxSoldier, 3, ThraxGrenadier, 1, 4, "The Main Fuel Pump's blast: the ambush, west"),
            new(PumpAmbushPool + 1, Map, new(336.0f, 207.87f, -246.0f), 0, Scripted, OnceRespawn, Caretaker, 2, Hunter, 1, 4, "The Main Fuel Pump's blast: the ambush, east"),

            // The way up and the Command Center.
            new(569180, Map, new(376.0f, 231.35f, -225.0f), 0, Automatic, Respawn, ThraxSoldier, 2, ShieldDrone, 1, 4, "The way up to the Command Center"),
            new(569181, Map, new(440.0f, 233.74f, -200.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Caretaker, 1, 3, "The Command Center's corridor"),
            new(WardenPool, Map, WardenAt, 1.57, Automatic, NamedRespawn, TimoraWarden, Comment: "The Timora Warden in the Command Center"),
            new(569182, Map, new(470.0f, 231.82f, -190.0f), 0, Automatic, Respawn, ThraxSoldier, 2, Radius: 2, Comment: "The Command Center: the Warden's guard, north"),
            new(569183, Map, new(470.0f, 232.14f, -212.0f), 0, Automatic, Respawn, ThraxTechnician, 2, Radius: 2, Comment: "The Command Center: the Warden's guard, south"),
        };

        // ---- Existing rows changed ------------------------------------------------------------

        /// <summary>The Divide seed's placeholder pools here: set scripted, so nothing wakes them.</summary>
        public static readonly uint[] PlaceholderPools = Enumerable.Range(530051, 8).Select(id => (uint)id).ToArray();

        // ---- Usable and destructible objects ----------------------------------------------------

        public const uint DrillClass = 6318, PumpHouseClass = 10190, MainPumpClass = 12998, FuelClass = 12991,
            FireClass = 12993, FieldClass = 12997, ConsoleClass = 7468, ChargeClass = 7870, CrateClass = 11116, PadClass = 7895;

        public const uint DetonatorClass = 11263, DetonatorTemplate = 2550, MapCipherClass = 28654, MapCipherTemplate = 119011;

        public const uint TsOff = 55, TsOn = 56;

        /// <summary>A fuel depot field's states: up over its lake, and down (as Plateau's tanks are used).</summary>
        public const uint FieldUp = TsOff, FieldDown = TsOn;

        /// <summary>A charge: takes one detonator, goes off 5 s later, its blast reaching its drill and hurting whoever stayed beside it.</summary>
        public const uint ChargeFuseSeconds = 5, ChargeRadius = 10, ChargeDamageMin = 80, ChargeDamageMax = 120, ChargeResetSeconds = 30;

        public const uint DrillHitPoints = 20000, PumpHitPoints = 2500, MainPumpHitPoints = 50000, FuelHitPoints = 300;

        public sealed record Drill(uint Number, Vector3 Charge, Vector3 At, double Rotation, string Where);

        /// <summary>The five drills: four in the Expeditionary Excavation, the fifth on the cavern's climb, each with its charge.</summary>
        public static readonly Drill[] Drills =
        {
            new(1, new(-214.8f, 201.72f, 456.6f), new(-209.5f, 201.52f, 461.9f), -2.356, "the excavation's west side"),
            new(2, new(-77.3f, 199.84f, 393.7f), new(-77.3f, 199.82f, 401.2f), Math.PI, "the excavation's east side"),
            new(3, new(-90.2f, 194.88f, 325.0f), new(-82.7f, 192.0f, 325.0f), -Math.PI / 2, "the excavation's south side"),
            new(4, new(-146.0f, 158.86f, 378.7f), new(-146.0f, 158.93f, 386.2f), Math.PI, "the excavation's centre pit"),
            new(5, new(118.8f, 210.23f, 111.6f), new(121.67f, 210.52f, 118.53f), -2.749, "the Fuel Bore Cavern's climb"),
        };

        public const uint FirstDrill = 569301, FirstPumpHouse = 569311, MainPump = 569321, FirstFuel = 569331;
        public const uint FirstCharge = 569201, FirstField = 569211, FirstFire = 569221, FirstConsole = 569231, CipherCrate = 569241;

        public static uint DrillId(uint number) => FirstDrill + number - 1;
        public static uint ChargeId(uint number) => FirstCharge + number - 1;
        public static uint PumpHouseId(uint number) => FirstPumpHouse + number - 1;

        /// <summary>The nine fuel pumps: five in the grotto's pool, on its smoke vents, and four in the pool below the cavern's bridge.</summary>
        public static readonly (Vector3 At, double Rotation, string Where)[] PumpHouses =
        {
            (new(-94.5f, 145.74f, 77.0f), 0, "Fuel Bore Grotto"),
            (new(-112.0f, 145.75f, 88.5f), -0.785, "Fuel Bore Grotto"),
            (new(-69.5f, 145.81f, 124.0f), 3.142, "Fuel Bore Grotto"),
            (new(-107.5f, 145.74f, 113.0f), 3.142, "Fuel Bore Grotto"),
            (new(-98.0f, 145.74f, 125.0f), 1.571, "Fuel Bore Grotto"),
            (new(64.0f, 156.54f, -70.0f), 1.571, "the Fuel Bore Cavern's pool"),
            (new(100.0f, 156.54f, -74.0f), -1.571, "the Fuel Bore Cavern's pool"),
            (new(80.0f, 156.54f, -92.0f), 0, "the Fuel Bore Cavern's pool"),
            (new(66.0f, 156.54f, -52.0f), 3.142, "the Fuel Bore Cavern's pool"),
        };

        public sealed record Lake(uint Number, Vector3 At, Vector3 Console, double ConsoleRotation, string Where);

        /// <summary>The Main Fuel Pump's three fuel lakes, in the basin round it, and each one's console in the Command Center.</summary>
        public static readonly Lake[] Lakes =
        {
            new(1, new(306.7f, 204.6f, -262.0f), new(499.54f, 233.1f, -200.0f), 1.622, "west"),
            new(2, new(331.5f, 204.74f, -262.0f), new(479.91f, 233.1f, -180.0f), 0.042, "east"),
            new(3, new(320.0f, 204.93f, -249.0f), new(479.74f, 233.1f, -218.94f), 3.041, "north"),
        };

        public static uint FuelId(uint lake) => FirstFuel + lake - 1;
        public static uint FieldId(uint lake) => FirstField + lake - 1;
        public static uint FireId(uint lake) => FirstFire + lake - 1;
        public static uint ConsoleId(uint lake) => FirstConsole + lake - 1;

        public sealed record Usable(uint Id, uint ClassId, Vector3 At, double Rotation, uint State, bool IsUsable, string Comment,
            uint ItemTemplateId = 0, uint ItemClassId = 0, uint ItemCount = 0, uint ResetSeconds = 0, uint IntervalMinSeconds = 0,
            uint DamageMin = 0, uint DamageMax = 0, double Radius = 0);

        public sealed record Destructible(uint Id, uint ClassId, Vector3 At, double Rotation, uint HitPoints, string Comment);

        public static readonly Usable[] Usables = BuildUsables().ToArray();

        private static IEnumerable<Usable> BuildUsables()
        {
            foreach (var drill in Drills)
                yield return new(ChargeId(drill.Number), ChargeClass, drill.Charge, drill.Rotation, 0, true,
                    $"Timora Mines: the charge at drill {drill.Number}, {drill.Where}: takes a detonator",
                    ItemClassId: DetonatorClass, ItemCount: 1, ResetSeconds: ChargeResetSeconds, IntervalMinSeconds: ChargeFuseSeconds,
                    DamageMin: ChargeDamageMin, DamageMax: ChargeDamageMax, Radius: ChargeRadius);

            foreach (var lake in Lakes)
            {
                yield return new(FieldId(lake.Number), FieldClass, lake.At, 0, FieldDown, false,
                    $"Timora Mines: the security field over the {lake.Where} fuel lake, down until security engages");
                yield return new(FireId(lake.Number), FireClass, lake.At, 0, TsOff, false,
                    $"Timora Mines: the fire on the {lake.Where} fuel lake, out until it is lit");
                yield return new(ConsoleId(lake.Number), ConsoleClass, lake.Console, lake.ConsoleRotation, TsOff, true,
                    $"Timora Command Center: the {lake.Where} fuel lake's security console");
            }

            yield return new(CipherCrate, CrateClass, CrateAt, 1.2, 0, true,
                "Timora Mines: the crate across the Predator Bay with the Map Cipher (Stolen History)",
                ItemTemplateId: MapCipherTemplate, ItemCount: 1);
        }

        public static readonly Destructible[] Destructibles = BuildDestructibles().ToArray();

        private static IEnumerable<Destructible> BuildDestructibles()
        {
            foreach (var drill in Drills)
                yield return new(DrillId(drill.Number), DrillClass, drill.At, drill.Rotation, DrillHitPoints,
                    $"Timora Mines: Bane mining drill {drill.Number}, {drill.Where}");

            for (var i = 0; i < PumpHouses.Length; i++)
                yield return new(PumpHouseId((uint)i + 1), PumpHouseClass, PumpHouses[i].At, PumpHouses[i].Rotation, PumpHitPoints,
                    $"Timora Mines: fuel pump {i + 1}, {PumpHouses[i].Where}");

            yield return new(MainPump, MainPumpClass, PumpAt, 0, MainPumpHitPoints, "Timora Mines: the Main Fuel Pump, in the Fuel Egress basin");

            foreach (var lake in Lakes)
                yield return new(FuelId(lake.Number), FuelClass, lake.At, 0, FuelHitPoints, $"Timora Mines: the {lake.Where} fuel lake");
        }

        // ---- The transporter -------------------------------------------------------------------

        public const uint LocalTeleporterType = 1;
        public const uint CommandCenterPad = 946, EntrancePad = 947;

        public sealed record Pad(uint Id, Vector3 At, double Rotation, string Description);

        public static readonly Pad[] Pads =
        {
            new(CommandCenterPad, new(462.0f, 232.14f, -184.0f), Math.PI, "Local Teleporter: Timora Command Center, back to the entrance"),
            new(EntrancePad, new(-152.0f, 205.74f, 500.0f), Math.PI, "Local Teleporter: Timora Mines entrance, from the Command Center"),
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
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = Enumerable.Range(0, 8).Select(i => i < c.Actions.Length ? c.Actions[i] : 0U).ToArray();
                yield return $"INSERT INTO creature ({CreatureColumns}) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                if (c.Actions.Length > 0)
                    yield return $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} " +
                        $"FROM creature x WHERE x.id = {c.Id} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {c.Id});";

                if (c.LookFrom != 0)
                    yield return CopyLook(c.Id, c.LookFrom);
            }

            foreach (var p in Pools)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " + Slots + ", radius) " +
                    $"SELECT {p.Id}, {p.Mode}, 0, {p.Respawn}, {N(p.At.X)}, {N(p.At.Y)}, {N(p.At.Z)}, {N(p.Rotation)}, {p.Map}, " +
                    $"{p.Creature}, {p.Count}, {p.Count}, {p.Creature2}, {p.Count2}, {p.Count2}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            yield return $"UPDATE spawnpool SET mode = {Scripted} WHERE id IN ({string.Join(", ", PlaceholderPools)}) AND map_context_id = {Map} AND mode = {Automatic};";
            yield return Move(Kearney, (KearneyWasAt.X, KearneyWasAt.Z), (KearneyAt.X, KearneyAt.Y, KearneyAt.Z), 0, KearneyRotation);
            yield return Move(Pastre, (PastreWasAt.X, PastreWasAt.Z), (PastreAt.X, PastreAt.Y, PastreAt.Z), 0, PastreRotation);

            foreach (var u in Usables)
                yield return "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, " +
                    "item_count, reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                    $"SELECT {u.Id}, {Map}, {u.ClassId}, {N(u.At.X)}, {N(u.At.Y)}, {N(u.At.Z)}, {N(u.Rotation)}, {u.State}, {(u.IsUsable ? 1 : 0)}, {u.ItemTemplateId}, {u.ItemClassId}, " +
                    $"{u.ItemCount}, {u.ResetSeconds}, {u.IntervalMinSeconds}, 0, {u.DamageMin}, {u.DamageMax}, {N(u.Radius)}, 0, {S(u.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {u.ClassId} AND NOT EXISTS (SELECT 1 FROM world_usable x WHERE x.id = {u.Id});";

            foreach (var d in Destructibles)
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {d.Id}, {Map}, {d.ClassId}, {N(d.At.X)}, {N(d.At.Y)}, {N(d.At.Z)}, {N(d.Rotation)}, {d.HitPoints}, 0, 0, 0, {S(d.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {d.ClassId} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {d.Id});";

            foreach (var p in Pads)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {PadClass}, {LocalTeleporterType}, {S(p.Description)}, {N(p.At.X)}, {N(p.At.Y)}, {N(p.At.Z)}, {N(p.Rotation)}, {Map} " +
                    $"FROM entityclass e WHERE e.id = {PadClass} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";

            // One way: the Command Center's pad to the entrance's.
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
                $"SELECT {CommandCenterPad}, {EntrancePad} FROM teleporter a, teleporter b " +
                $"WHERE a.id = {CommandCenterPad} AND a.type = {LocalTeleporterType} AND a.map_context_id = {Map} " +
                $"AND b.id = {EntrancePad} AND b.type = {LocalTeleporterType} AND b.map_context_id = {Map} " +
                $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {CommandCenterPad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {CommandCenterPad} AND to_teleporter_id = {EntrancePad};";

            foreach (var p in Pads)
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {PadClass} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {Map} AND ABS(pos_x - {N(p.At.X)}) < 0.01 AND ABS(pos_z - {N(p.At.Z)}) < 0.01;";

            foreach (var d in Destructibles)
                yield return $"DELETE FROM world_destructible WHERE id = {d.Id} AND map_context_id = {Map} AND class_id = {d.ClassId};";

            foreach (var u in Usables)
                yield return $"DELETE FROM world_usable WHERE id = {u.Id} AND map_context_id = {Map} AND class_id = {u.ClassId};";

            yield return Move(Pastre, (PastreAt.X, PastreAt.Z), PastreWasAt, PastreRotation, 0);
            yield return Move(Kearney, (KearneyAt.X, KearneyAt.Z), KearneyWasAt, KearneyRotation, 0);
            yield return $"UPDATE spawnpool SET mode = {Automatic} WHERE id IN ({string.Join(", ", PlaceholderPools)}) AND map_context_id = {Map} AND mode = {Scripted};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND map_context_id = {p.Map} AND creature_1_Id = {p.Creature};";

            foreach (var c in Creatures)
            {
                if (c.LookFrom != 0)
                    yield return $"DELETE FROM creature_appearance WHERE id = {c.Id};";
                if (c.Actions.Length > 0)
                    yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {InPool(c.Id)});";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id} " +
                    "AND NOT EXISTS (SELECT 1 FROM creature c WHERE " +
                    $"{a.Id} IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8));";
        }

        private static string CopyLook(uint creature, uint from) =>
            $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {creature}, a.slot_id, a.Class_id, a.color " +
            $"FROM creature_appearance a WHERE a.id = {from} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {creature}) " +
            $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {creature});";

        private static string Move(uint pool, (double X, double Z) from, (double X, double Y, double Z) to, double fromRotation, double toRotation) =>
            $"UPDATE spawnpool SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, rotation = {N(toRotation)} " +
            $"WHERE id = {pool} AND map_context_id = {Map} AND creature_1_Id = {pool} " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01 AND ABS(rotation - {N(fromRotation)}) < 0.01;";

        internal static string S(string value) => "'" + value.Replace("'", "''") + "'";

        internal static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        internal static string N(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
