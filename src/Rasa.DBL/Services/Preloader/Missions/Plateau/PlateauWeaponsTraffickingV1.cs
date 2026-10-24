using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Weapons Trafficking (native mission 904), Valverde Pools: Amee Corman at the Retread Camp wants five of the Bane's
    /// prototype weapons from the Lamna Armory (Lamnas Armory, squad instance 1465) before she says what she knows of
    /// the Eloh. The next step of Bosley's chain after New Orders From The General (1541).
    ///
    /// Native: texts 4788-4791, 7347 (missionconversation): offer "You bring me five prototype weapons from Lamna and
    /// I'll tell you what I know about the Eloh.", turn-in "I'll just take them off your hands."; objectives
    /// (missionobjective), no counters, empty descriptions: 1 "Get The Prototype Weapons" 4792/7348, 2-6 "Find
    /// Prototype 1".."Find Prototype 5" 5964-5968 / 7349-7353; no objective conversation. Items (class, template):
    /// MisItemLamnaPrototypeWeaponRifle 21322/42441, Pistol 23909/47457, Netgun 23910/47458, Injector 23912/47460,
    /// GrenadeLauncher 23911/47459. The client's maps place no footlocker.
    ///
    /// Sourced: TaRapedia Weapons Trafficking: giver and receiver Amee Corman, Retread Camp; requires New Orders From
    /// The General; 65,000 XP, 5,400 credits, one of AccuMax Incendiary Blade lvl 36, ChiTech Conical Healing Disc lvl
    /// 35, Teleract Cryogenic Shotgun lvl 37, Vitalius Virulent Staff lvl 35; objectives Prototype Rifle, Pistol, Net
    /// Gun, Injector Gun, Grenade Launcher; "The items must be found in sequence ... you will pass Bane footlockers that
    /// will eventually open with the items but not until you retrace your path." TR MMO DB: level 33, "obtain the
    /// weapon prototypes from Bane Footlockers, but they must be obtained in order".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24):
    ///  - The footlockers: five personal Bane item crates (class 11116 UsableItemDisp_item_crate_bane_v01, an item
    ///    dispenser with the client's use argument 3, states 81/82, as Beer Hunter's plants; the Bane locker classes
    ///    are destructibles with no use). The scene is bound to the Armory: they stand in the squad's copy from the
    ///    player's first arrival, all five, and only the one of the open objective gives its prototype.
    ///  - Their places: five Armory rooms on the navmesh, not in path order, so a player passes later ones.
    ///  - 1 completes with the fifth prototype; Amee takes the five at the turn-in.
    ///  - Rewards: the nearest Uncommon template of each kind (manufacturer names are not in the client tables).
    ///    No indicators: instance.
    /// </summary>
    public static class PlateauWeaponsTraffickingV1
    {
        public const uint MissionId = 904;
        public const uint Level = 33;
        public const uint MapContextId = 1465;          // adv_foreas_valverde_pools_test_weapons_center, Lamnas Armory
        public const uint Amee = PlateauNewOrdersFromTheGeneralV1.Amee;
        public const uint GetThem = 1;
        public const uint Taken = 1;

        public const uint CrateClass = 11116;           // UsableItemDisp_item_crate_bane_v01
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint IncendiaryBlade = 45726;      // Weapon_Avatar_Blade_Fire_UNC_36_to_40
        public const uint HealingDisc = 120030;         // Tool_Avatar_Healing_Disc_Area_UNC_35_to_39 ("conical" read as area)
        public const uint CryogenicShotgun = 14899;     // Weapon_Avatar_Shotgun_v1_Ice_UNC_37_to_41
        public const uint VirulentStaff = 15247;        // Weapon_Avatar_Staff_Virulent_UNC_35_to_39

        /// <summary>Prototypes 1-5: (objective, name text, body text, item key, template, crate place on the navmesh).</summary>
        public static readonly (uint Objective, uint Name, uint Body, string Key, uint Template, ScenePosition At)[] Prototypes =
        {
            (2, 5964, 7349, "prototype-rifle", 42441, new(240, -22.99f, 0.6f)),             // lower service room, 269 m
            (3, 5965, 7350, "prototype-pistol", 47457, new(56, 17.21f, -264)),               // far west service room, 810 m
            (4, 5966, 7351, "prototype-net-gun", 47458, new(240, 17.01f, 72)),               // upper service room, 178 m
            (5, 5967, 7352, "prototype-injector-gun", 47460, new(80, 8.01f, -16)),           // west control room, 710 m
            (6, 5968, 7353, "prototype-grenade-launcher", 47459, new(184, -24.19f, -107)),   // south control room, 447 m
        };

        public static string CrateRole(int index) => $"footlocker-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Weapons Trafficking", 4788, Amee, Amee, Level);
            mission.Reward(Experience, Credits, new[] { IncendiaryBlade, HealingDisc, CryogenicShotgun, VirulentStaff });
            mission.Prerequisite(PlateauNewOrdersFromTheGeneralV1.MissionId);

            mission.Objective(GetThem, 4792, 7348, 0, MissionObjectiveState.Incomplete);
            foreach (var prototype in Prototypes)
                mission.Objective(prototype.Objective, prototype.Name, prototype.Body, prototype.Objective - 1,
                    prototype.Objective == Prototypes[0].Objective ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);
            mission.Scenario(Taken, "Prototypes taken from the footlockers", (uint)Prototypes.Length);

            // 2-6: each from its footlocker, in order; each opens the next.
            for (var index = 0; index < Prototypes.Length; index++)
            {
                var objective = Prototypes[index].Objective;
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: Taken);
                if (index + 1 < Prototypes.Length)
                    PlateauSilentButDeadlyV1.Open(mission, objective, 1, Prototypes[index + 1].Objective);
            }

            // 1: done with the fifth; Amee takes the five at the turn-in.
            mission.Transition(GetThem);
            mission.ObjectiveState(GetThem, Prototypes[^1].Objective, MissionObjectiveState.Completed);
            mission.Action(GetThem, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,   // accepted at Amee in Valverde Pools, played in the Armory
                Items = new()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Prototypes.Length; index++)
            {
                var (objective, _, _, key, template, at) = Prototypes[index];
                var role = CrateRole(index);
                var sequence = (uint)index + 1;
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove, TurnInQuantity: 1));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, at,
                    Orientation: index * 1.2566370614, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"empty-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{key}-from-{role}", MissionId, key, template, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Taken, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/904",
                "Native1 get them 4792/7348, 2-6 find prototype 1-5 5964-5968/7349-7353, no counters, no objective conversation. " +
                "Texts 4788-4791, 7347 (prototypes from Lamna). Items 21322/42441, 23909/47457, 23910/47458, 23912/47460, " +
                "23911/47459. No footlocker on the maps.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Weapons_Trafficking",
                "Giver/receiver Amee Corman, Retread Camp; requires New Orders From The General; 65000XP, 5400 credits; AccuMax " +
                "Incendiary Blade lvl36/ChiTech Conical Healing Disc lvl35/Teleract Cryogenic Shotgun lvl37/Vitalius Virulent Staff " +
                "lvl35; rifle, pistol, net gun, injector gun, grenade launcher in sequence from Bane footlockers. TR MMO DB level33.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauWeaponsTraffickingV1",
                "Five personal Bane item crates (class11116, arg3, states81/82) in Lamnas Armory 1465 rooms, all there from the " +
                "first arrival, only the open objective's one gives; places not in path order. 1 with the fifth; Amee takes them " +
                "at the turn-in. Items 45726, 120030, 14899, 15247. Places are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
