using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Crystal Blue Persuasion (native mission 954), Plateau: Senior Engineer Mauer at Fort Defiance High
    /// Command powers the base with crystals from the Mox's gut, from the Mox Lair (PlateauMoxLairV1).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5182-5185 and 6344 (missionconversation); objective
    /// 1 "Collect 12 Mox Crystals." 5205/6345, no counter (missionobjective). The crystal is class 21813
    /// MisPlateauItemMoxCrystal (template 44821). Mauer is creature 510192.
    ///
    /// Sourced: TaRapedia Crystal Blue Persuasion (stub): giver and receiver Mauer, requirement The
    /// Defiant Ones, follow-up Alpha Mox, 40,000 XP, no credits, "100 Cartridges" four times as the
    /// choice; "Killing the Mox with turrets will make the crystals fail to drop." TR MMO DB: level 30,
    /// reward 0; "This quest opens up after you have completed the Defiant Ones quest".
    ///
    /// Reconstructed (ESTIMATE): a crystal a personal drop from the Plateau Mox (540016) at half its kills
    /// while objective 1 is open, picked up from the corpse; Mauer keeps the twelve. The four choices read
    /// as 100 of each ammunition kind at the level 21-30 grade (High Grade): cartridges, power cells,
    /// canister ammunition, rockets.
    /// </summary>
    public static class PlateauCrystalBluePersuasionV1
    {
        public const uint MissionId = 954;
        public const uint Level = 30;
        public const uint Mauer = PlateauFortDefianceNpcsV1.Mauer;
        public const uint Mox = PlateauMoxLairV1.Mox;
        public const uint CrystalTemplate = 44821, CrystalClass = 21813, Crystals = 12;
        public const string CrystalKey = "mox-crystal";
        public const int CrystalChance = 50;

        public const uint Experience = 40000;
        public const uint Credits = 0;
        public const uint Ammunition = 100;
        public const uint Cartridges = 110904;          // Ammo_Cartridge_3_High_Grade
        public const uint PowerCells = 110896;          // Ammo_Battery_3_High_Grade
        public const uint Canisters = 110899;           // Ammo_Canister_3_High_Grade
        public const uint Rockets = 110912;             // Ammo_Rocket_3_High_Grade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Crystal Blue Persuasion", 5182, Mauer, Mauer, Level);
            mission.Reward(Experience, Credits,
                new (uint, uint)[] { (Cartridges, Ammunition), (PowerCells, Ammunition), (Canisters, Ammunition), (Rockets, Ammunition) },
                new (uint, uint)[0]);
            mission.Prerequisite(PlateauDefiantOnesV1.MissionId);

            mission.Objective(1, 5205, 6345, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, CrystalClass, Crystals);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -443, 374, 790, 40);        // the Mox Lair

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/954",
                "Native1 'Collect 12 Mox Crystals.' 5205/6345, no counter; texts 5182-5185, 6344. Crystal class21813/template44821.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Crystal_Blue_Persuasion",
                "Stub. Giver/receiver Senior Engineer Mauer, Fort Defiance; requirement The Defiant Ones; 40000XP, no credits; " +
                "'100 Cartridges' four times; turret kills drop no crystal. TR MMO DB: level30.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauCrystalBluePersuasionV1",
                "Crystal a 50% personal drop from Mox540016 while 1 is open; Mauer keeps twelve. Choices as 100 High Grade " +
                "ammunition each: 110904, 110896, 110899, 110912. Mox Lair pools and Mauer's place (PlateauMoxLairV1, " +
                "PlateauFortDefianceNpcsV1).", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(CrystalKey, CrystalTemplate, MissionItemScope.CharacterOwned, Crystals,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: Crystals,
                        Drop: new MissionItemDropDefinition(new[] { Mox }, 1, PlateauMissionDataV1.MapContextId, CrystalChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
