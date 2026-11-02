using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Gun Control - Part III (native mission 1867), Valverde Marshes: Lou sends the finished CGH 9000 Ultra Electric
    /// Chaingun to Shaman Masai; on the way Retread Duvall offers to buy it. The player's choice sets the follow-up: Part IV
    /// for Lou (1868, the gun given) or for Duvall (1904, the gun sold).
    ///
    /// Native: texts 19119-19123 (missionconversation): log "Deliver the CGH 9000 ... to Shaman Masai", offer "If you could
    /// deliver this to Shaman Masai for me, I'd owe you one.", turn-in in Masai's voice; objectives 1 "Deliver CGH 9000 Ultra
    /// Electric Chaingun to Shaman Masai." 19441/19442, 2 "You've been sidetracked - speak with Retread Duvall." 19444/19445
    /// (counter 19545, empty), 3 "Return to Shaman Masai without the gun." 19483/19484, 4 "HIDDEN - here so I can add the
    /// flag or just complete." 19546/19547 (missionobjective). Objective conversations: 1867,1 package 991 flag 1 COMPLETION
    /// 19443 (Masai thanks), 1867,2 package 950 flag 1 CHOICEBODY 19479 (Duvall: "an easy 50,000 credits, give me the gun,
    /// tell a white lie to the Forean, and then return for your reward"), CHOICE1 19480 "Not a chance.", CHOICE2 19481 "I
    /// could use the cash.", CHOICE3 empty; 1867,3 package 991 flag 1 COMPLETION 19485 (Masai, no gun). Item class 28634 CGH
    /// 9000 Ultra Electric Chaingun (World DB template 118998).
    ///
    /// Sourced: TaRapedia Gun Control - Part III: giver Retread Lou, receiver Shaman Masai; level 36; 65,000 XP, 5,400
    /// credits; the branch at Retread Duvall on the north road.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The gun issued at the accept. 1 and 2 open at the accept, 3 hidden. "Not a chance" completes 2 and changes
    ///    nothing. "I could use the cash" completes 2, takes the gun, fails 1 (a scene step; failing an optional objective
    ///    keeps the mission) and opens 3. Masai's talk on 1 takes the gun; on 3 there is none. Duvall asks only while the
    ///    gun is held (not after the delivery).
    ///  - 4 (hidden, the only required objective) completes as soon as 1 or 3 does (an aggregate of the two); 1 and 3 each pay
    ///    and set the server's flag 1867001 to 1 (gave) or 2 (sold), which offers 1868 or 1904. The flag is set only at the
    ///    end, so an abandon after a sale leaves both follow-ups closed until 1867 is done again. 1, 2 and 3 optional.
    ///  - No extra credits for the sale (the triage): Duvall's 50,000 are 1904's story, its reward is 1868's.
    ///  - A marker at Masai on 1 and 3, at Duvall on 2.
    /// </summary>
    public static class MarshesGunControlPartThreeV1
    {
        public const uint MissionId = 1867;
        public const uint Level = 36;
        public const uint Lou = MarshesGunControlNpcsV1.Lou, Masai = MarshesGunControlNpcsV1.Masai;
        public const uint MasaiPackage = MarshesGunControlNpcsV1.MasaiPackage, DuvallPackage = MarshesStalkerWoodsNpcsV1.DuvallPackage;
        public const uint Deliver = 1, Sidetracked = 2, Return = 3, Outcome = 4;
        public const uint Refuse = 1, Sell = 2;
        public const uint SaleSequence = 11;

        public const uint GunFlag = 1867001, Gave = 1, Sold = 2;
        public const uint GunTemplate = 118998, GunClass = 28634;
        public const string GunKey = "cgh-9000-chaingun";

        public const uint Experience = 65000;
        public const uint Credits = 5400;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Gun Control - Part III", 19119, Lou, Masai, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Prerequisite(MarshesGunControlPartTwoV1.MissionId);
            mission.Scenario(SaleSequence, "The gun sold to Duvall: the delivery fails", 0);

            mission.Objective(Deliver, 19441, 19442, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Sidetracked, 19444, 19445, 1, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Return, 19483, 19484, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Outcome, 19546, 19547, 3, MissionObjectiveState.Incomplete);

            // 1: the gun to Masai.
            mission.Transition(Deliver);
            mission.Conversation(Deliver, MasaiPackage);
            mission.Action(Deliver, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("masai-takes-the-gun", MissionId, GunKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Deliver, 2, MissionActionKind.SetPlayerFlag, playerFlag: GunFlag, playerFlagValue: Gave);
            mission.Action(Deliver, 3, MissionActionKind.GrantReward, reward: 1);

            // 2: Duvall's offer, refused or taken.
            mission.Transition(Sidetracked, Refuse, Refuse);
            mission.Conversation(Sidetracked, DuvallPackage, 1, Refuse);
            mission.Transition(Sidetracked, Sell, Sell);
            mission.Conversation(Sidetracked, DuvallPackage, 1, Sell);
            mission.Action(Sidetracked, 1, MissionActionKind.ConsumeMissionItem, transition: Sell,
                itemIntent: new ConsumeMissionItemIntent("duvall-buys-the-gun", MissionId, GunKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Sidetracked, 2, MissionActionKind.RevealObjective, targetObjective: Return, transition: Sell);
            mission.Action(Sidetracked, 3, MissionActionKind.ActivateObjective, targetObjective: Return,
                state: MissionObjectiveState.Incomplete, transition: Sell);
            mission.Action(Sidetracked, 4, MissionActionKind.StartScenario, scenario: SaleSequence, transition: Sell);

            // 3: back at Masai with no gun.
            mission.Transition(Return);
            mission.Conversation(Return, MasaiPackage);
            mission.Action(Return, 1, MissionActionKind.SetPlayerFlag, playerFlag: GunFlag, playerFlagValue: Sold);
            mission.Action(Return, 2, MissionActionKind.GrantReward, reward: 1);

            var masai = MarshesGunControlNpcsV1.MasaiAt;
            var duvall = MarshesStalkerWoodsNpcsV1.DuvallAt;
            mission.Indicator(Deliver, 1, masai.X, masai.Y, masai.Z);
            mission.Indicator(Sidetracked, 2, duvall.X, duvall.Y, duvall.Z);
            mission.Indicator(Return, 3, masai.X, masai.Y, masai.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1867",
                "Native1 'Deliver CGH 9000 Ultra Electric Chaingun to Shaman Masai.' 19441/19442, 2 'sidetracked - speak with " +
                "Retread Duvall' 19444/19445 (counter19545 empty), 3 'Return to Shaman Masai without the gun.' 19483/19484, 4 " +
                "'HIDDEN - here so I can add the flag or just complete.' 19546/19547. objectiveconversation 1867,1 pkg991 19443; " +
                "1867,2 pkg950 choice body19479, choices 19480 'Not a chance', 19481 'I could use the cash'; 1867,3 pkg991 " +
                "19485. Texts 19119-19123. Item class28634 (template118998).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_III",
                "Giver Retread Lou, receiver Shaman Masai; level36; 65000XP, 5400 credits; the branch at Retread Duvall.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesGunControlPartThreeV1",
                "Gun at the accept. 1 and 2 open; Duvall's choice 1 completes 2, choice 2 completes 2, takes the gun, fails 1 " +
                "(scene) and opens 3. Hidden 4 an aggregate of 1 or 3; 1 and 3 pay and set flag1867001 to 1 (gave, 1868) or 2 " +
                "(sold, 1904). No extra credits for the sale. Markers at Masai and Duvall.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Outcome },
                ObjectiveAggregations = new() { [Outcome] = new MissionObjectiveAggregation(new[] { Deliver, Return }, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Sidetracked, DuvallPackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Refuse, [2] = Sell },
                        requirement: new AssignmentItemRequirement(MissionId, GunKey))
                },
                Items = new()
                {
                    new MissionItemBinding(GunKey, GunTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-gun", MissionId, GunKey, GunTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[SaleSequence] = new SceneSequenceDefinition
            {
                Character = { new ObjectiveIntent("sold-no-delivery", MissionId, Deliver, MissionObjectiveState.Failed) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
