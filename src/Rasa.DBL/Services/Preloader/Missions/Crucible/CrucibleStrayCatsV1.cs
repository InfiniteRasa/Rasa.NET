using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Stray Cats (native mission 1152), Ligo Crucible: Intelligence Officer D'Ambrose at Outpost Intrepid sends the player to
    /// find the AWOL soldiers' camp west of Staal and offer their leader the AFS's terms; the report gives the camp away. The
    /// fork of the AWOL line: Stray Cats done (the camp betrayed) leads to Making Amends (1155); The Other Side Of The Coin
    /// (1153, the camp's secret kept) fails it and leads to Sweet Revenge (1205). One of the two per player.
    ///
    /// Native: texts 8434-8437, 8438 (missionconversation): log "A group of AWOL soldiers from Fort Intrepid have reportedly
    /// formed their own community somewhere to the west of Staal. Intelligence Officer D'Ambrose wants you to infiltrate their
    /// ranks and offer them freedom from prosecution", turn-in "So, they're not having any, eh? ... Rules are rules, and
    /// desertion is desertion. Well done, soldier."; objectives (missionobjective, no counters) 1 "Locate the leader of the
    /// Awols." 9106/9107 ("Find the Awol encampment in the hills to the west of Staal and question their leader."), 2 "Report
    /// back to D'Ambrose." 9109/9110; objectiveconversation (1152,1) package 1292 Cheung 9108 ("A deal? Come back to the AFS
    /// and get [..] on?"), (1152,2) 1301 D'Ambrose 9111 ("You located the Awol camp? Did you give them our terms?"). The
    /// client's 1153 log 8440: "keep the location of the camp a secret. If you do, your mission for D'Ambrose will fail."
    ///
    /// Sourced: TaRapedia Stray Cats: giver Intelligence Officer D'Ambrose (Outpost Intrepid); requires Calendar Girl; 75,000
    /// XP, 5,700 credits; one of Vextronics Torqueshell Rifle, Vitalius Electric Polarity Gun, Vextronics Pulse Cannon,
    /// Vextronics Laser Rifle; option 1 of The Other Side Of The Coin fails this mission. TaRapedia updates 2008-01-28 and the
    /// Exploration Guide 1.4: a Stray Cats failed by The Other Side Of The Coin "can no longer be regained". The known issue of
    /// the patch notes (2008-04 to 2008-05: "Refusing to help the AWOLs completes the Other Side of the Coin mission, and fails
    /// the Stray Cats mission. This should be the opposite.") is a retail bug: not built (triage decision 1). TR MMO DB: level
    /// 40.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage decision 1):
    ///  - Receiver D'Ambrose. 1 opens 2. D'Ambrose's report talk (2) gives the camp away: its scene signals 1153, whose open
    ///    objective then fails (the camp is betrayed; a signal to a 1153 not held changes nothing), as Needle in a Bane Camp
    ///    (893) closes The Choices We Make (1078). 1153's promise signals this mission the same way: its open objectives fail.
    ///    An unconditional FailRelatedMission is not used: it throws when the other mission is not held.
    ///  - Not offered and not taken once 1153 is done (its promise made: Success, or turned in): the scene's requirement.
    ///    The requirement gates the offer and the accept only (MissionApplication admission), not objective progress: that
    ///    is why 1153's promise still fails a 1152 in the log. Do not move the requirement onto the objectives: that would
    ///    stop the fork's fail transitions. A Success 1153 cannot be abandoned or cleared, so the gate holds for good.
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 40 ("Torqueshell Rifle" of no kind read as
    ///    physical, "Pulse Cannon" as the EMP rocket launcher, as the Marshes'): Torqueshell Rifle 45796, Electric Polarity Gun
    ///    15550, EMP Rocket Launcher 15442, Laser Rifle 14699; one to choose. A marker at Cheung and at D'Ambrose.
    /// </summary>
    public static class CrucibleStrayCatsV1
    {
        public const uint MissionId = 1152;
        public const uint Level = 40;
        public const uint DAmbrose = CrucibleAwolCampNpcsV1.DAmbrose;
        public const uint LocateLeader = 1, Report = 2;
        public const double MarkerRadius = 8;

        /// <summary>
        /// The AWOL fork (as MarshesNeedleInABaneCampV2): the other path's signal arrives as scenario ClosedByOther; this path's
        /// last talk starts scenario CloseOther, whose scene sequence signals the other mission's ClosedByOther.
        /// </summary>
        public const uint ClosedByOther = 9, CloseOther = 10;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint TorqueshellRifle = 45796;     // Weapon_Avatar_Torqueshell_Rifle_Physical_UNC_38_to_42
        public const uint ElectricPolarityGun = 15550;  // Weapon_Avatar_PolarityGun_Electric_UNC_38_to_42
        public const uint PulseCannon = 15442;          // Weapon_Avatar_RocketLauncher_EMP_UNC_39_to_43
        public const uint LaserRifle = 14699;           // Weapon_Avatar_Rifle_Laser_UNC_36_to_40

        /// <summary>Not offered and not taken once The Other Side Of The Coin is done (Success or Completed); gates admission only, not objective progress.</summary>
        public static MissionRequirement CampSecretNotKept() =>
            new NotRequirement(new MissionStateRequirement(CrucibleOtherSideOfTheCoinV1.MissionId));

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Stray Cats", 8434, DAmbrose, DAmbrose, Level);
            mission.Reward(Experience, Credits, new[] { TorqueshellRifle, ElectricPolarityGun, PulseCannon, LaserRifle });
            mission.Prerequisite(CrucibleCalendarGirlV1.MissionId);

            mission.Objective(LocateLeader, 9106, 9107, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Report, 9109, 9110, 1, MissionObjectiveState.Inactive);

            // 1: Cheung hears the terms and refuses them.
            mission.Transition(LocateLeader);
            mission.Conversation(LocateLeader, CrucibleAwolCampNpcsV1.CheungPackage);
            CrucibleCalendarGirlV1.Open(mission, LocateLeader, 1, 1, Report);

            // 2: the report to D'Ambrose gives the camp away: The Other Side Of The Coin closes.
            mission.Transition(Report);
            mission.Conversation(Report, CrucibleAwolCampNpcsV1.DAmbrosePackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Requirement = CampSecretNotKept()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            CloseTheOther(mission, scene, Report, 2, CrucibleOtherSideOfTheCoinV1.MissionId);
            ClosedByTheOther(mission, "The Other Side Of The Coin", (LocateLeader, 2), (Report, 2));

            var cheung = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.Cheung);
            var dambrose = CrucibleAwolCampNpcsV1.At(DAmbrose);
            mission.Indicator(LocateLeader, 1, cheung.X, cheung.Y, cheung.Z, MarkerRadius);
            mission.Indicator(Report, 2, dambrose.X, dambrose.Y, dambrose.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1152",
                "Native1 'Locate the leader of the Awols.' 9106/9107, 2 'Report back to D'Ambrose.' 9109/9110, no counters; " +
                "objectiveconversation 1152,1 pkg1292 9108 (Cheung refuses), 1152,2 pkg1301 9111. Texts 8434-8438. 1153 log 8440: " +
                "keeping the camp secret fails the mission for D'Ambrose.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Stray_Cats",
                "Giver Intelligence Officer D'Ambrose (Outpost Intrepid); requires Calendar Girl; 75000XP, 5700 credits; Vextronics " +
                "Torqueshell Rifle, Vitalius Electric Polarity Gun, Vextronics Pulse Cannon, Vextronics Laser Rifle; option 1 of " +
                "1153 fails it; once failed it cannot be regained (TaRapedia 2008-01-28, EG 1.4). Patch-note known issue (refuse " +
                "fails 1152) not built. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleStrayCatsV1",
                "Receiver D'Ambrose; 1 opens 2. The report talk signals 1153 (its open objective fails, as 893/1078); 1153's " +
                "promise fails this one the same way. Not offered or taken once 1153 is done. Cheung moved to the Awol Camp " +
                "(CrucibleAwolCampNpcsV1). Items 45796, 15550, 15442, 14699, one to choose. Markers at Cheung and D'Ambrose.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        /// <summary>The other path's signal fails these objectives (each by its own transition) while they are Incomplete.</summary>
        internal static void ClosedByTheOther(CrucibleMissionDataV1 mission, string other, params (uint Objective, uint Transition)[] objectives)
        {
            mission.Scenario(ClosedByOther, $"{other} done: this path closes", 1);
            foreach (var (objective, transition) in objectives)
            {
                mission.Transition(objective, transition, transition, MissionObjectiveState.Failed);
                mission.Progress(objective, transition, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ClosedByOther);
            }
        }

        /// <summary>This path's last talk closes the other: scenario CloseOther, its sequence's signal to <paramref name="other"/>.</summary>
        internal static void CloseTheOther(CrucibleMissionDataV1 mission, MissionSceneDefinition scene, uint objective, uint action,
            uint other, uint transition = 1)
        {
            mission.Scenario(CloseOther, $"Done: mission {other} closes", 0);
            mission.Action(objective, action, MissionActionKind.StartScenario, scenario: CloseOther, transition: transition);
            scene.Sequences[CloseOther] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(other, ClosedByOther, 1) } };
        }
    }
}
