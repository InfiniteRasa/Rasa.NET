using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Cost of Doing Business (native mission 1079), Turpis Refinery (squad instance 1429), Valverde Pools: Biotech
    /// Johansson wants enough of the refinery's lab equipment destroyed to cost the Bane 72 hours of reconstruction.
    ///
    /// Native: texts 6087-6090, 7318 (missionconversation): log "Biotech Johansson wants you to destroy enough lab
    /// equipment to cost the Bane 72 hours of reconstruction time.", offer "Every thing you destroy in there has an
    /// associated cost of reconstruction in hours. If you blow up a barrel, it costs them an hours worth of time to replace
    /// it. Some things are worth more time than others, like lab tables, those take longer ... That's 72 hours worth of
    /// destruction.", turn-in "Freaking awesome! You did it."; objective 2 "Destruction" 6091/7319 counter 6092 "Hours to
    /// Reconstruct" (missionobjective); no objective 1; no objective conversation.
    ///
    /// Sourced: TaRapedia Cost of Doing Business: giver and receiver Biotech Johansson (Turpis Refinery); no requirement;
    /// level 33; 50,000 XP, 4,950 credits; one of Teleract Cryogenic Blade lvl 31, Prodigy Radial Healing Disc lvl 30,
    /// Animatics Sonic Pistol lvl 30, Teleract Pulse Staff lvl 30; "Hours to Reconstruct 0 / 72". Ten Ton Hammer Turpis
    /// Refinery guide: "As you go through shooting lab tables, security CPUs, storage tanks, hoses, storage lockers, etc.,
    /// you will notice a message Hours to Reconstruct ##/72 appear." Patch notes (IGN boards, 2008): "Destroying storage
    /// tanks inside Turpis Refinery will now give credit for "Cost of Doing Business"", "Storage Tanks will no longer
    /// respawn", "All pipes inside Turpis Refinery should now be destroyable". TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: personal lab objects with hour values that add up to 72,
    /// a counter in hours):
    ///  - Twenty-two personal Bane destructibles (InertDestroyable classes, 100 hit points each) at places on the
    ///    facility's navmesh round the horseshoe: six barrels (1 hour each, the client's), four pipes, the "hoses" (2),
    ///    four closed lockers (3), three storage tanks (4), two security machines, the "security CPUs" (5), and three lab
    ///    tables (8): 72 hours. Each destruction counts its hours: the client's counter "Hours to Reconstruct" goes up by
    ///    that many, one scene event an hour; 2 completes at 72, so every object must go.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels ("Radial" Area, "Pulse" EMP,
    ///    "Cryogenic" Ice), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsCostOfDoingBusinessV1
    {
        public const uint MissionId = 1079;
        public const uint Level = 33;
        public const uint MapContextId = PoolsTurpisRefineryNpcsV1.MapContextId;
        public const uint Johansson = PoolsTurpisRefineryNpcsV1.Johansson;
        public const uint Destruction = 2, HoursLost = 1, Hours = 72;
        public const uint HitPoints = 100;
        public const double ObjectRadius = 8;
        public const uint IntactState = PoolsSabotageTheTowerV1.IntactState, DestroyedState = PoolsSabotageTheTowerV1.DestroyedState;

        public const uint Barrel = 9260;                // UsableInertDestBaneBarrelV01
        public const uint Pipe = 20592;                 // UsableInertDestBanePipeV01
        public const uint Locker = 20591;               // UsableInertDestBaneLockerClosed
        public const uint Tank = 20612;                 // UsableInertDestBaneTankV01
        public const uint SecurityMachine = 10191;      // UsableInertDestBaneSecurityMachineryV01
        public const uint LabTable = 20641;             // UsableInertDestBaneLabEquipmentTableV01

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint CryogenicBlade = 47648;       // Weapon_Avatar_Blade_Ice_UNC_31_to_35
        public const uint RadialHealingDisc = 121346;   // Tool_Avatar_Healing_Disc_Area_UNC_30_to_34
        public const uint SonicPistol = 17215;          // Weapon_Avatar_Pistol_Sonic_UNC_30_to_34
        public const uint PulseStaff = 18028;           // Weapon_Avatar_Staff_EMP_UNC_30_to_34

        /// <summary>The objects: class, the hours it costs, place (scene sequences 1-22, in this order).</summary>
        public static readonly (uint Class, uint Hours, ScenePosition At)[] Objects =
        {
            (Barrel, 1, new(-232f, 109.07f, -96f)),
            (Pipe, 2, new(-216f, 109.76f, -56f)),
            (Locker, 3, new(-200f, 109.75f, 32f)),
            (Barrel, 1, new(-160f, 107.72f, -8f)),
            (Tank, 4, new(-136.9f, 109.57f, 72.8f)),
            (Locker, 3, new(-88.2f, 108.65f, 95.7f)),
            (LabTable, 8, new(-64.6f, 106.4f, 159.2f)),
            (LabTable, 8, new(-72f, 91.16f, 200f)),
            (SecurityMachine, 5, new(-48f, 91.18f, 248f)),
            (Barrel, 1, new(-24f, 104.03f, 144f)),
            (LabTable, 8, new(8f, 105.91f, 183.2f)),
            (Pipe, 2, new(8f, 105.91f, 168.8f)),
            (Tank, 4, new(57.2f, 106.11f, 71.1f)),
            (Barrel, 1, new(79.6f, 106.11f, 81f)),
            (Locker, 3, new(79.7f, 106.11f, -16.8f)),
            (SecurityMachine, 5, new(144f, 106.11f, -8f)),
            (Pipe, 2, new(160f, 106.23f, 40f)),
            (Tank, 4, new(96f, 106.11f, -24f)),
            (Barrel, 1, new(-32f, 107.75f, -144f)),
            (Locker, 3, new(-32f, 107.71f, -168f)),
            (Pipe, 2, new(-24f, 109.88f, -104f)),
            (Barrel, 1, new(-152f, 109.75f, 8f)),
        };

        public static string ObjectRole(int index) => $"lab-object-{index + 1}";

        /// <summary>The first scene event (hour) of object <paramref name="index"/>: its hours follow.</summary>
        public static uint FirstHour(int index) => 1 + (uint)Objects.Take(index).Sum(entry => entry.Hours);

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Cost of Doing Business", 6087, Johansson, Johansson, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicBlade, RadialHealingDisc, SonicPistol, PulseStaff });

            mission.Objective(Destruction, 6091, 7319, 0, MissionObjectiveState.Incomplete, counterTextId: 6092);
            mission.Scenario(HoursLost, "Bane hours of reconstruction", Hours);
            mission.Transition(Destruction);
            for (var hour = 1U; hour <= Hours; hour++)
                mission.Progress(Destruction, MissionProgressEventKind.ScenarioEvent, hour, Hours, counter: HoursLost, trigger: hour);
            mission.Action(Destruction, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Objects.Length; index++)
            {
                var (objectClass, hours, at) = Objects[index];
                var sequence = (uint)index + 1;
                var role = ObjectRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, objectClass, at,
                    Orientation: index * 0.7853981634, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destruction, sequence, HitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                var destroyed = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    }
                };
                for (var hour = FirstHour(index); hour < FirstHour(index) + hours; hour++)
                    destroyed.Signals.Add(new SceneMissionSignal(MissionId, HoursLost, hour));
                scene.Sequences[sequence] = destroyed;
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1079",
                "Native2 'Destruction' 6091/7319 counter6092 'Hours to Reconstruct'; no objective 1; no objectiveconversation. " +
                "Texts 6087-6090, 7318 (a barrel an hour, lab tables longer, 72 hours).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Cost_of_Doing_Business",
                "Giver/receiver Biotech Johansson (Turpis Refinery); no requirement; level33; 50000XP, 4950 credits; one of Teleract " +
                "Cryogenic Blade31, Prodigy Radial Healing Disc30, Animatics Sonic Pistol30, Teleract Pulse Staff30. Ten Ton Hammer: " +
                "lab tables, security CPUs, storage tanks, hoses, storage lockers; patch notes: tanks count, pipes destroyable.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsCostOfDoingBusinessV1",
                "22 personal destructibles (user), 100HP: 6 barrels 1h, 4 pipes 2h, 4 lockers 3h, 3 tanks 4h, 2 security " +
                "machines 5h, 3 lab tables 8h = 72; a scene event an hour. Items 47648, 121346, 17215, 18028, one to choose. " +
                "Places are estimates. Indicators in the instance too.", 0.3);

            for (var index = 0; index < Objects.Length; index++)
                mission.Indicator(Destruction, 1 + (uint)index, Objects[index].At.X, Objects[index].At.Y, Objects[index].At.Z, ObjectRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
