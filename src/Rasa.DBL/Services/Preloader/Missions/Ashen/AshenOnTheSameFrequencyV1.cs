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
    /// On The Same Frequency (native mission 1277), Ligo Ashen Desert: with the Bane codes (1275) Captain Marcus Emmons has the
    /// player program the Radar Operations Consoles on the top floors of Stonewall Ridge's four radar towers; the Bane, who might
    /// drop an assault force, ambush the player at them.
    ///
    /// Native: texts 10265-10269 (missionconversation); objectives (missionobjective) 1-4 "Program Radar Tower 1".."4"
    /// 10270/10271, 10272/10273, 10274/10275, 10276/10277, 5 "Ambushes" 11818/11819 (a helper of the client's, no text the
    /// player sees); no counters, no objective conversation. Usable name 65 "Radar Operations Console". The client map: four
    /// ArchHumOutpostTowerV02 with a radar ArchHumanBaseRadarV01 on top, (-63, 373, -401), (-4, 359, -534), (59, 370, -475),
    /// (147, 356, -603) (statics). World DB: the towers' linked teleporters, ground 539 / 538 / 537 / 540 with their second
    /// floors 823 / 824 / 825 / 826 (13.4 m above, no stairs: ControlTowerTeleporters).
    ///
    /// Sourced: TaRapedia On The Same Frequency: giver and reward giver Captain Marcus Emmons; requires Cycled Frequencies
    /// (1275); follow-up King of the Hill (1278); 190,000 XP, 8,400 credits; Vextronics Cryogenic Blade, Electric Rifle,
    /// Chaingun, Pistol; the consoles "on the top floor" of each tower, "through the teleporter"; "Start with this tower here".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band).
    ///  - Tower 1 the Stonewall Command Tower by Emmons (539 / 823: "this tower here"), 2 the Operations tower (538 / 824), 3
    ///    and 4 the two artillery control towers (537 / 825, 540 / 826): the order of the client's objectives on the walk away
    ///    from Emmons. The four can be done in any order.
    ///  - A personal Radar Operations Console on each top floor (the client's human monitor UsableTwoStateHumComputerV01V06
    ///    9478, off 55, on 56, usabledata 1: as Pools' Total Slaughter), named 65, 4 m off the upper teleporter; on the
    ///    client's floor and the navmesh, walked to from the upper pad and back. Its use completes its objective (a scene
    ///    result) and turns it on.
    ///  - The ambushes (5, not written, as 1857's helpers): at towers 2-4 the console's use brings three of the Ashen Thrax
    ///    Soldiers (531008) at the tower's foot, who attack the player (Crucible's The Gene Machine ticks). Tower 1 has none
    ///    (Emmons is there).
    ///  - A marker for each objective at its tower's ground teleporter (the way up).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (client entityclass names): Blade Ice
    ///    12101 (41-45, "Cryogenic"), Rifle Electric 11953 (41-45), Machine Gun Physical 48300 (38-42, "Chaingun"), Pistol
    ///    Physical 14409 (40-44); one to choose.
    /// </summary>
    public static class AshenOnTheSameFrequencyV1
    {
        public const uint MissionId = 1277;
        public const uint Level = 41;
        public const uint Emmons = AshenLeadInNpcsV1.Emmons;
        public const uint Programmed = 1;               // the scenario of the consoles' uses: step K for tower K
        public const uint Soldier = 531008;             // Thrax Soldier - Ashen Desert

        public const uint ConsoleClass = 9478;          // UsableTwoStateHumComputerV01V06
        public const uint ConsoleName = 65;             // usablenameoverride "Radar Operations Console"
        public const uint UseArgument = 1;              // usabledata of 9478: 1
        public const uint OffState = 55, OnState = 56;

        /// <summary>
        /// The towers in objective order: objective, ground teleporter, upper teleporter, the ground pad (marker), the console
        /// (the second floor's navmesh, 4 m off the upper pad, seen from the 16 places 2 m round it, ESTIMATE), its facing
        /// (ESTIMATE), and the ambush spots at the tower's foot, 9-13 m off the ground pad on the navmesh joined to the
        /// waypoint, seen from the 16 places round them (none at tower 1; the probes of 2026-11-10). As set 9 wrote them (live);
        /// fix job E (migration 20271110001010, AshenFixEV1.Towers) moves the consoles 5-6 m off the upper pads and the ambushes
        /// onto the top floors, and keeps each console's use on its floor: the game reads the scene from the World DB.
        /// </summary>
        public static readonly (uint Objective, uint Ground, uint Upper, ScenePosition PadAt, ScenePosition ConsoleAt, double Facing,
            ScenePosition[] AmbushAt)[] Towers =
        {
            (1, 539, 823, new(-62.80f, 353.43f, -400.71f), new(-64.31f, 367.05f, -397.04f), 3.1416, new ScenePosition[0]),
            (2, 538, 824, new(-3.80f, 339.87f, -533.49f), new(-6.55f, 353.45f, -530.78f), 3.1416,
                new ScenePosition[] { new(7.12f, 336.88f, -533.46f), new(-5.30f, 337.85f, -521.89f), new(-8.78f, 337.57f, -521.48f) }),
            (3, 537, 825, new(59.41f, 350.58f, -474.90f), new(60.95f, 364.06f, -471.29f), 3.1416,
                new ScenePosition[] { new(47.81f, 348.25f, -473.40f), new(59.41f, 348.91f, -463.90f), new(70.41f, 348.09f, -473.40f) }),
            (4, 540, 826, new(146.75f, 337.25f, -602.82f), new(146.81f, 350.85f, -598.80f), 3.1416,
                new ScenePosition[] { new(158.41f, 335.93f, -598.61f), new(150.96f, 335.87f, -592.66f), new(138.97f, 335.92f, -595.04f) })
        };

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint CryogenicBlade = 12101;       // Weapon_Avatar_Blade_Ice_UNC_41_to_45
        public const uint ElectricRifle = 11953;        // Weapon_Avatar_Rifle_Electric_UNC_41_to_45
        public const uint Chaingun = 48300;             // Weapon_Avatar_MachineGun_Physical_UNC_38_to_42
        public const uint Pistol = 14409;               // Weapon_Avatar_Pistol_Physical_UNC_40_to_44

        public static string ConsoleRole(int tower) => $"stonewall-radar-console-{tower + 1}";
        public static string AmbushRole(int tower, int member) => $"stonewall-ambush-{tower + 1}-{member + 1}";
        public static uint ConsoleSequence(int tower) => 1 + (uint)tower;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "On The Same Frequency", 10265, Emmons, Emmons, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicBlade, ElectricRifle, Chaingun, Pistol });
            mission.Prerequisite(AshenCycledFrequenciesV1.MissionId);
            mission.Scenario(Programmed, "A Radar Operations Console programmed", (uint)Towers.Length);

            var nameText = 10270U;
            foreach (var tower in Towers)
            {
                mission.Objective(tower.Objective, nameText, nameText + 1, tower.Objective - 1, MissionObjectiveState.Incomplete);
                nameText += 2;
                mission.Transition(tower.Objective);
                mission.Progress(tower.Objective, MissionProgressEventKind.ScenarioEvent, tower.Objective, null, counter: Programmed);
                mission.Indicator(tower.Objective, tower.Objective, tower.PadAt.X, tower.PadAt.Y, tower.PadAt.Z);
            }
            mission.Action(Towers[0].Objective, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1277",
                "Native1-4 'Program Radar Tower 1'..'4' 10270/10271..10276/10277, 5 'Ambushes' 11818/11819 (helper); no counters; " +
                "no objectiveconversation. Texts 10265-10269. Usable name 65 'Radar Operations Console'. Radars on 4 " +
                "ArchHumOutpostTowerV02 (-63, -401), (-4, -534), (59, -475), (147, -603); teleporters 537-540 <-> 823-826.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/On_The_Same_Frequency",
                "Giver/reward giver Captain Marcus Emmons; requires Cycled Frequencies; follow-up King of the Hill; 190000XP, 8400 " +
                "credits; Vextronics Cryogenic Blade, Electric Rifle, Chaingun, Pistol; consoles on the top floor through the " +
                "teleporter; 'Start with this tower here'.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenOnTheSameFrequencyV1",
                "Level41 estimated. Towers 1-4 = 539/823, 538/824, 537/825, 540/826, any order. A personal console 9478 (name65; " +
                "arg1; 55->56) per top floor 4 m off the upper pad; its use completes its objective. At towers 2-4 three Thrax " +
                "Soldiers 531008 at the foot attack (5 not written). Markers at the ground pads. Items 12101, 11953, 48300, 14409 " +
                "(lowest Uncommon, band of level 41), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own consoles on the four top floors; the ambushes at towers 2-4 (as set 9 wrote it).</summary>
        public static MissionSceneDefinition Scene() => Scene(Towers);

        /// <summary>
        /// The scene for <paramref name="towers"/>, each console used only within <paramref name="maxUseHeight"/> m of its floor
        /// (SceneObjectAction.MaxUseHeight; 0: no limit, as set 9). Fix job E (AshenFixEV1) builds its scene with this.
        /// </summary>
        internal static MissionSceneDefinition Scene((uint Objective, uint Ground, uint Upper, ScenePosition PadAt, ScenePosition ConsoleAt,
            double Facing, ScenePosition[] AmbushAt)[] towers, float maxUseHeight = 0)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < towers.Length; index++)
            {
                var tower = towers[index];
                var role = ConsoleRole(index);
                var sequence = ConsoleSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ConsoleClass, tower.ConsoleAt,
                    Orientation: tower.Facing, InitialObjectState: OffState, NameOverrideId: ConsoleName,
                    UseAction: new SceneObjectAction(MissionId, tower.Objective, sequence, UseArgument, maxUseHeight));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                var programmed = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-programmed", role, OnState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Programmed, tower.Objective) }
                };
                for (var member = 0; member < tower.AmbushAt.Length; member++)
                {
                    var ambusher = AmbushRole(index, member);
                    scene.Actors[ambusher] = new SceneActorDefinition(ambusher, SceneActorKind.Creature, Soldier, tower.AmbushAt[member],
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    programmed.World.Add(new EnsureActorIntent($"{ambusher}-comes", ambusher));
                    programmed.World.Add(QuestSceneIntents.Attack($"{ambusher}-attacks", ambusher));
                }
                scene.Sequences[sequence] = programmed;
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
