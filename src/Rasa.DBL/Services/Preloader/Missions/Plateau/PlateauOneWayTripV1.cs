using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// One Way Trip (native mission 877): Colonel Bosley at Camp Resistance sends the player through Ustor Yard
    /// (squad instance 1502) onto the Ustor Bridge and into Maligo Base (squad instance 1830) to find and destroy
    /// the Bane's prototype wormhole; then back to him.
    ///
    /// Native: texts 4595-4598 and 7895 (missionconversation): Bosley's offer "there's a maintenance bridge
    /// connecting Ustor Yard to Maligo, and there's a Bane teleporter on the bridge", turn-in "With that Wormhole
    /// device offline ... Altera and boys are still alive and kicking"; objectives (missionobjective), no counters:
    /// 10 "Rendezvous With Field Lt. Rapp" 9122/9123, 9 "Gain Access to Maligo Bridge Through Ustor Yard's Western
    /// Exit" 9124/9125, 1 "Locate the Bane Wormhole Device" 4607/7896, 6 "Determine why there are Bane and
    /// Prisoners slaughtered" 6231/7897, 11 "Find out how to power down the wormhole" 17321/17322, 7 "ITEM - Obtain
    /// Level 3 Access Keypass" 6238/7900, 14 "Wormhole Powercube obtained" 17354/17355, 4 "Power down Wormhole
    /// Device" 4842/7898, 16 "Speak with Demolitionist Maybauer for further instructions" 19230/19231, 15 "Bomb
    /// Obtained" 17356/17357, 12 "Destroy the wormhole prototype" 17346/17347 ("place a bomb, destroying it before
    /// any mind controlled prisoners make their way into the device"), 13 "Return to Colonel Bosley" 17351/17352.
    /// The talks (objectiveconversation): 10 Rapp 1303 (9126), 6 Neuman 981 (6232), 11 Neuman 981 (17344: "locate
    /// the powercube that activates its power and place it into its corresponding power standby socket"), 16
    /// Maybauer 982 (20235, and 19232 on flag 115: "the Bane have started to march the prisoners towards the
    /// prototype. Take one of the bombs from the crate next to me and place it on the device"), 12 Homburg 2220
    /// (17350: "I've had my men rewire this local teleporter"), 13 Bosley 1001 (17512). Items: MaligoLevel3Pass
    /// (class 21054, template 42416), MaligoWHActivationCube (21443, 42448), MaligoBomb (26720, 117046). Objects:
    /// UsableInertDestBanePrototypeWormholeDeviceV01 (10174). The client's Maligo map places none of them.
    ///
    /// Sourced: TaRapedia One Way Trip: giver and receiver Colonel Bosley, level "33 (possibly less)", requires
    /// Blueprint For Victory, follow-ups South Of The Border and New Orders From The General; 90,000 XP, 8,000
    /// credits, four level 30 items: Phoenix Bio Armor Gloves, Phoenix Graviton Armor Helmet, ChiTech Mech Armor
    /// Gloves, Titan Stealth Armor Gloves. Walkthrough (PlanetTR t-3649, 2008-02-12): cross the security beams near
    /// "Special Projects" for 1; the powercube into the socket, the terminal beside it opens the wormhole room;
    /// Maybauer's talk starts a timer (about 15 minutes, forum) while the prisoners walk to the wormhole; a bomb
    /// from the crate next to him. Ten Ton Hammer (Maligo upper level, 2008-03): Overseer Kythura drops the Level
    /// 3 keypass south of the second beams; Homburg teleports in after the bomb. Patch notes: the mission fails
    /// when the time runs out, and is taken again from Bosley.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23; see PlateauMaligoBaseV1 for the map):
    ///  - The order (the client has none): 10 Rapp, 9 the bridge, then 6 and 1 (the beams lie between Neuman and
    ///    the socket, so 1 is always done on the way), 11, 7 (Kythura beyond the beams), 14 (the computer bank
    ///    appears once the Level 3 keypass is held), 4 (the socket), 16 Maybauer (appears at the socket), then 15
    ///    and 12 together, 13. Objective 6 at Neuman, as the client's package 981 (the walkthrough says Altera).
    ///  - 9: the area at the Maligo door's end of the Ustor Bridge, on the Plateau (PlateauMaligoBaseV1's link
    ///    from Ustor's west exit sets the player down on the bridge).
    ///  - 1: the area of the second laser beams (client ArchBaneTripWireEmitter at (-151, -88, 114..142)), by the
    ///    Special Projects region label (-289, -100, 128) on Level 2.
    ///  - 7: Overseer Kythura's keypass, a personal 100% drop while 7 is open, kept. CHANGE LATER: for every player.
    ///  - 14: a personal computer bank (UsableStatelessBaneConsoleKeyboard 20631) on the floor by the client's
    ///    command center console (-118, -98, 106.6; the navmesh there is cut off from Level 2) gives the powercube. 4: the personal socket (UsableTwoStateBaneConsoleKeycardSlot
    ///    11309) on the overlook above Special Projects takes it; Maybauer appears beside it.
    ///  - 16: Maybauer's talk starts objective 12, which fails the mission after <see cref="DeadlineSeconds"/> (15
    ///    minutes, the forum's HUD timer). Four Mind Controlled Soldiers walk slowly through the chamber to the
    ///    device and stop beside it at about the deadline: a sight only. CHANGE LATER: in 2008 the prisoners
    ///    reaching the device may have failed the mission (no source says).
    ///  - 15: the personal crate (Bane Cargo Crate 21568, as Bomb Voyage's) beside Maybauer gives the bomb.
    ///  - The chamber: the Special Projects room (the client's ChunnelRoomMaligo at (-246, -100, 128)) is an island
    ///    on the navmesh. The terminal (20631) beside the socket takes the player in (a scene transfer). CHANGE
    ///    LATER: a door that opens. The way out is the chamber's local teleporter (PlateauMaligoBaseV1).
    ///  - 12: the bomb is set at the device by an Explosive Charge (UsableBombHumV01 7870, usable once 15 is done);
    ///    the device shows destroyed, the deadline is met, and Homburg appears; his talk completes 12.
    ///  - The rewards: templates 120354-120357, the Uncommon V04 30-34 Bio gloves, Graviton helmet, Mech gloves and
    ///    Stealth gloves, four mission-reward templates in TaRapedia's order (the manufacturer names are not in
    ///    the client tables). Level 33.
    ///  - Indicators on every objective, in the instances too: 10 at Rapp (Ustor Yard), 9 on the bridge, 1 at the
    ///    second beams, 6 and 11 at Neuman, 7 at Kythura, 14 at the computer bank, 4 at the socket, 16 at Maybauer, 15 at
    ///    the bomb crate, 12 at the device and at the terminal that takes the player into the chamber, 13 on Bosley. The
    ///    Maligo places are this class's and PlateauMaligoBaseV1's (ESTIMATE).
    /// </summary>
    public static class PlateauOneWayTripV1
    {
        public const uint MissionId = 877;
        public const uint Level = 33;
        public const uint Bosley = PlateauGiantKillerV1.Bosley, BosleyPackage = PlateauGiantKillerV1.BosleyPackage;

        public const uint Locate = 1, PowerDown = 4, Massacre = 6, Keypass = 7, Bridge = 9, Rapp = 10, HowTo = 11,
            Destroy = 12, Report = 13, Powercube = 14, Bomb = 15, Maybauer = 16;
        public const uint BridgeArea = 1, BeamsArea = 2;
        public const uint DeadlineSeconds = 900;

        /// <summary>Scene sequences: the scenarios' (started by actions), then the uses'.</summary>
        public const uint BankAppears = 1, SocketAppears = 2, March = 3, Armed = 4, Results = 5, CubeTaken = 6, BombTaken = 7;
        public const uint BankUsed = 21, SocketUsed = 22, CrateUsed = 23, IntoChamber = 24, Planted = 25;
        public const uint Socketed = 1, Taken = 1;

        public const uint Pass = 42416, PassClass = 21054;
        public const uint Cube = 42448, CubeClass = 21443;
        public const uint BombItem = 117046, BombClass = 26720;
        public const string PassKey = "maligo-level-3-keypass", CubeKey = "wormhole-powercube", BombKey = "maligo-bomb";

        public const uint BankClass = PlateauSilentButDeadlyV1.ComputerClass, BankState = PlateauSilentButDeadlyV1.ComputerState;
        public const uint SocketClass = PlateauMonkeyWrenchV1.SlotClass;
        public const uint SocketReadyState = PlateauMonkeyWrenchV1.SlotReadyState, SocketSetState = PlateauMonkeyWrenchV1.SlotSetState;
        public const uint TerminalClass = PlateauSilentButDeadlyV1.ComputerClass;
        public const uint CrateClass = PlateauBombVoyageV1.CrateClass, CrateUseArgument = PlateauBombVoyageV1.UseArgument,
            CrateState = PlateauBombVoyageV1.CrateState;
        public const uint DeviceClass = 10174;          // UsableInertDestBanePrototypeWormholeDeviceV01
        public const uint DeviceIntactState = 110, DeviceDestroyedState = 2;
        /// <summary>A destruction it never reaches (not targetable), so that the charge's blow shows as a destruction
        /// (DestroyableStates): the client ignores a 110 -> 2 use transition (PlateauInterceptionV1.PumpHitPoints).</summary>
        public const uint DeviceHitPoints = 100;
        public const uint ChargeClass = 7870;   // UsableBombHumV01 "Explosive Charge" (drawn while usable)
        public const uint UseArgument = 1;
        public const string BankRole = "computer-bank", SocketRole = "standby-socket", TerminalRole = "chamber-terminal",
            CrateRole = "bomb-crate", DeviceRole = "wormhole-device", ChargeRole = "explosive-charge",
            MaybauerRole = "maybauer", HomburgRole = "homburg";

        public const uint Experience = 90000;
        public const uint Credits = 8000;
        public const uint BioGloves = 120354, GravitonHelmet = 120355, MechGloves = 120356, StealthGloves = 120357;

        /// <summary>The bridge's Maligo end, walked into from where Ustor's west exit sets the player down.</summary>
        public static readonly (double X, double Y, double Z) BridgeAt = (16, 424.5, -617);
        public const double BridgeRadius = 7;

        public static readonly (double X, double Y, double Z) BeamsAt = (-151, -90.28, 128);
        public const double BeamsRadius = 14;
        public const double NpcRadius = 8, ObjectRadius = 6, OfficerRadius = 15;

        public static readonly ScenePosition BankAt = new(-111, -100.28f, 116);
        public static readonly ScenePosition SocketAt = new(-186.33f, -90.28f, 122.4f);
        public static readonly ScenePosition TerminalAt = new(-185.93f, -90.28f, 133.6f);
        public static readonly ScenePosition MaybauerAt = new(-176.73f, -90.28f, 128);
        public static readonly ScenePosition CrateAt = new(-173.99f, -90.28f, 133.44f);

        /// <summary>The Special Projects chamber: where the terminal sets the player down (facing west), the device, the charge, Homburg.</summary>
        public static readonly ScenePosition ChamberArrival = new(-222, -104.28f, 128);
        public const double ChamberArrivalYaw = 1.5708;
        public static readonly ScenePosition DeviceAt = new(-246, -104.28f, 128);
        /// <summary>5 m west of the arrival, on the line to the device: at 3 m and at 11 m from the device the real
        /// client's walk stopped 5-7 m short of it (the device's collision and the chamber's floor around it), out of
        /// use range (to-fix #31). ESTIMATE place.</summary>
        public static readonly ScenePosition ChargeAt = new(-227, -104.28f, 128);
        public static readonly ScenePosition HomburgAt = new(-228, -104.28f, 120);

        /// <summary>The prisoners' walks across the chamber: four stops of 200 s, at 0.4 m/s, so they stand at the device at about the deadline.</summary>
        public const uint StopMilliseconds = 200000;
        public const float MarchSpeed = 0.4f;
        public static readonly float[] PrisonerLanes = { 112, 120, 136, 144 };
        public static readonly float[] MarchX = { -272, -266, -261, -256, -252 };

        public const double MarchYaw = -1.5708;         // east, to the device

        public static string PrisonerRole(int index) => $"prisoner-{index + 1}";
        public static string MarchRoute(int index) => $"march-{index + 1}";

        /// <summary>
        /// Prisoner <paramref name="index"/>'s walk: from the chamber's west end along its lane, closing half-way in on
        /// the device's line, a stop at each point between the first and the last.
        /// </summary>
        public static SceneWaypoint[] MarchOf(int index)
        {
            var lane = PrisonerLanes[index];
            var last = MarchX.Length - 1;
            return MarchX.Select((x, step) => new SceneWaypoint(
                new ScenePosition(x, ChamberArrival.Y, lane + (DeviceAt.Z - lane) * 0.5f * step / last),
                MarchYaw, step == 0 || step == last ? 0 : StopMilliseconds)).ToArray();
        }

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "One Way Trip", 4595, Bosley, Bosley, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves });
            mission.Prerequisite(PlateauBlueprintForVictoryV1.MissionId);

            mission.Objective(Rapp, 9122, 9123, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Bridge, 9124, 9125, 1, MissionObjectiveState.Inactive);
            mission.Objective(Locate, 4607, 7896, 2, MissionObjectiveState.Inactive);
            mission.Objective(Massacre, 6231, 7897, 3, MissionObjectiveState.Inactive);
            mission.Objective(HowTo, 17321, 17322, 4, MissionObjectiveState.Inactive);
            mission.Objective(Keypass, 6238, 7900, 5, MissionObjectiveState.Inactive);
            mission.Objective(Powercube, 17354, 17355, 6, MissionObjectiveState.Inactive);
            mission.Objective(PowerDown, 4842, 7898, 7, MissionObjectiveState.Inactive);
            mission.Objective(Maybauer, 19230, 19231, 8, MissionObjectiveState.Inactive);
            mission.Objective(Bomb, 17356, 17357, 9, MissionObjectiveState.Inactive);
            mission.Objective(Destroy, 17346, 17347, 10, MissionObjectiveState.Inactive);
            mission.Objective(Report, 17351, 17352, 11, MissionObjectiveState.Inactive);
            mission.Scenario(BankAppears, "The computer bank with the powercube", 0);
            mission.Scenario(SocketAppears, "The standby powercube socket", 0);
            mission.Scenario(March, "The prisoners march; the bomb crate, the terminal and the device", 0);
            mission.Scenario(Armed, "The bomb in hand: the charge at the device", 0);
            mission.Scenario(Results, "The powercube in the socket", 1);
            mission.Scenario(CubeTaken, "The powercube taken from the computer bank", 1);
            mission.Scenario(BombTaken, "A bomb taken from the crate", 1);
            mission.Area(BridgeArea, PlateauMissionDataV1.MapContextId, BridgeAt.X, BridgeAt.Y, BridgeAt.Z, BridgeRadius);
            mission.Area(BeamsArea, PlateauMaligoBaseV1.MapContextId, BeamsAt.X, BeamsAt.Y, BeamsAt.Z, BeamsRadius);

            mission.Transition(Rapp);
            mission.Conversation(Rapp, PlateauUstorYardV1.RappPackage);
            PlateauSilentButDeadlyV1.Open(mission, Rapp, 1, Bridge);

            mission.Transition(Bridge);
            mission.EnterArea(Bridge, BridgeArea);
            PlateauSilentButDeadlyV1.Open(mission, Bridge, 1, Locate);
            PlateauSilentButDeadlyV1.Open(mission, Bridge, 3, Massacre);

            mission.Transition(Locate);
            mission.EnterArea(Locate, BeamsArea);

            mission.Transition(Massacre);
            mission.Conversation(Massacre, PlateauMaligoBaseV1.NeumanPackage);
            PlateauSilentButDeadlyV1.Open(mission, Massacre, 1, HowTo);

            mission.Transition(HowTo);
            mission.Conversation(HowTo, PlateauMaligoBaseV1.NeumanPackage);
            PlateauSilentButDeadlyV1.Open(mission, HowTo, 1, Keypass);

            mission.Transition(Keypass);
            mission.Progress(Keypass, MissionProgressEventKind.ItemAcquired, PassClass, 1);
            PlateauSilentButDeadlyV1.Open(mission, Keypass, 1, Powercube);
            mission.Action(Keypass, 3, MissionActionKind.StartScenario, scenario: BankAppears);

            mission.Transition(Powercube);
            mission.Progress(Powercube, MissionProgressEventKind.ScenarioEvent, Taken, null, counter: CubeTaken);
            PlateauSilentButDeadlyV1.Open(mission, Powercube, 1, PowerDown);
            mission.Action(Powercube, 3, MissionActionKind.StartScenario, scenario: SocketAppears);

            mission.Transition(PowerDown);
            mission.Progress(PowerDown, MissionProgressEventKind.ScenarioEvent, Socketed, null, counter: Results);
            PlateauSilentButDeadlyV1.Open(mission, PowerDown, 1, Maybauer);

            // 16: Maybauer's talk opens 15 and 12; 12 coming open starts its deadline.
            mission.Transition(Maybauer);
            mission.Conversation(Maybauer, PlateauMaligoBaseV1.MaybauerPackage);
            PlateauSilentButDeadlyV1.Open(mission, Maybauer, 1, Bomb);
            PlateauSilentButDeadlyV1.Open(mission, Maybauer, 3, Destroy);
            mission.Action(Maybauer, 5, MissionActionKind.StartScenario, scenario: March);

            mission.Transition(Bomb);
            mission.Progress(Bomb, MissionProgressEventKind.ScenarioEvent, Taken, null, counter: BombTaken);
            mission.Action(Bomb, 1, MissionActionKind.StartScenario, scenario: Armed);

            // 12: transition 1 fails it at the deadline; transition 2 is Homburg's talk, after the bomb.
            mission.FailOnDeadline(Destroy, DeadlineSeconds);
            mission.Transition(Destroy, 2, 2);
            mission.Conversation(Destroy, PlateauMaligoBaseV1.HomburgPackage, 1, 2);
            mission.Action(Destroy, 1, MissionActionKind.RevealObjective, targetObjective: Report, transition: 2);
            mission.Action(Destroy, 2, MissionActionKind.ActivateObjective, targetObjective: Report,
                state: MissionObjectiveState.Incomplete, transition: 2);

            mission.Transition(Report);
            mission.Conversation(Report, BosleyPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Bridge, 1, BridgeAt.X, BridgeAt.Y, BridgeAt.Z, 15);
            mission.Indicator(Report, 2, -632.3, 447.95, -232.5);   // Colonel Bosley (pool 510176)
            var rapp = PlateauUstorYardV1.RappAt;
            var neuman = PlateauMaligoBaseV1.NeumanAt;
            var kythura = PlateauMaligoBaseV1.KythuraAt;
            mission.Indicator(Rapp, 3, rapp.X, rapp.Y, rapp.Z, NpcRadius);
            mission.Indicator(Locate, 4, BeamsAt.X, BeamsAt.Y, BeamsAt.Z, BeamsRadius);
            mission.Indicator(Massacre, 5, neuman.X, neuman.Y, neuman.Z, NpcRadius);
            mission.Indicator(HowTo, 6, neuman.X, neuman.Y, neuman.Z, NpcRadius);
            mission.Indicator(Keypass, 7, kythura.X, kythura.Y, kythura.Z, OfficerRadius);
            mission.Indicator(Powercube, 8, BankAt.X, BankAt.Y, BankAt.Z, ObjectRadius);
            mission.Indicator(PowerDown, 9, SocketAt.X, SocketAt.Y, SocketAt.Z, ObjectRadius);
            mission.Indicator(Maybauer, 10, MaybauerAt.X, MaybauerAt.Y, MaybauerAt.Z, NpcRadius);
            mission.Indicator(Bomb, 11, CrateAt.X, CrateAt.Y, CrateAt.Z, ObjectRadius);
            mission.Indicator(Destroy, 12, DeviceAt.X, DeviceAt.Y, DeviceAt.Z, ObjectRadius);
            mission.Indicator(Destroy, 13, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, ObjectRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/877",
                "Native10 Rapp 9122/9123, 9 bridge 9124/9125, 1 locate 4607/7896, 6 massacre 6231/7897, 11 power down how 17321/17322, " +
                "7 Level 3 keypass 6238/7900, 14 powercube 17354/17355, 4 power down 4842/7898, 16 Maybauer 19230/19231, 15 bomb " +
                "17356/17357, 12 destroy 17346/17347, 13 Bosley 17351/17352; no counters. objectiveconversation 10/1303 9126, 6/981 " +
                "6232, 11/981 17344, 16/982 20235 (19232 flag115), 12/2220 17350, 13/1001 17512. Texts 4595-4598, 7895. Pass " +
                "21054/42416, cube 21443/42448, bomb 26720/117046, device class10174.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/One_Way_Trip",
                "Giver/receiver Colonel Bosley; level '33 (possibly less)'; requires Blueprint For Victory; 90000XP, 8000 credits; " +
                "Phoenix Bio Armor Gloves/Phoenix Graviton Armor Helmet/ChiTech Mech Armor Gloves/Titan Stealth Armor Gloves. " +
                "Walkthrough (PlanetTR t-3649): beams near Special Projects, powercube to the socket, the terminal opens the " +
                "wormhole room, Maybauer starts the timer, a bomb from his crate.");
            mission.Evidence(3, MissionEvidenceSourceKind.Documentation, "https://www.tentonhammer.com/guides/guide-to-maligo-base-upper-level",
                "Ten Ton Hammer 2008-03: Kythura drops the Level 3 keypass south of the second beams; the computer bank gives the " +
                "powercube; Maybauer pops in when it is placed; Homburg teleports in after the bomb. Forum: a 15 minute HUD timer, " +
                "the mission fails when it runs out.", 0.5);
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauOneWayTripV1",
                "Level33. Order 10, 9, {1, 6}, 11, 7, 14, 4, 16, {15, 12}, 13. 9 on the bridge (1497), 1 at the second beams (1830). " +
                "Kythura650089's keypass a personal 100% drop. Personal bank20631, socket11309, Maybauer650085, crate21568, terminal " +
                "20631 (scene transfer into the Special Projects chamber), device10174, charge7870, Homburg650086. 12 fails after " +
                "900s; four Mind Controlled Soldiers650087 walk to the device (a sight only). Rewards 120354-120357. CHANGE LATER: " +
                "squad-wide drops, real doors, a keypass lock. Places are estimates. Indicators on every objective.", 0.3);

            mission.Enable(Scene());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>The personal objects and NPCs of the mission, on the squad's copy of Maligo.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = PlateauMaligoBaseV1.MapContextId,   // accepted at Bosley on the Plateau, played in Maligo Base
                MapContextId = PlateauMaligoBaseV1.MapContextId,
                ExistingFactObjectiveIds = new() { Keypass },
                Items = new()
                {
                    new MissionItemBinding(PassKey, Pass, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { PlateauMaligoBaseV1.Kythura }, Keypass, PlateauMaligoBaseV1.MapContextId, 100, 1)),
                    new MissionItemBinding(CubeKey, Cube, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(BombKey, BombItem, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                Routes = new()
            };

            scene.Actors[BankRole] = new SceneActorDefinition(BankRole, SceneActorKind.Object, BankClass, BankAt,
                Orientation: 3.1416, InitialObjectState: BankState, UseAction: new SceneObjectAction(MissionId, Powercube, BankUsed, UseArgument));
            scene.Actors[SocketRole] = new SceneActorDefinition(SocketRole, SceneActorKind.Object, SocketClass, SocketAt,
                InitialObjectState: SocketReadyState, UseAction: new SceneObjectAction(MissionId, PowerDown, SocketUsed, UseArgument));
            scene.Actors[MaybauerRole] = new SceneActorDefinition(MaybauerRole, SceneActorKind.Creature, PlateauMaligoBaseV1.Maybauer,
                MaybauerAt, Orientation: 1.5708);
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, CrateAt,
                InitialObjectState: CrateState, UseAction: new SceneObjectAction(MissionId, Bomb, CrateUsed, CrateUseArgument));
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: 1.5708, InitialObjectState: BankState, UseAction: new SceneObjectAction(MissionId, Destroy, IntoChamber, UseArgument));
            scene.Actors[DeviceRole] = new SceneActorDefinition(DeviceRole, SceneActorKind.Object, DeviceClass, DeviceAt,
                InitialObjectState: DeviceIntactState, InitiallyInteractable: false, ViewRadius: 60,
                Destruction: new SceneObjectDestruction(MissionId, Destroy, Planted, DeviceHitPoints, DeviceDestroyedState));
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                InitiallyInteractable: false, UseAction: new SceneObjectAction(MissionId, Destroy, Planted, UseArgument));
            scene.Actors[HomburgRole] = new SceneActorDefinition(HomburgRole, SceneActorKind.Creature, PlateauMaligoBaseV1.Homburg,
                HomburgAt, Orientation: -1.5708);

            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[BankAppears] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-bank", BankRole) } };
            scene.Sequences[BankUsed] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("bank-done", BankRole, false) },
                Character = { new IssueMissionItemIntent("take-powercube", MissionId, CubeKey, Cube, 1) },
                Signals = { new SceneMissionSignal(MissionId, CubeTaken, Taken) }
            };
            scene.Sequences[SocketAppears] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-socket", SocketRole) } };
            scene.Sequences[SocketUsed] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("cube-in", SocketRole, SocketSetState),
                    new SetInteractionIntent("socket-done", SocketRole, false),
                    new EnsureActorIntent("maybauer-comes", MaybauerRole)
                },
                Character = { new ConsumeMissionItemIntent("cube-in-socket", MissionId, CubeKey, 1, MissionItemScope.AssignmentIssued) },
                Signals = { new SceneMissionSignal(MissionId, Results, Socketed) }
            };

            var march = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("create-crate", CrateRole),
                    new EnsureActorIntent("create-terminal", TerminalRole),
                    new EnsureActorIntent("create-device", DeviceRole),
                    new EnsureActorIntent("create-charge", ChargeRole)
                }
            };
            for (var index = 0; index < PrisonerLanes.Length; index++)
            {
                var role = PrisonerRole(index);
                var points = MarchOf(index);
                scene.Routes[MarchRoute(index)] = new SceneRoute(MarchRoute(index), points, Speed: MarchSpeed);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PlateauMaligoBaseV1.MindControlled,
                    points[0].Position, Orientation: MarchYaw, InitiallyInteractable: false);
                march.World.Add(new EnsureActorIntent($"{role}-comes", role));
                march.World.Add(new RunRouteIntent($"{role}-marches", role, MarchRoute(index)));
            }
            scene.Sequences[March] = march;

            scene.Sequences[CrateUsed] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("crate-done", CrateRole, false) },
                Character = { new IssueMissionItemIntent("take-bomb", MissionId, BombKey, BombItem, 1) },
                Signals = { new SceneMissionSignal(MissionId, BombTaken, Taken) }
            };
            scene.Sequences[Armed] = new SceneSequenceDefinition { World = { new SetInteractionIntent("charge-ready", ChargeRole, true) } };
            scene.Sequences[IntoChamber] = new SceneSequenceDefinition
            {
                World = { new TransferIntent("into-chamber", PlateauMaligoBaseV1.MapContextId, ChamberArrival, ChamberArrivalYaw) }
            };
            scene.Sequences[Planted] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("charge-done", ChargeRole, false),
                    new TransitionObjectStateIntent("device-destroyed", DeviceRole, DeviceDestroyedState),
                    new EnsureActorIntent("homburg-comes", HomburgRole)
                },
                Character =
                {
                    new ConsumeMissionItemIntent("bomb-on-device", MissionId, BombKey, 1, MissionItemScope.AssignmentIssued),
                    new MissionDeadlineIntent("prototype-destroyed", MissionId, DeadlineIntentKind.Satisfy)
                }
            };
            return scene;
        }
    }
}
