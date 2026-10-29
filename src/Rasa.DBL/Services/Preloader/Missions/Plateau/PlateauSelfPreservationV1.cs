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
    /// Self Preservation (native mission 879), Maligo Base (squad instance 1830): Colonel Bosley radios that security
    /// CPUs in the north of the first level guard the way to Level 2; destroy them all and radio back.
    ///
    /// Native: texts 4603-4606 and 11141 (missionconversation): offer "the security chambers may be located somewhere in
    /// the northern region of the first level ... These security CPUs may allow destruction through conventional
    /// firepower", turn-in "Damn fine job soldier! ... With those units offline, the 2nd floor should prove to be
    /// easier"; objective 1 "Destroy all Security CPU Units" 4824/11142, counter text 4825 "Security CPU Units Online"
    /// (missionobjective); no objective conversation. Object classes "Security CPU Unit" (physicalentityclassname):
    /// UsableInertDestBaneSecurityMachineryV01 10191 and V02 21000 (destroyable, augmentation 41). The client's Maligo
    /// map places none.
    ///
    /// Sourced: TaRapedia Self Preservation: giver and reward Radio, Maligo Base; 40,000 XP, 1,000 credits. TR MMO DB:
    /// level 30. Ten Ton Hammer (Maligo lower level, 2008-03): the radio call comes as the player nears the first laser
    /// beams; three Security CPUs in rooms in the north of the first level, behind coloured switches and force fields;
    /// it completes by radio, "6200 credits and choice of new boots".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23):
    ///  - Offered by radio on arriving in Maligo (with Showdown), turned in by radio.
    ///  - Three personal destroyable CPUs (100 hit points) in the north of Level 1
    ///    (+z north: see NOTES-bosley-followups.md), one in each of the three dead-end service rooms there (client map:
    ///    the rooms with a cap, ArchBaneIndustrialServiceRoom16m16mV02 at (42, -132, 328) and (-6, -132, 328), and the
    ///    lower room at (-46, -140, 312) down the ramp), by the rooms' consoles; one counter of three.
    ///  - TTH's coloured switches and force fields, simplified (ESTIMATE): a sealed Bane field across each room's one
    ///    opening (PlateauSelfPreservationFieldsV1) and a switch outside it (UsableTwoStateBaneSwitchV02 7467, named
    ///    "Force Field Controller", usablenameoverride 241; no coloured class in the client): green for the north-east
    ///    room, yellow for the north room, red for the north-west room. A switch's use completes its hidden objective
    ///    (2-4), whose transition takes its field down (TakeDownField), and lets its CPU be shot: a CPU cannot be hit
    ///    before (not interactable), so it is not shot through the field. TTH's puzzle (a switch opening another room,
    ///    the green one flipped twice) is not built.
    ///  - Rewards: TaRapedia's 40,000 XP and 1,000 credits; TTH's choice of boots as the four Uncommon V04 30-34 boots
    ///    120330-120333. Level 30 (TR MMO DB).
    ///  - Indicators (in the instance too), all on objective 1 (the switches' objectives are hidden): one at each CPU
    ///    (1-3) and one at each switch (4-6).
    /// </summary>
    public static class PlateauSelfPreservationV1
    {
        public const uint MissionId = 879;
        public const uint Level = 30;
        public const uint Destroy = 1, Destroyed = 1, Switched = 2;
        public const uint SwitchClass = 7467;           // UsableTwoStateBaneSwitchV02
        public const uint SwitchOff = 55, SwitchOn = 56, UseArgument = 1;
        public const uint SwitchName = 241;             // usablenameoverridelanguage "Force Field Controller"
        public const double CpuRadius = 8, SwitchRadius = 4;
        public const uint CpuHitPoints = 100, IntactState = 110, DestroyedState = 2;

        public const uint Experience = 40000;
        public const uint Credits = 1000;
        public const uint BioBoots = 120330;            // Armor_T3_Bio_V04_UNC_Boots_30_to_34
        public const uint GravitonBoots = 120331;       // Armor_T3_Graviton_V04_UNC_Boots_30_to_34
        public const uint MechBoots = 120332;           // Armor_T3_Mech_V04_UNC_Boots_30_to_34
        public const uint StealthBoots = 120333;        // Armor_T3_Stealth_V04_UNC_Boots_30_to_34

        /// <summary>The CPUs: scene sequence, class and place (Level 1, north), each in a dead-end room.</summary>
        public static readonly (uint Sequence, uint Class, ScenePosition At, string Where)[] Cpus =
        {
            (1, 10191, new(42, -130.88f, 320), "the north-east room, by its consoles"),
            (2, 21000, new(-12, -130.88f, 324), "the north room, by the consoles at (-15, -130, 327)"),
            (3, 10191, new(-46, -138.9f, 320), "the north-west room down the ramp, by its consoles"),
        };

        /// <summary>
        /// The switches, one per CPU (the same index): hidden objective, use sequence, colour and place, outside its room's
        /// field (ESTIMATE places, on the navmesh).
        /// </summary>
        public static readonly (uint Objective, uint Sequence, string Colour, ScenePosition At, double Facing)[] Switches =
        {
            (2, 11, "green", new(45.3f, -130.88f, 306), -1.5708),
            (3, 12, "yellow", new(-27, -130.88f, 331.3f), 3.1416),
            (4, 13, "red", new(-49.3f, -130.88f, 292), 1.5708),
        };

        public static string CpuRole(uint sequence) => $"security-cpu-{sequence}";
        public static string SwitchRole(int index) => $"{Switches[index].Colour}-switch";

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
            foreach (var cpu in Cpus)
            {
                var role = CpuRole(cpu.Sequence);
                // Not to be hit before its switch is used (PracticeTargetManager: a disabled object is no target).
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, cpu.Class, cpu.At,
                    Orientation: cpu.Sequence * 2.0943951024, InitiallyInteractable: false, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy, cpu.Sequence, CpuHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[cpu.Sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
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
                        new SetInteractionIntent($"expose-{CpuRole(Cpus[index].Sequence)}", CpuRole(Cpus[index].Sequence), true)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Switched, (uint)index + 1) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/879",
                "Native1 'Destroy all Security CPU Units' 4824/11142 counter4825 'Security CPU Units Online'; no objective " +
                "conversation. Texts 4603-4606, 11141. Security CPU Unit classes 10191/21000 (destroyable, augmentation41); none " +
                "on the Maligo v3 map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Self_Preservation",
                "Giver Radio, reward from Radio, Maligo Base; 40000XP, 1000 credits. TR MMO DB level30. Ten Ton Hammer " +
                "guide-to-maligo-base-lower-level: radio near the first beams; three CPUs in the north of level 1 behind coloured " +
                "switches; 6200 credits and a choice of boots.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSelfPreservationV1",
                "Level30. Radio offer on arriving in Maligo1830, radio turn-in. Three personal 100HP CPUs (10191, 21000, 10191) in " +
                "the three dead-end rooms of the north of Level 1, counter of three; each behind a sealed field that its switch " +
                "(7467, hidden objectives 2-4) takes down, and only then to be shot. Boots 120330-120333. Places are estimates. " +
                "Indicators at the CPUs and the switches.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
