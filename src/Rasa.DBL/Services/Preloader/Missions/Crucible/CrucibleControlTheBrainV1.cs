using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Control the Brain (native mission 1521), the Chaukas Robotics Facility (squad instance 2146): Cipher Specialist Brian
    /// goes with the player to the Chaukas Brain in the round room north of the entrance, has the room cleared, sends the
    /// player for spare parts on the lower level and is kept safe while he takes the Brain over.
    ///
    /// Native: texts 13581-13585 (missionconversation): offer "... the Chaukas Brain is located in the large circular room due
    /// North ..."; objectives (missionobjective) 3 "Escort Brian to the Chaukas Brain Room" 13648/13649 (13649 empty), 4 "Find
    /// the Spare Parts" 13650/13651, 5 "Take the Parts to Brian" 13652/13653, 6 "Keep Brian Safe" 13654/13655, 7 "Talk to
    /// Brian" 14861/14862, 8 "Clear the Brain Room of Enemies so Brian can Access the Brain" 20199/20200; no counters.
    /// Objective conversations (1521,5) package 1800 text 13720 "Quick Give me the part.", (1521,7) package 1800 text 14863.
    /// Usable name 189 "Spare Parts Container" (usablenameoverride). The client has no spare-parts item class, and no table
    /// ties the name to a class: the class 26580 UsableTreasureDispWardenbotCrateV01 (usabledata argument 1, closed 200,
    /// opened 201) is ESTIMATE, the facility's own Warden Bot crate.
    ///
    /// Sourced: TaRapedia Control the Brain: giver and reward giver Brian (Chaukas); requires Get to Chaukas; follow-up Fix
    /// the Factory; 90,000 XP, 8,200 credits; Eclipse Filament Blade, Teleract Exothermic Polarity Gun, Vextronics Pulse RPG
    /// Launcher, Vextronics Shotgun; the Control Room "north of this location", spare parts "in the lower level", "ramps to
    /// the lower level just to the south of the brain control room" (cache, 0.5); an escort since 2008-03-26 (IGN TR boards:
    /// "NPC Brian will follow the player to the Brain. A new objective was added"). TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Taken at Brian in the squad's Chaukas, the scene bound to it. Brian the scene's copy of his row (OwnerOnly, the public
    ///    Brian hidden while it lives, ReplaceWhilePresent), unhurt by anything (Invulnerable: no source says he can die, and a
    ///    death would need a fail path the client has no text for). He follows the player FollowDelayMilliseconds after the
    ///    accept (a scene timer, as Cleaning House 894).
    ///  - 3 the player at the Brain room's mouth (AreaEntered, EscortRadius; Brian follows, the area checks the player, as
    ///    Miasma Testing 1136). 8 the Brain room's three Claviger pools (531127-531129) dead at once in the squad's copy
    ///    (SpawnPoolCleared, ExistingFactObjectiveIds, as Your Part of the Bargain 1529). 3 and 8 open at the accept, in either
    ///    order.
    ///  - Hidden 9 (8's texts) an aggregate of 3 and 8; hidden 10 waits for 9, opens 7 and walks Brian (follow off, a route) to
    ///    the Brain computer, as 1529's tracker.
    ///  - 7 the talk with Brian at the Brain opens 4. 4 the personal Spare Parts Container on the lower level (-16, 1.51,
    ///    -164: ReachClearance and more outside every Claviger pool's spawn reach; the first place, -22, 1.9, -144, was 12 m
    ///    from 531120's), used once
    ///    (a scenario event; no item, the client has none) opens 5. 5 the talk with Brian ("Quick Give me the part.") opens 6
    ///    and brings three Warden Bot Clavigers (the facility's own row 531023) into the room at Brian. 6 the three dead (scene
    ///    defeats, ScenarioEvent, as A Taste of Their Own Medicine 546).
    ///  - 6 waits for hidden 11 (6's texts), which counts the three deaths: the client has no counter text for 6, and a
    ///    visible count needs one (as 546's room tally).
    ///  - Ordinals in the play order: 3, 8, 7, 4, 5, 6.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 35 (Blade Physical 31-35 47688, Polarity
    ///    Gun Fire 33-37 48676, Rocket Launcher EMP 34-38 15437, Shotgun Physical 32-36 14815), one to choose.
    ///  - Markers: the Brain room's mouth, each Claviger pool, Brian at the Brain, the container.
    /// </summary>
    public static class CrucibleControlTheBrainV1
    {
        public const uint MissionId = 1521;
        public const uint Level = 35;
        public const uint Brian = CrucibleChaukasNpcsV1.Brian, BrianPackage = CrucibleChaukasNpcsV1.BrianPackage;
        public const uint MapContextId = CrucibleChaukasNpcsV1.Chaukas;
        public const uint Escort = 3, FindParts = 4, TakeParts = 5, KeepSafe = 6, TalkToBrian = 7, ClearRoom = 8, Tracker = 9, Opened = 10, AttackersTally = 11;
        public const uint EscortArea = 1;
        public const double EscortRadius = 8, BrianRadius = 8, ContainerRadius = 3, PoolMarkerPadding = 4, DefendRadius = 15;

        /// <summary>Scenarios: Brian walks to the Brain, the parts taken, the attackers come, the attackers' kills.</summary>
        public const uint AtTheBrain = 2, PartsTaken = 3, AttackersCome = 4, AttackersDown = 5;
        public const uint FollowNowSequence = 21, PartsSequence = 30, AttackerDownFrom = 10;
        public const uint FollowDelayMilliseconds = 3000;
        public const string BrianRole = "brian", ContainerRole = "spare-parts-container", BrainRoute = "brian-to-the-brain";
        public const float RouteSpeed = 3f;

        /// <summary>The Brain room's mouth (where the corridor from the south opens into it) and Brian's place at the Brain.</summary>
        public static readonly (double X, double Y, double Z) MouthAt = (-14, 16.36, 4);
        public static readonly ScenePosition BrainAt = new(-24f, 16.04f, 38f);

        public const uint ContainerClass = 26580;       // UsableTreasureDispWardenbotCrateV01
        public const uint ContainerName = 189;          // usablenameoverride "Spare Parts Container"
        public const uint UseArgument = 1;              // usabledata lookup[26580]: [0] None
        public const uint ClosedState = 200, OpenedState = 201;
        public static readonly ScenePosition ContainerAt = new(-16f, 1.51f, -164f);

        /// <summary>The three Clavigers that come at Brian (6), each 28 m or more from him.</summary>
        public static readonly ScenePosition[] AttackersAt =
        {
            new(6f, 16.23f, 62f), new(-40f, 16.22f, 62f), new(-16f, 16.23f, 86f)
        };

        public const uint Experience = 90000;
        public const uint Credits = 8200;
        public const uint FilamentBlade = 47688;        // Weapon_Avatar_Blade_Physical_UNC_31_to_35
        public const uint ExothermicPolarityGun = 48676; // Weapon_Avatar_PolarityGun_Fire_UNC_33_to_37
        public const uint PulseRpgLauncher = 15437;     // Weapon_Avatar_RocketLauncher_EMP_UNC_34_to_38
        public const uint Shotgun = 14815;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_32_to_36

        public static string AttackerRole(int index) => $"claviger-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Control the Brain", 13581, Brian, Brian, Level);
            mission.Reward(Experience, Credits, new[] { FilamentBlade, ExothermicPolarityGun, PulseRpgLauncher, Shotgun });
            mission.Prerequisite(CrucibleGetToChaukasV1.MissionId);
            mission.Scenario(AtTheBrain, "The Brain room cleared: Brian walks to the Brain", 0);
            mission.Scenario(PartsTaken, "The spare parts taken", 1);
            mission.Scenario(AttackersCome, "The parts at Brian: the Clavigers come", 0);
            mission.Scenario(AttackersDown, "The Clavigers at Brian killed", (uint)AttackersAt.Length);

            mission.Objective(Escort, 13648, 13649, 0, MissionObjectiveState.Incomplete);
            mission.Objective(ClearRoom, 20199, 20200, 1, MissionObjectiveState.Incomplete);
            mission.Objective(TalkToBrian, 14861, 14862, 2, MissionObjectiveState.Inactive);
            mission.Objective(FindParts, 13650, 13651, 3, MissionObjectiveState.Inactive);
            mission.Objective(TakeParts, 13652, 13653, 4, MissionObjectiveState.Inactive);
            mission.Objective(KeepSafe, 13654, 13655, 5, MissionObjectiveState.Inactive);
            mission.Objective(Tracker, 20199, 20200, 6, MissionObjectiveState.Incomplete, required: false);    // hidden
            mission.Objective(Opened, 20199, 20200, 7, MissionObjectiveState.Incomplete, required: false);     // hidden
            mission.Objective(AttackersTally, 13654, 13655, 8, MissionObjectiveState.Incomplete, required: false); // hidden

            // 3: the player at the Brain room's mouth, Brian with him.
            mission.Area(EscortArea, MapContextId, MouthAt.X, MouthAt.Y, MouthAt.Z, EscortRadius);
            mission.Transition(Escort);
            mission.EnterArea(Escort, EscortArea);

            // 8: the room's three Claviger pools dead at once in the squad's copy.
            mission.Transition(ClearRoom);
            var trigger = 1U;
            foreach (var (pool, _, _) in CrucibleChaukasNpcsV1.BrainRoomPools)
                mission.Progress(ClearRoom, MissionProgressEventKind.SpawnPoolCleared, pool, null, trigger: trigger++);

            // 10, hidden: 9 (3 and 8 done) opens 7 and walks Brian to the Brain.
            mission.Transition(Opened);
            mission.ObjectiveState(Opened, Tracker, MissionObjectiveState.Completed);
            CrucibleCalendarGirlV1.Open(mission, Opened, 1, 1, TalkToBrian);
            mission.Action(Opened, 3, MissionActionKind.StartScenario, scenario: AtTheBrain);

            // 7: Brian at the Brain sends the player for the parts.
            mission.Transition(TalkToBrian);
            mission.Conversation(TalkToBrian, BrianPackage);
            CrucibleCalendarGirlV1.Open(mission, TalkToBrian, 1, 1, FindParts);

            // 4: the container on the lower level.
            mission.Transition(FindParts);
            mission.Progress(FindParts, MissionProgressEventKind.ScenarioEvent, 1, null, counter: PartsTaken);
            CrucibleCalendarGirlV1.Open(mission, FindParts, 1, 1, TakeParts);

            // 5: the parts to Brian; the Clavigers come.
            mission.Transition(TakeParts);
            mission.Conversation(TakeParts, BrianPackage);
            CrucibleCalendarGirlV1.Open(mission, TakeParts, 1, 1, KeepSafe);
            mission.Action(TakeParts, 3, MissionActionKind.StartScenario, scenario: AttackersCome);

            // 6: the three Clavigers dead (hidden 11 counts them: the client has no counter text for 6, as 546's room tally).
            mission.Transition(AttackersTally);
            for (var kill = 1U; kill <= AttackersAt.Length; kill++)
                mission.Progress(AttackersTally, MissionProgressEventKind.ScenarioEvent, kill, (uint)AttackersAt.Length, counter: AttackersDown,
                    trigger: kill);
            mission.Transition(KeepSafe);
            mission.ObjectiveState(KeepSafe, AttackersTally, MissionObjectiveState.Completed);
            mission.Action(KeepSafe, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            mission.Indicator(Escort, marker++, MouthAt.X, MouthAt.Y, MouthAt.Z, EscortRadius, MapContextId);
            foreach (var (_, at, radius) in CrucibleChaukasNpcsV1.BrainRoomPools)
                mission.Indicator(ClearRoom, marker++, at.X, at.Y, at.Z, radius + PoolMarkerPadding, MapContextId);
            mission.Indicator(TalkToBrian, marker++, BrainAt.X, BrainAt.Y, BrainAt.Z, BrianRadius, MapContextId);
            mission.Indicator(FindParts, marker++, ContainerAt.X, ContainerAt.Y, ContainerAt.Z, ContainerRadius, MapContextId);
            mission.Indicator(TakeParts, marker++, BrainAt.X, BrainAt.Y, BrainAt.Z, BrianRadius, MapContextId);
            mission.Indicator(KeepSafe, marker, BrainAt.X, BrainAt.Y, BrainAt.Z, DefendRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1521",
                "Native3 'Escort Brian to the Chaukas Brain Room' 13648/13649, 4 'Find the Spare Parts' 13650/13651, 5 'Take the " +
                "Parts to Brian' 13652/13653, 6 'Keep Brian Safe' 13654/13655, 7 'Talk to Brian' 14861/14862, 8 'Clear the Brain " +
                "Room of Enemies so Brian can Access the Brain' 20199/20200; no counters. objectiveconversation 1521,5 pkg1800 13720, " +
                "1521,7 pkg1800 14863. Texts 13581-13585. Usable class26580 name189 'Spare Parts Container'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Control_the_Brain",
                "Brian gives and rewards; requires Get to Chaukas; follow-up Fix the Factory; 90000XP, 8200 credits; Eclipse Filament " +
                "Blade, Teleract Exothermic Polarity Gun, Vextronics Pulse RPG Launcher, Vextronics Shotgun; Control Room north, " +
                "parts on the lower level; an escort since 2008-03-26 (IGN TR boards). TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleControlTheBrainV1",
                "Bound to the squad's Chaukas 2146. Brian's copy (OwnerOnly, invulnerable) follows; 3 the 8 m area at the Brain " +
                "room's mouth; 8 pools 531127-531129 cleared at once; hidden 9 aggregates 3 and 8, hidden 10 opens 7 and walks " +
                "Brian to -24,16.04,38; 4 the personal container (26580, name189, arg1, 200->201) at -16,1.51,-164; 5 brings three " +
                "Clavigers 531023 at Brian, hidden 11 counts their deaths, 6 follows it. Items 47688, 48676, 15437, 14815, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Brian's copy, the container and the attackers, on the squad's copy of Chaukas.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Brian in the squad's Chaukas
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { Tracker, Opened, AttackersTally },
                ExistingFactObjectiveIds = new() { ClearRoom },
                ObjectiveAggregations = new()
                {
                    [Tracker] = new MissionObjectiveAggregation(new[] { Escort, ClearRoom }, 2)
                },
                Routes = new(),
                DefeatSequences = new()
            };
            var brian = CrucibleChaukasNpcsV1.BrianAt;
            scene.Actors[BrianRole] = new SceneActorDefinition(BrianRole, SceneActorKind.Creature, Brian,
                new ScenePosition((float)brian.X, (float)brian.Y, (float)brian.Z), FollowOffset: new ScenePosition(0f, 0f, -2.5f),
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true }, OwnerOnly: true,
                ReplacesPublicSpawnId: CrucibleChaukasNpcsV1.Brian, ReplaceWhilePresent: true);
            scene.Actors[ContainerRole] = new SceneActorDefinition(ContainerRole, SceneActorKind.Object, ContainerClass, ContainerAt,
                InitialObjectState: ClosedState, NameOverrideId: ContainerName,
                UseAction: new SceneObjectAction(MissionId, FindParts, PartsSequence, UseArgument));
            scene.Routes[BrainRoute] = new SceneRoute(BrainRoute, new[] { new SceneWaypoint(BrainAt, 0) }, Speed: RouteSpeed);

            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("brian-comes", BrianRole), new EnsureActorIntent("container-comes", ContainerRole) },
                Timers = { new SequenceTimer("brian-sets-off", FollowDelayMilliseconds, FollowNowSequence) }
            };
            scene.Sequences[FollowNowSequence] = new SceneSequenceDefinition
            {
                World = { new FollowActorIntent("brian-follows", BrianRole, 0, Enabled: true) }
            };
            scene.Sequences[AtTheBrain] = new SceneSequenceDefinition
            {
                World =
                {
                    new FollowActorIntent("brian-stops", BrianRole, 0, Enabled: false),
                    new RunRouteIntent("brian-to-the-brain", BrianRole, BrainRoute)
                }
            };
            scene.Sequences[PartsSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("container-opened", ContainerRole, OpenedState),
                    new SetInteractionIntent("container-done", ContainerRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, PartsTaken, 1) }
            };

            var come = new SceneSequenceDefinition();
            for (var index = 0; index < AttackersAt.Length; index++)
            {
                var role = AttackerRole(index);
                var down = AttackerDownFrom + 1 + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, CrucibleChaukasNpcsV1.Claviger,
                    AttackersAt[index], GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                come.World.Add(new EnsureActorIntent($"{role}-comes", role));
                come.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role, BrianRole));
                scene.DefeatSequences[role] = down;
                scene.Sequences[down] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, AttackersDown, (uint)index + 1) } };
            }
            scene.Sequences[AttackersCome] = come;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
