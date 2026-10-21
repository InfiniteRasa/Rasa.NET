using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Symbolism (native mission 2017): General Bailey sends the player after the unknown Logos symbol on the
    /// conduits at Twin Pillars - the Hermit on Pools, Retread McCormick in the Marshes, the Logos "Earth" in the
    /// Enigma Caverns (Wilderness), then Archaeologist Topper at Twin Pillars.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 21925-21929 (missionconversation); objectives 1 "Question
    /// the Hermit (Valverde: Pools)" 21930/21931, 2 "Speak to Retread McCormick (Retread Camp: Marshes)"
    /// 21933/21934, 4 "Seek out the Logos Symbol (Enigma Caverns: Wilderness)" 21938/21939, 6 "Report to
    /// Archaeologist Topper (Twin Pillars: Wilderness)" 21943/21944, no counters (missionobjective); the talks
    /// (objectiveconversation): 1 Hermit 325 (21932), 2 McCormick 968 (21935: "look for a hole that leads to the
    /// bottom of Enigma Caverns"), 6 Topper 2595 (21945). The Logos is the server's shrine 408 "Earth" (class
    /// 30408) on the Wilderness (-828.5, 143, -738.2); the client map has the Earth door signs Defend, Yours,
    /// Planet at (-848, 145, -718).
    ///
    /// Sourced: TaRapedia Symbolism: giver General Thaddeus T. Bailey, Fort Defiance; reward from Archeologist
    /// Wynne Topper; requirement A Mystery Unearthed; 50000 XP, 0 credits; the walkthrough: the Hermit on Pools,
    /// McCormick on Marshes, "Get the Earth (logos) in Enigma Caverns", Topper at Twin Pillars; the door needs
    /// Defend, Yours and Planet.
    ///
    /// Reconstructed (ESTIMATE): level 50, as A Mystery Unearthed; the objectives one after another 1, 2, 4, 6;
    /// an Earth Logos the player already has counts. No markers: every objective is off the Plateau, and a
    /// marker carries no map (MissionInfo).
    /// </summary>
    public static class PlateauSymbolismV1
    {
        public const uint MissionId = 2017;
        public const uint Level = 50;
        public const uint Hermit = 1, McCormick = 2, Symbol = 4, Report = 6;
        public const uint Earth = 408;                  // logos "Earth", class 30408

        public const uint Experience = 50000;
        public const uint Credits = 0;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Symbolism", 21925, PlateauBaileyNpcsV1.Bailey,
                PlateauMysteryUnearthedV1.Topper, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Prerequisite(PlateauMysteryUnearthedV1.MissionId);

            mission.Objective(Hermit, 21930, 21931, 0, MissionObjectiveState.Incomplete);
            mission.Objective(McCormick, 21933, 21934, 1, MissionObjectiveState.Inactive);
            mission.Objective(Symbol, 21938, 21939, 2, MissionObjectiveState.Inactive);
            mission.Objective(Report, 21943, 21944, 3, MissionObjectiveState.Inactive);

            mission.Transition(Hermit);
            mission.Conversation(Hermit, PlateauBaileyNpcsV1.HermitPackage);
            Open(mission, Hermit, McCormick);

            mission.Transition(McCormick);
            mission.Conversation(McCormick, PlateauBaileyNpcsV1.McCormickPackage);
            Open(mission, McCormick, Symbol);

            mission.Transition(Symbol);
            mission.Progress(Symbol, MissionProgressEventKind.LogosAcquired, Earth, null);
            Open(mission, Symbol, Report);

            mission.Transition(Report);
            mission.Conversation(Report, PlateauMysteryUnearthedV1.TopperPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/2017",
                "Native1 Hermit 21930/21931, 2 McCormick 21933/21934, 4 Logos Symbol 21938/21939, 6 Topper 21943/21944, no " +
                "counters; objectiveconversation 2017: 1/325 21932, 2/968 21935, 6/2595 21945. Texts 21925-21929. Logos408 " +
                "'Earth' class30408 at -828.5,143,-738.2 (Wilderness); door signs Defend/Yours/Planet at -848,145,-718 (client map).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Symbolism",
                "Giver General Thaddeus T. Bailey, Fort Defiance; reward from Archeologist Wynne Topper; requirement A Mystery " +
                "Unearthed; 50000XP, 0 credits; Hermit on Pools, McCormick on Marshes, Earth logos in Enigma Caverns behind a " +
                "door needing Defend, Yours, Planet; Topper at Twin Pillars.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSymbolismV1",
                "Level50 as A Mystery Unearthed; order 1,2,4,6; an Earth Logos already owned counts; no markers (a marker has " +
                "no map). Hermit new, McCormick moved to the Marshes (PlateauBaileyNpcsV1).", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { Symbol } });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        private static void Open(PlateauMissionDataV1 mission, uint objective, uint next)
        {
            mission.Action(objective, 1, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, 2, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete);
        }
    }
}
