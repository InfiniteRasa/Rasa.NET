using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// All the Latest Rage! (native mission 1293), Ligo Ashen Desert: after the Atta line, Ida Hayner's next jewelry line is
    /// Granitour shell: 10 Shiny Granitour Carapace off the Granitour of Coheri Pools and the Pristine Granitour Carapace off the
    /// one she calls Wendy.
    ///
    /// Native: texts 10426-10429, 10430 (missionconversation): title "All the Latest Rage!", log "Recover 10 Shiny Granitour
    /// Carapace from the Granitour and 1 Pristine Granitour Carapace from Wendy, all of which can be found in the eastern part
    /// of Maji Field known as Coheri Pools. Return them all to Ida Hayner in Shadow's Edge Post ...", offer "... The Colonel
    /// calls them Granitour ... Everyone loved my Atta line ... I call this one Wendy ...", turn-in "This is just so ab fab.
    /// ..."; objective 1 "Recover 10 Shiny Granitour Carapace" 10431/10432, 2 "Recover a Pristine Granitour Carapace"
    /// 11054/11055, no counters (missionobjective); no objective conversation. Items class 25483 Shiny Granitour Carapace
    /// (template 97484, stack 10), 25487 Pristine Granitour Carapace (97488, stack 1). Names 9288 Ida Hayner, 9362 Wendy. The
    /// client's map marker Coheri Pools (-188, 291, 544).
    ///
    /// Sourced: TaRapedia All the Latest Rage!: giver and reward giver Ida Hayner; requires Designer Jewelry Gone Wrong (1287);
    /// 200,000 XP, 8,600 credits; one of AccuMax Sonic Torqueshell Rifle, Eclipse Virulent Disperser, Vextronics Electric
    /// Polarity Gun, Laser Pistol; Wendy "/loc -190 580, near the marker for Coheri Pools"; Ellatha forum (Kelisco) "-215,
    /// 630" (map view: about -215, 573; within 25 m).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (triage D5: the follow-up of a level-41 mission; TaRapedia's 200,000 XP the zone's highest).
    ///  - The carapace a personal 75% drop off the set's Granitour (AshenDesignerHidesNpcsV1) on Ashen Desert while objective
    ///    1 is open, 10 at most; the Pristine Carapace off Wendy at 100%, one; taken at the turn-in, kept at an abandon.
    ///  - Markers: 1 on the set's two Granitour pools at Coheri Pools (r20), 2 on Wendy's pool (r15).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 (World DB entityclass names): Sonic
    ///    Torqueshell Rifle 45773 (38-42), Virulent Disperser 12077 (42-46), Electric Polarity Gun 15550 (38-42), Laser Pistol
    ///    14492 (40-44); one to choose.
    /// </summary>
    public static class AshenLatestRageV1
    {
        public const uint MissionId = 1293;
        public const uint Level = 42;
        public const uint Ida = AshenLeadInNpcsV1.IdaHayner;
        public const uint Shiny = 1, Pristine = 2;
        public const uint Count = 10;
        public const int Chance = 75, BossChance = 100;
        public const uint ShinyTemplate = 97484, ShinyClass = 25483;
        public const uint PristineTemplate = 97488, PristineClass = 25487;
        public const string ShinyKey = "shiny-granitour-carapace", PristineKey = "pristine-granitour-carapace";
        public const double MarkerRadius = 20, BossMarkerRadius = 15;

        public const uint Experience = 200000;
        public const uint Credits = 8600;
        public const uint SonicTorqueshell = 45773;     // Weapon_Avatar_Torqueshell_Rifle_Sonic_UNC_38_to_42
        public const uint VirulentDisperser = 12077;    // Weapon_Avatar_PropellantGun_Virulent_UNC_42_to_46
        public const uint ElectricPolarityGun = 15550;  // Weapon_Avatar_PolarityGun_Electric_UNC_38_to_42
        public const uint LaserPistol = 14492;          // Weapon_Avatar_Pistol_Laser_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "All the Latest Rage!", 10426, Ida, Ida, Level);
            mission.Reward(Experience, Credits, new[] { SonicTorqueshell, VirulentDisperser, ElectricPolarityGun, LaserPistol });
            mission.Prerequisite(AshenDesignerJewelryV1.MissionId);

            mission.Objective(Shiny, 10431, 10432, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Shiny);
            mission.Progress(Shiny, MissionProgressEventKind.ItemAcquired, ShinyClass, Count);
            mission.Action(Shiny, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Pristine, 11054, 11055, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Pristine);
            mission.Progress(Pristine, MissionProgressEventKind.ItemAcquired, PristineClass, 1);

            var marker = 1U;
            foreach (var (_, at) in AshenDesignerHidesNpcsV1.GranitourPools)
                mission.Indicator(Shiny, marker++, at.X, at.Y, at.Z, MarkerRadius);
            var wendy = AshenDesignerHidesNpcsV1.WendyAt;
            mission.Indicator(Pristine, marker, wendy.X, wendy.Y, wendy.Z, BossMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1293",
                "Native1 'Recover 10 Shiny Granitour Carapace' 10431/10432, 2 'Recover a Pristine Granitour Carapace' 11054/11055, " +
                "no counters; no objectiveconversation. Texts 10426-10430 (Ida Hayner's; the Granitour and Wendy at Coheri Pools, the " +
                "eastern part of Maji Field). Items class25483 Shiny Granitour Carapace (template97484, stack10), class25487 " +
                "Pristine Granitour Carapace (template97488). Names 9288 Ida Hayner, 9362 Wendy. Map marker Coheri Pools (-188, 291, 544).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/All_the_Latest_Rage%21",
                "Giver/reward giver Ida Hayner; requires Designer Jewelry Gone Wrong; 200000XP, 8600 credits; AccuMax Sonic " +
                "Torqueshell Rifle, Eclipse Virulent Disperser, Vextronics Electric Polarity Gun, Laser Pistol; Wendy /loc -190 580 " +
                "near the Coheri Pools marker; Ellatha forum -215, 630 map view.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenLatestRageV1",
                "Level42 estimated. New Granitour 790373 (copy of 540031) in two Coheri Pools pools 790374, 790375 and Wendy 790376 " +
                "(Granitour boss class 25620, pool 790377); the carapace a personal 75% drop, 10 at most, the Pristine Carapace off " +
                "Wendy at 100%; taken at the turn-in, kept at an abandon; markers r20 on the pools, r15 on Wendy. Items 45773, " +
                "12077, 15550, 14492, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Shiny, Pristine },
                Items = new()
                {
                    new MissionItemBinding(ShinyKey, ShinyTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Granitour }, Shiny, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(PristineKey, PristineTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Wendy }, Pristine, AshenMissionDataV1.MapContextId, BossChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
