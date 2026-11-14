using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Other Side Of The Coin (native mission 1153), Ligo Crucible: at the Awol Camp, Sergeant Breiburg asks the player not
    /// to give the camp away to D'Ambrose; the player tells Captain Cheung the decision. The promise fails Stray Cats (1152)
    /// and leads to Sweet Revenge (1205); the refusal fails this mission.
    ///
    /// Native: texts 8439-8442, 8443 (missionconversation): log "Captain Chueng, the leader of the Awol Camp, wants you to
    /// disobey your orders from Intelligence Officer D'Ambrose and keep the location of the camp a secret. If you do, your
    /// mission for D'Ambrose will fail.", offer "I know what you're here for. You're going to rat us out, aren't you? ... just
    /// don't give them our location. Will you do that much for us?", turn-in "You made the right choice. ... You'll always be
    /// welcome in our camp."; objective 1 "Inform Cheung of your decision." 9112/9113 ("Will you help protect the Awols or
    /// report them? Inform Captain Cheung of your decision."), no counter; objectiveconversation (1153,1) package 1292 Cheung
    /// choice body 9114 ("Will you keep our location a secret?"), choice 1 9115 "Promise to keep their location a secret.",
    /// choice 2 9116 "Refuse to keep their location a secret.", choice 3 empty.
    ///
    /// Sourced: TaRapedia The Other Side Of The Coin: giver Awol Sergeant Breiburg (AWOL Camp); requires Stray Cats "(in
    /// mission log)"; 75,000 XP, 5,700 credits; one of Vextronics Electric Blade, Electric Injector Gun, Exothermic Polarity
    /// Gun, Incendiary RPG Launcher; option 1 (keep the secret): debriefing by Breiburg, the mission completes, Stray Cats
    /// fails, follow-up Sweet Revenge; option 2 (refuse): "You immediately fail this mission. You [can] get this mission again
    /// and choose Option 1."; "Inform Cheung of your decision" goes to Awol Captain Cheung. The patch notes' known issue
    /// (refusing completes this mission and fails Stray Cats, "This should be the opposite") is a retail bug: not built (triage
    /// decision 1). TR MMO DB: level 40.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage decision 1):
    ///  - Receiver Breiburg (TaRapedia's debriefing; the client names no receiver). Offered while Stray Cats is Active in the
    ///    mission log (a MissionAccepted prerequisite), so not once Stray Cats is done.
    ///  - Cheung's choice 1 completes 1 and signals Stray Cats, whose open objectives then fail (CrucibleStrayCatsV1, as 893 /
    ///    1078); choice 2 fails 1 and the mission with it. A failed one cleared, Breiburg offers it again while Stray Cats is
    ///    still Active. Stray Cats' report to D'Ambrose fails an open 1 the same way (the camp betrayed).
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 40 ("Exothermic" read as Fire,
    ///    "Incendiary RPG Launcher" as the physical rocket launcher: no fire one exists, as 1203): Electric Blade 15475,
    ///    Electric Injector Gun 15588, Fire Polarity Gun 48680, Rocket Launcher 49140; one to choose. A marker at Cheung.
    /// </summary>
    public static class CrucibleOtherSideOfTheCoinV1
    {
        public const uint MissionId = 1153;
        public const uint Level = 40;
        public const uint Breiburg = CrucibleAwolCampNpcsV1.Breiburg;
        public const uint Decision = 1;
        /// <summary>Cheung's choices (the client's choice 1 and 2), each its own transition of 1; 3 the signal of Stray Cats' report.</summary>
        public const uint Promise = 1, Refuse = 2, Betrayed = 3;
        public const double MarkerRadius = 8;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint ElectricBlade = 15475;        // Weapon_Avatar_Blade_Electric_UNC_36_to_40
        public const uint ElectricInjectorGun = 15588;  // Weapon_Avatar_InjectionGun_Electric_UNC_37_to_41
        public const uint FirePolarityGun = 48680;      // Weapon_Avatar_PolarityGun_Fire_UNC_38_to_42
        public const uint RocketLauncher = 49140;       // Weapon_Avatar_RocketLauncher_Physical_UNC_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var cheung = CrucibleAwolCampNpcsV1.CheungPackage;
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Other Side Of The Coin", 8439, Breiburg, Breiburg, Level);
            mission.Reward(Experience, Credits, new[] { ElectricBlade, ElectricInjectorGun, FirePolarityGun, RocketLauncher });
            mission.ActivePrerequisite(CrucibleStrayCatsV1.MissionId);

            mission.Objective(Decision, 9112, 9113, 0, MissionObjectiveState.Incomplete);

            // Choice 1: the secret kept. Stray Cats closes.
            mission.Transition(Decision, Promise, Promise);
            mission.Conversation(Decision, cheung, 1, Promise);
            mission.Action(Decision, 1, MissionActionKind.GrantReward, reward: 1, transition: Promise);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Decision, cheung, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = Promise, [2] = Refuse })
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            CrucibleStrayCatsV1.CloseTheOther(mission, scene, Decision, 2, CrucibleStrayCatsV1.MissionId, Promise);

            // Choice 2: the secret refused: the mission fails.
            mission.Transition(Decision, Refuse, Refuse, MissionObjectiveState.Failed);
            mission.Conversation(Decision, cheung, 1, Refuse);

            // Stray Cats' report gives the camp away: an open decision fails.
            CrucibleStrayCatsV1.ClosedByTheOther(mission, "Stray Cats", (Decision, Betrayed));

            var at = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.Cheung);
            mission.Indicator(Decision, 1, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1153",
                "Native1 'Inform Cheung of your decision.' 9112/9113, no counter; objectiveconversation 1153,1 pkg1292 choice " +
                "body9114, 9115 promise to keep the secret, 9116 refuse, choice 3 empty. Texts 8439-8443 (log: keeping the secret " +
                "fails the mission for D'Ambrose).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Other_Side_Of_The_Coin",
                "Giver Awol Sergeant Breiburg (AWOL Camp); requires Stray Cats (in mission log); 75000XP, 5700 credits; Vextronics " +
                "Electric Blade, Electric Injector Gun, Exothermic Polarity Gun, Incendiary RPG Launcher; option 1 completes it and " +
                "fails Stray Cats (debriefing by Breiburg, then Sweet Revenge); option 2 fails it at once, it can be taken again. " +
                "Patch-note known issue (the opposite) not built. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleOtherSideOfTheCoinV1",
                "Receiver Breiburg; offered while 1152 is Active. Cheung's choice 1 completes 1 and signals 1152 (its open " +
                "objectives fail); choice 2 fails 1. 1152's report fails an open 1. Cheung moved to the Awol Camp. Items 15475, " +
                "15588, 48680, 49140, one to choose. A marker at Cheung.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
