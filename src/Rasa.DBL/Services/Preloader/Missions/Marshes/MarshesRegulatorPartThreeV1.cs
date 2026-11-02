using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Regulator: Part III (native mission 1029), Valverde Marshes: Conscript Kaav, the Bane Traitor, tells the player to
    /// take down a Bane Strider for its power core and take it back to Retread Wilson.
    ///
    /// Native: texts 5713-5716 and 7833 (missionconversation): log "Take down a Bane Strider, retrieve its power core, then
    /// report back to Retread Wilson at the Logos Research Base Camp.", offer "Striders have big power! You take down strider -
    /// maybe alone, maybe with friends - and get power core.", turn-in "a Strider power core? ... Hang on to that - McCormick is
    /// going to want it"; objective 1 "Collect a power core from a Bane Strider" 5717/7834, no counter (missionobjective); no
    /// objective conversation. Item Strider Power Core class 21089 MisMarshesItemStriderPowerCore (World DB template 42433,
    /// stack 1).
    ///
    /// Sourced: TaRapedia The Regulator: Part 3: giver Conscript Kaav ("The Refueling Station"), receiver Retread Wilson;
    /// requires Part 2; follow-up Part 4; level 37; 32,500 XP, 5,550 credits; one of AccuMax Incendiary Injector Gun lvl 37,
    /// Vitalius Electric Chaingun lvl 38, ChiTech Pulse Pistol lvl 35, AccuMax Pulse Cannon lvl 34. TR MMO DB: 32,500 XP,
    /// 1,800 credits (TaRapedia's later 5,550 wins, the triage's conflict 21). Ten Ton Hammer Retread City: 5,550 credits, the
    /// same four; the cores "in triangular crates... at 225,-668 and 190,-788" (the client's "take down a Bane Strider" wins,
    /// the triage's conflict 14: no crates).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The core is a personal drop picked up from the corpse of a Bane Strider on the Marshes at 100% (the triage): the
    ///    mission's Strider pool by the Depot (MarshesRegulatorNpcsV1) or the Strider boss Daddy Long-Legs 520031 (World DB).
    ///    Wilson keeps it at the turn-in (no cross-mission hand-off, as Part I).
    ///  - The weapons the lowest-id Uncommon template of the band that starts at the named level (as set 1-3; the manufacturer
    ///    names are not in the client tables), one to choose. A marker at the Strider pool.
    /// </summary>
    public static class MarshesRegulatorPartThreeV1
    {
        public const uint MissionId = 1029;
        public const uint Level = 37;
        public const uint Kaav = MarshesRegulatorNpcsV1.Kaav, Wilson = MarshesRegulatorNpcsV1.Wilson;
        public const uint Core = 1;

        public const uint CoreTemplate = 42433, CoreClass = 21089, CoreCount = 1;
        public const string CoreKey = "strider-power-core";
        public const int CoreChance = 100;
        public const uint Strider = MarshesRegulatorNpcsV1.Strider;
        public const uint DaddyLongLegs = 520031;       // the Marshes' Strider boss (World DB, BossSpawnpoolPreloader)
        public const double PoolMarkerRadius = 20;

        public const uint Experience = 32500;
        public const uint Credits = 5550;
        public const uint IncendiaryInjectorGun = 45777;    // Weapon_Avatar_InjectionGun_Fire_UNC_37_to_41
        public const uint ElectricChaingun = 15111;         // Weapon_Avatar_MachineGun_Electric_UNC_38_to_42
        public const uint PulsePistol = 14569;              // Weapon_Avatar_Pistol_EMP_UNC_35_to_39
        public const uint PulseCannon = 15437;              // Weapon_Avatar_RocketLauncher_EMP_UNC_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Regulator: Part III", 5713, Kaav, Wilson, Level);
            mission.Reward(Experience, Credits, new[] { IncendiaryInjectorGun, ElectricChaingun, PulsePistol, PulseCannon });
            mission.Prerequisite(MarshesRegulatorPartTwoV1.MissionId);

            mission.Objective(Core, 5717, 7834, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Core);
            mission.Progress(Core, MissionProgressEventKind.ItemAcquired, CoreClass, CoreCount);
            mission.Action(Core, 1, MissionActionKind.GrantReward, reward: 1);
            var pool = MarshesRegulatorNpcsV1.StriderAt;
            mission.Indicator(Core, 1, pool.X, pool.Y, pool.Z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1029",
                "Native1 'Collect a power core from a Bane Strider' 5717/7834, no counter; no objectiveconversation. Texts " +
                "5713-5716, 7833 (log: report back to Retread Wilson at the Logos Research Base Camp). Item class21089 Strider " +
                "Power Core (template42433).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_3",
                "Giver Conscript Kaav, receiver Retread Wilson; requires Part 2; level37; 32500XP, 5550 credits; one of AccuMax " +
                "Incendiary Injector Gun lvl37, Vitalius Electric Chaingun lvl38, ChiTech Pulse Pistol lvl35, AccuMax Pulse Cannon " +
                "lvl34. TR MMO DB 1800 credits. Ten Ton Hammer: 5550 credits, cores in crates at 225,-668 and 190,-788.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRegulatorPartThreeV1",
                "Core (42433) a personal drop at 100% from a Strider: the mission pool 760315 (Pools Strider row at level36) or " +
                "Daddy Long-Legs 520031; Wilson keeps it. No crates. Items 45777, 15111, 14569, 15437 (lowest Uncommon of the band " +
                "from the named level), one to choose. A marker at the Strider pool.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Core },
                Items = new()
                {
                    new MissionItemBinding(CoreKey, CoreTemplate, MissionItemScope.CharacterOwned, CoreCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: CoreCount,
                        Drop: new MissionItemDropDefinition(new[] { Strider, DaddyLongLegs }, Core, MarshesMissionDataV1.MapContextId,
                            CoreChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
