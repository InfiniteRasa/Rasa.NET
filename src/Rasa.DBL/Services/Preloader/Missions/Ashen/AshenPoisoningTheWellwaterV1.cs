using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Poisoning the Wellwater (native mission 1302), Ligo Ashen Desert: after Neurotoxins (1300), Agent Hale sends the player
    /// with four sabotaged Water Filtration Systems to the four Water Harvesters of Maji Field.
    ///
    /// Native: texts 10520-10523, 10524 (missionconversation): title "Poisoning the Wellwater", log "Travel to the four Water
    /// Harvesters of Maji Field and install the sabotaged water filtration systems given to you by Agent Hale. Once you have
    /// sabotaged the Water Harvesters, return to Agent Hale at White Oasis Outpost.", offer "The preparations on the Water
    /// Filtration System is complete. All you need to do is install these directly into the Water Harvesters ...", turn-in
    /// "Congratulations, soldier. You've made many new friends in very high places. ...", reminder 10524; objectives
    /// (missionobjective) 1-4 "Sabotage Harvester 1".."4" 11862/11863, 11864/11865, 11866/11867, 11868/11869, no counters. No
    /// objective conversation, no marker names. Item class 25494 Mis_Ligo_Ashen_Maji_Filtration "Water Filtration System"
    /// (template 97495, stack 4: itemclass). Usable names (usablenameoverride) 69 LIGO_ASHEN_MAJI_DIAGNOSTIC "Harvester Control
    /// Panel", 70 LIGO_ASHEN_MAJI_WATERREPAIR "Harvester Filtration Unit".
    ///
    /// Sourced: TaRapedia Poisoning the Wellwater: giver and reward giver Agent Hale; requires Neurotoxins; 4,000 credits, XP
    /// "Unknown"; one of Modification: Fire Resistance, Ice Resistance, Virulent Resistance, Electric Resistance; "Harvester
    /// Control Panel 1 at -305.7, 198.1, 2 at -260.2, 340.6, 3 at -375.5, 394.1, 4 at -363.9, 510.7"; "install these directly
    /// into the Water Harvesters".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Offered after Neurotoxins with flag 257 at 1 (the poison side; Neurotoxins already needs it, the gate keeps the chain
    ///    on one side). Level 42 (D5). 45,000 XP (TaRapedia "Unknown": the zone's 4,000-4,100-credit missions, Formal
    ///    Complaints and its talks, pay 45,000).
    ///  - Four Water Filtration Systems (a mission item, AssignmentIssued, 4: the client's stack) at the accept, taken back at
    ///    an abandon. A personal panel at each harvester on Analyzing the Harvesters' places (TaRapedia names this mission's
    ///    panels by 1295's name and spots; set 11 moved 2 and 4 off a Fithik pool and Ulovik and to a place seen from all round),
    ///    the same monitor (UsableTwoStateHumComputerV01V06 9478, off 55, on 56, use argument 1) named 69 "Harvester Control
    ///    Panel" (TaRapedia's name; the client's 70 "Harvester Filtration Unit", MAJI_WATERREPAIR, reads as the repair side's
    ///    cut 1301). Analyzing the Harvesters is done before (1298 needs it), so its panels are gone: one panel at a place.
    ///  - A panel's use installs one system (it is taken), turns the panel on and completes its objective once. Any order.
    ///  - A marker r8 on each panel.
    ///  - The items: the grade [4] armour modules (as Robotic Salvage Acquisitions' TaRapedia "Modification" list, the same
    ///    four): Resist Fire 123026, Resist Ice 123031, Resist Virulent 123071, Resist Electric 123016; one to choose.
    /// </summary>
    public static class AshenPoisoningTheWellwaterV1
    {
        public const uint MissionId = 1302;
        public const uint Level = 42;
        public const uint Hale = AshenWhiteOasisNpcsV1.Hale;
        public const uint Installed = 1;                // the scenario of the panels' uses: step K for harvester K
        public const double MarkerRadius = 8;

        public const uint FilterTemplate = 97495, FilterClass = 25494;
        public const uint Filters = 4;
        public const string FilterKey = "water-filtration-system";
        public const uint PanelClass = AshenAnalyzingHarvestersV1.PanelClass;     // 9478 UsableTwoStateHumComputerV01V06
        public const uint PanelName = AshenAnalyzingHarvestersV1.PanelName;       // 69 "Harvester Control Panel" (TaRapedia's name)
        public const uint UseArgument = AshenAnalyzingHarvestersV1.UseArgument;
        public const uint OffState = AshenAnalyzingHarvestersV1.OffState, OnState = AshenAnalyzingHarvestersV1.OnState;

        /// <summary>The panels at harvesters 1-4: Analyzing the Harvesters' places.</summary>
        public static ScenePosition[] PanelsAt => AshenAnalyzingHarvestersV1.PanelsAt;

        public const uint Experience = 45000;
        public const uint Credits = 4000;
        public const uint ResistFire = AshenRoboticAcquisitionsV1.ResistFire;         // Armor Module: Resist Fire [4]
        public const uint ResistIce = AshenRoboticAcquisitionsV1.ResistIce;           // Armor Module: Resist Ice [4]
        public const uint ResistVirulent = AshenRoboticAcquisitionsV1.ResistVirulent; // Armor Module: Resist Virulent [4]
        public const uint ResistElectric = AshenRoboticAcquisitionsV1.ResistElectric; // Armor Module: Resist Electric [4]

        /// <summary>The objective of harvester <paramref name="number"/> (1-4): the client's objective of the same number.</summary>
        public static uint Sabotage(int number) => (uint)number;

        public static string PanelRole(int number) => $"maji-harvester-filter-panel-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Poisoning the Wellwater", 10520, Hale, Hale, Level);
            mission.Reward(Experience, Credits, new[] { ResistFire, ResistIce, ResistVirulent, ResistElectric });
            mission.Prerequisite(AshenNeurotoxinsV1.MissionId, 1);
            mission.FlagPrerequisite(AshenMurkyWatersV1.PoisonFlag, AshenMurkyWatersV1.Chosen, "Murky Waters choice 2, the poison (flag 257)", 2);
            mission.Scenario(Installed, "A Water Filtration System installed", (uint)PanelsAt.Length);

            for (var number = 1; number <= PanelsAt.Length; number++)
            {
                var objective = Sabotage(number);
                mission.Objective(objective, 11860 + 2 * objective, 11861 + 2 * objective, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, objective, null, counter: Installed);
                var at = PanelsAt[number - 1];
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1302",
                "Native1-4 'Sabotage Harvester 1'..'4' 11862/11863..11868/11869, no counters; no objectiveconversation. Texts " +
                "10520-10524 (the four Water Harvesters of Maji Field; return to Agent Hale). Item class25494 Water Filtration " +
                "System (template97495, stack4). Usable names 69 'Harvester Control Panel', 70 'Harvester Filtration Unit'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Poisoning_the_Wellwater",
                "Giver/reward giver Agent Hale; requires Neurotoxins; 4000 credits, XP Unknown; Modification: Fire, Ice, Virulent, " +
                "Electric Resistance; Harvester Control Panels at -305.7, 198.1 / -260.2, 340.6 / -375.5, 394.1 / -363.9, 510.7; " +
                "install these directly into the Water Harvesters.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenPoisoningTheWellwaterV1",
                "After 1300 with flag257 at 1. Level42 and 45000XP estimated. Four systems 97495 at the accept, gone at an abandon; " +
                "a personal panel 9478 (name69; arg1; 55->56) at each of 1295's four places takes one and completes its objective. " +
                "Markers r8. Modules 123026, 123031, 123071, 123016, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The four filtration systems issued at the accept and the player's own four panels.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(FilterKey, FilterTemplate, MissionItemScope.AssignmentIssued, Filters,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-water-filtration-systems", MissionId, FilterKey, FilterTemplate, Filters) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= PanelsAt.Length; number++)
            {
                var role = PanelRole(number);
                var objective = Sabotage(number);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PanelClass, PanelsAt[number - 1],
                    InitialObjectState: OffState, NameOverrideId: PanelName,
                    UseAction: new SceneObjectAction(MissionId, objective, objective, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-installed", role, OnState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-filter", MissionId, FilterKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, Installed, objective) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
