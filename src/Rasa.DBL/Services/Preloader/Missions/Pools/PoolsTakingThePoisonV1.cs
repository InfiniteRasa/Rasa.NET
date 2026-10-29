using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Taking the Poison (native mission 1590), Valverde Pools: Corporal Sheen orders the player to collect the three
    /// poison canisters at the Thrax Barracks for the AFS, the AFS's side of the fork (PoolsPoisonCanistersV1).
    ///
    /// Native: texts 15038-15042 (missionconversation): offer "Your actions have been monitored on our system ... You are
    /// NOT to be destroying those poison canisters for the Retreads.", turn-in "Good work soldier."; objectives 1 "Collect
    /// Poison Canister I" 15043/15044, 3 "Destroy Poison Canister II" 15055/15056, 5 "Destroy Poison Canister III"
    /// 15067/15068 (the client's names), 2, 4, 6 "Tracker" (missionobjective); the consoles' packages 1641-1643
    /// (PoolsPoisonCanistersV1). Items: Poison Canister 97545 (class 25679), 111220 (26495), 111221 (26496).
    ///
    /// Sourced: TaRapedia Taking the Poison: giver and receiver Corporal Sheen (East Listening Post), "issued by an incoming
    /// transmission. It can also be issued directly by Corporal Sheen"; requires Spy Networking; follow-up Spoils of War;
    /// level 33; 130,000 XP, 5,400 credits; one of Modification: Debuff to Electric Resist, Fire Resist, Ice Resist,
    /// Virulent Resist.
    ///
    /// Reconstructed (ESTIMATE): offered by Sheen only (the server's radio offer and an NPC giver are one channel or the
    /// other: the NPC, who also re-offers it, as TaRapedia says); the canisters and the fork as PoolsPoisonCanistersV1, each
    /// dispensed canister an item Sheen takes at the turn-in; the trackers left out. "Modification" read as the grade [4]
    /// Weapon Modules (as The Supply Master's), one to choose. The markers on the consoles.
    /// </summary>
    public static class PoolsTakingThePoisonV1
    {
        public const uint MissionId = 1590;
        public const uint Level = 33;
        public const uint Sheen = PoolsSpyNpcsV1.Sheen;

        public const uint Experience = 130000;
        public const uint Credits = 5400;
        public const uint DebuffElectric = 123166;      // Weapon Module: Debuff Electric Resist [4]
        public const uint DebuffFire = 123176;          // Weapon Module: Debuff Fire Resist [4]
        public const uint DebuffIce = 123181;           // Weapon Module: Debuff Ice Resist [4]
        public const uint DebuffVirulent = 123201;      // Weapon Module: Debuff Virulent Resist [4]

        public static readonly (uint Name, uint Body)[] Texts = { (15043, 15044), (15055, 15056), (15067, 15068) };
        public static readonly (uint Template, string Key)[] Canisters =
        {
            (97545, "poison-canister-1"), (111220, "poison-canister-2"), (111221, "poison-canister-3")
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Taking the Poison", 15038, Sheen, Sheen, Level);
            mission.Reward(Experience, Credits, new[] { DebuffElectric, DebuffFire, DebuffIce, DebuffVirulent });
            mission.Prerequisite(PoolsSpyNetworkingV1.MissionId);
            var scene = PoolsPoisonCanistersV1.Build(mission, MissionId, PoolsPoisonControlV1.MissionId,
                PoolsPoisonCanistersV1.Afs, PoolsPoisonCanistersV1.Retreads, ownIsSelfDestruct: false, Canisters, Texts);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1590",
                "Native1 'Collect Poison Canister I' 15043/15044, 3/5 'Destroy Poison Canister II/III' 15055-15068, 2/4/6 " +
                "'Tracker'; objectiveconversation packages1641-1643 flag1 'Dispense Cannister', flag446 'Self Destruct'/'Dispense " +
                "Poison Cannister'. Texts 15038-15042. Items 97545/25679, 111220/26495, 111221/26496.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Taking_the_Poison",
                "Giver/receiver Corporal Sheen (radio or directly); requires Spy Networking; follow-up Spoils of War; level33; " +
                "130000XP, 5400 credits; one of Modification: Debuff to Electric/Fire/Ice/Virulent Resist; Poison Control taken " +
                "ends this offer.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsPoisonCanistersV1",
                "Offered by Sheen (package 510197); personal consoles (10519) 1.5m beside 1368's; fork flag1368001; each " +
                "canister an item. Modules [4] 123166, 123176, 123181, 123201.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
