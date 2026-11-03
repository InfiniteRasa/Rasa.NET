using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Timora
{
    using Mine = Rasa.Services.Preloader.TimoraMinesContent;
    using Torcastra = Rasa.Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;

    /// <summary>
    /// Timora Mines' missions (Timora_mines), on the place TimoraMinesContent puts down and the
    /// Game's TimoraMines runs:
    ///  - 340 Timora Mines: Report to Field Ranger Kearney (Cmd. Sgt. Simpson at Foreas Base, to
    ///    Kearney at the mine's entrance): the way in.
    ///  - 1905 Timora Mines: Central Bound Patrol (Kearney, to Field Ranger Sanchez in the Supply
    ///    Storage): talked to again, Kearney sends four of his soldiers with the player; one of them
    ///    brought to Sanchez is the escort, all of them dead before that fails it.
    ///  - 403 Disable Production in the Mine (Field Ranger Morrow): the five drills, each blown with
    ///    one of her detonators at its charge, and the nine fuel pumps, shot.
    ///  - 404 Fuel Lake Sabotage (Simpson): the Main Fuel Pump in the Fuel Egress, set off by a
    ///    sustained burn of its three fuel lakes; the security system's fields over them are
    ///    switched off at the Command Center's three consoles.
    ///  - 792 Climbing the Corporate Ladder (Sergeant Pastre): the Timora Warden, two Timora
    ///    Overseers and the Timora Wardmaster.
    ///  - 1276 Mining for Information (Base Cmdr. Jayjack at Foreas Base): the Eloh information in
    ///    the Eloh Shelter, then three waves of Bane against the clock.
    ///  - 1860 Stolen History (TorcastraMissionsV1's, under its revision) is switched on: the Map
    ///    Cipher is in the crate across the Predator Bay, and the Minos Caverns crate with the
    ///    Galactic Map is made usable.
    ///
    /// The client gives the missions, their objectives, texts, counters and the conversation
    /// packages (Kearney's 158, the Eloh information's 1472); the walkthroughs the givers, the order
    /// of things and Fuel Lake Sabotage's reward. The other rewards are ours, at that one's rate. What
    /// the Game raises comes in as scenario events (<see cref="Events"/>).
    /// </summary>
    public static class TimoraMinesMissionsV1
    {
        public const uint ReportToKearney = 340, CentralBound = 1905, DisableProduction = 403, FuelLake = 404,
            CorporateLadder = 792, MiningForInformation = 1276;

        public static readonly uint[] All = { ReportToKearney, CentralBound, DisableProduction, FuelLake, CorporateLadder, MiningForInformation };

        /// <summary>What Timora_mines switches on: its six and Torcastra's 1860 Stolen History.</summary>
        public static readonly uint[] Enabled = All.Append(Torcastra.StolenHistory).ToArray();

        public const string Revision = "timora_1_16_5";

        /// <summary>The client's mission category: Instance (Timora Mines).</summary>
        public const uint Category = 10000037;

        /// <summary>The Eloh information's conversation package (objectiveconversation 1276/1).</summary>
        public const uint ElohPackage = 1472;

        /// <summary>UsableNPCElohHologramLarge, over the Eloh Shelter's projector.</summary>
        public const uint HologramClass = 24732;

        /// <summary>How long the Eloh Shelter's ambush gives to beat its three waves.</summary>
        public const uint AmbushSeconds = 300;

        /// <summary>Detonators Morrow hands over (the Game's TimoraMines, once an assignment): one a drill, and a spare.</summary>
        public const uint Detonators = 6;

        /// <summary>The client's marker names: Bane Mining Drills, Fuel Lake, Predator Launch Bay, Eloh Chamber.</summary>
        public const uint MiningDrillsMarker = 134, FuelLakeMarker = 26, PredatorBayMarker = 2, ElohChamberMarker = 384;

        public static uint Unnamed(uint objective) => Rasa.Structures.MissionIndicator.UnnamedFrom + objective;

        /// <summary>The scenario events the Game raises, by mission: (scenario, event).</summary>
        public static class Events
        {
            // 1905: scenario 1.
            public const uint Escort = 1;
            public const uint Arrived = 1, Lost = 2;

            // 403: scenario 1, drill n blown is event n; scenario 2, pump n destroyed is event n.
            public const uint Drills = 1, Pumps = 2;

            // 404: scenario 1.
            public const uint MainPump = 1;
            public const uint PumpDestroyed = 1;

            // 1276: scenario 1.
            public const uint Ambush = 1;
            public const uint Defended = 1;
        }

        public static IEnumerable<string> Up()
        {
            var sql = new List<string>();

            ReportToKearneyRows(sql);
            CentralBoundRows(sql);
            DisableProductionRows(sql);
            FuelLakeRows(sql);
            CorporateLadderRows(sql);
            MiningForInformationRows(sql);
            sql.AddRange(StolenHistoryUp());

            return sql;
        }

        public static IEnumerable<string> Down() => StolenHistoryDown().Concat(InstanceMissionSql.Remove(Revision, All));

        private static InstanceMissionSql.RewardItem[] Choice(params uint[] templates) =>
            templates.Select(template => new InstanceMissionSql.RewardItem(template, 1)).ToArray();

        private static void OnEvent(InstanceMissionSql mission, uint objective, uint scenario, uint eventId, uint transition = 1) =>
            mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: scenario, transition: transition);

        private static void OnEvents(InstanceMissionSql mission, uint objective, uint scenario, uint count)
        {
            for (var eventId = 1U; eventId <= count; eventId++)
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: scenario, target: count, trigger: eventId);
        }

        private static uint[] Upto(uint count) => Enumerable.Range(1, (int)count).Select(n => (uint)n).ToArray();

        /// <summary>A mission's binding, for the bootcamp's graduates; one whose events the Game raises has the data sequence.</summary>
        private static MissionSceneDefinition Scene(MissionSceneDefinition scene, bool events = true)
        {
            scene.Requirement ??= new CustomRequirement("character.starting-experience-completed");
            scene.Script ??= events ? "data.sequence" : null;
            return scene;
        }

        private static void ReportToKearneyRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ReportToKearney, "Timora Mines: Report to Field Ranger Kearney", 206,
                Mine.Simpson, Mine.Kearney, 17, Category);
            mission.Reward(4000, 400);

            mission.Objective(2, 4154, 7916, 0, MissionObjectiveState.Incomplete);
            mission.Transition(2);
            mission.Conversation(2, Mine.KearneyPackage);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(2, Unnamed(2), Mine.KearneyAt.X, Mine.KearneyAt.Y, Mine.KearneyAt.Z, 6);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/340",
                "Simpson's \"We've got a team out there in Timora Mines just radioed in for assistance\": report to Kearney2996 (package158) in the mines.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "tentonhammer:timora-mines",
                "Cmd. Sgt. Simpson in the Foreas Base command centre (-34.6, 121.1, 426.8) sends players to the mines; Kearney at the entrance (-136.9, 205.6, 518.2). 4000 XP / 400 credits are ours.");
            mission.Enable(Scene(new MissionSceneDefinition(), events: false));
        }

        private static void CentralBoundRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CentralBound, "Timora Mines: Central Bound Patrol", 19738,
                Mine.Kearney, Mine.Sanchez, 18, Category);
            mission.Reward(16000, 1600, selectableItems: Choice(12835, 13364, 3614));

            mission.Objective(2, 19743, 19744, 0, MissionObjectiveState.Incomplete);
            mission.Objective(1, 19746, 19747, 1);

            mission.Scenario(Events.Escort, "Kearney's escort brought to Sanchez, or lost", Upto(2));

            // Talked to again, Kearney sends the soldiers (TimoraMines).
            mission.Transition(2);
            mission.Conversation(2, Mine.KearneyPackage);
            mission.RevealAndActivate(2, 1);

            mission.Transition(1);
            OnEvent(mission, 1, Events.Escort, Events.Arrived);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(1, 2, MissionObjectiveState.Failed, 2);
            OnEvent(mission, 1, Events.Escort, Events.Lost, 2);

            mission.Indicator(2, Unnamed(2), Mine.KearneyAt.X, Mine.KearneyAt.Y, Mine.KearneyAt.Z, 6);
            mission.Indicator(1, Unnamed(1), Mine.SanchezAt.X, Mine.SanchezAt.Y, Mine.SanchezAt.Z, 10);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1905",
                "Kearney2996 (package158, \"Talk to me again when you are ready to go\"): report to him2, escort the soldiers to the lower mines1; turned in to Field Ranger Sanchez3046 (\"Sanchez will meet you down there\").");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Timora_Mines",
                "\"As soon as you click on that contact, you will be issued a small squad of NPCs. If they all die before mission completion, you fail.\" Sanchez in the Supply Storage (56.6, 145.6, 50.0, Ten Ton Hammer). " +
                "Ours: four AFS infantry who follow the player; one within 12 m of Sanchez is the escort. 16000 XP / 1600 credits and the level-19 Hazmat boots, Reflective gloves, physical shotgun are ours.");
            mission.Enable(Scene(new MissionSceneDefinition()));
        }

        private static void DisableProductionRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, DisableProduction, "Disable Production in the Mine", 594,
                Mine.Morrow, Mine.Morrow, 19, Category);
            mission.Reward(32000, 3200, selectableItems: Choice(13216, 12734, 3441));

            mission.Objective(1, 602, 7409, 0, MissionObjectiveState.Incomplete, counterTextId: 3858);
            mission.Objective(2, 607, 7410, 1, MissionObjectiveState.Incomplete, counterTextId: 20000006);

            mission.Scenario(Events.Drills, "Timora's mining drills blown", Upto((uint)Mine.Drills.Length));
            mission.Scenario(Events.Pumps, "Timora's fuel pumps destroyed", Upto((uint)Mine.PumpHouses.Length));

            mission.Transition(1);
            OnEvents(mission, 1, Events.Drills, (uint)Mine.Drills.Length);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Transition(2);
            OnEvents(mission, 2, Events.Pumps, (uint)Mine.PumpHouses.Length);

            mission.Indicator(1, MiningDrillsMarker, -146.2, 158.5, 378.7, 120);
            mission.Indicator(2, Unnamed(2), -91.2, 146.7, 126.7, 60);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/403",
                "Morrow3047's \"I've got some detonators for taking out the drills, but you're going to need good old fashioned firepower to take out those pumps\": Bomb the Mining Drills1 (counter3858), Destroy the Fuel Pumps2 (counter20000006); MisDivideItemTimoraBombs11263 (template2550).");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Timora_Mines; tentonhammer:timora-mines",
                "Five drills - four in the Expeditionary Excavation, the fifth on the Fuel Bore Cavern's climb - each blown at its red circle; nine pumps - five in the Fuel Bore Grotto, four below the cavern's bridge - shot. " +
                $"{Detonators} detonators handed over by the Game's TimoraMines once an assignment, as ordinary items: a charge takes one, and a mission's own item it could not. 32000 XP / 3200 credits, as Fuel Lake Sabotage's, and the level-19 Mech boots, Bio gloves, physical rocket launcher are ours.");

            mission.Enable(Scene(new MissionSceneDefinition()));
        }

        private static void FuelLakeRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, FuelLake, "Fuel Lake Sabotage", 598, Mine.Simpson, Mine.Simpson, 20, Category);
            mission.Reward(32000, 3200, selectableItems: Choice(3121, 13507, 13025));

            mission.Objective(1, 4142, 7267, 0, MissionObjectiveState.Incomplete, counterTextId: 4261);
            mission.Scenario(Events.MainPump, "The Main Fuel Pump destroyed", Upto(1));
            mission.Transition(1);
            OnEvent(mission, 1, Events.MainPump, Events.PumpDestroyed);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, FuelLakeMarker, Mine.PumpAt.X, Mine.PumpAt.Y, Mine.PumpAt.Z, 25);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/404",
                "Simpson127: \"You'll need a sustained burn of the nearby fuel lakes to create enough heat to ignite the pump. If the Bane security system kicks in, you'll need to disable it.\" Destroy the Master Fuel Lake Pump1 (counter4261).");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Fuel_Lake_Sabotage; tentonhammer:timora-mines",
                "The Main Pump (312.2, 204.9, -267.5) with three pools round it, lit by gunfire; a force field over a lake is security, switched off at the three consoles - one per lake - in the Warden's lair. " +
                "32000 XP, 3200 credits and Electric Polarity Gun, Stealth Armor Helmet, Graviton Armor Legs as the wiki has them, here the level-19 classes' first templates. " +
                "Ours: the fuel depot classes (pump12998, fuel12991, fire12993, field12997), the burn's length and heat, security engaging at the first lake lit.");
            mission.Enable(Scene(new MissionSceneDefinition()));
        }

        private static void CorporateLadderRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, CorporateLadder, "Climbing the Corporate Ladder", 3859,
                Mine.Pastre, Mine.Pastre, 20, Category);
            mission.Reward(32000, 3200, selectableItems: Choice(13045, 13529, 3297));

            mission.Objective(1, 3863, 9266, 0, MissionObjectiveState.Incomplete, counterTextId: 3864);
            mission.Objective(2, 3865, 9265, 1, MissionObjectiveState.Incomplete, counterTextId: 3868);
            mission.Objective(3, 3866, 9264, 2, MissionObjectiveState.Incomplete, counterTextId: 3867);

            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, Mine.TimoraWarden, counter: 0, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.CreatureKilled, Mine.TimoraOverseer, counter: 0, target: 2);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.CreatureKilled, Mine.TimoraWardmaster, counter: 0, target: 1);

            mission.Indicator(1, Unnamed(1), Mine.WardenAt.X, Mine.WardenAt.Y, Mine.WardenAt.Z, 15);
            mission.Indicator(2, Unnamed(2), Mine.OverseerBunkerAt.X, Mine.OverseerBunkerAt.Y, Mine.OverseerBunkerAt.Z, 15);
            mission.Indicator(3, Unnamed(3), Mine.WardmasterAt.X, Mine.WardmasterAt.Y, Mine.WardmasterAt.Z, 15);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/792",
                "Pastre6878: \"four primary Bane - the Wardmaster, the Overseers, and the Warden\": the Warden1 (Timora Warden20000012), the Overseers2 (Timora Overseer20000011), the Wardmaster3 (Timora Wardmaster20000013).");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Timora_Mines; tentonhammer:timora-mines",
                "One Overseer in the bunker up the hill (-241.9, 217.9, 289.3), the other at the bottom of the cavern's left path; the Warden in the Command Center, \"extremely difficult\". " +
                "Ours: the Wardmaster on the Fuel Egress's mining control terminal; 32000 XP / 3200 credits and the level-19 Graviton vest, Stealth legs, laser rifle.");
            mission.Enable(Scene(new MissionSceneDefinition(), events: false));
        }

        private static void MiningForInformationRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, MiningForInformation, "Mining for Information", 10253,
                Mine.Jayjack, Mine.Jayjack, 19, Category);
            mission.Reward(24000, 2400, selectableItems: Choice(12756, 13237, 11394));

            mission.Objective(1, 10258, 10259, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 10261, 10262, 1);

            mission.Scenario(Events.Ambush, "The Eloh Shelter's three waves beaten", Upto(1));

            mission.Transition(1);
            mission.Conversation(1, ElohPackage);
            mission.RevealAndActivate(1, 2);

            mission.Transition(2);
            OnEvent(mission, 2, Events.Ambush, Events.Defended);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2, 2, MissionObjectiveState.Failed, 2);
            mission.Timer(2, AmbushSeconds, 2);

            mission.Indicator(1, ElohChamberMarker, Mine.HologramAt.X, Mine.HologramAt.Y, Mine.HologramAt.Z, 8);
            mission.Indicator(2, Unnamed(2), Mine.HologramAt.X, Mine.HologramAt.Y, Mine.HologramAt.Z, 25);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/1276",
                "Jayjack124: locate and interact with any Eloh information1 (package1472, \"The Eloh holographic data is uploaded to your personal data recorder\"), then the ambush2.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "wiki:Timora_Mines; tentonhammer:timora-mines",
                "\"Clicking this icon will spawn 3 waves of ambushes, and you must defeat all 3 waves... This part is on a timer\" - the cave right of the bridge (97.3, 202.2, -0.1) to the Eloh Shelter. " +
                $"Ours: the large Eloh hologram over the shelter's projector, the waves, {AmbushSeconds} s, 24000 XP / 2400 credits and the level-19 Bio helmet, Mech gloves, physical leech gun.");

            var scene = Scene(new MissionSceneDefinition
            {
                Dialogue = new() { new(1, ElohPackage, 1, transitionId: 1) }
            });
            scene.Actors["eloh-hologram"] = new SceneActorDefinition("eloh-hologram", SceneActorKind.Object, HologramClass,
                new ScenePosition(Mine.HologramAt.X, Mine.HologramAt.Y, Mine.HologramAt.Z), -System.Math.PI / 2,
                Conversation: new SceneObjectConversation(MiningForInformation, 1, ElohPackage, 1));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = new List<WorldIntent> { new EnsureActorIntent("ensure-eloh-hologram", "eloh-hologram") }
            };
            mission.Enable(scene);
        }

        #region Stolen History

        public const uint MinosMapChest = 568011, MinosMap = 1347, MinosChestClass = 11116;
        public static readonly (float X, float Y, float Z) MinosChestAt = (159.0f, 3.77f, 70.0f);

        /// <summary>Stolen History on, the Minos crate usable, and its two pieces' markers.</summary>
        public static IEnumerable<string> StolenHistoryUp()
        {
            yield return $"UPDATE world_usable SET usable = 1 WHERE id = {MinosMapChest} AND map_context_id = {MinosMap} AND class_id = {MinosChestClass} AND usable = 0;";
            yield return Marker(1, Unnamed(1), MinosChestAt.X, MinosChestAt.Y, MinosChestAt.Z);
            yield return Marker(2, Unnamed(2), Mine.CrateAt.X, Mine.CrateAt.Y, Mine.CrateAt.Z);
            yield return $"UPDATE mission_content_definition SET enabled = 1 WHERE mission_id = {Torcastra.StolenHistory} AND content_revision = {InstanceMissionSql.Literal(Torcastra.Revision)} AND enabled = 0;";
        }

        public static IEnumerable<string> StolenHistoryDown()
        {
            yield return $"UPDATE mission_content_definition SET enabled = 0 WHERE mission_id = {Torcastra.StolenHistory} AND content_revision = {InstanceMissionSql.Literal(Torcastra.Revision)} AND enabled = 1;";
            foreach (var objective in new uint[] { 1, 2 })
                yield return $"DELETE FROM mission_indicator WHERE mission_id = {Torcastra.StolenHistory} AND content_revision = {InstanceMissionSql.Literal(Torcastra.Revision)} " +
                    $"AND objective_id = {objective} AND indicator_id = {Unnamed(objective)};";
            yield return $"UPDATE world_usable SET usable = 0 WHERE id = {MinosMapChest} AND map_context_id = {MinosMap} AND class_id = {MinosChestClass} AND usable = 1;";
        }

        private static string Marker(uint objective, uint indicator, float x, float y, float z) =>
            "INSERT INTO mission_indicator (mission_id, content_revision, requirement, objective_id, indicator_id, pos_x, pos_y, pos_z, radius, show_3d_effect, comment) " +
            $"SELECT {Torcastra.StolenHistory}, {InstanceMissionSql.Literal(Torcastra.Revision)}, {(byte)MissionContentRequirement.Required}, {objective}, {indicator}, " +
            $"{InstanceMissionSql.Literal(x)}, {InstanceMissionSql.Literal(y)}, {InstanceMissionSql.Literal(z)}, 8, 0, 'Objective marker' " +
            $"FROM mission_content_definition d WHERE d.mission_id = {Torcastra.StolenHistory} AND d.content_revision = {InstanceMissionSql.Literal(Torcastra.Revision)} " +
            $"AND NOT EXISTS (SELECT 1 FROM mission_indicator x WHERE x.mission_id = {Torcastra.StolenHistory} AND x.content_revision = d.content_revision " +
            $"AND x.objective_id = {objective} AND x.indicator_id = {indicator});";

        #endregion
    }
}
