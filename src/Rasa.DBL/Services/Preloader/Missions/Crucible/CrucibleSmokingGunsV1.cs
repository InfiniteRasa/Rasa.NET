using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Smoking Guns (native mission 1148), Ligo Crucible: Top Khana Grupa Munit gives the player three black market arms
    /// shipments for Corporal Fiorella at Outpost Intrepid, Labbna Sigal Haggus in the Labbna Quarter and Sergeant Melching
    /// at the Awol Camp.
    ///
    /// Native: texts 8404-8407, 8408 (missionconversation): log "deliver black market Arms Shipments to Outpost Intrepid, the
    /// Labbna Syndicate, and the Awol Camp. If you sucessfully deliver all three, he will present you with a Larai Token." (the
    /// client's text; the turn-in "here is your token" is the Top Khana's), turn-in "We are hearing reports that all three
    /// shipments have been delivered"; objectives (missionobjective, no counters) 1 "Deliver Outpost Intrepid Shipment"
    /// 9346/9347 (Fiorella), 2 "Deliver Labbna Shipment" 9349/9350 (Haggus), 3 "Deliver Awol Shipment" 9352/9353 (Melching);
    /// objectiveconversation (1148,1) package 1296 (Fiorella) 9348, (1148,2) 1232 (Haggus) 9351, (1148,3) 1295 (Melching)
    /// variant 177 9355 ("the [..] that ratted out Deridre"), variant 178 9354 ("the one that helped Deirdre"), variant 203
    /// 9356 ("Who the hell are you?"). Item classes 24953 Outpost Intrepid, 24952 Labbna, 24954 Awol Arms Shipment (templates
    /// 50358, 50357, 50359); 24751 Top Khana Syndicate Token (50316).
    ///
    /// Sourced: TaRapedia Smoking Guns: giver Top Khana Grupa Munit (Staal Detention Center); requires The Boss Of All Bosses;
    /// 75,000 XP, 0 credits; the Top Khana Syndicate Token, then Vitalius Bio Armor Gloves (icon 3) or CryoGen Graviton Armor
    /// Vest (4); the three receivers. TR MMO DB quest list: level 35, 75000, 0.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Munit. The three shipments at the accept, the three deliveries in any order, each receiver takes his.
    ///    Fiorella and Haggus moved out of the placeholder clusters (CrucibleGrupaChiefsNpcsV1), Melching where upstream has
    ///    him at the Awol Camp.
    ///  - Melching's three texts by what the player did in Calendar Girl (1144, not built yet): its flag
    ///    <see cref="CalendarGirlFlag"/> 1 "ratted out" (variant 177), 2 "helped" (178), unset or any other value "neither"
    ///    (203). 1144 (set 6) sets the flag; until then every player hears 203.
    ///  - The token a fixed reward; the two items the lowest Uncommon templates of each kind and icon number in the band of
    ///    level 35: Bio V03 Gloves 19524, Graviton V04 Vest 25357; one to choose. A marker at each receiver.
    /// </summary>
    public static class CrucibleSmokingGunsV1
    {
        public const uint MissionId = 1148;
        public const uint Level = 35;
        public const uint Munit = CrucibleOutpostIntrepidNpcsV1.Munit;
        public const uint Intrepid = 1, Labbna = 2, Awol = 3;
        public const double MarkerRadius = 8;

        /// <summary>Calendar Girl's outcome (set 6 sets it, as MarshesHighwayRobberyV1.ChoiceFlag): 1 ratted out, 2 helped.</summary>
        public const uint CalendarGirlFlag = 1144001, RattedOut = 1, Helped = 2;
        public const uint RattedOutVariant = 177, HelpedVariant = 178, NeitherVariant = 203;   // objectiveconversation 1148,3
        public const uint NeitherTalk = 1, RattedOutTalk = 2, HelpedTalk = 3;                  // objective 3's transitions

        /// <summary>The three deliveries: objective, name, body, the receiver's pool, its package, the shipment's key, template, class.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Npc, uint Package, string Key, uint Template, uint Class)[] Deliveries =
        {
            (Intrepid, 9346, 9347, CrucibleGrupaChiefsNpcsV1.Fiorella, CrucibleGrupaChiefsNpcsV1.FiorellaPackage, "intrepid-arms-shipment", 50358, 24953),
            (Labbna, 9349, 9350, CrucibleGrupaChiefsNpcsV1.Haggus, CrucibleGrupaChiefsNpcsV1.HaggusPackage, "labbna-arms-shipment", 50357, 24952),
            (Awol, 9352, 9353, CrucibleGrupaChiefsNpcsV1.Melching, CrucibleGrupaChiefsNpcsV1.MelchingPackage, "awol-arms-shipment", 50359, 24954)
        };

        public const uint Experience = 75000;
        public const uint Credits = 0;
        public const uint TopKhanaToken = 50316, TopKhanaTokenClass = 24751;
        public const uint BioGloves = 19524;        // Armor_T3_Bio_V03_UNC_Gloves_35_to_39
        public const uint GravitonVest = 25357;     // Armor_T3_Graviton_V04_UNC_Vest_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Smoking Guns", 8404, Munit, Munit, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonVest }, new[] { TopKhanaToken });
            mission.Prerequisite(CrucibleBossOfAllBossesV1.MissionId);

            for (var index = 0; index < Deliveries.Length; index++)
            {
                var (objective, name, body, npc, package, key, _, _) = Deliveries[index];
                mission.Objective(objective, name, body, (uint)index, MissionObjectiveState.Incomplete);
                var at = CrucibleGrupaChiefsNpcsV1.At(npc);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
                if (objective == Awol)
                {
                    foreach (var (transition, variant) in new[] { (NeitherTalk, NeitherVariant), (RattedOutTalk, RattedOutVariant), (HelpedTalk, HelpedVariant) })
                    {
                        mission.Transition(objective, transition, transition);
                        mission.Conversation(objective, package, variant, transition);
                        Deliver(mission, objective, key, transition, index == 0);
                    }
                    continue;
                }
                mission.Transition(objective);
                mission.Conversation(objective, package);
                Deliver(mission, objective, key, 1, index == 0);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1148",
                "Native1 'Deliver Outpost Intrepid Shipment' 9346/9347, 2 'Deliver Labbna Shipment' 9349/9350, 3 'Deliver Awol " +
                "Shipment' 9352/9353, no counters; objectiveconversation 1148,1 package1296 9348, 1148,2 1232 9351, 1148,3 1295 " +
                "variant177 9355 (ratted out Deirdre), 178 9354 (helped Deirdre), 203 9356 (neither). Texts 8404-8408 (log names a " +
                "Larai Token). Shipments 24953/50358, 24952/50357, 24954/50359; token 24751/50316.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Smoking_Guns",
                "Giver Top Khana Grupa Munit (Staal); requires The Boss Of All Bosses; 75000XP, 0 credits; Top Khana Syndicate " +
                "Token, then Vitalius Bio Armor Gloves 3 or CryoGen Graviton Armor Vest 4; Corporal Fiorella (Outpost Intrepid), " +
                "Sigal Haggus (Labbna Quarter), Sergeant Melching (AWOL Camp). TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleSmokingGunsV1",
                "Receiver Munit. The three shipments at the accept, delivered in any order. Melching's text by flag 1144001 (1 " +
                "ratted out 177, 2 helped 178, else 203; set by 1144, not built). Fiorella and Haggus moved. Token fixed; items " +
                "19524, 25357, one to choose. A marker at each receiver.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The three shipments at the accept; Melching's three texts gated by Calendar Girl's flag.</summary>
        public static MissionSceneDefinition Scene()
        {
            var items = new List<MissionItemBinding>();
            var issued = new List<CharacterIntent>();
            foreach (var (_, _, _, _, _, key, template, _) in Deliveries)
            {
                items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                issued.Add(new IssueMissionItemIntent($"issue-{key}", MissionId, key, template, 1));
            }
            var package = CrucibleGrupaChiefsNpcsV1.MelchingPackage;
            var rattedOut = new FlagRequirement(CalendarGirlFlag, RattedOut);
            var helped = new FlagRequirement(CalendarGirlFlag, Helped);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = items,
                AcceptanceItems = issued,
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Awol, package, NeitherVariant, MissionDialogueKind.Completion, transitionId: NeitherTalk,
                        requirement: new NotRequirement(new AnyRequirement(new MissionRequirement[] { rattedOut, helped }))),
                    new(Awol, package, RattedOutVariant, MissionDialogueKind.Completion, transitionId: RattedOutTalk, requirement: rattedOut),
                    new(Awol, package, HelpedVariant, MissionDialogueKind.Completion, transitionId: HelpedTalk, requirement: helped)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        /// <summary>The receiver takes his shipment; the first delivery's transition also names the reward.</summary>
        private static void Deliver(CrucibleMissionDataV1 mission, uint objective, string key, uint transition, bool reward)
        {
            mission.Action(objective, 1, MissionActionKind.ConsumeMissionItem, transition: transition,
                itemIntent: new ConsumeMissionItemIntent($"{key}-delivered-{transition}", MissionId, key, 1, MissionItemScope.AssignmentIssued));
            if (reward)
                mission.Action(objective, 2, MissionActionKind.GrantReward, reward: 1, transition: transition);
        }
    }
}
