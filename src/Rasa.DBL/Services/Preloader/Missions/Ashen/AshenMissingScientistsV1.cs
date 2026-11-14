using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Missing Scientists (native mission 1280), Ligo Ashen Desert: Surveyor Malya Odesha by the Indra Caverns lost two
    /// researchers in Indra Pass; the player finds Researcher Veyon's body and the Indra Lightning Resistor Codes on it.
    ///
    /// Native: texts 10312-10315, 10316 (missionconversation): log "Find the body of Researcher Veyon in Indra Pass and recover the
    /// Indra Lightning Resistor Codes. Return to Surveyor Odesh in Indra Caves once you have recovered the codes.", offer "I need
    /// your help finding some of my lost researchers. ...", turn-in "So Researcher Veyon is dead, and Researcher Berish is
    /// missing. ..."; objective 1 "Recover Resistor Codes" 10317/10318, no counter (missionobjective); no objective conversation.
    /// Item class 25475 Mis_Ligo_Ashen_ResistorCodes "Indra Lightning Resistor Codes" (template 97476). Usable name 67
    /// LIGO_ASHEN_DEAD_ISHIN_VEYON "Corpse of Researcher Veyon" (usablenameoverride). Name 9355 Odesha. The client map: region
    /// Indra Pass (270, 367, 527) r200.
    ///
    /// Sourced: TaRapedia Missing Scientists: giver and reward giver Surveyor Malya Odesha (Indra Pass); follow-up God of Storms;
    /// 95,000 XP, 6,300 credits; one of Hellstrom Bio Armor Vest, Pulsar Stealth Armor Gloves, Voltrox Graviton Armor Gloves,
    /// Teleract Hazmat Armor Helmet.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5: an Ashen Desert chain's first mission). No prerequisite (TaRapedia names none).
    ///  - Veyon's body a personal Brann corpse (the client's UsableTreasureDispBrannCorpseMaleV01 7928: Veyon and the Berishes
    ///    are Brann; closed 200, opened 201; usabledata None: the client sends 1) named 67, in the north of the pass where the
    ///    valley the players walk (the client's floor) meets the region's circle: (178, -536); on a client floor, one floor,
    ///    under open sky, 34 m or more outside every hostile pool's circle, walked to on the client's floor from the Indra Pass
    ///    waypoint and back (probe of 2026-11-10). Its search gives the codes (a mission item, AssignmentIssued, 1) and completes
    ///    1 (a scene result); Odesha takes the codes at the turn-in; an abandon takes them and the body. A marker r8 on it.
    ///  - The items: the lowest Uncommon template of the lowest version of each kind in the band of level 41 (client entityclass
    ///    names): Bio V02 Vest 19286 (40-44), Stealth V02 Gloves 38952 (40-44), Graviton V02 Gloves 23698 (40-44), Hazmat V02
    ///    Helmet 20653 (39-43); one to choose.
    /// </summary>
    public static class AshenMissingScientistsV1
    {
        public const uint MissionId = 1280;
        public const uint Level = 41;
        public const uint Odesha = AshenIndraPassNpcsV1.Odesha;
        public const uint Recover = 1;
        public const uint Found = 1;                    // the scenario of the body's search: one scene result
        public const double MarkerRadius = 8;

        public const uint CodesTemplate = 97476, CodesClass = 25475;
        public const string CodesKey = "indra-lightning-resistor-codes";
        public const uint CorpseClass = 7928;           // UsableTreasureDispBrannCorpseMaleV01
        public const uint CorpseName = 67;              // usablenameoverride LIGO_ASHEN_DEAD_ISHIN_VEYON "Corpse of Researcher Veyon"
        public const uint UseArgument = 1;              // usabledata lookup[7928][0] is None: the client sends 1
        public const uint ClosedState = 200, OpenedState = 201;     // USE_TD_STATE_CLOSED, USE_TD_STATE_OPENED
        public const string CorpseRole = "indra-pass-veyon";

        public static readonly ScenePosition VeyonAt = new(178f, 315.73f, 536f);

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint BioVest = 19286;              // Armor_T3_Bio_V02_UNC_Vest_40_to_44
        public const uint StealthGloves = 38952;        // Armor_T3_Stealth_V02_UNC_Gloves_40_to_44
        public const uint GravitonGloves = 23698;       // Armor_T3_Graviton_V02_UNC_Gloves_40_to_44
        public const uint HazmatHelmet = 20653;         // Armor_T2_Hazmat_V02_UNC_Helmet_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Missing Scientists", 10312, Odesha, Odesha, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, StealthGloves, GravitonGloves, HazmatHelmet });
            mission.Scenario(Found, "Researcher Veyon's body searched", 1);

            mission.Objective(Recover, 10317, 10318, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Found);
            mission.Action(Recover, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Recover, 1, VeyonAt.X, VeyonAt.Y, VeyonAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1280",
                "Native1 'Recover Resistor Codes' 10317/10318, no counter; no objectiveconversation. Texts 10312-10316 (Veyon's body " +
                "in Indra Pass, the codes back to Surveyor Odesha). Item class25475 Indra Lightning Resistor Codes (template97476). " +
                "Usable name 67 'Corpse of Researcher Veyon'. Name 9355 Odesha. Region Indra Pass (270, 367, 527) r200.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Missing_Scientists",
                "Giver/reward giver Surveyor Malya Odesha (Indra Pass); follow-up God of Storms; 95000XP, 6300 credits; Hellstrom Bio " +
                "Armor Vest, Pulsar Stealth Armor Gloves, Voltrox Graviton Armor Gloves, Teleract Hazmat Armor Helmet.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenMissingScientistsV1",
                "Level41 estimated; no prerequisite. Veyon a personal Brann corpse 7928 (name67; arg1; 200->201) at (178, -536) in " +
                "the pass's valley; its search gives the codes (AssignmentIssued, taken at the turn-in) and completes 1. Marker r8. " +
                "Items 19286, 38952, 23698, 20653 (lowest Uncommon, band of level 41), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own Veyon, searched once for the codes.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(CodesKey, CodesTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, VeyonAt,
                InitialObjectState: ClosedState, NameOverrideId: CorpseName,
                UseAction: new SceneObjectAction(MissionId, Recover, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{CorpseRole}", CorpseRole));
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{CorpseRole}-searched", CorpseRole, OpenedState),
                    new SetInteractionIntent($"{CorpseRole}-done", CorpseRole, false)
                },
                Character = { new IssueMissionItemIntent($"{CorpseRole}-codes", MissionId, CodesKey, CodesTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, Found, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
