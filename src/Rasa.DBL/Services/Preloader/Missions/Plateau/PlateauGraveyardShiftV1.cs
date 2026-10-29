using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Graveyard Shift (native mission 933), Plateau: Loremaster Talisen at New Velon Village wants the
    /// Tomb Miasmas cleared from the Forean burial ground atop Mount Reverence.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5016-5019 and 6364 (missionconversation);
    /// objectives 1 "Kill 12 Tomb Miasmas." 5021/6365, counter 5022, and 2 "Kill 6 Tomb Miasma Bulls."
    /// 5023/6366, counter 5024 (missionobjective). Talisen is creature 510185 (name 8610).
    ///
    /// Sourced (TaRapedia Graveyard Shift): giver and receiver Talisen, level "30 (unconfirmed)", no
    /// requirement, 45,000 XP, 4,800 credits, "Equipment"; the miasmas are at Vogren's Tomb.
    ///
    /// Reconstructed (ESTIMATE): the Vogren's Tomb miasmas and Bulls of PlateauTombMiasmasV1; the server's
    /// general Plateau miasma (540015, also named "Tomb Miasma", 428) counts for objective 1 too.
    /// "Equipment" read as the four Uncommon V04 gloves of the 30-34 band, one of each armor type, no
    /// module. Indicator r60 over the pools.
    /// </summary>
    public static class PlateauGraveyardShiftV1
    {
        public const uint MissionId = 933;
        public const uint Level = 30;
        public const uint Talisen = 510185;
        public const uint GeneralTombMiasma = 540015;   // RegionCreaturePreloader "Miasma - Plateau", name 428
        public const uint Miasmas = 12, Bulls = 6;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioGloves = 46011;            // Armor_T3_Bio_V04_UNC_Gloves_30_to_34
        public const uint GravitonGloves = 24950;       // Armor_T3_Graviton_V04_UNC_Gloves_30_to_34
        public const uint MechGloves = 34076;           // Armor_T3_Mech_V04_UNC_Gloves_30_to_34
        public const uint StealthGloves = 40203;        // Armor_T3_Stealth_V04_UNC_Gloves_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Graveyard Shift", 5016, Talisen, Talisen, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonGloves, MechGloves, StealthGloves });

            mission.Objective(1, 5021, 6365, 0, MissionObjectiveState.Incomplete, counterTextId: 5022);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, PlateauTombMiasmasV1.TombMiasma, Miasmas, counter: 0, trigger: 1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, GeneralTombMiasma, Miasmas, counter: 0, trigger: 2);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(2, 5023, 6366, 1, MissionObjectiveState.Incomplete, counterTextId: 5024);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.CreatureKilled, PlateauTombMiasmasV1.Bull, Bulls, counter: 0);

            mission.Indicator(1, 1, -710, 464, 540, 60);       // the miasmas west of Vogren's Tomb
            mission.Indicator(2, 2, -730, 464, 555, 40);       // the Bulls

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/933",
                "Native1 'Kill 12 Tomb Miasmas' 5021/6365 counter5022, 2 'Kill 6 Tomb Miasma Bulls' 5023/6366 counter5024; texts " +
                "5016-5019, 6364. Names 8761 Tomb Miasma, 8762 Tomb Miasma Bull (creaturenamelanguage). Mount Reverence marker61 " +
                "(-755,461,497), Vogren's Tomb marker74 (-669,467,540).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Graveyard_Shift",
                "Giver/receiver Loremaster Talisen, New Velon Village; level '30 (unconfirmed)'; no requirement; 45000XP, 4800 " +
                "credits, 'Equipment'; miasmas at Vogren's Tomb, 4-5 bulls at a time, respawn about 5 minutes.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauGraveyardShiftV1",
                "Tomb Miasma650022 and Bull650023 at Vogren's Tomb (PlateauTombMiasmasV1: class6170, stats, pools estimated); " +
                "the general Plateau miasma540015 (name428 'Tomb Miasma') counts too. Equipment read as Uncommon V04 gloves " +
                "30-34: 46011, 24950, 34076, 40203.", 0.3);

            mission.Enable(new());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
