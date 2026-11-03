using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Core of the Matter (native mission 1878), Valverde Marshes: Sergeant McDonald overheard Lt. Kiang sending the
    /// player to the Bane base by the supply depot; he wants two Bane power cores from the crates there to power the outpost.
    ///
    /// Native: texts 19174-19178 (missionconversation): log "Gather 2 Bane power cores from the crates in the eastern Bane
    /// base near the Bane Supply Depot.", offer "there are crates containing power cores ready to be shipped out all over the
    /// place ... If you could snag two power cores for me", turn-in "I'll start the modifications now"; objective 1 "Gather 2
    /// Bane power cores from the crates in Bane base near Bane Supply Depot." 19556/19557, no counter (missionobjective); no
    /// objective conversation. Item Bane Power Core class 28635 MisMarshesBanePowerCore (World DB template 118999). Client
    /// item templates 121229-121232: Bio Gloves, Graviton Helmet, Mech Gloves, Stealth Gloves 35-39.
    ///
    /// Sourced: TaRapedia The Core of the Matter: giver and receiver Sergeant McDonald; requires Under the Radar; 150,000 XP,
    /// 7,600 credits; 2 power cores; "boxes containing power cores ready to be shipped out all over the place" near the supply
    /// depot. Ten Ton Hammer Eastern Marshes (the group-3 mapping: its text filed under 914 is 1878's): the crates "all over
    /// the Bane Base in the southeast"; a Graviton Helmet or Bio, Mech or Stealth Gloves. World DB: the Bane Supply Depot door
    /// (452, 224, -582), Conscript Kaav's place (440, 223.68, -590).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 37 (the sentry tower set's; TaRapedia none).
    ///  - Three personal Bane item crates (11116, as Hard Core's) in the base in front of the depot door, on the navmesh and
    ///    walked to from the Bane Assertion Camp waypoint (probe of 2026-11-02); one core each. A hidden tally (objective 2,
    ///    under 1's texts: 1 has no client counter text) counts two, and 1 completes with it; the third crate then closes
    ///    with the objective. McDonald takes the cores at the turn-in; an abandon takes them.
    ///  - The rewards the client's block 121229-121232, one to choose (Ten Ton Hammer's four). One unnamed marker round the
    ///    three crates.
    ///
    /// V2 (2026-11-03, to-fix 14; migration 20271104002100_Marshes_core_of_the_matter_v2 writes it in place of
    /// MarshesCoreOfTheMatterV1, which the live server applied): the second crate stood 2.5 m from the depot door's map link
    /// 41 (radius 6 at 451.6, -581.9) and the third 15 m east of it, past the door from the first: a player walking up to
    /// the second, or from the first to the third, is taken into the depot and his use is refused there ("not on the current
    /// map instance"). All three now stand west of the door, 19-34 m from the link, on the walked-to ground (navmesh probe
    /// of 2026-11-03): (420, -590) as before, (428, -578), (435, -572); the marker round them (r20).
    ///
    /// V3 (2026-11-03, to-fix 24; migration 20271104002900_Marshes_on_the_client_floor writes it in place of V2): the third
    /// crate (435, 226.08, -572) stood where the client has no collision floor at all under it (MapGeometry of the client
    /// files: the depot's 223.6 m floor ends at z -577; the navmesh polygon reached past it). It now stands on that floor at
    /// (434, 224.18, -580), 0.57 m over it as the second crate (ESTIMATE: the crate's origin height), 17.7 m from link 41.
    /// </summary>
    public static class MarshesCoreOfTheMatterV3
    {
        public const uint MissionId = 1878;
        public const uint Level = 37;
        public const uint McDonald = MarshesSentryTowerNpcsV1.McDonald;
        public const uint Gather = 1;
        public const uint Tally = 2;                    // hidden: the two cores (1 has no client counter text)
        public const uint CoresTaken = 1;

        public const uint CoreTemplate = 118999, CoreClass = 28635, Cores = 2;
        public const string CoreKey = "bane-power-core";
        public const uint CrateClass = 11116;           // UsableItemDisp_item_crate_bane_v01
        public const uint UseArgument = Pools.PoolsSmashAndGrabV1.UseArgument;
        public const uint ReadyState = Pools.PoolsSmashAndGrabV1.ReadyState, TakenState = Pools.PoolsSmashAndGrabV1.TakenState;
        public static readonly ScenePosition[] Crates =
        {
            new(420f, 224.17f, -590f),
            new(428f, 224.18f, -578f),
            new(434f, 224.18f, -580f),
        };
        public static readonly (double X, double Y, double Z) BaseAt = (428, 224.5, -580);
        public const double BaseRadius = 20;

        public const uint Experience = 150000;
        public const uint Credits = 7600;
        public const uint BioGloves = 121229;           // Armor_T3_Bio_V04_UNC_Gloves_35_to_39
        public const uint GravitonHelmet = 121230;      // Armor_T3_Graviton_V04_UNC_Helmet_35_to_39
        public const uint MechGloves = 121231;          // Armor_T3_Mech_V04_UNC_Gloves_35_to_39
        public const uint StealthGloves = 121232;       // Armor_T3_Stealth_V04_UNC_Gloves_35_to_39

        public static string CrateRole(int index) => $"core-crate-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Core of the Matter", 19174, McDonald, McDonald, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves });
            mission.Prerequisite(MarshesUnderTheRadarV1.MissionId);

            mission.Objective(Gather, 19556, 19557, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 19556, 19557, 1, MissionObjectiveState.Incomplete);
            mission.Scenario(CoresTaken, "A Bane power core taken from a crate", (uint)Crates.Length);
            mission.Transition(Gather);
            mission.ObjectiveState(Gather, Tally, MissionObjectiveState.Completed);
            mission.Action(Gather, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Tally);
            for (var id = 1U; id <= Crates.Length; id++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Cores, counter: CoresTaken, trigger: id);
            mission.Indicator(Gather, 1, BaseAt.X, BaseAt.Y, BaseAt.Z, BaseRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1878",
                "Native1 'Gather 2 Bane power cores from the crates in Bane base near Bane Supply Depot.' 19556/19557, no counter; " +
                "no objectiveconversation. Texts 19174-19178 (crates of power cores all over the place). Item class28635 Bane " +
                "Power Core (template118999). Templates 121229-121232.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Core_of_the_Matter",
                "Giver/receiver Sergeant McDonald; requires Under the Radar; 150000XP, 7600 credits; 2 power cores from boxes near " +
                "the supply depot. Ten Ton Hammer (filed under 914): crates all over the Bane Base in the southeast; Graviton " +
                "Helmet or Bio/Mech/Stealth Gloves.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesCoreOfTheMatterV3",
                "Level37. Three personal Bane crates 11116 (arg3) west of the depot door (420,-590), (428,-578), (434,-580), a " +
                "core (118999) each; a hidden tally2 counts two, 1 completes with it; McDonald takes them. Items the client block 121229-121232, one to choose. " +
                "One marker round the crates.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(CoreKey, CoreTemplate, MissionItemScope.AssignmentIssued, Cores,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: Cores)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Crates.Length; index++)
            {
                var sequence = (uint)index + 1;
                var role = CrateRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, Crates[index],
                    Orientation: index * 1.0471975512, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, Gather, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-core", MissionId, CoreKey, CoreTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, CoresTaken, sequence) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
