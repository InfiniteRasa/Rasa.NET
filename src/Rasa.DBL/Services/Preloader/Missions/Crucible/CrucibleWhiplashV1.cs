using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Whiplash (native mission 1157), Ligo Crucible: Medical Technician Halprin at Outpost Intrepid needs ten Lasher venom
    /// glands for an anti-venom: a corporal stumbled into a Lasher nest.
    ///
    /// Native: texts 8463-8466, 8467 (missionconversation): log "A medic at Outpost Intrepid needs you to gather Lasher venom
    /// glands so that he can synthesize an Antivenom for a poisoned soldier.", offer "The Corporal stumbled into a Lasher
    /// nest ... I'll need at least ten of them.", turn-in "You got ten venom glads? Quick, let's have them!"; objective 1
    /// "Acquire 10 Venom Glands." 8602/8603, no counter (missionobjective); no objective conversation. Item class 24667
    /// Lasher Venom Gland (template 50295).
    ///
    /// Sourced: TaRapedia Whiplash: giver Medical Technician Halprin (Outpost Intrepid); no requirement, no follow-up;
    /// 75,000 XP, 5,700 credits; one of ChiTech RPG Launcher, ChiTech Electric Net Gun, AccuMax Virulent Leech Gun,
    /// Vextronics Pulse Shotgun; "Lasher Venom Gland x/10". TR MMO DB: level 40, "Acquire Lasher Venom Glands (0/10), Return
    /// to Medical Technician Halprin", 5700.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Halprin (the turn-in text is his; MMODB "Return to"). Halprin new (CrucibleWardensAndLashersNpcsV1).
    ///  - The nest: the new Lasher pools north-east of the Awol Camp (no source places it). A gland a personal drop from any
    ///    Lasher on the Crucible at 60% while the objective is open (no source gives a rate). Halprin takes the ten at the
    ///    turn-in; an abandon keeps them.
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 40 ("RPG Launcher" read as the physical
    ///    rocket launcher, "Pulse" as EMP, as the Marshes' Pulse Cannons): Rocket Launcher 49140, Electric Net Gun 15513,
    ///    Virulent Leech Gun 15301, EMP Shotgun 15025; one to choose. A marker at each Lasher pool.
    /// </summary>
    public static class CrucibleWhiplashV1
    {
        public const uint MissionId = 1157;
        public const uint Level = 40;
        public const uint Halprin = CrucibleWardensAndLashersNpcsV1.Halprin;
        public const uint Glands = 1;
        public const uint GlandCount = 10;
        public const int GlandChance = 60;
        public const uint GlandTemplate = 50295, GlandClass = 24667;
        public const string GlandKey = "lasher-venom-glands";
        public const uint Lasher = CrucibleWardensAndLashersNpcsV1.Lasher;
        public const double PoolMarkerRadius = 20;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint RocketLauncher = 49140;       // Weapon_Avatar_RocketLauncher_Physical_UNC_39_to_43
        public const uint ElectricNetGun = 15513;       // Weapon_Avatar_NetGun_Electric_UNC_36_to_40
        public const uint VirulentLeechGun = 15301;     // Weapon_Avatar_LeechGun_Virulent_UNC_39_to_43
        public const uint PulseShotgun = 15025;         // Weapon_Avatar_Shotgun_v1_EMP_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Whiplash", 8463, Halprin, Halprin, Level);
            mission.Reward(Experience, Credits, new[] { RocketLauncher, ElectricNetGun, VirulentLeechGun, PulseShotgun });

            mission.Objective(Glands, 8602, 8603, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Glands);
            mission.Progress(Glands, MissionProgressEventKind.ItemAcquired, GlandClass, GlandCount);
            mission.Action(Glands, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, at) in CrucibleWardensAndLashersNpcsV1.LasherPools)
                mission.Indicator(Glands, marker++, at.X, at.Y, at.Z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1157",
                "Native1 'Acquire 10 Venom Glands.' 8602/8603, no counter; no objectiveconversation. Texts 8463-8467 (a Lasher " +
                "nest). Item class24667 Lasher Venom Gland (template50295). Name 9114 Medical Technician Halprin; class 7477 Lasher.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Whiplash",
                "Giver Medical Technician Halprin (Outpost Intrepid, 995, 144, 108); no requirement; 75000XP, 5700 credits; " +
                "ChiTech RPG Launcher, ChiTech Electric Net Gun, AccuMax Virulent Leech Gun, Vextronics Pulse Shotgun; Lasher Venom " +
                "Gland x/10. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleWhiplashV1",
                "Receiver Halprin 770006 new (copy of DeLisi's row). New Lashers 770008 (class 7477, level40) in pools " +
                "770012-770013 north-east of the Awol Camp; a gland a personal 60% drop, taken at the turn-in, kept at an abandon. " +
                "Items 49140, 15513, 15301, 15025, one to choose. A marker at each pool.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Glands },
                Items = new()
                {
                    new MissionItemBinding(GlandKey, GlandTemplate, MissionItemScope.CharacterOwned, GlandCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: GlandCount,
                        Drop: new MissionItemDropDefinition(new[] { Lasher }, Glands, CrucibleMissionDataV1.MapContextId, GlandChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
