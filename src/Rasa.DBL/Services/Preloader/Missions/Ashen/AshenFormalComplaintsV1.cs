using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Formal Complaints (native mission 1254), Ligo Ashen Desert: Scavenger Vakiel Latan of the Labbna Syndicate in Ashoka
    /// Settlement says Captain Yamamoto threatens her Syndicate. The player takes her side (to Colonel Linier) or wants to hear
    /// Yamamoto first; the choice is for good and splits the zone: Latan's salvage chain (1264-1266, set 7) or Ito's AFS chain
    /// (The Human Perspective 1258, then 1259, 1261, 1285, 1267-1269, sets 6 and 8).
    ///
    /// Native: texts 10049-10052, 10053 "none" (missionconversation): log "Speak with Vakiel Latan of the Labbna Syndicate
    /// about the tense relationship between Captain Yamamoto and the Labbna Syndicate. You must choose between helping Vakiel
    /// Latan, or speaking with Captain Yamamoto ...", offer "One of your officers does not like Brann. Captain Yamamoto has
    /// threatened me and my Syndicate. ... Your AFS has been good to me and my Labbna", turn-in "I understand, and accept your
    /// answer."; objectives (missionobjective, no counters) 1 "Discuss the situation with Vakiel Latan" 10054/10055, 2 "Speak
    /// with Vakiel Latan" 10060/10061, 3 "Speak with Vakiel Latan" 10063/10064; objectiveconversation (1254,1) package 1449
    /// variant 1 choice body 10056 ("So Earther, what will you do? ... Do you support his racism?"), choice 1 10057 "His
    /// behavior is unacceptable. I will speak with Colonel Linier for you.", choice 2 10058 "I'm not sure if I believe your
    /// story. I think I will speak with Captain Yamamoto first.", choice 3 10059 (empty); (1254,2) 1449 variant 250 10062 "You
    /// do credit to your species. Me and all Labbna thank you." (and its reminder 10066, the same words); (1254,3) 1449 variant
    /// 251 10065 "I understand. You are loyal to your own, blindly so. If you change your mind, I will be here." (reminder
    /// 10067, the same). The variant of a conversation is the player flag the client reads for it: 250 after choice 1, 251
    /// after choice 2; the client's other missions read the same two (1255-1257 variant 250; 1258, 1259, 1261 variant 251).
    ///
    /// Sourced: TaRapedia Formal Complaints: 45,000 XP, 4,100 credits; one of Class VIII EMP Grenade x3, Res Trauma Kit,
    /// Concussion Grenade x3, Basic Med Pack x6; two options; follow-up The Human Perspective "(If Option 2)"; the infobox names
    /// Captain Amandla Botha as giver and reward giver (0.3: the text is Latan's, and 1258 says "your conversation with
    /// Latan"). TaRapedia Targets of Opportunity (1580): "Only one of Scavenger or Logistician is available depending on which
    /// side you take in Formal Complaints". Items by their client names: 26279 Class VIII EMP Grenade (template 111029), 23924
    /// Res Trauma Kit (47600), 26269 Concussion Grenade (111019), 22546 Basic Med Pack (45063).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage D1):
    ///  - Giver and receiver Latan (the client's voice). Level 40 (D5).
    ///  - 1 opens at the accept and completes at either choice. Choice 1 sets flag 250 to 1 and flag 251 to 0 and opens 2;
    ///    choice 2 sets 251 to 1 and 250 to 0 and opens 3 (the two flags never both set: the follow-ups read one each). 2 and 3
    ///    are Latan's answers with the client's variant texts. 1, 2 and 3 are optional; the server-only hidden 4 (1's texts, the
    ///    client has no 4) is the one required objective, an aggregate of 2 or 3 (count 1, as Calendar Girl's 7), so either
    ///    answer makes the mission completeable once.
    ///  - The flags stay at an abandon (as Calendar Girl's); taken again, the choice is asked again and the last one stands.
    ///    The client's reminder texts (10066, 10067) are not sent (no set has a reminder topic).
    ///  - The four stacks one to choose. A marker at Latan for 1, 2 and 3.
    /// </summary>
    public static class AshenFormalComplaintsV1
    {
        public const uint MissionId = 1254;
        public const uint Level = 40;
        public const uint Latan = AshenFormalComplaintsNpcsV1.Latan, LatanPackage = AshenFormalComplaintsNpcsV1.LatanPackage;
        public const uint Discuss = 1, LatanSide = 2, YamamotoSide = 3, Outcome = 4;
        /// <summary>The choices (the client's choice 1 and 2) and the transitions of 1 they take.</summary>
        public const uint SideWithLatan = 1, HearYamamoto = 2;
        /// <summary>The player flags (the client's conversation variants): 250 Latan's side, 251 Yamamoto's side; value 1 when chosen.</summary>
        public const uint LatanFlag = 250, YamamotoFlag = 251, Chosen = 1, NotChosen = 0;
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Class VIII Res Trauma Kit (class 23924)
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade (class 26269)
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Formal Complaints", 10049, Latan, Latan, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);

            mission.Objective(Discuss, 10054, 10055, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(LatanSide, 10060, 10061, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(YamamotoSide, 10063, 10064, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Outcome, 10054, 10055, 3, MissionObjectiveState.Incomplete);    // hidden, server-only

            // 1: the choice.
            foreach (var (choice, own, other, next) in new[] { (SideWithLatan, LatanFlag, YamamotoFlag, LatanSide), (HearYamamoto, YamamotoFlag, LatanFlag, YamamotoSide) })
            {
                mission.Transition(Discuss, choice, choice);
                mission.Conversation(Discuss, LatanPackage, 1, choice);
                mission.Action(Discuss, 1, MissionActionKind.SetPlayerFlag, playerFlag: own, playerFlagValue: Chosen, transition: choice);
                mission.Action(Discuss, 2, MissionActionKind.SetPlayerFlag, playerFlag: other, playerFlagValue: NotChosen, transition: choice);
                mission.Action(Discuss, 3, MissionActionKind.RevealObjective, targetObjective: next, transition: choice);
                mission.Action(Discuss, 4, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete,
                    transition: choice);
            }
            // 2 and 3: Latan's answer, in the variant the client reads for the flag.
            foreach (var (objective, flag) in new[] { (LatanSide, LatanFlag), (YamamotoSide, YamamotoFlag) })
            {
                mission.Transition(objective);
                mission.Conversation(objective, LatanPackage, flag);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
            }

            var latan = AshenFormalComplaintsNpcsV1.At(Latan);
            foreach (var objective in new[] { Discuss, LatanSide, YamamotoSide })
                mission.Indicator(objective, objective, latan.X, latan.Y, latan.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1254",
                "Native1 'Discuss the situation with Vakiel Latan' 10054/10055, 2 'Speak with Vakiel Latan' 10060/10061, 3 the same " +
                "10063/10064; no counters. objectiveconversation 1254,1 pkg1449 choice body10056, 10057 Colonel Linier, 10058 " +
                "Yamamoto first, 10059 empty; 1254,2 1449 variant250 10062 (reminder 10066); 1254,3 1449 variant251 10065 (reminder " +
                "10067). Texts 10049-10052, 10053 'none'. Name 9277 Latan. Items 26279/111029, 23924/47600, 26269/111019, 22546/45063.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Formal_Complaints",
                "45000XP, 4100 credits; Class VIII EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3, Basic Med Pack x6; two " +
                "options; follow-up The Human Perspective (if option 2); infobox giver Botha (the text is Latan's). 1580: Scavenger " +
                "or Logistician by the side taken here.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenFormalComplaintsV1",
                "Giver and receiver Latan 790200 (new, lower Ashoka). Level40 estimated. 1 the choice: 1 sets flag250 (251 to 0) and " +
                "opens 2, 2 sets flag251 (250 to 0) and opens 3; hidden 4 required, aggregate of 2 or 3. Flags kept at an abandon. " +
                "Stacks 111029x3, 47600x1, 111019x3, 45063x6, one to choose. A marker at Latan.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Latan's choice on 1; 4 the end of either answer.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Outcome },
                ObjectiveAggregations = new() { [Outcome] = new MissionObjectiveAggregation(new[] { LatanSide, YamamotoSide }, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Discuss, LatanPackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = SideWithLatan, [2] = HearYamamoto })
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
