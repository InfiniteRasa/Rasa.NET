using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Subversion (native mission 1451), Indra Caverns (squad instance 2055): Sergeant Jonas Drof, at the Gotha Processing Plant's
    /// entrance, sends the player in to sabotage the plant: the gas flow valves of the two gas processing rooms and the four Thrax
    /// life support systems of the sleep pods.
    ///
    /// Native: texts 12889-12893 (missionconversation); objectives (missionobjective) 1 "Sabotage Gotha Processing" 12896/12897
    /// counter 12898 "Sabotage Gas Flow Valves", 2 "Shut Down Thrax Life Support" 12899/12900 counter 12901 "Life Support
    /// Systems", 3-4 "Gas Flow Control (1 of 2)" / "(2 of 2)" 13735/13736, 13737/13738, 5-6 "Thrax Life Support System (1 of 4)" /
    /// "(2 of 4)" 13739/13740, 13741/13742, 7-8 (blank names) 13743/13744, 13745/13746; no objective conversation. Usable names
    /// (usablenameoverride) 152 Gas Flow Control, 153 Life Support System. Indicator names 216 "Gas Flow Controls", 217 "Thrax
    /// Life Support System".
    ///
    /// Sourced: TaRapedia Subversion (0.5): Drof gives; "completed by selecting the 'Radio HQ' button"; requires Meet Sergeant
    /// Drof; 9,000 credits (no experience given: a page bug); one of Vextronics Pulse Rifle, AccuMax Incendiary Staff, Phoenix
    /// Direct Repair Tool, Titan Tissue Extractor.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 44; taken and turned in at Drof's second row (AshenGothaPlantNpcsV1) in the squad's copy. The client first
    ///    (audit S1): the summary 12890 ends "Speak to Sergeant Drof for your reward", the completion text 12892 is Drof's and
    ///    the reminder 12893 is not empty; TaRapedia's "Radio HQ" turn-in is not followed.
    ///  - 3 and 4 the player's own Gas Flow Controls (UsableTwoStateBaneConsoleSwitchV01 7468, ready 55, set 56, argument 1, name
    ///    152), one in each gas processing room (Alpha east, Beta west: the client map's two processing wings); 5-8 the player's own
    ///    Life Support Systems (7468, name 153), one beside each of the four sleep pod groups (the client map's
    ///    ArchBaneSleepPodGroupV01, the south and the east barracks halls, upper and lower decks). A use sets the switch and
    ///    completes its objective (a scene result; in a squad's copy every member on 1451 is credited, GroupCreditService).
    ///  - 1 counts 3-4, 2 counts 5-8 (the scene's aggregations on counter 0); the reward with the last.
    ///  - TaRapedia's six Thrax at each use are not made (to-fix row 26: a solo player must win each fight; the plant's own pools
    ///    stand in the rooms): docs/ashen-playtest.md.
    ///  - Experience 110,000 (the page gives none; 1447's, the same level band). The Tissue Extractor: the level 44-48 Physical
    ///    Leech Gun 12053.
    ///  - Markers: 216 on each valve, 217 on each life support system; 1 and 2 on the rooms' middles.
    /// </summary>
    public static class AshenSubversionV1
    {
        public const uint MissionId = 1451;
        public const uint Level = 44;
        public const uint Drof = AshenGothaPlantNpcsV1.Drof;
        public const uint Caverns = AshenGothaPlantNpcsV1.Caverns;
        public const uint Valves = 1, LifeSupport = 2;
        public const uint Valve1 = 3, Support1 = 5;
        public const uint ValveCount = 2, SupportCount = 4;
        public const uint ValveIndicator = 216, SupportIndicator = 217;
        public const double ObjectMarkerRadius = 6, RoomMarkerRadius = 30;   // ESTIMATE

        public const uint SwitchClass = 7468;           // UsableTwoStateBaneConsoleSwitchV01
        public const uint SwitchReady = 55, SwitchSet = 56;
        public const uint UseArgument = 1;              // usabledata lookup[7468]: (1, 0, 0, 0, 0.0)
        public const uint ValveName = 152, SupportName = 153;

        /// <summary>The scenario: one scene result per switch (step n: switch n, valves 1-2 then life support 3-6).</summary>
        public const uint Sabotaged = 1;

        public const uint Experience = 110000;
        public const uint Credits = 9000;
        // ESTIMATE: Pulse Rifle, Incendiary Staff and Repair Tool templates chosen by name and level band (TaRapedia gives no levels).
        public const uint PulseRifle = 11957;           // Weapon_Avatar_Rifle_EMP_UNC_41_to_45
        public const uint IncendiaryStaff = 49496;      // Weapon_Avatar_Staff_Fire_UNC_40_to_44
        public const uint RepairTool = 18274;           // Tool_Avatar_Field_Repair_Direct_UNC_40_to_44
        public const uint TissueExtractor = 12053;      // Weapon_Avatar_LeechGun_Physical_UNC_44_to_48

        /// <summary>The Gas Flow Controls: Alpha (east), Beta (west).</summary>
        public static readonly ScenePosition[] ValvesAt =
        {
            new(-143f, 137.59f, -167f),
            new(-415f, 137.59f, -237f)
        };

        /// <summary>The Life Support Systems: the south hall's upper and lower decks, the east hall's upper and lower decks.</summary>
        public static readonly ScenePosition[] SupportsAt =
        {
            new(-172f, 137.11f, -416f),
            new(-177f, 131.21f, -416f),
            new(-81f, 137.11f, -363f),
            new(-82f, 130.23f, -369f)
        };

        /// <summary>The markers of 1 and 2: the middle between the two valves' rooms, the barracks' junction.</summary>
        public static readonly (double X, double Y, double Z) ValvesMarkerAt = (-279, 128, -264);
        public static readonly (double X, double Y, double Z) BarracksMarkerAt = (-156, 128, -344);

        public static uint ValveObjective(int number) => Valve1 + (uint)number - 1;
        public static uint SupportObjective(int number) => Support1 + (uint)number - 1;
        public static string ValveRole(int number) => $"gotha-gas-valve-{number}";
        public static string SupportRole(int number) => $"gotha-life-support-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Subversion", 12889, Drof, Drof, Level);
            mission.Reward(Experience, Credits, new[] { PulseRifle, IncendiaryStaff, RepairTool, TissueExtractor });
            mission.Prerequisite(AshenMeetSergeantDrofV1.MissionId);
            mission.Scenario(Sabotaged, "A valve or life support system sabotaged", ValveCount + SupportCount);

            mission.Objective(Valves, 12896, 12897, 0, MissionObjectiveState.Incomplete, counterTextId: 12898);
            mission.Objective(LifeSupport, 12899, 12900, 1, MissionObjectiveState.Incomplete, counterTextId: 12901);
            var step = 1U;
            for (var number = 1; number <= ValveCount; number++, step++)
            {
                var objective = ValveObjective(number);
                mission.Objective(objective, 13733 + 2 * (uint)number, 13734 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, step, null, counter: Sabotaged);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = ValvesAt[number - 1];
                mission.NamedIndicator(objective, ValveIndicator, at.X, at.Y, at.Z, ObjectMarkerRadius, Caverns);
            }
            for (var number = 1; number <= SupportCount; number++, step++)
            {
                var objective = SupportObjective(number);
                mission.Objective(objective, 13737 + 2 * (uint)number, 13738 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, step, null, counter: Sabotaged);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = SupportsAt[number - 1];
                mission.NamedIndicator(objective, SupportIndicator, at.X, at.Y, at.Z, ObjectMarkerRadius, Caverns);
            }
            mission.NamedIndicator(Valves, ValveIndicator, ValvesMarkerAt.X, ValvesMarkerAt.Y, ValvesMarkerAt.Z, RoomMarkerRadius, Caverns);
            mission.NamedIndicator(LifeSupport, SupportIndicator, BarracksMarkerAt.X, BarracksMarkerAt.Y, BarracksMarkerAt.Z, RoomMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1451",
                "Native1 'Sabotage Gotha Processing' 12896/12897 c12898, 2 'Shut Down Thrax Life Support' 12899/12900 c12901, 3-4 'Gas " +
                "Flow Control (1 of 2)/(2 of 2)' 13735-13738, 5-6 'Thrax Life Support System (1 of 4)/(2 of 4)' 13739-13742, 7-8 blank " +
                "13743-13746; no objectiveconversation. Texts 12889-12893. Usable names 152, 153. Indicators 216, 217.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Subversion",
                "Sergeant Jonas Drof gives; completed by the Radio HQ button; requires Meet Sergeant Drof; 9000 credits; Vextronics " +
                "Pulse Rifle, AccuMax Incendiary Staff, Phoenix Direct Repair Tool, Titan Tissue Extractor; six Thrax at each use.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenSubversionV1",
                "Level44; bound to the squad's copy; turned in at Drof (client 12890 'Speak to Sergeant Drof for your reward', 12892, 12893; not TaRapedia's radio); 110000 XP estimated. Two personal Gas Flow Controls 7468 " +
                "(name152) in the Alpha and Beta processing rooms, four personal Life Support Systems 7468 (name153) by the four " +
                "sleep pod groups; a use sets the switch (55->56) and completes its objective; 1 counts 3-4, 2 counts 5-8. No " +
                "Thrax spawned at a use (a solo player's fight). Items 11957, 49496, 18274, 12053.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own two valves and four life support systems in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Caverns,
                MapContextId = Caverns,
                ObjectiveAggregations = new()
                {
                    [Valves] = new MissionObjectiveAggregation(Enumerable.Range(1, (int)ValveCount).Select(ValveObjective).ToArray(), ValveCount, 0),
                    [LifeSupport] = new MissionObjectiveAggregation(Enumerable.Range(1, (int)SupportCount).Select(SupportObjective).ToArray(), SupportCount, 0)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var step = 1U;
            for (var number = 1; number <= ValveCount; number++, step++)
                Switch(scene, ValveRole(number), ValvesAt[number - 1], ValveName, ValveObjective(number), step);
            for (var number = 1; number <= SupportCount; number++, step++)
                Switch(scene, SupportRole(number), SupportsAt[number - 1], SupportName, SupportObjective(number), step);
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>A switch of the player's own: used, it is set, used no more, and sends its step's event.</summary>
        private static void Switch(MissionSceneDefinition scene, string role, ScenePosition at, uint name, uint objective, uint step)
        {
            var sequence = 10 + step;
            scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SwitchClass, at,
                InitialObjectState: SwitchReady, NameOverrideId: name,
                UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
            scene.Sequences[sequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{role}-set", role, SwitchSet),
                    new SetInteractionIntent($"{role}-done", role, false)
                },
                Signals = { new SceneMissionSignal(MissionId, Sabotaged, step) }
            };
        }
    }
}
