using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Eastern Listening Post (native mission 1323), Valverde Pools: Colonel 'Snake' Washington sends the player to pick up
    /// the East Listening Post's lost supply package and take it to Lieutenant Chester there.
    ///
    /// Native: texts 10778-10782 (missionconversation): log "recover the lost supply package for the East Listening Post
    /// and deliver it to Lieutenant Chester", offer "the last shipment never made it. But the beacon on the package is
    /// still transmitting", turn-in (Chester) "Just set those supplies down and I'll brief you on your orders."; objective 1
    /// "Collect ELP Supply Package" 10783/10784 ("Follow the beacon to the missing supply package"), no counters
    /// (missionobjective); no objective conversation. Item: ELP Supplies 97511 (class 25586 MisPoolsItemELPSupplies).
    ///
    /// Sourced: TaRapedia Eastern Listening Post: giver Colonel 'Snake' Washington, reward from Lieutenant Chester (East
    /// Listening Post); requires The Supply Master; follow-up Smash and Grab; 32,500 XP, 3,600 credits, 2 Class VII EMP
    /// Bombs, 2 Class VII Armor Chargers, 4 Class VII Incendiary Grenades, 4 Class VII EMP Grenades; walkthrough: the beacon
    /// at x 240.4, y -141.2, "in a large crater", "One or more striders may spawn when you approach the crate". TR MMO DB:
    /// level 35 (TaRapedia: 33).
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: add 2-3 personal striders): a personal AFS item crate
    /// (class 11078, as The Thirsty Hermit's cabinet) on the crater's floor 6 m from TaRapedia's point (the point itself is
    /// on a ledge with no walk down), usable once: it gives the package and completes 1. Two personal Thrax Cavalrymen
    /// (strider riders, 750100 of Foreas Base Past, level 30) stand in the crater from the accept, hostile, no loot.
    /// Chester takes the package at the turn-in. Level 35 (TR MMO DB, as this batch takes its levels). Incendiary read as
    /// the Fire grenade; the items the lowest template of each, all given. The marker on the crate.
    /// </summary>
    public static class PoolsEasternListeningPostV1
    {
        public const uint MissionId = 1323;
        public const uint Level = 35;
        public const uint Snake = PoolsWashingtonPlaceV1.Snake;
        public const uint Chester = PoolsSupplyNpcsV1.Chester;
        public const uint Collect = 1, CrateScenario = 1;

        public const uint PackageTemplate = 97511, PackageClass = 25586;
        public const string PackageKey = "elp-supplies";
        public const uint CrateClass = 11078;           // UsableItemDisp_item_crate_hum_v01
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public static readonly ScenePosition CrateAt = new(234.4f, 679.58f, -141.2f);

        public const uint Strider = Plateau.PlateauForeasBasePastNpcsV1.Cavalryman;
        public static readonly ScenePosition[] StridersAt =
        {
            new(228.4f, 679.49f, -141.2f),
            new(240.4f, 679.94f, -123.2f),
        };

        public const uint Experience = 32500;
        public const uint Credits = 3600;
        public const uint EmpBomb = PoolsMedicinalMeasuresV1.EmpBomb, EmpBombs = 2;
        public const uint ArmorCharger = PoolsMedicinalMeasuresV1.ArmorCharger, ArmorChargers = 2;
        public const uint FireGrenade = PoolsCultDividedV1.FireGrenade, FireGrenades = 4;
        public const uint EmpGrenade = PoolsMedicinalMeasuresV1.EmpGrenade, EmpGrenades = 4;

        public static string StriderRole(int index) => $"strider-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Eastern Listening Post", 10778, Snake, Chester, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (EmpBomb, EmpBombs), (ArmorCharger, ArmorChargers), (FireGrenade, FireGrenades), (EmpGrenade, EmpGrenades)
            });
            mission.Prerequisite(PoolsSupplyMasterV1.MissionId);

            mission.Objective(Collect, 10783, 10784, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(CrateScenario, "The ELP supply package picked up", 1);
            mission.Transition(Collect);
            mission.Progress(Collect, MissionProgressEventKind.ScenarioEvent, 1, null, counter: CrateScenario);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Collect, 1, CrateAt.X, CrateAt.Y, CrateAt.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(PackageKey, PackageTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors["crate"] = new SceneActorDefinition("crate", SceneActorKind.Object, CrateClass, CrateAt,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Collect, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-crate", "crate"));
            for (var index = 0; index < StridersAt.Length; index++)
            {
                var role = StriderRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Strider, StridersAt[index],
                    Orientation: index * 3.1415926536, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
            }
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("open-crate", "crate", TakenState),
                    new SetInteractionIntent("close-crate", "crate", false)
                },
                Character = { new IssueMissionItemIntent("package-taken", MissionId, PackageKey, PackageTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, CrateScenario, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1323",
                "Native1 'Collect ELP Supply Package' 10783/10784, no counters; no objectiveconversation. Texts 10778-10782. " +
                "ELP Supplies template97511/class25586.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Eastern_Listening_Post",
                "Giver Colonel 'Snake' Washington, reward from Lieutenant Chester; requires The Supply Master; follow-up Smash and " +
                "Grab; 32500XP, 3600 credits, 2 Class VII EMP Bomb, 2 Class VII Armor Charger, 4 Class VII Incendiary Grenade, 4 " +
                "Class VII EMP Grenade; beacon at 240.4,-141.2 in a large crater; striders may spawn. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsEasternListeningPostV1",
                "Personal crate (11078) on the crater floor at 234.4,679.58,-141.2; two personal Thrax Cavalrymen 750100 (user: " +
                "2-3) from the accept; Chester takes the package. Items 111232x2, 118898x2, 111038x4, 111028x4.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
