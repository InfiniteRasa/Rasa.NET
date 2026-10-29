using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Retread Planning Part II (native mission 1330), Valverde Pools: the Hermit begs the player to go to Amee Corman and
    /// offer to help with the Retreads' attack on the Thrax Barracks.
    ///
    /// Native: texts 10899-10903 (missionconversation): log "Convince Amee Corman to let you help her with the attack on
    /// the Bane Barracks", offer "So the Retreads are planning a full frontal assault on the Thrax Barracks? That's
    /// suicide.", turn-in (Amee) "Alright, I'll let you help because I trust you."; objective 1 "Speak to Amee Corman"
    /// 10904/10905, no counters (missionobjective); talk 1330,1 on package 329 (Amee, text 10906 "How did you find out about
    /// our upcoming attack on the Thrax Barracks?").
    ///
    /// Sourced: TaRapedia Retread Planning Part II: giver the Hermit, reward from Amee Corman at the Retread Camp; requires
    /// Retread Planning; follow-up Spy Networking; 30,000 XP, 3,500 credits, 2 Class VII Advanced Med Packs, 2 Class VII
    /// Armor Chargers, 4 Class VII Fragmentation Grenades, 4 Class VII Cryogenic Grenades. TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE): talk only, no scene. Cryogenic read as the Ice grenade; the items the lowest template of
    /// each, all given. The marker on Amee.
    /// </summary>
    public static class PoolsRetreadPlanningPartTwoV1
    {
        public const uint MissionId = 1330;
        public const uint Level = 33;
        public const uint Hermit = PoolsThirstyHermitV1.Hermit;
        public const uint Amee = PoolsRetreadCampNpcsV1.Amee;
        public const uint Speak = 1;

        public const uint Experience = 30000;
        public const uint Credits = 3500;
        public const uint MedPack = PoolsThirstyHermitV1.MedPack, MedPacks = 2;
        public const uint ArmorCharger = PoolsMedicinalMeasuresV1.ArmorCharger, ArmorChargers = 2;
        public const uint FragGrenade = PoolsThirstyHermitV1.FragGrenade, FragGrenades = 4;
        public const uint IceGrenade = PoolsCultDividedV1.IceGrenade, IceGrenades = 4;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Retread Planning Part II", 10899, Hermit, Amee, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (ArmorCharger, ArmorChargers), (FragGrenade, FragGrenades), (IceGrenade, IceGrenades)
            });
            mission.Prerequisite(PoolsRetreadPlanningV1.MissionId);

            mission.Objective(Speak, 10904, 10905, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, PoolsRetreadCampNpcsV1.AmeePackage);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);
            var at = PoolsRetreadCampNpcsV1.AmeeAt;
            mission.Indicator(Speak, 1, at.X, at.Y, at.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1330",
                "Native1 'Speak to Amee Corman' 10904/10905, no counters; objectiveconversation 1330,1 package329 text10906. " +
                "Texts 10899-10903.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Retread_Planning_Part_II",
                "Giver the Hermit, reward from Amee Corman (Retread Camp); requires Retread Planning; follow-up Spy Networking; " +
                "30000XP, 3500 credits, 2 Class VII Advanced Med Pack, 2 Class VII Armor Charger, 4 Class VII Fragmentation " +
                "Grenade, 4 Class VII Cryogenic Grenade. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsRetreadPlanningPartTwoV1",
                "Talk only. Items 45062x2, 118898x2, 45446x4, 111048x4.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
