using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Out Of Sight (native mission 1208), Ligo Crucible: Top Khana Sigal Pico wants six Optical Processors from Bane Shield
    /// Drones for the Top Khana's own sharpshooting sights.
    ///
    /// Native: texts 9502-9505, 9506 (missionconversation): log "Hunt Shield Drones and acquire 6 of their Optical Processors
    /// for Sigal Pico of the Top Khana syndicate at Staal.", offer "we are needing the Optical Processors of Bane Shield
    /// Drones", turn-in "Ah, I see you have acquired the Optical Processors I requested."; objective 1 "Get 6 Optical
    /// Processors." 9507/9508, no counter (missionobjective); no objective conversation. Item class 25078
    /// MisCrucibleItemOpticalProcessor "Shield Drone Optical Processor" (template 50434). Name 9063 "Top Khana Sigal Pico".
    ///
    /// Sourced: TaRapedia Out Of Sight: giver Top Khana Sigal Pico (Staal Detention Center); requires Smoking Guns (1148);
    /// 80,000 XP, 5,850 credits; one of Titan Mech Armor Gloves (icon 2), Hellstrom Stealth Armor Vest (2), Pulsar Hazmat Armor
    /// Gloves (2), Pulsar Motor Assist Armor Gloves (5); "Shield Drone Optical Processor" from Bane Shield Drones; a user note
    /// (0.3): "can also be obtained by killing shield drones on other maps, I did it on Mires".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 35 (no source: the level of 1148, and 1202's, which pays the same). Receiver Pico (the turn-in text is his).
    ///    Pico new (CrucibleStaalSigalsNpcsV1).
    ///  - The drones the new Crucible Shield Drones 770022 in pools 770024-770026 north of Khulago Base. A processor a personal
    ///    drop from any of them on the Crucible at 50% while the objective is open (no source gives a rate). Only on the
    ///    Crucible: a drop binding names one map (MissionItemDropDefinition), so the drones of other maps (TaRapedia's user
    ///    note) give none. Pico takes the six at the turn-in; an abandon keeps them.
    ///  - The items the lowest Uncommon templates of the kinds and icon numbers in the band of level 35: Mech V02 Gloves 32806,
    ///    Stealth V02 Vest 10637, Hazmat V02 Gloves 20484, Motor Assist V05 Gloves 29533; one to choose. A marker (radius 20)
    ///    on each drone pool.
    /// </summary>
    public static class CrucibleOutOfSightV1
    {
        public const uint MissionId = 1208;
        public const uint Level = 35;
        public const uint Pico = CrucibleStaalSigalsNpcsV1.Pico;
        public const uint Processors = 1;
        public const uint ProcessorCount = 6;
        public const int ProcessorChance = 50;
        public const uint ProcessorTemplate = 50434, ProcessorClass = 25078;
        public const string ProcessorKey = "shield-drone-optical-processors";
        public const uint ShieldDrone = CrucibleStaalSigalsNpcsV1.ShieldDrone;
        public const double MarkerRadius = 20;

        public const uint Experience = 80000;
        public const uint Credits = 5850;
        public const uint MechGloves = 32806;           // Armor_T3_Mech_V02_UNC_Gloves_35_to_39
        public const uint StealthVest = 10637;          // Armor_T3_Stealth_V02_UNC_Vest_35_to_39
        public const uint HazmatGloves = 20484;         // Armor_T2_Hazmat_V02_UNC_Gloves_34_to_38
        public const uint MotorAssistGloves = 29533;    // Armor_T1_MotorAssist_V05_UNC_Gloves_33_to_37

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Out Of Sight", 9502, Pico, Pico, Level);
            mission.Reward(Experience, Credits, new[] { MechGloves, StealthVest, HazmatGloves, MotorAssistGloves });
            mission.Prerequisite(CrucibleSmokingGunsV1.MissionId);

            mission.Objective(Processors, 9507, 9508, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Processors);
            mission.Progress(Processors, MissionProgressEventKind.ItemAcquired, ProcessorClass, ProcessorCount);
            mission.Action(Processors, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, at) in CrucibleStaalSigalsNpcsV1.DronePools)
                mission.Indicator(Processors, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1208",
                "Native1 'Get 6 Optical Processors.' 9507/9508, no counter; no objectiveconversation. Texts 9502-9506 (Bane Shield " +
                "Drones). Item class25078 Shield Drone Optical Processor (template50434). Name 9063 Top Khana Sigal Pico; class 7233 " +
                "Bane_Shield_Drone.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Out_Of_Sight",
                "Giver Top Khana Sigal Pico (Staal Detention Center); requires Smoking Guns; 80000XP, 5850 credits; one of Titan Mech " +
                "Gloves 2, Hellstrom Stealth Vest 2, Pulsar Hazmat Gloves 2, Pulsar Motor Assist Gloves 5; from Bane Shield Drones; " +
                "user note: also from drones on other maps (Mires).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleOutOfSightV1",
                "Level35. Receiver Pico 770020 new (copy of Vitto's row) 4 m from Munit. New Shield Drones 770022 (class 7233, " +
                "level35) in pools 770024-770026 north of Khulago Base; a processor a personal 50% drop on the Crucible only, taken " +
                "at the turn-in, kept at an abandon. Items 32806, 10637, 20484, 29533, one to choose. A marker r20 on each pool.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Processors },
                Items = new()
                {
                    new MissionItemBinding(ProcessorKey, ProcessorTemplate, MissionItemScope.CharacterOwned, ProcessorCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: ProcessorCount,
                        Drop: new MissionItemDropDefinition(new[] { ShieldDrone }, Processors, CrucibleMissionDataV1.MapContextId, ProcessorChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
