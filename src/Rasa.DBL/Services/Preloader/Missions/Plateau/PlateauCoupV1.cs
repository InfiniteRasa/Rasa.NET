using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Coup (native mission 878), Maligo Base (squad instance 1830), Level 2: a wounded prisoner sends the player
    /// to Squad Leader Altera, who leads the AFS prisoners' revolt; supplies and ammunition for the barricade, a
    /// Bane counter-attack, then the control room to the West to open the prison cells.
    ///
    /// Native: texts 4599-4602 and 9544 (missionconversation): the Downed Prisoner's offer "They've taken
    /// them...to the...wormhole ... you must find ...Altera. He's....to the north west...", Altera's turn-in "Now
    /// that the prisoners have been freed ... Take this as a personal token of appreciation"; objectives
    /// (missionobjective) 1 "Locate and speak with Squad Leader Altera" 4826/9545, 2 "Find Ammunition and Supplies
    /// for the prisoners to the south" 4828/9546 counter 19816 "HiddenCount", 4 "Storm the control room to the
    /// West and release the prisoners." 4832/9548 counter 6168 (no text), 5 "Regroup back with Squad Leader
    /// Altera" 6086/9547, 6 "Report to Information Specialist Neuman" 19817/19818; the talks (objectiveconversation):
    /// 1 Altera 980 (4827: "I've been trying to get supplies and ammo back to the barricade we setup to the south
    /// ... As for the ammo? You'd be best served by picking whatever you can off the wounded."), 6 Neuman 981
    /// (19819: "Squad Leader Altera sent you? ... we're short on supplies and ammo. Please, get out there and find
    /// whatever you can!"), 5 Altera 980 (6166: "With those supplies brought to the barricade ... They're
    /// controlling the cell block via the control room up ahead ... It sounds like the doors opened up down the
    /// hallway ... Ready your weapons...they may be coming..."). Items: MaligoAmmoCrate (class 21437, template
    /// 42442), MaligoSupplyCrate (21438, 42443).
    ///
    /// Sourced: TaRapedia Coup: giver Downed Prisoner, reward from Squad Leader Alter, requires Showdown; 50,000
    /// XP, 1,000 credits; four objectives (no Neuman: she is a later addition). TR MMO DB: level 30. Ten Ton Hammer
    /// (Maligo upper level): corpses give ammo, the storage rooms supplies, the supplies go to Neuman; then a Bane
    /// wave attacks; a terminal opens the cells and the prisoners run free.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23):
    ///  - The order of the talks: 1 Altera, 6 Neuman at the barricade (her line asks for the supplies), 2 the
    ///    supplies, 5 back to Altera (Neuman's reminder 6181 sends the player back to him), 4 the control room.
    ///  - 2: three supply crates in the storage rooms and three ammunition boxes by the massacre's dead, each a
    ///    personal Bane Cargo Crate (class 21568, as Bomb Voyage's) that gives its item once and closes; six
    ///    distinct uses complete 2. The items go at the end (cleanup Remove).
    ///  - 5: four of Ustor Yard's Bane security (PlateauUstorYardV1.Security) come down the hallway and attack.
    ///  - 4: a personal console (UsableTwoStateBaneConsoleSwitchV01, 7468, as Silent But Deadly's) in the control
    ///    room to the West (the client map's RoomControl24mV07 and its command center console at (-350, -98, 149)).
    ///    No freed prisoners are shown (TTH's cutscene).
    ///  - No item reward ("a personal token": the sources name none). Level 30 (TR MMO DB).
    ///  - Indicators (in the instance too; ESTIMATE places, as the NPCs' and objects'): 1 and 5 at Altera, 6 at Neuman
    ///    (PlateauMaligoBaseV1's pools), 2 at each of the six crates, 4 at the control room console.
    /// </summary>
    public static class PlateauCoupV1
    {
        public const uint MissionId = 878;
        public const uint Level = 30;
        public const uint Giver = PlateauMaligoBaseV1.DownedPrisoner, Altera = PlateauMaligoBaseV1.Altera;
        public const uint MeetAltera = 1, Gather = 2, Release = 4, Regroup = 5, ReportNeuman = 6;
        public const uint Crates = 1, ConsoleUsed = 2, CratesOpen = 3, WaveComes = 4, ConsoleOpen = 5;

        public const uint SupplyItem = 42443, SupplyClass = 21438, AmmoItem = 42442, AmmoClass = 21437;
        public const string SupplyKey = "maligo-supplies", AmmoKey = "maligo-ammunition";
        public const uint CrateClass = PlateauBombVoyageV1.CrateClass, CrateUseArgument = PlateauBombVoyageV1.UseArgument,
            CrateState = PlateauBombVoyageV1.CrateState;
        public const uint ConsoleClass = PlateauSilentButDeadlyV1.ConsoleClass;
        public const uint ConsoleReadyState = PlateauSilentButDeadlyV1.ConsoleReadyState, ConsoleSetState = PlateauSilentButDeadlyV1.ConsoleSetState;
        public const string ConsoleRole = "prison-console";

        public const double NpcRadius = 8, CrateRadius = 4, ConsoleRadius = 8;

        public const uint Experience = 50000;
        public const uint Credits = 1000;

        /// <summary>Supplies, in the storage rooms (ServiceRoom16m) south of Altera.</summary>
        public static readonly ScenePosition[] SuppliesAt = { new(90, -98.88f, 166), new(2, -98.88f, 24), new(-30, -90.64f, 56) };

        /// <summary>Ammunition, by the massacre's dead in the control room by the Level 2 teleporter.</summary>
        public static readonly ScenePosition[] AmmoAt = { new(80, -100.28f, 120), new(100, -100.2f, 136), new(102.55f, -99.88f, 118.78f) };

        public static readonly ScenePosition ConsoleAt = new(-345.13f, -100.28f, 140);

        /// <summary>The Bane wave, in the hallway east of Altera's control room.</summary>
        public static readonly ScenePosition[] WaveAt =
        {
            new(-97.13f, -97.88f, 128), new(-97.53f, -97.48f, 122), new(-100, -96.45f, 136), new(-104.31f, -99.88f, 120.2f)
        };

        public static string SupplyRole(int index) => $"supplies-{index + 1}";
        public static string AmmoRole(int index) => $"ammunition-{index + 1}";
        public static string WaveRole(int index) => $"wave-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Coup", 4599, Giver, Altera, Level);
            mission.Reward(Experience, Credits, System.Array.Empty<uint>());
            mission.Prerequisite(PlateauShowdownV1.MissionId);

            mission.Objective(MeetAltera, 4826, 9545, 0, MissionObjectiveState.Incomplete);
            mission.Objective(ReportNeuman, 19817, 19818, 1, MissionObjectiveState.Inactive);
            mission.Objective(Gather, 4828, 9546, 2, MissionObjectiveState.Inactive, counterTextId: 19816);
            mission.Objective(Regroup, 6086, 9547, 3, MissionObjectiveState.Inactive);
            mission.Objective(Release, 4832, 9548, 4, MissionObjectiveState.Inactive);
            var crates = (uint)(SuppliesAt.Length + AmmoAt.Length);
            mission.Scenario(Crates, "Supplies and ammunition gathered", crates);
            mission.Scenario(ConsoleUsed, "The prison cells opened", 1);
            mission.Scenario(CratesOpen, "The supply crates and ammunition boxes set out", 0);
            mission.Scenario(WaveComes, "The Bane come down the hallway", 0);
            mission.Scenario(ConsoleOpen, "The control room console set up", 0);

            mission.Transition(MeetAltera);
            mission.Conversation(MeetAltera, PlateauMaligoBaseV1.AlteraPackage);
            PlateauSilentButDeadlyV1.Open(mission, MeetAltera, 1, ReportNeuman);

            mission.Transition(ReportNeuman);
            mission.Conversation(ReportNeuman, PlateauMaligoBaseV1.NeumanPackage);
            PlateauSilentButDeadlyV1.Open(mission, ReportNeuman, 1, Gather);
            mission.Action(ReportNeuman, 3, MissionActionKind.StartScenario, scenario: CratesOpen);

            mission.Transition(Gather);
            for (var crate = 1U; crate <= crates; crate++)
                mission.Progress(Gather, MissionProgressEventKind.ScenarioEvent, crate, crates, counter: Crates, trigger: crate);
            PlateauSilentButDeadlyV1.Open(mission, Gather, 1, Regroup);

            mission.Transition(Regroup);
            mission.Conversation(Regroup, PlateauMaligoBaseV1.AlteraPackage);
            PlateauSilentButDeadlyV1.Open(mission, Regroup, 1, Release);
            mission.Action(Regroup, 3, MissionActionKind.StartScenario, scenario: WaveComes);
            mission.Action(Regroup, 4, MissionActionKind.StartScenario, scenario: ConsoleOpen);

            mission.Transition(Release);
            mission.Progress(Release, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ConsoleUsed);
            mission.Action(Release, 1, MissionActionKind.GrantReward, reward: 1);

            var altera = PlateauMaligoBaseV1.AlteraAt;
            var neuman = PlateauMaligoBaseV1.NeumanAt;
            mission.Indicator(MeetAltera, 1, altera.X, altera.Y, altera.Z, NpcRadius);
            mission.Indicator(ReportNeuman, 2, neuman.X, neuman.Y, neuman.Z, NpcRadius);
            var crateAt = SuppliesAt.Concat(AmmoAt).ToArray();
            for (var index = 0; index < crateAt.Length; index++)
                mission.Indicator(Gather, 3 + (uint)index, crateAt[index].X, crateAt[index].Y, crateAt[index].Z, CrateRadius);
            mission.Indicator(Regroup, 9, altera.X, altera.Y, altera.Z, NpcRadius);
            mission.Indicator(Release, 10, ConsoleAt.X, ConsoleAt.Y, ConsoleAt.Z, ConsoleRadius);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = PlateauMaligoBaseV1.MapContextId,
                MapContextId = PlateauMaligoBaseV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(SupplyKey, SupplyItem, MissionItemScope.AssignmentIssued, (uint)SuppliesAt.Length,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(AmmoKey, AmmoItem, MissionItemScope.AssignmentIssued, (uint)AmmoAt.Length,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var open = new SceneSequenceDefinition();
            for (var index = 0; index < crates; index++)
            {
                var supply = index < SuppliesAt.Length;
                var role = supply ? SupplyRole(index) : AmmoRole(index - SuppliesAt.Length);
                var at = supply ? SuppliesAt[index] : AmmoAt[index - SuppliesAt.Length];
                var used = 11U + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CrateClass, at,
                    InitialObjectState: CrateState, UseAction: new SceneObjectAction(MissionId, Gather, used, CrateUseArgument));
                open.World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[used] = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"close-{role}", role, false) },
                    Character =
                    {
                        new IssueMissionItemIntent($"take-{role}", MissionId, supply ? SupplyKey : AmmoKey, supply ? SupplyItem : AmmoItem, 1)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Crates, (uint)index + 1) }
                };
            }
            scene.Sequences[CratesOpen] = open;

            var wave = new SceneSequenceDefinition();
            for (var index = 0; index < WaveAt.Length; index++)
            {
                var role = WaveRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauUstorYardV1.Security, WaveAt[index],
                    Orientation: 1.5708, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                wave.World.Add(new EnsureActorIntent($"{role}-comes", role));
                wave.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
            }
            scene.Sequences[WaveComes] = wave;

            scene.Actors[ConsoleRole] = new SceneActorDefinition(ConsoleRole, SceneActorKind.Object, ConsoleClass, ConsoleAt,
                InitialObjectState: ConsoleReadyState, UseAction: new SceneObjectAction(MissionId, Release, 31, 1));
            scene.Sequences[ConsoleOpen] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-console", ConsoleRole) } };
            scene.Sequences[31] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("cells-open", ConsoleRole, ConsoleSetState),
                    new SetInteractionIntent("console-done", ConsoleRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, ConsoleUsed, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/878",
                "Native1 Altera 4826/9545, 2 supplies 4828/9546 counter19816 'HiddenCount', 4 control room 4832/9548 counter6168, 5 " +
                "regroup 6086/9547, 6 Neuman 19817/19818; objectiveconversation 878,1,980 text4827, 878,6,981 text19819, 878,5,980 " +
                "text6166. Texts 4599-4602, 9544. MaligoAmmoCrate class21437/template42442, MaligoSupplyCrate 21438/42443.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Coup",
                "Giver Downed Prisoner, reward from Squad Leader Alter; requires Showdown; 50000XP, 1000 credits. TR MMO DB level30. " +
                "Ten Ton Hammer guide-to-maligo-base-upper-level: ammo from corpses, supplies from storage rooms, to Neuman; a Bane " +
                "wave; a terminal opens the cells.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauCoupV1",
                "Level30. Order 1, 6, 2, 5, 4. Six personal crates21568 (3 supplies, 3 ammunition) each give an item once; on 5 four " +
                "security650062 attack; console7468 in the West control room opens the cells. No item reward. Places are estimates. " +
                "Indicators at Altera, Neuman, the crates and the console.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
