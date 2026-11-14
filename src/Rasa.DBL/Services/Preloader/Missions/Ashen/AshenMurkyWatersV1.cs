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
    /// Murky Waters (native mission 1298), Ligo Ashen Desert: while Doctor Meeks works on the harvesters' processing units,
    /// Agent Hale of "High Command" asks the player to poison the harvesters instead of repairing them. The choice is for good
    /// and splits the White Oasis chain: Hale's poison (Neurotoxins 1300, then Poisoning the Wellwater 1302) or Meeks' repair
    /// (Water Purification 1299); set 12 builds both.
    ///
    /// Native: texts 10476-10479, 10480 "none" (missionconversation): log "Agent Hale wants urgently wants to speak with you
    /// concerning the water harvesters at Maji Field. He can be found at White Oasis Base.", offer "While I'm working on these
    /// processing units, there's someone from "High Command" here to see you. ... He goes by the name Hale, Agent Hale. He's
    /// waiting for you over there." (Meeks' voice), turn-in "It's been a pleasure."; objectives (missionobjective, no counters)
    /// 1 "Speak with Agent Hale" 10481/10482, 2 "Poison" 10487/10488, 3 "Repair" 10490/10491; objectiveconversation (1298,1)
    /// package 1478 variant 1 choice body 10483 ("It has come to my attention that you are actively working to repair the
    /// water harvesters. ..."), choice 1 10484 "I can't do that. I will not resort to your tactics.", choice 2 10485 "I agree
    /// with you. This might turn the tide for us.", choice 3 10486 (empty); (1298,2) 1478 variant 257 10489 "You are wise,
    /// indeed. I can appreciate someone that looks at the big picture."; (1298,3) 1478 variant 258 10492 "Well, that is your
    /// choice, of course. Good luck.". The variant of a conversation is the player flag the client reads for it: 257 after the
    /// poison (choice 2), 258 after the repair (choice 1); 1300 / 1302 read 257, 1299 258 (triage D3, research).
    ///
    /// Sourced: TaRapedia Murky Waters: Agent Hale gives and rewards (White Oasis); requires Analyzing the Harvesters; follow-up
    /// "Neurotoxins, Water Purification"; 50,000 XP, 4,300 credits; one of Class VIII Basic Med Pack x6, EMP Bomb, Frag Grenade
    /// x3, Cryogenic Grenade x3. TaRapedia Neurotoxins: "requires Murky Waters (Choice A)". Items by their client names: 22546
    /// Class VIII Basic Med Pack (template 45063), 26543 Class VIII EMP Bomb (111233), 22964 Class VIII Fragmentation Grenade
    /// (45447), 26299 Class VIII Cryogenic Grenade (111049).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage D3, the pattern of set 6's Formal Complaints):
    ///  - Giver Meeks (the client's offer is his voice; TaRapedia's infobox "Hale" is 0.3: its own lead-in "Murky Waters Lead In",
    ///    no client id, is not built), receiver Hale (the turn-in text). Level 41 (D5).
    ///  - 1 opens at the accept and completes at either choice at Hale. Choice 1 (the repair) sets flag 258 to 1 and 257 to 0
    ///    and opens 3; choice 2 (the poison) sets 257 to 1 and 258 to 0 and opens 2. 2 and 3 are Hale's answers with the
    ///    client's variant texts. 1, 2 and 3 are optional; the server-only hidden 4 (1's texts) is the one required objective,
    ///    an aggregate of 2 or 3 (count 1, as Formal Complaints' 4).
    ///  - The flags stay at an abandon (as Formal Complaints'); taken again, the choice is asked again and the last one stands.
    ///  - The four stacks one to choose. A marker at Hale for 1, 2 and 3.
    /// </summary>
    public static class AshenMurkyWatersV1
    {
        public const uint MissionId = 1298;
        public const uint Level = 41;
        public const uint Meeks = AshenWhiteOasisNpcsV1.Meeks, Hale = AshenWhiteOasisNpcsV1.Hale, HalePackage = AshenWhiteOasisNpcsV1.HalePackage;
        public const uint Speak = 1, Poison = 2, Repair = 3, Outcome = 4;
        /// <summary>The choices (the client's choice 1 and 2) and the transitions of 1 they take.</summary>
        public const uint RefuseHale = 1, AgreeWithHale = 2;
        /// <summary>The player flags (the client's conversation variants): 257 the poison, 258 the repair; value 1 when chosen.</summary>
        public const uint PoisonFlag = 257, RepairFlag = 258, Chosen = 1, NotChosen = 0;
        public const double MarkerRadius = 8;

        public const uint Experience = 50000;
        public const uint Credits = 4300;
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)
        public const uint EmpBomb = 111233, EmpBombs = 1;                       // Class VIII EMP Bomb (class 26543)
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Fragmentation Grenade (class 22964)
        public const uint CryogenicGrenade = 111049, CryogenicGrenades = 3;     // Class VIII Cryogenic Grenade (class 26299)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Murky Waters", 10476, Meeks, Hale, Level);
            mission.Reward(Experience, Credits,
                new[] { (BasicMedPack, BasicMedPacks), (EmpBomb, EmpBombs), (FragGrenade, FragGrenades), (CryogenicGrenade, CryogenicGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenAnalyzingHarvestersV1.MissionId);

            mission.Objective(Speak, 10481, 10482, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(Poison, 10487, 10488, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Repair, 10490, 10491, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Outcome, 10481, 10482, 3, MissionObjectiveState.Incomplete);    // hidden, server-only

            // 1: the choice at Hale.
            foreach (var (choice, own, other, next) in new[] { (RefuseHale, RepairFlag, PoisonFlag, Repair), (AgreeWithHale, PoisonFlag, RepairFlag, Poison) })
            {
                mission.Transition(Speak, choice, choice);
                mission.Conversation(Speak, HalePackage, 1, choice);
                mission.Action(Speak, 1, MissionActionKind.SetPlayerFlag, playerFlag: own, playerFlagValue: Chosen, transition: choice);
                mission.Action(Speak, 2, MissionActionKind.SetPlayerFlag, playerFlag: other, playerFlagValue: NotChosen, transition: choice);
                mission.Action(Speak, 3, MissionActionKind.RevealObjective, targetObjective: next, transition: choice);
                mission.Action(Speak, 4, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete,
                    transition: choice);
            }
            // 2 and 3: Hale's answer, in the variant the client reads for the flag.
            foreach (var (objective, flag) in new[] { (Poison, PoisonFlag), (Repair, RepairFlag) })
            {
                mission.Transition(objective);
                mission.Conversation(objective, HalePackage, flag);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
            }

            var hale = AshenWhiteOasisNpcsV1.HaleAt;
            foreach (var objective in new[] { Speak, Poison, Repair })
                mission.Indicator(objective, objective, hale.X, hale.Y, hale.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1298",
                "Native1 'Speak with Agent Hale' 10481/10482, 2 'Poison' 10487/10488, 3 'Repair' 10490/10491; no counters. " +
                "objectiveconversation 1298,1 pkg1478 choice body10483, 10484 refuse, 10485 agree, 10486 empty; 1298,2 1478 " +
                "variant257 10489; 1298,3 1478 variant258 10492. Texts 10476-10479, 10480 'none' (the offer in Meeks' voice). Names " +
                "9356 Meeks, 9357 Hale. Items 22546/45063, 26543/111233, 22964/45447, 26299/111049.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Murky_Waters",
                "Agent Hale gives and rewards (White Oasis); requires Analyzing the Harvesters; follow-ups Neurotoxins, Water " +
                "Purification; 50000XP, 4300 credits; Class VIII Basic Med Pack x6, EMP Bomb, Frag Grenade x3, Cryogenic Grenade x3. " +
                "Neurotoxins requires Murky Waters (Choice A).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenMurkyWatersV1",
                "Giver Meeks 790400 (the offer's voice), receiver Hale 790401 (new, White Oasis Post). Level41 estimated. 1 the " +
                "choice at Hale: 1 sets flag258 (257 to 0) and opens 3, 2 sets flag257 (258 to 0) and opens 2; hidden 4 required, " +
                "aggregate of 2 or 3. Flags kept at an abandon. Stacks 45063x6, 111233x1, 45447x3, 111049x3, one to choose. A " +
                "marker at Hale.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Hale's choice on 1; 4 the end of either answer.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Outcome },
                ObjectiveAggregations = new() { [Outcome] = new MissionObjectiveAggregation(new[] { Poison, Repair }, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Speak, HalePackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = RefuseHale, [2] = AgreeWithHale })
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
