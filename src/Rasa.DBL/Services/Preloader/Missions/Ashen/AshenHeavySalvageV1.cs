using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Heavy Salvage Recovery (native mission 1265), Ligo Ashen Desert: the last of Scavenger Vakiel Latan's salvage jobs, the
    /// power cores and computers of the large Bane vehicles: one salvage off a Desert Stalker in the Boneyard and one off a
    /// Desert Predator on the Sheitan Highway.
    ///
    /// Native: texts 10148-10151, 10152 (missionconversation): log "Recover 1 Intact Predator Salvage from Desert Predators along
    /// Sheitan Highway and 1 Intact Stalker Salvage from Desert Stalkers in the Boneyard. Return to Vakiel Latan once you are
    /// finished.", offer "... Stalkers and Predators have unique power cores and unique computers ... There are Stalkers in the
    /// Boneyard, and Predators near Maji Field. ...", turn-in "I can't tell you how much help you have been to us. ... you will
    /// always have an ally in me."; objective 1 "Recover 1 Stalker Salvage" 10177/10178, 2 "Recover 1 Predator Salvage"
    /// 10179/10180, no counters (missionobjective); no objective conversation. Items class 25446 Intact Stalker Salvage
    /// (template 97467), 25447 Intact Predator Salvage (97468), stack 1.
    ///
    /// Sourced: TaRapedia Heavy Salvage Recovery: giver Latan; requires Weapon Salvage Recovery; 200,000 XP (0.3: the chain's
    /// other two give 95,000), 6,300 credits; no item named.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (D5). The Stalker salvage a personal drop off set 4's Desert Stalker 790121 (the Boneyard's two pools of one),
    ///    the Predator salvage off set 3's Desert Predator 790080 (its two beats on the Sheitan Highway: the client's log and
    ///    TaRapedia; the offer's "near Maji Field" is not followed, deviation), each on Ashen Desert while its objective is open,
    ///    at 100% (one of each; one vehicle a kill, as the log's "recover 1 ... from Desert Predators"); taken at the turn-in;
    ///    kept at an abandon.
    ///  - Markers on the vehicles' places as their own missions have them: the Stalker pools (r15, Stalking the Stalkers'), the
    ///    Predator beats (Air Superiority's two markers).
    ///  - Rewards: TaRapedia names no item: the stacks of the zone's talks (Formal Complaints'), one to choose (triage D6, as
    ///    The Human Perspective).
    /// </summary>
    public static class AshenHeavySalvageV1
    {
        public const uint MissionId = 1265;
        public const uint Level = 42;
        public const uint Latan = AshenFormalComplaintsNpcsV1.Latan;
        public const uint StalkerSalvage = 1, PredatorSalvage = 2;
        public const int Chance = 100;
        public const uint StalkerTemplate = 97467, StalkerClass = 25446;
        public const uint PredatorTemplate = 97468, PredatorClass = 25447;
        public const string StalkerKey = "intact-stalker-salvage", PredatorKey = "intact-predator-salvage";
        public const uint Stalker = AshenBoneyardNpcsV1.Stalker, Predator = AshenSheitanHighwayNpcsV1.Predator;

        public const uint Experience = 200000;
        public const uint Credits = 6300;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Heavy Salvage Recovery", 10148, Latan, Latan, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (AshenFormalComplaintsV1.EmpGrenade, AshenFormalComplaintsV1.EmpGrenades), (AshenFormalComplaintsV1.ResTraumaKit, AshenFormalComplaintsV1.ResTraumaKits),
                    (AshenFormalComplaintsV1.ConcussionGrenade, AshenFormalComplaintsV1.ConcussionGrenades), (AshenFormalComplaintsV1.BasicMedPack, AshenFormalComplaintsV1.BasicMedPacks)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenWeaponSalvageV1.MissionId);

            mission.Objective(StalkerSalvage, 10177, 10178, 0, MissionObjectiveState.Incomplete);
            mission.Transition(StalkerSalvage);
            mission.Progress(StalkerSalvage, MissionProgressEventKind.ItemAcquired, StalkerClass, 1);
            mission.Action(StalkerSalvage, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(PredatorSalvage, 10179, 10180, 1, MissionObjectiveState.Incomplete);
            mission.Transition(PredatorSalvage);
            mission.Progress(PredatorSalvage, MissionProgressEventKind.ItemAcquired, PredatorClass, 1);

            var marker = 1U;
            foreach (var (_, at) in AshenBoneyardNpcsV1.StalkerPools)
                mission.Indicator(StalkerSalvage, marker++, at.X, at.Y, at.Z, AshenStalkingTheStalkersV1.MarkerRadius);
            foreach (var (at, radius) in AshenAirSuperiorityV1.Markers)
                mission.Indicator(PredatorSalvage, marker++, at.X, at.Y, at.Z, radius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1265",
                "Native1 'Recover 1 Stalker Salvage' 10177/10178, 2 'Recover 1 Predator Salvage' 10179/10180, no counters; no " +
                "objectiveconversation. Texts 10148-10152 (Latan's; Predators along Sheitan Highway, Stalkers in the Boneyard). " +
                "Items class25446 Intact Stalker Salvage (template97467), class25447 Intact Predator Salvage (template97468).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Heavy_Salvage_Recovery",
                "Giver Scavenger Vakiel Latan (Ashoka); requires Weapon Salvage Recovery; 200000XP, 6300 credits; no item named.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenHeavySalvageV1",
                "Level42 estimated. The Stalker salvage a personal 100% drop off Desert Stalker 790121 (Boneyard pools 790125, " +
                "790126), the Predator salvage off Desert Predator 790080 (Sheitan Highway beats 790089, 790090), one each, taken at " +
                "the turn-in, kept at an abandon; markers as 1272's and 1248's. Stacks of 1254 (111029x3, 47600x1, 111019x3, 45063x6), " +
                "one to choose (zone median).", 0.3);

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
