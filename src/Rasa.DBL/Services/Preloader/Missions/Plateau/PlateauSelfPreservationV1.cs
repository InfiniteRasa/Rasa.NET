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
    ///  - Three personal destroyable CPUs (as The Final Assault's stations, 100 hit points) in the north of Level 1
    ///    (+z north: see NOTES-bosley-followups.md), two by the client map's consoles there; one counter of three.
    ///    The coloured switches and force fields are not built (missing mechanic: a mission force field).
    ///  - Rewards: TaRapedia's 40,000 XP and 1,000 credits; TTH's choice of boots as the four Uncommon V04 30-34 boots
    ///    120330-120333. Level 30 (TR MMO DB). No indicators: instance.
    /// </summary>
    public static class PlateauSelfPreservationV1
    {
        public const uint MissionId = 879;
        public const uint Level = 30;
        public const uint Destroy = 1, Destroyed = 1;
        public const uint CpuHitPoints = 100, IntactState = 110, DestroyedState = 2;

        public const uint Experience = 40000;
        public const uint Credits = 1000;
        public const uint BioBoots = 120330;            // Armor_T3_Bio_V04_UNC_Boots_30_to_34
        public const uint GravitonBoots = 120331;       // Armor_T3_Graviton_V04_UNC_Boots_30_to_34
        public const uint MechBoots = 120332;           // Armor_T3_Mech_V04_UNC_Boots_30_to_34
        public const uint StealthBoots = 120333;        // Armor_T3_Stealth_V04_UNC_Boots_30_to_34

        /// <summary>The CPUs: scene sequence, class and place (Level 1, north).</summary>
        public static readonly (uint Sequence, uint Class, ScenePosition At, string Where)[] Cpus =
        {
            (1, 10191, new(24, -130.88f, 274), "by the console at (22, -129, 269)"),
            (2, 21000, new(-12, -130.88f, 324), "by the consoles at (-15, -130, 327)"),
            (3, 10191, new(-52, -130.88f, 288), "the north-west room"),
        };

        public static string CpuRole(uint sequence) => $"security-cpu-{sequence}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Self Preservation", 4603, null, null, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.RadioOfferAndCompletion(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent,
                MissionOfferSourceDefinition.MapArrivalKey, PlateauMaligoBaseV1.MapContextId));

            mission.Objective(Destroy, 4824, 11142, 0, MissionObjectiveState.Incomplete, counterTextId: 4825);
            mission.Scenario(Destroyed, "Security CPU Units destroyed", (uint)Cpus.Length);
            mission.Transition(Destroy);
            foreach (var cpu in Cpus)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, cpu.Sequence, (uint)Cpus.Length,
                    counter: Destroyed, trigger: cpu.Sequence);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauMaligoBaseV1.MapContextId
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var cpu in Cpus)
            {
                var role = CpuRole(cpu.Sequence);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, cpu.Class, cpu.At,
                    Orientation: cpu.Sequence * 2.0943951024, InitialObjectState: IntactState,
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
                "the north of Level 1, counter of three. No switches or force fields (missing mechanic). Boots 120330-120333. " +
                "Places are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
