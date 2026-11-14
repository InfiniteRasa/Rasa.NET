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
    /// Vindication (native mission 1395), Indra Caverns (squad instance 2055): Sergeant Jonas Drof gives the player a key pass to
    /// the Brann facility's rooms: Scout Aksrah's body holds a radio, Thrax Overseer Kuutk's hand opens Scout Hansa Naimi's cell,
    /// and Naimi knows where the power supply for the Control Room's console was hidden.
    ///
    /// Native: texts 12023-12027 (missionconversation); objectives (missionobjective, no objective 1) 2 "Find Scout Aksrah"
    /// 12176/12177, 3 "Find Scout Naimi" 12178/12179, 4 "Defeat Thrax Overseer Kuutk" 12180/12181, 5 "Retrieve the Power Supply"
    /// 12191/12192, 6 "Free the Prisoner" 12241/12242 ("Use the Thrax Hand found on the Overseer's body to unlock Naimi's cell"),
    /// no counters; objective conversation (1395, 3) package 1651 (Naimi) 12190 ("I've marked the location where the power
    /// supply was hidden"). Items: class 25697 Key Pass (97548; the offer: "a key pass which you'll need to access most of the
    /// rooms"), class 25711 Radio (97551), class 25789 Thrax Hand (110824), class 25631 Power Supply (97519). Usable names 134
    /// (Aksrah's corpse), 143 (the Supply Crate). Indicator names 225 (2), 226 (3), 227 (4), 228 (5).
    ///
    /// Sourced: TaRapedia Vindication (0.5): Drof gives and rewards; requires Threshold; follow-up 1447; 110,000 XP, 8,800
    /// credits; one of Vextronics Cryogenic Blade, Virulent Disperser, Endothermic Polarity Gun, Cryogenic Leech Gun.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; taken at Drof in the squad's copy. The Key Pass at the accept (AssignmentIssued; gone at the end).
    ///  - Three personal Brann doors (10749, closed 31, open 91, argument 1) locked to the key pass, in the jambs of the three rooms
    ///    the mission goes into: dorm 152 (Aksrah), the Science Lab (Kuutk), dorm 32 (the crate). Each is usable while hidden 7
    ///    (the aggregate of 2-6) is open, its use opens it (the pass kept). The Control Room is open already (its endcap a static
    ///    breach); the cells are the Bane's.
    ///  - 2 Aksrah's body (a personal human corpse 7865, name 134) in dorm 152: its search gives the Radio (AssignmentIssued, gone at
    ///    the end; a squad mate's 2 completes without one) and completes 2 (a scene result).
    ///  - 4 Kuutk (AshenIndraFacilityNpcsV1: one, the Science Lab) killed; while hidden 8 (the hand picked up: a drop names an
    ///    item-acquisition objective) is open he drops the Thrax Hand to every member on 1395 (CharacterOwned, 100 %, kept at an
    ///    abandon as Crucible 1401's cards); a kept hand counts for nothing (6 waits for the cell).
    ///  - 6 Naimi's cell: a personal Bane cell door (UsableDoorBaneEnslaverCellV01 24910, closed 31, open 91 - ESTIMATE the
    ///    states of 10749's kind; argument 1) at the static cell B's origin and yaw, locked to the hand; its use sends the event of 6;
    ///    6 takes the hand of each member whose 6 completes (his own, as 1401's cards) and opens his cell and 3; a mate away at
    ///    the use opens his own cell later with his own hand.
    ///  - 3 the talk at Naimi (package 1651) opens 5; 5 the Supply Crate in dorm 32 (personal, UsableTreasureDispHumCrateV01 7861,
    ///    closed 200, opened 201, name 143): its search gives the Power Supply (AssignmentIssued, gone at the end; 1447's offer
    ///    asks to keep it: set 16 issues one there) and completes 5 (a scene result). The turn-in at Drof.
    ///  - Markers: 225 Aksrah, 227 Kuutk, one before the cell (6), 226 Naimi, 228 the crate; all in Indra Caverns.
    /// </summary>
    public static class AshenVindicationV1
    {
        public const uint MissionId = 1395;
        public const uint Level = 43;
        public const uint Caverns = AshenIndraFacilityNpcsV1.Caverns;
        public const uint Drof = AshenIndraFacilityNpcsV1.Drof;
        public const uint NaimiPackage = AshenIndraFacilityNpcsV1.NaimiPackage;
        public const uint Kuutk = AshenIndraFacilityNpcsV1.Kuutk, KuutkPool = AshenIndraFacilityNpcsV1.KuutkPool;
        public const uint FindAksrah = 2, FindNaimi = 3, DefeatKuutk = 4, PowerSupply = 5, FreeThePrisoner = 6, KeyPassGate = 7, HandTaken = 8;

        public const uint KeyPassTemplate = 97548, KeyPassClass = 25697;
        public const uint RadioTemplate = 97551, RadioClass = 25711;
        public const uint HandTemplate = 110824, HandClass = 25789;
        public const uint PowerSupplyTemplate = 97519, PowerSupplyClass = 25631;
        public const string KeyPassKey = "indra-key-pass", RadioKey = "aksrahs-radio", HandKey = "thrax-hand", PowerSupplyKey = "indra-power-supply";
        public const int HandChance = 100;

        public const uint CorpseClass = 7865;           // UsableTreasureDispHumCorpseMaleV02
        public const uint CorpseName = 134;             // usablenameoverride: Scout Aksrah's corpse
        public const uint CrateClass = 7861;            // UsableTreasureDispHumCrateV01
        public const uint CrateName = 143;              // usablenameoverride: the Supply Crate
        public const uint ChestClosed = 200, ChestOpened = 201;
        public const uint DoorClass = 10749;            // UsableDoorBrannCorridor04mV01
        public const uint CellClass = 24910;            // UsableDoorBaneEnslaverCellV01
        public const uint DoorClosed = 31, DoorOpen = 91;
        public const uint UseArgument = 1;              // usabledata lookup[7865], [7861], [10749], [24910]: all None

        public const string CorpseRole = "indra-aksrah", CrateRole = "indra-supply-crate", CellRole = "indra-naimi-cell";
        public const string KuutkRole = "kuutk";

        public static readonly ScenePosition AksrahAt = new(153f, 8.5f, -296f);
        public static readonly ScenePosition CrateAt = new(24f, 8.5f, -284f);
        /// <summary>Cell B's origin (10.4, 24.6, -244.2) and yaw (its static's quaternion (0, -0.921, 0, 0.389)).</summary>
        public static readonly ScenePosition CellAt = new(10.4f, 24.6f, -244.2f);
        public static readonly double CellOrientation = 2 * System.Math.Atan2(-0.921, 0.389);

        /// <summary>The key-pass doors: role, place, yaw.</summary>
        public static readonly (string Role, ScenePosition At, double Orientation)[] Doors =
        {
            ("indra-dorm-152-door", new ScenePosition(150f, 8.5f, -296f), System.Math.PI / 2),
            ("indra-science-lab-door", new ScenePosition(36f, -7.5f, -362f), System.Math.PI),
            ("indra-dorm-32-door", new ScenePosition(34f, 8.5f, -280f), -System.Math.PI / 2)
        };

        /// <summary>Scenarios: Aksrah searched (2), the cell used (6), the crate searched (5), the cell opens (6 done).</summary>
        public const uint AksrahSearched = 1, CellUsed = 2, CrateSearched = 3, CellOpens = 4;
        public const uint AksrahSequence = 11, CellSequence = 12, CrateSequence = 13, DoorSequenceFrom = 21;

        public const uint AksrahIndicator = 225, NaimiIndicator = 226, KuutkIndicator = 227, CrateIndicator = 228, CellIndicator = 1;
        public static readonly (double X, double Y, double Z) CellMarkerAt = (12.5, 24.8, -243);
        public const double CorpseMarkerRadius = 8, NpcMarkerRadius = 8, BossMarkerRadius = 10, CellMarkerRadius = 5;

        public const uint Experience = 110000;
        public const uint Credits = 8800;
        public const uint CryogenicBlade = 12101;       // Weapon_Avatar_Blade_Ice_UNC_41_to_45
        public const uint VirulentDisperser = 12077;    // Weapon_Avatar_PropellantGun_Virulent_UNC_42_to_46
        public const uint PolarityGun = 12137;          // Weapon_Avatar_PolarityGun_Ice_UNC_43_to_47
        public const uint LeechGun = 47772;             // Weapon_Avatar_LeechGun_Ice_UNC_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Vindication", 12023, Drof, Drof, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicBlade, VirulentDisperser, PolarityGun, LeechGun });
            mission.Prerequisite(AshenThresholdV1.MissionId);
            mission.Scenario(AksrahSearched, "Scout Aksrah's body searched", 1);
            mission.Scenario(CellUsed, "Naimi's cell used with the Thrax Hand", 1);
            mission.Scenario(CrateSearched, "The Supply Crate searched", 1);
            mission.Scenario(CellOpens, "Naimi's cell opens (6 done)", 0);

            mission.Objective(FindAksrah, 12176, 12177, 0, MissionObjectiveState.Incomplete);
            mission.Objective(FindNaimi, 12178, 12179, 1, MissionObjectiveState.Inactive);
            mission.Objective(DefeatKuutk, 12180, 12181, 2, MissionObjectiveState.Incomplete);
            mission.Objective(PowerSupply, 12191, 12192, 3, MissionObjectiveState.Inactive);
            mission.Objective(FreeThePrisoner, 12241, 12242, 4, MissionObjectiveState.Incomplete);
            mission.Objective(KeyPassGate, 12176, 12177, 5, MissionObjectiveState.Incomplete, required: false);    // hidden
            mission.Objective(HandTaken, 12180, 12181, 6, MissionObjectiveState.Incomplete, required: false);      // hidden

            // 8, hidden: the Thrax Hand picked up (the drop's objective: the hand drops while 8 is open).
            mission.Transition(HandTaken);
            mission.Progress(HandTaken, MissionProgressEventKind.ItemAcquired, HandClass, 1);

            // 2: Aksrah's body searched (the radio comes with the search).
            mission.Transition(FindAksrah);
            mission.Progress(FindAksrah, MissionProgressEventKind.ScenarioEvent, 1, null, counter: AksrahSearched);

            // 4: Kuutk killed.
            mission.Transition(DefeatKuutk);
            mission.Progress(DefeatKuutk, MissionProgressEventKind.CreatureKilled, Kuutk, null);

            // 6: the cell used with the hand: each member whose 6 completes spends his own hand (as Crucible 1401's cards), his
            // own cell opens and 3 opens; a mate away at the use opens his own cell later with his own hand.
            mission.Transition(FreeThePrisoner);
            mission.Progress(FreeThePrisoner, MissionProgressEventKind.ScenarioEvent, 1, null, counter: CellUsed);
            mission.Action(FreeThePrisoner, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("naimis-cell-takes-the-hand", MissionId, HandKey, 1, MissionItemScope.CharacterOwned));
            mission.Action(FreeThePrisoner, 2, MissionActionKind.StartScenario, scenario: CellOpens);
            mission.Action(FreeThePrisoner, 3, MissionActionKind.RevealObjective, targetObjective: FindNaimi);
            mission.Action(FreeThePrisoner, 4, MissionActionKind.ActivateObjective, targetObjective: FindNaimi, state: MissionObjectiveState.Incomplete);

            // 3: the talk at Naimi opens 5.
            mission.Transition(FindNaimi);
            mission.Conversation(FindNaimi, NaimiPackage);
            mission.Action(FindNaimi, 1, MissionActionKind.RevealObjective, targetObjective: PowerSupply);
            mission.Action(FindNaimi, 2, MissionActionKind.ActivateObjective, targetObjective: PowerSupply, state: MissionObjectiveState.Incomplete);

            // 5: the crate searched (the power supply comes with the search); the reward at Drof.
            mission.Transition(PowerSupply);
            mission.Progress(PowerSupply, MissionProgressEventKind.ScenarioEvent, 1, null, counter: CrateSearched);
            mission.Action(PowerSupply, 1, MissionActionKind.GrantReward, reward: 1);

            var naimi = AshenIndraFacilityNpcsV1.NaimiAt;
            var kuutk = AshenIndraFacilityNpcsV1.KuutkAt;
            mission.NamedIndicator(FindAksrah, AksrahIndicator, AksrahAt.X, AksrahAt.Y, AksrahAt.Z, CorpseMarkerRadius, Caverns);
            mission.NamedIndicator(FindNaimi, NaimiIndicator, naimi.X, naimi.Y, naimi.Z, NpcMarkerRadius, Caverns);
            mission.NamedIndicator(DefeatKuutk, KuutkIndicator, kuutk.X, kuutk.Y, kuutk.Z, BossMarkerRadius, Caverns);
            mission.NamedIndicator(PowerSupply, CrateIndicator, CrateAt.X, CrateAt.Y, CrateAt.Z, CorpseMarkerRadius, Caverns);
            mission.Indicator(FreeThePrisoner, CellIndicator, CellMarkerAt.X, CellMarkerAt.Y, CellMarkerAt.Z, CellMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1395",
                "Native2 'Find Scout Aksrah' 12176/12177, 3 'Find Scout Naimi' 12178/12179, 4 'Defeat Thrax Overseer Kuutk' " +
                "12180/12181, 5 'Retrieve the Power Supply' 12191/12192, 6 'Free the Prisoner' 12241/12242, no counters; " +
                "objectiveconversation 1395,3 pkg1651 12190. Texts 12023-12027. Items Key Pass 25697 (97548), Radio 25711 (97551), " +
                "Thrax Hand 25789 (110824), Power Supply 25631 (97519). Usable names 134, 143. Indicators 225-228. Names 9427 Drof, " +
                "9530 Naimi, 9408 Kuutk.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Vindication",
                "Drof gives and rewards; requires Threshold; follow-up 1447; 110000XP, 8800 credits; Vextronics Cryogenic Blade, " +
                "Virulent Disperser, Endothermic Polarity Gun, Cryogenic Leech Gun.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenVindicationV1",
                "Level43; bound to the squad's copy 2055. Key pass at the accept; three personal doors 10749 keyed 97548 (dorm 152, " +
                "the Science Lab, dorm 32), open while hidden 7 (the aggregate of 2-6) is. 2 Aksrah corpse 7865 (name134) in dorm " +
                "152 gives the radio; 4 Kuutk 790563 (pool 790564, the Science Lab) drops the hand (100%, while hidden 8 is open); 6 the " +
                "cell 24910 at cell B (10.4,24.6,-244.2) keyed 110824: its use completes 6, which takes each member's hand and opens his cell and 3; 3 " +
                "Naimi pkg1651 opens 5; 5 the crate 7861 (name143) in dorm 32 gives the power supply. Items 12101, 12077, 12137, " +
                "47772, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The key pass, the player's own doors, Aksrah, the cell and the crate; Kuutk named, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Caverns,    // taken at Drof in the squad's copy
                MapContextId = Caverns,
                HiddenObjectiveIds = new() { KeyPassGate, HandTaken },
                ObjectiveAggregations = new()
                {
                    [KeyPassGate] = new MissionObjectiveAggregation(new[] { FindAksrah, FindNaimi, DefeatKuutk, PowerSupply, FreeThePrisoner }, 5)
                },
                Items = new()
                {
                    Issued(KeyPassKey, KeyPassTemplate),
                    Issued(RadioKey, RadioTemplate),
                    new MissionItemBinding(HandKey, HandTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { Kuutk }, HandTaken, Caverns, HandChance, 1)),
                    Issued(PowerSupplyKey, PowerSupplyTemplate)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("drof-gives-the-key-pass", MissionId, KeyPassKey, KeyPassTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[KuutkRole] = new SceneActorDefinition(KuutkRole, SceneActorKind.PublicSpawn, KuutkPool);

            for (var index = 0; index < Doors.Length; index++)
            {
                var (role, at, yaw) = Doors[index];
                var sequence = DoorSequenceFrom + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, DoorClass, at, Orientation: yaw,
                    InitialObjectState: DoorClosed, UseAction: new SceneObjectAction(MissionId, KeyPassGate, sequence, UseArgument),
                    KeyItemTemplateId: KeyPassTemplate);
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-opens", role, DoorOpen),
                        new SetInteractionIntent($"{role}-done", role, false)
                    }
                };
            }

            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, AksrahAt,
                InitialObjectState: ChestClosed, NameOverrideId: CorpseName,
                UseAction: new SceneObjectAction(MissionId, FindAksrah, AksrahSequence, UseArgument));
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, CrateAt,
                InitialObjectState: ChestClosed, NameOverrideId: CrateName,
                UseAction: new SceneObjectAction(MissionId, PowerSupply, CrateSequence, UseArgument));
            scene.Actors[CellRole] = new SceneActorDefinition(CellRole, SceneActorKind.Object, CellClass, CellAt, Orientation: CellOrientation,
                InitialObjectState: DoorClosed, UseAction: new SceneObjectAction(MissionId, FreeThePrisoner, CellSequence, UseArgument),
                KeyItemTemplateId: HandTemplate);
            foreach (var role in new[] { CorpseRole, CrateRole, CellRole })
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));

            scene.Sequences[AksrahSequence] = Search(CorpseRole, RadioKey, RadioTemplate, AksrahSearched);
            scene.Sequences[CrateSequence] = Search(CrateRole, PowerSupplyKey, PowerSupplyTemplate, CrateSearched);
            // The cell used: the event (6 takes each member's hand and opens his cell).
            scene.Sequences[CellSequence] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, CellUsed, 1) }
            };
            scene.Sequences[CellOpens] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{CellRole}-opens", CellRole, DoorOpen),
                    new SetInteractionIntent($"{CellRole}-done", CellRole, false)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        private static MissionItemBinding Issued(string key, uint template) =>
            new(key, template, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove);

        /// <summary>A body or crate searched: opened, its item to the user, the event.</summary>
        private static SceneSequenceDefinition Search(string role, string key, uint template, uint scenario) => new()
        {
            World =
            {
                new TransitionObjectStateIntent($"{role}-searched", role, ChestOpened),
                new SetInteractionIntent($"{role}-done", role, false)
            },
            Character = { new IssueMissionItemIntent($"{role}-item", MissionId, key, template, 1) },
            Signals = { new SceneMissionSignal(MissionId, scenario, 1) }
        };
    }
}
