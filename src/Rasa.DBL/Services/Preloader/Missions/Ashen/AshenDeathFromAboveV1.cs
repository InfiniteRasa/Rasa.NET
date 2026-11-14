using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Death from Above (native mission 1352), Ligo Ashen Desert: Captain Akio Yamamoto at Boneyard Outpost wants the four Bane
    /// mortars along the trench between the Boneyard and the Bane Conscription Base destroyed, then the six long range missile
    /// launchers on the base's west side blown up with the explosives he gives.
    ///
    /// Native: texts 11060-11064 (missionconversation): log "Destroy the 4 Bane Mortars along the trench separating the Boneyard
    /// from the Bane Conscription Base. Then head north and destroy the 6 Long Range Missile Launchers using the High Grade
    /// Plastic Explosives given to you by Captain Yamamoto. ...", offer "... Take out the mortars along the trench, then sneak
    /// into the west side of the Conscription Base and take out those missile emplacements. ...", turn-in "Hot damn,
    /// soldier. ...", reminder "I'm still seeing bombs dropping everywhere. ..."; objectives (missionobjective) 1-4 "Destroy
    /// Mortar 1" .. "4" 11065/11066 .. 11071/11072, 5-10 "Destroy Launcher 1" .. "6" 11073/11074 .. 11083/11084, no
    /// counters; 11 "Mortar Counter" 11085/11086 counter 11087 "Mortars Destroyed". No objective conversation, no marker names.
    /// Item class 25490 Consc_Missile_Bomb "High Grade Plastic Explosives" (template 97491, stack 10). Launcher class 21873
    /// UsableInertDestBaneMissileLauncherV01 "Missile Launcher" (augmentation 41, targetable; no static of it on the map).
    ///
    /// Sourced: TaRapedia Death from Above: giver and reward giver Captain Aiko (client: Akio) Yamamoto (Boneyard Outpost);
    /// requires Fallen Soldiers and Xanxless Job; 200,000 XP, 8,600 credits; one of Vextronics Incendiary Blade, Pulse Cannon,
    /// Photonic Shotgun, Vitalius Cryogenic Injector Gun. TaRapedia updates 2007-08-21 (known issue): "High Grade Plastic is
    /// not added to inventory when the mission is accepted" (the explosives are given at the accept; here they are).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band, after 1262 and 1263).
    ///  - Mortar n: the kill of the copy AshenConscriptionBaseNpcsV1 put in trench pool n (west to east), once (CreatureKilled,
    ///    no counter), as set 3's 1249. 11 is the count of 1-4 done (an aggregation on counter 0, as Wilderness' Survey Says
    ///    7): it shows the mortars destroyed, a second kill of one mortar does not count again.
    ///  - Six explosives (a mission item, AssignmentIssued, 6) at the accept, taken back at an abandon. Six personal missile
    ///    launchers in the walled yard of the base north of the Garrison (its west side: two rows of three, x 376-428, z 128 and 144), each
    ///    on flat navmesh ground and a client floor; beside each (11 m off its point across its row, outside its model by 3 m
    ///    and more whichever way along the row the model faces) a personal Detonation Device (7334,
    ///    UsableItemAccHumBombV01, use argument 1, 119 to 121, as Marshes' Cleaning House and Buried Gas Lines). The device
    ///    takes one explosive and blows its launcher (110 to 2, shown as a destruction: the launcher keeps a destruction of
    ///    100 HP, as PlateauInterceptionV1's pumps), completing that launcher's objective once. The client lets a player aim
    ///    at a launcher (class 21873's target flag is 1, the Plateau pump's is 0), but the launcher is not interactable and
    ///    the server takes a hit on an object only through PracticeTargetManager.TryGetTarget, which skips a disabled
    ///    object: the hit is dropped, only the device blows it.
    ///  - Markers: r10 on each mortar, r10 on each launcher's device, and 11's r75 over the trench round the four mortars.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (client entityclass names; "Incendiary"
    ///    fire, "Pulse" EMP, "Cannon" the rocket launcher, "Photonic" laser, "Cryogenic Injector" the ice injection gun, as the
    ///    earlier sets read them): Blade Fire 12099 (41-45), Rocket Launcher EMP 15442 (39-43), Shotgun Laser 14980 (37-41),
    ///    Injection Gun Ice 48108 (37-41); one to choose.
    /// </summary>
    public static class AshenDeathFromAboveV1
    {
        public const uint MissionId = 1352;
        public const uint Level = 41;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint Mortars = 4, Launchers = 6;
        public const uint Counter = 11;                 // "Mortar Counter", counter 11087 "Mortars Destroyed"
        public const uint Blown = 1;                    // the scenario of the launchers: one scene result per launcher
        public const double MortarRadius = 10, LauncherRadius = 10, TrenchRadius = 75;

        public const uint ExplosiveTemplate = 97491, ExplosiveClass = 25490;
        public const string ExplosiveKey = "high-grade-plastic-explosives";
        public const uint LauncherClass = 21873;        // UsableInertDestBaneMissileLauncherV01 "Missile Launcher"
        public const uint IntactState = 110, DestroyedState = 2;
        /// <summary>A destruction no hit reaches (the server drops hits on a disabled object), so that the device's blow shows as a destruction (PlateauInterceptionV1.PumpHitPoints).</summary>
        public const uint LauncherHitPoints = 100;
        public const uint DeviceClass = MarshesDevice.Class;            // UsableItemAccHumBombV01 "Detonation Device"
        public const uint DeviceReadyState = MarshesDevice.Ready, DevicePlantedState = MarshesDevice.Planted;
        public const uint UseArgument = MarshesDevice.UseArgument;

        /// <summary>The launchers, 1-6: west to east, the south row then the north row (facing 0: the model runs 6.4 m west to 15.9 m east of its point, 7.4 m north and south; the ground is flat for it facing either way along the row).</summary>
        public static readonly ScenePosition[] LaunchersAt =
        {
            new(376f, 304.15f, 128f), new(400f, 304.1f, 128f), new(428f, 303.92f, 128f),
            new(376f, 304.21f, 144f), new(400f, 304.17f, 144f), new(428f, 304.14f, 144f)
        };

        /// <summary>
        /// Each launcher's Detonation Device, 11 m off its row (south of the south row, north of the north row: 3.5 m outside
        /// the model's 7.5 m half-width whichever way along the row it faces), on navmesh ground within 1 m of the client floor
        /// (probe 2026-11-09; 2, 5 and 6 off the launcher's x: round x 388-404 crates raise the navmesh and hide a device).
        /// </summary>
        public static readonly ScenePosition[] DevicesAt =
        {
            new(376f, 304.24f, 117f), new(412f, 304.13f, 117f), new(428f, 302.68f, 117f),
            new(376f, 304.27f, 155f), new(416f, 304.23f, 155f), new(424f, 304.18f, 155f)
        };

        /// <summary>11's marker: the middle of the four trench mortars, its radius takes in all four.</summary>
        public static readonly (double X, double Y, double Z) TrenchAt = (501.4, 281.59, -174.1);

        public const uint Experience = 200000;
        public const uint Credits = 8600;
        public const uint IncendiaryBlade = 12099;      // Weapon_Avatar_Blade_Fire_UNC_41_to_45
        public const uint PulseCannon = 15442;          // Weapon_Avatar_RocketLauncher_EMP_UNC_39_to_43
        public const uint PhotonicShotgun = 14980;      // Weapon_Avatar_Shotgun_v1_Laser_UNC_37_to_41
        public const uint CryogenicInjector = 48108;    // Weapon_Avatar_InjectionGun_Ice_UNC_37_to_41

        /// <summary>The objective of Mortar <paramref name="number"/> (1-4): the client's objective of the same number.</summary>
        public static uint DestroyMortar(int number) => (uint)number;

        /// <summary>The objective of Launcher <paramref name="number"/> (1-6): the client's 5-10.</summary>
        public static uint DestroyLauncher(int number) => 4 + (uint)number;

        public static string LauncherRole(int number) => $"missile-launcher-{number}";
        public static string DeviceRole(int number) => $"missile-launcher-{number}-detonation-device";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Death from Above", 11060, Yamamoto, Yamamoto, Level);
            mission.Reward(Experience, Credits, new[] { IncendiaryBlade, PulseCannon, PhotonicShotgun, CryogenicInjector });
            mission.Prerequisite(AshenFallenSoldiersV1.MissionId, 1);
            mission.Prerequisite(AshenXanxlessJobV1.MissionId, 2);
            mission.Scenario(Blown, "A missile launcher blown up", Launchers);

            for (var number = 1; number <= Mortars; number++)
            {
                var (creature, _, at) = AshenConscriptionBaseNpcsV1.Mortars[number - 1];
                var objective = DestroyMortar(number);
                mission.Objective(objective, 11063 + 2 * (uint)number, 11064 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature, null);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MortarRadius);
            }
            for (var number = 1; number <= Launchers; number++)
            {
                var objective = DestroyLauncher(number);
                mission.Objective(objective, 11071 + 2 * (uint)number, 11072 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: Blown);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var device = DevicesAt[number - 1];
                mission.Indicator(objective, objective, device.X, device.Y, device.Z, LauncherRadius);
            }
            // 11: the count of 1-4 done (the scene's aggregation on counter 0), as Wilderness' Survey Says 7.
            mission.Objective(Counter, 11085, 11086, Counter - 1, MissionObjectiveState.Incomplete, counterTextId: 11087);
            mission.Indicator(Counter, Counter, TrenchAt.X, TrenchAt.Y, TrenchAt.Z, TrenchRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1352",
                "Native1-4 'Destroy Mortar 1..4' 11065/11066 .. 11071/11072, 5-10 'Destroy Launcher 1..6' 11073/11074 .. " +
                "11083/11084, no counters; 11 'Mortar Counter' 11085/11086 counter11087 'Mortars Destroyed'; no " +
                "objectiveconversation. Texts 11060-11064 (4 trench mortars, 6 launchers with the explosives). Item class25490 " +
                "High Grade Plastic Explosives (template97491). Launcher class21873.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Death_from_Above",
                "Giver/reward giver Captain Aiko Yamamoto (Boneyard Outpost); requires Fallen Soldiers, Xanxless Job; 200000XP, " +
                "8600 credits; Vextronics Incendiary Blade, Pulse Cannon, Photonic Shotgun, Vitalius Cryogenic Injector Gun. " +
                "Updates 2007-08-21: the explosives not given at the accept (known issue).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDeathFromAboveV1",
                "Level41 estimated. Trench pools 592199, 592202-592204 hold copies 790160-790163 of 592011; Mortar n = the kill " +
                "of copy n, once; 11 aggregates 1-4 (counter 0, to 4). Six explosives at the accept; six personal launchers 21873 in " +
                "the base's yard, a Detonation Device 7334 (arg1, 119->121) beside each takes one and blows it (110->2). " +
                "Markers r10, 11 r75. Items 12099, 15442, 14980, 48108 (lowest Uncommon, band of level 41), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The six explosives issued at the accept and the player's own six launchers, each with its Detonation Device.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                ObjectiveAggregations = new()
                {
                    [Counter] = new MissionObjectiveAggregation(Enumerable.Range(1, (int)Mortars).Select(number => DestroyMortar(number)).ToArray(), Mortars, 0)
                },
                Items = new()
                {
                    new MissionItemBinding(ExplosiveKey, ExplosiveTemplate, MissionItemScope.AssignmentIssued, Launchers,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-high-grade-plastic-explosives", MissionId, ExplosiveKey, ExplosiveTemplate, Launchers) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Launchers; number++)
            {
                var launcher = LauncherRole(number);
                var device = DeviceRole(number);
                var sequence = (uint)number;
                var objective = DestroyLauncher(number);
                scene.Actors[launcher] = new SceneActorDefinition(launcher, SceneActorKind.Object, LauncherClass, LaunchersAt[number - 1],
                    InitiallyInteractable: false, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, objective, sequence, LauncherHitPoints, DestroyedState));
                scene.Actors[device] = new SceneActorDefinition(device, SceneActorKind.Object, DeviceClass, DevicesAt[number - 1],
                    InitialObjectState: DeviceReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{launcher}", launcher));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{device}", device));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{device}", device, DevicePlantedState),
                        new SetInteractionIntent($"close-{device}", device, false),
                        new TransitionObjectStateIntent($"blow-{launcher}", launcher, DestroyedState)
                    },
                    Character = { new ConsumeMissionItemIntent($"{device}-explosive", MissionId, ExplosiveKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, Blown, sequence) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The client's bomb acceptor, as Marshes' Buried Gas Lines and Cleaning House use it.</summary>
        internal static class MarshesDevice
        {
            public const uint Class = Marshes.MarshesBuriedGasLinesV1.ReceptacleClass;   // 7334 UsableItemAccHumBombV01
            public const uint Ready = Marshes.MarshesBuriedGasLinesV1.ReadyState;        // 119
            public const uint Planted = Marshes.MarshesBuriedGasLinesV1.PlantedState;    // 121
            public const uint UseArgument = Marshes.MarshesBuriedGasLinesV1.UseArgument; // usabledata lookup[7334][0] = 1
        }
    }
}
