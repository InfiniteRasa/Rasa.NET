using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Medicinal Measures (native mission 1066), Valverde Pools: Dr. Hester at the Snake Pit wants the nectar of the
    /// Mawflower, for "pain medicine".
    ///
    /// Native: texts 6005-6008 and 7643 (missionconversation): offer "the nectar from the Mawflower when fermented makes a
    /// delicious... I mean potent medicine", turn-in "We'll be drinking again in no time! ... Medically speaking."; objective
    /// 1 "Collect Mawflower Nectar" 6019/7644 ("from the Malflower plants in Valverde Pools"), no counter
    /// (missionobjective). Nectar: class 22137 MisPoolsItemMawflowernectar (template 45045). The plant: classes 22518-22522
    /// UsableItemDispMawflowerV01DELETE..V05DELETE "Mawflower", item dispensers (augmentation 34), five models. The
    /// client's map places no plant.
    ///
    /// Sourced: TaRapedia Medicinal Measures: giver and receiver Dr. Hester (The Snakepit); 10 Mawflower Nectar; 27,500 XP,
    /// 3,400 credits, 2 Class VII EMP Bombs, 2 Class VII Armor Chargers, 4 Class VII EMP Grenades, 4 Class VII Concussion
    /// Grenades (client classes 26542, 28489, 26278, 26268); no requirement; "There is a field of the flowers around 54.1,
    /// 665.7, 458.5. Another field at: 267.4, 680.2, -530.0"; "tentacle-like looking plants... grow in bunches". TR MMO DB:
    /// level 35.
    ///
    /// Reconstructed (ESTIMATE): ten personal plants (two of each model) in TaRapedia's second field, on the navmesh's main
    /// region; the first field's place has no navmesh within 30 m (with z = -458.5 it is a flat area, maybe water), so no
    /// plant stands there. Usable while objective 1 is open; each use takes the server's 10 s object use, gives one nectar
    /// and closes that plant. The client has no counter text for objective 1, so a hidden tally (objective
    /// <see cref="Tally"/>, objective 1's texts) counts ten distinct plants, as PlateauBeerHunterV1's; 1 completes with it. Hester takes the
    /// nectar at the turn-in (the nectar goes with the mission). The reward items as the lowest template of each client
    /// class (the EMP Bomb has two), all given. The marker on the field.
    /// </summary>
    public static class PoolsMedicinalMeasuresV1
    {
        public const uint MissionId = 1066;
        public const uint Level = 35;
        public const uint Hester = PoolsSnakePitNpcsV1.Hester;
        public const uint Collect = 1;
        public const uint Tally = 2;                    // hidden: ten distinct plants

        public const uint NectarTemplate = 45045, NectarClass = 22137, Nectars = 10;
        public const string NectarKey = "mawflower-nectar";
        public static readonly uint[] PlantClasses = { 22518, 22519, 22520, 22521, 22522 };   // UsableItemDispMawflowerV01..V05DELETE
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;

        /// <summary>The plants, in TaRapedia's field round (267.4, -530.0).</summary>
        public static readonly ScenePosition[] Plants =
        {
            new(267.4f, 680.5f, -530),
            new(262, 680.5f, -527),
            new(272, 681.1f, -527),
            new(262, 680.6f, -533),
            new(270, 681.2f, -534),
            new(258, 680.5f, -530),
            new(265, 680.4f, -522),
            new(270, 680.8f, -520),
            new(257, 680.5f, -524),
            new(256, 681.0f, -536),
        };

        public const uint Experience = 27500;
        public const uint Credits = 3400;
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint ArmorCharger = 118898, ArmorChargers = 2;             // Class VII Armor Charger
        public const uint EmpGrenade = 111028, EmpGrenades = 4;                 // Class VII EMP Grenade
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4;   // Class VII Concussion Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Medicinal Measures", 6005, Hester, Hester, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (EmpBomb, EmpBombs), (ArmorCharger, ArmorChargers), (EmpGrenade, EmpGrenades), (ConcussionGrenade, ConcussionGrenades)
            });

            mission.Objective(Collect, 6019, 7644, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 6019, 7644, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Ten distinct Mawflowers harvested", (uint)Plants.Length);
            mission.Transition(Collect);
            mission.ObjectiveState(Collect, Tally, MissionObjectiveState.Completed);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Tally);
            mission.Indicator(Collect, 1, 264, 680.6, -529, 20);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(NectarKey, NectarTemplate, MissionItemScope.AssignmentIssued, Nectars,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Plants.Length; index++)
            {
                var id = (uint)index + 1;
                var role = $"mawflower-{id}";
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Nectars, counter: 1, trigger: id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PlantClasses[index % PlantClasses.Length],
                    Plants[index], Orientation: index * 0.6283185307, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Collect, id, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"harvest-{role}", role, TakenState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"nectar-from-{role}", MissionId, NectarKey, NectarTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, 1, id) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1066",
                "Native1 'Collect Mawflower Nectar' 6019/7644, no counter. Texts 6005-6008, 7643. Nectar class22137/template45045; " +
                "plant classes 22518-22522 (augmentation34). No plant on the map. Hester name8865, package1129.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Medicinal_Measures",
                "Giver/receiver Dr. Hester (The Snakepit); 10 Mawflower Nectar; 27500XP, 3400 credits, 2 Class VII EMP Bomb, 2 Class " +
                "VII Armor Charger, 4 Class VII EMP Grenade, 4 Class VII Concussion Grenade; fields 54.1,665.7,458.5 and " +
                "267.4,680.2,-530.0. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMedicinalMeasuresV1",
                "Ten personal plants (classes 22518-22522, two each) in the second field round 267,-530 on the navmesh (the first " +
                "field's place has no navmesh); usable while 1 is open, one nectar each; hidden tally2 counts ten distinct plants, 1 completes with it; the nectar " +
                "goes with the mission. Items 111232x2, 118898x2, 111028x4, 111018x4. Hester (760201, new).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
