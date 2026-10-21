using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Comic Relief (native mission 1251), Plateau: Corporal "Relay" O'Brien at Camp Resistance lost
    /// his "Atom Smash" comic book to Major Lulu Lonigan at poker. She trades it only for a carton of
    /// Hillsboro Lights, which Private "Scrounger" Scarpelo gets for the Thrax trophies of Scavenger
    /// Hunt (1015). NPCs: PlateauScroungerNpcsV1.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 10008-10012 (missionconversation); objectives
    /// 1 "Speak to Major Lonigan." 10015/10016, 7 "Trade with Major Lonigan." 10033/10034, 8 "Get
    /// Relay's comic back." 10013/10014, 9 "Get Lulu's Cigarettes." 10018/10019, 10 "Talk to
    /// 'Scrounger.'" 10020/10021, no counters (missionobjective); Lonigan's lines on package 999
    /// for objectives 1 (10017) and 7 (10035), Scrounger's on package 1000 for objective 10 (10022)
    /// (objectiveconversation, flag 1). The comic is item class 21885 (template 44879), the carton
    /// 21886 (44880).
    ///
    /// Sourced (TaRapedia Comic Relief): giver and receiver O'Brien, level 31, 50,000 XP, 4,950
    /// credits, "Equipment"; "This mission consists only of talking to the mission NPCs. However, to
    /// obtain the cigarettes you have to complete Scavenger Hunt." Scavenger Hunt's requirement is
    /// "Comic Relief (one objective)".
    ///
    /// Reconstructed (ESTIMATE): 8, 1 and 9 open; 1 opens 10, 9 opens 7. Talking to the Scrounger
    /// (objective 10) sets flag <see cref="ScroungerFlag"/>, which offers Scavenger Hunt - the "one
    /// objective". Objective 9 completes with Scavenger Hunt, whose fixed reward is the carton - also
    /// one completed before acceptance (the server counts history only for objectives open then, so
    /// 9 is open from the start; a held item counts only for an item that drops). At Lonigan (7) the carton is taken and the comic issued; 8
    /// completes when 7 does (ObjectiveState trigger); O'Brien keeps the comic at turn-in.
    /// "Equipment" read as the four Uncommon V04 vests of the 30-34 band, one of each armor type,
    /// no module. Indicators on Lonigan and the Scrounger.
    /// </summary>
    public static class PlateauComicReliefV1
    {
        public const uint MissionId = 1251;
        public const uint Level = 31;
        public const uint OBrien = PlateauScroungerNpcsV1.OBrien;
        public const uint Lonigan = PlateauScroungerNpcsV1.Lonigan;
        public const uint Scarpelo = PlateauScroungerNpcsV1.Scarpelo;
        public const uint ScroungerFlag = 1251001;
        /// <summary>Scavenger Hunt (PlateauScavengerHuntV1), whose reward is the carton.</summary>
        public const uint PlateauScavengerHuntMissionId = 1015;

        public const uint ComicTemplate = 44879, ComicClass = 21885;
        public const uint CartonTemplate = 44880, CartonClass = 21886;
        public const string ComicKey = "atom-smash-comic", CartonKey = "hillsboro-lights";

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint BioVest = 46035;              // Armor_T3_Bio_V04_UNC_Vest_30_to_34
        public const uint GravitonVest = 25337;         // Armor_T3_Graviton_V04_UNC_Vest_30_to_34
        public const uint MechVest = 34463;             // Armor_T3_Mech_V04_UNC_Vest_30_to_34
        public const uint StealthVest = 40591;          // Armor_T3_Stealth_V04_UNC_Vest_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Comic Relief", 10008, OBrien, OBrien, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonVest, MechVest, StealthVest });

            mission.Objective(8, 10013, 10014, 0, MissionObjectiveState.Incomplete);
            mission.Objective(1, 10015, 10016, 1, MissionObjectiveState.Incomplete);
            mission.Objective(10, 10020, 10021, 2, MissionObjectiveState.Inactive);
            mission.Objective(9, 10018, 10019, 3, MissionObjectiveState.Incomplete);
            mission.Objective(7, 10033, 10034, 4, MissionObjectiveState.Inactive);

            // 1: Lonigan names her price; on to the Scrounger.
            mission.Transition(1);
            mission.Conversation(1, PlateauScroungerNpcsV1.LoniganPackage);
            Open(mission, 1, 1, 10);

            // 10: the Scrounger asks for trophies - Scavenger Hunt is offered from here on.
            mission.Transition(10);
            mission.Conversation(10, PlateauScroungerNpcsV1.ScarpeloPackage);
            mission.Action(10, 1, MissionActionKind.SetPlayerFlag, playerFlag: ScroungerFlag, playerFlagValue: 1,
                comment: "The Scrounger offers Scavenger Hunt");

            // 9: the carton from the Scrounger - Scavenger Hunt completed; open from the start, so one completed
            // before acceptance counts too (from the mission history).
            mission.Transition(9);
            mission.Progress(9, MissionProgressEventKind.MissionCompleted, PlateauScavengerHuntMissionId, null);
            Open(mission, 9, 1, 7);

            // 7: Lonigan takes the carton and hands over the comic.
            mission.Transition(7);
            mission.Conversation(7, PlateauScroungerNpcsV1.LoniganPackage);
            mission.Action(7, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("lonigan-takes-cigarettes", MissionId, CartonKey, 1,
                    MissionItemScope.CharacterOwned));
            mission.Action(7, 2, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("lonigan-returns-comic", MissionId, ComicKey, ComicTemplate, 1));
            mission.Action(7, 3, MissionActionKind.GrantReward, reward: 1);

            // 8: the comic back - the trade at Lonigan completes it (a transition completes only its own objective).
            mission.Transition(8);
            mission.ObjectiveState(8, 7, MissionObjectiveState.Completed);

            mission.Indicator(1, 1, -618, 441.87, -237);       // Lonigan
            mission.Indicator(10, 2, -622, 442.37, -227);      // the Scrounger
            mission.Indicator(7, 3, -618, 441.87, -237);       // Lonigan

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1251",
                "Native1 Speak to Lonigan 10015/10016, 7 Trade 10033/10034, 8 Get the comic 10013/10014, 9 Cigarettes 10018/10019, " +
                "10 Talk to Scrounger 10020/10021, no counters; objectiveconversation 1251,1,999 / 1251,7,999 / 1251,10,1000 " +
                "texts 10017/10035/10022. Texts 10008-10012. Comic class21885/template44879, carton 21886/44880. Names 8615-8617.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Comic_Relief",
                "Giver/receiver Corporal 'Relay' O'Brien, Camp Resistance; level31; 50000XP, 4950 credits, 'Equipment'; talking " +
                "only, the cigarettes need Scavenger Hunt. NPC pages: O'Brien -621,441,-234; Lonigan female 33 (same /loc).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauComicReliefV1",
                "8, 1, 9 open; 1 opens 10, 9 opens 7. Objective10 sets flag1251001 (offers 1015). 9 = Scavenger Hunt completed (its reward is the carton). 7 consumes the carton, issues " +
                "the comic; 8 completes on 7 (ObjectiveState); O'Brien keeps the comic. Equipment read as Uncommon V04 vests 30-34: 46035, 25337, " +
                "34463, 40591. NPC looks and Lonigan's place are estimates (PlateauScroungerNpcsV1).", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 9 },
                Items = new()
                {
                    new MissionItemBinding(CartonKey, CartonTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain),
                    new MissionItemBinding(ComicKey, ComicTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            });
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PlateauMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
