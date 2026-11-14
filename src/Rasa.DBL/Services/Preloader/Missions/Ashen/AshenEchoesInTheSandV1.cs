using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Echoes in the Sand (native mission 1736), Ligo Ashen Desert, the Crucible's It Came From Erdas (1218) followed up: Captain
    /// Amandla Botha at Ashoka sends the player with a joint research team, Kapa Grupa Lohen's representative and an AFS
    /// researcher, to the Eloh crash site at Daedalus' Landing, which the Bane secured hours before; an Eloh obelisk there gives
    /// the player a vision, and the team comes back to Botha with traces of the Atta Queen's pheromone.
    ///
    /// Native: texts 16665-16668, 16669 (missionconversation): log "Escort the collaborative research team into Daedalus' Landing
    /// and recover information from the crash site. ...", offer "I need you to protect the research team ... Your biggest problem
    /// is the Bane, who secured the crash site just a few hours ago. ... bring the team back here", turn-in "Looks like the
    /// research team also found traces of a potent Atta pheromone ... scout out Avernus Atta Colony."; objectives
    /// (missionobjective) 1 "Escort Researchers to Daedalus' Landing" 16970/16971 ("Protect Researcher Jackson and Researcher
    /// Yorish ... If either researcher is killed, then you will fail your mission."), 2 "Escort Researchers back to Ashoka"
    /// 16972/16973, 3 "Syndicate Researcher" 16974/16975, 4 "AFS Researcher" 16976/16977, 5 "Ambushes" 17013/17014; no counters;
    /// objective conversations 1736,1 package 2066 (the obelisk) 16978, 1736,2 package 1410 (Botha) 16979 (variant 1 each). Names
    /// 9278 Botha, 10060 Researcher Yorish, 10061 Researcher Jackson. The client map: the region Daedalus' Landing (134, 285,
    /// -164) r179 and the Eloh Reliquary Escape Pod there (ArchElohReliquaryEscapePodv01, map statics); no obelisk at the pod.
    ///
    /// Sourced: TaRapedia Echoes in the Sand: giver and reward giver Captain Amandla Botha (Ashoka); requires It Came From Erdas;
    /// 190,000 XP, 8,400 credits; Vextronics Laser Pistol, Laser Rifle, Photonic Shotgun, Laser Chaingun; the obelisk's vision,
    /// then "the researchers have completed their scans of the area and are waiting for you".
    ///
    /// Revision crucible_1_16_5 (CrucibleMissionDataV1), as It Came From Erdas (1218) before it: a required chain stays on one
    /// revision (MissionContentValidator, "cross-revision-reference"); its category and map are Ashen Desert's.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (triage D5; TaRapedia's XP is 1281's, level 42).
    ///  - The two researchers (AshenAshokaNpcsV1) the run's own, by Botha at the accept, following the player at once, side by
    ///    side (triage M6: both at once; a Marshes escort leg runs one group at a time, so this is data.sequence with the
    ///    engine's follow, as the Crucible's Control the Brain). Yorish is 3 ("Syndicate Researcher"), Jackson 4 ("AFS
    ///    Researcher"): the two hidden, required helpers, open from the accept, done when 2 is; either's death (its defeat
    ///    sequence) fails its helper and with it the mission (the log's "If either researcher is killed"), and takes the other,
    ///    the obelisk and the Bane away.
    ///  - 5 "Ambushes" a hidden helper (not required): the player at the landing (ArrivalRadius round the obelisk) brings the
    ///    first ambush, two of the Ashen Thrax Soldiers (531008, as set 9's tower ambushes) 69-73 m back on the way in (each
    ///    spot seen from all 16 places round it: the rocks by (220, -240) hid two), who attack the player.
    ///  - 6 a server-only hidden helper (not required; it carries 5's texts, the client has no objective 6): it counts the
    ///    first pair's deaths (each one's defeat sequence signals scenario 4) to 2, then brings the obelisk (the client's
    ///    NPCElohObeliskV01 30138, an NPC-augmented object; package 2066). So the talk, and with it the second ambush, waits
    ///    until both of the first are dead: never four Thrax at once (to-fix row 26: three Atta Soldiers killed a solo
    ///    player).
    ///  - 1 the obelisk's talk (16978's vision) opens 2 and brings the second ambush, two more 68-72 m back on the way home.
    ///    2 the talk with Botha (16979); the researchers go in. Ambushes of two, the player their target: a solo level 50
    ///    player wins them (to-fix row 26: three at once did not).
    ///  - Places (probe of 2026-11-11): the obelisk at (188, -200) in Daedalus' Landing, 50 m outside the circle of the Tree
    ///    Mite pool 540072 that stands on the pod (upstream; not moved) and 34 m or more outside every other hostile pool's; the
    ///    way from Botha (the navmesh's, by (258, -299) and (220, -240)) on the client's floor both ways, 30 m or more off every
    ///    hostile circle.
    ///  - Markers: 1 on the obelisk (r20, the arrival), 2 at Botha.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 (client entityclass names): Pistol Laser
    ///    14492 (40-44), Rifle Laser 11947 (41-45), Shotgun Laser 11983 (42-46, "Photonic"), Machine Gun Laser 45762 (38-42,
    ///    "Chaingun"); one to choose.
    /// </summary>
    public static class AshenEchoesInTheSandV1
    {
        public const uint MissionId = 1736;
        public const uint Level = 42;
        public const uint Botha = AshenSupplyLogisticsV1.Botha, BothaPackage = AshenSupplyLogisticsV1.BothaPackage;
        public const uint ItCameFromErdas = 1218;
        public const string Revision = CrucibleMissionDataV1.Revision;
        public const uint ToTheLanding = 1, BackToAshoka = 2, Syndicate = 3, Afs = 4, Ambushes = 5, FirstPairDown = 6;
        public const uint ArrivalArea = 1;
        public const uint Variant = 1;
        public const double ArrivalRadius = 20, BothaRadius = 8;

        /// <summary>Scenarios / sequences: the landing reached, the obelisk's talk, Botha's talk; the researchers' deaths.</summary>
        public const uint Arrived = 1, Leaving = 2, Home = 3;
        /// <summary>The scenario 6 counts (step K: the first ambush's member K dead) and the one that brings the obelisk.</summary>
        public const uint FirstPairCounted = 4, ObeliskComes = 5;
        public const uint YorishLost = 101, JacksonLost = 102;
        /// <summary>The first ambush's members' defeat sequences (member K: FirstPairLost + K).</summary>
        public const uint FirstPairLost = 110;

        public const uint ObeliskClass = 30138;          // NPCElohObeliskV01 (aug 52, the NPC augmentation)
        public const uint ObeliskPackage = 2066;         // objectiveconversation 1736,1
        public const uint Soldier = AshenOnTheSameFrequencyV1.Soldier;  // Thrax Soldier - Ashen Desert 531008
        public const string YorishRole = "researcher-yorish", JacksonRole = "researcher-jackson", ObeliskRole = "daedalus-obelisk";

        public static readonly ScenePosition YorishAt = new(381f, 280.45f, -461f);
        public static readonly ScenePosition JacksonAt = new(383f, 280.09f, -461f);
        public static readonly ScenePosition ObeliskAt = new(188f, 298.03f, -200f);
        public static readonly ScenePosition BothaAt = new(378f, 280.69f, -458.5f);

        /// <summary>The first ambush (at the landing) and the second (at the obelisk's talk), each two Thrax Soldiers back on the way.</summary>
        public static readonly ScenePosition[] FirstAmbushAt = { new(230f, 310.97f, -255f), new(232f, 311.83f, -258f) };
        public static readonly ScenePosition[] SecondAmbushAt = { new(235f, 310.61f, -250f), new(237f, 311.09f, -253f) };

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint LaserPistol = 14492;          // Weapon_Avatar_Pistol_Laser_UNC_40_to_44
        public const uint LaserRifle = 11947;           // Weapon_Avatar_Rifle_Laser_UNC_41_to_45
        public const uint PhotonicShotgun = 11983;      // Weapon_Avatar_Shotgun_v1_Laser_UNC_42_to_46
        public const uint LaserChaingun = 45762;        // Weapon_Avatar_MachineGun_Laser_UNC_38_to_42

        public static string AmbushRole(int ambush, int member) => $"daedalus-ambush-{ambush}-{member}";

        public static void Up(MigrationBuilder migration)
        {
            // The Crucible's revision (the chain it ends: 1218), the Ashen category and map (as Marshes It Lies in Ruins 1673).
            var mission = new AshenMissionDataV1(migration, MissionId, Revision, "Echoes in the Sand", 16665, Botha, Botha, Level);
            mission.Reward(Experience, Credits, new[] { LaserPistol, LaserRifle, PhotonicShotgun, LaserChaingun });
            mission.Prerequisite(ItCameFromErdas);
            mission.Scenario(Arrived, "Daedalus' Landing reached: the first ambush", 0);
            mission.Scenario(Leaving, "The obelisk's vision: the second ambush", 0);
            mission.Scenario(Home, "Back at Botha: the researchers go in", 0);
            mission.Scenario(FirstPairCounted, "The first ambush dead", (uint)FirstAmbushAt.Length);
            mission.Scenario(ObeliskComes, "The first ambush dead: the obelisk", 0);

            mission.Objective(ToTheLanding, 16970, 16971, 0, MissionObjectiveState.Incomplete);
            mission.Objective(BackToAshoka, 16972, 16973, 1, MissionObjectiveState.Inactive);
            mission.Objective(Syndicate, 16974, 16975, 2, MissionObjectiveState.Incomplete);     // hidden
            mission.Objective(Afs, 16976, 16977, 3, MissionObjectiveState.Incomplete);           // hidden
            mission.Objective(Ambushes, 17013, 17014, 4, MissionObjectiveState.Incomplete, required: false);   // hidden
            mission.Objective(FirstPairDown, 17013, 17014, 5, MissionObjectiveState.Incomplete, required: false);   // hidden, server-only

            // 5, hidden: the player at the landing brings the first ambush.
            mission.Area(ArrivalArea, AshenMissionDataV1.MapContextId, ObeliskAt.X, ObeliskAt.Y, ObeliskAt.Z, ArrivalRadius);
            mission.Transition(Ambushes);
            mission.EnterArea(Ambushes, ArrivalArea);
            mission.Action(Ambushes, 1, MissionActionKind.StartScenario, scenario: Arrived);

            // 6, hidden: both of the first ambush dead bring the obelisk.
            mission.Transition(FirstPairDown);
            for (var member = 1U; member <= FirstAmbushAt.Length; member++)
                mission.Progress(FirstPairDown, MissionProgressEventKind.ScenarioEvent, member, (uint)FirstAmbushAt.Length, counter: FirstPairCounted, trigger: member);
            mission.Action(FirstPairDown, 1, MissionActionKind.StartScenario, scenario: ObeliskComes);

            // 1: the obelisk's talk opens 2 and brings the second ambush.
            mission.Transition(ToTheLanding);
            mission.Conversation(ToTheLanding, ObeliskPackage, Variant);
            mission.Action(ToTheLanding, 1, MissionActionKind.RevealObjective, targetObjective: BackToAshoka);
            mission.Action(ToTheLanding, 2, MissionActionKind.ActivateObjective, targetObjective: BackToAshoka, state: MissionObjectiveState.Incomplete);
            mission.Action(ToTheLanding, 3, MissionActionKind.StartScenario, scenario: Leaving);

            // 2: Botha's talk; the researchers go in.
            mission.Transition(BackToAshoka);
            mission.Conversation(BackToAshoka, BothaPackage, Variant);
            mission.Action(BackToAshoka, 1, MissionActionKind.StartScenario, scenario: Home);
            mission.Action(BackToAshoka, 2, MissionActionKind.GrantReward, reward: 1);

            // 3, 4, hidden: each researcher brought back (2 done); a death fails them (the scene).
            foreach (var helper in new[] { Syndicate, Afs })
            {
                mission.Transition(helper);
                mission.ObjectiveState(helper, BackToAshoka, MissionObjectiveState.Completed);
            }

            mission.Indicator(ToTheLanding, 1, ObeliskAt.X, ObeliskAt.Y, ObeliskAt.Z, ArrivalRadius);
            mission.Indicator(BackToAshoka, 2, BothaAt.X, BothaAt.Y, BothaAt.Z, BothaRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1736",
                "Native1 'Escort Researchers to Daedalus' Landing' 16970/16971 (either researcher killed fails), 2 'Escort " +
                "Researchers back to Ashoka' 16972/16973, 3 'Syndicate Researcher' 16974/16975, 4 'AFS Researcher' 16976/16977, 5 " +
                "'Ambushes' 17013/17014, no counters; objectiveconversation 1736,1 pkg2066 16978 (the obelisk), 1736,2 pkg1410 16979 " +
                "(Botha). Texts 16665-16669. Names 9278, 10060 Yorish, 10061 Jackson. Region Daedalus' Landing (134, 285, -164) r179, " +
                "the Eloh Reliquary Escape Pod there.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Echoes_in_the_Sand",
                "Giver/reward giver Captain Amandla Botha (Ashoka); requires It Came From Erdas; 190000XP, 8400 credits; Vextronics " +
                "Laser Pistol, Laser Rifle, Photonic Shotgun, Laser Chaingun; the obelisk's vision, the researchers' scans, back to " +
                "Botha.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenEchoesInTheSandV1",
                "Level42 estimated. Yorish 790521 (3) and Jackson 790522 (4) the run's own, following from Botha at once; either's " +
                "death fails its hidden helper and the mission. Hidden 5: the landing (r20 at 188, -200) brings two Thrax Soldiers " +
                "531008; hidden server-only 6 counts both dead, then brings the obelisk 30138 (pkg2066); the obelisk's talk (1) " +
                "opens 2 and brings two more; Botha's talk (2) " +
                "takes the researchers in, 3 and 4 follow it. Markers on the obelisk and Botha. Items 14492, 11947, 11983, 45762, " +
                "one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The researchers, the obelisk and the two ambushes.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Syndicate, Afs, Ambushes, FirstPairDown },
                // The obelisk's talk: an object's topic (as Timora's Eloh hologram, the Crucible's terminals).
                Dialogue = new() { new(ToTheLanding, ObeliskPackage, Variant, transitionId: 1) },
                DefeatSequences = new()
            };
            scene.Actors[YorishRole] = new SceneActorDefinition(YorishRole, SceneActorKind.Creature, AshenAshokaNpcsV1.Yorish, YorishAt,
                FollowOffset: new ScenePosition(-1.5f, 0f, -2f));
            scene.Actors[JacksonRole] = new SceneActorDefinition(JacksonRole, SceneActorKind.Creature, AshenAshokaNpcsV1.Jackson, JacksonAt,
                FollowOffset: new ScenePosition(1.5f, 0f, -2f));
            scene.Actors[ObeliskRole] = new SceneActorDefinition(ObeliskRole, SceneActorKind.Object, ObeliskClass, ObeliskAt,
                Conversation: new SceneObjectConversation(MissionId, ToTheLanding, ObeliskPackage, ToTheLanding, Variant));

            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("yorish-comes", YorishRole),
                    new EnsureActorIntent("jackson-comes", JacksonRole),
                    new SetInteractionIntent("yorish-escorted", YorishRole, false),
                    new SetInteractionIntent("jackson-escorted", JacksonRole, false),
                    new FollowActorIntent("yorish-follows", YorishRole, 0, Enabled: true),
                    new FollowActorIntent("jackson-follows", JacksonRole, 0, Enabled: true)
                }
            };

            var arrived = new SceneSequenceDefinition();
            scene.Sequences[ObeliskComes] = new SceneSequenceDefinition { World = { new EnsureActorIntent("obelisk-comes", ObeliskRole) } };
            var leaving = new SceneSequenceDefinition();
            foreach (var (ambush, places, sequence) in new[] { (1, FirstAmbushAt, arrived), (2, SecondAmbushAt, leaving) })
                for (var member = 0; member < places.Length; member++)
                {
                    var role = AmbushRole(ambush, member + 1);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Soldier, places[member],
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    sequence.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    sequence.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role, holdTarget: true));
                    if (ambush == 1)
                    {
                        // Its death counts for 6 (the obelisk once both are dead).
                        var lost = FirstPairLost + (uint)member + 1;
                        scene.DefeatSequences[role] = lost;
                        scene.Sequences[lost] = new SceneSequenceDefinition
                        {
                            Signals = { new SceneMissionSignal(MissionId, FirstPairCounted, (uint)member + 1) }
                        };
                    }
                }
            scene.Sequences[Arrived] = arrived;
            scene.Sequences[Leaving] = leaving;
            scene.Sequences[Home] = new SceneSequenceDefinition
            {
                World = { new RemoveActorIntent("yorish-goes-in", YorishRole), new RemoveActorIntent("jackson-goes-in", JacksonRole) }
            };

            // A researcher's death: its helper fails (and the mission with it); the other, the obelisk and the Bane go away (a
            // removal of an absent actor is applied as done: SceneWorldAdapter).
            foreach (var (role, helper, sequence) in new[] { (YorishRole, Syndicate, YorishLost), (JacksonRole, Afs, JacksonLost) })
            {
                scene.DefeatSequences[role] = sequence;
                var lost = new SceneSequenceDefinition
                {
                    Character = { new ObjectiveIntent($"{role}-lost", MissionId, helper, MissionObjectiveState.Failed) }
                };
                foreach (var other in new[] { YorishRole, JacksonRole, ObeliskRole })
                    lost.World.Add(new RemoveActorIntent($"{role}-lost-{other}-gone", other));
                for (var ambush = 1; ambush <= 2; ambush++)
                    for (var member = 1; member <= 2; member++)
                        lost.World.Add(new RemoveActorIntent($"{role}-lost-{AmbushRole(ambush, member)}-gone", AmbushRole(ambush, member)));
                scene.Sequences[sequence] = lost;
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
