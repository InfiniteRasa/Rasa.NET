using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Mapping The Pools (native mission 1025), Valverde Pools: Specialist Allen forgot the GPS beacons; the player takes
    /// a pack from Spymaster Timlin's cases at the Snake Pit, plants five around the Pools, tells Timlin, and goes back to
    /// Allen.
    ///
    /// Native: texts 5675-5678 and 7568 (missionconversation): offer "I forgot to pick up the GPS beacons... Could you head
    /// over to the Snake Pit", turn-in "Thanks for all the help you gave me"; objectives 1 "Speak with Spymaster Timlin to
    /// collect a pack of GPS Beacons." 5688/7569, 2 "Pick up a pack of GPS Beacons" 5690/7570 ("located next to Spymaster
    /// Timlin"), 3-7 "Plant GPS Beacon #1".."#5" 5691-5695/7572-7576, 8 "Beacon Tracker" 5698/7577 counter 5699 "Beacons
    /// Placed", 9 "Report to Spymaster Timlin that the job is complete." 5700/7578, 11 "Beacon Tracker" 7715/7716
    /// (missionobjective). Timlin's lines on package 343 (objectiveconversation 1025): 1 "They're right next to me in those
    /// cases" (5689), 2 "Were you not listening to me?" (5696), 9 "So Specialist Allen managed to get all of those GPS
    /// becons set up all by himself?" (5703) with the choices "Praise Specialist Allen" (5704) and "Take Credit for
    /// planting the GPS Beacons." (5705), and his answers on flags 122 (7751, 5701) and 123 (5702). Classes: the pack
    /// 22987 UsableItemDispGpsbeacons "GPS Beacons" (an item dispenser, augmentation 34, use argument 3; usable name
    /// override 162 POOLS_GPS_BEACONS "GPS Beacons"); the beacon item 743, class 7829 MisPoolsItemGpsbeacon "GPS Beacon";
    /// 7847 UsableItemAccHumGpsBeacon "GPS Beacon" (an item acceptor, augmentation 43, use argument 3, a planted beacon's
    /// model 21980 in state 121, usabledata). The client's map places none of them.
    ///
    /// Sourced: TaRapedia Mapping The Pools: giver and receiver Specialist Allen; no requirement; 25,000 XP, 3,300 credits,
    /// 2 Class VI EMP Bomb, 2 Class VI Adrenaline Booster, 4 Class VI Incendiary Grenade, 4 Class VI EMP Grenade (client
    /// classes 26541, 28467, 26287, 26277); the crate at -965.3, 915.7, 428.1; beacons #1 -568.8, 900.0, 511.2, #2 -264.5,
    /// 866.7, 473.8, #3 -27.9, 818.0, 234.9, #4 -505.1, 775.5, -10.3, #5 -668.4, 858.2, 257.1. TR MMO DB (Pools quest
    /// list): level 35.
    ///
    /// Reconstructed (ESTIMATE): the pack (user, 2026-10-25: 22987, item 743,
    /// acceptor 7847) a personal crate that gives five beacons and closes; the five beacons in any order (user), each a
    /// personal acceptor that takes one, shows the planted beacon and closes, completes its objective and counts on 8; 9
    /// opens with 8. "Take Credit" gives no bonus (user): both choices complete 9 alike. Timlin's answers (flags 122/123)
    /// are not shown: the choice completes 9 at once. 11, with no text, is left out; item 744 (GPS Data) is not used. The
    /// beacon heights from the navmesh (#4 2.4 m above TaRapedia's). The reward items as the lowest template of each
    /// client class, all given.
    /// </summary>
    public static class PoolsMappingThePoolsV1
    {
        public const uint MissionId = 1025;
        public const uint Level = 35;
        public const uint Allen = PoolsSnakePitContactsV1.Allen;
        public const uint TimlinPackage = PoolsSnakePitContactsV1.TimlinPackage;
        public const uint Speak = 1, PickUp = 2, Tracker = 8, Report = 9;
        public const uint Praise = 1, TakeCredit = 2;   // Timlin's choices, and 9's transitions

        public const uint BeaconTemplate = 743, BeaconClass = 7829, Beacons = 5;
        public const string BeaconKey = "gps-beacon";
        public const uint PackClass = 22987;            // UsableItemDispGpsbeacons
        public const uint PackReadyState = 81, PackTakenState = 82;
        public const uint SpotClass = 7847;             // UsableItemAccHumGpsBeacon
        public const uint SpotReadyState = 119, SpotPlantedState = 121;
        public const uint UseArgument = 3;
        public const uint PackScenario = 1, BeaconScenario = 2;

        public static readonly ScenePosition PackAt = new(-965.3f, 915.9f, 428.1f);

        /// <summary>Beacons #1-#5 at TaRapedia's places, on the navmesh.</summary>
        public static readonly ScenePosition[] Spots =
        {
            new(-568.8f, 900.4f, 511.2f),
            new(-264.5f, 866.7f, 473.8f),
            new(-27.9f, 818.0f, 234.9f),
            new(-505.1f, 777.9f, -10.3f),
            new(-668.4f, 858.6f, 257.1f),
        };

        /// <summary>Objective of beacon #<paramref name="number"/> (1-5): 3-7.</summary>
        public static uint Plant(int number) => 2 + (uint)number;

        public const uint Experience = 25000;
        public const uint Credits = 3300;
        public const uint EmpBomb = 111231, EmpBombs = 2;                       // Class VI EMP Bomb
        public const uint AdrenalineBooster = 118813, AdrenalineBoosters = 2;   // Class VI Adrenaline Booster
        public const uint IncendiaryGrenade = 111037, IncendiaryGrenades = 4;   // Class VI Incendiary Grenade
        public const uint EmpGrenade = 111027, EmpGrenades = 4;                 // Class VI EMP Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Mapping The Pools", 5675, Allen, Allen, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (EmpBomb, EmpBombs), (AdrenalineBooster, AdrenalineBoosters), (IncendiaryGrenade, IncendiaryGrenades), (EmpGrenade, EmpGrenades)
            });

            mission.Objective(Speak, 5688, 7569, 0, MissionObjectiveState.Incomplete);
            mission.Objective(PickUp, 5690, 7570, 1, MissionObjectiveState.Inactive);
            for (var number = 1; number <= Beacons; number++)
                mission.Objective(Plant(number), 5690 + (uint)number, 7571 + (uint)number, 1 + (uint)number, MissionObjectiveState.Inactive);
            mission.Objective(Tracker, 5698, 7577, 7, MissionObjectiveState.Inactive, counterTextId: 5699);
            mission.Objective(Report, 5700, 7578, 8, MissionObjectiveState.Inactive);
            mission.Scenario(PackScenario, "The pack of GPS Beacons taken", 1);
            mission.Scenario(BeaconScenario, "A GPS Beacon planted", Beacons);

            // 1: Timlin points at his cases.
            mission.Transition(Speak);
            mission.Conversation(Speak, TimlinPackage);
            Open(mission, Speak, 1, PickUp);

            // 2: the pack; the five beacons and the tracker open with it.
            mission.Transition(PickUp);
            mission.Progress(PickUp, MissionProgressEventKind.ScenarioEvent, 1, null, counter: PackScenario);
            var action = 1U;
            for (var number = 1; number <= Beacons; number++, action += 2)
                Open(mission, PickUp, action, Plant(number));
            Open(mission, PickUp, action, Tracker);

            // 3-7: each beacon at its spot, in any order; 8 counts the five.
            for (var number = 1; number <= Beacons; number++)
            {
                mission.Transition(Plant(number));
                mission.Progress(Plant(number), MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: BeaconScenario);
            }
            mission.Transition(Tracker);
            for (var number = 1U; number <= Beacons; number++)
                mission.Progress(Tracker, MissionProgressEventKind.ScenarioEvent, number, Beacons, counter: BeaconScenario, trigger: number);
            Open(mission, Tracker, 1, Report);

            // 9: Timlin, either choice.
            mission.Transition(Report, Praise, Praise);
            mission.Conversation(Report, TimlinPackage, 1, Praise);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1, transition: Praise);
            mission.Transition(Report, TakeCredit, TakeCredit);
            mission.Conversation(Report, TimlinPackage, 1, TakeCredit);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1, transition: TakeCredit);

            var timlin = PoolsSnakePitContactsV1.TimlinAt;
            mission.Indicator(Speak, 1, timlin.X, timlin.Y, timlin.Z);
            mission.Indicator(PickUp, 2, PackAt.X, PackAt.Y, PackAt.Z);
            for (var number = 1; number <= Beacons; number++)
            {
                var spot = Spots[number - 1];
                mission.Indicator(Plant(number), 2 + (uint)number, spot.X, spot.Y, spot.Z);
                mission.Indicator(Tracker, 8 + (uint)number, spot.X, spot.Y, spot.Z);
            }
            mission.Indicator(Report, 8, timlin.X, timlin.Y, timlin.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(BeaconKey, BeaconTemplate, MissionItemScope.AssignmentIssued, Beacons,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Report, TimlinPackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Praise, [2] = TakeCredit })
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors["pack"] = new SceneActorDefinition("pack", SceneActorKind.Object, PackClass, PackAt,
                InitialObjectState: PackReadyState, UseAction: new SceneObjectAction(MissionId, PickUp, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-pack", "pack"));
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("open-pack", "pack", PackTakenState),
                    new SetInteractionIntent("close-pack", "pack", false)
                },
                Character = { new IssueMissionItemIntent("beacons-from-pack", MissionId, BeaconKey, BeaconTemplate, Beacons) },
                Signals = { new SceneMissionSignal(MissionId, PackScenario, 1) }
            };
            for (var number = 1; number <= Beacons; number++)
            {
                var role = $"beacon-{number}";
                var sequence = 1 + (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, Spots[number - 1],
                    InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, Plant(number), sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{role}", role, SpotPlantedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-planted", MissionId, BeaconKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, BeaconScenario, (uint)number) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1025",
                "Native1 5688/7569, 2 'Pick up a pack of GPS Beacons' 5690/7570, 3-7 'Plant GPS Beacon #1-#5' 5691-5695/7572-7576, " +
                "8 'Beacon Tracker' 5698/7577 counter5699, 9 'Report to Spymaster Timlin' 5700/7578, 11 7715/7716; " +
                "objectiveconversation 1025 package343: 1 5689, 2 5696, 9 5703 choices 5704/5705, flags122 7751/5701, 123 5702. " +
                "Texts 5675-5678, 7568. Pack class22987 (augmentation34, use3, override162), beacon item743/class7829, spot " +
                "class7847 (augmentation43, use3, planted model in state121). None on the map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mapping_The_Pools",
                "Giver/receiver Specialist Allen; no requirement; 25000XP, 3300 credits, 2 Class VI EMP Bomb, 2 Class VI Adrenaline " +
                "Booster, 4 Class VI Incendiary Grenade, 4 Class VI EMP Grenade; crate -965.3,915.7,428.1; beacons -568.8,900,511.2 / " +
                "-264.5,866.7,473.8 / -27.9,818,234.9 / -505.1,775.5,-10.3 / -668.4,858.2,257.1. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMappingThePoolsV1",
                "Personal pack 22987 gives five beacons 743; five personal spots 7847, any order, each takes one, completes " +
                "its objective and counts on 8; 9 opens with 8; both choices complete 9 (no bonus, user); Timlin's answers not " +
                "shown; 11 and item744 unused. Items 111231x2, 118813x2, 111037x4, 111027x4. Timlin 760202, Allen 760203 new.", 0.3);

            mission.Enable(scene);
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PoolsMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
