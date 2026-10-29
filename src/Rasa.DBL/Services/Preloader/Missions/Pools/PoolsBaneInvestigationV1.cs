using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Bane Investigation (native mission 932), Valverde Pools: Lieutenant Feldman at the Snake Pit wants to know why the
    /// Bane built a base at the western edge of the region; a terminal in a pillbox at its entrance shows that they are
    /// after an Eloh relic.
    ///
    /// Native: texts 5003-5006 and 7973 (missionconversation): log "Investigate the Bane activity in the western part of
    /// Valverde Pools by gaining access to the terminal located outside the fortifications.", offer "they were setting up
    /// a terminal line in one of the pill boxes guarding the entrance to their fortifications", turn-in "So... they found
    /// some sort after some sort of Eloh artifact?"; objectives 1 "Gain access to the Bane terminal" 5020/7974, 2 "Search
    /// for information for the reason the Bane have invested western Valverde Pools." 5034/7975, 3 "Return to Lt.
    /// Feldman" 5036/7976, no counters (missionobjective); the terminal's text on package 1007 for objective 2 (5035, "Upon
    /// activation this terminal shows what appears to be an overhead map of the area") (objectiveconversation 932,2, flag
    /// 1); no line for objective 3. The client's map has Bane consoles in the fort: ArchBaneObjConsoleV03V02 (10422) at
    /// (-790.0, 783.5, -11.3), ArchBaneObjConsoleV02 (7463) at (-795.9, 783.4, -47.3); neither talks.
    ///
    /// Sourced: TaRapedia Bane Investigation: giver and receiver Lieutenant Feldman (The Snakepit); no requirement;
    /// follow-up Sabotage the Tower; 60,000 XP, 5,250 credits; one of Modification: Crit Hit Increase, Armor Absorption,
    /// Threat Reduction, Armor Piercing; the terminal "Bane CammLink Terminal" under "Gain access to the Bane terminal".
    /// TR MMO DB: level 35, objectives "Gain access to the Bane Terminal, Return to Lieutenant Feldman".
    ///
    /// Reconstructed (ESTIMATE): the terminal a personal UsableNPCBaneKeyboard (10519, an NPC augmentation 52, as Plateau
    /// Watchtowers' 28580) at the console nearest the fort entrance, (-790, 783.6, -11.3) on the navmesh; plain talk, no
    /// keycard and no Cipher (user, 2026-10-25); one talk completes 1 and 2 (user); 3 then opens as a guide to Feldman
    /// (not required: it has no line, the turn-in at Feldman ends the mission). "Modification" read as grade [4]
    /// modules, as Monkey Wrench's: Weapon Module: Crit Hit Bonus, Armor Module: Armor Absorption, Tool Module: Threat
    /// Reduction (user), Weapon Module: Armor Piercing, one to choose.
    /// </summary>
    public static class PoolsBaneInvestigationV1
    {
        public const uint MissionId = 932;
        public const uint Level = 35;
        public const uint Feldman = PoolsSnakePitContactsV1.Feldman;
        public const uint Access = 1, Search = 2, Return = 3;

        public const uint TerminalClass = 10519;        // UsableNPCBaneKeyboard
        public const uint TerminalPackage = 1007;
        public static readonly ScenePosition TerminalAt = new(-790f, 783.6f, -11.3f);

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint CritHitBonus = 123161;        // Weapon Module: Crit Hit Bonus [4]
        public const uint ArmorAbsorption = 122951;     // Armor Module: Armor Absorption [4]
        public const uint ThreatReduction = 123141;     // Tool Module: Threat Reduction [4]
        public const uint ArmorPiercing = 123146;       // Weapon Module: Armor Piercing [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Bane Investigation", 5003, Feldman, Feldman, Level);
            mission.Reward(Experience, Credits, new[] { CritHitBonus, ArmorAbsorption, ThreatReduction, ArmorPiercing });

            mission.Objective(Access, 5020, 7974, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Search, 5034, 7975, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Return, 5036, 7976, 2, MissionObjectiveState.Inactive, required: false);

            // 2: the terminal's map; 1 is done with it.
            mission.Transition(Search);
            mission.Conversation(Search, TerminalPackage);
            mission.Action(Search, 1, MissionActionKind.RevealObjective, targetObjective: Return);
            mission.Action(Search, 2, MissionActionKind.ActivateObjective, targetObjective: Return, state: MissionObjectiveState.Incomplete);
            mission.Action(Search, 3, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Access);
            mission.ObjectiveState(Access, Search, MissionObjectiveState.Completed);

            mission.Indicator(Access, 1, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, 10);
            mission.Indicator(Search, 2, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, 10);
            var feldman = PoolsSnakePitContactsV1.FeldmanAt;
            mission.Indicator(Return, 3, feldman.X, feldman.Y, feldman.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Search, TerminalPackage, 1, transitionId: 1)
                }
            };
            scene.Actors["terminal"] = new SceneActorDefinition("terminal", SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: 0, Conversation: new SceneObjectConversation(MissionId, Search, TerminalPackage, Search));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-terminal", "terminal") } };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/932",
                "Native1 'Gain access to the Bane terminal' 5020/7974, 2 'Search for information...' 5034/7975, 3 'Return to Lt. " +
                "Feldman' 5036/7976, no counters; objectiveconversation 932,2 package1007 text5035; none for 3. Texts 5003-5006, " +
                "7973. Map consoles 10422 (-790,783.5,-11.3) and 7463 (-795.9,783.4,-47.3), not talking.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bane_Investigation",
                "Giver/receiver Lieutenant Feldman (The Snakepit); no requirement; follow-up Sabotage the Tower; 60000XP, 5250 " +
                "credits; one of Modification: Crit Hit Increase/Armor Absorption/Threat Reduction/Armor Piercing; 'Bane CammLink " +
                "Terminal'. TR MMO DB level35, series Bane Investigation.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsBaneInvestigationV1",
                "Personal terminal class10519 (augmentation52) at the console -790,783.6,-11.3; plain talk, no keycard (user); one " +
                "talk completes 1 and 2 (user); 3 a guide to Feldman, not required. Modules [4] 123161, 122951, 123141 (user), " +
                "123146, one to choose. Feldman 760204 new.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
