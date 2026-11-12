using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Under the Radar - Part II, the overseer version (native mission 1877), Valverde Marshes: the dropship's crew took a
    /// picture of an overseer set down on a building east of Paludos; Lt. Kiang sends the player to take him out and bring
    /// back his troop movement plans.
    ///
    /// Native: texts 19169-19173 (missionconversation): log "Intercept Overseer Nelg and obtain data on the Bane troop
    /// movements in marshes.", offer "To the north, east of Palados the crew took a picture of an overseer being dropshipped
    /// on top of a building ... If you can take him out, I suspect you'll be able to find troop movement plans", turn-in as
    /// 1876's; objective 1 "Intercept Overseer Nelg and obtain data on the Bane troop movements in Marshes." 19685/19686, no
    /// counter (missionobjective); no objective conversation. Name 10339 Overseer Nelg. Item Bane Troop Data class 28642
    /// (World DB template 119003).
    ///
    /// Sourced: TaRapedia Under the Radar - Part II (one page with 1876; its briefing is 1877's, its history to 2008-04 has
    /// 1877's objective): giver and receiver Lt. Kiang; requires Under the Radar; level 37; 140,000 XP, 7,400 credits; one of
    /// four Armor Boots lvl 35. Ten Ton Hammer Eastern Marshes: "the outpost where Overseer Nelg lives at 170,224,55". Client
    /// item templates 121245-121248: Bio, Graviton, Stealth, Mech Boots 35-39, the second such block after 1876's.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - At the accept the mission sets the player's own Nelg (MarshesSentryTowerNpcsV1, level 38, 12000 hit points, the
    ///    triage) down in his post at Ten Ton Hammer's point; he attacks. No dropship is shown (the server's arrival pools are
    ///    public). His corpse holds the troop data for the player while 1 is open (100%, as 1029's core); picked up, it
    ///    completes 1; a kill alone does not. Data already held counts at the accept (a corpse drop is the player's own and
    ///    stays at an abandon). Kiang takes it at the turn-in. A corpse left until it rots (120 s) leaves no data, and
    ///    neither a reload nor a map made again sets a new Nelg (the scene skips an actor recorded defeated): only an
    ///    abandon and a new accept do (MarshesSentryTowerTests.NelgsCorpseRotsAndOnlyANewAcceptSetsHimDownAgain). After a
    ///    pickup no second Nelg comes until the turn-in.
    ///  - Done, it sets the "Part II done" flag as 1876 does (MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag).
    ///  - The rewards the client's second block 121245-121248, one to choose. A marker (no client name) at Nelg's post.
    ///
    /// V2 (2026-11-03, to-fix 25; migration 20271104002900_Marshes_on_the_client_floor writes it in place of V1): on his post
    /// (170, 224.68, 55; Ten Ton Hammer's point) Nelg stood on an island the real client cannot walk to from the deck: he
    /// never attacked, and his corpse stayed out of the 6 m loot reach. He and his marker now stand on the deck east of the
    /// post at (181.5, 224.6, 44) (ESTIMATE, a deviation from Ten Ton Hammer's point): a client collision floor 224.55 under
    /// him, 2.3 m from the nearest wall, the deck's walk from the Bane Assertion Camp waypoint reaches him.
    /// </summary>
    public static class MarshesUnderTheRadarNelgV2
    {
        public const uint MissionId = 1877;
        public const uint Level = 37;
        public const uint Kiang = MarshesSentryTowerNpcsV1.Kiang;
        public const uint Nelg = MarshesSentryTowerNpcsV1.Nelg;
        public const uint Intercept = 1;

        public const uint DataTemplate = MarshesUnderTheRadarPartTwoV1.DataTemplate, DataClass = MarshesUnderTheRadarPartTwoV1.DataClass;
        public const string DataKey = "bane-troop-data-nelg";
        public const int DataChance = 100;
        public const string NelgRole = "overseer-nelg";
        public const double MarkerRadius = 15;
        public static readonly ScenePosition NelgAt = new(181.5f, 224.6f, 44f);

        public const uint Experience = 140000;
        public const uint Credits = 7400;
        public const uint BioBoots = 121245;            // Armor_T3_Bio_V04_UNC_Boots_35_to_39
        public const uint GravitonBoots = 121246;       // Armor_T3_Graviton_V04_UNC_Boots_35_to_39
        public const uint StealthBoots = 121247;        // Armor_T3_Stealth_V04_UNC_Boots_35_to_39
        public const uint MechBoots = 121248;           // Armor_T3_Mech_V04_UNC_Boots_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Under the Radar - Part II (Overseer Nelg)", 19169,
                Kiang, Kiang, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, StealthBoots, MechBoots });
            mission.Prerequisite(MarshesUnderTheRadarV1.MissionId);

            mission.Objective(Intercept, 19685, 19686, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Intercept);
            mission.Progress(Intercept, MissionProgressEventKind.ItemAcquired, DataClass, 1);
            mission.Action(Intercept, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Action(Intercept, 2, MissionActionKind.SetPlayerFlag, playerFlag: MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag,
                playerFlagValue: 1, comment: "Under the Radar - Part II done (1877)");
            mission.Indicator(Intercept, 1, NelgAt.X, NelgAt.Y, NelgAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1877",
                "Native1 'Intercept Overseer Nelg and obtain data on the Bane troop movements in Marshes.' 19685/19686, no counter; " +
                "no objectiveconversation. Texts 19169-19173 (dropshipped on top of a building east of Palados). Name 10339 " +
                "Overseer Nelg. Item class28642 Bane Troop Data (template119003). Templates 121245-121248.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Under_the_Radar_-_Part_II",
                "Shared page with 1876: giver/receiver Lt. Kiang; requires Under the Radar; level37; 140000XP, 7400 credits; four " +
                "Armor Boots lvl35; briefing and old objective of 1877. Ten Ton Hammer: Overseer Nelg's outpost at 170,224,55.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesUnderTheRadarNelgV2",
                "A personal hostile Nelg 760319 (L38, 12000HP) set down at accept on the deck (181.5,224.6,44); the data (119003) a 100% " +
                "drop of his corpse while 1 is open, picked up it completes 1, which sets flag 1876001. Items the client block " +
                "121245-121248, one to choose. A marker on him.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                ExistingFactObjectiveIds = new() { Intercept },
                Items = new()
                {
                    new MissionItemBinding(DataKey, DataTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Nelg }, Intercept, MarshesMissionDataV1.MapContextId, DataChance, 1))
                }
            };
            scene.Actors[NelgRole] = new SceneActorDefinition(NelgRole, SceneActorKind.Creature, Nelg, NelgAt,
                Orientation: 3.1416, GameplayPolicy: Pools.PoolsSabotageTheTowerV1.Hostile());
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("nelg-set-down", NelgRole),
                    QuestSceneIntents.Attack("nelg-attacks", NelgRole)
                }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
