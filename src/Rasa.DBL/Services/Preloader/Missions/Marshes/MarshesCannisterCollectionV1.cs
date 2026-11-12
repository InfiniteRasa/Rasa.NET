using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Cannister Collection (native mission 498), Valverde Marshes: Sergeant Starling in Paludos sends the player to fight
    /// the Bane in the western marshes and bring back anything of interest: a sealed Bane gas canister.
    ///
    /// Native: texts 1238-1241 and 8303 (missionconversation): log "Fight Bane in the western marshes. If you find any
    /// objects of interest, return with them to Sergeant Starling in Paludos.", offer "Head out into the northern and
    /// western marshes and start killing Bane. If you find something interesting, head back here", turn-in "Interesting.
    /// Sealed tight; I'm not opening it here. I'll have it delivered to our science guys."; objective 1 "Track down
    /// information on Bane gas harvesting" 1433/8304, no counter (missionobjective); no objective conversation. Item: Gas
    /// Collection Canister class 7668 MisMarshesItemCannisterBaneDataPad (template 661, World DB itemtemplate_itemclass).
    ///
    /// Sourced: TaRapedia Cannister Collection: giver and receiver Sergeant Starling (Paludos); level 35; 65,000 XP, 5,400
    /// credits; one of Vitalius Cryogenic Injector Gun lvl 37, Teleract Pulse Chaingun lvl 33, Animatics Electric Rifle
    /// lvl 36, Teleract Rocket Launcher lvl 34; no drop source or rate. TR MMO DB: level 33. World DB: Sergeant Starling
    /// 510171 (pool at -321, 227, -11, an upstream placeholder spot); Thrax Soldier 531045 and Thrax Grenadier 531046 are
    /// the Marshes' Bane infantry (44 pools, most of them east and south).
    ///
    /// Reconstructed (ESTIMATE): the canister as a personal drop, 25% a kill (no source gives a rate), of the Marshes'
    /// Thrax Soldiers and Grenadiers, picked up from the corpse; the drop is not limited to the west (a drop has no area on
    /// the server), the markers are on the three Thrax pools west of Paludos' longitude (World DB pools 531293, 531297,
    /// 531283; the markers at their navmesh points -470, 217.5, 39.3; -262.8, 288.9, -532.4; -319.2, 285.7, -566.8).
    /// Starling keeps it at the turn-in. Level TaRapedia's. The items
    /// the lowest Uncommon templates of the bands that hold those levels: "Cryogenic" read as Ice, "Pulse" as EMP (as Pools
    /// 898's Pulse Chaingun 15150), the Rocket Launcher as the physical one; one to choose.
    /// </summary>
    public static class MarshesCannisterCollectionV1
    {
        public const uint MissionId = 498;
        public const uint Level = 35;
        public const uint Starling = 510171;
        public const uint Collect = 1;

        public const uint CanisterTemplate = 661, CanisterClass = 7668, Canisters = 1;
        public const string CanisterKey = "gas-collection-canister";
        public const int DropChance = 25;
        public const uint ThraxSoldier = 531045, ThraxGrenadier = 531046;

        /// <summary>The markers: the Thrax pools in the west (World DB pools 531293, 531297, 531283), their radius and more.</summary>
        public static readonly (ScenePosition At, double Radius)[] Markers =
        {
            (new(-470f, 217.5f, 39.3f), 30), (new(-262.8f, 288.9f, -532.4f), 30), (new(-319.2f, 285.7f, -566.8f), 40)
        };

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint CryogenicInjectorGun = 48108; // Weapon_Avatar_InjectionGun_Ice_UNC_37_to_41
        public const uint PulseChaingun = 15150;        // Weapon_Avatar_MachineGun_EMP_UNC_33_to_37
        public const uint ElectricRifle = 14738;        // Weapon_Avatar_Rifle_Electric_UNC_36_to_40
        public const uint RocketLauncher = 49136;       // Weapon_Avatar_RocketLauncher_Physical_UNC_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Cannister Collection", 1238, Starling, Starling, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicInjectorGun, PulseChaingun, ElectricRifle, RocketLauncher });

            mission.Objective(Collect, 1433, 8304, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Collect);
            mission.Progress(Collect, MissionProgressEventKind.ItemAcquired, CanisterClass, Canisters);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);

            for (var index = 0; index < Markers.Length; index++)
                mission.Indicator(Collect, 1 + (uint)index, Markers[index].At.X, Markers[index].At.Y, Markers[index].At.Z, Markers[index].Radius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/498",
                "Native1 'Track down information on Bane gas harvesting' 1433/8304, no counter; no objectiveconversation. Texts " +
                "1238-1241, 8303 (log 'Fight Bane in the western marshes'). Gas Collection Canister class7668/template661. Name 3123 " +
                "Sergeant Starling.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Cannister_Collection",
                "Giver/receiver Sergeant Starling (Paludos); level35; 65000XP, 5400 credits; one of Vitalius Cryogenic Injector " +
                "Gun37, Teleract Pulse Chaingun33, Animatics Electric Rifle36, Teleract Rocket Launcher34; no drop source or rate. " +
                "TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesCannisterCollectionV1",
                "Personal drop of the canister from Thrax Soldier 531045 and Grenadier 531046 on map1454, 25% (rate estimated), " +
                "anywhere (no drop area); Starling keeps it. Markers on the three western Thrax pools. Items 48108, 15150, 14738, " +
                "49136 (lowest Uncommon of each kind and band), one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Collect },
                Items = new()
                {
                    new MissionItemBinding(CanisterKey, CanisterTemplate, MissionItemScope.CharacterOwned, Canisters,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: Canisters,
                        Drop: new MissionItemDropDefinition(new[] { ThraxSoldier, ThraxGrenadier }, Collect,
                            MarshesMissionDataV1.MapContextId, DropChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
