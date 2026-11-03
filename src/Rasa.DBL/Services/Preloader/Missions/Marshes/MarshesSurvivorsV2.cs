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
    /// Survivors (native mission 664), the Bane Supply Depot (squad instance 1451): Headquarters picks up a broken call from
    /// the downed C6801 crew in the depot's northeast; the player finds them under attack, drives the Bane off, and brings
    /// Copilot Ortega supplies from the wreck.
    ///
    /// Native: texts 2647-2650 and 7242 (missionconversation): title "Survivors", log "Locate the downed C6801 hedgehog crew in
    /// the northeast ... Report to HQ once you've briefed with the survivors.", offer (Ortega's broken call) "Crew down ...
    /// northeast of insertion point ... they've spotted us! Hold these sons of a bitches back!", turn-in (Ortega) "This is
    /// plenty of equipment"; objectives (missionobjective) 1 "Locate the C6801 crew before they are killed" 2651/7243, 5
    /// "Eliminate the surrounding Bane attacking the Survivors Camp" 6253/7244 counter 6254 "Bane Remaining", 3 "Bring
    /// Copilot Ortega supplies from downed dropships" 2655/7245; objectiveconversation (664,5) package 542 17370 ("Did you
    /// see all of them coming our way?! Man I'm glad to see you."), (664,3) package 542 2656 and 16136 ("can you go out and
    /// find some supplies off the wreckage"). Items C6801 Supply Crate class 10433 (template 2310), C6801 Medical Crate
    /// 10436 (2311).
    ///
    /// Sourced: TaRapedia Survivors: giver Headquarters (radio), receiver C-6801 Copilot Ortega (Bane Supply Depot); 80,000
    /// XP, 6,000 credits; one of 12 Class VIII Basic Med Pack, 6 Fragmentation Grenade, 2 EMP Bomb, 9 Cryogenic Grenade. TR
    /// MMO DB: level 35. Patch notes 2008-01-17: a 10 minute timer, crates once per player. World DB: the depot's hospital
    /// 500260 "Survivor's Camp" (129.9, 125.6, 312.9). The client map: the dropship wreck (134.1, 133.3, 345.6).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's Q11 and Q32):
    ///  - Offered by radio on arrival in the squad's depot (no requirement); played there.
    ///  - 1: 10 minutes from the accept (the patch notes' timer) to walk into the camp (20 m round the hospital); else 1 and
    ///    the mission fail. The camp reached brings the attack: three waves of four of the run's own Thrax (the triage's 12)
    ///    at once, after 30 s and after 60 s, from the camp's south and west, each attacking the player. 1 opens 5 (to-fix 5:
    ///    the fight is a visible objective), which counts the 12 kills with the client's counter "Bane Remaining" (6254; the
    ///    server's counters count up, n/12, as upstream Pravus' "Bane Remaining" ambush) and opens 3: two of the player's own
    ///    crates stand at the wreck. Each crate gives its item once (a Supply and a Medical crate); hidden 4 counts the two and
    ///    completes 3. Ortega takes them at the turn-in (he is the receiver). The client's objective talks at Ortega's package
    ///    542 (664,5 "Did you see all of them coming our way?!" and 664,3 "find some supplies off the wreckage") are not used:
    ///    5 is the fight, 3 the items themselves.
    ///  - Markers: the depot's door on the Marshes and the camp (1), the camp (5), the wreck's two crates and Ortega (3).
    ///
    /// V2 (2026-11-03, to-fix 13): the mission as MarshesSurvivorsV1 wrote it, with to-fix 5 (5 the visible kill objective with counter 6254, opened by 1; hidden tally 2 dropped; Ortega's talk the turn-in). MarshesSurvivorsV1 is the mission as the live
    /// server applied it; migration 20271104001900_Marshes_playtest_fixes removes it and writes this one.
    /// </summary>
    public static class MarshesSurvivorsV2
    {
        public const uint MissionId = 664;
        public const uint Level = 35;
        public const uint Ortega = MarshesBaneSupplyDepotNpcsV1.Ortega;
        public const uint Depot = MarshesBaneSupplyDepotNpcsV1.MapContextId;
        public const uint Locate = 1, Eliminate = 5, Supplies = 3, CrateTally = 4;
        public const uint BaneRemaining = 6254;         // 5's counter text (missionobjective 664,5)
        public const uint CampArea = 1;
        public const uint DeadlineSeconds = 600;

        /// <summary>Scenarios: the attack, the crates (their sequences); the kills, the crates taken.</summary>
        public const uint AttackComes = 1, CratesReady = 2, Kills = 3, CratesTaken = 4;
        public const uint WaveSequence = 10, CrateSequence = 20, DefeatSequence = 100;

        public const uint Soldier = 531045, Grenadier = 531046;
        public static readonly (double X, double Y, double Z) CampAt = (129.9, 125.66, 312.9);
        public const double CampRadius = 20;

        /// <summary>The waves: when (ms after the camp is reached) and from where (south and west of the camp).</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (0, new[] { (Soldier, new ScenePosition(100f, 121.87f, 280f)), (Soldier, new ScenePosition(130f, 122.25f, 270f)),
                (Grenadier, new ScenePosition(90.8f, 124.24f, 309.2f)), (Grenadier, new ScenePosition(160f, 134.02f, 290f)) }),
            (30000, new[] { (Soldier, new ScenePosition(102f, 121.87f, 280f)), (Soldier, new ScenePosition(132f, 122.25f, 270f)),
                (Soldier, new ScenePosition(90.8f, 124.24f, 311.2f)), (Grenadier, new ScenePosition(160f, 134.02f, 292f)) }),
            (60000, new[] { (Soldier, new ScenePosition(98f, 121.87f, 280f)), (Grenadier, new ScenePosition(128f, 122.25f, 270f)),
                (Soldier, new ScenePosition(90.8f, 124.24f, 307.2f)), (Soldier, new ScenePosition(158f, 134.02f, 290f)) })
        };
        public const uint Attackers = 12;

        public const uint CrateClass = 11078;           // item_crate_hum_v01 (914's supply crates)
        public const uint CrateArgument = 3;
        /// <summary>The wreck's crates: role, item key and template, place.</summary>
        public static readonly (string Role, string Key, uint Template, ScenePosition At)[] Crates =
        {
            ("supply-crate", "c6801-supply-crate", 2310, new ScenePosition(128f, 128.53f, 340f)),
            ("medical-crate", "c6801-medical-crate", 2311, new ScenePosition(140f, 132.53f, 340f)),
        };

        public const uint Experience = 80000;
        public const uint Credits = 6000;
        public const uint BasicMedPack = 45063, BasicMedPacks = 12;            // Class VIII Basic Med Pack
        public const uint FragGrenade = 45447, FragGrenades = 6;               // Class VIII Fragmentation Grenade
        public const uint EmpBomb = 111233, EmpBombs = 2;                      // Class VIII EMP Bomb
        public const uint CryoGrenade = 111049, CryoGrenades = 9;              // Class VIII Cryogenic Grenade

        public static string WaveRole(int wave, int member) => $"wave-{wave + 1}-{member + 1}";
        public static string WaveTimer(int wave) => $"wave-{wave + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Survivors", 2647, null, Ortega, Level);
            mission.RadioOffer(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Depot));
            mission.Reward(Experience, Credits,
                new[] { (BasicMedPack, BasicMedPacks), (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryoGrenade, CryoGrenades) },
                new (uint, uint)[0]);
            mission.Scenario(AttackComes, "At the camp: the Bane attack", 0);
            mission.Scenario(CratesReady, "Ortega asks: the wreck's crates", 0);
            mission.Scenario(Kills, "The attacking Thrax killed", Attackers);
            mission.Scenario(CratesTaken, "A crate's supplies taken", (uint)Crates.Length);
            mission.Area(CampArea, Depot, CampAt.X, CampAt.Y, CampAt.Z, CampRadius, 12);

            mission.Objective(Locate, 2651, 7243, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Eliminate, 6253, 7244, 1, MissionObjectiveState.Inactive, counterTextId: BaneRemaining);
            mission.Objective(Supplies, 2655, 7245, 2, MissionObjectiveState.Inactive);
            mission.Objective(CrateTally, 2655, 7245, 3, MissionObjectiveState.Incomplete, required: false);    // hidden

            mission.Transition(Locate);
            mission.EnterArea(Locate, CampArea);
            mission.Action(Locate, 1, MissionActionKind.StartScenario, scenario: AttackComes);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Locate, 2, Eliminate);
            mission.FailOnDeadline(Locate, DeadlineSeconds, 2);

            // 5 is the fight itself (to-fix 5): open from the camp, it counts the 12 kills ("Bane Remaining" n/12: the
            // server's counters count up, as Pravus' "Bane Remaining" ambush) and opens 3.
            mission.Transition(Eliminate);
            for (var kill = 1U; kill <= Attackers; kill++)
                mission.Progress(Eliminate, MissionProgressEventKind.ScenarioEvent, kill, Attackers, counter: Kills, trigger: kill);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Eliminate, 1, Supplies);
            mission.Action(Eliminate, 3, MissionActionKind.StartScenario, scenario: CratesReady);

            mission.Transition(CrateTally);
            for (var crate = 1U; crate <= Crates.Length; crate++)
                mission.Progress(CrateTally, MissionProgressEventKind.ScenarioEvent, crate, (uint)Crates.Length, counter: CratesTaken, trigger: crate);
            mission.Transition(Supplies);
            mission.ObjectiveState(Supplies, CrateTally, MissionObjectiveState.Completed);
            mission.Action(Supplies, 1, MissionActionKind.GrantReward, reward: 1);

            var door = MarshesBaneSupplyDepotNpcsV1.DoorAt;
            mission.Indicator(Locate, 1, door.X, door.Y, door.Z, 10);
            mission.Indicator(Locate, 2, CampAt.X, CampAt.Y, CampAt.Z, CampRadius, Depot);
            mission.Indicator(Eliminate, 3, CampAt.X, CampAt.Y, CampAt.Z, CampRadius, Depot);
            for (var index = 0; index < Crates.Length; index++)
                mission.Indicator(Supplies, 4 + (uint)index, Crates[index].At.X, Crates[index].At.Y, Crates[index].At.Z, 8, Depot);
            var ortega = MarshesBaneSupplyDepotNpcsV1.OrtegaAt;
            mission.Indicator(Supplies, 6, ortega.X, ortega.Y, ortega.Z, 8, Depot);      // the turn-in

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/664",
                "Native1 'Locate the C6801 crew before they are killed' 2651/7243, 5 'Eliminate the surrounding Bane attacking the " +
                "Survivors Camp' 6253/7244 counter6254, 3 'Bring Copilot Ortega supplies from downed dropships' 2655/7245. " +
                "objectiveconversation 664,5 pkg542 17370, 664,3 pkg542 2656/16136. Texts 2647-2650, 7242. Name 6483 Ortega. " +
                "Items classes 10433/10436 (templates 2310, 2311).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Survivors",
                "Giver Headquarters (radio), receiver C-6801 Copilot Ortega (Bane Supply Depot); 80000XP, 6000 credits; one of 12 " +
                "Class VIII Basic Med Pack, 6 Fragmentation Grenade, 2 EMP Bomb, 9 Cryogenic Grenade. TR MMO DB level35. Patch " +
                "notes 2008-01-17: 10 minute timer, crates once per player.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesSurvivorsV2",
                "Radio offer on arrival in 1451; 1 an area r20 at the Survivor's Camp within 600 s; 3 waves of 4 Thrax at 0/30/60 s, " +
                "5 (opened by 1, counter6254) counts the 12 kills and opens 3: two personal crates (11078) at the wreck, one item " +
                "each, hidden 4 counts them. Ortega 760382 takes them. Places, counts and times are estimates.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The run's own attackers and crates, on the squad's copy of the depot.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Depot,
                MapContextId = Depot,
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { CrateTally },
                Items = new()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var attack = new SceneSequenceDefinition();
            var kill = 0U;
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                var comes = new SceneSequenceDefinition();
                for (var member = 0; member < Waves[wave].Members.Length; member++)
                {
                    var role = WaveRole(wave, member);
                    var (creature, at) = Waves[wave].Members[member];
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    comes.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    comes.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                    kill++;
                    scene.DefeatSequences[role] = DefeatSequence + kill;
                    scene.Sequences[DefeatSequence + kill] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, Kills, kill) } };
                }
                if (Waves[wave].After == 0)
                {
                    attack.World.AddRange(comes.World);
                    continue;
                }
                attack.Timers.Add(new SequenceTimer(WaveTimer(wave), Waves[wave].After, WaveSequence + (uint)wave));
                scene.Sequences[WaveSequence + (uint)wave] = comes;
            }
            scene.Sequences[AttackComes] = attack;

            var crates = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var (role, key, template, at) = Crates[index];
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, at, Orientation: index * 1.5708,
                    UseAction: new SceneObjectAction(MissionId, Supplies, CrateSequence + (uint)index, CrateArgument));
                crates.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[CrateSequence + (uint)index] = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"{role}-taken", role, false) },
                    Character = { new IssueMissionItemIntent($"{role}-supplies", MissionId, key, template, 1) },
                    Signals = { new SceneMissionSignal(MissionId, CratesTaken, (uint)index + 1) }
                };
            }
            scene.Sequences[CratesReady] = crates;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
