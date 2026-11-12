using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Insertion Point (native mission 663), the Bane Supply Depot (squad instance 1451): Elder Toida in Paludos mourns the
    /// Foreans lost in the air battle over the depot and sends the player in, by force or by stealth, to disable the depot's
    /// mortars before the AFS' last aerial assault; Headquarters takes the report by radio.
    ///
    /// Native: texts 2642-2645 and 6998 (missionconversation): title "Insertion Point", log "Infiltrate the Bane Supply Depot to
    /// the southeast by means of force or stealth. Once inside, find a way to disable the Bane mortars prior to the final AFS
    /// aerial assault.", offer (Toida) "Today is a day of mourning ... a most vile toxin known only as 'Maroxin'", turn-in
    /// "Your preparation has graced us with a moment of opportunity while those mortars are disabled."; objectives
    /// (missionobjective) 4 "Method 1: Infiltrate The Depot by Force" 6247/6999, 3 "Method 2: Infiltrate The Depot by Stealth"
    /// 6248/7000, 5 "no desc" 7501/7502, 6 "Disable the above ground Bane Mortars" 17358/17359 ("by means of destruction or by
    /// deactivation through a Bane terminal") counter 17360 "Bane Mortars"; no objective conversation. Indicator name 71 "Bane
    /// Terminal".
    ///
    /// Sourced: TaRapedia Insertion Point: giver Elder Toida, receiver Headquarters (radio); requires level 37; 170,000 XP,
    /// 6,000 credits; one of 6 Class VIII Advanced Med Pack, 9 EMP Grenade, 3 Res Trauma Kit, 9 Concussion Grenade; the
    /// cameras by the rear entrance (Maroxin storage); the mortar terminal (-88.2, 80.0, -160.4). World DB: the depot's 13
    /// Bane Mortar pools (592144-592156, creature 592006). The depot's client map: ArchBaneIndustrialEntranceV01 at (-284, 148,
    /// 36) (by the arrival) and (68, 96, 120) (inside, the lower level); the Bane console cluster at (-91, 80, -160).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the camera detection the server lacks: NOTES-missing-mechanics.md "Detection
    /// (stealth)"):
    ///  - Taken at Toida on the Marshes; played in the squad's depot. Level 37 and the level-37 requirement (TaRapedia).
    ///  - The alarm is a hit, not a camera: four of the run's own Thrax guard the depot's front entrance; the player's first
    ///    hit on any of them (DamagedSequences, as Slay on Target's alarm) sounds it, or the defeat of one killed with one shot
    ///    (DefeatSequences: a kill clears the hit before the scene sees it). The alarm completes 4 (force) and fails 3
    ///    (stealth); walking into the lower entrance (12 m round 68, 97.6, 120) before it completes 3. 3 and 4 are optional;
    ///    hidden 5 (the client's "no desc") completes on either; it is required, so the mission needs a way in.
    ///  - 6, open from the accept (TaRapedia history: the 1.3 bug "mortars destroyed before Method 1 or 2 are not counted"
    ///    was removed in 2008-10): the 13 above-ground mortars of the depot killed (counted on this map only), or one use of the player's own Bane
    ///    computer at TaRapedia's terminal (deactivation): either completes 6. Headquarters pays by radio.
    ///  - Markers: the depot's door on the Marshes (3, 4), the front entrance (4), the lower entrance (3), the client's "Bane
    ///    Terminal" at the computer and one at each mortar (6).
    /// </summary>
    public static class MarshesInsertionPointV1
    {
        public const uint MissionId = 663;
        public const uint Level = 37;
        public const uint Toida = MarshesWelcomeWagonV1.Toida;
        public const uint Depot = MarshesBaneSupplyDepotNpcsV1.MapContextId;
        public const uint Force = 4, Stealth = 3, Inside = 5, Mortars = 6;
        public const uint RearArea = 1;

        /// <summary>Scenarios: the alarm, the terminal used.</summary>
        public const uint Alarm = 1, TerminalUsed = 2;
        public const uint AlarmSequence = 30, TerminalSequence = 40;

        public const uint Soldier = 531045, Grenadier = 531046;
        public const uint BaneMortar = 592006, MortarCount = 13;
        public const uint ComputerClass = MarshesUnderTheRadarPartTwoV1.ComputerClass;
        public const uint ComputerState = MarshesUnderTheRadarPartTwoV1.ComputerState;
        public const uint UseArgument = MarshesUnderTheRadarPartTwoV1.UseArgument;
        public const uint BaneTerminal = MarshesBeinvenidosDelVoroV1.BaneTerminal;
        public const string ComputerRole = "mortar-terminal";

        public static readonly (double X, double Y, double Z) FrontAt = (-284, 149.64, 36);
        public static readonly (double X, double Y, double Z) RearAt = (68, 97.64, 120);
        public const double RearRadius = 12, FrontRadius = 15;
        public static readonly ScenePosition TerminalAt = new(-88.2f, 80.04f, -160.4f);
        public static readonly ScenePosition[] GuardsAt =
        {
            new(-284f, 149.64f, 36f), new(-290f, 149.83f, 30f), new(-278.26f, 149.64f, 40.15f), new(-276.8f, 149.64f, 30f)
        };

        /// <summary>The depot's mortar pools (World DB 592144-592156).</summary>
        public static readonly (double X, double Y, double Z)[] MortarsAt =
        {
            (-102.5, 131.4, 288.8), (-65, 131.3, 43), (-65, 131.3, 27.1), (-27.6, 131.6, 101.2), (-26.2, 131.4, -60.1),
            (-18.9, 131.7, 258.6), (38.6, 131.7, -190.1), (65.8, 131.8, -122.1), (71.7, 131.5, 40.7), (77.8, 131.8, -225),
            (97.8, 141.6, -25.5), (103.4, 129.7, 207.9), (212.7, 131.8, -223.2)
        };

        public const uint Experience = 170000;
        public const uint Credits = 6000;
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 6;       // Class VIII Advanced Med Pack
        public const uint EmpGrenade = 111029, EmpGrenades = 9;                // Class VIII EMP Grenade
        public const uint ResTraumaKit = 47600, ResTraumaKits = 3;             // Class VIII Res Trauma Kit
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 9;  // Class VIII Concussion Grenade

        public static string GuardRole(int index) => $"front-guard-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Insertion Point", 2642, Toida, null, Level);
            mission.RadioCompletion();
            mission.LevelPrerequisite(Level);
            mission.Reward(Experience, Credits,
                new[] { (AdvancedMedPack, AdvancedMedPacks), (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits),
                    (ConcussionGrenade, ConcussionGrenades) },
                new (uint, uint)[0]);
            mission.Scenario(Alarm, "A front guard hit: the alarm", 1);
            mission.Scenario(TerminalUsed, "The mortars deactivated at the terminal", 1);
            mission.Area(RearArea, Depot, RearAt.X, RearAt.Y, RearAt.Z, RearRadius, 10);

            mission.Objective(Force, 6247, 6999, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Stealth, 6248, 7000, 1, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Inside, 7501, 7502, 2, MissionObjectiveState.Incomplete);                       // hidden
            mission.Objective(Mortars, 17358, 17359, 3, MissionObjectiveState.Incomplete, counterTextId: 17360);

            // 4 by the alarm; 3 by the lower entrance before it, failed by it.
            mission.Transition(Force);
            mission.Progress(Force, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Alarm);
            mission.Transition(Stealth);
            mission.EnterArea(Stealth, RearArea);
            mission.Transition(Stealth, 2, 2, MissionObjectiveState.Failed);
            mission.Progress(Stealth, 2, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Alarm);

            // 5 on either way in (required: the mission needs a way in); 6 counts from the accept (retail after its 2008-10 fix).
            mission.Transition(Inside);
            mission.ObjectiveState(Inside, Force, MissionObjectiveState.Completed);
            mission.Transition(Inside, 2, 2);
            mission.ObjectiveState(Inside, 2, Stealth, MissionObjectiveState.Completed);

            // 6 by the mortars destroyed or by the terminal.
            mission.Transition(Mortars);
            mission.Progress(Mortars, MissionProgressEventKind.CreatureKilled, BaneMortar, MortarCount, counter: 0);
            mission.Action(Mortars, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Mortars, 2, 2);
            mission.Progress(Mortars, 2, MissionProgressEventKind.ScenarioEvent, 1, null, counter: TerminalUsed);
            mission.Action(Mortars, 1, MissionActionKind.GrantReward, reward: 1, transition: 2);

            var door = MarshesBaneSupplyDepotNpcsV1.DoorAt;
            mission.Indicator(Force, 1, door.X, door.Y, door.Z, 10);
            mission.Indicator(Force, 2, FrontAt.X, FrontAt.Y, FrontAt.Z, FrontRadius, Depot);
            mission.Indicator(Stealth, 3, door.X, door.Y, door.Z, 10);
            mission.Indicator(Stealth, 4, RearAt.X, RearAt.Y, RearAt.Z, RearRadius, Depot);
            mission.NamedIndicator(Mortars, BaneTerminal, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, 8, Depot);
            for (var index = 0; index < MortarsAt.Length; index++)
                mission.Indicator(Mortars, 5 + (uint)index, MortarsAt[index].X, MortarsAt[index].Y, MortarsAt[index].Z, 10, Depot);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/663",
                "Native4 'Method 1: Infiltrate The Depot by Force' 6247/6999, 3 'Method 2: Infiltrate The Depot by Stealth' " +
                "6248/7000, 5 'no desc' 7501/7502, 6 'Disable the above ground Bane Mortars' 17358/17359 counter17360; no " +
                "objectiveconversation. Texts 2642-2645, 6998. Indicator name71 'Bane Terminal'. Map 1451 entrances (-284,148,36), " +
                "(68,96,120), console cluster (-91,80,-160).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Insertion_Point",
                "Giver Elder Toida, receiver Headquarters (radio); requires level 37; 170000XP, 6000 credits; one of 6 Class VIII " +
                "Advanced Med Pack, 9 EMP Grenade, 3 Res Trauma Kit, 9 Concussion Grenade; cameras by the rear entrance; mortar " +
                "terminal (-88.2, 80.0, -160.4).", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesInsertionPointV1",
                "Bound to 1451. The alarm a first hit on (or a defeat of) four front-entrance Thrax (no camera detection): it completes 4 and fails " +
                "3; the lower entrance area r12 at (68,97.6,120) completes 3 before it; hidden 5 (required) on either; 6 open from the accept: 13 mortars " +
                "592006 killed on 1451 or one use of a personal Bane computer at the terminal. Radio turn-in.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The run's own front guards and terminal computer, on the squad's copy of the depot.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Depot,      // taken at Toida on the Marshes, played in the squad's depot
                MapContextId = Depot,
                DamagedSequences = new(),
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { Inside },
                ObjectiveRequirements = { [Mortars] = new MapRequirement(new[] { Depot }) }
            };
            var start = new SceneSequenceDefinition();
            for (var index = 0; index < GuardsAt.Length; index++)
            {
                var role = GuardRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, index % 2 == 0 ? Soldier : Grenadier,
                    GuardsAt[index], Orientation: 1.5708, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                start.World.Add(new EnsureActorIntent($"{role}-stands", role));
                scene.DamagedSequences[role] = AlarmSequence;
                // A guard killed with one shot is no hit by the scene's tick (a kill clears it): its defeat sounds it too.
                scene.DefeatSequences[role] = AlarmSequence;
            }
            scene.Actors[ComputerRole] = new SceneActorDefinition(ComputerRole, SceneActorKind.Object, ComputerClass, TerminalAt,
                Orientation: -1.5708, InitialObjectState: ComputerState,
                UseAction: new SceneObjectAction(MissionId, Mortars, TerminalSequence, UseArgument));
            start.World.Add(new EnsureActorIntent("create-mortar-terminal", ComputerRole));
            scene.Sequences[0] = start;
            scene.Sequences[AlarmSequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, Alarm, 1) } };
            scene.Sequences[TerminalSequence] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("mortar-terminal-done", ComputerRole, false) },
                Signals = { new SceneMissionSignal(MissionId, TerminalUsed, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
