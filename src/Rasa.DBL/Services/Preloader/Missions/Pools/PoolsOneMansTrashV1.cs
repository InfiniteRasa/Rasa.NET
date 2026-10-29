using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// One Man's Trash (native mission 562), Valverde Pools: Austin Buckley at the Retread Camp wants Thrax ammunition from
    /// the dump by the Thrax Barracks and the soldiers' ration packs from the Snake Pit.
    ///
    /// Native: texts 1638-1641 and 7612 (missionconversation): log "Steal an ammunition crate from the Thrax ammo depot near
    /// the Thrax Barracks and a Ration Pack from the Snake Pit", offer "There's a Thrax ammo dump near their Barracks to the
    /// east ... even though they keep guards posted ... get some of those special rations you guys have stored up in the
    /// Snake Pit!"; objectives 1 "Collect Thrax Ammo Crates" 1651/7613, 2 "Collect Ration Packs" 1652/7614, no counters
    /// (missionobjective); no objective conversation. Items: Thrax Ammo Crate 705 (class 7783), Ration Packs 704 (class
    /// 7782). Dispenser classes 7832 UsableItemDispThraxammo and 7831 UsableItemDispRationpacksDELETE (augmentation 34).
    ///
    /// Sourced: TaRapedia One Man's Trash: giver and receiver Austin Buckley, Retread Camp; requires A Cult Divided;
    /// follow-up Retread Planning; 55,000 XP, 5,100 credits; one of Astra Hazmat Armor Helmet level 34, Phoenix Mech Armor
    /// Vest 35, Phoenix Motor Assist Helmet 33, Teleract Reflective Helmet 34; walkthrough: the ration packs in a container
    /// in the Snake Pit at (-953, 919, 458). TR MMO DB: level 33. Warcry and PlanetTR known issues (2008): the containers
    /// that hold the items did not respawn.
    ///
    /// Reconstructed (ESTIMATE): one crate and one ration pack (the log's "an ammunition crate" and "a Ration Pack");
    /// personal containers, so none waits for another player: the ammo crate (class 11116, a Bane item crate: 7832 has the
    /// client's "Default" mesh 776, to-fix Pools #5) by the Thrax Barracks' south wall
    /// (the user's choice: an estimated place; the client map places no dump), the ration crate at TaRapedia's place (an
    /// AFS item crate, class 11078, as The Thirsty Hermit's cabinet: 7831 is marked deleted). Each is usable once, gives
    /// its item and completes its objective, in either order; the items go at the turn-in. No guards are added (the
    /// Barracks' own Thrax stand there). The items the lowest Uncommon templates of the lines and bands that hold those
    /// names and levels (Mech V04 for TaRapedia's "4"), one to choose.
    /// </summary>
    public static class PoolsOneMansTrashV1
    {
        public const uint MissionId = 562;
        public const uint Level = 33;
        public const uint Buckley = PoolsRetreadCampNpcsV1.Buckley;
        public const uint Ammo = 1, Rations = 2;
        public const uint CrateScenario = 1;
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;

        public const uint Experience = 55000;
        public const uint Credits = 5100;
        public const uint HazmatHelmet = 20633;         // Armor_T2_Hazmat_V02_UNC_Helmet_34_to_38
        public const uint MechVest = 34483;             // Armor_T3_Mech_V04_UNC_Vest_35_to_39
        public const uint MotorAssistHelmet = 28920;    // Armor_T1_MotorAssist_V04_UNC_Helmet_33_to_37
        public const uint ReflectiveHelmet = 35869;     // Armor_T2_Reflective_V02_UNC_Helmet_34_to_38

        /// <summary>The two containers: objective, item template and class, item key, container class, place (scene events 1, 2).</summary>
        public static readonly (uint Objective, uint Template, uint Class, string Key, uint CrateClass, ScenePosition At)[] Crates =
        {
            (Ammo, 705, 7783, "thrax-ammo-crate", 11116, new(500, 680.4f, 460)),
            (Rations, 704, 7782, "ration-packs", 11078, new(-953, 919.2f, 458)),
        };

        public static string CrateRole(int index) => index == 0 ? "ammo-crate" : "ration-crate";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "One Man's Trash", 1638, Buckley, Buckley, Level);
            mission.Reward(Experience, Credits, new[] { HazmatHelmet, MechVest, MotorAssistHelmet, ReflectiveHelmet });
            mission.Prerequisite(PoolsCultDividedV1.MissionId);

            mission.Objective(Ammo, 1651, 7613, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Rations, 1652, 7614, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(CrateScenario, "A container emptied", (uint)Crates.Length);

            var scene = new MissionSceneDefinition { Script = "data.sequence", Items = new() };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var (objective, template, _, key, crateClass, at) = Crates[index];
                var sequence = (uint)index + 1;
                var role = CrateRole(index);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, sequence, null, counter: CrateScenario);
                if (index == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, sequence, at.X, at.Y, at.Z);

                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, crateClass, at,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-taken", MissionId, key, template, 1) },
                    Signals = { new SceneMissionSignal(MissionId, CrateScenario, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/562",
                "Native1 'Collect Thrax Ammo Crates' 1651/7613, 2 'Collect Ration Packs' 1652/7614, no counters; no " +
                "objectiveconversation. Texts 1638-1641, 7612. Ammo crate template705/class7783, ration packs 704/7782; " +
                "dispensers 7832 (Thraxammo), 7831 (Rationpacks, marked DELETE).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/One_Man%27s_Trash",
                "Giver/receiver Austin Buckley (Retread Camp); requires A Cult Divided; follow-up Retread Planning; 55000XP, 5100 " +
                "credits; one of Astra Hazmat Helmet34, Phoenix Mech Vest35, Phoenix Motor Assist Helmet33, Teleract Reflective " +
                "Helmet34; ration packs at -953,919,458. TR MMO DB level33. Warcry/PlanetTR: containers did not respawn.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsOneMansTrashV1",
                "One of each; personal ammo crate (11116; 7832 has the Default mesh) by the Thrax Barracks at 500,680.4,460 (estimated), personal ration " +
                "crate (11078) at TaRapedia's place; either order. Items 20633, 34483, 28920, 35869, one to choose.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
