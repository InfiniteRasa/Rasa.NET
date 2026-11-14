using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Weapon Salvage Recovery (native mission 1266), Ligo Ashen Desert: Scavenger Vakiel Latan wants Thrax weapons for the Larai
    /// Syndicate's repairs: 15 rifles off Thrax Soldiers and 15 grenade launchers off Thrax Ravagers.
    ///
    /// Native: texts 10153-10156, 10157 (missionconversation): log "Recover weapon salvage from Thrax Soldiers and Thrax Ravagers
    /// in the Ashen Desert. Return to Vakiel Latan in the Ashoka Settlement when you have collected 15 Thrax Rifle Salvage and 15
    /// Thrax Grenade Launcher Salvage.", offer "Now we need some weapon parts, to help repair Larai's equipment. ... there are
    /// Thrax everywhere in the desert. ...", turn-in "Now Larai Syndicate will stop harassing me about parts ..."; objective 1
    /// "Recover 15 Thrax Rifles" 10181/10182, 2 "Recover 15 Thrax Grenade Launchers" 10183/10184, no counters
    /// (missionobjective); no objective conversation. Items class 25448 Thrax Rifle Salvage (template 97469), 25449 Thrax
    /// Grenade Launcher Salvage (97470).
    ///
    /// Sourced: TaRapedia Weapon Salvage Recovery: giver and reward giver Latan; requires Robotic Salvage Recovery; follow-up
    /// Heavy Salvage Recovery; 95,000 XP, 6,300 credits; Audiodyne Incendiary Injector Gun. World DB: Thrax Soldier - Ashen
    /// Desert 531008, Thrax Grenadier - Ashen Desert 531009 (named Thrax Ravager by set 3, triage D9).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (D5). A rifle a personal 75% drop (as Robotic Salvage Recovery's) off a Thrax Soldier 531008, a grenade
    ///    launcher off a Thrax Ravager 531009, on Ashen Desert while its objective is open (any pool of them: the client's
    ///    "Thrax everywhere in the desert"); 15 of each at most; taken at the turn-in; kept at an abandon.
    ///  - Markers (r25): the rifles on the Boneyard's three Thrax pools (Dearly Departed's, two or three Soldiers each); the
    ///    launchers on the Sheitan Highway's two Ravager pools (set 3, three each; the Boneyard pools hold at most one).
    ///  - The item: the lowest Uncommon fire injection gun in the band of level 41, 45777 (as Highway to Hell's); the only one.
    /// </summary>
    public static class AshenWeaponSalvageV1
    {
        public const uint MissionId = 1266;
        public const uint Level = 41;
        public const uint Latan = AshenFormalComplaintsNpcsV1.Latan;
        public const uint Rifles = 1, Launchers = 2;
        public const uint Count = 15;
        public const int Chance = AshenRoboticSalvageV1.Chance;
        public const uint RifleTemplate = 97469, RifleClass = 25448;
        public const uint LauncherTemplate = 97470, LauncherClass = 25449;
        public const string RifleKey = "thrax-rifle-salvage", LauncherKey = "thrax-grenade-launcher-salvage";
        public const uint ThraxSoldier = AshenSheitanHighwayNpcsV1.Soldier, ThraxRavager = AshenSheitanHighwayNpcsV1.Ravager;
        public const double MarkerRadius = 25;

        /// <summary>The Sheitan Highway's Ravager pools (set 3) the launcher markers stand on.</summary>
        public static readonly uint[] RavagerPools = { 790085, 790086 };

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint IncendiaryInjector = 45777;   // Weapon_Avatar_InjectionGun_Fire_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Weapon Salvage Recovery", 10153, Latan, Latan, Level);
            mission.Reward(Experience, Credits, new[] { IncendiaryInjector });
            mission.Prerequisite(AshenRoboticSalvageV1.MissionId);

            mission.Objective(Rifles, 10181, 10182, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Rifles);
            mission.Progress(Rifles, MissionProgressEventKind.ItemAcquired, RifleClass, Count);
            mission.Action(Rifles, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Launchers, 10183, 10184, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Launchers);
            mission.Progress(Launchers, MissionProgressEventKind.ItemAcquired, LauncherClass, Count);

            var marker = 1U;
            foreach (var pool in AshenDearlyDepartedV1.Pools)
                mission.Indicator(Rifles, marker++, pool.Marker.X, pool.Marker.Y, pool.Marker.Z, MarkerRadius);
            foreach (var pool in RavagerPools)
            {
                var (_, _, at) = System.Array.Find(AshenSheitanHighwayNpcsV1.ThraxPools, entry => entry.Pool == pool);
                mission.Indicator(Launchers, marker++, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1266",
                "Native1 'Recover 15 Thrax Rifles' 10181/10182, 2 'Recover 15 Thrax Grenade Launchers' 10183/10184, no counters; no " +
                "objectiveconversation. Texts 10153-10157 (Latan's; Thrax Soldiers and Ravagers in the Ashen Desert). Items " +
                "class25448 Thrax Rifle Salvage (template97469), class25449 Thrax Grenade Launcher Salvage (template97470).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Weapon_Salvage_Recovery",
                "Giver/reward giver Scavenger Vakiel Latan (Ashoka); requires Robotic Salvage Recovery; follow-up Heavy Salvage " +
                "Recovery; 95000XP, 6300 credits; Audiodyne Incendiary Injector Gun; Thrax everywhere in the desert.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenWeaponSalvageV1",
                "Level41 estimated. A rifle a personal 75% drop off Thrax Soldier 531008, a launcher off Thrax Ravager 531009, on " +
                "Ashen Desert, 15 of each, taken at the turn-in, kept at an abandon. Markers r25 on the Boneyard Thrax pools 531038, " +
                "531039, 531041 (rifles) and the highway Ravager pools 790085, 790086 (launchers). Item 45777, the only one.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Rifles, Launchers },
                Items = new()
                {
                    new MissionItemBinding(RifleKey, RifleTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { ThraxSoldier }, Rifles, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(LauncherKey, LauncherTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { ThraxRavager }, Launchers, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
