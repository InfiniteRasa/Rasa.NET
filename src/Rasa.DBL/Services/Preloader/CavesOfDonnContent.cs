using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// The Caves of Donn (adv_foreas_concordia_wilderness_cavesofdonn02, 1506) as the 1.16.5
    /// client has it after the August 2008 rework, with the TaRapedia walkthrough's fights: its
    /// people, its Bane, its egg clusters and its six missions, in the client's own names and
    /// texts.
    ///
    /// Who and what:
    ///  - Lieutenant Stone at the entrance, who gives Too Quiet; Elder Nekala at Mar Fend on the
    ///    hill, who gives The Reason Why; the Dying Forean in the burned village below it, who
    ///    gives What Evil Lurks; Sergeant Napoli, lost in the tunnel behind the second cave-in,
    ///    who gives Bugged to Death. The Lost Foreans of Spirit Cave are the instance's to put
    ///    down and walk out (CavesOfDonn).
    ///  - The Bane at the walkthrough's places, levels 9 to 11: the pincer hill at the Eloh
    ///    Chantry around Excavator Xang, the mortar bowl at Undone Fairway, the plateau at the
    ///    Vitiated Shrine, Fithiks in the tunnels between. The two ambushes of Too Quiet and
    ///    Scavenger D'rak are the instance's.
    ///  - Six Fithik egg clusters in the tunnels, for Bugged to Death (world_destructible).
    ///  - The missions: To the Caves (Council Elder Nula, Ranja Gorge) and Logos: Feeling, Trap,
    ///    Heal (Receptive Liaison Standley, Twin Pillars) outside; Too Quiet, What Evil Lurks,
    ///    The Reason Why and Bugged to Death inside. Body Count (691), which the rework retired,
    ///    goes from the story group of Wilderness Targets of Opportunity (1449, objective 55).
    /// The usables - braziers, cave-ins, the fallen soldier, the distress signal, the gate, the
    /// chamber's door, its corpses, reliquary, projector and teleporter - and the scenario events
    /// the missions wait for are the server's, per copy of the instance (CavesOfDonn).
    /// </summary>
    public static class CavesOfDonnContent
    {
        public const string Revision = "donn_1_16_5";
        public const uint Donn = 1506, Wilderness = 1220;

        /// <summary>missioncategorylanguage: "Instance (Caves of Donn)", and the Wilderness battlefield's.</summary>
        public const uint Category = 10000045, WildernessCategory = 10000044;

        public const uint ToTheCaves = 859, LogosFeelingTrapHeal = 923, TooQuiet = 674, WhatEvilLurks = 692,
            TheReasonWhy = 693, BuggedToDeath = 2004, BodyCount = 691;

        public static readonly uint[] Missions = { ToTheCaves, LogosFeelingTrapHeal, TooQuiet, WhatEvilLurks, TheReasonWhy, BuggedToDeath };

        /// <summary>The scenario of every mission whose events the instance records, and its events.</summary>
        public const uint Scenario = 1;
        public const uint Arrived = 1;
        public const uint HillScouted = 1, FirstAmbushBeaten = 2, SecondAmbushBeaten = 3, SignalFound = 4, ReachedPyramid = 5;
        public const uint CaveOpened = 1, LostFound = 2, Extracted = 3, SurvivorLost = 4, XangDown = 5;
        public const uint FirstRuin = 1, SecondRuin = 2, FinalRuin = 3, GateOpened = 4, ChamberFound = 5,
            ReliquaryOpened = 6, DrakDown = 7;

        /// <summary>The Reason Why's statues are a counter of their own: a scenario whose events are the three, 1 to 3.</summary>
        public const uint StatueScenario = 2;
        public const uint FirstStatue = 1, SecondStatue = 2, ThirdStatue = 3;

        /// <summary>The Logos of the three tests (logos table), in their order.</summary>
        public const uint TrapLogos = 55, FeelingLogos = 13, HealLogos = 17;

        // People.
        public const uint Stone = 566001, Napoli = 566002, Nekala = 566003, DyingForean = 566004,
            LostForeanLeader = 566005, LostForean = 566006, Drak = 566007;

        // Bane, levels 9 to 11.
        public const uint Soldier = 566010, Grenadier = 566011, ForeanMachina = 566012, HominisMachina = 566013,
            ShieldDrone = 566014, Technician = 566015, Lightbender = 566016, Fithik = 566017, MachinaChampion = 566018;

        /// <summary>The Wilderness's Excavator Xang (his spawn pool is the map's already), and the Ranja egg clusters' hatchling.</summary>
        public const uint Xang = 520011, Hatchling = 552001;

        public const uint Nula = 93, Standley = 134;

        /// <summary>Conversation packages: Nekala, the Dying Forean, the Lost Foreans, the Eloh Projector.</summary>
        public const uint NekalaPackage = 2582, DyingForeanPackage = 610, LostForeanPackage = 611, ProjectorPackage = 1308;

        public const uint StoneRifle = 566101, NapoliRifle = 566102;
        public const uint StonePool = 566201, NapoliPool = 566202, NekalaPool = 566203, DyingForeanPool = 566204;

        /// <summary>Items: CavesDonn_Bomb, the three Mirrored Eloh Statues, the Eloh Artifact Excavation Orders.</summary>
        public const uint BombTemplate = 122844, OrdersTemplate = 2367;
        public static readonly uint[] StatueTemplates = { 122840, 122841, 122842 };

        // Usables.
        public const uint BrazierClass = 7636, CaveInClass = 29434, FallenSoldierClass = 7865, SignalClass = 7592,
            GateClass = 29764, ChamberDoorClass = 10605, ShamanCorpseClass = 21563, ReliquaryClass = 10308,
            ProjectorClass = 23110, TeleporterClass = 20823, EggClusterClass = 10180;

        /// <summary>usablenameoverride: CAVESDONN_FALLEN_SOLDIER, CAVESDONN_DISTRESS_SIGNAL, HOLOGRAM_PLATFORM.</summary>
        public const uint FallenSoldierName = 336, SignalName = 335, TeleporterName = 126;

        /// <summary>missionobjectiveindicatorlanguage: the client's own marker names this instance has a use for.</summary>
        public const uint LostPartyMarker = 100, DistressSignalMarker = 101, ElohRuinsMarker = 121, LogosPuzzleMarker = 344,
            ElohChamberMarker = 384, CaveInMarker = 439;

        // Where things are, on the instance's navmesh (x, height, z).
        public static readonly Vector3 Entrance = new(-275.6f, 24.0f, -363f);
        public static readonly Vector3 WildernessEntrance = new(-438.3519f, 164.9596f, 260.4733f);
        public static readonly Vector3 StoneAtEntrance = new(-279.6f, 23.25f, -331.6f);
        public static readonly Vector3 StoneAtMarFend = new(-310f, 21.21f, -232f);
        public static readonly Vector3 StoneAtVillage = new(-296f, 8.66f, -220f);
        public static readonly Vector3 StoneAtPyramid = new(-295.55f, 9.01f, -180.34f);
        public static readonly Vector3 NekalaAtMarFend = new(-313f, 21.65f, -224f);
        public static readonly Vector3 NekalaAtEntrance = new(-283.2f, 25.25f, -354.8f);
        public static readonly Vector3 HillBottom = new(-292.8f, 18.45f, -258f);
        public static readonly Vector3 MarFend = new(-322f, 21.56f, -228f);
        public static readonly Vector3[] FirstAmbushSpots =
        {
            new(-316f, 21.58f, -232f), new(-328f, 21.62f, -232f), new(-322f, 21.65f, -238f),
            new(-312f, 21.65f, -226f), new(-332f, 21.51f, -226f), new(-318f, 21.65f, -238.4f)
        };
        public static readonly Vector3 Village = new(-298f, 7.54f, -238f);
        public static readonly Vector3[] SecondAmbushSpots =
        {
            new(-306f, 7.48f, -248f), new(-292f, 8.71f, -234f), new(-300f, 7.62f, -230f),
            new(-290.4f, 8.45f, -244.4f), new(-303.6f, 7.84f, -238f)
        };
        public static readonly Vector3 DistressSignal = new(-293.33f, 7.25f, -212.1f);
        public static readonly Vector3 DyingForeanSpot = new(-288.68f, 7.25f, -210.16f);
        public static readonly Vector3 Pyramid = new(-295.55f, 9.01f, -180.34f);
        public static readonly Vector3 FallenSoldier = new(-276.1f, 7.5f, -176.9f);
        public static readonly Vector3[] LostForeanSpots = { new(-191f, 6.21f, -145f), new(-195f, 6.23f, -149f), new(-187f, 6.26f, -150f) };
        public static readonly Vector3 SpiritCave = new(-191f, 6.21f, -145f);
        public static readonly Vector3 Extraction = new(-205f, 10.45f, -230f);
        public static readonly Vector3 Chantry = new(-160.2f, 36.0f, -320f);
        public static readonly Vector3 NapoliSpot = new(-84f, 17.62f, -296f);
        public static readonly Vector3 Fairway = new(7.5f, 22.2f, -232f);
        public static readonly Vector3 VitiatedShrine = new(160f, 4.8f, -302f);
        public static readonly Vector3 Gate = new(170f, 1f, -302f);
        public static readonly Vector3 ChamberDoor = new(254f, 5f, -302f);
        public static readonly Vector3 Chamber = new(278f, 5.05f, -302f);
        public static readonly Vector3 Reliquary = new(278f, 5.05f, -302f);
        public static readonly Vector3 Projector = new(288f, 9.24f, -292f);
        public static readonly Vector3 Teleporter = new(278f, 5.2f, -319f);
        public static readonly Vector3 Corridor = new(-320f, 25.06f, -359f);
        public static readonly Vector3[] StatueSpots = { new(264f, 5.05f, -316f), new(292f, 5.05f, -316f), new(266f, 5.08f, -288f) };
        public static readonly Vector3 DrakSpot = new(184f, 1.43f, -276f);
        public static readonly Vector3[] DrakGuardSpots = { new(180f, 1.39f, -280f), new(188f, 1.5f, -280f) };

        /// <summary>A cave-in: where it stands, its turn, and what it blocks.</summary>
        public sealed record CaveIn(string Key, Vector3 Position, float Yaw, string Comment);

        /// <summary>
        /// The three cave-ins: the mouth of Spirit Cave (What Evil Lurks' bomb), the tunnel from
        /// the Eloh Chantry to Undone Fairway (Napoli behind it) and the tunnel on to the
        /// Vitiated Shrine. Each stands across its tunnel, which runs east and west.
        /// </summary>
        public static readonly CaveIn[] CaveIns =
        {
            new("donn:cave-in-spirit-cave", new Vector3(-263.2f, 7.45f, -170f), MathF.PI / 2, "the mouth of Spirit Cave"),
            new("donn:cave-in-chantry", new Vector3(-97.2f, 16.65f, -291.2f), MathF.PI / 2, "the tunnel from the Eloh Chantry to Undone Fairway"),
            new("donn:cave-in-fairway", new Vector3(83.89f, 20.66f, -246.78f), MathF.PI / 2, "the tunnel from Undone Fairway to the Vitiated Shrine")
        };

        /// <summary>A test: the site's Logos, its ruin's event, its braziers in the order they are to be lit.</summary>
        public sealed record Test(string Name, uint Logos, uint RuinEvent, Vector3 Site, Vector3[] Braziers);

        /// <summary>
        /// The three tests, in the order Elder Nekala gives them: Mar Fend's (Trap), the Eloh
        /// Chantry's (Feeling, on its four shrines) and Undone Fairway's (Heal). Each site's four
        /// braziers are listed in the order that lights them; one out of turn puts them all out.
        /// </summary>
        public static readonly Test[] Tests =
        {
            new("Mar Fend", TrapLogos, FirstRuin, new Vector3(-326.88f, 21.9f, -220.82f), new[]
            {
                new Vector3(-321f, 21.53f, -214f), new Vector3(-336f, 21.5f, -227f),
                new Vector3(-332f, 21.62f, -214f), new Vector3(-317f, 21.5f, -227f)
            }),
            new("the Eloh Chantry", FeelingLogos, SecondRuin, new Vector3(-160.24f, 36.1f, -325.6f), new[]
            {
                new Vector3(-163.7f, 29.75f, -296.8f), new Vector3(-165.4f, 32.42f, -309.6f),
                new Vector3(-156.72f, 25.49f, -297.76f), new Vector3(-156.8f, 32.76f, -309.9f)
            }),
            new("Undone Fairway", HealLogos, FinalRuin, new Vector3(7.64f, 22.2f, -232.33f), new[]
            {
                new Vector3(9.34f, 22.14f, -242.98f), new Vector3(0.5f, 21.67f, -222f),
                new Vector3(9f, 22.08f, -221f), new Vector3(0.5f, 21.65f, -243f)
            })
        };

        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, OjasaHiveContent.TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>A spawn pool: its creature, how many, where and how far they stand from it, and how long they take to come back.</summary>
        public sealed record Spot(uint Id, uint Creature, uint Count, double X, double Y, double Z, double Rotation, double Radius,
            uint RespawnSeconds, string Comment);

        public static readonly Act[] Actions =
        {
            new(StoneRifle, "Lieutenant Stone rifle 1/134", OjasaHiveContent.RifleAttack, OjasaHiveContent.RifleArg, 1, 25, 1000, 0, 30, 55, 1),
            new(NapoliRifle, "Sergeant Napoli rifle 1/134", OjasaHiveContent.RifleAttack, OjasaHiveContent.RifleArg, 1, 25, 1000, 0, 30, 55, 1)
        };

        private const OjasaHiveContent.TargetCategoryRow Hostile = OjasaHiveContent.TargetCategoryRow.Hostile;
        private const OjasaHiveContent.TargetCategoryRow Friendly = OjasaHiveContent.TargetCategoryRow.Friendly;

        /// <summary>
        /// The people and the Bane. The Bane keep the Wilderness rows' actions (Thrax Soldier 3,
        /// Grenadier 57, Forean Machina 630100, Hominis Machina 8, Shield Drone 85, Technician 47,
        /// Lightbender 630071, Fithik 1, Lt. Casper 520012 and Xang 520011) at the walkthrough's
        /// levels.
        /// </summary>
        public static readonly Beast[] Creatures =
        {
            new(Stone, "Lieutenant Stone - Caves of Donn", SwapsetMale, Friendly, 11, 3000, 60, 6696, 7, 3, StoneRifle),
            new(Napoli, "Sergeant Napoli - Caves of Donn", SwapsetMale, Friendly, 11, 3000, 60, 6697, 0, 0, NapoliRifle),
            new(Nekala, "Elder Nekala - Caves of Donn", 7035, Friendly, 12, 3000, 40, 9186, 7, 3),
            new(DyingForean, "Dying Forean - Caves of Donn", 7035, Friendly, 10, 1000, 0, 9729, 0, 0),
            new(LostForeanLeader, "Lost Forean - Caves of Donn, the one who speaks", 7034, Friendly, 9, 900, 30, 6740, 7, 3),
            new(LostForean, "Lost Forean - Caves of Donn", 7036, Friendly, 9, 700, 30, 6740, 7, 3),
            new(Drak, "Scavenger D'rak - Caves of Donn", 10504, Hostile, 11, 2400, 60, 10159, 9, 5, 34, 57012, 61011),
            new(Soldier, "Thrax Soldier - Caves of Donn", 20757, Hostile, 10, 800, 40, 0, 9, 5, 70001, 76032),
            new(Grenadier, "Thrax Grenadier - Caves of Donn", 9354, Hostile, 10, 750, 40, 0, 9, 5, 19, 20),
            new(ForeanMachina, "Forean Machina - Caves of Donn", 6236, Hostile, 9, 800, 35, 0, 9, 5, 630103, 62059),
            new(HominisMachina, "Hominis Machina - Caves of Donn", 3868, Hostile, 10, 850, 40, 0, 9, 5, 72002, 62059),
            new(ShieldDrone, "Bane Shield Drone - Caves of Donn", 7233, Hostile, 10, 900, 50, 8122, 9, 5, 42, 45),
            new(Technician, "Thrax Technician - Caves of Donn", 11320, Hostile, 10, 700, 30, 0, 9, 5, 70002, 70003, 62050, 65004),
            new(Lightbender, "Lightbender - Caves of Donn", 7120, Hostile, 10, 750, 35, 0, 9, 5, 30, 54012),
            new(Fithik, "Fithik - Caves of Donn", 4313, Hostile, 9, 450, 20, 0, 9, 5, 72001, 60008),
            new(MachinaChampion, "Hominis Machina champion - Caves of Donn ambush", 21474, Hostile, 11, 1800, 55, 0, 9, 5, 18, 62060)
        };

        /// <summary>Stone and Napoli are AFS, in the kit of a lieutenant and a sergeant of the Wilderness's outposts.</summary>
        public static readonly (uint Creature, uint LooksLike)[] Looks = { (Stone, 510107), (Napoli, 510046) };

        public static readonly Spot[] Pools =
        {
            new(StonePool, Stone, 1, StoneAtEntrance.X, StoneAtEntrance.Y, StoneAtEntrance.Z, 3.1, 0, 20, "Lieutenant Stone at the entrance"),
            new(NapoliPool, Napoli, 1, NapoliSpot.X, NapoliSpot.Y, NapoliSpot.Z, 1.57, 0, 20, "Sergeant Napoli behind the second cave-in"),
            new(NekalaPool, Nekala, 1, NekalaAtMarFend.X, NekalaAtMarFend.Y, NekalaAtMarFend.Z, 2.0, 0, 20, "Elder Nekala at Mar Fend"),
            new(DyingForeanPool, DyingForean, 1, DyingForeanSpot.X, DyingForeanSpot.Y, DyingForeanSpot.Z, 4.2, 0, 20, "the Dying Forean in the burned village"),

            // Spirit Cave and its southern passage: insects.
            new(566210, Fithik, 2, -199.2, 10.45, -199.2, 0, 4, 2700, "Fithiks in Spirit Cave"),
            new(566211, Fithik, 2, -212, 8.47, -232, 0, 4, 2700, "Fithiks in Spirit Cave's southern passage"),

            // The pincer hill, the Eloh Chantry: Xang's guard.
            new(566220, ShieldDrone, 1, -160, 34.96, -318, 0, 0, 2700, "the Chantry's shield drone"),
            new(566221, Technician, 1, -164.8, 33.45, -312, 0, 0, 2700, "a Chantry healer"),
            new(566222, Technician, 1, -155, 32.23, -307, 0, 0, 2700, "a Chantry healer"),
            new(566223, Soldier, 1, -150, 28.01, -294, 0, 0, 2700, "a Chantry soldier"),
            new(566224, Soldier, 1, -151, 26.25, -281, 0, 0, 2700, "a Chantry soldier"),
            new(566225, Grenadier, 1, -170, 30.57, -300, 0, 0, 2700, "a Chantry grenadier"),
            new(566226, ForeanMachina, 1, -186, 30.33, -316, 0, 0, 2700, "a Chantry Machina"),
            new(566227, HominisMachina, 1, -191, 22.35, -289, 0, 0, 2700, "a Chantry Machina"),
            new(566228, Lightbender, 1, -150, 23.53, -274, 0, 0, 2700, "a Chantry Lightbender"),
            new(566229, Lightbender, 1, -145, 17.65, -266, 0, 0, 2700, "a Chantry Lightbender"),
            new(566230, Lightbender, 1, -186.91, 16.74, -273.04, 0, 0, 2700, "a Chantry Lightbender"),
            new(566231, Lightbender, 1, -184, 15.21, -263, 0, 0, 2700, "a Chantry Lightbender"),

            // The tunnel to Undone Fairway: insects.
            new(566240, Fithik, 2, -130, 15.15, -287.03, 0, 4, 2700, "Fithiks in the Chantry tunnel"),
            new(566241, Fithik, 2, -70, 16.42, -294, 0, 4, 2700, "Fithiks in the Chantry tunnel"),

            // The mortar bowl, Undone Fairway: the lip and the far tunnel's mouth.
            new(566250, Soldier, 1, -20, 20.82, -272, 0, 0, 2700, "a Fairway soldier"),
            new(566251, Soldier, 1, -17.17, 21.97, -284.98, 0, 0, 2700, "a Fairway soldier"),
            new(566252, Grenadier, 1, 2, 31.19, -300, 0, 0, 2700, "a Fairway grenadier on the lip"),
            new(566253, ForeanMachina, 1, 31, 31.65, -281, 0, 0, 2700, "a Fairway Machina on the lip"),
            new(566254, HominisMachina, 1, -3, 28.45, -302, 0, 0, 2700, "a Fairway Machina on the lip"),
            new(566255, Lightbender, 1, 57, 20.85, -248, 0, 0, 2700, "a Lightbender at the far tunnel"),
            new(566256, Lightbender, 1, 62, 20.65, -247.6, 0, 0, 2700, "a Lightbender at the far tunnel"),

            // The plateau, the Vitiated Shrine.
            new(566260, ShieldDrone, 1, 150, 1.25, -302, 0, 0, 2700, "the shrine's shield drone"),
            new(566261, Soldier, 1, 140.33, 1.48, -289.98, 0, 0, 2700, "a shrine soldier"),
            new(566262, Soldier, 1, 140, 1.28, -315, 0, 0, 2700, "a shrine soldier"),
            new(566263, Soldier, 1, 190, 1.58, -285, 0, 0, 2700, "a shrine soldier"),
            new(566264, Technician, 1, 186, 1.64, -302, 0, 0, 2700, "a shrine healer"),
            new(566265, ForeanMachina, 1, 155, 1.45, -330, 0, 0, 2700, "a shrine Machina"),
            new(566266, HominisMachina, 1, 155, 1.27, -275, 0, 0, 2700, "a shrine Machina")
        };

        /// <summary>spawnpool_pose's Standing (NpcPose): at its post and facing, not strolling.</summary>
        public const uint StandingPose = 1;

        /// <summary>The NPCs stand at their posts; Stone is moved by the instance.</summary>
        public static readonly uint[] StandingPools = { NapoliPool, NekalaPool, DyingForeanPool };

        /// <summary>An egg cluster: its world_destructible row and where it stands.</summary>
        public sealed record EggCluster(uint Id, double X, double Y, double Z, string Comment);

        /// <summary>Bugged to Death's six: two in each tunnel. Its event is its place in this list, from 1.</summary>
        public static readonly EggCluster[] EggClusters =
        {
            new(566501, -200.29, 9.19, -188.82, "Spirit Cave"),
            new(566502, -221, 15.54, -238, "Spirit Cave's southern passage"),
            new(566503, -123, 16.08, -289, "the Chantry tunnel"),
            new(566504, -60, 14.39, -293, "the Chantry tunnel, past the cave-in"),
            new(566505, 88.8, 19.59, -248, "the shrine tunnel"),
            new(566506, 106, 6.25, -259.6, "the shrine tunnel")
        };

        /// <summary>The event of an egg cluster's row, or 0.</summary>
        public static uint EggClusterEvent(uint rowId)
        {
            for (var i = 0; i < EggClusters.Length; i++)
                if (EggClusters[i].Id == rowId)
                    return (uint)i + 1;

            return 0;
        }

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

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            foreach (var (creature, looksLike) in Looks)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {creature}, a.slot_id, a.Class_id, a.color " +
                    $"FROM creature_appearance a WHERE a.id = {looksLike} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {creature}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {creature});";

            foreach (var (id, package, comment) in Packages)
                yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {id}, {package}, {S(comment)} " +
                    $"FROM entityclass e WHERE e.id = {SwapsetMale} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {id});";

            foreach (var p in Pools)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Id}, 0, 0, {p.RespawnSeconds}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {Donn}, {p.Creature}, {p.Count}, {p.Count}, " +
                    $"0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {N(p.Radius)} " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            foreach (var pool in StandingPools)
                yield return AddPose(pool, StandingPose);

            foreach (var egg in EggClusters)
                yield return "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                    $"SELECT {egg.Id}, {Donn}, {EggClusterClass}, {N(egg.X)}, {N(egg.Y)}, {N(egg.Z)}, 0, 0, {Hatchling}, 2, {WorldDestructibleEntry.SideDefault}, " +
                    $"{S("Caves of Donn: Fithik egg cluster in " + egg.Comment)} " +
                    $"FROM entityclass e WHERE e.id = {EggClusterClass} AND NOT EXISTS (SELECT 1 FROM world_destructible x WHERE x.id = {egg.Id});";

            foreach (var sql in MissionRows())
                yield return sql;

            yield return TargetsOfOpportunityBinding(StoryGroupsWithout(BodyCount));
        }

        public static IEnumerable<string> Down()
        {
            yield return TargetsOfOpportunityBinding(null);

            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var egg in EggClusters)
                yield return $"DELETE FROM world_destructible WHERE id = {egg.Id} AND class_id = {EggClusterClass};";

            foreach (var pool in StandingPools)
                yield return $"DELETE FROM spawnpool_pose WHERE id = {pool};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Creature};";

            foreach (var (id, package, _) in Packages)
                yield return $"DELETE FROM npc_package WHERE id = {id} AND package_id = {package};";

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

        private static readonly (uint Id, uint Package, string Comment)[] Packages =
        {
            (Nekala, NekalaPackage, "Elder Nekala - Caves of Donn"),
            (DyingForean, DyingForeanPackage, "Dying Forean - Caves of Donn"),
            (LostForeanLeader, LostForeanPackage, "Lost Forean - Caves of Donn")
        };

        #region Targets of Opportunity

        /// <summary>The story groups of 1449's objective 55, one mission left out.</summary>
        public static IReadOnlyList<IReadOnlyList<uint>> StoryGroupsWithout(uint missionId) =>
            TargetsOfOpportunitySeed.OneOffs.WildernessScene().ObjectiveHistoryAggregations[55].Groups
                .Where(group => !group.Contains(missionId)).ToList();

        /// <summary>
        /// 1449's binding as the Wilderness's one-off titles leave it, with objective 55's story
        /// groups given (null: as they were).
        /// </summary>
        public static MissionSceneDefinition TargetsOfOpportunityScene(IReadOnlyList<IReadOnlyList<uint>> storyGroups)
        {
            var scene = TargetsOfOpportunitySeed.OneOffs.WildernessScene();

            if (storyGroups != null)
                scene.ObjectiveHistoryAggregations[55] = new MissionHistoryAggregation(storyGroups,
                    scene.ObjectiveHistoryAggregations[55].CounterId);

            return scene;
        }

        /// <summary>That binding, written whole: it is one document.</summary>
        public static string TargetsOfOpportunityBinding(IReadOnlyList<IReadOnlyList<uint>> storyGroups)
        {
            var json = JsonSerializer.Serialize(TargetsOfOpportunityScene(storyGroups), MissionContentCodec.Options);

            return $"UPDATE mission_scene_binding SET bindings = {InstanceMissionSql.S(json)} " +
                $"WHERE mission_id = {WildernessTargetsKillRules.MissionId} AND content_revision = {InstanceMissionSql.S(WildernessTargetsKillRules.Revision)};";
        }

        #endregion

        #region Missions

        private const string Wiki = "https://tabularasa.fandom.com/wiki/Caves_of_Donn";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            ToTheCavesRows(sql);
            LogosRows(sql);
            TooQuietRows(sql);
            WhatEvilLurksRows(sql);
            TheReasonWhyRows(sql);
            BuggedToDeathRows(sql);
            return sql;
        }

        private static InstanceMissionSql.RewardItem Item(uint template, uint quantity = 1) => new(template, quantity);

        /// <summary>Nula sends the player to the caves; being in them is the recon, radioed in.</summary>
        private static void ToTheCavesRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ToTheCaves, "To the Caves", 4470, Nula, null, 9, WildernessCategory);
            mission.Reward(5000, 1500, selectableItems: new[] { Item(45047, 3), Item(111023, 3) });
            mission.Objective(1, 4474, 8072, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, Arrived, counter: Scenario);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Arrived at the Caves of Donn", Arrived);
            mission.Indicator(1, MissionIndicator.UnnamedFrom + 1, WildernessEntrance.X, WildernessEntrance.Y, WildernessEntrance.Z);
            mission.RadioTurnIn();
            Evidence(mission, "Council Elder Nula93 at Ranja Gorge asks for a recon of the caves; entering the instance1506 records " +
                "event1 and HQ is radioed (the finishing text is HQ's). Wiki reward 5000 XP, 1500 credits, a Class II Advanced Med Pack45047 " +
                "or Class II EMP Grenade111023, three of either.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>Standley's three Logos of the caves: each learned, or already known, is its objective.</summary>
        private static void LogosRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LogosFeelingTrapHeal, "Logos: Feeling, Trap, Heal", 4928, Standley, Standley, 10, WildernessCategory);
            mission.Reward(20000, 3750);
            var objectives = new (uint Objective, uint Name, uint Body, uint Logos, Test Test)[]
            {
                (4, 4932, 6305, FeelingLogos, Tests[1]), (5, 4933, 6306, TrapLogos, Tests[0]), (6, 16990, 16991, HealLogos, Tests[2])
            };
            uint ordinal = 1;
            foreach (var (objective, name, body, logos, test) in objectives)
            {
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.LogosAcquired, logos);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, LogosPuzzleMarker, test.Site.X, test.Site.Y, test.Site.Z);
            }
            Evidence(mission, "Standley134 at Twin Pillars wants evidence of the three Logos terminals; each Logos acquired (Feeling13, " +
                "Trap55, Heal17) completes its objective, and one already known counts. The terminals wake when their test is passed " +
                "(CavesOfDonn braziers). Wiki reward 20000 XP, 3750 credits.");
            mission.Enable(new MissionSceneDefinition
            {
                Requirement = new CustomRequirement("character.starting-experience-completed"),
                ExistingFactObjectiveIds = objectives.Select(entry => entry.Objective).ToList()
            });
        }

        /// <summary>
        /// Stone's scouting: the bottom of the hill, the ambush at Mar Fend, Nekala, the second
        /// ambush in the burned village, the distress signal, the Dying Forean and the Pyramid,
        /// one after the other; HQ is radioed at the end.
        /// </summary>
        private static void TooQuietRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TooQuiet, "Too Quiet", 2817, Stone, null, 9, Category);
            mission.Reward(10000, 2000, selectableItems: new[] { Item(121310), Item(13356) });
            var steps = new (uint Objective, uint Name, uint Body)[]
            {
                (3, 3021, 7519), (4, 3028, 7520), (5, 21321, 21322), (8, 21644, 21645), (9, 21646, 21647), (10, 21653, 21654), (11, 21690, 21691)
            };
            for (var i = 0; i < steps.Length; i++)
                mission.Objective(steps[i].Objective, steps[i].Name, steps[i].Body, (uint)i + 1,
                    i == 0 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);

            void Step(uint objective, uint next, uint eventId)
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: Scenario);
                mission.RevealAndActivate(objective, next);
            }

            Step(3, 4, HillScouted);
            Step(4, 5, FirstAmbushBeaten);
            mission.Transition(5);
            mission.Conversation(5, NekalaPackage);
            mission.RevealAndActivate(5, 8);
            Step(8, 9, SecondAmbushBeaten);
            Step(9, 10, SignalFound);
            mission.Transition(10);
            mission.Conversation(10, DyingForeanPackage);
            mission.RevealAndActivate(10, 11);
            mission.Transition(11);
            mission.Progress(11, MissionProgressEventKind.ScenarioEvent, ReachedPyramid, counter: Scenario);
            mission.Action(11, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Too Quiet", HillScouted, FirstAmbushBeaten, SecondAmbushBeaten, SignalFound, ReachedPyramid);
            mission.Indicator(3, MissionIndicator.UnnamedFrom + 3, HillBottom.X, HillBottom.Y, HillBottom.Z, 15);
            mission.Indicator(4, MissionIndicator.UnnamedFrom + 4, MarFend.X, MarFend.Y, MarFend.Z, 20);
            mission.Indicator(5, MissionIndicator.UnnamedFrom + 5, NekalaAtMarFend.X, NekalaAtMarFend.Y, NekalaAtMarFend.Z);
            mission.Indicator(8, MissionIndicator.UnnamedFrom + 8, Village.X, Village.Y, Village.Z, 20);
            mission.Indicator(9, DistressSignalMarker, DistressSignal.X, DistressSignal.Y, DistressSignal.Z);
            mission.Indicator(10, MissionIndicator.UnnamedFrom + 10, DyingForeanSpot.X, DyingForeanSpot.Y, DyingForeanSpot.Z);
            mission.Indicator(11, MissionIndicator.UnnamedFrom + 11, Pyramid.X, Pyramid.Y, Pyramid.Z, 15);
            mission.RadioTurnIn();
            Evidence(mission, "Stone in the entrance. The bottom of the hill (-293, 18, -258) is event1; reaching Mar Fend drops the " +
                "walkthrough's ambush (shield drone, healer, soldiers, Machina and a Machina champion), all dead is event2; Nekala2582; " +
                "the second ambush in the burned village, event3; the Distress Signal (usable name335) beside the Dying Forean, event4; " +
                "the Dying Forean610; Stone walks to the Pyramid and arriving there is event5. Events are the copy's, for everyone on " +
                "the objective. Wiki reward 10000 XP, 2000 credits, the Conical Repair Tool121310 or Reflective Armor Gloves13356 (level9).");
            mission.Enable(Scene(events: true));
        }

        /// <summary>
        /// The Dying Forean's plea: the bomb off the fallen soldier and the cave-in at Spirit
        /// Cave, the Lost Foreans inside, their walk out to the extraction point (failed if one
        /// dies) and Xang, whose death secures it.
        /// </summary>
        private static void WhatEvilLurksRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, WhatEvilLurks, "What Evil Lurks", 2993, DyingForean, null, 10, Category);
            mission.Reward(10000, 2000, selectableItems: new[] { Item(11387), Item(13412) });
            mission.Objective(4, 21658, 21659, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 21656, 21657, 2);
            mission.Objective(5, 21660, 21661, 3);
            mission.Objective(2, 17593, 17594, 4);
            mission.Objective(1, 3356, 7551, 5);
            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.ScenarioEvent, CaveOpened, counter: Scenario);
            mission.RevealAndActivate(4, 3);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ScenarioEvent, LostFound, counter: Scenario);
            mission.RevealAndActivate(3, 5);
            mission.Transition(5);
            mission.Conversation(5, LostForeanPackage);
            mission.RevealAndActivate(5, 2);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ScenarioEvent, Extracted, counter: Scenario);
            mission.RevealAndActivate(2, 1);
            mission.Transition(2, transition: 2, result: MissionObjectiveState.Failed, sequence: 2);
            mission.Progress(2, MissionProgressEventKind.ScenarioEvent, SurvivorLost, counter: Scenario, transition: 2);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, XangDown, counter: Scenario);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "What Evil Lurks", CaveOpened, LostFound, Extracted, SurvivorLost, XangDown);
            mission.Indicator(4, CaveInMarker, CaveIns[0].Position.X, CaveIns[0].Position.Y, CaveIns[0].Position.Z);
            mission.Indicator(3, LostPartyMarker, SpiritCave.X, SpiritCave.Y, SpiritCave.Z, 20);
            mission.Indicator(5, MissionIndicator.UnnamedFrom + 5, SpiritCave.X, SpiritCave.Y, SpiritCave.Z);
            mission.Indicator(2, MissionIndicator.UnnamedFrom + 2, Extraction.X, Extraction.Y, Extraction.Z, 12);
            mission.Indicator(1, MissionIndicator.UnnamedFrom + 1, Chantry.X, Chantry.Y, Chantry.Z, 20);
            mission.RadioTurnIn();
            Evidence(mission, "The Fallen Soldier (usable name336) by the mouth of Spirit Cave gives a CavesDonn_Bomb122844; used on the " +
                "cave-in there it is event1. Coming within 20 m of the Lost Foreans is event2, the one who speaks611 is objective5, and " +
                "their walk to the extraction point (-205, 10, -230) is event3; one of them dying is event4, which fails it (patch notes " +
                "2008-01-29). Xang520011 dead while the objective is open is event5. Wiki reward 10000 XP, 2000 credits, a Leech Gun11387 " +
                "or Reflective Armor Legs13412 (level9).");
            mission.Enable(Scene(events: true));
        }

        /// <summary>
        /// Nekala's: the three tests and their Logos, the gate they open, the hidden chamber
        /// behind it with its statues, reliquary and projector, D'rak who follows the player in,
        /// and the teleporter back to her.
        /// </summary>
        private static void TheReasonWhyRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TheReasonWhy, "The Reason Why", 2997, Nekala, Nekala, 11, Category);
            mission.Reward(10000, 2000, selectableItems: new[] { Item(12883), Item(110842) });
            mission.Objective(1, 3357, 6537, 1, MissionObjectiveState.Incomplete);
            mission.Objective(11, 21596, 21597, 2, MissionObjectiveState.Incomplete);
            mission.Objective(2, 3358, 6538, 3, MissionObjectiveState.Incomplete);
            mission.Objective(12, 21598, 21599, 4, MissionObjectiveState.Incomplete);
            mission.Objective(3, 3359, 6539, 5, MissionObjectiveState.Incomplete);
            mission.Objective(13, 21600, 21601, 6, MissionObjectiveState.Incomplete);
            mission.Objective(10, 21546, 21547, 7, MissionObjectiveState.Incomplete);
            mission.Objective(17, 21651, 21652, 8);
            mission.Objective(16, 21649, 21650, 9);
            mission.Objective(19, 21680, 21681, 10, counterTextId: 21682);
            mission.Objective(14, 21613, 21614, 11);
            mission.Objective(7, 9250, 9251, 12);
            mission.Objective(18, 21683, 21684, 13);

            foreach (var (objective, eventId) in new[] { (1U, FirstRuin), (2U, SecondRuin), (3U, FinalRuin) })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: Scenario);
            }

            foreach (var (objective, logos) in new[] { (11U, TrapLogos), (12U, FeelingLogos), (13U, HealLogos) })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.LogosAcquired, logos);
            }

            mission.Transition(10);
            mission.Progress(10, MissionProgressEventKind.ScenarioEvent, GateOpened, counter: Scenario);
            mission.RevealAndActivate(10, 17, 1);
            mission.RevealAndActivate(10, 16, 3);
            mission.Transition(17);
            mission.Progress(17, MissionProgressEventKind.ScenarioEvent, DrakDown, counter: Scenario);
            mission.Transition(16);
            mission.Progress(16, MissionProgressEventKind.ScenarioEvent, ChamberFound, counter: Scenario);
            mission.RevealAndActivate(16, 14, 1);
            mission.RevealAndActivate(16, 7, 3);
            mission.RevealAndActivate(16, 19, 5);
            mission.Transition(19);
            uint trigger = 1;
            foreach (var statue in new[] { FirstStatue, SecondStatue, ThirdStatue })
                mission.Progress(19, MissionProgressEventKind.ScenarioEvent, statue, counter: StatueScenario, target: 3, trigger: trigger++);
            mission.Transition(14);
            mission.Progress(14, MissionProgressEventKind.ScenarioEvent, ReliquaryOpened, counter: Scenario);
            // An object's conversation is the object's interaction (MissionApplication.TryApplyDialogue).
            mission.Transition(7);
            mission.Progress(7, MissionProgressEventKind.InteractionUsed, ProjectorClass);
            mission.RevealAndActivate(7, 18);
            mission.Transition(18);
            mission.Conversation(18, NekalaPackage);
            mission.Action(18, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "The Reason Why", FirstRuin, SecondRuin, FinalRuin, GateOpened, ChamberFound,
                ReliquaryOpened, DrakDown);
            mission.Scenario(StatueScenario, "The Mirrored Eloh Statues", FirstStatue, SecondStatue, ThirdStatue);

            for (var i = 0; i < Tests.Length; i++)
            {
                var site = Tests[i].Site;
                mission.Indicator((uint)i + 1, ElohRuinsMarker, site.X, site.Y, site.Z, 15);
                mission.Indicator((uint)i + 11, LogosPuzzleMarker, site.X, site.Y, site.Z);
            }
            mission.Indicator(10, ElohChamberMarker, Gate.X, Gate.Y, Gate.Z);
            mission.Indicator(17, MissionIndicator.UnnamedFrom + 17, DrakSpot.X, DrakSpot.Y, DrakSpot.Z, 15);
            mission.Indicator(16, ElohChamberMarker, Chamber.X, Chamber.Y, Chamber.Z, 15);
            mission.Indicator(14, MissionIndicator.UnnamedFrom + 14, Reliquary.X, Reliquary.Y, Reliquary.Z);
            mission.Indicator(7, MissionIndicator.UnnamedFrom + 7, Projector.X, Projector.Y, Projector.Z);
            mission.Indicator(18, MissionIndicator.UnnamedFrom + 18, NekalaAtEntrance.X, NekalaAtEntrance.Y, NekalaAtEntrance.Z);
            Evidence(mission, "Nekala9186 hands over two CavesDonn_Bomb122844 for the cave-ins. The three tests are brazier sequences " +
                "(UsableTwoStateForeanElohBrazierV01) at Mar Fend, the Eloh Chantry and Undone Fairway; coming to a site is its ruin " +
                "(events1-3), and four braziers lit in the site's order wake its Logos (Trap55, Feeling13, Heal17). The three Logos " +
                "open the Caves of Donn gate (UsableDoorForeanElohCaveofDonnV01) in the Vitiated Shrine, event4: D'rak10159 comes out of " +
                "the Bane dig (event7 when he dies) and the chamber's Eloh door opens. In the chamber (event5) three Forean shamans each " +
                "hold a Mirrored Eloh Statue (events1-3 of scenario2, counted by objective19), the Eloh Reliquary opens (event6), the projector " +
                "speaks1308, and the hologram platform takes the player to the corridor by the entrance, where Nekala waits. Wiki " +
                "reward 10000 XP, 2000 credits, a Hazmat Armor Helmet12883 or Area Healing Disc110842.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>Napoli's bounty on the six egg clusters of the tunnels; each one's destruction counts once.</summary>
        private static void BuggedToDeathRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, BuggedToDeath, "Bugged to Death", 21548, Napoli, Napoli, 10, Category);
            mission.Reward(8000, 1600);
            mission.Objective(1, 21553, 21554, 1, MissionObjectiveState.Incomplete, counterTextId: 21555);
            mission.Transition(1);
            for (uint cluster = 1; cluster <= EggClusters.Length; cluster++)
                mission.Progress(1, MissionProgressEventKind.ScenarioEvent, cluster, counter: Scenario,
                    target: (uint)EggClusters.Length, trigger: cluster);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(Scenario, "Fithik egg clusters destroyed",
                Enumerable.Range(1, EggClusters.Length).Select(cluster => (uint)cluster).ToArray());
            for (var i = 0; i < EggClusters.Length; i++)
                mission.Indicator(1, MissionIndicator.UnnamedFrom + (uint)i + 1, EggClusters[i].X, EggClusters[i].Y, EggClusters[i].Z);
            Evidence(mission, "Napoli6697, found behind the cave-in of the Chantry tunnel, pays for the egg clusters10180 of the caves: " +
                "six, two in each tunnel, each destroyed once counted (events1-6, counter21555). Added by the August 2008 patch with " +
                "no reward recorded: 8000 XP and 1600 credits, beside the instance's others.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>A mission's binding: for the bootcamp's graduates, with a scenario for the events the instance records.</summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Wiki,
                "The Caves of Donn walkthrough and mission pages: encounters, coordinates and rewards (pre-rework flow).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "CavesOfDonnContent", reconstruction);
        }

        #endregion
    }
}
