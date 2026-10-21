using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Blue Flu (native mission 1014), Plateau: MP Lieutenant Parkman, down with the Forean flu, sends the
    /// player to CID Commander Provost, who sends them to relieve Security Guard Norton at the supply
    /// warehouse; Norton is found unconscious. It opens Murder Most Foul (895).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5547-5550 and 6307 (missionconversation; turn-in 5550
    /// "Security Guard Norton appears to be unconscious."); objectives 2 "Report to CID Headquarters."
    /// 5551/6308 and 1 "Report to the Fort Defiance Warehouse." 5553/6309, no counters (missionobjective);
    /// Provost's line on package 962 for 2 (5552), Norton's on 959 for 1 (5554) (objectiveconversation).
    ///
    /// Sourced: TaRapedia Blue Flu: giver MP Lieutenant Parkman, requirement The Defiant Ones, follow-up
    /// Murder Most Foul, 20000 XP; objectives in the order 2, 1. Ten Ton Hammer (Fort Defiance part 1): "level
    /// 30 to begin"; Parkman at -70, 404, 951; Provost at -219, 421, 931; Norton at -185, 398, 1016;
    /// "Interacting with his corpse will reward you with 3100 credits and your choice of a Class VI Advanced
    /// MedPak, Class VI Emp Grenade, Class VI Res Trauma Kit, or Class VI Concussions Grenades".
    ///
    /// Reconstructed: level 30. The reward is Ten Ton Hammer's (user decision; TaRapedia's three "Pathogex"
    /// helmets match no template set): templates 45059, 111027, 47598, 111017, one of them.
    /// </summary>
    public static class PlateauBlueFluV1
    {
        public const uint MissionId = 1014;
        public const uint Level = 30;
        public const uint Warehouse = 1, Headquarters = 2;
        public const uint Parkman = PlateauCidNpcsV1.Parkman, Norton = PlateauCidNpcsV1.Norton;

        public const uint Experience = 20000;
        public const uint Credits = 3100;
        public const uint MedPack = 45059, EmpGrenade = 111027, ResKit = 47598, ConcussionGrenade = 111017;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Blue Flu", 5547, Parkman, Norton, Level);
            mission.Reward(Experience, Credits, new[] { MedPack, EmpGrenade, ResKit, ConcussionGrenade });
            mission.Prerequisite(PlateauDefiantOnesV1.MissionId);

            mission.Objective(Headquarters, 5551, 6308, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Warehouse, 5553, 6309, 1, MissionObjectiveState.Inactive);

            mission.Transition(Headquarters);
            mission.Conversation(Headquarters, PlateauCidNpcsV1.ProvostPackage);
            mission.Action(Headquarters, 1, MissionActionKind.RevealObjective, targetObjective: Warehouse);
            mission.Action(Headquarters, 2, MissionActionKind.ActivateObjective, targetObjective: Warehouse,
                state: MissionObjectiveState.Incomplete);

            mission.Transition(Warehouse);
            mission.Conversation(Warehouse, PlateauCidNpcsV1.NortonPackage);
            mission.Action(Warehouse, 1, MissionActionKind.GrantReward, reward: 1);

            var provost = PlateauCidNpcsV1.ProvostAt;
            mission.Indicator(Headquarters, 1, provost.X, provost.Y, provost.Z);
            var norton = PlateauCidNpcsV1.Moves[0].At;
            mission.Indicator(Warehouse, 2, norton.X, norton.Y, norton.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1014",
                "Native2 'Report to CID Headquarters.' 5551/6308, 1 'Report to the Fort Defiance Warehouse.' 5553/6309, no " +
                "counters; objectiveconversation 1014,2,962 text5552 and 1014,1,959 text5554. Texts 5547-5550, 6307. Parkman name8768.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Blue_Flu",
                "Giver MP Lieutenant Parkman; requirement The Defiant Ones; follow-up Murder Most Foul; 20000XP; 'Pathogex' " +
                "helmets. Ten Ton Hammer Fort Defiance part 1: level 30; 3100 credits and a choice of Class VI Advanced MedPak, " +
                "EMP Grenade, Res Trauma Kit, Concussion Grenade at Norton's body.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBlueFluV1",
                "Level30. Ten Ton Hammer's reward (user decision): 45059, 111027, 47598, 111017. Parkman new, Norton moved to the " +
                "warehouse and lying (PlateauCidNpcsV1).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
