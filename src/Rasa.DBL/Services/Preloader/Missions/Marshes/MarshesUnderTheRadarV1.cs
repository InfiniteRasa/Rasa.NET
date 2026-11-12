using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Under the Radar (native mission 1875), Valverde Marshes: Warrior Dikah at Far West Watch sends the player east to the
    /// AFS supply dropship that went down, for its black box, and on to Lt. Kiang, who heads the watch.
    ///
    /// Native: texts 19159-19163 (missionconversation): log "Gather the black box from the downed AFS dropship east of Far
    /// West Watch.", offer "An AFS dropship was set to deliver much needed supplies to us ... you should find the ship's black
    /// box and take it to Lt. Kiang who heads the Far West Watch.", turn-in "This is troubling news."; objective 1 "Gather the
    /// black box from the downed dropship east of Far West Watch." 19563/19564, no counter (missionobjective); no objective
    /// conversation. Item AFS Dropship Black Box class 28641 MisMarshesAFSDropshipBlackBox (World DB template 119002). Object
    /// 22105 UsableItemDispBlackBox (entityclass: item giver, augmentation 34, mesh 21101); usabledata lookup[22105][0] None,
    /// so the client sends 1 (Usable.__init__). Map: two downed AFS dropships (ArchHumDropshipPropV01/V02Destroyed) at
    /// (-54.4, 213.6, -359.2) and (-55.4, 215.9, -380.5). Indicator name 28 "Downed Dropship"
    /// (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia Under the Radar: giver Warrior Dikah, receiver Lt. Kiang; 65,000 XP, 5,400 credits; one of Eclipse
    /// Pulse Chaingun lvl 33, Audiodyne Shotgun lvl 37, Vextronics Virulent Injector Gun lvl 37, ChiTech Sonic Cannon lvl 34;
    /// the box at (-51, -365); follow-ups Part II and The Core of the Matter. Ten Ton Hammer Eastern Marshes: Dikah at Far West
    /// Watch -523, 222, -176; "black box crate at the downed dropship -51,216,-366"; 5,400 credits. Client item templates
    /// 121237-121240 (World DB itemtemplate): EMP Machine Gun 33-37, Physical Shotgun 37-41, Virulent Injection Gun 37-41,
    /// Sonic Rocket Launcher 34-38 - TaRapedia's four, kinds and levels, in its order.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - One personal black box giver (22105, states 81 -> 82 as the other augmentation-34 givers) at Ten Ton Hammer's point
    ///    by the wreck, on the navmesh (probe of 2026-11-02); its use gives the box and completes 1; no room, no box (the giver
    ///    waits). Kiang keeps the box at the turn-in.
    ///  - The rewards the client's template block 121237-121240, one to choose. Level 37 (the triage). A marker under the
    ///    client's "Downed Dropship" at the giver.
    /// </summary>
    public static class MarshesUnderTheRadarV1
    {
        public const uint MissionId = 1875;
        public const uint Level = 37;
        public const uint Dikah = MarshesSentryTowerNpcsV1.Dikah, Kiang = MarshesSentryTowerNpcsV1.Kiang;
        public const uint Gather = 1;

        public const uint BoxTemplate = 119002, BoxClass = 28641;
        public const string BoxKey = "afs-dropship-black-box";
        public const uint GiverClass = 22105;           // UsableItemDispBlackBox
        public const uint UseArgument = 1;              // usabledata lookup[22105][0] is None: the client sends 1
        public const uint ReadyState = 81, TakenState = 82;     // ESTIMATE: the 11096/11114 pattern, no client state entry for 22105
        public const string GiverRole = "black-box";
        public const uint DownedDropship = 28;          // missionobjectiveindicatorlanguage "Downed Dropship"
        public const double MarkerRadius = 15;
        public static readonly ScenePosition GiverAt = new(-51f, 217.07f, -366f);

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint PulseChaingun = 121237;       // Weapon_Avatar_MachineGun_EMP_UNC_33_to_37
        public const uint Shotgun = 121238;             // Weapon_Avatar_Shotgun_v1_Physical_UNC_37_to_41
        public const uint VirulentInjectorGun = 121239; // Weapon_Avatar_InjectionGun_Virulent_UNC_37_to_41
        public const uint SonicCannon = 121240;         // Weapon_Avatar_RocketLauncher_Sonic_UNC_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Under the Radar", 19159, Dikah, Kiang, Level);
            mission.Reward(Experience, Credits, new[] { PulseChaingun, Shotgun, VirulentInjectorGun, SonicCannon });

            mission.Objective(Gather, 19563, 19564, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(1, "Take the black box from the downed dropship", 1);
            mission.Transition(Gather);
            mission.Progress(Gather, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);
            mission.Action(Gather, 1, MissionActionKind.GrantReward, reward: 1);
            mission.NamedIndicator(Gather, DownedDropship, GiverAt.X, GiverAt.Y, GiverAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1875",
                "Native1 'Gather the black box from the downed dropship east of Far West Watch.' 19563/19564, no counter; no " +
                "objectiveconversation. Texts 19159-19163 (take it to Lt. Kiang). Item class28641 AFS Dropship Black Box " +
                "(template119002). Giver 22105 UsableItemDispBlackBox (usabledata None: arg1). Map wrecks (-54.4,213.6,-359.2), " +
                "(-55.4,215.9,-380.5). Indicator name28 'Downed Dropship'. Templates 121237-121240.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Under_the_Radar",
                "Giver Warrior Dikah, receiver Lt. Kiang; 65000XP, 5400 credits; one of Eclipse Pulse Chaingun33, Audiodyne " +
                "Shotgun37, Vextronics Virulent Injector Gun37, ChiTech Sonic Cannon34; box at -51,-365. Ten Ton Hammer: Dikah " +
                "-523,222,-176; black box crate at the downed dropship -51,216,-366.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesUnderTheRadarV1",
                "One personal giver 22105 (arg1, states81/82) at (-51,217.07,-366); its use issues the box (119002, taken at the " +
                "turn-in) and completes 1. Level37. Items the client block 121237-121240, one to choose. Marker 'Downed Dropship' " +
                "at the giver.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(BoxKey, BoxTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            };
            scene.Actors[GiverRole] = new SceneActorDefinition(GiverRole, SceneActorKind.Object, GiverClass, GiverAt,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Gather, 1, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{GiverRole}", GiverRole) } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("take-black-box", GiverRole, TakenState),
                    new SetInteractionIntent($"close-{GiverRole}", GiverRole, false)
                },
                Character = { new IssueMissionItemIntent("black-box-from-wreck", MissionId, BoxKey, BoxTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
