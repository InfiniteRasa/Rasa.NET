using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Interception! (native mission 1189), Ustor Yard (squad instance 1502): Field Lieutenant Rapp sends the
    /// player north to Chemical Storage to take out a Bane patrol before a second one lands, gather the
    /// materials from the storage tanks and blow the fuel pumps.
    ///
    /// Native: texts 9137-9141 (missionconversation): "the Bane are landing fairly regularly, gathering
    /// materials from the tanks nearby ... another ship is here doing the same thing ... If you can intercept
    /// one of the patrols before the secondary patrol appears, I'm sure you can get a pass to access the tanks.
    /// Then, for good measure, blow it all to kingdom come!"; objectives (missionobjective) 1 "Intercept Bane
    /// Patrol." 9241/9242, 2 "Take Out Patrol Before Secondary Patrol Arrives." 9243/9244, 3 "Blow Fuel Pumps in
    /// Chemical Storage to Disrupt Supply Line." 9245/9246 counter 9249 "Fuel Pumps Destroyed", 4 "Gather
    /// Materials From Storage Tanks in Chemical Storage." 9247/9248 counter 17888 "Storage Tank Materials
    /// Gathered."; no objective conversation. Items: Fuel Pump Detonators (class 24893, template 50350),
    /// Storage Tank (24909, 50356). The client's map: Ustor Chemical Storage (map_region 6, round (289, 173,
    /// 108)) with a Bane landing pad (228, 174, 118); no tank or pump.
    ///
    /// Sourced: TaRapedia Interception!: giver and receiver Field Lieutenant Rapp; requirement Silent But
    /// Deadly; follow-up Monkey Wrench; 40,000 XP, 4,650 credits; one of 3 Class VI Basic Med Packs, 3 Class VI
    /// Fragmentation Grenades, a Class VI EMP Bomb, 3 Class VI Cryogenic Grenades; "clear this area of the one
    /// time spawning Thrax first, otherwise you can end up in quite a firefight when you're on the clock to
    /// defeat the patrol in time". TR MMO DB: level 32, 4,650 credits.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - 1: done on walking into Chemical Storage by the pad (area <see cref="Storage"/>): the first patrol,
    ///    four of the yard's security (PlateauUstorYardV1.Security), stands on the pad and attacks, and a
    ///    <see cref="SecondaryPatrolMs"/> timer starts.
    ///  - 2: done once the four are dead, in time or not (a hidden tally, objective <see cref="Tally"/>, counts
    ///    them: 2 has no client counter text): there is no mission fail.
    ///  - The second patrol (four Thrax and two Kael, level 32 at twice the hit points: PlateauUstorYardV1)
    ///    always lands when the timer ends: "that puts some extra pressure" (the user). Late, the player
    ///    fights what is left of the first and all of the second.
    ///  - No pass item: the tanks and pumps are set up once 2 is done. 4: three personal
    ///    UsableTwoStateBaneFuelDepotTankForcefieldV01 (12997), each used once (no Storage Tank item). 3: three
    ///    personal destroyable UsableInertDestBaneRawFuelPumpGroupV01 (23133, the client's Ustor-era pump
    ///    class), 100 hit points, shot, not bombed (the user; the detonators are not given).
    ///  - The rewards as the client's Class VI items, the lowest template of each (the EMP Bomb has two).
    ///    No indicators: instance.
    /// </summary>
    public static class PlateauInterceptionV1
    {
        public const uint MissionId = 1189;
        public const uint Level = 32;
        public const uint Rapp = PlateauUstorYardV1.Rapp;
        public const uint Intercept = 1, TakeOut = 2, Pumps = 3, Tanks = 4, Tally = 5;
        public const uint Storage = 1;
        public const uint FirstPatrolLands = 1, SecondPatrolLands = 2, FirstPatrolDown = 3, TanksAndPumps = 4, TanksUsed = 5, PumpsDown = 6;
        public const uint SecondaryPatrolMs = 180000;
        public const string SecondaryTimer = "secondary-patrol";

        public const uint TankClass = 12997;            // UsableTwoStateBaneFuelDepotTankForcefieldV01
        public const uint TankReadyState = 55, TankUsedState = 56, UseArgument = 1;
        public const uint PumpClass = 23133;            // UsableInertDestBaneRawFuelPumpGroupV01
        public const uint PumpHitPoints = 100, IntactState = 110, DestroyedState = 2;

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint MedPack = 45057;              // Class VI Basic Med Pack
        public const uint FragGrenade = 45445;          // Class VI Fragmentation Grenade
        public const uint EmpBomb = 111231;             // Class VI EMP Bomb
        public const uint CryoGrenade = 111047;         // Class VI Cryogenic Grenade

        public static readonly (double X, double Y, double Z) StorageAt = (250, 176, 115);
        public const double StorageRadius = 45, StorageHeight = 10;

        public static readonly ScenePosition[] FirstPatrolAt =
        {
            new(224, 177.32f, 118), new(232, 177.32f, 118), new(228, 177.32f, 114), new(228, 177.32f, 122)
        };

        public static readonly (uint Creature, ScenePosition At)[] SecondPatrol =
        {
            (PlateauUstorYardV1.PatrolThrax, new(222, 177.32f, 112)),
            (PlateauUstorYardV1.PatrolThrax, new(234, 177.32f, 112)),
            (PlateauUstorYardV1.PatrolThrax, new(222, 177.32f, 124)),
            (PlateauUstorYardV1.PatrolThrax, new(234, 177.32f, 124)),
            (PlateauUstorYardV1.PatrolKael, new(228, 177.32f, 110)),
            (PlateauUstorYardV1.PatrolKael, new(228, 177.32f, 126)),
        };

        public static readonly ScenePosition[] TanksAt = { new(302.4f, 176.6f, 116.8f), new(316, 178.1f, 128), new(270, 173.16f, 125) };
        public static readonly ScenePosition[] PumpsAt = { new(256, 177.38f, 102), new(290, 174.58f, 95), new(240, 175.96f, 130) };

        public static string FirstRole(int index) => $"patrol-1-{index + 1}";
        public static string SecondRole(int index) => $"patrol-2-{index + 1}";
        public static string TankRole(int index) => $"tank-{index + 1}";
        public static string PumpRole(int index) => $"pump-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Interception!", 9137, Rapp, Rapp, Level);
            mission.Reward(Experience, Credits, new[] { (MedPack, 3U), (FragGrenade, 3U), (EmpBomb, 1U), (CryoGrenade, 3U) },
                System.Array.Empty<(uint, uint)>());
            mission.Prerequisite(PlateauSilentButDeadlyV1.MissionId);

            mission.Objective(Intercept, 9241, 9242, 0, MissionObjectiveState.Incomplete);
            mission.Objective(TakeOut, 9243, 9244, 1, MissionObjectiveState.Inactive);
            mission.Objective(Tanks, 9247, 9248, 2, MissionObjectiveState.Inactive, counterTextId: 17888);
            mission.Objective(Pumps, 9245, 9246, 3, MissionObjectiveState.Inactive, counterTextId: 9249);
            mission.Objective(Tally, 9243, 9244, 4, MissionObjectiveState.Incomplete);   // counts only the first patrol
            mission.Scenario(FirstPatrolLands, "The first patrol on the pad, the timer on", 0);
            mission.Scenario(FirstPatrolDown, "The first patrol killed", (uint)FirstPatrolAt.Length);
            mission.Scenario(TanksAndPumps, "The tanks and pumps set up", 0);
            mission.Scenario(TanksUsed, "Storage tank materials gathered", (uint)TanksAt.Length);
            mission.Scenario(PumpsDown, "Fuel pumps destroyed", (uint)PumpsAt.Length);
            mission.Area(Storage, PlateauUstorYardV1.MapContextId, StorageAt.X, StorageAt.Y, StorageAt.Z, StorageRadius, StorageHeight);

            mission.Transition(Intercept);
            mission.EnterArea(Intercept, Storage);
            PlateauSilentButDeadlyV1.Open(mission, Intercept, 1, TakeOut);
            mission.Action(Intercept, 3, MissionActionKind.StartScenario, scenario: FirstPatrolLands);

            // 2: the four kills, counted by the hidden tally (2 has no client counter text).
            mission.Transition(Tally);
            for (var kill = 1U; kill <= FirstPatrolAt.Length; kill++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, kill, (uint)FirstPatrolAt.Length, counter: FirstPatrolDown, trigger: kill);
            mission.Transition(TakeOut);
            mission.ObjectiveState(TakeOut, Tally, MissionObjectiveState.Completed);
            PlateauSilentButDeadlyV1.Open(mission, TakeOut, 1, Tanks);
            PlateauSilentButDeadlyV1.Open(mission, TakeOut, 3, Pumps);
            mission.Action(TakeOut, 5, MissionActionKind.StartScenario, scenario: TanksAndPumps);
            mission.Action(TakeOut, 6, MissionActionKind.GrantReward, reward: 1);   // the turn-in waits for 3 and 4

            mission.Transition(Tanks);
            for (var tank = 1U; tank <= TanksAt.Length; tank++)
                mission.Progress(Tanks, MissionProgressEventKind.ScenarioEvent, tank, (uint)TanksAt.Length, counter: TanksUsed, trigger: tank);
            mission.Transition(Pumps);
            for (var pump = 1U; pump <= PumpsAt.Length; pump++)
                mission.Progress(Pumps, MissionProgressEventKind.ScenarioEvent, pump, (uint)PumpsAt.Length, counter: PumpsDown, trigger: pump);

            var scene = new MissionSceneDefinition { Script = "data.sequence", DefeatSequences = new(), HiddenObjectiveIds = new() { Tally } };
            var first = new SceneSequenceDefinition
            {
                Timers = { new SequenceTimer(SecondaryTimer, SecondaryPatrolMs, SecondPatrolLands) }
            };
            for (var index = 0; index < FirstPatrolAt.Length; index++)
            {
                var role = FirstRole(index);
                var defeated = 11U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauUstorYardV1.Security, FirstPatrolAt[index],
                    GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                first.World.Add(new EnsureActorIntent($"{role}-lands", role));
                first.World.Add(new AttackActorIntent($"{role}-attacks", role));
                scene.DefeatSequences[role] = defeated;
                scene.Sequences[defeated] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, FirstPatrolDown, (uint)index + 1) }
                };
            }
            var second = new SceneSequenceDefinition();
            for (var index = 0; index < SecondPatrol.Length; index++)
            {
                var role = SecondRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, SecondPatrol[index].Creature, SecondPatrol[index].At,
                    GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                second.World.Add(new EnsureActorIntent($"{role}-lands", role));
                second.World.Add(new AttackActorIntent($"{role}-attacks", role));
            }
            var setUp = new SceneSequenceDefinition();
            for (var index = 0; index < TanksAt.Length; index++)
            {
                var role = TankRole(index);
                var used = 21U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, TankClass, TanksAt[index],
                    Orientation: index * 2.0943951024, InitialObjectState: TankReadyState,
                    UseAction: new SceneObjectAction(MissionId, Tanks, used, UseArgument));
                setUp.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[used] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"gather-{role}", role, TankUsedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, TanksUsed, (uint)index + 1) }
                };
            }
            for (var index = 0; index < PumpsAt.Length; index++)
            {
                var role = PumpRole(index);
                var down = 31U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PumpClass, PumpsAt[index],
                    Orientation: index * 2.0943951024, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Pumps, down, PumpHitPoints, DestroyedState));
                setUp.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[down] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"blow-{role}", role, DestroyedState),
                        new SetInteractionIntent($"done-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, PumpsDown, (uint)index + 1) }
                };
            }
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[FirstPatrolLands] = first;
            scene.Sequences[SecondPatrolLands] = second;
            scene.Sequences[TanksAndPumps] = setUp;

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1189",
                "Native1 intercept 9241/9242, 2 before the secondary patrol 9243/9244, 3 pumps 9245/9246 counter9249, 4 tanks " +
                "9247/9248 counter17888; no objective conversation. Texts 9137-9141 (a pass to access the tanks). Detonators " +
                "class24893/template50350, Storage Tank 24909/50356. Chemical Storage (region6), landing pad (228,174,118).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Interception%21",
                "Giver/receiver Field Lieutenant Rapp; requirement Silent But Deadly; 40000XP, 4650 credits; 3 Class VI Basic Med " +
                "Packs/3 Class VI Fragmentation Grenades/Class VI EMP Bomb/3 Class VI Cryogenic Grenades; on the clock to defeat " +
                "the patrol in time. TR MMO DB level32, 4650 credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauInterceptionV1",
                "Level32. 1 on entering Chemical Storage: four security650062 on the pad attack, a 180s timer starts; 2 when they " +
                "die, no fail. The second patrol (4x650064, 2x650065) always lands at the timer. No pass: then three tanks12997 " +
                "used (4) and three pumps23133 shot (3), no items. Rewards 45057x3, 45445x3, 111231, 111047x3. Places, timer and " +
                "patrols are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>Every role of the second patrol (for tests).</summary>
        public static string[] SecondRoles => Enumerable.Range(0, SecondPatrol.Length).Select(SecondRole).ToArray();
    }
}
