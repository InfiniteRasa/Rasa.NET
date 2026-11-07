using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Your Part of the Bargain (native mission 1529), the Staal Junkyard (squad instance 2138): Cirkin's price for his help
    /// is the Thrax Technicians gone from his junkyard, five Dolomite Blocks and an Energy Matrix for his warden bot "Sparks".
    ///
    /// Native: texts 13656-13660 (missionconversation): log "Cirkin from the Staal Junkyard wants you to eliminate all the
    /// Thrax technicans from the southeastern part of Staal Junkyard, collect large blocks of Dolomite, and collect a Energy
    /// Matrix for his warden bot guardian "Sparks."", turn-in "Sparks will be very happy to finally have stable energy
    /// matrix."; objectives (missionobjective) 1 "Eliminate all Thrax Technicians" 13721/13722 counter 13723 "All Thrax
    /// Technicians Killed", 2 "Collect Dolomite" 13724/13725, 3 "Collect Energy Matrix" 13726/13727, 4 "Return to Cirkin"
    /// 13728/13729, 5 "Mission Tracker" 13730/13731 (13731 empty) counters 13732 "Objectives", 13734 "Dolomite Finished", 13733
    /// "Energy Matrix Finished". No objective conversation. Item classes 26401 MisJunkyardItemDolomiteBlock (template 111144),
    /// 26415 MisJunkyardItemEnergyMatrix (111161). Usable name 166 "Dolomite Block" (usablenameoverride).
    ///
    /// Sourced: TaRapedia Your Part of the Bargain: giver and reward giver Cirkin; requires Careful Negotiation; follow-up
    /// Rising Up; 180,000 XP, 8,200 credits; Olympia Graviton Armor Helmet 5, Pulsar Hazmat Armor Helmet 4, ChiTech Reflective
    /// Armor Boots 3, Vitalius Bio Armor Helmet 5; counters "All Thrax Technicians Killed x/1", "Dolomite Block x/5", "Energy
    /// Matrix x/1"; Dolomite at 306, 262, -65 and five more (the "small, blue crystal-shaped items ... on the ground", four in
    /// the south-east Bane area, one "a little north of the instance entrance" 28, 243, -219, one on the west side -331, 263,
    /// -36); the Energy Matrix from "a boss-class Ultra Reconstructor Robot, located near the upper centre of the map" (0.5;
    /// Cirkin's own text says "Ultra Warden Bot": to-fix row 52). TR MMO DB: level 40.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Taken at Cirkin in the squad's Junkyard, the scene bound to it (BoundMapContextId), as the Plateau instance missions.
    ///  - 1 the two Technician pools of the south-east (CrucibleStaalJunkyardNpcsV1) dead at once in the squad's copy
    ///    (SpawnPoolCleared, an all-pools set, as Plateau Holy Vengeance 1977); counts existing facts.
    ///  - 2 six personal Dolomite Blocks at TaRapedia's places (the client's crystal specimen UsableItemDispCrystalspecimen
    ///    7007, usabledata argument 3, ready 81, taken 82, as the item dispensers 11116; name 166). One use gives one block;
    ///    hidden 7 (the tracker's texts and its counter 13734 "Dolomite Finished") counts the uses, as The Departed's tracker;
    ///    the fifth completes 7 and 2. The sixth block stays; a use of it is refused (2 is no longer open, and the binding takes
    ///    no more than five). The item counter on 2 (n/5). Cirkin takes the blocks at the turn-in.
    ///  - Lamontix's and the Ultra Reconstructor's pools named public-spawn actors (in 1538's and this scene, as
    ///    PlateauMaligoOfficerSquadRespawnV1): in the squad's copy they come back on their own 90 s, not the copy's 45 minutes
    ///    (SpawnPoolManager.ReturnsOnOwnTime). The Technician pools come back with the copy.
    ///  - 3 the Energy Matrix a 100 % drop from the Ultra Reconstructor while 3 is open (CharacterOwned; in the squad's copy each
    ///    squad member near with the mission gets one, LootDispenserManager), taken at the turn-in.
    ///  - 5 the client's tracker, hidden: an aggregate of 1, 2 and 3; hidden 6 (5's texts) waits for 5 and opens 4 (an aggregate
    ///    does no actions), as Pools Bane Battle Plans 896. 4 the player walking up to Cirkin again (AreaEntered, 10 m).
    ///  - Markers on each Technician pool, each block, the Ultra Reconstructor and Cirkin.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40 (Graviton V04 Helmet
    ///    25119, Hazmat V03 Helmet 21400, Reflective V03 Boots 36339, Bio V04 Helmet 46021: the client has no Uncommon V05 or V04
    ///    of these in the band), one to choose.
    /// </summary>
    public static class CruciblePartOfTheBargainV1
    {
        public const uint MissionId = 1529;
        public const uint Level = 40;
        public const uint Cirkin = CrucibleStaalJunkyardNpcsV1.Cirkin;
        public const uint MapContextId = CrucibleStaalJunkyardNpcsV1.Junkyard;
        public const uint Technicians = 1, Dolomite = 2, EnergyMatrix = 3, ReturnToCirkin = 4, Tracker = 5, Opened = 6, DolomiteCount = 7;
        public const uint CirkinArea = 1;
        public const double CirkinRadius = 10, PoolMarkerRadius = 15, BlockMarkerRadius = 3, UltraMarkerRadius = 15;

        public const uint Taken = 1;                    // the scenario of the blocks' uses
        public const string UltraReconstructorRole = "ultra-reconstructor";

        public const uint DolomiteTemplate = 111144, DolomiteClass = 26401, Blocks = 5;
        public const string DolomiteKey = "junkyard-dolomite-blocks";
        public const uint EnergyMatrixTemplate = 111161, EnergyMatrixClass = 26415;
        public const string EnergyMatrixKey = "junkyard-energy-matrix";
        public const int EnergyMatrixChance = 100;

        public const uint BlockClass = 7007;            // UsableItemDispCrystalspecimenCONVERT2TD
        public const uint BlockName = 166;              // usablenameoverride "Dolomite Block"
        public const uint UseArgument = 3;              // usabledata lookup[7007][0]
        public const uint ReadyState = 81, TakenState = 82;

        /// <summary>The six Dolomite Blocks (TaRapedia's places, on the server navmesh's height).</summary>
        public static readonly ScenePosition[] BlocksAt =
        {
            new(306f, 263.51f, -65f), new(325f, 265.45f, -22f), new(225f, 242.21f, -172f), new(231f, 265.99f, -48f),
            new(28f, 243.67f, -219f), new(-331f, 263.43f, -36f)
        };

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint GravitonHelmet = 25119;       // Armor_T3_Graviton_V04_UNC_Helmet_40_to_44
        public const uint HazmatHelmet = 21400;         // Armor_T2_Hazmat_V03_UNC_Helmet_39_to_43
        public const uint ReflectiveBoots = 36339;      // Armor_T2_Reflective_V03_UNC_Boots_39_to_43
        public const uint BioHelmet = 46021;            // Armor_T3_Bio_V04_UNC_Helmet_40_to_44

        public static string BlockRole(int index) => $"dolomite-block-{index + 1}";
        public static uint BlockSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Your Part of the Bargain", 13656, Cirkin, Cirkin, Level);
            mission.Reward(Experience, Credits, new[] { GravitonHelmet, HazmatHelmet, ReflectiveBoots, BioHelmet });
            mission.Prerequisite(CrucibleCarefulNegotiationV1.MissionId);
            mission.Scenario(Taken, "A Dolomite Block taken", (uint)BlocksAt.Length);

            mission.Objective(Technicians, 13721, 13722, 0, MissionObjectiveState.Incomplete, counterTextId: 13723);
            mission.Objective(Dolomite, 13724, 13725, 1, MissionObjectiveState.Incomplete);
            mission.Objective(EnergyMatrix, 13726, 13727, 2, MissionObjectiveState.Incomplete);
            mission.Objective(ReturnToCirkin, 13728, 13729, 3, MissionObjectiveState.Inactive);
            mission.Objective(Tracker, 13730, 13731, 4, MissionObjectiveState.Incomplete, counterTextId: 13732, required: false);
            mission.Objective(Opened, 13730, 13731, 5, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(DolomiteCount, 13730, 13731, 6, MissionObjectiveState.Incomplete, counterTextId: 13734, required: false);

            // 1: both Technician pools dead at once in the squad's copy.
            mission.Transition(Technicians);
            var trigger = 1U;
            foreach (var (pool, _) in CrucibleStaalJunkyardNpcsV1.TechnicianPools)
                mission.Progress(Technicians, MissionProgressEventKind.SpawnPoolCleared, pool, null, trigger: trigger++);
            mission.Action(Technicians, 1, MissionActionKind.GrantReward, reward: 1);

            // 7, hidden: five blocks of six (the tracker's "Dolomite Finished" count). 2 with 7.
            mission.Transition(DolomiteCount);
            for (var block = 1U; block <= BlocksAt.Length; block++)
                mission.Progress(DolomiteCount, MissionProgressEventKind.ScenarioEvent, block, Blocks, counter: Taken, trigger: block);
            mission.Transition(Dolomite);
            mission.ObjectiveState(Dolomite, DolomiteCount, MissionObjectiveState.Completed);

            // 3: the Energy Matrix.
            mission.Transition(EnergyMatrix);
            mission.Progress(EnergyMatrix, MissionProgressEventKind.ItemAcquired, EnergyMatrixClass, 1);

            // 6, hidden: 5 (1, 2 and 3 done) opens 4.
            mission.Transition(Opened);
            mission.ObjectiveState(Opened, Tracker, MissionObjectiveState.Completed);
            CrucibleCalendarGirlV1.Open(mission, Opened, 1, 1, ReturnToCirkin);

            // 4: back to Cirkin.
            var cirkin = CrucibleStaalJunkyardNpcsV1.CirkinAt;
            mission.Area(CirkinArea, MapContextId, cirkin.X, cirkin.Y, cirkin.Z, CirkinRadius);
            mission.Transition(ReturnToCirkin);
            mission.EnterArea(ReturnToCirkin, CirkinArea);

            var marker = 1U;
            foreach (var (_, at) in CrucibleStaalJunkyardNpcsV1.TechnicianPools)
                mission.Indicator(Technicians, marker++, at.X, at.Y, at.Z, PoolMarkerRadius, MapContextId);
            foreach (var at in BlocksAt)
                mission.Indicator(Dolomite, marker++, at.X, at.Y, at.Z, BlockMarkerRadius, MapContextId);
            var ultra = CrucibleStaalJunkyardNpcsV1.UltraReconstructorAt;
            mission.Indicator(EnergyMatrix, marker++, ultra.X, ultra.Y, ultra.Z, UltraMarkerRadius, MapContextId);
            mission.Indicator(ReturnToCirkin, marker, cirkin.X, cirkin.Y, cirkin.Z, CirkinRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1529",
                "Native1 'Eliminate all Thrax Technicians' 13721/13722 counter13723, 2 'Collect Dolomite' 13724/13725, 3 'Collect " +
                "Energy Matrix' 13726/13727, 4 'Return to Cirkin' 13728/13729, 5 'Mission Tracker' 13730/13731 counters 13732, 13734, " +
                "13733; no objectiveconversation. Texts 13656-13660. Items class26401 Dolomite Block (111144), class26415 Energy " +
                "Matrix (111161). Usable name 166 'Dolomite Block'. Names 9643 Cirkin, 9640 Ultra Reconstructor.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Your_Part_of_the_Bargain",
                "Cirkin gives and rewards; requires Careful Negotiation; 180000XP, 8200 credits; Olympia Graviton Armor Helmet 5, " +
                "Pulsar Hazmat Armor Helmet 4, ChiTech Reflective Armor Boots 3, Vitalius Bio Armor Helmet 5; Dolomite at " +
                "306,262,-65 and five more; Technicians in the south-east; the Energy Matrix from a boss-class Ultra Reconstructor " +
                "near the upper centre. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CruciblePartOfTheBargainV1",
                "Bound to the squad's Junkyard 2138. 1 Technician pools 770036/770037 (770036, copy of 531020 at level40) cleared at " +
                "once; 2 six personal blocks (7007, name166, arg3, 81->82), hidden 7 counts five uses, the sixth stays; 3 a 100% drop from the " +
                "Ultra Reconstructor 770038 (class10314); hidden 5 aggregates 1-3, hidden 6 opens 4; 4 the 10 m area round Cirkin. " +
                "Items 25119, 21400, 36339, 46021, one to choose. Markers on the pools, the blocks, the Ultra Reconstructor, Cirkin.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own Dolomite Blocks in the squad's Junkyard, each taken once.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Cirkin in the squad's Junkyard
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { Tracker, Opened, DolomiteCount },
                ExistingFactObjectiveIds = new() { Technicians, EnergyMatrix },
                ObjectiveAggregations = new()
                {
                    [Tracker] = new MissionObjectiveAggregation(new[] { Technicians, Dolomite, EnergyMatrix }, 3)
                },
                Items = new()
                {
                    new MissionItemBinding(DolomiteKey, DolomiteTemplate, MissionItemScope.AssignmentIssued, Blocks,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        TurnInQuantity: Blocks, CounterObjectiveId: Dolomite),
                    new MissionItemBinding(EnergyMatrixKey, EnergyMatrixTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { CrucibleStaalJunkyardNpcsV1.UltraReconstructor }, EnergyMatrix,
                            MapContextId, EnergyMatrixChance, 1))
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < BlocksAt.Length; index++)
            {
                var role = BlockRole(index);
                var sequence = BlockSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, BlockClass, BlocksAt[index],
                    Orientation: index * 1.0471975512, InitialObjectState: ReadyState, NameOverrideId: BlockName,
                    UseAction: new SceneObjectAction(MissionId, Dolomite, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-taken", role, TakenState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-dolomite", MissionId, DolomiteKey, DolomiteTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Taken, (uint)index + 1) }
                };
            }
            // The boss named, no sequence: back on its pool's own time in the squad's copy (SpawnPoolManager.ReturnsOnOwnTime).
            scene.Actors[UltraReconstructorRole] = new SceneActorDefinition(UltraReconstructorRole, SceneActorKind.PublicSpawn,
                CrucibleStaalJunkyardNpcsV1.UltraReconstructor);
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
