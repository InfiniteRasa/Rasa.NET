using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauSelfPreservationV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Self Preservation (native mission 879), written again with an Explosive Charge beside each Security CPU
    /// (to-fix #54). Everything else is as PlateauSelfPreservationV1 (its texts, objectives, rewards, switches, fields,
    /// indicators and counters; its constants are used here), which stays as it was for its first migration.
    ///
    /// Native: the CPU classes 10191 and 21000 (UsableInertDestBaneSecurityMachineryV01/V02) are destroyable in the
    /// client's class data, but the real client does not take them as weapon targets: after the green switch the server
    /// sent the CPU targetable, and every shot at 2.8-5.7 m gave "no target" (to-fix #54; the same as the 1189 fuel pumps
    /// and the 1187 generator, to-fix #30 and #45).
    ///
    /// Reconstructed (ESTIMATE, as Interception's pumps and Silent But Deadly's generator, PlateauInterceptionV1 and
    /// PlateauSilentButDeadlyV1):
    ///  - One Explosive Charge (UsableBombHumV01 7870, use argument 1: usabledata has no entry, so the client's default)
    ///    about 5 m from each CPU, inside its room, between the room's opening (its field) and the CPU, on the navmesh's
    ///    ground and clear of the room's consoles and chairs (client map collision, tools/stuckprobe geo and rays: 3 m
    ///    free all round). Each CPU's indicator (radius 8) covers its charge.
    ///  - A charge is not usable until its room's switch is used (the switch's sequence sets it usable, where it set the
    ///    CPU open to fire before). Its use blows its CPU (the same destroyed state 2 and its destruction, so that the
    ///    client shows the explosion: DestroyableStates) and counts objective 1's counter (Destroyed, 3); the charge is
    ///    then no longer usable.
    ///  - The CPUs are not shot any more: never interactable, so no target (PracticeTargetManager). The destruction
    ///    entry stays only for the blow's look (PlateauSilentButDeadlyV1.GeneratorHitPoints).
    /// </summary>
    public static class PlateauSelfPreservationV2
    {
        public const uint ChargeClass = PlateauSilentButDeadlyV1.ChargeClass;   // UsableBombHumV01 "Explosive Charge"
        public const uint ChargeUseArgument = 1;

        /// <summary>
        /// Each CPU's charge (the index of PlateauSelfPreservationV1.Cpus), 5 m from it towards the room's opening, on the
        /// navmesh's ground (ESTIMATE places).
        /// </summary>
        public static readonly (ScenePosition At, string Where)[] ChargesAt =
        {
            (new(42, -130.8f, 315), "5 m south of CPU 1, 2.5 m inside the north-east room's field (z 312.5)"),
            (new(-15, -130.88f, 328), "5 m west of CPU 2, 6.8 m inside the north room's field (x -21.8), between its consoles"),
            (new(-45, -138.9f, 315), "5 m south of CPU 3, 1.5 m inside the north-west room's field (z 313.5), between its consoles"),
        };

        public static string ChargeRole(uint sequence) => $"security-cpu-charge-{sequence}";

        /// <summary>
        /// The switches as PlateauSelfPreservationV1.Switches, but the yellow one 2.8 m south, onto the floor of the
        /// service turn (piece 4167) west of its field: at (-27, 331.3) it stood outside the turn's curved wall, with no
        /// client floor behind it and no line of sight from the corridor (to-fix #54 retest; tools/stuckprobe slice and
        /// column, navmesh region 27). ESTIMATE place.
        /// </summary>
        public static readonly (uint Objective, uint Sequence, string Colour, ScenePosition At, double Facing)[] Switches =
        {
            PlateauSelfPreservationV1.Switches[0],
            (3, 12, "yellow", new(-27, -130.88f, 328.5f), 3.1416),
            PlateauSelfPreservationV1.Switches[2],
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Self Preservation", 4603, null, null, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.RadioOfferAndCompletion(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent,
                MissionOfferSourceDefinition.MapArrivalKey, PlateauMaligoBaseV1.MapContextId));

            mission.Objective(Destroy, 4824, 11142, 0, MissionObjectiveState.Incomplete, counterTextId: 4825);
            foreach (var flip in Switches)   // hidden: a switch used; its field goes (PlateauSelfPreservationFieldsV1)
                mission.Objective(flip.Objective, 4824, 11142, flip.Objective - 1, MissionObjectiveState.Incomplete, required: false);
            mission.Scenario(Destroyed, "Security CPU Units destroyed", (uint)Cpus.Length);
            mission.Scenario(Switched, "Force field switches used", (uint)Switches.Length);
            for (var index = 0; index < Cpus.Length; index++)
            {
                mission.Indicator(Destroy, Cpus[index].Sequence, Cpus[index].At.X, Cpus[index].At.Y, Cpus[index].At.Z, CpuRadius);
                mission.Indicator(Destroy, 4 + (uint)index, Switches[index].At.X, Switches[index].At.Y, Switches[index].At.Z, SwitchRadius);
            }
            mission.Transition(Destroy);
            foreach (var cpu in Cpus)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, cpu.Sequence, (uint)Cpus.Length,
                    counter: Destroyed, trigger: cpu.Sequence);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < Switches.Length; index++)
            {
                mission.Transition(Switches[index].Objective);
                mission.Progress(Switches[index].Objective, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: Switched);
            }

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = PlateauMaligoBaseV1.MapContextId,
                MapContextId = PlateauMaligoBaseV1.MapContextId,
                HiddenObjectiveIds = new(Switches.Select(flip => flip.Objective))
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Cpus.Length; index++)
            {
                var cpu = Cpus[index];
                var role = CpuRole(cpu.Sequence);
                var charge = ChargeRole(cpu.Sequence);
                // Never a target (PracticeTargetManager: a disabled object is no target); its charge blows it.
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, cpu.Class, cpu.At,
                    Orientation: cpu.Sequence * 2.0943951024, InitiallyInteractable: false, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy, cpu.Sequence, CpuHitPoints, DestroyedState));
                // Not usable until its room's switch is used.
                scene.Actors[charge] = new SceneActorDefinition(charge, SceneActorKind.Object, ChargeClass, ChargesAt[index].At,
                    InitiallyInteractable: false,
                    UseAction: new SceneObjectAction(MissionId, Destroy, cpu.Sequence, ChargeUseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{charge}", charge));
                scene.Sequences[cpu.Sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new SetInteractionIntent($"set-{charge}", charge, false),
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Destroyed, cpu.Sequence) }
                };
            }
            for (var index = 0; index < Switches.Length; index++)
            {
                var flip = Switches[index];
                var role = SwitchRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SwitchClass, flip.At, Orientation: flip.Facing,
                    InitialObjectState: SwitchOff, NameOverrideId: SwitchName,
                    UseAction: new SceneObjectAction(MissionId, flip.Objective, flip.Sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[flip.Sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"flip-{role}", role, SwitchOn),
                        new SetInteractionIntent($"done-{role}", role, false),
                        new SetInteractionIntent($"arm-{ChargeRole(Cpus[index].Sequence)}", ChargeRole(Cpus[index].Sequence), true)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Switched, (uint)index + 1) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/879",
                "Native1 'Destroy all Security CPU Units' 4824/11142 counter4825 'Security CPU Units Online'; no objective " +
                "conversation. Texts 4603-4606, 11141. Security CPU Unit classes 10191/21000 (destroyable, augmentation41); none " +
                "on the Maligo v3 map. The real client does not take 10191 as a weapon target (to-fix #54).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Self_Preservation",
                "Giver Radio, reward from Radio, Maligo Base; 40000XP, 1000 credits. TR MMO DB level30. Ten Ton Hammer " +
                "guide-to-maligo-base-lower-level: radio near the first beams; three CPUs in the north of level 1 behind coloured " +
                "switches; 6200 credits and a choice of boots.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSelfPreservationV2",
                "Level30. Radio offer on arriving in Maligo1830, radio turn-in. Three personal CPUs (10191, 21000, 10191) in " +
                "the three dead-end rooms of the north of Level 1, counter of three; each behind a sealed field that its switch " +
                "(7467, hidden objectives 2-4) takes down and that makes the room's Explosive Charge7870 usable; the charge blows " +
                "the CPU (not shot: not client-targetable). Boots 120330-120333. Places are estimates. Indicators at the CPUs " +
                "(covering the charges) and the switches.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
