using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Careful Negotiation (native mission 1519), Ligo Crucible and the Staal Junkyard: Captain DeLisi sends the player to kill
    /// the Lasher Kalamit and take its reproductive sack to Cirkin in the Junkyard, the price of a talk with him.
    ///
    /// Native: texts 13547-13551 (missionconversation): log "Kill the Lasher named Kalamit and take its reproductive sack to
    /// Circin inside the Staal Junkyard.", turn-in (Cirkin) "This very fine trophy you bring to me. Please allow me to pay for
    /// it."; objectives (missionobjective) 1 "Kill Kalamit" 13629/13630 ("Kill Kalamit near the Staal Junkyard to collect its
    /// reproductive sack."), 2 "Meet Cirkin" 13631/13632 ("Travel to Staal Junkyard to deliver the "trophy" to Cirkin."), no
    /// counters. No objective conversation. Item class 26412 MisJunkyardItemKalamitReproductiveSack (template 111149).
    ///
    /// Sourced: TaRapedia Careful Negotiation: giver Captain DeLisi (Outpost Intrepid), receiver Cirkin (Staal Junkyard);
    /// requires Business Proposal (1229); follow-up Your Part of the Bargain; 90,000 XP, 6,150 credits; "Kalamit's Reproductive
    /// Sack" x/1; one of Vextronics Sonic Rifle, Sonic RPG Launcher, Virulent Leech Gun, Electric Chaingun (0.5). No source
    /// gives the level.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Kalamit: CrucibleStaalJunkyardNpcsV1. The sack a personal 100 % drop (MissionItemBinding Drop, CharacterOwned) while
    ///    1 is open, kept at an abandon, taken by Cirkin at the turn-in. 1 the sack picked up (ItemAcquired); it opens 2.
    ///  - 2 "Meet Cirkin" the player walking up to Cirkin in the Junkyard (AreaEntered, 10 m round him; the client has no talk
    ///    for it). The Junkyard is a squad instance: its copies keep the map's context id, so the area is found in each.
    ///  - Markers: 1 on Kalamit's pool; 2 on the bridge road to the Junkyard door on the Crucible and on Cirkin in the Junkyard
    ///    (the scene's IndicatorMaps).
    ///  - Level 40, as its follow-up 1529 (TR MMO DB) and The Business Proposal's chain (no source for 1519 itself).
    ///  - The items: TaRapedia's four kinds (the client has no reward table), each the lowest Uncommon template whose band holds
    ///    level 40 (Sonic Rifle 36-40, Sonic Rocket Launcher 39-43, Virulent Leech Gun 39-43, Electric Machine Gun 38-42), one to
    ///    choose.
    /// </summary>
    public static class CrucibleCarefulNegotiationV1
    {
        public const uint MissionId = 1519;
        public const uint Level = 40;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Cirkin = CrucibleStaalJunkyardNpcsV1.Cirkin;
        public const uint KillKalamit = 1, MeetCirkin = 2;
        public const uint CirkinArea = 1;
        public const double CirkinRadius = 10, KalamitMarkerRadius = 20, BridgeMarkerRadius = 8;

        public const uint SackTemplate = 111149, SackClass = 26412;
        public const string SackKey = "kalamit-reproductive-sack";
        public const int SackChance = 100;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint SonicRifle = 14658;           // Weapon_Avatar_Rifle_Sonic_UNC_36_to_40
        public const uint SonicRocketLauncher = 49172;  // Weapon_Avatar_RocketLauncher_Sonic_UNC_39_to_43
        public const uint VirulentLeechGun = 15301;     // Weapon_Avatar_LeechGun_Virulent_UNC_39_to_43
        public const uint ElectricMachineGun = 15111;   // Weapon_Avatar_MachineGun_Electric_UNC_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Careful Negotiation", 13547, DeLisi, Cirkin, Level);
            mission.Reward(Experience, Credits, new[] { SonicRifle, SonicRocketLauncher, VirulentLeechGun, ElectricMachineGun });
            mission.Prerequisite(CrucibleBusinessProposalV1.MissionId);

            mission.Objective(KillKalamit, 13629, 13630, 0, MissionObjectiveState.Incomplete);
            mission.Objective(MeetCirkin, 13631, 13632, 1, MissionObjectiveState.Inactive);

            // 1: the sack from Kalamit.
            mission.Transition(KillKalamit);
            mission.Progress(KillKalamit, MissionProgressEventKind.ItemAcquired, SackClass, 1);
            CrucibleCalendarGirlV1.Open(mission, KillKalamit, 1, 1, MeetCirkin);

            // 2: up to Cirkin in the Junkyard.
            var cirkin = CrucibleStaalJunkyardNpcsV1.CirkinAt;
            mission.Area(CirkinArea, CrucibleStaalJunkyardNpcsV1.Junkyard, cirkin.X, cirkin.Y, cirkin.Z, CirkinRadius);
            mission.Transition(MeetCirkin);
            mission.EnterArea(MeetCirkin, CirkinArea);
            mission.Action(MeetCirkin, 1, MissionActionKind.GrantReward, reward: 1);

            var kalamit = CrucibleStaalJunkyardNpcsV1.KalamitAt;
            var bridge = CrucibleStaalJunkyardNpcsV1.BridgeAt;
            mission.Indicator(KillKalamit, 1, kalamit.X, kalamit.Y, kalamit.Z, KalamitMarkerRadius);
            mission.Indicator(MeetCirkin, 2, bridge.X, bridge.Y, bridge.Z, BridgeMarkerRadius);
            mission.Indicator(MeetCirkin, 3, cirkin.X, cirkin.Y, cirkin.Z, CirkinRadius, CrucibleStaalJunkyardNpcsV1.Junkyard);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1519",
                "Native1 'Kill Kalamit' 13629/13630, 2 'Meet Cirkin' 13631/13632, no counters; no objectiveconversation. Texts " +
                "13547-13551. Item class26412 Kalamit's Reproductive Sack (template111149). Names 9720 Kalamit, 9643 Cirkin.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Careful_Negotiation",
                "Giver Captain DeLisi, receiver Cirkin (Staal Junkyard); requires Business Proposal; follow-up Your Part of the " +
                "Bargain; 90000XP, 6150 credits; Kalamit 'one hell of a Lasher that we've seen wandering near the Junkyard'; Vextronics " +
                "Sonic Rifle, Sonic RPG Launcher, Virulent Leech Gun, Electric Chaingun. No source for the level.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleCarefulNegotiationV1",
                "Kalamit 770034 new (class10331, level42) in pool 770034 south-west of the Labbna Quarter; the sack a personal 100% " +
                "drop while 1 is open, kept at an abandon, Cirkin takes it. 1 opens 2; 2 the 10 m area round Cirkin 770030 in the " +
                "Junkyard 2138. Level40 (ESTIMATE, as 1529). Items 14658, 49172, 15301, 15111 (TaRapedia's kinds), one to choose. Markers: Kalamit, the Junkyard bridge " +
                "road, Cirkin.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                ExistingFactObjectiveIds = new() { KillKalamit },
                Items = new()
                {
                    new MissionItemBinding(SackKey, SackTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { CrucibleStaalJunkyardNpcsV1.Kalamit }, KillKalamit,
                            CrucibleMissionDataV1.MapContextId, SackChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
