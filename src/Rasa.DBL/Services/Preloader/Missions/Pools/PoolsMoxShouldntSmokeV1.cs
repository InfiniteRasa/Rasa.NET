using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Mox Shouldn't Smoke (native mission 1135), Retread Caves (squad instance 1694), Valverde Pools: Ellen Lenee sends
    /// the player to Dr. Donius, who tells of a carton of cigarettes near the caves' entrance; Ellen takes it.
    ///
    /// Native: texts 8043-8047 (missionconversation): log "Speak with Dr. Donius, then go and bring back the cigarettes.",
    /// offer "Dr. Donius has been working on it. Maybe you could ask him if he's figured anything out.", turn-in "Well,
    /// I'll take these cigarettes and we'll let the doc do his thing"; objectives 1 "Get the Carton of Cigarettes"
    /// 8051/8052, 2 "Talk to Dr. Donius" 8048/8049 (missionobjective); objectiveconversation 1135,2 package 849 text 8050
    /// "They love the taste of tobacco! ... I believe there might be a carton of cigarettes near the cave entrance, where
    /// you were attacked earlier? ... Why don't you go and see". Item: 49704 MisRetreadCavesCartonOfCigarettes (class
    /// 24039).
    ///
    /// Sourced: TaRapedia Mox Shouldn't Smoke: giver Ellen Lenee, reward giver Dr. Donius (Retread Caves); requires Mox
    /// Clearing; 45,000 XP, 4,800 credits; one of Astra Hazmat Armor Legs lvl 29, ChiTech Mech Armor Vest lvl 30, Teleract
    /// Motor Assist Armor Legs lvl 33, Titan Reflective Armor Legs lvl 29. Ten Ton Hammer Retread Caves guide: "Follow
    /// the mission marker to the AFS Supply Crate (-3.6, -12.3) to pick up the carton of smokes." TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The receiver Ellen Lenee, not TaRapedia's infobox's Dr. Donius: the client's turn-in text ("we'll let the doc
    ///    do his thing") is not the doctor's, and TaRapedia's own debriefing gives it to Ellen (client first, as Deadly
    ///    Moxins' receiver; taken by Claude, 2026-10-25). Ellen takes the carton (Dr. Donius hands a laced carton out in
    ///    A Mox On Both Our Houses).
    ///  - 2 first (the log's order), then 1 opens; a personal supply crate (class 11078 UsableItemDisp_item_crate_hum_v01,
    ///    as the Hermit's cabinet) on the first room's floor by Ten Ton Hammer's point gives the carton.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMoxShouldntSmokeV1
    {
        public const uint MissionId = 1135;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Ellen = PoolsRetreadCavesNpcsV1.Ellen, Donius = PoolsRetreadCavesNpcsV1.Donius;
        public const uint Carton = 1, Talk = 2;
        public const uint Taken = 5, CrateSequence = 1;

        public const uint CrateClass = PoolsThirstyHermitV1.CabinetClass;   // UsableItemDisp_item_crate_hum_v01
        public const uint UseArgument = PoolsThirstyHermitV1.UseArgument;
        public const uint ReadyState = PoolsThirstyHermitV1.ReadyState, TakenState = PoolsThirstyHermitV1.TakenState;
        public const uint CartonTemplate = 49704;       // MisRetreadCavesCartonOfCigarettes
        public const string CartonKey = "retread-cigarettes", CrateRole = "afs-supply-crate";
        public static readonly ScenePosition CrateAt = new(-1.2f, 83.05f, -16f);
        public const double ObjectRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint HazmatLegs = 20762;           // Armor_T2_Hazmat_V02_UNC_Legs_29_to_33
        public const uint MechVest = 33173;             // Armor_T3_Mech_V02_UNC_Vest_30_to_34
        public const uint MotorAssistLegs = 29073;      // Armor_T1_MotorAssist_V04_UNC_Legs_33_to_37
        public const uint ReflectiveLegs = 35999;       // Armor_T2_Reflective_V02_UNC_Legs_29_to_33

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Mox Shouldn't Smoke", 8043, Ellen, Ellen, Level);
            mission.Reward(Experience, Credits, new[] { HazmatLegs, MechVest, MotorAssistLegs, ReflectiveLegs });
            mission.Prerequisite(PoolsMoxClearingV1.MissionId);

            mission.Objective(Talk, 8048, 8049, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Carton, 8051, 8052, 1, MissionObjectiveState.Inactive);
            mission.Scenario(Taken, "The carton taken from the supply crate", 1);

            // 2: Donius tells of the carton; 1 opens.
            mission.Transition(Talk);
            mission.Conversation(Talk, PoolsRetreadCavesNpcsV1.DoniusPackage);
            mission.Action(Talk, 1, MissionActionKind.RevealObjective, targetObjective: Carton);
            mission.Action(Talk, 2, MissionActionKind.ActivateObjective, targetObjective: Carton, state: MissionObjectiveState.Incomplete);

            // 1: the carton from the crate.
            mission.Transition(Carton);
            mission.Progress(Carton, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Taken);
            mission.Action(Carton, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                Items = new()
                {
                    new MissionItemBinding(CartonKey, CartonTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, CrateAt,
                Orientation: 1.5708, InitialObjectState: ReadyState,
                UseAction: new SceneObjectAction(MissionId, Carton, CrateSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-crate", CrateRole) } };
            scene.Sequences[CrateSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("crate-taken", CrateRole, TakenState),
                    new SetInteractionIntent("crate-done", CrateRole, false)
                },
                Character = { new IssueMissionItemIntent("carton-from-crate", MissionId, CartonKey, CartonTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, Taken, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1135",
                "Native1 'Get the Carton of Cigarettes' 8051/8052, 2 'Talk to Dr. Donius' 8048/8049; objectiveconversation 1135,2 " +
                "package849 text8050 (a carton near the cave entrance). Texts 8043-8047. Item 49704 MisRetreadCavesCartonOfCigarettes.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mox_Shouldn%27t_Smoke",
                "Giver Ellen Lenee, reward Dr. Donius (infobox; the debriefing is Ellen's) (Retread Caves); requires Mox Clearing; 45000XP, 4800 credits; one of Astra " +
                "Hazmat Armor Legs29, ChiTech Mech Armor Vest30, Teleract Motor Assist Armor Legs33, Titan Reflective Armor Legs29. " +
                "Ten Ton Hammer: the AFS Supply Crate (-3.6, -12.3). TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMoxShouldntSmokeV1",
                "Receiver Ellen (client text, debriefing). 2 then 1; a personal crate 11078 in the first room gives the carton; " +
                "Ellen takes it. Items 20762, 33173, 29073, " +
                "35999, one to choose. Place an estimate. Indicators in the instance too.", 0.3);

            var donius = PoolsRetreadCavesNpcsV1.DoniusAt;
            mission.Indicator(Talk, 1, donius.X, donius.Y, donius.Z, ObjectRadius);
            mission.Indicator(Carton, 2, CrateAt.X, CrateAt.Y, CrateAt.Z, ObjectRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
