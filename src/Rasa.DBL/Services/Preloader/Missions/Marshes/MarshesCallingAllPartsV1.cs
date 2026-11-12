using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Calling all Parts (native mission 1084), Valverde Marshes: Retread Vincent wants parts for a Stalker controller: a
    /// motherboard from a Stalker, an AI controller from a Caretaker and a brain implant from a Forean Machina.
    ///
    /// Native: texts 6145-6148, 8454 (missionconversation): opening "They say you took down Toro", finishing "These are in
    /// decent shape - a little gooey though ... let's build you your Stalker Controller"; objectives 1 "Scavenge a
    /// Motherboard from a Stalker" 6149/8455, 2 "Scavenge an AI Controller from a Caretaker" 6150/8456, 3 "Scavenge a Brain
    /// Implant from a Forean Machina" 6151/8457, no counters (missionobjective); no objectiveconversation. Items class 22982
    /// (template 45454), 22983 (45455), 22984 (45456).
    ///
    /// Sourced: TaRapedia Calling all Parts: giver and receiver Retread Vincent; 70,000 XP, 5,550 credits; ChiTech Virulent
    /// Injector Gun, Vextronics Sonic Cannon, Vextronics Electric Rifle, Teleract Laser Chaingun (no levels); follow-up It
    /// Followed Me Home. TR MMO DB: level 33. World DB: the Marshes Stalker 540002 (one pool, -664.6, 217.6, -583), the
    /// Caretaker 531048 (in 24 mixed Thrax pools).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - After the Retread Arena's last round (1088, the opening's "you took down Toro"; either ending).
    ///  - Each part a personal drop at 100% from any of its kind on the Marshes while its objective is open (Stalker, Caretaker,
    ///    the new Forean Machina pool, MarshesRetreadArenaNpcsV1); Vincent takes them at the turn-in.
    ///  - The rewards the lowest Uncommon templates of the four kinds in the bands that hold level 33 (no client block
    ///    matches: Cannon = Rocket Launcher, Chaingun = Machine Gun, as the Pools); level 33. Markers at the Stalker pool,
    ///    the three Caretaker pools nearest Retread City and the Machina pool.
    /// </summary>
    public static class MarshesCallingAllPartsV1
    {
        public const uint MissionId = 1084;
        public const uint Level = 33;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        public const uint Motherboard = 1, AiController = 2, BrainImplant = 3;
        public const uint Stalker = 540002, Caretaker = 531048, Machina = MarshesRetreadArenaNpcsV1.Machina;
        public const double PoolMarkerRadius = 25;

        /// <summary>The parts: objective, names, template, class, key, the creature that drops it.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Template, uint Class, string Key, uint Creature)[] Parts =
        {
            (Motherboard, 6149, 8455, 45454, 22982, "stalker-motherboard", Stalker),
            (AiController, 6150, 8456, 45455, 22983, "caretaker-ai-controller", Caretaker),
            (BrainImplant, 6151, 8457, 45456, 22984, "machina-brain-implant", Machina)
        };

        /// <summary>The pools the markers stand on (World DB; the Machina's MarshesRetreadArenaNpcsV1).</summary>
        public static readonly (uint Objective, uint Pool, double X, double Y, double Z)[] Pools =
        {
            (Motherboard, 540002, -664.6, 217.6, -583),
            (AiController, 531283, -318, 285.3, -570.1), (AiController, 531307, 180.7, 220.4, -767.7),
            (AiController, 531286, 165, 224, -726.7),
            (BrainImplant, MarshesRetreadArenaNpcsV1.MachinaPool, MarshesRetreadArenaNpcsV1.MachinaAt.X,
                MarshesRetreadArenaNpcsV1.MachinaAt.Y, MarshesRetreadArenaNpcsV1.MachinaAt.Z)
        };

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public const uint VirulentInjectorGun = 15654;  // Weapon_Avatar_InjectionGun_Virulent_UNC_32_to_36
        public const uint SonicCannon = 49164;          // Weapon_Avatar_RocketLauncher_Sonic_UNC_29_to_33
        public const uint ElectricRifle = 14734;        // Weapon_Avatar_Rifle_Electric_UNC_31_to_35
        public const uint LaserChaingun = 48256;        // Weapon_Avatar_MachineGun_Laser_UNC_33_to_37

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Calling all Parts", 6145, Vincent, Vincent, Level);
            mission.Reward(Experience, Credits, new[] { VirulentInjectorGun, SonicCannon, ElectricRifle, LaserChaingun });
            mission.Prerequisite(MarshesRetreadArenaV1.LegendOrFall);

            foreach (var (objective, name, body, _, itemClass, _, _) in Parts)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ItemAcquired, itemClass, 1);
            }
            mission.Action(Motherboard, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (objective, _, x, y, z) in Pools)
                mission.Indicator(objective, marker++, x, y, z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1084",
                "Native1 'Scavenge a Motherboard from a Stalker' 6149/8455, 2 'Scavenge an AI Controller from a Caretaker' " +
                "6150/8456, 3 'Scavenge a Brain Implant from a Forean Machina' 6151/8457, no counters; no objectiveconversation. " +
                "Texts 6145-6148, 8454 (opening: you took down Toro). Items class22982 (45454), 22983 (45455), 22984 (45456).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Calling_all_Parts",
                "Giver/receiver Retread Vincent; 70000XP, 5550 credits; Virulent Injector Gun, Sonic Cannon, Electric Rifle, Laser " +
                "Chaingun; follow-up It Followed Me Home. MMODB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesCallingAllPartsV1",
                "After 1088. Each part a personal 100% drop from any Stalker 540002, Caretaker 531048, new Forean Machina 760351 " +
                "on map1454 while its objective is open; Vincent takes them. Items 15654, 49164, 14734, 48256 (lowest UNC of the " +
                "bands holding 33). Markers on the pools.", 0.3);

            var scene = new MissionSceneDefinition { ExistingFactObjectiveIds = new() { Motherboard, AiController, BrainImplant }, Items = new() };
            foreach (var (objective, _, _, template, _, key, creature) in Parts)
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1,
                    Drop: new MissionItemDropDefinition(new[] { creature }, objective, MarshesMissionDataV1.MapContextId, 100, 1)));
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
