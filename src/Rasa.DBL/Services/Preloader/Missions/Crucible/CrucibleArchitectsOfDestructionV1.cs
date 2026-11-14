using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Architects Of Destruction (native mission 1202), Ligo Crucible: Viddia Sigal Nema wants twenty Reconstructor Bots
    /// destroyed near the Chaukas Robotics Facility: they repair the Warden Bots and build new ones.
    ///
    /// Native: texts 9426-9429, 9430 (missionconversation): log "Sigal Nema of the Viddia wants you to hunt and destroy 20
    /// Reconstructor Bots. Reconstructor Bots can be found in the vicinity of the Chaukas Robotics Facility.", offer "Though
    /// they are not as common, a single Reconstructor Bot is more dangerous than twenty Warden bots", turn-in "You have
    /// destroyed 20 of the Reconstructor Bots? This is good news."; objective 1 "Kill 20 reconstructor Bots" 9431/9432,
    /// counter 9433 "Reconstructor Bots Killed" (missionobjective); no objective conversation. Name 9069 "Viddia Sigal Nema".
    ///
    /// Sourced: TaRapedia Architects Of Destruction: giver Viddia Sigal Nema (Staal Infirmary); requires Game Wardens (1147);
    /// 80,000 XP, 5,850 credits; one of Vitalius Filament Blade, Vextronics Cryogenic Injector Gun, Pulse Cannon, Pulse Pistol;
    /// 20 Reconstructor Bots. TR MMO DB quest list: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Nema (the turn-in text is hers). Nema new (CrucibleStaalSigalsNpcsV1).
    ///  - A kill of any creature of the Reconstructor classes counts (CreatureClassKilled 6960 Creature_Prison_Bot_Reconstructor
    ///    and 10314 its boss, counter 0, 20), as Targets of Opportunity 1589's "Crucible Terminator" names the bot classes.
    ///    The bots: set 4's Reconstructor Bot Laborers 770015 in set 4's two pools and set 5's two more.
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 35 ("Filament Blade" of no kind read as the
    ///    physical blade; "Cryogenic" the ice injection gun; "Pulse" EMP; "Cannon" the rocket launcher, the client's name, as
    ///    the Pools' Sonic Cannon): Blade 47688, Ice Injection Gun 48104, EMP Rocket Launcher 15437, EMP Pistol 14569; one to
    ///    choose. A marker (radius 20) on each of the four Reconstructor pools.
    /// </summary>
    public static class CrucibleArchitectsOfDestructionV1
    {
        public const uint MissionId = 1202;
        public const uint Level = 35;
        public const uint Nema = CrucibleStaalSigalsNpcsV1.Nema;
        public const uint Destroy = 1;
        public const uint Bots = 20;
        /// <summary>Creature_Prison_Bot_Reconstructor and Creature_Prison_Bot_Reconstructor_Boss (client entityclass).</summary>
        public static readonly uint[] ReconstructorClasses = { 6960, 10314 };
        public const double MarkerRadius = 20;

        /// <summary>The four Reconstructor pools: set 4's two and set 5's two.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] Pools =
            CrucibleMinefieldAndSalvageNpcsV1.ReconstructorPools.Concat(CrucibleStaalSigalsNpcsV1.ReconstructorPools).ToArray();

        public const uint Experience = 80000;
        public const uint Credits = 5850;
        public const uint FilamentBlade = 47688;        // Weapon_Avatar_Blade_Physical_UNC_31_to_35
        public const uint CryogenicInjectorGun = 48104; // Weapon_Avatar_InjectionGun_Ice_UNC_32_to_36
        public const uint PulseCannon = 15437;          // Weapon_Avatar_RocketLauncher_EMP_UNC_34_to_38
        public const uint PulsePistol = 14569;          // Weapon_Avatar_Pistol_EMP_UNC_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Architects Of Destruction", 9426, Nema, Nema, Level);
            mission.Reward(Experience, Credits, new[] { FilamentBlade, CryogenicInjectorGun, PulseCannon, PulsePistol });
            mission.Prerequisite(CrucibleGameWardensV1.MissionId);

            mission.Objective(Destroy, 9431, 9432, 0, MissionObjectiveState.Incomplete, counterTextId: 9433);
            mission.Transition(Destroy);
            for (var i = 0; i < ReconstructorClasses.Length; i++)
                mission.Progress(Destroy, MissionProgressEventKind.CreatureClassKilled, ReconstructorClasses[i], Bots, counter: 0, trigger: (uint)i + 1);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            var index = 0U;
            foreach (var (_, at) in Pools)
                mission.Indicator(Destroy, ++index, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1202",
                "Native1 'Kill 20 reconstructor Bots' 9431/9432 counter9433 'Reconstructor Bots Killed'; no objectiveconversation. " +
                "Texts 9426-9430 (Reconstructor Bots in the vicinity of the Chaukas Robotics Facility). Name 9069 Viddia Sigal Nema; " +
                "classes 6960 Creature_Prison_Bot_Reconstructor, 10314 its boss.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Architects_Of_Destruction",
                "Giver Viddia Sigal Nema (Staal Infirmary); requires Game Wardens; 80000XP, 5850 credits; one of Vitalius Filament " +
                "Blade, Vextronics Cryogenic Injector Gun, Pulse Cannon, Pulse Pistol; 20 Reconstructor Bots. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleArchitectsOfDestructionV1",
                "Receiver Nema 770019 new (copy of Donal's row) 4 m from Donal. CreatureClassKilled 6960 and 10314, counter 0, 20, " +
                "as ToO 1589 objective 71. Set 4's Reconstructor pools 770017-770018 and new 770027-770028. Items 47688, 48104, " +
                "15437, 14569, one to choose. A marker r20 on each pool.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
