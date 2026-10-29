using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Smash and Grab (native mission 1324), Valverde Pools: Lieutenant Chester at the East Listening Post sends the player
    /// into the Thrax Barracks to steal ammunition, plasma capacitors and nutrient paste, and to destroy the three Bane data
    /// servers beside them.
    ///
    /// Native: texts 10785-10789 (missionconversation): offer "the barracks has large stores of Thrax Ammunition, Plasma
    /// Capacitors, and Nutrient Paste ... you'll need to also destroy any of their Data Servers"; objectives 1 "Collect
    /// Thrax Ammo" 10812/10813, 2 "Collect Plasma Capacitors" 10814/10815, 3 "Collect Nutrient Paste from the Bane
    /// Barracks" 10816/10817, 4 "Destroy Thrax Ammo Data Server" 10818/10819 counter 10820 "Data Server Destroyed", 5
    /// "Destroy Plasma Capacitor Data Server" 10821/10822 counter 10823, 6 "Destroy Nutrient Paste Data Server" 10824/10825
    /// counter 10826, 7 "Additional Ammo" 17606/17607 ("This is for One Man's Trash if active.") (missionobjective); no
    /// objective conversation. Items: Thrax Ammo Crate 705 (as One Man's Trash's), Plasma Capacitor 97512 (class 25587),
    /// Nutrient Paste 97513 (class 25588).
    ///
    /// Sourced: TaRapedia Smash and Grab: giver and receiver Lieutenant Chester; requires Eastern Listening Post; follow-up
    /// Spoils of War; 130,000 XP, 7,200 credits; one of Shinobi Laser Chaingun level 33, Vitalius Cryogenic Net Gun 36,
    /// Vitalius Electric Polarity Gun 33, Eclipse Laser Rifle 36; walkthrough: "The items needed are in the two main
    /// buildings." TR MMO DB: level 35. Warcry/PlanetTR known issues (2008): the containers did not respawn; "After
    /// collecting the three ammo crates required for Smash and Grab, the ammo crates objective doesn't complete".
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: personal items and three Data Servers at estimated places
    /// inside the two main buildings; build objective 7's link):
    ///  - One personal container each, usable once (the patch note's three crates read as one a store): the ammo in a
    ///    Bane item crate (class 11116, as One Man's Trash's ammo) in the Barracks hall, the capacitors and the paste in Bane
    ///    item crates (class 11116, as Weapons Trafficking's footlockers) in the command centre (its one floor the navmesh
    ///    has; a navmesh probe of 2026-10-25).
    ///  - Each data server a personal Bane machine (class 10191 UsableInertDestBaneSecurityMachineryV01: the client's only
    ///    data-storage destructible, 7177 UsableInertDestTorcastrabanedatastorage, has its "Default" mesh 776, to-fix
    ///    Pools #5), 100 hit points, beside its store; "Data Server Destroyed 0/1" each.
    ///  - Objective 7 is left out (no use known); its link is built: the ammo's use also signals One Man's Trash's ammo
    ///    crate (scene event 1), which completes that objective when 562 is active and is ignored otherwise.
    ///  - Any order; the items go at the turn-in. The rewards the lowest Uncommon templates of the lines and bands that
    ///    hold those names and levels ("Chaingun" the machine gun, as Elimination's), one to choose.
    /// </summary>
    public static class PoolsSmashAndGrabV1
    {
        public const uint MissionId = 1324;
        public const uint Level = 35;
        public const uint Chester = PoolsSupplyNpcsV1.Chester;
        public const uint Ammo = 1, Capacitors = 2, Paste = 3, AmmoServer = 4, CapacitorServer = 5, PasteServer = 6;
        public const uint StoreScenario = 1;
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const uint ServerClass = 10191;          // UsableInertDestBaneSecurityMachineryV01
        public const uint ServerHitPoints = 100, IntactState = 110, DestroyedState = 2;

        public const uint Experience = 130000;
        public const uint Credits = 7200;
        public const uint LaserChaingun = 48256;        // Weapon_Avatar_MachineGun_Laser_UNC_33_to_37
        public const uint IceNetGun = 48396;            // Weapon_Avatar_NetGun_Ice_UNC_36_to_40
        public const uint ElectricPolarityGun = 15542;  // Weapon_Avatar_PolarityGun_Electric_UNC_33_to_37
        public const uint LaserRifle = 14699;           // Weapon_Avatar_Rifle_Laser_UNC_36_to_40

        /// <summary>The stores: objective, item template, key, container class and place (scene events 1-3).</summary>
        public static readonly (uint Objective, uint Template, string Key, uint Class, ScenePosition At)[] Stores =
        {
            (Ammo, 705, "smash-thrax-ammo", 11116, new(482.4f, 679.72f, 488.7f)),
            (Capacitors, 97512, "plasma-capacitors", 11116, new(563.1f, 685.69f, 583.6f)),
            (Paste, 97513, "nutrient-paste", 11116, new(567.1f, 685.79f, 590.1f)),
        };

        /// <summary>The data servers: objective, counter text and place (scene events 4-6).</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, ScenePosition At)[] Servers =
        {
            (AmmoServer, 10818, 10819, 10820, new(485.85f, 679.35f, 492.55f)),
            (CapacitorServer, 10821, 10822, 10823, new(567.1f, 685.8f, 582.1f)),
            (PasteServer, 10824, 10825, 10826, new(569.1f, 685.61f, 592.1f)),
        };

        public static string StoreRole(int index) => $"store-{index + 1}";
        public static string ServerRole(int index) => $"server-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Smash and Grab", 10785, Chester, Chester, Level);
            mission.Reward(Experience, Credits, new[] { LaserChaingun, IceNetGun, ElectricPolarityGun, LaserRifle });
            mission.Prerequisite(PoolsEasternListeningPostV1.MissionId);

            mission.Objective(Ammo, 10812, 10813, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Capacitors, 10814, 10815, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Paste, 10816, 10817, 2, MissionObjectiveState.Incomplete);
            foreach (var (objective, name, body, counter, _) in Servers)
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
            mission.Scenario(StoreScenario, "A Barracks store emptied or data server destroyed", 6);

            var scene = new MissionSceneDefinition { Script = "data.sequence", Items = new() };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Stores.Length; index++)
            {
                var (objective, template, key, crateClass, at) = Stores[index];
                var sequence = (uint)index + 1;
                var role = StoreRole(index);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, sequence, null, counter: StoreScenario);
                if (index == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, sequence, at.X, at.Y, at.Z);

                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, crateClass, at,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                var taken = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"open-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-taken", MissionId, key, template, 1) },
                    Signals = { new SceneMissionSignal(MissionId, StoreScenario, sequence) }
                };
                // Objective 7's link: the ammo is One Man's Trash's too, while that mission is active.
                if (objective == Ammo)
                    taken.Signals.Add(new SceneMissionSignal(PoolsOneMansTrashV1.MissionId, PoolsOneMansTrashV1.CrateScenario, 1));
                scene.Sequences[sequence] = taken;
            }
            for (var index = 0; index < Servers.Length; index++)
            {
                var (objective, _, _, _, at) = Servers[index];
                var sequence = (uint)Stores.Length + (uint)index + 1;
                var role = ServerRole(index);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, sequence, null, counter: StoreScenario);
                mission.Indicator(objective, sequence, at.X, at.Y, at.Z);

                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ServerClass, at,
                    Orientation: index * 1.5707963268, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, objective, sequence, ServerHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, StoreScenario, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1324",
                "Native1 'Collect Thrax Ammo' 10812/10813, 2 'Collect Plasma Capacitors' 10814/10815, 3 'Collect Nutrient Paste " +
                "from the Bane Barracks' 10816/10817, 4-6 'Destroy ... Data Server' 10818-10826 counters 10820/10823/10826 'Data " +
                "Server Destroyed', 7 'Additional Ammo' 17606/17607; no objectiveconversation. Texts 10785-10789. Items 705, " +
                "97512/25587, 97513/25588.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Smash_and_Grab",
                "Giver/receiver Lieutenant Chester; requires Eastern Listening Post; follow-up Spoils of War; 130000XP, 7200 " +
                "credits; one of Shinobi Laser Chaingun33, Vitalius Cryogenic Net Gun36, Vitalius Electric Polarity Gun33, " +
                "Eclipse Laser Rifle36; the items in the two main buildings. TR MMO DB level35. Warcry/PlanetTR: containers did " +
                "not respawn.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsSmashAndGrabV1",
                "Personal containers (11116 x3) and data servers (10191, 100HP; 7832 and 7177 have the Default mesh) in the Barracks hall and the command " +
                "centre (places estimated, user); 7 left out, its link a signal to 562's ammo event. Items 48256, 48396, 15542, " +
                "14699, one to choose.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
