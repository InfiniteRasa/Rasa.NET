using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Retread Planning (native mission 563), Valverde Pools: Headquarters calls by radio once One Man's Trash is turned in,
    /// and asks the player to find out who, what and where the Retreads plan; the five at the camp each say a line on each;
    /// the Hermit hears the news.
    ///
    /// Native: texts 1653-1656 and 7991 (missionconversation): log "The Hermit wishes for you to discover the Retread
    /// plans. He wants to know the \"Who, What, and Where\".", offer "I want to know who, what, and where. Talk to whomever
    /// you need to."; objectives (missionobjective): 3-7 "Who?" 1666-1674 / 7994-7998, 10-14 "What?" 1678-1686 / 8001-8005,
    /// 19-23 "Where?" 10884-10896 / 10885-10897, all without bodies or counters; 15 "Who?" 10869/10870 counter 10871 "Who
    /// Discovered", 16 "What?" 10872/10873 counter 10874, 17 "Where?" 10875/10876 counter 10877; 24 "Return to the Hermit"
    /// 10881/10882. Talks (objectiveconversation): 3/10/19 on package 329 (Amee), 4/11/20 on 328 (Buckley), 5/12/21 on 327
    /// (Venma), 6/13/22 on 326 (McKormic), 7/14/23 on 333 (Rickert); 24 on 325 (the Hermit, text 10883 "Do you bring news of
    /// the Retread plans?").
    ///
    /// Sourced: TaRapedia Retread Planning: giver Headquarters (radio), reward from the Hermit; requires One Man's Trash;
    /// follow-ups Retread Planning Part II and Stick Your Neck Out; 27,500 XP, 3,300 credits, 2 Class VII EMP Bombs, 2
    /// Class VII Adrenaline Boosters, 4 Class VII Cryogenic Grenades, 4 Class VII Incendiary Grenades; objectives "Who?",
    /// "What?", "Where?", "Return to the Hermit"; walkthrough "Talk to each surrounding person and between them they will
    /// reveal the mission details." TR MMO DB: level 33. Patch notes (PlanetTR, RGTR): "Fixed grammar from John McKormic in
    /// \"Retread Planning\"".
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: all the client's talks, any order within each NPC): the
    /// fifteen talks are hidden objectives, all open from the accept, so each of the five offers three topics at once;
    /// 15, 16 and 17 count their five (aggregates on counter 0). Two server-only hidden objectives open 24 once all three
    /// are done: 25 aggregates 15-17, and 26 completes on 25 and reveals 24 (an aggregate has no actions). The offer by
    /// radio after One Man's Trash, or on arrival in the Pools if it was turned in earlier, as Behind Enemy Lines. Cryogenic
    /// and Incendiary read as the Ice and Fire grenades; the items the lowest template of each, all given. The markers on
    /// the camp (Who, What, Where) and the Hermit.
    /// </summary>
    public static class PoolsRetreadPlanningV1
    {
        public const uint MissionId = 563;
        public const uint Level = 33;
        public const uint Hermit = PoolsThirstyHermitV1.Hermit, HermitPackage = PoolsThirstyHermitV1.HermitPackage;
        public const uint Who = 15, What = 16, Where = 17, ReturnToHermit = 24, AllKnown = 25, Gate = 26;

        public const uint Experience = 27500;
        public const uint Credits = 3300;
        public const uint EmpBomb = PoolsMedicinalMeasuresV1.EmpBomb, EmpBombs = 2;                  // Class VII EMP Bomb
        public const uint AdrenalineBooster = PoolsThirstyHermitV1.AdrenalineBooster, AdrenalineBoosters = 2;
        public const uint IceGrenade = PoolsCultDividedV1.IceGrenade, IceGrenades = 4;
        public const uint FireGrenade = PoolsCultDividedV1.FireGrenade, FireGrenades = 4;

        /// <summary>The five who talk, in the client's order within each topic: Amee, Buckley, Venma, McKormic, Rickert.</summary>
        public static readonly uint[] Packages =
        {
            PoolsRetreadCampNpcsV1.AmeePackage, PoolsRetreadCampNpcsV1.BuckleyPackage, PoolsRetreadCampNpcsV1.VenmaPackage,
            PoolsRetreadCampNpcsV1.McKormicPackage, PoolsRetreadCampNpcsV1.RickertPackage
        };

        /// <summary>
        /// The three topics: the counting objective, its counter text, its talks' first objective and their name and body
        /// texts (talk n: objective first + n).
        /// </summary>
        public static readonly (uint Topic, uint Name, uint Body, uint Counter, uint FirstTalk, uint[] TalkNames, uint[] TalkBodies)[] Topics =
        {
            (Who, 10869, 10870, 10871, 3, new uint[] { 1666, 1668, 1670, 1672, 1674 }, new uint[] { 7994, 7995, 7996, 7997, 7998 }),
            (What, 10872, 10873, 10874, 10, new uint[] { 1678, 1680, 1682, 1684, 1686 }, new uint[] { 8001, 8002, 8003, 8004, 8005 }),
            (Where, 10875, 10876, 10877, 19, new uint[] { 10884, 10887, 10890, 10893, 10896 }, new uint[] { 10885, 10888, 10891, 10894, 10897 }),
        };

        /// <summary>The talk objectives of a topic, in the order of <see cref="Packages"/>.</summary>
        public static uint[] Talks(uint topic)
        {
            var first = Topics.Single(entry => entry.Topic == topic).FirstTalk;
            return Enumerable.Range(0, Packages.Length).Select(index => first + (uint)index).ToArray();
        }

        public static IEnumerable<uint> AllTalks => Topics.SelectMany(entry => Talks(entry.Topic));

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Retread Planning", 1653, null, Hermit, Level);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(PoolsOneMansTrashV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey,
                    PoolsMissionDataV1.MapContextId));
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (EmpBomb, EmpBombs), (AdrenalineBooster, AdrenalineBoosters), (IceGrenade, IceGrenades), (FireGrenade, FireGrenades)
            });
            mission.Prerequisite(PoolsOneMansTrashV1.MissionId);

            var ordinal = 0U;
            foreach (var (topic, name, body, counter, _, _, _) in Topics)
                mission.Objective(topic, name, body, ordinal++, MissionObjectiveState.Incomplete, counterTextId: counter);
            mission.Objective(ReturnToHermit, 10881, 10882, ordinal++, MissionObjectiveState.Inactive);
            foreach (var (_, _, _, _, first, names, bodies) in Topics)
                for (var index = 0; index < Packages.Length; index++)
                {
                    var talk = first + (uint)index;
                    mission.Objective(talk, names[index], bodies[index], ordinal++, MissionObjectiveState.Incomplete);
                    mission.Transition(talk);
                    mission.Conversation(talk, Packages[index]);
                }
            mission.Objective(AllKnown, 10881, 10882, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(Gate, 10881, 10882, ordinal, MissionObjectiveState.Incomplete);

            // 26: once 25 has all three, the Hermit waits.
            mission.Transition(Gate);
            mission.ObjectiveState(Gate, AllKnown, MissionObjectiveState.Completed);
            mission.Action(Gate, 1, MissionActionKind.RevealObjective, targetObjective: ReturnToHermit);
            mission.Action(Gate, 2, MissionActionKind.ActivateObjective, targetObjective: ReturnToHermit,
                state: MissionObjectiveState.Incomplete);

            // 24: the Hermit hears the news.
            mission.Transition(ReturnToHermit);
            mission.Conversation(ReturnToHermit, HermitPackage);
            mission.Action(ReturnToHermit, 1, MissionActionKind.GrantReward, reward: 1);

            var camp = PoolsRetreadCampNpcsV1.AmeeAt;
            foreach (var (topic, _, _, _, _, _, _) in Topics)
                mission.Indicator(topic, topic - Who + 1, camp.X, camp.Y, camp.Z, 15);
            var hermit = PoolsThirstyHermitV1.HermitAt;
            mission.Indicator(ReturnToHermit, 4, hermit.X, hermit.Y, hermit.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/563",
                "Native3-7 'Who?', 10-14 'What?', 19-23 'Where?' (talks); 15 'Who?' 10869/10870 counter10871, 16 'What?' " +
                "10872/10873 counter10874, 17 'Where?' 10875/10876 counter10877; 24 'Return to the Hermit' 10881/10882. " +
                "objectiveconversation packages 329, 328, 327, 326, 333 for each topic; 24 on 325 text10883. Texts 1653-1656, 7991.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Retread_Planning",
                "Giver Headquarters (radio), reward from the Hermit; requires One Man's Trash; follow-ups Retread Planning Part " +
                "II, Stick Your Neck Out; 27500XP, 3300 credits, 2 Class VII EMP Bomb, 2 Class VII Adrenaline Booster, 4 Class VII " +
                "Cryogenic Grenade, 4 Class VII Incendiary Grenade; talk to each surrounding person. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsRetreadPlanningV1",
                "Fifteen hidden talks, all open at accept; 15-17 aggregates on counter0; server-only hidden 25 (aggregate of " +
                "15-17) and 26 (on 25: opens 24). Radio offer after 562 or on arrival. Items 111232x2, 118814x2, 111048x4, " +
                "111038x4.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Script = "data.sequence",
                Sequences = { [0] = new SceneSequenceDefinition() },
                HiddenObjectiveIds = AllTalks.Append(AllKnown).Append(Gate).ToList(),
                ObjectiveAggregations = Topics.ToDictionary(entry => entry.Topic,
                        entry => new MissionObjectiveAggregation(Talks(entry.Topic), (uint)Packages.Length, 0))
                    .Append(new KeyValuePair<uint, MissionObjectiveAggregation>(AllKnown,
                        new MissionObjectiveAggregation(new[] { Who, What, Where }, 3)))
                    .ToDictionary(entry => entry.Key, entry => entry.Value)
            });
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
