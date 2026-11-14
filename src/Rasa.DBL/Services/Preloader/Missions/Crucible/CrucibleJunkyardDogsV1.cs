using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Junkyard Dogs (native mission 1149), Ligo Crucible: Labbna Grupa Rigs wants twenty of the rabid Flare Gashers round
    /// the Staal Junkyard killed.
    ///
    /// Native: texts 8409-8412, 8413 (missionconversation): log "Rabid packs of Flare Gashers are prowling Staal Junkyard and
    /// the eastern desert. and disrupting Labbna salvage operations.", turn-in "Please accept the token of the Labbna. Prosta
    /// Tivna!"; objective 1 "Kill 20 Flare Gashers." 9357/9358, counter 9359 "Flare Gashers Killed" (missionobjective); no
    /// objective conversation. Item class 24752 Labbna Syndicate Token (template 50317).
    ///
    /// Sourced: TaRapedia Junkyard Dogs: giver and reward giver Labbna Grupa Rigs (Labbna Quarter); requires The Boss Of All
    /// Bosses; follow-up Salvage Duty; 75,000 XP, 0 credits; the Labbna Syndicate Token and one of Teleract Hazmat Armor Boots
    /// lvl 39, Titan Motor Assist Armor Boots lvl 38, Vitalius Mech Armor Vest lvl 35, Olympia Reflective Armor Boots lvl 39;
    /// "The Flare Gashers are located just before the bridge to the Staal Junkyard ... a pretty short respawn time"; the
    /// Crucible Targets of Opportunity (1589) counts the same kills. TR MMO DB: level 40, "Kill Rabid Flare Gashers (0/20)".
    /// World DB: the Crucible Flaregasher 540030 in pools 540139 (-492.1, 236.9, -149.1), 540140 (-533.2, 236.9, -251.1) and
    /// 540141 (-432.8, 236.9, -56.4) by the Staal Junkyard's door, and ten more pools on the map.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The kills as the Targets of Opportunity count Flare Gashers (TargetsOfOpportunitySeed, "Crucible Beast Master"):
    ///    creature flag 85 (SpeciesFlareGasher), counter 0, 20, anywhere; any player's own kill.
    ///  - The token a fixed reward; the four items the lowest Uncommon templates of the bands that hold TaRapedia's levels:
    ///    Hazmat V02 Boots 20355 (39-43), Motor Assist V04 Boots 28634 (38-42), Mech V02 Vest 33193 (35-39), Reflective V02
    ///    Boots 35591 (39-43); one to choose. A marker at each of the three pools by the junkyard.
    /// </summary>
    public static class CrucibleJunkyardDogsV1
    {
        public const uint MissionId = 1149;
        public const uint Level = 40;
        public const uint Rigs = CrucibleOutpostIntrepidNpcsV1.Rigs;
        public const uint Kill = 1;
        public const uint Kills = 20;
        public const uint FlareGasherFlag = 85;         // CreatureFlag.SpeciesFlareGasher (TargetsOfOpportunitySeed.Flag.FlareGasher)
        public const double MarkerRadius = 25;

        /// <summary>The Flaregasher pools by the Staal Junkyard's door (World DB): pool, place.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] Pools =
        {
            (540139, (-492.1, 236.9, -149.1)), (540140, (-533.2, 236.9, -251.1)), (540141, (-432.8, 236.9, -56.4))
        };

        public const uint Experience = 75000;
        public const uint Credits = 0;
        public const uint LabbnaToken = 50317, LabbnaTokenClass = 24752;
        public const uint HazmatBoots = 20355;          // Armor_T2_Hazmat_V02_UNC_Boots_39_to_43
        public const uint MotorAssistBoots = 28634;     // Armor_T1_MotorAssist_V04_UNC_Boots_38_to_42
        public const uint MechVest = 33193;             // Armor_T3_Mech_V02_UNC_Vest_35_to_39
        public const uint ReflectiveBoots = 35591;      // Armor_T2_Reflective_V02_UNC_Boots_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Junkyard Dogs", 8409, Rigs, Rigs, Level);
            mission.Reward(Experience, Credits, new[] { HazmatBoots, MotorAssistBoots, MechVest, ReflectiveBoots }, new[] { LabbnaToken });
            mission.Prerequisite(CrucibleBossOfAllBossesV1.MissionId);

            mission.Objective(Kill, 9357, 9358, 0, MissionObjectiveState.Incomplete, counterTextId: 9359);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureFlagKilled, FlareGasherFlag, Kills, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < Pools.Length; index++)
            {
                var at = Pools[index].At;
                mission.Indicator(Kill, (uint)index + 1, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1149",
                "Native1 'Kill 20 Flare Gashers.' 9357/9358 counter9359 'Flare Gashers Killed'; no objectiveconversation. Texts " +
                "8409-8413. Token class24752 (template50317). Name 9064 Labbna Grupa Rigs.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Junkyard_Dogs",
                "Giver/reward giver Labbna Grupa Rigs (Labbna Quarter); requires The Boss Of All Bosses; follow-up Salvage Duty; " +
                "75000XP, 0 credits; Labbna Syndicate Token and one of Hazmat Boots 39, Motor Assist Boots 38, Mech Vest 35, " +
                "Reflective Boots 39; Flare Gashers just before the bridge to the Staal Junkyard. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleJunkyardDogsV1",
                "Kills by creature flag 85 (SpeciesFlareGasher) as Targets of Opportunity 1589, counter 0, 20, anywhere. Token " +
                "fixed; items 20355, 28634, 33193, 35591, one to choose. A marker at each Flaregasher pool by the junkyard " +
                "(540139-540141).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
