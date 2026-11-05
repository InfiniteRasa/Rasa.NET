using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// A Taste of Their Own Medicine (native mission 546): Sergeant Fitz in Paludos sends the player into the Logos Research
    /// Facility (squad instance 1700) to Specialist Hax, who wants the Bane security room cleared and then hacks the Bane
    /// computers while the player holds the room.
    ///
    /// Native: texts 1474-1477 and 7155 (missionconversation): log "Rendezvous with Specialist Hax in the Logos Research
    /// Facility and help him hack into the Bane security system.", turn-in (Hax) "I've managed to uplink a feed to HQ";
    /// objectives (missionobjective, no counters) 5 "Rendezvous With Specialist Hax in Logos Research Facility" 6244/7156, 1
    /// "Secure the Bane Security Room for Specialist Hax." 3423/7157, 2 "Protect Specialist Hax While He Infiltrates the Bane
    /// Computer System." 4706/7158, 3 "HIDDEN!" 4707/7159, 4 "HIDDEN!!" 4708/7160; objective conversation 546,5 package 960
    /// 6245 (Hax: "There's just too many for me and this newbie medic to take on. Let's get in there and secure the area.
    /// Then I can get to work on these Bane computers while you keep an eye out."). Item block 120017-120020.
    ///
    /// Sourced: TaRapedia A Taste of Their Own Medicine: giver Sergeant Fitz (Temple of Paludos), receiver Specialist Hax
    /// (Logos Research Facility); 70,000 XP, 5,550 credits; one of ChiTech Bio Armor Gloves, Teleract Graviton Armor Helmet,
    /// Dynamo Mech Armor Gloves, Astra Stealth Armor Gloves (level 35); follow-ups Freedom's Just Another Word and Slay On
    /// Target. TR MMO DB: level 33. The client's item block 120017-120020 (Bio gloves, Graviton helmet, Mech gloves, Stealth
    /// gloves 35-39: TaRapedia's four in order; the client has two more such blocks, 119386 and 120454, ESTIMATE which is
    /// this one's). LRF map_region 20052 "Security Checkpoint" (760.2, 320.2, -136, radius 25.5), its Bane table (760.2,
    /// 320.8, -136). World DB: the Marshes Thrax Soldier 531045 and Grenadier 531046 (the facility's own pools' rows).
    ///
    /// Reconstructed (ESTIMATE, the triage's choices of 2026-11-02):
    ///  - Played in the squad's facility (BoundMapContextId): taken at Fitz, its scene waits for the player to come in.
    ///  - 5: the talk with the public Hax by the hospital. It opens 1, and six Thrax (the facility's soldiers and
    ///    grenadiers, the run's own) stand in the Security Checkpoint; hidden 3 counts their six kills, 3 done completes 1.
    ///  - 2: Hax (the same creature, the player's own, OwnerOnly; the public one is hidden from the player while he stands)
    ///    and the newbie medic come to the room's west side; three waves of four Thrax come down the tunnels at 10, 50 and 90
    ///    s and attack them; after 120 s (the triage's 2 minutes) the hack is done: what is left of the waves goes, hidden 4
    ///    completes, and 4 done completes 2. Hax's death in the hack fails 2 (and the mission); the medic's changes nothing.
    ///    Hax stays in the room as the receiver; the turn-in ends the scene, and the public Hax is back.
    ///  - Markers: 5 at Hax (in the facility) and at the facility's door on the Marshes, 1 round the room, 2 at Hax's place
    ///    in it.
    /// </summary>
    public static class MarshesTasteOfTheirOwnMedicineV1
    {
        public const uint MissionId = 546;
        public const uint Level = 33;
        public const uint Fitz = MarshesLogosResearchNpcsV1.Fitz, Hax = MarshesLogosResearchNpcsV1.Hax;
        public const uint Facility = MarshesLogosResearchNpcsV1.MapContextId;
        public const uint Rendezvous = 5, Secure = 1, Protect = 2, RoomTally = 3, Hack = 4;

        /// <summary>Scenarios: the room's Thrax come, the hack starts (their sequences); the kills, the hack's end, Hax's death.</summary>
        public const uint RoomComes = 1, HackStarts = 2, RoomDown = 3, HackDone = 4, HaxDown = 5;
        public const uint WaveSequence = 20, HackDoneSequence = 30, HaxDownSequence = 31;
        public const uint HackMilliseconds = 120000;
        public const string HaxRole = "hax", MedicRole = "newbie-medic";

        public const uint Soldier = 531045, Grenadier = 531046;

        /// <summary>The facility's door on the Marshes (map_link 43), the room, Hax's and the medic's places in it.</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (-318.36, 285.71, -497.8);
        public static readonly (double X, double Y, double Z) RoomAt = (760.2, 320.2, -136);
        public const double RoomRadius = 25;
        public static readonly ScenePosition HaxAt = new(748f, 319.78f, -130f);
        public static readonly ScenePosition MedicAt = new(750f, 319.77f, -142f);
        public const double HaxYaw = -1.5708;           // facing east, to the Bane table

        /// <summary>The room's six Thrax.</summary>
        public static readonly (uint Creature, ScenePosition At)[] Room =
        {
            (Soldier, new ScenePosition(764f, 320.04f, -130f)), (Soldier, new ScenePosition(768f, 319.83f, -144f)),
            (Grenadier, new ScenePosition(772f, 319.88f, -136f)), (Soldier, new ScenePosition(756f, 319.77f, -128f)),
            (Grenadier, new ScenePosition(760f, 320.07f, -120f)), (Soldier, new ScenePosition(760f, 319.97f, -150f)),
        };

        /// <summary>
        /// The waves: when (ms after the hack starts) and who comes from where (the north and south tunnels). Each on the
        /// tunnels' client floor (to-fix 31): the tunnels' floor at the checkpoint's level is 6-8 m wide (x 756-764), and wave
        /// 1's first three and wave 2's first stood at its sides, where the client has no floor at that level (the server
        /// navmesh has); they now stand on it (MarshesClientFloorTests).
        /// </summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (10000, new[] { (Soldier, new ScenePosition(758f, 321.93f, -100f)), (Soldier, new ScenePosition(762f, 321.93f, -100f)),
                (Grenadier, new ScenePosition(756f, 321.83f, -94f)), (Grenadier, new ScenePosition(760f, 321.77f, -96f)) }),
            (50000, new[] { (Soldier, new ScenePosition(760f, 321.73f, -178f)), (Soldier, new ScenePosition(756f, 321.77f, -180f)),
                (Grenadier, new ScenePosition(764f, 321.77f, -180f)), (Grenadier, new ScenePosition(760f, 321.77f, -184f)) }),
            (90000, new[] { (Soldier, new ScenePosition(756f, 321.77f, -104f)), (Grenadier, new ScenePosition(764f, 321.77f, -104f)),
                (Soldier, new ScenePosition(756f, 321.77f, -176f)), (Grenadier, new ScenePosition(764f, 321.77f, -176f)) })
        };

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public static readonly uint[] Rewards = { 120017, 120018, 120019, 120020 };

        public static string RoomRole(int index) => $"room-thrax-{index + 1}";
        public static string WaveRole(int wave, int member) => $"wave-{wave + 1}-{member + 1}";
        public static string WaveTimer(int wave) => $"wave-{wave + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "A Taste of Their Own Medicine", 1474, Fitz, Hax, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Scenario(RoomComes, "Hax met: the security room's Thrax", 0);
            mission.Scenario(HackStarts, "The room secured: Hax hacks the Bane computers", 0);
            mission.Scenario(RoomDown, "The security room's Thrax killed", (uint)Room.Length);
            mission.Scenario(HackDone, "The hack done", 1);
            mission.Scenario(HaxDown, "Hax killed in the hack", 1);

            mission.Objective(Rendezvous, 6244, 7156, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Secure, 3423, 7157, 1, MissionObjectiveState.Inactive);
            mission.Objective(Protect, 4706, 7158, 2, MissionObjectiveState.Inactive);
            mission.Objective(RoomTally, 4707, 7159, 3, MissionObjectiveState.Incomplete, required: false);   // hidden
            mission.Objective(Hack, 4708, 7160, 4, MissionObjectiveState.Incomplete, required: false);        // hidden

            mission.Transition(Rendezvous);
            mission.Conversation(Rendezvous, MarshesLogosResearchNpcsV1.HaxPackage);
            Open(mission, Rendezvous, 1, Secure);
            mission.Action(Rendezvous, 3, MissionActionKind.StartScenario, scenario: RoomComes);

            mission.Transition(RoomTally);
            for (var kill = 1U; kill <= Room.Length; kill++)
                mission.Progress(RoomTally, MissionProgressEventKind.ScenarioEvent, kill, (uint)Room.Length, counter: RoomDown, trigger: kill);
            mission.Transition(Secure);
            mission.ObjectiveState(Secure, RoomTally, MissionObjectiveState.Completed);
            Open(mission, Secure, 1, Protect);
            mission.Action(Secure, 3, MissionActionKind.StartScenario, scenario: HackStarts);

            mission.Transition(Hack);
            mission.Progress(Hack, MissionProgressEventKind.ScenarioEvent, 1, null, counter: HackDone);
            // 2: done with the hack (hidden 4), failed by Hax's death in it.
            mission.Transition(Protect);
            mission.ObjectiveState(Protect, Hack, MissionObjectiveState.Completed);
            mission.Action(Protect, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Protect, 2, 2, MissionObjectiveState.Failed);
            mission.Progress(Protect, 2, MissionProgressEventKind.ScenarioEvent, 1, null, counter: HaxDown);

            var hax = MarshesLogosResearchNpcsV1.HaxAt;
            mission.Indicator(Rendezvous, 1, hax.X, hax.Y, hax.Z, map: Facility);
            mission.Indicator(Rendezvous, 2, DoorAt.X, DoorAt.Y, DoorAt.Z, 10);
            mission.Indicator(Secure, 3, RoomAt.X, RoomAt.Y, RoomAt.Z, RoomRadius, Facility);
            mission.Indicator(Protect, 4, HaxAt.X, HaxAt.Y, HaxAt.Z, 12, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/546",
                "Native5 'Rendezvous With Specialist Hax in Logos Research Facility' 6244/7156, 1 'Secure the Bane Security Room for " +
                "Specialist Hax.' 3423/7157, 2 'Protect Specialist Hax While He Infiltrates the Bane Computer System.' 4706/7158, 3 " +
                "'HIDDEN!' 4707/7159, 4 'HIDDEN!!' 4708/7160; no counters. objectiveconversation 546,5 pkg960 6245 (Hax, 'this newbie " +
                "medic'). Texts 1474-1477, 7155. Names 8562 Hax, 8721 Field Medic. Item block 120017-120020. LRF region 20052 " +
                "'Security Checkpoint' (760.2,320.2,-136).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Taste_of_Their_Own_Medicine",
                "Giver Sergeant Fitz (Temple of Paludos), receiver Specialist Hax (Logos Research Facility); 70000XP, 5550 credits; " +
                "ChiTech Bio Armor Gloves/Teleract Graviton Armor Helmet/Dynamo Mech Armor Gloves/Astra Stealth Armor Gloves lvl35; " +
                "follow-ups 548, 718. TR MMO DB level33.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTasteOfTheirOwnMedicineV1",
                "Bound to the squad's LRF. Hax 760362 public by the hospital (814,321.77,-110); his talk brings six Thrax " +
                "531045/531046 into the Security Checkpoint, hidden 3 counts them, 1 completes. Then Hax (OwnerOnly, the public one " +
                "hidden) and the newbie medic 760364 at the room's west side; waves of 4 at 10/50/90 s; 120 s hack, hidden 4, 2 " +
                "completes; Hax's death in it fails 2. Places, counts and times are estimates.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The run's own Thrax, Hax and the medic, on the squad's copy of the facility.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,   // taken at Fitz on the Marshes, played in the squad's facility
                MapContextId = Facility,
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { RoomTally, Hack }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            var room = new SceneSequenceDefinition();
            for (var index = 0; index < Room.Length; index++)
            {
                var role = RoomRole(index);
                var defeated = 11U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Room[index].Creature, Room[index].At,
                    Orientation: 1.5708, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                room.World.Add(new EnsureActorIntent($"{role}-comes", role));
                scene.DefeatSequences[role] = defeated;
                scene.Sequences[defeated] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, RoomDown, (uint)index + 1) } };
            }
            scene.Sequences[RoomComes] = room;

            scene.Actors[HaxRole] = new SceneActorDefinition(HaxRole, SceneActorKind.Creature, Hax, HaxAt, Orientation: HaxYaw,
                OwnerOnly: true, ReplacesPublicSpawnId: MarshesLogosResearchNpcsV1.Hax, ReplaceWhilePresent: true);
            scene.Actors[MedicRole] = new SceneActorDefinition(MedicRole, SceneActorKind.Creature, MarshesLogosResearchNpcsV1.Medic,
                MedicAt, Orientation: HaxYaw, InitiallyInteractable: false);
            var hack = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("hax-comes", HaxRole), new EnsureActorIntent("medic-comes", MedicRole) },
                Timers = { new SequenceTimer("hack", HackMilliseconds, HackDoneSequence) }
            };
            var done = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, HackDone, 1) } };
            var lost = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, HaxDown, 1) },
                Timers = { new SequenceTimer("hack", 0, HackDoneSequence, Cancel: true) }
            };
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                hack.Timers.Add(new SequenceTimer(WaveTimer(wave), Waves[wave].After, WaveSequence + (uint)wave));
                lost.Timers.Add(new SequenceTimer(WaveTimer(wave), 0, WaveSequence + (uint)wave, Cancel: true));
                var comes = new SceneSequenceDefinition();
                for (var member = 0; member < Waves[wave].Members.Length; member++)
                {
                    var role = WaveRole(wave, member);
                    var (creature, at) = Waves[wave].Members[member];
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    comes.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    comes.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role, member % 2 == 0 ? HaxRole : MedicRole));
                    done.World.Add(new RemoveActorIntent($"{role}-goes", role));
                    lost.World.Add(new RemoveActorIntent($"{role}-goes-after-hax", role));
                }
                scene.Sequences[WaveSequence + (uint)wave] = comes;
            }
            done.World.Add(new RemoveActorIntent("medic-goes", MedicRole));
            lost.World.Add(new RemoveActorIntent("medic-goes-after-hax", MedicRole));
            scene.Sequences[HackStarts] = hack;
            scene.Sequences[HackDoneSequence] = done;
            scene.DefeatSequences[HaxRole] = HaxDownSequence;
            scene.Sequences[HaxDownSequence] = lost;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        internal static void Open(MarshesMissionDataV1 mission, uint objective, uint first, uint target, uint transition = 1)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target, transition: transition);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete, transition: transition);
        }
    }
}
