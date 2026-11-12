using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// It Came From Erdas (native mission 1218), Ligo Crucible and Ashen Desert: the Kardash Eloh Obelisk gives the player the
    /// Eloh Memory Capsule from the escape pod; Professor Tiernan hears of it and sends the player to Captain Botha at the
    /// Ashoka Settlement in Ashen Desert. Its follow-up Echoes in the Sand (1736, Ashen Desert) is not built.
    ///
    /// Native: texts 9589-9592, 9593 (missionconversation; 9593 "none"): log "Return to Professor Tiernan at the Eloh Crash site
    /// at Devil's Pitchfork and tell him of your discovery, then deliver the artifact to Captain Botha in Ashen Desert.", offer
    /// (the obelisk) "Inside the escape pod is a glowing artifact: an Eloh Memory Capsule. Take the capsule and return to
    /// Professor Tiernan and tell him of your discovery.", turn-in (Botha) "Professor Tiernan told me you'd be coming. Do you have
    /// the artifact? ..."; objectives (missionobjective) 1 "Tell Tiernan of your discovery." 10543/10544, 2 "Bring the artifact
    /// to Botha." 14819/14820 ("Journey to Ashaka Settlement in Ashen Desert and give the artifact to Captain Botha."), no
    /// counters; objective conversation (1218,1) package 1483 (Tiernan) 10545 ("... Bring it to Captain Botha at Ashaka
    /// Settlement in Ashen Desert ..."), (1218,2) package 1410 (Botha) 14821 "What is it?". Item class 25496
    /// Crucible_Eloh_Memory_Capsule "Eloh Memory Capsule" (template 97496).
    ///
    /// Sourced: TaRapedia It Came From Erdas: giver the Kardash Eloh Obelisk (Eloh Crash Site), reward giver Captain Amandla
    /// Botha (Ashoka Settlement); requires Point Of Impact; follow-up Echoes in the Sand; 85,000 XP, 6,000 credits; Audiodyne
    /// Mech Armor Boots 6 (level 40), Prodigy Graviton Armor Vest 1 (40), Luminar Hazmat Armor Vest 2 (44), Hellstrom Motor
    /// Assist Armor Helmet 4 (38), all purple with mods. TR MMO DB: level 35, "begins when you examine the Kardash Eloh
    /// Obelisk".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The obelisk is no creature, so it cannot give the mission: it is offered by radio when Point Of Impact is turned in
    ///    (MissionRewarded 1217, the obelisk's message) and again on arrival on the Crucible, as Gun Control - Part IV.
    ///  - The capsule issued at the accept ("Take the capsule"); Botha takes it at the turn-in. 1 Tiernan's talk opens 2; 2
    ///    Botha's talk on Ashen Desert (CrucibleKardashAndPrometheusNpcsV1); Botha takes the turn-in.
    ///  - Markers at Tiernan (the Crucible) and at Botha (Ashen Desert).
    ///  - The items: TaRapedia's kinds and icon numbers, the lowest Uncommon template whose band holds each one's TaRapedia
    ///    level, the nearest icon number where the band has none (Mech V04 Boots 33987, Graviton V02 Vest 24086, Hazmat V02
    ///    Vest 20971, MotorAssist V04 Helmet 28940); Uncommon, not purple, as every Crucible reward (no mods), one to choose.
    /// </summary>
    public static class CrucibleItCameFromErdasV1
    {
        public const uint MissionId = 1218;
        public const uint Level = 35;
        public const uint Botha = CrucibleKardashAndPrometheusNpcsV1.Botha;
        public const uint TiernanPackage = CrucibleKardashAndPrometheusNpcsV1.TiernanPackage;
        public const uint BothaPackage = CrucibleKardashAndPrometheusNpcsV1.BothaPackage;
        public const uint TellTiernan = 1, BringToBotha = 2;
        public const double MarkerRadius = 8;

        public const uint CapsuleTemplate = 97496, CapsuleClass = 25496;
        public const string CapsuleKey = "eloh-memory-capsule";

        public const uint Experience = 85000;
        public const uint Credits = 6000;
        public const uint MechBoots = 33987;            // Armor_T3_Mech_V04_UNC_Boots_40_to_44
        public const uint GravitonVest = 24086;         // Armor_T3_Graviton_V02_UNC_Vest_40_to_44
        public const uint HazmatVest = 20971;           // Armor_T2_Hazmat_V02_UNC_Vest_44_to_48
        public const uint MotorAssistHelmet = 28940;    // Armor_T1_MotorAssist_V04_UNC_Helmet_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "It Came From Erdas", 9589, null, Botha, Level);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(CruciblePointOfImpactV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey,
                    CrucibleMissionDataV1.MapContextId));
            mission.Reward(Experience, Credits, new[] { MechBoots, GravitonVest, HazmatVest, MotorAssistHelmet });
            mission.Prerequisite(CruciblePointOfImpactV1.MissionId);

            mission.Objective(TellTiernan, 10543, 10544, 0, MissionObjectiveState.Incomplete);
            mission.Objective(BringToBotha, 14819, 14820, 1, MissionObjectiveState.Inactive);

            // 1: Tiernan hears of the capsule; it opens 2.
            mission.Transition(TellTiernan);
            mission.Conversation(TellTiernan, TiernanPackage);
            CrucibleCalendarGirlV1.Open(mission, TellTiernan, 1, 1, BringToBotha);

            // 2: Botha on Ashen Desert, who takes the capsule at the turn-in.
            mission.Transition(BringToBotha);
            mission.Conversation(BringToBotha, BothaPackage);
            mission.Action(BringToBotha, 1, MissionActionKind.GrantReward, reward: 1);

            var tiernan = CrucibleKardashAndPrometheusNpcsV1.TiernanAt;
            var botha = CrucibleKardashAndPrometheusNpcsV1.BothaAt;
            mission.Indicator(TellTiernan, 1, tiernan.X, tiernan.Y, tiernan.Z, MarkerRadius);
            mission.Indicator(BringToBotha, 2, botha.X, botha.Y, botha.Z, MarkerRadius, CrucibleKardashAndPrometheusNpcsV1.AshenDesert);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1218",
                "Native1 'Tell Tiernan of your discovery.' 10543/10544, 2 'Bring the artifact to Botha.' 14819/14820, no counters; " +
                "objectiveconversation 1218,1 package1483 10545, 1218,2 package1410 14821. Texts 9589-9592, 9593 'none'. Item " +
                "class25496 Eloh Memory Capsule (template97496). Names 9365 Professor Tiernan, 9278 Captain Amandla Botha.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/It_Came_From_Erdas",
                "Giver the Kardash Eloh Obelisk (Eloh Crash Site), reward giver Captain Amandla Botha (Ashoka Settlement); requires " +
                "Point Of Impact; follow-up Echoes in the Sand; 85000XP, 6000 credits; Audiodyne Mech Armor Boots 6 (40), Prodigy " +
                "Graviton Armor Vest 1 (40), Luminar Hazmat Armor Vest 2 (44), Hellstrom Motor Assist Armor Helmet 4 (38), purple " +
                "with mods. TR MMO DB level35, begins at the obelisk.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleItCameFromErdasV1",
                "Offered by radio at 1217's reward and on arrival on the Crucible (the obelisk is no creature). The capsule issued at " +
                "the accept, taken by Botha at the turn-in. 1 Tiernan 770039 (package1483) opens 2; 2 Botha 770040 (package1410, " +
                "copy of Harris) on Ashen Desert 1734 at 378,280.69,-458.5. Items 33987, 24086, 20971, 28940 (Uncommon), one to " +
                "choose. Markers at Tiernan and Botha.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The capsule, issued at the accept and taken at the turn-in.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(CapsuleKey, CapsuleTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        TurnInQuantity: 1)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-eloh-memory-capsule", MissionId, CapsuleKey, CapsuleTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
