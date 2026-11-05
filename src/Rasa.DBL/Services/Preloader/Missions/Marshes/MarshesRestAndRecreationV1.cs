using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Rest and Recreation (native mission 1023), Valverde Marshes: Sergeant Elway at Falcon Hold has the player lead
    /// replacement squads round the guard stations east of Falcon Hold: to Lieutenant Belfast, from him to Lieutenant
    /// Delaio's lookout, from there to Lieutenant Urqhart's, and the last troop back to Falcon Hold.
    ///
    /// Native: texts 5635-5638, 7212 (missionconversation): log "Escort the replacement squad safely to Lieutenant Belfast
    /// east of Falcon Hold. He'll have instructions on your next troop drop. When all troops have been safely rotated, report
    /// back to Sergeant Elway", turn-in "Take these credits and buy yourself a little R&amp;R"; objectives 1 "Escort the first
    /// group to Lieutenant Belfast" 5639/7213, 5 "Report in to Lieutenant Belfast" 5723/7214, 2 "Escort the troops to
    /// Lieutenant Delaio's lookout." 5640/7215, 6 "Report in to Lieutenant Delaio" 5725/7216, 3 "... Urqhart's lookout."
    /// 5641/7217, 7 "Report in to Lieutenant Urqhart" 5726/7218, 4 "Escort the final troop back to Falcon Hold." 5642/7219
    /// (missionobjective); objective conversations 1023,5 package 1101 5724 (Belfast: "now get these boys rotated over to
    /// Delaio's lookout"), 1023,6 package 1102 5727 (Delaio), 1023,7 package 1100 5728 (Urqhart: "get these last soldiers
    /// back to Elway").
    ///
    /// Sourced: TaRapedia Rest and Recreation: giver and receiver Sergeant Elway (Falcon Hold); 70,000 XP, 5,550 credits;
    /// one of Modification: Crit Hit Increase / Body / Mind / Spirit Bonus lvl 30; 10 minutes per group; "make sure at least
    /// one of them gets to Belfast". Ten Ton Hammer Falcon Hold: Belfast at (110, 216, 405); a failed group fails the
    /// mission. TR MMO DB list: level 33. World DB: Elway 510169 at (-142.7, 219.1, 609.2).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Each leg a group of three of the run's own soldiers (MarshesEscortsNpcsV1.Soldier) that follow the player
    ///    (MarshesEscortScene): the first at the Falcon Hold waypoint at the accept, each later one at the officer who sends
    ///    it, after his talk. Walking into 15 m of the leg's officer (the last: 20 m of Elway) completes the leg and the
    ///    group goes in; the leg fails when all three are dead (the triage: three, at least one must arrive). A failed leg
    ///    fails the mission. Only the first group is timed: 10 minutes from the accept (its deadline). Each leg has its
    ///    deadline row (the client-sourced intent), but the server keeps one deadline per character and mission
    ///    (MissionDeadlineService, upstream), so legs 2-4 are untimed.
    ///  - Delaio and Urqhart at the Corman watchtowers (the triage). "Modification ... lvl 30" read as the [4] modules
    ///    (Tastes Like Grubber's reading): Weapon Crit Hit Bonus, Armor Body, Mind, Spirit Bonus, one to choose.
    ///  - Markers at the leg's officer (Elway for 4) and at the officer to report to.
    /// </summary>
    public static class MarshesRestAndRecreationV1
    {
        public const uint MissionId = 1023;
        public const uint Level = 33;
        public const uint Elway = 510169;
        public const uint LegSeconds = 600;
        public const double ArrivalRadius = 15, ElwayRadius = 20;

        public static uint ArrivedSequence(uint leg) => 10 + leg;
        public static uint StartSequence(uint leg) => 20 + leg;

        public static readonly ScenePosition ElwayAt = new(-142.7f, 219.28f, 609.2f);

        /// <summary>
        /// The legs: escort objective (name, body), destination and radius, the report objective there (name, body,
        /// package; 0 for the last), the group's place at its start.
        /// </summary>
        public static readonly (uint Escort, uint Name, uint Body, ScenePosition To, double Radius, uint Report, uint ReportName,
            uint ReportBody, uint Package, ScenePosition From)[] Legs =
        {
            (1, 5639, 7213, new(110f, 216.88f, 405f), ArrivalRadius, 5, 5723, 7214, MarshesEscortsNpcsV1.BelfastPackage,
                new(-143.27f, 219.05f, 581.54f)),
            (2, 5640, 7215, new(100f, 216.88f, 530.4f), ArrivalRadius, 6, 5725, 7216, MarshesEscortsNpcsV1.DelaioPackage,
                new(110f, 216.88f, 401f)),
            (3, 5641, 7217, new(45.5f, 216.88f, 704f), ArrivalRadius, 7, 5726, 7218, MarshesEscortsNpcsV1.UrqhartPackage,
                new(100f, 216.88f, 526.4f)),
            (4, 5642, 7219, ElwayAt, ElwayRadius, 0, 0, 0, 0, new(45.5f, 216.88f, 700f)),
        };

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint CritHitBonus = 123161;        // Weapon Module: Crit Hit Bonus [4]
        public const uint BodyBonus = 122966;           // Armor Module: Body Bonus [4]
        public const uint MindBonus = 122976;           // Armor Module: Mind Bonus [4]
        public const uint SpiritBonus = 122991;         // Armor Module: Spirit Bonus [4]

        /// <summary>A group of three round <paramref name="at"/>, 1.5 m apart.</summary>
        public static ScenePosition[] Group(ScenePosition at) =>
            new[] { new ScenePosition(at.X - 1.5f, at.Y, at.Z), at, new ScenePosition(at.X + 1.5f, at.Y, at.Z) };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Rest and Recreation", 5635, Elway, Elway, Level);
            mission.Reward(Experience, Credits, new[] { CritHitBonus, BodyBonus, MindBonus, SpiritBonus });

            var ordinal = 0U;
            for (var index = 0; index < Legs.Length; index++)
            {
                var leg = Legs[index];
                mission.Objective(leg.Escort, leg.Name, leg.Body, ordinal++,
                    index == 0 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);
                if (leg.Report != 0)
                    mission.Objective(leg.Report, leg.ReportName, leg.ReportBody, ordinal++, MissionObjectiveState.Inactive);
            }

            for (var index = 0; index < Legs.Length; index++)
            {
                mission.Scenario(ArrivedSequence(Legs[index].Escort), $"Group {Legs[index].Escort} arrived", 0);
                if (index > 0)
                    mission.Scenario(StartSequence(Legs[index].Escort), $"Group {Legs[index].Escort} sets out", 0);
            }

            for (var index = 0; index < Legs.Length; index++)
            {
                var leg = Legs[index];
                // The leg: walking in completes it (transition 1); 10 minutes fail it (transition 2; only leg 1 arms, the
                // server keeps one deadline per mission).
                mission.Area(leg.Escort, MarshesMissionDataV1.MapContextId, leg.To.X, leg.To.Y, leg.To.Z, leg.Radius);
                mission.Transition(leg.Escort);
                mission.EnterArea(leg.Escort, leg.Escort);
                mission.Action(leg.Escort, 1, MissionActionKind.StartScenario, scenario: ArrivedSequence(leg.Escort));
                if (leg.Report != 0)
                    Open(mission, leg.Escort, 2, leg.Report);
                else
                    mission.Action(leg.Escort, 2, MissionActionKind.GrantReward, reward: 1);
                mission.FailOnDeadline(leg.Escort, LegSeconds, transition: 2);
                mission.Indicator(leg.Escort, leg.Escort, leg.To.X, leg.To.Y, leg.To.Z, leg.Radius);

                if (leg.Report == 0)
                    continue;
                // The report: the officer's talk sends the next group.
                var next = Legs[index + 1].Escort;
                mission.Transition(leg.Report);
                mission.Conversation(leg.Report, leg.Package);
                Open(mission, leg.Report, 1, next);
                mission.Action(leg.Report, 3, MissionActionKind.StartScenario, scenario: StartSequence(next));
                mission.Indicator(leg.Report, leg.Report, leg.To.X, leg.To.Y, leg.To.Z);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1023",
                "Native1-4 escort the groups 5639-5642/7213-7219, 5-7 report to Belfast, Delaio, Urqhart 5723/5725/5726; " +
                "objectiveconversation 1023,5 pkg1101 5724, 1023,6 pkg1102 5727, 1023,7 pkg1100 5728. Texts 5635-5638. Names " +
                "8787-8789.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Rest_and_Recreation",
                "Giver/receiver Sergeant Elway (Falcon Hold); 70000XP, 5550 credits; one of Modification: Crit Hit Increase / " +
                "Body / Mind / Spirit Bonus lvl30; 10 minutes per group, at least one must get there. Ten Ton Hammer: Belfast " +
                "(110, 216, 405). TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRestAndRecreationV1",
                "Four legs of three personal soldiers 760338 (L33, 4000 HP) that follow the player; 15 m from the officer (20 m " +
                "from Elway) completes a leg; all three dead fail it; only the first group is timed, 10 minutes (engine: one " +
                "deadline per mission). Delaio, Urqhart at the Corman watchtowers. " +
                "Modules [4] 123161, 122966, 122976, 122991, one to choose.", 0.3);

            mission.Enable(MarshesEscortsNpcsV1.Scene(MissionId, Legs.Select((leg, index) =>
                new MarshesEscortsNpcsV1.Leg(leg.Escort, index == 0 ? 0 : StartSequence(leg.Escort), ArrivedSequence(leg.Escort),
                    null, MarshesEscortsNpcsV1.Soldier, Group(leg.From))).ToArray()));
        }

        /// <summary>Reveals and opens <paramref name="target"/> when <paramref name="objective"/> completes.</summary>
        private static void Open(MarshesMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
