using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Designer Jewelry Gone Wrong (native mission 1287), Ligo Ashen Desert: Ida Hayner in Shadow's Edge Post wants shiny Atta
    /// shell for her jewelry: 10 Shiny Atta Carapace off the Ashen Atta Soldiers and Harvesters of the Mauna Barrens and the
    /// Pristine Atta Carapace off the big one she calls Sally.
    ///
    /// Native: texts 10384-10387, 10388 (missionconversation): title "Designer Jewelry Gone Wrong", log "Travel to the Mauna
    /// Barrens northwest of Shadow's Edge Post and recover 10 Shiny Atta Carapace from the Ashen Atta Soldiers and Ashen Atta
    /// Harvesters in the Ashen Desert, and recover the Pristine Atta Carapace from the Ashen Atta Soldier called Sally. ...
    /// return them to Ida Hayner in Shadow's Edge Post ...", offer "... There's even a special Atta. A big one. Super nasty ...
    /// I call her Sally ...", turn-in "Oh, these will work wonderfully. ... I have a massive back order ..."; objective 1
    /// "Recover 10 Shiny Atta Carapace" 10389/10390, 2 "Recover a Pristine Atta Carapace" 11045/11046, no counters
    /// (missionobjective); no objective conversation. Items class 25482 Shiny Atta Carapace (template 97483, stack 10), 25486
    /// Pristine Atta Carapace (97487, stack 1) (itemclass). Names 9288 Ida Hayner, 9359 Sally.
    ///
    /// Sourced: TaRapedia Designer Jewelry Gone Wrong: giver and reward giver Ida Hayner (Shadow's Edge); follow-up All the
    /// Latest Rage!; 180,000 XP, 8,200 credits; one of Olympia Motor Assist Armor Legs (icon 11), CryoGen Mech Armor Legs (5),
    /// Olympia Bio Armor Legs (5), Teleract Graviton Armor Legs (1); Sally "A big one. Super nasty". Sally's place: TaRapedia
    /// (-439, -258); Ellatha forum (Kelisco) "approximately -460, -230" (map view, about 36 m off; research group 2 and 3).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5: the zone band; TaRapedia's 180,000 XP as the level-41 missions').
    ///  - The carapace a personal 75% drop (no source gives a rate; sets 7 and 8) off set 2's Ashen Atta Soldier 790043 and
    ///    Ashen Atta Worker 790044 (the client's "Harvesters": set 2's worker is the harvester class 7078) on Ashen Desert while
    ///    objective 1 is open, 10 at most (the stack); the Pristine Carapace off Sally (AshenDesignerHidesNpcsV1) at 100%, one.
    ///    Ida takes them at the turn-in; an abandon keeps them (as sets 7 and 8).
    ///  - Markers: 1 on set 2's four Atta Soldier and Worker pools (r20, as Angry Angry Atta's), 2 on Sally's pool (r15).
    ///  - The items: the lowest template of TaRapedia's kind and version number in the band of level 41 (World DB entityclass
    ///    names): Motor Assist V11 RAR Legs 45986 (38-42), Mech V05 RAR Legs 35019 (40-44), Bio V05 RAR Legs 46069 (40-44),
    ///    Graviton V01 CMN Legs 13043 (40-44; V01 has no Uncommon); one to choose.
    /// </summary>
    public static class AshenDesignerJewelryV1
    {
        public const uint MissionId = 1287;
        public const uint Level = 41;
        public const uint Ida = AshenLeadInNpcsV1.IdaHayner;
        public const uint Shiny = 1, Pristine = 2;
        public const uint Count = 10;
        public const int Chance = 75, BossChance = 100;
        public const uint ShinyTemplate = 97483, ShinyClass = 25482;
        public const uint PristineTemplate = 97487, PristineClass = 25486;
        public const string ShinyKey = "shiny-atta-carapace", PristineKey = "pristine-atta-carapace";
        public const double MarkerRadius = 20, BossMarkerRadius = 15;
        public static readonly uint[] Droppers = { AshenMaunaBarrensNpcsV1.AttaSoldier, AshenMaunaBarrensNpcsV1.AttaWorker };

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint MotorAssistLegs = 45986;      // Armor_T1_MotorAssist_V11_RAR_Legs_38_to_42
        public const uint MechLegs = 35019;             // Armor_T3_Mech_V05_RAR_Legs_40_to_44
        public const uint BioLegs = 46069;              // Armor_T3_Bio_V05_RAR_Legs_40_to_44
        public const uint GravitonLegs = 13043;         // Armor_T3_Graviton_V01_CMN_Legs_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Designer Jewelry Gone Wrong", 10384, Ida, Ida, Level);
            mission.Reward(Experience, Credits, new[] { MotorAssistLegs, MechLegs, BioLegs, GravitonLegs });

            mission.Objective(Shiny, 10389, 10390, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Shiny);
            mission.Progress(Shiny, MissionProgressEventKind.ItemAcquired, ShinyClass, Count);
            mission.Action(Shiny, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(Pristine, 11045, 11046, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Pristine);
            mission.Progress(Pristine, MissionProgressEventKind.ItemAcquired, PristineClass, 1);

            var marker = 1U;
            foreach (var (pool, creature, at) in AshenMaunaBarrensNpcsV1.AttaPools)
                if (System.Array.IndexOf(Droppers, creature) >= 0)
                    mission.Indicator(Shiny, marker++, at.X, at.Y, at.Z, MarkerRadius);
            var sally = AshenDesignerHidesNpcsV1.SallyAt;
            mission.Indicator(Pristine, marker, sally.X, sally.Y, sally.Z, BossMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1287",
                "Native1 'Recover 10 Shiny Atta Carapace' 10389/10390, 2 'Recover a Pristine Atta Carapace' 11045/11046, no " +
                "counters; no objectiveconversation. Texts 10384-10388 (Ida Hayner's; the Mauna Barrens' Ashen Atta Soldiers and " +
                "Harvesters, the Ashen Atta Soldier called Sally). Items class25482 Shiny Atta Carapace (template97483, stack10), " +
                "class25486 Pristine Atta Carapace (template97487). Names 9288 Ida Hayner, 9359 Sally.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Designer_Jewelry_Gone_Wrong",
                "Giver/reward giver Ida Hayner (Shadow's Edge); follow-up All the Latest Rage!; 180000XP, 8200 credits; Olympia " +
                "Motor Assist Armor Legs 11, CryoGen Mech Armor Legs 5, Olympia Bio Armor Legs 5, Teleract Graviton Armor Legs 1; " +
                "Sally at -439, -258 (TaRapedia), about -460, -230 map view (Ellatha forum).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenDesignerJewelryV1",
                "Level41 estimated. The carapace a personal 75% drop off Ashen Atta Soldier 790043 and Worker 790044 (set 2's " +
                "Mauna Barrens pools), 10 at most; the Pristine Carapace off Sally 790360 (pool 790361 at TaRapedia's loc) at 100%; " +
                "taken at the turn-in, kept at an abandon; markers r20 on the four pools, r15 on Sally. Items 45986, 35019, 46069, " +
                "13043, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Shiny, Pristine },
                Items = new()
                {
                    new MissionItemBinding(ShinyKey, ShinyTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(Droppers, Shiny, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(PristineKey, PristineTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Sally }, Pristine, AshenMissionDataV1.MapContextId, BossChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
