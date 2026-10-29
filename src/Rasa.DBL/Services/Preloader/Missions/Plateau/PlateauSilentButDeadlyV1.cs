using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Silent But Deadly (native mission 1187), Ustor Yard (squad instance 1502): Field Lieutenant Rapp sends the
    /// player into the southern security station to upload a virus that brings down the yard's force fields.
    ///
    /// Native: texts 9127-9131 (missionconversation): "infiltrate the southern security station and upload a
    /// virus into their systems ... you'll have to make sure not to get spotted", turn-in "the force fields are
    /// all down"; objectives (missionobjective) 1 "Avoid Detection While Uploading Virus to Southern Security
    /// Station." 9224/9225, 3 "Virus Uploaded - Bots Under AFS Control. Clear Out Bane Security Patrol."
    /// 9228/9229, 4 "Interface With Bane Computer and Bring Down Ustor Force Fields." 9230/9231, 5 "Presence
    /// Detected! ... Kill the Security Detail" 9226/9227, 6 "Blow Generator to Open Door to the Security Station"
    /// 9232/9233, 7 "Hidden - Activate forcefield keyboard" 17341/17342; no counters, no objective conversation.
    /// Items: AFS Charge (class 24894, template 50351, MisPlateauUstorItemGeneratorBomb), Bane Computer Virus
    /// (24908, 50355). The client's map: the station is Security Station Bravo (map_region 1), down the
    /// service entrance (232, 164, -124), its consoles at (150-156, 157.6, -190 to -195), a wrecked Bane
    /// generator by the entrance (216.7, 174.5, -111).
    ///
    /// Sourced: TaRapedia Silent But Deadly: giver and receiver Field Lieutenant Rapp, Ustor Yard; no
    /// requirement; follow-up Interception!; 40,000 XP; one of a Vitalius Electric Injector Gun, an Animatics
    /// Pulse Pistol, a Vextronics Incendiary Net Gun, a Teleract Sonic Cannon; avoiding detection by the
    /// Spy's Polymorph. TR MMO DB: level 32, 5,000 credits (TaRapedia: 500). A patch note: "bots were not
    /// turning evil when you failed this mission".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22, detection and fields 2026-10-26):
    ///  - Detection (MissionProgressEventKind.PlayerDetected): while 1 is open (the generator down, the virus not
    ///    up), a creature of one of <see cref="GuardPools"/> - the yard's Thrax pools nearest the service entrance
    ///    and the station - turning on the player fails 1 (optional, so the mission goes on) and opens 5: a
    ///    security detail of four comes up the station's tunnel (the patrol's places), its kills counted by a
    ///    hidden tally (<see cref="DetailTally"/>) complete 5, and the Bane computer comes as after 3. TaRapedia:
    ///    "If the second objective is failed, i.e. you are detected: Presence Detected! ... Interface with Bane
    ///    Computer". Killing the Thrax ahead, before the generator, avoids it (TaRapedia's walkthrough).
    ///    Required: 6 and 4 only, so either way ends. 5,000 credits and level 32 from TR MMO DB.
    ///  - 6: the generator, a personal UsableInertDestBaneOutpostGenerator (3857) beside the wreck at the entrance,
    ///    with an Explosive Charge (UsableBombHumV01 7870, as One Way Trip's) beside it: using the charge blows the
    ///    generator. Not shot: the client's class data has 3857 not targetable (target_flag 0), so the real client
    ///    cannot aim at it (to-fix #30; the user's first choice was "shot, not bombed"). The AFS Charge item is not
    ///    given (the use is the detonation). No door: the station's way in is open (its navmesh is not joined to
    ///    the yard's: playtest).
    ///  - 1: the virus at the station's console, a personal UsableTwoStateBaneConsoleSwitchV01 (7468), set
    ///    up once the generator is down; the virus item is not given (the use is the upload).
    ///  - 3: four of the yard's security (PlateauUstorYardV1.Security) come up the station's tunnel and attack;
    ///    the four kills, counted by a hidden tally (objective <see cref="Tally"/>: 3 has no client counter
    ///    text), complete it.
    ///  - 4: the Bane computer, a personal UsableStatelessBaneConsoleKeyboard (20631) by the prison
    ///    junction, set up once the patrol (or the detail) is down. Its use is hidden 7 "Activate forcefield
    ///    keyboard", and 7 done completes 4, whose transition takes the yard's force fields down
    ///    (PlateauSilentButDeadlyFieldsV1, TakeDownField).
    ///  - Indicators (one for each objective with a place; hidden 7, 8 and 9 have none): 5 and 3 at the station's
    ///    tunnel (the patrol and detail are there), 6 at the generator, 1 at the station's console, 4 at the Bane
    ///    computer.
    ///  - The weapons as the Uncommon base templates of the bands that hold level 32 ("Pulse" as EMP).
    /// </summary>
    public static class PlateauSilentButDeadlyV1
    {
        public const uint MissionId = 1187;
        public const uint Level = 32;
        public const uint Rapp = PlateauUstorYardV1.Rapp;
        public const uint Upload = 1, Patrol = 3, Computer = 4, Detected = 5, Generator = 6, Keyboard = 7, Tally = 8, DetailTally = 9;
        public const uint Results = 1, PatrolDown = 2, DetailDown = 3, ComputerAppears = 5, DetailComes = 7;
        public const uint GeneratorDown = 1, Uploaded = 2, ComputerUsed = 6;

        /// <summary>
        /// The station's guards (ESTIMATE): the Ustor Thrax pools nearest the south service entrance and Security Station
        /// Bravo (WorldSpawnpoolPreloader, BaneSquadSpawnpoolPreloader; nothing spawns in the station itself): 531651 (287,
        /// -70), 78 m from the generator; 560022 (160, -44), a Bane squad, 99 m; 531658 (50, -156), 132 m from the station.
        /// One detection transition of objective 1 each (several in one transition are rejected).
        /// </summary>
        public static readonly uint[] GuardPools = { 531651, 560022, 531658 };

        /// <summary>The station's tunnel, where the patrol and the detail come: objective 5's indicator.</summary>
        public static readonly (double X, double Y, double Z) TunnelAt = (190, 157.8, -192);
        public const double TunnelRadius = 30;

        /// <summary>The radius of the generator's, the console's and the computer's indicators.</summary>
        public const double ObjectRadius = 8;

        public const uint GeneratorClass = 3857;        // UsableInertDestBaneOutpostGenerator: not targetable in the client
        public const uint IntactState = 110, DestroyedState = 2;
        /// <summary>A destruction it never reaches (not targetable), so that the charge's blow shows as a destruction
        /// (DestroyableStates): the client ignores a 110 -> 2 use transition (PlateauInterceptionV1.PumpHitPoints).</summary>
        public const uint GeneratorHitPoints = 100;
        public const uint ChargeClass = 7870;   // UsableBombHumV01 "Explosive Charge" (drawn while usable)
        public const uint ConsoleClass = 7468;          // UsableTwoStateBaneConsoleSwitchV01
        public const uint ConsoleReadyState = 55, ConsoleSetState = 56;
        public const uint ComputerClass = 20631;        // UsableStatelessBaneConsoleKeyboard
        public const uint ComputerState = 44, UseArgument = 1;
        public const string GeneratorRole = "generator", ConsoleRole = "station-console", ComputerRole = "bane-computer",
            ChargeRole = "generator-charge";

        public const uint Experience = 40000;
        public const uint Credits = 5000;
        public const uint ElectricInjectorGun = 15582;  // Weapon_Avatar_InjectionGun_Electric_UNC_32_to_36
        public const uint PulsePistol = 17055;          // Weapon_Avatar_Pistol_EMP_UNC_30_to_34
        public const uint IncendiaryNetGun = 48360;     // Weapon_Avatar_NetGun_Fire_UNC_31_to_35
        public const uint SonicCannon = 49164;          // Weapon_Avatar_RocketLauncher_Sonic_UNC_29_to_33

        public static readonly ScenePosition GeneratorAt = new(225.8f, 179.09f, -117.6f);

        /// <summary>The generator's Explosive Charge, 5 m west of it, towards the entrance (clear of its collision: to-fix #31's lesson), on the navmesh's ground there.</summary>
        public static readonly ScenePosition ChargeAt = new(220.8f, 178.51f, -117.6f);
        public static readonly ScenePosition ConsoleAt = new(158, 157.8f, -192);
        public static readonly ScenePosition ComputerAt = new(228, 157.8f, -196);

        /// <summary>The station patrol, in the tunnel between the room and the consoles.</summary>
        public static readonly ScenePosition[] PatrolAt =
        {
            new(168, 157.8f, -196),
            new(176, 157.32f, -180),
            new(192, 157.8f, -196),
            new(216, 157.8f, -196),
        };

        public static string PatrolRole(int index) => $"station-patrol-{index + 1}";
        public static string DetailRole(int index) => $"security-detail-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Silent But Deadly", 9127, Rapp, Rapp, Level);
            mission.Reward(Experience, Credits, new[] { ElectricInjectorGun, PulsePistol, IncendiaryNetGun, SonicCannon });

            // Required: 6 and 4, the ends of both ways; 1 fails when the player is detected, and the mission goes on by 5.
            mission.Objective(Generator, 9232, 9233, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Upload, 9224, 9225, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Detected, 9226, 9227, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Patrol, 9228, 9229, 3, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Computer, 9230, 9231, 4, MissionObjectiveState.Inactive);
            mission.Objective(Keyboard, 17341, 17342, 5, MissionObjectiveState.Incomplete, required: false);   // hidden
            mission.Objective(Tally, 9228, 9229, 6, MissionObjectiveState.Incomplete, required: false);   // counts only the patrol
            mission.Objective(DetailTally, 9226, 9227, 7, MissionObjectiveState.Incomplete, required: false);   // counts only the detail
            mission.Scenario(Results, "Generator down, virus uploaded, computer used", 3);
            mission.Scenario(PatrolDown, "The station patrol killed", (uint)PatrolAt.Length);
            mission.Scenario(DetailDown, "The security detail killed", (uint)PatrolAt.Length);
            mission.Scenario(ComputerAppears, "The Bane computer set up", 0);
            mission.Scenario(DetailComes, "Presence detected: the security detail comes", 0);
            mission.Indicator(Detected, 1, TunnelAt.X, TunnelAt.Y, TunnelAt.Z, TunnelRadius);
            mission.Indicator(Generator, 2, GeneratorAt.X, GeneratorAt.Y, GeneratorAt.Z, ObjectRadius);
            mission.Indicator(Upload, 3, ConsoleAt.X, ConsoleAt.Y, ConsoleAt.Z, ObjectRadius);
            mission.Indicator(Patrol, 4, TunnelAt.X, TunnelAt.Y, TunnelAt.Z, TunnelRadius);
            mission.Indicator(Computer, 5, ComputerAt.X, ComputerAt.Y, ComputerAt.Z, ObjectRadius);

            mission.Transition(Generator);
            mission.Progress(Generator, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Results);
            Open(mission, Generator, 1, Upload);

            mission.Transition(Upload);
            mission.Progress(Upload, MissionProgressEventKind.ScenarioEvent, 2, null, counter: Results);
            Open(mission, Upload, 1, Patrol);
            // Detected by a guard pool's creature while 1 is open: 1 fails, 5 opens and the detail comes.
            for (var guard = 0; guard < GuardPools.Length; guard++)
            {
                var transition = 2U + (uint)guard;
                mission.FailTransition(Upload, transition);
                mission.Progress(Upload, transition, MissionProgressEventKind.PlayerDetected, GuardPools[guard], null);
                mission.Action(Upload, 1, MissionActionKind.RevealObjective, targetObjective: Detected, transition: transition);
                mission.Action(Upload, 2, MissionActionKind.ActivateObjective, targetObjective: Detected,
                    state: MissionObjectiveState.Incomplete, transition: transition);
                mission.Action(Upload, 3, MissionActionKind.StartScenario, scenario: DetailComes, transition: transition);
            }

            // 3: the four kills, counted by the hidden tally (3 has no client counter text).
            mission.Transition(Tally);
            for (var kill = 1U; kill <= PatrolAt.Length; kill++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, kill, (uint)PatrolAt.Length, counter: PatrolDown, trigger: kill);
            mission.Transition(Patrol);
            mission.ObjectiveState(Patrol, Tally, MissionObjectiveState.Completed);
            Open(mission, Patrol, 1, Computer);
            mission.Action(Patrol, 3, MissionActionKind.StartScenario, scenario: ComputerAppears);

            // 5: the detail's four kills, counted by its hidden tally, as 3's.
            mission.Transition(DetailTally);
            for (var kill = 1U; kill <= PatrolAt.Length; kill++)
                mission.Progress(DetailTally, MissionProgressEventKind.ScenarioEvent, kill, (uint)PatrolAt.Length, counter: DetailDown, trigger: kill);
            mission.Transition(Detected);
            mission.ObjectiveState(Detected, DetailTally, MissionObjectiveState.Completed);
            Open(mission, Detected, 1, Computer);
            mission.Action(Detected, 3, MissionActionKind.StartScenario, scenario: ComputerAppears);

            // 4: the keyboard used (hidden 7) completes it; its transition takes the fields down (PlateauSilentButDeadlyFieldsV1).
            mission.Transition(Keyboard);
            mission.Progress(Keyboard, MissionProgressEventKind.ScenarioEvent, 3, null, counter: Results);
            mission.Transition(Computer);
            mission.ObjectiveState(Computer, Keyboard, MissionObjectiveState.Completed);
            mission.Action(Computer, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence", DefeatSequences = new(), HiddenObjectiveIds = new() { Keyboard, Tally, DetailTally },
                BoundMapContextId = PlateauUstorYardV1.MapContextId,   // played in the squad's Ustor Yard, wherever it is taken
                MapContextId = PlateauUstorYardV1.MapContextId
            };
            scene.Actors[GeneratorRole] = new SceneActorDefinition(GeneratorRole, SceneActorKind.Object, GeneratorClass, GeneratorAt,
                InitiallyInteractable: false, InitialObjectState: IntactState,
                Destruction: new SceneObjectDestruction(MissionId, Generator, GeneratorDown, GeneratorHitPoints, DestroyedState));
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                UseAction: new SceneObjectAction(MissionId, Generator, GeneratorDown, UseArgument));
            scene.Actors[ConsoleRole] = new SceneActorDefinition(ConsoleRole, SceneActorKind.Object, ConsoleClass, ConsoleAt,
                InitialObjectState: ConsoleReadyState, UseAction: new SceneObjectAction(MissionId, Upload, Uploaded, UseArgument));
            scene.Actors[ComputerRole] = new SceneActorDefinition(ComputerRole, SceneActorKind.Object, ComputerClass, ComputerAt,
                Orientation: 1.5708, InitialObjectState: ComputerState,
                UseAction: new SceneObjectAction(MissionId, Computer, ComputerUsed, UseArgument));

            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("create-generator", GeneratorRole), new EnsureActorIntent("create-charge", ChargeRole) }
            };
            scene.Sequences[GeneratorDown] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("charge-set", ChargeRole, false),
                    new TransitionObjectStateIntent("generator-blown", GeneratorRole, DestroyedState),
                    new EnsureActorIntent("create-console", ConsoleRole)
                },
                Signals = { new SceneMissionSignal(MissionId, Results, 1) }
            };
            var uploaded = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("virus-uploaded", ConsoleRole, ConsoleSetState),
                    new SetInteractionIntent("console-done", ConsoleRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, Results, 2) }
            };
            for (var index = 0; index < PatrolAt.Length; index++)
            {
                var role = PatrolRole(index);
                var defeated = 11U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauUstorYardV1.Security, PatrolAt[index],
                    Orientation: 1.5708, GameplayPolicy: Hostile());
                uploaded.World.Add(new EnsureActorIntent($"{role}-comes", role));
                uploaded.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                scene.DefeatSequences[role] = defeated;
                scene.Sequences[defeated] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, PatrolDown, (uint)index + 1) }
                };
            }
            scene.Sequences[Uploaded] = uploaded;
            // Detected: the console is no use now, and the station's security detail comes up the tunnel.
            var detail = new SceneSequenceDefinition { World = { new SetInteractionIntent("console-locked", ConsoleRole, false, IfPresent: true) } };
            for (var index = 0; index < PatrolAt.Length; index++)
            {
                var role = DetailRole(index);
                var defeated = 21U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauUstorYardV1.Security, PatrolAt[index],
                    Orientation: 1.5708, GameplayPolicy: Hostile());
                detail.World.Add(new EnsureActorIntent($"{role}-comes", role));
                detail.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                scene.DefeatSequences[role] = defeated;
                scene.Sequences[defeated] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, DetailDown, (uint)index + 1) }
                };
            }
            scene.Sequences[DetailComes] = detail;
            scene.Sequences[ComputerAppears] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-computer", ComputerRole) } };
            scene.Sequences[ComputerUsed] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("computer-done", ComputerRole, false) },
                Signals = { new SceneMissionSignal(MissionId, Results, 3) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1187",
                "Native1 upload 9224/9225, 3 patrol 9228/9229, 4 computer 9230/9231, 5 detected 9226/9227, 6 generator 9232/9233, 7 " +
                "hidden keyboard 17341/17342; no counters, no objective conversation. Texts 9127-9131. AFS Charge class24894/" +
                "template50351, virus 24908/50355. Station Bravo (region1), consoles (150-156,157.6,-190..-195).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Silent_But_Deadly",
                "Giver/receiver Field Lieutenant Rapp, Ustor Yard; no requirement; 40000XP, 500 credits; Vitalius Electric Injector " +
                "Gun/Animatics Pulse Pistol/Vextronics Incendiary Net Gun/Teleract Sonic Cannon. TR MMO DB level32, 5000 credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSilentButDeadlyV1",
                "Level32, 5000 credits (MMO DB). Path 6,1,3,4, or detected (a creature of pool 531651/560022/531658 fights the " +
                "player while 1 is open): 1 fails, 5 (four security, the detail), 4. Generator3857 blown by an Explosive Charge7870 beside it (3857 is not client-targetable); " +
                "console7468 (the upload, no virus item); four security650062 attack; computer20631 once they are down, its use " +
                "hidden 7, which completes 4; 4 takes down three sealed Bane minor-entrance fields at the yard's minor entrances " +
                "(client map). Weapons 15582, 17055, 48360, 49164. Places and guard pools are estimates. Indicators: 5 and 3 " +
                "at the tunnel, 6 at the generator, 1 at the console, 4 at the computer.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>The run's own hostile creature: kill credit, no ordinary loot table (as Looking For Clues' Pendrik).</summary>
        internal static ActorGameplayPolicy Hostile() => new()
        {
            RewardScenarioKills = true, TrackParticipation = true,
            Loot = new AuthoredLootProfile(System.Array.Empty<LootDrop>())
        };

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        internal static void Open(PlateauMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }
    }
}
