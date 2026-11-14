using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Stalking the Stalkers (native mission 1272), Ligo Ashen Desert: Lieutenant Gabriel Infanger at Boneyard Outpost wants one
    /// of the Desert Stalkers the Bane drop on the Boneyard destroyed.
    ///
    /// Native: texts 10224-10228 (missionconversation): log "Travel into the Boneyard and destroy a Desert Stalker. Report back
    /// to Lieutenant Infanger once you have completed this task.", offer "We've got some serious ass to kick out there ...
    /// Cowards started dropping Stalkers on us ...", turn-in "I like you, soldier ...", reminder "You talk a lot ..."; objective
    /// (missionobjective) 1 "Destroy 1 Desert Stalker" 10229/10230 counter 10231 "Desert Stalkers". No objective conversation.
    /// Names 9304 Infanger, 9321 Class XI Desert Stalker.
    ///
    /// Sourced: TaRapedia Stalking the Stalkers: giver and reward giver Lieutenant Gabriel Infanger (Boneyard Outpost); no
    /// requirement; 190,000 XP, 8,400 credits; one of Vextronics Pulse Disperser, Cryogenic Injector Gun, Incendiary Net Gun,
    /// Electric Pistol; the Stalkers "just northwest of the boneyard at 595, -354".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band; TaRapedia's XP is twice set 2's level 40 missions', as set 3's 1247 and 1248).
    ///  - The Desert Stalker new (AshenBoneyardNpcsV1: a copy of the World DB's Stalker 520073 with the client name 9321), in two
    ///    pools of one by TaRapedia's spot; a kill counts (CreatureKilled, counter 0, 1); a marker (r15) on each pool.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (client entityclass names): Propellant
    ///    Gun EMP 15335 (37-41: "Disperser" is the propellant gun, as Pools' Virulent Disperser; "Pulse" read as EMP), Injection
    ///    Gun Ice 48108 (37-41, "Cryogenic Injector"), Net Gun Fire 12125 (41-45, "Incendiary"), Pistol Electric 14534 (40-44);
    ///    one to choose.
    /// </summary>
    public static class AshenStalkingTheStalkersV1
    {
        public const uint MissionId = 1272;
        public const uint Level = 41;
        public const uint Infanger = AshenBoneyardNpcsV1.Infanger;
        public const uint Destroy = 1;
        public const uint Kills = 1;
        public const uint Stalker = AshenBoneyardNpcsV1.Stalker;
        public const double MarkerRadius = 15;

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint PulseDisperser = 15335;       // Weapon_Avatar_PropellantGun_EMP_UNC_37_to_41
        public const uint CryogenicInjector = 48108;    // Weapon_Avatar_InjectionGun_Ice_UNC_37_to_41
        public const uint IncendiaryNetGun = 12125;     // Weapon_Avatar_NetGun_Fire_UNC_41_to_45
        public const uint ElectricPistol = 14534;       // Weapon_Avatar_Pistol_Electric_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Stalking the Stalkers", 10224, Infanger, Infanger, Level);
            mission.Reward(Experience, Credits, new[] { PulseDisperser, CryogenicInjector, IncendiaryNetGun, ElectricPistol });

            mission.Objective(Destroy, 10229, 10230, 0, MissionObjectiveState.Incomplete, counterTextId: 10231);
            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.CreatureKilled, Stalker, Kills, counter: 0);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, at) in AshenBoneyardNpcsV1.StalkerPools)
                mission.Indicator(Destroy, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1272",
                "Native1 'Destroy 1 Desert Stalker' 10229/10230 counter10231 'Desert Stalkers'; no objectiveconversation. Texts " +
                "10224-10228 (Stalkers dropped on the Boneyard; report to Lieutenant Infanger). Names 9304 Infanger, 9321 Class XI " +
                "Desert Stalker.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Stalking_the_Stalkers",
                "Giver/reward giver Lieutenant Gabriel Infanger (Boneyard Outpost); no requirement; 190000XP, 8400 credits; " +
                "Vextronics Pulse Disperser, Cryogenic Injector Gun, Incendiary Net Gun, Electric Pistol; the Stalkers just " +
                "northwest of the boneyard at 595, -354.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenStalkingTheStalkersV1",
                "Level41 estimated. Infanger 790120 new at Boneyard Outpost. New Desert Stalker 790121 (copy of 520073) in pools " +
                "790125, 790126 of one by TaR's spot; CreatureKilled once, counter 0; a marker r15 on each. Items 15335, 48108, " +
                "12125, 14534 (lowest Uncommon, band of level 41; Pulse Disperser read as the EMP propellant gun), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
