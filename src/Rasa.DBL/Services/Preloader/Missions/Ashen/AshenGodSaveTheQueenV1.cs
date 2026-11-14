using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// God Save the Queen (native mission 1537), Avernus Outpost (squad instance 2110): Captain Petrovich sends the player through
    /// the Atta Teleporter in the southern tunnel to the Queen's Chamber, where Colonel Vandeweghe leads him down to the Bane in the
    /// chamber; the room swept, the player tells Petrovich the queen is gone.
    ///
    /// Native: texts 13820-13824 (missionconversation): title "God Save the Queen", log "Petrovich is trusting you to enter the
    /// Queen's chamber and find out what's going on.", offer "Frogga says the teleporter is through this other tunnel. It's acting
    /// erratic, so we're hesitant to send more than one person through it ... See if you can get through it and find the Queen's
    /// chamber ...", turn-in "The Bane took the Atta Queen? Why? What the hell could they possibly want with her?", 13824 "none".
    /// Objectives (missionobjective) 1 "Find the Teleporter" 13825/13826, 2 "Teleport To the Queen's Chamber" 13835/13836, 3
    /// "Colonel Vandeweghe" 13837/13838, 4 "Follow Vandeweghe" 13840/13841, 5 "Talk to Vandeweghe" 13854/13855, 6 "Sweep the Room"
    /// 13857/13858, 7 "Talk To Vandeweghe" 13859/13860; no counters, no marker names. Objective conversations, package 1925
    /// (Vandeweghe) variant 1: 1537,3 13839 ("... I can hear laser fire coming from the bottom of this tunnel ... Why don't you
    /// join us?"), 1537,5 13856 ("Bane! ... I'm going to let the boys sweep up in here. You might even help them. Look for any sign
    /// of the queen ..."), 1537,7 13861 ("No sign of the queen ... get back through the teleporter and tell Petrovich and
    /// Tananda."). Name 9733 Colonel Vandeweghe.
    ///
    /// Sourced: TaRapedia God Save the Queen: giver and reward giver Captain Petrovich (Avernus Outpost); requires Assessing The Atta;
    /// follow-up The Big Secret; 90,000 XP, 6,150 credits; Class VIII Incendiary Grenade x3, Class VIII Advanced Med Pack x2, Class
    /// VIII EMP Grenade x3, Class VIII Res Trauma Kit; "Enter the southern tunnel and pass through the Atta teleporter there. On the
    /// other side you should meet Colonel Vandeweghe who will talk to you and escort you further into the Queen's Chamber. After a
    /// bit of walking, you'll stumble across some Bane troops and Vandeweghe will talk to you again. Finally, you must clear the
    /// chamber of remaining Bane ... Caretakers, Fithik, Altered Miasmas, and Thrax Tunnelers" (0.5). IGN boards (patch notes):
    /// "Added indicator for Avernus Outpost: God Save the Queen mission" (no id).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43. Requires 1536 (TaRapedia). Given and taken in at Petrovich's second row (790643) at the Food Storage.
    ///  - 1 the player at the southern tunnel's pad (AreaEntered, PadRadius round AshenAvernusHiveNpcsV1 917) opens 2; 2 the player
    ///    by Vandeweghe on the far side (AreaEntered, ColonelRadius round him; the pad's arrival 12 m off: an area is entered only by
    ///    a walk, MissionAreaService) opens 3.
    ///  - Vandeweghe's copy (OwnerOnly; he has no public pool, AshenAvernusHiveNpcsV1) stands at
    ///    his place from the accept, unhurt by anything. 3 the talk with him (13839) opens 4 and starts the lead: he goes down the
    ///    tunnel to the Queen's Chamber on the navmesh (QuestSceneIntents.Escort). 4 the player at the
    ///    tunnel's bottom (AreaEntered, BottomRadius round the lead's end) opens 5. Nothing waits for Vandeweghe: a stuck lead does
    ///    not hold the player.
    ///  - 5 the talk with him at the bottom (13856) opens 6; 6 the Bane's pool (AshenAvernusHiveNpcsV1 790658, upstream form: a
    ///    squad copy's pool) cleared (SpawnPoolCleared; a pool cleared before 6 opens counts at its opening while it is still down,
    ///    ExistingFactObjectiveIds). 6 starts Inactive: a squad mate's sweep before a member's 5 is not his (GroupCreditService
    ///    credits only an Incomplete objective). The pool comes back 45 min after the last of it died (the squad's copy,
    ///    SquadInstanceState), not after its 120 s: a sweep is never lost for good, but a pool cleared early waits that long.
    ///  - 7 the talk with him (13861) gives the reward and sends his copy away.
    ///  - The items by their client names, each a stack, one to choose: Class VIII Incendiary Grenade (111039) x3, Class VIII
    ///    Advanced Med Pack (45065) x2, Class VIII EMP Grenade (111029) x3, Class VIII Res Trauma Kit (47600) x1.
    ///  - Markers (unnamed; the client's 123 "Queen Chamber" is not tied to 1537 by any source): 1 the pad, 2 Vandeweghe's place,
    ///    3 Vandeweghe, 4 the bottom, 5 the bottom, 6 the Bane's pool, 7 the bottom. 5 and 7 stand at the lead's end: a marker does
    ///    not follow a scene actor (mission_indicator is a fixed place), so a Vandeweghe whose lead stopped short stands away from
    ///    them (docs/ashen-playtest.md).
    /// </summary>
    public static class AshenGodSaveTheQueenV1
    {
        public const uint MissionId = 1537;
        public const uint Level = 43;
        public const uint Outpost = AshenAvernusHiveNpcsV1.Outpost;
        public const uint Petrovich = AshenAvernusOutpostNpcsV1.PetrovichTop;
        public const uint Vandeweghe = AshenAvernusHiveNpcsV1.Vandeweghe, VandeweghePackage = AshenAvernusHiveNpcsV1.VandeweghePackage, Variant = 1;
        public const uint FindTheTeleporter = 1, TeleportToTheChamber = 2, Colonel = 3, FollowVandeweghe = 4, TalkToVandeweghe = 5, SweepTheRoom = 6,
            TalkAgain = 7;
        public const uint PadArea = 1, ColonelArea = 2, BottomArea = 3;
        public const double PadRadius = 8, ColonelRadius = 6, BottomRadius = 15, NpcMarkerRadius = 8, PoolMarkerPadding = 4;

        /// <summary>Scenarios: Vandeweghe leads down; Vandeweghe goes.</summary>
        public const uint LeadsDown = 1, Goes = 2;
        public const float RouteSpeed = 4f;
        public const string ColonelRole = "vandeweghe", DownRoute = "vandeweghe-down-the-tunnel";

        /// <summary>The bottom of the tunnel, at the Queen's Chamber's mouth: where the lead ends.</summary>
        public static readonly ScenePosition BottomAt = new(273f, 144.53f, -462f);

        /// <summary>Vandeweghe's way down the tunnel (the client-floor walk's corners on the navmesh's ground, ESTIMATE).</summary>
        public static readonly ScenePosition[] DownWay =
        {
            new(316f, 224.28f, -332f), new(282f, 220.24f, -342f), new(246f, 217.66f, -319f), new(211f, 220.08f, -318f),
            new(210f, 216.27f, -351f), new(206f, 216.85f, -383f), new(214f, 217.38f, -395f), new(208f, 215.09f, -411f),
            new(162f, 209.68f, -411f), new(150f, 201.23f, -379f), new(128f, 196.6f, -373f), new(118f, 184.96f, -398f),
            new(118f, 180.09f, -408f), new(144f, 168.67f, -418f), new(204f, 158.4f, -418f), new(264f, 151.33f, -418f),
            new(273f, 145.09f, -451f), BottomAt
        };

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;  // Class VIII Incendiary Grenade (class 26289)
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 2;       // Class VIII Advanced Med Pack (class 22548)
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;             // Class VIII Res Trauma Kit (class 23924)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "God Save the Queen", 13820, Petrovich, Petrovich, Level);
            mission.Prerequisite(AshenAssessingTheAttaV1.MissionId);
            mission.Reward(Experience, Credits,
                new[] { (IncendiaryGrenade, IncendiaryGrenades), (AdvancedMedPack, AdvancedMedPacks), (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits) },
                new (uint, uint)[0]);
            mission.Scenario(LeadsDown, "The talk: Vandeweghe leads down the tunnel", 0);
            mission.Scenario(Goes, "The last talk: Vandeweghe goes", 0);

            mission.Objective(FindTheTeleporter, 13825, 13826, 0, MissionObjectiveState.Incomplete);
            mission.Objective(TeleportToTheChamber, 13835, 13836, 1, MissionObjectiveState.Inactive);
            mission.Objective(Colonel, 13837, 13838, 2, MissionObjectiveState.Inactive);
            mission.Objective(FollowVandeweghe, 13840, 13841, 3, MissionObjectiveState.Inactive);
            mission.Objective(TalkToVandeweghe, 13854, 13855, 4, MissionObjectiveState.Inactive);
            mission.Objective(SweepTheRoom, 13857, 13858, 5, MissionObjectiveState.Inactive);
            mission.Objective(TalkAgain, 13859, 13860, 6, MissionObjectiveState.Inactive);

            var pad = AshenAvernusHiveNpcsV1.TunnelPadAt;
            var colonel = AshenAvernusHiveNpcsV1.VandewegheAt;
            var pool = AshenAvernusHiveNpcsV1.BaneAt;

            // 1: the player at the southern tunnel's pad.
            mission.Area(PadArea, Outpost, pad.X, pad.Y, pad.Z, PadRadius);
            mission.Transition(FindTheTeleporter);
            mission.EnterArea(FindTheTeleporter, PadArea);
            Open(mission, FindTheTeleporter, 1, TeleportToTheChamber);

            // 2: the player by Vandeweghe, past the far pad.
            mission.Area(ColonelArea, Outpost, colonel.X, colonel.Y, colonel.Z, ColonelRadius);
            mission.Transition(TeleportToTheChamber);
            mission.EnterArea(TeleportToTheChamber, ColonelArea);
            Open(mission, TeleportToTheChamber, 1, Colonel);

            // 3: the talk; Vandeweghe leads down.
            mission.Transition(Colonel);
            mission.Conversation(Colonel, VandeweghePackage, Variant);
            Open(mission, Colonel, 1, FollowVandeweghe);
            mission.Action(Colonel, 3, MissionActionKind.StartScenario, scenario: LeadsDown);

            // 4: the player at the bottom.
            mission.Area(BottomArea, Outpost, BottomAt.X, BottomAt.Y, BottomAt.Z, BottomRadius);
            mission.Transition(FollowVandeweghe);
            mission.EnterArea(FollowVandeweghe, BottomArea);
            Open(mission, FollowVandeweghe, 1, TalkToVandeweghe);

            // 5: "Bane!"
            mission.Transition(TalkToVandeweghe);
            mission.Conversation(TalkToVandeweghe, VandeweghePackage, Variant, 1);
            Open(mission, TalkToVandeweghe, 1, SweepTheRoom);

            // 6: the Bane's pool cleared in the squad's copy (one cleared before 6 opens counts at its opening).
            mission.Transition(SweepTheRoom);
            mission.Progress(SweepTheRoom, MissionProgressEventKind.SpawnPoolCleared, AshenAvernusHiveNpcsV1.BanePool, null);
            Open(mission, SweepTheRoom, 1, TalkAgain);

            // 7: "No sign of the queen"; Vandeweghe goes; the reward.
            mission.Transition(TalkAgain);
            mission.Conversation(TalkAgain, VandeweghePackage, Variant, 1);
            mission.Action(TalkAgain, 1, MissionActionKind.StartScenario, scenario: Goes);
            mission.Action(TalkAgain, 2, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(FindTheTeleporter, 1, pad.X, pad.Y, pad.Z, PadRadius, Outpost);
            mission.Indicator(TeleportToTheChamber, 2, colonel.X, colonel.Y, colonel.Z, ColonelRadius, Outpost);
            mission.Indicator(Colonel, 3, colonel.X, colonel.Y, colonel.Z, NpcMarkerRadius, Outpost);
            mission.Indicator(FollowVandeweghe, 4, BottomAt.X, BottomAt.Y, BottomAt.Z, BottomRadius, Outpost);
            mission.Indicator(TalkToVandeweghe, 5, BottomAt.X, BottomAt.Y, BottomAt.Z, NpcMarkerRadius, Outpost);
            mission.Indicator(SweepTheRoom, 6, pool.X, pool.Y, pool.Z, AshenAvernusHiveNpcsV1.BaneRadius + PoolMarkerPadding, Outpost);
            mission.Indicator(TalkAgain, 7, BottomAt.X, BottomAt.Y, BottomAt.Z, NpcMarkerRadius, Outpost);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1537",
                "Native1 'Find the Teleporter' 13825/13826, 2 'Teleport To the Queen's Chamber' 13835/13836, 3 'Colonel Vandeweghe' " +
                "13837/13838, 4 'Follow Vandeweghe' 13840/13841, 5 'Talk to Vandeweghe' 13854/13855, 6 'Sweep the Room' 13857/13858, " +
                "7 'Talk To Vandeweghe' 13859/13860; no counters, no indicator names. objectiveconversation 1537,3/5/7 pkg1925 13839, " +
                "13856, 13861. Texts 13820-13824. Name 9733 Vandeweghe.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/God_Save_the_Queen",
                "Giver/reward giver Captain Petrovich (Avernus Outpost); requires Assessing The Atta; follow-up The Big Secret; 90000XP, " +
                "6150 credits; Class VIII Incendiary Grenade x3, Class VIII Advanced Med Pack x2, Class VIII EMP Grenade x3, Class " +
                "VIII Res Trauma Kit; the southern tunnel's Atta teleporter, Vandeweghe escorts into the Queen's Chamber, Bane troops " +
                "(Caretakers, Fithik, Altered Miasmas, Thrax Tunnelers), back to Petrovich.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenGodSaveTheQueenV1",
                "Level43; requires 1536; Petrovich's second row 790643 gives and rewards. 1 the 8 m area at pad 917 (the southern " +
                "tunnel), pad 918 over the Queen's Chamber; 2 the 6 m area at Vandeweghe (349,233.11,-260); Vandeweghe's copy " +
                "(OwnerOnly, invulnerable; no public one); 3 his talk (pkg1925) and his navmesh lead down the tunnel; 4 the " +
                "15 m area at the bottom (273,144.53,-462); 5 his talk; 6 the Bane pool 790658 cleared (existing facts); 7 his talk, " +
                "he goes, the reward. Items 111039x3, 45065x2, 111029x3, 47600x1, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Vandeweghe's copy and his way down, in the squad's copy; the sweep's pool counted when it was cleared before 6 opens.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Outpost,    // taken at Petrovich in the squad's copy
                MapContextId = Outpost,
                ExistingFactObjectiveIds = new() { SweepTheRoom },
                Routes = new()
            };
            var at = AshenAvernusHiveNpcsV1.VandewegheAt;
            scene.Actors[ColonelRole] = new SceneActorDefinition(ColonelRole, SceneActorKind.Creature, Vandeweghe,
                new ScenePosition((float)at.X, (float)at.Y, (float)at.Z), GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true },
                OwnerOnly: true);
            scene.Routes[DownRoute] = new SceneRoute(DownRoute, DownWay.Select(point => new SceneWaypoint(point)).ToArray(), Speed: RouteSpeed);
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"{ColonelRole}-comes", ColonelRole) } };
            scene.Sequences[LeadsDown] = new SceneSequenceDefinition { World = { QuestSceneIntents.Escort("vandeweghe-leads-down", ColonelRole, DownRoute) } };
            scene.Sequences[Goes] = new SceneSequenceDefinition { World = { new RemoveActorIntent($"{ColonelRole}-goes", ColonelRole) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The objective's action <paramref name="action"/> reveals and opens <paramref name="next"/>.</summary>
        private static void Open(AshenMissionDataV1 mission, uint objective, uint action, uint next)
        {
            mission.Action(objective, action, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, action + 1, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete);
        }
    }
}
