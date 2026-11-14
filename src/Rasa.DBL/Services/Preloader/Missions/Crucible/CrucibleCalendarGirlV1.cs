using System.Collections.Generic;
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
    /// Calendar Girl (native mission 1144), Ligo Crucible: Lieutenant Sherer at Outpost Intrepid wants the AWOL Lieutenant
    /// Deirdre at Staal to sign his pin-up calendar. Deirdre asks the player to fake her death to Intelligence Officer
    /// D'Ambrose; the player helps her, rats her out, or threatens her. The end sets the flag that Sergeant Melching's text in
    /// Smoking Guns (1148, set 2) reads.
    ///
    /// Native: texts 8360-8363, 8364 (missionconversation): log "Lieutenant Sherer at Outpost Intrepid wants you to locate an
    /// AWOL female soldier named Lieutenant Deirdre at Staal and have her sign a pin-up calendar", turn-in "I promised you a
    /// reward and I always keep my word."; objectives (missionobjective, no counters) 1 "Find Awol Lt. Deirdre at Staal."
    /// 9160/9161, 8 "Give Deirdre the Calendar." 9172/9173, 5 "Speak to D'Ambrose." 9175/9176, 3 "Return to Deirdre."
    /// 9168/9169, 6 "Return to Lt. Sherer." 9181/9182 ("Return the unsigned calendar"), 4 "Return to Sherer." 9170/9171, 9
    /// "Comfort Lieutenant Sherer." 9211/9212, 7 "no desc" 9183/9184 (empty body). Objective conversations: (1144,1) package
    /// 1304 Deirdre choice body 9162 ("Take him my dog tags. Tell him you found them on my corpse"), choice 1 9163 "Agree to
    /// help Deirdre fake her own death.", choice 2 9164 "Threaten to turn Dierdre over to the AFS.", choice 3 empty; (1144,8)
    /// 1304 9174 ("I'll sign the damn thing. Just don't expect favors from any of the other Awols!"); (1144,5) 1301 D'Ambrose
    /// choice body 9177, choice 1 9178 "Tell him Deirdre is dead and give him her dog tags.", choice 2 9179 "Tell him Deirdre
    /// is alive and where he can find her."; (1144,3) 1304 9205 ("I'll spread the word among the Awols that you're okay");
    /// (1144,4) 1297 9206 (the signed calendar); (1144,6) 1297 choice body 9207, choice 1 9208 "Tell the truth, that you
    /// ratted her out to the AFS.", choice 2 9209 "Lie, and tell Sherer that she's dead."; (1144,9) 1297 9213 ("I can't
    /// believe she'd dead"). Melching's 1148,3 variants: 177 "the [..] that ratted out Deridre", 178 "the one that helped
    /// Deirdre", 203 "Who the hell are you?". Item class 24590 Pin-Up Girl Calendar (MisCrucibleItemPinupCalendar, template
    /// 50214); item class 24589 MisCrucibleItemDogtags (template 50213), Deirdre's dog tags.
    ///
    /// Sourced: TaRapedia Calendar Girl: giver Lieutenant Sherer (Outpost Intrepid, 969, 152, 108), D'Ambrose (975, 144, 110);
    /// 75,000 XP, 5,700 credits; one of ChiTech EMP Torqueshell Rifle, Teleract Electric Staff, Vextronics Endothermic
    /// Polarity Gun, Vextronics Incendiary Shotgun; the branches (help: D'Ambrose told she is dead, back to Deirdre, she
    /// signs, back to Sherer; rat out: D'Ambrose told where she is, the unsigned calendar back to Sherer; threaten: she signs,
    /// back to Sherer). TR MMO DB: level 40.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage decisions 1 and 7):
    ///  - Receiver Sherer. The calendar issued at the accept, taken by Sherer at 4 or 6. 1 and the hidden 7 open at the accept,
    ///    every other objective opens by the choice or talk before it: help 1 -> 5 (dead) -> 3 -> 4; rat out 1 -> 5 (alive) ->
    ///    6 -> truth (done) or lie -> 9; threaten 1 -> 8 -> 4. Objectives 1-6, 8 and 9 optional; 7 (the client's "no desc",
    ///    as Gun Control - Part III's hidden 4) is the only required one: it completes when 4 or 9 completes (an aggregate),
    ///    or at the truth (a scene step), so each end makes the mission completeable once.
    ///  - Flag <see cref="CrucibleSmokingGunsV1.CalendarGirlFlag"/> (1144001, set 2 reads it) set at the deed: 2 Helped at
    ///    D'Ambrose's "dead" (variant 178: "the one that helped Deirdre"), 1 RattedOut at his "alive" (177), 3 Threatened at
    ///    Deirdre's threat (Melching reads any other value as 203: "Who the hell are you?"; 9174 says the other Awols will not
    ///    do her favours, it does not say they know). Kept at an abandon; a new play sets it again.
    ///  - Deirdre's dog tags (class 24589, template 50213) issued at her "agree" (9162 "Take him my dog tags"); D'Ambrose takes
    ///    them at "dead" (9178 "give him her dog tags"). At "alive" the player keeps them until the mission ends (removed at
    ///    the turn-in, a failure or an abandon). A full bag refuses the "agree" talk (nothing changes; ask again).
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 40 ("Endothermic" read as Ice,
    ///    "Incendiary" as Fire): EMP Torqueshell Rifle 49596, Electric Staff 15189, Ice Polarity Gun 48712, Fire Shotgun
    ///    14861; one to choose. A marker at each NPC of each visible objective.
    /// </summary>
    public static class CrucibleCalendarGirlV1
    {
        public const uint MissionId = 1144;
        public const uint Level = 40;
        public const uint Sherer = CrucibleAwolCampNpcsV1.Sherer;
        public const uint FindDeirdre = 1, ReturnToDeirdre = 3, ReturnToSherer = 4, SpeakToDAmbrose = 5, UnsignedCalendar = 6,
            Outcome = 7, GiveCalendar = 8, ComfortSherer = 9;
        /// <summary>The choices (the client's choice 1 and 2) and the transition each takes.</summary>
        public const uint Agree = 1, Threaten = 2, Dead = 1, Alive = 2, Truth = 1, Lie = 2;
        /// <summary>The flag set 2 reads (CrucibleSmokingGunsV1): 1 ratted out, 2 helped, 3 threatened (Melching: neither).</summary>
        public const uint Flag = CrucibleSmokingGunsV1.CalendarGirlFlag, RattedOut = CrucibleSmokingGunsV1.RattedOut,
            Helped = CrucibleSmokingGunsV1.Helped, Threatened = 3;
        /// <summary>The truth told to Sherer: the scene step that completes 7.</summary>
        public const uint TruthTold = 11;
        public const uint CalendarTemplate = 50214, CalendarClass = 24590;
        public const string CalendarKey = "pin-up-girl-calendar";
        public const uint DogTagsTemplate = 50213, DogTagsClass = 24589;
        public const string DogTagsKey = "deirdres-dog-tags";
        public const double MarkerRadius = 8;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint EmpTorqueshellRifle = 49596;  // Weapon_Avatar_Torqueshell_Rifle_EMP_UNC_38_to_42
        public const uint ElectricStaff = 15189;        // Weapon_Avatar_Staff_Electric_UNC_40_to_44
        public const uint IcePolarityGun = 48712;       // Weapon_Avatar_PolarityGun_Ice_UNC_38_to_42
        public const uint FireShotgun = 14861;          // Weapon_Avatar_Shotgun_v1_Fire_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var deirdre = CrucibleAwolCampNpcsV1.DeirdrePackage;
            var dambrose = CrucibleAwolCampNpcsV1.DAmbrosePackage;
            var sherer = CrucibleAwolCampNpcsV1.ShererPackage;
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Calendar Girl", 8360, Sherer, Sherer, Level);
            mission.Reward(Experience, Credits, new[] { EmpTorqueshellRifle, ElectricStaff, IcePolarityGun, FireShotgun });
            mission.Scenario(TruthTold, "The truth told to Sherer: the mission ends", 0);

            mission.Objective(FindDeirdre, 9160, 9161, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(GiveCalendar, 9172, 9173, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(SpeakToDAmbrose, 9175, 9176, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(ReturnToDeirdre, 9168, 9169, 3, MissionObjectiveState.Inactive, required: false);
            mission.Objective(UnsignedCalendar, 9181, 9182, 4, MissionObjectiveState.Inactive, required: false);
            mission.Objective(ReturnToSherer, 9170, 9171, 5, MissionObjectiveState.Inactive, required: false);
            mission.Objective(ComfortSherer, 9211, 9212, 6, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Outcome, 9183, 9184, 7, MissionObjectiveState.Incomplete);

            // 1: Deirdre's choice: help her (to D'Ambrose) or threaten her (she signs).
            mission.Transition(FindDeirdre, Agree, Agree);
            mission.Conversation(FindDeirdre, deirdre, 1, Agree);
            Open(mission, FindDeirdre, Agree, 1, SpeakToDAmbrose);
            mission.Action(FindDeirdre, 3, MissionActionKind.IssueMissionItem, transition: Agree,
                itemIntent: new IssueMissionItemIntent("deirdre-gives-her-dog-tags", MissionId, DogTagsKey, DogTagsTemplate, 1));
            mission.Transition(FindDeirdre, Threaten, Threaten);
            mission.Conversation(FindDeirdre, deirdre, 1, Threaten);
            Open(mission, FindDeirdre, Threaten, 1, GiveCalendar);
            mission.Action(FindDeirdre, 3, MissionActionKind.SetPlayerFlag, playerFlag: Flag, playerFlagValue: Threatened, transition: Threaten);

            // 8: she signs under threat.
            mission.Transition(GiveCalendar);
            mission.Conversation(GiveCalendar, deirdre);
            Open(mission, GiveCalendar, 1, 1, ReturnToSherer);

            // 5: D'Ambrose's choice: she is dead (helped) or alive (ratted out).
            mission.Transition(SpeakToDAmbrose, Dead, Dead);
            mission.Conversation(SpeakToDAmbrose, dambrose, 1, Dead);
            Open(mission, SpeakToDAmbrose, Dead, 1, ReturnToDeirdre);
            mission.Action(SpeakToDAmbrose, 3, MissionActionKind.SetPlayerFlag, playerFlag: Flag, playerFlagValue: Helped, transition: Dead);
            mission.Action(SpeakToDAmbrose, 4, MissionActionKind.ConsumeMissionItem, transition: Dead,
                itemIntent: new ConsumeMissionItemIntent("dambrose-takes-the-dog-tags", MissionId, DogTagsKey, 1, MissionItemScope.AssignmentIssued));
            mission.Transition(SpeakToDAmbrose, Alive, Alive);
            mission.Conversation(SpeakToDAmbrose, dambrose, 1, Alive);
            Open(mission, SpeakToDAmbrose, Alive, 1, UnsignedCalendar);
            mission.Action(SpeakToDAmbrose, 3, MissionActionKind.SetPlayerFlag, playerFlag: Flag, playerFlagValue: RattedOut, transition: Alive);

            // 3: she is free and signs.
            mission.Transition(ReturnToDeirdre);
            mission.Conversation(ReturnToDeirdre, deirdre);
            Open(mission, ReturnToDeirdre, 1, 1, ReturnToSherer);

            // 4: the signed calendar to Sherer.
            mission.Transition(ReturnToSherer);
            mission.Conversation(ReturnToSherer, sherer);
            TakeCalendar(mission, ReturnToSherer, 1, 1);
            mission.Action(ReturnToSherer, 2, MissionActionKind.GrantReward, reward: 1);

            // 6: the unsigned calendar to Sherer: the truth ends it, the lie opens 9.
            mission.Transition(UnsignedCalendar, Truth, Truth);
            mission.Conversation(UnsignedCalendar, sherer, 1, Truth);
            TakeCalendar(mission, UnsignedCalendar, Truth, 1);
            mission.Action(UnsignedCalendar, 2, MissionActionKind.StartScenario, scenario: TruthTold, transition: Truth);
            mission.Action(UnsignedCalendar, 3, MissionActionKind.GrantReward, reward: 1, transition: Truth);
            mission.Transition(UnsignedCalendar, Lie, Lie);
            mission.Conversation(UnsignedCalendar, sherer, 1, Lie);
            TakeCalendar(mission, UnsignedCalendar, Lie, 1);
            Open(mission, UnsignedCalendar, Lie, 2, ComfortSherer);

            // 9: Sherer mourns her.
            mission.Transition(ComfortSherer);
            mission.Conversation(ComfortSherer, sherer);
            mission.Action(ComfortSherer, 1, MissionActionKind.GrantReward, reward: 1);

            var deirdreAt = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.Deirdre);
            var dambroseAt = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.DAmbrose);
            var shererAt = CrucibleAwolCampNpcsV1.At(Sherer);
            foreach (var (objective, at) in new[]
            {
                (FindDeirdre, deirdreAt), (GiveCalendar, deirdreAt), (SpeakToDAmbrose, dambroseAt), (ReturnToDeirdre, deirdreAt),
                (UnsignedCalendar, shererAt), (ReturnToSherer, shererAt), (ComfortSherer, shererAt)
            })
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1144",
                "Native1 'Find Awol Lt. Deirdre at Staal.' 9160/9161, 8 'Give Deirdre the Calendar.' 9172/9173, 5 'Speak to " +
                "D'Ambrose.' 9175/9176, 3 'Return to Deirdre.' 9168/9169, 6 'Return to Lt. Sherer.' 9181/9182, 4 'Return to Sherer.' " +
                "9170/9171, 9 'Comfort Lieutenant Sherer.' 9211/9212, 7 'no desc' 9183/9184; no counters. objectiveconversation " +
                "1144,1 pkg1304 choice body9162, 9163 agree to fake her death, 9164 threaten; 1144,8 1304 9174; 1144,5 pkg1301 " +
                "choice body9177, 9178 dead, 9179 alive; 1144,3 1304 9205; 1144,4 pkg1297 9206; 1144,6 1297 choice body9207, 9208 " +
                "truth, 9209 lie; 1144,9 1297 9213. Texts 8360-8364. Calendar class24590 (template50214), dog tags class24589 (template50213). 1148,3 variants 177 " +
                "ratted out, 178 helped, 203 neither.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Calendar_Girl",
                "Giver Lieutenant Sherer (Outpost Intrepid, 969, 152, 108), D'Ambrose (975, 144, 110); 75000XP, 5700 credits; " +
                "ChiTech EMP Torqueshell Rifle, Teleract Electric Staff, Vextronics Endothermic Polarity Gun, Vextronics Incendiary " +
                "Shotgun; branches: help (dead, back to Deirdre, back to Sherer), rat out (alive, unsigned calendar to Sherer), " +
                "threaten (she signs). TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleCalendarGirlV1",
                "Receiver Sherer; calendar at the accept, taken at 4 or 6. Choices open the next objective; 1-6, 8, 9 optional, " +
                "hidden 7 required (aggregate of 4 or 9, or the truth by a scene step). Flag1144001 at the deed: 2 helped " +
                "(D'Ambrose 'dead'), 1 ratted out ('alive'), 3 threatened. Dog tags issued at Deirdre's agree, taken at D'Ambrose's dead. Sherer, D'Ambrose, Deirdre moved " +
                "(CrucibleAwolCampNpcsV1). Items 49596, 15189, 48712, 14861, one to choose. A marker at each NPC.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The calendar at the accept, the dog tags at Deirdre's agree; the three choices; 7 the end of either signed branch or of the lie, or the truth.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Outcome },
                ObjectiveAggregations = new() { [Outcome] = new MissionObjectiveAggregation(new[] { ReturnToSherer, ComfortSherer }, 1) },
                Items = new()
                {
                    new MissionItemBinding(CalendarKey, CalendarTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(DogTagsKey, DogTagsTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-calendar", MissionId, CalendarKey, CalendarTemplate, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(FindDeirdre, CrucibleAwolCampNpcsV1.DeirdrePackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Agree, [2] = Threaten }),
                    new(SpeakToDAmbrose, CrucibleAwolCampNpcsV1.DAmbrosePackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Dead, [2] = Alive }),
                    new(UnsignedCalendar, CrucibleAwolCampNpcsV1.ShererPackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Truth, [2] = Lie })
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[TruthTold] = new SceneSequenceDefinition
            {
                Character = { new ObjectiveIntent("truth-told", MissionId, Outcome, MissionObjectiveState.Completed) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        private static void TakeCalendar(CrucibleMissionDataV1 mission, uint objective, uint transition, uint action) =>
            mission.Action(objective, action, MissionActionKind.ConsumeMissionItem, transition: transition,
                itemIntent: new ConsumeMissionItemIntent($"sherer-takes-the-calendar-{objective}-{transition}", MissionId, CalendarKey, 1,
                    MissionItemScope.AssignmentIssued));

        /// <summary>Reveals and activates <paramref name="next"/> by <paramref name="objective"/>'s transition, actions <paramref name="first"/> and the next.</summary>
        internal static void Open(CrucibleMissionDataV1 mission, uint objective, uint transition, uint first, uint next)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: next, transition: transition);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete, transition: transition);
        }
    }
}
