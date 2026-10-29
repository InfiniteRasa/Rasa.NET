using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Bane Battle Plans (native mission 896), Lamna Armory (squad instance 1465), Valverde Pools: Captain Arimoro, just
    /// inside the Armory's entrance, wants the Bane's battle plans from the Armory's computers.
    ///
    /// Native: texts 4716-4719, 7338 (missionconversation): log "Find the battleplans and return them to Captain
    /// Arimoro.", offer "they've leaked the plans to the overlords at the Armory", turn-in "According to these plans the
    /// Bane aren't invading Valverde at all!"; objectives 2 "Acquire the First Blueprint" 4720/7339 counter 4721 "Hard
    /// Drives Acquired", 3 "Acquire the Second Blueprint" 5961/7340, 4 "Acquire the Third Blueprint" 5962/7341, 5 "Acquire
    /// the Blueprints" 5963/7342 (missionobjective); no objective conversation. Item: 42438 MisItemLamnaBluePrints (class
    /// 21298). The client's map of the Armory places no console.
    ///
    /// Sourced: TaRapedia Bane Battle Plans (Lamna Armory): giver and receiver Captain Arimoro; no requirement; 60,000 XP,
    /// 5,250 credits; one of Dynamo Conical Repair Tool lvl 35, Shinobi Electric Pistol lvl 35, Vitalius Virulent Disperser
    /// lvl 32, AccuMax EMP Torqueshell Rifle lvl 33; "computer cards found in computer consoles in the chambers immediately
    /// to the west of the mission givers' room", with a boss there. Ten Ton Hammer Lamna Armory guide: "Head up the ramp
    /// to collect the three hard drives that represent the Bane Battle Plans", where Gaite spawns. TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Three personal Bane keyboards (class 20631 UsableStatelessBaneConsoleKeyboard, as Monkey Wrench's) in the
    ///    command room west of the givers (Gaite's room, PoolsLamnaArmoryNpcsV1), one for each of 2, 3 and 4, in any
    ///    order; each gives one blueprint and goes dead.
    ///  - 5 an aggregate of 2-4; a server-only hidden objective 6 completes on 5 and pays. 2's counter text is not used
    ///    (each of 2-4 is one drive).
    ///  - Arimoro takes the three blueprints at the turn-in. Rewards the lowest Uncommon templates of the bands that hold
    ///    those levels ("Conical" Repair Tool the Direct one, as Sabotage the Tower's), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsBaneBattlePlansV1
    {
        public const uint MissionId = 896;
        public const uint Level = 35;
        public const uint MapContextId = PoolsLamnaArmoryNpcsV1.MapContextId;
        public const uint Arimoro = PoolsLamnaArmoryNpcsV1.Arimoro;
        public const uint All = 5, Paid = 6;
        public const uint Downloaded = 1;
        public const double ObjectRadius = 8;

        public const uint ConsoleClass = Plateau.PlateauMonkeyWrenchV1.KeyboardClass;     // UsableStatelessBaneConsoleKeyboard
        public const uint ConsoleState = Plateau.PlateauMonkeyWrenchV1.KeyboardState, UseArgument = Plateau.PlateauMonkeyWrenchV1.UseArgument;
        public const uint Blueprint = 42438;            // MisItemLamnaBluePrints
        public const string BlueprintKey = "lamna-blueprints";

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint DirectRepairTool = PoolsSabotageTheTowerV1.DirectRepairTool;   // Tool_Avatar_Field_Repair_Direct_UNC_35_to_39
        public const uint ElectricPistol = PoolsSabotageTheTowerV1.ElectricPistol;       // Weapon_Avatar_Pistol_Electric_UNC_35_to_39
        public const uint VirulentDisperser = PoolsSabotageTheTowerV1.VirulentDisperser; // Weapon_Avatar_PropellantGun_Virulent_UNC_32_to_36
        public const uint EmpTorqueshellRifle = 49592;  // Weapon_Avatar_Torqueshell_Rifle_EMP_UNC_33_to_37

        /// <summary>The three drives: objective, name and body texts, the keyboard's place in the command room.</summary>
        public static readonly (uint Objective, uint Name, uint Body, ScenePosition At)[] Drives =
        {
            (2, 4720, 7339, new(297.9f, 8.01f, -9.3f)),
            (3, 5961, 7340, new(297.8f, 8.01f, -22.7f)),
            (4, 5962, 7341, new(329.8f, 8.01f, -22.6f)),
        };

        public static string ConsoleRole(int index) => $"battle-plans-console-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Bane Battle Plans", 4716, Arimoro, Arimoro, Level);
            mission.Reward(Experience, Credits, new[] { DirectRepairTool, ElectricPistol, VirulentDisperser, EmpTorqueshellRifle });

            var ordinal = 0U;
            foreach (var (objective, name, body, _) in Drives)
                mission.Objective(objective, name, body, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(All, 5963, 7342, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(Paid, 5963, 7342, ordinal, MissionObjectiveState.Incomplete);
            mission.Scenario(Downloaded, "Battle plans taken from the Armory's computers", (uint)Drives.Length);

            for (var index = 0; index < Drives.Length; index++)
            {
                mission.Transition(Drives[index].Objective);
                mission.Progress(Drives[index].Objective, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: Downloaded);
            }

            // 6: once 5 has all three, the mission pays.
            mission.Transition(Paid);
            mission.ObjectiveState(Paid, All, MissionObjectiveState.Completed);
            mission.Action(Paid, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,   // taken and played in the Armory
                HiddenObjectiveIds = new() { Paid },
                ObjectiveAggregations = new()
                {
                    [All] = new MissionObjectiveAggregation(new[] { Drives[0].Objective, Drives[1].Objective, Drives[2].Objective },
                        (uint)Drives.Length)
                },
                Items = new()
                {
                    new MissionItemBinding(BlueprintKey, Blueprint, MissionItemScope.AssignmentIssued, (uint)Drives.Length,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: (uint)Drives.Length)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Drives.Length; index++)
            {
                var (objective, _, _, at) = Drives[index];
                var role = ConsoleRole(index);
                var sequence = (uint)index + 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ConsoleClass, at,
                    Orientation: index * 2.0943951024, InitialObjectState: ConsoleState,
                    UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"used-{role}", role, false) },
                    Character = { new IssueMissionItemIntent($"blueprint-from-{role}", MissionId, BlueprintKey, Blueprint, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Downloaded, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/896",
                "Native2 'Acquire the First Blueprint' 4720/7339 counter4721 'Hard Drives Acquired', 3 second 5961/7340, 4 third " +
                "5962/7341, 5 'Acquire the Blueprints' 5963/7342; no objectiveconversation. Texts 4716-4719, 7338. Item 42438 " +
                "MisItemLamnaBluePrints. No console on the Armory's map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bane_Battle_Plans_(Lamna_Armory)",
                "Giver/receiver Captain Arimoro (Lamna Armory); no requirement; 60000XP, 5250 credits; one of Dynamo Conical " +
                "Repair Tool35, Shinobi Electric Pistol35, Vitalius Virulent Disperser32, AccuMax EMP Torqueshell Rifle33; consoles " +
                "in the chambers west of the givers' room. Ten Ton Hammer: three hard drives up the ramp. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsBaneBattlePlansV1",
                "Three personal Bane keyboards (20631) in the command room west of the givers, any order, one blueprint each; 5 " +
                "an aggregate of 2-4, hidden 6 pays; Arimoro takes the three. Items 18260, 14526, 15360, 49592, one to choose. " +
                "Places are estimates. Indicators in the instance too.", 0.3);

            // 2-4 at their keyboards, 5 at all three.
            for (var index = 0; index < Drives.Length; index++)
            {
                var (objective, _, _, at) = Drives[index];
                mission.Indicator(objective, 1 + (uint)index, at.X, at.Y, at.Z, ObjectRadius);
                mission.Indicator(All, 1 + (uint)(Drives.Length + index), at.X, at.Y, at.Z, ObjectRadius);
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
