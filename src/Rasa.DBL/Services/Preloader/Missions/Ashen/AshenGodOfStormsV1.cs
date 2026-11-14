using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// God of Storms (native mission 1281), Ligo Ashen Desert: with Veyon's codes the player reprograms the ten Indra Lightning
    /// Resistors of Indra Pass for Surveyor Odesha, so their feedback hits the Bane; Maiek Berish, held by the Bane, is given up.
    /// One of the two follow-ups of Missing Scientists: The Bonds of Blood (1282, Endala's) is the other.
    ///
    /// Native: texts 10319-10322, 10323 (missionconversation): log "Find the 10 Indra Lightning Resistors and reprogram them so they
    /// create a feedback loop to disrupt the Bane Conscription Facility. If you finish programming all 10 Resistors, then you will
    /// lose the opportunity to find Maiek. Report back to Surveyor Odesha ...", offer "That is wonderful that you found resistor
    /// codes. Now you must go into Indra Pass and reprogram them yourself. ...", turn-in "Now I will use the console here to create
    /// a feedback loop in the lightning resistors. ..."; objectives (missionobjective) 1-10 "Sabotage Resistor 1".."10"
    /// 10334/10335 .. 10352/10353, no counters, 11 "Flag Reader" 11175/11176, counter 11177 "Resistors Programmed". No objective
    /// conversation. Usable name 68 LIGO_ASHEN_INDRA_RESISTOR "Indra Lightning Resistor"; class 10196
    /// UsableTwoStateBrannLightningResistorV01 "Lightning Resistor" (usabledata None: the client sends 1; USE_TS_STATE_0 55,
    /// USE_TS_STATE_1 56). No map static of a resistor on Ashen Desert.
    ///
    /// Sourced: TaRapedia God of Storms: giver and reward giver Odesha; requires Missing Scientists; level 42 at most; 190,000 XP,
    /// 8,400 credits; one of Vextronics Rocket Launcher, Vitalius Electric Staff, AccuMax Incendiary Injector Gun, AccuMax
    /// Shotgun; "Once you accept God of Storms you cannot see or accept her quest" (The Bonds of Blood).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage D4):
    ///  - Level 42 (TaRapedia's cap). Not offered and not taken while The Bonds of Blood is held or once it is done (offer and
    ///    accept only, as WildernessAliaBranchesV1's exclusive pair; the scene's requirement); the reverse is 1282's.
    ///  - Ten personal resistors (class 10196, off 55, on 56, named 68) round the Indra Pass waypoint (1, 2, 3, 7) and in the
    ///    pass's valley west and north of it (the others; the client's floor, the way west round the rocks north of the cave mouth): spots of no source, each on a client floor,
    ///    one floor, under open sky, 34 m or more outside every hostile pool's circle and 25 m or more apart, walked to on the
    ///    client's floor from the Indra Pass waypoint and back, 30 m or more outside every hostile pool's circle the whole way
    ///    (probe of 2026-11-10). A resistor's use completes its objective (a scene result) and turns it on. Any order.
    ///  - The hidden 11 ("Flag Reader", the client's counter "Resistors Programmed") counts the ten (scene results, counter 1);
    ///    its completion is the tenth resistor: it closes an open The Bonds of Blood (a scene signal to 1282, whose open
    ///    objectives fail: the client log's "you will lose the opportunity to find Maiek"). Finding Maiek (1282) closes an open
    ///    God of Storms the same way. With the offers exclusive, the two are held together only by a player who held both before
    ///    (an older character, a GM); the closing keeps the log's rule then too. FailRelatedMission is not used (it throws
    ///    when the other mission is not held; Crucible set 6).
    ///  - Markers r8 on the resistors.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 ("Rocket Launcher" of no kind read as
    ///    physical, "Injector Gun" the injection gun, "Shotgun" physical): Rocket Launcher Physical 49140 (39-43), Staff
    ///    Electric 15189 (40-44), Injection Gun Fire 12143 (42-46), Shotgun Physical 11963 (42-46); one to choose.
    /// </summary>
    public static class AshenGodOfStormsV1
    {
        public const uint MissionId = 1281;
        public const uint Level = 42;
        public const uint Odesha = AshenIndraPassNpcsV1.Odesha;
        public const uint Programmed = 11;              // the hidden "Flag Reader"
        public const uint Sabotaged = 1;                // the scenario of the resistors' uses: step K for resistor K
        public const uint Counted = 2;                  // the scenario the hidden 11 counts: step K for resistor K
        public const double MarkerRadius = 8;

        /// <summary>The fork (as CrucibleStrayCatsV1): the other path's signal arrives as scenario ClosedByOther; this path's end starts CloseOther.</summary>
        public const uint ClosedByOther = 19, CloseOther = 20;   // above the resistors' sequences 1-10 (CloseOther is a scene sequence too)

        public const uint ResistorClass = 10196;        // UsableTwoStateBrannLightningResistorV01
        public const uint ResistorName = 68;            // usablenameoverride LIGO_ASHEN_INDRA_RESISTOR "Indra Lightning Resistor"
        public const uint UseArgument = 1;              // usabledata lookup[10196][0] is None: the client sends 1
        public const uint OffState = 55, OnState = 56;  // USE_TS_STATE_0, USE_TS_STATE_1

        /// <summary>Indra Lightning Resistors 1-10 (the client floor's heights), in the objectives' order.</summary>
        public static readonly ScenePosition[] ResistorsAt =
        {
            new(200f, 305.72f, 352f), new(158f, 308.37f, 362f), new(237f, 309.99f, 348f), new(174f, 313.66f, 462f),
            new(130f, 293.15f, 500f), new(176f, 304.8f, 496f), new(246f, 315.61f, 309f), new(96f, 292.41f, 462f),
            new(124f, 286.31f, 532f), new(96f, 292.05f, 554f)
        };

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint RocketLauncher = 49140;       // Weapon_Avatar_RocketLauncher_Physical_UNC_39_to_43
        public const uint ElectricStaff = 15189;        // Weapon_Avatar_Staff_Electric_UNC_40_to_44
        public const uint IncendiaryInjectorGun = 12143; // Weapon_Avatar_InjectionGun_Fire_UNC_42_to_46
        public const uint Shotgun = 11963;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_42_to_46

        public static string ResistorRole(int number) => $"indra-lightning-resistor-{number}";

        /// <summary>Objective <paramref name="number"/>'s client texts: "Sabotage Resistor N", 10334/10335 for 1 on.</summary>
        public static (uint Name, uint Body) ResistorText(int number) => (10332 + 2 * (uint)number, 10333 + 2 * (uint)number);

        /// <summary>Not offered and not taken while The Bonds of Blood is held or once it is done (admission only).</summary>
        public static MissionRequirement NotWhileTheOther(uint other) =>
            new NotRequirement(new AnyRequirement(new MissionRequirement[]
            {
                new MissionStateRequirement(other, Accepted: true),
                new MissionStateRequirement(other)
            }));

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "God of Storms", 10319, Odesha, Odesha, Level);
            mission.Reward(Experience, Credits, new[] { RocketLauncher, ElectricStaff, IncendiaryInjectorGun, Shotgun });
            mission.Prerequisite(AshenMissingScientistsV1.MissionId);
            mission.Scenario(Sabotaged, "An Indra Lightning Resistor reprogrammed", (uint)ResistorsAt.Length);
            mission.Scenario(Counted, "Resistors Programmed", (uint)ResistorsAt.Length);

            for (var number = 1; number <= ResistorsAt.Length; number++)
            {
                var objective = (uint)number;
                var (name, body) = ResistorText(number);
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, objective, null, counter: Sabotaged);
                var at = ResistorsAt[number - 1];
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Objective(Programmed, 11175, 11176, Programmed - 1, MissionObjectiveState.Incomplete, counterTextId: 11177);    // hidden
            mission.Transition(Programmed);
            for (var number = 1U; number <= ResistorsAt.Length; number++)
                mission.Progress(Programmed, MissionProgressEventKind.ScenarioEvent, number, (uint)ResistorsAt.Length, counter: Counted, trigger: number);
            mission.Action(Programmed, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = Scene();
            scene.Requirement = NotWhileTheOther(AshenBondsOfBloodV1.MissionId);
            // The tenth resistor closes an open The Bonds of Blood; finding Maiek closes this one.
            CloseTheOther(mission, scene, Programmed, 2, AshenBondsOfBloodV1.MissionId);
            var objectives = new (uint, uint)[ResistorsAt.Length + 1];
            for (var number = 1; number <= ResistorsAt.Length + 1; number++)
                objectives[number - 1] = ((uint)number, 2);
            ClosedByTheOther(mission, "The Bonds of Blood", objectives);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1281",
                "Native1-10 'Sabotage Resistor 1'..'10' 10334/10335..10352/10353, no counters; 11 'Flag Reader' 11175/11176 counter " +
                "11177 'Resistors Programmed'; no objectiveconversation. Texts 10319-10323 (ten resistors in Indra Pass; all ten lose " +
                "Maiek). Usable name 68 'Indra Lightning Resistor', class10196 UsableTwoStateBrannLightningResistorV01.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/God_of_Storms",
                "Giver/reward giver Odesha; requires Missing Scientists; level <=42; 190000XP, 8400 credits; Vextronics Rocket " +
                "Launcher, Vitalius Electric Staff, AccuMax Incendiary Injector Gun, AccuMax Shotgun; once accepted, Endala's quest " +
                "cannot be seen or accepted.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenGodOfStormsV1",
                "Level42 (TaR cap). Not offered while 1282 is held or done. Ten personal resistors 10196 (name68; arg1; 55->56) in " +
                "the pass's valley, any order; each completes its objective; hidden 11 counts ten, then closes an open 1282 (its " +
                "objectives fail); finding Maiek closes this one. Markers r8. Items 49140, 15189, 12143, 11963 (lowest Uncommon, " +
                "band of level 42), one to choose.", 0.3);

            mission.Enable(scene);
        }

        /// <summary>The player's own ten resistors.</summary>
        public static MissionSceneDefinition Scene() => Scene(ResistorsAt);

        /// <summary>The player's own ten resistors at <paramref name="resistorsAt"/> (Ashen fix G moves four; set 13's scene is Scene()).</summary>
        public static MissionSceneDefinition Scene(ScenePosition[] resistorsAt)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Programmed }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= resistorsAt.Length; number++)
            {
                var role = ResistorRole(number);
                var objective = (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ResistorClass, resistorsAt[number - 1],
                    Orientation: number * 1.3, InitialObjectState: OffState, NameOverrideId: ResistorName,
                    UseAction: new SceneObjectAction(MissionId, objective, objective, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-reprogrammed", role, OnState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Sabotaged, objective), new SceneMissionSignal(MissionId, Counted, objective) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The other path's signal fails these objectives (each by its own transition) while they are Incomplete (as CrucibleStrayCatsV1).</summary>
        internal static void ClosedByTheOther(AshenMissionDataV1 mission, string other, params (uint Objective, uint Transition)[] objectives)
        {
            mission.Scenario(ClosedByOther, $"{other} done: this path closes", 1);
            foreach (var (objective, transition) in objectives)
            {
                mission.Transition(objective, transition, transition, MissionObjectiveState.Failed);
                mission.Progress(objective, transition, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ClosedByOther);
            }
        }

        /// <summary>This path's end closes the other: scenario CloseOther, its sequence's signal to <paramref name="other"/>.</summary>
        internal static void CloseTheOther(AshenMissionDataV1 mission, MissionSceneDefinition scene, uint objective, uint action, uint other, uint transition = 1)
        {
            mission.Scenario(CloseOther, $"Done: mission {other} closes", 0);
            mission.Action(objective, action, MissionActionKind.StartScenario, scenario: CloseOther, transition: transition);
            scene.Sequences[CloseOther] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(other, ClosedByOther, 1) } };
        }
    }
}
