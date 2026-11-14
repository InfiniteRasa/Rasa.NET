using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Not My Junk (native mission 1538), Ligo Crucible and the Staal Junkyard (squad instance 2138): Awol Captain Cheung needs a
    /// Plasma Power Converter for the Awol Camp's generator; Cirkin has none, the Bane took them, and Lamontix holds the key to
    /// their lockbox.
    ///
    /// Native: texts 13862-13866 (missionconversation): log "Travel to the Staal Junkyard and speak with a Brann named Cirkin, to
    /// see if you can obtain a Plasma Power Converter for Captain Chueng at the Awol Camp.", turn-in "Great job getting us the
    /// Plasma Power Converter. ... Here's the relic. ... He called it an "Eloh Resonator.""; objectives (missionobjective) 1
    /// "Speak to Cirkin" 13877/13878, 2 "Kill Lamontix" 13880/13881 ("... to gain acces to the Bane Lock Box. Lamontix is located
    /// in the Bane controlled area directly east of Cirkin."), 3 "Return to Captain Cheung" 13882/13883, 4 "Retrieve the Power
    /// Converter" 13964/13965, no counters; objective conversation (1538,1) package 1742 Cirkin text 13943 ("That Zupa Bane named
    /// Lamontix he'd be the one to talk to! ... Lamontix somewhere directly East of here."). Items class 26438
    /// MisJunkyardItemBaneLockboxkeycard (template 111182), 26437 MisJunkyardItemPlasmapowerconverter (111181), 26797
    /// MisCrucibleItemElohResonator (117177). Usable name 175 "Bane Lockbox" (usablenameoverride).
    ///
    /// Sourced: TaRapedia Not My Junk: giver and reward giver Awol Captain Cheung (Awol Camp); requires Sweet Revenge or Making
    /// Amends; follow-up Point Of Impact; 85,000 XP, 0 credits; reward Eloh Resonator; "Lamontix, and the all important crate,
    /// are located around 339.1,-70.5", "The Plasma Power Conveter is in a nearby Bane Bin"; "Bane Lockbox Keycard" x/1; the
    /// keycard did not drop and the lockbox opened without it until the patch of 2008-01-31 (TaR-hist rev 26132/30380, 0.3).
    /// TR MMO DB: level 40.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Offered after Sweet Revenge (1205) or Making Amends (1155) completed (scene Requirement, an AnyRequirement).
    ///  - The scene bound to the Junkyard (BoundMapContextId): taken at Cheung on the Crucible, it waits for its owner in the
    ///    squad's Junkyard, as the Plateau instance missions. Objectives one by one: 1 Cirkin's talk opens 2; 2 the keycard
    ///    picked up (ItemAcquired: a personal 100 % drop from Lamontix while 2 is open, CharacterOwned; in the squad's copy each
    ///    squad member near with the mission gets one) opens 4; 4 the player's own Bane Lockbox (the item crate
    ///    UsableItemDisp_item_crate_bane 11116, usabledata argument 3, ready 81, taken 82, name 175) 15 m west of Lamontix by
    ///    TaRapedia's place: a use gives the converter and takes the keycard; it opens 3. The keycard is the gate: 4 is not
    ///    open, so the lockbox takes no use, before the keycard is held (an AssignmentItemRequirement takes only issued items).
    ///    3 the player walking up to Cheung on the Crucible (AreaEntered, 10 m; the client has no talk for it); Cheung takes
    ///    the converter at the turn-in.
    ///  - Markers: 1 on the Junkyard bridge road (Crucible) and on Cirkin; 2 on Lamontix; 4 on the lockbox; 3 on Cheung.
    /// </summary>
    public static class CrucibleNotMyJunkV1
    {
        public const uint MissionId = 1538;
        public const uint Level = 40;
        public const uint Cheung = CrucibleAwolCampNpcsV1.Cheung;
        public const uint CirkinPackage = CrucibleStaalJunkyardNpcsV1.CirkinPackage;
        public const uint MapContextId = CrucibleStaalJunkyardNpcsV1.Junkyard;
        public const uint SpeakToCirkin = 1, KillLamontix = 2, ReturnToCheung = 3, Retrieve = 4;
        public const uint CheungArea = 1;
        public const double CheungRadius = 10, CirkinRadius = 10, BridgeMarkerRadius = 8, LamontixMarkerRadius = 10, LockboxMarkerRadius = 3;

        public const uint Opened = 1;                   // the scenario of the lockbox's use

        public const uint KeycardTemplate = 111182, KeycardClass = 26438;
        public const string KeycardKey = "bane-lockbox-keycard";
        public const int KeycardChance = 100;
        public const uint ConverterTemplate = 111181, ConverterClass = 26437;
        public const string ConverterKey = "plasma-power-converter";

        public const string LockboxRole = "bane-lockbox";
        public const uint LockboxClass = 11116;         // UsableItemDisp_item_crate_bane_v01CONVERT2TD
        public const uint LockboxName = 175;            // usablenameoverride "Bane Lockbox"
        public const uint UseArgument = 3;              // usabledata lookup[11116][0]
        public const uint ReadyState = 81, TakenState = 82;
        public const uint LockboxSequence = 1;
        public const string LamontixRole = "lamontix";
        public static readonly ScenePosition LockboxAt = new(343f, 263.8f, -68f);

        public const uint Experience = 85000;
        public const uint Credits = 0;
        public const uint ElohResonator = 117177;       // MisCrucibleItemElohResonator

        /// <summary>Offered once Sweet Revenge (1205) or Making Amends (1155) is completed.</summary>
        public static MissionRequirement AfterTheLedger() => new AnyRequirement(new MissionRequirement[]
        {
            new MissionStateRequirement(CrucibleMakingAmendsV1.RevengeMissionId, MissionState.Completed),
            new MissionStateRequirement(CrucibleMakingAmendsV1.MissionId, MissionState.Completed)
        });

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Not My Junk", 13862, Cheung, Cheung, Level);
            mission.Reward(Experience, Credits, new uint[0], new[] { ElohResonator });
            mission.Scenario(Opened, "The Bane Lockbox opened", 1);

            mission.Objective(SpeakToCirkin, 13877, 13878, 0, MissionObjectiveState.Incomplete);
            mission.Objective(KillLamontix, 13880, 13881, 1, MissionObjectiveState.Inactive);
            mission.Objective(Retrieve, 13964, 13965, 2, MissionObjectiveState.Inactive);
            mission.Objective(ReturnToCheung, 13882, 13883, 3, MissionObjectiveState.Inactive);

            // 1: Cirkin has no converter; Lamontix has the key.
            mission.Transition(SpeakToCirkin);
            mission.Conversation(SpeakToCirkin, CirkinPackage);
            CrucibleCalendarGirlV1.Open(mission, SpeakToCirkin, 1, 1, KillLamontix);

            // 2: the keycard from Lamontix.
            mission.Transition(KillLamontix);
            mission.Progress(KillLamontix, MissionProgressEventKind.ItemAcquired, KeycardClass, 1);
            CrucibleCalendarGirlV1.Open(mission, KillLamontix, 1, 1, Retrieve);

            // 4: the lockbox gives the converter and takes the keycard.
            mission.Transition(Retrieve);
            mission.Progress(Retrieve, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Opened);
            mission.Action(Retrieve, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("lockbox-takes-the-keycard", MissionId, KeycardKey, 1, MissionItemScope.CharacterOwned));
            CrucibleCalendarGirlV1.Open(mission, Retrieve, 1, 2, ReturnToCheung);

            // 3: back to Cheung on the Crucible.
            var cheung = CrucibleAwolCampNpcsV1.At(Cheung);
            mission.Area(CheungArea, CrucibleMissionDataV1.MapContextId, cheung.X, cheung.Y, cheung.Z, CheungRadius);
            mission.Transition(ReturnToCheung);
            mission.EnterArea(ReturnToCheung, CheungArea);
            mission.Action(ReturnToCheung, 1, MissionActionKind.GrantReward, reward: 1);

            var bridge = CrucibleStaalJunkyardNpcsV1.BridgeAt;
            var cirkin = CrucibleStaalJunkyardNpcsV1.CirkinAt;
            var lamontix = CrucibleStaalJunkyardNpcsV1.LamontixAt;
            mission.Indicator(SpeakToCirkin, 1, bridge.X, bridge.Y, bridge.Z, BridgeMarkerRadius, CrucibleMissionDataV1.MapContextId);
            mission.Indicator(SpeakToCirkin, 2, cirkin.X, cirkin.Y, cirkin.Z, CirkinRadius, MapContextId);
            mission.Indicator(KillLamontix, 3, lamontix.X, lamontix.Y, lamontix.Z, LamontixMarkerRadius, MapContextId);
            mission.Indicator(Retrieve, 4, LockboxAt.X, LockboxAt.Y, LockboxAt.Z, LockboxMarkerRadius, MapContextId);
            mission.Indicator(ReturnToCheung, 5, cheung.X, cheung.Y, cheung.Z, CheungRadius, CrucibleMissionDataV1.MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1538",
                "Native1 'Speak to Cirkin' 13877/13878, 2 'Kill Lamontix' 13880/13881, 3 'Return to Captain Cheung' 13882/13883, 4 " +
                "'Retrieve the Power Converter' 13964/13965, no counters; objectiveconversation 1538,1 package1742 text13943. Texts " +
                "13862-13866. Items class26438 keycard (111182), class26437 converter (111181), class26797 Eloh Resonator (117177). " +
                "Usable name 175 'Bane Lockbox'. Names 9643 Cirkin, 9641 Lamontix.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Not_My_Junk",
                "Giver/reward giver Awol Captain Cheung; requires Sweet Revenge or Making Amends; 85000XP, 0 credits, Eloh Resonator; " +
                "Lamontix and the crate around 339.1,-70.5; the converter in a nearby Bane bin; Bane Lockbox Keycard x/1; the keycard " +
                "gates the lockbox since the patch of 2008-01-31 (TaR-hist). TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleNotMyJunkV1",
                "Offered after 1205 or 1155 completed (scene requirement). Bound to the squad's Junkyard 2138. 1 Cirkin's talk opens 2; " +
                "2 the keycard (a personal 100% drop from Lamontix 770035 while 2 is open) opens 4; 4 a personal Bane lockbox 11116 " +
                "(name175, arg3, 81->82) at 343,263.8,-68 gives the converter and takes the keycard, opens 3; 3 the 10 m area round " +
                "Cheung. Cheung takes the converter. Markers: bridge road, Cirkin, Lamontix, the lockbox, Cheung.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own Bane Lockbox in the squad's Junkyard, opened once.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Cheung, played in the squad's Junkyard
                MapContextId = MapContextId,
                Requirement = AfterTheLedger(),
                ExistingFactObjectiveIds = new() { KillLamontix },
                Items = new()
                {
                    new MissionItemBinding(KeycardKey, KeycardTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { CrucibleStaalJunkyardNpcsV1.Lamontix }, KillLamontix, MapContextId,
                            KeycardChance, 1)),
                    new MissionItemBinding(ConverterKey, ConverterTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        TurnInQuantity: 1)
                },
                Actors =
                {
                    [LockboxRole] = new SceneActorDefinition(LockboxRole, SceneActorKind.Object, LockboxClass, LockboxAt,
                        Orientation: 1.571, InitialObjectState: ReadyState, NameOverrideId: LockboxName,
                        UseAction: new SceneObjectAction(MissionId, Retrieve, LockboxSequence, UseArgument))
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{LockboxRole}", LockboxRole) } };
            scene.Sequences[LockboxSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{LockboxRole}-opened", LockboxRole, TakenState),
                    new SetInteractionIntent($"{LockboxRole}-done", LockboxRole, false)
                },
                Character = { new IssueMissionItemIntent($"{LockboxRole}-converter", MissionId, ConverterKey, ConverterTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, Opened, 1) }
            };
            // Lamontix named, no sequence: back on his pool's own time in the squad's copy (SpawnPoolManager.ReturnsOnOwnTime).
            scene.Actors[LamontixRole] = new SceneActorDefinition(LamontixRole, SceneActorKind.PublicSpawn, CrucibleStaalJunkyardNpcsV1.Lamontix);
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
