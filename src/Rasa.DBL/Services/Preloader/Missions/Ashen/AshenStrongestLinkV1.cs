using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Strongest Link (native mission 1355, Ashen Desert), Ligo Ashen Desert: Lieutenant Jan Blood at Boneyard Outpost wants
    /// the Linkers round the Bane Conscription Base thinned, fifteen of them, and their leader Overseer Naxveik killed.
    ///
    /// Native: texts 11144-11148 (missionconversation): log "Travel to the Bane Conscription Base, at the north end of the
    /// Boneyard, and kill 15 Linkers, along with Overseer Naxveik, who is protecting the power generators. Once you have
    /// successfully thinned their numbers, return to Lieutenant Jan Blood and report your success.", offer "There's a strong
    /// perimeter of Linkers protecting that base. Hundreds of them. ...", turn-in "That was some awesome handywork. ...",
    /// reminder "Right, I understand that the job is rough. ..."; objectives (missionobjective) 1 "Kill 15 Linkers"
    /// 11424/11425 counter 11426 "Linkers Killed", 2 "Kill Overseer Naxviek" 11427/11428 counter 11429. No objective
    /// conversation, no marker names. Names 9305 Blood, 414 Linker Master (World DB 531014), 9353 Overseer Naxveik (World DB
    /// 520005).
    ///
    /// Sourced: TaRapedia The Strongest Link (Ashen Desert): giver and reward giver Lieutenant Jan Blood (Boneyard Outpost);
    /// requires Fallen Soldiers and Xanxless Job; 200,000 XP, 8,600 credits; one of Pathogex Mech Armor Boots (icon 3),
    /// Audiodyne Graviton Armor Vest (1), Teleract Bio Armor Gloves (3), CryoGen Reflective Armor Helmet (4); Naxveik at /loc
    /// 311, 26. PlayTR EU patch notes 2007-12-11: "Linkers around the Bane Conscription Control Point now properly give player
    /// kill credit", "The kill requirement of Linkers has been increased from 10 to 15". The crossref's Ten Ton Hammer lines,
    /// level 30, Wolfe reward, radio offer and "requires Look Before You Sabotage" are the Mires mission of the same name
    /// (1307; triage X1) and are not used. TaRapedia updates 2007-08-21 lists for this mission the known issue "Mission is not
    /// shareable, even though nothing indic[ates]": it was meant to be shared, so it is shareable here.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 42 (triage D5: the zone band; the chain's last after 1262 and 1263, with the L44 Overseer).
    ///  - The Linkers are the World DB's Linker - Ashen Desert 531014 ("Linker Master"), the zone's only Linker row: a kill of
    ///    it counts (CreatureKilled, counter 0, 15), wherever it stands; five new pools of three round the base
    ///    (AshenConscriptionBaseNpcsV1), a marker (r15) on each. Naxveik's kill (CreatureKilled 520005, counter 0, 1; his pool
    ///    moved into the generators' compound by AshenConscriptionBaseNpcsV1), a marker r15 on him.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 42, the nearest icon where
    ///    the band has none: Mech V03 Boots 33342 (40-44), Graviton V02 Vest 24086 (40-44; no V01 Uncommon at 38-46), Bio V03
    ///    Gloves 19544 (40-44), Reflective V03 Helmet 36638 (39-43; no V04 Uncommon helmet); one to choose.
    /// </summary>
    public static class AshenStrongestLinkV1
    {
        public const uint MissionId = 1355;
        public const uint Level = 42;
        public const uint Blood = AshenBoneyardNpcsV1.Blood;
        public const uint Linkers = 1, Overseer = 2;
        public const uint LinkerKills = 15;
        public const uint Linker = AshenConscriptionBaseNpcsV1.Linker;
        public const uint Naxveik = AshenConscriptionBaseNpcsV1.Naxveik;
        public const double MarkerRadius = 15;

        public const uint Experience = 200000;
        public const uint Credits = 8600;
        public const uint MechBoots = 33342;            // Armor_T3_Mech_V03_UNC_Boots_40_to_44
        public const uint GravitonVest = 24086;         // Armor_T3_Graviton_V02_UNC_Vest_40_to_44
        public const uint BioGloves = 19544;            // Armor_T3_Bio_V03_UNC_Gloves_40_to_44
        public const uint ReflectiveHelmet = 36638;     // Armor_T2_Reflective_V03_UNC_Helmet_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "The Strongest Link", 11144, Blood, Blood, Level, shareable: true);
            mission.Reward(Experience, Credits, new[] { MechBoots, GravitonVest, BioGloves, ReflectiveHelmet });
            mission.Prerequisite(AshenFallenSoldiersV1.MissionId, 1);
            mission.Prerequisite(AshenXanxlessJobV1.MissionId, 2);

            mission.Objective(Linkers, 11424, 11425, 0, MissionObjectiveState.Incomplete, counterTextId: 11426);
            mission.Transition(Linkers);
            mission.Progress(Linkers, MissionProgressEventKind.CreatureKilled, Linker, LinkerKills, counter: 0);
            mission.Action(Linkers, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, at) in AshenConscriptionBaseNpcsV1.LinkerPools)
                mission.Indicator(Linkers, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Objective(Overseer, 11427, 11428, 1, MissionObjectiveState.Incomplete, counterTextId: 11429);
            mission.Transition(Overseer);
            mission.Progress(Overseer, MissionProgressEventKind.CreatureKilled, Naxveik, 1, counter: 0);
            mission.Action(Overseer, 1, MissionActionKind.GrantReward, reward: 1);
            var naxveik = AshenConscriptionBaseNpcsV1.NaxveikAt;
            mission.Indicator(Overseer, marker, naxveik.X, naxveik.Y, naxveik.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1355",
                "Native1 'Kill 15 Linkers' 11424/11425 counter11426 'Linkers Killed', 2 'Kill Overseer Naxviek' 11427/11428 " +
                "counter11429; no objectiveconversation. Texts 11144-11148 (the Bane Conscription Base; 15 Linkers and Overseer " +
                "Naxveik by the power generators). Names 9305 Blood, 414 Linker Master, 9353 Overseer Naxveik.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Strongest_Link_(Ashen_Desert)",
                "Giver/reward giver Lieutenant Jan Blood (Boneyard Outpost); requires Fallen Soldiers, Xanxless Job; 200000XP, " +
                "8600 credits; Pathogex Mech Armor Boots 3, Audiodyne Graviton Armor Vest 1, Teleract Bio Armor Gloves 3, CryoGen " +
                "Reflective Armor Helmet 4; Naxveik /loc 311, 26. Patch 2007-12-11: Linker kill credit; 10 raised to 15. Updates " +
                "2007-08-21: not shareable (known issue), so shareable.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenStrongestLinkV1",
                "Level42 estimated. Linkers = World DB 531014; CreatureKilled, counter 0, 15; new pools 790164-790168 of three " +
                "round the base, a marker r15 on each. Naxveik 520005 moved into the generators' compound; his kill, a marker " +
                "r15. Items 33342, 24086, 19544, 36638 (lowest Uncommon, nearest icon, band of level 42), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
