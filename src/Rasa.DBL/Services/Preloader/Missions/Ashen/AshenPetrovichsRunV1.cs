using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Petrovich's Run (native mission 1535), Avernus Outpost (squad instance 2110): Captain Petrovich has the AFS Graveyard
    /// cleared, leads his squad and the player up the tunnel, rests at the turn, holds off an Atta ambush there and goes on to the
    /// top.
    ///
    /// Native: texts 13754-13758 (missionconversation): log "Captain Petrovich wants the room cleared. Then he wants you to follow
    /// him up the Tunnel.", offer "Once we're sure this area is cleared of Atta, we're going up that tunnel. ... You stay with us,
    /// you stay alive. ... We move as soon as this room is clear!", turn-in 13757 (1533's 13699 too), 13758 "none". Objectives
    /// (missionobjective) 1 "Clear the Cave" 13759/13760, 2 "Tunnel Run" 13761/13762, 3 "Rest" 13763/13764, 4 "Fend off the
    /// Ambush!" 13766/13767, 5 "All The Way To The Top" 13768/13769; no counters, no marker names. Objective conversation 1535,3
    /// package 1803 (Petrovich) variant 1, text 13765 "We gotta wait here a minute. Let everybody catch their breath. Woo! That's a
    /// steep tunnel. This is as good a place as any to set up for the fight, too. You hear em coming? ...". Names 9672 Captain
    /// Petrovich, 9673 Avernus Outpost Soldier, 9674 Avernus Outpost Officer.
    ///
    /// Sourced: TaRapedia Petrovich's Run: giver and reward giver Captain Petrovich (Avernus Outpost); follow-up Assessing the Atta
    /// (1536); 90,000 XP, 6,150 credits; Class VIII Frag Grenade (3), EMP Bomb, Class VIII Cryogenic Grenade (3), Standard Med
    /// Pack (3), one to choose; clear the room, follow Petrovich and his squad up the tunnel, rest at the big turn, the ambush,
    /// the top, speak to him (0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (triage D5). No prerequisite (no source; TaRapedia names none). Bound to the squad's Avernus Outpost.
    ///  - Taken at Petrovich in the graveyard; the scene's copy of him (OwnerOnly, the public one hidden while it lives,
    ///    ReplaceWhilePresent, as Crucible's Control the Brain 1521) and his squad, an Avernus Outpost Officer and two Avernus
    ///    Outpost Soldiers (AshenAvernusOutpostNpcsV1, the player's own), stand by him from the accept. All four unhurt by anything
    ///    (Invulnerable: the client has no fail text, and a death would need a fail path).
    ///  - 1 the graveyard's four Atta pools (531049, 531050, 531060, 531063; upstream) dead at once in the squad's copy
    ///    (SpawnPoolCleared, ExistingFactObjectiveIds, as 1521's Brain room).
    ///  - Hidden 8 the squad ready (a scene timer 2 s after the accept), hidden 9 an aggregate of 1 and 8, hidden 10 waits for 9:
    ///    a cave cleared before the accept completes 1 at the accept, and a scene's start inside the accept is refused (a
    ///    different attempt), so the lead starts from 10, not from 1 (as 1521's tracker and opener).
    ///  - 10 opens 2: Petrovich leads up the tunnel (a scene route on the navmesh, fighting what he meets, as Plateau's
    ///    escorts: QuestSceneIntents.Escort), the squad follows the player ("You stay with us"). 2 the player at the turn
    ///    (AreaEntered, RestRadius round the rest place) opens 3.
    ///  - 3 the talk with Petrovich at the turn (13765) opens 4 and brings the ambush: two Atta Soldiers (the outpost's own row
    ///    531015, upstream) up the tunnel from the top, at the player; hidden 6 (Inactive until 3) counts both dead and brings
    ///    two more; hidden 7 (Inactive until 6) counts those; 4 follows 7 (the client has no counter text for 4; solo never four
    ///    Atta at once, as Echoes in the Sand 1736). 6 opens with the member's own talk, 7 when his 6 completes. A wave kill in
    ///    the squad's copy counts for every member on the mission whose counter is open (GroupCreditService, to-fix 29): a mate
    ///    who talks after the player's ambush is over gets his own two waves and his own 4; a mate who talks while the player's
    ///    first wave still stands has his 6 (and 7) completed by the player's kills, so his second wave comes while his own first
    ///    still stands (four of his at once) and his own waves stay uncounted (AshenAvernusOutpostTests).
    ///  - 4 done opens 5: Petrovich leads on to the top; 5 the player at the top (AreaEntered, TopRadius round Petrovich's second
    ///    row, the receiver): the squad goes, the reward. The turn-in at Petrovich's second row (790643), there from the start.
    ///  - The items by their client names, each a stack, one to choose: Class VIII Frag Grenade (45447) x3, Class VIII EMP Bomb
    ///    (111233) x1, Class VIII Cryogenic Grenade (111049) x3, Class VIII Standard Med Pack (45064) x3.
    ///  - Markers (unnamed): 1 each graveyard pool, 2 the turn, 3 Petrovich at the turn, 4 the turn, 5 the top.
    /// </summary>
    public static class AshenPetrovichsRunV1
    {
        public const uint MissionId = 1535;
        public const uint Level = 42;
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint Petrovich = AshenAvernusOutpostNpcsV1.Petrovich, PetrovichTop = AshenAvernusOutpostNpcsV1.PetrovichTop;
        public const uint PetrovichPackage = AshenAvernusOutpostNpcsV1.PetrovichPackage, Variant = 1;
        public const uint ClearTheCave = 1, TunnelRun = 2, Rest = 3, FendOff = 4, ToTheTop = 5, FirstWaveDown = 6, SecondWaveDown = 7,
            Ready = 8, Tracker = 9, Leads = 10;
        public const uint RestArea = 1, TopArea = 2;
        public const double RestRadius = 10, TopRadius = 10, PoolMarkerPadding = 4, NpcMarkerRadius = 8, AmbushMarkerRadius = 15;   // ESTIMATE (no source gives radii)

        /// <summary>The graveyard's Atta pools (World DB, upstream): pool, centre, radius.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At, double Radius)[] CavePools =
        {
            (531049, (821.3, 289, -240.5), 20), (531050, (944.7, 263.2, -289.7), 20), (531060, (974.5, 274.1, -230.4), 13.7),
            (531063, (867.5, 282.3, -280.7), 7)
        };

        /// <summary>Scenarios: up the tunnel, the ambush, the second wave, on to the top, home; the waves' kills.</summary>
        public const uint UpTheTunnel = 1, Ambush = 2, SecondWave = 3, OnToTheTop = 4, Home = 5, FirstWaveCounted = 6, SecondWaveCounted = 7,
            SquadReady = 8;
        public const uint FirstDownFrom = 100, SecondDownFrom = 110, ReadySequence = 30;
        /// <summary>The squad ready a moment after the accept (a scene timer): the lead never starts inside the accept itself.</summary>
        public const uint ReadyDelayMilliseconds = 2000;
        public const uint AttaSoldier = 531015;          // the outpost's Atta Soldier row (upstream)
        public const float RouteSpeed = 4f;

        public const string PetrovichRole = "petrovich", OfficerRole = "avernus-officer", TunnelRoute = "petrovich-up-the-tunnel",
            TopRoute = "petrovich-to-the-top";
        public static string SoldierRole(int index) => $"avernus-soldier-{index + 1}";
        public static string AmbushRole(int wave, int member) => $"tunnel-ambush-{wave}-{member}";

        /// <summary>The squad at Petrovich (the graveyard), and their places behind the player.</summary>
        public static readonly ScenePosition OfficerAt = new(907f, 274.11f, -243f);
        public static readonly ScenePosition[] SoldiersAt = { new(907f, 274.2f, -248f), new(911f, 273.6f, -244f) };
        public static readonly ScenePosition OfficerOffset = new(0f, 0f, -2.5f);
        public static readonly ScenePosition[] SoldierOffsets = { new(-1.5f, 0f, -2f), new(1.5f, 0f, -2f) };

        /// <summary>The turn (the rest) and the top (Petrovich's second row).</summary>
        public static readonly ScenePosition RestAt = new(775f, 317.37f, -112.5f);
        public static readonly (double X, double Y, double Z) TopAt = AshenAvernusOutpostNpcsV1.PetrovichTopAt;

        /// <summary>Petrovich's way up the tunnel (the client-floor walk's corners, ESTIMATE) and on to the top, beside his second row.</summary>
        public static readonly ScenePosition[] TunnelWay =
        {
            new(804f, 289.24f, -245f), new(793f, 297.02f, -233f), new(790f, 296.52f, -231f), new(789f, 296.41f, -229f),
            new(778f, 315.59f, -118f), RestAt
        };
        public static readonly ScenePosition[] TopWay =
        {
            new(772f, 318.68f, -109f), new(770f, 319.27f, -108f), new(757f, 320.18f, -112f), new(754f, 322.04f, -115f),
            new(746f, 330.12f, -128f), new(745f, 329.91f, -141f), new(739.5f, 329.15f, -146.7f), new(733f, 338.19f, -198f),
            new(735f, 343.9f, -234f), new(719f, 352.94f, -249f), new(693f, 353.63f, -253f), new(678f, 353.25f, -246f),
            new(648f, 353.37f, -245f)
        };

        /// <summary>The ambush: up the tunnel from the top, about 25 m above the turn (both waves the same places).</summary>
        public static readonly ScenePosition[] AmbushAt = { new(745f, 329.96f, -135f), new(747f, 329.93f, -127f) };

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Frag Grenade (class 22964)
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)
        public const uint CryogenicGrenade = 111049, CryogenicGrenades = 3;     // Class VIII Cryogenic Grenade (class 26299)
        public const uint StandardMedPack = 45064, StandardMedPacks = 3;        // Class VIII Standard Med Pack (class 22547)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Petrovich's Run", 13754, Petrovich, PetrovichTop, Level);
            mission.Reward(Experience, Credits,
                new[] { (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);
            mission.Scenario(UpTheTunnel, "The cave cleared: Petrovich leads up the tunnel", 0);
            mission.Scenario(Ambush, "The rest talk: the ambush", 0);
            mission.Scenario(SecondWave, "The first wave dead: the second", 0);
            mission.Scenario(OnToTheTop, "The ambush held off: on to the top", 0);
            mission.Scenario(Home, "At the top: the squad goes", 0);
            mission.Scenario(FirstWaveCounted, "The ambush's first wave killed", (uint)AmbushAt.Length);
            mission.Scenario(SecondWaveCounted, "The ambush's second wave killed", (uint)AmbushAt.Length);
            mission.Scenario(SquadReady, "The squad ready (a moment after the accept)", 1);

            mission.Objective(ClearTheCave, 13759, 13760, 0, MissionObjectiveState.Incomplete);
            mission.Objective(TunnelRun, 13761, 13762, 1, MissionObjectiveState.Inactive);
            mission.Objective(Rest, 13763, 13764, 2, MissionObjectiveState.Inactive);
            mission.Objective(FendOff, 13766, 13767, 3, MissionObjectiveState.Inactive);
            mission.Objective(ToTheTop, 13768, 13769, 4, MissionObjectiveState.Inactive);
            mission.Objective(FirstWaveDown, 13766, 13767, 5, MissionObjectiveState.Inactive, required: false);     // hidden; 3 opens it
            mission.Objective(SecondWaveDown, 13766, 13767, 6, MissionObjectiveState.Inactive, required: false);    // hidden; 6 opens it
            mission.Objective(Ready, 13759, 13760, 7, MissionObjectiveState.Incomplete, required: false);           // hidden
            mission.Objective(Tracker, 13759, 13760, 8, MissionObjectiveState.Incomplete, required: false);         // hidden
            mission.Objective(Leads, 13759, 13760, 9, MissionObjectiveState.Incomplete, required: false);           // hidden

            // 1: the graveyard's four pools dead at once in the squad's copy (a cave cleared before the accept counts at it).
            mission.Transition(ClearTheCave);
            var trigger = 1U;
            foreach (var (pool, _, _) in CavePools)
                mission.Progress(ClearTheCave, MissionProgressEventKind.SpawnPoolCleared, pool, null, trigger: trigger++);

            // 8, hidden: the squad ready (the scene's timer after the accept); 9, hidden: 1 and 8 done; 10, hidden: 9 opens 2 and
            // Petrovich leads up the tunnel. Not 1's own actions: a scene's start inside the accept is refused (a different attempt).
            mission.Transition(Ready);
            mission.Progress(Ready, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SquadReady);
            mission.Transition(Leads);
            mission.ObjectiveState(Leads, Tracker, MissionObjectiveState.Completed);
            Open(mission, Leads, 1, TunnelRun);
            mission.Action(Leads, 3, MissionActionKind.StartScenario, scenario: UpTheTunnel);

            // 2: the player at the turn.
            mission.Area(RestArea, Outpost, RestAt.X, RestAt.Y, RestAt.Z, RestRadius);
            mission.Transition(TunnelRun);
            mission.EnterArea(TunnelRun, RestArea);
            Open(mission, TunnelRun, 1, Rest);

            // 3: the talk at the turn brings the ambush.
            mission.Transition(Rest);
            mission.Conversation(Rest, PetrovichPackage, Variant);
            Open(mission, Rest, 1, FendOff);
            mission.Action(Rest, 3, MissionActionKind.StartScenario, scenario: Ambush);
            // 6 opens with the player's own ambush (and 7 with his second wave): a squad mate's wave kills, sent to every member in
            // the copy, do not count before his own talk.
            Open(mission, Rest, 4, FirstWaveDown);     // hidden: the scene keeps it from the client

            // 6, hidden: the first wave dead brings the second; 7, hidden: the second dead; 4 follows 7.
            mission.Transition(FirstWaveDown);
            for (var member = 1U; member <= AmbushAt.Length; member++)
                mission.Progress(FirstWaveDown, MissionProgressEventKind.ScenarioEvent, member, (uint)AmbushAt.Length, counter: FirstWaveCounted, trigger: member);
            mission.Action(FirstWaveDown, 1, MissionActionKind.StartScenario, scenario: SecondWave);
            Open(mission, FirstWaveDown, 2, SecondWaveDown);
            mission.Transition(SecondWaveDown);
            for (var member = 1U; member <= AmbushAt.Length; member++)
                mission.Progress(SecondWaveDown, MissionProgressEventKind.ScenarioEvent, member, (uint)AmbushAt.Length, counter: SecondWaveCounted, trigger: member);
            mission.Transition(FendOff);
            mission.ObjectiveState(FendOff, SecondWaveDown, MissionObjectiveState.Completed);
            Open(mission, FendOff, 1, ToTheTop);
            mission.Action(FendOff, 3, MissionActionKind.StartScenario, scenario: OnToTheTop);

            // 5: the player at the top; the squad goes; the reward.
            mission.Area(TopArea, Outpost, TopAt.X, TopAt.Y, TopAt.Z, TopRadius);
            mission.Transition(ToTheTop);
            mission.EnterArea(ToTheTop, TopArea);
            mission.Action(ToTheTop, 1, MissionActionKind.StartScenario, scenario: Home);
            mission.Action(ToTheTop, 2, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (_, at, radius) in CavePools)
                mission.Indicator(ClearTheCave, marker++, at.X, at.Y, at.Z, radius + PoolMarkerPadding, Outpost);
            mission.Indicator(TunnelRun, marker++, RestAt.X, RestAt.Y, RestAt.Z, RestRadius, Outpost);
            mission.Indicator(Rest, marker++, RestAt.X, RestAt.Y, RestAt.Z, NpcMarkerRadius, Outpost);
            mission.Indicator(FendOff, marker++, RestAt.X, RestAt.Y, RestAt.Z, AmbushMarkerRadius, Outpost);
            mission.Indicator(ToTheTop, marker, TopAt.X, TopAt.Y, TopAt.Z, TopRadius, Outpost);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1535",
                "Native1 'Clear the Cave' 13759/13760, 2 'Tunnel Run' 13761/13762, 3 'Rest' 13763/13764, 4 'Fend off the Ambush!' " +
                "13766/13767, 5 'All The Way To The Top' 13768/13769; no counters, no indicator names. objectiveconversation 1535,3 " +
                "pkg1803 13765. Texts 13754-13758. Names 9672 Petrovich, 9673 Avernus Outpost Soldier, 9674 Avernus Outpost Officer.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Petrovich%27s_Run",
                "Giver/reward giver Captain Petrovich (Avernus Outpost); follow-up Assessing the Atta; 90000XP, 6150 credits; Class VIII " +
                "Frag Grenade (3), EMP Bomb, Class VIII Cryogenic Grenade (3), Standard Med Pack (3); clear the room, follow Petrovich " +
                "and his squad up the tunnel, rest at the big turn, the ambush, the top, speak to him.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenPetrovichsRunV1",
                "Level42; bound to the squad's copy 2110; Petrovich 790642 gives, his second row 790643 at the top rewards. Petrovich's " +
                "copy (OwnerOnly, invulnerable, the public one hidden) and his squad (officer 790644, soldiers 790645, invulnerable) from " +
                "the accept; 1 pools 531049, 531050, 531060, 531063 cleared at once; hidden 8 (a 2 s timer), 9 (1 and 8), 10 (opens 2): Petrovich leads a navmesh route to the turn " +
                "(775,317.37,-112.5), the squad follows the player; 2 the 10 m area there; 3 his talk (pkg1803) brings two Atta Soldiers " +
                "531015, hidden 6 (opened by 3) counts them and brings two more, hidden 7 (opened by 6) counts those, 4 follows 7; Petrovich leads on, 5 the 10 m " +
                "area at the top (645.5,352.67,-246.5): the squad goes. Items 45447x3, 111233x1, 111049x3, 45064x3, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Petrovich's copy, his squad, his routes and the ambush, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Outpost,    // taken at Petrovich in the squad's copy
                MapContextId = Outpost,
                HiddenObjectiveIds = new() { FirstWaveDown, SecondWaveDown, Ready, Tracker, Leads },
                ExistingFactObjectiveIds = new() { ClearTheCave },
                ObjectiveAggregations = new() { [Tracker] = new MissionObjectiveAggregation(new[] { ClearTheCave, Ready }, 2) },
                Routes = new(),
                DefeatSequences = new()
            };
            var at = AshenAvernusOutpostNpcsV1.PetrovichAt;
            var unhurt = new ActorGameplayPolicy { Invulnerable = true };
            scene.Actors[PetrovichRole] = new SceneActorDefinition(PetrovichRole, SceneActorKind.Creature, Petrovich,
                new ScenePosition((float)at.X, (float)at.Y, (float)at.Z), GameplayPolicy: unhurt, OwnerOnly: true,
                ReplacesPublicSpawnId: Petrovich, ReplaceWhilePresent: true);
            scene.Actors[OfficerRole] = new SceneActorDefinition(OfficerRole, SceneActorKind.Creature, AshenAvernusOutpostNpcsV1.Officer,
                OfficerAt, FollowOffset: OfficerOffset, GameplayPolicy: unhurt, OwnerOnly: true);
            for (var index = 0; index < SoldiersAt.Length; index++)
                scene.Actors[SoldierRole(index)] = new SceneActorDefinition(SoldierRole(index), SceneActorKind.Creature,
                    AshenAvernusOutpostNpcsV1.Soldier, SoldiersAt[index], FollowOffset: SoldierOffsets[index], GameplayPolicy: unhurt, OwnerOnly: true);
            scene.Routes[TunnelRoute] = new SceneRoute(TunnelRoute, TunnelWay.Select(point => new SceneWaypoint(point)).ToArray(), Speed: RouteSpeed);
            scene.Routes[TopRoute] = new SceneRoute(TopRoute, TopWay.Select(point => new SceneWaypoint(point)).ToArray(), Speed: RouteSpeed);

            var squad = new[] { PetrovichRole, OfficerRole }.Concat(Enumerable.Range(0, SoldiersAt.Length).Select(SoldierRole)).ToArray();
            var followers = squad.Skip(1).ToArray();
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                Timers = { new SequenceTimer("squad-ready", ReadyDelayMilliseconds, ReadySequence) }
            };
            foreach (var role in squad)
                scene.Sequences[0].World.Add(new EnsureActorIntent($"{role}-comes", role));
            scene.Sequences[ReadySequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, SquadReady, 1) } };

            var up = new SceneSequenceDefinition { World = { QuestSceneIntents.Escort("petrovich-leads-up-the-tunnel", PetrovichRole, TunnelRoute) } };
            foreach (var role in followers)
                up.World.Add(new FollowActorIntent($"{role}-follows", role, 0, Enabled: true));
            scene.Sequences[UpTheTunnel] = up;
            scene.Sequences[OnToTheTop] = new SceneSequenceDefinition
            {
                World = { QuestSceneIntents.Escort("petrovich-leads-to-the-top", PetrovichRole, TopRoute) }
            };
            var home = new SceneSequenceDefinition();
            foreach (var role in squad)
                home.World.Add(new RemoveActorIntent($"{role}-goes", role));
            scene.Sequences[Home] = home;

            foreach (var (wave, scenario, counted, downFrom) in new[] { (1, Ambush, FirstWaveCounted, FirstDownFrom), (2, SecondWave, SecondWaveCounted, SecondDownFrom) })
            {
                var comes = new SceneSequenceDefinition();
                for (var member = 0; member < AmbushAt.Length; member++)
                {
                    var role = AmbushRole(wave, member + 1);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, AttaSoldier, AmbushAt[member],
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    comes.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    comes.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role, holdTarget: true));
                    var down = downFrom + 1 + (uint)member;
                    scene.DefeatSequences[role] = down;
                    scene.Sequences[down] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, counted, (uint)member + 1) } };
                }
                scene.Sequences[scenario] = comes;
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The objective's action <paramref name="action"/> reveals and opens <paramref name="next"/>.</summary>
        private static void Open(AshenMissionDataV1 mission, uint objective, uint action, uint next)
        {
            mission.Action(objective, action, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, action + 1, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete);
        }
    }
}
