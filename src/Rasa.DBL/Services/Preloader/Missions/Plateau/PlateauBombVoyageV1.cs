using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Bomb Voyage (native mission 931), Plateau: Outpost Cmdr. Russ at Wedge Rock Outpost sends the player
    /// to plant Altitude Bombs in six outgoing Bane cargo crates by the landing pad at the bottom of
    /// Desolation Quarry, then back to him.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4998-5001 and 6313 (missionconversation); objective 2
    /// "Plant 6 Altitude Bombs." 5007/6314, counter 5008 "Altitude Bombs Planted"; 8 "Return to Commander
    /// Russ." 5014/6321; 1 and 3-7 have no text (5002, 5009-5013 "no desc"), one per crate, not sent here;
    /// Russ's "I got some good news for you, soldier!" on package 398 for objective 8 (5015)
    /// (objectiveconversation 931,8, flag 1). Items: Altitude Bomb, class 21573 (MisPlateauItemDQbomb,
    /// template 42499); the crate is class 21568 "Bane Cargo Crate" (UsableStatelessBaneCargoCrateDELETEDUPE,
    /// a stateless switch, augmentation 8; usabledata use argument 3, state USE_SS_STATE_0 44). The client's
    /// map places no crates; the landing pad is ArchBaneObjLandingpadv02 at (348.79, 281.6, -230.05).
    ///
    /// Sourced: TaRapedia Bomb Voyage: giver and receiver Outpost Cmdr. Russ, level "32 (possibly less)", no
    /// requirement; 45,000 XP, 4,800 credits, "Equipment"; "The six crates are at the bottom of Desolation
    /// Quarry, right next to the Stalker". TR MMO DB: level 30.
    ///
    /// Reconstructed (ESTIMATE): level 30. Russ gives six bombs on accept; each crate (personal, on the
    /// ground around the pad) takes one, counts once and closes, and a bomb prop (ArchHumBombV01) appears beside it. "Equipment" read as the four Uncommon V04
    /// vests of the 30-34 band, one of each armor type (the lowest template of each class).
    /// </summary>
    public static class PlateauBombVoyageV1
    {
        public const uint MissionId = 931;
        public const uint Level = 30;
        public const uint Russ = PlateauPerduNpcsV1.Russ, RussPackage = PlateauPerduNpcsV1.RussPackage;
        public const uint Plant = 2, Report = 8;
        public const uint Bomb = 42499, BombClass = 21573, Bombs = 6;
        public const uint CrateClass = 21568, UseArgument = 3, CrateState = 44;
        public const string BombKey = "altitude-bomb";
        /// <summary>The planted bomb the player sees beside each used crate: ArchHumBombV01, a prop (client entityclass 7284).</summary>
        public const uint PlantedBombClass = 7284;
        public const float PlantedBombOffset = 1f;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioVest = 46035, GravitonVest = 25337, MechVest = 34463, StealthVest = 40591;

        /// <summary>The crates, on the quarry floor around the landing pad (ground measured on the client's terrain).</summary>
        public static readonly ScenePosition[] Crates =
        {
            new(340, 284.36f, -224),
            new(336, 284.36f, -228),
            new(344, 284.11f, -240),
            new(352, 283.86f, -241),
            new(356, 284.11f, -221),
            new(362, 284.11f, -228),
        };

        public static string CrateRole(int index) => $"crate-{index + 1}";
        public static string PlantedBombRole(int index) => $"bomb-{index + 1}";

        /// <summary>On the ground beside the crate (ESTIMATE: the crate's size is not known), not usable.</summary>
        public static ScenePosition PlantedBomb(int index) =>
            new(Crates[index].X + PlantedBombOffset, Crates[index].Y, Crates[index].Z);

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Bomb Voyage", 4998, Russ, Russ, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonVest, MechVest, StealthVest });

            mission.Objective(Plant, 5007, 6314, 0, MissionObjectiveState.Incomplete, counterTextId: 5008);
            mission.Objective(Report, 5014, 6321, 1, MissionObjectiveState.Inactive);

            // 2: one scene result per crate, six distinct ones.
            mission.Scenario(1, "Plant an Altitude Bomb in six distinct cargo crates", Bombs);
            mission.Transition(Plant);
            for (var id = 1U; id <= Bombs; id++)
                mission.Progress(Plant, MissionProgressEventKind.ScenarioEvent, id, Bombs, counter: 1, trigger: id);
            mission.Action(Plant, 1, MissionActionKind.RevealObjective, targetObjective: Report);
            mission.Action(Plant, 2, MissionActionKind.ActivateObjective, targetObjective: Report, state: MissionObjectiveState.Incomplete);

            // 8: Russ.
            mission.Transition(Report);
            mission.Conversation(Report, RussPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var pad = (X: 348.79, Y: 284.11, Z: -230.05);
            mission.Indicator(Plant, 1, pad.X, pad.Y, pad.Z, 20);
            var russ = PlateauPerduNpcsV1.RussAt;
            mission.Indicator(Report, 2, russ.X, russ.Y, russ.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(BombKey, Bomb, MissionItemScope.AssignmentIssued, Bombs,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-bombs", MissionId, BombKey, Bomb, Bombs)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var role = CrateRole(index);
                var sequence = (uint)index + 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, Crates[index],
                    InitialObjectState: CrateState, UseAction: new SceneObjectAction(MissionId, Plant, sequence, UseArgument));
                var bomb = PlantedBombRole(index);
                scene.Actors[bomb] = new SceneActorDefinition(bomb, SceneActorKind.Object, PlantedBombClass, PlantedBomb(index),
                    InitiallyInteractable: false);
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"close-{role}", role, false), new EnsureActorIntent($"plant-{bomb}", bomb) },
                    Character = { new ConsumeMissionItemIntent($"bomb-in-{role}", MissionId, BombKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, 1, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/931",
                "Native2 'Plant 6 Altitude Bombs.' 5007/6314 counter5008, 8 'Return to Commander Russ.' 5014/6321, 1 and 3-7 " +
                "no text; objectiveconversation 931,8,398 text5015. Texts 4998-5001, 6313. Altitude Bomb class21573/template42499; " +
                "Bane Cargo Crate class21568 (augmentation8, use argument3, state44). No crates on the map; landing pad " +
                "(348.79, 281.6, -230.05).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bomb_Voyage",
                "Giver/receiver Outpost Cmdr. Russ, Wedge Rock Outpost; level '32 (possibly less)'; no requirement; 45000XP, 4800 " +
                "credits, 'Equipment'; six crates at the bottom of Desolation Quarry next to the Stalker. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBombVoyageV1",
                "Level30. Six bombs on accept; six personal crates around the pad (places estimated); each takes a bomb, counts " +
                "once, closes; a bomb left goes at completion (cleanup Remove). Equipment read as Uncommon V04 vests 30-34: 46035, 25337, 34463, 40591.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
