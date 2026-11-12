using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Reaping the Harvest (native mission 881), Valverde Marshes, V2 (to-fix row 20: the harvesters and taps on dry ground,
    /// MarshesHarvesterPlacesV2; otherwise as V1, which keeps the mission as Marshes_harvesters applied it): Researcher Vetter at Falcon Hold wants gas samples from
    /// five of the Bane gas harvesters in the swamp, for an anti-Bane agent.
    ///
    /// Native: texts 4615-4618 and 7227 (missionconversation): log "Gather gas samples from 5 of the Bane gas harvesters in the
    /// swamp. Return these samples to Researcher Vetter at Falcon Hold.", offer "Head out into the swamps and bring us back
    /// samples from the gas harvesters.", turn-in "Admirable effort! I'll have these delivered to the labs"; objective 1
    /// "Collect Samples from the Bane Gas Harvesters" 4619/7228, no counter (missionobjective); no objective conversation.
    /// Item Gas Harvester Sample class 21041 MisMarshesItemGasHarvesterSample (template 42413, stack 10). Objects: 7906
    /// UsableInertDestBaneGasHarvesterV01, 7908 UsableItemAccBaneGasHarvesterV01 and 10450 UsableItemDispBaneGasHarvesterV02,
    /// all named "Gas Harvester" (physicalentityclassnamelanguage); 10450, the item giver, has the client's "Default" mesh 776.
    /// 10450 and 7908 have no use argument in the usabledata lookup, so the client sends 1 for them (Usable.__init__,
    /// NOTES-use-argument-sweep.md).
    /// Indicator name 106 "Bane Harvester" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia Reaping the Harvest: giver and receiver Researcher Vetter (Falcon Hold); no requirement; follow-ups
    /// Beware: Buried Gas Lines and Hassling the Harvesters; 65,000 XP, 5,400 credits; rewards Voltrox Bio Armor Vest,
    /// Wellcare Stealth Armor Boots, Olympia Hazmat Armor Legs, Wellcare Reflective Armor Gloves (no levels). TR MMO DB: level
    /// 33. Ten Ton Hammer Falcon Hold: Vetter "in the middle of the base at -161,219,609"; the harvesters "all throughout the
    /// watery swamp area to the east. Swim right up to them, and use the small red contraption sticking out from the bottom
    /// of the harvester base... pick any five"; 5400 credits and a choice of Cryo Blade, Healing Disc, Cryo Shotgun or
    /// Virulent Staff (the older list: TaRapedia's armour is used, the triage's conflict 21). World DB: Vetter 510165 (pool
    /// at -147, 219, 614, an upstream spot 15 m from Ten Ton Hammer's; left there, the triage's Q2).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Seven personal harvesters at MarshesHarvestersNpcsV1.Harvesters, any five, each once: a harvester body (7908, the
    ///    real "Gas Harvester" mesh, no use) with its sample tap at the base (a Bane item crate 11116, the item giver the
    ///    real client draws and uses, PoolsSmashAndGrabV1; 10450 draws as the Default pyramid and 10379, the other giver with
    ///    the harvester mesh, has no use argument in usabledata, as 10450 and 7908). A tap's use gives one sample and counts; no room, no
    ///    sample and no count (the tap waits). After the fifth the other taps cannot be used. The client's objective has no
    ///    counter: a hidden server objective 2 counts the five and completes 1 (PoolsMedicinalMeasuresV1's tally).
    ///  - The samples go to Vetter at the turn-in and with an abandon. One marker, the client's "Bane Harvester", round the
    ///    field. Level TR MMO DB's (TaRapedia none). The rewards the lowest Uncommon template of each named kind and slot in
    ///    the band that holds level 33 (the manufacturer names are not in the client tables), one to choose.
    /// </summary>
    public static class MarshesReapingTheHarvestV2
    {
        public const uint MissionId = 881;
        public const uint Level = 33;
        public const uint Vetter = 510165;
        public const uint Collect = 1;
        public const uint Tally = 2;                    // hidden: counts the five samples (the client's objective has no counter)
        public const uint Sampled = 1;                  // the scenario: one event per harvester
        public const uint Samples = 5;

        public const uint SampleTemplate = 42413;       // Gas Harvester Sample, class 21041
        public const string SampleKey = "gas-harvester-sample";
        public const uint BodyClass = 7908;             // UsableItemAccBaneGasHarvesterV01, the harvester's look
        public const uint TapClass = 11116;             // UsableItemDisp_item_crate_bane_v01, the sample tap
        public const uint ReadyState = 81, TakenState = 82;
        public const uint UseArgument = 3;
        public const uint BaneHarvester = 106;          // missionobjectiveindicatorlanguage "Bane Harvester"

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint BioVest = 19246;              // Armor_T3_Bio_V02_UNC_Vest_30_to_34
        public const uint StealthBoots = 38783;         // Armor_T3_Stealth_V02_UNC_Boots_30_to_34
        public const uint HazmatLegs = 20762;           // Armor_T2_Hazmat_V02_UNC_Legs_29_to_33
        public const uint ReflectiveGloves = 35700;     // Armor_T2_Reflective_V02_UNC_Gloves_29_to_33

        public static ScenePosition[] Harvesters => MarshesHarvesterPlacesV2.Harvesters;

        /// <summary>Each harvester's sample tap (MarshesHarvesterPlacesV2, to-fix 20).</summary>
        public static ScenePosition[] Taps => MarshesHarvesterPlacesV2.Taps;

        public static string BodyRole(int index) => $"harvester-{index + 1}";
        public static string TapRole(int index) => $"harvester-tap-{index + 1}";
        public static uint TapSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Reaping the Harvest", 4615, Vetter, Vetter, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, StealthBoots, HazmatLegs, ReflectiveGloves });

            mission.Objective(Collect, 4619, 7228, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 4619, 7228, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(Sampled, "A gas harvester sampled", (uint)Harvesters.Length);
            mission.Transition(Collect);
            mission.ObjectiveState(Collect, Tally, MissionObjectiveState.Completed);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Tally);
            for (var id = 1U; id <= Harvesters.Length; id++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Samples, counter: Sampled, trigger: id);

            var field = MarshesHarvestersNpcsV1.FieldCentre;
            mission.NamedIndicator(Collect, BaneHarvester, field.X, field.Y, field.Z, MarshesHarvestersNpcsV1.FieldRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/881",
                "Native1 'Collect Samples from the Bane Gas Harvesters' 4619/7228, no counter; no objectiveconversation. Texts " +
                "4615-4618, 7227 (log: 5 harvesters, Vetter at Falcon Hold). Item class21041 Gas Harvester Sample (template42413). " +
                "Objects 7906/7908/10450 'Gas Harvester' (10450 the giver, mesh 776 Default). Indicator name106 'Bane Harvester'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Reaping_the_Harvest",
                "Giver/receiver Researcher Vetter (Falcon Hold); follow-ups 913, 890; 65000XP, 5400 credits; Voltrox Bio Armor " +
                "Vest, Wellcare Stealth Armor Boots, Olympia Hazmat Armor Legs, Wellcare Reflective Armor Gloves. TR MMO DB level33. " +
                "Ten Ton Hammer: Vetter at -161,219,609; harvesters all through the watery swamp east of Falcon Hold, use the small " +
                "red contraption at the base, pick any five; reward choice of Cryo Blade, Healing Disc, Cryo Shotgun, Virulent Staff.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesReapingTheHarvestV2",
                "7 personal harvesters (body 7908 + tap 11116; 10450 has the Default mesh) 40-60 m apart east of Falcon Hold, any " +
                "5, each once, a sample each (42413, taken at turn-in and abandon); hidden tally 2 counts them. One 'Bane Harvester' marker round the field. " +
                "Vetter's upstream pool kept. Level33. Items 19246, 38783, 20762, 35700 (lowest Uncommon of the band), one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(SampleKey, SampleTemplate, MissionItemScope.AssignmentIssued, Samples,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Harvesters.Length; index++)
            {
                var body = BodyRole(index);
                var tap = TapRole(index);
                var sequence = TapSequence(index);
                scene.Actors[body] = new SceneActorDefinition(body, SceneActorKind.Object, BodyClass, Harvesters[index],
                    Orientation: index * 0.9, InitiallyInteractable: false);
                scene.Actors[tap] = new SceneActorDefinition(tap, SceneActorKind.Object, TapClass, Taps[index],
                    Orientation: index * 0.9, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Collect, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{body}", body));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{tap}", tap));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"sample-{tap}", tap, TakenState),
                        new SetInteractionIntent($"close-{tap}", tap, false)
                    },
                    Character = { new IssueMissionItemIntent($"{tap}-sample", MissionId, SampleKey, SampleTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Sampled, (uint)index + 1) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
