using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// What's that Gas? (native mission 539), Valverde Marshes: Shaman Geli in Paludos wants samples of the marsh's plants
    /// and creatures to find the source of the sickness: calla fern fronds, Swamp Grubber hides, Maw glands and Mox hearts.
    ///
    /// Native: texts 1446-1449 and 9360 (missionconversation): log "Collect the 5 calla fern fronds from east of Paludos or
    /// northeast of Falcon Hold, 10 Swamp Grubber hides, 5 Maw glands, and 5 Mox hearts. Return them to Shaman Geli in
    /// Paludos", offer "Bring me calla ferns, Swamp Grubber hides, Maw glands, and Mox hearts.", turn-in "I am certain these
    /// samples will help us discover the source of the plague of the marshes."; objectives 1 "Gather 5 Calla Fern Fronds"
    /// 1542/9361, 2 "Gather 10 Swamp Grubber Hides" 1543/9362, 3 "Collect 5 Maw Glands" 1544/9363, 4 "Collect 10 Mox
    /// Hearts" 1545/9364, no counters (missionobjective); no objective conversation; indicator name 5 "Calla Ferns"
    /// (missionobjectiveindicatorlanguage). Items (World DB itemtemplate_itemclass): Calla Fern Frond class 7664 (template
    /// 657), Swamp Grubber Hide 7659 (652), Maw Gland 7662 (655), Mox Heart 7660 (653). Name 3122 Shaman Geli.
    ///
    /// Sourced: TaRapedia What's that Gas?: giver and receiver Shaman "Gali" (Paludos; the client's only name is Geli);
    /// level 34; 65,000 XP, 5,400 credits; 5 fronds, 10 hides, 5 glands, 10 hearts; one of Teleract Cryogenic RPG Launcher
    /// lvl 35, ChiTech Incendiary Leech Gun lvl 34, Eclipse Electric Net Gun lvl 36, Hellstrom Pistol lvl 35; the fronds
    /// dropped by Swamp Grubbers (walkthrough; a 2007 user note found none). TR MMO DB: level 33, 5,400 credits. World DB:
    /// Shaman Geli 510172 (pool at -318, 226, -11, an upstream placeholder spot); on map 1454 Swamp Grubber 540014 (2
    /// pools), Maw 531052 (3) and 540012 (3), Mox 540013 (3).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Ten Mox hearts: the client's objective title (and TaRapedia) over its log's five (client wins, the title).
    ///  - Personal drops, picked up from the corpse: the fronds from Swamp Grubbers at 50%, the hides from them at 100%,
    ///    the glands from either Maw row at 50%, the hearts from Mox at 50% (no source gives a rate). Geli keeps them.
    ///  - Six mission-only pools east of Paludos (MarshesPaludosNpcsV1.GasPools, two of each creature), as the open world
    ///    has too few (the Pools 1132 precedent); any of the creature's kills on the map count.
    ///  - Markers: objective 1 under the client's name 5 "Calla Ferns" round both Grubber pools, 2 on each Grubber pool, 3
    ///    on each Maw pool, 4 on each Mox pool.
    ///  - Level TaRapedia's. The items the lowest Uncommon templates of the bands that hold those levels: "Cryogenic RPG
    ///    Launcher" read as the Ice grenade launcher (no rocket launcher is Ice), "Incendiary" as Fire, the Hellstrom Pistol
    ///    as the physical one; one to choose.
    /// </summary>
    public static class MarshesWhatsThatGasV1
    {
        public const uint MissionId = 539;
        public const uint Level = 34;
        public const uint Geli = 510172;
        public const uint Ferns = 1, Hides = 2, Glands = 3, Hearts = 4;
        public const uint CallaFernsIndicator = 5;      // missionobjectiveindicatorlanguage 5 "Calla Ferns"

        public const uint FernTemplate = 657, FernClass = 7664, FernCount = 5;
        public const uint HideTemplate = 652, HideClass = 7659, HideCount = 10;
        public const uint GlandTemplate = 655, GlandClass = 7662, GlandCount = 5;
        public const uint HeartTemplate = 653, HeartClass = 7660, HeartCount = 10;
        public const string FernKey = "calla-fern-frond", HideKey = "swamp-grubber-hide", GlandKey = "maw-gland", HeartKey = "mox-heart";
        public const int FernChance = 50, HideChance = 100, GlandChance = 50, HeartChance = 50;

        public const uint SwampGrubber = MarshesPaludosNpcsV1.SwampGrubber, Mox = MarshesPaludosNpcsV1.Mox;
        public static readonly uint[] Maws = { 531052, 540012 };
        public const double PoolMarkerRadius = 20, FernMarkerRadius = 45;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint CryogenicLauncher = 47944;    // Weapon_Avatar_GrenadeLauncher_Ice_UNC_35_to_39
        public const uint IncendiaryLeechGun = 47728;   // Weapon_Avatar_LeechGun_Fire_UNC_34_to_38
        public const uint ElectricNetGun = 15513;       // Weapon_Avatar_NetGun_Electric_UNC_36_to_40
        public const uint Pistol = 14404;               // Weapon_Avatar_Pistol_Physical_UNC_35_to_39

        /// <summary>The middle of the two Grubber pools: the "Calla Ferns" marker.</summary>
        public static readonly (double X, double Y, double Z) FernsAt = (-120, 214.7, 20);

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "What's that Gas?", 1446, Geli, Geli, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicLauncher, IncendiaryLeechGun, ElectricNetGun, Pistol });

            mission.Objective(Ferns, 1542, 9361, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Hides, 1543, 9362, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Glands, 1544, 9363, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Hearts, 1545, 9364, 3, MissionObjectiveState.Incomplete);
            mission.Transition(Ferns);
            mission.Progress(Ferns, MissionProgressEventKind.ItemAcquired, FernClass, FernCount);
            mission.Action(Ferns, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Hides);
            mission.Progress(Hides, MissionProgressEventKind.ItemAcquired, HideClass, HideCount);
            mission.Transition(Glands);
            mission.Progress(Glands, MissionProgressEventKind.ItemAcquired, GlandClass, GlandCount);
            mission.Transition(Hearts);
            mission.Progress(Hearts, MissionProgressEventKind.ItemAcquired, HeartClass, HeartCount);

            mission.NamedIndicator(Ferns, CallaFernsIndicator, FernsAt.X, FernsAt.Y, FernsAt.Z, FernMarkerRadius);
            var marker = 1U;
            foreach (var (objective, creature) in new[] { (Hides, SwampGrubber), (Glands, Maws[0]), (Hearts, Mox) })
                foreach (var pool in MarshesPaludosNpcsV1.GasPools.Where(pool => pool.Creature == creature))
                    mission.Indicator(objective, marker++, pool.X, pool.Y, pool.Z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/539",
                "Native1 'Gather 5 Calla Fern Fronds' 1542/9361, 2 'Gather 10 Swamp Grubber Hides' 1543/9362, 3 'Collect 5 Maw " +
                "Glands' 1544/9363, 4 'Collect 10 Mox Hearts' 1545/9364, no counters; log 1447 says 5 Mox hearts. Texts 1446-1449, " +
                "9360. Indicator name5 'Calla Ferns'. Items class7664/template657, 7659/652, 7662/655, 7660/653. Name 3122 Shaman Geli.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/What%27s_that_Gas%3F",
                "Giver/receiver Shaman Gali (Paludos); level34; 65000XP, 5400 credits; 5 fronds, 10 hides, 5 glands, 10 hearts; one " +
                "of Teleract Cryogenic RPG Launcher35, ChiTech Incendiary Leech Gun34, Eclipse Electric Net Gun36, Hellstrom " +
                "Pistol35; fronds dropped by Swamp Grubbers. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesWhatsThatGasV1",
                "10 hearts (title over log). Personal drops on map1454: fronds 50% and hides 100% from Swamp Grubber 540014, glands " +
                "50% from Maw 531052/540012, hearts 50% from Mox 540013 (rates estimated); Geli keeps them. Six mission pools " +
                "760303-760308 east of Paludos (two of each). Markers on them, 1 under name5. Items 47944, 47728, 15513, 14404 " +
                "(lowest Uncommon of each kind and band), one to choose.", 0.3);

            var map = MarshesMissionDataV1.MapContextId;
            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Ferns, Hides, Glands, Hearts },
                Items = new()
                {
                    Drop(FernKey, FernTemplate, FernCount, new[] { SwampGrubber }, Ferns, FernChance, map),
                    Drop(HideKey, HideTemplate, HideCount, new[] { SwampGrubber }, Hides, HideChance, map),
                    Drop(GlandKey, GlandTemplate, GlandCount, Maws, Glands, GlandChance, map),
                    Drop(HeartKey, HeartTemplate, HeartCount, new[] { Mox }, Hearts, HeartChance, map)
                }
            });
        }

        private static MissionItemBinding Drop(string key, uint template, uint count, uint[] creatures, uint objective, int chance, uint map) =>
            new(key, template, MissionItemScope.CharacterOwned, count,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                MissionItemCleanupDisposition.Retain, TurnInQuantity: count,
                Drop: new MissionItemDropDefinition(creatures, objective, map, chance, 1));

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
