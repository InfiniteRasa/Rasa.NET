using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Now Can You Hear Me? (native mission 1869), Valverde Marshes: the creatures keep knocking down the AFS communication
    /// spikes between High Point Retreat and Paludos; Retread Dudley asks the player to place three new ones.
    ///
    /// Native: texts 19129-19133 (missionconversation): log "Place 3 communication signal boosters in key locations between
    /// High Point Retreat and Paludos", offer "the Thrax, Mox and Maw in the area keep knocking down the spikes", turn-in
    /// "Come in Paludos, do you read?"; objectives 1 "Place spike north of High Point Retreat." 19383/19384 counter 19385
    /// "Northern Communication Spike Placed", 2 "... north east ..." 19386/19387 counter 19388, 3 "... east ..." 19389/19390
    /// counter 19391 (missionobjective); no objective conversation. Item MisMarshesCommunicationsSpike class 28636 (World DB
    /// template 119000, stack 5). Usable name 273 MIS_MARSHES_COMMUNICATIONSPIKE "AFS Communication Spike"
    /// (usablenameoverride). Client item templates 121201-121204: Area Healing Disc 35-39, Ice Staff 35-39, Sonic Pistol
    /// 35-39, Ice Blade 36-40.
    ///
    /// Sourced: TaRapedia Now Can You Hear Me?: giver and receiver Retread Dudley (Stalker Woods); no requirement; 70,000 XP,
    /// 5,550 credits; one of ChiTech Radial Healing Disc 35, Audiodyne Cryogenic Staff 35, Teleract Sonic Pistol 35, ChiTech
    /// Cryogenic Blade 36 (the client block in that order).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 36 (the camp's; TaRapedia none).
    ///  - Three spikes at the accept (taken at an abandon); three personal item spots (class 7847 UsableItemAccHumGpsBeacon,
    ///    Mapping the Pools' acceptor, argument 3, states 119 to 121) named by the client's 273, each takes one spike and
    ///    completes its objective, any order. North of the camp is a navmesh island (probe of 2026-11-02): the spots stand on
    ///    the walked-to ground nearest the triage's places, north-north-east (-760, -508), north-east (-730, -490) and east
    ///    (-720, -550) of the High Point Retreat waypoint.
    ///  - The rewards the client's block 121201-121204, one to choose. A marker at each spot.
    /// </summary>
    public static class MarshesNowCanYouHearMeV1
    {
        public const uint MissionId = 1869;
        public const uint Level = 36;
        public const uint Dudley = MarshesStalkerWoodsNpcsV1.Dudley;

        public const uint SpikeTemplate = 119000, SpikeClass = 28636, Spikes = 3;
        public const string SpikeKey = "afs-communication-spike";
        public const uint SpotClass = Pools.PoolsMappingThePoolsV1.SpotClass;    // UsableItemAccHumGpsBeacon
        public const uint ReadyState = Pools.PoolsMappingThePoolsV1.SpotReadyState, PlacedState = Pools.PoolsMappingThePoolsV1.SpotPlantedState;
        public const uint UseArgument = Pools.PoolsMappingThePoolsV1.UseArgument;
        public const uint SpotName = 273;               // usablenameoverride MIS_MARSHES_COMMUNICATIONSPIKE
        public const double MarkerRadius = 8;

        /// <summary>The spots: objective, name, body and counter texts, where.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, ScenePosition At)[] Spots =
        {
            (1, 19383, 19384, 19385, new(-760f, 237.32f, -508f)),
            (2, 19386, 19387, 19388, new(-730f, 227.33f, -490f)),
            (3, 19389, 19390, 19391, new(-720f, 217.85f, -550f)),
        };

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint HealingDisc = 121201;         // Tool_Avatar_Healing_Disc_Area_UNC_35_to_39
        public const uint CryogenicStaff = 121202;      // Weapon_Avatar_Staff_Ice_UNC_35_to_39
        public const uint SonicPistol = 121203;         // Weapon_Avatar_Pistol_Sonic_UNC_35_to_39
        public const uint CryogenicBlade = 121204;      // Weapon_Avatar_Blade_Ice_UNC_36_to_40

        public static string SpotRole(uint objective) => $"spike-spot-{objective}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Now Can You Hear Me?", 19129, Dudley, Dudley, Level);
            mission.Reward(Experience, Credits, new[] { HealingDisc, CryogenicStaff, SonicPistol, CryogenicBlade });

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(SpikeKey, SpikeTemplate, MissionItemScope.AssignmentIssued, Spikes,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-spikes", MissionId, SpikeKey, SpikeTemplate, Spikes) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var (objective, name, body, counter, at) in Spots)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Scenario(objective, $"Communication spike {objective} placed", 1);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: objective);
                if (objective == 1)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);

                var role = SpotRole(objective);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, at,
                    InitialObjectState: ReadyState, NameOverrideId: SpotName,
                    UseAction: new SceneObjectAction(MissionId, objective, objective, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"place-{role}", role, PlacedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-spike", MissionId, SpikeKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, objective, 1) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1869",
                "Native1-3 'Place spike north / north east / east of High Point Retreat.' 19383-19390, counters 19385, 19388, " +
                "19391; no objectiveconversation. Texts 19129-19133. Item class28636 MisMarshesCommunicationsSpike (template " +
                "119000). Usable name273 'AFS Communication Spike'. Templates 121201-121204.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Now_Can_You_Hear_Me%3F",
                "Giver/receiver Retread Dudley (Stalker Woods); 70000XP, 5550 credits; one of ChiTech Radial Healing Disc35, " +
                "Audiodyne Cryogenic Staff35, Teleract Sonic Pistol35, ChiTech Cryogenic Blade36; the Thrax, Mox and Maw knock " +
                "the spikes down.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesNowCanYouHearMeV1",
                "Level36. Three spikes (119000) at accept; three personal spots 7847 (arg3, 119->121, name273) at (-760,-508), " +
                "(-730,-490), (-720,-550), walked to (north of the camp is a navmesh island); each takes a spike. Items the " +
                "client block 121201-121204, one to choose. A marker at each spot.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
