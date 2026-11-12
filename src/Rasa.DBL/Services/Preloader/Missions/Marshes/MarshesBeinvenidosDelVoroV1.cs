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
    /// Beinvenidos del Voro (native mission 919), the Bane Supply Depot (squad instance 1451): Headquarters sends the player to
    /// the depot's command consoles for Bane datablades on the Maroxin production, to be brought to Sergeant Fitz in Paludos.
    ///
    /// Native: texts 4907-4910 and 7096 (missionconversation): title "Beinvenidos del Voro", log "Gather Bane datablades from the
    /// Stalker production facility command consoles, then return them to Sergeant Fitz in Paludos.", offer "there is
    /// information on Maroxin production within the stalker factory. Check any consoles you find and bring any datablades back
    /// to Sergeant Fitz", turn-in (Fitz) "With the information in these 'blades, we can get a fix on the real story behind the
    /// Maroxin production."; objective (missionobjective) 1 "Collect the Datablades from the Stalker Production Command
    /// Consoles" 6237/7097, no counter; no objective conversation. Item Bane Datablade class 22986 MisMarshesItemBienviendoBlade
    /// (template 45458, stack 5). Indicator name 71 "Bane Terminal".
    ///
    /// Sourced: TaRapedia Beinvenidos del Voro: giver "Radio, at Bane Supply Depot", receiver Sergeant Fitz (the client's log);
    /// 75,000 XP, 5,750 credits. TaRapedia (the page's cached rewards, NOTES-marshes-research-group1.md): one of Voltrox Bio
    /// Armor Helmet, Audiodyne Stealth Armor Gloves, Olympia Graviton Armor Legs, Titan Motor Assist Armor Legs. The depot's client map: the remote terminals ArchBaneOutpostIncidentalTerminal
    /// Remote at (-83.4, 130.8, 198.9), (-1.5, 130.7, 92.9), (-1.5, 131, 171.9) and (-1.3, 130.7, -22.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's Q10/Q11 and its 919 places): offered by radio on arrival in the
    /// squad's depot; three of the player's own Bane computers (20631, as Under the Radar Part II's) beside the first three
    /// remote terminals, one datablade each (one use, then closed); objective 1 shows the blades held (n/3), hidden 2 counts
    /// the three uses and completes 1. Fitz in Paludos takes the blades. Level 35 (the triage's). The four armor pieces
    /// (ESTIMATE): the Uncommon base templates (lowest version) of those kinds and slots in the band that holds level 35, as
    /// Cleaning House's weapons (no client block holds the four). Markers: the client's "Bane Terminal" at the first computer, unnamed ones at the other two, the depot's door on the Marshes and Fitz.
    /// </summary>
    public static class MarshesBeinvenidosDelVoroV1
    {
        public const uint MissionId = 919;
        public const uint Level = 35;
        public const uint Fitz = MarshesLogosResearchNpcsV1.Fitz;
        public const uint Depot = MarshesBaneSupplyDepotNpcsV1.MapContextId;
        public const uint Collect = 1, Tally = 2;
        public const uint Blades = 1;                   // the scenario of the computers' uses

        public const uint ComputerClass = MarshesUnderTheRadarPartTwoV1.ComputerClass;
        public const uint ComputerState = MarshesUnderTheRadarPartTwoV1.ComputerState;
        public const uint UseArgument = MarshesUnderTheRadarPartTwoV1.UseArgument;
        public const uint BaneTerminal = 71;            // missionobjectiveindicatorlanguage
        public const uint BladeTemplate = 45458;        // Bane Datablade, class 22986
        public const string BladeKey = "bane-datablade";

        public static readonly ScenePosition[] ComputersAt =
        {
            new(-80.4f, 131.04f, 198.9f), new(1.23f, 130.86f, 92.77f), new(1.6f, 131.04f, 171.9f)
        };
        public static readonly (double X, double Y, double Z) FitzAt = (-325.5, 226.99, -9);

        public const uint Experience = 75000;
        public const uint Credits = 5750;
        public const uint BioHelmet = 19008;            // Armor_T3_Bio_V02_UNC_Helmet_35_to_39
        public const uint StealthGloves = 38932;        // Armor_T3_Stealth_V02_UNC_Gloves_35_to_39
        public const uint GravitonLegs = 23937;         // Armor_T3_Graviton_V02_UNC_Legs_35_to_39
        public const uint MotorAssistLegs = 29073;      // Armor_T1_MotorAssist_V04_UNC_Legs_33_to_37

        public static string ComputerRole(int index) => $"command-console-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Beinvenidos del Voro", 4907, null, Fitz, Level);
            mission.RadioOffer(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Depot));
            mission.Reward(Experience, Credits, new[] { BioHelmet, StealthGloves, GravitonLegs, MotorAssistLegs });
            mission.Scenario(Blades, "A datablade taken from a command console", (uint)ComputersAt.Length);

            mission.Objective(Collect, 6237, 7097, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 6237, 7097, 1, MissionObjectiveState.Incomplete, required: false);     // hidden

            mission.Transition(Tally);
            for (var blade = 1U; blade <= ComputersAt.Length; blade++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, blade, (uint)ComputersAt.Length, counter: Blades, trigger: blade);
            mission.Transition(Collect);
            mission.ObjectiveState(Collect, Tally, MissionObjectiveState.Completed);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);

            // The client's "Bane Terminal" on the first computer (an objective's markers need ids of their own), unnamed ones on
            // the other two, the depot's door and Fitz.
            mission.NamedIndicator(Collect, BaneTerminal, ComputersAt[0].X, ComputersAt[0].Y, ComputersAt[0].Z, 8, Depot);
            for (var index = 1; index < ComputersAt.Length; index++)
                mission.Indicator(Collect, (uint)index, ComputersAt[index].X, ComputersAt[index].Y, ComputersAt[index].Z, 8, Depot);
            var door = MarshesBaneSupplyDepotNpcsV1.DoorAt;
            mission.Indicator(Collect, 3, door.X, door.Y, door.Z, 10);
            mission.Indicator(Collect, 4, FitzAt.X, FitzAt.Y, FitzAt.Z, 8);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/919",
                "Native1 'Collect the Datablades from the Stalker Production Command Consoles' 6237/7097, no counter, no " +
                "objectiveconversation. Texts 4907-4910, 7096 (log: back to Sergeant Fitz in Paludos). Item class22986 Bane " +
                "Datablade (45458). Indicator name71 'Bane Terminal'. Map 1451 remote terminals.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Beinvenidos_del_Voro",
                "Giver 'Radio, at Bane Supply Depot'; 75000XP, 5750 credits; one of Voltrox Bio Armor Helmet, Audiodyne Stealth " +
                "Armor Gloves, Olympia Graviton Armor Legs, Titan Motor Assist Armor Legs (cached page).", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesBeinvenidosDelVoroV1",
                "Radio offer on arrival in 1451; three personal Bane computers (20631) beside the remote terminals, one datablade " +
                "each, n/3 on 1, hidden 2 counts the uses; Fitz 510170 takes them. Level 35. Rewards the UNC base templates of " +
                "the level-35 bands: 19008, 38932, 23937, 29073.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own command consoles, on the squad's copy of the depot.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Depot,
                MapContextId = Depot,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(BladeKey, BladeTemplate, MissionItemScope.AssignmentIssued, (uint)ComputersAt.Length,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        CounterObjectiveId: Collect)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < ComputersAt.Length; index++)
            {
                var role = ComputerRole(index);
                var sequence = 1 + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ComputerClass, ComputersAt[index],
                    Orientation: 1.5708, InitialObjectState: ComputerState,
                    UseAction: new SceneObjectAction(MissionId, Collect, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"{role}-done", role, false) },
                    Character = { new IssueMissionItemIntent($"{role}-blade", MissionId, BladeKey, BladeTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Blades, (uint)index + 1) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
