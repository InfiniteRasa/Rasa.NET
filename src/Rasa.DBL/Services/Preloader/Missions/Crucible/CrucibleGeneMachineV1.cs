using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Gene Machine (native mission 1857), Ligo Crucible: Zupa Eugin sends the player to Labbna Grupa Rigs by the Labbna
    /// salvage yard outside Staal, for the parts of his gene splicer: a Rendylitic Calibrator, a Biological Diagnostic Unit and
    /// three Vanadium Redox Power Cells, searched for in the yard's piles of junk.
    ///
    /// Native: texts 18916-18920 (missionconversation); objective 15 "Speak to Grupa Rigs." 19187/19188, 1 "Obtain a Rendylitic
    /// Calibrator" 19061/19062, 2 "Get a Biological Diagnostic Unit" 19063/19064, 3 "Get 3 Vanadium Redox Power Cells"
    /// 19065/19066, 4-14 "no desc" 19067-19088 (hidden), no counters (missionobjective); objective conversation (1857,15)
    /// package 1231 (Rigs) text 19189. Items: class 28625 Rendylitic Calibrator (template 118991), 28626 Biological Diagnostic
    /// Unit (118992), 28627 Vanadium Redox Power Cell (118993). Usable name 272 "Salvaged Machine Parts" (CRUCIBLE_VIDDEA_JUNK);
    /// indicator name 420 "Box O' Junk".
    ///
    /// Sourced: TaRapedia The Gene Machine: Eugin gives and rewards; requires Dark Genesis; follow-up Designer Genes; 75,000
    /// XP, 5,700 credits; Hellstrom Pulse Rifle, Vitalius Sonic RPG Launcher, AccuMax Cryogenic Net Gun or Eclipse Virulent
    /// Leech Gun; item places (x, z) Calibrator (-431, 276), Diagnostic Unit (-435, 267), Power Cells (-451, 247), (-401, 220),
    /// (-413, 253); four Silver Barb Ticks ambush; Rigs "unlocks" the junk; search every object (cylinders, boxes, small
    /// devices, two larger consoles); "half of what you find will be junk" (cache, 0.5). TTH: the salvage yard outside the base
    /// in the open air; one sample of the random spots: Calibrator (-431, 250, 276), Diagnostic Unit (-437, 249, 263), cells
    /// (-446, 250, 273), (-399, 249, 221), (-419, 251, 257) (0.5). TR MMO DB level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Eleven personal piles in the yard (TTH's five spots, each moved 2 m or less onto a client floor seen from 16 of 16
    ///    sides, and six of junk, probe 2026-11-08): Brann crates 7931 / 7934 and two Brann consoles 24980 (the "larger
    ///    consoles"), name 272, use argument 1, 200 -> 201. The spots are fixed: the server has no random pile spawn. Each pile
    ///    is the use of the objective it serves (the calibrator's 1, the unit's 2, the cells' and the junk 3), so none can be
    ///    searched before Rigs' talk opens 1-3. The client's eleven hidden objectives (4-14, one per pile, ESTIMATE) are not
    ///    needed so: hidden 4 counts the three cells (3 has no client counter text), 5-14 are not written.
    ///  - A pile with a part gives it (a mission item, n/3 on 3 for the cells) and completes its objective; a junk pile gives
    ///    nothing. Eugin takes the parts at the turn-in.
    ///  - The ambush: four Silver Barb Ticks (upstream 540028, Thunderhead, level 40) come out 30 m west of the Diagnostic
    ///    Unit's pile as it is searched, at the player.
    ///  - Markers: Rigs; one round the yard for 1, 2 and 3.
    ///  - The items: the client's Uncommon rows of TaRapedia's kinds in the band of level 35-40 (Rifle EMP 36-40 14780,
    ///    RocketLauncher Sonic 34-38 49168, NetGun Ice 36-40 48396, LeechGun Virulent 39-43 15301), one to choose.
    /// </summary>
    public static class CrucibleGeneMachineV1
    {
        public const uint MissionId = 1857;
        public const uint Level = 35;
        public const uint Eugin = CrucibleHybridArcNpcsV1.Eugin;
        public const uint Rigs = CrucibleOutpostIntrepidNpcsV1.Rigs, RigsPackage = CrucibleOutpostIntrepidNpcsV1.RigsPackage;
        public const uint SpeakToRigs = 15, Calibrator = 1, DiagnosticUnit = 2, PowerCells = 3, CellsTally = 4;
        /// <summary>Scenarios: the calibrator found, the unit found, a cell found (the step its pile's place among the cells').</summary>
        public const uint CalibratorFound = 1, DiagnosticUnitFound = 2, CellFound = 3;

        public const uint CalibratorTemplate = 118991, CalibratorClass = 28625;
        public const uint DiagnosticUnitTemplate = 118992, DiagnosticUnitClass = 28626;
        public const uint PowerCellTemplate = 118993, PowerCellClass = 28627, PowerCellCount = 3;
        public const string CalibratorKey = "rendylitic-calibrator", DiagnosticUnitKey = "biological-diagnostic-unit",
            PowerCellKey = "vanadium-redox-power-cell";

        public const uint CrateClass = 7931, CrateClass2 = 7934, ConsoleClass = 24980;  // UsableTreasureDispBrannCrateV01/V02, ...ConsoleV01
        public const uint PileName = 272;               // usablenameoverride CRUCIBLE_VIDDEA_JUNK "Salvaged Machine Parts"
        public const uint UseArgument = 1;
        public const uint ClosedState = 200, OpenedState = 201;

        /// <summary>The eleven piles: the objective whose use it is, class, place, the item in it (null: junk).</summary>
        public static readonly (uint Objective, uint Class, ScenePosition At, string Item)[] Piles =
        {
            (Calibrator, CrateClass, new(-431f, 250.47f, 276f), CalibratorKey),
            (DiagnosticUnit, CrateClass2, new(-436f, 249.49f, 262f), DiagnosticUnitKey),
            (PowerCells, CrateClass, new(-446f, 250.58f, 272f), PowerCellKey),
            (PowerCells, CrateClass2, new(-399f, 250.42f, 221f), PowerCellKey),
            (PowerCells, CrateClass, new(-418f, 251.69f, 255f), PowerCellKey),
            (PowerCells, CrateClass2, new(-423f, 251.13f, 286f), null),
            (PowerCells, ConsoleClass, new(-410f, 249.68f, 265f), null),
            (PowerCells, CrateClass, new(-450f, 249.61f, 258f), null),
            (PowerCells, CrateClass2, new(-408f, 249.48f, 239f), null),
            (PowerCells, ConsoleClass, new(-440f, 249.68f, 240f), null),
            (PowerCells, CrateClass, new(-417f, 250.37f, 227f), null)
        };

        /// <summary>The ambush: four Silver Barb Ticks 30 m west of the Diagnostic Unit's pile (probe 2026-11-08).</summary>
        public const uint Tick = 540028;                // Barb Tick - Thunderhead, name 8143 "Silver Barb Tick"
        public const int AmbushPile = 1;                // the Diagnostic Unit's, Piles[1]
        public static readonly ScenePosition[] TicksAt =
        {
            new(-465f, 250.59f, 255f), new(-468f, 250.64f, 259f), new(-462f, 250.21f, 258f), new(-466f, 250.71f, 256f)
        };

        /// <summary>The yard's marker: round all eleven piles.</summary>
        public static readonly (double X, double Y, double Z) YardAt = (-424, 250, 252);
        public const double YardRadius = 45, RigsRadius = 8;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint PulseRifle = 14780;           // Weapon_Avatar_Rifle_EMP_UNC_36_to_40
        public const uint SonicRpgLauncher = 49168;     // Weapon_Avatar_RocketLauncher_Sonic_UNC_34_to_38
        public const uint CryogenicNetGun = 48396;      // Weapon_Avatar_NetGun_Ice_UNC_36_to_40
        public const uint VirulentLeechGun = 15301;     // Weapon_Avatar_LeechGun_Virulent_UNC_39_to_43

        public static string PileRole(int index) => $"labbna-junk-{index + 1}";
        public static string TickRole(int index) => $"silver-barb-tick-{index + 1}";
        /// <summary>A pile's use sequence: its place in <see cref="Piles"/>, from 10 (above the scenarios').</summary>
        public static uint PileSequence(int index) => 10 + (uint)index;
        /// <summary>The cell piles' steps of <see cref="CellFound"/>: 1-3, by their place among the cells.</summary>
        public static uint CellStep(int index) => (uint)index - 1;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Gene Machine", 18916, Eugin, Eugin, Level);
            mission.Reward(Experience, Credits, new[] { PulseRifle, SonicRpgLauncher, CryogenicNetGun, VirulentLeechGun });
            mission.Prerequisite(CrucibleDarkGenesisV1.MissionId);
            mission.Scenario(CalibratorFound, "The Rendylitic Calibrator found", 1);
            mission.Scenario(DiagnosticUnitFound, "The Biological Diagnostic Unit found", 1);
            mission.Scenario(CellFound, "A Vanadium Redox Power Cell found", PowerCellCount);

            mission.Objective(SpeakToRigs, 19187, 19188, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Calibrator, 19061, 19062, 1, MissionObjectiveState.Inactive);
            mission.Objective(DiagnosticUnit, 19063, 19064, 2, MissionObjectiveState.Inactive);
            mission.Objective(PowerCells, 19065, 19066, 3, MissionObjectiveState.Inactive);
            mission.Objective(CellsTally, 19067, 19068, 4, MissionObjectiveState.Incomplete, required: false);   // hidden

            // 15: Rigs lets the player at the junk: 1-3 open.
            mission.Transition(SpeakToRigs);
            mission.Conversation(SpeakToRigs, RigsPackage);
            var action = 1U;
            foreach (var part in new[] { Calibrator, DiagnosticUnit, PowerCells })
            {
                CrucibleCalendarGirlV1.Open(mission, SpeakToRigs, 1, action, part);
                action += 2;
            }
            mission.Action(SpeakToRigs, action, MissionActionKind.GrantReward, reward: 1);

            // 1 and 2 with their pile.
            mission.Transition(Calibrator);
            mission.Progress(Calibrator, MissionProgressEventKind.ScenarioEvent, 1, null, counter: CalibratorFound);
            mission.Transition(DiagnosticUnit);
            mission.Progress(DiagnosticUnit, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DiagnosticUnitFound);

            // 3 with its three cells (hidden 4 counts them).
            mission.Transition(CellsTally);
            for (var cell = 1U; cell <= PowerCellCount; cell++)
                mission.Progress(CellsTally, MissionProgressEventKind.ScenarioEvent, cell, PowerCellCount, counter: CellFound, trigger: cell);
            mission.Transition(PowerCells);
            mission.ObjectiveState(PowerCells, CellsTally, MissionObjectiveState.Completed);

            var rigs = CrucibleOutpostIntrepidNpcsV1.At(Rigs);
            mission.Indicator(SpeakToRigs, 1, rigs.X, rigs.Y, rigs.Z, RigsRadius);
            foreach (var part in new[] { Calibrator, DiagnosticUnit, PowerCells })
                mission.Indicator(part, 1 + part, YardAt.X, YardAt.Y, YardAt.Z, YardRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1857",
                "Native15 'Speak to Grupa Rigs.' 19187/19188, 1 'Obtain a Rendylitic Calibrator' 19061/19062, 2 'Get a Biological " +
                "Diagnostic Unit' 19063/19064, 3 'Get 3 Vanadium Redox Power Cells' 19065/19066, 4-14 'no desc' 19067-19088 " +
                "(hidden), no counters; objectiveconversation 1857,15 package1231 text19189. Texts 18916-18920. Items class28625 " +
                "(template118991), 28626 (118992), 28627 (118993). Usable name 272 'Salvaged Machine Parts'; indicator name 420.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Gene_Machine",
                "Eugin gives and rewards; requires Dark Genesis; follow-up Designer Genes; 75000XP, 5700 credits; Hellstrom Pulse " +
                "Rifle, Vitalius Sonic RPG Launcher, AccuMax Cryogenic Net Gun, Eclipse Virulent Leech Gun; item locs -431 276, " +
                "-435 267, cells -451 247, -401 220, -413 253; four Silver Barb Ticks ambush; Rigs unlocks the junk; half is junk. " +
                "TTH: spots -431,250,276; -437,249,263; -446,250,273; -399,249,221; -419,251,257. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleGeneMachineV1",
                "Rigs' talk opens 1-3. Eleven personal piles (7931, 7934, two consoles 24980; name272; arg1; 200->201) at fixed " +
                "spots, five with the parts, six junk; each the use of the objective it serves (junk: 3). 1 and 2 with their pile, 3 " +
                "when hidden 4 counts three cells (n/3); 5-14 not written. Four ticks 540028 at the Diagnostic Unit's pile. Eugin " +
                "takes the parts. Items 14780, 49168, 48396, 15301, one to choose. Markers: Rigs, the yard for 1-3.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own piles of junk in the Labbna salvage yard; the parts they hold; the ticks.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { CellsTally },
                Items = new()
                {
                    new MissionItemBinding(CalibratorKey, CalibratorTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(DiagnosticUnitKey, DiagnosticUnitTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(PowerCellKey, PowerCellTemplate, MissionItemScope.AssignmentIssued, PowerCellCount,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        CounterObjectiveId: PowerCells)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Piles.Length; index++)
            {
                var (objective, classId, at, item) = Piles[index];
                var role = PileRole(index);
                var sequence = PileSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, classId, at, Orientation: index * 1.9,
                    InitialObjectState: ClosedState, NameOverrideId: PileName,
                    UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                var searched = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-searched", role, OpenedState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    }
                };
                if (item != null)
                {
                    var template = item == CalibratorKey ? CalibratorTemplate : item == DiagnosticUnitKey ? DiagnosticUnitTemplate : PowerCellTemplate;
                    searched.Character.Add(new IssueMissionItemIntent($"{role}-{item}", MissionId, item, template, 1));
                    searched.Signals.Add(item == CalibratorKey ? new SceneMissionSignal(MissionId, CalibratorFound, 1)
                        : item == DiagnosticUnitKey ? new SceneMissionSignal(MissionId, DiagnosticUnitFound, 1)
                        : new SceneMissionSignal(MissionId, CellFound, CellStep(index)));
                }
                if (index == AmbushPile)
                    for (var tick = 0; tick < TicksAt.Length; tick++)
                    {
                        var tickRole = TickRole(tick);
                        scene.Actors[tickRole] = new SceneActorDefinition(tickRole, SceneActorKind.Creature, Tick, TicksAt[tick],
                            GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                        searched.World.Add(new EnsureActorIntent($"{tickRole}-comes", tickRole));
                        searched.World.Add(QuestSceneIntents.Attack($"{tickRole}-attacks", tickRole));
                    }
                scene.Sequences[sequence] = searched;
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
