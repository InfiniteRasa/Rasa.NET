using System.Collections.Generic;
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
    /// Sabotage the Tower (native mission 937), Valverde Pools: Lieutenant Feldman at the Snake Pit sends the player
    /// into the Bane fort at the western edge of the region to destroy the equipment round the Eloh obelisk and the
    /// Linkers that guard it.
    ///
    /// Native: texts 5046-5049 and 7605 (missionconversation): log "Destroy the Bane personnel and their equipment
    /// protecting the Eloh Obelisk.", offer "zip inside their fortifications and destroy as much equipment as you can
    /// ... take out any Linkers"; objectives 1 "Destroy the Bane Equipment" 5050/7606, counters 0 = 5059 "Vital Bane
    /// Equipment Destroyed", 1 = 5058 "Bane Equipment Locker Destroyed", 2 = 5057 "Equipment Counter"; 2 "Eliminate the
    /// Linkers guarding the Eloh Relic site." 5051/7607, counter 5060 "Linkers eliminated" (missionobjective); no
    /// objective conversation. Destroyable Bane classes (augmentation 41, a model and a destroyed model): 20594
    /// UsableInertDestBaneGeneratorSmallV01 "Small Generator", 20591 UsableInertDestBaneLockerClosed "Closed Locker".
    ///
    /// Sourced: TaRapedia Sabotage the Tower: giver and receiver Lieutenant Feldman; requires Bane Investigation;
    /// follow-ups Bane Jamming, Elimination; 60,000 XP, 5,250 credits; one of Prodigy Conical Repair Tool level 35,
    /// Vextronics Electric Pistol 35, Teleract Virulent Disperser 32, Eclipse EMP Torqueshell Rifle 32; "Vital Bane
    /// Equipment Destroyed 0 / 6", "Bane Equipment Locker Destroyed 0 / 10", "Linkers eliminated 0 / 4". TR MMO DB:
    /// level 35.
    ///
    /// Reconstructed (ESTIMATE): the vital equipment six personal
    /// Small Generators and the lockers ten personal Closed Lockers (user, 2026-10-25), 100 hit points each, at places
    /// on the fort's navmesh, the generators the six nearest the obelisk (PoolsObeliskV1); objective 1 shows both counts
    /// at once (the engine's counter groups) and completes when both are full; counter 2, which TaRapedia does not show,
    /// is left out. Four personal Linkers (PoolsLinkerV1) round the obelisk from the accept, no loot. The objectives in
    /// any order. "Conical" Repair Tool read as the Direct Repair Tool (Toxic Detox reads "Radial" as the Area one); the
    /// items the lowest Uncommon templates of the bands that hold those levels, one to choose.
    /// </summary>
    public static class PoolsSabotageTheTowerV1
    {
        public const uint MissionId = 937;
        public const uint Level = 35;
        public const uint Feldman = PoolsSnakePitContactsV1.Feldman;
        public const uint Equipment = 1, Linkers = 2;
        public const uint EquipmentScenario = 1, LinkerScenario = 2;

        public const uint GeneratorClass = 20594;       // UsableInertDestBaneGeneratorSmallV01
        public const uint LockerClass = 20591;          // UsableInertDestBaneLockerClosed
        public const uint EquipmentHitPoints = 100;
        public const uint IntactState = 110;            // IdesStateIntact
        public const uint DestroyedState = 2;           // StateDestroyed

        /// <summary>
        /// Objective 1's marker: the fort, round all sixteen pieces (the smallest circle about them is 75 m about
        /// (-845, -44); to-fix Pools #2: a 60 m circle about (-840, -46) left five out, up to 79 m off).
        /// </summary>
        public static readonly (double X, double Y, double Z) FortAt = (-845, 790, -44);
        public const double FortRadius = 80;
        public const uint Linker = PoolsLinkerV1.Linker;

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint DirectRepairTool = 18260;     // Tool_Avatar_Field_Repair_Direct_UNC_35_to_39
        public const uint ElectricPistol = 14526;       // Weapon_Avatar_Pistol_Electric_UNC_35_to_39
        public const uint VirulentDisperser = 15360;    // Weapon_Avatar_PropellantGun_Virulent_UNC_32_to_36
        public const uint EmpTorqueshellRifle = 49588;  // Weapon_Avatar_Torqueshell_Rifle_EMP_UNC_30_to_32

        /// <summary>The vital equipment: scene events 1-6.</summary>
        public static readonly ScenePosition[] Generators =
        {
            new(-868, 791.4f, -40),
            new(-850, 791.37f, 10),
            new(-842, 791.4f, -54),
            new(-874, 791.36f, 8),
            new(-852, 791.6f, 26),
            new(-800, 782.6f, -30),
        };

        /// <summary>The lockers: scene events 7-16.</summary>
        public static readonly ScenePosition[] Lockers =
        {
            new(-876, 791.9f, 24),
            new(-864, 790.44f, -72),
            new(-847.6f, 791f, -105.2f),    // was (-858, 791, -112): 12.4 m off the navmesh (MissionGroundTests)
            new(-840, 792.24f, -70),
            new(-838, 789.73f, -86),
            new(-836, 784.64f, -102),
            new(-834, 792.46f, -118),
            new(-828, 791.79f, 28),
            new(-812, 782.77f, -60),
            new(-810, 782.98f, -100),
        };

        /// <summary>Round the obelisk, on the navmesh.</summary>
        public static readonly ScenePosition[] LinkersAt =
        {
            new(-854, 791.4f, -26),
            new(-854, 791.46f, -14),
            new(-842, 791.4f, -26),
            new(-844, 791.4f, -30),
        };

        public static string EquipmentRole(uint id) => id <= Generators.Length ? $"generator-{id}" : $"locker-{id - Generators.Length}";
        public static string LinkerRole(int index) => $"linker-{index + 1}";

        /// <summary>The scene sequence of Linker <paramref name="index"/>'s death: after the 16 pieces of equipment.</summary>
        public static uint LinkerDown(int index) => 17 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Sabotage the Tower", 5046, Feldman, Feldman, Level);
            mission.Reward(Experience, Credits, new[] { DirectRepairTool, ElectricPistol, VirulentDisperser, EmpTorqueshellRifle });
            mission.Prerequisite(PoolsBaneInvestigationV1.MissionId);

            var pieces = (uint)(Generators.Length + Lockers.Length);
            mission.Objective(Equipment, 5050, 7606, 0, MissionObjectiveState.Incomplete, counterTextId: 5059, counter1TextId: 5058);
            mission.Objective(Linkers, 5051, 7607, 1, MissionObjectiveState.Incomplete, counterTextId: 5060);
            mission.Scenario(EquipmentScenario, "Destroy the Bane equipment round the obelisk", pieces);
            mission.Scenario(LinkerScenario, "Eliminate the Linkers at the obelisk", (uint)LinkersAt.Length);

            // 1: sixteen pieces, two counts.
            mission.Transition(Equipment);
            for (var id = 1U; id <= pieces; id++)
                mission.Progress(Equipment, MissionProgressEventKind.ScenarioEvent, id, pieces, counter: EquipmentScenario, trigger: id);
            mission.Action(Equipment, 1, MissionActionKind.GrantReward, reward: 1);

            // 2: the four Linkers.
            mission.Transition(Linkers);
            for (var id = 1U; id <= LinkersAt.Length; id++)
                mission.Progress(Linkers, MissionProgressEventKind.ScenarioEvent, id, (uint)LinkersAt.Length, counter: LinkerScenario, trigger: id);
            mission.Action(Linkers, 1, MissionActionKind.GrantReward, reward: 1);

            var obelisk = PoolsObeliskV1.At;
            mission.Indicator(Equipment, 1, FortAt.X, FortAt.Y, FortAt.Z, FortRadius);
            mission.Indicator(Linkers, 2, obelisk.X, obelisk.Y, obelisk.Z, 15);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                DefeatSequences = new(),
                CounterGroups = new()
                {
                    [Equipment] = new()
                    {
                        Enumerable.Range(1, Generators.Length).Select(id => (uint)id).ToList(),
                        Enumerable.Range(Generators.Length + 1, Lockers.Length).Select(id => (uint)id).ToList()
                    }
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var places = Generators.Concat(Lockers).ToArray();
            for (var index = 0; index < places.Length; index++)
            {
                var id = (uint)index + 1;
                var role = EquipmentRole(id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object,
                    index < Generators.Length ? GeneratorClass : LockerClass, places[index],
                    Orientation: index * 0.7853981634, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Equipment, id, EquipmentHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, EquipmentScenario, id) }
                };
            }
            for (var index = 0; index < LinkersAt.Length; index++)
            {
                var role = LinkerRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Linker, LinkersAt[index],
                    Orientation: index * 1.5707963268, GameplayPolicy: Hostile());
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.DefeatSequences[role] = LinkerDown(index);
                scene.Sequences[LinkerDown(index)] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, LinkerScenario, (uint)index + 1) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/937",
                "Native1 'Destroy the Bane Equipment' 5050/7606 counters0 5059 'Vital Bane Equipment Destroyed', 1 5058 'Bane " +
                "Equipment Locker Destroyed', 2 5057 'Equipment Counter'; 2 'Eliminate the Linkers...' 5051/7607 counter5060; no " +
                "objectiveconversation. Texts 5046-5049, 7605. Classes 20594 Small Generator, 20591 Closed Locker (augmentation41). " +
                "No obelisk on the 1.16.5 map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Sabotage_the_Tower",
                "Giver/receiver Lieutenant Feldman; requires Bane Investigation; follow-ups Bane Jamming, Elimination; 60000XP, " +
                "5250 credits; one of Prodigy Conical Repair Tool35, Vextronics Electric Pistol35, Teleract Virulent Disperser32, " +
                "Eclipse EMP Torqueshell Rifle32; counts 0/6 vital, 0/10 lockers, 0/4 Linkers. Ten Ton Hammer: the Bane base " +
                "round the Eloh obelisk. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsSabotageTheTowerV1",
                "Six personal Small Generators and ten Closed Lockers (user), 100HP, places estimated; both counts on " +
                "objective1 (counter groups); counter2 left out. Four personal Linkers 760205 (level35, user) round the obelisk " +
                "760017. Any order. Conical read as Direct. Items 18260, 14526, 15360, 49588, one to choose.", 0.3);

            mission.Enable(scene);
        }

        internal static ActorGameplayPolicy Hostile() => new()
        {
            RewardScenarioKills = true, TrackParticipation = true,
            // The run's own creature on a public map: no ordinary loot table can bind it, so none.
            Loot = new AuthoredLootProfile(System.Array.Empty<LootDrop>())
        };

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
