using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Fixing the Food Chain (native mission 961), Valverde Marshes, V2 (to-fix row 19: each acceptor at its trough's origin and
    /// turn, <see cref="Feeders"/>; otherwise as V1, which keeps the mission as Marshes_falcon_hold applied it): Research Chemist Horsh at Falcon Hold has treated Mox
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
    public static class MarshesFixingTheFoodChainV2
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

        /// <summary>
        /// The map's twelve Treeback Feeders (to-fix row 19, 2026-11-03): each acceptor at its trough's own origin, the client
        /// map's ArchForeanTreebackfeederV01 static (class 24979; adv_foreas_valverde_marshes.map, tools/mission-crossref/
        /// mapfile.py), so that its food mesh lies in the trough. Why: the acceptor's mesh (arch_forean_treebackfeeder_food_*_v01.geo,
        /// render box x -2.6..2.7, y 0.37..1.05, z -0.9..0.9 about its origin) is the trough's basin fill: the trough mesh
        /// (arch_forean_treebackfeeder_v01.geo) has its basin box at x +-2.7, z +-1, floor y 0.53, rim y 0.76 about the same
        /// origin. V1 stood each one at the navmesh height on the trough (about 1.1 m above the origin) with no turn, so the
        /// food floated over the trough (the tester's real-client screenshots, 961-feeder-with.png and -hidden.png).
        /// </summary>
        public static readonly ScenePosition[] Feeders =
        {
            new(-148.085f, 216.743f, 751.467f), new(134.203f, 216.841f, 297.739f), new(243.742f, 216.764f, 427.162f),
            new(139.42f, 216.848f, 667.99f), new(-1.555f, 216.838f, 778.228f), new(-264.19f, 216.733f, 663.226f),
            new(-422.163f, 216.92f, 553.75f), new(-329.471f, 216.887f, 448.892f), new(-230.289f, 216.834f, 425.301f),
            new(-142.384f, 216.765f, 461.821f), new(-78.898f, 216.746f, 407.304f), new(-155.599f, 216.846f, 321.474f)
        };

        /// <summary>
        /// Each trough's turn about the vertical (radians, 2 atan2(qy, qw) of the static's rotation; no tilt), as the server
        /// sends a scene object's Orientation (Quaternion.CreateFromYawPitchRoll, WorldLocationDescriptorPacket).
        /// </summary>
        public static readonly double[] FeederYaws =
        {
            0, 0, 0,
            0.9364, 0, 0.6157,
            2.0057, -0.2139, 2.9254,
            0, 2.045, 0
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
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesFixingTheFoodChainV2",
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
                    Orientation: FeederYaws[index], InitialObjectState: ReadyState, NameOverrideId: FeederName,
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
