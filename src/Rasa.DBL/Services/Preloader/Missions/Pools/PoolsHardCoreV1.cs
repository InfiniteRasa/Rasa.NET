using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Hard Core (native mission 884), Turpis Refinery (squad instance 1429), Valverde Pools: Spymaster Terentyeva wants
    /// the three Bane hard drives of the refinery's main control room and toxin control centres.
    ///
    /// Native: texts 4629-4632, 7323 (missionconversation): log "Find and acquire the 3 hard drives and bring them back to
    /// Spymaster Terentyeva.", offer "The first is located in the main control room. The second and third are located in
    /// the toxin control centers. I'll make sure you have information in your local map", turn-in "we'll begin
    /// deconstructing this information immediately."; objectives 1 "Acquire the Hard Drives (3)" 4666/7324 counter 4667
    /// "Hard Drives Acquired", 2 "Hard Drive Alpha" 6210/7325, 3 "Hard Drive Bravo" 6211/7326, 4 "Hard Drive Charlie"
    /// 6212/7327 (missionobjective); no objective conversation. Item: 111226 MisTurpisHardDrive (class 26529, "Bane Hard
    /// Drive").
    ///
    /// Sourced: TaRapedia Hard Core: giver and receiver Spymaster Terentyeva (Turpis Refinery); no requirement; level 33;
    /// 50,000 XP, 4,950 credits; one of Teleract Sonic RPG Launcher lvl 30, Vextronics Cryogenic Leech Gun lvl 34, Animatics
    /// Cryogenic Net Gun lvl 31, ChiTech Pulse Rifle lvl 31. Ten Ton Hammer Turpis Refinery guide: "three hard drives to
    /// recover at the mission marker points of: -247, -64 (charlie); -20, 175 (alpha); and 64, -96 (bravo)"; "the
    /// non-looter did not get credit for looting the hard drives ... the hard drives respawned"; "the hard drives dont
    /// stack in your backpack". TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - One personal Bane item crate each (class 11116, as Smash and Grab's), usable once, at Ten Ton Hammer's points (x, z)
    ///    on the navmesh's floor nearest them: Alpha in the control room at the top of the horseshoe, Bravo and Charlie on
    ///    the east and west sides. Each gives one hard drive, completes its own objective and counts one for 1; any order.
    ///    Terentyeva takes the three at the turn-in. The drives do not stack (class 26529, stack size 1): three bag
    ///    slots, as Ten Ton Hammer says.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels ("Pulse" EMP, "Cryogenic" Ice), one to
    ///    choose. Markers in the instance too.
    /// </summary>
    public static class PoolsHardCoreV1
    {
        public const uint MissionId = 884;
        public const uint Level = 33;
        public const uint MapContextId = PoolsTurpisRefineryNpcsV1.MapContextId;
        public const uint Terentyeva = PoolsTurpisRefineryNpcsV1.Terentyeva;
        public const uint Acquire = 1, Alpha = 2, Bravo = 3, Charlie = 4;
        public const uint DrivesTaken = 1;

        public const uint DriveTemplate = 111226, DriveClass = 26529, Drives = 3;  // MisTurpisHardDrive
        public const string DriveKey = "turpis-hard-drive";
        public const uint CrateClass = 11116;
        public const double ObjectRadius = 8;
        public const uint UseArgument = PoolsSmashAndGrabV1.UseArgument;
        public const uint ReadyState = PoolsSmashAndGrabV1.ReadyState, TakenState = PoolsSmashAndGrabV1.TakenState;

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint SonicRpgLauncher = 49164;     // Weapon_Avatar_RocketLauncher_Sonic_UNC_29_to_33
        public const uint CryogenicLeechGun = 47768;    // Weapon_Avatar_LeechGun_Ice_UNC_34_to_38
        public const uint CryogenicNetGun = 48392;      // Weapon_Avatar_NetGun_Ice_UNC_31_to_35
        public const uint PulseRifle = 14774;           // Weapon_Avatar_Rifle_EMP_UNC_31_to_35

        /// <summary>The drives: objective and place (scene events 1-3).</summary>
        public static readonly (uint Objective, ScenePosition At)[] DrivesAt =
        {
            (Alpha, new(-24f, 107.71f, 176f)),
            (Bravo, new(64f, 107.86f, -96f)),
            (Charlie, new(-239.2f, 116.31f, -64f)),
        };

        public static string CrateRole(int index) => $"hard-drive-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Hard Core", 4629, Terentyeva, Terentyeva, Level);
            mission.Reward(Experience, Credits, new[] { SonicRpgLauncher, CryogenicLeechGun, CryogenicNetGun, PulseRifle });

            mission.Objective(Acquire, 4666, 7324, 0, MissionObjectiveState.Incomplete, counterTextId: 4667);
            mission.Objective(Alpha, 6210, 7325, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Bravo, 6211, 7326, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Charlie, 6212, 7327, 3, MissionObjectiveState.Incomplete);
            mission.Scenario(DrivesTaken, "A Bane hard drive taken", Drives);

            mission.Transition(Acquire);
            for (var id = 1U; id <= Drives; id++)
                mission.Progress(Acquire, MissionProgressEventKind.ScenarioEvent, id, Drives, counter: DrivesTaken, trigger: id);
            mission.Action(Acquire, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                Items = new()
                {
                    new MissionItemBinding(DriveKey, DriveTemplate, MissionItemScope.AssignmentIssued, Drives,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < DrivesAt.Length; index++)
            {
                var (objective, at) = DrivesAt[index];
                var sequence = (uint)index + 1;
                var role = CrateRole(index);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, sequence, null, counter: DrivesTaken);

                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, at,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-taken", MissionId, DriveKey, DriveTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, DrivesTaken, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/884",
                "Native1 'Acquire the Hard Drives (3)' 4666/7324 counter4667 'Hard Drives Acquired', 2-4 'Hard Drive Alpha/Bravo/" +
                "Charlie' 6210-6212/7325-7327; no objectiveconversation. Texts 4629-4632, 7323. Item 111226 MisTurpisHardDrive class26529.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Hard_Core",
                "Giver/receiver Spymaster Terentyeva (Turpis Refinery); no requirement; level33; 50000XP, 4950 credits; one of " +
                "Teleract Sonic RPG Launcher30, Vextronics Cryogenic Leech Gun34, Animatics Cryogenic Net Gun31, ChiTech Pulse " +
                "Rifle31. Ten Ton Hammer: drives at -247,-64 (charlie), -20,175 (alpha), 64,-96 (bravo); they do not stack.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsHardCoreV1",
                "Three personal Bane item crates 11116 at Ten Ton Hammer's points, one drive each, any order; 1 counts the three. " +
                "Items 49164, 47768, 48392, 14774, one to choose. Indicators in the instance too.", 0.4);

            // 1 at each crate, 2-4 at their own.
            for (var index = 0; index < DrivesAt.Length; index++)
            {
                var (objective, at) = DrivesAt[index];
                mission.Indicator(Acquire, 1 + (uint)index, at.X, at.Y, at.Z, ObjectRadius);
                mission.Indicator(objective, 1 + (uint)(DrivesAt.Length + index), at.X, at.Y, at.Z, ObjectRadius);
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
