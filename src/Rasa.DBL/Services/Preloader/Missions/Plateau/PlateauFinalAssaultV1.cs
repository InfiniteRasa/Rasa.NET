using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Final Assault (native mission 1167), Temporal Chamber (squad instance 2029): in the Bane-held
    /// future of Alia Das, the dying Colonel Hudson gives the player a detonator: destroy the three Security
    /// Control Stations topside, then place the detonator on the Bane power core underground. The Eloh
    /// hologram Veryin takes the mission back in the chamber.
    ///
    /// Native: texts 8577-8581, turn-in 8580 Veryin's (missionconversation); objectives (missionobjective)
    /// 1 "Destroy Security Control Station Alpha" 8604/8605 counter 8606, 2 "Place Detonator on Power Core"
    /// 8607/8608, 3 Bravo 11034/11035 counter 11036, 4 Charlie 11037/11038 counter 11039; no objective
    /// conversation. Client classes: Mis_Temporal_Sacrifice_Bomb (24091, template 49708);
    /// UsableInertDestBaneSecurityMachineryTallV01/V02 (24733/24735, destroyable, augmentation 41, a
    /// destroyed mesh at state 2), next to the chamber's own classes (24732, 24822). The client's map places
    /// no station and no core; the underground facility is ArchBaneIndustrialRoomControl24mV15 (684, 292,
    /// -368) behind the service entrance (620, 300, -440).
    ///
    /// Sourced: TaRapedia The Final Assault: giver Colonel Hudson, receiver the Eloh Hologram, Temporal
    /// Chamber; requirement Future Sacrifice; 140,000 XP, 9,250 credits, one of an Eclipse Rocket Launcher,
    /// a Vextronics Rifle, a Vextronics Incendiary Net Gun, a Vextronics Pulse Pistol; "Destroy the 3
    /// Security Control Stations that maintain the force field to the underground facility. Once the force
    /// field is down, place the detonator on the Bane Power Core." TR MMO DB: level 30, 9,250 credits.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - Hudson gives the detonator on accept.
    ///  - The stations: three personal destroyable scene objects of 24733/24735/24733, 100 hit points, as
    ///    Toxic Detox's pumps, in the outpost yard: by the barracks, by the tower, by the atmosphere
    ///    generator (at the navmesh's ground). Each completes its objective (1, 3, 4); a hidden tally
    ///    (objective <see cref="Tally"/>) of the three opens objective 2.
    ///  - No force field (the server has no mission field in a squad copy: NOTES-missing-mechanics.md).
    ///    In its stead the core takes the detonator only once objective 2 is open; the Thrax pools guard
    ///    the entrance and the facility.
    ///  - The core: a personal UsableTwoStateBaneIndustrialReactorRoomSunV01 (4231) in the control room,
    ///    used once with the detonator (state 55 to 56, use argument 1: the client's usabledata gives none).
    ///  - The weapons as the Uncommon base templates of the bands that hold level 30, no manufacturer
    ///    ("Pulse" read as EMP, as The Beer Hunter's Pulse Disperser). No indicators: instance.
    /// </summary>
    public static class PlateauFinalAssaultV1
    {
        public const uint MissionId = 1167;
        public const uint Level = 30;
        public const uint Hudson = PlateauTemporalChamberNpcsV1.Hudson;
        public const uint Veryin = PlateauTemporalChamberNpcsV1.Veryin;
        public const uint Alpha = 1, Detonate = 2, Bravo = 3, Charlie = 4, Tally = 5;
        public const uint Detonator = 49708, DetonatorClass = 24091;
        public const string DetonatorKey = "sacrifice-detonator";
        public const uint StationHitPoints = 100, IntactState = 110, DestroyedState = 2;
        public const uint CoreClass = 4231;             // UsableTwoStateBaneIndustrialReactorRoomSunV01
        public const uint UseArgument = 1, CoreReadyState = 55, CoreSetState = 56;
        public const uint CoreSequence = 4;
        public const string CoreRole = "power-core";

        public const uint Experience = 140000;
        public const uint Credits = 9250;
        public const uint RocketLauncher = 49132;       // Weapon_Avatar_RocketLauncher_Physical_UNC_29_to_33
        public const uint Rifle = 17528;                // Weapon_Avatar_Rifle_Physical_UNC_26_to_30
        public const uint IncendiaryNetGun = 48356;     // Weapon_Avatar_NetGun_Fire_UNC_26_to_30
        public const uint PulsePistol = 17055;          // Weapon_Avatar_Pistol_EMP_UNC_30_to_34

        /// <summary>The stations: objective, sequence, class and place.</summary>
        public static readonly (uint Objective, uint Sequence, uint Class, ScenePosition At, string Where)[] Stations =
        {
            (Alpha, 1, 24733, new(590, 299.54f, -447), "by the barracks"),
            (Bravo, 2, 24735, new(642, 301.58f, -472), "by the tower"),
            (Charlie, 3, 24733, new(634, 293.56f, -622), "by the atmosphere generator"),
        };

        public static readonly ScenePosition CoreAt = new(684, 293.04f, -362);

        public static string StationRole(uint objective) => $"station-{objective}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Final Assault", 8577, Hudson, Veryin, Level);
            mission.Reward(Experience, Credits, new[] { RocketLauncher, Rifle, IncendiaryNetGun, PulsePistol });
            mission.Prerequisite(PlateauFutureSacrificeV1.MissionId);

            mission.Objective(Alpha, 8604, 8605, 0, MissionObjectiveState.Incomplete, counterTextId: 8606);
            mission.Objective(Bravo, 11034, 11035, 1, MissionObjectiveState.Incomplete, counterTextId: 11036);
            mission.Objective(Charlie, 11037, 11038, 2, MissionObjectiveState.Incomplete, counterTextId: 11039);
            mission.Objective(Detonate, 8607, 8608, 3, MissionObjectiveState.Inactive);
            mission.Objective(Tally, 8604, 8605, 4, MissionObjectiveState.Incomplete);   // counts only the stations
            mission.Scenario(1, "Three stations destroyed, the detonator on the core", CoreSequence);

            mission.Transition(Tally);
            foreach (var station in Stations)
            {
                mission.Transition(station.Objective);
                mission.Progress(station.Objective, MissionProgressEventKind.ScenarioEvent, station.Sequence, null, counter: 1);
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, station.Sequence, (uint)Stations.Length,
                    counter: 1, trigger: station.Sequence);
            }
            mission.Action(Tally, 1, MissionActionKind.RevealObjective, targetObjective: Detonate);
            mission.Action(Tally, 2, MissionActionKind.ActivateObjective, targetObjective: Detonate, state: MissionObjectiveState.Incomplete);

            mission.Transition(Detonate);
            mission.Progress(Detonate, MissionProgressEventKind.ScenarioEvent, CoreSequence, null, counter: 1);
            mission.Action(Detonate, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(DetonatorKey, Detonator, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-detonator", MissionId, DetonatorKey, Detonator, 1)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var station in Stations)
            {
                var role = StationRole(station.Objective);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, station.Class, station.At,
                    Orientation: station.Sequence * 2.0943951024, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, station.Objective, station.Sequence, StationHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[station.Sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, 1, station.Sequence) }
                };
            }
            scene.Actors[CoreRole] = new SceneActorDefinition(CoreRole, SceneActorKind.Object, CoreClass, CoreAt,
                InitialObjectState: CoreReadyState, UseAction: new SceneObjectAction(MissionId, Detonate, CoreSequence, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{CoreRole}", CoreRole));
            scene.Sequences[CoreSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("arm-power-core", CoreRole, CoreSetState),
                    new SetInteractionIntent("close-power-core", CoreRole, false)
                },
                Character = { new ConsumeMissionItemIntent("detonator-on-core", MissionId, DetonatorKey, 1, MissionItemScope.AssignmentIssued) },
                Signals = { new SceneMissionSignal(MissionId, 1, CoreSequence) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1167",
                "Native1 Alpha 8604/8605 counter8606, 2 'Place Detonator on Power Core' 8607/8608, 3 Bravo 11034/11035 counter11036, " +
                "4 Charlie 11037/11038 counter11039; no objective conversation. Texts 8577-8581. Detonator class24091/template49708; " +
                "stations 24733/24735 (destroyable, augmentation41). No station or core on the map; facility (684,292,-368).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Final_Assault",
                "Giver Colonel Hudson, receiver the Eloh Hologram (Temporal Chamber); requirement Future Sacrifice; 140000XP, 9250 " +
                "credits, one of Eclipse Rocket Launcher/Vextronics Rifle/Vextronics Incendiary Net Gun/Vextronics Pulse Pistol; three " +
                "stations keep a force field on the underground facility. TR MMO DB level30, 9250 credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauFinalAssaultV1",
                "Level30. Detonator on accept. Three personal 100HP stations in the yard; a hidden tally opens objective 2. No force " +
                "field (missing mechanic): the core (class4231, control room) takes the detonator once 2 is open. Weapons 49132, " +
                "17528, 48356, 17055. Places are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
