using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Gun Control - Part II (native mission 1866), Valverde Marshes: Lou's gun is built but its power cells are empty; he
    /// gives the player an electrical discharge container that drains the charge of dying Mox.
    ///
    /// Native: texts 19114-19118 (missionconversation): log "Head north and kill 10 mox and gather their electrical
    /// essence.", offer "Take this electrical discharge container. When a mox is close to death it will drain the charge
    /// from them", turn-in "now we're cooking with gas... er... electricity"; objective 1 "Gather the electrical discharge
    /// from 10 mox." 19438/19439, counter 19440 "Number of Mox Killed for Electrical Discharge" (missionobjective); no
    /// objective conversation. Item class 28633 Mox Electrical Discharge Container (World DB template 118997, stack 1).
    /// Client item templates 121213-121216: Mech Helmet, Graviton Legs, Stealth Helmet, Bio Legs, all 35-39.
    ///
    /// Sourced: TaRapedia Gun Control - Part II: giver and receiver Retread Lou; requires Gun Control; level 36; 65,000 XP,
    /// 5,400 credits; four items of level 35 (the client block); 10 mox. World DB: the Marshes Mox 540013 in pools 540038
    /// (-495, -258) and 540039 (-596, -167), north-north-east of the camp, and set 1's 760307-760308.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The container issued at the accept, taken at the turn-in or an abandon. Any Marshes Mox kill counts (counter 0),
    ///    ten to finish; the container is not checked at the kill (no rule needs it).
    ///  - No new Mox pools: north of the camp is a hill the navmesh does not reach (probe of 2026-11-02), and the World DB
    ///    pools 540038 and 540039 lie north of it, walked to from the camp; a marker on each. The triage's mission-only
    ///    pools are left out (only the creatures the quests need).
    ///  - The rewards the client block 121213-121216, one to choose.
    /// </summary>
    public static class MarshesGunControlPartTwoV1
    {
        public const uint MissionId = 1866;
        public const uint Level = 36;
        public const uint Lou = MarshesGunControlNpcsV1.Lou;
        public const uint Gather = 1, Mox = MarshesPaludosNpcsV1.Mox, MoxCount = 10;

        public const uint ContainerTemplate = 118997, ContainerClass = 28633;
        public const string ContainerKey = "mox-electrical-discharge-container";
        public const double PoolMarkerRadius = 20;

        /// <summary>The World DB Mox pools north of High Point Retreat.</summary>
        public static readonly (uint Pool, double X, double Y, double Z)[] MoxPools =
        {
            (540038, -495.3, 218.61, -257.7), (540039, -595.9, 217.52, -166.8)
        };

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint MechHelmet = 121213;          // Armor_T3_Mech_V04_UNC_Helmet_35_to_39
        public const uint GravitonLegs = 121214;        // Armor_T3_Graviton_V04_UNC_Legs_35_to_39
        public const uint StealthHelmet = 121215;       // Armor_T3_Stealth_V04_UNC_Helmet_35_to_39
        public const uint BioLegs = 121216;             // Armor_T3_Bio_V04_UNC_Legs_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Gun Control - Part II", 19114, Lou, Lou, Level);
            mission.Reward(Experience, Credits, new[] { MechHelmet, GravitonLegs, StealthHelmet, BioLegs });
            mission.Prerequisite(MarshesGunControlV1.MissionId);

            mission.Objective(Gather, 19438, 19439, 0, MissionObjectiveState.Incomplete, counterTextId: 19440);
            mission.Transition(Gather);
            mission.Progress(Gather, MissionProgressEventKind.CreatureKilled, Mox, MoxCount, counter: 0);
            mission.Action(Gather, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, x, y, z) in MoxPools)
                mission.Indicator(Gather, marker++, x, y, z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1866",
                "Native1 'Gather the electrical discharge from 10 mox.' 19438/19439 counter19440 'Number of Mox Killed for " +
                "Electrical Discharge'; no objectiveconversation. Texts 19114-19118 (offer: the container). Item class28633 " +
                "(template118997). Templates 121213-121216.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Gun_Control_-_Part_II",
                "Giver/receiver Retread Lou; requires Gun Control; level36; 65000XP, 5400 credits; 4 items lvl35; 10 mox.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesGunControlPartTwoV1",
                "Container 118997 at the accept, taken at the turn-in. Any Mox 540013 kill on map1454 counts, 10. No new pools: " +
                "markers on the World DB pools 540038, 540039 (north of the camp's hill, walked to). Items the client block " +
                "121213-121216, one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding(ContainerKey, ContainerTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-container", MissionId, ContainerKey, ContainerTemplate, 1) }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
