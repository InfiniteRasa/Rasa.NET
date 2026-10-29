using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Mox Clearing (native mission 1132), Retread Caves (squad instance 1694), Valverde Pools: Ellen Lenee at the caves'
    /// Retread Camp wants twelve Mox killed.
    ///
    /// Native: texts 8013-8017 (missionconversation): log "Kill 12 Mox.", offer "They've taken up residence in one of the
    /// larger caves, and there's an alpha male leading them. I need you to kill them", turn-in "Well, that helps a bit,
    /// but it isn't going to be enough, I'm afraid."; objective 2 "Mox Killed" 8018/8019, counter 8020 "Mox Killed"
    /// (missionobjective; the client has no objective 1); no objective conversation.
    ///
    /// Sourced: TaRapedia Mox Clearing: giver and receiver Ellen Lenee (Retread Caves); no requirement; 45,000 XP, 4,800
    /// credits; one of ChiTech Laser Chaingun lvl 33, Shinobi Electric Net Gun lvl 31, Vextronics Laser Pistol lvl 30,
    /// ChiTech Endothermic Polarity Gun lvl 33; until patch 1.4 no Mox kill counted. Ten Ton Hammer Retread Caves guide:
    /// "the formerly bugged Mox Clearing mission where you need to kill twelve Mox. Head to the Western Grotto, and clear
    /// the main room." TR MMO DB: level 35, 50,000 XP.
    ///
    /// Reconstructed (ESTIMATE): any kill of the caves' Mox counts, of the five pools of three PoolsRetreadCavesNpcsV1 adds
    /// to the Western Grotto (the user's choice of 2026-10-25: twelve must be there to kill); the Grotto's older pool of
    /// Pools Mox (540011, also in the Pools zone) does not count. XP
    /// TaRapedia's. Rewards the lowest Uncommon templates of the bands that hold those levels (client names "Laser
    /// Chaingun", "Electric Net Gun", "Laser Pistol", "Endothermic Polarity Gun"), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMoxClearingV1
    {
        public const uint MissionId = 1132;
        public const uint Level = 35;
        public const uint Ellen = PoolsRetreadCavesNpcsV1.Ellen;
        public const uint Kill = 2, Kills = 12;
        public const double PoolRadius = 15;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint LaserChaingun = 48256;        // Weapon_Avatar_MachineGun_Laser_UNC_33_to_37
        public const uint ElectricNetGun = 15507;       // Weapon_Avatar_NetGun_Electric_UNC_31_to_35
        public const uint LaserPistol = 17111;          // Weapon_Avatar_Pistol_Laser_UNC_30_to_34
        public const uint EndothermicPolarityGun = 48708; // Weapon_Avatar_PolarityGun_Ice_UNC_33_to_37

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Mox Clearing", 8013, Ellen, Ellen, Level);
            mission.Reward(Experience, Credits, new[] { LaserChaingun, ElectricNetGun, LaserPistol, EndothermicPolarityGun });

            mission.Objective(Kill, 8018, 8019, 0, MissionObjectiveState.Incomplete, counterTextId: 8020);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, PoolsRetreadCavesNpcsV1.Mox, Kills, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1132",
                "Native2 'Mox Killed' 8018/8019 counter8020 'Mox Killed' (no objective 1); no objectiveconversation. Texts " +
                "8013-8017 (log 'Kill 12 Mox.').");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mox_Clearing",
                "Giver/receiver Ellen Lenee (Retread Caves); no requirement; 45000XP, 4800 credits; one of ChiTech Laser Chaingun33, " +
                "Shinobi Electric Net Gun31, Vextronics Laser Pistol30, ChiTech Endothermic Polarity Gun33. Ten Ton Hammer: twelve " +
                "Mox in the Western Grotto's main room. TR MMO DB level35, 50000XP.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMoxClearingV1",
                "Kills of the caves' Mox 760230 count, five pools 760230-760234 of three in the Grotto (user). XP " +
                "TaRapedia's. Items 48256, 15507, 17111, 48708, one to choose. Indicators in the instance too.", 0.3);

            var pools = PoolsRetreadCavesNpcsV1.MoxPools;
            for (var index = 0; index < pools.Length; index++)
                mission.Indicator(Kill, 1 + (uint)index, pools[index].X, pools[index].Y, pools[index].Z, PoolRadius);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
