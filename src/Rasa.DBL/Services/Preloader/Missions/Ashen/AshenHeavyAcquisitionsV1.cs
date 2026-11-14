using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Heavy Salvage Acquisitions (native mission 1268), Ligo Ashen Desert: the last of Shinichiro Ito's requisition orders, the
    /// power cores of the large Bane vehicles: one salvage off a Desert Stalker in the Boneyard and one off a Desert Predator on
    /// the Sheitan Highway. The AFS twin of Latan's Heavy Salvage Recovery (1265); its completion gives the title "Desert
    /// Logistician" (TaRapedia; the title is the Targets of Opportunity's, not built here).
    ///
    /// Native: texts 10163-10166, 10167 (missionconversation): log "Recover 1 Intact Stalker Salvage from Desert Stalkers in the
    /// Boneyard and 1 Intact Predator Salvage from Desert Predators at Sheitan Highway. Return to Shichiro Ito once you have
    /// completed the requisition order.", offer "HQ has put in a req order for some specialized salvage, specifically the power
    /// cores from those Desert Stalkers and Predators. ... I recommend getting some friends to help you on this ...", turn-in
    /// "Well, soldier, it looks like you've filled all of our requisition orders for now. ...", reminder "The order is for some
    /// special salvaged parts from Bane Stalkers and Bane Predators. ..."; objective 1 "Recover 1 Stalker Salvage" 10189/10190,
    /// 2 "Recover 1 Predator Salvage" 10191/10192, no counters (missionobjective); no objective conversation. Items class 25446
    /// Intact Stalker Salvage (template 97467), 25447 Intact Predator Salvage (97468), the same as 1265's.
    ///
    /// Sourced: TaRapedia Heavy Salvage Acquisitions: giver and reward giver Ito; requires Weapon Salvage Acquisitions;
    /// follow-up none; no XP, credits or item; "Completion gives the Title 'Desert Logistician'".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (D5). 200,000 XP and 6,300 credits: no source (D6); as Latan's twin 1265 (TaRapedia's for it).
    ///  - The salvage as 1265's: the Stalker salvage a personal drop off set 4's Desert Stalker 790121, the Predator salvage off
    ///    set 3's Desert Predator 790080 (the Sheitan Highway beats: the client log), each on Ashen Desert while its objective is
    ///    open, at 100%, one of each; taken at the turn-in; kept at an abandon. Markers as 1265's.
    ///  - Rewards: none named: the stacks of the zone's talks (Formal Complaints'), one to choose (as 1265).
    /// </summary>
    public static class AshenHeavyAcquisitionsV1
    {
        public const uint MissionId = 1268;
        public const uint Level = 42;
        public const uint Ito = AshenFormalComplaintsNpcsV1.Ito;
        public const uint StalkerSalvage = 1, PredatorSalvage = 2;
        public const int Chance = 100;
        public const uint StalkerTemplate = AshenHeavySalvageV1.StalkerTemplate, StalkerClass = AshenHeavySalvageV1.StalkerClass;
        public const uint PredatorTemplate = AshenHeavySalvageV1.PredatorTemplate, PredatorClass = AshenHeavySalvageV1.PredatorClass;
        public const string StalkerKey = "ito-intact-stalker-salvage", PredatorKey = "ito-intact-predator-salvage";
        public const uint Stalker = AshenBoneyardNpcsV1.Stalker, Predator = AshenSheitanHighwayNpcsV1.Predator;

        public const uint Experience = 200000;
        public const uint Credits = 6300;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Heavy Salvage Acquisitions", 10163, Ito, Ito, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (AshenFormalComplaintsV1.EmpGrenade, AshenFormalComplaintsV1.EmpGrenades), (AshenFormalComplaintsV1.ResTraumaKit, AshenFormalComplaintsV1.ResTraumaKits),
                    (AshenFormalComplaintsV1.ConcussionGrenade, AshenFormalComplaintsV1.ConcussionGrenades), (AshenFormalComplaintsV1.BasicMedPack, AshenFormalComplaintsV1.BasicMedPacks)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenWeaponAcquisitionsV1.MissionId);

            mission.Objective(StalkerSalvage, 10189, 10190, 0, MissionObjectiveState.Incomplete);
            mission.Transition(StalkerSalvage);
            mission.Progress(StalkerSalvage, MissionProgressEventKind.ItemAcquired, StalkerClass, 1);
            mission.Action(StalkerSalvage, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(PredatorSalvage, 10191, 10192, 1, MissionObjectiveState.Incomplete);
            mission.Transition(PredatorSalvage);
            mission.Progress(PredatorSalvage, MissionProgressEventKind.ItemAcquired, PredatorClass, 1);

            var marker = 1U;
            foreach (var (_, at) in AshenBoneyardNpcsV1.StalkerPools)
                mission.Indicator(StalkerSalvage, marker++, at.X, at.Y, at.Z, AshenStalkingTheStalkersV1.MarkerRadius);
            foreach (var (at, radius) in AshenAirSuperiorityV1.Markers)
                mission.Indicator(PredatorSalvage, marker++, at.X, at.Y, at.Z, radius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1268",
                "Native1 'Recover 1 Stalker Salvage' 10189/10190, 2 'Recover 1 Predator Salvage' 10191/10192, no counters; no " +
                "objectiveconversation. Texts 10163-10167 (Ito's; Stalkers in the Boneyard, Predators at Sheitan Highway). Items " +
                "class25446 Intact Stalker Salvage (template97467), class25447 Intact Predator Salvage (template97468).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Heavy_Salvage_Acquisitions",
                "Giver/reward giver Shinichiro Ito (Ashoka); requires Weapon Salvage Acquisitions; follow-up none; no XP, credits or " +
                "item named; completion gives the title Desert Logistician.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenHeavyAcquisitionsV1",
                "Level42, 200000XP and 6300 credits estimated (as 1265). The Stalker salvage a personal 100% drop off Desert Stalker " +
                "790121 (Boneyard pools 790125, 790126), the Predator salvage off Desert Predator 790080 (Sheitan Highway beats " +
                "790089, 790090), one each, taken at the turn-in, kept at an abandon; markers as 1265's. Stacks of 1254 (111029x3, " +
                "47600x1, 111019x3, 45063x6), one to choose (zone median).", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { StalkerSalvage, PredatorSalvage },
                Items = new()
                {
                    new MissionItemBinding(StalkerKey, StalkerTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Stalker }, StalkerSalvage, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(PredatorKey, PredatorTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Predator }, PredatorSalvage, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
