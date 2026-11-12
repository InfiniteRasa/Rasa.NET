using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Picking Up the Pieces (native mission 892), the Village Ruins (squad instance 1743): Forean Shaman Rakla at P'reo Das
    /// asks the player to take the relics off the village's fallen warriors and bring them back to her people.
    ///
    /// Native: texts 4680-4683 and 7281 (missionconversation): title "Picking Up the Pieces", log "Retreive Forean relics from
    /// the village ruins.", offer "could you find these relics left behind on our fallen comrades and return them to my
    /// people?", turn-in "This will mean so much and bring strength back into our burdened villagers."; objective
    /// (missionobjective) 1 "Retrieve Forean Relics From the Fallen" 4695/7282, no counter; no objective conversation. Item
    /// Forean Relic class 26688 MisVillageRuinsNecklace (template 111319, stack 1); object Forean Warrior Corpse 21478
    /// (PropForeanWarriorCorpseV01, no use) and Knapsack 11096 (UsableItemDisp_item_bag_hum_v01).
    ///
    /// Sourced: TaRapedia Picking up the Pieces: giver and receiver Forean Shaman Rakla (P'reo Das); 60,000 XP, 5,250 credits;
    /// one of Vextronics Flame Thrower, Vextronics Cryogenic Staff, Vextronics Cryogenic Net Gun, Vextronics Virulent Leech
    /// Gun; the relics on Forean corpses, "at least 4 in P'reo Das village, at least 2 in Ra'a Das". TR MMO DB: level 35. The
    /// client's item block 96333-96336 (Propellant Gun Fire 32-36, Staff Ice 35-39, Net Gun Ice 31-35, Leech Gun Virulent
    /// 34-38: TaRapedia's four in order, Rare).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's Q12/14 choice):
    ///  - Taken at Rakla in the squad's Village Ruins. Six of the player's own Forean warrior corpses (TaRapedia's "at least"
    ///    counts: four in P'reo Das, two in Ra'a Das, on the walked-to ground; navmesh probe of 2026-11-03), each with a
    ///    knapsack (as Divided We Fall's Helka): one use gives one Forean Relic and closes the knapsack. Objective 1 shows the
    ///    relics held (n/6, the item counter); hidden 2 counts the six uses and completes 1. Rakla takes the relics at the
    ///    turn-in (the binding's completion cleanup).
    ///  - Markers round the four corpses of P'reo Das, round the two of Ra'a Das, and at Rakla.
    /// </summary>
    public static class MarshesPickingUpThePiecesV1
    {
        public const uint MissionId = 892;
        public const uint Level = 35;
        public const uint Rakla = MarshesPreoDasVillageNpcsV1.Rakla;
        public const uint Ruins = MarshesPreoDasVillageNpcsV1.MapContextId;
        public const uint Retrieve = 1, Tally = 2;
        public const uint Relics = 1;                   // the scenario of the knapsacks' uses

        public const uint CorpseClass = MarshesDividedWeFallV1.CorpseClass;     // Forean Warrior Corpse, no use
        public const uint PackClass = MarshesDividedWeFallV1.PackClass;         // Knapsack, the use
        public const uint UseArgument = MarshesDividedWeFallV1.UseArgument;
        public const uint ReadyState = MarshesDividedWeFallV1.ReadyState, TakenState = MarshesDividedWeFallV1.TakenState;
        public const uint RelicTemplate = 111319;       // Forean Relic, class 26688
        public const string RelicKey = "forean-relic";

        /// <summary>The fallen: four in P'reo Das, two in Ra'a Das.</summary>
        public static readonly ScenePosition[] CorpsesAt =
        {
            new(84.24f, 59.04f, -223.14f), new(120f, 56.01f, -224f), new(80f, 58.97f, -244f), new(105f, 55.37f, -215f),
            new(231.08f, 56.17f, -7.8f), new(247.42f, 56.17f, 8.58f)
        };
        public static readonly (double X, double Y, double Z) PreoDasCorpsesAt = (97, 57.4, -227);
        public const double PreoDasRadius = 25;
        public static readonly (double X, double Y, double Z) RaaDasCorpsesAt = (239.2, 56.17, 0.4);
        public const double RaaDasRadius = 15;

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public static readonly uint[] Rewards = { 96333, 96334, 96335, 96336 };

        public static string CorpseRole(int index) => $"fallen-{index + 1}";
        public static string PackRole(int index) => $"fallen-{index + 1}-knapsack";
        public static uint PackSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Picking Up the Pieces", 4680, Rakla, Rakla, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Scenario(Relics, "A relic taken from the fallen", (uint)CorpsesAt.Length);

            mission.Objective(Retrieve, 4695, 7282, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 4695, 7282, 1, MissionObjectiveState.Incomplete, required: false);     // hidden

            mission.Transition(Tally);
            for (var relic = 1U; relic <= CorpsesAt.Length; relic++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, relic, (uint)CorpsesAt.Length, counter: Relics, trigger: relic);
            mission.Transition(Retrieve);
            mission.ObjectiveState(Retrieve, Tally, MissionObjectiveState.Completed);
            mission.Action(Retrieve, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Retrieve, 1, PreoDasCorpsesAt.X, PreoDasCorpsesAt.Y, PreoDasCorpsesAt.Z, PreoDasRadius, Ruins);
            mission.Indicator(Retrieve, 2, RaaDasCorpsesAt.X, RaaDasCorpsesAt.Y, RaaDasCorpsesAt.Z, RaaDasRadius, Ruins);
            var rakla = MarshesPreoDasVillageNpcsV1.RaklaAt;
            mission.Indicator(Retrieve, 3, rakla.X, rakla.Y, rakla.Z, 8, Ruins);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/892",
                "Native1 'Retrieve Forean Relics From the Fallen' 4695/7282, no counter, no objectiveconversation. Texts 4680-4683, " +
                "7281 ('relics left behind on our fallen comrades'). Name 8559 Rakla. Item class26688 Forean Relic (111319). " +
                "Objects 21478 Forean Warrior Corpse, 11096 Knapsack. Item block 96333-96336.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Picking_up_the_Pieces",
                "Giver/receiver Forean Shaman Rakla (P'reo Das); 60000XP, 5250 credits; one of Vextronics Flame Thrower, Cryogenic " +
                "Staff, Cryogenic Net Gun, Virulent Leech Gun; relics on Forean corpses, at least 4 in P'reo Das, at least 2 in Ra'a " +
                "Das. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesPickingUpThePiecesV1",
                "Bound to 1743; Rakla 760378 at P'reo Das. Six personal corpses (21478) with knapsacks (11096), 4 at P'reo Das, 2 at " +
                "Ra'a Das; one use gives one relic; the item counter on 1 (n/6), hidden 2 counts the uses. Rakla takes the relics.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own fallen and their knapsacks, on the squad's copy of the Village Ruins.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Ruins,
                MapContextId = Ruins,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(RelicKey, RelicTemplate, MissionItemScope.AssignmentIssued, (uint)CorpsesAt.Length,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        CounterObjectiveId: Retrieve)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < CorpsesAt.Length; index++)
            {
                var corpse = CorpseRole(index);
                var pack = PackRole(index);
                var yaw = index * 1.0472;
                scene.Actors[corpse] = new SceneActorDefinition(corpse, SceneActorKind.Object, CorpseClass, CorpsesAt[index],
                    Orientation: yaw, InitiallyInteractable: false);
                scene.Actors[pack] = new SceneActorDefinition(pack, SceneActorKind.Object, PackClass, CorpsesAt[index],
                    Orientation: yaw, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Retrieve, PackSequence(index), UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{corpse}", corpse));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{pack}", pack));
                scene.Sequences[PackSequence(index)] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{pack}-searched", pack, TakenState),
                        new SetInteractionIntent($"{pack}-done", pack, false)
                    },
                    Character = { new IssueMissionItemIntent($"{corpse}-relic", MissionId, RelicKey, RelicTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Relics, (uint)index + 1) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
