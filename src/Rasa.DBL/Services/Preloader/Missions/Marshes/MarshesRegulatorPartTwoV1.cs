using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Regulator: Part II (native mission 1028), Valverde Marshes: Retread Wilson sends the player to a Thrax the Retreads
    /// once spared, outside the Bane Supply Depot, for a sample of the Bane nanovirus.
    ///
    /// Native: texts 5707-5710 and 9820 (missionconversation): log "Locate the Bane Traitor outside the Bane Supply Depot.
    /// Speak to him to get a Bane nanoviral sample.", offer "We've got one of the Thrax... he's a bit indebted to the cause...
    /// He'll be somewhere around the supply depot. Get a sample of the nanovirus.", turn-in "You tell Wilson our debt is paid!
    /// It's too dangerous out here to help Retreads!"; objective 1 "Get the Bane Nanovirus Tracker from the Bane Traitor"
    /// 5711/9821, no counter (missionobjective); objectiveconversation (1028,1) package 970 text 5712 "Quickly! Take this and
    /// go before they see you!". Item Bane Nanovirus class 21090 MisMarshesItemBaneNanovirus (World DB template 42434, stack 1).
    /// Name 8580 Conscript Kaav.
    ///
    /// Sourced: TaRapedia The Regulator: Part 2: giver Retread Wilson, receiver Conscript Kaav; requires Part 1; follow-up Part
    /// 3; level 37; 35,500 XP, 3,700 credits; one of Class VII Advanced Med Pack (2), Class VII Armor Charger (2), Class VII
    /// Concussion Grenade (4), Class VII Fragmentation Grenade (4). Ten Ton Hammer Retread City: the same four, 3,700 credits.
    /// TR MMO DB: level 35 (XP and credits not entered).
    ///
    /// Reconstructed (ESTIMATE): the Traitor's talk completes 1 and hands over the sample; the turn-in at the Traitor (the
    /// client's receiver, his turn-in text), who takes it back for Wilson (no cross-mission hand-off, as Part I). Level
    /// TaRapedia's. The stacks the client's Class VII templates. A marker at Kaav.
    /// </summary>
    public static class MarshesRegulatorPartTwoV1
    {
        public const uint MissionId = 1028;
        public const uint Level = 37;
        public const uint Wilson = MarshesRegulatorNpcsV1.Wilson, Kaav = MarshesRegulatorNpcsV1.Kaav;
        public const uint KaavPackage = MarshesRegulatorNpcsV1.KaavPackage;
        public const uint Tracker = 1;

        public const uint NanovirusTemplate = 42434, NanovirusClass = 21090, NanovirusCount = 1;
        public const string NanovirusKey = "bane-nanovirus";

        public const uint Experience = 35500;
        public const uint Credits = 3700;
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;      // Class VII Advanced Med Pack (class 22545)
        public const uint ArmorCharger = 118898, ArmorChargers = 2;          // Class VII Armor Charger (class 28489)
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4; // Class VII Concussion Grenade (class 26268)
        public const uint FragGrenade = 45446, FragGrenades = 4;             // Class VII Fragmentation Grenade (class 22963)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Regulator: Part II", 5707, Wilson, Kaav, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (AdvancedMedPack, AdvancedMedPacks), (ArmorCharger, ArmorChargers), (ConcussionGrenade, ConcussionGrenades),
                    (FragGrenade, FragGrenades)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(MarshesRegulatorPartOneV1.MissionId);

            mission.Objective(Tracker, 5711, 9821, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Tracker);
            mission.Conversation(Tracker, KaavPackage);
            mission.Action(Tracker, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("kaav-hands-nanovirus", MissionId, NanovirusKey, NanovirusTemplate, NanovirusCount));
            mission.Action(Tracker, 2, MissionActionKind.GrantReward, reward: 1);
            var kaav = MarshesRegulatorNpcsV1.KaavAt;
            mission.Indicator(Tracker, 1, kaav.X, kaav.Y, kaav.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1028",
                "Native1 'Get the Bane Nanovirus Tracker from the Bane Traitor' 5711/9821, no counter; objectiveconversation " +
                "1028,1 package970 text5712. Texts 5707-5710, 9820 (log: the Bane Traitor outside the Bane Supply Depot). Item " +
                "class21090 Bane Nanovirus (template42434). Name 8580 Conscript Kaav.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_2",
                "Giver Retread Wilson, receiver Conscript Kaav (around the Supply Depot); requires Part 1; level37; 35500XP, 3700 " +
                "credits; one of Class VII Advanced Med Pack (2), Armor Charger (2), Concussion Grenade (4), Fragmentation Grenade " +
                "(4). Ten Ton Hammer: the same; the Traitor at the refueling station around -20,-638. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRegulatorPartTwoV1",
                "Kaav 760314 (Nakrinx's class 11320, neutral, level37, package970) 13 m from the Depot door on the navmesh. His talk " +
                "completes 1 and issues the nanovirus (42434), taken at the turn-in at Kaav. Stacks 45062 x2, 118898 x2, 111018 x4, " +
                "45446 x4, one to choose. A marker at Kaav.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding(NanovirusKey, NanovirusTemplate, MissionItemScope.AssignmentIssued, NanovirusCount,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: NanovirusCount)
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
