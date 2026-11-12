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
    /// The Choices We Make (native mission 1078), the Village Ruins (squad instance 1743): Headquarters calls after The Lost
    /// Ones: Operation Master Myrquar, behind the toxin pumping, ships out with the next prisoners in 25 minutes; the player
    /// kills him, takes the Logos element that powers the outpost in Central Operations, destroys the Bane equipment there,
    /// takes a toxin sample and brings it to Elder Toida in Paludos. Not both this and Needle in a Bane Camp (893).
    ///
    /// Native: texts 6077-6080 and 7278 (missionconversation): title "The Choices We Make", log "Terminate Operation Master
    /// Myrquar before he leaves on a transport then indentify and disable the Bane base power.", offer "he will be shipping out
    /// with the next batch of Forean prisoners in the next 25 minutes", turn-in (Toida) "so this is the cause of the sickness in
    /// P'reo Das ... a pure sample"; objectives (missionobjective) 1 "Intercept and Terminate Operation Master Myrquar"
    /// 6136/7279, 2 "Enter Central Operations and Retrieve the Element Powering the Bane Outpost." 6137/7280, 3 "Destory Bane
    /// Equipment and Gather Toxin Sample" 17190/17191, 4 "Return Toxin Sample to Elder Toida in Palados" 17192/17193; no
    /// counters; objectiveconversation 1078,4 package 288 text 17194 (Toida: "What is this that you have brought to me?").
    /// Items: Level 1 Security Clearance Pass (class 22979 MisMarshsesVillageRuinsKeyCard, template 45453), Concentrated Toxin
    /// Sample (26696 Mis_Marshes_PreoDas_MoroximSample, 117028). The Village Ruins' Logos "Disperse" (logos 30, class 12678, in
    /// the control room at -33, 65.5, 390).
    ///
    /// Sourced: TaRapedia The Choices We Make: giver Headquarters (radio), at P'reo Das; requires The Lost Ones; 7,000 credits,
    /// no XP given; Myrquar's timer 00:25:00; one of Vextronics Electric Torqueshell Rifle, Vextronics Photonic Blade, ChiTech
    /// Pulse Cannon, Vitalius Cryogenic Leech Gun. TaRapedia P'reo Das: 893 and 1078 "are mutually exclusive"; the Disperse
    /// Logos "in the central console of the Bane control room", this path's reward; Dirty Secrets' bug note: the key to
    /// Central Operations "is obtained from Operation Master Myrquar". TR MMO DB level 33.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's choices 12 and 19 and of 2026-11-02):
    ///  - Offered by radio at The Lost Ones' turn-in and again on arrival in the Village Ruins, not once 893 is done. Bound to
    ///    the squad's Village Ruins.
    ///  - 1: Myrquar (760375) and four Thrax Soldiers on the walked-to ground south of the landing pads (the pads are a navmesh
    ///    island); 1 fails 1500 s after the accept (the offer's 25 minutes), the mission with it, and a scene timer of the same
    ///    length takes Myrquar and his guards away (he ships out). His death completes 1, gives the key (the bug note; the
    ///    control room has no locked door here: the key opens nothing) and stops that timer.
    ///  - 2: the Disperse Logos; one the player has already counts (Looking For Logos?' way).
    ///  - 3 opens with 2 done: three pieces of Bane lab equipment (class 20595, as Slay on Target's) and a Bane crate (11116,
    ///    argument 3) with the sample in the control room round the Logos; the three destroyed and the sample taken, in any
    ///    order, complete 3. Toida's talk takes the sample and completes 4; she pays.
    ///  - The fork with 893 is made at Myrquar (893's offer: "not enough time to both save this village and intercept the next
    ///    Bane convoy"): his death signals 893, which fails if it is held; 893's talk while he lives signals 1078, whose 1
    ///    fails (the mission with it) and he and his guards leave. A signal to a mission not held changes nothing.
    ///  - XP 65,000 (TaRapedia none). The weapons the Uncommon base templates of the bands that hold level 33. Markers at
    ///    Myrquar, the Logos, the client's "Bane Equipment" (72, by name only) round the control room, and Toida.
    /// </summary>
    public static class MarshesTheChoicesWeMakeV1
    {
        public const uint MissionId = 1078;
        public const uint Level = 33;
        public const uint Toida = MarshesPreoDasNpcsV1.Toida, ToidaPackage = 288;
        public const uint Ruins = MarshesPreoDasNpcsV1.MapContextId;
        public const uint Myrquar = 1, Element = 2, Equipment = 3, Return = 4, LabTally = 5;
        public const uint DeadlineSeconds = 1500;

        /// <summary>Scenarios: the lab set down (its sequence), then the results (Myrquar down; the lab: three pieces and the sample).</summary>
        public const uint LabReady = 1, MyrquarDown = 2, LabResults = 3;
        public const uint MyrquarSequence = 11, ShipsOutSequence = 12, PieceSequence = 21, SampleSequence = 31;
        public const string ShipsOutTimer = "myrquar-ships-out";

        public const uint DisperseLogos = 30;
        public static readonly (double X, double Y, double Z) DisperseAt = (-33, 65.5, 390);
        public const uint KeyTemplate = 45453, KeyClass = 22979;
        public const string KeyKey = "village-ruins-security-pass";
        public const uint SampleTemplate = 117028, SampleClass = 26696;
        public const string SampleKey = "moroxim-toxin-sample";
        public const uint EquipmentClass = MarshesSlayOnTargetV1.EquipmentClass;
        public const uint IntactState = MarshesSlayOnTargetV1.IntactState, DestroyedState = MarshesSlayOnTargetV1.DestroyedState;
        public const uint EquipmentHitPoints = MarshesSlayOnTargetV1.EquipmentHitPoints;
        public const uint CrateClass = MarshesSlayOnTargetV1.DispenserClass, CrateArgument = MarshesSlayOnTargetV1.DispenserArgument;
        public const uint EquipmentMarker = 72;         // missionobjectiveindicatorlanguage "Bane Equipment"
        public const string MyrquarRole = "myrquar", CrateRole = "toxin-crate";
        public const uint Soldier = MarshesSlayOnTargetV1.Soldier;

        public static readonly ScenePosition MyrquarAt = new(25f, 59.65f, 270f);
        public static readonly ScenePosition[] GuardsAt =
        {
            new(21f, 59.84f, 265f), new(29f, 59.57f, 265f), new(21f, 59.57f, 275f), new(29f, 59.61f, 275f)
        };
        public static readonly ScenePosition[] PiecesAt = { new(-25f, 63.79f, 396f), new(-37f, 63.77f, 396f), new(-31f, 63.77f, 405f) };
        public static readonly ScenePosition CrateAt = new(-31f, 63.77f, 384f);
        /// <summary>The control room (TaRapedia's "Bane control room"), round the Disperse Logos.</summary>
        public static readonly (double X, double Y, double Z) ControlRoomAt = (-31, 63.77, 396);
        public const double ControlRoomRadius = 18;
        public static readonly (double X, double Y, double Z) ToidaAt = (-320.8, 218.48, -6.8);
        public const double MarkerRadius = 8;

        public const uint Experience = 65000;
        public const uint Credits = 7000;
        public const uint ElectricTorqueshellRifle = 49572;    // Weapon_Avatar_Torqueshell_Rifle_Electric_UNC_33_to_37
        public const uint PhotonicBlade = 47668;               // Weapon_Avatar_Blade_Laser_UNC_31_to_35
        public const uint PulseCannon = 17681;                 // Weapon_Avatar_RocketLauncher_EMP_UNC_29_to_33
        public const uint CryogenicLeechGun = 47764;           // Weapon_Avatar_LeechGun_Ice_UNC_29_to_33

        public static string GuardRole(int index) => $"myrquar-guard-{index + 1}";
        public static string PieceRole(int index) => $"bane-equipment-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Choices We Make", 6077, null, Toida, Level);
            mission.Reward(Experience, Credits, new[] { ElectricTorqueshellRifle, PhotonicBlade, PulseCannon, CryogenicLeechGun });
            mission.Prerequisite(MarshesTheLostOnesV1.MissionId);
            mission.RadioOffer(
                MissionOfferSourceDefinition.MissionRewarded(MarshesTheLostOnesV1.MissionId) with
                {
                    Requirement = MarshesNeedleInABaneCampV1.NotTheOtherPath(MarshesNeedleInABaneCampV1.MissionId)
                },
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Ruins,
                    Requirement: MarshesNeedleInABaneCampV1.NotTheOtherPath(MarshesNeedleInABaneCampV1.MissionId)));
            mission.Scenario(MyrquarDown, "Operation Master Myrquar killed", 1);

            mission.Objective(Myrquar, 6136, 7279, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Element, 6137, 7280, 1, MissionObjectiveState.Inactive);
            mission.Objective(Equipment, 17190, 17191, 2, MissionObjectiveState.Inactive);
            mission.Objective(Return, 17192, 17193, 3, MissionObjectiveState.Inactive);

            mission.Transition(Myrquar);
            mission.Progress(Myrquar, MissionProgressEventKind.ScenarioEvent, 1, null, counter: MyrquarDown);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Myrquar, 1, Element);
            mission.FailOnDeadline(Myrquar, DeadlineSeconds, 2);

            Lab(mission, MissionId, Element, Equipment, LabTally, Return, DisperseLogos, 17190, 17191, 4, gated: true);

            mission.Transition(Return);
            mission.Conversation(Return, ToidaPackage);
            mission.Action(Return, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("toida-takes-the-sample", MissionId, SampleKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Return, 2, MissionActionKind.GrantReward, reward: 1);
            // The choice is made at Myrquar: his death closes 893 (the convoy intercepted, no time to save the village), and 893
            // done while he lives fails 1 (transition 3) and sends him and his guards away.
            MarshesNeedleInABaneCampV1.ClosedByTheOther(mission, "Needle in a Bane Camp", (Myrquar, 3));
            mission.Scenario(ShipsOutSequence, "893 done: Myrquar and his guards leave", 0);
            mission.Action(Myrquar, 1, MissionActionKind.StartScenario, scenario: ShipsOutSequence, transition: 3);

            mission.Indicator(Myrquar, 1, MyrquarAt.X, MyrquarAt.Y, MyrquarAt.Z, 12, Ruins);
            mission.Indicator(Element, 2, DisperseAt.X, DisperseAt.Y, DisperseAt.Z, MarkerRadius, Ruins);
            mission.NamedIndicator(Equipment, EquipmentMarker, ControlRoomAt.X, ControlRoomAt.Y, ControlRoomAt.Z, ControlRoomRadius, Ruins);
            mission.Indicator(Return, 3, ToidaAt.X, ToidaAt.Y, ToidaAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1078",
                "Native1 'Intercept and Terminate Operation Master Myrquar' 6136/7279, 2 'Enter Central Operations and Retrieve the " +
                "Element' 6137/7280, 3 'Destory Bane Equipment and Gather Toxin Sample' 17190/17191, 4 'Return Toxin Sample to " +
                "Elder Toida in Palados' 17192/17193; no counters; objectiveconversation 1078,4 pkg288 17194. Texts 6077-6080, 7278 " +
                "(offer: 25 minutes). Name 10226 Myrquar. Items 22979/45453 key pass, 26696/117028 toxin sample. Logos 30 " +
                "Disperse (-33,65.5,390). IND 72 'Bane Equipment' (by name).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Choices_We_Make",
                "Giver Headquarters (radio), P'reo Das; requires The Lost Ones; 7000 credits, no XP; Myrquar timer 00:25:00; one " +
                "of Vextronics Electric Torqueshell Rifle, Photonic Blade, ChiTech Pulse Cannon, Vitalius Cryogenic Leech Gun. " +
                "P'reo Das page: 893/1078 mutually exclusive; Disperse in the control room's central console. TR MMO DB level33.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTheChoicesWeMakeV1",
                "Radio offer at 1076's reward and on arrival in 1743, not once 893 is done; bound to 1743. Myrquar 760375 and 4 " +
                "Thrax 531045 south of the landing pads (25,59.65,270); 1 fails after 1500 s, a scene timer takes him away; his " +
                "death gives the key (opens nothing). 2 Logos 30, owned counts. 3: 3 lab equipment 20595 and a crate 11116 (arg3, the sample) in the control " +
                "room. Myrquar's death signals 893 (fails if held); 893's talk while he lives fails 1. Toida's talk takes the sample. XP 65000; weapons 49572, 47668, 17681, 47764.", 0.3);

            var scene = LabScene(MissionId, Element, LabTally, PiecesAt, CrateAt, gated: true);
            scene.Items.Add(new MissionItemBinding(KeyKey, KeyTemplate, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
            scene.Requirement = MarshesNeedleInABaneCampV1.NotTheOtherPath(MarshesNeedleInABaneCampV1.MissionId);
            scene.DefeatSequences = new() { [MyrquarRole] = MyrquarSequence };
            scene.Actors[MyrquarRole] = new SceneActorDefinition(MyrquarRole, SceneActorKind.Creature, MarshesPreoDasNpcsV1.Myrquar, MyrquarAt,
                Orientation: 3.1416, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
            scene.Sequences[0].World.Add(new EnsureActorIntent("myrquar-stands", MyrquarRole));
            for (var index = 0; index < GuardsAt.Length; index++)
            {
                var role = GuardRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Soldier, GuardsAt[index],
                    Orientation: 3.1416, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                scene.Sequences[0].World.Add(new EnsureActorIntent($"{role}-stands", role));
            }
            // He ships out at the deadline with his guards (the objective fails by its own timer at the same time).
            scene.Sequences[0].Timers.Add(new SequenceTimer(ShipsOutTimer, DeadlineSeconds * 1000, ShipsOutSequence));
            var shipsOut = new SceneSequenceDefinition { World = { new RemoveActorIntent("myrquar-ships-out", MyrquarRole) } };
            for (var index = 0; index < GuardsAt.Length; index++)
                shipsOut.World.Add(new RemoveActorIntent($"{GuardRole(index)}-ships-out", GuardRole(index)));
            scene.Sequences[ShipsOutSequence] = shipsOut;
            scene.Sequences[MyrquarSequence] = new SceneSequenceDefinition
            {
                Character = { new IssueMissionItemIntent("myrquars-key", MissionId, KeyKey, KeyTemplate, 1) },
                Signals =
                {
                    new SceneMissionSignal(MissionId, MyrquarDown, 1),
                    // The choice made: 893 closes (a held 893 fails; one not held is not touched).
                    new SceneMissionSignal(MarshesNeedleInABaneCampV1.MissionId, MarshesNeedleInABaneCampV1.ClosedByOther, 1)
                },
                Timers = { new SequenceTimer(ShipsOutTimer, 0, ShipsOutSequence, Cancel: true) }
            };
            mission.Enable(scene);
        }

        /// <summary>
        /// The Logos objective <paramref name="element"/> (the Disperse Logos, one already owned counting) and the lab objective
        /// <paramref name="equipment"/> (three pieces and the sample in any order, counted by the hidden <paramref name="tally"/>
        /// under <paramref name="equipment"/>'s texts: the client has no counter text), which opens <paramref name="next"/>.
        /// <paramref name="gated"/>: the element opens the lab objective and sets the lab down (1078: the element comes after
        /// Myrquar); else both are open from the accept and the lab stands from the scene's start (1077: an owned Logos completes
        /// the element in the accept itself, where no scene sequence can be queued yet). 1077's and 1078's.
        /// </summary>
        internal static void Lab(MarshesMissionDataV1 mission, uint missionId, uint element, uint equipment, uint tally, uint next,
            uint logos, uint nameText, uint bodyText, uint tallyOrdinal, bool gated)
        {
            mission.Objective(tally, nameText, bodyText, tallyOrdinal, MissionObjectiveState.Incomplete, required: false);   // hidden
            if (gated)
                mission.Scenario(LabReady, "The element taken: the control room's equipment and crate", 0);
            mission.Scenario(LabResults, "Three pieces of equipment destroyed, the sample taken", (uint)PiecesAt.Length + 1);

            mission.Transition(element);
            mission.Progress(element, MissionProgressEventKind.LogosAcquired, logos, null);
            if (gated)
            {
                MarshesTasteOfTheirOwnMedicineV1.Open(mission, element, 1, equipment);
                mission.Action(element, 3, MissionActionKind.StartScenario, scenario: LabReady);
            }

            mission.Transition(tally);
            for (var result = 1U; result <= PiecesAt.Length + 1; result++)
                mission.Progress(tally, MissionProgressEventKind.ScenarioEvent, result, (uint)PiecesAt.Length + 1,
                    counter: LabResults, trigger: result);
            mission.Transition(equipment);
            mission.ObjectiveState(equipment, tally, MissionObjectiveState.Completed);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, equipment, 1, next);
        }

        /// <summary>
        /// The lab's scene: the three pieces and the crate counted by the hidden <paramref name="tally"/> (set down by LabReady if
        /// <paramref name="gated"/>, else at the start), the sample's item; the Logos objective <paramref name="element"/> an
        /// existing fact.
        /// </summary>
        internal static MissionSceneDefinition LabScene(uint missionId, uint element, uint tally, ScenePosition[] pieces, ScenePosition crate,
            bool gated)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Ruins,
                MapContextId = Ruins,
                ExistingFactObjectiveIds = new() { element },
                HiddenObjectiveIds = new() { tally },
                Items = new()
                {
                    new MissionItemBinding(SampleKey, SampleTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            var lab = new SceneSequenceDefinition();
            for (var index = 0; index < pieces.Length; index++)
            {
                var role = PieceRole(index);
                var sequence = PieceSequence + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, EquipmentClass, pieces[index],
                    Orientation: index * 1.5708, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(missionId, tally, sequence, EquipmentHitPoints, DestroyedState));
                lab.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState) },
                    Signals = { new SceneMissionSignal(missionId, LabResults, (uint)index + 1) }
                };
            }
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, crate,
                Orientation: -1.5708, UseAction: new SceneObjectAction(missionId, tally, SampleSequence, CrateArgument));
            lab.World.Add(new EnsureActorIntent("create-crate", CrateRole));
            if (gated)
            {
                scene.Sequences[0] = new SceneSequenceDefinition();
                scene.Sequences[LabReady] = lab;
            }
            else
                scene.Sequences[0] = lab;
            scene.Sequences[SampleSequence] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("crate-done", CrateRole, false) },
                Character = { new IssueMissionItemIntent("take-sample", missionId, SampleKey, SampleTemplate, 1) },
                Signals = { new SceneMissionSignal(missionId, LabResults, (uint)pieces.Length + 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
