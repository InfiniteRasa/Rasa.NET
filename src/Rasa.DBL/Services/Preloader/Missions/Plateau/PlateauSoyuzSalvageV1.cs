using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Soyuz Salvage: Plateau (native mission 2007): Commander Grissom at Fort Defiance wants a piece of
    /// the Orbital Module of General British's Soyuz, which came down somewhere on the Plateau.
    ///
    /// Native: name/log/offer/turn-in texts 21579-21582, reminder 21583 "none" (missionconversation);
    /// objective 1 "Salvage the Soyuz Debris" 21604/21605, no counter (missionobjective). Debris: classes
    /// 29783 and 29784 "Soyuz Orbital Module Debris", item dispensers (augmentation 34: states 81, 82); the
    /// salvaged piece is class 29785 (template 122851); class 29436 "Soyuz ISS Model Rocket" (template
    /// 122834). The client's Plateau map places no debris. Grissom is creature 510180.
    ///
    /// Sourced: TaRapedia Soyuz Salvage: Plateau: giver and receiver Grissom, level 30 (no confirmed lower
    /// bound), reward "Soyuz ISS Model Rocket", no XP or credits given. Patch notes (play.tr.eu, rgtr):
    /// Operation Immortality event, five Soyuz Salvage missions, "players can only complete one of the
    /// five", Fort Defiance on Plateau level 34.
    ///
    /// Reconstructed (ESTIMATE): level 34; three personal debris pieces, both models, at flat open ground
    /// far from every pool and place marker (navmesh, reached from Trinity Bridge, ground measured on the
    /// client's terrain); using one issues the piece and closes all three; Grissom keeps the piece. XP
    /// 50,000 and credits 5,000 estimated; the model rocket the fixed reward. The other four zones'
    /// versions are not on the server, so "only one of five" is not enforced.
    /// </summary>
    public static class PlateauSoyuzSalvageV1
    {
        public const uint MissionId = 2007;
        public const uint Level = 34;
        public const uint Grissom = 510180;
        public const uint DebrisTemplate = 122851, DebrisClass = 29785;
        public const uint ModelRocket = 122834;         // AccountReward_SoyuzModelRocket_Default
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const string DebrisKey = "soyuz-debris";

        /// <summary>(role, class, place): north-west of the tower ruins, by the chasm's east part, by the Septic Bog.</summary>
        public static readonly (string Role, uint Class, ScenePosition At)[] Pieces =
        {
            ("debris-1", 29783, new ScenePosition(-475f, 380.12f, 650f)),
            ("debris-2", 29784, new ScenePosition(175f, 349.11f, 175f)),
            ("debris-3", 29783, new ScenePosition(525f, 441.87f, -675f)),
        };

        public const uint Experience = 50000;
        public const uint Credits = 5000;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Soyuz Salvage: Plateau", 21579, Grissom, Grissom, Level);
            mission.Reward(Experience, Credits, new uint[0], fixedTemplates: new[] { ModelRocket });

            mission.Objective(1, 21604, 21605, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Salvage one piece of the Soyuz Orbital Module", 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(DebrisKey, DebrisTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Pieces.Length; index++)
            {
                var piece = Pieces[index];
                var sequence = (uint)index + 1;
                mission.Indicator(1, sequence, piece.At.X, piece.At.Y, piece.At.Z, 40);
                scene.Actors[piece.Role] = new SceneActorDefinition(piece.Role, SceneActorKind.Object, piece.Class, piece.At,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, 1, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{piece.Role}", piece.Role));
                var salvage = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"salvage-{piece.Role}", piece.Role, TakenState) },
                    Character = { new IssueMissionItemIntent($"debris-from-{piece.Role}", MissionId, DebrisKey, DebrisTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, 1, 1) }
                };
                foreach (var other in Pieces)
                    salvage.World.Add(new SetInteractionIntent($"close-{other.Role}-after-{piece.Role}", other.Role, false));
                scene.Sequences[sequence] = salvage;
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/2007",
                "Native1 'Salvage the Soyuz Debris' 21604/21605, no counter; texts 21579-21583. Debris classes 29783/29784 " +
                "(augmentation34), piece 29785/template122851, Soyuz ISS Model Rocket 29436/template122834. No debris on the map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Soyuz_Salvage:_Plateau",
                "Giver/receiver Commander Grissom, Fort Defiance; level 30 (no confirmed lower bound); reward Soyuz ISS Model Rocket; " +
                "no XP or credits given. Patch notes: Operation Immortality, one of five Soyuz Salvage missions, Plateau level34.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSoyuzSalvageV1",
                "Three personal debris pieces at (-475,380.12,650), (175,349.11,175), (525,441.87,-675): places, XP 50000 and " +
                "credits 5000 estimated. One piece completes, closes all; Grissom keeps it. Only-one-of-five not enforced.", 0.2);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
