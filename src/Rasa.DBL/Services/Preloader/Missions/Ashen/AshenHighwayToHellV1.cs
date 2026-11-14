using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Highway to Hell (native mission 1247), Ligo Ashen Desert: Captain Sera Karter at Shadow's Edge Post wants the Bane
    /// beachhead north of the post on the Sheitan Highway thinned: ten Thrax Ravagers and ten Thrax Soldiers.
    ///
    /// Native: texts 9962-9966 (missionconversation): log "Travel north of Shadow's Edge Post and kill the Thrax invading from
    /// the Sheitan Highway. You will need to kill 10 Thrax Soldiers and 10 Thrax Ravagers ...", offer "The Bane have established
    /// a strong beachhead north of us on the Sheitan Highway ...", turn-in "You smell like ozone and Thrax blood ...", reminder
    /// "The Thrax are landing in waves, north of here along the Sheitan Highway."; objectives (missionobjective) 1 "Kill 10 Thrax
    /// Ravagers" 9967/9968 counter 9969 "Thrax Ravagers", 3 "Kill 10 Thrax Soldiers" 9973/9974 counter 9975 "Thrax Soldiers"
    /// (no objective 2: its texts 9970-9972 "Thrax Contrivers" are not in missionobjective). No objective conversation. Names
    /// 9281 Karter, 9223 Thrax Ravager, 355 Thrax Soldier Mark VI (World DB 531008).
    ///
    /// Sourced: TaRapedia Highway to Hell: giver and reward giver Captain Sera Karter (Shadow's Edge Post); no requirement;
    /// 180,000 XP, 8,200 credits; one of Vextronics Electric Torqueshell Rifle, Teleract Incendiary Injector Gun, Vextronics
    /// Cryogenic RPG Launcher, ChiTech Laser Chaingun.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band; no chain).
    ///  - The Ravagers are the World DB's Ashen Thrax Grenadier 531009, named "Thrax Ravager" by AshenSheitanHighwayNpcsV1
    ///    (triage D9); the Soldiers its Thrax Soldier Mark VI 531008. Each count a kill of its row (CreatureKilled, counter 0,
    ///    10), wherever it stands; two pools of three of each in and by the highway camp (AshenSheitanHighwayNpcsV1), a marker
    ///    (r20) on each of an objective's two pools.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (client entityclass names): Torqueshell
    ///    Rifle Electric 49576 (38-42), Injection Gun Fire 45777 (37-41, "Incendiary Injector"), Grenade Launcher Ice 47948
    ///    (40-44: the client has no ice rocket launcher; "Cryogenic RPG" read as the ice grenade launcher), Machine Gun Laser
    ///    45762 (38-42, "Chaingun"); one to choose.
    /// </summary>
    public static class AshenHighwayToHellV1
    {
        public const uint MissionId = 1247;
        public const uint Level = 41;
        public const uint Karter = AshenMaunaBarrensNpcsV1.Karter;
        public const uint Ravagers = 1, Soldiers = 3;
        public const uint Kills = 10;
        public const uint Ravager = AshenSheitanHighwayNpcsV1.Ravager;
        public const uint Soldier = AshenSheitanHighwayNpcsV1.Soldier;
        public const double MarkerRadius = 20;

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint ElectricTorqueshell = 49576;  // Weapon_Avatar_Torqueshell_Rifle_Electric_UNC_38_to_42
        public const uint FireInjector = 45777;         // Weapon_Avatar_InjectionGun_Fire_UNC_37_to_41
        public const uint IceLauncher = 47948;          // Weapon_Avatar_GrenadeLauncher_Ice_UNC_40_to_44
        public const uint LaserChaingun = 45762;        // Weapon_Avatar_MachineGun_Laser_UNC_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Highway to Hell", 9962, Karter, Karter, Level);
            mission.Reward(Experience, Credits, new[] { ElectricTorqueshell, FireInjector, IceLauncher, LaserChaingun });

            mission.Objective(Ravagers, 9967, 9968, 0, MissionObjectiveState.Incomplete, counterTextId: 9969);
            mission.Transition(Ravagers);
            mission.Progress(Ravagers, MissionProgressEventKind.CreatureKilled, Ravager, Kills, counter: 0);
            mission.Action(Ravagers, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Soldiers, 9973, 9974, 1, MissionObjectiveState.Incomplete, counterTextId: 9975);
            mission.Transition(Soldiers);
            mission.Progress(Soldiers, MissionProgressEventKind.CreatureKilled, Soldier, Kills, counter: 0);

            var marker = 1U;
            foreach (var (_, creature, at) in AshenSheitanHighwayNpcsV1.ThraxPools)
                mission.Indicator(creature == Ravager ? Ravagers : Soldiers, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1247",
                "Native1 'Kill 10 Thrax Ravagers' 9967/9968 counter9969 'Thrax Ravagers', 3 'Kill 10 Thrax Soldiers' 9973/9974 " +
                "counter9975 'Thrax Soldiers'; no objective 2; no objectiveconversation. Texts 9962-9966 (the Sheitan Highway " +
                "beachhead). Names 9281 Karter, 9223 Thrax Ravager.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Highway_to_Hell",
                "Giver/reward giver Captain Sera Karter (Shadow's Edge Post); no requirement; 180000XP, 8200 credits; Vextronics " +
                "Electric Torqueshell Rifle, Teleract Incendiary Injector Gun, Vextronics Cryogenic RPG Launcher, ChiTech Laser " +
                "Chaingun.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenHighwayToHellV1",
                "Level41 estimated. Ravager = Grenadier 531009 renamed 9223; Soldier = 531008. CreatureKilled, counter 0, 10 each; " +
                "pools 790085-790088 in the highway camp, a marker r20 on each. Items 49576, 45777, 47948, 45762 (lowest " +
                "Uncommon, band of level 41; RPG read as the ice grenade launcher), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
