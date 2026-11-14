using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Raid the Conscription Facility (native mission 1497), the Bane Conscription Facility (squad instance 1988): Capt.
    /// Peacock sends the player to bomb the front force field and hack a Bane CommLink Terminal to stand the alarm down.
    ///
    /// Native: texts 13379-13383 (missionconversation): title "Raid the Conscription Facility", log "Bring down the defenses of
    /// the Bane conscription facility.", offer "... I need you to take down the Bane defenses and crack this facility open like
    /// an egg!", turn-in "Standing down the alarm and hacking into the Bane security system remotely now. Stand by.", reminder
    /// "Get in there and secure the facility."; objectives (missionobjective) 1 "Bomb the Front Forcefield" 14011/14012, 2
    /// "Hack Computer Terminal to Stand Down Alarm" 14013/14014, no counters; objective conversation (1497,2) package 1929
    /// variant 1, 14015 "Area Secure - alarm standing down." (the terminal). Item class 26459 MisBCFBombs "Detonator" (template
    /// 111195, stack 2). Indicator name 251 "Conscription Database Interface" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia Raid the Conscription Facility (0.5): giver Capt. Peacock, reward giver Bane CommLink Terminal;
    /// requires Lockdown the Conscription Facility; follow-up Blast the Biolab; 95,000 XP, 8,400 credits; Hellstrom Bio Armor
    /// Boots 4, Pulsar Stealth Armor Helmet 3, Voltrox Graviton Armor Legs 4, Audiodyne Motor Assist Armor Gloves 10 (the old
    /// revision, 01-2008); "pick up the Dominate logos before entering the facility proper" (the Hidden Cave's logos 138, in
    /// the World DB already).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42; taken at Peacock (AshenConscriptionEntryNpcsV1 790659) in the squad's copy; requires 1500. The Detonator
    ///    at the accept (AssignmentIssued, each member his own).
    ///  - The front field is each member's own scene object (UsableForceFieldBaneOutpostWallMajorV01 9572, intact Bane state
    ///    196, as set 16's rear door 10749 is each member's own): it stands for him from his accept while his 1 is open and goes
    ///    when his 1 is done; a member who has 1497 done, or never took it, has none (no public field: a public one would also
    ///    stand in a new copy before a player who did 1497 long ago, and would bar 1551, 1499 and 1501 beyond it). ESTIMATE:
    ///    retail most likely barred a player without 1497 too; the server shows a mission's objects only to its owner, so
    ///    a field for him alone has no mission to belong to. Its stop is the client's collision with its mesh (as the doors').
    ///  - 1 the player's own Explosive Charge (UsableBombHumV01 7870, argument 1; not locked: a lock in state 0 locks nothing)
    ///    5 m before his field: the use sends 1's event; 1 takes the member's Detonator (a member whose Detonator can not be
    ///    taken is refused: his 1 stays open, his field standing, his charge usable), and only then are his field taken away
    ///    and his charge set.
    ///  - 2 the talk with the player's own Bane CommLink Terminal (UsableNPCBaneKeyboard 10519, package 1929, as Crucible
    ///    1215's) on the west side of the HQ's round hall past the field (Central Operations' control room below has no walk
    ///    from the hall that passes the client floor checks: a 1.15 m lip at the spiral's top); it gives the reward. An object can not take a mission in
    ///    (MissionObjectConversations), so the turn-in is by radio (radio completion, as 1447's): a deviation from TaRapedia's
    ///    "reward giver Bane CommLink Terminal" (docs/ashen-playtest.md).
    ///  - Markers: 1 on the charge 5 m before the field, r10 (unnamed), 2 the terminal (251).
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 42: Bio V04 Boots 46005,
    ///    Stealth V03 Helmet 39727, Graviton V04 Legs 25248, Motor Assist V07 Gloves 31088 (38-42; no V10 Uncommon gloves, as
    ///    1243's); one to choose.
    /// </summary>
    public static class AshenRaidTheConscriptionFacilityV1
    {
        public const uint MissionId = 1497;
        public const uint Level = 42;
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint Peacock = AshenConscriptionEntryNpcsV1.Peacock;
        public const uint FieldClass = AshenConscriptionEntryNpcsV1.FieldClass;
        public const uint FieldUp = 196;                // FF_STATE_FACTION_B_INTACT (ForceFields.StateOf: a Bane faction field, intact)
        public const uint BombTheField = 1, HackTheTerminal = 2;

        public const uint DetonatorTemplate = 111195, DetonatorClass = 26459;
        public const string DetonatorKey = "bcf-field-detonator";

        public const uint ChargeClass = 7870;           // UsableBombHumV01 "Explosive Charge"
        public const uint TerminalClass = 10519;        // UsableNPCBaneKeyboard, "Bane CommLink Terminal"
        public const uint TerminalPackage = 1929;       // objectiveconversation 1497,2
        public const uint TerminalName = 167;           // usablenameoverride CONSCRIPTION_DATABASE_INTERFACE "Conscription Database Interface"
        public const uint UseArgument = 1;              // usabledata lookup[7870]: None
        public const string ChargeRole = "bcf-field-charge", TerminalRole = "bcf-commlink-terminal", FieldRole = "bcf-front-field";

        /// <summary>Scenarios: the charge used (1's event), the field down (1 done).</summary>
        public const uint ChargeSet = 1, FieldGoes = 2;
        public const uint ChargeSequence = 11;

        public const uint TerminalIndicator = 251;
        public const double FieldMarkerRadius = 10, ObjectMarkerRadius = 6;    // ESTIMATE

        /// <summary>5 m in front (south) of the field, on the tunnel's floor.</summary>
        public static readonly ScenePosition ChargeAt = new(6.5f, 137.61f, -410f);
        /// <summary>The HQ's round hall past the field, its west side.</summary>
        public static readonly ScenePosition TerminalAt = new(-4f, 135.94f, -372f);
        public const double TerminalOrientation = 0;

        public const uint Experience = 95000;
        public const uint Credits = 8400;
        public const uint BioBoots = 46005;             // Armor_T3_Bio_V04_UNC_Boots_40_to_44
        public const uint StealthHelmet = 39727;        // Armor_T3_Stealth_V03_UNC_Helmet_40_to_44
        public const uint GravitonLegs = 25248;         // Armor_T3_Graviton_V04_UNC_Legs_40_to_44
        public const uint MotorAssistGloves = 31088;    // Armor_T1_MotorAssist_V07_UNC_Gloves_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Raid the Conscription Facility", 13379, Peacock, null, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, StealthHelmet, GravitonLegs, MotorAssistGloves });
            mission.Prerequisite(AshenLockdownTheConscriptionFacilityV1.MissionId);
            mission.RadioCompletion();
            mission.Scenario(ChargeSet, "The field's charge set", 1);
            mission.Scenario(FieldGoes, "The front field down (1 done)", 0);

            mission.Objective(BombTheField, 14011, 14012, 0, MissionObjectiveState.Incomplete);
            mission.Objective(HackTheTerminal, 14013, 14014, 1, MissionObjectiveState.Inactive);

            // 1: the charge set: each member whose 1 completes spends his own Detonator, then his own field goes.
            mission.Transition(BombTheField);
            mission.Progress(BombTheField, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ChargeSet);
            mission.Action(BombTheField, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("the-field-takes-the-detonator", MissionId, DetonatorKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(BombTheField, 2, MissionActionKind.StartScenario, scenario: FieldGoes);
            Open(mission, BombTheField, 3, HackTheTerminal);

            // 2: the terminal's talk; the reward (the turn-in by radio).
            mission.Transition(HackTheTerminal);
            mission.Conversation(HackTheTerminal, TerminalPackage);
            mission.Action(HackTheTerminal, 1, MissionActionKind.GrantReward, reward: 1);

            // 1's marker on the charge (the field's foot is under the tunnel's floor); its circle holds the field 5 m on.
            mission.Indicator(BombTheField, 1, ChargeAt.X, ChargeAt.Y, ChargeAt.Z, FieldMarkerRadius, Facility);
            mission.NamedIndicator(HackTheTerminal, TerminalIndicator, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, ObjectMarkerRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1497",
                "Native1 'Bomb the Front Forcefield' 14011/14012, 2 'Hack Computer Terminal to Stand Down Alarm' 14013/14014, no " +
                "counters; objectiveconversation 1497,2 pkg1929 14015 'Area Secure - alarm standing down.'. Texts 13379-13383. Item " +
                "Detonator 26459 (111195). Indicator 251 Conscription Database Interface.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Raid_the_Conscription_Facility",
                "Capt. Peacock gives, Bane CommLink Terminal rewards; requires Lockdown the Conscription Facility; follow-up Blast the " +
                "Biolab; 95000XP, 8400 credits; Hellstrom Bio Armor Boots 4, Pulsar Stealth Armor Helmet 3, Voltrox Graviton Armor " +
                "Legs 4, Audiodyne Motor Assist Armor Gloves 10; the Dominate logos before the facility proper.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenRaidTheConscriptionFacilityV1",
                "Level42; bound to the squad's copy; requires 1500; radio completion (an object takes no mission in). Detonator at " +
                "the accept. A personal front field 9572 (state 196) across the HQ tunnel (6.5,129.5,-405) while 1 is open (none " +
                "for a member without 1497 open: ESTIMATE); 1 a personal charge 7870 5 m before it: 1 takes each member's " +
                "Detonator, then his field goes and his charge is set; 2 a personal talking terminal 10519 (pkg1929, named 167 usablenameoverride) " +
                "in the HQ hall (-4,135.94,-372); its talk rewards. Items 46005, 39727, 25248, 31088.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The Detonator, the player's own field, charge and terminal, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,   // taken at Peacock in the squad's copy
                MapContextId = Facility,
                Dialogue = new List<MissionDialogueTopicDefinition> { new(HackTheTerminal, TerminalPackage, 1, transitionId: 1) },
                Items = new()
                {
                    new MissionItemBinding(DetonatorKey, DetonatorTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("peacock-gives-the-detonator", MissionId, DetonatorKey, DetonatorTemplate, 1) }
            };
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                UseAction: new SceneObjectAction(MissionId, BombTheField, ChargeSequence, UseArgument));
            var field = AshenConscriptionEntryNpcsV1.FieldAt;
            scene.Actors[FieldRole] = new SceneActorDefinition(FieldRole, SceneActorKind.Object, FieldClass,
                new ScenePosition((float)field.X, (float)field.Y, (float)field.Z), Orientation: AshenConscriptionEntryNpcsV1.FieldYaw,
                InitiallyInteractable: false, InitialObjectState: FieldUp);
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: TerminalOrientation, NameOverrideId: TerminalName,
                Conversation: new SceneObjectConversation(MissionId, HackTheTerminal, TerminalPackage, HackTheTerminal));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent($"create-{FieldRole}", FieldRole),
                    new EnsureActorIntent($"create-{ChargeRole}", ChargeRole),
                    new EnsureActorIntent($"create-{TerminalRole}", TerminalRole)
                }
            };
            // The charge used: the event of 1 only (1 takes the Detonator, takes the field down, then sets the charge).
            scene.Sequences[ChargeSequence] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, ChargeSet, 1) }
            };
            scene.Sequences[FieldGoes] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("field-charge-set", ChargeRole, false),
                    new RemoveActorIntent("front-field-goes", FieldRole)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The objective's action <paramref name="action"/> reveals and opens <paramref name="next"/>.</summary>
        internal static void Open(AshenMissionDataV1 mission, uint objective, uint action, uint next)
        {
            mission.Action(objective, action, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, action + 1, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete);
        }
    }
}
