using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Departed (native mission 1143), Ligo Crucible: Private Drake at Outpost Intrepid asks for the dogtags of the AFS
    /// soldiers who fell in the ruins of Fort Intrepid.
    ///
    /// Native: texts 8353-8356, 8357 (missionconversation): log "Scout the ruins of Fort Intrepid and retrieve the dogtags
    /// from the corpses of any AFS soldiers that you find. Bring the tags back to Private Drake at Outpost Intrepid.", offer
    /// "The Bane are still patrolling the ruins of the fort looking for survivors", turn-in "Pretty grim up there, ain't
    /// it?"; objectives (missionobjective) 1 "Collect 12 Sets of Dogtags." 8358/8359, no counter; 2-21 "no desc" 9719-9758
    /// (twenty hidden ones, no texts); 22 "Item Tracker" 20331/20332 (20332: empty text in the client), counter 20333
    /// "ItemCheck". No objective conversation.
    /// Item class 24589 Fort Intrepid Dogtags (template 50213). Usable name 12 "Fort Intrepid Casualty"
    /// (usablenameoverride CRUCIBLE_FORTINTREPID_SOLDIER).
    ///
    /// Sourced: TaRapedia The Departed: giver and reward giver Private Drake (Outpost Intrepid); no requirement; 75,000 XP,
    /// 5,700 credits; one of Voltrox Bio Armor Legs (icon 3), Titan Mech Armor Gloves (5), Titan Reflective Armor Gloves (5),
    /// Luminar Graviton Armor Helmet (3); "the corpses of 12 AFS soldiers from the ruins of Fort Intrepid". TR MMO DB quest
    /// list: level 40, 75000, 5700.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Twenty personal AFS corpses (the twenty hidden objectives): the client's human soldier corpses
    ///    UsableTreasureDispHumCorpseMale V01-V04 (24995, 7865, 24989, 24990; closed 200, opened 201; usabledata None: the
    ///    client sends 1), named "Fort Intrepid Casualty" (12), as the Caves of Donn's fallen soldier (7865, name 336). They
    ///    lie in the ruins east of the control point, 45-99 m from Executioner Derge's spot (757, 549), on the client's floor
    ///    (probe of 2026-11-07). One use gives one set of dogtags; hidden 22 counts the uses, 12 complete it and 1, and then
    ///    all twenty corpses go (the searched ones too). The client's twenty per-corpse objectives are not written: one count of distinct corpses does
    ///    their work.
    ///  - The item counter on 1 (n/12). Drake takes the tags at the turn-in.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40: Bio V03 Legs 19802,
    ///    Mech Gloves 34116 (V04: the client has no Uncommon V05 gloves), Reflective Gloves 36488 (V03, the same), Graviton
    ///    V03 Helmet 24474; one to choose. One marker round the corpses.
    /// </summary>
    public static class CrucibleDepartedV1
    {
        public const uint MissionId = 1143;
        public const uint Level = 40;
        public const uint Drake = CrucibleOutpostIntrepidNpcsV1.Drake;
        public const uint Collect = 1, Tracker = 22;
        public const uint Tags = 12;
        public const uint Taken = 1;                    // the scenario of the corpses' uses
        public const uint RestLeft = 100;               // the scenario (and scene sequence) that takes all twenty corpses away: above the uses' 1-20

        public const uint TagsTemplate = 50213, TagsClass = 24589;
        public const string TagsKey = "fort-intrepid-dogtags";
        public static readonly uint[] CorpseClasses = { 24995, 7865, 24989, 24990 };   // UsableTreasureDispHumCorpseMaleV01-V04
        public const uint CorpseName = 12;              // usablenameoverride CRUCIBLE_FORTINTREPID_SOLDIER "Fort Intrepid Casualty"
        public const uint UseArgument = 1;              // usabledata lookup[class][0] is None: the client sends 1
        public const uint ClosedState = 200, OpenedState = 201;     // USE_TD_STATE_CLOSED, USE_TD_STATE_OPENED

        /// <summary>The twenty fallen in the ruins of Fort Intrepid (client floor heights).</summary>
        public static readonly ScenePosition[] CorpsesAt =
        {
            new(798.69f, 168.08f, 567.74f), new(807.24f, 168.08f, 551.81f), new(794f, 168.08f, 588f), new(813.75f, 168.08f, 577.63f),
            new(799.85f, 168.08f, 534.58f), new(811.13f, 168.27f, 595.66f), new(829.88f, 169.07f, 566.97f), new(824.14f, 168.08f, 544.31f),
            new(776.87f, 168.62f, 595.66f), new(794f, 171.46f, 606f), new(829.33f, 168.08f, 586.71f), new(796.95f, 168.08f, 516.09f),
            new(757.55f, 168.08f, 595.23f), new(817.05f, 169.04f, 612.84f), new(846.35f, 168.08f, 577.26f), new(847f, 168.08f, 553.65f),
            new(837.51f, 168.34f, 532.02f), new(770.95f, 168.08f, 612.84f), new(794f, 174.65f, 624f), new(840.91f, 168.43f, 601.41f)
        };

        /// <summary>The marker round the corpses: their centre and a radius that takes in all of them.</summary>
        public static readonly (double X, double Y, double Z) FieldAt = (802.3, 168.5, 570);
        public const double FieldRadius = 70;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint BioLegs = 19802;              // Armor_T3_Bio_V03_UNC_Legs_40_to_44
        public const uint MechGloves = 34116;           // Armor_T3_Mech_V04_UNC_Gloves_40_to_44
        public const uint ReflectiveGloves = 36488;     // Armor_T2_Reflective_V03_UNC_Gloves_39_to_43
        public const uint GravitonHelmet = 24474;       // Armor_T3_Graviton_V03_UNC_Helmet_40_to_44

        public static string CorpseRole(int index) => $"fort-intrepid-casualty-{index + 1}";
        public static uint CorpseSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Departed", 8353, Drake, Drake, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, MechGloves, ReflectiveGloves, GravitonHelmet });
            mission.Scenario(Taken, "A set of dogtags taken from the fallen", (uint)CorpsesAt.Length);
            mission.Scenario(RestLeft, "Twelve sets taken: all the fallen go", 0);

            mission.Objective(Collect, 8358, 8359, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tracker, 20331, 20332, 1, MissionObjectiveState.Incomplete, counterTextId: 20333, required: false);   // hidden

            mission.Transition(Tracker);
            for (var corpse = 1U; corpse <= CorpsesAt.Length; corpse++)
                mission.Progress(Tracker, MissionProgressEventKind.ScenarioEvent, corpse, Tags, counter: Taken, trigger: corpse);
            mission.Action(Tracker, 1, MissionActionKind.StartScenario, scenario: RestLeft);
            mission.Transition(Collect);
            mission.ObjectiveState(Collect, Tracker, MissionObjectiveState.Completed);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Collect, 1, FieldAt.X, FieldAt.Y, FieldAt.Z, FieldRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1143",
                "Native1 'Collect 12 Sets of Dogtags.' 8358/8359, no counter; 2-21 'no desc' 9719-9758 (hidden); 22 'Item Tracker' " +
                "20331/20332 (20332 is the client's body id, its text is empty) counter20333 'ItemCheck'; no objectiveconversation. Texts 8353-8357. Item class24589 Fort Intrepid " +
                "Dogtags (template50213). Usable name 12 'Fort Intrepid Casualty'. Name 9061 Private Drake.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Departed",
                "Giver/reward giver Private Drake (Outpost Intrepid, 952, 144, 110); no requirement; 75000XP, 5700 credits; one of " +
                "Voltrox Bio Armor Legs 3, Titan Mech Armor Gloves 5, Titan Reflective Armor Gloves 5, Luminar Graviton Armor Helmet " +
                "3; corpses of 12 AFS soldiers in the ruins of Fort Intrepid. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleDepartedV1",
                "Twenty personal human corpses (24995, 7865, 24989, 24990; name 12; arg1; 200->201) in the ruins east of the control " +
                "point; one use gives one set of tags; hidden 22 counts 12 distinct ones and completes 1; then all twenty go. The " +
                "item counter on 1 (n/12). Drake takes the tags. Items 19802, 34116, 36488, 24474, one to choose. One marker.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own fallen in the ruins, each searched once for its tags.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tracker },
                Items = new()
                {
                    new MissionItemBinding(TagsKey, TagsTemplate, MissionItemScope.AssignmentIssued, Tags,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        CounterObjectiveId: Collect)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var rest = new SceneSequenceDefinition();
            for (var index = 0; index < CorpsesAt.Length; index++)
            {
                var role = CorpseRole(index);
                var sequence = CorpseSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CorpseClasses[index % CorpseClasses.Length],
                    CorpsesAt[index], Orientation: index * 1.9, InitialObjectState: ClosedState, NameOverrideId: CorpseName,
                    UseAction: new SceneObjectAction(MissionId, Collect, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-searched", role, OpenedState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-dogtags", MissionId, TagsKey, TagsTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Taken, (uint)index + 1) }
                };
                rest.World.Add(new RemoveActorIntent($"{role}-left", role));
            }
            scene.Sequences[RestLeft] = rest;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
