using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// South Of The Border (native mission 1068): Colonel Bosley at Camp Resistance sends the player with a crate of
    /// medical supplies through Martyr's Canyon to Colonel "Snake" Washington at the Snake Pit, Valverde Pools.
    ///
    /// Native: texts 6032-6035 and 6452 (missionconversation): offer "If you can get this crate of medical supplies
    /// through to him, I'm sure he'll be happy as a gopher in soft dirt", turn-in "This is all you brought? Medical
    /// supplies? No ammo?"; objective 1 "Deliver supplies to \"Snake.\"" 6036/6453 (missionobjective), no counter;
    /// its talk (objectiveconversation) on package 331, text 6037 "Who the hell are you?". Item: Medical Supplies for
    /// "The Snakepit" (class 22619, template 45128).
    ///
    /// Sourced: TaRapedia South Of The Border: giver Colonel Bosley, reward giver Colonel 'Snake' Washington at The
    /// Snakepit, requires One Way Trip; 25,000 XP, 3,300 credits, 2 Class VI Advanced Med Packs, 2 Class VI Power
    /// Chargers, 4 Class VI Concussion Grenades, 4 Class VI EMP Grenades (client classes 22542, 28497, 26267, 26277;
    /// templates 45059, 118906, 111017, 111027). TR MMO DB: level 25.
    ///
    /// Reconstructed (ESTIMATE): Bosley gives the crate on accept; Snake's talk takes it and completes 1; the turn-in
    /// is at Snake (PlateauSnakePitV1, a new NPC at an estimated place). Indicator: 1 at Snake (PlateauSnakePitV1.SnakeAt,
    /// ESTIMATE place), on Valverde Pools. The server sends a marker without a map: it shows on every map (fixed later).
    /// </summary>
    public static class PlateauSouthOfTheBorderV1
    {
        public const uint MissionId = 1068;
        public const uint Level = 25;
        public const uint Bosley = PlateauGiantKillerV1.Bosley, Snake = PlateauSnakePitV1.Snake;
        public const uint Deliver = 1;

        public const uint Supplies = 45128, SuppliesClass = 22619;
        public const string SuppliesKey = "snakepit-medical-supplies";

        public const uint Experience = 25000;
        public const uint Credits = 3300;
        public const uint MedPack = 45059, MedPacks = 2;                // Class VI Advanced Med Pack
        public const uint PowerCharger = 118906, PowerChargers = 2;     // Class VI Power Charger
        public const uint ConcussionGrenade = 111017, ConcussionGrenades = 4;   // Class VI Concussion Grenade
        public const uint EmpGrenade = 111027, EmpGrenades = 4;         // Class VI EMP Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "South Of The Border", 6032, Bosley, Snake, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (PowerCharger, PowerChargers), (ConcussionGrenade, ConcussionGrenades), (EmpGrenade, EmpGrenades)
            });
            mission.Prerequisite(PlateauOneWayTripV1.MissionId);

            mission.Objective(Deliver, 6036, 6453, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Deliver);
            mission.Conversation(Deliver, PlateauSnakePitV1.SnakePackage);
            mission.Action(Deliver, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("supplies-to-snake", MissionId, SuppliesKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Deliver, 2, MissionActionKind.GrantReward, reward: 1);
            var snake = PlateauSnakePitV1.SnakeAt;
            mission.Indicator(Deliver, 1, snake.X, snake.Y, snake.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(SuppliesKey, Supplies, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-supplies", MissionId, SuppliesKey, Supplies, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1068",
                "Native1 'Deliver supplies to \"Snake.\"' 6036/6453, no counter; objectiveconversation 1068,1 on package 331 text6037. " +
                "Texts 6032-6035, 6452. Medical Supplies for The Snakepit class22619/template45128. Snake name4372.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/South_Of_The_Border",
                "Giver Colonel Bosley, reward giver Colonel 'Snake' Washington (The Snakepit); requires One Way Trip; 25000XP, 3300 " +
                "credits, 2 Class VI Advanced Med Pack, 2 Class VI Power Charger, 4 Class VI Concussion Grenade, 4 Class VI EMP " +
                "Grenade. TR MMO DB level25.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSouthOfTheBorderV1",
                "Level25. The crate on accept; Snake's talk (750095, new, by the Snake Pit vendors) takes it and completes 1; " +
                "turn-in at Snake. Items 45059x2, 118906x2, 111017x4, 111027x4. Indicator at Snake (Valverde Pools). Place is an estimate.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
