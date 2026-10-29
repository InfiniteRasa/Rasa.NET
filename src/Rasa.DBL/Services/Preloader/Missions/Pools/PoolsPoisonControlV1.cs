using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Poison Control (native mission 1368), Valverde Pools: Amee Corman sends the player into the Thrax Barracks to set
    /// the three poison canisters to self-destruct, the Retreads' side of the fork (PoolsPoisonCanistersV1).
    ///
    /// Native: texts 11283-11287 (missionconversation): log "Doing anything but destroying the Canisters will fail this
    /// mission.", turn-in "If we Retreads had medals you would have earned the highest one."; objectives 1, 3, 5 "Destroy
    /// Poison Canister I-III" 15000/15001, 15014/15015, 15026/15027; 2, 4, 6 "Tracker" (missionobjective); the consoles'
    /// packages 1641-1643 (PoolsPoisonCanistersV1).
    ///
    /// Sourced: TaRapedia Poison Control: giver and receiver Amee Corman (Retread Camp); requires Spy Networking; 130,000
    /// XP, 7,200 credits; one of Wellcare Bio Boots [4], Olympia Graviton Boots [4], Vitalius Mech Boots [4], Teleract
    /// Stealth Boots [4], level 35. TR MMO DB: level 32.
    ///
    /// Reconstructed (ESTIMATE): the canisters and the fork as PoolsPoisonCanistersV1; the trackers left out. Rewards the
    /// lowest Uncommon V04 boots of the four lines at 35, one to choose. The markers on the consoles.
    /// </summary>
    public static class PoolsPoisonControlV1
    {
        public const uint MissionId = 1368;
        public const uint Level = 32;
        public const uint Amee = PoolsRetreadCampNpcsV1.Amee;

        public const uint Experience = 130000;
        public const uint Credits = 7200;
        public const uint BioBoots = 46004;             // Armor_T3_Bio_V04_UNC_Boots_35_to_39
        public const uint GravitonBoots = 24841;        // Armor_T3_Graviton_V04_UNC_Boots_35_to_39
        public const uint MechBoots = 33967;            // Armor_T3_Mech_V04_UNC_Boots_35_to_39
        public const uint StealthBoots = 40094;         // Armor_T3_Stealth_V04_UNC_Boots_35_to_39

        public static readonly (uint Name, uint Body)[] Texts = { (15000, 15001), (15014, 15015), (15026, 15027) };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Poison Control", 11283, Amee, Amee, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.Prerequisite(PoolsSpyNetworkingV1.MissionId);
            var scene = PoolsPoisonCanistersV1.Build(mission, MissionId, PoolsTakingThePoisonV1.MissionId,
                PoolsPoisonCanistersV1.Retreads, PoolsPoisonCanistersV1.Afs, ownIsSelfDestruct: true, item: null, Texts);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1368",
                "Native1/3/5 'Destroy Poison Canister I-III' 15000-15027, 2/4/6 'Tracker'; objectiveconversation packages1641-1643 " +
                "flag1 'Self Destruct', flag446 'Self Destruct'/'Dispense Poison Cannister'. Texts 11283-11287.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Poison_Control",
                "Giver/receiver Amee Corman; requires Spy Networking; 130000XP, 7200 credits; one of Wellcare Bio, Olympia Graviton, " +
                "Vitalius Mech, Teleract Stealth Boots[4]35; a command completes its mission's objective and fails the other. TR " +
                "MMO DB level32.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsPoisonCanistersV1",
                "Personal consoles (10519) at three Barracks consoles (estimated, user); fork flag1368001; trackers left out. " +
                "Items 46004, 24841, 33967, 40094.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
