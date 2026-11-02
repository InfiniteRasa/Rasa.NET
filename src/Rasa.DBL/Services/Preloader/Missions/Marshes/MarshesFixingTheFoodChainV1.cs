using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Fixing the Food Chain (native mission 961), Valverde Marshes: Research Chemist Horsh at Falcon Hold has treated Mox
    /// feed to purge the Grubbers' toxins from the Mox, and the player puts it in the feeders round Falcon Hold.
    ///
    /// Native: texts 5221-5224 and 7003 (missionconversation): log "Distribute the 10 bags of Mox feed among the troughs
    /// located in the northern Marshes. Report back to Research Chemist Horsch in Falcon Hold on completion.", offer "You'll
    /// find troughs in the swamps all around Falcon's Hold. Place the treated Mox feed in the troughs ... hungry Mox have been
    /// known to swarm.", turn-in "You survived! ... I'm glad to see that you've restored some order to the ecology";
    /// objective 1 "Deliver modified feed to the Mox Feeders" 5225/7004 counter 5268 "Feeders filled:" (missionobjective); no
    /// objective conversation. Item Treated Grubber Meat class 21818 MisMarshesItemMoxFeed (template 44823, stack 10). Object
    /// class 21824 UsableItemAccForeanTreebackFood (entityclass: an item acceptor, augmentation 43; states 119 active, 121
    /// accepted, usabledata); usable name 168 MARSHES_MOX_TROUGHS (usablenameoverride). Map Valverde Marshes: twelve
    /// ArchForeanTreebackfeederV01 statics round Falcon Hold (NOTES-marshes-research-client.md).
    ///
    /// Sourced: TaRapedia Fixing the Food Chain: giver and receiver Research Chemist Horsh (Falcon Hold); requires Tastes Like
    /// Grubber; level 37; 32,500 XP, 3,600 credits; one of 2 Class VII EMP Bomb, 2 Adrenaline Booster, 4 Frag, 4 Cryo;
    /// feeders round Falcon Hold. TR MMO DB: level 33, 3,600 credits; "Mox Feeders are near Falcon Hold". Ten Ton Hammer
    /// Falcon Hold: 10 troughs, "many more than 10 possible spots to visit", the Mox attack you at the troughs, "choice of
    /// consumables".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Ten bags at the accept (the log's count; none without room for them), taken back at an abandon.
    ///  - One personal feed acceptor (class 21824, named "Mox Feeders") on each of the map's twelve Treeback Feeders, on its
    ///    top (the navmesh height there); any ten, each once, each takes a bag. A marker on each feeder (unnamed: an
    ///    objective keys one indicator per client name, and twelve feeders share one).
    ///  - The Mox swarm left out (no objective needs it). Level TaRapedia's. The rewards the lowest template of each client
    ///    class (Class VII EMP Bomb 26542, Class VII Adrenaline Booster 28468, Class VII Fragmentation Grenade 22963, Class VII
    ///    Cryogenic Grenade 26298) in TaRapedia's stacks, one stack to choose.
    /// </summary>
    public static class MarshesFixingTheFoodChainV1
    {
        public const uint MissionId = 961;
        public const uint Level = 37;
        public const uint Horsh = MarshesFalconHoldNpcsV1.Horsh;
        public const uint Deliver = 1;
        public const uint FeedersFilled = 1;            // the scenario: one event per feeder

        public const uint FeedTemplate = 44823, FeedClass = 21818, Bags = 10;   // Treated Grubber Meat
        public const string FeedKey = "treated-grubber-meat";
        public const uint FeederClass = 21824;          // UsableItemAccForeanTreebackFood
        public const uint FeederName = 168;             // usablenameoverride MARSHES_MOX_TROUGHS "Mox Feeders"
        public const uint ReadyState = 119, FedState = 121;
        public const uint UseArgument = 1;              // usabledata lookup[21824][0]
        public const double ObjectRadius = 8;

        public const uint Experience = 32500;
        public const uint Credits = 3600;
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint AdrenalineBooster = 118814, AdrenalineBoosters = 2;   // Class VII Adrenaline Booster
        public const uint FragGrenade = 45446, FragGrenades = 4;                // Class VII Fragmentation Grenade
        public const uint CryoGrenade = 111048, CryoGrenades = 4;               // Class VII Cryogenic Grenade

        /// <summary>The map's twelve Treeback Feeders (x, z), at the navmesh height on them (probe of 2026-11-02).</summary>
        public static readonly ScenePosition[] Feeders =
        {
            new(-148.1f, 217.88f, 751.5f), new(134.2f, 217.63f, 297.7f), new(243.7f, 217.78f, 427.2f),
            new(139.4f, 217.88f, 668f), new(-1.6f, 217.88f, 778.2f), new(-264.2f, 217.84f, 663.2f),
            new(-422.2f, 217.68f, 553.8f), new(-329.5f, 217.68f, 448.9f), new(-230.3f, 217.67f, 425.3f),
            new(-142.4f, 217.47f, 461.8f), new(-78.9f, 217.72f, 407.3f), new(-155.6f, 217.78f, 321.5f)
        };

        public static string FeederRole(int index) => $"mox-feeder-{index + 1}";
        public static uint FeederSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Fixing the Food Chain", 5221, Horsh, Horsh, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (AdrenalineBooster, AdrenalineBoosters), (FragGrenade, FragGrenades), (CryoGrenade, CryoGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(MarshesTastesLikeGrubberV1.MissionId);

            mission.Objective(Deliver, 5225, 7004, 0, MissionObjectiveState.Incomplete, counterTextId: 5268);
            mission.Scenario(FeedersFilled, "Mox feed placed", (uint)Feeders.Length);
            mission.Transition(Deliver);
            for (var id = 1U; id <= Feeders.Length; id++)
                mission.Progress(Deliver, MissionProgressEventKind.ScenarioEvent, id, Bags, counter: FeedersFilled, trigger: id);
            mission.Action(Deliver, 1, MissionActionKind.GrantReward, reward: 1);

            for (var index = 0; index < Feeders.Length; index++)
                mission.Indicator(Deliver, 1 + (uint)index, Feeders[index].X, Feeders[index].Y, Feeders[index].Z, ObjectRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/961",
                "Native1 'Deliver modified feed to the Mox Feeders' 5225/7004 counter5268 'Feeders filled:'; no " +
                "objectiveconversation. Texts 5221-5224, 7003 (log: 10 bags, troughs in the northern Marshes, Horsch). Item " +
                "class21818 Treated Grubber Meat (template44823). Object class21824 UsableItemAccForeanTreebackFood (acceptor, " +
                "states 119/121), usable name168 MARSHES_MOX_TROUGHS. Map: 12 ArchForeanTreebackfeederV01 round Falcon Hold.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Fixing_the_Food_Chain",
                "Giver/receiver Research Chemist Horsh (Falcon Hold); requires Tastes Like Grubber; level37; 32500XP, 3600 credits; " +
                "one of 2 Class VII EMP Bomb, 2 Adrenaline Booster, 4 Frag, 4 Cryo. TR MMO DB level33, feeders near Falcon Hold. " +
                "Ten Ton Hammer: 10 troughs, many more than 10 spots, Mox attack at the troughs, choice of consumables.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesFixingTheFoodChainV1",
                "10 bags at accept (taken back at an abandon). 12 personal acceptors 21824 on the 12 map feeders, any 10, each " +
                "once, each takes a bag; a marker on each. Mox swarm left out. Items 111232x2, 118814x2, 45446x4, 111048x4 " +
                "(lowest template of each class), one stack to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(FeedKey, FeedTemplate, MissionItemScope.AssignmentIssued, Bags,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-mox-feed", MissionId, FeedKey, FeedTemplate, Bags) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Feeders.Length; index++)
            {
                var role = FeederRole(index);
                var sequence = FeederSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, FeederClass, Feeders[index],
                    InitialObjectState: ReadyState, NameOverrideId: FeederName,
                    UseAction: new SceneObjectAction(MissionId, Deliver, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"feed-{role}", role, FedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-feed", MissionId, FeedKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, FeedersFilled, (uint)index + 1) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
