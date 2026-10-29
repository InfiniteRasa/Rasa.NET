using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The three poison canisters at the Thrax Barracks, Valverde Pools, and the fork between Poison Control (1368,
    /// PoolsPoisonControlV1: destroy them for the Retreads) and Taking the Poison (1590, PoolsTakingThePoisonV1: collect
    /// them for the AFS).
    ///
    /// Native (objectiveconversation): each canister's control console is a package, 1641, 1642, 1643, on objectives 1, 3
    /// and 5 of both missions. Under flag 1 (only that mission taken) it offers its own command ("Self Destruct" in 1368,
    /// "Dispense Cannister" in 1590); under flag 446 (both) both commands: 1 "Self Destruct", 2 "Dispense Poison
    /// Cannister". "Capacity 75.0" / "72.7". Objectives 2, 4 and 6 "Tracker" have no use known.
    ///
    /// Sourced: TaRapedia Poison Control walkthrough: "I was able to accept this mission plus Taking the Poison at the same
    /// time. When you activate the canister's, you can issue the command to Destroy or Dispense the canister, depending on
    /// which mission you have (you can have both initially). Choosing a command will complete an objective for the
    /// corresponding mission, and fail the alternate mission." Taking the Poison: "If you choose Poison Control for Amee
    /// Corman at Retread Camp, then Corporal Sheen will no longer offer you this mission"; with it "the Retreads apparently do
    /// not offer you any further missions". Spy Networking's data: the canisters "in several locations within the barracks".
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: canisters at estimated places at the Thrax Barracks, a fork
    /// rule):
    ///  - Each mission sets down its own three personal consoles (class 10519 UsableNPCBaneKeyboard, as Bane
    ///    Investigation's), by three of the Barracks' Bane consoles on the map; 1590's 1.5 m beside 1368's. A console is a
    ///    menu (SceneObjectConversation.Menu): its flag-1 page while the other mission is not taken, its flag-446 page
    ///    while it is.
    ///  - The fork: the character's flag 1368001 holds the side chosen (1 Retreads, 2 AFS). The first command sets it; with
    ///    both missions taken it also fails the other one (FailRelatedMission). The other mission's command on a console is
    ///    a choice that keeps this objective open and signals the other mission's objective, which completes there and
    ///    fails this one. Neither mission is offered once the other is done or the other side is chosen. The side
    ///    stays chosen after an abandon (TaRapedia: the choice closes the other mission).
    ///  - Amee's soldiers ("My soldiers will help protect you") are left out.
    /// </summary>
    public static class PoolsPoisonCanistersV1
    {
        public const uint ForkFlag = 1368001, Retreads = 1, Afs = 2;
        public const uint SoloFlag = 1, BothFlag = 446;
        public const uint ConsoleClass = 10519;        // UsableNPCBaneKeyboard
        public const uint SignalScenario = 1;
        public const uint Solo = 1, ForkOwn = 2, ForkOther = 3, Signalled = 4;

        /// <summary>Canister I-III: objective, console package, place (1368's; 1590's 1.5 m east).</summary>
        public static readonly (uint Objective, uint Package, ScenePosition At)[] Canisters =
        {
            (1, 1641, new(492.0f, 679.44f, 446.5f)),
            (3, 1642, new(600.0f, 671.6f, 512.2f)),
            (5, 1643, new(630.5f, 685.4f, 581.6f)),
        };

        public static ScenePosition ConsoleAt(int index, bool afs) =>
            afs ? new(Canisters[index].At.X + 1.5f, Canisters[index].At.Y, Canisters[index].At.Z) : Canisters[index].At;

        public static string ConsoleRole(int index) => $"canister-{index + 1}";

        /// <summary>The scene sequence that signals the other mission's canister <paramref name="index"/>.</summary>
        public static uint SignalSequence(int index) => 11 + (uint)index;

        /// <summary>
        /// One side: <paramref name="own"/> with <paramref name="other"/>; <paramref name="ownSide"/> the fork flag's value;
        /// <paramref name="ownIsSelfDestruct"/> whether its command is the flag-446 page's first; <paramref name="item"/> per
        /// canister what its command gives (null for none).
        /// </summary>
        public static MissionSceneDefinition Build(PoolsMissionDataV1 mission, uint own, uint other, uint ownSide, uint otherSide,
            bool ownIsSelfDestruct, (uint Template, string Key)[] item, (uint Name, uint Body)[] texts)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = item == null ? null : item.Select(entry => new MissionItemBinding(entry.Key, entry.Template,
                    MissionItemScope.AssignmentIssued, 1, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove)).ToList(),
                Dialogue = new List<MissionDialogueTopicDefinition>(),
                // Not offered once the other is done or the other side is chosen.
                Requirement = new NotRequirement(new AnyRequirement(new MissionRequirement[]
                {
                    new FlagRequirement(ForkFlag, otherSide), new MissionStateRequirement(other)
                }))
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Scenario(SignalScenario, "A canister's command from the other mission's console", (uint)Canisters.Length);
            for (var index = 0; index < Canisters.Length; index++)
                mission.Scenario(SignalSequence(index), $"Canister {index + 1}: the other mission's command", 0);
            var otherTaken = new MissionStateRequirement(other, MissionState.Active);

            for (var index = 0; index < Canisters.Length; index++)
            {
                var (objective, package, _) = Canisters[index];
                var at = ConsoleAt(index, ownSide == Afs);
                mission.Objective(objective, texts[index].Name, texts[index].Body, (uint)index, MissionObjectiveState.Incomplete);

                void Command(uint transition, bool failOther)
                {
                    mission.Action(objective, 1, MissionActionKind.SetPlayerFlag, playerFlag: ForkFlag, playerFlagValue: ownSide,
                        transition: transition);
                    var next = 2U;
                    if (failOther)
                        mission.Action(objective, next++, MissionActionKind.FailRelatedMission, transition: transition,
                            itemIntent: new FailRelatedMissionIntent($"canister-{index + 1}-fails-{other}-{transition}", other));
                    if (item != null)
                        mission.Action(objective, next++, MissionActionKind.IssueMissionItem, transition: transition,
                            itemIntent: new IssueMissionItemIntent($"canister-{index + 1}-{transition}", own, item[index].Key,
                                item[index].Template, 1));
                    if (index == 0)
                        mission.Action(objective, next, MissionActionKind.GrantReward, reward: 1, transition: transition);
                }

                // Its own command, alone or with both taken; the other's, from this console; this one's, from the other's.
                mission.Transition(objective, Solo, Solo);
                mission.Conversation(objective, package, SoloFlag, Solo);
                Command(Solo, failOther: false);
                mission.Transition(objective, ForkOwn, ForkOwn);
                mission.Conversation(objective, package, BothFlag, ForkOwn);
                Command(ForkOwn, failOther: true);
                mission.Transition(objective, ForkOther, ForkOther, MissionObjectiveState.Incomplete);
                mission.Conversation(objective, package, BothFlag, ForkOther);
                mission.Action(objective, 1, MissionActionKind.StartScenario, scenario: SignalSequence(index), transition: ForkOther);
                mission.Transition(objective, Signalled, Signalled);
                mission.Progress(objective, Signalled, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: SignalScenario);
                Command(Signalled, failOther: true);
                mission.Indicator(objective, (uint)index + 1, at.X, at.Y, at.Z);

                scene.Dialogue.Add(new(objective, package, SoloFlag, MissionDialogueKind.Choice,
                    choices: new Dictionary<int, uint> { [1] = Solo, [2] = Solo }, requirement: new NotRequirement(otherTaken)));
                scene.Dialogue.Add(new(objective, package, BothFlag, MissionDialogueKind.Choice,
                    choices: ownIsSelfDestruct
                        ? new Dictionary<int, uint> { [1] = ForkOwn, [2] = ForkOther }
                        : new Dictionary<int, uint> { [1] = ForkOther, [2] = ForkOwn },
                    requirement: otherTaken));

                var role = ConsoleRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ConsoleClass, at,
                    Conversation: new SceneObjectConversation(own, objective, package, objective, SoloFlag,
                        MissionDialogueKind.Choice, Menu: true));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[SignalSequence(index)] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(other, SignalScenario, (uint)index + 1) }
                };
            }
            return scene;
        }
    }
}
