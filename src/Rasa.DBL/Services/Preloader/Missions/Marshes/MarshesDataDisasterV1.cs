using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Data Disaster (native mission 1870), Valverde Marshes: the Bane took AFS troop placement data from a smaller outpost;
    /// Sergeant McDonald sends the player to the data hub east of Paludos to get into its security system and purge it.
    ///
    /// Native: texts 19134-19138 (missionconversation): log "Recover AFS Troop Placement Data Acquired by the Bane. It is
    /// likely being stored in a data hub east of Paludos.", offer "get into their security system, you may be able to purge
    /// the data from the system", turn-in "That'll teach the Thrax ..."; objective 1 "Recover AFS troop placement data acquired
    /// by the Bane." 19550/19551, no counter (missionobjective); objective conversation 1870,1 package 2374 flag 1 COMPLETION
    /// text 19552 "AFS Troop placement info." (objectiveconversation). Client item templates 121205-121208: Reflective Legs
    /// 34-38, Hazmat Legs 34-38, Motor Assist Legs 38-42, Mech Vest 35-39.
    ///
    /// Sourced: TaRapedia Data Disaster: giver and receiver Sergeant McDonald; 70,000 XP, 5,550 credits; Mech Vest 35 or
    /// Reflective 34, Hazmat 34, Motor Assist 38 Legs. Ten Ton Hammer Eastern Marshes (the group-3 mapping: its text filed
    /// under 914 is 1870's): "the Bane CP at 166,232,57. Upstairs you'll find the Bane CommLink terminal"; Mech Vest or
    /// Reflective, Hazmat or Motor Assist Legs. Pools precedent: Bane Investigation's "Bane CommLink Terminal", a talking
    /// Bane keyboard (10519, PoolsBaneInvestigationV1).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Offered once Under the Radar - Part II is done in either version (mission flag
    ///    MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag); level 37 (the set's).
    ///  - The terminal a personal UsableNPCBaneKeyboard (10519) that talks with package 2374: the talk shows the client's
    ///    "AFS Troop placement info." and completes 1. No item (the objective has none).
    ///  - Not at Ten Ton Hammer's point: (166, 232, 57) is the roof of Nelg's post, and the roof and the post's floor are
    ///    navmesh islands (no walk from the Bane Assertion Camp waypoint, probe of 2026-11-02). The terminal stands on the
    ///    walked-to deck east of the post (178, 224.68, 46), 16 m from it, the nearest reachable spot (fallback: playtest if
    ///    the post's upper floor is reachable in the client).
    ///  - The rewards the client's block 121205-121208, one to choose (TaRapedia's levels). An unnamed marker at the terminal.
    /// </summary>
    public static class MarshesDataDisasterV1
    {
        public const uint MissionId = 1870;
        public const uint Level = 37;
        public const uint McDonald = MarshesSentryTowerNpcsV1.McDonald;
        public const uint Recover = 1;

        public const uint TerminalClass = Pools.PoolsBaneInvestigationV1.TerminalClass;    // UsableNPCBaneKeyboard
        public const uint TerminalPackage = 2374;
        public const string TerminalRole = "bane-commlink-terminal";
        public static readonly ScenePosition TerminalAt = new(178f, 224.68f, 46f);
        public static readonly (double X, double Y, double Z) TenTonHammerAt = (166, 232, 57);
        public const double MarkerRadius = 10;

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint ReflectiveLegs = 121205;      // Armor_T2_Reflective_V02_UNC_Legs_34_to_38
        public const uint HazmatLegs = 121206;          // Armor_T2_Hazmat_V02_UNC_Legs_34_to_38
        public const uint MotorAssistLegs = 121207;     // Armor_T1_MotorAssist_V06_UNC_Legs_38_to_42
        public const uint MechVest = 121208;            // Armor_T3_Mech_V04_UNC_Vest_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Data Disaster", 19134, McDonald, McDonald, Level);
            mission.Reward(Experience, Credits, new[] { ReflectiveLegs, HazmatLegs, MotorAssistLegs, MechVest });
            mission.FlagPrerequisite(MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag, 1, "Requires Under the Radar - Part II (1876 or 1877)");

            mission.Objective(Recover, 19550, 19551, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Recover);
            mission.Conversation(Recover, TerminalPackage);
            mission.Action(Recover, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Recover, 1, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1870",
                "Native1 'Recover AFS troop placement data acquired by the Bane.' 19550/19551, no counter; objectiveconversation " +
                "1870,1 package2374 flag1 completion text19552 'AFS Troop placement info.'. Texts 19134-19138 (data hub east of " +
                "Paludos, security system). Templates 121205-121208.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Data_Disaster",
                "Giver/receiver Sergeant McDonald; 70000XP, 5550 credits; Mech Vest35 or Reflective34/Hazmat34/Motor Assist38 " +
                "Legs. Ten Ton Hammer (filed under 914): the Bane CP at 166,232,57, upstairs the Bane CommLink terminal.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesDataDisasterV1",
                "Requires flag 1876001; level37. A personal talking Bane keyboard 10519 (package2374) on the walked-to deck at " +
                "(178,224.68,46): Ten Ton Hammer's point is the roof of Nelg's post, a navmesh island. Its talk completes 1. " +
                "Items the client block 121205-121208, one to choose. A marker at the terminal.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition> { new(Recover, TerminalPackage, 1, transitionId: 1) }
            };
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: 0, Conversation: new SceneObjectConversation(MissionId, Recover, TerminalPackage, Recover));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{TerminalRole}", TerminalRole) } };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
