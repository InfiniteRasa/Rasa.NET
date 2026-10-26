using System.Collections.Generic;
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
    using static MinosCavernsContent;

    /// <summary>
    /// The four Minos Caverns missions of client 1.16.5, in its own names, texts and conversation
    /// packages, as the Tabula Rasa wiki and Ten Ton Hammer tell them, on the people and things
    /// MinosCavernsContent places:
    ///
    ///  - Retrieval for Recon (382): Lieutenant Sebastian in the Foreas Base command tower sends
    ///    the player after Recon Officer Tyler, last seen in the hills above Delta Outpost. Tyler
    ///    sits in the caverns' southern vault with a broken leg; finding him (his conversation,
    ///    package 177) is the objective and he takes the turn-in.
    ///  - Data Thieves (383): Tyler hands over three detonators and sends the player into the Bane
    ///    base for the Allied Datapad the Thrax Technician General carries, Recon Officer Karik and
    ///    Ranger Zola held in the prison, and the four monitoring databanks in the control room;
    ///    the rescued follow the player out to the entrance (MinosCaverns). Back to Tyler.
    ///  - Cave Extraction (392): Tyler, his leg seen to, is walked out to the entrance
    ///    (MinosCaveExtractionScene, the public encounter on his spawn) and turns the mission in
    ///    there.
    ///  - Destroying the Warnet Barrier (384): Corporal Foster at Delta Outpost's east gate wants
    ///    eight of the nine warnet nests in the caverns smashed, and counts only those.
    ///
    /// Rewards are the wiki's: the experience and credits as given, and the gear with the module
    /// its brand names - Olympia Resist Physical, Pathogex Resist Virulent, Voltrox Resist
    /// Electric, Luminar Resist Photonic, Teleract Armor Absorption, Vitalius Steal Health,
    /// Vextronics the debuff of the weapon's own damage - at level [2], the band of level 11-20
    /// gear. Each mission's rewards are a choice of one.
    /// </summary>
    public static class MinosCavernsMissions
    {
        public const string Revision = "minos_1_16_5";

        /// <summary>Instance (Minos Caverns): the client's mission category.</summary>
        public const uint Category = 10000035;

        public const uint RetrievalForRecon = 382, DataThieves = 383, WarnetBarrier = 384, CaveExtraction = 392;
        public static readonly uint[] Missions = { RetrievalForRecon, DataThieves, CaveExtraction, WarnetBarrier };

        /// <summary>Tyler's conversation package, and Karik's and Zola's (the client's objectiveconversation rows).</summary>
        public const uint TylerPackage = 177, KarikPackage = 523, ZolaPackage = 524;

        /// <summary>The Allied Datapad (MisDivideItemMinosdatapad): its class, and the template the Technician General drops.</summary>
        public const uint DatapadClass = 7107, DatapadTemplate = 467;

        /// <summary>Tyler hands these over with Data Thieves: two charges to set and one to spare.</summary>
        public const uint Detonators = 3;

        /// <summary>How many nests of the nine Foster wants smashed, and the databanks Tyler wants destroyed.</summary>
        public const uint NestsToDestroy = 8, DatabanksToDestroy = 4;

        /// <summary>The escort's scenario and its event (the scene's signal), as InstanceMissionSql.Scenario writes them.</summary>
        public const uint EscortScenario = 1, EscortArrived = 1;

        /// <summary>The client's indicator names: Bane/Minos Caverns Entrance, Datapad Homing Beacon, Estimated Location: Monitoring Databanks, Minos Caverns Exit, Lieutenant Sebastian.</summary>
        public const uint EntranceMarker = 33, DatapadMarker = 31, DatabanksMarker = 30, ExitMarker = 10000004;

        /// <summary>The mouth of the caverns on the Divide (map_link 1148 to 1347), and where a player arrives inside.</summary>
        public static readonly Vector3 CaveMouth = new(640.1f, 162.38f, -124f);
        public static readonly Vector3 Entrance = new(-15.8f, 29.5f, 122.6f);

        /// <summary>Where Tyler sits, the Technician General stands, the prison is, the analyzer stands among the databanks.</summary>
        public static readonly Vector3 TylerSpot = new(58f, -30.41f, -49f);
        public static readonly Vector3 SideRoom = new(127.9f, 2.92f, 75.9f);
        public static readonly Vector3 Prison = new(296f, 3.87f, 113.5f);
        public static readonly Vector3 Analyzer = new(172f, 1.9f, -96f);

        #region Rewards

        /// <summary>Armor Module: Resist Physical [2] (Olympia), Resist Virulent [2] (Pathogex), Resist Electric [2] (Voltrox), Resist Photonic [2] (Luminar), Armor Absorption [2] (Teleract).</summary>
        public const uint Olympia = 100021, Pathogex = 100045, Voltrox = 100033, Luminar = 100029, Teleract = 900181;

        /// <summary>Weapon Module: Steal Health [2] (Vitalius), Debuff Sonic Resist [2] and Debuff Photonic Resist [2] (Vextronics).</summary>
        public const uint VitaliusWeapon = 900266, VextronicsSonic = 900226, VextronicsPhotonic = 900236;

        /// <summary>The gear, at the class's level nearest 14 among the quality-3 templates: the class's name is the client's.</summary>
        public const uint HazmatHelmet = 20568;          // Hazmat Armor Helmet, level 14
        public const uint ReflectiveHelmet = 35804;      // Reflective Armor Helmet, level 14
        public const uint VirulentLeechGun = 15280;      // Virulent Leech Gun, level 14
        public const uint MotorAssistLegs = 29008;       // Motor Assist Armor Legs, level 13
        public const uint BioHelmet = 18943;             // Bio Armor Helmet, level 15
        public const uint ReflectiveVest = 35439;        // Reflective Armor Vest, level 14
        public const uint SonicPistol = 14432;           // Sonic Pistol, level 15
        public const uint BioBoots = 18424;              // Bio Armor Boots, level 15
        public const uint LaserChaingun = 48240;         // Laser Chaingun, level 13
        public const uint GravitonVest = 24001;          // Graviton Armor Vest, level 15

        #endregion

        /// <summary>
        /// The walkers' speeds: Tyler's are Matthew Corman's, the Wilderness escort's; the captives
        /// follow at a walk and run to catch up. The rows were placed standing still (0, 0).
        /// </summary>
        public const uint TylerRun = 6, TylerWalk = 2, CaptiveRun = 7, CaptiveWalk = 3;

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            var sql = new List<string>
            {
                $"UPDATE creature SET run_speed = {TylerRun}, walk_speed = {TylerWalk} WHERE id = {Tyler} AND run_speed = 0 AND walk_speed = 0;",
                $"UPDATE creature SET run_speed = {CaptiveRun}, walk_speed = {CaptiveWalk} WHERE id IN ({Karik}, {Zola}) AND run_speed = 0 AND walk_speed = 0;"
            };
            RetrievalForReconRows(sql);
            DataThievesRows(sql);
            CaveExtractionRows(sql);
            WarnetBarrierRows(sql);
            return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            yield return $"UPDATE creature SET run_speed = 0, walk_speed = 0 WHERE id = {Tyler} AND run_speed = {TylerRun} AND walk_speed = {TylerWalk};";
            yield return $"UPDATE creature SET run_speed = 0, walk_speed = 0 WHERE id IN ({Karik}, {Zola}) AND run_speed = {CaptiveRun} AND walk_speed = {CaptiveWalk};";
        }

        private static InstanceMissionSql.RewardItem Item(uint template, uint module) => new(template, 1, module);

        /// <summary>Sebastian sends the player into the hills; Tyler, found, is the objective and takes the turn-in.</summary>
        private static void RetrievalForReconRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, RetrievalForRecon, "Retrieval for Recon", 445, Sebastian, Tyler, 13, Category);
            mission.Reward(17000, 2550, selectableItems: new[] { Item(BioHelmet, Voltrox), Item(ReflectiveVest, Olympia), Item(SonicPistol, VextronicsSonic) });
            mission.Objective(1, 485, 7989, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, TylerPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, EntranceMarker, CaveMouth.X, CaveMouth.Y, CaveMouth.Z, 15);
            Evidence(mission, "Lt. Sebastian (name125) in the Foreas Base command tower gives it; Recon Officer Tyler (name3017, package177) " +
                "in the caverns' southern vault at 58, -30, -49 (the wiki's loc) is found by his conversation (text525) and takes the " +
                "turn-in. The marker is the client's Bane/Minos Caverns Entrance (33) at the cave mouth on the Divide. Voltrox Bio Armor " +
                $"Helmet{BioHelmet} with Resist Electric[2]{Voltrox}, Olympia Reflective Armor Vest{ReflectiveVest} with Resist " +
                $"Physical[2]{Olympia}, Vextronics Sonic Pistol{SonicPistol} with Debuff Sonic Resist[2]{VextronicsSonic}: the wiki's " +
                "rewards, a choice of one, each with its brand's module.");
            mission.Enable(Scene());
        }

        /// <summary>
        /// The datapad off the Technician General, the captives' conversation (Karik's or Zola's;
        /// the rescued follow the player out, MinosCaverns) and the four databanks, in any order;
        /// three detonators on acceptance for the charges at the base's doors.
        /// </summary>
        private static void DataThievesRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, DataThieves, "Data Thieves", 449, Tyler, Tyler, 14, Category);
            mission.Reward(13000, 1950, selectableItems: new[] { Item(HazmatHelmet, Olympia), Item(ReflectiveHelmet, Pathogex) });
            mission.Prerequisite(RetrievalForRecon);
            mission.Objective(1, 473, 7634, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 2354, 7635, 2, MissionObjectiveState.Incomplete);
            mission.Objective(4, 2697, 7636, 3, MissionObjectiveState.Incomplete, counterTextId: 2698);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, DatapadClass, target: 1);
            mission.Transition(2);
            mission.Conversation(2, KarikPackage);
            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.ObjectDestroyed, DatabankClass, counter: 0, target: DatabanksToDestroy);
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, DatapadMarker, SideRoom.X, SideRoom.Y, SideRoom.Z, 10);
            mission.Indicator(2, MissionIndicator.UnnamedFrom + 2, Prison.X, Prison.Y, Prison.Z, 10);
            mission.Indicator(4, DatabanksMarker, Analyzer.X, Analyzer.Y, Analyzer.Z, 15);
            Evidence(mission, "Tyler gives and takes it. Objective1: the Allied Datapad (class7107, template467) drops for each player " +
                $"on it from the Thrax Technician General (creature{TechnicianGeneral}) in the side room off the entrance hall; one goes " +
                "with the turn-in. Objective2: Recon Officer Karik's conversation (package523, text2357), who speaks for both; Zola's " +
                "(package524, text2356) is the same objective. Objective4: four monitoring databanks (class10191) in the control room, " +
                "counted as 'Monitoring Machines Destroyed.' (2698) and sounding the base's alarm (BaneAlarms). The client's objectives 5 " +
                "('no desc') and 6 ('girls_leave') are the server's: the rescued follow the player to the entrance and leave " +
                "(MinosCaverns). Three GenItemDetonator44859 are issued on acceptance for the two charged doors, taken back at the end. " +
                $"Olympia Hazmat Armor Helmet{HazmatHelmet} with Resist Physical[2]{Olympia}, Pathogex Reflective Armor " +
                $"Helmet{ReflectiveHelmet} with Resist Virulent[2]{Pathogex}: the wiki's rewards.");
            var scene = Scene();
            scene.Items = new List<MissionItemBinding>
            {
                new("datapad", DatapadTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1, Drop: new MissionItemDropDefinition(new[] { TechnicianGeneral }, 1, Map, 100, 1)),
                new("detonators", DetonatorTemplate, MissionItemScope.AssignmentIssued, Detonators,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
            };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("tyler-detonators", DataThieves, "detonators", DetonatorTemplate, Detonators)
            };
            mission.Enable(scene);
        }

        /// <summary>Tyler walked out to the entrance: the public encounter on his spawn, the route through the caverns.</summary>
        private static void CaveExtractionRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CaveExtraction, "Cave Extraction", 526, Tyler, Tyler, 14, Category);
            mission.Reward(14000, 2100, selectableItems: new[] { Item(VirulentLeechGun, VitaliusWeapon), Item(MotorAssistLegs, Pathogex) });
            mission.Prerequisite(DataThieves);
            mission.Objective(1, 1194, 7987, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, EscortArrived, counter: EscortScenario);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Scenario(EscortScenario, "Tyler walked to the entrance", EscortArrived);
            mission.Indicator(1, ExitMarker, Entrance.X, Entrance.Y, Entrance.Z, 15);
            Evidence(mission, "Tyler gives it in the vault and takes it at the entrance: the public encounter on his spawn " +
                $"(pool{TylerPool}) walks him the route minos-caverns (MinosCaveExtractionScene), and his arrival is event1 of " +
                "scenario1. He waits there, his conversation open again, until the reward, then goes back to the vault. The marker " +
                "is the client's Minos Caverns Exit (10000004). Vitalius Virulent Leech Gun" + VirulentLeechGun + " with Steal " +
                $"Health[2]{VitaliusWeapon} (a leech gun's Vitalius), Pathogex Motor Assist Armor Legs{MotorAssistLegs} with Resist " +
                $"Virulent[2]{Pathogex}: the wiki's rewards.");
            var scene = Scene();
            scene.Script = "minos.cave-extraction";
            scene.Actors["tyler"] = new SceneActorDefinition("tyler", SceneActorKind.PublicSpawn, TylerPool);
            scene.Routes["minos-caverns"] = TylerRoute();
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[1] = new SceneSequenceDefinition();
            scene.PublicEncounter = new PublicEncounterBinding(CaveExtraction, TylerPool, "tyler", scene.Script, OwnerLossPolicy: "Fail");
            mission.Enable(scene);
        }

        /// <summary>Eight of the nine nests, counted in the caverns only.</summary>
        private static void WarnetBarrierRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, WarnetBarrier, "Destroying the Warnet Barrier", 453, Foster, Foster, 14, Category);
            mission.Reward(17000, 2550, selectableItems: new[] { Item(BioBoots, Teleract), Item(LaserChaingun, VextronicsPhotonic), Item(GravitonVest, Luminar) });
            mission.Objective(1, 468, 8022, 1, MissionObjectiveState.Incomplete, counterTextId: 1607);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ObjectDestroyed, WarnetNestClass, counter: 0, target: NestsToDestroy);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, EntranceMarker, CaveMouth.X, CaveMouth.Y, CaveMouth.Z, 15);
            Evidence(mission, "Corporal Foster (name3016) by Delta Outpost's east gate gives and takes it. 'Destroy 8 warnet nests in " +
                "Minos Caverns' (8022): the nests (class7099) placed in the caverns, counted on map 1347 alone, as 'Warnet Nests " +
                "Destroyed' (1607; the client's 'Warnet Nests Remaining' 20000007 counts the other way, which the counters do not). " +
                $"Teleract Bio Armor Boots{BioBoots} with Armor Absorption[2]{Teleract}, Vextronics Laser Chaingun{LaserChaingun} with " +
                $"Debuff Photonic Resist[2]{VextronicsPhotonic} (a laser's damage), Luminar Graviton Armor Vest{GravitonVest} with Resist " +
                $"Photonic[2]{Luminar}: the wiki's rewards, a choice of one.");
            var scene = Scene();
            scene.ObjectiveRequirements[1] = new MapRequirement(new[] { Map });
            mission.Enable(scene);
        }

        /// <summary>Tyler's spawn pool (MinosCavernsContent.OtherPools).</summary>
        public const uint TylerPool = 568041;

        /// <summary>
        /// Tyler's way out, on the navmesh built with a 1.5 m climb (the caverns' floors are rough;
        /// the 0.9 m build left the vault, the main cavern and the entrance tunnel in islands):
        /// from his spot down the river to the vault's south end and back up its west side, up the
        /// slope at its north-east onto the main cavern's south passage, round the east turn and
        /// west along the cavern to the tee, up the entrance tunnel past the medic to where a
        /// player arrives. About 560 m. Each leg is a walk the navmesh allows
        /// (MinosCavernsMissionsTests).
        /// </summary>
        public static SceneRoute TylerRoute() => new("minos-caverns", new SceneWaypoint[]
        {
            new(new ScenePosition(58.0f, -30.41f, -49.0f)),
            new(new ScenePosition(74.27f, -25.54f, -79.8f)),
            new(new ScenePosition(59.0f, -24.84f, -111.01f)),
            new(new ScenePosition(34.0f, -19.73f, -106.0f)),
            new(new ScenePosition(20.0f, -15.6f, -83.0f)),
            new(new ScenePosition(19.81f, -11.13f, -47.72f)),
            new(new ScenePosition(39.0f, -4.68f, -19.0f)),
            new(new ScenePosition(52.0f, 1.88f, -20.0f)),
            new(new ScenePosition(64.0f, 11.65f, -14.0f)),
            new(new ScenePosition(80.0f, 16.84f, -10.0f)),
            new(new ScenePosition(87.74f, 14.47f, 27.11f)),
            new(new ScenePosition(100.0f, 14.31f, 51.0f)),
            new(new ScenePosition(96.0f, 16.64f, 66.0f)),
            new(new ScenePosition(79.0f, 13.33f, 72.0f)),
            new(new ScenePosition(66.0f, 16.64f, 66.0f)),
            new(new ScenePosition(59.62f, 14.03f, 43.62f)),
            new(new ScenePosition(33.0f, 13.27f, 30.0f)),
            new(new ScenePosition(5.02f, 13.56f, 25.98f)),
            new(new ScenePosition(-2.0f, 15.0f, 35.0f)),
            new(new ScenePosition(-1.0f, 19.45f, 44.0f)),
            new(new ScenePosition(-2.0f, 23.0f, 59.0f)),
            new(new ScenePosition(-2.88f, 30.85f, 88.98f)),
            new(new ScenePosition(-4.0f, 30.81f, 107.0f)),
            new(new ScenePosition(-15.8f, 29.8f, 122.6f))
        }, Speed: 4.5f);

        /// <summary>A mission's binding: for the bootcamp's graduates, as the Wilderness and Ojasa missions are.</summary>
        private static MissionSceneDefinition Scene() => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed")
        };

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Minos_Caverns",
                "The Minos Caverns walkthrough and mission pages: route, levels, rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "MinosCavernsMissions", reconstruction);
        }
    }
}
