using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Angry Angry Atta (native mission 1242), Ligo Ashen Desert: Lieutenant Simon Kuang at Shadow's Edge Post wants the Atta
    /// that pour into the Mauna Barrens thinned: ten Ashen Atta Soldiers and ten Ashen Atta Workers.
    ///
    /// Native: texts 9914-9918 (missionconversation): log "Travel to the Mauna Barrens and kill 10 Ashen Atta Soldiers and 10
    /// Ashen Atta Workers. Return to Lieutenant Simon Kuang once you are done.", offer "The Atta have been acting hostile ever
    /// since they overran Avernus ... more and more Atta are pouring into Mauna Barrens ...", turn-in "No matter how many we
    /// kill, more and more seem to pour in. ... we may need to take more drastic measures."; objectives (missionobjective) 1
    /// "Kill 10 Ashen Atta Soldiers" 9919/9920 counter 9921 "Atta Soldiers", 2 "Kill 10 Atta Workers" 9922/9923 counter 9924
    /// "Atta Workers". No objective conversation. Names 9291 Kuang, 9318 Ashen Atta Soldier, 9319 Ashen Atta Worker.
    ///
    /// Sourced: TaRapedia Angry Angry Atta: giver and reward giver Lieutenant Simon Kuang (Shadow's Edge Post); no requirement;
    /// follow-up Chemical Oversight; 90,000 XP, 6,150 credits; one of Eclipse Laser Torqueshell Rifle, Vextronics Endothermic
    /// Polarity Gun, Vextronics Chaingun, Vextronics Pulse Shotgun.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5: the first of Kuang's chain).
    ///  - Each count a kill of its new creature (AshenMaunaBarrensNpcsV1: Ashen Atta Soldier 790043, Ashen Atta Worker 790044;
    ///    CreatureKilled, counter 0, 10), in the Mauna Barrens' four pools of them. A marker (r20) on each of an objective's two
    ///    pools.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 40 (client entityclass names): Torqueshell
    ///    Rifle Laser 49616 (38-42), Polarity Gun Ice 48712 (38-42, "Endothermic"), Machine Gun Physical 48300 (38-42,
    ///    "Chaingun"), Shotgun v1 EMP 15025 (37-41, "Pulse", as the Crucible's Whiplash); one to choose.
    /// </summary>
    public static class AshenAngryAngryAttaV1
    {
        public const uint MissionId = 1242;
        public const uint Level = 40;
        public const uint Kuang = AshenMaunaBarrensNpcsV1.Kuang;
        public const uint Soldiers = 1, Workers = 2;
        public const uint Kills = 10;
        public const uint Soldier = AshenMaunaBarrensNpcsV1.AttaSoldier;
        public const uint Worker = AshenMaunaBarrensNpcsV1.AttaWorker;
        public const double MarkerRadius = 20;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint LaserTorqueshell = 49616;     // Weapon_Avatar_Torqueshell_Rifle_Laser_UNC_38_to_42
        public const uint IcePolarityGun = 48712;       // Weapon_Avatar_PolarityGun_Ice_UNC_38_to_42
        public const uint Chaingun = 48300;             // Weapon_Avatar_MachineGun_Physical_UNC_38_to_42
        public const uint PulseShotgun = 15025;         // Weapon_Avatar_Shotgun_v1_EMP_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Angry Angry Atta", 9914, Kuang, Kuang, Level);
            mission.Reward(Experience, Credits, new[] { LaserTorqueshell, IcePolarityGun, Chaingun, PulseShotgun });

            mission.Objective(Soldiers, 9919, 9920, 0, MissionObjectiveState.Incomplete, counterTextId: 9921);
            mission.Transition(Soldiers);
            mission.Progress(Soldiers, MissionProgressEventKind.CreatureKilled, Soldier, Kills, counter: 0);
            mission.Action(Soldiers, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Workers, 9922, 9923, 1, MissionObjectiveState.Incomplete, counterTextId: 9924);
            mission.Transition(Workers);
            mission.Progress(Workers, MissionProgressEventKind.CreatureKilled, Worker, Kills, counter: 0);

            var marker = 1U;
            foreach (var (_, creature, at) in AshenMaunaBarrensNpcsV1.AttaPools)
                if (creature == Soldier)
                    mission.Indicator(Soldiers, marker++, at.X, at.Y, at.Z, MarkerRadius);
                else if (creature == Worker)
                    mission.Indicator(Workers, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1242",
                "Native1 'Kill 10 Ashen Atta Soldiers' 9919/9920 counter9921 'Atta Soldiers', 2 'Kill 10 Atta Workers' 9922/9923 " +
                "counter9924 'Atta Workers'; no objectiveconversation. Texts 9914-9918 (the Mauna Barrens). Names 9291 Kuang, 9318 " +
                "Ashen Atta Soldier, 9319 Ashen Atta Worker.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Angry_Angry_Atta",
                "Giver/reward giver Lieutenant Simon Kuang (Shadow's Edge Post); no requirement; follow-up Chemical Oversight; " +
                "90000XP, 6150 credits; Eclipse Laser Torqueshell Rifle, Vextronics Endothermic Polarity Gun, Vextronics Chaingun, " +
                "Vextronics Pulse Shotgun.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenAngryAngryAttaV1",
                "Level40 estimated. Kuang 790040 new at the Shadow's Edge west gate. New Ashen Atta Soldier 790043 (copy of 531015) " +
                "and Worker 790044 (copy of 531099) in Mauna Barrens pools 790046-790049; CreatureKilled, counter 0, 10 each; a " +
                "marker r20 on each pool. Items 49616, 48712, 48300, 15025 (lowest Uncommon, band of level 40), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
