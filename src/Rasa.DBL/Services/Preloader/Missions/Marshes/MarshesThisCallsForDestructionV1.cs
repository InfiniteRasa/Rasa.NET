using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// This Calls For Destruction (native mission 1879), Valverde Marshes: the troop data says more Bane are on the move;
    /// Lt. Kiang sends the player to upload a virus to the computers of the hub outside the Bane Supply Depot, then blow the
    /// generator that feeds it.
    ///
    /// Native: texts 19179-19183 (missionconversation): log "Upload a virus to 3 bane computers then destroy their primary
    /// generator.", offer "upload a virus to several of the computers in the main hub outside of the Bane supply depot, then
    /// destroying the generator the feeds the area", reminder "upload this virus then blow the generator", turn-in "Amazing
    /// work, soldier"; objective 1 "Upload 3 AFS viruses to the Bane Computers in the base outside of Bane Supply Depot."
    /// 19558/19559, counter 19560 "Viruses Uploaded", 2 "Destroy the Main Generator." 19561/19562, no counter
    /// (missionobjective); no objective conversation. Items Bane Computer Virus class 28645 MisMarshesBaneVirus (World DB
    /// template 119006), Generator Bomb class 28644 (119005). Client item templates 121233-121236: Stealth Vest, Bio Vest,
    /// Mech Legs, Graviton Vest 35-39. Client map: three Bane mining control terminals
    /// (ArchBaneGenObjMiningcontrolterminalV01) at (418, 222, -720), (411, 222, -734), (430, 222, -734), south of the depot.
    ///
    /// Sourced: TaRapedia This Calls For Destruction: giver and receiver Lt. Kiang; requires Under the Radar - Part II; level
    /// 37; 65,000 XP, 5,400 credits. Ten Ton Hammer Eastern Marshes: "upload a virus to the computers in the area outside the
    /// main base, and destroy the generator that feeds the area"; 5,400 credits and a choice of Mech Legs or a Stealth, Bio or
    /// Graviton Vest.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Offered once Part II is done in either version (mission flag MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag).
    ///  - The hub: the client map's terminal group south of the depot. Three personal Bane console switches (7468, as Silent
    ///    But Deadly's console, states 55 to 56) beside the map's terminals, on the navmesh and walked to from the Bane
    ///    Assertion Camp waypoint (probe of 2026-11-02); each takes one of the three viruses given at the accept (taken at an
    ///    abandon) and counts once.
    ///  - 2 opens when 1 is done: a personal Bane outpost generator (3857, not targetable in the client) with an Explosive
    ///    Charge (7870) beside it, as Silent But Deadly's; the charge's use blows the generator and completes 2. The Generator
    ///    Bomb item is not given (the use is the detonation), as there.
    ///  - The rewards the client's block 121233-121236, one to choose. Unnamed markers round the consoles and at the generator.
    /// </summary>
    public static class MarshesThisCallsForDestructionV1
    {
        public const uint MissionId = 1879;
        public const uint Level = 37;
        public const uint Kiang = MarshesSentryTowerNpcsV1.Kiang;
        public const uint Upload = 1, Destroy = 2;
        public const uint Uploads = 1, GeneratorScenario = 2;

        public const uint VirusTemplate = 119006, VirusClass = 28645, Viruses = 3;
        public const string VirusKey = "afs-virus";
        public const uint ConsoleClass = Plateau.PlateauSilentButDeadlyV1.ConsoleClass;         // UsableTwoStateBaneConsoleSwitchV01
        public const uint ConsoleReadyState = Plateau.PlateauSilentButDeadlyV1.ConsoleReadyState;
        public const uint ConsoleSetState = Plateau.PlateauSilentButDeadlyV1.ConsoleSetState;
        public const uint GeneratorClass = Plateau.PlateauSilentButDeadlyV1.GeneratorClass;     // UsableInertDestBaneOutpostGenerator
        public const uint ChargeClass = Plateau.PlateauSilentButDeadlyV1.ChargeClass;           // UsableBombHumV01
        public const uint IntactState = Plateau.PlateauSilentButDeadlyV1.IntactState;
        public const uint DestroyedState = Plateau.PlateauSilentButDeadlyV1.DestroyedState;
        public const uint GeneratorHitPoints = Plateau.PlateauSilentButDeadlyV1.GeneratorHitPoints;
        public const uint UseArgument = Plateau.PlateauSilentButDeadlyV1.UseArgument;
        public const string GeneratorRole = "main-generator", ChargeRole = "generator-charge";
        public const uint Blown = 4;                    // the generator's scene sequence, after the three consoles

        public static readonly ScenePosition[] Consoles =
        {
            new(415f, 222.22f, -715f),
            new(410f, 222.08f, -735.2f),
            new(435f, 222.08f, -735f),
        };
        public static readonly (double X, double Y, double Z) HubAt = (420, 222.1, -728);
        public const double HubRadius = 20;
        public static readonly ScenePosition GeneratorAt = new(400f, 222.09f, -730f);
        public static readonly ScenePosition ChargeAt = new(405f, 222.08f, -730f);
        public const double GeneratorRadius = 10;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint StealthVest = 121233;         // Armor_T3_Stealth_V04_UNC_Vest_35_to_39
        public const uint BioVest = 121234;             // Armor_T3_Bio_V04_UNC_Vest_35_to_39
        public const uint MechLegs = 121235;            // Armor_T3_Mech_V04_UNC_Legs_35_to_39
        public const uint GravitonVest = 121236;        // Armor_T3_Graviton_V04_UNC_Vest_35_to_39

        public static string ConsoleRole(int index) => $"bane-computer-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "This Calls For Destruction", 19179, Kiang, Kiang, Level);
            mission.Reward(Experience, Credits, new[] { StealthVest, BioVest, MechLegs, GravitonVest });
            mission.FlagPrerequisite(MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag, 1, "Requires Under the Radar - Part II (1876 or 1877)");

            mission.Objective(Upload, 19558, 19559, 0, MissionObjectiveState.Incomplete, counterTextId: 19560);
            mission.Objective(Destroy, 19561, 19562, 1, MissionObjectiveState.Inactive);
            mission.Scenario(Uploads, "A virus uploaded to a Bane computer", (uint)Consoles.Length);
            mission.Scenario(GeneratorScenario, "The main generator blown", 1);

            mission.Transition(Upload);
            for (var id = 1U; id <= Consoles.Length; id++)
                mission.Progress(Upload, MissionProgressEventKind.ScenarioEvent, id, Viruses, counter: Uploads, trigger: id);
            mission.Action(Upload, 1, MissionActionKind.RevealObjective, targetObjective: Destroy);
            mission.Action(Upload, 2, MissionActionKind.ActivateObjective, targetObjective: Destroy, state: MissionObjectiveState.Incomplete);

            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, 1, null, counter: GeneratorScenario);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Upload, 1, HubAt.X, HubAt.Y, HubAt.Z, HubRadius);
            mission.Indicator(Destroy, 2, GeneratorAt.X, GeneratorAt.Y, GeneratorAt.Z, GeneratorRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1879",
                "Native1 'Upload 3 AFS viruses to the Bane Computers in the base outside of Bane Supply Depot.' 19558/19559 " +
                "counter19560 'Viruses Uploaded', 2 'Destroy the Main Generator.' 19561/19562; no objectiveconversation. Texts " +
                "19179-19183. Items class28645 Bane Computer Virus (template119006), class28644 Generator Bomb (119005). Map " +
                "terminals (418,-720), (411,-734), (430,-734). Templates 121233-121236.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/This_Calls_For_Destruction",
                "Giver/receiver Lt. Kiang; requires Part II; level37; 65000XP, 5400 credits; computers in the main hub outside " +
                "the Bane supply depot, then the generator. Ten Ton Hammer: computers outside the main base, the generator that " +
                "feeds the area; Mech Legs or Stealth/Bio/Graviton Vest.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesThisCallsForDestructionV1",
                "Requires flag 1876001. Three personal Bane consoles 7468 (arg1, 55->56) by the map terminals, a virus (119006, " +
                "3 at accept) each; then 2: generator 3857 at (400,-730) blown by its charge 7870 (arg1), no bomb item. Items " +
                "the client block 121233-121236, one to choose. Markers round the consoles and at the generator.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(VirusKey, VirusTemplate, MissionItemScope.AssignmentIssued, Viruses,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-viruses", MissionId, VirusKey, VirusTemplate, Viruses) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Consoles.Length; index++)
            {
                var sequence = (uint)index + 1;
                var role = ConsoleRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ConsoleClass, Consoles[index],
                    Orientation: index * 2.0943951024, InitialObjectState: ConsoleReadyState,
                    UseAction: new SceneObjectAction(MissionId, Upload, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"upload-{role}", role, ConsoleSetState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-virus", MissionId, VirusKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, Uploads, sequence) }
                };
            }
            scene.Actors[GeneratorRole] = new SceneActorDefinition(GeneratorRole, SceneActorKind.Object, GeneratorClass, GeneratorAt,
                InitiallyInteractable: false, InitialObjectState: IntactState,
                Destruction: new SceneObjectDestruction(MissionId, Destroy, Blown, GeneratorHitPoints, DestroyedState));
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                UseAction: new SceneObjectAction(MissionId, Destroy, Blown, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-generator", GeneratorRole));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-charge", ChargeRole));
            scene.Sequences[Blown] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("charge-set", ChargeRole, false),
                    new TransitionObjectStateIntent("generator-blown", GeneratorRole, DestroyedState)
                },
                Signals = { new SceneMissionSignal(MissionId, GeneratorScenario, 1) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
