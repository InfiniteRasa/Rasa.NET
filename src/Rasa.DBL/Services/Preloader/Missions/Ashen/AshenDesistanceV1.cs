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
    /// Desistance (native mission 1452), Indra Caverns (squad instance 2055): Scout Hansa Naimi, at the Gotha Processing Plant's
    /// entrance, gives the player a bomb: through the Juggernaut bay, up to the landing pad, the outpost generator blown, into Bane
    /// Central Administration, Overlord Kretas killed; his hand opens his teleporter, which takes the player back to Major Diren.
    ///
    /// Native: texts 12902-12906 (missionconversation; 12904 "Take this bomb with you"); objectives (missionobjective) 4 "Find the
    /// Landing Pad" 12907/12908, 1 "Unlock Juggernaut Bay Doors" 12909/12910, 2 "Get Past the Juggernauts" 12911/12912 ("Use one of
    /// the Juggernaut maintenance consoles ... without a fight"), 3 "Bomb the Outpost Generator" 12913/12914 ("You set off the siren
    /// and alerted the Bane's dropships"), 5 "Locate Bane Central Administration" 12915/12916, 6 "Defeat Overlord Kretas"
    /// 12917/12918, 7 "Activate Kretas' Teleporter" 12919/12920 ("teleport back to Indra Watch Post"), 8 "Report to Major Diren"
    /// 17113/17114, no counters; objective conversation (1452, 8) package 1802 (Diren) 17115. Items: class 26261 Bomb (111011),
    /// class 26230 Overlord Kretas' Hand (110986). Usable names 149 Juggernaut Programming Console, 150 Juggernaut Systems Console,
    /// 151 Security Door Control Console, 154 Teleportation Console. Indicator names 209 Overlord Kretas, 210 Bane Landing Pad, 211
    /// Bane Central Administration, 212 Juggernaut Operations, 213 Door Security Console, 214 Outpost Generator, 215 Teleporter
    /// Activation Console, 260 Major Diren.
    ///
    /// Sourced: TaRapedia Desistance (0.5): Scout Hansa Naimi gives, Major Diren rewards; 120,000 XP, 9,000 credits; one of
    /// Bio-Engineered Boots, Mechanized Boots, Stealth Gloves, Graviton Gloves (the V02 armour of levels 40-44: ESTIMATE, the
    /// templates chosen by name and level band, TaRapedia gives no levels).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 44; taken at Naimi's second row (AshenGothaPlantNpcsV1) in the squad's copy, requires Subversion (the story's
    ///    order); the Bomb at the accept (AssignmentIssued, each member his own).
    ///  - The order 1, 2, 4, 3, 5, 6, 7, 8 (the plant's way: the bay, the pad, the generator on it, Central Administration).
    ///  - 1 the player's own Security Door Control Console (7468, name 151) at the bay's north-east door; 2 either of the player's
    ///    own Juggernaut consoles (7468, names 149 and 150) in Juggernaut Operations west of the bay: the client map's three
    ///    Juggernauts are statics (ArchBaneGenObjJuggernautV01) and stand down with it; no Juggernaut creature is made.
    ///  - 4 the pad's area (the upper deck west of the landing pad); 3 the player's own Outpost Generator (UsableInertDestBane
    ///    OutpostGenerator 3857, intact 110, destroyed 2; not targetable in the client) on the pad deck, blown by its Explosive
    ///    Charge (UsableBombHumV01 7870, argument 1; not locked: the class is drawn in state 0, and a lock in state 0 locks nothing,
    ///    UsableLock.IsLockedIn): the use sends 3's event (a scene result); 3 takes each member's bomb (a member whose bomb can not
    ///    be taken keeps his 3 open and his charge usable), and only then are his charge set and his generator blown. No siren and no dropship wave (the deck's own pools stand by it): docs/ashen-playtest.md.
    ///  - 5 Central Administration's area (its north hall); 6 Kretas killed (one, AshenGothaPlantNpcsV1; a named public-spawn
    ///    actor); while hidden 9 (the hand picked up) is open he drops the hand to every member on 1452 (CharacterOwned, 100 %).
    ///  - 7 the player's own Teleportation Console (7468, name 154) in Central Administration's west end, locked to the hand: its
    ///    use completes 7, 7 takes each member's hand, and the member's own teleporter (UsableTwoStateBrannTeleporter 24805, off
    ///    55, on 56) stands on beside it: its use takes him to Indra Watch Post, beside Major Diren (a move on this map).
    ///  - 8 the talk at Diren (package 1802); the turn-in there.
    ///  - Markers: 213 the door console, 212 Juggernaut Operations, 210 the pad, 214 the generator, 211 Central Administration,
    ///    209 Kretas, 215 the console, 260 Diren; all in Indra Caverns.
    /// </summary>
    public static class AshenDesistanceV1
    {
        public const uint MissionId = 1452;
        public const uint Level = 44;
        public const uint Caverns = AshenGothaPlantNpcsV1.Caverns;
        public const uint Naimi = AshenGothaPlantNpcsV1.Naimi;
        public const uint Diren = AshenIndraFacilityNpcsV1.Diren, DirenPackage = AshenIndraFacilityNpcsV1.DirenPackage;
        public const uint Kretas = AshenGothaPlantNpcsV1.Kretas, KretasPool = AshenGothaPlantNpcsV1.KretasPool;

        public const uint BayDoors = 1, Juggernauts = 2, Generator = 3, LandingPad = 4, Administration = 5, DefeatKretas = 6,
            Teleporter = 7, ReportToDiren = 8, HandTaken = 9;
        public const uint PadArea = 1, AdministrationArea = 2;

        public const uint BombTemplate = 111011, BombClass = 26261;
        public const uint HandTemplate = 110986, HandClass = 26230;
        public const string BombKey = "gotha-bomb", HandKey = "kretas-hand";
        public const int HandChance = 100;

        public const uint SwitchClass = 7468;           // UsableTwoStateBaneConsoleSwitchV01
        public const uint SwitchReady = 55, SwitchSet = 56;
        public const uint GeneratorClass = 3857;        // UsableInertDestBaneOutpostGenerator: not targetable in the client
        public const uint IntactState = 110, DestroyedState = 2;
        public const uint GeneratorHitPoints = 100;    // ESTIMATE (not targetable in the client; the charge blows it)
        public const uint ChargeClass = 7870;           // UsableBombHumV01 "Explosive Charge"
        public const uint TeleporterClass = 24805;      // UsableTwoStateBrannTeleporter
        public const uint TeleporterOff = 55, TeleporterOn = 56;
        public const uint UseArgument = 1;              // usabledata lookup[7468]: (1, 0, 0, 0, 0.0); [7870], [24805]: None
        public const uint DoorConsoleName = 151, ProgrammingName = 149, SystemsName = 150, TeleportName = 154;

        public const string DoorConsoleRole = "gotha-door-console", ProgrammingRole = "gotha-juggernaut-programming",
            SystemsRole = "gotha-juggernaut-systems", GeneratorRole = "gotha-outpost-generator", ChargeRole = "gotha-generator-charge",
            ConsoleRole = "gotha-teleport-console", TeleporterRole = "gotha-kretas-teleporter", KretasRole = "kretas";

        /// <summary>Scenarios: the door console used, a Juggernaut console used, the charge set, the console used, the teleporter on (7
        /// done), the generator blown (3 done).</summary>
        public const uint DoorUnlocked = 1, JuggernautsDown = 2, GeneratorBlown = 3, ConsoleUsed = 4, TeleporterStands = 5, GeneratorGoes = 6;
        public const uint DoorSequence = 11, ProgrammingSequence = 12, SystemsSequence = 13, ChargeSequence = 14, ConsoleSequence = 15,
            TeleportSequence = 16;

        public const uint DoorConsoleIndicator = 213, OperationsIndicator = 212, PadIndicator = 210, GeneratorIndicator = 214,
            AdministrationIndicator = 211, KretasIndicator = 209, ConsoleIndicator = 215, DirenIndicator = 260;
        public const double ObjectMarkerRadius = 6, AreaMarkerRadius = 12, BossMarkerRadius = 10, NpcMarkerRadius = 8;   // ESTIMATE

        public static readonly ScenePosition DoorConsoleAt = new(-300f, 128.01f, -224f);
        public static readonly ScenePosition ProgrammingAt = new(-395f, 128.01f, -246f);
        public static readonly ScenePosition SystemsAt = new(-398f, 128.01f, -253f);
        public static readonly (double X, double Y, double Z) OperationsMarkerAt = (-396, 128.01, -250);
        public static readonly (double X, double Y, double Z) PadAt = (-395, 176.2, -295);
        public const double PadRadius = 14;              // ESTIMATE
        public static readonly ScenePosition GeneratorAt = new(-401f, 176.31f, -301f);
        public static readonly ScenePosition ChargeAt = new(-406f, 176.31f, -303f);
        public static readonly (double X, double Y, double Z) AdministrationAt = (-364, 168.71, -398);
        /// <summary>
        /// ESTIMATE: 16 m, so that Kretas (8 m south of the middle) stands well inside: one who shoots him from the hall's edge has
        /// entered the area (5) first and his kill counts (audit N5).
        /// </summary>
        public const double AdministrationRadius = 16;
        public static readonly ScenePosition ConsoleAt = new(-392f, 169.01f, -432f);
        public static readonly ScenePosition TeleporterAt = new(-389f, 168.53f, -431f);
        /// <summary>Where the teleporter takes the player: Indra Watch Post, beside Major Diren (set 15's walk to him).</summary>
        public static readonly ScenePosition DirenArrival = new(-51f, 7.6f, -407f);
        public const double DirenArrivalYaw = 0;

        public const uint Experience = 120000;
        public const uint Credits = 9000;
        public const uint BioBoots = 18770;             // Armor_Bio_V02 Boots, levels 40-44
        public const uint MechBoots = 32697;            // Armor_Mech_V02 Boots, levels 40-44
        public const uint StealthGloves = 38952;        // Armor_Stealth_V02 Gloves, levels 40-44
        public const uint GravitonGloves = 23698;       // Armor_Graviton_V02 Gloves, levels 40-44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Desistance", 12902, Naimi, Diren, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, MechBoots, StealthGloves, GravitonGloves });
            mission.Prerequisite(AshenSubversionV1.MissionId);
            mission.Scenario(DoorUnlocked, "The bay's door console used", 1);
            mission.Scenario(JuggernautsDown, "A Juggernaut console used", 1);
            mission.Scenario(GeneratorBlown, "The generator's charge set", 1);
            mission.Scenario(ConsoleUsed, "The Teleportation Console used with the hand", 1);
            mission.Scenario(TeleporterStands, "Kretas' teleporter on (7 done)", 0);
            mission.Scenario(GeneratorGoes, "The generator blown (3 done)", 0);

            mission.Objective(BayDoors, 12909, 12910, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Juggernauts, 12911, 12912, 1, MissionObjectiveState.Inactive);
            mission.Objective(LandingPad, 12907, 12908, 2, MissionObjectiveState.Inactive);
            mission.Objective(Generator, 12913, 12914, 3, MissionObjectiveState.Inactive);
            mission.Objective(Administration, 12915, 12916, 4, MissionObjectiveState.Inactive);
            mission.Objective(DefeatKretas, 12917, 12918, 5, MissionObjectiveState.Inactive);
            mission.Objective(Teleporter, 12919, 12920, 6, MissionObjectiveState.Inactive);
            mission.Objective(ReportToDiren, 17113, 17114, 7, MissionObjectiveState.Inactive);
            mission.Objective(HandTaken, 12917, 12918, 8, MissionObjectiveState.Incomplete, required: false);     // hidden

            mission.Area(PadArea, Caverns, PadAt.X, PadAt.Y, PadAt.Z, PadRadius);
            mission.Area(AdministrationArea, Caverns, AdministrationAt.X, AdministrationAt.Y, AdministrationAt.Z, AdministrationRadius);

            // 9, hidden: the hand picked up (the drop's objective: the hand drops while 9 is open).
            mission.Transition(HandTaken);
            mission.Progress(HandTaken, MissionProgressEventKind.ItemAcquired, HandClass, 1);

            mission.Transition(BayDoors);
            mission.Progress(BayDoors, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DoorUnlocked);
            Open(mission, BayDoors, 1, Juggernauts);

            mission.Transition(Juggernauts);
            mission.Progress(Juggernauts, MissionProgressEventKind.ScenarioEvent, 1, null, counter: JuggernautsDown);
            Open(mission, Juggernauts, 1, LandingPad);

            mission.Transition(LandingPad);
            mission.EnterArea(LandingPad, PadArea);
            Open(mission, LandingPad, 1, Generator);

            // 3: the charge set: each member whose 3 completes spends his own bomb (as Crucible 1401's cards).
            mission.Transition(Generator);
            mission.Progress(Generator, MissionProgressEventKind.ScenarioEvent, 1, null, counter: GeneratorBlown);
            mission.Action(Generator, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("the-generator-takes-the-bomb", MissionId, BombKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Generator, 2, MissionActionKind.StartScenario, scenario: GeneratorGoes);
            Open(mission, Generator, 3, Administration);

            mission.Transition(Administration);
            mission.EnterArea(Administration, AdministrationArea);
            Open(mission, Administration, 1, DefeatKretas);

            mission.Transition(DefeatKretas);
            mission.Progress(DefeatKretas, MissionProgressEventKind.CreatureKilled, Kretas, null);
            Open(mission, DefeatKretas, 1, Teleporter);

            // 7: the console used with the hand: each member whose 7 completes spends his own hand, his own teleporter comes on.
            mission.Transition(Teleporter);
            mission.Progress(Teleporter, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ConsoleUsed);
            mission.Action(Teleporter, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("the-console-takes-the-hand", MissionId, HandKey, 1, MissionItemScope.CharacterOwned));
            mission.Action(Teleporter, 2, MissionActionKind.StartScenario, scenario: TeleporterStands);
            Open(mission, Teleporter, 3, ReportToDiren);

            mission.Transition(ReportToDiren);
            mission.Conversation(ReportToDiren, DirenPackage);
            mission.Action(ReportToDiren, 1, MissionActionKind.GrantReward, reward: 1);

            var kretas = AshenGothaPlantNpcsV1.KretasAt;
            var diren = AshenIndraFacilityNpcsV1.DirenAt;
            mission.NamedIndicator(BayDoors, DoorConsoleIndicator, DoorConsoleAt.X, DoorConsoleAt.Y, DoorConsoleAt.Z, ObjectMarkerRadius, Caverns);
            mission.NamedIndicator(Juggernauts, OperationsIndicator, OperationsMarkerAt.X, OperationsMarkerAt.Y, OperationsMarkerAt.Z, AreaMarkerRadius, Caverns);
            mission.NamedIndicator(LandingPad, PadIndicator, PadAt.X, PadAt.Y, PadAt.Z, PadRadius, Caverns);
            mission.NamedIndicator(Generator, GeneratorIndicator, GeneratorAt.X, GeneratorAt.Y, GeneratorAt.Z, ObjectMarkerRadius, Caverns);
            mission.NamedIndicator(Administration, AdministrationIndicator, AdministrationAt.X, AdministrationAt.Y, AdministrationAt.Z,
                AdministrationRadius, Caverns);
            mission.NamedIndicator(DefeatKretas, KretasIndicator, kretas.X, kretas.Y, kretas.Z, BossMarkerRadius, Caverns);
            mission.NamedIndicator(Teleporter, ConsoleIndicator, ConsoleAt.X, ConsoleAt.Y, ConsoleAt.Z, ObjectMarkerRadius, Caverns);
            mission.NamedIndicator(ReportToDiren, DirenIndicator, diren.X, diren.Y, diren.Z, NpcMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1452",
                "Native4 'Find the Landing Pad' 12907/12908, 1 'Unlock Juggernaut Bay Doors' 12909/12910, 2 'Get Past the " +
                "Juggernauts' 12911/12912, 3 'Bomb the Outpost Generator' 12913/12914, 5 'Locate Bane Central Administration' " +
                "12915/12916, 6 'Defeat Overlord Kretas' 12917/12918, 7 'Activate Kretas' Teleporter' 12919/12920, 8 'Report to " +
                "Major Diren' 17113/17114, no counters; objectiveconversation 1452,8 pkg1802 17115. Texts 12902-12906. Items Bomb " +
                "26261 (111011), Overlord Kretas' Hand 26230 (110986). Usable names 149-151, 154. Indicators 209-215, 260.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Desistance",
                "Scout Hansa Naimi gives, Major Diren rewards; 120000XP, 9000 credits; Bio-Engineered Boots, Mechanized Boots, " +
                "Stealth Gloves, Graviton Gloves.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDesistanceV1",
                "Level44; bound to the squad's copy; requires 1451 (estimated). Bomb at the accept. Order 1, 2, 4, 3, 5, 6, 7, 8. " +
                "Personal consoles 7468: the bay door's (name151), two Juggernaut consoles (149, 150; either completes 2; the " +
                "Juggernauts are the map's statics, none made); pad area; generator 3857 on the pad deck blown by a charge 7870 " +
                "(3 takes each member's bomb, then the generator blows; no siren wave); Central Administration area; Kretas 790602 (pool " +
                "790603) drops the hand (100%, while hidden 9 is open); the Teleportation Console (154) locked to the hand: 7 takes " +
                "each member's hand and his teleporter 24805 comes on: its use takes him beside Diren (-51,7.6,-407); 8 Diren " +
                "pkg1802. Items 18770, 32697, 38952, 23698.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The bomb, the player's own consoles, generator, charge and teleporter; Kretas named, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Caverns,    // taken at Naimi in the squad's copy
                MapContextId = Caverns,
                HiddenObjectiveIds = new() { HandTaken },
                Items = new()
                {
                    new MissionItemBinding(BombKey, BombTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(HandKey, HandTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { Kretas }, HandTaken, Caverns, HandChance, 1))
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("naimi-gives-the-bomb", MissionId, BombKey, BombTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[KretasRole] = new SceneActorDefinition(KretasRole, SceneActorKind.PublicSpawn, KretasPool);

            Switch(scene, DoorConsoleRole, DoorConsoleAt, DoorConsoleName, BayDoors, DoorSequence, DoorUnlocked, null);
            Switch(scene, ProgrammingRole, ProgrammingAt, ProgrammingName, Juggernauts, ProgrammingSequence, JuggernautsDown, SystemsRole);
            Switch(scene, SystemsRole, SystemsAt, SystemsName, Juggernauts, SystemsSequence, JuggernautsDown, ProgrammingRole);

            scene.Actors[GeneratorRole] = new SceneActorDefinition(GeneratorRole, SceneActorKind.Object, GeneratorClass, GeneratorAt,
                InitiallyInteractable: false, InitialObjectState: IntactState,
                Destruction: new SceneObjectDestruction(MissionId, Generator, ChargeSequence, GeneratorHitPoints, DestroyedState));
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                UseAction: new SceneObjectAction(MissionId, Generator, ChargeSequence, UseArgument));
            scene.Actors[ConsoleRole] = new SceneActorDefinition(ConsoleRole, SceneActorKind.Object, SwitchClass, ConsoleAt,
                InitialObjectState: SwitchReady, NameOverrideId: TeleportName,
                UseAction: new SceneObjectAction(MissionId, Teleporter, ConsoleSequence, UseArgument), KeyItemTemplateId: HandTemplate);
            scene.Actors[TeleporterRole] = new SceneActorDefinition(TeleporterRole, SceneActorKind.Object, TeleporterClass, TeleporterAt,
                InitiallyInteractable: false, InitialObjectState: TeleporterOff,
                UseAction: new SceneObjectAction(MissionId, ReportToDiren, TeleportSequence, UseArgument));
            foreach (var role in new[] { GeneratorRole, ChargeRole, ConsoleRole, TeleporterRole })
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));

            // The charge used (or the generator shot, which the client does not offer): the event of 3 only.
            scene.Sequences[ChargeSequence] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, GeneratorBlown, 1) }
            };
            // 3 done (its bomb taken first): the charge set, the generator blown. A member whose bomb can not be taken keeps his
            // own charge usable and his generator standing (the set 15 door's order: the use sends the event, the take, then this).
            scene.Sequences[GeneratorGoes] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("charge-set", ChargeRole, false),
                    new TransitionObjectStateIntent("generator-blown", GeneratorRole, DestroyedState)
                }
            };
            // The console used with the hand: the event of 7 (7 takes each member's hand, then sets his console and turns his
            // teleporter on).
            scene.Sequences[ConsoleSequence] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, ConsoleUsed, 1) }
            };
            scene.Sequences[TeleporterStands] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("teleporter-on", TeleporterRole, TeleporterOn),
                    new SetInteractionIntent("teleporter-usable", TeleporterRole, true),
                    new TransitionObjectStateIntent("console-set-too", ConsoleRole, SwitchSet),
                    new SetInteractionIntent("console-done-too", ConsoleRole, false)
                }
            };
            scene.Sequences[TeleportSequence] = new SceneSequenceDefinition
            {
                World = { new TransferIntent("kretas-teleporter-to-diren", Caverns, DirenArrival, DirenArrivalYaw) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The objective's action <paramref name="action"/> reveals and opens <paramref name="next"/>.</summary>
        private static void Open(AshenMissionDataV1 mission, uint objective, uint action, uint next)
        {
            mission.Action(objective, action, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, action + 1, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete);
        }

        /// <summary>A console of the player's own: used, it is set, used no more (its twin too), and sends its event.</summary>
        private static void Switch(MissionSceneDefinition scene, string role, ScenePosition at, uint name, uint objective, uint sequence,
            uint scenario, string twin)
        {
            scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SwitchClass, at,
                InitialObjectState: SwitchReady, NameOverrideId: name,
                UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
            var used = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{role}-set", role, SwitchSet),
                    new SetInteractionIntent($"{role}-done", role, false)
                },
                Signals = { new SceneMissionSignal(MissionId, scenario, 1) }
            };
            if (twin != null)
                used.World.Add(new SetInteractionIntent($"{twin}-closed-by-{role}", twin, false));
            scene.Sequences[sequence] = used;
        }
    }
}
