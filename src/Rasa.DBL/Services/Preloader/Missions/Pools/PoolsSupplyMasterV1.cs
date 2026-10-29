using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Supply Master (native mission 1322), Valverde Pools: Supply Chief Riggins sends the player to "interrogate"
    /// Nakrinx, the Bane Supply Master, on his rounds from the Thrax Barracks, once his bodyguards are dead.
    ///
    /// Native: texts 10768-10772 (missionconversation): log "He can be found traveling between the Thrax Barracks and
    /// Lamna Armory.", offer "The best chance you'll have is to eliminate his bodyguards and interrogate him in the field",
    /// turn-in "I'm going to have to get this information to The Snake."; objectives 1 "Interrogate Nakrinx" 10773/10774
    /// ("You'll have to kill his bodyguards to get him to talk."), 2 "Return to Supply Chief Riggins" 10776/10777, no
    /// counters (missionobjective); talk 1322,1 on package 1522 (Nakrinx, text 10775).
    ///
    /// Sourced: TaRapedia The Supply Master: giver and receiver Supply Chief Riggins (The Snakepit); requires Death Patrol;
    /// follow-up Eastern Listening Post; 60,000 XP, 5,250 credits; one of Modification: Steal Adrenaline, Steal Armor, Steal
    /// Health, Steal Power; walkthrough: kill the four Thrax that patrol with him, then a mission indicator pops above his
    /// head. TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE): Nakrinx and his guards are public (PoolsSupplyNpcsV1). Objective 1 shows from the
    /// start, not yet active (NotAssigned). A hidden server-only objective 3 counts four kills of his bodyguards (any
    /// four, the player's kill credit) and then activates 1: the talk is offered only then (the client gives the
    /// bodyguards no objective of their own). Nothing of 3 shows, as in the walkthrough: the client names an objective
    /// and its counters only from its own rows, and 1322 has none for 3 (a visible 3 read "Missing translation for
    /// missionobjective ID 1322/3/0" and "... 1322/3", to-fix Pools #6, #7). 1 opens 2, a guide to Riggins with
    /// no line (not required: the turn-in at Riggins ends the mission, as Bane Investigation's 3). "Modification" read as
    /// the grade [4] Weapon Modules (precedent Bane Investigation), one to choose. Markers on the beat's start and on Riggins.
    /// </summary>
    public static class PoolsSupplyMasterV1
    {
        public const uint MissionId = 1322;
        public const uint Level = 33;
        public const uint Riggins = PoolsSnakePitNpcsV1.Riggins;
        public const uint Interrogate = 1, Return = 2;
        public const uint Bodyguards = 3;               // hidden: four bodyguard kills

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint StealAdrenaline = 123261;     // Weapon Module: Steal Adrenaline [4]
        public const uint StealArmor = 123266;          // Weapon Module: Steal Armor [4]
        public const uint StealHealth = 123271;         // Weapon Module: Steal Health [4]
        public const uint StealPower = 123276;          // Weapon Module: Steal Power [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "The Supply Master", 10768, Riggins, Riggins, Level);
            mission.Reward(Experience, Credits, new[] { StealAdrenaline, StealArmor, StealHealth, StealPower });
            mission.Prerequisite(PoolsDeathPatrolV1.MissionId);

            mission.Objective(Interrogate, 10773, 10774, 0, MissionObjectiveState.NotAssigned);
            mission.Objective(Return, 10776, 10777, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Bodyguards, 10773, 10774, 2, MissionObjectiveState.Incomplete);

            // 3: four of his bodyguards dead.
            mission.Transition(Bodyguards);
            mission.Progress(Bodyguards, MissionProgressEventKind.CreatureKilled, PoolsSupplyNpcsV1.Bodyguard, PoolsSupplyNpcsV1.Bodyguards, counter: 0);
            mission.Action(Bodyguards, 1, MissionActionKind.ActivateObjective, targetObjective: Interrogate, state: MissionObjectiveState.Incomplete);

            // 1: Nakrinx talks.
            mission.Transition(Interrogate);
            mission.Conversation(Interrogate, PoolsSupplyNpcsV1.NakrinxPackage);
            mission.Action(Interrogate, 1, MissionActionKind.RevealObjective, targetObjective: Return);
            mission.Action(Interrogate, 2, MissionActionKind.ActivateObjective, targetObjective: Return, state: MissionObjectiveState.Incomplete);
            mission.Action(Interrogate, 3, MissionActionKind.GrantReward, reward: 1);

            var start = PoolsSupplyNpcsV1.Out[0];
            var riggins = PoolsSnakePitNpcsV1.RigginsAt;
            mission.Indicator(Return, 2, riggins.X, riggins.Y, riggins.Z);
            mission.Indicator(Interrogate, 3, start.X, start.Y, start.Z, 60);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1322",
                "Native1 'Interrogate Nakrinx' 10773/10774 ('kill his bodyguards to get him to talk'), 2 'Return to Supply Chief " +
                "Riggins' 10776/10777, no counters; objectiveconversation 1322,1 package1522 text10775. Texts 10768-10772. Name " +
                "9403 Nakrinx.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Supply_Master",
                "Giver/receiver Supply Chief Riggins; requires Death Patrol; follow-up Eastern Listening Post; 60000XP, 5250 " +
                "credits; one of Modification: Steal Adrenaline/Armor/Health/Power; kill the four Thrax that patrol with him, " +
                "then talk. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsSupplyMasterV1",
                "Public patrol: Nakrinx 760210 (class 11320, neutral, no attack) and 4 bodyguards 760211 (user), beat from the Barracks south " +
                "(estimated). 1 shown, not active, from the start; hidden server objective 3 (4 bodyguard kills) activates it; 2 a guide, not required. " +
                "Modules [4] 123261, 123266, 123271, 123276, one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition { Script = "data.sequence", HiddenObjectiveIds = new() { Bodyguards } });
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
