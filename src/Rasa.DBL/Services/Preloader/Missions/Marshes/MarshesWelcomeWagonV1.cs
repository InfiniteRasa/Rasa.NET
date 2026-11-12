using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Roll out the Welcome Wagon (native mission 541), Valverde Marshes: Elder Toida in Paludos wants Overseer Cast'ka, who
    /// inspects the southern Bane guard towers, dead.
    ///
    /// Native: texts 1454-1457 and 7422 (missionconversation): log "Watch for Overseer Cast'ka inspecting the southern Bane
    /// guard towers. Elminate him and report back to Elder Toida in Paludos.", offer "a high-ranked Bane named Cast'ka ...
    /// will be inspecting the outer guard towers", turn-in "I expect his inspection didn't end quite as planned, eh?";
    /// objective 1 "Eliminate the Bane Boss, Cast'ka, and report back to Elder Toida" 1496/7423, counter 1497 "Boss
    /// Eliminated:" (missionobjective); no objective conversation. Names 3125 Elder Toida, 8548 Overseer Cast'ka.
    ///
    /// Sourced: TaRapedia Roll out the Welcome Wagon: giver and receiver Elder Toida (Paludos); level 36; 75,000 XP, 5,700
    /// credits; one of AccuMax Laser Chaingun lvl 38, ChiTech Incendiary Net Gun lvl 36, Vextronics Exothermic Polarity Gun
    /// lvl 38, Vitalius Sonic Shotgun lvl 37; Cast'ka near the southern gate of the Bane Refueling Station. TR MMO DB: level
    /// 33, 5,700 credits; Overseer Cast'ka a "Quest Monster". World DB: Elder Toida 510160 (pool at -321, 218, -7, an
    /// upstream placeholder spot); Overseer Cast'ka 520032 (L38, pool 520032 at 125, 213.9, -364, respawn 500 s).
    ///
    /// Reconstructed (ESTIMATE): the kill of the public Cast'ka counts (anyone's kill credit, as the server gives it, Pools
    /// 941 precedent); his pool stays where upstream has it (Ellatha's map-view value, upstream data bug 8 in the triage).
    /// Level TaRapedia's (rule: TaRapedia over TR MMO DB). The items the lowest Uncommon templates of the bands that hold
    /// those levels (the manufacturer names are not in the client tables), one to choose.
    /// </summary>
    public static class MarshesWelcomeWagonV1
    {
        public const uint MissionId = 541;
        public const uint Level = 36;
        public const uint Toida = 510160;
        public const uint Kill = 1;
        public const uint Castka = 520032;
        public static readonly (double X, double Y, double Z) CastkaAt = (125, 213.9, -364);
        public const double CastkaRadius = 15;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint LaserChaingun = 45762;        // Weapon_Avatar_MachineGun_Laser_UNC_38_to_42
        public const uint IncendiaryNetGun = 45812;     // Weapon_Avatar_NetGun_Fire_UNC_36_to_40
        public const uint ExothermicPolarityGun = 48680; // Weapon_Avatar_PolarityGun_Fire_UNC_38_to_42
        public const uint SonicShotgun = 14937;         // Weapon_Avatar_Shotgun_v1_Sonic_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Roll out the Welcome Wagon", 1454, Toida, Toida, Level);
            mission.Reward(Experience, Credits, new[] { LaserChaingun, IncendiaryNetGun, ExothermicPolarityGun, SonicShotgun });

            mission.Objective(Kill, 1496, 7423, 0, MissionObjectiveState.Incomplete, counterTextId: 1497);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Castka, 1, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Kill, 1, CastkaAt.X, CastkaAt.Y, CastkaAt.Z, CastkaRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/541",
                "Native1 'Eliminate the Bane Boss, Cast'ka, and report back to Elder Toida' 1496/7423 counter1497 'Boss Eliminated:'; " +
                "no objectiveconversation. Texts 1454-1457, 7422. Names 3125 Elder Toida, 8548 Overseer Cast'ka.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Roll_out_the_Welcome_Wagon",
                "Giver/receiver Elder Toida (Paludos); level36; 75000XP, 5700 credits; one of AccuMax Laser Chaingun38, ChiTech " +
                "Incendiary Net Gun36, Vextronics Exothermic Polarity Gun38, Vitalius Sonic Shotgun37; Cast'ka by the southern gate " +
                "of the Bane Refueling Station. TR MMO DB level33, Cast'ka a Quest Monster.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesWelcomeWagonV1",
                "The public Cast'ka 520032 (pool 520032, upstream place) counts. Level TaRapedia's. Items 45762, 45812, 48680, 14937 " +
                "(lowest Uncommon of each kind and band), one to choose. Marker on his pool.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
