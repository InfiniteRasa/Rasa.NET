using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Gun Control - Part IV, the two ends of Gun Control - Part III (1867): native mission 1868, the gun given (Shaman Masai
    /// sends the player back to Retread Lou), and native mission 1904, the gun sold (Retread Duvall pays out).
    ///
    /// Native 1868: texts 19124-19128 (missionconversation): log "Return to Retread Lou in High Point Retreat.", offer in
    /// Masai's voice ("Please let him know that I received his gift and that we are now, as he says, even."), turn-in in
    /// Lou's ("I took some of the left over parts I had and made you a weapon of you own"); objective 1 19486/19487; objective
    /// conversation 1868,1 package 2356 flag 1 COMPLETION 19488 (Lou: "never play poker with a Forean").
    /// Native 1904: texts 19693-19697 (reminder empty): log "Return to Retread Duvall in the forest outside High Point
    /// Retreat.", offer in Duvall's voice over the comm ("Thanks for keeping your comm channel open as proof that you didn't
    /// rat me out ... Swing back by and I'll have your credits here waiting"), turn-in "It was a pleasure doing business with
    /// you."; objective 1 19698/19699; objective conversation 1904,1 package 950 flag 1 COMPLETION 19700 (Duvall: "A deals a
    /// deal"). Client item templates 121217-121220: Rare EMP Machine Gun 33-37, Shotgun 37-41, Virulent Injection Gun
    /// 37-41, Sonic Rocket Launcher 34-38.
    ///
    /// Sourced: TaRapedia Gun Control - Part IV (one page for both): giver Shaman Masai, receiver Retread Lou; requires Part
    /// III; level 36; 32,500 XP, 3,600 credits; one of Teleract Pulse Chaingun 33, Audiodyne Shotgun 37, Vextronics Virulent
    /// Injector Gun 37, ChiTech Sonic Cannon 34 (the client block in that order). The client's 1904 receiver Duvall wins
    /// over the page's Lou (the triage's 8).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Exclusive by 1867's flag 1867001 (1 gave: 1868, 2 sold: 1904), from 1867's hidden "add the flag" objective and the
    ///    two Part IV texts; the client has no prerequisite table to say so.
    ///  - 1868 offered by Masai (TaRapedia). 1904 offered by radio when 1867 is rewarded (its offer is Duvall's comm call,
    ///    the triage had Masai), and again on arrival on the Marshes if turned down (Retread Planning's way); turned in at
    ///    Duvall.
    ///  - Both one talk at the receiver; both pay the TaRapedia reward and the client block 121217-121220, one to choose (the
    ///    triage: 1904's reward 1868's; no other block fits, the UNC twins 121237-121240 are 1875's). No 50,000 credits.
    ///  - A marker at the receiver.
    /// </summary>
    public static class MarshesGunControlPartFourV1
    {
        public const uint LouMissionId = 1868, DuvallMissionId = 1904;
        public const uint Level = 36;
        public const uint Return = 1;
        public const uint Lou = MarshesGunControlNpcsV1.Lou, Masai = MarshesGunControlNpcsV1.Masai;
        public const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall;
        public const uint GunFlag = MarshesGunControlPartThreeV1.GunFlag;

        public const uint Experience = 32500;
        public const uint Credits = 3600;
        public const uint EmpMachineGun = 121217;       // Weapon_Avatar_MachineGun_EMP_RAR_33_to_37
        public const uint Shotgun = 121218;             // Weapon_Avatar_Shotgun_v1_Physical_RAR_37_to_41
        public const uint VirulentInjectionGun = 121219; // Weapon_Avatar_InjectionGun_Virulent_RAR_37_to_41
        public const uint SonicRocketLauncher = 121220; // Weapon_Avatar_RocketLauncher_Sonic_RAR_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            Part(migration, LouMissionId, "Gun Control - Part IV (Lou)", 19124, Masai, Lou, MarshesGunControlNpcsV1.LouPackage,
                MarshesGunControlPartThreeV1.Gave, MarshesGunControlNpcsV1.LouAt, 19486, 19487,
                "Native1 19486/19487 'Return to Retread Lou'; objectiveconversation 1868,1 pkg2356 19488. Texts 19124-19128 " +
                "(offer Masai, turn-in Lou). Templates 121217-121220.",
                "Giver Masai (TaRapedia), receiver Lou; offered for flag1867001=1 (gun given); one talk at Lou.");
            Part(migration, DuvallMissionId, "Gun Control - Part IV (Duvall)", 19693, null, Duvall, MarshesStalkerWoodsNpcsV1.DuvallPackage,
                MarshesGunControlPartThreeV1.Sold, MarshesStalkerWoodsNpcsV1.DuvallAt, 19698, 19699,
                "Native1 19698/19699 'Return to Retread Duvall'; objectiveconversation 1904,1 pkg950 19700. Texts 19693-19697 " +
                "(offer: Duvall on the comm, turn-in Duvall).",
                "Offered by radio at 1867's reward (again on arrival on the Marshes) for flag1867001=2 (gun sold), receiver Duvall (client); one talk; 1868's reward.");
        }

        private static void Part(MigrationBuilder migration, uint missionId, string name, uint nameText, uint? giver, uint receiver,
            uint package, uint flagValue, (double X, double Y, double Z) at, uint objectiveName, uint objectiveBody,
            string client, string reconstruction)
        {
            var mission = new MarshesMissionDataV1(migration, missionId, name, nameText, giver, receiver, Level);
            mission.Reward(Experience, Credits, new[] { EmpMachineGun, Shotgun, VirulentInjectionGun, SonicRocketLauncher });
            mission.Prerequisite(MarshesGunControlPartThreeV1.MissionId);
            mission.FlagPrerequisite(GunFlag, flagValue, $"Gun Control - Part III ended with flag {GunFlag} = {flagValue}", 2);
            if (giver == null)
                mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(MarshesGunControlPartThreeV1.MissionId),
                    new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey,
                        MarshesMissionDataV1.MapContextId));

            mission.Objective(Return, objectiveName, objectiveBody, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Return);
            mission.Conversation(Return, package);
            mission.Action(Return, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Return, 1, at.X, at.Y, at.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{missionId}", client);
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_IV",
                "One page for 1868 and 1904: giver Shaman Masai, receiver Retread Lou; requires Part III; level36; 32500XP, 3600 " +
                "credits; one of Teleract Pulse Chaingun33, Audiodyne Shotgun37, Vextronics Virulent Injector Gun37, ChiTech " +
                "Sonic Cannon34.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesGunControlPartFourV1",
                reconstruction + " Items the client block 121217-121220, one to choose. A marker at the receiver.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration)
        {
            MarshesMissionDataV1.Remove(migration, DuvallMissionId);
            MarshesMissionDataV1.Remove(migration, LouMissionId);
        }
    }
}
