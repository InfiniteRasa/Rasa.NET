using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Soyuz Salvage: Marshes (native mission 2009), Valverde Marshes: High Command wants a piece of the Orbital Module of
    /// General British's Soyuz, which came down somewhere in the Marshes.
    ///
    /// Native: name/log/offer/turn-in texts 21589-21592, reminder 21593 "none" (missionconversation); objective 1 "Salvage
    /// the Soyuz Debris" 21608/21609 ("Locate and salvage the Soyuz orbital module Debris that crashed somewhere in Valverde
    /// Marshes."), no counter (missionobjective); no objective conversation. Debris classes 29783 and 29784 "Soyuz Orbital
    /// Module Debris" (augmentation 34, use argument 1: usabledata gives them no other), the piece class 29785 (template
    /// 122851), the Soyuz ISS Model Rocket 29436 (template 122834), as PlateauSoyuzSalvageV1. The client's Marshes map
    /// places no debris. Name 3123 Sergeant Starling.
    ///
    /// Sourced: TaRapedia updates 2008-08-14 (and PN-D11): Operation Immortality, five Soyuz Salvage missions, "players can
    /// only complete one of the five", "Palados on Marshes (Level 37)"; no source names the giver. The sister missions'
    /// reward the Soyuz ISS Model Rocket. World DB: Sergeant Starling 510171 in Paludos.
    ///
    /// Reconstructed (ESTIMATE, as PlateauSoyuzSalvageV1):
    ///  - Giver and receiver Sergeant Starling in Paludos (the triage's Q21: an AFS sergeant at the named base). Level 37.
    ///  - Five personal debris pieces, both models, at flat open ground over the map's regions (west, south-west, north-east,
    ///    the middle east, east; navmesh probe of 2026-11-03: each walked to from its nearest waypoint), far from the pools:
    ///    using one issues the piece and closes all five; Starling keeps it. XP 65,000 and credits 5,400 Starling's Cannister
    ///    Collection's; the model rocket the fixed reward. Only one of the five per character is not enforced (Plateau's
    ///    2007 is the only other one on the server).
    /// </summary>
    public static class MarshesSoyuzSalvageV1
    {
        public const uint MissionId = 2009;
        public const uint Level = 37;
        public const uint Starling = MarshesCannisterCollectionV1.Starling;
        public const uint Salvage = 1;
        public const uint DebrisTemplate = PlateauSoyuzSalvageV1.DebrisTemplate, DebrisClass = PlateauSoyuzSalvageV1.DebrisClass;
        public const uint ModelRocket = PlateauSoyuzSalvageV1.ModelRocket;
        public const uint UseArgument = PlateauSoyuzSalvageV1.UseArgument;
        public const uint ReadyState = PlateauSoyuzSalvageV1.ReadyState, TakenState = PlateauSoyuzSalvageV1.TakenState;
        public const string DebrisKey = "soyuz-debris";
        public const double MarkerRadius = 40;

        /// <summary>(role, class, place): west, south-west, north-east, the middle east, east.</summary>
        public static readonly (string Role, uint Class, ScenePosition At)[] Pieces =
        {
            ("debris-1", 29783, new ScenePosition(-560f, 217.48f, 350f)),
            ("debris-2", 29784, new ScenePosition(-550f, 217.08f, -150f)),
            ("debris-3", 29783, new ScenePosition(250f, 214.12f, 600f)),
            ("debris-4", 29784, new ScenePosition(200f, 214.3f, 150f)),
            ("debris-5", 29783, new ScenePosition(300f, 214.08f, -100f)),
        };

        public const uint Experience = 65000;
        public const uint Credits = 5400;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Soyuz Salvage: Marshes", 21589, Starling, Starling, Level);
            mission.Reward(Experience, Credits, new uint[0], fixedTemplates: new[] { ModelRocket });

            mission.Objective(Salvage, 21608, 21609, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Salvage one piece of the Soyuz Orbital Module", 1);
            mission.Transition(Salvage);
            mission.Progress(Salvage, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(Salvage, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
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
                mission.Indicator(Salvage, sequence, piece.At.X, piece.At.Y, piece.At.Z, MarkerRadius);
                scene.Actors[piece.Role] = new SceneActorDefinition(piece.Role, SceneActorKind.Object, piece.Class, piece.At,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Salvage, sequence, UseArgument));
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

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/2009",
                "Native1 'Salvage the Soyuz Debris' 21608/21609, no counter; texts 21589-21593. Debris classes 29783/29784 " +
                "(augmentation34, use argument 1), piece 29785/template122851, Soyuz ISS Model Rocket 29436/template122834. No " +
                "debris on the map. Name3123 Sergeant Starling.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Updates/2008-08-14",
                "Operation Immortality: five Soyuz Salvage missions, players can only complete one; Palados on Marshes level37. " +
                "No giver named.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesSoyuzSalvageV1",
                "Giver/receiver Sergeant Starling 510171 (Paludos). Five personal debris pieces at (-560,217.48,350), " +
                "(-550,217.08,-150), (250,214.12,600), (200,214.3,150), (300,214.08,-100): places, XP 65000, credits 5400 " +
                "estimated. One piece completes, closes all; Starling keeps it. Only-one-of-five not enforced.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
