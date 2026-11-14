using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Irreconcilable Differences (native mission 1185), Ligo Crucible: Trista, Kappa Grupa Lohen's bondservant, wants a
    /// champion. Lohen sets the challenge: kill the Thrax Preceptor Molitor, who travels with a pack of his kind, and bring
    /// Lohen his necklace of bones. Lohen lets Trista go; she stays with him.
    ///
    /// Native: texts 9062-9065, 9066 (missionconversation): log "Trista, bondservant to Kappa Grupa Lohen, wants you to
    /// champion her by asking Lohen for a challenge.", offer "Tell Lohen that you wish to challenge my bond", turn-in "I am
    /// free to choose my own path now, but I choose to remain at his side."; objectives (missionobjective, no counters) 1
    /// "Speak to Lohen." 9076/9077, 2 "Get Molitor's Bone Necklace." 9079/9080 ("Kill Thrax Preceptor Molitor and retrieve his
    /// Necklace of Bones."), 3 "Return to Lohen." 9081/9082; objective conversations (1185,1) package 1222 Lohen variant 1 text
    /// 9078 ("There is a demon Thrax who plagues our business-doings in the outlands ... He travels with a pack of his kind"),
    /// (1185,3) 1222 variant 1 text 9118 ("This necklace is indeed a trophy befitting the Kappa Grupa!"). Item class 24956
    /// MisCrucibleItemMolitorsBoneNecklace "Precept Molitor's Bone Necklace" (template 50360). Names 9072 "Trista", 9172
    /// "Preceptor Molitor".
    ///
    /// Sourced: TaRapedia Irreconcilable Differences: Trista gives and rewards (Staal Detention Center); requires Earning
    /// Respect (1170); 160,000 XP, 3,900 credits; one of Class VII Cryogenic Grenade x3, Standard Med Pack x3, Incendiary Grenade
    /// x3, Advanced Med Pack x2 (the client names, physicalentityclassnamelanguage: 26298 template 111048, 22544 45061, 26288
    /// 111038, 22545 45062). TaRapedia Preceptor Molitor: north-east of the AWOL Camp, patrols the road, -846.0, 266.2, 376.4;
    /// "He drops Molitor's Bone Necklace". TR MMO DB quest list: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - 1 opens 2, 2 opens 3, as Plateau Cryptic Messages (557). The necklace a personal drop from Molitor 770023 at 100%
    ///    while objective 2 is open (CrucibleStaalSigalsNpcsV1: a copy of Executioner Derge, level 43, in one pool with three
    ///    Thrax Soldiers and a Thrax Grenadier); a necklace already held counts when 2 opens. Lohen takes it at 3; the reward at
    ///    Trista. An abandon keeps it. Once 2 is complete Molitor drops no necklace again: a player who loses it abandons and
    ///    does the mission again (as Cryptic Messages).
    ///  - Trista new, next to Lohen in his office. Markers: Lohen (1 and 3), Molitor's pool (2, radius 20).
    /// </summary>
    public static class CrucibleIrreconcilableDifferencesV1
    {
        public const uint MissionId = 1185;
        public const uint Level = 35;
        public const uint Trista = CrucibleStaalSigalsNpcsV1.Trista;
        public const uint Lohen = CrucibleGrupaChiefsNpcsV1.Lohen, LohenPackage = CrucibleGrupaChiefsNpcsV1.LohenPackage;
        public const uint Challenge = 1, Necklace = 2, ReturnToLohen = 3;
        public const uint NecklaceTemplate = 50360, NecklaceClass = 24956;
        public const string NecklaceKey = "molitors-bone-necklace";
        public const int NecklaceChance = 100;
        public const uint Molitor = CrucibleStaalSigalsNpcsV1.Molitor;
        public const double LohenMarkerRadius = 8, MolitorMarkerRadius = 20;

        public const uint Experience = 160000;
        public const uint Credits = 3900;
        public const uint CryogenicGrenade = 111048, CryogenicGrenades = 3;     // Class VII Cryogenic Grenade (class 26298)
        public const uint StandardMedPack = 45061, StandardMedPacks = 3;        // Class VII Standard Med Pack (class 22544)
        public const uint IncendiaryGrenade = 111038, IncendiaryGrenades = 3;   // Class VII Incendiary Grenade (class 26288)
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;        // Class VII Advanced Med Pack (class 22545)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Irreconcilable Differences", 9062, Trista, Trista, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks), (IncendiaryGrenade, IncendiaryGrenades),
                    (AdvancedMedPack, AdvancedMedPacks)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleEarningRespectV1.MissionId);

            mission.Objective(Challenge, 9076, 9077, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Necklace, 9079, 9080, 1, MissionObjectiveState.Inactive);
            mission.Objective(ReturnToLohen, 9081, 9082, 2, MissionObjectiveState.Inactive);

            // 1: Lohen sets the challenge.
            mission.Transition(Challenge);
            mission.Conversation(Challenge, LohenPackage);
            Open(mission, Challenge, 1, Necklace);

            // 2: Molitor's necklace.
            mission.Transition(Necklace);
            mission.Progress(Necklace, MissionProgressEventKind.ItemAcquired, NecklaceClass, 1);
            Open(mission, Necklace, 1, ReturnToLohen);

            // 3: Lohen takes the necklace.
            mission.Transition(ReturnToLohen);
            mission.Conversation(ReturnToLohen, LohenPackage);
            mission.Action(ReturnToLohen, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("lohen-takes-the-necklace", MissionId, NecklaceKey, 1, MissionItemScope.CharacterOwned));
            mission.Action(ReturnToLohen, 2, MissionActionKind.GrantReward, reward: 1);

            var lohen = CrucibleGrupaChiefsNpcsV1.At(Lohen);
            var molitor = CrucibleStaalSigalsNpcsV1.MolitorAt;
            mission.Indicator(Challenge, 1, lohen.X, lohen.Y, lohen.Z, LohenMarkerRadius);
            mission.Indicator(Necklace, 2, molitor.X, molitor.Y, molitor.Z, MolitorMarkerRadius);
            mission.Indicator(ReturnToLohen, 3, lohen.X, lohen.Y, lohen.Z, LohenMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1185",
                "Native1 'Speak to Lohen.' 9076/9077, 2 'Get Molitor's Bone Necklace.' 9079/9080, 3 'Return to Lohen.' 9081/9082, no " +
                "counters; objectiveconversation 1185,1 package1222 text9078 and 1185,3 1222 text9118. Texts 9062-9066. Necklace " +
                "class24956 (template50360). Names 9072 Trista, 9172 Preceptor Molitor (a pack of his kind).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Irreconcilable_Differences",
                "Trista gives and rewards (Staal Detention Center; standing next to Lohen); requires Earning Respect; 160000XP, 3900 " +
                "credits; Class VII Cryogenic Grenade x3, Standard Med Pack x3, Incendiary Grenade x3, Advanced Med Pack x2. Molitor " +
                "north-east of the AWOL Camp, -846.0, 266.2, 376.4 (TaRapedia, Ellatha). TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleIrreconcilableDifferencesV1",
                "1 opens 2, 2 opens 3. Molitor 770023 new (copy of Derge 520013, level43, 10905HP) in pool 770029 with three Thrax " +
                "Soldiers 531018 and a Grenadier 531019; the necklace a personal 100% drop while 2 is open; Lohen takes it at 3. " +
                "Trista 770021 new (copy of Lohen's row: no female Brann row) 4 m from Lohen. Markers on Lohen and the pool.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Necklace },
                Items = new()
                {
                    new MissionItemBinding(NecklaceKey, NecklaceTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { Molitor }, Necklace, CrucibleMissionDataV1.MapContextId, NecklaceChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        /// <summary>Reveals and activates <paramref name="next"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(CrucibleMissionDataV1 mission, uint objective, uint first, uint next)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete);
        }
    }
}
