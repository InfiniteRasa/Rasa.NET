using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Leggo My Logos (native mission 550), the Logos Research Facility (squad instance 1700): Captain Sakai sends the player
    /// to blow up the two Maroxim extraction tanks of the facility's level 3, take the Logos element that powers it from the
    /// Logos Analyzer and get out before the facility blows up; then to Sergeant Fitz in Paludos.
    ///
    /// Native: texts 1490-1493 and 7304 (missionconversation): log "Destroy the primary and secondary receptive extraction
    /// tanks in the level 3 area and retrieve the Logos powering the facility.", offer "destroy the tanks and get that Logos
    /// element away from those bastards. You'll find some of our gear in those Bane crates", turn-in (Fitz) "the receptive
    /// samples you obtained contained Brann DNA"; objectives (missionobjective) 3 "Get Bombs From Nearby Bane Crate."
    /// 9009/9010, 1 "Destroy Primary Maroxim Extraction Device to Obtain Extracted Receptive DNA" 3424/7305 counter 6255
    /// "Maroxim Extraction Devices Destroyed", 5 "Destroy Secondary ..." 9044/9045, 9 "Logos Analysis Lab Security Breached.
    /// Disable Logos Analyzer Forcefield" 10569/10570, 2 "Retrieve Logos Element From Logos Analyzer" 6256/7306, 4
    /// "Analyzation Systems Unstable. Escape Logos Research Before the Facility Explodes." 9011/9012, 6 "Escaped From Logos
    /// Research Self Destruct Successfully." 9049/9050, 8 "Hidden Failed Self Destruct!" 9292/9293, 7 "Speak With Lieutenant
    /// Russell." 9053/9054, 10 "Escaped From Logos Research. Return for Debriefing." 17329/17330; objective conversations
    /// 550,7 package 284 9056 (Russell: "You're finally awake! A special ops officer by the name of Brock double timed you
    /// here ... requisitioning your bane DNA samples") and 550,10 package 2203 17331 (Brock: "you are carrying Bane receptive
    /// DNA ... I'll take them from here. Continue on your way to Sgt. Fitz"). Items: Detonator (class 11057, template 2511,
    /// MisLogosResearchItemDetonators), Logos Receptive DNA (26717, 117042). Classes UsableInertDestBaneExtractionDroneV01
    /// (11255) and UsableTwoStateBaneIndustrialRoomLogosStoneAnalyzer (7241, states 55/56). LRF map_region 20056 "Logos
    /// Analysis Lab" (608.2, 262.8, -351.8), its console bank (605.8-610.8, 247.5, -349.2 to -354.2).
    ///
    /// Sourced: TaRapedia Leggo My Logos: giver Captain Sakai, receiver Sergeant Fitz (Paludos); requires Freedom's Just
    /// Another Word; 7,600 credits, XP "?"; one of Vextronics Sonic Torqueshell Rifle, Teleract Cryogenic Blade, ChiTech Pulse
    /// Cannon, Vextronics Virulent Leech Gun; Special Ops Officer Brock takes the samples. Patch notes 2007-07-03 "The timer
    /// for escaping the facility is too low", 2007-11-14 the timer fixed, "Added mini boss into logos analyzer room" (not
    /// built). TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE, the triage's choices of 2026-11-02; confidence 0.3):
    ///  - Bound to the squad's facility. 3: a Bane cargo crate by the south bio lab gives two detonators. The tanks: an
    ///    extraction drone in each bio lab of level 3 (south, then north), blown by an Explosive Charge beside it, one
    ///    detonator each (the drones are not targetable in the client, as One Way Trip's device); each gives one Logos
    ///    Receptive DNA. The secondary's charge comes only after the primary.
    ///  - 9: a forcefield console at the Logos Analysis Lab's door (no field object is put in: no client class fits); 2: the
    ///    Logos Analyzer (7241, shown, not used) on the lab's floor and a keyboard before it: its use takes the element and
    ///    the analyzer goes to its other state. Not the LRF Logos shrine (the triage's conflict 20 took it; the client's
    ///    analyzer class fits the objective's text better, ESTIMATE).
    ///  - 4 and 6 open together: 4 fails after 300 s (DeadlineSeconds, ESTIMATE); walking into the facility's entrance hall
    ///    (EscapeArea, round the door to the Marshes) before that completes both; Brock comes there, and 10 opens: his talk
    ///    takes the DNA. 4 failed: hidden 8 completes, the player is set down in the entrance hall by Russell (no knock-out: a
    ///    transfer), 7 and 10 open, 6 fails; Russell's talk (Brock took the DNA) completes 7 and 10 with it. Either way Fitz
    ///    pays. A player who dies after 2 respawns at the entrance's hospital, inside EscapeArea: escaped (not prevented).
    ///  - XP 80,000 (TaRapedia has none; between 548's 75,000 and the zone's later ones, ESTIMATE). The weapons the Uncommon
    ///    base templates of the level 35 bands (no client block of TaRapedia's four).
    ///  - Markers on every visible objective: the crate, the two tanks, the console, the analyzer, the entrance hall (4, 6),
    ///    Russell (7), Brock (10).
    /// </summary>
    public static class MarshesLeggoMyLogosV1
    {
        public const uint MissionId = 550;
        public const uint Level = 35;
        public const uint Sakai = MarshesLogosResearchNpcsV1.Sakai, Fitz = MarshesLogosResearchNpcsV1.Fitz;
        public const uint Facility = MarshesLogosResearchNpcsV1.MapContextId;
        public const uint Primary = 1, Logos = 2, Bombs = 3, Escape = 4, Secondary = 5, Escaped = 6, Russell = 7, FailedEscape = 8,
            Field = 9, Debrief = 10;
        public const uint EscapeArea = 1;
        public const uint DeadlineSeconds = 300;

        /// <summary>Scenarios: what comes (their sequences), then the results (1 bombs, 2 primary, 3 secondary, 4 field, 5 logos).</summary>
        public const uint PrimaryArmed = 1, SecondaryArmed = 2, Breach = 3, AnalyzerReady = 4, BrockComes = 5, Blast = 6, Results = 7,
            PrimaryTally = 8;   // the primary tank's own count (its counter text 6255)
        public const uint CrateUsed = 21, PrimaryBlown = 22, SecondaryBlown = 23, FieldDown = 24, LogosTaken = 25;

        public const uint DetonatorTemplate = 2511, Detonators = 2, DnaTemplate = 117042, DnaSamples = 2;
        public const string DetonatorKey = "lrf-detonators", DnaKey = "lrf-receptive-dna";
        public const uint CrateClass = PlateauBombVoyageV1.CrateClass, CrateUseArgument = PlateauBombVoyageV1.UseArgument,
            CrateState = PlateauBombVoyageV1.CrateState;
        public const uint DeviceClass = 11255;          // UsableInertDestBaneExtractionDroneV01
        public const uint DeviceIntactState = 110, DeviceDestroyedState = 2, DeviceHitPoints = 100;
        public const uint ChargeClass = 7870;   // UsableBombHumV01 "Explosive Charge"
        public const uint KeyboardClass = PlateauSilentButDeadlyV1.ComputerClass, KeyboardState = PlateauSilentButDeadlyV1.ComputerState;
        public const uint AnalyzerClass = 7241;          // UsableTwoStateBaneIndustrialRoomLogosStoneAnalyzer
        public const uint AnalyzerReadyState = 55, AnalyzerTakenState = 56;
        public const uint UseArgument = 1;
        public const string CrateRole = "bomb-crate", PrimaryRole = "primary-tank", SecondaryRole = "secondary-tank",
            PrimaryChargeRole = "primary-charge", SecondaryChargeRole = "secondary-charge", ConsoleRole = "forcefield-console",
            AnalyzerRole = "logos-analyzer", AnalyzerKeyboardRole = "analyzer-keyboard", BrockRole = "brock", RussellRole = "russell";

        public static readonly ScenePosition CrateAt = new(580f, 257.79f, -190f);
        public static readonly ScenePosition PrimaryAt = new(608f, 257.77f, -212f), PrimaryChargeAt = new(608f, 257.77f, -206f);
        public static readonly ScenePosition SecondaryAt = new(608f, 257.77f, -44f), SecondaryChargeAt = new(608f, 257.77f, -50f);
        public static readonly ScenePosition ConsoleAt = new(604f, 257.77f, -296f);
        public static readonly ScenePosition AnalyzerAt = new(608f, 247.78f, -346f), AnalyzerKeyboardAt = new(608f, 248.15f, -340f);
        public const double AnalyzerYaw = 3.1416;      // the keyboard faces south, to the analyzer

        /// <summary>The entrance hall round the door to the Marshes: the escape; Brock and Russell there, the failed one set down there.</summary>
        public static readonly (double X, double Y, double Z) EscapeAt = (868, 353.77, -120);
        public const double EscapeRadius = 12;
        /// <summary>On the hall's open client floor (353.61); (866, -118) was in a gap of it by a wall (to-fix 33).</summary>
        public static readonly ScenePosition BrockAt = new(871f, 353.77f, -118f);
        public static readonly ScenePosition RussellAt = new(866f, 353.77f, -112f);
        public static readonly ScenePosition WakeAt = new(868f, 353.77f, -116f);
        public const double WakeYaw = 0;
        public const double ObjectRadius = 8;

        public const uint Experience = 80000;
        public const uint Credits = 7600;
        public const uint SonicTorqueshellRifle = 49652;   // Weapon_Avatar_Torqueshell_Rifle_Sonic_UNC_33_to_37
        public const uint CryogenicBlade = 47648;          // Weapon_Avatar_Blade_Ice_UNC_31_to_35
        public const uint PulseCannon = 15437;             // Weapon_Avatar_RocketLauncher_EMP_UNC_34_to_38
        public const uint VirulentLeechGun = 15295;        // Weapon_Avatar_LeechGun_Virulent_UNC_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Leggo My Logos", 1490, Sakai, Fitz, Level);
            mission.Reward(Experience, Credits, new[] { SonicTorqueshellRifle, CryogenicBlade, PulseCannon, VirulentLeechGun });
            mission.Prerequisite(MarshesFreedomsJustAnotherWordV1.MissionId);
            mission.Scenario(PrimaryArmed, "The detonators held: the primary tank's charge", 0);
            mission.Scenario(SecondaryArmed, "The primary tank blown: the secondary tank's charge", 0);
            mission.Scenario(Breach, "Both tanks blown: the forcefield console", 0);
            mission.Scenario(AnalyzerReady, "The forcefield down: the analyzer's keyboard", 0);
            mission.Scenario(BrockComes, "Escaped: Special Ops Officer Brock", 0);
            mission.Scenario(Blast, "The self destruct: set down by Lieutenant Russell", 0);
            mission.Scenario(Results, "Bombs, primary tank, secondary tank, forcefield, Logos element", 5);
            mission.Scenario(PrimaryTally, "The primary tank blown", 1);
            mission.Area(EscapeArea, Facility, EscapeAt.X, EscapeAt.Y, EscapeAt.Z, EscapeRadius);

            mission.Objective(Bombs, 9009, 9010, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Primary, 3424, 7305, 1, MissionObjectiveState.Inactive, counterTextId: 6255);
            mission.Objective(Secondary, 9044, 9045, 2, MissionObjectiveState.Inactive);
            mission.Objective(Field, 10569, 10570, 3, MissionObjectiveState.Inactive);
            mission.Objective(Logos, 6256, 7306, 4, MissionObjectiveState.Inactive);
            mission.Objective(Escape, 9011, 9012, 5, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Escaped, 9049, 9050, 6, MissionObjectiveState.Inactive, required: false);
            mission.Objective(FailedEscape, 9292, 9293, 7, MissionObjectiveState.Incomplete, required: false);   // hidden
            mission.Objective(Russell, 9053, 9054, 8, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Debrief, 17329, 17330, 9, MissionObjectiveState.Inactive);

            void Step(uint objective, uint result, uint? target, uint next, uint scenario, uint counter = Results)
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, result, target, counter: counter);
                MarshesTasteOfTheirOwnMedicineV1.Open(mission, objective, 1, next);
                mission.Action(objective, 3, MissionActionKind.StartScenario, scenario: scenario);
            }
            Step(Bombs, 1, null, Primary, PrimaryArmed);
            mission.Action(Bombs, 4, MissionActionKind.GrantReward, reward: 1);   // the turn-in's reward (Fitz)
            Step(Primary, 1, 1, Secondary, SecondaryArmed, PrimaryTally);
            Step(Secondary, 3, null, Field, Breach);
            Step(Field, 4, null, Logos, AnalyzerReady);
            // 2: the element taken opens 4 (its deadline) and 6.
            mission.Transition(Logos);
            mission.Progress(Logos, MissionProgressEventKind.ScenarioEvent, 5, null, counter: Results);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Logos, 1, Escape);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Logos, 3, Escaped);

            // 4 and 6: the entrance hall reached in time completes both; 6 opens 10 and brings Brock.
            mission.Transition(Escape);
            mission.EnterArea(Escape, EscapeArea);
            mission.FailOnDeadline(Escape, DeadlineSeconds, 2);
            mission.Transition(Escaped);
            mission.EnterArea(Escaped, EscapeArea);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Escaped, 1, Debrief);
            mission.Action(Escaped, 3, MissionActionKind.StartScenario, scenario: BrockComes);
            mission.Transition(Escaped, 2, 2, MissionObjectiveState.Failed);
            mission.ObjectiveState(Escaped, 2, Escape, MissionObjectiveState.Failed);

            // Hidden 8: 4 failed - set down by Russell; 7 and 10 open.
            mission.Transition(FailedEscape);
            mission.ObjectiveState(FailedEscape, Escape, MissionObjectiveState.Failed);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, FailedEscape, 1, Russell);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, FailedEscape, 3, Debrief);
            mission.Action(FailedEscape, 5, MissionActionKind.StartScenario, scenario: Blast);

            mission.Transition(Russell);
            mission.Conversation(Russell, MarshesLogosResearchNpcsV1.RussellPackage);
            mission.Action(Russell, 1, MissionActionKind.RemoveMissionItems,
                itemIntent: new RemoveMissionItemsIntent("brock-took-the-dna", MissionId, DnaKey));

            // 10: Brock's talk (he takes the DNA), or Russell's after the self destruct.
            mission.Transition(Debrief);
            mission.Conversation(Debrief, MarshesLogosResearchNpcsV1.BrockPackage);
            mission.Action(Debrief, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("brock-takes-the-dna", MissionId, DnaKey, DnaSamples, MissionItemScope.AssignmentIssued));
            mission.Transition(Debrief, 2, 2);
            mission.ObjectiveState(Debrief, 2, Russell, MissionObjectiveState.Completed);

            mission.Indicator(Bombs, 1, CrateAt.X, CrateAt.Y, CrateAt.Z, ObjectRadius, Facility);
            mission.Indicator(Primary, 2, PrimaryAt.X, PrimaryAt.Y, PrimaryAt.Z, ObjectRadius, Facility);
            mission.Indicator(Secondary, 3, SecondaryAt.X, SecondaryAt.Y, SecondaryAt.Z, ObjectRadius, Facility);
            mission.Indicator(Field, 4, ConsoleAt.X, ConsoleAt.Y, ConsoleAt.Z, ObjectRadius, Facility);
            mission.Indicator(Logos, 5, AnalyzerAt.X, AnalyzerAt.Y, AnalyzerAt.Z, ObjectRadius, Facility);
            mission.Indicator(Escape, 6, EscapeAt.X, EscapeAt.Y, EscapeAt.Z, EscapeRadius, Facility);
            mission.Indicator(Escaped, 7, EscapeAt.X, EscapeAt.Y, EscapeAt.Z, EscapeRadius, Facility);
            mission.Indicator(Russell, 8, RussellAt.X, RussellAt.Y, RussellAt.Z, ObjectRadius, Facility);
            mission.Indicator(Debrief, 9, BrockAt.X, BrockAt.Y, BrockAt.Z, ObjectRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/550",
                "Native3 bombs 9009/9010, 1 primary 3424/7305 counter6255, 5 secondary 9044/9045, 9 forcefield 10569/10570, 2 Logos " +
                "element 6256/7306, 4 escape 9011/9012, 6 escaped 9049/9050, 8 hidden failed 9292/9293, 7 Russell 9053/9054, 10 " +
                "debriefing 17329/17330. objectiveconversation 550,7 pkg284 9056 (Russell), 550,10 pkg2203 17331 (Brock). Texts " +
                "1490-1493, 7304. Detonator 11057/2511, Logos Receptive DNA 26717/117042; classes 11255 extraction drone, 7241 " +
                "Logos stone analyzer (55/56). Names 3135 Russell, 10133 Brock. LRF region 20056 'Logos Analysis Lab'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Leggo_My_Logos",
                "Giver Captain Sakai, receiver Sergeant Fitz (Paludos); requires 548; 7600 credits, XP unknown; Vextronics Sonic " +
                "Torqueshell Rifle/Teleract Cryogenic Blade/ChiTech Pulse Cannon/Vextronics Virulent Leech Gun; Brock takes the " +
                "samples. Patches: escape timer too low (2007-07), fixed and a mini boss added (2007-11). TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesLeggoMyLogosV1",
                "Bound to the squad's LRF. Crate 21568 (2 detonators) by the south bio lab; extraction drones 11255 in the south and " +
                "north bio labs, each blown by a charge 7870 (1 detonator, 1 DNA); forcefield console 20631 at the analysis lab's " +
                "door (no field object); analyzer 7241 shown, its keyboard 20631 takes the element (not the LRF Logos shrine). 4 and " +
                "6 together, 4 fails after 300 s; the entrance hall in time: Brock 760371 (takes the DNA, 10); late: hidden 8, set " +
                "down by Russell 760370 (7 completes 10). XP 80000; weapons 49652, 47648, 15437, 15295. Places, times estimates.", 0.3);

            mission.Enable(Scene());
        }

        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                HiddenObjectiveIds = new() { FailedEscape },
                Items = new()
                {
                    new MissionItemBinding(DetonatorKey, DetonatorTemplate, MissionItemScope.AssignmentIssued, Detonators,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(DnaKey, DnaTemplate, MissionItemScope.AssignmentIssued, DnaSamples,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, CrateAt,
                Orientation: 1.5708, InitialObjectState: CrateState, UseAction: new SceneObjectAction(MissionId, Bombs, CrateUsed, CrateUseArgument));
            foreach (var (device, at, objective, blown) in new[]
            {
                (PrimaryRole, PrimaryAt, Primary, PrimaryBlown), (SecondaryRole, SecondaryAt, Secondary, SecondaryBlown)
            })
                scene.Actors[device] = new SceneActorDefinition(device, SceneActorKind.Object, DeviceClass, at,
                    InitialObjectState: DeviceIntactState, InitiallyInteractable: false,
                    Destruction: new SceneObjectDestruction(MissionId, objective, blown, DeviceHitPoints, DeviceDestroyedState));
            scene.Actors[PrimaryChargeRole] = new SceneActorDefinition(PrimaryChargeRole, SceneActorKind.Object, ChargeClass, PrimaryChargeAt,
                UseAction: new SceneObjectAction(MissionId, Primary, PrimaryBlown, UseArgument));
            scene.Actors[SecondaryChargeRole] = new SceneActorDefinition(SecondaryChargeRole, SceneActorKind.Object, ChargeClass,
                SecondaryChargeAt, UseAction: new SceneObjectAction(MissionId, Secondary, SecondaryBlown, UseArgument));
            scene.Actors[ConsoleRole] = new SceneActorDefinition(ConsoleRole, SceneActorKind.Object, KeyboardClass, ConsoleAt,
                InitialObjectState: KeyboardState, UseAction: new SceneObjectAction(MissionId, Field, FieldDown, UseArgument));
            scene.Actors[AnalyzerRole] = new SceneActorDefinition(AnalyzerRole, SceneActorKind.Object, AnalyzerClass, AnalyzerAt,
                InitialObjectState: AnalyzerReadyState, InitiallyInteractable: false);
            scene.Actors[AnalyzerKeyboardRole] = new SceneActorDefinition(AnalyzerKeyboardRole, SceneActorKind.Object, KeyboardClass,
                AnalyzerKeyboardAt, Orientation: AnalyzerYaw, InitialObjectState: KeyboardState,
                UseAction: new SceneObjectAction(MissionId, Logos, LogosTaken, UseArgument));
            scene.Actors[BrockRole] = new SceneActorDefinition(BrockRole, SceneActorKind.Creature, MarshesLogosResearchNpcsV1.Brock, BrockAt,
                Orientation: 3.1416, GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });
            scene.Actors[RussellRole] = new SceneActorDefinition(RussellRole, SceneActorKind.Creature, MarshesLogosResearchNpcsV1.Russell,
                RussellAt, Orientation: 3.1416, GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });

            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("create-crate", CrateRole), new EnsureActorIntent("create-primary", PrimaryRole),
                    new EnsureActorIntent("create-secondary", SecondaryRole), new EnsureActorIntent("create-analyzer", AnalyzerRole)
                }
            };
            scene.Sequences[CrateUsed] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("crate-done", CrateRole, false) },
                Character = { new IssueMissionItemIntent("take-detonators", MissionId, DetonatorKey, DetonatorTemplate, Detonators) },
                Signals = { new SceneMissionSignal(MissionId, Results, 1) }
            };
            scene.Sequences[PrimaryArmed] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-primary-charge", PrimaryChargeRole) } };
            scene.Sequences[SecondaryArmed] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-secondary-charge", SecondaryChargeRole) } };
            scene.Sequences[PrimaryBlown] = Blow(PrimaryChargeRole, PrimaryRole, 2);
            scene.Sequences[PrimaryBlown].Signals.Add(new SceneMissionSignal(MissionId, PrimaryTally, 1));
            scene.Sequences[SecondaryBlown] = Blow(SecondaryChargeRole, SecondaryRole, 3);
            scene.Sequences[Breach] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-console", ConsoleRole) } };
            scene.Sequences[FieldDown] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("console-done", ConsoleRole, false) },
                Signals = { new SceneMissionSignal(MissionId, Results, 4) }
            };
            scene.Sequences[AnalyzerReady] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-analyzer-keyboard", AnalyzerKeyboardRole) } };
            scene.Sequences[LogosTaken] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("keyboard-done", AnalyzerKeyboardRole, false),
                    new TransitionObjectStateIntent("element-taken", AnalyzerRole, AnalyzerTakenState)
                },
                Signals = { new SceneMissionSignal(MissionId, Results, 5) }
            };
            scene.Sequences[BrockComes] = new SceneSequenceDefinition { World = { new EnsureActorIntent("brock-comes", BrockRole) } };
            scene.Sequences[Blast] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("russell-comes", RussellRole),
                    new TransferIntent("set-down-by-russell", Facility, WakeAt, WakeYaw)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);

        /// <summary>A tank blown at its charge: one detonator, the tank destroyed, one sample of DNA, the result.</summary>
        private static SceneSequenceDefinition Blow(string charge, string device, uint result) => new()
        {
            World =
            {
                new SetInteractionIntent($"{charge}-set", charge, false),
                new TransitionObjectStateIntent($"{device}-destroyed", device, DeviceDestroyedState)
            },
            Character =
            {
                new ConsumeMissionItemIntent($"{charge}-detonator", MissionId, DetonatorKey, 1, MissionItemScope.AssignmentIssued),
                new IssueMissionItemIntent($"{device}-dna", MissionId, DnaKey, DnaTemplate, 1)
            },
            Signals = { new SceneMissionSignal(MissionId, Results, result) }
        };
    }
}
