using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Last Men Standing (native mission 1142), Ligo Crucible: Corporal Shandy in the wormhole chamber of Outpost
    /// Intrepid gives the new arrival their transfer orders for Captain DeLisi.
    ///
    /// Native: texts 8344-8347, 8348 (missionconversation): log "Report to Captain Delisi in the Command Bunker at Fort
    /// Intrepid and give him your transfer orders.", offer "We're all that's left of the Fort Intrepid staff", turn-in "Let's
    /// have a look at your file. Hmm. A receptive, huh?"; objective 1 "Bring your transfer orders to DeLisi." 8533/8534, no
    /// counter (missionobjective); objectiveconversation (1142,1) package 1219 (DeLisi) text 8535 "A transfer? Well praise
    /// the Lord and pass the ammunition!". Item class 24588 Your Transfer Orders (template 50212). Names 9060 Shandy, 9059
    /// DeLisi.
    ///
    /// Sourced: TaRapedia The Last Man Standing: giver Corporal Shandy, reward giver Captain DeLisi, both Outpost Intrepid;
    /// no requirement; follow-up A Fort Called Intrepid; 37,500 XP, 3,800 credits; ClassVII EMP Bomb, Power Charger,
    /// Incendiary Grenade, EMP Grenade; Shandy gives "this flash drive". TR MMO DB quest list: level 40, 37500, 3800.
    ///
    /// Reconstructed (ESTIMATE): the orders are issued at the accept and DeLisi takes them at his talk; the stacks of the
    /// four (2, 2, 4, 4) as Marshes Light the Fires (1864) has for the same TaRapedia list, its templates (Class VII EMP
    /// Bomb 111232, Power Charger 118907) and Highway Robbery's (Incendiary Grenade 111038, EMP Grenade 111028); one to
    /// choose. A marker at DeLisi.
    /// </summary>
    public static class CrucibleLastMenStandingV1
    {
        public const uint MissionId = 1142;
        public const uint Level = 40;
        public const uint Shandy = CrucibleOutpostIntrepidNpcsV1.Shandy;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint DeLisiPackage = CrucibleOutpostIntrepidNpcsV1.DeLisiPackage;
        public const uint Report = 1;

        public const uint OrdersTemplate = 50212, OrdersClass = 24588;
        public const string OrdersKey = "transfer-orders";
        public const double MarkerRadius = 8;

        public const uint Experience = 37500;
        public const uint Credits = 3800;
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint PowerCharger = 118907, PowerChargers = 2;             // Class VII Power Charger
        public const uint IncendiaryGrenade = 111038, IncendiaryGrenades = 4;   // Consumable_Grenade_Fire_07_35
        public const uint EmpGrenade = 111028, EmpGrenades = 4;                 // Consumable_Grenade_EMP_07_35

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Last Men Standing", 8344, Shandy, DeLisi, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (PowerCharger, PowerChargers), (IncendiaryGrenade, IncendiaryGrenades), (EmpGrenade, EmpGrenades) },
                new (uint, uint)[0]);

            mission.Objective(Report, 8533, 8534, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, DeLisiPackage);
            mission.Action(Report, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("delisi-takes-the-orders", MissionId, OrdersKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Report, 2, MissionActionKind.GrantReward, reward: 1);

            var delisi = CrucibleOutpostIntrepidNpcsV1.At(DeLisi);
            mission.Indicator(Report, 1, delisi.X, delisi.Y, delisi.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1142",
                "Native1 'Bring your transfer orders to DeLisi.' 8533/8534, no counter; objectiveconversation 1142,1 package1219 " +
                "text8535. Texts 8344-8348. Item class24588 Your Transfer Orders (template50212). Names 9060 Shandy, 9059 DeLisi.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Last_Man_Standing",
                "Giver Corporal Shandy (wormhole chamber, 988, 140, 60), reward giver Captain DeLisi (952, 144, 106); no requirement; " +
                "follow-up A Fort Called Intrepid; 37500XP, 3800 credits; ClassVII EMP Bomb, Power Charger, Incendiary Grenade, EMP " +
                "Grenade. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleLastMenStandingV1",
                "Shandy 770000 new (copy of DeLisi's row, package 770000). The orders issued at the accept, taken at DeLisi's talk " +
                "(package1219). Stacks 111232x2, 118907x2, 111038x4, 111028x4 (as Marshes 1864), one to choose. A marker at DeLisi.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(OrdersKey, OrdersTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-transfer-orders", MissionId, OrdersKey, OrdersTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
