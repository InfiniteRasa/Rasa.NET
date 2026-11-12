using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Beware: Buried Gas Lines (native mission 913), Valverde Marshes: Lieutenant Brazos at Paludos gives the player
    /// detonators to blow up the supply joints of the Bane gas pipelines in the far east of the Marshes.
    ///
    /// Native: texts 4883-4886 and 9333 (missionconversation): log "Use the detonators to blow up the 8 Bane pipeline supply
    /// joints in the pipeline fields of the eastern Marshes to disrupt gas delivery to the Bane Supply Depot. Report back to
    /// Lieutenant Brazos", offer "The pipelines are located in the far east, in the most polluted section of the marshes
    /// behind enemy lines ... locate the pipeline supply joints and use these detonators to take them out. Be careful! The
    /// Bane have released a nanovirus in the area that will weaken your armor.", turn-in "The lines are destroyed? Good
    /// work!"; objectives 1-4 "Destroy the first/second/third/fourth pipeline supply joint" 4956/9334, 9377/9378, 9380/9381,
    /// 9383/9384, counters 4957 "First Pipeline Destroyed", 9379, 9382, 9385 (missionobjective); no objective conversation.
    /// Item Detonator class 21538 MisMarshesPipelineBomb (templates 42483, 42484; stack 5). Bomb acceptor 7334
    /// UsableItemAccHumBombV01 "Detonation Device" (augmentation 43, use argument 1, usabledata). Name 8595 Lieutenant
    /// Brazos. Indicator name 138 "Pipeline Supply Point" (missionobjectiveindicatorlanguage). Map Valverde Marshes: the
    /// ArchBanePipeline* statics east of the Bane Refueling Station, ends at (732, -276), (860, -148), (860, -68), (700, 116).
    ///
    /// Sourced: TaRapedia Beware: Buried Gas Lines: giver and receiver Lieutenant Brazos (Paludos); requires Reaping the
    /// Harvest; level 37; 140,000 XP, no credits; one item of level 33 plus one of four of level 35 (no names; an old
    /// revision names "Grey Market Motor Assist Armor Boots"). TaRapedia updates 2007-07-03: "Unable to play explosives loc
    /// 734, 213, -149"; 2008-03-26 (and the 1.6 patch notes): "Fixed ... to only allow players to use bomb receptacles once
    /// while on mission". The patch note files it under "Marshes", not the Bane Supply Depot: open world (the triage's
    /// conflict 11).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Four joints of two bomb receptacles each (the log's 8, the client's 4 objectives: the triage's conflict 2), at the
    ///    pipeline ends (732, -276), (734, -149) (TaRapedia's place, by the map turn at 732, -156), (860, -68) and (700, 107)
    ///    (between its two receptacles, 9 m south of the map end at 700, 116),
    ///    one receptacle on each side, 10-15 m apart, on the navmesh (probe of 2026-11-02). Personal acceptors (7334), each
    ///    used once: it takes a detonator and counts for its joint; two complete the joint's objective. Any order.
    ///  - Eight detonators (template 42483, the lower of the class's two) at the accept, taken back at an abandon; no room for
    ///    them, no accept (PoolsThingsThatBlowUpV1). No explosion and no pipeline wreck object. The nanovirus armour debuff is
    ///    left out (the triage's Q34: no area debuff in the code). A marker, the client's "Pipeline Supply Point", on each
    ///    joint.
    ///  - Brazos MarshesHarvestersNpcsV1's. The fixed level-33 item Motor Assist boots (the old revision's name; the band
    ///    that holds 33), the four to choose as MarshesHasslingTheHarvestersV1's (the band that holds 35, Q37).
    /// </summary>
    public static class MarshesBuriedGasLinesV1
    {
        public const uint MissionId = 913;
        public const uint Level = 37;
        public const uint Brazos = MarshesHarvestersNpcsV1.Brazos;
        public const uint Joints = 4, PerJoint = 2;

        public const uint DetonatorTemplate = 42483, Detonators = 8;   // Detonator, class 21538
        public const string DetonatorKey = "pipeline-detonator";
        public const uint ReceptacleClass = 7334;       // UsableItemAccHumBombV01 "Detonation Device"
        public const uint ReadyState = 119, PlantedState = 121;
        public const uint UseArgument = 1;
        public const uint PipelineSupplyPoint = 138;    // missionobjectiveindicatorlanguage
        public const double JointRadius = 20;

        public const uint Experience = 140000;
        public const uint Credits = 0;
        public const uint MotorAssistBoots = 28614;     // Armor_T1_MotorAssist_V04_UNC_Boots_33_to_37, with every choice
        public const uint BioBoots = MarshesHasslingTheHarvestersV1.BioBoots;
        public const uint GravitonBoots = MarshesHasslingTheHarvestersV1.GravitonBoots;
        public const uint MechHelmet = MarshesHasslingTheHarvestersV1.MechHelmet;
        public const uint StealthVest = MarshesHasslingTheHarvestersV1.StealthVest;

        /// <summary>The four joints: objective (its scenario too), name, body, counter, the pipeline point and its two receptacles.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, ScenePosition At, ScenePosition[] Receptacles)[] JointRows =
        {
            (1, 4956, 9334, 4957, new(732f, 213.6f, -276f), new ScenePosition[] { new(720f, 213.56f, -276f), new(744f, 213.69f, -276f) }),
            (2, 9377, 9378, 9379, new(734f, 214.7f, -149f), new ScenePosition[] { new(722f, 215.38f, -149f), new(746f, 214f, -161f) }),
            (3, 9380, 9381, 9382, new(860f, 216.4f, -68f), new ScenePosition[] { new(848f, 216.28f, -68f), new(872f, 216.61f, -68f) }),
            (4, 9383, 9384, 9385, new(700f, 218.1f, 107f), new ScenePosition[] { new(688f, 216.33f, 104f), new(712f, 219.86f, 110f) }),
        };

        public static string ReceptacleRole(int joint, int side) => $"joint-{joint + 1}-receptacle-{side + 1}";
        public static uint ReceptacleSequence(int joint, int side) => 1 + (uint)(joint * PerJoint + side);

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Beware: Buried Gas Lines", 4883, Brazos, Brazos, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechHelmet, StealthVest }, new[] { MotorAssistBoots });
            mission.Prerequisite(MarshesReapingTheHarvestV1.MissionId);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(DetonatorKey, DetonatorTemplate, MissionItemScope.AssignmentIssued, Detonators,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-detonators", MissionId, DetonatorKey, DetonatorTemplate, Detonators) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            for (var joint = 0; joint < JointRows.Length; joint++)
            {
                var (objective, name, body, counter, at, receptacles) = JointRows[joint];
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Scenario(objective, $"Pipeline supply joint {objective} blown", PerJoint);
                mission.Transition(objective);
                for (var side = 1U; side <= PerJoint; side++)
                    mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, side, PerJoint, counter: objective, trigger: side);
                if (joint == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.NamedIndicator(objective, PipelineSupplyPoint, at.X, at.Y, at.Z, JointRadius);

                for (var side = 0; side < receptacles.Length; side++)
                {
                    var role = ReceptacleRole(joint, side);
                    var sequence = ReceptacleSequence(joint, side);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ReceptacleClass, receptacles[side],
                        InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                    scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                    scene.Sequences[sequence] = new SceneSequenceDefinition
                    {
                        World =
                        {
                            new TransitionObjectStateIntent($"plant-{role}", role, PlantedState),
                            new SetInteractionIntent($"close-{role}", role, false)
                        },
                        Character = { new ConsumeMissionItemIntent($"{role}-detonator", MissionId, DetonatorKey, 1, MissionItemScope.AssignmentIssued) },
                        Signals = { new SceneMissionSignal(MissionId, objective, (uint)side + 1) }
                    };
                }
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/913",
                "Native1-4 'Destroy the first..fourth pipeline supply joint' 4956/9334, 9377/9378, 9380/9381, 9383/9384, counters " +
                "4957, 9379, 9382, 9385; no objectiveconversation. Texts 4883-4886, 9333 (log: 8 joints, pipeline fields of the " +
                "eastern Marshes, Brazos). Item class21538 Detonator (42483/42484). Acceptor 7334 'Detonation Device'. Name8595 " +
                "Lieutenant Brazos. Indicator name138 'Pipeline Supply Point'. Map: ArchBanePipeline* ends east of the Refueling Station.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Beware%3A_Buried_Gas_Lines",
                "Giver/receiver Lieutenant Brazos (Paludos); requires Reaping the Harvest; level37; 140000XP, no credits; one lvl33 " +
                "item plus one of four lvl35 (old revision: Grey Market Motor Assist Armor Boots). Updates 2007-07-03 a joint at " +
                "734, 213, -149; 2008-03-26 / patch 1.6 bomb receptacles once per player while on the mission (filed under Marshes).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesBuriedGasLinesV1",
                "Open world. 4 joints x 2 personal acceptors 7334 at the pipeline ends (732,-276), (734,-149), (860,-68), (700,107), " +
                "each once, a detonator each, any order; 8 detonators (42483) at accept, taken at abandon. No debuff, no wreck. " +
                "Brazos 760312 a copy of Morrison by the Paludos med tent. Items 28614 fixed + one of 18750, 23549, 8855, 10637.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
