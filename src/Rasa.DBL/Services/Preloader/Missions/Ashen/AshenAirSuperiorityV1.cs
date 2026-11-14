using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Air Superiority (native mission 1248), Ligo Ashen Desert: Lt. Colonel Trent Ivan at Shadow's Edge Post wants the Desert
    /// Predators that patrol the Sheitan Highway out of the way, so bombers can hit the Bane camp: one destroyed.
    ///
    /// Native: texts 9976-9980 (missionconversation): log "Destroy a Desert Predator that is patrolling the Sheitan Highway, north
    /// of Shadow's Edge Post. Report back to Lieutenant Colonel Ivan once you are completed.", offer "The Sheitan Highway is being
    /// patrolled by Desert Predators ... I'd recommend getting some help. Once you have a team together, just head north along
    /// the road.", turn-in "With those Predators out of the way, we can drop some strategic ordinance on that camp.", reminder
    /// "Once the two Predators patrolling the Sheitan Highway have been destroyed ..."; objective 1 "Destroy 1 Desert Predator"
    /// 9981/9982 counter 9983 "Desert Predators" (missionobjective). No objective conversation. Names 9261 Ivan, 9327 Desert
    /// Predator.
    ///
    /// Sourced: TaRapedia Air Superiority: giver and reward giver Lt. Colonel Trent Ivan (Shadow's Edge Post); no requirement;
    /// 180,000 XP, 8,200 credits; one of Vitalius Flame Thrower, Teleract Electric Rifle, Teleract Cryogenic Leech Gun,
    /// Vextronics Electric Chaingun.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 42 (triage D5: the zone band's top; the Predator's level; a group mission, "getting some help").
    ///  - The kill: the new Desert Predator (AshenSheitanHighwayNpcsV1, 790080; CreatureKilled, counter 0, 1, the client's
    ///    count: the reminder's "two" are the two that patrol). Two of them, each on its own beat on the road south of the Bane
    ///    camp; a marker on each beat that holds the whole beat (r50 and r40).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 (client entityclass names): Propellant Gun
    ///    Fire 12065 (42-46, "Flame Thrower"), Rifle Electric 11953 (41-45), Leech Gun Ice 47772 (39-43, "Cryogenic"), Machine
    ///    Gun Electric 15111 (38-42, "Chaingun"); one to choose.
    /// </summary>
    public static class AshenAirSuperiorityV1
    {
        public const uint MissionId = 1248;
        public const uint Level = 42;
        public const uint Ivan = AshenLeadInNpcsV1.Ivan;
        public const uint Destroy = 1;
        public const uint Kills = 1;
        public const uint Predator = AshenSheitanHighwayNpcsV1.Predator;

        /// <summary>A marker on each beat: its centre (a step's navmesh ground) and a radius that holds every step of the beat.</summary>
        public static readonly ((double X, double Y, double Z) At, double Radius)[] Markers =
        {
            ((-174.4, 296.05, -482.2), 50),
            ((-161.6, 299.17, -392.2), 40)
        };

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint FlameThrower = 12065;         // Weapon_Avatar_PropellantGun_Fire_UNC_42_to_46
        public const uint ElectricRifle = 11953;        // Weapon_Avatar_Rifle_Electric_UNC_41_to_45
        public const uint IceLeechGun = 47772;          // Weapon_Avatar_LeechGun_Ice_UNC_39_to_43
        public const uint ElectricChaingun = 15111;     // Weapon_Avatar_MachineGun_Electric_UNC_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Air Superiority", 9976, Ivan, Ivan, Level);
            mission.Reward(Experience, Credits, new[] { FlameThrower, ElectricRifle, IceLeechGun, ElectricChaingun });

            mission.Objective(Destroy, 9981, 9982, 0, MissionObjectiveState.Incomplete, counterTextId: 9983);
            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.CreatureKilled, Predator, Kills, counter: 0);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < Markers.Length; index++)
            {
                var (at, radius) = Markers[index];
                mission.Indicator(Destroy, 1 + (uint)index, at.X, at.Y, at.Z, radius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1248",
                "Native1 'Destroy 1 Desert Predator' 9981/9982 counter9983 'Desert Predators'; no objectiveconversation. Texts " +
                "9976-9980 (Predators patrolling the Sheitan Highway; 'the two Predators patrolling'). Names 9261 Ivan, 9327 " +
                "Desert Predator.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Air_Superiority",
                "Giver/reward giver Lt. Colonel Trent Ivan (Shadow's Edge Post); no requirement; 180000XP, 8200 credits; Vitalius " +
                "Flame Thrower, Teleract Electric Rifle, Teleract Cryogenic Leech Gun, Vextronics Electric Chaingun.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenAirSuperiorityV1",
                "Level42 estimated. New Desert Predator 790080 (copy of 520022, L42) on two beats on the highway road south of " +
                "the Bane camp, pools 790089, 790090; CreatureKilled once, counter 0; a marker on each beat (r50, r40). Items " +
                "12065, 11953, 47772, 15111 (lowest Uncommon, band of level 42), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
