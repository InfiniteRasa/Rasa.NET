using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Mortar Madness (native mission 1874), Valverde Marshes: Sergeant McDonald at the sentry tower of Far West Watch wants
    /// three of the Bane mortars that shell the AFS from the Bane strongholds to the east taken out.
    ///
    /// Native: texts 19154-19158 (missionconversation): log "Take out mortars surrounding the Bane strongholds to the east of
    /// Far West Watch.", offer "Those mortars to the east are ripping our soldiers apart.", turn-in "Your work saved lives,
    /// soldier."; objective 1 "Destroy 3 Bane Mortars at Bane Bases East of Far West Watch" 19553/19554, counter 19555 "Bane
    /// Mortars Destroyed" (missionobjective); no objective conversation.
    ///
    /// Sourced: TaRapedia Mortar Madness: giver and receiver Sgt. McDonald (Far West Watch, earlier "Central Guard Tower");
    /// 130,000 XP, 7,200 credits; four items of levels 33/35/34/34. Ten Ton Hammer Eastern Marshes: McDonald at -125, 224,
    /// -311; 3 mortars, "found along the Bane fortification walls in the east, along a north to south line"; 7,200 credits;
    /// Stealth Legs, Motor Assist / Hazmat / Reflective Helmet. World DB: the Marshes' Bane Mortar 592006 (class 7482
    /// Emplacement_Bane_Turret_Standard, level 36) in 15 pools; the eight automatic ones of the eastern wall line 592164-592171
    /// run from (177, -554) to (462, 79). Client item templates 121221-121224 (World DB itemtemplate, the client's list): Motor
    /// Assist Helmet 33-37, Stealth Legs 35-39, Hazmat Helmet 34-38, Reflective Helmet 34-38 - TaRapedia's levels and Ten Ton
    /// Hammer's kinds, in that order.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Any Bane Mortar kill on the Marshes counts (the public mortars, as 541's public boss), three to finish (counter 0).
    ///  - The rewards the client's template block 121221-121224 (its kinds and levels match both sources), one to choose.
    ///    Level 37 (the triage; no source names one).
    ///  - A marker on each mortar of the eastern wall line, none named: the client's 414 "Bane Mortar Position" can key only
    ///    one marker of the objective (961's rule).
    /// </summary>
    public static class MarshesMortarMadnessV1
    {
        public const uint MissionId = 1874;
        public const uint Level = 37;
        public const uint McDonald = MarshesSentryTowerNpcsV1.McDonald;
        public const uint Destroy = 1;

        public const uint BaneMortar = 592006;          // Bane Mortar - Marshes (World DB)
        public const uint Mortars = 3;
        public const double MarkerRadius = 15;

        /// <summary>The automatic Bane Mortar pools of the eastern wall line (World DB spawnpool), north to south by z.</summary>
        public static readonly (uint Pool, double X, double Y, double Z)[] WallMortars =
        {
            (592164, 176.586, 229.285, -554.06), (592165, 247.697, 229.285, -467.506), (592166, 294.206, 229.285, -411.35),
            (592167, 377.255, 223.448, -269.534), (592168, 409.053, 223.623, -193.944), (592169, 434.858, 223.623, -121.008),
            (592170, 446.902, 223.478, -50.398), (592171, 462.255, 222.862, 78.515)
        };

        public const uint Experience = 130000;
        public const uint Credits = 7200;
        public const uint MotorAssistHelmet = 121221;   // Armor_T1_MotorAssist_V06_UNC_Helmet_33_to_37
        public const uint StealthLegs = 121222;         // Armor_T3_Stealth_V04_UNC_Legs_35_to_39
        public const uint HazmatHelmet = 121223;        // Armor_T2_Hazmat_V02_UNC_Helmet_34_to_38
        public const uint ReflectiveHelmet = 121224;    // Armor_T2_Reflective_V02_UNC_Helmet_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Mortar Madness", 19154, McDonald, McDonald, Level);
            mission.Reward(Experience, Credits, new[] { MotorAssistHelmet, StealthLegs, HazmatHelmet, ReflectiveHelmet });

            mission.Objective(Destroy, 19553, 19554, 0, MissionObjectiveState.Incomplete, counterTextId: 19555);
            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.CreatureKilled, BaneMortar, Mortars, counter: 0);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < WallMortars.Length; index++)
            {
                var (_, x, y, z) = WallMortars[index];
                mission.Indicator(Destroy, 1 + (uint)index, x, y, z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1874",
                "Native1 'Destroy 3 Bane Mortars at Bane Bases East of Far West Watch' 19553/19554 counter19555 'Bane Mortars " +
                "Destroyed'; no objectiveconversation. Texts 19154-19158. Name 10327 Sergeant McDonald. Item templates " +
                "121221-121224 (Motor Assist Helmet 33-37, Stealth Legs 35-39, Hazmat Helmet 34-38, Reflective Helmet 34-38).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mortar_Madness",
                "Giver/receiver Sgt. McDonald (Far West Watch); 130000XP, 7200 credits; 4 items lvl33/35/34/34. Ten Ton Hammer " +
                "Eastern Marshes: McDonald -125,224,-311; 3 mortars along the Bane walls in the east, north to south; Stealth " +
                "Legs, Motor Assist / Hazmat / Reflective Helmet.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesMortarMadnessV1",
                "Any public Bane Mortar 592006 kill on the Marshes counts, 3 to finish. Level37. Items the client block " +
                "121221-121224, one to choose. Eight unnamed markers on the wall-line mortars 592164-592171.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
