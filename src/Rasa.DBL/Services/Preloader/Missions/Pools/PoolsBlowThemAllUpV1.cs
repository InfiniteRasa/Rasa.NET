using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Blow Them All Up (native mission 903), Lamna Armory (squad instance 1465), Valverde Pools: Colonel Herring, just
    /// inside the Armory's entrance, wants the Bane's weapons lockers in the Armory destroyed.
    ///
    /// Native: texts 4782-4785, 7343 (missionconversation): log "Find and destroy all 20 Bane Weapons Lockers in Lamna
    /// Armory and return to Colonel Helling when the job is done.", offer "They keep weapons lockers scattered around the
    /// interior", turn-in "There is nothing like the smell of a burning weapons cache is there?"; objective 1 "Destroy 20
    /// Weapons Lockers" 4786/7344, counter 4787 "Weapons Lockers Destroyed" (missionobjective); no objective conversation.
    /// The client's map of the Armory places no locker. Destroyable Bane locker: 20591 UsableInertDestBaneLockerClosed
    /// (as Sabotage the Tower's).
    ///
    /// Sourced: TaRapedia Blow Them All Up: giver and receiver Colonel Herring (Lamna Armory); no requirement; 100,000 XP,
    /// 6,600 credits; one of ChiTech Chaingun lvl 33, AccuMax Incendiary Net Gun lvl 31, Vextronics Exothermic Polarity Gun
    /// lvl 33, ChiTech Shotgun lvl 32; "fire over his right shoulder. One weapons locker down!". Ten Ton Hammer Lamna
    /// Armory guide: the first locker by the medic, more on the ramp where the battle plans are, past the laser gate on the
    /// walkway, and up the staircase of the green stasis room (Research and Development), "which should take you to twenty
    /// by now". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE): twenty personal Closed Lockers, 100 hit points each, at places on the navmesh along that
    /// route (the entrance hall by the medic, the command room west of it, the lower hall and the labs below), each with
    /// a complete path from the door; there from the accept in the squad's copy. Rewards the lowest Uncommon templates of
    /// the bands that hold those levels ("Chaingun" the Machine Gun, "Exothermic" Fire), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsBlowThemAllUpV1
    {
        public const uint MissionId = 903;
        public const uint Level = 35;
        public const uint MapContextId = PoolsLamnaArmoryNpcsV1.MapContextId;
        public const uint Herring = PoolsLamnaArmoryNpcsV1.Herring;
        public const uint Destroy = 1, Destroyed = 1;

        public const uint LockerClass = PoolsSabotageTheTowerV1.LockerClass;
        public const uint LockerHitPoints = 100;
        public const double ObjectRadius = 8;
        public const uint IntactState = PoolsSabotageTheTowerV1.IntactState, DestroyedState = PoolsSabotageTheTowerV1.DestroyedState;

        public const uint Experience = 100000;
        public const uint Credits = 6600;
        public const uint Chaingun = 48296;             // Weapon_Avatar_MachineGun_Physical_UNC_33_to_37
        public const uint IncendiaryNetGun = 48360;     // Weapon_Avatar_NetGun_Fire_UNC_31_to_35
        public const uint ExothermicPolarityGun = 48676; // Weapon_Avatar_PolarityGun_Fire_UNC_33_to_37
        public const uint Shotgun = 14815;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_32_to_36

        /// <summary>The lockers, in the route's order (scene events 1-20); the walk from the door in the comment.</summary>
        public static readonly ScenePosition[] Lockers =
        {
            new(376f, 9.68f, -16f),     //  33 m, entrance hall
            new(376f, 9.86f, -32f),     //  40 m, by the medic
            new(376f, 9.81f, 0f),       //  40 m
            new(360f, 9.61f, -16f),     //  49 m
            new(376f, 5.9f, 16f),       //  56 m, the ramp down
            new(344f, 9.64f, -16f),     //  65 m
            new(376f, 1.41f, 32f),      //  73 m
            new(329.8f, 8.01f, -9.4f),  //  81 m, command room
            new(375.6f, 1.21f, 47.9f),  //  89 m
            new(320f, 7.81f, -24f),     //  90 m
            new(312f, 7.85f, -8f),      //  98 m
            new(312f, 8.54f, 8f),       // 107 m
            new(312f, 8.59f, -40f),     // 108 m
            new(359.9f, -5.41f, 63.6f), // 111 m, lower hall
            new(280f, -15.49f, 56f),    // 195 m, labs below
            new(280f, -24f, 40f),       // 213 m, stasis room
            new(264.7f, -23.99f, 39.4f),// 229 m
            new(295.2f, -23.99f, 39.2f),// 229 m
            new(271.9f, -23.79f, 24f),  // 231 m
            new(288.3f, -24.07f, 24f),  // 231 m
        };

        public static string LockerRole(int index) => $"weapons-locker-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Blow Them All Up", 4782, Herring, Herring, Level);
            mission.Reward(Experience, Credits, new[] { Chaingun, IncendiaryNetGun, ExothermicPolarityGun, Shotgun });

            var count = (uint)Lockers.Length;
            mission.Objective(Destroy, 4786, 7344, 0, MissionObjectiveState.Incomplete, counterTextId: 4787);
            mission.Scenario(Destroyed, "Bane weapons lockers destroyed", count);
            mission.Transition(Destroy);
            for (var id = 1U; id <= count; id++)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, id, count, counter: Destroyed, trigger: id);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId    // taken and played in the Armory
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Lockers.Length; index++)
            {
                var id = (uint)index + 1;
                var role = LockerRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, LockerClass, Lockers[index],
                    Orientation: index * 0.7853981634, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy, id, LockerHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Destroyed, id) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/903",
                "Native1 'Destroy 20 Weapons Lockers' 4786/7344 counter4787 'Weapons Lockers Destroyed'; no objectiveconversation. " +
                "Texts 4782-4785, 7343. No locker on the Armory's map; locker class 20591.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Blow_Them_All_Up",
                "Giver/receiver Colonel Herring (Lamna Armory); no requirement; 100000XP, 6600 credits; one of ChiTech Chaingun33, " +
                "AccuMax Incendiary Net Gun31, Vextronics Exothermic Polarity Gun33, ChiTech Shotgun32. Ten Ton Hammer: lockers by " +
                "the medic, the ramp, the walkway, the stasis room. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsBlowThemAllUpV1",
                "Twenty personal Closed Lockers (20591), 100HP, at estimated places along Ten Ton Hammer's route, each walked to " +
                "from the door. Items 48296, 48360, 48676, 14815, one to choose. Indicators in the instance too.", 0.3);

            for (var index = 0; index < Lockers.Length; index++)
                mission.Indicator(Destroy, 1 + (uint)index, Lockers[index].X, Lockers[index].Y, Lockers[index].Z, ObjectRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
