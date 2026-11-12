using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Heads Will Roll (native mission 1206), Ligo Crucible: Sergeant Melching at the Awol Camp sends the player after three
    /// Thrax officers who travel together under a Shield Drone.
    ///
    /// Native: texts 9487-9491 (missionconversation): log "Sergeant Melching at the Awol Camp wants you to kill three Thrax
    /// officers that have been harrassing the Awols: Archfiend Krubb, Coercer Mungrel and Defiler Sedge.", offer "They travel
    /// together, and they're usually protected by a Shield Drone. You'll need to get in close and take out that drone",
    /// turn-in "I can't believe you got all three of them!"; objectives (missionobjective) 1 "Kill Archfiend Krubb" 9509/9510,
    /// 2 "Kill Coercer Mungrel" 9511/9512, 3 "Kill Defiler Sedge" 9513/9514, no counters. No objective conversation.
    ///
    /// Sourced: TaRapedia Heads Will Roll: giver and reward giver Sergeant Melching (Awol Camp); 160,000 XP, 7,800 credits;
    /// requires The Other Side Of The Coin or Making Amends (TaRapedia 1205, 1153, 1155 follow-ups, 0.5); locs -839.5, 285.4,
    /// 576.3; -731, 466; -764, 276, 493; one of Vextronics Photonic Blade, Teleract Incendiary Injector Gun, Shinobi Electric
    /// Chaingun, Vextronics Cryogenic Shotgun (0.5). TR MMO DB: level 45.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The offer after 1153 or 1155 completed (scene Requirement, AnyRequirement of two MissionStateRequirements: the mission
    ///    rows take one prerequisite kind only, an AND).
    ///  - The officers and their squad: CrucibleStaalJunkyardNpcsV1. Each objective a kill of its officer (CreatureKilled), any
    ///    order; a kill before the accept does not count. One marker for each on the rock bridge over their trench (60 m).
    ///  - The items: TaRapedia's four kinds (the client has no reward table), each the lowest Uncommon template whose band holds
    ///    level 45 (Laser Blade 41-45, Fire Injection Gun 42-46, Electric Machine Gun 43-47, Ice Shotgun 42-46), one to choose.
    /// </summary>
    public static class CrucibleHeadsWillRollV1
    {
        public const uint MissionId = 1206;
        public const uint Level = 45;
        public const uint Melching = CrucibleGrupaChiefsNpcsV1.Melching;
        public const uint KillKrubb = 1, KillMungrel = 2, KillSedge = 3;
        public const double MarkerRadius = 60;

        /// <summary>The officers: objective, name text, body text, creature.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Creature)[] Officers =
        {
            (KillKrubb, 9509, 9510, CrucibleStaalJunkyardNpcsV1.Krubb),
            (KillMungrel, 9511, 9512, CrucibleStaalJunkyardNpcsV1.Mungrel),
            (KillSedge, 9513, 9514, CrucibleStaalJunkyardNpcsV1.Sedge)
        };

        public const uint Experience = 160000;
        public const uint Credits = 7800;
        public const uint LaserBlade = 12105;           // Weapon_Avatar_Blade_Laser_UNC_41_to_45
        public const uint FireInjectionGun = 12143;     // Weapon_Avatar_InjectionGun_Fire_UNC_42_to_46
        public const uint ElectricMachineGun = 12027;   // Weapon_Avatar_MachineGun_Electric_UNC_43_to_47
        public const uint IceShotgun = 11973;           // Weapon_Avatar_Shotgun_v1_Ice_UNC_42_to_46

        /// <summary>Offered once The Other Side Of The Coin (1153) or Making Amends (1155) is completed.</summary>
        public static MissionRequirement AfterTheCamp() => new AnyRequirement(new MissionRequirement[]
        {
            new MissionStateRequirement(CrucibleOtherSideOfTheCoinV1.MissionId, MissionState.Completed),
            new MissionStateRequirement(CrucibleMakingAmendsV1.MissionId, MissionState.Completed)
        });

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Heads Will Roll", 9487, Melching, Melching, Level);
            mission.Reward(Experience, Credits, new[] { LaserBlade, FireInjectionGun, ElectricMachineGun, IceShotgun });

            var bridge = CrucibleStaalJunkyardNpcsV1.RockBridgeAt;
            foreach (var (objective, name, body, creature) in Officers)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature, null);
                mission.Indicator(objective, objective, bridge.X, bridge.Y, bridge.Z, MarkerRadius);
            }
            mission.Action(KillKrubb, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1206",
                "Native1 'Kill Archfiend Krubb' 9509/9510, 2 'Kill Coercer Mungrel' 9511/9512, 3 'Kill Defiler Sedge' 9513/9514, no " +
                "counters; no objectiveconversation. Texts 9487-9491 (they travel together under a Shield Drone). Names 9175, 9176, 9177.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Heads_Will_Roll",
                "Giver/reward giver Sergeant Melching (Awol Camp); 160000XP, 7800 credits; locs -839.5,285.4,576.3; -731,466; " +
                "-764,276,493; TaR-hist rev27891 '-796,289,528 a small rock bridge, 3 Thrax Bosses spawn around the bridge'. Requires " +
                "1153 or 1155 (follow-up lists). Vextronics Photonic Blade, Teleract Incendiary Injector Gun, Shinobi Electric " +
                "Chaingun, Vextronics Cryogenic Shotgun. TR MMO DB level45.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleHeadsWillRollV1",
                "Offered after 1153 or 1155 completed (scene requirement). Krubb 770031 (copy of Derge), Mungrel 770032 (of Davinx), " +
                "Sedge 770033 (of Orax), level45, in NPC squad 770031 with a Shield Drone 770022 on the trench by the rock bridge " +
                "(there and back, 300s respawn). Three CreatureKilled objectives, any order. Items 12105, 12143, 12027, 11973 (TaRapedia's " +
                "kinds), one to choose. A marker at the bridge for each.", 0.3);

            mission.Enable(new MissionSceneDefinition { Requirement = AfterTheCamp() });
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
