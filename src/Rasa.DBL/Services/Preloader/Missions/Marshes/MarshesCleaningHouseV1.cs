using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Cleaning House (native mission 894), the Village Ruins (squad instance 1743): Elder Kirii at P'reo Das gives the player
    /// explosives to blow up the Bane pumps that poison the soil round Central Operations, and sends him to free the Foreans
    /// held in the villages round P'reo Das and bring them out to Warrior Tola.
    ///
    /// Native: texts 4690-4693 and 7290 (missionconversation): title "Cleaning House", log "Destroy Bane pumps which are
    /// poisoning the area and rescue captive Foreans.", offer "our brothers have been made prisoners in their own villages ...
    /// I've heard word of pumps being the cause of this, pumping poisons into the soil. Help rid us of these poisons and save
    /// our brothers and sisters who are in need. I've given you something that should aid you in your journey.", turn-in
    /// (Tola) "Eloh be praised! We have lost many"; objectives (missionobjective) 1 "Destroy Pumping Apparatus" 6127/7291
    /// counter 6128 "Pumps Destroyed", 8-11 "Destroy Pump 1".."Pump 4" 17144-17151, 6 "Rescue Forean Captured By the Bane"
    /// 6186/7292 counter 6187 "Forean Camps Freed", 2 "Rescue Foreans Captured at Ra'a Das" 6129/7293, 3 "... at E'bae Das"
    /// 6131/7294, 13 "... at He'kal Das" 17152/17153, 14-16 "[Optional] Locate Additional Survivors." 17168/17169,
    /// 17171/17172, 17174/17175, 12 "Escort Foreans Out of P'reo Das" 17155/17156; objectiveconversation (894,2 and 14)
    /// package 1158 6130/17170, (894,3 and 15) 1159 6132/17173, (894,13 and 16) 1160 17154/17176 ("I will come with you").
    /// Items Forean Explosive Device class 22967 MisMarshesVillageRuinsBombs (template 45450, stack 10); objects Toxic Waste
    /// Pump 21007 (UsableInertDestBaneRawFuelPumpHouseV02), Detonation Device 7334 (UsableItemAccHumBombV01, as 913's).
    ///
    /// Sourced: TaRapedia Cleaning House: giver Kirii, receiver Forean Warrior Tola (P'reo Das); requires Never Surrender;
    /// 130,000 XP, 7,000 credits; one of Vextronics Sonic Torqueshell Rifle, ChiTech Laser Pistol, Vextronics Electric
    /// Chaingun, Vextronics Sonic RPG Launcher; 4 pumps. TaRapedia updates 2007-07-03: "Cannot complete mission because
    /// Detonator acceptors not targetable" (the pumps take bombs at acceptors). TaRapedia history: "Rescue Forean Captured By
    /// the Bane: 0/4" and "[Optional] Rescue more Foreans: 0/2" in an older list. TR MMO DB: level 35. The weapons: the
    /// Uncommon base templates of the bands that hold level 35 (no client block holds the four).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Taken at Kirii in the squad's Village Ruins; four Forean Explosive Devices at the accept ("I've given you something";
    ///    the instance's own bomb item), taken back at an abandon. Four of the player's own pumps on the walked-to ground in
    ///    front of Central Operations (navmesh probe of 2026-11-03), each with a Detonation Device beside it: one use plants a
    ///    device and blows the pump (its objective 8-11, and one of 1's four).
    ///  - The three camps (Ra'a Das, E'bae Das round its hut and platforms at (60, -120), He'kal Das): at each a Forean
    ///    Prisoner of the camp's package and two of the player's own Thrax guards from the accept, and one more prisoner of the
    ///    same package a little apart (the optional survivor). A prisoner's talk completes its objective and sets him following
    ///    the player; the three camps' talks count for 6. Both prisoners of a camp have the camp's client package, so either one
    ///    offers both talks; each talk sets its own prisoner following. 6 done opens 12: walking into the arrival (15 m round
    ///    Tola) completes it (the server checks the player, as PoolsMiasmaTestingV1's return). The prisoners follow until the
    ///    turn-in at Tola, which takes them away (the engine's reward retirement). The prisoners and survivors cannot be
    ///    hurt (neutral, a player could kill one, and a dead prisoner would leave his camp's objective open until an abandon).
    ///  - Markers on each pump, round the pumps, at each camp and prisoner, and at Tola.
    /// </summary>
    public static class MarshesCleaningHouseV1
    {
        public const uint MissionId = 894;
        public const uint Level = 35;
        public const uint Kirii = MarshesPreoDasVillageNpcsV1.Kirii, Tola = MarshesPreoDasVillageNpcsV1.Tola;
        public const uint Ruins = MarshesPreoDasVillageNpcsV1.MapContextId;
        public const uint Pumps = 1, Camps = 6, Escort = 12;
        public const uint ExitArea = 1;

        /// <summary>Scenarios: the pumps blown (1's count), the camps freed (6's count).</summary>
        public const uint PumpsBlown = 1, CampsFreed = 2;
        public static uint PumpBlown(int pump) => 11 + (uint)pump;             // each pump's own objective
        public static uint FollowSequence(int camp, bool optional) => (optional ? 24U : 21U) + (uint)camp;
        public static uint PlantSequence(int pump) => 41 + (uint)pump;

        public const uint ExplosiveTemplate = 45450, Explosives = 4;          // Forean Explosive Device, class 22967
        public const string ExplosiveKey = "forean-explosive";
        public const uint PumpClass = 21007;                                    // Toxic Waste Pump
        public const uint IntactState = 110, DestroyedState = 2;
        public const uint AcceptorClass = MarshesBuriedGasLinesV1.ReceptacleClass;     // Detonation Device
        public const uint ReadyState = MarshesBuriedGasLinesV1.ReadyState, PlantedState = MarshesBuriedGasLinesV1.PlantedState;
        public const uint UseArgument = MarshesBuriedGasLinesV1.UseArgument;
        public const uint Soldier = 531045;
        public const double MarkerRadius = 8, CampRadius = 15, ExitRadius = 15;

        /// <summary>The pumps: objective, name, body; the pump and its acceptor.</summary>
        public static readonly (uint Objective, uint Name, uint Body, ScenePosition At, ScenePosition Acceptor)[] PumpRows =
        {
            (8, 17144, 17145, new(10f, 62.53f, 330f), new(14f, 61.38f, 330f)),
            (9, 17146, 17147, new(37.2f, 62.97f, 330.75f), new(37f, 62.04f, 325.97f)),
            (10, 17148, 17149, new(-19.49f, 60.77f, 300.31f), new(-15f, 62.11f, 300f)),
            (11, 17150, 17151, new(69.81f, 59.57f, 299.78f), new(66f, 60.37f, 300.35f)),
        };
        public static readonly (double X, double Y, double Z) PumpsAt = (25, 61.4, 315);
        public const double PumpsRadius = 35;

        /// <summary>
        /// The camps: the rescue and optional objectives (name, body), the prisoner row and package, the camp's centre, the
        /// prisoner's and the survivor's places, the two guards.
        /// </summary>
        public static readonly (uint Rescue, uint RescueName, uint RescueBody, uint Survivor, uint SurvivorName, uint SurvivorBody,
            uint Prisoner, uint Package, (double X, double Y, double Z) Centre, ScenePosition PrisonerAt, ScenePosition SurvivorAt,
            ScenePosition[] GuardsAt)[] CampRows =
        {
            (2, 6129, 7293, 14, 17168, 17169, MarshesPreoDasVillageNpcsV1.RaaPrisoner, MarshesPreoDasVillageNpcsV1.RaaPackage,
                (250, 57, 8), new(245f, 56.27f, 0f), new(256.8f, 58.17f, 19.15f),
                new ScenePosition[] { new(226f, 55.12f, 4f), new(250f, 56.37f, -20f) }),
            (3, 6131, 7294, 15, 17171, 17172, MarshesPreoDasVillageNpcsV1.EbaePrisoner, MarshesPreoDasVillageNpcsV1.EbaePackage,
                (75, 55.5, -122), new(60f, 55.75f, -120f), new(90f, 55.37f, -125f),
                new ScenePosition[] { new(66f, 56.37f, -130f), new(52f, 61.57f, -128f) }),
            (13, 17152, 17153, 16, 17174, 17175, MarshesPreoDasVillageNpcsV1.HekalPrisoner, MarshesPreoDasVillageNpcsV1.HekalPackage,
                (145, 65.8, 14), new(140f, 66.09f, 12f), new(151.2f, 65.57f, 15.55f),
                new ScenePosition[] { new(128f, 65.98f, 10f), new(145f, 65.6f, -5f) }),
        };

        public const uint Experience = 130000;
        public const uint Credits = 7000;
        public const uint SonicTorqueshellRifle = 49652;   // Weapon_Avatar_Torqueshell_Rifle_Sonic_UNC_33_to_37
        public const uint LaserPistol = 14487;             // Weapon_Avatar_Pistol_Laser_UNC_35_to_39
        public const uint ElectricChaingun = 15105;        // Weapon_Avatar_MachineGun_Electric_UNC_33_to_37
        public const uint SonicRocketLauncher = 49168;     // Weapon_Avatar_RocketLauncher_Sonic_UNC_34_to_38

        public static string PumpRole(int pump) => $"pump-{pump + 1}";
        public static string AcceptorRole(int pump) => $"pump-{pump + 1}-detonation-device";
        public static string PrisonerRole(int camp, bool optional) => optional ? $"camp-{camp + 1}-survivor" : $"camp-{camp + 1}-prisoner";
        public static string GuardRole(int camp, int guard) => $"camp-{camp + 1}-guard-{guard + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Cleaning House", 4690, Kirii, Tola, Level);
            mission.Reward(Experience, Credits, new[] { SonicTorqueshellRifle, LaserPistol, ElectricChaingun, SonicRocketLauncher });
            mission.Prerequisite(MarshesNeverSurrenderV1.MissionId);
            mission.Scenario(PumpsBlown, "A Bane pump blown", (uint)PumpRows.Length);
            mission.Scenario(CampsFreed, "A camp's prisoner freed", (uint)CampRows.Length);
            mission.Area(ExitArea, Ruins, MarshesPreoDasVillageNpcsV1.TolaAt.X, MarshesPreoDasVillageNpcsV1.TolaAt.Y,
                MarshesPreoDasVillageNpcsV1.TolaAt.Z, ExitRadius);

            var ordinal = 0U;
            mission.Objective(Pumps, 6127, 7291, ordinal++, MissionObjectiveState.Incomplete, counterTextId: 6128);
            foreach (var pump in PumpRows)
                mission.Objective(pump.Objective, pump.Name, pump.Body, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(Camps, 6186, 7292, ordinal++, MissionObjectiveState.Incomplete, counterTextId: 6187);
            foreach (var camp in CampRows)
                mission.Objective(camp.Rescue, camp.RescueName, camp.RescueBody, ordinal++, MissionObjectiveState.Incomplete);
            foreach (var camp in CampRows)
                mission.Objective(camp.Survivor, camp.SurvivorName, camp.SurvivorBody, ordinal++, MissionObjectiveState.Incomplete,
                    required: false);
            mission.Objective(Escort, 17155, 17156, ordinal, MissionObjectiveState.Inactive);

            // 1 and 8-11: each pump blown completes its own objective and counts for 1.
            mission.Transition(Pumps);
            for (var pump = 1U; pump <= PumpRows.Length; pump++)
                mission.Progress(Pumps, MissionProgressEventKind.ScenarioEvent, pump, (uint)PumpRows.Length, counter: PumpsBlown, trigger: pump);
            var marker = 1U;
            mission.Indicator(Pumps, marker++, PumpsAt.X, PumpsAt.Y, PumpsAt.Z, PumpsRadius, Ruins);
            for (var index = 0; index < PumpRows.Length; index++)
            {
                var pump = PumpRows[index];
                mission.Scenario(PumpBlown(index), $"Pump {index + 1} blown", 1);
                mission.Transition(pump.Objective);
                mission.Progress(pump.Objective, MissionProgressEventKind.ScenarioEvent, 1, null, counter: PumpBlown(index));
                mission.Indicator(pump.Objective, marker++, pump.At.X, pump.At.Y, pump.At.Z, MarkerRadius, Ruins);
            }

            // 6 and 2, 3, 13: each camp's prisoner, by his talk; 14-16 the survivors, by theirs. 6 opens 12.
            mission.Transition(Camps);
            for (var camp = 1U; camp <= CampRows.Length; camp++)
                mission.Progress(Camps, MissionProgressEventKind.ScenarioEvent, camp, (uint)CampRows.Length, counter: CampsFreed, trigger: camp);
            MarshesTasteOfTheirOwnMedicineV1.Open(mission, Camps, 1, Escort);
            for (var index = 0; index < CampRows.Length; index++)
            {
                var camp = CampRows[index];
                mission.Indicator(Camps, marker++, camp.Centre.X, camp.Centre.Y, camp.Centre.Z, CampRadius, Ruins);
                foreach (var (objective, optional, at) in new[] { (camp.Rescue, false, camp.PrisonerAt), (camp.Survivor, true, camp.SurvivorAt) })
                {
                    mission.Scenario(FollowSequence(index, optional), optional ? $"Camp {index + 1}'s survivor follows" : $"Camp {index + 1}'s prisoner follows", 0);
                    mission.Transition(objective);
                    mission.Conversation(objective, camp.Package);
                    mission.Action(objective, 1, MissionActionKind.StartScenario, scenario: FollowSequence(index, optional));
                    mission.Indicator(objective, marker++, at.X, at.Y, at.Z, MarkerRadius, Ruins);
                }
            }

            // 12: out of P'reo Das, at Tola.
            mission.Transition(Escort);
            mission.EnterArea(Escort, ExitArea);
            mission.Action(Escort, 1, MissionActionKind.GrantReward, reward: 1);
            var tola = MarshesPreoDasVillageNpcsV1.TolaAt;
            mission.Indicator(Escort, marker, tola.X, tola.Y, tola.Z, ExitRadius, Ruins);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/894",
                "Native1 'Destroy Pumping Apparatus' 6127/7291 counter6128, 8-11 'Destroy Pump 1..4' 17144-17151, 6 'Rescue Forean " +
                "Captured By the Bane' 6186/7292 counter6187 'Forean Camps Freed', 2 Ra'a Das 6129/7293, 3 E'bae Das 6131/7294, 13 " +
                "He'kal Das 17152/17153, 14-16 '[Optional] Locate Additional Survivors.' 17168-17175, 12 'Escort Foreans Out of P'reo " +
                "Das' 17155/17156. objectiveconversation 894,2/14 pkg1158, 3/15 pkg1159, 13/16 pkg1160. Texts 4690-4693, 7290 " +
                "('I've given you something'). Names 8557 Kirii, 8560 Tola, 5289 Forean Prisoner. Item class22967 (45450). Objects " +
                "21007 Toxic Waste Pump, 7334 Detonation Device.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Cleaning_House",
                "Giver Kirii, receiver Forean Warrior Tola (P'reo Das); requires Never Surrender; 130000XP, 7000 credits; one of " +
                "Vextronics Sonic Torqueshell Rifle, ChiTech Laser Pistol, Vextronics Electric Chaingun, Vextronics Sonic RPG " +
                "Launcher; 4 pumps. Updates 2007-07-03: detonator acceptors not targetable. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesCleaningHouseV1",
                "Bound to 1743; Kirii 760377 -> Tola 760376. 4 explosives (45450) at accept; 4 personal pumps (21007) before Central " +
                "Operations, each with an acceptor (7334): a use blows its pump. Camps Ra'a/E'bae/He'kal Das: a prisoner " +
                "(760379-760381, the client packages) and 2 Thrax guards, a survivor apart; talks set them following; 6 opens 12, " +
                "an area r15 at Tola. Weapons 49652, 14487, 15105, 49168. Places are estimates.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own pumps, acceptors, prisoners and guards, on the squad's copy of the Village Ruins.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Ruins,
                MapContextId = Ruins,
                Items = new()
                {
                    new MissionItemBinding(ExplosiveKey, ExplosiveTemplate, MissionItemScope.AssignmentIssued, Explosives,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("kiriis-explosives", MissionId, ExplosiveKey, ExplosiveTemplate, Explosives) }
            };
            var start = new SceneSequenceDefinition();
            for (var index = 0; index < PumpRows.Length; index++)
            {
                var pump = PumpRole(index);
                var acceptor = AcceptorRole(index);
                var row = PumpRows[index];
                scene.Actors[pump] = new SceneActorDefinition(pump, SceneActorKind.Object, PumpClass, row.At,
                    Orientation: index * 1.5708, InitiallyInteractable: false, InitialObjectState: IntactState);
                scene.Actors[acceptor] = new SceneActorDefinition(acceptor, SceneActorKind.Object, AcceptorClass, row.Acceptor,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, row.Objective, PlantSequence(index), UseArgument));
                start.World.Add(new EnsureActorIntent($"create-{pump}", pump));
                start.World.Add(new EnsureActorIntent($"create-{acceptor}", acceptor));
                scene.Sequences[PlantSequence(index)] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{acceptor}", acceptor, PlantedState),
                        new SetInteractionIntent($"close-{acceptor}", acceptor, false),
                        new TransitionObjectStateIntent($"blow-{pump}", pump, DestroyedState)
                    },
                    Character = { new ConsumeMissionItemIntent($"{acceptor}-explosive", MissionId, ExplosiveKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, PumpsBlown, (uint)index + 1), new SceneMissionSignal(MissionId, PumpBlown(index), 1) }
                };
            }
            for (var index = 0; index < CampRows.Length; index++)
            {
                var camp = CampRows[index];
                foreach (var (optional, at) in new[] { (false, camp.PrisonerAt), (true, camp.SurvivorAt) })
                {
                    var role = PrisonerRole(index, optional);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, camp.Prisoner, at,
                        Orientation: index * 2.0944, FollowOffset: new ScenePosition(optional ? 1.5f : -1.5f, 0f, -2.5f),
                        GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });     // neutral: a dead one would block 6
                    start.World.Add(new EnsureActorIntent($"create-{role}", role));
                    var follows = new SceneSequenceDefinition
                    {
                        World = { new SetInteractionIntent($"{role}-rescued", role, false), new FollowActorIntent($"{role}-follows", role, 0, Enabled: true) }
                    };
                    if (!optional)
                        follows.Signals.Add(new SceneMissionSignal(MissionId, CampsFreed, (uint)index + 1));
                    scene.Sequences[FollowSequence(index, optional)] = follows;
                }
                for (var guard = 0; guard < camp.GuardsAt.Length; guard++)
                {
                    var role = GuardRole(index, guard);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Soldier, camp.GuardsAt[guard],
                        Orientation: guard * 3.1416, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    start.World.Add(new EnsureActorIntent($"{role}-stands", role));
                }
            }
            scene.Sequences[0] = start;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
