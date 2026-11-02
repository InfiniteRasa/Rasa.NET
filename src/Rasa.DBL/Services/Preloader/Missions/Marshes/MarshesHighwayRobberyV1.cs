using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Highway Robbery (native mission 914), Valverde Marshes: Retread Vincent in Retread City wants the AFS's medical
    /// supplies stolen from their med tents at Paludos or Falcon Hold - or the player tells the AFS (Commander Figgins). Either
    /// way the player reports back to Vincent, who answers the choice.
    ///
    /// Native: texts 4887-4890, 6406 (missionconversation): log "Steal medical supplies from the med tents in Paludos or
    /// Falcon's Hold and return them to Retread Vincent-- or report his request to the commanding officer in the Falcon
    /// Hold. No matter your choice, return to Retread Vincent", finishing "Your behavior is exactly what I expected";
    /// objectives 1 "Option: Steal Medical Supplies from the AFS hospitals" 5733/6407, 2 "Option: Rat out the Retreads"
    /// 5734/6408, 3 "no desc" 5744/6409, 4 "Report back to Retread Vincent" 5745/6410 (missionobjective); objective
    /// conversations 914,2 package 1011 5735 ("Damn Retreads! ... I'll go send a runner over with some supplies"), 914,4
    /// package 951 (Vincent) variant 130 7147 ("Your AFS buddies were already here with the supplies ... I just don't think
    /// you're going to fit in well here") and variant 131 7146 ("Bypass the red tape ... fit in well around these parts").
    /// Item class 22006 AFS Medical Supplies (template 44929). Usable name 3 "AFS Supply Crate" (usablenameoverride).
    ///
    /// Sourced: TaRapedia Highway Robbery: giver Retread Vincent (Retread City); level 36; 35,000 XP, 3,700 credits; one of 2
    /// Class VII Advanced Med Pack, 2 Class VII Adrenaline Booster, 4 Class VII Incendiary Grenade, 4 Class VII EMP Grenade;
    /// follow-up Production Destruction; "you don't have to go to both bases". World DB: Vincent 510168 (package 951); med
    /// tents (client map) Paludos (-314.9, 218.3, 130.2), Falcon Hold (-145.4, 219, 615.4).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Package 1011 is Commander Figgins (MarshesRetreadCityNpcsV1, the triage's Q4).
    ///  - A personal AFS supply crate (11078, argument 3, named by the client's 3) by each med tent; either one gives the
    ///    supplies and completes 1 (optional); 2 (optional) is Figgins' talk. Choosing one fails the other, closes the crates
    ///    and sets flag 914001 (1 stolen, 2 told), then 4 opens. Hidden 3 an aggregate of 1 or 2.
    ///  - 4 is Vincent's talk with the client's text of the choice: variant 131 after the theft (he takes the supplies),
    ///    130 after the telling (dialogue topics gated by the flag).
    ///  - 914001 also chooses the follow-up: 916 (Vincent) after the theft, 1033 (Holloway) after the telling (the triage's
    ///    Q16).
    ///  - The stacks the lowest Class VII templates of TaRapedia's four. Markers at both crates (1), Figgins (2), Vincent (4).
    /// </summary>
    public static class MarshesHighwayRobberyV1
    {
        public const uint MissionId = 914;
        public const uint Level = 36;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, VincentPackage = 951;
        public const uint Figgins = MarshesRetreadCityNpcsV1.Figgins, FigginsPackage = MarshesRetreadCityNpcsV1.FigginsPackage;
        public const uint Steal = 1, RatOut = 2, Outcome = 3, Report = 4;
        public const uint StealChosen = 11, RatChosen = 12;             // scenarios (and their scene sequences)
        public const uint StolenReport = 1, ToldReport = 2;              // objective 4's transitions
        public const uint StolenVariant = 131, ToldVariant = 130;        // objectiveconversation 914,4 variants
        public const uint ChoiceFlag = 914001, StoleFlag = 1, ToldFlag = 2;

        public const uint SuppliesTemplate = 44929, SuppliesClass = 22006;
        public const string SuppliesKey = "afs-medical-supplies";
        public const uint CrateClass = 11078;                          // UsableItemDisp_item_crate_hum_v01
        public const uint CrateName = 3;                               // usablenameoverride RETREAD_CAVE_AFS_SUPPLY_CRATE "AFS Supply Crate"
        public const uint UseArgument = Pools.PoolsSmashAndGrabV1.UseArgument;
        public const uint ReadyState = Pools.PoolsSmashAndGrabV1.ReadyState, TakenState = Pools.PoolsSmashAndGrabV1.TakenState;

        /// <summary>The crates: Paludos's med tent, Falcon Hold's (on the navmesh beside the client map's tents).</summary>
        public static readonly ScenePosition[] Crates = { new(-310f, 218.48f, 126f), new(-150f, 219.28f, 612f) };
        public static readonly ScenePosition VincentAt = new(-276.8f, 217.48f, -761f);

        public const uint Experience = 35000;
        public const uint Credits = 3700;
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;          // Consumable_Medpack_07C_35
        public const uint AdrenalineBooster = 118814, AdrenalineBoosters = 2;     // Class VII Adrenaline Booster
        public const uint IncendiaryGrenade = 111038, IncendiaryGrenades = 4;     // Consumable_Grenade_Fire_07_35
        public const uint EmpGrenade = 111028, EmpGrenades = 4;                   // Consumable_Grenade_EMP_07_35

        /// <summary>The scenario of crate <paramref name="crate"/> (1-based): its one event, the supplies taken.</summary>
        public static uint StolenAt(uint crate) => crate;

        public static string CrateRole(int index) => $"supply-crate-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Highway Robbery", 4887, Vincent, Vincent, Level);
            mission.Reward(Experience, Credits,
                new[] { (AdvancedMedPack, AdvancedMedPacks), (AdrenalineBooster, AdrenalineBoosters), (IncendiaryGrenade, IncendiaryGrenades),
                    (EmpGrenade, EmpGrenades) },
                new (uint, uint)[0]);
            for (var crate = 1U; crate <= Crates.Length; crate++)
                mission.Scenario(StolenAt(crate), $"The supplies taken from crate {crate}", 1);
            mission.Scenario(StealChosen, "Stolen: the telling closes", 0);
            mission.Scenario(RatChosen, "Told: the crates close", 0);

            mission.Objective(Steal, 5733, 6407, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(RatOut, 5734, 6408, 1, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Outcome, 5744, 6409, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Report, 5745, 6410, 3, MissionObjectiveState.Inactive);

            // 1: the supplies from either crate (a transition per crate: one-off events, no client counter).
            for (var crate = 1U; crate <= Crates.Length; crate++)
            {
                var first = 4 * (crate - 1);
                mission.Transition(Steal, crate, crate);
                mission.Progress(Steal, crate, MissionProgressEventKind.ScenarioEvent, 1, null, counter: StolenAt(crate));
                mission.Action(Steal, first + 1, MissionActionKind.SetPlayerFlag, playerFlag: ChoiceFlag, playerFlagValue: StoleFlag,
                    transition: crate);
                mission.Action(Steal, first + 2, MissionActionKind.RevealObjective, targetObjective: Report, transition: crate);
                mission.Action(Steal, first + 3, MissionActionKind.ActivateObjective, targetObjective: Report,
                    state: MissionObjectiveState.Incomplete, transition: crate);
                mission.Action(Steal, first + 4, MissionActionKind.StartScenario, scenario: StealChosen, transition: crate);
            }
            // 2: Figgins hears of it.
            mission.Transition(RatOut);
            mission.Conversation(RatOut, FigginsPackage);
            mission.Action(RatOut, 1, MissionActionKind.SetPlayerFlag, playerFlag: ChoiceFlag, playerFlagValue: ToldFlag);
            mission.Action(RatOut, 2, MissionActionKind.RevealObjective, targetObjective: Report);
            mission.Action(RatOut, 3, MissionActionKind.ActivateObjective, targetObjective: Report, state: MissionObjectiveState.Incomplete);
            mission.Action(RatOut, 4, MissionActionKind.StartScenario, scenario: RatChosen);
            // 4: Vincent's answer to the choice.
            mission.Transition(Report, StolenReport, StolenReport);
            mission.Conversation(Report, VincentPackage, StolenVariant, StolenReport);
            mission.Action(Report, 1, MissionActionKind.ConsumeMissionItem, transition: StolenReport,
                itemIntent: new ConsumeMissionItemIntent("vincent-takes-the-supplies", MissionId, SuppliesKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Report, 2, MissionActionKind.GrantReward, reward: 1, transition: StolenReport);
            mission.Transition(Report, ToldReport, ToldReport);
            mission.Conversation(Report, VincentPackage, ToldVariant, ToldReport);
            mission.Action(Report, 3, MissionActionKind.GrantReward, reward: 1, transition: ToldReport);

            for (var index = 0; index < Crates.Length; index++)
                mission.Indicator(Steal, (uint)index + 1, Crates[index].X, Crates[index].Y, Crates[index].Z);
            var figgins = MarshesRetreadCityNpcsV1.FigginsAt;
            mission.Indicator(RatOut, 3, figgins.X, figgins.Y, figgins.Z);
            mission.Indicator(Report, 4, VincentAt.X, VincentAt.Y, VincentAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/914",
                "Native1 'Option: Steal Medical Supplies' 5733/6407, 2 'Option: Rat out the Retreads' 5734/6408, 3 'no desc' " +
                "5744/6409, 4 'Report back to Retread Vincent' 5745/6410; objectiveconversation 914,2 pkg1011 5735; 914,4 pkg951 " +
                "variant130 7147 (told), variant131 7146 (stolen). Texts 4887-4890. Item class22006 (template44929). Name3.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Highway_Robbery",
                "Giver Retread Vincent; level36; 35000XP, 3700 credits; one of 2 Advanced Med Pack VII, 2 Adrenaline Booster VII, 4 " +
                "Incendiary Grenade VII, 4 EMP Grenade VII; follow-up Production Destruction; either base will do.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesHighwayRobberyV1",
                "Package1011 = Commander Figgins (new). A personal AFS supply crate 11078 (arg3, name3) by each med tent; either " +
                "gives the supplies (1). Choosing fails the other, closes the crates, sets flag914001 (1 stolen, 2 told); 4 at " +
                "Vincent with variant 131 or 130 by the flag; the flag picks 916 or 1033. Stacks 45062x2, 118814x2, 111038x4, " +
                "111028x4.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Outcome },
                ObjectiveAggregations = new() { [Outcome] = new MissionObjectiveAggregation(new[] { Steal, RatOut }, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Report, VincentPackage, StolenVariant, MissionDialogueKind.Completion, transitionId: StolenReport,
                        requirement: new FlagRequirement(ChoiceFlag, StoleFlag)),
                    new(Report, VincentPackage, ToldVariant, MissionDialogueKind.Completion, transitionId: ToldReport,
                        requirement: new FlagRequirement(ChoiceFlag, ToldFlag))
                },
                Items = new()
                {
                    new MissionItemBinding(SuppliesKey, SuppliesTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var role = CrateRole(index);
                var sequence = (uint)index + 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, Crates[index],
                    Orientation: index * 1.5, InitialObjectState: ReadyState, NameOverrideId: CrateName,
                    UseAction: new SceneObjectAction(MissionId, Steal, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"open-{role}", role, TakenState), new SetInteractionIntent($"close-{role}", role, false) },
                    Character = { new IssueMissionItemIntent($"{role}-supplies", MissionId, SuppliesKey, SuppliesTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, StolenAt(sequence), 1) }
                };
            }
            scene.Sequences[StealChosen] = new SceneSequenceDefinition
            {
                World = CloseAll("stolen"),
                Character = { new ObjectiveIntent("stolen-not-told", MissionId, RatOut, MissionObjectiveState.Failed) }
            };
            scene.Sequences[RatChosen] = new SceneSequenceDefinition
            {
                World = CloseAll("told"),
                Character = { new ObjectiveIntent("told-not-stolen", MissionId, Steal, MissionObjectiveState.Failed) }
            };

            mission.Enable(scene);
        }

        /// <summary>The crates go (once a choice is made); <paramref name="prefix"/> keeps the operation keys apart.</summary>
        private static List<WorldIntent> CloseAll(string prefix)
        {
            var world = new List<WorldIntent>();
            for (var index = 0; index < Crates.Length; index++)
                world.Add(new RemoveActorIntent($"{prefix}-{CrateRole(index)}-gone", CrateRole(index)));
            return world;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
