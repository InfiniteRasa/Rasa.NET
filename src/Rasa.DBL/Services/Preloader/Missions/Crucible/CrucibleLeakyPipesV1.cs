using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Leaky Pipes (native mission 1398), the Incurables Ward (squad instance 2141): Engineer Dhimand gets his tool chest back
    /// from Lord Jaikev, is taken through the Maintenance Corridors behind Jaikev's Throne Room to the water processing system,
    /// kept safe there while he seals the pipes, and taken back to Supervisor Sheira.
    ///
    /// Native: texts 12047-12051 (missionconversation): log "Engineer Dhimand needs your help to repair the damaged water
    /// processing system ... Recover Dhimand's Tool Chest from Lord Jaikev and return it to Engineer Dhimand. Then escort
    /// Engineer Dhimand through the Maintenance Corridors to the water processing system and defend him while he seals the
    /// damaged water pipes. Once you are finished, lead him out of the Incurables Ward and back to Supervisor Sheira.", offer
    /// "... he stole my tools and the replacement parts from me ... I programmed the Warden Bots to protect me ...", turn-in
    /// "Grupa Donal be very thankful for your help, earther. He wanted you to have this as a token of thanks."; objectives
    /// (missionobjective, no counters) 1 "Recover Dhimand's Tool Chest" 12691/12692, 2 "Return Tools to Engineer Dhimand"
    /// 12695/12696, 3 "Escort Engineer Dhimand" 12698/12699 ("... take him into the Maintenance Corridors behind Jaikev's
    /// throne room. Find the water processing system ..."), 4 "Protect Engineer Dhimand" 12704/12705 ("... while he repairs the
    /// water processing system."), 5 "Escort Engineer Dhimand" 12706/12707 ("Take Engineer Dhimand back to Supervisor Sheira
    /// back at the entrance to the Incurables Ward."); objective conversations (1398,2) package 1700 (Dhimand) 12697 "My tools!
    /// ... You lead the way.", (1398,4) 1700 12727 "Alright! Let's get out of here.", (1398,5) package 1640 (Sheira) 12726 "So
    /// the water system is being sealed? ...". Item class 25953 Incurables_Chest_Tools "Engineer Dhimand's Tool Chest"
    /// (physicalentityclassnamelanguage; template 110915). Map labels (client mk): Maintenance Tunnels (-234, 87, 222), Water
    /// Processing System (-366, 87, 212).
    ///
    /// Sourced: TaRapedia Leaky Pipes (cache, 0.5): Engineer Dhimand, Incurables Ward, no requirement, XP, credits and items
    /// "?"/"ToDo"; kill Jaikev and Parvatee in the Throne Room; the escort through hip-deep water; at the water system defend
    /// him about 2 minutes, three waves from the hallway, about 15 s after the arrival, about 45 s later and about a minute
    /// later; then escort him out; "getting too far ahead of him will cause the mission to fail"; a death fails it. The real
    /// client walks to both places with complete navmesh paths (tester, 2026-11-08 00:35-00:55).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 40 (as Lost in the Asylum; no source). No prerequisite. Taken at Dhimand in the squad's Ward (the scene bound to
    ///    it); the receiver Sheira.
    ///  - 1 the tool chest, a personal 100% drop from Jaikev 770049 while 1 is open (as Irreconcilable Differences 1185); one
    ///    held already counts at the accept.
    ///  - Dhimand: the scene's copy of his row (OwnerOnly, the public Dhimand hidden while it lives, ReplaceWhilePresent),
    ///    unhurt by anything (Invulnerable, as 1521's Brian: no fail text in the client; TaRapedia's fail rules not built,
    ///    deviation). His row gets run 7 / walk 2 (NpcUp; the Plateau escorts'), his public pool the pose Standing, so the
    ///    public Dhimand keeps his post (as 1521's Brian).
    ///  - 2 the talk at Dhimand takes the chest (CharacterOwned) and he follows the player. 3 the player in an 8 m area at the
    ///    Water Processing System (AreaEntered, as 1521); Dhimand stops following and walks to his repair place there.
    ///  - 4: three waves of three Incurables (the Ward's Brute and Slayer rows) come from the hallway east of the water room
    ///    at 15 s, 60 s and 120 s after the arrival and go for Dhimand; hidden 6 counts their nine deaths and sets the player's
    ///    flag <see cref="DefendedFlag"/>; 4's talk at Dhimand needs the flag (ObjectiveRequirements, as Plateau Array of
    ///    Hope 935). 2's talk sets the flag back to 0, so a play after an abandon must defend him again. Wave sizes no source.
    ///  - 4's talk: Dhimand follows again; 5 the talk at Sheira (package 1640). The server navmesh has a 1 m gap at the north
    ///    airlock (upstream N13): Dhimand cannot follow past it. The talk does not need him there (deviation).
    ///  - No reward items: the client has no reward table and TaRapedia lists none ("this" in the turn-in names none). 170,000
    ///    XP and 8,000 credits as 1444 (ESTIMATE).
    ///  - Markers: Jaikev (1), Dhimand (2), the water system (3), Dhimand's repair place (4), Sheira (5), all in the Ward.
    /// </summary>
    public static class CrucibleLeakyPipesV1
    {
        public const uint MissionId = 1398;
        public const uint Level = 40;
        public const uint Dhimand = CrucibleIncurablesWardNpcsV1.Dhimand, DhimandPackage = CrucibleIncurablesWardNpcsV1.DhimandPackage;
        public const uint Sheira = CrucibleIncurablesWardNpcsV1.Sheira, SheiraPackage = CrucibleIncurablesWardNpcsV1.SheiraPackage;
        public const uint MapContextId = CrucibleIncurablesWardNpcsV1.Ward;
        public const uint RecoverChest = 1, ReturnTools = 2, Escort = 3, Protect = 4, EscortBack = 5, WavesTally = 6;
        public const uint WaterArea = 1;

        /// <summary>The player's flag the defence sets (hidden 6) and 4's talk needs; 2's talk sets it back to 0.</summary>
        public const uint DefendedFlag = 1398001;

        public const uint ChestTemplate = 110915, ChestClass = 25953;
        public const string ChestKey = "dhimands-tool-chest";
        public const int ChestChance = 100;

        /// <summary>Dhimand's speeds for the escort (as the Plateau escorts and 1521's Brian), and his public pool's pose.</summary>
        public const uint RunSpeed = 7, WalkSpeed = 2;
        public const byte StandingPose = CrucibleChaukasNpcsV1.StandingPose;

        /// <summary>Scenarios: Dhimand follows (2's talk), the defence (3), Dhimand follows out (4's talk), the waves' kills.</summary>
        public const uint FollowsScenario = 2, DefenceScenario = 3, OutScenario = 4, WavesDown = 5;
        public const uint Wave1Sequence = 31, Wave2Sequence = 32, Wave3Sequence = 33, DownFrom = 40;
        public const string DhimandRole = "dhimand", RepairRoute = "dhimand-to-the-pipes";
        public const float RouteSpeed = 3f;

        /// <summary>The waves: when (ms after the arrival at the water system; TaRapedia ~15 s, ~45 s later, ~1 min later) and where.</summary>
        public static readonly uint[] WaveMilliseconds = { 15000, 60000, 120000 };
        public const int PerWave = 3;

        /// <summary>The Water Processing System (client mk label), the area of 3; Dhimand's repair place in it.</summary>
        public static readonly (double X, double Y, double Z) WaterAt = (-366, 86.61, 212);
        public static readonly ScenePosition RepairAt = new(-366f, 86.61f, 208f);
        public const double AreaRadius = 8, NpcMarkerRadius = 8, BossMarkerRadius = 10, DefendRadius = 15;

        /// <summary>
        /// The nine wave Incurables in the hallway east of the water room (probe 2026-11-08: on the navmesh and a client floor,
        /// a complete walk to the repair place, 38 m and more from it): wave by wave, three a wave.
        /// </summary>
        public static readonly ScenePosition[] WaveAt =
        {
            new(-326f, 86.61f, 222f), new(-324f, 86.61f, 220f), new(-328f, 86.61f, 220f),
            new(-316f, 86.61f, 222f), new(-314f, 86.61f, 220f), new(-318f, 86.61f, 220f),
            new(-306f, 86.61f, 222f), new(-304f, 86.61f, 220f), new(-308f, 86.61f, 220f)
        };

        public const uint Experience = 170000;
        public const uint Credits = 8000;

        public static string WaveRole(int index) => $"incurable-{index + 1}";

        /// <summary>The kind of the wave Incurable <paramref name="index"/>: Brute, Slayer, Brute in each wave.</summary>
        public static uint WaveKind(int index) => index % PerWave == 1 ? CrucibleIncurablesWardNpcsV1.Slayer : CrucibleIncurablesWardNpcsV1.Brute;

        /// <summary>Dhimand made a walker: his row's speeds, his public pool posed Standing.</summary>
        public static IEnumerable<string> NpcUp()
        {
            yield return $"UPDATE creature SET run_speed = {RunSpeed}, walk_speed = {WalkSpeed} WHERE id = {Dhimand};";
            yield return $"INSERT INTO spawnpool_pose (id, pose) VALUES ({Dhimand}, {StandingPose});";
        }

        public static IEnumerable<string> NpcDown()
        {
            yield return $"DELETE FROM spawnpool_pose WHERE id = {Dhimand};";
            yield return $"UPDATE creature SET run_speed = 0, walk_speed = 0 WHERE id = {Dhimand};";
        }

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Leaky Pipes", 12047, Dhimand, Sheira, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Scenario(FollowsScenario, "The tools back: Dhimand follows", 0);
            mission.Scenario(DefenceScenario, "At the water system: the waves come", 0);
            mission.Scenario(OutScenario, "The pipes sealed: Dhimand follows out", 0);
            mission.Scenario(WavesDown, "The wave Incurables killed", (uint)WaveAt.Length);

            mission.Objective(RecoverChest, 12691, 12692, 0, MissionObjectiveState.Incomplete);
            mission.Objective(ReturnTools, 12695, 12696, 1, MissionObjectiveState.Inactive);
            mission.Objective(Escort, 12698, 12699, 2, MissionObjectiveState.Inactive);
            mission.Objective(Protect, 12704, 12705, 3, MissionObjectiveState.Inactive);
            mission.Objective(EscortBack, 12706, 12707, 4, MissionObjectiveState.Inactive);
            mission.Objective(WavesTally, 12704, 12705, 5, MissionObjectiveState.Incomplete, required: false);    // hidden

            // 1: the tool chest from Jaikev.
            mission.Transition(RecoverChest);
            mission.Progress(RecoverChest, MissionProgressEventKind.ItemAcquired, ChestClass, 1);
            CrucibleCalendarGirlV1.Open(mission, RecoverChest, 1, 1, ReturnTools);

            // 2: the chest to Dhimand; the defence flag back to 0; he follows.
            mission.Transition(ReturnTools);
            mission.Conversation(ReturnTools, DhimandPackage);
            mission.Action(ReturnTools, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("dhimand-takes-his-tools", MissionId, ChestKey, 1, MissionItemScope.CharacterOwned));
            CrucibleCalendarGirlV1.Open(mission, ReturnTools, 1, 2, Escort);
            mission.Action(ReturnTools, 4, MissionActionKind.SetPlayerFlag, playerFlag: DefendedFlag, playerFlagValue: 0);
            mission.Action(ReturnTools, 5, MissionActionKind.StartScenario, scenario: FollowsScenario);

            // 3: the player at the water processing system; Dhimand to his repair place, the waves on their timers.
            mission.Area(WaterArea, MapContextId, WaterAt.X, WaterAt.Y, WaterAt.Z, AreaRadius);
            mission.Transition(Escort);
            mission.EnterArea(Escort, WaterArea);
            CrucibleCalendarGirlV1.Open(mission, Escort, 1, 1, Protect);
            mission.Action(Escort, 3, MissionActionKind.StartScenario, scenario: DefenceScenario);

            // 6, hidden: the nine wave Incurables dead; the flag 4's talk needs.
            mission.Transition(WavesTally);
            for (var kill = 1U; kill <= WaveAt.Length; kill++)
                mission.Progress(WavesTally, MissionProgressEventKind.ScenarioEvent, kill, (uint)WaveAt.Length, counter: WavesDown, trigger: kill);
            mission.Action(WavesTally, 1, MissionActionKind.SetPlayerFlag, playerFlag: DefendedFlag, playerFlagValue: 1);

            // 4: the talk at Dhimand once the waves are dead; he follows out.
            mission.Transition(Protect);
            mission.Conversation(Protect, DhimandPackage);
            CrucibleCalendarGirlV1.Open(mission, Protect, 1, 1, EscortBack);
            mission.Action(Protect, 3, MissionActionKind.StartScenario, scenario: OutScenario);

            // 5: the talk at Sheira.
            mission.Transition(EscortBack);
            mission.Conversation(EscortBack, SheiraPackage);
            mission.Action(EscortBack, 1, MissionActionKind.GrantReward, reward: 1);

            var jaikev = CrucibleIncurablesWardNpcsV1.JaikevAt;
            var dhimand = CrucibleIncurablesWardNpcsV1.DhimandAt;
            var sheira = CrucibleIncurablesWardNpcsV1.SheiraAt;
            mission.Indicator(RecoverChest, 1, jaikev.X, jaikev.Y, jaikev.Z, BossMarkerRadius, MapContextId);
            mission.Indicator(ReturnTools, 2, dhimand.X, dhimand.Y, dhimand.Z, NpcMarkerRadius, MapContextId);
            mission.Indicator(Escort, 3, WaterAt.X, WaterAt.Y, WaterAt.Z, AreaRadius, MapContextId);
            mission.Indicator(Protect, 4, RepairAt.X, RepairAt.Y, RepairAt.Z, DefendRadius, MapContextId);
            mission.Indicator(EscortBack, 5, sheira.X, sheira.Y, sheira.Z, NpcMarkerRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1398",
                "Native1 'Recover Dhimand's Tool Chest' 12691/12692, 2 'Return Tools to Engineer Dhimand' 12695/12696, 3 'Escort " +
                "Engineer Dhimand' 12698/12699, 4 'Protect Engineer Dhimand' 12704/12705, 5 'Escort Engineer Dhimand' 12706/12707; no " +
                "counters. objectiveconversation 1398,2 pkg1700 12697, 1398,4 1700 12727, 1398,5 pkg1640 12726. Texts 12047-12051. " +
                "Item class25953 'Engineer Dhimand's Tool Chest' (template110915). Map labels Maintenance Tunnels -234,87,222, Water " +
                "Processing System -366,87,212.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Leaky_Pipes",
                "Engineer Dhimand, Incurables Ward, no requirement, rewards unknown (cache): kill Jaikev and Parvatee; escort through " +
                "hip-deep water; defend about 2 minutes, three waves from the hallway (~15 s, ~45 s later, ~1 min later); escort " +
                "out; too far ahead or a death fails it.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleLeakyPipesV1",
                "Bound to the squad's Ward 2141; level40, no prerequisite. 1 a personal 100% drop of the chest from Jaikev 770049; 2 " +
                "Dhimand's talk takes it, flag1398001 to 0, his copy (OwnerOnly, invulnerable, run7/walk2) follows; 3 an 8 m area " +
                "at -366,86.61,212, he walks to -366,86.61,208; nine Incurables 770047/770048 in three waves at 15/60/120 s from " +
                "the hallway at x -326..-304; hidden 6 counts them and sets the flag; 4 his talk (needs the flag), he follows; 5 " +
                "Sheira's talk. No fail path, no items; 170000XP, 8000 credits.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Dhimand's copy and the waves, in the squad's Ward.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Dhimand in the squad's Ward
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { WavesTally },
                ExistingFactObjectiveIds = new() { RecoverChest },
                ObjectiveRequirements = { [Protect] = new FlagRequirement(DefendedFlag, 1) },
                Items = new()
                {
                    new MissionItemBinding(ChestKey, ChestTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { CrucibleIncurablesWardNpcsV1.Jaikev }, RecoverChest, MapContextId, ChestChance, 1))
                },
                Routes = new(),
                DefeatSequences = new()
            };
            var dhimand = CrucibleIncurablesWardNpcsV1.DhimandAt;
            scene.Actors[DhimandRole] = new SceneActorDefinition(DhimandRole, SceneActorKind.Creature, Dhimand,
                new ScenePosition((float)dhimand.X, (float)dhimand.Y, (float)dhimand.Z), FollowOffset: new ScenePosition(0f, 0f, -2.5f),
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true }, OwnerOnly: true,
                ReplacesPublicSpawnId: Dhimand, ReplaceWhilePresent: true);
            scene.Routes[RepairRoute] = new SceneRoute(RepairRoute, new[] { new SceneWaypoint(RepairAt, 0) }, Speed: RouteSpeed);

            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("dhimand-comes", DhimandRole) } };
            scene.Sequences[FollowsScenario] = new SceneSequenceDefinition
            {
                World = { new FollowActorIntent("dhimand-follows", DhimandRole, 0, Enabled: true) }
            };
            scene.Sequences[DefenceScenario] = new SceneSequenceDefinition
            {
                World =
                {
                    new FollowActorIntent("dhimand-stops", DhimandRole, 0, Enabled: false),
                    new RunRouteIntent("dhimand-to-the-pipes", DhimandRole, RepairRoute)
                },
                Timers =
                {
                    new SequenceTimer("wave-1", WaveMilliseconds[0], Wave1Sequence),
                    new SequenceTimer("wave-2", WaveMilliseconds[1], Wave2Sequence),
                    new SequenceTimer("wave-3", WaveMilliseconds[2], Wave3Sequence)
                }
            };
            scene.Sequences[OutScenario] = new SceneSequenceDefinition
            {
                World = { new FollowActorIntent("dhimand-follows-out", DhimandRole, 0, Enabled: true) }
            };

            var waves = new[] { Wave1Sequence, Wave2Sequence, Wave3Sequence };
            for (var wave = 0; wave < waves.Length; wave++)
            {
                var come = new SceneSequenceDefinition();
                for (var slot = 0; slot < PerWave; slot++)
                {
                    var index = wave * PerWave + slot;
                    var role = WaveRole(index);
                    var down = DownFrom + 1 + (uint)index;
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, WaveKind(index), WaveAt[index],
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    come.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    come.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role, DhimandRole));
                    scene.DefeatSequences[role] = down;
                    scene.Sequences[down] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, WavesDown, (uint)index + 1) } };
                }
                scene.Sequences[waves[wave]] = come;
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
