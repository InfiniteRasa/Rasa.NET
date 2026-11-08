using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Factory Fortification (native mission 1523), the Chaukas Robotics Facility (squad instance 2146): Cipher Specialist
    /// Brian has the player hold the factory, the east round room (Robotics Assembly), against the Thrax who come to take it.
    ///
    /// Native: texts 13591-13595 (missionconversation): title "Factory Fortification", log "Kill the Bane attackers.", offer
    /// "The Bane are attacking Chaukas! Clear them all out before they can get to the factory.", turn-in "Great job clearing
    /// out the Thrax attackers. I think we have done great things here today.", 13595 "none"; objective (missionobjective) 1
    /// "Defend the Factory" 14380/14381 ("The Chaukas Facility has fallen under attack. Do not let any of the bane get to the
    /// Factory, and take it over."), no counter; no objective conversation; no indicator name. Names (creaturenamelanguage)
    /// 9224 "Thrax Soldier", 9529 "Refitted Warden Bot". Region (client statics): Robotics Assembly (88, 6, -120); the room's
    /// model arch_wardenbot_corridor_room_01_assembly_v01 at (88, 16, -48), its floor x 16-160, z -192..-48.
    ///
    /// Sourced: TaRapedia Factory Fortification: giver and reward giver Brian; requires Fix the Factory; follow-up Information
    /// is Key; 45,000 XP, 4,100 credits; Class VIII EMP Bomb (2), Basic Med Pack (3), Incendiary Grenade (3), Res Trauma Kit
    /// (3); "Enter the eastern circular room from the nearby northern entrance, then wait for the Thrax Soldiers to arrive"
    /// (0.5). TR MMO DB: level 35. PlanetTR-3508 (0.3): after everything is killed, friendly Warden Bots appear.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Taken at the public Brian at the entrance, the scene bound to the squad's Chaukas; the turn-in at him.
    ///  - Hidden 2: the player in the room (AreaEntered: an upright cylinder of RoomRadius round the room's middle, its floor,
    ///    strips and bridge; the room's rim and doors outside it) starts the defence. Three waves of three Thrax Soldiers (the
    ///    Crucible's own row 531018, "Thrax Soldier Mark IX", level 42) come WaveMilliseconds after it from the south corridor
    ///    (the side away from the north door the player came in by, towards the facility's entrance) and go for the player
    ///    (AttackActorIntent at the owner, past their leash). Wave sizes and times no source (as Leaky Pipes 1398).
    ///  - Hidden 3 counts the nine deaths (scene defeats, ScenarioEvent, as 1521's 11); 1 waits for it: the client has no
    ///    counter text for 1 (as 1521's 6).
    ///  - 1 done: the factory is the player's, and two Refitted Warden Bots (a new friendly row of the client's name 9529, class
    ///    26395 NPC_Prison_Bot_Warden, the friendly Warden Bot) stand on the room's floor (PlanetTR-3508). They stand still
    ///    and go with the scene at the turn-in.
    ///  - The room's floor is a navmesh island on upstream's mesh (NOTES-upstream-bugs.md N14): there the Thrax stop at the
    ///    room's south strip and bridge; the local fix v2 (data/navfix, one off-mesh link, one way, from the south strip down
    ///    to the east floor at z -176, as the real client steps it) joins the east half; the way out is the room's south rim
    ///    slope, which the client walks and the mesh does not have (N14 v2).
    ///    The wave places, in the corridor, are on the arrival's part on both meshes.
    ///  - The items by their client names, each a stack, one to choose (as Sadistic Sabotage 1445): 26543 Class VIII EMP Bomb
    ///    (111233) x2, 22546 Class VIII Basic Med Pack (45063) x3, 26289 Class VIII Incendiary Grenade (111039) x3, 23924 Class
    ///    VIII Res Trauma Kit (47600) x3; "Class VIII" for all four (TaRapedia writes it once).
    ///  - The marker: on the room's floor east of the factory (FactoryAt).
    /// </summary>
    public static class CrucibleFactoryFortificationV1
    {
        public const uint MissionId = 1523;
        public const uint Level = 35;
        public const uint Brian = CrucibleChaukasNpcsV1.Brian;
        public const uint MapContextId = CrucibleChaukasNpcsV1.Chaukas;
        public const uint Defend = 1, InTheRoom = 2, WavesTally = 3;
        public const uint RoomArea = 1;

        /// <summary>The east room's middle and the cylinder of hidden 2 (its half-height: floor y 16, strips 17.3, bridge 24).</summary>
        public static readonly (double X, double Y, double Z) RoomAt = (88, 20, -120);
        public const double RoomRadius = 60, RoomHalfHeight = 8;

        /// <summary>1's marker: the room's floor east of the factory (a navmesh island on upstream's mesh, N14).</summary>
        public static readonly (double X, double Y, double Z) FactoryAt = (120, 16.04, -150);
        public const double FactoryRadius = 12;

        /// <summary>Scenarios: the defence (hidden 2), the factory taken (1), the Thrax' kills.</summary>
        public const uint DefenceScenario = 2, TakenScenario = 3, WavesDown = 4;
        public const uint Wave1Sequence = 31, Wave2Sequence = 32, Wave3Sequence = 33, DownFrom = 40;

        /// <summary>The waves, ms after the player came into the room (no source; as Leaky Pipes 1398).</summary>
        public static readonly uint[] WaveMilliseconds = { 15000, 60000, 120000 };
        public const int PerWave = 3;

        /// <summary>The Crucible's Thrax Soldier (upstream 531018, "Thrax Soldier Mark IX", level 42).</summary>
        public const uint ThraxSoldier = 531018;

        /// <summary>
        /// The nine Thrax in the south corridor (probe 2026-11-08: on the navmesh's arrival part, upstream's and the local
        /// fix's, and a client floor; 37 m and more from 1's marker): wave by wave, three a wave.
        /// </summary>
        public static readonly ScenePosition[] WaveAt =
        {
            new(88f, 17.44f, -190f), new(86.2f, 17.44f, -194f), new(89.5f, 17.44f, -194f),
            new(88f, 17.44f, -200f), new(86.2f, 17.44f, -204f), new(89.5f, 17.44f, -204f),
            new(80f, 17.44f, -208f), new(76f, 17.44f, -208f), new(72f, 17.54f, -208f)
        };

        /// <summary>The Refitted Warden Bot: a new row (the Crucible block), friendly, standing still.</summary>
        public const uint RefittedBot = 770063, RefittedBotName = 9529, RefittedBotClass = 26395;   // NPC_Prison_Bot_Warden
        public const uint NextFreeId = 770064;
        public static readonly ScenePosition[] BotsAt = { new(111f, 16.04f, -141f), new(129f, 16.04f, -159f) };

        public const uint Experience = 45000;
        public const uint Credits = 4100;
        public const uint EmpBomb = 111233, EmpBombs = 2;                       // Class VIII EMP Bomb (class 26543)
        public const uint BasicMedPack = 45063, BasicMedPacks = 3;              // Class VIII Basic Med Pack (class 22546)
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;   // Class VIII Incendiary Grenade (class 26289)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 3;              // Class VIII Res Trauma Kit (class 23924)

        public static string WaveRole(int index) => $"thrax-{index + 1}";
        public static string BotRole(int index) => $"refitted-warden-bot-{index + 1}";

        /// <summary>The Refitted Warden Bot's row: friendly, level 40 and 4500/300 as the set's other new NPCs, no speed, no attack.</summary>
        public static IEnumerable<string> NpcUp()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({RefittedBot}, 'Refitted Warden Bot - Chaukas Robotics Facility, the factory (1523)', {RefittedBotClass}, " +
                         $"{CrucibleChaukasNpcsV1.Friendly}, {CrucibleChaukasNpcsV1.NpcLevel}, {CrucibleChaukasNpcsV1.HitPoints}, {RefittedBotName}, " +
                         "0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return Stat(RefittedBot, CrucibleChaukasNpcsV1.HitPoints, CrucibleChaukasNpcsV1.Armor);
        }

        public static IEnumerable<string> NpcDown()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {RefittedBot};";
            yield return $"DELETE FROM creature WHERE id = {RefittedBot};";
        }

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Factory Fortification", 13591, Brian, Brian, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (BasicMedPack, BasicMedPacks), (IncendiaryGrenade, IncendiaryGrenades), (ResTraumaKit, ResTraumaKits) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleFixTheFactoryV1.MissionId);
            mission.Scenario(DefenceScenario, "In the factory room: the Thrax come", 0);
            mission.Scenario(TakenScenario, "The factory held: the Refitted Warden Bots", 0);
            mission.Scenario(WavesDown, "The Thrax attackers killed", (uint)WaveAt.Length);

            mission.Objective(Defend, 14380, 14381, 0, MissionObjectiveState.Incomplete);
            mission.Objective(InTheRoom, 14380, 14381, 1, MissionObjectiveState.Incomplete, required: false);    // hidden
            mission.Objective(WavesTally, 14380, 14381, 2, MissionObjectiveState.Incomplete, required: false);   // hidden

            // 2, hidden: the player in the factory room; the waves on their timers.
            mission.Area(RoomArea, MapContextId, RoomAt.X, RoomAt.Y, RoomAt.Z, RoomRadius, RoomHalfHeight);
            mission.Transition(InTheRoom);
            mission.EnterArea(InTheRoom, RoomArea);
            mission.Action(InTheRoom, 1, MissionActionKind.StartScenario, scenario: DefenceScenario);

            // 3, hidden: the nine Thrax dead.
            mission.Transition(WavesTally);
            for (var kill = 1U; kill <= WaveAt.Length; kill++)
                mission.Progress(WavesTally, MissionProgressEventKind.ScenarioEvent, kill, (uint)WaveAt.Length, counter: WavesDown, trigger: kill);

            // 1: the factory held; the Refitted Warden Bots come out.
            mission.Transition(Defend);
            mission.ObjectiveState(Defend, WavesTally, MissionObjectiveState.Completed);
            mission.Action(Defend, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Action(Defend, 2, MissionActionKind.StartScenario, scenario: TakenScenario);

            mission.Indicator(Defend, 1, FactoryAt.X, FactoryAt.Y, FactoryAt.Z, FactoryRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1523",
                "Native1 'Defend the Factory' 14380/14381, no counter, no objectiveconversation. Texts 13591-13595 (log 'Kill the " +
                "Bane attackers.'). Names 9224 'Thrax Soldier', 9529 'Refitted Warden Bot'. Region Robotics Assembly 88,6,-120.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Factory_Fortification",
                "Brian gives and rewards; requires Fix the Factory; follow-up Information is Key; 45000XP, 4100 credits; Class VIII EMP " +
                "Bomb (2), Basic Med Pack (3), Incendiary Grenade (3), Res Trauma Kit (3); enter the eastern circular room from the " +
                "northern entrance, wait for the Thrax Soldiers. TR MMO DB level35. PlanetTR-3508: friendly Warden Bots after the kills (0.3).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleFactoryFortificationV1",
                "Bound to the squad's Chaukas 2146. Hidden 2 the room (r60 round 88,20,-120) starts three waves of three Thrax Soldiers " +
                "531018 at 15/60/120 s from the south corridor, at the player; hidden 3 counts the nine, 1 follows it; then two " +
                "Refitted Warden Bots 770063 on the room's floor. Items 111233x2, 45063x3, 111039x3, 47600x3, one to choose. Marker " +
                "120,16.04,-150 r12.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The waves and the Refitted Warden Bots, in the squad's Chaukas.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Brian in the squad's Chaukas
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { InTheRoom, WavesTally },
                DefeatSequences = new()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[DefenceScenario] = new SceneSequenceDefinition
            {
                Timers =
                {
                    new SequenceTimer("wave-1", WaveMilliseconds[0], Wave1Sequence),
                    new SequenceTimer("wave-2", WaveMilliseconds[1], Wave2Sequence),
                    new SequenceTimer("wave-3", WaveMilliseconds[2], Wave3Sequence)
                }
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
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, ThraxSoldier, WaveAt[index],
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    come.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    come.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                    scene.DefeatSequences[role] = down;
                    scene.Sequences[down] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, WavesDown, (uint)index + 1) } };
                }
                scene.Sequences[waves[wave]] = come;
            }

            var taken = new SceneSequenceDefinition();
            for (var index = 0; index < BotsAt.Length; index++)
            {
                var role = BotRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, RefittedBot, BotsAt[index], OwnerOnly: true);
                taken.World.Add(new EnsureActorIntent($"{role}-comes", role));
            }
            scene.Sequences[TakenScenario] = taken;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
