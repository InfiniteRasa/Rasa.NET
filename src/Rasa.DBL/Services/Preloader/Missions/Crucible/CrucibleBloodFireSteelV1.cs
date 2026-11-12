using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Blood, Fire, and Steel (native mission 1401), the Incurables Ward (squad instance 2141): Mechanic Shanul sends the
    /// player for the keycards of Atali (west wing) and Umank (east wing), which open the two locked doors north of the ring;
    /// behind them the Headsman and his Warden Bots wake, and the Headsman's keycard opens the door to Jaikev's Sanctum.
    ///
    /// Native: texts 12062-12066 (missionconversation 1 name 12062, 2 log 12063, 3 offer 12064, 4 turn-in 12065 (Shanul), 6
    /// "none" 12066); objectives (missionobjective) 1 "Recover Atali's Keycard" 12660/12661, 2 "Recover Umank's Keycard"
    /// 12662/12663, 3 "Counter" 12664/12665 counter 12666, 4 "Report to Mechanic Shanul" 12667/12668, 5 "Recover Headsman's
    /// Keycard" 12670/12671, 6 "Open the Door to Jaikev's Sanctum" 12693/12694; objective conversation (1401, 4) package 1664
    /// (Shanul) 12669. Items: classes 25909/25910/25911 "Atali's/Umank's/Headsman's Incurable Ward Keycard", templates
    /// 110887/110888/110889. Names 9536 Atali, 9535 Umank, 9540 Headsman. Door class 10749 UsableDoorBrannCorridor04mV01
    /// (states 31 closed, 91 open; usabledata lookup[10749] all None: use argument 1).
    ///
    /// Sourced: TaRapedia Blood, Fire, and Steel (0.7): Mechanic Shanul, the Incurables Ward; kill Atali (west) and Umank
    /// (east) for their keycards; "Straight north are the 2 locked doors" (TaRapedia Rehabilitation at Gunpoint); a use with
    /// the keycard opens a door and takes the card (2008-01); with both open the Warden Bots and the Headsman come; his card
    /// opens the door to Jaikev's Sanctum. Route and wings: NOTES-crucible-research-group4.md "1401 wing doors" and "1401
    /// verify and retail detail" (client geometry, 0.8). Patch 2008-03-26: doors at the boss areas (places unknown).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 40, no prerequisite, 170,000 XP and 8,000 credits, no items (as 1398 and 1444; no source). Taken at Shanul in
    ///    the squad's Ward (the scene bound to it); giver and receiver Shanul.
    ///  - Atali 770070 and Umank 770071 (class 25547, Jaikev's; level 42, 12000 HP, armor 300, Jaikev's attack) on point pools
    ///    in the west wing (-18, 62.61, -6) and the east wing (202, 54.61, 210), each with a point pool of two Brutes and two
    ///    Slayers; named public-spawn actors (90 s in the squad's copy). 1 and 2 their cards, a 100% drop to every member on
    ///    1401 (CharacterOwned, Retain); one held already counts at the accept. 3 (hidden) both; 7 (hidden) opens 4.
    ///  - 4 the talk at Shanul opens 5 and the north doors' hidden 8 and 9 (his text 12669: "more locked doors").
    ///  - The two north doors (class 10749, personal scene objects, closed 31) at the ring's jamb (78, 62, 40) and the first
    ///    airlock's throat (78, 62, 50), yaw 0; locked to Atali's / Umank's card (UsableLock.KeyItemTemplateId: the client's
    ///    CanUnlock; without it the server refuses the use, PmMissingReqItem). Each is usable while its own hidden objective
    ///    (8 / 9) is open; 4 opens 8 and 9 with 5 (not gated on 5: 5 can complete first, by a kept Headsman card or a squad
    ///    mate's kill). The use opens the door (91) and its event takes the card (8 / 9); 10 (hidden) both, 11 (hidden)
    ///    wakes the Headsman 770072 (class 10315 Creature_Prison_Bot_Warden_Boss; level 42, 12000 HP, armor 300, the Warden
    ///    Bot's attacks) at (78, 62.61, 62) and four Warden Bots (row 531023) around him; they go for the player. 5 his card (a 100% drop while 5 is open).
    ///  - 6 the Sanctum door (class 10749, locked to the Headsman's card) at the jamb (94, 78, 400), south of Paliv's body:
    ///    the use opens it and takes the card; the reward.
    ///  - Deviations: the boss-area doors of 2008-03-26 are not built (places unknown; a shut-while-fighting door needs a
    ///    combat trigger). The doors stop only their owner's client (a scene object shown to its owner); creatures and other
    ///    players pass. Each member on 1401 wakes his own Headsman group. In the squad's instance a door used by one member
    ///    counts for every member on 1401 whose gate is open (a scene event, GroupCreditService): each one's card is taken and
    ///    each one's own door opens (the door opens from its objective, 8 / 9 / 6, not from the use).
    ///  - Incurable pool 770048 (76, 63.01, 132) made a point: its radius-5 draw reached off over the big polygons (C11).
    ///  - Markers: Atali (1), Umank (2), Shanul (4), the doors and the Headsman (5), the Sanctum door (6).
    /// </summary>
    public static class CrucibleBloodFireSteelV1
    {
        public const uint MissionId = 1401;
        public const uint Level = 40;
        public const uint Shanul = CrucibleIncurablesWardNpcsV1.Shanul, ShanulPackage = CrucibleIncurablesWardNpcsV1.ShanulPackage;
        public const uint MapContextId = CrucibleIncurablesWardNpcsV1.Ward;
        public const uint AtaliCard = 1, UmankCard = 2, Counter = 3, ReportToShanul = 4, HeadsmanCard = 5, SanctumDoor = 6;
        public const uint CardsOpener = 7, AtaliDoorUsed = 8, UmankDoorUsed = 9, DoorsTracker = 10, DoorsOpener = 11;

        /// <summary>The bosses and their pools (ids from 770070; set 12b takes up to 770063).</summary>
        public const uint Atali = 770070, AtaliName = 9536, Umank = 770071, UmankName = 9535;
        public const uint Headsman = 770072, HeadsmanName = 9540, HeadsmanClass = 10315;
        public const uint BossClass = CrucibleIncurablesWardNpcsV1.JaikevClass;
        public const uint BossLevel = CrucibleIncurablesWardNpcsV1.BossLevel, BossHitPoints = CrucibleIncurablesWardNpcsV1.BossHitPoints;
        public const uint BossArmor = CrucibleIncurablesWardNpcsV1.BossArmor;
        public const uint WardenLaser = 53035, WardenShock = 53036, WardenThird = 60001;   // creature 531023's actions
        public const uint WardenBot = CrucibleChaukasNpcsV1.Claviger;                       // 531023
        public const uint AtaliPool = 770070, UmankPool = 770071, AtaliGuards = 770072, UmankGuards = 770073;
        public const uint NextFreeId = 770074;
        public static readonly (double X, double Y, double Z) AtaliAt = (-18, 62.61, -6), AtaliGuardsAt = (-24, 62.61, -14);
        public static readonly (double X, double Y, double Z) UmankAt = (202, 54.61, 210), UmankGuardsAt = (210, 54.61, 202);
        public const uint GuardBrutes = 2, GuardSlayers = 2;

        /// <summary>Incurable pool 770048 (the north corridor) made a point: radius 5 before.</summary>
        public const uint PointedPool = 770048, PointedPoolRadiusBefore = 5;

        public const uint AtaliCardTemplate = 110887, AtaliCardClass = 25909;
        public const uint UmankCardTemplate = 110888, UmankCardClass = 25910;
        public const uint HeadsmanCardTemplate = 110889, HeadsmanCardClass = 25911;
        public const string AtaliCardKey = "atalis-keycard", UmankCardKey = "umanks-keycard", HeadsmanCardKey = "headsmans-keycard";
        public const int CardChance = 100;

        public const uint DoorClass = 10749;            // UsableDoorBrannCorridor04mV01
        public const uint UseArgument = 1;              // usabledata lookup[10749]: all None
        public const uint ClosedState = 31, OpenState = 91;
        public static readonly ScenePosition AtaliDoorAt = new(78f, 62f, 40f);
        public static readonly ScenePosition UmankDoorAt = new(78f, 62f, 50f);
        public static readonly ScenePosition SanctumDoorAt = new(94f, 78f, 400f);
        public const string AtaliRole = "atali", UmankRole = "umank", HeadsmanRole = "headsman";
        public const string AtaliDoorRole = "atalis-door", UmankDoorRole = "umanks-door", SanctumDoorRole = "sanctum-door";

        /// <summary>
        /// Scenarios: each door used (its event), the Headsman's group wakes, each door opens. A use's sequence (11-13) only
        /// sends the event; the door opens in the sequence its objective starts (5-7). In the squad's instance a member's scene
        /// event counts for every member on 1401 (GroupCreditService.SharedInSquadInstance): the objective that takes each
        /// one's card also opens each one's own door, so no member is left behind a shut door with the card gone.
        /// </summary>
        public const uint AtaliDoorOpened = 1, UmankDoorOpened = 2, SanctumDoorOpened = 3, HeadsmanWakes = 4;
        public const uint AtaliDoorOpens = 5, UmankDoorOpens = 6, SanctumDoorOpens = 7;
        public const uint AtaliDoorSequence = 11, UmankDoorSequence = 12, SanctumDoorSequence = 13;

        public static readonly ScenePosition HeadsmanAt = new(78f, 62.61f, 62f);
        public static readonly ScenePosition[] WardenBotsAt =
        {
            new(75f, 62.61f, 60f), new(81f, 62.61f, 60f), new(75f, 62.61f, 64f), new(81f, 62.61f, 64f)
        };

        /// <summary>The markers of 5 (both doors; the Headsman) and 6 (before the Sanctum door).</summary>
        public static readonly (double X, double Y, double Z) DoorsMarkerAt = (78, 62.6, 45), SanctumMarkerAt = (94, 78.6, 397);
        public const double BossMarkerRadius = 10, NpcMarkerRadius = 8, DoorsMarkerRadius = 8, HeadsmanMarkerRadius = 10, SanctumMarkerRadius = 6;

        public const uint Experience = 170000;
        public const uint Credits = 8000;

        public static string WardenBotRole(int index) => $"warden-bot-{index + 1}";

        public static IEnumerable<string> NpcUp()
        {
            var attack = CrucibleIncurablesWardNpcsV1.Attack;
            yield return Creature(Atali, "Atali - Incurables Ward, the west wing (1401)", BossClass, BossLevel, BossHitPoints, AtaliName, attack);
            yield return Stat(Atali, BossHitPoints, BossArmor);
            yield return Creature(Umank, "Umank - Incurables Ward, the east wing (1401)", BossClass, BossLevel, BossHitPoints, UmankName, attack);
            yield return Stat(Umank, BossHitPoints, BossArmor);
            yield return Creature(Headsman, "Headsman - Incurables Ward, north of the ring (1401)", HeadsmanClass, BossLevel, BossHitPoints,
                HeadsmanName, WardenLaser, WardenShock, WardenThird);
            yield return Stat(Headsman, BossHitPoints, BossArmor);
            var respawn = CrucibleIncurablesWardNpcsV1.WardRespawnTenths;
            yield return Pool(AtaliPool, AtaliAt.X, AtaliAt.Y, AtaliAt.Z, MapContextId, Atali, 1, 1, respawn, 0);
            yield return Pool(UmankPool, UmankAt.X, UmankAt.Y, UmankAt.Z, MapContextId, Umank, 1, 1, respawn, 0);
            yield return Guards(AtaliGuards, AtaliGuardsAt);
            yield return Guards(UmankGuards, UmankGuardsAt);
            yield return $"UPDATE spawnpool SET radius = 0 WHERE id = {PointedPool};";
        }

        public static IEnumerable<string> NpcDown()
        {
            yield return $"UPDATE spawnpool SET radius = {PointedPoolRadiusBefore} WHERE id = {PointedPool};";
            yield return $"DELETE FROM spawnpool WHERE id IN ({AtaliPool}, {UmankPool}, {AtaliGuards}, {UmankGuards});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Atali}, {Umank}, {Headsman});";
            yield return $"DELETE FROM creature WHERE id IN ({Atali}, {Umank}, {Headsman});";
        }

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Blood, Fire, and Steel", 12062, Shanul, Shanul, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Scenario(AtaliDoorOpened, "Atali's door opened", 1);
            mission.Scenario(UmankDoorOpened, "Umank's door opened", 1);
            mission.Scenario(SanctumDoorOpened, "The door to Jaikev's Sanctum opened", 1);
            mission.Scenario(HeadsmanWakes, "Both north doors open: the Headsman and the Warden Bots wake", 0);
            mission.Scenario(AtaliDoorOpens, "Atali's door opens (8 done)", 0);
            mission.Scenario(UmankDoorOpens, "Umank's door opens (9 done)", 0);
            mission.Scenario(SanctumDoorOpens, "The door to Jaikev's Sanctum opens (6 done)", 0);

            mission.Objective(AtaliCard, 12660, 12661, 0, MissionObjectiveState.Incomplete);
            mission.Objective(UmankCard, 12662, 12663, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Counter, 12664, 12665, 2, MissionObjectiveState.Incomplete, required: false);     // hidden
            mission.Objective(ReportToShanul, 12667, 12668, 3, MissionObjectiveState.Inactive);
            mission.Objective(HeadsmanCard, 12670, 12671, 4, MissionObjectiveState.Inactive);
            mission.Objective(SanctumDoor, 12693, 12694, 5, MissionObjectiveState.Inactive);
            mission.Objective(CardsOpener, 12664, 12665, 6, MissionObjectiveState.Incomplete, required: false);     // hidden
            mission.Objective(AtaliDoorUsed, 12670, 12671, 7, MissionObjectiveState.Inactive, required: false);     // hidden
            mission.Objective(UmankDoorUsed, 12670, 12671, 8, MissionObjectiveState.Inactive, required: false);     // hidden
            mission.Objective(DoorsTracker, 12670, 12671, 9, MissionObjectiveState.Incomplete, required: false);    // hidden
            mission.Objective(DoorsOpener, 12670, 12671, 10, MissionObjectiveState.Incomplete, required: false);    // hidden

            // 1, 2: the bosses' keycards.
            mission.Transition(AtaliCard);
            mission.Progress(AtaliCard, MissionProgressEventKind.ItemAcquired, AtaliCardClass, 1);
            mission.Transition(UmankCard);
            mission.Progress(UmankCard, MissionProgressEventKind.ItemAcquired, UmankCardClass, 1);

            // 7, hidden: 3 (1 and 2 done) opens 4.
            mission.Transition(CardsOpener);
            mission.ObjectiveState(CardsOpener, Counter, MissionObjectiveState.Completed);
            CrucibleCalendarGirlV1.Open(mission, CardsOpener, 1, 1, ReportToShanul);

            // 4: the talk at Shanul opens 5 and the two north doors (hidden 8, 9: "you may run into more locked doors", 12669).
            mission.Transition(ReportToShanul);
            mission.Conversation(ReportToShanul, ShanulPackage);
            CrucibleCalendarGirlV1.Open(mission, ReportToShanul, 1, 1, HeadsmanCard);
            CrucibleCalendarGirlV1.Open(mission, ReportToShanul, 1, 3, AtaliDoorUsed);
            CrucibleCalendarGirlV1.Open(mission, ReportToShanul, 1, 5, UmankDoorUsed);

            // 8, 9, hidden: a north door used takes its card and opens the door. Each door is gated on its own objective, not
            // on 5: 5 can complete before the doors (a Headsman card kept from an abandoned run, or a squad mate's Headsman kill).
            foreach (var (objective, scenario, key, reason, opens) in new[]
            {
                (AtaliDoorUsed, AtaliDoorOpened, AtaliCardKey, "atalis-door-takes-the-card", AtaliDoorOpens),
                (UmankDoorUsed, UmankDoorOpened, UmankCardKey, "umanks-door-takes-the-card", UmankDoorOpens)
            })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, 1, null, counter: scenario);
                mission.Action(objective, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent(reason, MissionId, key, 1, MissionItemScope.CharacterOwned));
                mission.Action(objective, 2, MissionActionKind.StartScenario, scenario: opens);
            }

            // 11, hidden: 10 (both doors open) wakes the Headsman and his bots.
            mission.Transition(DoorsOpener);
            mission.ObjectiveState(DoorsOpener, DoorsTracker, MissionObjectiveState.Completed);
            mission.Action(DoorsOpener, 1, MissionActionKind.StartScenario, scenario: HeadsmanWakes);

            // 5: the Headsman's keycard.
            mission.Transition(HeadsmanCard);
            mission.Progress(HeadsmanCard, MissionProgressEventKind.ItemAcquired, HeadsmanCardClass, 1);
            CrucibleCalendarGirlV1.Open(mission, HeadsmanCard, 1, 1, SanctumDoor);

            // 6: the Sanctum door opened takes the card; the reward.
            mission.Transition(SanctumDoor);
            mission.Progress(SanctumDoor, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SanctumDoorOpened);
            mission.Action(SanctumDoor, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("sanctum-door-takes-the-card", MissionId, HeadsmanCardKey, 1, MissionItemScope.CharacterOwned));
            mission.Action(SanctumDoor, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Action(SanctumDoor, 3, MissionActionKind.StartScenario, scenario: SanctumDoorOpens);

            var shanul = CrucibleIncurablesWardNpcsV1.ShanulAt;
            var marker = 1U;
            mission.Indicator(AtaliCard, marker++, AtaliAt.X, AtaliAt.Y, AtaliAt.Z, BossMarkerRadius, MapContextId);
            mission.Indicator(UmankCard, marker++, UmankAt.X, UmankAt.Y, UmankAt.Z, BossMarkerRadius, MapContextId);
            mission.Indicator(ReportToShanul, marker++, shanul.X, shanul.Y, shanul.Z, NpcMarkerRadius, MapContextId);
            mission.Indicator(HeadsmanCard, marker++, DoorsMarkerAt.X, DoorsMarkerAt.Y, DoorsMarkerAt.Z, DoorsMarkerRadius, MapContextId);
            mission.Indicator(HeadsmanCard, marker++, HeadsmanAt.X, HeadsmanAt.Y, HeadsmanAt.Z, HeadsmanMarkerRadius, MapContextId);
            mission.Indicator(SanctumDoor, marker, SanctumMarkerAt.X, SanctumMarkerAt.Y, SanctumMarkerAt.Z, SanctumMarkerRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1401",
                "Native1 'Recover Atali's Keycard' 12660/12661, 2 'Recover Umank's Keycard' 12662/12663, 3 'Counter' 12664/12665 " +
                "counter12666, 4 'Report to Mechanic Shanul' 12667/12668, 5 'Recover Headsman's Keycard' 12670/12671, 6 'Open the " +
                "Door to Jaikev's Sanctum' 12693/12694. objectiveconversation 1401,4 pkg1664 12669. Texts 12062-12066. Keycards " +
                "class25909-25911, templates110887-110889. Names 9536 Atali, 9535 Umank, 9540 Headsman. Door class10749 (31/91, arg1).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Blood,_Fire,_and_Steel",
                "Mechanic Shanul, Incurables Ward; Atali (west) and Umank (east) drop keycards; two locked doors straight north; a " +
                "keycard use opens a door and takes the card; both open wake the Warden Bots and the Headsman; his card opens " +
                "Jaikev's Sanctum. Boss-area doors (2008-03-26). Rewards unknown. Route (NOTES-crucible-research-group4.md, client " +
                "geometry): no doors to the wings, the way down the centre room's stairs, the only way north at x78, z36-40.", 0.7);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleBloodFireSteelV1",
                "Bound to the squad's Ward 2141; level40, no prerequisite; Shanul gives and rewards. Atali 770070 (-18,62.61,-6) and " +
                "Umank 770071 (202,54.61,210), class25547, point pools with guards 770072/770073; 100% card drops to the squad. " +
                "Hidden 3/7 open 4 (Shanul pkg1664), 4 opens 5 and hidden 8/9 (the north doors' gates). Personal doors 10749 at 78,62,40 and 78,62,50 keyed 110887/110888; " +
                "hidden 8/9 take the cards and open the doors (squad mates' too), 10/11 wake Headsman 770072 (class10315) at 78,62.61,62 and four Warden Bots 531023; 5 " +
                "his card; 6 the Sanctum door 10749 at 94,78,400 keyed 110889 takes it and opens it. No items; 170000XP, 8000 credits. Pool " +
                "770048 a point. Boss-area doors not built.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The bosses named, the player's three doors and the Headsman's group, in the squad's Ward.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Shanul in the squad's Ward
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { Counter, CardsOpener, AtaliDoorUsed, UmankDoorUsed, DoorsTracker, DoorsOpener },
                ExistingFactObjectiveIds = new() { AtaliCard, UmankCard },
                ObjectiveAggregations = new()
                {
                    [Counter] = new MissionObjectiveAggregation(new[] { AtaliCard, UmankCard }, 2),
                    [DoorsTracker] = new MissionObjectiveAggregation(new[] { AtaliDoorUsed, UmankDoorUsed }, 2)
                },
                Items = new()
                {
                    Card(AtaliCardKey, AtaliCardTemplate, Atali, AtaliCard),
                    Card(UmankCardKey, UmankCardTemplate, Umank, UmankCard),
                    Card(HeadsmanCardKey, HeadsmanCardTemplate, Headsman, HeadsmanCard)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[AtaliRole] = new SceneActorDefinition(AtaliRole, SceneActorKind.PublicSpawn, AtaliPool);
            scene.Actors[UmankRole] = new SceneActorDefinition(UmankRole, SceneActorKind.PublicSpawn, UmankPool);

            foreach (var (role, at, card, objective, sequence, scenario, opens) in new[]
            {
                (AtaliDoorRole, AtaliDoorAt, AtaliCardTemplate, AtaliDoorUsed, AtaliDoorSequence, AtaliDoorOpened, AtaliDoorOpens),
                (UmankDoorRole, UmankDoorAt, UmankCardTemplate, UmankDoorUsed, UmankDoorSequence, UmankDoorOpened, UmankDoorOpens),
                (SanctumDoorRole, SanctumDoorAt, HeadsmanCardTemplate, SanctumDoor, SanctumDoorSequence, SanctumDoorOpened, SanctumDoorOpens)
            })
            {
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, DoorClass, at, Orientation: 0.0,
                    InitialObjectState: ClosedState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument),
                    KeyItemTemplateId: card);
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                // The use: its event only (the objective takes the card and starts the opening).
                scene.Sequences[sequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, scenario, 1) } };
                scene.Sequences[opens] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-opens", role, OpenState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    }
                };
            }

            var wake = new SceneSequenceDefinition();
            scene.Actors[HeadsmanRole] = new SceneActorDefinition(HeadsmanRole, SceneActorKind.Creature, Headsman, HeadsmanAt,
                GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
            wake.World.Add(new EnsureActorIntent("headsman-wakes", HeadsmanRole));
            wake.World.Add(QuestSceneIntents.Attack("headsman-attacks", HeadsmanRole));
            for (var index = 0; index < WardenBotsAt.Length; index++)
            {
                var role = WardenBotRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, WardenBot, WardenBotsAt[index],
                    GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                wake.World.Add(new EnsureActorIntent($"{role}-wakes", role));
                wake.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
            }
            scene.Sequences[HeadsmanWakes] = wake;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        /// <summary>A keycard: a personal 100% drop from <paramref name="creature"/> while <paramref name="objective"/> is open.</summary>
        private static MissionItemBinding Card(string key, uint template, uint creature, uint objective) =>
            new(key, template, MissionItemScope.CharacterOwned, 1,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                Drop: new MissionItemDropDefinition(new[] { creature }, objective, MapContextId, CardChance, 1));

        /// <summary>A point pool of two Brutes and two Slayers (two creature slots; PlateauCreatureSqlV1.Pool writes one).</summary>
        private static string Guards(uint pool, (double X, double Y, double Z) at) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"VALUES ({pool}, 0, 0, {CrucibleIncurablesWardNpcsV1.WardRespawnTenths}, {N(at.X)}, {N(at.Y)}, {N(at.Z)}, 0, {MapContextId}, " +
            $"{CrucibleIncurablesWardNpcsV1.Brute}, {GuardBrutes}, {GuardBrutes}, {CrucibleIncurablesWardNpcsV1.Slayer}, {GuardSlayers}, {GuardSlayers}, " +
            "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
