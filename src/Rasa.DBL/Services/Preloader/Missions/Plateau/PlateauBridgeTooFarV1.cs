using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// A Bridge Too Far (native mission 713), Plateau: Field Lt. Peterson at Fort Defiance wants the CPU
    /// of the Bane's new Juggernaut prototype, which fights at Trinity Bridge (PlateauPrototypeJuggernautV1).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 3365-3368 and 6286 (missionconversation); objective
    /// 1 "Retrieve a Prototype Juggernaut CPU." 4508/6287, no counter (missionobjective). The CPU is class
    /// 21015 MisPlateauItemJuggernautCPU "Juggernaut CPU" (template 42406). Peterson is creature 510182.
    ///
    /// Sourced: TaRapedia A Bridge Too Far: giver and receiver Peterson, requirement The Defiant Ones,
    /// follow-up The Beer Hunter, 45,000 XP, 6,200 credits, item names lost ("or; or; or"). TR MMO DB:
    /// level 30, reward 6200. Ten Ton Hammer (Fort Defiance part 1): 6200 credits "and your choice of
    /// Cryogen Bio Armor Legs, CryoGen Mech Armor Vest, CryoGen Stealth Armor Boots, or Prodigy Reflective
    /// Armor Gloves".
    ///
    /// Reconstructed (ESTIMATE): the CPU a personal drop from the Prototype Juggernaut at every kill while
    /// objective 1 is open, Peterson keeps it; the items as the Uncommon base templates of those slots in
    /// the 30-34 band (29-33 for the Reflective gloves), no manufacturer or modifier: Bio V04 legs, Mech V04
    /// vest, Stealth V04 boots, Reflective V02 gloves. Indicator on the Juggernaut's spawn.
    /// </summary>
    public static class PlateauBridgeTooFarV1
    {
        public const uint MissionId = 713;
        public const uint Level = 30;
        public const uint Peterson = PlateauFortDefianceNpcsV1.Peterson;
        public const uint Juggernaut = PlateauPrototypeJuggernautV1.Juggernaut;
        public const uint CpuTemplate = 42406, CpuClass = 21015;
        public const string CpuKey = "prototype-juggernaut-cpu";
        public const int CpuChance = 100;

        public const uint Experience = 45000;
        public const uint Credits = 6200;
        public const uint BioLegs = 46027;              // Armor_T3_Bio_V04_UNC_Legs_30_to_34
        public const uint MechVest = 34463;             // Armor_T3_Mech_V04_UNC_Vest_30_to_34
        public const uint StealthBoots = 40074;         // Armor_T3_Stealth_V04_UNC_Boots_30_to_34
        public const uint ReflectiveGloves = 35700;     // Armor_T2_Reflective_V02_UNC_Gloves_29_to_33

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "A Bridge Too Far", 3365, Peterson, Peterson, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, MechVest, StealthBoots, ReflectiveGloves });
            mission.Prerequisite(PlateauDefiantOnesV1.MissionId);

            mission.Objective(1, 4508, 6287, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, CpuClass, 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var at = PlateauPrototypeJuggernautV1.At;
            mission.Indicator(1, 1, at.X, at.Y, at.Z, 40);      // the Juggernaut

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/713",
                "Native1 'Retrieve a Prototype Juggernaut CPU.' 4508/6287, no counter; texts 3365-3368, 6286. CPU class21015/" +
                "template42406. Juggernaut name8772, class24555 (boss).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Bridge_Too_Far",
                "Giver/receiver Field Lt. Peterson, Fort Defiance; requirement The Defiant Ones; 45000XP, 6200 credits, items " +
                "lost. Ten Ton Hammer: choice of Cryogen Bio Legs, CryoGen Mech Vest, CryoGen Stealth Boots, Prodigy " +
                "Reflective Gloves; the Juggernaut around -170,374,322.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBridgeTooFarV1",
                "Level30. CPU a 100% personal drop from the Prototype Juggernaut650026 (PlateauPrototypeJuggernautV1) while 1 " +
                "is open; Peterson keeps it. Items as Uncommon base templates, no manufacturer or modifier: 46027, 34463, " +
                "40074, 35700. Indicator r40 on the spawn.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(CpuKey, CpuTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Juggernaut }, 1, PlateauMissionDataV1.MapContextId, CpuChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
