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
    /// Plateau_toxic_detox); the Scrounger's chain (1251, 1015, 1017); Wedge Rock's All Along The
    /// Watchtowers (1825, Plateau_watchtowers) and Cryptic Messages (557, Plateau_cryptic_messages); Fort
    /// Defiance's Soyuz Salvage: Plateau (2007, Plateau_soyuz_salvage), The Defiant Ones (887,
    /// Plateau_defiant_ones), A Bridge Too Far (713, Plateau_bridge_too_far), The Beer Hunter (855,
    /// Plateau_beer_hunter), Crystal Blue Persuasion (954, Plateau_crystal_blue_persuasion) and Alpha Mox
    /// (1027, Plateau_alpha_mox); Special Agent Perdu's Clean Slate (1983, Plateau_clean_slate), Mind Bender
    /// (905, Plateau_mind_bender), Photo Ops (929, Plateau_photo_ops), The Results Are In (936,
    /// Plateau_results_are_in) and Mind Slayer (719, Plateau_mind_slayer); Russ's and Garde's, the murder
    /// case's, Bailey's and Ridout's missions, Behind Enemy Lines and Mislaid Plans (Plateau_bomb_voyage to
    /// Plateau_mislaid_plans); New Velon Village's Graveyard Shift (933, Plateau_graveyard_shift), Village Of The
    /// Damned (1981, Plateau_village_of_the_damned), The Book Of Velonar (930, Plateau_book_of_velonar) and Going
    /// Postal (1034, Plateau_going_postal); Ustor Yard's Silent But Deadly (1187), Interception! (1189) and Monkey
    /// Wrench (1188) (Plateau_ustor_yard); Maligo Base's Showdown (876), Coup (878) and One Way Trip (877)
    /// (Plateau_maligo_base); Bosley's South Of The Border (1068) and New Orders From The General (1541) and Maligo's
    /// Self Preservation (879) (Plateau_bosley_followups); Ustor Yard's Out of Thin Air (1190,
    /// Plateau_out_of_thin_air); Valverde Pools' Weapons Trafficking (904, Plateau_weapons_trafficking); Mount
    /// Reverence's A Voice From The Past (934, Plateau_voice_from_the_past) and Trinity Bridge Logos: Past (1044,
    /// Plateau_trinity_logos_past); the hologram's five instanced Trinity Bridge Logos (1047, 1048, 1049, 1051, 1057,
    /// Plateau_trinity_bridge_logos) and Sampei's The Order Of Velon (1982, Plateau_order_of_velon); the Temporal
    /// Chamber's Array Of Hope (935), Future Sacrifice (1171) and The Final Assault (1167) (Plateau_temporal_chamber);
    /// Sampei's Deadly Silence (1974, Plateau_deadly_silence); Velon Hollow's Ancient Mysteries (1978) and Mountain
    /// Spring Toxins (1979) (Plateau_velon_hollow_bane); Clark's The Better Half (1975, Plateau_better_half); the
    /// Temporal Chamber's Foreas Base Past (1178) and Send a Message (1181) (Plateau_temporal_past); Velon Hollow's
    /// These Old Bones (1976, Plateau_these_old_bones) and Holy Vengeance (1977, Plateau_holy_vengeance); Valverde Pools'
    /// Retreads to Retreads (1751, Plateau_retreads_to_retreads), on this revision because it follows Weapons Trafficking.
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
            PlateauScavengerHuntV1.MissionId, PlateauStalkMarketV1.MissionId, PlateauWatchtowersV1.MissionId,
            PlateauCrypticMessagesV1.MissionId, PlateauSoyuzSalvageV1.MissionId, PlateauDefiantOnesV1.MissionId,
            PlateauBridgeTooFarV1.MissionId, PlateauBeerHunterV1.MissionId, PlateauCrystalBluePersuasionV1.MissionId,
            PlateauAlphaMoxV1.MissionId, PlateauCleanSlateV1.MissionId, PlateauMindBenderV1.MissionId,
            PlateauPhotoOpsV1.MissionId, PlateauResultsAreInV1.MissionId, PlateauMindSlayerV1.MissionId,
            PlateauBombVoyageV1.MissionId, PlateauLookoutDownBelowV1.MissionId, PlateauBloodhoundV1.MissionId,
            PlateauBlueFluV1.MissionId, PlateauMurderMostFoulV1.MissionId, PlateauLookingForCluesV1.MissionId,
            PlateauMysteryUnearthedV1.MissionId, PlateauSymbolismV1.MissionId, PlateauLogosVortexV1.MissionId,
            PlateauBehindEnemyLinesV1.MissionId, PlateauMislaidPlansV1.MissionId,
            PlateauGraveyardShiftV1.MissionId, PlateauVillageOfTheDamnedV1.MissionId, PlateauBookOfVelonarV1.MissionId,
            PlateauGoingPostalV1.MissionId, PlateauSilentButDeadlyV1.MissionId, PlateauInterceptionV1.MissionId,
            PlateauMonkeyWrenchV1.MissionId, PlateauShowdownV1.MissionId, PlateauCoupV1.MissionId,
            PlateauOneWayTripV1.MissionId, PlateauSouthOfTheBorderV1.MissionId, PlateauNewOrdersFromTheGeneralV1.MissionId,
            PlateauSelfPreservationV1.MissionId, PlateauOutOfThinAirV1.MissionId, PlateauWeaponsTraffickingV1.MissionId,
            PlateauVoiceFromThePastV1.MissionId, PlateauTrinityLogosPastV1.MissionId,
            PlateauTrinityBridgeLogosV1.Future.MissionId, PlateauTrinityBridgeLogosV1.Disperse.MissionId,
            PlateauTrinityBridgeLogosV1.LogosElement.MissionId, PlateauTrinityBridgeLogosV1.Far.MissionId,
            PlateauTrinityBridgeLogosV1.Distance.MissionId, PlateauOrderOfVelonV1.MissionId, PlateauArrayOfHopeV1.MissionId,
            PlateauFutureSacrificeV1.MissionId, PlateauFinalAssaultV1.MissionId, PlateauDeadlySilenceV1.MissionId,
            PlateauAncientMysteriesV1.MissionId, PlateauMountainSpringToxinsV1.MissionId, PlateauBetterHalfV1.MissionId,
            PlateauForeasBasePastV1.MissionId, PlateauSendAMessageV1.MissionId, PlateauTheseOldBonesV1.MissionId,
            PlateauHolyVengeanceV1.MissionId, PlateauRetreadsToRetreadsV1.MissionId
        };

        /// <summary>
        /// Every Plateau objective with a place has a marker, in instances and on other maps too (the server draws a
        /// marker on every map for now); hidden objectives and those of <see cref="NoMarker"/> have none. Giant Killer's
        /// two indicators, Incommunicado's three, Canyon Sweep's one, Blueprint For Victory's two, Toxic Detox's one,
        /// Comic Relief's five, Scavenger Hunt's two (the nearest Thrax), The Stalk Market's three. All Along The
        /// Watchtowers', Cryptic Messages' and Soyuz Salvage's three each. The Beer Hunter's four; The Defiant Ones', A
        /// Bridge Too Far's, Crystal Blue Persuasion's and Alpha Mox's one each. Clean Slate's three, Mind Bender's
        /// eight, Photo Ops' five, The Results Are In's and Mind Slayer's one each. Bomb Voyage's two, Lookout Down
        /// Below's one, The Stainless Steel Bloodhound's two. Blue Flu's two, Murder Most Foul's seven, Looking For
        /// Clues' eight. Logos: Vortex's one; A Mystery Unearthed's one (Bailey) and Symbolism's four (the Hermit,
        /// McCormick, the Earth shrine, Topper). Behind Enemy Lines' and Mislaid Plans' one each. Graveyard Shift's
        /// two, Village Of The Damned's and The Book Of Velonar's one each, Going Postal's two. Silent But Deadly's
        /// five, Interception!'s eight, Monkey Wrench's three, Out of Thin Air's three. Showdown's two, Coup's ten, One
        /// Way Trip's thirteen, Self Preservation's six, South Of The Border's and New Orders From The General's one
        /// each. Weapons Trafficking's six (Valverde Pools and the Lamna Armory) as PlateauLamnasArmoryTests. A Voice From
        /// The Past's and Trinity Bridge Logos: Past's one each. The five instanced Trinity Bridge Logos one each, at
        /// their shrine, and The Order Of Velon one, at Hathen. Array Of Hope's one; Future Sacrifice's one (Hudson), The
        /// Final Assault's five, Foreas Base Past's seven, Send a Message's one. Deadly Silence's four, These Old Bones'
        /// four, Holy Vengeance's six, Ancient Mysteries' nine, Mountain Spring Toxins' six, The Better Half's four.
        /// Retreads to Retreads' one (1751, at Vincent on Valverde Marshes).
        /// </summary>
        internal const int Indicators = 206;

        /// <summary>Visible objectives with no marker: none.</summary>
        internal static readonly (uint Mission, uint Objective)[] NoMarker = { };

        private const string Before = "20270921000100_Take_down_field_action";
        private const string GiantKiller = "20270922000000_Plateau_giant_killer";
        private const string Incommunicado = "20270922000100_Plateau_incommunicado";
        private const string CanyonSweep = "20270922000200_Plateau_canyon_sweep";
        private const string BlueprintForVictory = "20270922000300_Plateau_blueprint_for_victory";
        private const string ToxicDetox = "20270922000400_Plateau_toxic_detox";
        private const string ComicRelief = "20270922000500_Plateau_comic_relief";
        private const string ScavengerHunt = "20270922000600_Plateau_scavenger_hunt";
        private const string StalkMarket = "20270922000700_Plateau_stalk_market";
        private const string GraveyardShift = "20270922010000_Plateau_graveyard_shift";
        private const string VillageOfTheDamned = "20270922010100_Plateau_village_of_the_damned";
        private const string BookOfVelonar = "20270922010200_Plateau_book_of_velonar";
        private const string GoingPostal = "20270922010300_Plateau_going_postal";
        private const string Watchtowers = "20270922000800_Plateau_watchtowers";
        private const string CrypticMessages = "20270922000900_Plateau_cryptic_messages";
        private const string SoyuzSalvage = "20270922001000_Plateau_soyuz_salvage";
        private const string DefiantOnes = "20270922001100_Plateau_defiant_ones";
        private const string BridgeTooFar = "20270922001200_Plateau_bridge_too_far";
        private const string BeerHunter = "20270922001300_Plateau_beer_hunter";
        private const string CrystalBluePersuasion = "20270922001400_Plateau_crystal_blue_persuasion";
        private const string AlphaMox = "20270922001500_Plateau_alpha_mox";
        private const string InfectedForeanAttack = "20270922010400_Plateau_infected_forean_attack";
        private const string VogrenNpcs = "20270922020000_Plateau_vogren_npcs";
        private const string VoiceFromThePast = "20270922020100_Plateau_voice_from_the_past";
        private const string TrinityLogosPast = "20270922020200_Plateau_trinity_logos_past";
        private const string PerduNpcs = "20270922001600_Plateau_perdu_npcs";
        private const string SurveillanceSentinels = "20270922001700_Plateau_surveillance_sentinels";
        private const string CleanSlate = "20270922001800_Plateau_clean_slate";
        private const string MindBender = "20270922001900_Plateau_mind_bender";
        private const string PhotoOps = "20270922002000_Plateau_photo_ops";
        private const string ResultsAreIn = "20270922002100_Plateau_results_are_in";
        private const string MindSlayer = "20270922002200_Plateau_mind_slayer";
        private const string WedgeRockNpcs = "20270922002300_Plateau_wedge_rock_npcs";
        private const string ChasmNpcs = "20270922002400_Plateau_chasm_npcs";
        private const string BombVoyage = "20270922002500_Plateau_bomb_voyage";
        private const string LookoutDownBelow = "20270922002600_Plateau_lookout_down_below";
        private const string Bloodhound = "20270922002700_Plateau_bloodhound";
        private const string CidNpcs = "20270922002800_Plateau_cid_npcs";
        private const string BlueFlu = "20270922002900_Plateau_blue_flu";
        private const string MurderMostFoul = "20270922003000_Plateau_murder_most_foul";
        private const string LookingForClues = "20270922003100_Plateau_looking_for_clues";
        private const string BaileyNpcs = "20270922003200_Plateau_bailey_npcs";
        private const string MysteryUnearthed = "20270922003300_Plateau_mystery_unearthed";
        private const string Symbolism = "20270922003400_Plateau_symbolism";
        private const string LogosVortex = "20270922003500_Plateau_logos_vortex";
        private const string BehindEnemyLines = "20270922003600_Plateau_behind_enemy_lines";
        private const string MislaidPlans = "20270922003700_Plateau_mislaid_plans";
        private const string TrinityBridgeLogos = "20270922020300_Plateau_trinity_bridge_logos";
        private const string OrderOfVelon = "20270922020400_Plateau_order_of_velon";
        private const string TemporalChamber = "20270922020500_Plateau_temporal_chamber";
        private const string DeadlySilence = "20270922020600_Plateau_deadly_silence";
        private const string UstorYard = "20270922010500_Plateau_ustor_yard";
        private const string VelonHollowBane = "20270922020700_Plateau_velon_hollow_bane";
        private const string BetterHalf = "20270922020800_Plateau_better_half";
        private const string MaligoBase = "20270922010600_Plateau_maligo_base";
        private const string BosleyFollowups = "20270922010700_Plateau_bosley_followups";
        private const string TemporalPast = "20270922020900_Plateau_temporal_past";
        private const string OutOfThinAir = "20270922010800_Plateau_out_of_thin_air";
        private const string WeaponsTrafficking = "20270922010900_Plateau_weapons_trafficking";
        private const string TheseOldBones = "20270922021000_Plateau_these_old_bones";
        private const string NpcSquadLeaderBystander = "20270925000000_Npc_squad_leader_bystander";

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
                (PlateauMislaidPlansV1.MissionId, PlateauBaileyNpcsV1.Bailey, PlateauBaileyNpcsV1.Bailey),
                (1047, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (1048, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (1049, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (1051, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (1057, PlateauVogrenNpcsV1.Hologram, PlateauVogrenNpcsV1.Hologram),
                (PlateauOrderOfVelonV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauOrderOfVelonV1.Hathen),
                (PlateauArrayOfHopeV1.MissionId, PlateauVogrenNpcsV1.Hologram, PlateauTemporalChamberNpcsV1.Veryin),
                (PlateauFutureSacrificeV1.MissionId, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.Hudson),
                (PlateauFinalAssaultV1.MissionId, PlateauTemporalChamberNpcsV1.Hudson, PlateauTemporalChamberNpcsV1.Veryin),
                (PlateauDeadlySilenceV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauSilentButDeadlyV1.MissionId, PlateauUstorYardV1.Rapp, PlateauUstorYardV1.Rapp),
                (PlateauInterceptionV1.MissionId, PlateauUstorYardV1.Rapp, PlateauUstorYardV1.Rapp),
                (PlateauMonkeyWrenchV1.MissionId, PlateauUstorYardV1.Rapp, null),
                (PlateauAncientMysteriesV1.MissionId, PlateauVelonHollowBaneV1.Hundra, PlateauVelonHollowBaneV1.Hundra),
                (PlateauMountainSpringToxinsV1.MissionId, PlateauPerduNpcsV1.Russ, PlateauPerduNpcsV1.Russ),
                (PlateauBetterHalfV1.MissionId, PlateauPerduNpcsV1.Clark, PlateauPerduNpcsV1.Clark),
                (PlateauShowdownV1.MissionId, null, null),
                (PlateauCoupV1.MissionId, PlateauMaligoBaseV1.DownedPrisoner, PlateauMaligoBaseV1.Altera),
                (PlateauOneWayTripV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauGiantKillerV1.Bosley),
                (PlateauSouthOfTheBorderV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauSnakePitV1.Snake),
                (PlateauNewOrdersFromTheGeneralV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauNewOrdersFromTheGeneralV1.Amee),
                (PlateauSelfPreservationV1.MissionId, null, null),
                (PlateauForeasBasePastV1.MissionId, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.Veryin),
                (PlateauSendAMessageV1.MissionId, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.Veryin),
                (PlateauOutOfThinAirV1.MissionId, PlateauMaligoPrisonerV1.Prisoner, null),
                (PlateauWeaponsTraffickingV1.MissionId, PlateauNewOrdersFromTheGeneralV1.Amee, PlateauNewOrdersFromTheGeneralV1.Amee),
                (PlateauTheseOldBonesV1.MissionId, PlateauOrderOfVelonV1.Hathen, PlateauTheseOldBonesV1.Algona),
                (PlateauHolyVengeanceV1.MissionId, PlateauTheseOldBonesV1.Algona, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauRetreadsToRetreadsV1.MissionId, PlateauNewOrdersFromTheGeneralV1.Amee, PlateauRetreadsToRetreadsV1.Vincent)
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
                Assert.AreEqual<uint?>(mission switch { 1251 or 1015 or 1017 or 1027 or 1044 => 31U, 930 or 1825 or 557 or 934 or 905 or 929 or 936 or 719 => 32U, 2007 => 34U, 2016 or 2017 => 50U, 1974 or 1187 or 1188 or 1189 or 1978 or 1979 or 1975 or 1190 or 1976 or 1977 => 32U, 1047 or 1048 or 1049 or 1051 or 1057 or 877 or 1541 or 904 or 1751 => 33U, 1068 => 25U, _ => 30U },
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
                (965, 5269, new (uint, uint, uint, uint?)[] { (1, 5277, 6412, null) }),
                (1047, 5885, new (uint, uint, uint, uint?)[] { (1, 5889, 6378, null) }),
                (1048, 5890, new (uint, uint, uint, uint?)[] { (1, 5894, 6372, null) }),
                (1049, 5895, new (uint, uint, uint, uint?)[] { (1, 5899, 6382, null) }),
                (1051, 5905, new (uint, uint, uint, uint?)[] { (1, 5909, 6376, null) }),
                (1057, 5935, new (uint, uint, uint, uint?)[] { (1, 5939, 6374, null) }),
                (1982, 20866, new (uint, uint, uint, uint?)[] { (1, 20889, 20890, null) }),
                (935, 5030, new (uint, uint, uint, uint?)[] { (1, 9156, 9157, null), (2, 9156, 9157, null) }),
                (1171, 8729, new (uint, uint, uint, uint?)[] { (1, 8751, 8752, null) }),
                (1167, 8577, new (uint, uint, uint, uint?)[] { (1, 8604, 8605, 8606), (2, 8607, 8608, null), (3, 11034, 11035, 11036),
                    (4, 11037, 11038, 11039), (5, 8604, 8605, null) }),
                (1974, 20798, new (uint, uint, uint, uint?)[] { (1, 20849, 20850, null), (2, 20852, 20853, null), (3, 20854, 20855, null),
                    (4, 20856, 20857, null) }),
                (1187, 9127, new (uint, uint, uint, uint?)[] { (1, 9224, 9225, null), (3, 9228, 9229, null), (4, 9230, 9231, null),
                    (5, 9226, 9227, null), (6, 9232, 9233, null), (7, 17341, 17342, null), (8, 9228, 9229, null), (9, 9226, 9227, null) }),
                (1189, 9137, new (uint, uint, uint, uint?)[] { (1, 9241, 9242, null), (2, 9243, 9244, null), (3, 9245, 9246, 9249),
                    (4, 9247, 9248, 17888), (5, 9243, 9244, null) }),
                (1188, 9132, new (uint, uint, uint, uint?)[] { (1, 9147, 9148, null), (2, 9151, 9152, null), (3, 9149, 9150, null) }),
                (1978, 20818, new (uint, uint, uint, uint?)[] { (1, 20835, 20836, null), (2, 20837, 20838, null), (3, 20929, 20930, null) }),
                (1979, 20823, new (uint, uint, uint, uint?)[] { (1, 20906, 20907, null), (2, 20908, 20909, null), (3, 20910, 20911, null),
                    (4, 20912, 20913, null), (5, 20914, 20915, null), (6, 20916, 20917, null), (7, 20908, 20909, null) }),
                (1975, 20803, new (uint, uint, uint, uint?)[] { (1, 20871, 20872, null), (2, 20873, 20874, null), (3, 20875, 20876, null),
                    (4, 20877, 20878, null), (5, 20879, 20880, null) }),
                (876, 4591, new (uint, uint, uint, uint?)[] { (1, 4671, 8877, null), (2, 4841, 8878, null) }),
                (878, 4599, new (uint, uint, uint, uint?)[] { (1, 4826, 9545, null), (2, 4828, 9546, 19816), (4, 4832, 9548, null),
                    (5, 6086, 9547, null), (6, 19817, 19818, null) }),
                (877, 4595, new (uint, uint, uint, uint?)[] { (1, 4607, 7896, null), (4, 4842, 7898, null), (6, 6231, 7897, null),
                    (7, 6238, 7900, null), (9, 9124, 9125, null), (10, 9122, 9123, null), (11, 17321, 17322, null),
                    (12, 17346, 17347, null), (13, 17351, 17352, null), (14, 17354, 17355, null), (15, 17356, 17357, null),
                    (16, 19230, 19231, null) }),
                (1068, 6032, new (uint, uint, uint, uint?)[] { (1, 6036, 6453, null) }),
                (1541, 13931, new (uint, uint, uint, uint?)[] { (1, 13936, 13937, null) }),
                (879, 4603, new (uint, uint, uint, uint?)[] { (1, 4824, 11142, 4825), (2, 4824, 11142, null), (3, 4824, 11142, null), (4, 4824, 11142, null) }),
                (1178, 8846, new (uint, uint, uint, uint?)[] { (1, 8856, 8857, null), (3, 8915, 8916, null), (4, 8947, 8948, null),
                    (5, 8950, 8951, 8952), (6, 8954, 8955, null), (7, 8957, 8958, null) }),
                (1181, 8966, new (uint, uint, uint, uint?)[] { (2, 8976, 8977, null) }),
                (1190, 9142, new (uint, uint, uint, uint?)[] { (1, 9236, 9237, null), (2, 9234, 9235, null), (3, 9238, 9239, null) }),
                (904, 4788, new (uint, uint, uint, uint?)[] { (1, 4792, 7348, null), (2, 5964, 7349, null), (3, 5965, 7350, null),
                    (4, 5966, 7351, null), (5, 5967, 7352, null), (6, 5968, 7353, null) }),
                (1976, 20808, new (uint, uint, uint, uint?)[] { (1, 20841, 20842, null), (2, 20843, 20844, null), (3, 20845, 20846, null),
                    (4, 20847, 20848, null), (5, 20841, 20842, null), (6, 20843, 20844, null), (7, 20845, 20846, null) }),
                (1977, 20813, new (uint, uint, uint, uint?)[] { (1, 20883, 20884, null), (2, 20885, 20886, null), (3, 20887, 20888, null),
                    (4, 21162, 21163, null) }),
                (1751, 17125, new (uint, uint, uint, uint?)[] { (1, 17165, 17166, null) })
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
                    // 1178.5 shows Xykryl (8952) and his strike force (8953) on two counters (to-fix #43).
                    Assert.AreEqual(mission == 1178 && objective == 5 ? 8953U : null, row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }
        }

        /// <summary>
        /// Incommunicado follows Giant Killer; each commander's mission follows his own delivery: the
        /// flag that Incommunicado's delivery objective sets (action 3, SetPlayerFlag to 1). Scavenger
        /// Hunt follows the Scrounger's talk in Comic Relief (objective 10, action 1, the same way);
        /// The Stalk Market follows Scavenger Hunt. Wedge Rock's two, Soyuz Salvage and The Defiant Ones have no
        /// requirement; Fort Defiance's others follow The Defiant Ones: A Bridge Too Far, then The Beer Hunter;
        /// Crystal Blue Persuasion, then Alpha Mox. Clean Slate has none; Perdu's line follows it: Mind Bender, Photo
        /// Ops, The Results Are In, Mind Slayer. Bomb Voyage and Lookout Down Below have none; The Stainless Steel
        /// Bloodhound follows Lookout Down Below. Fort Defiance's murder case: Blue Flu follows The Defiant Ones, Murder
        /// Most Foul Blue Flu, Looking For Clues Murder Most Foul. A Mystery Unearthed and Logos: Vortex have none;
        /// Symbolism follows A Mystery Unearthed. Behind Enemy Lines follows The Stainless Steel Bloodhound; Mislaid
        /// Plans has none. New Velon Village's four have none. Interception! follows Silent But Deadly, Monkey Wrench
        /// Interception!, Out of Thin Air Monkey Wrench. Showdown has none; Coup follows Showdown, One Way Trip
        /// Blueprint For Victory. South Of The Border and New Orders From The General follow One Way Trip; Self
        /// Preservation has none. Weapons Trafficking follows New Orders From The General. A Voice From The Past has
        /// none; Trinity Bridge Logos: Past follows it, and the five instanced Trinity Bridge Logos follow that. The
        /// Order Of Velon, Deadly Silence, Ancient Mysteries, Mountain Spring Toxins and The Better Half follow Village
        /// Of The Damned. Array Of Hope follows all six Trinity Bridge Logos; Future Sacrifice follows Array Of Hope, The
        /// Final Assault Future Sacrifice, Foreas Base Past The Final Assault, Send a Message Foreas Base Past. These Old
        /// Bones follows Village Of The Damned, Holy Vengeance These Old Bones.
        /// </summary>
        [TestMethod]
        public void EachMissionRequiresTheMissionOrDeliveryBeforeIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(52, prerequisites);
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
            foreach (var mission in new uint[] { 933, 1981, 930, 1034, 1825, 557, 2007, 887, 934, 1983, 931, 970, 2016, 1659, 965, 876, 879 })
                Assert.IsFalse(prerequisites.Any(entry => entry.MissionId == mission), $"{mission}");
            foreach (var (mission, required) in new (uint, uint)[] { (713, 887), (954, 887), (855, 713), (1027, 954), (1044, 934), (905, 1983), (929, 905), (936, 929), (719, 936), (867, 970), (1014, 887), (895, 1014), (928, 895), (2017, 2016), (555, 867), (1047, 1044), (1048, 1044), (1049, 1044), (1051, 1044), (1057, 1044), (1982, 1981), (1171, 935), (1167, 1171), (1974, 1981), (1189, 1187), (1188, 1189), (1978, 1981), (1979, 1981), (1975, 1981), (878, 876), (877, 1046), (1068, 877), (1541, 877), (1178, 1167), (1181, 1178), (1190, 1188), (904, 1541), (1976, 1981), (1977, 1976), (1751, 904) })
            {
                var prerequisite = prerequisites.Single(entry => entry.MissionId == mission);
                Assert.AreEqual((MissionPrerequisiteKind.MissionCompleted, (uint?)required),
                    (prerequisite.Kind, prerequisite.RequiredMissionId), $"{mission}");
            }
            var arrayOfHope = prerequisites.Where(entry => entry.MissionId == PlateauArrayOfHopeV1.MissionId).ToArray();
            Assert.IsTrue(arrayOfHope.All(entry => entry.Kind == MissionPrerequisiteKind.MissionCompleted));
            CollectionAssert.AreEquivalent(new uint?[] { 1044, 1047, 1048, 1049, 1051, 1057 },
                arrayOfHope.Select(entry => entry.RequiredMissionId).ToArray(), "Array Of Hope: the six Trinity Bridge Logos");
        }

        /// <summary>
        /// The missions' sources and how far each is trusted: the client's tables 1.0, TaRapedia 0.5
        /// (0.4 for Blueprint For Victory, whose page gives no drop rate, and for Scavenger Hunt, Cryptic
        /// Messages and Soyuz Salvage, stub pages), and what this repository reconstructed 0.3 (the guards,
        /// the drop rates, the pumps, the NPCs' looks and Lonigan's place, the Stalkers, the towers, Vorvaak's
        /// drop, Fort Defiance's NPCs, the Juggernaut, the plants, the Mox Lair) - 0.2 for Soyuz Salvage, whose
        /// places, XP and credits no source gives. The Defiant Ones', The Beer Hunter's and Crystal Blue
        /// Persuasion's pages are stubs (0.4), as are Mind Bender's and Mind Slayer's. New Velon Village's: the
        /// miasmas, the Infected Foreans, the book's place and Porthus's place are reconstructed (0.3); Going
        /// Postal's page is a stub (0.4). A Voice From The Past's page is a stub (0.4). The Order Of Velon has no
        /// page: the client and the reconstruction only. The Temporal Chamber's three are sourced as the Logos (0.5).
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
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1047, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1048, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1049, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1051, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1057, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1982, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (935, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1171, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1167, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1974, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1187, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1189, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1188, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1978, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1979, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1975, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.2) }),
                (876, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (878, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (877, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Documentation, 0.5), (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1068, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1541, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (879, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1178, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1181, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1190, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (904, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1976, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.2) }),
                (1977, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.2) }),
                (1751, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) })
            })
                CollectionAssert.AreEqual(confidences, evidence.Where(entry => entry.MissionId == mission)
                    .OrderBy(entry => entry.EvidenceId).Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(),
                    $"{mission} evidence");
        }

        [TestMethod]
        public void EveryVisibleObjectiveWithAPlaceHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = PlateauMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var hidden = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .SelectMany(entry => System.Text.RegularExpressions.Regex.Match(entry.Bindings, "\"HiddenObjectiveIds\"\\s*:\\s*\\[([^\\]]*)\\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).Select(id => (entry.MissionId, uint.Parse(id.Trim()))))
                .ToHashSet();
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.IsTrue(hidden.Contains((PlateauBeerHunterV1.MissionId, PlateauBeerHunterV1.Tally)), "the hidden objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            CollectionAssert.AreEquivalent(NoMarker, missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
        }

        /// <summary>
        /// To-fix #20: an objective that is done when a pool is cleared (SpawnPoolCleared) and has a marker on that pool
        /// (within <see cref="OnThePool"/> m of its centre, flat) has one at least as wide as the pool's spawn radius: the
        /// player sees on the map the whole ground the pool's creatures stand on (Holy Vengeance's 15 m markers lay on pools
        /// of 30 and 40 m).
        /// </summary>
        [TestMethod]
        public void PoolMarkersCoverThePoolsSpawnRadius()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = PlateauMissionDataV1.Revision;
            var cleared = world.MissionTriggerEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && entry.EventKind == (byte)Rasa.Data.MissionProgressEventKind.SpawnPoolCleared)
                .ToList().Where(entry => MissionIds.Contains(entry.MissionId) && entry.SubjectId.HasValue)
                .Select(entry => (entry.MissionId, entry.ObjectiveId, Pool: entry.SubjectId.Value)).Distinct().ToList();
            Assert.IsNotEmpty(cleared, "objectives on cleared pools");
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList();
            var checkedOnes = 0;
            var tooSmall = new System.Collections.Generic.List<string>();
            foreach (var (mission, objective, poolId) in cleared)
            {
                var pool = world.SpawnPoolEntries.AsNoTracking().SingleOrDefault(entry => entry.Id == poolId);
                Assert.IsNotNull(pool, $"{mission} objective {objective}: pool {poolId} is a row");
                foreach (var marker in markers.Where(entry => entry.MissionId == mission && entry.ObjectiveId == objective &&
                             System.Numerics.Vector2.Distance(new((float)entry.PosX, (float)entry.PosZ), new((float)pool.PosX, (float)pool.PosZ)) <= OnThePool))
                {
                    checkedOnes++;
                    if (marker.Radius < pool.Radius)
                        tooSmall.Add($"{mission} objective {objective} marker {marker.IndicatorId - Rasa.Structures.MissionIndicator.UnnamedFrom}: " +
                                     $"{marker.Radius} m on pool {poolId} of {pool.Radius} m");
                }
            }
            Assert.IsGreaterThan(0, checkedOnes, "markers on cleared pools");
            Assert.IsEmpty(tooSmall, string.Join("; ", tooSmall));
        }

        /// <summary>How near a pool's centre a marker stands to be that pool's (flat, metres).</summary>
        private const float OnThePool = 5;

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

        /// <summary>
        /// Every text the World migrations write fits its MySQL column (MySqlWorldContextModelSnapshot): SQLite takes a
        /// text of any length, MySQL stops the migration ("Data too long for column"). The MySQL migrations write the
        /// same rows as the SQLite ones (the same data classes).
        /// </summary>
        [TestMethod]
        public void EveryWorldTextFitsItsMySqlColumn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var model = new Rasa.Migrations.MySqlWorld.MySqlWorldContextModelSnapshot().Model;
            var tooLong = new System.Collections.Generic.List<string>();
            var checkedColumns = 0;
            foreach (var entity in model.GetEntityTypes())
            {
                var table = entity.GetTableName();
                if (table == null) continue;
                foreach (var property in entity.GetProperties())
                {
                    var type = property.GetColumnType();
                    var match = type == null ? null : System.Text.RegularExpressions.Regex.Match(type, @"^(?:var)?char\((\d+)\)$");
                    var length = match is { Success: true } ? int.Parse(match.Groups[1].Value) : property.GetMaxLength();
                    if (length == null) continue;
                    checkedColumns++;
                    var column = property.GetColumnName();
                    var count = harness.World.Database.SqlQueryRaw<int>(
                        $"SELECT COUNT(*) AS Value FROM \"{table}\" WHERE length(\"{column}\") > {length}").AsEnumerable().Single();
                    if (count > 0)
                        tooLong.Add($"{table}.{column} ({length}): {count} rows");
                }
            }
            Assert.IsGreaterThan(50, checkedColumns, "columns with a MySQL length");
            Assert.IsEmpty(tooLong, string.Join("; ", tooLong));
        }

        [TestMethod]
        public void TheReconSquadsStandAndRollBackAndGoliathsPoolHasNoGuards()
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
            Assert.AreEqual((0U, (byte)0, (byte)0), Guards(), "the Kael come with Goliath's squad (PlateauGiantKillerSquadV1), not his pool");

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
        /// New Velon Village's missions roll back a migration at a time: Infected Forean Attack puts the Infected
        /// Forean's lightning back to the goo gun's shot; Going Postal puts Porthus back on
        /// the chasm's floor; The Book Of Velonar; Village Of The Damned with its Infected Forean
        /// (creature, stats, look, action, four pools); Graveyard Shift with the Tomb Miasma and the Bull
        /// (creatures, stats, four actions, five pools). Mislaid Plans stays.
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
            (uint, uint, uint) Attack()
            {
                var row = world.CreatureActionEntries.AsNoTracking().Single(entry => entry.Id == PlateauInfectedForeansV1.Weapon);
                return (row.ActionId, row.ActionArgId, row.DamageType);
            }

            Assert.AreEqual((2, 2, 4, 5), Miasmas());
            Assert.AreEqual((1, 1, 1, 1, 4), Infected());
            Assert.AreEqual(PlateauGoingPostalV1.PorthusAt, Porthus());
            Assert.AreEqual((203U, 1U, 13U), Attack(), "the Infected Forean's lightning");

            world.GetService<IMigrator>().Migrate(GoingPostal);
            Assert.AreEqual((1U, 116U, 1U), Attack(), "the goo gun's shot, as it was");
            Assert.IsTrue(Has(PlateauGoingPostalV1.MissionId));

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

            world.GetService<IMigrator>().Migrate(MislaidPlans);
            Assert.IsFalse(Has(PlateauGraveyardShiftV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), Miasmas());
            Assert.IsTrue(Has(PlateauMislaidPlansV1.MissionId));
        }

        /// <summary>
        /// Wedge Rock's missions and Soyuz Salvage roll back a migration at a time: Soyuz Salvage; Cryptic
        /// Messages with its radio channel; All Along The Watchtowers. The Stalk Market stays.
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

            world.GetService<IMigrator>().Migrate(StalkMarket);
            Assert.IsFalse(Has(PlateauWatchtowersV1.MissionId));
            Assert.IsTrue(Has(PlateauStalkMarketV1.MissionId));
        }

        /// <summary>
        /// Fort Defiance's missions roll back a migration at a time: Alpha Mox; Crystal Blue Persuasion with
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

            world.GetService<IMigrator>().Migrate(AlphaMox);
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
        /// The Past; then Tendahi and the hologram (creature, look, name, package, pool). Weapons Trafficking stays.
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

            world.GetService<IMigrator>().Migrate(WeaponsTrafficking);
            Assert.AreEqual((0, 0, 0, 0, 0), Npcs());
            Assert.IsTrue(Has(PlateauWeaponsTraffickingV1.MissionId));
        }

        /// <summary>
        /// Perdu's missions roll back a migration at a time: Mind Slayer; The Results Are In; Photo Ops; Mind
        /// Bender; Clean Slate; then the sentinels (creatures, stats, action, pools); then the NPCs (Wainwright,
        /// Russ, the four soldiers, Clean Slate's two templates, the pistol), and Perdu and Clark back where the
        /// NPC seed had them. Alpha Mox stays.
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
                (SurveillanceSentinels, PlateauCleanSlateV1.MissionId, PlateauAlphaMoxV1.MissionId)
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

            world.GetService<IMigrator>().Migrate(AlphaMox);
            Assert.AreEqual((0, 0, 0, 0, 0, 0), Npcs());
            foreach (var move in PlateauPerduNpcsV1.Moves)
                Assert.AreEqual(move.Before, At(move.Pool), $"{move.Pool} back where the NPC seed had it");
            Assert.IsTrue(Has(PlateauAlphaMoxV1.MissionId));
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
        /// Goliath is half again his model's size (PlateauGiantKillerScaleV1); the size rolls back
        /// with Plateau_giant_killer. His squad's rows are PlateauGiantKillerSquadTests'.
        /// </summary>
        [TestMethod]
        public void GoliathsSizeRollsBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;

            Assert.AreEqual(1.5, world.CreatureScaleEntries.AsNoTracking().Single(row => row.Id == PlateauGiantKillerV1.Goliath).Scale,
                "Goliath is half again his model's size (PlateauGiantKillerScaleV1)");
            Assert.AreEqual(1, world.CreatureScaleEntries.AsNoTracking().Count(), "no other creature has a size of its own");

            world.GetService<IMigrator>().Migrate("20270921000100_Take_down_field_action");
            Assert.AreEqual(0, world.CreatureScaleEntries.AsNoTracking().Count(), "Goliath's size goes with Plateau_giant_killer");
            Assert.IsFalse(world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauGiantKillerV1.MissionId));
        }

        /// <summary>
        /// The instanced Trinity Bridge Logos (Plateau_trinity_bridge_logos) and The Order Of Velon
        /// (Plateau_order_of_velon) go with their migrations, each on its own; the missions before stay.
        /// </summary>
        [TestMethod]
        public void TheInstanceMissionsRollBackWithTheirMigrations()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var logos = PlateauTrinityBridgeLogosV1.All.Select(entry => entry.MissionId).ToArray();
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            bool AnyRow(uint mission) => world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.IsTrue(logos.All(Has));
            Assert.IsTrue(Has(PlateauOrderOfVelonV1.MissionId));

            world.GetService<IMigrator>().Migrate(TrinityBridgeLogos);
            Assert.IsFalse(Has(PlateauOrderOfVelonV1.MissionId));
            Assert.IsFalse(AnyRow(PlateauOrderOfVelonV1.MissionId), "no row of The Order Of Velon is left");
            Assert.IsTrue(logos.All(Has), "the Logos missions stay");

            world.GetService<IMigrator>().Migrate(TrinityLogosPast);
            Assert.IsFalse(logos.Any(Has));
            Assert.IsFalse(logos.Any(AnyRow), "no row of the Logos missions is left");
            Assert.IsTrue(Has(PlateauTrinityLogosPastV1.MissionId));
            Assert.IsTrue(Has(PlateauVillageOfTheDamnedV1.MissionId));
        }

        /// <summary>
        /// The Temporal Chamber (Plateau_temporal_chamber) goes with its migration: its three missions, Veryin,
        /// Hudson and the teleporter pair; The Order Of Velon and the Logos missions stay.
        /// </summary>
        [TestMethod]
        public void TheTemporalChamberRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new[] { PlateauArrayOfHopeV1.MissionId, PlateauFutureSacrificeV1.MissionId, PlateauFinalAssaultV1.MissionId };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            bool AnyRow(uint mission) => world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionActionEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == mission) ||
                world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            var creatures = new[] { PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.Hudson };
            var pools = new[] { PlateauTemporalChamberNpcsV1.VeryinPool, PlateauTemporalChamberNpcsV1.HudsonPool };
            var pads = new[] { PlateauTemporalChamberNpcsV1.ChamberPad, PlateauTemporalChamberNpcsV1.AliaDasPad };

            Assert.IsTrue(missions.All(Has));
            Assert.HasCount(2, world.CreatureEntries.AsNoTracking().Where(row => creatures.Contains(row.Id)).ToArray());
            Assert.HasCount(2, world.SpawnPoolEntries.AsNoTracking().Where(row => pools.Contains(row.Id)).ToArray());
            Assert.HasCount(2, world.TeleporterEntries.AsNoTracking().Where(row => pads.Contains(row.Id)).ToArray());

            world.GetService<IMigrator>().Migrate(OrderOfVelon);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(missions.Any(AnyRow), "no row of the chamber's missions is left");
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => creatures.Contains(row.Id)));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => pools.Contains(row.Id)));
            Assert.IsFalse(world.TeleporterEntries.AsNoTracking().Any(row => pads.Contains(row.Id)));
            Assert.IsTrue(Has(PlateauOrderOfVelonV1.MissionId));
            Assert.IsTrue(Has(PlateauTrinityLogosPastV1.MissionId));
        }

        /// <summary>
        /// Deadly Silence and its three bosses (Plateau_deadly_silence) go with their migration; the
        /// Temporal Chamber's missions and The Order Of Velon stay.
        /// </summary>
        [TestMethod]
        public void DeadlySilenceRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            var bosses = PlateauVelonElderBossesV1.All.Select(boss => boss.Id).ToArray();
            var pools = PlateauVelonElderBossesV1.All.Select(boss => boss.Pool).ToArray();

            Assert.IsTrue(Has(PlateauDeadlySilenceV1.MissionId));
            Assert.HasCount(3, world.CreatureEntries.AsNoTracking().Where(row => bosses.Contains(row.Id)).ToArray());
            Assert.HasCount(3, world.SpawnPoolEntries.AsNoTracking().Where(row => pools.Contains(row.Id)).ToArray());

            world.GetService<IMigrator>().Migrate(TemporalChamber);
            Assert.IsFalse(Has(PlateauDeadlySilenceV1.MissionId));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauDeadlySilenceV1.MissionId));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauDeadlySilenceV1.MissionId));
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => bosses.Contains(row.Id)));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => pools.Contains(row.Id)));
            Assert.IsTrue(Has(PlateauFinalAssaultV1.MissionId));
            Assert.IsTrue(Has(PlateauOrderOfVelonV1.MissionId));
        }

        /// <summary>
        /// Ustor Yard's door, Rapp, the yard's Bane and Rapp's three missions (Plateau_ustor_yard) go with their
        /// migration; Going Postal stays.
        /// </summary>
        [TestMethod]
        public void UstorYardRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            var missions = new[] { PlateauSilentButDeadlyV1.MissionId, PlateauInterceptionV1.MissionId, PlateauMonkeyWrenchV1.MissionId };
            var creatures = new[]
            {
                PlateauUstorYardV1.Rapp, PlateauUstorYardV1.Security, PlateauUstorYardV1.Officer,
                PlateauUstorYardV1.PatrolThrax, PlateauUstorYardV1.PatrolKael
            };
            var pools = new[] { PlateauUstorYardV1.RappPool, PlateauUstorYardV1.OfficerPool };
            var links = new[] { PlateauUstorYardV1.IntoUstor, PlateauUstorYardV1.OutOfUstor };

            Assert.IsTrue(missions.All(Has));
            Assert.HasCount(5, world.CreatureEntries.AsNoTracking().Where(row => creatures.Contains(row.Id)).ToArray());
            Assert.HasCount(2, world.SpawnPoolEntries.AsNoTracking().Where(row => pools.Contains(row.Id)).ToArray());
            Assert.HasCount(2, world.MapLinkEntries.AsNoTracking().Where(row => links.Contains(row.Id)).ToArray());

            world.GetService<IMigrator>().Migrate(InfectedForeanAttack);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => creatures.Contains(row.Id)));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => pools.Contains(row.Id)));
            Assert.IsFalse(world.MapLinkEntries.AsNoTracking().Any(row => links.Contains(row.Id)));
            Assert.IsTrue(Has(PlateauGoingPostalV1.MissionId));
        }

        /// <summary>
        /// The missions' sealed force fields (world_destructible, sealed, Bane side, a field class, on the mission's squad map)
        /// and the TakeDownField actions naming them on the transition that brings them down; rolled back to before them, the
        /// rows go with the missions' area migrations.
        /// </summary>
        [TestMethod]
        public void TheMissionForceFieldsAreSealedTakenDownByTheirTransitionsAndRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var lots = FieldLots();

            foreach (var lot in lots)
            {
                foreach (var (id, objective, transition) in lot.Fields)
                {
                    var row = world.WorldDestructibleEntries.AsNoTracking().SingleOrDefault(entry => entry.Id == id);
                    Assert.IsNotNull(row, $"{lot.Mission}: field {id}");
                    Assert.IsTrue(row.Sealed, $"{lot.Mission}: field {id} sealed");
                    Assert.AreEqual(lot.Map, row.MapContextId, $"{lot.Mission}: field {id} map");
                    Assert.AreEqual(lot.Class, row.ClassId, $"{lot.Mission}: field {id} class");
                    Assert.AreEqual(WorldDestructibleEntry.SideBane, row.Side, $"{lot.Mission}: field {id} side");
                    Assert.IsTrue(Rasa.Managers.SquadInstancePolicies.IsListedSquadMap(row.MapContextId), $"{lot.Mission}: a squad map");

                    var actions = world.MissionActionEntries.AsNoTracking().Where(entry => entry.MissionId == lot.Mission &&
                        entry.ContentRevision == PlateauMissionDataV1.Revision && entry.Kind == MissionActionKind.TakeDownField).ToList();
                    var action = actions.SingleOrDefault(entry =>
                        System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Scenes.CharacterIntent>(entry.ItemIntentJson,
                            Rasa.Missions.Content.MissionContentCodec.Options) is Rasa.Missions.Scenes.TakeDownFieldIntent takeDown &&
                        takeDown.DestructibleId == id);
                    Assert.IsNotNull(action, $"{lot.Mission}: a TakeDownField for {id}");
                    Assert.AreEqual((objective, transition), (action.ObjectiveId, action.TransitionId), $"{lot.Mission}: {id}'s transition");
                }
            }

            Assert.IsFalse(world.WorldDestructibleEntries.AsNoTracking().Any(entry => entry.Id == PlateauSilentButDeadlyFieldsV1.Retired),
                "1187: no field on the hospital's gate");
            Assert.AreEqual(2, world.MissionActionEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauSilentButDeadlyV1.MissionId &&
                entry.Kind == MissionActionKind.TakeDownField), "1187: two fields taken down");

            world.GetService<IMigrator>().Migrate("20270921000100_Take_down_field_action");
            foreach (var lot in lots)
            {
                var ids = lot.Fields.Select(field => field.Id).ToArray();
                Assert.IsFalse(world.WorldDestructibleEntries.AsNoTracking().Any(entry => ids.Contains(entry.Id)), $"{lot.Mission}: fields gone");
                Assert.IsFalse(world.MissionActionEntries.AsNoTracking().Any(entry => entry.MissionId == lot.Mission &&
                    entry.Kind == MissionActionKind.TakeDownField), $"{lot.Mission}: actions gone");
                Assert.IsFalse(world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == lot.Mission &&
                    entry.ContentRevision == PlateauMissionDataV1.Revision), $"{lot.Mission} goes with its area migration");
            }
        }

        /// <summary>Each mission's force fields: its map, the field class, and each row with the objective and transition that take it down.</summary>
        private static (uint Mission, uint Map, uint Class, (uint Id, uint Objective, uint Transition)[] Fields)[] FieldLots() => new[]
        {
            (PlateauSilentButDeadlyV1.MissionId, PlateauUstorYardV1.MapContextId, PlateauSilentButDeadlyFieldsV1.FieldClass,
                PlateauSilentButDeadlyFieldsV1.Fields.Select(field => (field.Id, PlateauSilentButDeadlyV1.Computer, 1U)).ToArray()),
            (PlateauFinalAssaultV1.MissionId, PlateauTemporalChamberNpcsV1.MapContextId, PlateauFinalAssaultFieldV1.FieldClass,
                new[] { (PlateauFinalAssaultFieldV1.Field, PlateauFinalAssaultV1.Tally, 1U) }),
            (PlateauSelfPreservationV1.MissionId, PlateauMaligoBaseV1.MapContextId, PlateauSelfPreservationFieldsV1.FieldClass,
                PlateauSelfPreservationFieldsV1.Fields.Select((field, index) => (field.Id, PlateauSelfPreservationV1.Switches[index].Objective, 1U)).ToArray()),
        };

        /// <summary>
        /// Velon Hollow's Bane, its officers and Mox, Markin, Hundra's package and the two missions
        /// (Plateau_velon_hollow_bane) go with their migration, and the outpost's three pools get their Howling Maw
        /// creatures back; Deadly Silence and Ustor Yard stay.
        /// </summary>
        [TestMethod]
        public void VelonHollowBaneRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            uint Slot(uint pool, int slot)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return slot switch
                {
                    1 => row.Creature1Id, 2 => row.Creature2Id, 3 => row.Creature3Id,
                    4 => row.Creature4Id, 5 => row.Creature5Id, _ => row.Creature6Id
                };
            }
            var missions = new[] { PlateauAncientMysteriesV1.MissionId, PlateauMountainSpringToxinsV1.MissionId };
            var creatures = new[]
            {
                PlateauVelonHollowBaneV1.Soldier, PlateauVelonHollowBaneV1.Grenadier, PlateauVelonHollowBaneV1.Miasma,
                PlateauVelonHollowBaneV1.Officer, PlateauVelonHollowBaneV1.MoxBoss, PlateauVelonHollowBaneV1.Commander,
                PlateauVelonHollowBaneV1.Markin
            };
            var pools = new[]
            {
                PlateauVelonHollowBaneV1.ControlRoomPool, PlateauVelonHollowBaneV1.TunnelPool, PlateauVelonHollowBaneV1.MoxPool,
                PlateauVelonHollowBaneV1.MarkinPool
            };
            var packages = new[] { PlateauVelonHollowBaneV1.Hundra, PlateauVelonHollowBaneV1.Markin };

            Assert.IsTrue(missions.All(Has));
            Assert.HasCount(7, world.CreatureEntries.AsNoTracking().Where(row => creatures.Contains(row.Id)).ToArray());
            Assert.HasCount(4, world.SpawnPoolEntries.AsNoTracking().Where(row => pools.Contains(row.Id)).ToArray());
            Assert.HasCount(2, world.NpcPackageEntries.AsNoTracking().Where(row => packages.Contains(row.Id)).ToArray());
            foreach (var (pool, slot, _, replacement) in PlateauVelonHollowBaneV1.Outpost)
                Assert.AreEqual(replacement, Slot(pool, slot), $"pool {pool} slot {slot}");

            world.GetService<IMigrator>().Migrate(DeadlySilence);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionAreaEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => creatures.Contains(row.Id)));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => pools.Contains(row.Id)));
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => packages.Contains(row.Id)));
            foreach (var (pool, slot, old, _) in PlateauVelonHollowBaneV1.Outpost)
                Assert.AreEqual(old, Slot(pool, slot), $"pool {pool} slot {slot} back");
            Assert.IsTrue(Has(PlateauDeadlySilenceV1.MissionId));
            Assert.IsTrue(Has(PlateauMonkeyWrenchV1.MissionId));
        }

        /// <summary>The Better Half, Quarsh and Tala (Plateau_better_half) go with their migration; Velon Hollow's Bane stays.</summary>
        [TestMethod]
        public void BetterHalfRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            var creatures = new[] { PlateauBetterHalfV1.Quarsh, PlateauBetterHalfV1.Tala };

            Assert.IsTrue(Has(PlateauBetterHalfV1.MissionId));
            var rows = world.CreatureEntries.AsNoTracking().Where(row => creatures.Contains(row.Id)).ToDictionary(row => row.Id);
            Assert.HasCount(2, rows);
            Assert.AreEqual(3U, rows[PlateauBetterHalfV1.Quarsh].Faction, "Quarsh neutral: no aggro until he is hit");
            Assert.AreEqual(1U, rows[PlateauBetterHalfV1.Tala].Faction);
            Assert.AreEqual(0U, rows[PlateauBetterHalfV1.Tala].RunSpeed, "Tala does not stroll");
            Assert.AreEqual(4U, rows[PlateauBetterHalfV1.Tala].WalkSpeed, "over her route's speed: she walks out");
            world.GetService<IMigrator>().Migrate(VelonHollowBane);
            Assert.IsFalse(Has(PlateauBetterHalfV1.MissionId));
            Assert.IsFalse(world.MissionAreaEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauBetterHalfV1.MissionId));
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => creatures.Contains(row.Id)));
            Assert.IsTrue(Has(PlateauMountainSpringToxinsV1.MissionId));
            Assert.IsTrue(world.CreatureEntries.AsNoTracking().Any(row => row.Id == PlateauVelonHollowBaneV1.Officer));
        }

        /// <summary>
        /// Maligo Base's NPCs and officers, the Ustor Bridge links, the local teleporters and their links, and Showdown, Coup
        /// and One Way Trip (Plateau_maligo_base) go with their migration; Ustor Yard stays.
        /// </summary>
        [TestMethod]
        public void MaligoBaseRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var missions = new[] { PlateauShowdownV1.MissionId, PlateauCoupV1.MissionId, PlateauOneWayTripV1.MissionId };
            var ids = $"BETWEEN {PlateauMaligoBaseV1.DownedPrisoner} AND {PlateauMaligoBaseV1.Kythura}";
            var pads = $"({PlateauMaligoBaseV1.Level1Pad}, {PlateauMaligoBaseV1.Level2Pad}, {PlateauMaligoBaseV1.ChamberPad})";
            var links = new[] { PlateauMaligoBaseV1.OntoBridge, PlateauMaligoBaseV1.BackIntoUstor };

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual(8, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id {ids}"));
            Assert.AreEqual(5, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id {ids}"));
            Assert.AreEqual(4, Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id {ids}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM spawnpool_pose WHERE id {ids}"));
            Assert.AreEqual(3, Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN {pads}"));
            Assert.AreEqual(3, Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN {pads}"));
            Assert.HasCount(2, world.MapLinkEntries.AsNoTracking().Where(row => links.Contains(row.Id)).ToArray());

            world.GetService<IMigrator>().Migrate(UstorYard);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionAreaEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            foreach (var table in new[] { "creature", "creature_appearance", "creature_stat", "spawnpool", "npc_package", "spawnpool_pose" })
                Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id {ids}"), table);
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN {pads}"));
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN {pads}"));
            Assert.IsFalse(world.MapLinkEntries.AsNoTracking().Any(row => links.Contains(row.Id)));
            Assert.IsTrue(Has(PlateauMonkeyWrenchV1.MissionId));
        }

        /// <summary>
        /// Colonel "Snake" Washington and South Of The Border, New Orders From The General and Self Preservation
        /// (Plateau_bosley_followups) go with their migration; Maligo Base's missions and Amee Corman stay.
        /// </summary>
        [TestMethod]
        public void BosleysFollowUpsRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var missions = new[] { PlateauSouthOfTheBorderV1.MissionId, PlateauNewOrdersFromTheGeneralV1.MissionId, PlateauSelfPreservationV1.MissionId };
            var snake = PlateauSnakePitV1.Snake;

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {snake}"));
            Assert.AreEqual(6, Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {snake}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {snake} AND package_id = {PlateauSnakePitV1.SnakePackage}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {snake} AND map_context_id = {PlateauSnakePitV1.MapContextId}"));

            world.GetService<IMigrator>().Migrate(MaligoBase);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            foreach (var table in new[] { "creature", "creature_appearance", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id = {snake}"), table);
            Assert.IsTrue(Has(PlateauOneWayTripV1.MissionId));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {PlateauNewOrdersFromTheGeneralV1.Amee}"), "Amee stays");
        }

        /// <summary>
        /// Colonel Fonseca, the Foreas Base past templates and actions, the pads on Veryin's left and north of HQ, and
        /// Foreas Base Past and Send a Message (Plateau_temporal_past) go with their migration; Veryin, Hudson and the
        /// Bosley follow-ups stay.
        /// </summary>
        [TestMethod]
        public void TemporalPastRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var missions = new[] { PlateauForeasBasePastV1.MissionId, PlateauSendAMessageV1.MissionId };
            var ids = $"BETWEEN {PlateauForeasBasePastNpcsV1.Fonseca} AND {PlateauForeasBasePastNpcsV1.SpecialForces}";
            var actions = $"BETWEEN {PlateauForeasBasePastNpcsV1.DefenderPistol} AND {PlateauForeasBasePastNpcsV1.StriderLaser}";
            var pads = $"({PlateauForeasBasePastNpcsV1.ChamberPad}, {PlateauForeasBasePastNpcsV1.PastPad})";

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual(7, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id {ids}"));
            Assert.AreEqual(6, Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id {ids}"), "all but Fonseca");
            Assert.AreEqual(3, Count($"SELECT COUNT(*) AS Value FROM creature_action WHERE id {actions}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {PlateauForeasBasePastNpcsV1.Fonseca} " +
                $"AND package_id = {PlateauForeasBasePastNpcsV1.FonsecaPackage}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {PlateauForeasBasePastNpcsV1.FonsecaPool} " +
                $"AND map_context_id = {PlateauForeasBasePastNpcsV1.MapContextId}"));
            Assert.IsTrue(Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {PlateauForeasBasePastNpcsV1.Fonseca}") > 0,
                "Fonseca wears the officer uniform");
            Assert.AreEqual(2, Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN {pads}"));
            Assert.AreEqual(2, Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN {pads}"));

            world.GetService<IMigrator>().Migrate(BetterHalf);
            Assert.IsFalse(missions.Any(Has));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => missions.Contains(entry.MissionId)));
            foreach (var table in new[] { "creature", "creature_appearance", "creature_stat", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id {ids}"), table);
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM creature_action WHERE id {actions}"));
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN {pads}"));
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN {pads}"));
            Assert.IsTrue(Has(PlateauFinalAssaultV1.MissionId));
            Assert.IsTrue(Has(PlateauSelfPreservationV1.MissionId));
            Assert.AreEqual(2, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({PlateauTemporalChamberNpcsV1.VeryinPool}, " +
                $"{PlateauTemporalChamberNpcsV1.HudsonPool})"), "Veryin and Hudson stay");
        }

        /// <summary>
        /// The Maligo Base Prisoner, the patrol's template and Out of Thin Air (Plateau_out_of_thin_air) go with their
        /// migration; Ustor Yard's chain and the Bosley follow-ups stay.
        /// </summary>
        [TestMethod]
        public void OutOfThinAirRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var ids = $"IN ({PlateauMaligoPrisonerV1.Prisoner}, {PlateauMaligoPrisonerV1.Patrol})";

            Assert.IsTrue(Has(PlateauOutOfThinAirV1.MissionId));
            Assert.AreEqual(2, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id {ids}"));
            Assert.AreEqual(2, Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id {ids}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id = {PlateauMaligoPrisonerV1.Prisoner} " +
                $"AND package_id = {PlateauMaligoPrisonerV1.PrisonerPackage}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {PlateauMaligoPrisonerV1.PrisonerPool} " +
                $"AND map_context_id = {PlateauMaligoPrisonerV1.MapContextId}"));
            Assert.IsTrue(Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {PlateauMaligoPrisonerV1.Prisoner}") > 0,
                "she wears the prisoners' clothes");

            world.GetService<IMigrator>().Migrate(BosleyFollowups);
            Assert.IsFalse(Has(PlateauOutOfThinAirV1.MissionId));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauOutOfThinAirV1.MissionId));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauOutOfThinAirV1.MissionId));
            foreach (var table in new[] { "creature", "creature_appearance", "creature_stat", "spawnpool", "npc_package" })
                Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id {ids}"), table);
            Assert.IsTrue(Has(PlateauMonkeyWrenchV1.MissionId));
            Assert.IsTrue(Has(PlateauSelfPreservationV1.MissionId));
        }

        /// <summary>Weapons Trafficking (Plateau_weapons_trafficking) goes with its migration; Out of Thin Air and New Orders stay.</summary>
        [TestMethod]
        public void WeaponsTraffickingRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);

            Assert.IsTrue(Has(PlateauWeaponsTraffickingV1.MissionId));
            Assert.AreEqual(6, world.MissionObjectiveDefinitionEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauWeaponsTraffickingV1.MissionId));

            world.GetService<IMigrator>().Migrate(OutOfThinAir);
            Assert.IsFalse(Has(PlateauWeaponsTraffickingV1.MissionId));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauWeaponsTraffickingV1.MissionId));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauWeaponsTraffickingV1.MissionId));
            Assert.IsTrue(Has(PlateauOutOfThinAirV1.MissionId));
            Assert.IsTrue(Has(PlateauNewOrdersFromTheGeneralV1.MissionId));
        }

        /// <summary>Weapons Trafficking's footlocker markers (PlateauWeaponsTraffickingMarkersV1, in Plateau_weapons_trafficking): all six objectives marked.</summary>
        [TestMethod]
        public void WeaponsTraffickingMarksTheArmoryDoorAndItsFootlockers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            uint[] Marked() => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == PlateauWeaponsTraffickingV1.MissionId && entry.ContentRevision == PlateauMissionDataV1.Revision)
                .Select(entry => entry.ObjectiveId).ToList().OrderBy(id => id).ToArray();

            CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4, 5, 6 }, Marked());
            Assert.AreEqual(6, world.MissionObjectiveDefinitionEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauWeaponsTraffickingV1.MissionId));
        }

        /// <summary>
        /// To-fix #16 (PlateauContactsOutOfAggroV1, in Plateau_maligo_base): Rapp, the Downed Prisoner and Altera and the
        /// markers on Rapp and Altera stand at PlateauContactsOutOfAggroV1's places.
        /// </summary>
        [TestMethod]
        public void TheContactsStandOutOfTheBanesAggroRange()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            (double X, double Y, double Z) Pool(uint id)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                return (row.PosX, row.PosY, row.PosZ);
            }
            (double X, double Y, double Z) Marker(uint mission, uint indicator)
            {
                var row = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == mission &&
                    entry.ContentRevision == PlateauMissionDataV1.Revision && entry.IndicatorId == Rasa.Structures.MissionIndicator.UnnamedFrom + indicator);
                return (row.PosX, row.PosY, row.PosZ);
            }
            void AssertAt((double X, double Y, double Z) expected, (double X, double Y, double Z) actual, string what)
            {
                Assert.AreEqual(expected.X, actual.X, 0.01, what);
                Assert.AreEqual(expected.Y, actual.Y, 0.01, what);
                Assert.AreEqual(expected.Z, actual.Z, 0.01, what);
            }
            void AssertPlaces((double X, double Y, double Z) rapp, (double X, double Y, double Z) prisoner, (double X, double Y, double Z) altera)
            {
                AssertAt(rapp, Pool(PlateauUstorYardV1.RappPool), "Rapp");
                AssertAt(prisoner, Pool(PlateauMaligoBaseV1.DownedPrisoner), "the Downed Prisoner");
                AssertAt(altera, Pool(PlateauMaligoBaseV1.Altera), "Altera");
                foreach (var (mission, indicator) in PlateauContactsOutOfAggroV1.RappMarkers)
                    AssertAt(rapp, Marker(mission, indicator), $"{mission} marker {indicator} on Rapp");
                foreach (var (mission, indicator) in PlateauContactsOutOfAggroV1.AlteraMarkers)
                    AssertAt(altera, Marker(mission, indicator), $"{mission} marker {indicator} on Altera");
            }

            AssertPlaces(PlateauContactsOutOfAggroV1.RappAt, PlateauContactsOutOfAggroV1.DownedPrisonerAt, PlateauContactsOutOfAggroV1.AlteraAt);
        }

        /// <summary>
        /// To-fix #18: Zyrutha's and Kythura's pools respawn after 600 tenths of a second (PlateauMaligoOfficerSquadRespawnV1,
        /// in Plateau_maligo_base); the scenes of 876 and 877 name the pools as public-spawn actors.
        /// </summary>
        [TestMethod]
        public void TheSoyuzDebrisTakeTheUseArgumentTheClientSends()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            string Binding() => world.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {PlateauSoyuzSalvageV1.MissionId}").Single();
            int Count(string json, uint argument) => json.Split($"\"actionArgId\":{argument}").Length - 1;

            // To-fix #48: the client sends 1 for classes 29783/29784 (generated/client/usabledata).
            Assert.AreEqual(1u, PlateauSoyuzSalvageV1.UseArgument);
            Assert.AreEqual(3, Count(Binding(), 1), "each debris piece");
            Assert.AreEqual(0, Count(Binding(), 3));

            world.GetService<IMigrator>().Migrate("20271006000000_Pools_turpis_refinery");
            Assert.AreEqual(3, Count(Binding(), 3), "the binding as the earlier migration wrote it");
            world.GetService<IMigrator>().Migrate();
            Assert.AreEqual(3, Count(Binding(), 1), "Plateau_soyuz_salvage_use_argument puts 1 in it");
            Assert.AreEqual(0, Count(Binding(), 3));
        }

        [TestMethod]
        public void MaligoOfficersRespawnAfterAMinute()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            uint[] Respawns() => new[] { PlateauMaligoBaseV1.Zyrutha, PlateauMaligoBaseV1.Kythura }
                .Select(pool => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).RespawnTime).ToArray();
            bool[] Named() => new[]
                {
                    (PlateauShowdownV1.MissionId, PlateauMaligoOfficerSquadRespawnV1.ZyruthaRole),
                    (PlateauOneWayTripV1.MissionId, PlateauMaligoOfficerSquadRespawnV1.KythuraRole)
                }
                .Select(entry => world.Database.SqlQueryRaw<string>(
                    $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {entry.Item1}").Single()
                    .Contains($"\"{entry.Item2}\"")).ToArray();

            CollectionAssert.AreEqual(new uint[] { 600, 600 }, Respawns());
            CollectionAssert.AreEqual(new[] { true, true }, Named());
        }

        /// <summary>
        /// To-fix #23: each Plateau hostile pool's respawn is its data class's seconds in spawnpool.respown_time's tenths
        /// (300 s is 3000), as each area's migration writes it. Those are all our pools not written 20 but the Maligo officers (600 tenths already), and the mission
        /// NPCs' pools stay at 20.
        /// </summary>
        [TestMethod]
        public void PlateauPoolsRespawnInTenthsOfASecond()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            uint Respawn(uint pool) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).RespawnTime;
            uint[] Ours() => world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.Id >= 750000 && entry.Id < 760000).Select(entry => entry.Id).ToArray();
            var officers = new[] { PlateauMaligoBaseV1.Zyrutha, PlateauMaligoBaseV1.Kythura };
            var pools = PlateauPoolRespawnTenthsV1.Pools;

            Assert.HasCount(pools.Count, pools.Select(pool => pool.Pool).Distinct().ToArray(), "no pool twice");
            Assert.AreEqual(3000u, Respawn(PlateauTombMiasmasV1.Pools[0].Id), "a Tomb Miasma pool: 300 s");
            Assert.AreEqual(6000u, Respawn(PlateauPrototypeJuggernautV1.Pool), "the Prototype Juggernaut: 600 s");
            foreach (var (pool, seconds, what) in pools)
                Assert.AreEqual(seconds * 10, Respawn(pool), $"{pool} {what}");
            foreach (var pool in officers)
                Assert.AreEqual(PlateauMaligoOfficerSquadRespawnV1.OfficerRespawnTenths, Respawn(pool), $"{pool} officer, not again");
            var npcs = Ours().Except(pools.Select(pool => pool.Pool)).Except(officers).ToArray();
            Assert.IsNotEmpty(npcs);
            foreach (var pool in npcs)
                Assert.AreEqual(20u, Respawn(pool), $"{pool}: a mission NPC's pool stays at 20");
        }

        /// <summary>To-fix #20 (PlateauPoolMarkersV1, in Plateau_holy_vengeance): Holy Vengeance's outpost markers are 30, 30 and 40 m.</summary>
        [TestMethod]
        public void HolyVengeancePoolMarkersCoverTheirPools()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            double[] Radii() => PlateauPoolMarkersV1.HolyVengeance
                .Select(marker => world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == PlateauHolyVengeanceV1.MissionId &&
                    entry.ContentRevision == PlateauMissionDataV1.Revision &&
                    entry.IndicatorId == Rasa.Structures.MissionIndicator.UnnamedFrom + marker.Indicator).Radius).ToArray();

            CollectionAssert.AreEqual(new double[] { 30, 30, 40 }, Radii());
        }

        /// <summary>These Old Bones (Plateau_these_old_bones) goes with its migration, Hathen's escort copy and Algona too; Temporal Past stays.</summary>
        [TestMethod]
        public void TheseOldBonesRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            bool Creature(uint id) => world.CreatureEntries.AsNoTracking().Any(entry => entry.Id == id);

            Assert.IsTrue(Has(PlateauTheseOldBonesV1.MissionId));
            Assert.AreEqual(7, world.MissionObjectiveDefinitionEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauTheseOldBonesV1.MissionId));
            Assert.IsTrue(Creature(PlateauTheseOldBonesV1.HathenEscort) && Creature(PlateauTheseOldBonesV1.Algona));

            world.GetService<IMigrator>().Migrate(TemporalPast);
            Assert.IsFalse(Has(PlateauTheseOldBonesV1.MissionId));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauTheseOldBonesV1.MissionId));
            Assert.IsFalse(Creature(PlateauTheseOldBonesV1.HathenEscort) || Creature(PlateauTheseOldBonesV1.Algona));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(entry => entry.Id == PlateauTheseOldBonesV1.AlgonaPool));
            Assert.IsTrue(Has(PlateauForeasBasePastV1.MissionId));
        }

        /// <summary>Holy Vengeance (Plateau_holy_vengeance) goes with its migration, its followers and crate too; These Old Bones stays.</summary>
        [TestMethod]
        public void HolyVengeanceRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);
            bool Creature(uint id) => world.CreatureEntries.AsNoTracking().Any(entry => entry.Id == id);

            Assert.IsTrue(Has(PlateauHolyVengeanceV1.MissionId));
            Assert.AreEqual(4, world.MissionObjectiveDefinitionEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauHolyVengeanceV1.MissionId));
            Assert.IsTrue(Creature(PlateauHolyVengeanceV1.AlgonaEscort) && Creature(PlateauHolyVengeanceV1.Warrior));

            world.GetService<IMigrator>().Migrate(TheseOldBones);
            Assert.IsFalse(Has(PlateauHolyVengeanceV1.MissionId));
            Assert.IsFalse(world.MissionTriggerEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauHolyVengeanceV1.MissionId));
            Assert.IsFalse(Creature(PlateauHolyVengeanceV1.AlgonaEscort) || Creature(PlateauHolyVengeanceV1.Warrior));
            Assert.IsTrue(Has(PlateauTheseOldBonesV1.MissionId));
            Assert.IsTrue(Creature(PlateauTheseOldBonesV1.Algona));
        }

        /// <summary>Retreads to Retreads (Plateau_retreads_to_retreads) goes with its migration; Holy Vengeance stays.</summary>
        [TestMethod]
        public void RetreadsToRetreadsRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision);

            Assert.IsTrue(Has(PlateauRetreadsToRetreadsV1.MissionId));
            Assert.AreEqual(4, world.MissionRewardItemEntries.AsNoTracking().Count(entry => entry.MissionId == PlateauRetreadsToRetreadsV1.MissionId));

            world.GetService<IMigrator>().Migrate(NpcSquadLeaderBystander);
            Assert.IsFalse(Has(PlateauRetreadsToRetreadsV1.MissionId));
            Assert.IsFalse(world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauRetreadsToRetreadsV1.MissionId));
            Assert.IsFalse(world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == PlateauRetreadsToRetreadsV1.MissionId));
            Assert.IsTrue(Has(PlateauHolyVengeanceV1.MissionId));
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
