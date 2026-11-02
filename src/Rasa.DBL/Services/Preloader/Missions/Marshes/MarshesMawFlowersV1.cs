using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Stop and Smell the Maw Flowers (native mission 1871), Valverde Marshes: Retread Duvall, on guard duty, wants five of
    /// the flowers the Maw shed north of High Point Retreat.
    ///
    /// Native: texts 19139-19143 (missionconversation): log "Head north and gather 5 Maw flowers.", offer "The Maw around here
    /// have these flowers on the top of their heads ... I can't go out and harvest these flowers that the Maw was shed over
    /// time", reminder "just north of here", turn-in "I'll be off k-rations"; objective 1 "Gather Maw Flowers north of High
    /// Point Retreat." 19446/19447, no counter (missionobjective); no objective conversation. Item MisMarshesMawFlower class
    /// 28643 (World DB template 119004). Client item templates 121225-121228: Area Repair Tool 35-39, Sonic Torqueshell
    /// Rifle 33-37, Rifle 36-40, Sonic Propellant Gun 37-41.
    ///
    /// Sourced: TaRapedia Stop and Smell the Maw Flowers: giver Retread Duvall, reward from Retread Vincent; no requirement;
    /// 65,000 XP, 5,400 credits; one of ChiTech Area Repair Tool 35, Vextronics Sonic Torqueshell Rifle 33, Teleract Rifle 36,
    /// Teleract Sonic Disperser 37 (the client block in that order); "make sure a maw isn't around when you try to take
    /// their flowers".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Duvall (the triage's Q19: the client turn-in text is his; TaRapedia's Vincent stands in Retread City).
    ///    Level 36 (TaRapedia none).
    ///  - Five personal shed flowers (the client's Mawflower item givers 22518-22522, Medicinal Measures' way: argument 3,
    ///    states 81 to 82), one flower each; a hidden tally (objective 2, under 1's texts: 1 has no client counter text) counts
    ///    five and 1 completes with it. North of the camp is a navmesh island (probe of 2026-11-02): the flowers lie on the
    ///    walked-to slope north-east of the waypoint, 8-14 m apart, with the Maw pool of two (MarshesStalkerWoodsNpcsV1) among
    ///    them. Duvall takes the five at the turn-in; an abandon takes them.
    ///  - The rewards the client's block 121225-121228, one to choose. One marker round the flowers.
    /// </summary>
    public static class MarshesMawFlowersV1
    {
        public const uint MissionId = 1871;
        public const uint Level = 36;
        public const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall;
        public const uint Gather = 1, Tally = 2;
        public const uint FlowersTaken = 1;

        public const uint FlowerTemplate = 119004, FlowerClass = 28643, Flowers = 5;
        public const string FlowerKey = "maw-flower";
        public static readonly uint[] GiverClasses = Pools.PoolsMedicinalMeasuresV1.PlantClasses;   // UsableItemDispMawflowerV01..V05
        public const uint UseArgument = Pools.PoolsMedicinalMeasuresV1.UseArgument;
        public const uint ReadyState = Pools.PoolsMedicinalMeasuresV1.ReadyState, TakenState = Pools.PoolsMedicinalMeasuresV1.TakenState;
        public static readonly ScenePosition[] FlowersAt =
        {
            new(-752f, 232.16f, -508f),
            new(-744f, 231.53f, -512f),
            new(-748f, 233.36f, -520f),
            new(-740f, 229.5f, -520f),
            new(-744f, 235.08f, -524f),
        };
        public static readonly (double X, double Y, double Z) FieldAt = (-748, 232.5, -516);
        public const double FieldRadius = 15;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint AreaRepairTool = 121225;      // Tool_Avatar_Field_Repair_Area_UNC_35_to_39
        public const uint SonicTorqueshellRifle = 121226; // Weapon_Avatar_Torqueshell_Rifle_Sonic_RAR_33_to_37
        public const uint Rifle = 121227;               // Weapon_Avatar_Rifle_Physical_UNC_36_to_40
        public const uint SonicDisperser = 121228;      // Weapon_Avatar_PropellantGun_Sonic_UNC_37_to_41

        public static string FlowerRole(int index) => $"maw-flower-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Stop and Smell the Maw Flowers", 19139, Duvall, Duvall, Level);
            mission.Reward(Experience, Credits, new[] { AreaRepairTool, SonicTorqueshellRifle, Rifle, SonicDisperser });

            mission.Objective(Gather, 19446, 19447, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 19446, 19447, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(FlowersTaken, "A shed Maw flower taken", (uint)FlowersAt.Length);
            mission.Transition(Gather);
            mission.ObjectiveState(Gather, Tally, MissionObjectiveState.Completed);
            mission.Action(Gather, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Tally);
            for (var id = 1U; id <= FlowersAt.Length; id++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Flowers, counter: FlowersTaken, trigger: id);
            mission.Indicator(Gather, 1, FieldAt.X, FieldAt.Y, FieldAt.Z, FieldRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1871",
                "Native1 'Gather Maw Flowers north of High Point Retreat.' 19446/19447, no counter; no objectiveconversation. " +
                "Texts 19139-19143 (5 flowers the Maw shed, just north of here). Item class28643 MisMarshesMawFlower (template " +
                "119004). Templates 121225-121228.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Stop_and_Smell_the_Maw_Flowers",
                "Giver Retread Duvall, reward from Retread Vincent; 65000XP, 5400 credits; one of ChiTech Area Repair Tool35, " +
                "Vextronics Sonic Torqueshell Rifle33, Teleract Rifle36, Teleract Sonic Disperser37; no Maw around when taking " +
                "the flowers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesMawFlowersV1",
                "Receiver Duvall (Q19); level36. Five personal flower givers 22518-22522 (arg3, 81->82) on the walked-to slope " +
                "north-east of High Point Retreat (north is a navmesh island), a flower (119004) each; hidden tally2 counts five; " +
                "a Maw pool of two among them. Items the client block 121225-121228, one to choose. One marker.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(FlowerKey, FlowerTemplate, MissionItemScope.AssignmentIssued, Flowers,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: Flowers)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < FlowersAt.Length; index++)
            {
                var sequence = (uint)index + 1;
                var role = FlowerRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, GiverClasses[index % GiverClasses.Length],
                    FlowersAt[index], Orientation: index * 1.2566370614, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Gather, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"pick-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-flower", MissionId, FlowerKey, FlowerTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, FlowersTaken, sequence) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
