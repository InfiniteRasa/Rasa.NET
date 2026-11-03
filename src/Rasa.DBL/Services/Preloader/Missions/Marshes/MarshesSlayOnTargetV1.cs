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
    /// Slay on Target (native mission 718), the Logos Research Facility (squad instance 1700): Specialist Hax sends the player
    /// into the facility's three experimentation rooms to kill their overseers, destroy the lab equipment and take a DNA
    /// specimen; Overseer Mesgel raises the alarm, and a Bane teleporter brings troops until it is destroyed. Turned in by
    /// radio.
    ///
    /// Native: texts 3427-3430 and 7307 (missionconversation): log "Infiltrate the experimentation rooms, destroy the lab
    /// equipment and gather a lab sample.", offer "there are 3 main experimentation rooms here ... We'll need a sample of what
    /// they're extracting ... After that, blow all their equipment to hell ... Radio in when you're good to go", turn-in "It
    /// looks like ordinary fluids ... how can it be this acidic"; objectives (missionobjective) 1 "Eliminate Coroner Larvax"
    /// 3432/7308 counter 3434, 2 "Eliminate Dissector Xynnr." 3433/7309 counter 3435, 3 "Eliminate Overseer Mesgel."
    /// 4715/7310 (second counter 9008 "Boss Hit"), 4 "Destroy Bane Lab Equipment." 8980/8981 counter 8982 "Lab Equipment
    /// Destroyed", 5 "Gather DNA Specimen for AFS Analysis." 8983/8984, 6 "Alarm signaled by Mesgel! Destroy the teleporter to
    /// stop the influx of troops." 8987/8988, 7 "HIDDEN - Part 3 Required!" 8985/8986 (the counter texts look shifted in the
    /// client: 3434 reads "Caretaker Mesgel", 3435 "Larvax"); no objective conversation. Item DNA Sample Jar (class 11015,
    /// template 2506, MisLogosResearchItemDNASample). Classes UsableInertDestBaneLabEquipmentV01 (20595) and
    /// UsableInertDestBaneTeleporterV01DELETEDUPE (10603, the Bane teleporter's mesh, targetable). The LRF map: three rooms
    /// at level 288 with extraction sensors and the extracted dead (ArchBaneExtractionSensorsV01, PropHum*CorpseExtraction)
    /// at (776, -104), (688, -24) and (648, -200), the medical shelves of the third at (626.3, 288, -193 to -207); the Test
    /// Chamber (map_region 20053, 608, 247.2, -128).
    ///
    /// Sourced: TaRapedia Slay On Target: giver Specialist Hax, receiver Headquarters (radio); requires A Taste of Their Own
    /// Medicine; 75,000 XP, 5,700 credits; one of Class VII EMP Bomb, Class VII Cryogenic Grenade, Class VII Standard Med
    /// Pack; 6 lab equipment, 1 DNA sample jar; Larvax, Xynnr and Mesgel drop keycards level 1, 2, 3 (not built: no use for
    /// them in the client's data). Patch 2007-11-14: "DNA sample usables are now mission activated". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE, the triage's choices of 2026-11-02; confidence 0.3):
    ///  - Bound to the squad's facility. Larvax, Xynnr and Mesgel (the run's own, level 38) one in each room (in that order
    ///    from the hospital), two pieces of lab equipment in each (shot down, 100 hit points), a DNA dispenser (the Bane item
    ///    crate of the Marshes' sample taps, 881) at the third room's medical shelves; 1-5 open from the start.
    ///  - Mesgel's first hit (DamagedSequences) is the alarm: hidden 7 completes and opens 6; a Bane teleporter stands in the
    ///    Test Chamber and sends four Thrax at the player at once and every 30 s, six waves at most; its destruction (shot
    ///    down, 600 hit points) stops them and completes 6.
    ///  - The stacks 2 EMP bombs, 4 Cryogenic grenades, 2 Standard Med Packs (TaRapedia names no counts; the Marshes' Class VII
    ///    stacks), Standard the 07B Med Pack. The keycards are not dropped.
    ///  - Markers: each boss, each room's equipment, the dispenser, the teleporter.
    /// </summary>
    public static class MarshesSlayOnTargetV1
    {
        public const uint MissionId = 718;
        public const uint Level = 35;
        public const uint Hax = MarshesLogosResearchNpcsV1.Hax;
        public const uint Facility = MarshesLogosResearchNpcsV1.MapContextId;
        public const uint Larvax = 1, Xynnr = 2, Mesgel = 3, Equipment = 4, Specimen = 5, Teleporter = 6, Alarm = 7;

        /// <summary>Scenarios (results only): each boss down, the equipment, the DNA, the alarm, the teleporter.</summary>
        public const uint LarvaxDown = 1, XynnrDown = 2, MesgelDown = 3, EquipmentDown = 4, DnaTaken = 5, AlarmRaised = 6, TeleporterDown = 7;
        public const uint BossSequence = 11, EquipmentSequence = 21, DnaSequence = 31, AlarmSequence = 32, TeleporterSequence = 33,
            WaveSequence = 40;
        public const uint Waves = 6, WaveSize = 4, WaveMilliseconds = 30000;

        public const uint DnaTemplate = 2506, DnaClass = 11015;
        public const string DnaKey = "lrf-dna-sample-jar";
        public const uint DispenserClass = 11116, DispenserArgument = 3;   // UsableItemDisp_item_crate_bane_v01CONVERT2TD (881's taps)
        public const uint EquipmentClass = 20595;       // UsableInertDestBaneLabEquipmentV01
        public const uint TeleporterClass = 10603;      // UsableInertDestBaneTeleporterV01DELETEDUPE
        public const uint IntactState = 110, DestroyedState = 2;
        public const uint EquipmentHitPoints = 100, TeleporterHitPoints = 600;
        public const uint Soldier = 531045, Grenadier = 531046;
        public const string DispenserRole = "dna-dispenser", TeleporterRole = "bane-teleporter";

        /// <summary>The bosses: objective, creature, scenario, place.</summary>
        public static readonly (uint Objective, uint Creature, uint Scenario, ScenePosition At)[] Bosses =
        {
            (Larvax, MarshesLogosResearchNpcsV1.Larvax, LarvaxDown, new(776f, 288.97f, -104f)),
            (Xynnr, MarshesLogosResearchNpcsV1.Xynnr, XynnrDown, new(688f, 288.97f, -24f)),
            (Mesgel, MarshesLogosResearchNpcsV1.Mesgel, MesgelDown, new(648f, 288.99f, -200f)),
        };

        /// <summary>Two pieces of lab equipment in each room.</summary>
        public static readonly ScenePosition[] EquipmentAt =
        {
            new(770f, 287.77f, -110f), new(782f, 287.91f, -98f),
            new(682f, 287.77f, -18f), new(694f, 287.77f, -30f),
            new(642f, 287.77f, -194f), new(654f, 287.81f, -206f),
        };
        public static readonly ScenePosition DispenserAt = new(632f, 288.17f, -200f);
        public static readonly ScenePosition TeleporterAt = new(608f, 248.31f, -104f);

        /// <summary>The waves' places round the teleporter.</summary>
        public static readonly ScenePosition[] WaveAt =
        {
            new(600f, 248.15f, -110f), new(616f, 248.57f, -110f), new(604f, 247.63f, -112f), new(612f, 247.41f, -112f)
        };

        public const double BossRadius = 15, ObjectRadius = 8, RoomRadius = 15;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint EmpBomb = 111232, EmpBombs = 2;               // Class VII EMP Bomb
        public const uint CryoGrenade = 111048, CryoGrenades = 4;       // Class VII Cryogenic Grenade
        public const uint StandardMedPack = 45061, StandardMedPacks = 2; // Class VII Standard Med Pack (Consumable_Medpack_07B_35)

        public static string BossRole(int index) => $"boss-{index + 1}";
        public static string EquipmentRole(int index) => $"lab-equipment-{index + 1}";
        public static string WaveRole(int wave, int member) => $"influx-{wave + 1}-{member + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Slay on Target", 3427, Hax, null, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[] { (EmpBomb, EmpBombs), (CryoGrenade, CryoGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);
            mission.RadioCompletion();
            mission.Prerequisite(MarshesTasteOfTheirOwnMedicineV1.MissionId);
            mission.Scenario(LarvaxDown, "Coroner Larvax killed", 1);
            mission.Scenario(XynnrDown, "Dissector Xynnr killed", 1);
            mission.Scenario(MesgelDown, "Overseer Mesgel killed", 1);
            mission.Scenario(EquipmentDown, "The lab equipment destroyed", (uint)EquipmentAt.Length);
            mission.Scenario(DnaTaken, "The DNA specimen taken", 1);
            mission.Scenario(AlarmRaised, "Mesgel hit: the alarm", 1);
            mission.Scenario(TeleporterDown, "The teleporter destroyed", 1);

            mission.Objective(Larvax, 3432, 7308, 0, MissionObjectiveState.Incomplete, counterTextId: 3434);
            mission.Objective(Xynnr, 3433, 7309, 1, MissionObjectiveState.Incomplete, counterTextId: 3435);
            mission.Objective(Mesgel, 4715, 7310, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Equipment, 8980, 8981, 3, MissionObjectiveState.Incomplete, counterTextId: 8982);
            mission.Objective(Specimen, 8983, 8984, 4, MissionObjectiveState.Incomplete);
            mission.Objective(Teleporter, 8987, 8988, 5, MissionObjectiveState.Inactive);
            mission.Objective(Alarm, 8985, 8986, 6, MissionObjectiveState.Incomplete, required: false);   // hidden

            mission.Transition(Larvax);
            mission.Progress(Larvax, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: LarvaxDown);
            mission.Action(Larvax, 1, MissionActionKind.GrantReward, reward: 1);   // the radio turn-in's reward
            mission.Transition(Xynnr);
            mission.Progress(Xynnr, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: XynnrDown);
            mission.Transition(Mesgel);
            mission.Progress(Mesgel, MissionProgressEventKind.ScenarioEvent, 1, null, counter: MesgelDown);
            mission.Transition(Equipment);
            for (var piece = 1U; piece <= EquipmentAt.Length; piece++)
                mission.Progress(Equipment, MissionProgressEventKind.ScenarioEvent, piece, (uint)EquipmentAt.Length, counter: EquipmentDown, trigger: piece);
            mission.Transition(Specimen);
            mission.Progress(Specimen, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DnaTaken);
            mission.Transition(Alarm);
            mission.Progress(Alarm, MissionProgressEventKind.ScenarioEvent, 1, null, counter: AlarmRaised);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Alarm, 1, Teleporter);
            mission.Transition(Teleporter);
            mission.Progress(Teleporter, MissionProgressEventKind.ScenarioEvent, 1, null, counter: TeleporterDown);

            var indicator = 1U;
            foreach (var boss in Bosses)
                mission.Indicator(boss.Objective, indicator++, boss.At.X, boss.At.Y, boss.At.Z, BossRadius, Facility);
            foreach (var boss in Bosses)
                mission.Indicator(Equipment, indicator++, boss.At.X, boss.At.Y, boss.At.Z, RoomRadius, Facility);
            mission.Indicator(Specimen, indicator++, DispenserAt.X, DispenserAt.Y, DispenserAt.Z, ObjectRadius, Facility);
            mission.Indicator(Teleporter, indicator, TeleporterAt.X, TeleporterAt.Y, TeleporterAt.Z, ObjectRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/718",
                "Native1 Larvax 3432/7308 counter3434, 2 Xynnr 3433/7309 counter3435, 3 Mesgel 4715/7310 (counter1 9008 'Boss Hit'), 4 " +
                "lab equipment 8980/8981 counter8982, 5 DNA specimen 8983/8984, 6 teleporter 8987/8988, 7 hidden 'Part 3 Required' " +
                "8985/8986; no objectiveconversation. Texts 3427-3430, 7307. DNA Sample Jar 11015/2506; classes 20595 lab equipment, " +
                "10603 Bane teleporter (destructible). Names 8572-8574. LRF rooms (776,-104), (688,-24), (648,-200) at y288.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Slay_On_Target",
                "Giver Specialist Hax, receiver Headquarters (radio); requires 546; 75000XP, 5700 credits; Class VII EMP Bomb/" +
                "Cryogenic Grenade/Standard Med Pack; 6 lab equipment, 1 DNA sample jar; bosses drop keycards 1-3. Patch 2007-11-14 " +
                "DNA usables mission activated. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesSlayOnTargetV1",
                "Bound to the squad's LRF. The run's own Larvax 760366, Xynnr 760367, Mesgel 760368 (L38) one a room, 2 lab " +
                "equipment 20595 a room, DNA dispenser 11116 at the third room's shelves. Mesgel's first hit: hidden 7, 6 opens, a " +
                "teleporter 10603 in the Test Chamber sends 4 Thrax 531045/531046 at once and every 30 s (6 waves at most) until " +
                "shot down. Stacks 2/4/2 estimated; keycards not dropped. Places estimates.", 0.3);

            mission.Enable(Scene());
        }

        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                DefeatSequences = new(),
                DamagedSequences = new() { [BossRole(2)] = AlarmSequence },
                HiddenObjectiveIds = new() { Alarm },
                Items = new()
                {
                    new MissionItemBinding(DnaKey, DnaTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            var start = new SceneSequenceDefinition();
            for (var index = 0; index < Bosses.Length; index++)
            {
                var role = BossRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Bosses[index].Creature, Bosses[index].At,
                    Orientation: 1.5708, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                start.World.Add(new EnsureActorIntent($"{role}-stands", role));
                scene.DefeatSequences[role] = BossSequence + (uint)index;
                scene.Sequences[BossSequence + (uint)index] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, Bosses[index].Scenario, 1) }
                };
            }
            for (var index = 0; index < EquipmentAt.Length; index++)
            {
                var role = EquipmentRole(index);
                var sequence = EquipmentSequence + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, EquipmentClass, EquipmentAt[index],
                    Orientation: index * 0.7853981634, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Equipment, sequence, EquipmentHitPoints, DestroyedState));
                start.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState) },
                    Signals = { new SceneMissionSignal(MissionId, EquipmentDown, (uint)index + 1) }
                };
            }
            scene.Actors[DispenserRole] = new SceneActorDefinition(DispenserRole, SceneActorKind.Object, DispenserClass, DispenserAt,
                Orientation: -1.5708, UseAction: new SceneObjectAction(MissionId, Specimen, DnaSequence, DispenserArgument));
            start.World.Add(new EnsureActorIntent("create-dispenser", DispenserRole));
            scene.Sequences[DnaSequence] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("dispenser-done", DispenserRole, false) },
                Character = { new IssueMissionItemIntent("take-dna", MissionId, DnaKey, DnaTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, DnaTaken, 1) }
            };

            scene.Actors[TeleporterRole] = new SceneActorDefinition(TeleporterRole, SceneActorKind.Object, TeleporterClass, TeleporterAt,
                InitialObjectState: IntactState, ViewRadius: 60,
                Destruction: new SceneObjectDestruction(MissionId, Teleporter, TeleporterSequence, TeleporterHitPoints, DestroyedState));
            // The alarm: the teleporter stands and the first wave comes at once; each wave sets the next one's timer.
            var alarm = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("teleporter-comes", TeleporterRole) },
                Signals = { new SceneMissionSignal(MissionId, AlarmRaised, 1) }
            };
            var stopped = new SceneSequenceDefinition
            {
                World = { new TransitionObjectStateIntent("teleporter-destroyed", TeleporterRole, DestroyedState) },
                Signals = { new SceneMissionSignal(MissionId, TeleporterDown, 1) },
                Timers = { new SequenceTimer("influx", 0, WaveSequence, Cancel: true) }
            };
            for (var wave = 0; wave < Waves; wave++)
            {
                var comes = wave == 0 ? alarm : new SceneSequenceDefinition();
                for (var member = 0; member < WaveSize; member++)
                {
                    var role = WaveRole(wave, member);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, member % 2 == 0 ? Soldier : Grenadier,
                        WaveAt[member], GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    comes.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    comes.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                }
                if (wave + 1 < Waves)
                    comes.Timers.Add(new SequenceTimer("influx", WaveMilliseconds, WaveSequence + (uint)wave + 1));
                if (wave > 0)
                    scene.Sequences[WaveSequence + (uint)wave] = comes;
            }
            scene.Sequences[0] = start;
            scene.Sequences[AlarmSequence] = alarm;
            scene.Sequences[TeleporterSequence] = stopped;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
