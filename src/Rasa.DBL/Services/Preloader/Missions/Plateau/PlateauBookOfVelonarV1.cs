using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Book Of Velonar (native mission 930), Plateau: Luminary Sampei at New Velon Village sends the
    /// player to the Ruins of Velon Das for the sacred text lost when the village fell.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4983-4986 and 6329 (missionconversation);
    /// objective 1 "Retrieve the Book of Velonar." 4987/6330, no counter (missionobjective). The book is
    /// item class 21561 "The Book Of Velonar" (template 42495); its in-world proxy is class 21997
    /// UsableItemDispBookOfVelonar, an item dispenser (augmentation 34: states 81 active, 82 inactive).
    ///
    /// Sourced (TaRapedia The Book of Velonar): giver and receiver Sampei, level "32 (possibly less)", no
    /// requirement, 40,000 XP, 4,650 credits, "Modifications"; "The book is within a Forean Satchel on the
    /// ground somewhere inside the ruins of Velon Das"; "Inside North most hut (... at coordinates -473,
    /// 393, -8)".
    ///
    /// Reconstructed (ESTIMATE): level 32; one personal dispenser on the hut's floor at (-473, 393.08, -8)
    /// (the floor under the walkthrough's point, measured on the client's meshes), used with action 80
    /// arg 3 as the other dispensers; use issues the book, closes the dispenser and passes scene result 1;
    /// Sampei keeps the book at turn-in. "Modifications" read as four grade [4] modules to choose from,
    /// two armor and two weapon.
    /// </summary>
    public static class PlateauBookOfVelonarV1
    {
        public const uint MissionId = 930;
        public const uint Level = 32;
        public const uint Sampei = PlateauVillageOfTheDamnedV1.Sampei;
        public const uint BookTemplate = 42495, BookClass = 21561;
        public const uint DispenserClass = 21997, UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const string BookKey = "book-of-velonar";
        public static readonly ScenePosition Book = new(-473, 393.08f, -8);

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint HealthModule = 122971;        // Armor Module: Health Bonus [4]
        public const uint PowerModule = 122981;         // Armor Module: Power Bonus [4]
        public const uint CritModule = 123161;          // Weapon Module: Crit Hit Bonus [4]
        public const uint PiercingModule = 123146;      // Weapon Module: Armor Piercing [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Book Of Velonar", 4983, Sampei, Sampei, Level);
            mission.Reward(Experience, Credits, new[] { HealthModule, PowerModule, CritModule, PiercingModule });

            mission.Objective(1, 4987, 6330, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Take the Book of Velonar from its satchel", 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, Book.X, Book.Y, Book.Z, 15);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(BookKey, BookTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors["book"] = new SceneActorDefinition("book", SceneActorKind.Object, DispenserClass, Book,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, 1, 1, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-book", "book") } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("take-book", "book", TakenState),
                    new SetInteractionIntent("disable-book", "book", false)
                },
                Character = { new IssueMissionItemIntent("book-from-satchel", MissionId, BookKey, BookTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/930",
                "Native1 'Retrieve the Book of Velonar' 4987/6330, no counter; texts 4983-4986, 6329. Book class21561/template42495; " +
                "dispenser class21997 UsableItemDispBookOfVelonar (augmentation34). Velon Das Ruins marker68 (-475,393,0).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Book_of_Velonar",
                "Giver/receiver Luminary Sampei, New Velon Village; level '32 (possibly less)'; no requirement; 40000XP, 4650 " +
                "credits, 'Modifications'; the book in a Forean satchel in the ruins, north-most hut at -473,393,-8.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBookOfVelonarV1",
                "One personal dispenser21997 at (-473,393.08,-8) (hut floor), action80/arg3, states81/82; use issues the book " +
                "(assignment item, taken at turn-in). Modifications read as grade[4] modules: 122971, 122981, 123161, 123146.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
