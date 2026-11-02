using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Under the Radar - Part II, the control point version (native mission 1876), Valverde Marshes: Lt. Kiang read the black
    /// box; the Bane are massing in the east. He sends the player to the smaller Bane base to the east to hack its data network
    /// for their troop movements.
    ///
    /// Native: texts 19164-19168 (missionconversation): log "Engage control point to the east and gather info on Bane troop
    /// movement.", offer "To the east the Bane have a smaller base under their control. If we could take that base and hack
    /// into the data network, I suspect you'll be able to find troop movement plans", turn-in "With this, intelligence should
    /// be able to come up with a better way for us to attack the Bane"; objective 1 "Engage control point to the east and
    /// gather info on Bane troop movement." 19687/19688, no counter (missionobjective); no objective conversation. Item Bane
    /// Troop Data class 28642 MisMarshesBaneScematics (World DB template 119003). Object 20631
    /// UsableStatelessBaneConsoleKeyboard (usabledata lookup[20631][0] None: the client sends 1; the Plateau's and the Pools'
    /// Bane computers). Indicator name 71 "Bane Terminal" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia Under the Radar - Part II (one page with 1877): giver and receiver Lt. Kiang; requires Under the
    /// Radar; follow-up This Calls For Destruction; level 37; 140,000 XP, 7,400 credits; one of Astra Bio, Titan Graviton,
    /// Prodigy Stealth, Olympia Mech Armor Boots (lvl 35); objective "Engage control point to the east ... Bane Troop Data 0 /
    /// 1". Ten Ton Hammer Eastern Marshes: "A small Control Post nearby may provide a way to hack into the Bane network... The
    /// computer you seek is right outside the CP at 101,226,-289"; 7,400 credits and a choice of boots. World DB: control point
    /// 11 Bane Assertion Camp (104.7, 219.1, -325.6), owner Bane. Client item templates 121241-121244: Bio, Graviton, Stealth,
    /// Mech Boots 35-39, TaRapedia's four in its order (121245-121248, the same four again, are 1877's).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - No capture (the triage's Q22): the server's control points drive no mission objective (no progress kind for a
    ///    capture or an owner), and Ten Ton Hammer's computer stands outside the point. One personal Bane computer (20631,
    ///    state 44) at Ten Ton Hammer's point, on the navmesh (probe of 2026-11-02); its use gives the troop data and completes
    ///    1; no room, no data (the computer waits). Kiang keeps the data at the turn-in.
    ///  - 1876 and 1877 are offered side by side after 1875 (the triage's Q22); either one, done, sets mission flag
    ///    <see cref="PartTwoDoneFlag"/> for This Calls For Destruction and Data Disaster (set 6). Both can be done.
    ///  - The rewards the client's block 121241-121244, one to choose (the lower block for the lower mission id). A marker
    ///    under the client's "Bane Terminal" at the computer.
    /// </summary>
    public static class MarshesUnderTheRadarPartTwoV1
    {
        public const uint MissionId = 1876;
        public const uint Level = 37;
        public const uint Kiang = MarshesSentryTowerNpcsV1.Kiang;
        public const uint Engage = 1;

        /// <summary>"Under the Radar - Part II" done, by either version (1876 or 1877): mission flag, value 1.</summary>
        public const uint PartTwoDoneFlag = 1876001;

        public const uint DataTemplate = 119003, DataClass = 28642;     // Bane Troop Data
        public const string DataKey = "bane-troop-data-network";
        public const uint ComputerClass = 20631;        // UsableStatelessBaneConsoleKeyboard
        public const uint ComputerState = 44;           // as PlateauSilentButDeadlyV1.ComputerState
        public const uint UseArgument = 1;              // usabledata lookup[20631][0] is None: the client sends 1
        public const string ComputerRole = "bane-network-computer";
        public const uint BaneTerminal = 71;            // missionobjectiveindicatorlanguage "Bane Terminal"
        public const double MarkerRadius = 8;
        public static readonly ScenePosition ComputerAt = new(101f, 226.1f, -289f);

        public const uint Experience = 140000;
        public const uint Credits = 7400;
        public const uint BioBoots = 121241;            // Armor_T3_Bio_V04_UNC_Boots_35_to_39
        public const uint GravitonBoots = 121242;       // Armor_T3_Graviton_V04_UNC_Boots_35_to_39
        public const uint StealthBoots = 121243;        // Armor_T3_Stealth_V04_UNC_Boots_35_to_39
        public const uint MechBoots = 121244;           // Armor_T3_Mech_V04_UNC_Boots_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Under the Radar - Part II (control point)", 19164,
                Kiang, Kiang, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, StealthBoots, MechBoots });
            mission.Prerequisite(MarshesUnderTheRadarV1.MissionId);

            mission.Objective(Engage, 19687, 19688, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Hack the Bane data network outside the control point", 1);
            mission.Transition(Engage);
            mission.Progress(Engage, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(Engage, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Action(Engage, 2, MissionActionKind.SetPlayerFlag, playerFlag: PartTwoDoneFlag, playerFlagValue: 1,
                comment: "Under the Radar - Part II done (1876)");
            mission.NamedIndicator(Engage, BaneTerminal, ComputerAt.X, ComputerAt.Y, ComputerAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1876",
                "Native1 'Engage control point to the east and gather info on Bane troop movement.' 19687/19688, no counter; no " +
                "objectiveconversation. Texts 19164-19168 (hack into the data network). Item class28642 Bane Troop Data " +
                "(template119003). Computer 20631 (usabledata None: arg1). Indicator name71 'Bane Terminal'. Templates 121241-121244.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II",
                "Giver/receiver Lt. Kiang; requires Under the Radar; level37; 140000XP, 7400 credits; one of Astra Bio, Titan " +
                "Graviton, Prodigy Stealth, Olympia Mech Armor Boots lvl35; Bane Troop Data 0/1. Ten Ton Hammer: the computer " +
                "right outside the CP at 101,226,-289.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesUnderTheRadarPartTwoV1",
                "No capture: one personal Bane computer 20631 (arg1, state44) at (101,226.1,-289); its use issues the data " +
                "(119003, taken at the turn-in) and completes 1, which sets flag 1876001. Items the client block 121241-121244, " +
                "one to choose. Marker 'Bane Terminal' at the computer.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(DataKey, DataTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors[ComputerRole] = new SceneActorDefinition(ComputerRole, SceneActorKind.Object, ComputerClass, ComputerAt,
                InitialObjectState: ComputerState, UseAction: new SceneObjectAction(MissionId, Engage, 1, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{ComputerRole}", ComputerRole) } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent($"close-{ComputerRole}", ComputerRole, false) },
                Character = { new IssueMissionItemIntent("troop-data-from-network", MissionId, DataKey, DataTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
