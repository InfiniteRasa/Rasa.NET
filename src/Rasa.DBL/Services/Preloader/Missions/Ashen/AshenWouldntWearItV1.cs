using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// I Wouldn't Wear It, But... (native mission 1289), Ligo Ashen Desert: Ida Hayner's line of clothes in Lasher leather: 10
    /// Lasher Leather Hides off the Lashers and the Pristine Lasher Hide off the one she calls Molly, who hangs around Ashoka.
    ///
    /// Native: texts 10398-10401, 10402 (missionconversation): title "I Wouldn't Wear It, But...", log "Recover 10 Lasher
    /// Leather Hides from the Lashers in the Ashen Desert, as well as 1 Pristine Lasher Hide from the Lasher named Molly. Molly
    /// can be found just west of Ashoka along the road. ...", offer "... Lasher skin! So hot it's POISON! ... I call her Molly.
    /// ... The file says that she hangs around Ashoka ...", turn-in "I'm so happy I could squeal! ..."; objective 1 "Recover
    /// 10 Lasher Leather Hides" 10403/10404, 2 "Recover 1 Pristine Lasher Hide" 11056/11057, no counters (missionobjective); no
    /// objective conversation. Items class 25485 Lasher Leather Hide (template 97486, stack 10), 25488 Pristine Lasher Hide
    /// (97489, stack 1). Names 9288 Ida Hayner, 9360 Molly.
    ///
    /// Sourced: TaRapedia I Wouldn't Wear It, But...: giver and reward giver Ida Hayner; follow-up A New Fad; 180,000 XP, 8,200
    /// credits; one of Vextronics Cryogenic Injector Gun, Laser Cannon, Pulse Chaingun, Pistol; Molly "in a little notch in
    /// the hillside directly SW of the Ashoka teleport pad (275, -659)"; Ellatha forum (Kelisco) "270, 600" (the minus sign
    /// lost: 270, -600 map view) and "she spawns on the road west of Ashoka ... over a hill on a rock shelf" (south of it).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5; TaRapedia's 180,000 XP as 1287's).
    ///  - The hide a personal 75% drop off the set's Lasher (AshenDesignerHidesNpcsV1) on Ashen Desert while objective 1 is
    ///    open, 10 at most; the Pristine Hide off Molly at 100%, one; taken at the turn-in, kept at an abandon. The client's
    ///    "Lashers in the Ashen Desert" names no place: the set's two Lasher pools stand by the road west of Ashoka, on Molly's
    ///    way (1300's "the ridge west of White Oasis" is set 12's).
    ///  - Markers: 1 on the two Lasher pools (r20), 2 on Molly's pool (r15).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (World DB entityclass names): Cryogenic
    ///    Injector Gun 48108 (37-41), Laser Cannon 49108 (39-43), Pulse Chaingun 15154 (38-42), Pistol 14409 (40-44); one to
    ///    choose.
    /// </summary>
    public static class AshenWouldntWearItV1
    {
        public const uint MissionId = 1289;
        public const uint Level = 41;
        public const uint Ida = AshenLeadInNpcsV1.IdaHayner;
        public const uint Leather = 1, Pristine = 2;
        public const uint Count = 10;
        public const int Chance = 75, BossChance = 100;
        public const uint LeatherTemplate = 97486, LeatherClass = 25485;
        public const uint PristineTemplate = 97489, PristineClass = 25488;
        public const string LeatherKey = "lasher-leather-hide", PristineKey = "pristine-lasher-hide";
        public const double MarkerRadius = 20, BossMarkerRadius = 15;

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint CryogenicInjector = 48108;    // Weapon_Avatar_InjectionGun_Ice_UNC_37_to_41
        public const uint LaserCannon = 49108;          // Weapon_Avatar_RocketLauncher_Laser_UNC_39_to_43
        public const uint PulseChaingun = 15154;        // Weapon_Avatar_MachineGun_EMP_UNC_38_to_42
        public const uint Pistol = 14409;               // Weapon_Avatar_Pistol_Physical_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "I Wouldn't Wear It, But...", 10398, Ida, Ida, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicInjector, LaserCannon, PulseChaingun, Pistol });

            mission.Objective(Leather, 10403, 10404, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Leather);
            mission.Progress(Leather, MissionProgressEventKind.ItemAcquired, LeatherClass, Count);
            mission.Action(Leather, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Pristine, 11056, 11057, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Pristine);
            mission.Progress(Pristine, MissionProgressEventKind.ItemAcquired, PristineClass, 1);

            var marker = 1U;
            foreach (var (_, at) in AshenDesignerHidesNpcsV1.LasherPools)
                mission.Indicator(Leather, marker++, at.X, at.Y, at.Z, MarkerRadius);
            var molly = AshenDesignerHidesNpcsV1.MollyAt;
            mission.Indicator(Pristine, marker, molly.X, molly.Y, molly.Z, BossMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1289",
                "Native1 'Recover 10 Lasher Leather Hides' 10403/10404, 2 'Recover 1 Pristine Lasher Hide' 11056/11057, no counters; " +
                "no objectiveconversation. Texts 10398-10402 (Ida Hayner's; Molly just west of Ashoka along the road, she hangs " +
                "around Ashoka). Items class25485 Lasher Leather Hide (template97486, stack10), class25488 Pristine Lasher Hide " +
                "(template97489). Names 9288 Ida Hayner, 9360 Molly.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/I_Wouldn%27t_Wear_It%2C_But...",
                "Giver/reward giver Ida Hayner; follow-up A New Fad; 180000XP, 8200 credits; Vextronics Cryogenic Injector Gun, " +
                "Laser Cannon, Pulse Chaingun, Pistol; Molly in a notch in the hillside SW of the Ashoka teleport pad (275, -659); " +
                "Ellatha forum: on a rock shelf south of the road west of Ashoka.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenWouldntWearItV1",
                "Level41 estimated. New Lasher 790362 (copy of 770008) in two pools 790363, 790364 by the road west of Ashoka and " +
                "Molly 790365 (Lasher boss class 10331, pool 790366); the hide a personal 75% drop, 10 at most, the Pristine Hide " +
                "off Molly at 100%; taken at the turn-in, kept at an abandon; markers r20 on the pools, r15 on Molly. Items 48108, " +
                "49108, 15154, 14409, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Leather, Pristine },
                Items = new()
                {
                    new MissionItemBinding(LeatherKey, LeatherTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Lasher }, Leather, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(PristineKey, PristineTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Molly }, Pristine, AshenMissionDataV1.MapContextId, BossChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
