using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Incommunicado (native mission 1040), Plateau: the Bane jam the radios, and Colonel Bosley at
    /// Camp Resistance sends the player into Martyr's Canyon with three modified communicators, one
    /// for each recon squad commander (PlateauReconSquadsV1). Follows Giant Killer (939).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5823-5826 and 6367 (missionconversation);
    /// objectives 1 "Deliver a comm unit to Carville." 5832/6368, 2 "... to Petrie." 5833/6369,
    /// 3 "... to Locke." 5834/6370 (missionobjective), none with a counter and no report objective;
    /// each commander's line on his package (objectiveconversation 1040,1,1116 / 1040,2,1118 /
    /// 1040,3,1117, flag 1, completion; texts 5835, 6859, 6860). The Modified AFS Communicator is
    /// class 22094, template 45010 (stack 3).
    ///
    /// Sourced (TaRapedia Incommunicado): giver and receiver Colonel Bosley, requirement Giant
    /// Killer, 100,000 XP, 6,600 credits, one of four green weapons - Shinobi Laser Chaingun 33,
    /// Teleract Electric Net Gun 31, Shinobi Laser Pistol 30, Shinobi Endothermic Polarity Gun 33.
    /// Reconstructed: level 30 (TaRapedia "32 (possibly less)", TR MMO DB 25; set equal to Giant
    /// Killer, the level of the zone's creatures); the weapons are the Uncommon base templates of
    /// the bands that hold those levels, no module; Bosley issues three communicators on acceptance and each delivery
    /// takes one (abandonment or failure removes the rest); the three deliveries may be made in
    /// any order and the reward is paid at turn-in to Bosley; one indicator on each commander.
    /// </summary>
    public static class PlateauIncommunicadoV1
    {
        public const uint MissionId = 1040;
        public const uint GiantKiller = PlateauGiantKillerV1.MissionId;
        public const uint Level = 30;
        public const uint Bosley = PlateauGiantKillerV1.Bosley;

        public const uint CommunicatorTemplate = 45010;
        public const string CommunicatorKey = "modified-afs-communicator";

        public const uint Experience = 100000;
        public const uint Credits = 6600;
        public const uint LaserChaingun = 48256;        // Weapon_Avatar_MachineGun_Laser_UNC_33_to_37
        public const uint ElectricNetGun = 15507;       // Weapon_Avatar_NetGun_Electric_UNC_31_to_35
        public const uint LaserPistol = 120384;         // Weapon_Avatar_Pistol_Laser_UNC_30_to_34
        public const uint PolarityGun = 48708;          // Weapon_Avatar_PolarityGun_Ice_UNC_33_to_37

        /// <summary>Objective, name and body text, commander's package, where he stands.</summary>
        private static readonly (uint Objective, uint Name, uint Body, uint Package, double X, double Y, double Z)[] Deliveries =
        {
            (1, 5832, 6368, PlateauReconSquadsV1.CarvellePackage, -432, 320.36, -797),   // Alpha Squad Commander Carvelle
            (2, 5833, 6369, PlateauReconSquadsV1.PetriePackage, -147, 408.12, -882),      // Beta Squad Commander Petrie
            (3, 5834, 6370, PlateauReconSquadsV1.LockePackage, 164, 322.36, -771),         // Delta Squad Commander Locke
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Incommunicado", 5823, Bosley, Bosley, Level);
            mission.Reward(Experience, Credits,
                new[] { LaserChaingun, ElectricNetGun, LaserPistol, PolarityGun });
            mission.Prerequisite(GiantKiller);

            foreach (var delivery in Deliveries)
            {
                mission.Objective(delivery.Objective, delivery.Name, delivery.Body, delivery.Objective - 1,
                    MissionObjectiveState.Incomplete);
                mission.Transition(delivery.Objective);
                mission.Conversation(delivery.Objective, delivery.Package);
                mission.Action(delivery.Objective, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent($"deliver-communicator-{delivery.Package}", MissionId,
                        CommunicatorKey, 1, MissionItemScope.AssignmentIssued));
                mission.Indicator(delivery.Objective, delivery.Objective, delivery.X, delivery.Y, delivery.Z);
            }

            // The one GrantReward names the turn-in package; it is paid when Bosley takes the mission
            // back, whichever delivery came last.
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1040",
                "Native1-3 deliver a comm unit to Carville/Petrie/Locke (5832-5834, 6368-6370), no counters, no report objective; " +
                "objectiveconversation 1040,1,1116 / 1040,2,1118 / 1040,3,1117 texts5835/6859/6860. Communicator class22094/template45010. " +
                "Texts 5823-5826, 6367. Commanders creaturenamelanguage 8847-8849 (Carvelle, not Carville).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Incommunicado",
                "Giver/receiver Colonel Bosley, Camp Resistance; requirement Giant Killer; level '32 (possibly less)'; 100000XP, " +
                "6600 credits; one of green Shinobi Laser Chaingun33/Teleract Electric Net Gun31/Shinobi Laser Pistol30/" +
                "Shinobi Endothermic Polarity Gun33. Commanders' locations from their TaRapedia NPC pages.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauIncommunicadoV1",
                "Level30. Uncommon base weapons of the bands holding the source levels, no module: 48256, 15507, 120384, 48708. " +
                "Three assignment-issued communicators45010 on acceptance, one consumed per delivery, the rest removed on " +
                "abandonment/failure. Deliveries in any order; reward at turn-in to Bosley. Indicators r8 on each commander.");

            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding(CommunicatorKey, CommunicatorTemplate, MissionItemScope.AssignmentIssued, 3,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-communicators", MissionId, CommunicatorKey, CommunicatorTemplate, 3)
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
