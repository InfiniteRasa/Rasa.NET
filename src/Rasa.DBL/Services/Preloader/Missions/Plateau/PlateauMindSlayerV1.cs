using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Mind Slayer (native mission 719), Plateau: Special Agent Perdu sends the player into the Derak prison
    /// in Desolation Quarry to kill Inquisitor Krakatus and bring back his datapad.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 3438-3441 and 6403 (missionconversation); objective 1
    /// "Kill Inquisitor Krakatus." 5774/6405, "Assassinate Inquisitor Krakatus at the Derak Prison Facility
    /// inside Desolation Quarry and retrieve his datapad", no counter (missionobjective); no objective
    /// conversation. Datapad: Inquisitor Krakatus' Datapad, class 22087 (template 45007). Offer (3440): "That
    /// surveillance data blew this case wide open". Derak is on the Plateau map, no instance (gamecontext has
    /// none); Krakatus is the server's boss 520052, at the prison tunnel (243, 313.6, -240).
    ///
    /// Sourced: TaRapedia Mind Slayer (stub): giver and receiver Agent Perdu, level "32 (possibly less)",
    /// requirement Mind Bender, 100,000 XP, 6,600 credits, "Equipment"; the datapad 0/1; "Krakatus is in the
    /// back section of the jail". Ellatha: Inquisitor Krakatus, Technician, "Inside Derak Prison".
    ///
    /// Reconstructed (ESTIMATE): level 32; offered after The Results Are In (936), whose TaRapedia follow-up is
    /// Mind Slayer and which the offer text follows, not after Mind Bender. The datapad a personal drop from
    /// Krakatus at every kill while objective 1 is open; Perdu keeps it. "Equipment" read as Giant Killer's
    /// four Uncommon base weapons of the level-30 band.
    /// </summary>
    public static class PlateauMindSlayerV1
    {
        public const uint MissionId = 719;
        public const uint Level = 32;
        public const uint Perdu = PlateauPerduNpcsV1.Perdu;
        public const uint Krakatus = 520052;
        public const uint DatapadTemplate = 45007, DatapadClass = 22087;
        public const string DatapadKey = "krakatus-datapad";
        public const int DatapadChance = 100;
        public static readonly (double X, double Y, double Z) KrakatusAt = (243.4, 313.6, -240);

        public const uint Experience = 100000;
        public const uint Credits = 6600;
        public const uint WeaponA = 47644, WeaponB = 48764, WeaponC = 120733, WeaponD = 16597;   // as Giant Killer

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Mind Slayer", 3438, Perdu, Perdu, Level);
            mission.Reward(Experience, Credits, new[] { WeaponA, WeaponB, WeaponC, WeaponD });
            mission.Prerequisite(PlateauResultsAreInV1.MissionId);

            mission.Objective(1, 5774, 6405, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, DatapadClass, 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, KrakatusAt.X, KrakatusAt.Y, KrakatusAt.Z, 15);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/719",
                "Native1 'Kill Inquisitor Krakatus.' 5774/6405, no counter, no objective conversation; texts 3438-3441, 6403. " +
                "Datapad class22087/template45007. No Derak gamecontext: the prison is on map1497. Krakatus name8605.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mind_Slayer",
                "Stub. Giver/receiver Agent Perdu; level '32 (possibly less)'; requirement Mind Bender; 100000XP, 6600 credits, " +
                "'Equipment'; Krakatus in the back of the jail. Ellatha: Krakatus inside Derak Prison.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMindSlayerV1",
                "Level32. Offered after The Results Are In (936). Datapad a 100% personal drop from Krakatus520052 while 1 is " +
                "open; Perdu keeps it. Equipment read as Giant Killer's weapons: 47644, 48764, 120733, 16597.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(DatapadKey, DatapadTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Krakatus }, 1, PlateauMissionDataV1.MapContextId, DatapadChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
