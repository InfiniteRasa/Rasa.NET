using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Light the Fires (native mission 1864), Valverde Marshes: Retread Dudley at High Point Retreat asks the player to light
    /// the Retreads' signal fires across the marsh.
    ///
    /// Native: texts 19104-19108 (missionconversation): log "Visit the Forean fire pits throughout marshes and ensure they are
    /// lit properly.", offer "we retreads have made it a point to try to keep signal fires burning", turn-in "These signal
    /// fires are more important than most people realize"; objectives 1 "Light the fire NE of High Point Retreat."
    /// 19368/19369, 2-4 "Light the first/second/third fire west of Paludos." 19371/19372, 19374/19375, 19377/19378, 5 "Light
    /// the fire at High Point Retreat." 19380/19381, counters 19370, 19373, 19376, 19379, 19382 "Fire N Visited"
    /// (missionobjective); no objective conversation. Class 6137 UsableTwoStateForeanFirePitV01 (augmentation 9, usabledata
    /// lookup[0] None: the client sends 1); usableaugmentationstate[9] = [55, 56] (g_usabledata.dis): cold 55, lit 56 (the
    /// lit animation keys on leaving 55).
    ///
    /// Sourced: TaRapedia Light the Fires: giver and receiver Retread Dudley (Stalker Woods); level 36; 32,500 XP, 3,600
    /// credits; one of 2 Class VII EMP Bomb, 2 Power Charger, 4 Concussion Grenade, 4 Fragmentation Grenade; "Take them
    /// counterclockwise".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Five personal Forean fire pits (6137, the client's states 55 to 56), any order, each lit
    ///    once by a use; each completes its objective. Fire 5 at High Point Retreat's kindling (client map, -786.5, -522.9);
    ///    fires 2-4 west of Paludos at the triage's places; fire 1: the triage's (-740, -490) is a navmesh island (probe of
    ///    2026-11-02), so it stands 10 m south of it on the walked-to slope. All walked to from the High Point Retreat waypoint
    ///    (Paludos's own navmesh is cut off from the marsh).
    ///  - The rewards the client classes' lowest templates (961's and 925's), one stack to choose. A marker at each fire.
    /// </summary>
    public static class MarshesLightTheFiresV1
    {
        public const uint MissionId = 1864;
        public const uint Level = 36;
        public const uint Dudley = MarshesStalkerWoodsNpcsV1.Dudley;

        public const uint FireClass = 6137;             // UsableTwoStateForeanFirePitV01
        public const uint ColdState = 55, LitState = 56;
        public const uint UseArgument = 1;              // usabledata lookup[6137][0] is None: the client sends 1
        public const double MarkerRadius = 8;

        /// <summary>The fires: objective, name, body and counter texts, where.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, ScenePosition At)[] Fires =
        {
            (1, 19368, 19369, 19370, new(-740f, 230.92f, -500f)),
            (2, 19371, 19372, 19373, new(-420f, 214.45f, -40f)),
            (3, 19374, 19375, 19376, new(-430f, 214.7f, 20f)),
            (4, 19377, 19378, 19379, new(-410f, 215.57f, 70f)),
            (5, 19380, 19381, 19382, new(-786.5f, 243.38f, -522.9f)),
        };

        public const uint Experience = 32500;
        public const uint Credits = 3600;
        public const uint EmpBomb = 111232, EmpBombs = 2;                       // Class VII EMP Bomb
        public const uint PowerCharger = 118907, PowerChargers = 2;             // Class VII Power Charger
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4;   // Class VII Concussion Grenade
        public const uint FragGrenade = 45446, FragGrenades = 4;                // Class VII Fragmentation Grenade

        public static string FireRole(uint objective) => $"signal-fire-{objective}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Light the Fires", 19104, Dudley, Dudley, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (PowerCharger, PowerChargers), (ConcussionGrenade, ConcussionGrenades), (FragGrenade, FragGrenades) },
                new (uint, uint)[0]);

            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = MarshesMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var (objective, name, body, counter, at) in Fires)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Scenario(objective, $"Signal fire {objective} lit", 1);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: objective);
                if (objective == 1)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);

                var role = FireRole(objective);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, FireClass, at,
                    InitialObjectState: ColdState, UseAction: new SceneObjectAction(MissionId, objective, objective, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"light-{role}", role, LitState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, objective, 1) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1864",
                "Native1-5 'Light the fire NE of High Point Retreat / first, second, third fire west of Paludos / at High Point " +
                "Retreat' 19368-19381, counters 19370-19382 'Fire N Visited'; no objectiveconversation. Texts 19104-19108. Class " +
                "6137 UsableTwoStateForeanFirePitV01 (usabledata None: arg1). Map kindling at High Point Retreat (-786.5,-522.9).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Light_the_Fires",
                "Giver/receiver Retread Dudley (Stalker Woods); level36; 32500XP, 3600 credits; one of 2 Class VII EMP bomb, 2 " +
                "Power Charger, 4 Concussion Grenade, 4 Fragmentation Grenade; take them counterclockwise.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesLightTheFiresV1",
                "Five personal fire pits 6137 (arg1, 55->56), any order, one use each completes its objective: (-740,-500), " +
                "(-420,-40), (-430,20), (-410,70), (-786.5,-522.9). Items 111232x2, 118907x2, 111018x4, 45446x4. A marker " +
                "at each fire.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
