using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Mislaid Plans (native mission 965), Plateau: General Bailey at Fort Defiance sends the player to the
    /// ruins of Pinnacle Watchtower for the battle plans a dead courier carried.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5269-5272 and 6411 (missionconversation); objective 1
    /// "Retrive the battle plans." 5277/6412 and objective 3 "RADIATION WARNING!" 5299/6413 ("You are taking
    /// damage from radiation in the area! Complete the mission quickly!"), no counters (missionobjective); no
    /// objective conversation. The plans are item class 21839 "AFS Battle Plans" (template 44835); the bag is
    /// class 21841 UsableItemDispCourierBag "Courier Bag", an item dispenser (augmentation 34, use argument 3 in
    /// usabledata, as The Book Of Velonar's).
    ///
    /// Sourced: TaRapedia Mislaid Plans: giver General Thaddeus T. Bailey, Fort Defiance; 45,000 XP; no
    /// credits or items given; "Look for a usable courier bag on the ground. It is generally located on the
    /// far side of the wrecked watchtower. The radiation is nothing to worry if your armor is in good
    /// condition." TaRapedia Logos guide: Pinnacle Watchtower at "(-551, 387)", in the guide's map units (its
    /// Strong at "-505, 219" is the server's (-501, 20)): the client map's only destroyed tower,
    /// ArchHumOutpostTowerDestroyedV01 at (-544.2, 394.47, 180.83). TR MMO DB quest list: level 30.
    ///
    /// Reconstructed (ESTIMATE): one personal bag at (-552, 395.3, 174), on the ground south-west of the tower
    /// (the far side from Fort Defiance; height measured on the client's meshes), used as The Book Of
    /// Velonar's satchel; Bailey keeps the plans; 4,650 credits (no source gives them). No radiation: the
    /// server has no area that damages the player, so objective 3 is left out (NOTES-missing-mechanics.md).
    /// </summary>
    public static class PlateauMislaidPlansV1
    {
        public const uint MissionId = 965;
        public const uint Level = 30;
        public const uint Retrieve = 1;
        public const uint Bailey = PlateauBaileyNpcsV1.Bailey;
        public const uint PlansTemplate = 44835, PlansClass = 21839;
        public const uint BagClass = 21841, UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const string PlansKey = "battle-plans";
        public static readonly ScenePosition Bag = new(-552, 395.3f, 174);
        public static readonly (double X, double Y, double Z) Tower = (-544.2, 394.65, 180.8);

        public const uint Experience = 45000;
        public const uint Credits = 4650;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Mislaid Plans", 5269, Bailey, Bailey, Level);
            mission.Reward(Experience, Credits, new uint[0]);

            mission.Objective(Retrieve, 5277, 6412, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Take the battle plans from the courier bag", 1);
            mission.Transition(Retrieve);
            mission.Progress(Retrieve, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(Retrieve, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Retrieve, 1, Tower.X, Tower.Y, Tower.Z, 15);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                // The bag stands on the Plateau whatever map the mission is taken on (a game master's .givemission in an
                // instance), and is not taken along into an instance.
                BoundMapContextId = PlateauMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(PlansKey, PlansTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors["bag"] = new SceneActorDefinition("bag", SceneActorKind.Object, BagClass, Bag,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, 1, 1, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-bag", "bag") } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("take-plans", "bag", TakenState),
                    new SetInteractionIntent("disable-bag", "bag", false)
                },
                Character = { new IssueMissionItemIntent("plans-from-bag", MissionId, PlansKey, PlansTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/965",
                "Native1 'Retrive the battle plans.' 5277/6412, native3 'RADIATION WARNING!' 5299/6413, no counters; texts " +
                "5269-5272, 6411. Plans class21839/template44835; bag class21841 UsableItemDispCourierBag (augmentation34, " +
                "usabledata arg3). Destroyed tower ArchHumOutpostTowerDestroyedV01 at -544.2,394.47,180.83 (client map).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mislaid_Plans",
                "Giver General Thaddeus T. Bailey, Fort Defiance; 45000XP; a usable courier bag on the far side of the wrecked " +
                "watchtower; radiation. Logos guide: Pinnacle Watchtower (-551, 387) in map units. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMislaidPlansV1",
                "One personal bag at (-552,395.3,174) (ground SW of the tower), action80/arg3, states81/82; use issues the " +
                "plans (assignment item, taken at turn-in). 4650 credits. No radiation area (missing mechanic): objective3 " +
                "left out.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
