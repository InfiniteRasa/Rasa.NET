using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Gun Control (native mission 1865), Valverde Marshes: Retread Lou at High Point Retreat builds a special-order gun and
    /// needs parts off the Bane: Thrax Warrant Officer armor fragments and Lightbender rifle power cores.
    ///
    /// Native: texts 19109-19113 (missionconversation): log "Head to the east and gather some materials off the Thrax
    /// Warrant Officers and Lightbenders.", offer "what I need is currently on the Bane that are right over the hill",
    /// reminder (10 cores; the objective says 5); objectives 1 "Gather 10 Thrax Warrant Officer armor fragments."
    /// 19392/19393, 2 "Gather 5 Lightbender rifle power cores." 19394/19395, no counters (missionobjective); no objective
    /// conversation. Items MisMarshes class 28631 Thrax Warrant Officer Armor Fragments (World DB template 118995, stack 10)
    /// and 28632 Lightbender Rifle Power Cores (118996). Client item templates 121209-121212: Hazmat Gloves 34-38, Motor
    /// Assist Gloves 33-37, Bio Helmet 35-39, Reflective Gloves 34-38.
    ///
    /// Sourced: TaRapedia Gun Control: giver and receiver Retread Lou (Stalker Woods); 65,000 XP, 5,400 credits; four items
    /// of levels 34/33/35/34 (the client block in that order); 10 fragments, 5 cores. World DB: no "Warrant Officer" on the
    /// Marshes; the Thrax Technician 531047 (level 34) in ten mixed Thrax pools 531282-531296.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The Warrant Officers the Marshes' Thrax Technicians 531047 (the triage); fragments personal drops at 60%, from any
    ///    of them on the map. The cores from the new Lightbenders east of the camp (MarshesGunControlNpcsV1) at 50%. No
    ///    source gives a rate. Lou keeps both at the turn-in. The objective's 5 cores, not the reminder's 10.
    ///  - Level 36 (the camp's; TaRapedia none). Markers on the three technician pools nearest the camp (531283, 531293,
    ///    531288) and on the Lightbender pool. The rewards the client block 121209-121212, one to choose.
    /// </summary>
    public static class MarshesGunControlV1
    {
        public const uint MissionId = 1865;
        public const uint Level = 36;
        public const uint Lou = MarshesGunControlNpcsV1.Lou;
        public const uint Fragments = 1, Cores = 2;

        public const uint FragmentTemplate = 118995, FragmentClass = 28631, FragmentCount = 10;
        public const string FragmentKey = "warrant-officer-armor-fragments";
        public const int FragmentChance = 60;
        public const uint ThraxTechnician = 531047;     // Thrax Technician - Marshes (World DB)

        public const uint CoreTemplate = 118996, CoreClass = 28632, CoreCount = 5;
        public const string CoreKey = "lightbender-rifle-power-cores";
        public const int CoreChance = 50;
        public const uint Lightbender = MarshesGunControlNpcsV1.Lightbender;
        public const double PoolMarkerRadius = 25;

        /// <summary>The Thrax pools with technicians nearest High Point Retreat (World DB spawnpool).</summary>
        public static readonly (uint Pool, double X, double Y, double Z)[] TechnicianPools =
        {
            (531283, -318.0, 285.3, -570.1), (531293, -470.0, 217.2, 39.3), (531288, 24.0, 223.3, -368.3)
        };

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint HazmatGloves = 121209;        // Armor_T2_Hazmat_V02_UNC_Gloves_34_to_38
        public const uint MotorAssistGloves = 121210;   // Armor_T1_MotorAssist_V06_UNC_Gloves_33_to_37
        public const uint BioHelmet = 121211;           // Armor_T3_Bio_V04_UNC_Helmet_35_to_39
        public const uint ReflectiveGloves = 121212;    // Armor_T2_Reflective_V02_UNC_Gloves_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Gun Control", 19109, Lou, Lou, Level);
            mission.Reward(Experience, Credits, new[] { HazmatGloves, MotorAssistGloves, BioHelmet, ReflectiveGloves });

            mission.Objective(Fragments, 19392, 19393, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Fragments);
            mission.Progress(Fragments, MissionProgressEventKind.ItemAcquired, FragmentClass, FragmentCount);
            mission.Objective(Cores, 19394, 19395, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Cores);
            mission.Progress(Cores, MissionProgressEventKind.ItemAcquired, CoreClass, CoreCount);
            mission.Action(Fragments, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (_, x, y, z) in TechnicianPools)
                mission.Indicator(Fragments, marker++, x, y, z, PoolMarkerRadius);
            var lightbenders = MarshesGunControlNpcsV1.LightbenderAt;
            mission.Indicator(Cores, marker, lightbenders.X, lightbenders.Y, lightbenders.Z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1865",
                "Native1 'Gather 10 Thrax Warrant Officer armor fragments.' 19392/19393, 2 'Gather 5 Lightbender rifle power " +
                "cores.' 19394/19395, no counters; no objectiveconversation. Texts 19109-19113 (log: east; offer: the Bane over " +
                "the hill). Items class28631 (template118995), class28632 (118996). Templates 121209-121212.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Gun_Control",
                "Giver/receiver Retread Lou (Stalker Woods); 65000XP, 5400 credits; 4 items lvl34/33/35/34; 10 Thrax Warrant " +
                "Officer armor fragments, 5 Lightbender rifle power cores; the Bane right over the hill.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesGunControlV1",
                "Warrant Officers = Thrax Technician 531047 (any on map1454), fragments 60%; cores 50% from the new Lightbender " +
                "760331 pool east of the camp; rates estimated, Lou keeps both. Level36. Markers on pools 531283, 531293, 531288 " +
                "and the Lightbenders. Items the client block 121209-121212, one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Fragments, Cores },
                Items = new()
                {
                    new MissionItemBinding(FragmentKey, FragmentTemplate, MissionItemScope.CharacterOwned, FragmentCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: FragmentCount,
                        Drop: new MissionItemDropDefinition(new[] { ThraxTechnician }, Fragments, MarshesMissionDataV1.MapContextId, FragmentChance, 1)),
                    new MissionItemBinding(CoreKey, CoreTemplate, MissionItemScope.CharacterOwned, CoreCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: CoreCount,
                        Drop: new MissionItemDropDefinition(new[] { Lightbender }, Cores, MarshesMissionDataV1.MapContextId, CoreChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
