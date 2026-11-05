using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Regulator: Part IV (native mission 1030), Valverde Marshes: Retread Wilson sends the player to the Paludos Ruins
    /// for one of the AFS viral scrappers left there, the last part, and on to Retread McCormick, who builds the Nanoviral
    /// Regulator.
    ///
    /// Native: texts 5718-5721 and 8069 (missionconversation): log "Locate an AFS viral scrapper in the Paludos Ruins and
    /// deliver it to McCormick in the Retread Camp in the South.", offer "the viral scrapper. You AFS dropped a bunch in
    /// Paludos awhile back... Dig around in the ruins there", turn-in "there you go - one nanoviral regulator. Make sure you
    /// have this in your backpack when you go into the pipeline area"; objective 1 "Locate an AFS viral scrapper in the
    /// Paludos Ruins" 5722/8070, no counter (missionobjective); no objective conversation. Items AFS Viral Scrapper class 21091
    /// MisMarshesItemAFSViralScrapper (World DB template 42435, mesh 19939) and Nanoviral Regulator class 21092 (template
    /// 42436). Object 11114 UsableItemDisp_item_canister_hum_v01, an item giver with the scrapper's mesh 19939 (entityclass),
    /// use argument 3 (usabledata lookup[11114][0]). Indicator name 146 "Paludos Ruins" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia The Regulator: Part 4: giver Retread Wilson, receiver Retread McCormick; requires Part 3; 70,000 XP,
    /// 5,550 credits; four items of level 35 (no names); the scrapper at /loc (-2, 216, 29). TR MMO DB: level 35, 70,000 XP,
    /// 1,800 credits (TaRapedia's later 5,550 wins, the triage's conflict 21). Ten Ton Hammer Retread City: the scrapper "near
    /// Paludos in the ruins to the east".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - One personal item giver (11114) at TaRapedia's /loc, on the navmesh (probe of 2026-11-02); its use gives the
    ///    scrapper and completes 1; no room, no scrapper (the giver waits). McCormick keeps it at the turn-in.
    ///  - The Regulator a plain item with every choice: the server has no area armour debuff for 913's pipeline fields to
    ///    block (the triage's Q34, a deviation). The four items (no names on any source, the triage's Q37) the lowest Uncommon
    ///    template of the four T3 kinds of the 35-39 band, as 890 (MarshesHasslingTheHarvestersV1). Level TR MMO DB's.
    ///  - A marker under the client's "Paludos Ruins" at the giver.
    /// </summary>
    public static class MarshesRegulatorPartFourV1
    {
        public const uint MissionId = 1030;
        public const uint Level = 35;
        public const uint Wilson = MarshesRegulatorNpcsV1.Wilson, McCormick = MarshesRegulatorNpcsV1.McCormick;
        public const uint Locate = 1;

        public const uint ScrapperTemplate = 42435, ScrapperClass = 21091;
        public const string ScrapperKey = "afs-viral-scrapper";
        public const uint GiverClass = 11114;           // UsableItemDisp_item_canister_hum_v01, the scrapper's mesh 19939
        public const uint UseArgument = 3;              // usabledata lookup[11114][0]
        public const uint ReadyState = 81, TakenState = 82;
        public const string GiverRole = "viral-scrapper";
        public const uint PaludosRuins = 146;           // missionobjectiveindicatorlanguage "Paludos Ruins"
        public const double MarkerRadius = 15;
        public static readonly ScenePosition GiverAt = new(-2f, 217.22f, 29f);

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint Regulator = 42436;            // Nanoviral Regulator, class 21092, with every choice
        public const uint BioBoots = 18750;             // Armor_T3_Bio_V02_UNC_Boots_35_to_39
        public const uint GravitonBoots = 23549;        // Armor_T3_Graviton_V02_UNC_Boots_35_to_39
        public const uint MechHelmet = 8855;            // Armor_T3_Mech_V03_UNC_Helmet_35_to_39
        public const uint StealthVest = 10637;          // Armor_T3_Stealth_V02_UNC_Vest_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Regulator: Part IV", 5718, Wilson, McCormick, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechHelmet, StealthVest }, new[] { Regulator });
            mission.Prerequisite(MarshesRegulatorPartThreeV1.MissionId);

            mission.Objective(Locate, 5722, 8070, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Take an AFS viral scrapper from the Paludos Ruins", 1);
            mission.Transition(Locate);
            mission.Progress(Locate, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(Locate, 1, MissionActionKind.GrantReward, reward: 1);
            mission.NamedIndicator(Locate, PaludosRuins, GiverAt.X, GiverAt.Y, GiverAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1030",
                "Native1 'Locate an AFS viral scrapper in the Paludos Ruins' 5722/8070, no counter; no objectiveconversation. " +
                "Texts 5718-5721, 8069 (log: deliver it to McCormick in the Retread Camp). Items class21091 AFS Viral Scrapper " +
                "(template42435, mesh19939), class21092 Nanoviral Regulator (template42436). Giver 11114 " +
                "UsableItemDisp_item_canister_hum_v01 (mesh19939, usabledata arg3). Indicator name146 'Paludos Ruins'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_4",
                "Giver Retread Wilson, receiver Retread McCormick; requires Part 3; 70000XP, 5550 credits; 4 items lvl35; scrapper " +
                "at /loc -2,216,29. TR MMO DB level35, 1800 credits. Ten Ton Hammer: near Paludos in the ruins to the east.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRegulatorPartFourV1",
                "One personal giver 11114 (arg3, states81/82) at (-2,217.22,29); its use issues the scrapper (42435, taken at the " +
                "turn-in) and completes 1. Regulator 42436 with every choice, no armour effect (no area debuff). Items 18750, " +
                "23549, 8855, 10637 (lowest Uncommon T3 of 35-39), one to choose. Marker 'Paludos Ruins' at the giver.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ScrapperKey, ScrapperTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors[GiverRole] = new SceneActorDefinition(GiverRole, SceneActorKind.Object, GiverClass, GiverAt,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Locate, 1, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{GiverRole}", GiverRole) } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("take-scrapper", GiverRole, TakenState),
                    new SetInteractionIntent($"close-{GiverRole}", GiverRole, false)
                },
                Character = { new IssueMissionItemIntent("scrapper-from-ruins", MissionId, ScrapperKey, ScrapperTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
