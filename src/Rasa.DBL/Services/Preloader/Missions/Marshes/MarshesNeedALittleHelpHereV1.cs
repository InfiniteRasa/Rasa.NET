using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Need a Little Help Here (native mission 1873), Valverde Marshes: the Bane ambushed the last supply run and took the
    /// marsh sickness medical boosts; Archer Thoung asks the player to find them and inject four of those stationed along the
    /// marsh.
    ///
    /// Native: texts 19149-19153 (missionconversation): log "Gather Marsh Sickness Medical Boost from the Bane and give them to
    /// soldiers stationed along the marsh.", offer "the squad was ambushed and the Bane took their supplies", reminder "Try
    /// heading north.", turn-in "Thank you, my brother"; objectives (missionobjective) 1 "Find supplies stolen by the Bane's
    /// northbound patrol." 19565/19566, 2 "Inject Pvt. Brady with the marsh sickness medical boost." 19567/19568, 3 Archer Zui
    /// 19570/19571, 4 Warrior Dahn 19573/19574, 5 Elder Nez 19576/19577, no counters; objective conversations (flag 1,
    /// COMPLETION) 1873,2 package 2375 text 19569, 1873,3 2376 / 19572, 1873,4 2377 / 19575, 1873,5 2378 / 19578. Item
    /// Medical Booster Pack class 28640 MisMarshesLittleHelpHereSupplies (World DB template 119001).
    ///
    /// Sourced: TaRapedia Need a Little Help Here: giver and receiver Archer Thoung (the sentry tower between the CPs); level
    /// 37; 70,000 XP, 5,550 credits; injectees Pvt. Brady, Elder Nez, Warrior Dahn, Archer Zui; one of Modification: Threat
    /// Reduction, Armor Piercing, Crit Hit Increase, Armor Absorption lvl 30. Ten Ton Hammer Eastern Marshes (the group-3
    /// mapping): Thoung at -127, 220, -341; the supplies in "the outpost where Overseer Nelg lives at 170,224,55"; 5,550 credits
    /// and a choice of Armor Modification Recipe. No requirement on TaRapedia.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - 1: four personal Bane item crates (11116) at Nelg's post, one pack each; the four packs complete 1 and open 2-5.
    ///    The post's floor (224.68, x 160-196, z 45-72) is a navmesh island (probe of 2026-11-02): three crates stand on the
    ///    walked-to deck east of it, one on the ground below the deck. A hidden tally (objective 6, under 1's texts: 1 has
    ///    no client counter text) counts the four. The patrol drop of the client text is not built.
    ///  - 2-5 in any order: each talk (the client's package) takes one pack and completes its objective.
    ///  - The packs are given only by the crates (at most four) and go with the mission at an abandon.
    ///  - The rewards Bane Investigation's four grade [4] modules (the user's reading of "Modification", 932: Weapon Module Crit
    ///    Hit Bonus, Armor Module Armor Absorption, Tool Module Threat Reduction, Weapon Module Armor Piercing); the client has no
    ///    per-mission block for them. Markers: round the crates for 1, at each soldier for 2-5.
    /// </summary>
    public static class MarshesNeedALittleHelpHereV1
    {
        public const uint MissionId = 1873;
        public const uint Level = 37;
        public const uint Thoung = MarshesSentryTowerTwoNpcsV1.Thoung;
        public const uint Find = 1, Brady = 2, Zui = 3, Dahn = 4, Nez = 5;
        public const uint Tally = 6;                    // hidden: the four packs (1 has no client counter text)
        public const uint PacksTaken = 1;

        public const uint PackTemplate = 119001, PackClass = 28640, Packs = 4;
        public const string PackKey = "marsh-sickness-boost";
        public const uint CrateClass = MarshesCoreOfTheMatterV1.CrateClass;
        public const uint UseArgument = MarshesCoreOfTheMatterV1.UseArgument;
        public const uint ReadyState = MarshesCoreOfTheMatterV1.ReadyState, TakenState = MarshesCoreOfTheMatterV1.TakenState;
        public static readonly ScenePosition[] Crates =
        {
            new(176.8f, 224.68f, 41.2f),
            new(181.8f, 224.68f, 45.8f),
            new(175.5f, 224.68f, 44f),      // on the open deck (client floor 224.55), not in the railing at 183.2, 42.8 (to-fix 27)
            new(170f, 216.1f, 40f),
        };
        public static readonly (double X, double Y, double Z) CratesAt = (178, 224.68, 43);
        public const double CratesRadius = 15;

        /// <summary>The soldiers: objective, name and body text, the client's package and its completion text, where.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Package, uint Npc, (double X, double Y, double Z) At)[] Soldiers =
        {
            (Brady, 19567, 19568, MarshesSentryTowerTwoNpcsV1.BradyPackage, MarshesSentryTowerTwoNpcsV1.Brady, MarshesSentryTowerTwoNpcsV1.BradyAt),
            (Zui, 19570, 19571, MarshesSentryTowerTwoNpcsV1.ZuiPackage, MarshesSentryTowerTwoNpcsV1.Zui, MarshesSentryTowerTwoNpcsV1.ZuiAt),
            (Dahn, 19573, 19574, MarshesSentryTowerTwoNpcsV1.DahnPackage, MarshesSentryTowerTwoNpcsV1.Dahn, MarshesSentryTowerTwoNpcsV1.DahnAt),
            (Nez, 19576, 19577, MarshesSentryTowerTwoNpcsV1.NezPackage, MarshesSentryTowerTwoNpcsV1.Nez, MarshesSentryTowerTwoNpcsV1.NezAt),
        };
        public const double SoldierRadius = 8;

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint CritHitBonus = Pools.PoolsBaneInvestigationV1.CritHitBonus;         // Weapon Module: Crit Hit Bonus [4]
        public const uint ArmorAbsorption = Pools.PoolsBaneInvestigationV1.ArmorAbsorption;   // Armor Module: Armor Absorption [4]
        public const uint ThreatReduction = Pools.PoolsBaneInvestigationV1.ThreatReduction;   // Tool Module: Threat Reduction [4]
        public const uint ArmorPiercing = Pools.PoolsBaneInvestigationV1.ArmorPiercing;       // Weapon Module: Armor Piercing [4]

        public static string CrateRole(int index) => $"supply-crate-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Need a Little Help Here", 19149, Thoung, Thoung, Level);
            mission.Reward(Experience, Credits, new[] { CritHitBonus, ArmorAbsorption, ThreatReduction, ArmorPiercing });

            mission.Objective(Find, 19565, 19566, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 19565, 19566, 5, MissionObjectiveState.Incomplete);
            foreach (var (objective, name, body, _, _, _) in Soldiers)
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Inactive);
            mission.Scenario(PacksTaken, "A marsh sickness boost taken from a crate", (uint)Crates.Length);

            // 1: the four packs; done, it opens the four injections.
            mission.Transition(Find);
            mission.ObjectiveState(Find, Tally, MissionObjectiveState.Completed);
            var action = 1U;
            foreach (var (objective, _, _, _, _, _) in Soldiers)
            {
                mission.Action(Find, action++, MissionActionKind.RevealObjective, targetObjective: objective);
                mission.Action(Find, action++, MissionActionKind.ActivateObjective, targetObjective: objective,
                    state: MissionObjectiveState.Incomplete);
            }
            mission.Action(Find, action, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Tally);
            for (var id = 1U; id <= Crates.Length; id++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Packs, counter: PacksTaken, trigger: id);
            mission.Indicator(Find, 1, CratesAt.X, CratesAt.Y, CratesAt.Z, CratesRadius);

            // 2-5: each soldier's talk takes a pack.
            foreach (var (objective, _, _, package, _, at) in Soldiers)
            {
                mission.Transition(objective);
                mission.Conversation(objective, package);
                mission.Action(objective, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent($"inject-{objective}", MissionId, PackKey, 1, MissionItemScope.AssignmentIssued));
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, SoldierRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1873",
                "Native1 'Find supplies stolen by the Bane's northbound patrol.' 19565/19566, 2-5 'Inject Pvt. Brady/Archer Zui/" +
                "Warrior Dahn/Elder Nez with the marsh sickness medical boost.' 19567-19577, no counters; objectiveconversation " +
                "1873,2-5 packages 2375-2378 texts 19569, 19572, 19575, 19578. Texts 19149-19153. Item class28640 Medical Booster " +
                "Pack (template119001). Names 10324, 10335-10338.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Need_a_Little_Help_Here",
                "Giver/receiver Archer Thoung; level37; 70000XP, 5550 credits; Brady, Nez, Dahn, Zui; Modification: Threat " +
                "Reduction/Armor Piercing/Crit Hit Increase/Armor Absorption lvl30. Ten Ton Hammer: Thoung -127,220,-341; the " +
                "supplies at Nelg's outpost 170,224,55.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesNeedALittleHelpHereV1",
                "Four personal Bane crates 11116 (arg3) by Nelg's post (its floor a navmesh island: three on the deck, one below), a " +
                "pack (119001) each; hidden tally6 counts four, 1 completes with it and opens 2-5; each talk takes one. Soldiers 760321-760324 at the tower foot. " +
                "Modules [4] 123161, 122951, 123141, 123146 (as 932). Markers at the crates and each soldier.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(PackKey, PackTemplate, MissionItemScope.AssignmentIssued, Packs,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var sequence = (uint)index + 1;
                var role = CrateRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, Crates[index],
                    Orientation: index * 1.5707963268, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Find, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-pack", MissionId, PackKey, PackTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, PacksTaken, sequence) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
