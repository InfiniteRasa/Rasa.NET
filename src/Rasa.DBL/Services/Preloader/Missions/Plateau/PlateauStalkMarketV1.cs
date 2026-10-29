using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Stalk Market (native mission 1017), Plateau: Private "Scrounger" Scarpelo at Camp
    /// Resistance wants Stalker parts from the Bane Stalkers south of Trinity Bridge
    /// (PlateauBaneStalkersV1).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5587-5590 and 6457 (missionconversation);
    /// objectives 1 "Obtain 6 Stalker Actuators." 5605/6458, 2 "Obtain 4 Gyrostabilizers." 5606/6459,
    /// 3 "Obtain 4 Fuel Cells." 5607/6460, no counters (missionobjective). Actuator: item class
    /// 21893 (template 44898); "Stalker Geostabilizer" 21894 (44899), the client's gyrostabilizer;
    /// Protonic Fuel Cell 21895 (44900).
    ///
    /// Sourced (TaRapedia The Stalk Market): giver and receiver Scarpelo, level 31, requirement
    /// Scavenger Hunt, 100,000 XP, 6,600 credits, no item named; the Scrounger's turn-in "See
    /// anything you like?" (5590); the Stalkers south of Trinity Bridge, often in pairs; "Make sure
    /// that you cause most of the damage for the kill or you will not get the drops."
    ///
    /// Reconstructed (ESTIMATE): personal drops from the Plateau Stalker, picked up from the corpse:
    /// an actuator at 50%, a geostabilizer and a fuel cell at 35% each a kill; the Scrounger keeps the
    /// parts at turn-in; one of four Uncommon base items of the 30-35 bands, no module - Electric
    /// Blade, Physical Staff, EMP Pistol, Healing Disc (direct); one indicator over the Stalkers.
    /// The Incline and Mires Stalkers (TaRapedia) are not on the server.
    /// </summary>
    public static class PlateauStalkMarketV1
    {
        public const uint MissionId = 1017;
        public const uint Level = 31;
        public const uint Scarpelo = PlateauScroungerNpcsV1.Scarpelo;

        public const uint ActuatorTemplate = 44898, ActuatorClass = 21893, Actuators = 6;
        public const uint GeostabilizerTemplate = 44899, GeostabilizerClass = 21894, Geostabilizers = 4;
        public const uint FuelCellTemplate = 44900, FuelCellClass = 21895, FuelCells = 4;
        public const int ActuatorChance = 50, GeostabilizerChance = 35, FuelCellChance = 35;

        public const uint Experience = 100000;
        public const uint Credits = 6600;
        public const uint ElectricBlade = 15470;        // Weapon_Avatar_Blade_Electric_UNC_31_to_35
        public const uint PhysicalStaff = 49528;        // Weapon_Avatar_Staff_Physical_UNC_30_to_34
        public const uint EmpPistol = 17055;            // Weapon_Avatar_Pistol_EMP_UNC_30_to_34
        public const uint HealingDisc = 18338;          // Tool_Avatar_Healing_Disc_Direct_UNC_30_to_34

        private static readonly (uint Objective, uint Name, uint Body, string Key, uint Template, uint Class, uint Count, int Chance)[] Parts =
        {
            (1, 5605, 6458, "stalker-actuator", ActuatorTemplate, ActuatorClass, Actuators, ActuatorChance),
            (2, 5606, 6459, "stalker-geostabilizer", GeostabilizerTemplate, GeostabilizerClass, Geostabilizers, GeostabilizerChance),
            (3, 5607, 6460, "protonic-fuel-cell", FuelCellTemplate, FuelCellClass, FuelCells, FuelCellChance),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Stalk Market", 5587, Scarpelo, Scarpelo, Level);
            mission.Reward(Experience, Credits, new[] { ElectricBlade, PhysicalStaff, EmpPistol, HealingDisc });
            mission.Prerequisite(PlateauScavengerHuntV1.MissionId);

            var scene = new MissionSceneDefinition { ExistingFactObjectiveIds = new(), Items = new() };
            foreach (var part in Parts)
            {
                mission.Objective(part.Objective, part.Name, part.Body, part.Objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(part.Objective);
                mission.Progress(part.Objective, MissionProgressEventKind.ItemAcquired, part.Class, part.Count);
                mission.Indicator(part.Objective, part.Objective, -178, 375, 8, 120);     // the Stalkers
                scene.ExistingFactObjectiveIds.Add(part.Objective);
                scene.Items.Add(new MissionItemBinding(part.Key, part.Template, MissionItemScope.CharacterOwned, part.Count,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: part.Count,
                    Drop: new MissionItemDropDefinition(new[] { PlateauBaneStalkersV1.Stalker }, part.Objective,
                        PlateauMissionDataV1.MapContextId, part.Chance, 1)));
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1017",
                "Native1 6 Stalker Actuators 5605/6458, 2 4 Gyrostabilizers 5606/6459, 3 4 Fuel Cells 5607/6460, no counters. " +
                "Texts 5587-5590, 6457. Classes 21893/21894 ('Stalker Geostabilizer')/21895, templates 44898-44900.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Stalk_Market",
                "Giver/receiver Private 'Scrounger' Scarpelo; level31; requirement Scavenger Hunt; 100000XP, 6600 credits, no item " +
                "named; Stalkers south of Trinity Bridge, often in pairs; most damage needed for the drops.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauStalkMarketV1",
                "Personal drops from Plateau Stalker650021 (PlateauBaneStalkersV1) on map1497: actuator 50%, geostabilizer and " +
                "fuel cell 35% (rates, Stalker level, places estimated). Turn-in takes the parts. Uncommon base items, no module: " +
                "15470, 49528, 17055, 18338. Indicator r120 over the Stalkers.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
