using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Weapon Salvage Acquisitions (native mission 1269), Ligo Ashen Desert: Shinichiro Ito's next requisition order, weapon
    /// parts for command's repairs: 15 rifles off Thrax Soldiers and 15 grenade launchers off Thrax Ravagers. The AFS twin of
    /// Latan's Weapon Salvage Recovery (1266).
    ///
    /// Native: texts 10168-10171, 10172 (missionconversation): log "Recover 15 Thrax Rifle Salvage from Thrax Soldiers and 15
    /// Thrax Grenade Launcher Salvage from Thrax Ravagers. Return the requisition order to Shichiro Ito in Ashoka Settlement once
    /// you are done.", offer "The latest requisition order is for weapon parts. ... Their rifles and grenade launchers are ideal
    /// for most of the weapon repairs we have. Same as last time, get out there and kill some Thrax. ...", turn-in "You're making
    /// my life way too easy. ...", reminder "We need some weapon parts recovered from dead Thrax ..."; objective 1 "Recover 15
    /// Thrax Rifles" 10193/10194, 2 "Recover 15 Thrax Grenade Launchers" 10195/10196, no counters (missionobjective); no
    /// objective conversation. Items class 25448 Thrax Rifle Salvage (template 97469), 25449 Thrax Grenade Launcher Salvage
    /// (97470), the same as 1266's.
    ///
    /// Sourced: TaRapedia Weapon Salvage Acquisitions: giver and reward giver Ito; requires Robotic Salvage Acquisitions;
    /// follow-up Heavy Salvage Acquisitions; 6,300 credits, XP "?"; one of Shinobi Cryogenic Blade, Astra Radial Healing Disc,
    /// ChiTech Sonic Pistol, Teleract Pulse Staff.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (D5, as Robotic Salvage Acquisitions). 95,000 XP (TaRapedia "?": as Latan's twin 1266).
    ///  - The salvage as 1266's: a rifle a personal 75% drop off a Thrax Soldier 531008, a grenade launcher off a Thrax Ravager
    ///    531009, on Ashen Desert while its objective is open (any pool of them); 15 of each at most; taken at the turn-in; kept
    ///    at an abandon. Markers (r25): the rifles on the Boneyard's three Thrax pools (531038, 531039, 531041), the launchers
    ///    on the Sheitan Highway's two Ravager pools (as 1266's). 531041 (set 6's point at (487, 271.9, -219), in a dip): the
    ///    client-floor walk from the Ashoka Settlement waypoint climbs 0.99 m over a metre on the way in and 1.01-1.05 m on
    ///    the way out; the real client walked in and out (tester 2026-11-09 23:41-23:54), so the walk check allows 1.1 m.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 (the manufacturer names are not in the
    ///    client tables): Cryogenic Blade 41-45 12101, Area Healing Disc 40-44 119882 ("Radial" read as the area disc, as the
    ///    Pools' Death Patrol), Sonic Pistol 40-44 14452, Pulse Staff 40-44 15220. One to choose.
    /// </summary>
    public static class AshenWeaponAcquisitionsV1
    {
        public const uint MissionId = 1269;
        public const uint Level = 42;
        public const uint Ito = AshenFormalComplaintsNpcsV1.Ito;
        public const uint Rifles = 1, Launchers = 2;
        public const uint Count = 15;
        public const int Chance = AshenRoboticAcquisitionsV1.Chance;
        public const uint RifleTemplate = AshenWeaponSalvageV1.RifleTemplate, RifleClass = AshenWeaponSalvageV1.RifleClass;
        public const uint LauncherTemplate = AshenWeaponSalvageV1.LauncherTemplate, LauncherClass = AshenWeaponSalvageV1.LauncherClass;
        public const string RifleKey = "ito-thrax-rifle-salvage", LauncherKey = "ito-thrax-grenade-launcher-salvage";
        public const uint ThraxSoldier = AshenSheitanHighwayNpcsV1.Soldier, ThraxRavager = AshenSheitanHighwayNpcsV1.Ravager;
        public const double MarkerRadius = 25;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint CryogenicBlade = 12101;       // Weapon_Avatar_Blade_Ice_UNC_41_to_45
        public const uint AreaHealingDisc = 119882;     // Tool_Avatar_Healing_Disc_Area_UNC_40_to_44
        public const uint SonicPistol = 14452;          // Weapon_Avatar_Pistol_Sonic_UNC_40_to_44
        public const uint PulseStaff = 15220;           // Weapon_Avatar_Staff_EMP_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Weapon Salvage Acquisitions", 10168, Ito, Ito, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicBlade, AreaHealingDisc, SonicPistol, PulseStaff });
            mission.Prerequisite(AshenRoboticAcquisitionsV1.MissionId);

            mission.Objective(Rifles, 10193, 10194, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Rifles);
            mission.Progress(Rifles, MissionProgressEventKind.ItemAcquired, RifleClass, Count);
            mission.Action(Rifles, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Launchers, 10195, 10196, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Launchers);
            mission.Progress(Launchers, MissionProgressEventKind.ItemAcquired, LauncherClass, Count);

            var marker = 1U;
            foreach (var pool in AshenDearlyDepartedV1.Pools)
                mission.Indicator(Rifles, marker++, pool.Marker.X, pool.Marker.Y, pool.Marker.Z, MarkerRadius);
            foreach (var pool in AshenWeaponSalvageV1.RavagerPools)
            {
                var (_, _, at) = System.Array.Find(AshenSheitanHighwayNpcsV1.ThraxPools, entry => entry.Pool == pool);
                mission.Indicator(Launchers, marker++, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1269",
                "Native1 'Recover 15 Thrax Rifles' 10193/10194, 2 'Recover 15 Thrax Grenade Launchers' 10195/10196, no counters; no " +
                "objectiveconversation. Texts 10168-10172 (Ito's; Thrax Soldiers and Ravagers). Items class25448 Thrax Rifle Salvage " +
                "(template97469), class25449 Thrax Grenade Launcher Salvage (template97470).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Weapon_Salvage_Acquisitions",
                "Giver/reward giver Shinichiro Ito (Ashoka); requires Robotic Salvage Acquisitions; follow-up Heavy Salvage " +
                "Acquisitions; 6300 credits, XP '?'; Shinobi Cryogenic Blade, Astra Radial Healing Disc, ChiTech Sonic Pistol, " +
                "Teleract Pulse Staff.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenWeaponAcquisitionsV1",
                "Level42, 95000XP estimated. A rifle a personal 75% drop off Thrax Soldier 531008, a launcher off Thrax Ravager " +
                "531009, on Ashen Desert, 15 of each, taken at the turn-in, kept at an abandon. Markers r25 on the Boneyard Thrax " +
                "pools 531038, 531039, 531041 (rifles) and the highway Ravager pools 790085, 790086 (launchers). Items 12101, 119882, " +
                "14452, 15220 (lowest Uncommon of each kind at level 42), one to choose.", 0.3);

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
