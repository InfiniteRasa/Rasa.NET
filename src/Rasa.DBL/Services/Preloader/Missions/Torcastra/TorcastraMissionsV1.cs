using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Torcastra
{
    using Prison = Rasa.Services.Preloader.TorcastraPrisonContent;

    /// <summary>
    /// Torcastra Prison's missions (Torcastra_prison), on the place TorcastraPrisonContent puts
    /// down and the Game's TorcastraPrison runs:
    ///  - 594 Securing Torcastra (Lt. Cisco): the four bunkers, the alarm's stand-down before its
    ///    air strike, the Gatekeeper's key and the gate.
    ///  - 397 In Prison: In Terror (Field Sgt. O'Toole at Foreas Base, to Ranger Ferme): her
    ///    interrogators, and her cell.
    ///  - 398 Information Sabotage (Ferme, to HQ by radio): the storage banks and panels, the worm,
    ///    Ferme again, the two data terminals.
    ///  - 399 Prison Riot (HQ by radio, both ways, once 398 is done): the five cell blocks.
    ///  - 356 Keymaster of Torcastra (Cisco at the gate, to Base Cmdr. Matlin; after 594): the
    ///    prisoner computer banks, Airman Hamilton's release, and his choice - escorted to Cisco or
    ///    killed - each way reported to Cisco and then Matlin.
    ///  - 1860 Stolen History (Elder Q'uoa): authored with Overseer Torqua's drop of the
    ///    rendezvous coordinates, but not enabled: its other two pieces are in Minos Caverns and
    ///    Timora Mines, which have nothing yet to give them.
    ///
    /// The client gives the missions, their objectives, texts, counters and NPC packages; the wiki
    /// the order of things and the rewards' experience and credits. What the Game raises comes in
    /// as scenario events (<see cref="Events"/>); a distinct set of them is a scenario of its own,
    /// events 1..N.
    /// </summary>
    public static class TorcastraMissionsV1
    {
        public const uint Securing = 594, InTerror = 397, Sabotage = 398, Riot = 399, Keymaster = 356, StolenHistory = 1860;

        public static readonly uint[] All = { Securing, InTerror, Sabotage, Riot, Keymaster, StolenHistory };

        /// <summary>The ones enabled: all but Stolen History.</summary>
        public static readonly uint[] Enabled = { Securing, InTerror, Sabotage, Riot, Keymaster };

        public const string Revision = "torcastra_1_16_5";

        /// <summary>The client's mission category for the Concordia Divide's instances.</summary>
        public const uint Category = 10000038;

        public const string RiotOfferKey = "torcastra.prison-riot";

        /// <summary>
        /// The client's names for map markers (missionobjectiveindicatorlanguage) that fit: a
        /// marker keyed by one carries it; the others are keyed from MissionIndicator.UnnamedFrom
        /// up, and the client names them by their objective.
        /// </summary>
        public const uint PrisonCellSwitch = 24, BaneTerminal = 71, BaneDataServers = 177, OverseerTorquaName = 392, CellBlock = 10000001;

        public static uint Unnamed(uint objective) => Rasa.Structures.MissionIndicator.UnnamedFrom + objective;

        /// <summary>How long the alarm gives before its air strike (the wiki's 20 minutes).</summary>
        public const uint AlarmSeconds = 1200;

        /// <summary>The scenario events the Game raises, by mission: (scenario, event).</summary>
        public static class Events
        {
            // 594: scenario 1, the bunkers (NE, NW, SE, SW - TorcastraPrisonContent.Bunkers' Event); scenario 2, the rest.
            public const uint Bunkers = 1, Plateau = 2;
            public const uint AlarmRaised = 1, AirStrike = 2, GateOpened = 3;

            // 397: scenario 1.
            public const uint Ferme = 1;
            public const uint GuardsCleared = 1, CellOpened = 2;

            // 398: scenario 1, the storage units A and B; 2, the second panels, both; 3, the rest.
            public const uint StorageUnits = 1, StoragePanels = 2, Data = 3;
            public const uint PanelsDestroyed = 1;
            public const uint WormUploaded = 1, PrimaryTerminal = 2, SecondaryTerminal = 3;

            // 399: scenario 1, block n unlocked is event n.
            public const uint CellBlocks = 1;

            // 356: scenario 1.
            public const uint Hamilton = 1;
            public const uint Released = 1, Delivered = 2, Killed = 3;
        }

        public static IEnumerable<string> Up()
        {
            var sql = new List<string>();

            SecuringTorcastra(sql);
            InPrisonInTerror(sql);
            InformationSabotage(sql);
            PrisonRiot(sql);
            KeymasterOfTorcastra(sql);
            StolenHistoryTorqua(sql);

            return sql;
        }

        public static IEnumerable<string> Down() => InstanceMissionSql.Remove(Revision, All);

        private static InstanceMissionSql.RewardItem[] Choice(params uint[] templates) =>
            templates.Select(template => new InstanceMissionSql.RewardItem(template, 1)).ToArray();

        private static uint[] Upto(uint count) => Enumerable.Range(1, (int)count).Select(n => (uint)n).ToArray();

        /// <summary>One event of a scenario the Game's TorcastraPrison raises.</summary>
        private static void OnEvent(InstanceMissionSql mission, uint objective, uint scenario, uint eventId, uint transition = 1) =>
            mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: scenario, transition: transition);

        /// <summary>All of events 1..<paramref name="count"/> of a scenario, in any order.</summary>
        private static void OnEvents(InstanceMissionSql mission, uint objective, uint scenario, uint count)
        {
            for (var eventId = 1U; eventId <= count; eventId++)
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: scenario, target: count, trigger: eventId);
        }

        /// <summary>Reveals and activates each of <paramref name="next"/>, from action <paramref name="firstAction"/> on.</summary>
        private static void Reveal(InstanceMissionSql mission, uint objective, uint firstAction, uint transition, params uint[] next)
        {
            foreach (var target in next)
            {
                mission.RevealAndActivate(objective, target, firstAction, transition);
                firstAction += 2;
            }
        }

        /// <summary>
        /// A mission's binding, for the bootcamp's graduates. One whose events the Game raises has
        /// scenarios, which a mission has only with a script: the data sequence, which with no
        /// sequences of its own does nothing.
        /// </summary>
        private static MissionSceneDefinition Scene(MissionSceneDefinition scene, bool events = true)
        {
            scene.Requirement ??= new CustomRequirement("character.starting-experience-completed");
            scene.Script ??= events ? "data.sequence" : null;
            return scene;
        }

        private static MissionItemBinding Dropped(string key, uint template, uint creature, uint objective) =>
            new(key, template, MissionItemScope.CharacterOwned, 1,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                Drop: new MissionItemDropDefinition(new[] { creature }, objective, Prison.Map, 100, 1));

        private static void SecuringTorcastra(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Securing, "Securing Torcastra", 1877, Prison.CiscoAtEntrance, Prison.CiscoAtGate, 15, Category);
            mission.Reward(34000, 3800, selectableItems: Choice(13302, 13388, 13025));

            mission.Objective(1, 1881, 8087, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 5061, 8088, 1, MissionObjectiveState.Incomplete);
            mission.Objective(10, 10601, 10602, 2, MissionObjectiveState.Incomplete);
            mission.Objective(11, 10765, 10766, 3, MissionObjectiveState.Incomplete);
            mission.Objective(6, 5295, 8086, 4, MissionObjectiveState.Incomplete);
            mission.Objective(9, 10552, 10553, 5, counterTextId: 10767);
            mission.Objective(3, 5064, 8091, 6);
            mission.Objective(4, 5065, 8092, 7);

            mission.Scenario(Events.Bunkers, "Torcastra bunkers secured: NE, NW, SE, SW", Upto(4));
            mission.Scenario(Events.Plateau, "Torcastra alarm raised, air strike, gate opened", Upto(3));

            foreach (var bunker in Prison.Bunkers)
            {
                mission.Transition(bunker.Objective);
                OnEvent(mission, bunker.Objective, Events.Bunkers, bunker.Event);
            }

            // Hidden: the alarm, which puts up the race to stand it down.
            mission.Transition(6);
            OnEvent(mission, 6, Events.Plateau, Events.AlarmRaised);
            Reveal(mission, 6, 1, 1, 9);

            // All four before the strike; the strike, or the time, fails it.
            mission.Transition(9);
            OnEvents(mission, 9, Events.Bunkers, 4);
            Reveal(mission, 9, 1, 1, 3);
            mission.Transition(9, 2, MissionObjectiveState.Failed, 2);
            mission.Timer(9, AlarmSeconds, 2);
            mission.Transition(9, 3, MissionObjectiveState.Failed, 3);
            OnEvent(mission, 9, Events.Plateau, Events.AirStrike, 3);

            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, Prison.GateKeyClass, target: 1);
            Reveal(mission, 3, 1, 1, 4);

            mission.Transition(4);
            OnEvent(mission, 4, Events.Plateau, Events.GateOpened);
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);

            foreach (var bunker in Prison.Bunkers)
                mission.Indicator(bunker.Objective, Unnamed(bunker.Objective), bunker.X, bunker.Y, bunker.Z, 15);
            mission.Indicator(9, Unnamed(9), -66.0, 80.0, -150.0, 140);
            mission.Indicator(3, Unnamed(3), -80.0, 117.15, -184.0, 10);
            mission.Indicator(4, Unnamed(4), -90.0, 98.35, -252.0, 6);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/594",
                "Cisco1156: bunkers NE1/NW2/SE10/SW11, hidden6 then the alarm9 (counter10767 Bane Bunkers Secured), gate key3 class21718/template42634, gate4.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Torcastra_Prison",
                "Crossing the laser trip wire at the Gateway starts 20 minutes; each bunker is secured when the Bane at it and the turret on its roof are dead, all four stand the alarm down. " +
                "Ours: the time running out, or the strike, fails 9 and so the mission; the Gatekeeper on the hill opposite the gate drops the key to whoever holds 3; the key slot beside the gate drops the field. " +
                "34000 XP and 3800 credits from the wiki; level-15 Mech vest 13302, Reflective helmet 13388, Graviton legs 13025 for its unnamed choice.");

            mission.Enable(Scene(new MissionSceneDefinition
            {
                HiddenObjectiveIds = new() { 6 },
                ExistingFactObjectiveIds = new() { 3 },
                Items = new() { Dropped("torcastra-gate-key", Prison.GateKeyTemplate, Prison.GatekeeperXsic, 3) }
            }));
        }

        private static void InPrisonInTerror(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, InTerror, "In Prison: In Terror", 563, Prison.OToole, Prison.Ferme, 15, Category);
            mission.Reward(17000, 2550, selectableItems: Choice(13197, 12859, 13045));

            mission.Objective(1, 577, 7149, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 4311, 7150, 1);

            mission.Scenario(Events.Ferme, "Ranger Ferme's guards cleared, her cell opened", Upto(2));

            mission.Transition(1);
            OnEvent(mission, 1, Events.Ferme, Events.GuardsCleared);
            Reveal(mission, 1, 1, 1, 2);

            mission.Transition(2);
            OnEvent(mission, 2, Events.Ferme, Events.CellOpened);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(2, PrisonCellSwitch, -141.0, 100.92, -552.0, 4);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/397",
                "O'Toole129 at Foreas Base sends the player for Ranger Ferme3034/package193: clear her guards1, open her cell2.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:In_Prison:_In_Terror",
                "Ferme is interrogated in the information extraction room: three Thrax interrogators8058, cleared when all are dead, then her cell console. 17000 XP / 2550 credits.");
            mission.Enable(Scene(new MissionSceneDefinition()));
        }

        private static void InformationSabotage(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Sabotage, "Information Sabotage", 567, Prison.Ferme, null, 15, Category, radioCompleteable: true);
            mission.Reward(17000, 2550, selectableItems: Choice(1159, 2907, 1339));

            mission.Objective(2, 576, 7204, 0, MissionObjectiveState.Incomplete, counterTextId: 1552);
            mission.Objective(9, 4465, 7205, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 990, 7206, 2, MissionObjectiveState.Incomplete);
            mission.Objective(8, 4351, 7207, 3);
            mission.Objective(6, 4353, 7208, 4);
            mission.Objective(7, 4354, 7209, 5);

            mission.Scenario(Events.StorageUnits, "Main storage banks A and B destroyed", Upto(2));
            mission.Scenario(Events.StoragePanels, "Both second storage panels destroyed", Upto(1));
            mission.Scenario(Events.Data, "Worm uploaded, primary and secondary terminals read", Upto(3));

            mission.Transition(2);
            OnEvents(mission, 2, Events.StorageUnits, 2);

            // Both at once: the client has no counter for these.
            mission.Transition(9);
            OnEvent(mission, 9, Events.StoragePanels, Events.PanelsDestroyed);

            mission.Transition(3);
            OnEvent(mission, 3, Events.Data, Events.WormUploaded);
            mission.Action(3, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("torcastra-upload-worm", Sabotage, "torcastra-worm", 1, MissionItemScope.AssignmentIssued));
            Reveal(mission, 3, 2, 1, 8);

            mission.Transition(8);
            mission.Conversation(8, Prison.FermePackage);
            Reveal(mission, 8, 1, 1, 6, 7);

            mission.Transition(6);
            OnEvent(mission, 6, Events.Data, Events.PrimaryTerminal);
            mission.Action(6, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Transition(7);
            OnEvent(mission, 7, Events.Data, Events.SecondaryTerminal);

            mission.Indicator(2, BaneDataServers, -16.0, 99.9, -548.0, 12);
            mission.Indicator(9, BaneDataServers, -16.0, 99.6, -548.0, 12);
            mission.Indicator(3, Unnamed(3), -16.0, 100.67, -550.4, 4);
            mission.Indicator(8, Unnamed(8), -139.5, 99.47, -556.0, 4);
            mission.Indicator(6, BaneTerminal, -16.0, 100.67, -486.3, 4);
            mission.Indicator(7, BaneTerminal, 39.96, 101.07, -627.3, 4);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/398",
                "Ferme193 gives the worm (datapad template483): the primary storage console2 (counter1552), the second panels9, the worm3, Ferme8, the primary6 and secondary7 terminals; turned in to HQ.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Information_Sabotage",
                "The data storage centre just east of Ferme: two lab machines for the banks, two console brains for the panels, datablade consoles for the worm's console and the terminals; the worm console takes nothing until all four are down. 17000 XP / 2550 credits.");

            mission.Channels(MissionChannel.Npc, MissionChannel.Radio);
            mission.Enable(Scene(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding("torcastra-worm", Prison.DatapadTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("torcastra-issue-worm", Sabotage, "torcastra-worm", Prison.DatapadTemplate, 1)
                }
            }));
        }

        private static void PrisonRiot(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Riot, "Prison Riot", 571, null, null, 15, Category, radioCompleteable: true);
            mission.Reward(17000, 2550, selectableItems: Choice(167, 1285, 601));
            mission.Prerequisite(Sabotage);

            var names = new uint[] { 4340, 4341, 4342, 4343, 4344 };
            var bodies = new uint[] { 8078, 8079, 8080, 8081, 8082 };

            mission.Scenario(Events.CellBlocks, "Cell blocks 1-5 unlocked", Upto(5));

            for (var block = 1U; block <= 5; block++)
            {
                mission.Objective(block, names[block - 1], bodies[block - 1], block - 1, MissionObjectiveState.Incomplete);
                mission.Transition(block);
                OnEvent(mission, block, Events.CellBlocks, block);

                var cells = Prison.CellBlocks[block - 1];
                mission.Indicator(block, CellBlock, cells.X, cells.FloorY, cells.Z, 6);
            }

            mission.Action(5, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/399",
                "HQ by radio: unlock cell blocks 1-5; 6 is the client's own placeholder (THIS IS HIDDEN!!) and is left out.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Prison_Riot",
                "Offered by radio once 398 is done. Each block's console is seven hex panels that blink their sequence: two that call Bane security, the ones to press, then the last. " +
                "West blocks press 1,3,5,6 then 4, never 2 or 7; the east blocks the mirror. 17000 XP / 2550 credits.");

            mission.Channels(MissionChannel.Radio, MissionChannel.Radio,
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, RiotOfferKey, Prison.Map));
            mission.Enable(Scene(new MissionSceneDefinition()));
        }

        /// <summary>The prisoner computer banks, where Hamilton is found: an object the player speaks to.</summary>
        public static readonly ScenePosition ComputerBanks = new(-129.3f, 101.3f, -456.0f);
        public const double ComputerBanksYaw = -1.597;

        private static void KeymasterOfTorcastra(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Keymaster, "Keymaster of Torcastra", 291, Prison.CiscoAtGate, Prison.Matlin, 16, Category);
            mission.Reward(34000, 2250, selectableItems: Choice(13077, 12831, 35380));
            mission.Prerequisite(Securing);

            mission.Objective(4, 10790, 10791, 0, MissionObjectiveState.Incomplete);
            mission.Objective(5, 10793, 10794, 1);
            mission.Objective(11, 10795, 10796, 2);
            mission.Objective(8, 10802, 10803, 3, required: false);
            mission.Objective(7, 10800, 10801, 4, required: false);
            mission.Objective(10, 10809, 10810, 5, required: false);
            mission.Objective(9, 10806, 10807, 6, required: false);
            mission.Objective(2, 4347, 8075, 7, required: false);
            mission.Objective(13, 12131, 12132, 8, required: false);
            mission.Objective(6, 10798, 10799, 9, MissionObjectiveState.Incomplete);

            mission.Scenario(Events.Hamilton, "Airman Hamilton released, delivered to Cisco, killed", Upto(3));

            mission.Transition(4);
            mission.Conversation(4, Prison.TerminalPackage);
            Reveal(mission, 4, 1, 1, 5);

            mission.Transition(5);
            OnEvent(mission, 5, Events.Hamilton, Events.Released);
            Reveal(mission, 5, 1, 1, 11);

            // Hamilton asks to be killed: taken in (choice 1), or not (choice 2).
            mission.Transition(11, 1, sequence: 1);
            mission.Conversation(11, Prison.HamiltonPackage, transition: 1);
            Reveal(mission, 11, 1, 1, 8);
            mission.Transition(11, 2, sequence: 2);
            mission.Conversation(11, Prison.HamiltonPackage, transition: 2);
            Reveal(mission, 11, 1, 2, 7);

            mission.Transition(8);
            OnEvent(mission, 8, Events.Hamilton, Events.Delivered);
            Reveal(mission, 8, 1, 1, 10);
            mission.Transition(10);
            mission.Conversation(10, Prison.CiscoPackage);
            Reveal(mission, 10, 1, 1, 2);
            mission.Transition(2);
            mission.Conversation(2, Prison.MatlinPackage);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Transition(7);
            OnEvent(mission, 7, Events.Hamilton, Events.Killed);
            Reveal(mission, 7, 1, 1, 9);
            mission.Transition(9);
            mission.Conversation(9, Prison.CiscoPackage);
            Reveal(mission, 9, 1, 1, 13);
            mission.Transition(13);
            mission.Conversation(13, Prison.MatlinPackage);
            mission.Action(13, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(4, Unnamed(4), ComputerBanks.X, ComputerBanks.Y, ComputerBanks.Z, 4);
            mission.Indicator(5, Unnamed(5), Prison.HamiltonX, Prison.HamiltonY, Prison.HamiltonZ, 6);
            mission.Indicator(11, Unnamed(11), Prison.HamiltonX, Prison.HamiltonY, Prison.HamiltonZ, 6);
            mission.Indicator(8, Unnamed(8), -90.0, 96.54, -268.0, 8);
            mission.Indicator(10, Unnamed(10), -90.0, 96.54, -268.0, 4);
            mission.Indicator(9, Unnamed(9), -90.0, 96.54, -268.0, 4);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/356",
                "Computer banks package1527 (Research Ward - Level 1), release5, Hamilton1526 choice: kind4 take him in, kind5 kill him; Cisco1156 objectives10/9, Matlin1214 objectives2/13; hidden6 either way.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Keymaster_of_Torcastra",
                "The Cell Block Warden9409 dead, whoever holds 5 takes his pass key (access key class25319/template97391, given by the Game's TorcastraPrison: a mission drop has to be an item objective's), which the research ward's restraints take. Taken in, Hamilton follows the player and is delivered within 8 m of Cisco at the gate; " +
                "not, he dies there. Hidden6 is either report to Matlin. 34000 XP / 2250 credits.");

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                HiddenObjectiveIds = new() { 6 },
                ObjectiveAggregations = new() { [6] = new(new uint[] { 2, 13 }, 1) },
                Dialogue = new()
                {
                    new(4, Prison.TerminalPackage, 1, transitionId: 1),
                    new(11, Prison.HamiltonPackage, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 })
                }
            };
            scene.Actors["computer-banks"] = new SceneActorDefinition("computer-banks", SceneActorKind.Object,
                Prison.CommLinkClass, ComputerBanks, ComputerBanksYaw,
                Conversation: new SceneObjectConversation(Keymaster, 4, Prison.TerminalPackage, 4));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = new List<WorldIntent> { new EnsureActorIntent("ensure-computer-banks", "computer-banks") }
            };

            mission.Enable(Scene(scene));
        }

        private static void StolenHistoryTorqua(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, StolenHistory, "Stolen History", 18949, 510104, 510104, 16, Category);
            mission.Reward(30000, 3000);

            mission.Objective(1, 19526, 19527, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 19528, 19529, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 19530, 19531, 2, MissionObjectiveState.Incomplete);

            foreach (var (objective, itemClass) in new[] { (1U, Prison.GalacticMapClass), (2U, Prison.MapCipherClass), (3U, Prison.CoordinatesClass) })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ItemAcquired, itemClass, target: 1);
            }
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(3, OverseerTorquaName, 40.0, 99.47, -652.0, 10);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1860",
                "Elder Q'uoa: the galactic map (Minos Caverns, class28655), the map cipher (Timora Mines, class28654), the rendezvous coordinates from Overseer Torqua10332 in Torcastra (class28657/template119013).");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:TorcastraMissionsV1",
                "Only Torqua's piece is placed: he drops the coordinates to whoever holds 3. Left disabled until Minos Caverns and Timora Mines give theirs.");

            mission.Enable(Scene(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 3 },
                Items = new() { Dropped("torcastra-rendezvous-coordinates", Prison.CoordinatesTemplate, Prison.OverseerTorqua, 3) }
            }, events: false), enabled: false);
        }
    }
}
