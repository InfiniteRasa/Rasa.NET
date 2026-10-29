using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Alpha Mox (native mission 1027), Plateau: Senior Engineer Mauer wants the crystal of the Mox Alpha
    /// Male, the pack leader at the back of the Mox Lair (PlateauMoxLairV1).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5683-5686 and 6290 (missionconversation); objective
    /// 1 "Acquire the Alpha Mox's crystal." 5687/6291, no counter (missionobjective). The crystal is class
    /// 21984 MisPlateauItemGiantMoxCrystal "Giant Mox Crystal" (template 44923). Mauer is creature 510192,
    /// the Mox Alpha Male 520053.
    ///
    /// Sourced: TaRapedia Alpha Mox: giver and receiver Mauer, requirement Crystal Blue Persuasion, 45,000
    /// XP, 4,800 credits, no item; "The Alpha Mox is in the back of the cave." TR MMO DB: level 31, reward
    /// 4800.
    ///
    /// Reconstructed (ESTIMATE): the crystal a personal drop from the Alpha Male at every kill while
    /// objective 1 is open; Mauer keeps it. Indicator on the Alpha Male.
    /// </summary>
    public static class PlateauAlphaMoxV1
    {
        public const uint MissionId = 1027;
        public const uint Level = 31;
        public const uint Mauer = PlateauFortDefianceNpcsV1.Mauer;
        public const uint AlphaMale = PlateauMoxLairV1.AlphaMale;
        public const uint CrystalTemplate = 44923, CrystalClass = 21984;
        public const string CrystalKey = "giant-mox-crystal";
        public const int CrystalChance = 100;

        public const uint Experience = 45000;
        public const uint Credits = 4800;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Alpha Mox", 5683, Mauer, Mauer, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Prerequisite(PlateauCrystalBluePersuasionV1.MissionId);

            mission.Objective(1, 5687, 6291, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, CrystalClass, 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var at = PlateauMoxLairV1.AlphaAt;
            mission.Indicator(1, 1, at.X, at.Y, at.Z, 15);     // the Alpha Male

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1027",
                "Native1 'Acquire the Alpha Mox's crystal.' 5687/6291, no counter; texts 5683-5686, 6290. Giant Mox Crystal " +
                "class21984/template44923.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Alpha_Mox",
                "Giver/receiver Senior Engineer Mauer, Fort Defiance; requirement Crystal Blue Persuasion; 45000XP, 4800 " +
                "credits, no item; the Alpha Mox in the back of the cave. TR MMO DB: level31.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauAlphaMoxV1",
                "Crystal a 100% personal drop from the Mox Alpha Male520053 while 1 is open; Mauer keeps it. The Alpha Male " +
                "moved to the back of the lair (PlateauMoxLairV1). Indicator r15 on it.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(CrystalKey, CrystalTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AlphaMale }, 1, PlateauMissionDataV1.MapContextId, CrystalChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
