using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// A Cult Divided (native mission 560), Valverde Pools: the Hermit sends the player with his token to the Retreads'
    /// camp, where John McKormic stops them.
    ///
    /// Native: texts 1623-1626 and 7970 (missionconversation): log "Use the Hermit's Token to introduce yourself to the
    /// Retread splinter group.", offer "Taking this small token to them will at least grant you an audience", turn-in
    /// (McKormic) "Where did you find this artifact? Amee, our leader, will want me to show her this."; objectives 2
    /// "Find the Retread Camp" 9341/9342, 3 "Item Tracker" 10550/10551, no counters (missionobjective); talk 560,2 on
    /// package 326 (McKormic, text 9343 "Hold it right there. What do you want?"). Item: Hermit's Token, template 699
    /// (class 7765 MisPoolsItemHermitToken).
    ///
    /// Sourced: TaRapedia A Cult Divided: giver the Hermit, reward from John McKormic at the Retread Camp; requires The
    /// Thirsty Hermit; follow-ups One Man's Trash and Testing the Pools; 27,500 XP, 3,400 credits, 2 Class VII Advanced Med
    /// Packs, 2 Class VII Armor Chargers, 4 Class VII Cryogenic Grenades, 4 Class VII Incendiary Grenades. TR MMO DB:
    /// level 33, series Death Patrol - The Thirsty Hermit - A Cult Divided.
    ///
    /// Reconstructed (ESTIMATE): the Hermit gives the token on accept; McKormic's talk (objective 2) takes it. Objective 3
    /// "Item Tracker" (no body text, no source of a use) is left out, as Sabotage the Tower's counter 2. Cryogenic and
    /// Incendiary read as the Ice and Fire grenades; the items the lowest template of each, all given. The marker on
    /// McKormic.
    /// </summary>
    public static class PoolsCultDividedV1
    {
        public const uint MissionId = 560;
        public const uint Level = 33;
        public const uint Hermit = PoolsThirstyHermitV1.Hermit;
        public const uint McKormic = PoolsRetreadCampNpcsV1.McKormic;
        public const uint FindCamp = 2;

        public const uint TokenTemplate = 699, TokenClass = 7765;
        public const string TokenKey = "hermits-token";

        public const uint Experience = 27500;
        public const uint Credits = 3400;
        public const uint MedPack = PoolsThirstyHermitV1.MedPack, MedPacks = 2;                 // Class VII Advanced Med Pack
        public const uint ArmorCharger = PoolsMedicinalMeasuresV1.ArmorCharger, ArmorChargers = 2; // Class VII Armor Charger
        public const uint IceGrenade = 111048, IceGrenades = 4;                                 // Consumable_Grenade_Ice_07_35
        public const uint FireGrenade = 111038, FireGrenades = 4;                               // Consumable_Grenade_Fire_07_35

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "A Cult Divided", 1623, Hermit, McKormic, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (ArmorCharger, ArmorChargers), (IceGrenade, IceGrenades), (FireGrenade, FireGrenades)
            });
            mission.Prerequisite(PoolsThirstyHermitV1.MissionId);

            mission.Objective(FindCamp, 9341, 9342, 0, MissionObjectiveState.Incomplete);
            mission.Transition(FindCamp);
            mission.Conversation(FindCamp, PoolsRetreadCampNpcsV1.McKormicPackage);
            mission.Action(FindCamp, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("mckormic-takes-token", MissionId, TokenKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(FindCamp, 2, MissionActionKind.GrantReward, reward: 1);
            var at = PoolsRetreadCampNpcsV1.McKormicAt;
            mission.Indicator(FindCamp, 1, at.X, at.Y, at.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/560",
                "Native2 'Find the Retread Camp' 9341/9342, 3 'Item Tracker' 10550/10551, no counters; objectiveconversation 560,2 " +
                "package326 text9343. Texts 1623-1626, 7970. Hermit's Token template699/class7765.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Cult_Divided",
                "Giver the Hermit, reward from John McKormic (Retread Camp); requires The Thirsty Hermit; follow-ups One Man's " +
                "Trash, Testing the Pools; 27500XP, 3400 credits, 2 Class VII Advanced Med Pack, 2 Class VII Armor Charger, 4 Class " +
                "VII Cryogenic Grenade, 4 Class VII Incendiary Grenade. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsCultDividedV1",
                "Token on accept; McKormic (760206, new) takes it at objective 2's talk; objective 3 left out. Items 45062x2, " +
                "118898x2, 111048x4, 111038x4.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(TokenKey, TokenTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-token", MissionId, TokenKey, TokenTemplate, 1) },
                Sequences = { [0] = new SceneSequenceDefinition() }
            });
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
