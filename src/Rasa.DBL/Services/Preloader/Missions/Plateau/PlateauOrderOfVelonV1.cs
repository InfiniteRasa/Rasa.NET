using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Order Of Velon (native mission 1982), Plateau: Luminary Sampei at New Velon Village sends the
    /// player into Velon Hollow (squad instance 2136) to find Elder Hathan, head of the Order of Velon, who
    /// takes the mission there.
    ///
    /// Native: texts 20866-20870 (missionconversation; turn-in 20869 "The situation here is grave.
    /// Something is turning my people into mindless monsters!", Hathan's); objective 1 "Locate Elder
    /// Hathan." 20889/20890, no counter (missionobjective); objectiveconversation 1982,1,2263 text 20891.
    /// Package 2263 is the server's Elder Hathen (creature 510152, pool 510152 at 165, 77, -520 in Velon
    /// Hollow, beside its hospital). Velon Hollow levels 30-32 (gamecontext 2136).
    ///
    /// Sourced: none beyond the client (no TaRapedia page; TR MMO DB lists no such quest).
    ///
    /// Reconstructed (ESTIMATE): level 30, as 1981; requirement Village Of The Damned (1981): the offer
    /// opens "Even worse news has reached us since last we talked", and TaRapedia gives 1981 for the
    /// other Velon Hollow missions of New Velon. Reward, by the user's rule for a mission with none: one
    /// med pack of the class below its level (Class V Standard Med Pack 45055, Consumable_Medpack_05B_25)
    /// and XP in line with its neighbours, a little over 1981's for the smaller item reward: 45,000 XP
    /// (as 1034 Going Postal, level 30), no credits (as 1981). No indicator: Hathan is in an instance.
    /// </summary>
    public static class PlateauOrderOfVelonV1
    {
        public const uint MissionId = 1982;
        public const uint Level = 30;
        public const uint Sampei = PlateauVillageOfTheDamnedV1.Sampei;
        public const uint Hathen = 510152;
        public const uint HathenPackage = 2263;
        public const uint VelonHollow = 2136;

        public const uint Experience = 45000;
        public const uint Credits = 0;
        public const uint MedPack = 45055;              // Class V Standard Med Pack (Consumable_Medpack_05B_25)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Order Of Velon", 20866, Sampei, Hathen, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new (uint, uint)[] { (MedPack, 1) });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            mission.Objective(1, 20889, 20890, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, HathenPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1982",
                "Native1 'Locate Elder Hathan.' 20889/20890, no counter; objectiveconversation 1982,1,2263 text20891. Texts " +
                "20866-20870; turn-in 20869 is Hathan's. Velon Hollow 2136 levels 30-32 (gamecontext).");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauOrderOfVelonV1",
                "Giver Sampei 510186, receiver Elder Hathen 510152 (package 2263, pool510152 in Velon Hollow). Level30 and " +
                "requirement 1981 (no source). Reward by the user's rule: 45000XP, 0 credits, 1 Class V Standard Med Pack 45055.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
