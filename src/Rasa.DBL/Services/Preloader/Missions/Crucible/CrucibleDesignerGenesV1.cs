using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Designer Genes (native mission 1881), Ligo Crucible: Zupa Eugin wants five DNA samples of Thrax bosses.
    ///
    /// Native: texts 19215-19219 (missionconversation); objective 1 "Get 5 DNA Samples from Thrax bosses." 19489/19490, no
    /// counter (missionobjective); no objective conversation. Item class 28658 Thrax Overseer DNA (template 121148).
    ///
    /// Sourced: TaRapedia Designer Genes: Eugin gives and rewards; requires The Gene Machine; follow-up Genome Sweet Genome;
    /// 150,000 XP, 7,600 credits; 5 DNA samples from "Thrax Overseers, Preceptors, Executioners and other officers" in the
    /// Crucible; "you can even kill the same boss several times"; control point bosses drop none (rev 28327 "Killed BBQ at CP,
    /// no DNA dropped"); Molitor and Mungrel north of the Awol Camp, Sedge and Krubb by the Seeds logos; 3 bosses round a small
    /// rock bridge at (-796, 289, 528) (cache, 0.5). TTH: "you can repeat the kill", control point bosses do not count; reward
    /// Graviton Gloves, Motor Assist, Reflective or Hazmat Vest (0.5). TaRapedia levels 38, 39, 39, 35. PlanetTR-3001: one boss
    /// killed over and over works (0.3). TR MMO DB level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The bosses: the Thrax officers the server has on the Crucible outside the control point: Preceptor Molitor 770023,
    ///    Archfiend Krubb 770031, Coercer Mungrel 770032, Defiler Sedge 770033 (the rock bridge trench), Maxxum Ugoretz 770042
    ///    (Khulago) and Executioner Derge 520013 (the Fort Intrepid rampart). Each kill gives one sample (100 %), the killer's,
    ///    while 1 is open; the samples are the player's own and stay at an abandon (as Whiplash's glands). Inquisitor Braak and
    ///    Taskmaster Druul (TTH) have no row; Lamontix stands in the Junkyard instance (a drop is bound to one map).
    ///  - Markers on Molitor (r20), the rock bridge trench (r60), Ugoretz (r15) and Derge (r15).
    ///  - The items: the client's Uncommon rows of TaRapedia's kinds and levels (Graviton V02 Gloves 35-39 23678, MotorAssist
    ///    V04 Vest 38-42 29247, Reflective V02 Vest 39-43 36188, Hazmat V02 Vest 34-38 20931), one to choose.
    /// </summary>
    public static class CrucibleDesignerGenesV1
    {
        public const uint MissionId = 1881;
        public const uint Level = 35;
        public const uint Eugin = CrucibleHybridArcNpcsV1.Eugin;
        public const uint Samples = 1;
        public const uint SampleCount = 5;
        public const int SampleChance = 100;
        public const uint SampleTemplate = 121148, SampleClass = 28658;
        public const string SampleKey = "thrax-overseer-dna";

        /// <summary>The Thrax bosses that give a sample.</summary>
        public static readonly uint[] Bosses =
        {
            CrucibleStaalSigalsNpcsV1.Molitor, CrucibleStaalJunkyardNpcsV1.Krubb, CrucibleStaalJunkyardNpcsV1.Mungrel,
            CrucibleStaalJunkyardNpcsV1.Sedge, CruciblePrometheusOutpostNpcsV1.Ugoretz, CrucibleMinefieldAndSalvageNpcsV1.Derge
        };

        public const double MolitorRadius = 20, RockBridgeRadius = 60, BossRadius = 15;

        public const uint Experience = 150000;
        public const uint Credits = 7600;
        public const uint GravitonGloves = 23678;       // Armor_T3_Graviton_V02_UNC_Gloves_35_to_39
        public const uint MotorAssistVest = 29247;      // Armor_T1_MotorAssist_V04_UNC_Vest_38_to_42
        public const uint ReflectiveVest = 36188;       // Armor_T2_Reflective_V02_UNC_Vest_39_to_43
        public const uint HazmatVest = 20931;           // Armor_T2_Hazmat_V02_UNC_Vest_34_to_38

        /// <summary>The markers: place and radius.</summary>
        public static ((double X, double Y, double Z) At, double Radius)[] Markers => new[]
        {
            (CrucibleFixFV1.Now(CrucibleStaalSigalsNpcsV1.MolitorPool).Value, MolitorRadius),
            (CrucibleStaalJunkyardNpcsV1.RockBridgeAt, RockBridgeRadius),
            (CruciblePrometheusOutpostNpcsV1.UgoretzAt, BossRadius),
            (CrucibleMinefieldAndSalvageNpcsV1.DergeAt, BossRadius)
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Designer Genes", 19215, Eugin, Eugin, Level);
            mission.Reward(Experience, Credits, new[] { GravitonGloves, MotorAssistVest, ReflectiveVest, HazmatVest });
            mission.Prerequisite(CrucibleGeneMachineV1.MissionId);

            mission.Objective(Samples, 19489, 19490, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Samples);
            mission.Progress(Samples, MissionProgressEventKind.ItemAcquired, SampleClass, SampleCount);
            mission.Action(Samples, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (at, radius) in Markers)
                mission.Indicator(Samples, marker++, at.X, at.Y, at.Z, radius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1881",
                "Native1 'Get 5 DNA Samples from Thrax bosses.' 19489/19490, no counter; no objectiveconversation. Texts " +
                "19215-19219. Item class28658 Thrax Overseer DNA (template121148).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Designer_Genes",
                "Eugin gives and rewards; requires The Gene Machine; follow-up Genome Sweet Genome; 150000XP, 7600 credits; 5 DNA " +
                "samples from Thrax Overseers, Preceptors, Executioners and other officers; the same boss again counts; CP bosses " +
                "drop none; Molitor, Mungrel, Sedge, Krubb; 3 bosses by the rock bridge -796,289,528. TTH: Graviton Gloves, Motor " +
                "Assist, Reflective, Hazmat Vest. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleDesignerGenesV1",
                "A sample (the player's own, kept at an abandon) at every kill of Molitor 770023, Krubb 770031, Mungrel 770032, " +
                "Sedge 770033, Ugoretz 770042, Derge 520013 while 1 is open; five complete it; Eugin takes them. Braak and Druul " +
                "have no row. Items 23678, 29247, 36188, 20931, one to choose. Markers on Molitor, the rock bridge, Ugoretz, Derge.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Samples },
                Items = new()
                {
                    new MissionItemBinding(SampleKey, SampleTemplate, MissionItemScope.CharacterOwned, SampleCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: SampleCount,
                        Drop: new MissionItemDropDefinition(Bosses, Samples, CrucibleMissionDataV1.MapContextId, SampleChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
