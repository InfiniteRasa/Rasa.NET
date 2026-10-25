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
    /// The Ojasa Atta Hive (adv_arieki_torden_incline_ojasaattahive, 1865), entered from Incline,
    /// as Ten Ton Hammer's Ojasa Colony guide tells it: its people, its Atta and its five
    /// missions, in the client's own names and texts.
    ///
    /// Who and what:
    ///  - Munitions Expert Sal at Incline's Plains Post, who gives Fried Atta; Explosives Specialist
    ///    John inside, who takes the canister and gives the rest. John goes where the squad's
    ///    story is (OjasaHive): the camp by the entrance, the door of the third cell, the Queen's
    ///    chamber.
    ///  - The hive's Atta at the guide's levels, 24 to 27: the 31 pools already in the hive move
    ///    to Atta of their own, as the Incline's level 37 and 38 rows are Incline's too. A level 25
    ///    Ojasa Defender among the Brann dead of the first cell, a level 26 Ojasa Taskmaster in
    ///    the second, the level 27 Ojasa Royal Guard at the door of the Queen's chamber, and the
    ///    Atta Hive Queen on her throne, beyond reach.
    ///  - Four Crazed Brann Soldiers guarding the Brann camp in the third cell.
    ///  - The missions: Fried Atta, Something Smells, Brann Bacon, Atta Ought To Do It and Long
    ///    Live the Queen, one after the other, John giving and taking each after the first.
    /// The usables (the Brann corpse, the camp's bins, the Shocking Device), the flamethrower
    /// test's wave, the young Atta, the Atta and Brann at the Queen and the scenario events the
    /// missions wait for are the server's, per copy of the hive (OjasaHive).
    ///
    /// Health and damage follow the Incline's Atta down the levels: a level's thug is
    /// 7071 x 1.0905^(level - 38), the curve the Incline rows sit on, a minion three quarters
    /// of it, a lieutenant one and three quarters, a boss half as much again; damage scales by
    /// the same factor.
    /// </summary>
    public static class OjasaHiveContent
    {
        public const string Revision = "ojasa_1_16_5";
        public const uint Hive = 1865, Incline = 1761;

        /// <summary>missioncategorylanguage: "Instance (Ojasa Atta Hive)".</summary>
        public const uint Category = 10000024;

        public const uint FriedAtta = 1491, SomethingSmells = 1492, BrannBacon = 1493, AttaOughtToDoIt = 1494, LongLiveTheQueen = 1495;
        public static readonly uint[] Missions = { FriedAtta, SomethingSmells, BrannBacon, AttaOughtToDoIt, LongLiveTheQueen };

        /// <summary>John's conversation package: the one Fried Atta's first objective is a conversation with.</summary>
        public const uint JohnPackage = 1750;

        /// <summary>The scenario of each mission whose events the hive records.</summary>
        public const uint TestScenario = 1, DeviceScenario = 1, QueenScenario = 1;
        public const uint WaveCleared = 1, HatchlingsDead = 1;
        public const uint QueenChecked = 1, ChoseAtta = 2, ChoseBrann = 3, AttaWon = 4, BrannWon = 5;

        public const uint Sal = 565001, John = 565002;
        public const uint AttaSoldier = 565010, AttaHarvester = 565011, AttaGrub = 565012;
        public const uint Defender = 565013, Taskmaster = 565014, RoyalGuard = 565015, Queen = 565016;
        public const uint YoungAtta = 565017, WaveAtta = 565018;
        public const uint BrannGuard = 565020, BrannRaider = 565022, BrawlAtta = 565023;

        /// <summary>The Atta of the hive, the brawl's aside: what goes over to the player's side when they help the Atta.</summary>
        public static readonly uint[] HiveAtta = { AttaSoldier, AttaHarvester, AttaGrub, Defender, Taskmaster, RoyalGuard, YoungAtta, WaveAtta };

        public const uint InclineAttaSoldier = 531037, InclineAttaHarvester = 531038, InclineAttaGrub = 531039;

        public const uint CanisterTemplate = 111137, MedallionTemplate = 111139;

        /// <summary>
        /// missionobjectiveindicatorlanguage: the hive's own marker names. An objective with none
        /// is keyed from MissionIndicator.UnnamedFrom, and the client names it by the objective.
        /// </summary>
        public const uint OjasaAttaHiveMarker = 278, BrannCorpseMarker = 234, BrannCampMarker = 235, QueenMarker = 236;

        /// <summary>Incline's way into the hive (map_link 68).</summary>
        public static readonly Vector3 InclineEntrance = new(-548.7698f, 301.0576f, 344.2102f);

        public const uint BrannCorpseClass = 7928, BrannBinClass = 7931, ShockingDeviceClass = 26423;

        /// <summary>usablenameoverride SHOCKING_DEVICE.</summary>
        public const uint ShockingDeviceName = 160;

        public const uint SalPool = 565201, JohnPool = 565202, DefenderPool = 565203, TaskmasterPool = 565204,
            RoyalGuardPool = 565205, QueenPool = 565206, FirstGuardPool = 565207;

        // Where things are, on the hive's navmesh (x, height, z). The guide's coordinates are the
        // world's, two of its minus signs lost.
        public static readonly Vector3 Entrance = new(553.7f, 88.6f, 146.5f);
        public static readonly Vector3 JohnAtCamp = new(431f, 73.334984f, 134.5f);
        public static readonly Vector3 JohnAtCellThree = new(-163.4f, 108.16511f, -178f);
        public static readonly Vector3 JohnAtChamber = new(175f, 115.61543f, -245f);
        public static readonly Vector3 BrannCorpse = new(316f, 84.06988f, 136f);
        public static readonly Vector3[] BrannBins = { new(-399f, 125.4702f, -215f), new(-341f, 123.860565f, -204f) };
        public static readonly Vector3 ShockingDevice = new(-104f, 118.465454f, -375f);
        public static readonly Vector3 Eggs = new(-95.3f, 121.3479f, -394f);
        public static readonly Vector3 Throne = new(181.5f, 131.20636f, -290.5f);
        public static readonly Vector3 BrannCamp = new(-386f, 122.6f, -204f);
        public static readonly Vector3[] WaveSpawns = { new(380f, 76.26f, 118f), new(372f, 77.3f, 118.4f), new(300f, 92.72f, 151f), new(295.9f, 93.46f, 153.5f) };
        public static readonly Vector3[] BrawlBrann = { new(198f, 111.04f, -236f), new(212f, 111.32f, -245.4f), new(200f, 112.56f, -252f) };
        public static readonly Vector3[] BrawlAttaSpots = { new(214f, 110.94f, -232f), new(190f, 111.19f, -244f), new(205f, 110.6f, -240f) };
        public static readonly Vector3 Brawl = new(205f, 110.6f, -240f);

        /// <summary>A creature row, its stats and its actions (as its creature_action ids).</summary>
        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        /// <summary>The creature table's faction column: the target category by number.</summary>
        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1, Neutral = 3, Ignore = 6 }

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public sealed record Spot(uint Id, uint Map, uint Creature, double X, double Y, double Z, double Rotation, uint RespawnTenths);

        // Existing actions the new Atta keep: they do no damage of their own.
        public const uint HarvesterPheromone = 56007, HarvesterWarcry = 61056, GrubCocoon = 65039;

        public const uint RifleAttack = 1, RifleArg = 134;

        public static readonly Act[] Actions =
        {
            new(565101, "Ojasa Atta Soldier atta_soldier_melee", 478, 1, 0.5, 4, 1000, 0, 100, 200, 1),
            new(565102, "Ojasa Atta Soldier atta_soldier_acid_spit", 479, 1, 1, 10, 5000, 0, 44, 90, 4),
            new(565103, "Ojasa Atta Soldier atta_soldier_rock_throw", 292, 1, 1, 50, 3000, 1366, 100, 200, 1),
            new(565104, "Ojasa Atta Harvester atta_harvester_acid_spit", 475, 1, 1, 20, 5000, 0, 46, 64, 4),
            new(565105, "Ojasa Atta Grub atta_grub_melee", 499, 1, 0.5, 2, 1000, 433, 55, 110, 1),
            new(565106, "Ojasa Defender atta_soldier_melee", 478, 1, 0.5, 4, 1000, 0, 125, 250, 1),
            new(565107, "Ojasa Defender atta_soldier_acid_spit", 479, 1, 1, 10, 5000, 0, 55, 113, 4),
            new(565108, "Ojasa Defender atta_soldier_rock_throw", 292, 1, 1, 50, 3000, 1366, 125, 250, 1),
            new(565109, "Ojasa Taskmaster atta_harvester_acid_spit", 475, 1, 1, 20, 5000, 0, 81, 114, 4),
            new(565110, "Ojasa Royal Guard atta_soldier_melee", 478, 1, 0.5, 4, 1000, 0, 179, 357, 1),
            new(565111, "Ojasa Royal Guard atta_soldier_acid_spit", 479, 1, 1, 10, 5000, 0, 78, 161, 4),
            new(565112, "Ojasa Royal Guard atta_soldier_rock_throw", 292, 1, 1, 50, 3000, 1366, 179, 357, 1),
            new(565113, "Ojasa Young Atta atta_soldier_melee", 478, 1, 0.5, 4, 1000, 0, 92, 183, 1),
            new(565114, "Ojasa Young Atta atta_soldier_acid_spit", 479, 1, 1, 10, 5000, 0, 40, 83, 4),
            new(565115, "Ojasa Queen's Atta atta_soldier_melee", 478, 1, 0.5, 4, 1000, 0, 109, 218, 1),
            new(565116, "Ojasa Queen's Atta atta_soldier_acid_spit", 479, 1, 1, 10, 5000, 0, 48, 98, 4),
            new(565117, "Ojasa Queen's Atta atta_soldier_rock_throw", 292, 1, 1, 50, 3000, 1366, 109, 218, 1),
            new(565118, "Ojasa Crazed Brann Soldier rifle 1/134", RifleAttack, RifleArg, 1, 25, 1000, 0, 60, 110, 1),
            new(565119, "Ojasa Brann Assassin rifle 1/134", RifleAttack, RifleArg, 1, 25, 1000, 0, 65, 120, 1),
            new(565120, "Explosives Specialist John rifle 1/134", RifleAttack, RifleArg, 1, 25, 800, 0, 80, 120, 1)
        };

        public static readonly Beast[] Creatures =
        {
            new(Sal, "Munitions Expert Sal - Plains Post", SwapsetMale, TargetCategoryRow.Friendly, 30, 3000, 0, 9659, 0, 0),
            new(John, "Explosives Specialist John - Ojasa", SwapsetMale, TargetCategoryRow.Friendly, 27, 20000, 0, 9660, 0, 0, 565120),
            new(AttaSoldier, "Atta Soldier Conscript - Ojasa", 7081, TargetCategoryRow.Hostile, 25, 2292, 88, 8095, 9, 5, 565101, 565102, 565103),
            new(AttaHarvester, "Atta Harvester Gatherer - Ojasa", 7078, TargetCategoryRow.Hostile, 24, 1577, 57, 8071, 9, 5, 565104, HarvesterPheromone, HarvesterWarcry),
            new(AttaGrub, "Atta Grub - Ojasa", 29089, TargetCategoryRow.Hostile, 24, 1577, 57, 0, 9, 5, 565105, GrubCocoon),
            new(Defender, "Ojasa Defender", 7081, TargetCategoryRow.Hostile, 25, 4011, 110, 9703, 9, 5, 565106, 565107, 565108),
            new(Taskmaster, "Ojasa Taskmaster", 24592, TargetCategoryRow.Hostile, 26, 6563, 130, 9704, 9, 5, 565109, HarvesterPheromone, HarvesterWarcry),
            new(RoyalGuard, "Ojasa Royal Guard", 22627, TargetCategoryRow.Hostile, 27, 7156, 150, 9702, 9, 5, 565110, 565111, 565112),
            new(Queen, "Atta Hive Queen - Ojasa", 7079, TargetCategoryRow.Ignore, 28, 50000, 300, 8092, 0, 0),
            new(YoungAtta, "Young Atta - Ojasa eggs", 25612, TargetCategoryRow.Hostile, 24, 1577, 57, 8094, 9, 5, 565113, 565114),
            new(WaveAtta, "Atta Soldier Conscript - Ojasa fuel test", 7081, TargetCategoryRow.Hostile, 25, 1719, 88, 8095, 9, 5, 565101, 565102),
            // Redshirt_Brann_Male: the female's mesh (18704) has no skeleton in the client's animation data.
            new(BrannGuard, "Crazed Brann Soldier - Ojasa camp", 7253, TargetCategoryRow.Hostile, 25, 2292, 88, 9440, 9, 5, 565118),
            new(BrannRaider, "Brann Assassin - Ojasa Queen", 7253, TargetCategoryRow.Neutral, 26, 2500, 96, 10027, 9, 5, 565119),
            new(BrawlAtta, "Atta Soldier Guard - Ojasa Queen", 7081, TargetCategoryRow.Neutral, 26, 2500, 96, 8096, 9, 5, 565115, 565116, 565117)
        };

        /// <summary>John: Caufield's man in the sapper's kit, a rifle in his hands.</summary>
        public static readonly Slot[] JohnLook =
        {
            new(1, 9808, 4280052432), new(2, SapperBoots, 4280052432), new(3, SapperGloves, 4280052432),
            new(13, 4095, 1), new(14, 9781, DarkHair), new(15, SapperTorso, 4280052432),
            new(16, SapperLegs, 4280052432), new(17, 24008, SkinLight)
        };

        /// <summary>Sal looks as Quartermaster Caufield does.</summary>
        public const uint SalLooksLike = 132;

        public static readonly Spot[] Pools =
        {
            new(SalPool, Incline, Sal, 145, 233.0, -275, 0.29, 20),
            new(JohnPool, Hive, John, JohnAtCamp.X, JohnAtCamp.Y, JohnAtCamp.Z, 4.4, 20),
            new(DefenderPool, Hive, Defender, 165, 117.06781, 205, 3.1, 2700),
            new(TaskmasterPool, Hive, Taskmaster, 13.8, 64.90885, 30.4, 1.6, 2700),
            new(RoyalGuardPool, Hive, RoyalGuard, 105, 114.92353, -305, 1.0, 2700),
            new(QueenPool, Hive, Queen, Throne.X, Throne.Y, Throne.Z, 4.2, 2700),
            new(FirstGuardPool, Hive, BrannGuard, -380, 122.51784, -200, 0.8, 1200),
            new(FirstGuardPool + 1, Hive, BrannGuard, -395.40533, 123.52354, -195.61575, 5.5, 1200),
            new(FirstGuardPool + 2, Hive, BrannGuard, -370.21393, 122.08612, -209.78261, 2.4, 1200),
            new(FirstGuardPool + 3, Hive, BrannGuard, -405, 123.26666, -205, 4.7, 1200)
        };

        /// <summary>The hive's pools go over from the Incline's rows to these.</summary>
        public static readonly (uint From, uint To)[] Repointed =
        {
            (InclineAttaSoldier, AttaSoldier), (InclineAttaHarvester, AttaHarvester), (InclineAttaGrub, AttaGrub)
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

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {Sal}, a.slot_id, a.Class_id, a.color " +
                $"FROM creature_appearance a WHERE a.id = {SalLooksLike} AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {Sal}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance x WHERE x.id = {Sal});";

            foreach (var sql in Change(John, new Slot[0], JohnLook))
                yield return sql;

            yield return $"INSERT INTO npc_package (id, package_id, comment) SELECT {John}, {JohnPackage}, 'Explosives Specialist John' " +
                $"FROM creature c WHERE c.id = {John} AND NOT EXISTS (SELECT 1 FROM npc_package x WHERE x.id = {John});";

            foreach (var p in Pools)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {p.Id}, 0, 0, {p.RespawnTenths}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, {N(p.Rotation)}, {p.Map}, {p.Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {p.Creature} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {p.Id});";

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {to} WHERE map_context_id = {Hive} AND creature_{slot}_Id = {from} " +
                        $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id = {to});";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            foreach (var (from, to) in Repointed)
                for (var slot = 1; slot <= 6; slot++)
                    yield return $"UPDATE spawnpool SET creature_{slot}_Id = {from} WHERE map_context_id = {Hive} AND creature_{slot}_Id = {to};";

            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Creature};";

            yield return $"DELETE FROM npc_package WHERE id = {John} AND package_id = {JohnPackage};";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Sal}, {John});";

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

        private const string Guide = "https://www.tentonhammer.com/guides/ojasa-colony";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            FriedAttaRows(sql);
            SomethingSmellsRows(sql);
            BrannBaconRows(sql);
            AttaOughtToDoItRows(sql);
            LongLiveTheQueenRows(sql);
            return sql;
        }

        private static InstanceMissionSql.RewardItem Item(uint template, uint quantity = 1, uint module = 0) => new(template, quantity, module);

        /// <summary>
        /// Sal hands over the canister; John takes it (package 1750) and goes to try it, and the
        /// player keeps him alive through the Atta it brings (OjasaHive's flamethrower test).
        /// </summary>
        private static void FriedAttaRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, FriedAtta, "Fried Atta", 13311, Sal, John, 25, Category);
            mission.Reward(12500, 2500, selectableItems: new[] { Item(111016, 3), Item(45056, 3), Item(47597) });
            mission.Objective(1, 13338, 13339, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 13341, 13342, 2);
            mission.Transition(1);
            mission.Conversation(1, JohnPackage);
            mission.Action(1, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("john-takes-canister", FriedAtta, "canister", 1, MissionItemScope.AssignmentIssued));
            mission.RevealAndActivate(1, 2, 2);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ScenarioEvent, WaveCleared, counter: TestScenario);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(TestScenario, "John's flamethrower test", WaveCleared);
            mission.Indicator(1, OjasaAttaHiveMarker, InclineEntrance.X, InclineEntrance.Y, InclineEntrance.Z);
            mission.Indicator(2, MissionIndicator.UnnamedFrom + 2, JohnAtCamp.X, JohnAtCamp.Y, JohnAtCamp.Z, 30);
            Evidence(mission, "Sal at Incline's Plains Post gives the Burn Your Eyes Out canister111137 on acceptance, the marker on " +
                "Incline's way into the hive (Ojasa Atta Hive, 278); John (package1750) " +
                "takes it. Protect John is the server's: a wave of six Atta from the hive's tunnel at the camp, cleared with John above " +
                "15% health, records event1 of scenario1 for every squad member on the objective. XP five times the credits, a reconstruction.");
            var scene = Scene(events: true);
            scene.Items = new List<MissionItemBinding> { Issued("canister", CanisterTemplate) };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("sal-canister", FriedAtta, "canister", CanisterTemplate, 1)
            };
            mission.Enable(scene);
        }

        /// <summary>The Brann corpse by the entrance: searched, it gives the Old Medallion John then looks at.</summary>
        private static void SomethingSmellsRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, SomethingSmells, "Something Smells", 13316, John, John, 25, Category);
            mission.Reward(12500, 2500, selectableItems: new[] { Item(111230), Item(111046, 3), Item(45054, 3) });
            mission.Prerequisite(FriedAtta);
            mission.Objective(1, 13343, 13344, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.InteractionUsed, BrannCorpseClass);
            mission.Action(1, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("brann-medallion", SomethingSmells, "medallion", MedallionTemplate, 1));
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, BrannCorpseMarker, BrannCorpse.X, BrannCorpse.Y, BrannCorpse.Z);
            Evidence(mission, "The searchable Brann corpse7928 lies at the guide's 316, 83, 136, on the ground. Its search gives the Old " +
                "Medallion111139, which goes with the turn-in to John.");
            var scene = Scene();
            scene.Items = new List<MissionItemBinding> { Issued("medallion", MedallionTemplate) };
            mission.Enable(scene);
        }

        /// <summary>The Brann camp in the third cell: its bins searched, and John told at the cell's door.</summary>
        private static void BrannBaconRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, BrannBacon, "Brann Bacon", 13321, John, John, 25, Category);
            mission.Reward(15000, 3000, selectableItems: new[] { Item(19848, 1, 100038), Item(23773, 1, 100042), Item(28079, 1, 100046) });
            mission.Prerequisite(SomethingSmells);
            mission.Objective(1, 13345, 13346, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.InteractionUsed, BrannBinClass);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, BrannCampMarker, BrannCamp.X, BrannCamp.Y, BrannCamp.Z, 20);
            Evidence(mission, "Four Crazed Brann Soldiers guard the camp at -386, 122, -204; either of its rusty bins7931 (-399, 125, -215 " +
                "and -341, 123, -204) is the search. John waits at the third cell's door for the turn-in. Hellstrom Bio Armor Vest19848 " +
                "with Resist Fire[3]100038, CryoGen Graviton Armor Helmet23773 with Resist Ice[3]100042 and Pathogex Motor Assist Armor " +
                "Helmet28079 with Resist Virulent[3]100046: the guide's brands at level 25, their brand's module in the piece.");
            mission.Enable(Scene());
        }

        /// <summary>The Shocking Device by the eggs: used, two young Atta hatch, and both are to die.</summary>
        private static void AttaOughtToDoItRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, AttaOughtToDoIt, "Atta Ought To Do It", 13326, John, John, 26, Category);
            mission.Reward(15000, 3000, selectableItems: new[] { Item(123000), Item(123005), Item(122985) });
            mission.Prerequisite(BrannBacon);
            mission.Objective(1, 13361, 13362, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, HatchlingsDead, counter: DeviceScenario);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(DeviceScenario, "The Shocking Device", HatchlingsDead);
            mission.Indicator(1, MissionIndicator.UnnamedFrom + 1, ShockingDevice.X, ShockingDevice.Y, ShockingDevice.Z);
            Evidence(mission, "The Shocking Device26423 (name160) at -104, 118, -375 is wired to the eggs at -95, 121, -394: used, it " +
                "hatches two Young Atta25612, and the objective is both dead (event1 of scenario1, for every squad member on it: the " +
                "client has no counter text for a count). Regen modules[3] for the guide's regen recipes. The guide had no marker; the " +
                "device has one.");
            mission.Enable(Scene(events: true));
        }

        /// <summary>
        /// The Queen's chamber: seeing her (the camera's fly-over) checks on her, and the brawl
        /// below her is the choice - the player's first shot - and the fight that follows. Help the
        /// Atta and Help the Brann are the two ways; the one not taken fails, and Decide who to
        /// help is done when the one taken is won.
        /// </summary>
        private static void LongLiveTheQueenRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, LongLiveTheQueen, "Long Live the Queen", 13331, John, John, 27, Category);
            mission.Reward(20000, 4000, selectableItems: new[] { Item(48392, 1, 900247), Item(47728, 1, 900242), Item(17517, 1, 900222) });
            mission.Prerequisite(AttaOughtToDoIt);
            mission.Objective(1, 13363, 13364, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 13365, 13366, 2, required: false);
            mission.Objective(3, 13367, 13368, 3, required: false);
            mission.Objective(4, 13369, 13370, 4);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, QueenChecked, counter: QueenScenario);
            mission.RevealAndActivate(1, 2, 1);
            mission.RevealAndActivate(1, 3, 3);
            mission.RevealAndActivate(1, 4, 5);
            foreach (var (objective, won, otherChoice) in new[] { (2U, AttaWon, ChoseBrann), (3U, BrannWon, ChoseAtta) })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, won, counter: QueenScenario);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Transition(objective, transition: 2, result: MissionObjectiveState.Failed, sequence: 2);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, otherChoice, counter: QueenScenario, transition: 2);
            }
            mission.Scenario(QueenScenario, "The Queen's chamber", QueenChecked, ChoseAtta, ChoseBrann, AttaWon, BrannWon);
            mission.Indicator(1, QueenMarker, Throne.X, Throne.Y, Throne.Z, 30);
            for (uint objective = 2; objective <= 4; objective++)
                mission.Indicator(objective, MissionIndicator.UnnamedFrom + objective, Brawl.X, Brawl.Y, Brawl.Z, 15);
            Evidence(mission, "Client1.16.5 has no Echo! Echo! Echo!; its 1495 carries the choice as objectives2-4. Checking on the Queen is " +
                "coming within 70 m of her throne (camera script1 plays). Three Brann Assassins and three Atta brawl below her, kept " +
                "at it until a player's first hit on one of them takes the other side; the side taken goes Friendly, the other Hostile, " +
                "and the side's win completes its objective, the other's fails at the choice. Helping the Atta turns the hive's Atta " +
                "Friendly; John fights those about him at the turn-in. The brawl waits for the mission, so entering the chamber " +
                "first no longer loses it. One reward list for both ways, as a mission has one: the Vextronics weapons at level 25, " +
                "each with its brand's module.");
            var scene = Scene(events: true);
            scene.ObjectiveAggregations = new Dictionary<uint, MissionObjectiveAggregation>
            {
                [4] = new(new uint[] { 2, 3 }, 1)
            };
            mission.Enable(scene);
        }

        /// <summary>
        /// A mission's binding: for the bootcamp's graduates. One whose events the hive records
        /// has a scenario, which a mission has only with a script; the data sequence with no
        /// sequences is one that does nothing.
        /// </summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static MissionItemBinding Issued(string key, uint template) =>
            new(key, template, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove);

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide, "The Ojasa Colony walkthrough: route, levels and rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "OjasaHiveContent", reconstruction);
        }

        #endregion
    }
}
