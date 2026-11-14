using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Harmful Mutations (native mission 1243), Ligo Ashen Desert: Lieutenant Simon Kuang sends the player into the Mauna
    /// Barrens for four blood samples of the Mutated Atta Soldiers, for Doctor Jung Kwon at Shadow's Edge Post.
    ///
    /// Native: texts 9925-9929 (missionconversation): log "Travel to the Mauna Barrens and retrieve 4 Mutated Atta Blood
    /// Samples from the Mutated Atta Soldiers. Return the blood samples to Doctor Kwon as soon as you have collected enough.",
    /// offer "Our recon teams noticed that some of the Atta are mutating. Doctor Kwon is looking into it, but she's going to
    /// need some blood samples. ...", turn-in "These are the Atta blood samples I requested? ..." (Kwon's voice); objective 1
    /// "Recover 4 Mutant Atta Blood Samples" 9930/9931, no counter (missionobjective). No objective conversation. Item class
    /// 25409 Mis_Ligo_Ashen_Atta_Mutant_Samples "Mutated Atta Blood Samples" (template 97458). Names 9291 Kuang, 9298 Kwon,
    /// 9320 Mutated Atta Soldier.
    ///
    /// Sourced: TaRapedia Harmful Mutations: giver Lieutenant Simon Kuang, reward giver Doctor Jung Kwon (Shadow's Edge Post);
    /// no requirement; follow-up Eyes on the Ground; 4 samples; 90,000 XP, 6,150 credits; one of Titan Mech Armor Gloves (icon
    /// 5), Luminar Stealth Armor Legs (5), Wellcare Reflective Armor Legs (3), Dynamo Motor Assist Armor Gloves (11).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5).
    ///  - A sample a personal drop from a Mutated Atta Soldier (AshenMaunaBarrensNpcsV1 790045) on Ashen Desert while the
    ///    objective is open, at 50% (no source gives a rate; the Crucible's 50-75%); Kwon takes the four at the turn-in; an
    ///    abandon keeps them (as the Crucible's drops). A marker (r20) on each of the two Mutated Atta pools.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40, the nearest icon number
    ///    where the band has none: Mech V04 Gloves 34116 (no V05 Uncommon gloves, as the Crucible's The Departed), Stealth V04
    ///    Legs 40502 (no V05), Reflective V03 Legs 36787, Motor Assist V07 Gloves 31088 (38-42; the band's highest, no V11); one
    ///    to choose.
    /// </summary>
    public static class AshenHarmfulMutationsV1
    {
        public const uint MissionId = 1243;
        public const uint Level = 40;
        public const uint Kuang = AshenMaunaBarrensNpcsV1.Kuang;
        public const uint Kwon = AshenMaunaBarrensNpcsV1.Kwon;
        public const uint Samples = 1;
        public const uint SampleCount = 4;
        public const int SampleChance = 50;
        public const uint SampleTemplate = 97458, SampleClass = 25409;
        public const string SampleKey = "mutated-atta-blood-samples";
        public const uint Mutated = AshenMaunaBarrensNpcsV1.MutatedSoldier;
        public const double MarkerRadius = 20;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint MechGloves = 34116;           // Armor_T3_Mech_V04_UNC_Gloves_40_to_44
        public const uint StealthLegs = 40502;          // Armor_T3_Stealth_V04_UNC_Legs_40_to_44
        public const uint ReflectiveLegs = 36787;       // Armor_T2_Reflective_V03_UNC_Legs_39_to_43
        public const uint MotorAssistGloves = 31088;    // Armor_T1_MotorAssist_V07_UNC_Gloves_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Harmful Mutations", 9925, Kuang, Kwon, Level);
            mission.Reward(Experience, Credits, new[] { MechGloves, StealthLegs, ReflectiveLegs, MotorAssistGloves });

            mission.Objective(Samples, 9930, 9931, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Samples);
            mission.Progress(Samples, MissionProgressEventKind.ItemAcquired, SampleClass, SampleCount);
            mission.Action(Samples, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, creature, at) in AshenMaunaBarrensNpcsV1.AttaPools)
                if (creature == Mutated)
                    mission.Indicator(Samples, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1243",
                "Native1 'Recover 4 Mutant Atta Blood Samples' 9930/9931, no counter; no objectiveconversation. Texts 9925-9929 " +
                "(Mauna Barrens; the turn-in is Kwon's). Item class25409 Mutated Atta Blood Samples (template97458). Names 9291 " +
                "Kuang, 9298 Kwon, 9320 Mutated Atta Soldier.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Harmful_Mutations",
                "Giver Lieutenant Simon Kuang, reward giver Doctor Jung Kwon (Shadow's Edge Post); no requirement; follow-up Eyes " +
                "on the Ground; 4 samples; 90000XP, 6150 credits; Titan Mech Armor Gloves 5, Luminar Stealth Armor Legs 5, " +
                "Wellcare Reflective Armor Legs 3, Dynamo Motor Assist Armor Gloves 11.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenHarmfulMutationsV1",
                "Level40 estimated. Kwon 790041 new by the Shadow's Edge hospital. New Mutated Atta Soldier 790045 (copy of " +
                "531098) in Mauna Barrens pools 790050-790051; a sample a personal 50% drop, taken at the turn-in, kept at an " +
                "abandon; a marker r20 on each pool. Items 34116, 40502, 36787, 31088, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Samples },
                Items = new()
                {
                    new MissionItemBinding(SampleKey, SampleTemplate, MissionItemScope.CharacterOwned, SampleCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: SampleCount,
                        Drop: new MissionItemDropDefinition(new[] { Mutated }, Samples, AshenMissionDataV1.MapContextId, SampleChance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
