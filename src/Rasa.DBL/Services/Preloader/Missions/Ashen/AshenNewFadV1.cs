using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// A New Fad (native mission 1291), Ligo Ashen Desert: after the Lasher line, Ida Hayner's next clothes are Beam Manta
    /// leather: 10 Beam Manta Leather Hides off the Beam Manta and the Pristine Beam Manta Hide off the one she calls Trudy.
    ///
    /// Native: texts 10412-10415, 10416 (missionconversation): title "A New Fad", log "Recover 10 Beam Manta Leather Hides from
    /// the Beam Manta in the Ashen Desert, and 1 Pristine Beam Manta Hide from the Beam Manta named Trudy. You can find Beam
    /// Manta along the eastern side of the Boneyard, the Indra Pass, and the northeast part of Maji Field. You can find Trudy
    /// along the northeast ridge of Maji Field. ...", offer "Lashers aren't the only things out there with high quality
    /// leather. Beam Manta! ... I call her Trudy ...", turn-in "Straight off the presses. ..."; objective 1 "Recover 10 Beam
    /// Manta Hides" 10417/10418, 2 "Recover 1 Pristine Beam Manta Hide" 11058/11059, no counters (missionobjective); no
    /// objective conversation. Items class 25484 Beam Manta Leather Hide (template 97485, stack 10), 25489 Pristine Beam Manta
    /// Leather (97490, stack 1). Names 9288 Ida Hayner, 9361 Trudy, 8023 Beam Manta Pilot.
    ///
    /// Sourced: TaRapedia A New Fad: giver and reward giver Ida Hayner; requires I Wouldn't Wear It, But... (1289); 190,000 XP,
    /// 8,400 credits; one of ChiTech Electric Polarity Gun, Vextronics Electric Net Gun, Sonic Cannon, Electric Chaingun;
    /// TaRapedia Trudy: "a boss Beam Manta ... at coordinates -242 771 on top of hill, north of Coheri Pools map marker at the
    /// very north of the map".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (triage D5: the follow-up of a level-41 mission, one level up as 1293 after 1287). The XP does not fix the
    ///    level: 190,000 is given by 1277 (level 41) and 1278 (level 42), and 1269 (level 42) gives 95,000.
    ///  - The hide a personal 75% drop off the set's Beam Manta (AshenDesignerHidesNpcsV1) on Ashen Desert while objective 1
    ///    is open, 10 at most; the Pristine Hide off Trudy at 100%, one; taken at the turn-in, kept at an abandon.
    ///  - Markers: 1 on the set's three Beam Manta pools, one in each of the client's three places (r20), 2 on Trudy's pool
    ///    (r15).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 (World DB entityclass names): Electric
    ///    Polarity Gun 15550 (38-42), Electric Net Gun 12131 (41-45), Sonic Cannon 49172 (39-43), Electric Chaingun 15111
    ///    (38-42); one to choose.
    /// </summary>
    public static class AshenNewFadV1
    {
        public const uint MissionId = 1291;
        public const uint Level = 42;
        public const uint Ida = AshenLeadInNpcsV1.IdaHayner;
        public const uint Leather = 1, Pristine = 2;
        public const uint Count = 10;
        public const int Chance = 75, BossChance = 100;
        public const uint LeatherTemplate = 97485, LeatherClass = 25484;
        public const uint PristineTemplate = 97490, PristineClass = 25489;
        public const string LeatherKey = "beam-manta-leather-hide", PristineKey = "pristine-beam-manta-hide";
        public const double MarkerRadius = 20, BossMarkerRadius = 15;

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint ElectricPolarityGun = 15550;  // Weapon_Avatar_PolarityGun_Electric_UNC_38_to_42
        public const uint ElectricNetGun = 12131;       // Weapon_Avatar_NetGun_Electric_UNC_41_to_45
        public const uint SonicCannon = 49172;          // Weapon_Avatar_RocketLauncher_Sonic_UNC_39_to_43
        public const uint ElectricChaingun = 15111;     // Weapon_Avatar_MachineGun_Electric_UNC_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "A New Fad", 10412, Ida, Ida, Level);
            mission.Reward(Experience, Credits, new[] { ElectricPolarityGun, ElectricNetGun, SonicCannon, ElectricChaingun });
            mission.Prerequisite(AshenWouldntWearItV1.MissionId);

            mission.Objective(Leather, 10417, 10418, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Leather);
            mission.Progress(Leather, MissionProgressEventKind.ItemAcquired, LeatherClass, Count);
            mission.Action(Leather, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Pristine, 11058, 11059, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Pristine);
            mission.Progress(Pristine, MissionProgressEventKind.ItemAcquired, PristineClass, 1);

            var marker = 1U;
            foreach (var (_, at) in AshenDesignerHidesNpcsV1.MantaPools)
                mission.Indicator(Leather, marker++, at.X, at.Y, at.Z, MarkerRadius);
            var trudy = AshenDesignerHidesNpcsV1.TrudyAt;
            mission.Indicator(Pristine, marker, trudy.X, trudy.Y, trudy.Z, BossMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1291",
                "Native1 'Recover 10 Beam Manta Hides' 10417/10418, 2 'Recover 1 Pristine Beam Manta Hide' 11058/11059, no counters; " +
                "no objectiveconversation. Texts 10412-10416 (Ida Hayner's; Beam Manta along the eastern side of the Boneyard, the " +
                "Indra Pass and the northeast part of Maji Field, Trudy along its northeast ridge). Items class25484 Beam Manta " +
                "Leather Hide (template97485, stack10), class25489 Pristine Beam Manta Leather (template97490). Names 9288 Ida " +
                "Hayner, 9361 Trudy.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_New_Fad",
                "Giver/reward giver Ida Hayner; requires I Wouldn't Wear It, But...; 190000XP, 8400 credits; ChiTech Electric " +
                "Polarity Gun, Vextronics Electric Net Gun, Sonic Cannon, Electric Chaingun; Trudy at -242 771 on top of a hill " +
                "north of the Coheri Pools marker (TaRapedia Trudy).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenNewFadV1",
                "Level42 estimated. New Beam Manta 790367 (copy of 540040) in three pools 790368-790370 (the eastern Boneyard, the " +
                "Indra Pass, northeast Maji Field) and Trudy 790371 (Beam Manta boss class 10317, pool 790372); the hide a personal " +
                "75% drop, 10 at most, the Pristine Hide off Trudy at 100%; taken at the turn-in, kept at an abandon; markers r20 " +
                "on the pools, r15 on Trudy. Items 15550, 12131, 49172, 15111, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Leather, Pristine },
                Items = new()
                {
                    new MissionItemBinding(LeatherKey, LeatherTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Manta }, Leather, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(PristineKey, PristineTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Trudy }, Pristine, AshenMissionDataV1.MapContextId, BossChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
