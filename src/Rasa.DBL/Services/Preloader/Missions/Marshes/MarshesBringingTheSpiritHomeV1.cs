using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Bringing the Spirit Home (native mission 864), Valverde Marshes: Elder Bargas at the Temple of Paludos sends the
    /// player to put down Forean Machina, the Bane's melded Forean warriors, and to bring the last of their spirit to an urn
    /// on a Forean altar.
    ///
    /// Native: texts 4510-4513 and 9370 (missionconversation): log "Put down 4 Forean Machina in the eastern marshes, then
    /// bring the remains of their spirit to an urn on a Forean altar. Return to Elder Bargas", offer "Out to the east and
    /// north, you will find small groups of Forean Machina ... take the last remnants of their spirit to the Forean altars
    /// found in and around Paludos", turn-in "you have done a great service to these fallen warriors"; objectives 2
    /// "Euthanize 4 Forean Machina and collect their Spirit Icons" 4731/9371, 1 "Place the Forean spirit icons at one of the
    /// the altars." 4532/9372 counter 4533 "Spirit Shards Returned:" (missionobjective); no objective conversation. Item
    /// Forean Spirit Icon class 21021 MisMarshesItemForeanIcon (template 42408). Indicator name 141 "Forean Spirit Altar"
    /// (missionobjectiveindicatorlanguage). Map Valverde Marshes: the ArchForeanElohShrine statics of the Temple of
    /// Paludos (-248..-168, 16..160), among them two squares of four (V02) on its upper floor either side of Bargas,
    /// centred on (-180, 28) and (-236, 28). Class 7636 UsableTwoStateForeanElohBrazierV01 (augmentation 9, use argument 1:
    /// usabledata gives it no other), the client's only usable Forean burner.
    ///
    /// Sourced: TaRapedia Bringing the Spirit Home: giver and receiver Elder Bargas (Temple of Paludos); level 36; 75,000
    /// XP, 5,700 credits; one of Astra Hazmat Armor Legs lvl 39, Wellcare Motor Assist Armor Legs lvl 38, Olympia Reflective
    /// Armor Legs lvl 39, Dynamo Stealth Armor Legs lvl 35; TaRapedia Forean Machina: they "resurrect until ... looted". The
    /// rewards the client's item block 120045-120048 (Hazmat Legs 39, Motor Assist Legs 38, Reflective Legs 39, Stealth Legs
    /// 35: TaRapedia's four in order). TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - A Spirit Icon a personal 100% drop from any Forean Machina (760351: the two new pools of MarshesEastAndNorthNpcsV1
    ///    and the one south-east of High Point Retreat) while 2 is open; four complete it and open 1. A Machina's first death
    ///    is a self revive (upstream): the icon drops at its real death.
    ///  - The altars: a personal Forean brazier (7636, "an urn on the altar") in the middle of each of the two squares of
    ///    shrines by Bargas. A use of either, while 1 is open, takes the four icons, lights it and closes both; the counter
    ///    goes to 4 (one shard per icon). The icons are the player's (they stay at an abandon, as 1084's parts).
    ///  - Level TaRapedia's. Markers on the two new Machina pools (2) and one, the client's "Forean Spirit Altar", over both
    ///    braziers (1).
    /// </summary>
    public static class MarshesBringingTheSpiritHomeV1
    {
        public const uint MissionId = 864;
        public const uint Level = 36;
        public const uint Bargas = MarshesEastAndNorthNpcsV1.Bargas;
        public const uint Place = 1, Euthanize = 2;
        public const uint Returned = 1;                 // scenario: shard n is event n

        public const uint Machina = MarshesEastAndNorthNpcsV1.Machina;
        public const uint IconTemplate = 42408, IconClass = 21021, Icons = 4;
        public const string IconKey = "forean-spirit-icon";
        public const uint BrazierClass = 7636;          // UsableTwoStateForeanElohBrazierV01
        public const uint ReadyState = 55, LitState = 56, UseArgument = 1;
        public const uint ForeanSpiritAltar = 141;      // missionobjectiveindicatorlanguage
        public const double PoolMarkerRadius = 25, AltarMarkerRadius = 35;

        /// <summary>The braziers: role and place (the middle of each square of shrines, on the navmesh).</summary>
        public static readonly (string Role, ScenePosition At)[] Altars =
        {
            ("altar-east", new ScenePosition(-180f, 230.88f, 28f)),
            ("altar-west", new ScenePosition(-236f, 230.88f, 28f))
        };

        public static readonly ScenePosition AltarMarker = new(-208f, 230.88f, 28f);

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public static readonly uint[] Rewards = { 120045, 120046, 120047, 120048 };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Bringing the Spirit Home", 4510, Bargas, Bargas, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Scenario(Returned, "The spirit icons placed at an altar", Icons);

            mission.Objective(Euthanize, 4731, 9371, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Place, 4532, 9372, 1, MissionObjectiveState.Inactive, counterTextId: 4533);

            // 2: four icons; 1 opens.
            mission.Transition(Euthanize);
            mission.Progress(Euthanize, MissionProgressEventKind.ItemAcquired, IconClass, Icons);
            mission.Action(Euthanize, 1, MissionActionKind.RevealObjective, targetObjective: Place);
            mission.Action(Euthanize, 2, MissionActionKind.ActivateObjective, targetObjective: Place, state: MissionObjectiveState.Incomplete);

            // 1: the four shards, from one use of an altar.
            mission.Transition(Place);
            for (var shard = 1U; shard <= Icons; shard++)
                mission.Progress(Place, MissionProgressEventKind.ScenarioEvent, shard, Icons, counter: Returned, trigger: shard);
            mission.Action(Place, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Euthanize, 1, MarshesEastAndNorthNpcsV1.NorthMachinaAt.X, MarshesEastAndNorthNpcsV1.NorthMachinaAt.Y,
                MarshesEastAndNorthNpcsV1.NorthMachinaAt.Z, PoolMarkerRadius);
            mission.Indicator(Euthanize, 2, MarshesEastAndNorthNpcsV1.EastMachinaAt.X, MarshesEastAndNorthNpcsV1.EastMachinaAt.Y,
                MarshesEastAndNorthNpcsV1.EastMachinaAt.Z, PoolMarkerRadius);
            mission.NamedIndicator(Place, ForeanSpiritAltar, AltarMarker.X, AltarMarker.Y, AltarMarker.Z, AltarMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/864",
                "Native2 'Euthanize 4 Forean Machina and collect their Spirit Icons' 4731/9371, 1 'Place the Forean spirit icons at " +
                "one of the the altars.' 4532/9372 counter4533 'Spirit Shards Returned:'; no objectiveconversation. Texts 4510-4513, " +
                "9370 (east and north; altars in and around Paludos; an urn on the altar). Item class21021 (42408). Indicator " +
                "name141 'Forean Spirit Altar'. Name8536 Elder Bargas. Map: Forean shrines at the Temple of Paludos. Brazier 7636. " +
                "Item block 120045-120048.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bringing_the_Spirit_Home",
                "Giver/receiver Elder Bargas, Temple of Paludos (-205,230,40); level36; 75000XP, 5700 credits; one of Astra Hazmat " +
                "Armor Legs39, Wellcare Motor Assist Armor Legs38, Olympia Reflective Armor Legs39, Dynamo Stealth Armor Legs35. " +
                "Forean Machina resurrect until looted. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesBringingTheSpiritHomeV1",
                "Icon 42408 a personal 100% drop from any Forean Machina 760351 on map1454 while 2 is open (pools 760353 north and " +
                "760354 east of Paludos, 760351 by High Point Retreat); 4 complete 2. Two personal braziers 7636 in the shrine " +
                "squares by Bargas; one use takes the 4 icons, counts 4 shards and closes both. Items the client block.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                ExistingFactObjectiveIds = new() { Euthanize },
                Items = new()
                {
                    new MissionItemBinding(IconKey, IconTemplate, MissionItemScope.CharacterOwned, Icons,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { Machina }, Euthanize, MarshesMissionDataV1.MapContextId, 100, 1))
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Altars.Length; index++)
            {
                var (role, at) = Altars[index];
                var sequence = (uint)index + 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, BrazierClass, at,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Place, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                var placed = new SceneSequenceDefinition
                {
                    World = { new TransitionObjectStateIntent($"light-{role}", role, LitState) },
                    Character = { new ConsumeMissionItemIntent($"icons-at-{role}", MissionId, IconKey, Icons, MissionItemScope.CharacterOwned) }
                };
                foreach (var (other, _) in Altars)
                    placed.World.Add(new SetInteractionIntent($"close-{other}-after-{role}", other, false));
                for (var shard = 1U; shard <= Icons; shard++)
                    placed.Signals.Add(new SceneMissionSignal(MissionId, Returned, shard));
                scene.Sequences[sequence] = placed;
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
