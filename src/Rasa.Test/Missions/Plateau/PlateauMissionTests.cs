using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Plateau
{
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Plateau missions (PlateauMissionDataV1): Colonel Bosley's Giant Killer (939,
    /// Plateau_giant_killer) and Incommunicado (1040, Plateau_incommunicado) with its three recon
    /// squads (PlateauReconSquadsV1); the commanders' Canyon Sweep (1045, Plateau_canyon_sweep, with
    /// the Maligo Elite Guards of PlateauMaligoGuardsV1 and the flags of PlateauIncommunicadoFlagsV1),
    /// Blueprint For Victory (1046, Plateau_blueprint_for_victory) and Toxic Detox (1053,
    /// Plateau_toxic_detox); the Scrounger's chain (1251, 1015, 1017); New Velon Village's Graveyard Shift
    /// (933, Plateau_graveyard_shift), Village Of The Damned (1981, Plateau_village_of_the_damned), The
    /// Book Of Velonar (930, Plateau_book_of_velonar) and Going Postal (1034, Plateau_going_postal);
    /// Wedge Rock's All Along The Watchtowers (1825, Plateau_watchtowers) and Cryptic Messages (557,
    /// Plateau_cryptic_messages); Fort Defiance's Soyuz Salvage: Plateau (2007, Plateau_soyuz_salvage), The
    /// Defiant Ones (887, Plateau_defiant_ones), A Bridge Too Far (713, Plateau_bridge_too_far), The Beer
    /// Hunter (855, Plateau_beer_hunter), Crystal Blue Persuasion (954, Plateau_crystal_blue_persuasion) and
    /// Alpha Mox (1027, Plateau_alpha_mox); Mount Reverence's A Voice From The Past (934,
    /// Plateau_voice_from_the_past) and Trinity Bridge Logos: Past (1044, Plateau_trinity_logos_past); Special
    /// Agent Perdu's Clean Slate (1983, Plateau_clean_slate), Mind Bender (905, Plateau_mind_bender), Photo Ops
    /// (929, Plateau_photo_ops), The Results Are In (936, Plateau_results_are_in) and Mind Slayer (719,
    /// Plateau_mind_slayer).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMissionTests
    {
        /// <summary>The Plateau missions the World migrations enable, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            PlateauGiantKillerV1.MissionId, PlateauIncommunicadoV1.MissionId, PlateauCanyonSweepV1.MissionId,
            PlateauBlueprintForVictoryV1.MissionId, PlateauToxicDetoxV1.MissionId, PlateauComicReliefV1.MissionId,
            PlateauScavengerHuntV1.MissionId, PlateauStalkMarketV1.MissionId, PlateauGraveyardShiftV1.MissionId,
            PlateauVillageOfTheDamnedV1.MissionId, PlateauBookOfVelonarV1.MissionId, PlateauGoingPostalV1.MissionId,
            PlateauWatchtowersV1.MissionId, PlateauCrypticMessagesV1.MissionId, PlateauSoyuzSalvageV1.MissionId,
            PlateauDefiantOnesV1.MissionId, PlateauBridgeTooFarV1.MissionId, PlateauBeerHunterV1.MissionId,
            PlateauCrystalBluePersuasionV1.MissionId, PlateauAlphaMoxV1.MissionId, PlateauVoiceFromThePastV1.MissionId,
            PlateauTrinityLogosPastV1.MissionId, PlateauCleanSlateV1.MissionId, PlateauMindBenderV1.MissionId,
            PlateauPhotoOpsV1.MissionId, PlateauResultsAreInV1.MissionId, PlateauMindSlayerV1.MissionId,
            PlateauBombVoyageV1.MissionId, PlateauLookoutDownBelowV1.MissionId, PlateauBloodhoundV1.MissionId,
            PlateauBlueFluV1.MissionId, PlateauMurderMostFoulV1.MissionId, PlateauLookingForCluesV1.MissionId,
            PlateauMysteryUnearthedV1.MissionId, PlateauSymbolismV1.MissionId, PlateauLogosVortexV1.MissionId,
            PlateauBehindEnemyLinesV1.MissionId, PlateauMislaidPlansV1.MissionId
        };

        /// <summary>
        /// Giant Killer's two indicators, Incommunicado's three, Canyon Sweep's one, Blueprint For Victory's two,
        /// Toxic Detox's one, Comic Relief's three, The Stalk Market's three; Scavenger Hunt has none.
        /// Graveyard Shift's two, Village Of The Damned's, The Book Of Velonar's and Going Postal's one each.
        /// All Along The Watchtowers', Cryptic Messages' and Soyuz Salvage's three each. The Beer Hunter's four;
        /// The Defiant Ones', A Bridge Too Far's, Crystal Blue Persuasion's and Alpha Mox's one each.
        /// A Voice From The Past's and Trinity Bridge Logos: Past's one each. Clean Slate's three, Mind Bender's
        /// eight, Photo Ops' five, The Results Are In's and Mind Slayer's one each. Bomb Voyage's two, Lookout Down
        /// Below's one, The Stainless Steel Bloodhound's two. Blue Flu's two, Murder Most Foul's seven, Looking For
        /// Clues' eight. Logos: Vortex's one; A Mystery Unearthed and Symbolism have none (their objectives are on other
        /// maps, and a marker carries no map). Behind Enemy Lines' and Mislaid Plans' one each.
        /// </summary>
        internal const int Indicators = 82;

        private const string Before = "20270123000000_Thunderhead_base_teleporters";
        private const string GiantKiller = "20270125000000_Plateau_giant_killer";
        private const string Incommunicado = "20270126000000_Plateau_incommunicado";
        private const string CanyonSweep = "20270127000000_Plateau_canyon_sweep";
        private const string BlueprintForVictory = "20270128000000_Plateau_blueprint_for_victory";
        private const string ToxicDetox = "20270129000000_Plateau_toxic_detox";
        private const string ComicRelief = "20270130000000_Plateau_comic_relief";
        private const string ScavengerHunt = "20270131000000_Plateau_scavenger_hunt";
        private const string StalkMarket = "20270201000000_Plateau_stalk_market";
        private const string GraveyardShift = "20270202000000_Plateau_graveyard_shift";
        private const string VillageOfTheDamned = "20270203000000_Plateau_village_of_the_damned";
        private const string BookOfVelonar = "20270204000000_Plateau_book_of_velonar";
        private const string GoingPostal = "20270205000000_Plateau_going_postal";
        private const string Watchtowers = "20270206000000_Plateau_watchtowers";
        private const string CrypticMessages = "20270207000000_Plateau_cryptic_messages";
        private const string SoyuzSalvage = "20270208000000_Plateau_soyuz_salvage";
        private const string DefiantOnes = "20270209000000_Plateau_defiant_ones";
        private const string BridgeTooFar = "20270210000000_Plateau_bridge_too_far";
        private const string BeerHunter = "20270211000000_Plateau_beer_hunter";
        private const string CrystalBluePersuasion = "20270212000000_Plateau_crystal_blue_persuasion";
        private const string AlphaMox = "20270213000000_Plateau_alpha_mox";
        private const string InfectedForeanAttack = "20270214000000_Plateau_infected_forean_attack";
        private const string VogrenNpcs = "20270215000000_Plateau_vogren_npcs";
        private const string VoiceFromThePast = "20270216000000_Plateau_voice_from_the_past";
        private const string TrinityLogosPast = "20270217000000_Plateau_trinity_logos_past";
        private const string PerduNpcs = "20270218000000_Plateau_perdu_npcs";
        private const string SurveillanceSentinels = "20270219000000_Plateau_surveillance_sentinels";
        private const string CleanSlate = "20270220000000_Plateau_clean_slate";
        private const string MindBender = "20270221000000_Plateau_mind_bender";
        private const string PhotoOps = "20270222000000_Plateau_photo_ops";
        private const string ResultsAreIn = "20270223000000_Plateau_results_are_in";
        private const string MindSlayer = "20270224000000_Plateau_mind_slayer";
        private const string WedgeRockNpcs = "20270225000000_Plateau_wedge_rock_npcs";
        private const string ChasmNpcs = "20270226000000_Plateau_chasm_npcs";
        private const string BombVoyage = "20270227000000_Plateau_bomb_voyage";
        private const string LookoutDownBelow = "20270228000000_Plateau_lookout_down_below";
        private const string Bloodhound = "20270301000000_Plateau_bloodhound";
        private const string CidNpcs = "20270302000000_Plateau_cid_npcs";
        private const string BlueFlu = "20270303000000_Plateau_blue_flu";
        private const string MurderMostFoul = "20270304000000_Plateau_murder_most_foul";
        private const string LookingForClues = "20270305000000_Plateau_looking_for_clues";
        private const string BaileyNpcs = "20270306000000_Plateau_bailey_npcs";
        private const string MysteryUnearthed = "20270307000000_Plateau_mystery_unearthed";
        private const string Symbolism = "20270308000000_Plateau_symbolism";
        private const string LogosVortex = "20270309000000_Plateau_logos_vortex";
        private const string BehindEnemyLines = "20270310000000_Plateau_behind_enemy_lines";

        [TestMethod]
        public void TheMissionsAreEnabledAndOperationalAtTheirGiversAndReceiversAndLevels()
        {
            var expected = new (uint Mission, uint? Giver, uint? Receiver)[]
            {
                (PlateauGiantKillerV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauGiantKillerV1.Bosley),
                (PlateauIncommunicadoV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauGiantKillerV1.Bosley),
                (PlateauCanyonSweepV1.MissionId, PlateauReconSquadsV1.Carvelle, PlateauReconSquadsV1.Carvelle),
                (PlateauBlueprintForVictoryV1.MissionId, PlateauReconSquadsV1.Petrie, PlateauGiantKillerV1.Bosley),
                (PlateauToxicDetoxV1.MissionId, PlateauReconSquadsV1.Locke, PlateauReconSquadsV1.Locke),
                (PlateauComicReliefV1.MissionId, PlateauScroungerNpcsV1.OBrien, PlateauScroungerNpcsV1.OBrien),
                (PlateauScavengerHuntV1.MissionId, PlateauScroungerNpcsV1.Scarpelo, PlateauScroungerNpcsV1.Scarpelo),
                (PlateauStalkMarketV1.MissionId, PlateauScroungerNpcsV1.Scarpelo, PlateauScroungerNpcsV1.Scarpelo),
                (PlateauGraveyardShiftV1.MissionId, PlateauGraveyardShiftV1.Talisen, PlateauGraveyardShiftV1.Talisen),
                (PlateauVillageOfTheDamnedV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauBookOfVelonarV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauGoingPostalV1.MissionId, PlateauGoingPostalV1.Kaely, PlateauGoingPostalV1.Kaely),
                (PlateauWatchtowersV1.MissionId, PlateauWatchtowersV1.Thibodeau, PlateauWatchtowersV1.Thibodeau),
                (PlateauCrypticMessagesV1.MissionId, PlateauCrypticMessagesV1.Devinchy, null),
                (PlateauSoyuzSalvageV1.MissionId, PlateauSoyuzSalvageV1.Grissom, PlateauSoyuzSalvageV1.Grissom),
                (PlateauDefiantOnesV1.MissionId, PlateauFortDefianceNpcsV1.Mamede, PlateauFortDefianceNpcsV1.Peterson),
                (PlateauBridgeTooFarV1.MissionId, PlateauFortDefianceNpcsV1.Peterson, PlateauFortDefianceNpcsV1.Peterson),
                (PlateauBeerHunterV1.MissionId, PlateauFortDefianceNpcsV1.Peterson, PlateauFortDefianceNpcsV1.Peterson),
                (PlateauCrystalBluePersuasionV1.MissionId, PlateauFortDefianceNpcsV1.Mauer, PlateauFortDefianceNpcsV1.Mauer),
                (PlateauAlphaMoxV1.MissionId, PlateauFortDefianceNpcsV1.Mauer, PlateauFortDefianceNpcsV1.Mauer),
                (PlateauVoiceFromThePastV1.MissionId, PlateauVogrenNpcsV1.Tendahi, PlateauVogrenNpcsV1.Hologram),
                (PlateauTrinityLogosPastV1.MissionId, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (PlateauCleanSlateV1.MissionId, PlateauPerduNpcsV1.Wainwright, PlateauPerduNpcsV1.Perdu),
                (PlateauMindBenderV1.MissionId, PlateauPerduNpcsV1.Perdu, PlateauPerduNpcsV1.Perdu),
                (PlateauPhotoOpsV1.MissionId, PlateauPerduNpcsV1.Russ, PlateauPerduNpcsV1.Clark),
                (PlateauResultsAreInV1.MissionId, PlateauPerduNpcsV1.Clark, PlateauPerduNpcsV1.Perdu),
                (PlateauMindSlayerV1.MissionId, PlateauPerduNpcsV1.Perdu, PlateauPerduNpcsV1.Perdu),
                (PlateauBombVoyageV1.MissionId, PlateauPerduNpcsV1.Russ, PlateauPerduNpcsV1.Russ),
                (PlateauLookoutDownBelowV1.MissionId, PlateauPerduNpcsV1.Russ, PlateauBloodhoundV1.Garde),
                (PlateauBloodhoundV1.MissionId, PlateauBloodhoundV1.Garde, null),
                (PlateauBlueFluV1.MissionId, PlateauCidNpcsV1.Parkman, PlateauCidNpcsV1.Norton),
                (PlateauMurderMostFoulV1.MissionId, PlateauCidNpcsV1.Norton, PlateauCidNpcsV1.Crais),
                (PlateauLookingForCluesV1.MissionId, PlateauCidNpcsV1.Crais, PlateauCidNpcsV1.Crais),
                (PlateauMysteryUnearthedV1.MissionId, PlateauMysteryUnearthedV1.Topper, PlateauBaileyNpcsV1.Bailey),
                (PlateauSymbolismV1.MissionId, PlateauBaileyNpcsV1.Bailey, PlateauMysteryUnearthedV1.Topper),
                (PlateauLogosVortexV1.MissionId, PlateauBaileyNpcsV1.Ridout, PlateauBaileyNpcsV1.Ridout),
                (PlateauBehindEnemyLinesV1.MissionId, null, PlateauGiantKillerV1.Bosley),
                (PlateauMislaidPlansV1.MissionId, PlateauBaileyNpcsV1.Bailey, PlateauBaileyNpcsV1.Bailey)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(mission switch { 1251 or 1015 or 1017 or 1027 or 1044 => 31U, 930 or 1825 or 557 or 934 or 905 or 929 or 936 or 719 => 32U, 2007 => 34U, 2016 or 2017 => 50U, _ => 30U },
                    entry.Level,
                    $"{mission} level");
                Assert.AreEqual<uint?>(PlateauMissionDataV1.CategoryId, entry.CategoryId);
            }

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The client's own texts, as WildernessContentInventoryTests checks the Wilderness ones: the
        /// ids are written out here from the client tables (missionconversation, missionobjective),
        /// not taken from the data classes, so a wrong id in those fails. Only Canyon Sweep, Toxic
        /// Detox, Graveyard Shift and Village Of The Damned have a counter text in the client.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (939, 5104, new (uint, uint, uint, uint?)[] { (1, 5108, 6354, null), (2, 5109, 6355, null) }),
                (1040, 5823, new (uint, uint, uint, uint?)[] { (1, 5832, 6368, null), (2, 5833, 6369, null), (3, 5834, 6370, null) }),
                (1045, 5875, new (uint, uint, uint, uint?)[] { (1, 5879, 6333, 5880) }),
                (1046, 5881, new (uint, uint, uint, uint?)[] { (1, 5922, 6311, null), (3, 6024, 6312, null) }),
                (1053, 5916, new (uint, uint, uint, uint?)[] { (1, 5920, 6474, 5921) }),
                (1251, 10008, new (uint, uint, uint, uint?)[]
                {
                    (1, 10015, 10016, null), (7, 10033, 10034, null), (8, 10013, 10014, null), (9, 10018, 10019, null),
                    (10, 10020, 10021, null)
                }),
                (1015, 5555, new (uint, uint, uint, uint?)[] { (3, 5563, 6445, null), (4, 5564, 6446, null) }),
                (1017, 5587, new (uint, uint, uint, uint?)[] { (1, 5605, 6458, null), (2, 5606, 6459, null), (3, 5607, 6460, null) }),
                (933, 5016, new (uint, uint, uint, uint?)[] { (1, 5021, 6365, 5022), (2, 5023, 6366, 5024) }),
                (1981, 20858, new (uint, uint, uint, uint?)[] { (1, 20863, 20864, 20865) }),
                (930, 4983, new (uint, uint, uint, uint?)[] { (1, 4987, 6330, null) }),
                (1034, 5776, new (uint, uint, uint, uint?)[] { (1, 5785, 6363, null), (2, 8093, 8094, null) }),
                (1825, 18540, new (uint, uint, uint, uint?)[] { (1, 18545, 18546, null), (2, 18548, 18549, null), (3, 18551, 18552, null) }),
                (557, 1580, new (uint, uint, uint, uint?)[] { (1, 3415, 6350, null), (2, 3419, 6351, null), (3, 3420, 6352, null) }),
                (2007, 21579, new (uint, uint, uint, uint?)[] { (1, 21604, 21605, null) }),
                (887, 4642, new (uint, uint, uint, uint?)[] { (1, 6968, 6969, null) }),
                (713, 3365, new (uint, uint, uint, uint?)[] { (1, 4508, 6287, null) }),
                (855, 4429, new (uint, uint, uint, uint?)[]
                {
                    (1, 4433, 6294, null), (2, 4435, 6295, null), (3, 4436, 6296, null), (4, 4437, 6297, null),
                    (5, 4435, 6295, null)
                }),
                (954, 5182, new (uint, uint, uint, uint?)[] { (1, 5205, 6345, null) }),
                (1027, 5683, new (uint, uint, uint, uint?)[] { (1, 5687, 6291, null) }),
                (934, 5026, new (uint, uint, uint, uint?)[] { (1, 5053, 6480, null) }),
                (1044, 5870, new (uint, uint, uint, uint?)[] { (1, 5874, 6380, null) }),
                (1983, 20892, new (uint, uint, uint, uint?)[] { (1, 20897, 20898, null), (4, 20904, 20905, null), (5, 20921, 20922, null) }),
                (905, 4804, new (uint, uint, uint, uint?)[]
                {
                    (11, 8233, 8234, null), (12, 8240, 8241, null), (13, 8243, 8244, null), (14, 8246, 8247, null),
                    (3, 4810, 6397, null), (8, 4812, 6398, null), (9, 4814, 6399, null), (10, 4816, 6400, null)
                }),
                (929, 4978, new (uint, uint, uint, uint?)[]
                {
                    (5, 4993, 6428, null), (6, 5729, 6429, null), (7, 5730, 6430, null), (8, 5731, 6431, null), (9, 5732, 6432, null)
                }),
                (936, 5037, new (uint, uint, uint, uint?)[] { (1, 5772, 6434, null) }),
                (719, 3438, new (uint, uint, uint, uint?)[] { (1, 5774, 6405, null) }),
                (931, 4998, new (uint, uint, uint, uint?)[] { (2, 5007, 6314, 5008), (8, 5014, 6321, null) }),
                (970, 5305, new (uint, uint, uint, uint?)[] { (1, 5610, 6394, null) }),
                (867, 4534, new (uint, uint, uint, uint?)[] { (3, 4539, 6471, null), (2, 4540, 6472, null) }),
                (1014, 5547, new (uint, uint, uint, uint?)[] { (2, 5551, 6308, null), (1, 5553, 6309, null) }),
                (895, 4698, new (uint, uint, uint, uint?)[]
                {
                    (1, 4722, 6420, null), (2, 4723, 6421, null), (3, 4724, 6422, null), (4, 4725, 6426, null),
                    (5, 4729, 6423, null), (6, 4730, 6425, null), (7, 4734, 6424, null)
                }),
                (928, 4974, new (uint, uint, uint, uint?)[]
                {
                    (1, 5070, 6386, null), (4, 5075, 6387, null), (6, 5081, 6384, null), (5, 5083, 6385, null),
                    (2, 5071, 6388, null), (8, 5090, 6389, null), (9, 5091, 6390, null), (10, 5092, 6391, null),
                    (11, 5100, 6392, null)
                }),
                (2016, 21909, new (uint, uint, uint, uint?)[] { (1, 21914, 21915, null) }),
                (2017, 21925, new (uint, uint, uint, uint?)[]
                {
                    (1, 21930, 21931, null), (2, 21933, 21934, null), (4, 21938, 21939, null), (6, 21943, 21944, null)
                }),
                (1659, 15972, new (uint, uint, uint, uint?)[] { (18, 15977, 15978, null) }),
                (555, 1548, new (uint, uint, uint, uint?)[] { (1, 3388, 6303, null) }),
                (965, 5269, new (uint, uint, uint, uint?)[] { (1, 5277, 6412, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
                CollectionAssert.AreEquivalent(objectives.Select(row => row.Objective).ToArray(),
                    actual.Select(row => row.ObjectiveId).ToArray(), $"{mission} objectives");
                foreach (var (objective, name, body, counter) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.AreEqual(counter, row.ClientCounter0TextId, $"{mission}.{objective} counter0");
                    Assert.IsNull(row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }
        }

        /// <summary>
        /// Incommunicado follows Giant Killer; each commander's mission follows his own delivery: the
        /// flag that Incommunicado's delivery objective sets (action 3, SetPlayerFlag to 1). Scavenger
        /// Hunt follows the Scrounger's talk in Comic Relief (objective 10, action 1, the same way);
        /// The Stalk Market follows Scavenger Hunt. New Velon Village's four, Wedge Rock's two, Soyuz Salvage
        /// and The Defiant Ones have no requirement; Fort Defiance's others follow The Defiant Ones: A Bridge Too
        /// Far, then The Beer Hunter; Crystal Blue Persuasion, then Alpha Mox. A Voice From The Past has none;
        /// Trinity Bridge Logos: Past follows it. Clean Slate has none; Perdu's line follows it: Mind Bender, Photo
        /// Ops, The Results Are In, Mind Slayer. Bomb Voyage and Lookout Down Below have none; The Stainless Steel
        /// Bloodhound follows Lookout Down Below. Fort Defiance's murder case: Blue Flu follows The Defiant Ones, Murder
        /// Most Foul Blue Flu, Looking For Clues Murder Most Foul. A Mystery Unearthed and Logos: Vortex have none;
        /// Symbolism follows A Mystery Unearthed. Behind Enemy Lines follows The Stainless Steel Bloodhound; Mislaid
        /// Plans has none.
        /// </summary>
        [TestMethod]
        public void EachMissionRequiresTheMissionOrDeliveryBeforeIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(21, prerequisites);
            var incommunicado = prerequisites.Single(entry => entry.MissionId == PlateauIncommunicadoV1.MissionId);
            Assert.AreEqual(MissionPrerequisiteKind.MissionCompleted, incommunicado.Kind);
            Assert.AreEqual<uint?>(PlateauGiantKillerV1.MissionId, incommunicado.RequiredMissionId);

            var deliveries = harness.World.MissionActionEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision &&
                    entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.Kind == MissionActionKind.SetPlayerFlag)
                .ToArray();
            foreach (var (mission, objective, flag) in new (uint, uint, uint)[]
            {
                (1045, 1, 1040001), (1046, 2, 1040002), (1053, 3, 1040003)
            })
            {
                var prerequisite = prerequisites.Single(entry => entry.MissionId == mission);
                Assert.AreEqual(MissionPrerequisiteKind.PlayerFlagValue, prerequisite.Kind, $"{mission}");
                Assert.AreEqual<uint?>(flag, prerequisite.PlayerFlagId, $"{mission} flag");
                Assert.AreEqual<uint?>(1, prerequisite.PlayerFlagValue, $"{mission} flag value");
                var action = deliveries.Single(entry => entry.ObjectiveId == objective);
                Assert.AreEqual((3U, (uint?)flag, (uint?)1), (action.ActionId, action.PlayerFlagId, action.PlayerFlagValue),
                    $"Incommunicado delivery {objective}");
            }
            Assert.HasCount(3, deliveries);

            var scavengerHunt = prerequisites.Single(entry => entry.MissionId == PlateauScavengerHuntV1.MissionId);
            Assert.AreEqual((MissionPrerequisiteKind.PlayerFlagValue, (uint?)1251001, (uint?)1),
                (scavengerHunt.Kind, scavengerHunt.PlayerFlagId, scavengerHunt.PlayerFlagValue));
            var scrounger = harness.World.MissionActionEntries.AsNoTracking().Single(entry =>
                entry.ContentRevision == PlateauMissionDataV1.Revision && entry.MissionId == PlateauComicReliefV1.MissionId &&
                entry.Kind == MissionActionKind.SetPlayerFlag);
            Assert.AreEqual((10U, 1U, (uint?)1251001, (uint?)1),
                (scrounger.ObjectiveId, scrounger.ActionId, scrounger.PlayerFlagId, scrounger.PlayerFlagValue));
            var stalkMarket = prerequisites.Single(entry => entry.MissionId == PlateauStalkMarketV1.MissionId);
            Assert.AreEqual((MissionPrerequisiteKind.MissionCompleted, (uint?)1015),
                (stalkMarket.Kind, stalkMarket.RequiredMissionId));
            foreach (var mission in new uint[] { 933, 1981, 930, 1034, 1825, 557, 2007, 887, 934, 1983, 931, 970, 2016, 1659, 965 })
                Assert.IsFalse(prerequisites.Any(entry => entry.MissionId == mission), $"{mission}");
            foreach (var (mission, required) in new (uint, uint)[] { (713, 887), (954, 887), (855, 713), (1027, 954), (1044, 934), (905, 1983), (929, 905), (936, 929), (719, 936), (867, 970), (1014, 887), (895, 1014), (928, 895), (2017, 2016), (555, 867) })
            {
                var prerequisite = prerequisites.Single(entry => entry.MissionId == mission);
                Assert.AreEqual((MissionPrerequisiteKind.MissionCompleted, (uint?)required),
                    (prerequisite.Kind, prerequisite.RequiredMissionId), $"{mission}");
            }
        }

        /// <summary>
        /// The missions' sources and how far each is trusted: the client's tables 1.0, TaRapedia 0.5
        /// (0.4 for Blueprint For Victory, whose page gives no drop rate, and for Scavenger Hunt and Going
        /// Postal, Cryptic Messages and Soyuz Salvage, stub pages), and what this repository
        /// reconstructed 0.3 (the guards, the drop rates, the pumps, the NPCs' looks and Lonigan's place,
        /// the Stalkers, the miasmas, the Infected Foreans, the book's place, Porthus's place, the
        /// towers, Vorvaak's drop, Fort Defiance's NPCs, the Juggernaut, the plants, the Mox Lair) - 0.2 for Soyuz
        /// Salvage, whose places, XP and credits no source gives. The Defiant Ones', The Beer Hunter's and
        /// Crystal Blue Persuasion's pages are stubs (0.4), as are A Voice From The Past's, Mind Bender's and
        /// Mind Slayer's.
        /// </summary>
        [TestMethod]
        public void TheCommandersMissionsTrustWhatIsReconstructedLeast()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            foreach (var (mission, confidences) in new (uint, (MissionEvidenceSourceKind, double)[])[]
            {
                (1045, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1046, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1053, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1251, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1015, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1017, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (933, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1981, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (930, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1034, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1825, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (557, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (2007, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.2) }),
                (887, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (713, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (855, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (954, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1027, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (934, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1044, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1983, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (905, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (929, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (936, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (719, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (931, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (970, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (867, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1014, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (895, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (928, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.2) }),
                (2016, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (2017, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1659, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (555, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (965, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) })
            })
                CollectionAssert.AreEqual(confidences, evidence.Where(entry => entry.MissionId == mission)
                    .OrderBy(entry => entry.EvidenceId).Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(),
                    $"{mission} evidence");
        }

        [TestMethod]
        public void IndicatorsCarryNoOtherMissionsClientName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            foreach (var row in indicators)
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                    $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
        }

        [TestMethod]
        public void TheReconSquadsAndGoliathsGuardsStandAndRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Squads(string table) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {PlateauReconSquadsV1.First} AND {PlateauReconSquadsV1.Last}";
            (uint Creature, byte Min, byte Max) Guards()
            {
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == PlateauGiantKillerV1.GoliathPool);
                return (pool.Creature2Id, pool.Creature2MinCount, pool.Creature2MaxCount);
            }

            Assert.AreEqual(11, Count(Squads("creature")));
            Assert.AreEqual(11, Count(Squads("spawnpool")));
            Assert.AreEqual(3, Count(Squads("npc_package")));
            Assert.AreEqual(((uint)PlateauGiantKillerV1.KaelGuard, (byte)PlateauGiantKillerV1.GuardsMin,
                (byte)PlateauGiantKillerV1.GuardsMax), Guards());

            world.GetService<IMigrator>().Migrate(GiantKiller);
            Assert.AreEqual(0, Count(Squads("creature")));
            Assert.AreEqual(0, Count(Squads("creature_appearance")));
            Assert.AreEqual(0, Count(Squads("spawnpool")));
            Assert.AreEqual(0, Count(Squads("npc_package")));
            Assert.IsFalse(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == PlateauIncommunicadoV1.MissionId));

            world.GetService<IMigrator>().Migrate(Before);
            Assert.AreEqual((0U, (byte)0, (byte)0), Guards());
            Assert.IsFalse(world.MissionContentDefinitionEntries.Any(entry => entry.ContentRevision == PlateauMissionDataV1.Revision));
        }

        /// <summary>
        /// Each migration takes out only its own rows: Toxic Detox and Blueprint For Victory their
        /// missions; Canyon Sweep its mission, the Maligo Elite Guards and guardmasters (creatures,
        /// stats, actions, six pools) and Incommunicado's flag actions, which leaves Incommunicado.
        /// </summary>
        [TestMethod]
        public void TheMaligoGuardsTheFlagsAndTheCommandersMissionsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var creatures = $"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({PlateauMaligoGuardsV1.Guard}, {PlateauMaligoGuardsV1.Guardmaster})";
            var stats = $"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({PlateauMaligoGuardsV1.Guard}, {PlateauMaligoGuardsV1.Guardmaster})";
            var actions = $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {PlateauMaligoGuardsV1.FirstAction} AND {PlateauMaligoGuardsV1.LastAction}";
            var pools = $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN {PlateauMaligoGuardsV1.FirstPool} AND {PlateauMaligoGuardsV1.LastPool} " +
                $"AND map_context_id = {PlateauMissionDataV1.MapContextId}";
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Flags() => world.MissionActionEntries.AsNoTracking().Count(entry =>
                entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.Kind == MissionActionKind.SetPlayerFlag);

            Assert.AreEqual(2, Count(creatures));
            Assert.AreEqual(2, Count(stats));
            Assert.AreEqual(7, Count(actions));
            Assert.AreEqual(6, Count(pools));
            Assert.AreEqual(3, Flags());

            world.GetService<IMigrator>().Migrate(BlueprintForVictory);
            Assert.IsFalse(Has(PlateauToxicDetoxV1.MissionId));
            Assert.IsTrue(Has(PlateauBlueprintForVictoryV1.MissionId));

            world.GetService<IMigrator>().Migrate(CanyonSweep);
            Assert.IsFalse(Has(PlateauBlueprintForVictoryV1.MissionId));
            Assert.IsTrue(Has(PlateauCanyonSweepV1.MissionId));
            Assert.AreEqual(6, Count(pools), "Canyon Sweep's guards stay with it");

            world.GetService<IMigrator>().Migrate(Incommunicado);
            Assert.IsFalse(Has(PlateauCanyonSweepV1.MissionId));
            Assert.AreEqual(0, Count(creatures));
            Assert.AreEqual(0, Count(stats));
            Assert.AreEqual(0, Count(actions));
            Assert.AreEqual(0, Count(pools));
            Assert.AreEqual(0, Flags());
            Assert.IsTrue(Has(PlateauIncommunicadoV1.MissionId));
            Assert.AreEqual(2, world.MissionActionEntries.AsNoTracking().Count(entry =>
                entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.ObjectiveId == 1), "Incommunicado's own actions stay");
        }

        /// <summary>
        /// The Scrounger's chain rolls back a migration at a time: The Stalk Market with its Stalker
        /// (creature, stats, three actions, four pools); Scavenger Hunt; Comic Relief with O'Brien,
        /// Lonigan and the Scrounger (creatures, looks, pools, packages 999 and 1000). Toxic Detox stays.
        /// </summary>
        [TestMethod]
        public void TheScroungersChainItsNpcsAndTheStalkersRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            var npcs = Ids("creature", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var looks = Ids("creature_appearance", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var npcPools = Ids("spawnpool", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var packages = $"SELECT COUNT(*) AS Value FROM npc_package WHERE package_id IN (999, 1000)";
            var stalker = Ids("creature", PlateauBaneStalkersV1.Stalker, PlateauBaneStalkersV1.Stalker);
            var stalkerStat = Ids("creature_stat", PlateauBaneStalkersV1.Stalker, PlateauBaneStalkersV1.Stalker);
            var stalkerActions = Ids("creature_action", PlateauBaneStalkersV1.FirstAction, PlateauBaneStalkersV1.LastAction);
            var stalkerPools = Ids("spawnpool", PlateauBaneStalkersV1.FirstPool, PlateauBaneStalkersV1.LastPool) +
                $" AND map_context_id = {PlateauMissionDataV1.MapContextId} AND creature_1_Id = {PlateauBaneStalkersV1.Stalker}";
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((3, 19, 3, 2), (Count(npcs), Count(looks), Count(npcPools), Count(packages)));
            Assert.AreEqual((1, 1, 3, 4), (Count(stalker), Count(stalkerStat), Count(stalkerActions), Count(stalkerPools)));

            world.GetService<IMigrator>().Migrate(ScavengerHunt);
            Assert.IsFalse(Has(PlateauStalkMarketV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), (Count(stalker), Count(stalkerStat), Count(stalkerActions), Count(stalkerPools)));
            Assert.IsTrue(Has(PlateauScavengerHuntV1.MissionId));

            world.GetService<IMigrator>().Migrate(ComicRelief);
            Assert.IsFalse(Has(PlateauScavengerHuntV1.MissionId));
            Assert.IsTrue(Has(PlateauComicReliefV1.MissionId));
            Assert.AreEqual(3, Count(npcs), "the NPCs stay with Comic Relief");

            world.GetService<IMigrator>().Migrate(ToxicDetox);
            Assert.IsFalse(Has(PlateauComicReliefV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), (Count(npcs), Count(looks), Count(npcPools), Count(packages)));
            Assert.IsTrue(Has(PlateauToxicDetoxV1.MissionId));
        }

        /// <summary>
        /// New Velon Village's missions roll back a migration at a time: Going Postal puts Porthus back on
        /// the chasm's floor; The Book Of Velonar; Village Of The Damned with its Infected Forean
        /// (creature, stats, look, action, four pools); Graveyard Shift with the Tomb Miasma and the Bull
        /// (creatures, stats, four actions, five pools). The Stalk Market stays.
        /// </summary>
        [TestMethod]
        public void NewVelonVillagesMissionsTheirCreaturesAndPorthusRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            var miasmas = (Ids("creature", PlateauTombMiasmasV1.TombMiasma, PlateauTombMiasmasV1.Bull),
                Ids("creature_stat", PlateauTombMiasmasV1.TombMiasma, PlateauTombMiasmasV1.Bull),
                Ids("creature_action", PlateauTombMiasmasV1.FirstAction, PlateauTombMiasmasV1.LastAction),
                Ids("spawnpool", PlateauTombMiasmasV1.FirstPool, PlateauTombMiasmasV1.LastPool) +
                    $" AND map_context_id = {PlateauMissionDataV1.MapContextId}");
            var infected = (Ids("creature", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_stat", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_appearance", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_action", PlateauInfectedForeansV1.Weapon, PlateauInfectedForeansV1.Weapon),
                Ids("spawnpool", PlateauInfectedForeansV1.FirstPool, PlateauInfectedForeansV1.LastPool) +
                    $" AND creature_1_Id = {PlateauInfectedForeansV1.Infected}");
            (int, int, int, int) Miasmas() => (Count(miasmas.Item1), Count(miasmas.Item2), Count(miasmas.Item3), Count(miasmas.Item4));
            (int, int, int, int, int) Infected() => (Count(infected.Item1), Count(infected.Item2), Count(infected.Item3),
                Count(infected.Item4), Count(infected.Item5));
            (double, double, double) Porthus()
            {
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == PlateauGoingPostalV1.Porthus);
                return (Math.Round(pool.PosX, 2), Math.Round(pool.PosY, 2), Math.Round(pool.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((2, 2, 4, 5), Miasmas());
            Assert.AreEqual((1, 1, 1, 1, 4), Infected());
            Assert.AreEqual(PlateauGoingPostalV1.PorthusAt, Porthus());

            world.GetService<IMigrator>().Migrate(BookOfVelonar);
            Assert.IsFalse(Has(PlateauGoingPostalV1.MissionId));
            Assert.AreEqual(PlateauGoingPostalV1.PorthusBefore, Porthus());
            Assert.IsTrue(Has(PlateauBookOfVelonarV1.MissionId));

            world.GetService<IMigrator>().Migrate(VillageOfTheDamned);
            Assert.IsFalse(Has(PlateauBookOfVelonarV1.MissionId));
            Assert.IsTrue(Has(PlateauVillageOfTheDamnedV1.MissionId));
            Assert.AreEqual((1, 1, 1, 1, 4), Infected(), "the Infected Foreans stay with Village Of The Damned");

            world.GetService<IMigrator>().Migrate(GraveyardShift);
            Assert.IsFalse(Has(PlateauVillageOfTheDamnedV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0, 0), Infected());
            Assert.IsTrue(Has(PlateauGraveyardShiftV1.MissionId));

            world.GetService<IMigrator>().Migrate(StalkMarket);
            Assert.IsFalse(Has(PlateauGraveyardShiftV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), Miasmas());
            Assert.IsTrue(Has(PlateauStalkMarketV1.MissionId));
        }

        /// <summary>
        /// Wedge Rock's missions and Soyuz Salvage roll back a migration at a time: Soyuz Salvage; Cryptic
        /// Messages with its radio channel; All Along The Watchtowers. Going Postal stays.
        /// </summary>
        [TestMethod]
        public void WedgeRocksMissionsAndSoyuzSalvageRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Channels() => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM mission_channel_policy WHERE mission_id = {PlateauCrypticMessagesV1.MissionId} " +
                $"AND content_revision = '{PlateauMissionDataV1.Revision}'").AsEnumerable().Single();

            Assert.AreEqual(1, Channels());
            Assert.IsTrue(world.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == PlateauCrypticMessagesV1.MissionId).RadioCompleteable);

            world.GetService<IMigrator>().Migrate(CrypticMessages);
            Assert.IsFalse(Has(PlateauSoyuzSalvageV1.MissionId));
            Assert.IsTrue(Has(PlateauCrypticMessagesV1.MissionId));

            world.GetService<IMigrator>().Migrate(Watchtowers);
            Assert.IsFalse(Has(PlateauCrypticMessagesV1.MissionId));
            Assert.AreEqual(0, Channels());
            Assert.IsTrue(Has(PlateauWatchtowersV1.MissionId));

            world.GetService<IMigrator>().Migrate(GoingPostal);
            Assert.IsFalse(Has(PlateauWatchtowersV1.MissionId));
            Assert.IsTrue(Has(PlateauGoingPostalV1.MissionId));
        }

        /// <summary>
        /// The Infected Forean's lightning goes back to the goo gun's shot. Fort Defiance's missions roll back
        /// a migration at a time: Alpha Mox; Crystal Blue Persuasion with
        /// the Mox Lair's pools, and the Alpha Male back on the cliff; The Beer Hunter; A Bridge Too Far with
        /// the Prototype Juggernaut (creature, stats, action, pool); The Defiant Ones with Mamede (creature,
        /// look, pool), and Peterson, Mauer and McLaughlin back at the dropship. Soyuz Salvage stays.
        /// </summary>
        [TestMethod]
        public void FortDefiancesMissionsTheirCreaturesAndNpcsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            int Lair() => Count(Ids("spawnpool", PlateauMoxLairV1.FirstPool, PlateauMoxLairV1.LastPool) +
                $" AND creature_1_Id = {PlateauMoxLairV1.Mox}");
            (int, int, int, int) Juggernaut() => (
                Count(Ids("creature", PlateauPrototypeJuggernautV1.Juggernaut, PlateauPrototypeJuggernautV1.Juggernaut)),
                Count(Ids("creature_stat", PlateauPrototypeJuggernautV1.Juggernaut, PlateauPrototypeJuggernautV1.Juggernaut)),
                Count(Ids("creature_action", PlateauPrototypeJuggernautV1.Guns, PlateauPrototypeJuggernautV1.Guns)),
                Count(Ids("spawnpool", PlateauPrototypeJuggernautV1.Pool, PlateauPrototypeJuggernautV1.Pool)));
            (int, int, int) Mamede() => (
                Count(Ids("creature", PlateauFortDefianceNpcsV1.Mamede, PlateauFortDefianceNpcsV1.Mamede)),
                Count(Ids("creature_appearance", PlateauFortDefianceNpcsV1.Mamede, PlateauFortDefianceNpcsV1.Mamede)),
                Count(Ids("spawnpool", PlateauFortDefianceNpcsV1.MamedePool, PlateauFortDefianceNpcsV1.MamedePool)));
            (double, double, double) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (Math.Round(row.PosX, 2), Math.Round(row.PosY, 2), Math.Round(row.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            (uint, uint, uint) Attack()
            {
                var row = world.CreatureActionEntries.AsNoTracking().Single(entry => entry.Id == PlateauInfectedForeansV1.Weapon);
                return (row.ActionId, row.ActionArgId, row.DamageType);
            }

            Assert.AreEqual((203U, 1U, 13U), Attack(), "the Infected Forean's lightning");
            world.GetService<IMigrator>().Migrate(AlphaMox);
            Assert.AreEqual((1U, 116U, 1U), Attack(), "the goo gun's shot, as it was");
            Assert.IsTrue(Has(PlateauAlphaMoxV1.MissionId));

            Assert.AreEqual(3, Lair());
            Assert.AreEqual(PlateauMoxLairV1.AlphaAt, At(PlateauMoxLairV1.AlphaMale));
            Assert.AreEqual((1, 1, 1, 1), Juggernaut());
            Assert.AreEqual((1, 6, 1), Mamede());
            foreach (var move in PlateauFortDefianceNpcsV1.Moves)
                Assert.AreEqual(move.At, At(move.Pool), $"{move.Pool}");

            world.GetService<IMigrator>().Migrate(CrystalBluePersuasion);
            Assert.IsFalse(Has(PlateauAlphaMoxV1.MissionId));
            Assert.IsTrue(Has(PlateauCrystalBluePersuasionV1.MissionId));

            world.GetService<IMigrator>().Migrate(BeerHunter);
            Assert.IsFalse(Has(PlateauCrystalBluePersuasionV1.MissionId));
            Assert.AreEqual(0, Lair());
            Assert.AreEqual(PlateauMoxLairV1.AlphaBefore, At(PlateauMoxLairV1.AlphaMale));
            Assert.IsTrue(Has(PlateauBeerHunterV1.MissionId));

            world.GetService<IMigrator>().Migrate(BridgeTooFar);
            Assert.IsFalse(Has(PlateauBeerHunterV1.MissionId));
            Assert.AreEqual((1, 1, 1, 1), Juggernaut(), "the Juggernaut stays with A Bridge Too Far");

            world.GetService<IMigrator>().Migrate(DefiantOnes);
            Assert.IsFalse(Has(PlateauBridgeTooFarV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), Juggernaut());
            Assert.AreEqual((1, 6, 1), Mamede(), "Mamede stays with The Defiant Ones");

            world.GetService<IMigrator>().Migrate(SoyuzSalvage);
            Assert.IsFalse(Has(PlateauDefiantOnesV1.MissionId));
            Assert.AreEqual((0, 0, 0), Mamede());
            foreach (var move in PlateauFortDefianceNpcsV1.Moves)
                Assert.AreEqual(move.Before, At(move.Pool), $"{move.Pool} back at the dropship");
            Assert.IsTrue(Has(PlateauSoyuzSalvageV1.MissionId));
        }

        /// <summary>
        /// Mount Reverence's missions roll back a migration at a time: Trinity Bridge Logos: Past; A Voice From
        /// The Past; then Tendahi and the hologram (creature, look, name, package, pool). Fort Defiance's stay.
        /// </summary>
        [TestMethod]
        public void MountReverencesMissionsAndNpcsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({PlateauVogrenNpcsV1.Tendahi}, {PlateauVogrenNpcsV1.Hologram})";
            (int, int, int, int, int) Npcs() => (Count(Ids("creature")), Count(Ids("creature_appearance")),
                Count(Ids("creature_actor_name")), Count(Ids("npc_package")),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN " +
                    $"({PlateauVogrenNpcsV1.TendahiPool}, {PlateauVogrenNpcsV1.HologramPool})"));
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((2, 1, 2, 1, 2), Npcs());
            Assert.AreEqual(PlateauVogrenNpcsV1.HologramPackage, world.NpcPackageEntries.AsNoTracking()
                .Single(entry => entry.Id == PlateauVogrenNpcsV1.Hologram).PackageId);

            world.GetService<IMigrator>().Migrate(VoiceFromThePast);
            Assert.IsFalse(Has(PlateauTrinityLogosPastV1.MissionId));
            Assert.IsTrue(Has(PlateauVoiceFromThePastV1.MissionId));

            world.GetService<IMigrator>().Migrate(VogrenNpcs);
            Assert.IsFalse(Has(PlateauVoiceFromThePastV1.MissionId));
            Assert.AreEqual((2, 1, 2, 1, 2), Npcs(), "the NPCs stay with their own migration");

            world.GetService<IMigrator>().Migrate(InfectedForeanAttack);
            Assert.AreEqual((0, 0, 0, 0, 0), Npcs());
            Assert.IsTrue(Has(PlateauAlphaMoxV1.MissionId));
        }

        /// <summary>
        /// Perdu's missions roll back a migration at a time: Mind Slayer; The Results Are In; Photo Ops; Mind
        /// Bender; Clean Slate; then the sentinels (creatures, stats, action, pools); then the NPCs (Wainwright,
        /// Russ, the four soldiers, Clean Slate's two templates, the pistol), and Perdu and Clark back where the
        /// NPC seed had them. Mount Reverence's stay.
        /// </summary>
        [TestMethod]
        public void PerdusMissionsTheirSentinelsAndNpcsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            var sentinels = PlateauSurveillanceSentinelsV1.Sentinels;
            (int, int, int, int) Sentinels() => (Count(Ids("creature", sentinels[0].Id, sentinels[^1].Id)),
                Count(Ids("creature_stat", sentinels[0].Id, sentinels[^1].Id)),
                Count(Ids("creature_action", PlateauSurveillanceSentinelsV1.Weapon, PlateauSurveillanceSentinelsV1.Weapon)),
                Count(Ids("spawnpool", sentinels[0].Pool, sentinels[^1].Pool)));
            (int, int, int, int, int, int) Npcs() => (
                Count(Ids("creature", PlateauPerduNpcsV1.Wainwright, PlateauPerduNpcsV1.Berserk)),
                Count(Ids("creature_appearance", PlateauPerduNpcsV1.Wainwright, PlateauPerduNpcsV1.Berserk)),
                Count(Ids("npc_package", PlateauPerduNpcsV1.Wainwright, PlateauPerduNpcsV1.Berserk)),
                Count(Ids("creature_actor_name", PlateauPerduNpcsV1.Wainwright, PlateauPerduNpcsV1.Berserk)),
                Count(Ids("spawnpool", PlateauPerduNpcsV1.WainwrightPool, PlateauPerduNpcsV1.Implanted[^1].Pool)),
                Count(Ids("creature_action", PlateauPerduNpcsV1.BerserkPistol, PlateauPerduNpcsV1.BerserkPistol)));
            (double, double, double) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (Math.Round(row.PosX, 2), Math.Round(row.PosY, 2), Math.Round(row.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            // Six humans with the uniform (6 rows each), the two soldiers' pistols; five packages (Russ and the
            // four); two actor names; six pools; the berserk pistol.
            var npcs = (8, 6 * 6 + 2, 5, 2, 6, 1);
            Assert.AreEqual(npcs, Npcs());
            Assert.AreEqual((5, 5, 1, 5), Sentinels());
            foreach (var move in PlateauPerduNpcsV1.Moves)
                Assert.AreEqual(move.At, At(move.Pool), $"{move.Pool}");

            foreach (var (target, gone, stays) in new (string, uint, uint)[]
            {
                (ResultsAreIn, PlateauMindSlayerV1.MissionId, PlateauResultsAreInV1.MissionId),
                (PhotoOps, PlateauResultsAreInV1.MissionId, PlateauPhotoOpsV1.MissionId),
                (MindBender, PlateauPhotoOpsV1.MissionId, PlateauMindBenderV1.MissionId),
                (CleanSlate, PlateauMindBenderV1.MissionId, PlateauCleanSlateV1.MissionId),
                (SurveillanceSentinels, PlateauCleanSlateV1.MissionId, PlateauTrinityLogosPastV1.MissionId)
            })
            {
                world.GetService<IMigrator>().Migrate(target);
                Assert.IsFalse(Has(gone), $"{gone} rolled back");
                Assert.IsTrue(Has(stays), $"{stays} stays");
            }
            Assert.AreEqual((5, 5, 1, 5), Sentinels(), "the sentinels stay with their own migration");

            world.GetService<IMigrator>().Migrate(PerduNpcs);
            Assert.AreEqual((0, 0, 0, 0), Sentinels());
            Assert.AreEqual(npcs, Npcs(), "the NPCs stay with their own migration");

            world.GetService<IMigrator>().Migrate(TrinityLogosPast);
            Assert.AreEqual((0, 0, 0, 0, 0, 0), Npcs());
            foreach (var move in PlateauPerduNpcsV1.Moves)
                Assert.AreEqual(move.Before, At(move.Pool), $"{move.Pool} back where the NPC seed had it");
            Assert.IsTrue(Has(PlateauTrinityLogosPastV1.MissionId));
        }

        /// <summary>
        /// Russ's and Garde's missions roll back a migration at a time: The Stainless Steel Bloodhound; Lookout Down
        /// Below; Bomb Voyage; then the Bloodhound's sentinel (creature, stat). The Wedge Rock move stays.
        /// </summary>
        [TestMethod]
        public void RusssAndGardesMissionsAndTheSentinelRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int) Sentinel() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PlateauChasmNpcsV1.Sentinel}"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id = {PlateauChasmNpcsV1.Sentinel}"));
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((1, 1), Sentinel());
            foreach (var (target, gone, stays) in new (string, uint, uint)[]
            {
                (LookoutDownBelow, PlateauBloodhoundV1.MissionId, PlateauLookoutDownBelowV1.MissionId),
                (BombVoyage, PlateauLookoutDownBelowV1.MissionId, PlateauBombVoyageV1.MissionId),
                (ChasmNpcs, PlateauBombVoyageV1.MissionId, PlateauMindSlayerV1.MissionId)
            })
            {
                world.GetService<IMigrator>().Migrate(target);
                Assert.IsFalse(Has(gone), $"{gone} rolled back");
                Assert.IsTrue(Has(stays), $"{stays} stays");
                Assert.AreEqual((1, 1), Sentinel(), $"the sentinel stays with its own migration ({target})");
            }

            world.GetService<IMigrator>().Migrate(WedgeRockNpcs);
            Assert.AreEqual((0, 0), Sentinel());
            var thibodeau = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == PlateauWedgeRockNpcsV1.Thibodeau);
            Assert.AreEqual(PlateauWedgeRockNpcsV1.Moves[0].At.X, Math.Round(thibodeau.PosX, 2), "the Wedge Rock move stays");
        }

        /// <summary>
        /// Fort Defiance's murder case rolls back a migration at a time: Looking For Clues; Murder Most Foul; Blue
        /// Flu; then the case's NPCs (Parkman's pool, the new templates, Pendrik's shot, Norton's pose), and Norton and
        /// Crais go back to the dropship marker. The Bloodhound stays.
        /// </summary>
        [TestMethod]
        public void FortDefiancesMurderCaseAndItsNpcsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int) Npcs() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN {PlateauCidNpcsV1.Parkman} AND {PlateauCidNpcsV1.CrapsPlayer}"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {PlateauCidNpcsV1.ParkmanPool}"),
                Count($"SELECT COUNT(*) AS Value FROM creature_action WHERE id = {PlateauCidNpcsV1.PendrikShot}"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN {PlateauCidNpcsV1.Parkman} AND {PlateauCidNpcsV1.CrapsPlayer}"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool_pose WHERE id = {PlateauCidNpcsV1.Norton}"));
            (double, double, double) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (Math.Round(row.PosX, 2), Math.Round(row.PosY, 2), Math.Round(row.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((5, 1, 1, 2, 1), Npcs());
            foreach (var move in PlateauCidNpcsV1.Moves)
                Assert.AreEqual(move.At, At(move.Pool), $"{move.Pool}");
            foreach (var (target, gone, stays) in new (string, uint, uint)[]
            {
                (MurderMostFoul, PlateauLookingForCluesV1.MissionId, PlateauMurderMostFoulV1.MissionId),
                (BlueFlu, PlateauMurderMostFoulV1.MissionId, PlateauBlueFluV1.MissionId),
                (CidNpcs, PlateauBlueFluV1.MissionId, PlateauBloodhoundV1.MissionId)
            })
            {
                world.GetService<IMigrator>().Migrate(target);
                Assert.IsFalse(Has(gone), $"{gone} rolled back");
                Assert.IsTrue(Has(stays), $"{stays} stays");
                Assert.AreEqual((5, 1, 1, 2, 1), Npcs(), $"the NPCs stay with their own migration ({target})");
            }

            world.GetService<IMigrator>().Migrate(Bloodhound);
            Assert.AreEqual((0, 0, 0, 0, 0), Npcs());
            foreach (var move in PlateauCidNpcsV1.Moves)
                Assert.AreEqual(move.Before, At(move.Pool), $"{move.Pool} back at the dropship marker");
            Assert.IsTrue(Has(PlateauBloodhoundV1.MissionId));
        }

        /// <summary>
        /// Bailey's and Ridout's missions roll back a migration at a time: Logos: Vortex; Symbolism; A Mystery
        /// Unearthed; then their NPCs (Bailey, Ridout, the Hermit), and Retread McCormick goes back to Pools.
        /// The murder case stays.
        /// </summary>
        [TestMethod]
        public void BaileysAndRidoutsMissionsAndTheirNpcsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int) Npcs() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN {PlateauBaileyNpcsV1.Bailey} AND {PlateauBaileyNpcsV1.Hermit}"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN {PlateauBaileyNpcsV1.BaileyPool} AND {PlateauBaileyNpcsV1.HermitPool}"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN {PlateauBaileyNpcsV1.Bailey} AND {PlateauBaileyNpcsV1.Hermit}"));
            (uint, double, double, double) McCormick()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == PlateauBaileyNpcsV1.McCormick);
                return (row.MapContextId, Math.Round(row.PosX, 2), Math.Round(row.PosY, 2), Math.Round(row.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((3, 3, 2), Npcs());
            var at = PlateauBaileyNpcsV1.McCormickAt;
            Assert.AreEqual((PlateauBaileyNpcsV1.MarshesMap, at.X, at.Y, at.Z), McCormick());
            foreach (var (target, gone, stays) in new (string, uint, uint)[]
            {
                (Symbolism, PlateauLogosVortexV1.MissionId, PlateauSymbolismV1.MissionId),
                (MysteryUnearthed, PlateauSymbolismV1.MissionId, PlateauMysteryUnearthedV1.MissionId),
                (BaileyNpcs, PlateauMysteryUnearthedV1.MissionId, PlateauLookingForCluesV1.MissionId)
            })
            {
                world.GetService<IMigrator>().Migrate(target);
                Assert.IsFalse(Has(gone), $"{gone} rolled back");
                Assert.IsTrue(Has(stays), $"{stays} stays");
                Assert.AreEqual((3, 3, 2), Npcs(), $"the NPCs stay with their own migration ({target})");
            }

            world.GetService<IMigrator>().Migrate(LookingForClues);
            Assert.AreEqual((0, 0, 0), Npcs());
            var before = PlateauBaileyNpcsV1.McCormickBefore;
            Assert.AreEqual((PlateauBaileyNpcsV1.PoolsMap, before.X, before.Y, before.Z), McCormick(), "McCormick back on Pools");
            Assert.IsTrue(Has(PlateauLookingForCluesV1.MissionId));
        }

        /// <summary>
        /// Behind Enemy Lines is offered by radio and turned in at Bosley (mission_channel_policy); Mislaid Plans
        /// and then Behind Enemy Lines roll back a migration at a time, with the channel policy; Logos: Vortex stays.
        /// </summary>
        [TestMethod]
        public void BehindEnemyLinesIsARadioCallAndBothRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Policies() => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM mission_channel_policy WHERE mission_id = {PlateauBehindEnemyLinesV1.MissionId}").AsEnumerable().Single();

            var definition = harness.Manager.LoadedMissions[PlateauBehindEnemyLinesV1.MissionId];
            Assert.AreEqual((Rasa.Missions.Definitions.MissionChannel.Radio, Rasa.Missions.Definitions.MissionChannel.Npc),
                (definition.AcceptanceChannel, definition.CompletionChannel));
            CollectionAssert.AreEquivalent(new[] { ("mission.rewarded:867", (uint?)null), ("map.arrival", (uint?)PlateauMissionDataV1.MapContextId) },
                definition.RadioSources.Select(source => (source.Key, source.MapContextId)).ToArray());
            Assert.IsFalse(definition.RadioCompletable == true, "turned in at Bosley");
            Assert.AreEqual(1, Policies());

            world.GetService<IMigrator>().Migrate(BehindEnemyLines);
            Assert.IsFalse(Has(PlateauMislaidPlansV1.MissionId), "Mislaid Plans rolled back");
            Assert.IsTrue(Has(PlateauBehindEnemyLinesV1.MissionId), "Behind Enemy Lines stays");
            world.GetService<IMigrator>().Migrate(LogosVortex);
            Assert.IsFalse(Has(PlateauBehindEnemyLinesV1.MissionId), "Behind Enemy Lines rolled back");
            Assert.AreEqual(0, Policies(), "its channel policy too");
            Assert.IsTrue(Has(PlateauLogosVortexV1.MissionId));
        }

        /// <summary>
        /// Wedge Rock's four NPCs and Cryptic Messages' two markers go back to the rock's top when their
        /// migration rolls back; Perdu's missions stay.
        /// </summary>
        [TestMethod]
        public void WedgeRocksNpcsAndMarkersRollBackToTheRock()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            (double, double, double) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (Math.Round(row.PosX, 2), Math.Round(row.PosY, 2), Math.Round(row.PosZ, 2));
            }
            (double, double, double) Marker(uint objective, uint indicator)
            {
                var row = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == PlateauCrypticMessagesV1.MissionId &&
                    entry.ObjectiveId == objective && entry.IndicatorId == Rasa.Structures.MissionIndicator.UnnamedFrom + indicator);
                return (row.PosX, row.PosY, row.PosZ);
            }

            foreach (var move in PlateauWedgeRockNpcsV1.Moves)
                Assert.AreEqual(move.At, At(move.Pool), $"{move.Pool}");
            foreach (var marker in PlateauWedgeRockNpcsV1.Markers)
                Assert.AreEqual(marker.At, Marker(marker.Objective, marker.Indicator), $"marker {marker.Objective}");

            world.GetService<IMigrator>().Migrate(MindSlayer);
            foreach (var move in PlateauWedgeRockNpcsV1.Moves)
                Assert.AreEqual(move.Before, At(move.Pool), $"{move.Pool} back on the rock");
            foreach (var marker in PlateauWedgeRockNpcsV1.Markers)
                Assert.AreEqual(marker.Before, Marker(marker.Objective, marker.Indicator), $"marker {marker.Objective} back");
            Assert.IsTrue(world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauMindSlayerV1.MissionId));
        }
    }
}
