using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Array Of Hope (native mission 935), Plateau: once the player has the six Trinity Bridge Logos,
    /// Vogren's hologram sends them to the six Logos Conduits on Trinity Bridge; the conduits open a
    /// portal at the bridge's centre, and beyond it, in the Temporal Chamber (squad instance 2029), the
    /// Eloh hologram Veryin asks "Who comes here?" and takes the mission.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5030-5033 and 6292 (missionconversation); objective
    /// 1 "Activate Trinity Bridge." 9156/9157, no counter (missionobjective); objectiveconversation
    /// 935,1,1199 text 9158 "Who comes here?" (Veryin, PlateauTemporalChamberNpcsV1). The client has no
    /// conduit: the bridge has the Logos signs ArchElohLogosSign Past, Future, Dispersion, Distance, Far
    /// and Logos, 68-86 m round the centre (-120, 394, 161), where the portal is map_link 49.
    ///
    /// Sourced: TaRapedia Array of Hope: giver Vogren's Hologram, Vogren's Tomb; receiver the Eloh
    /// Hologram, Temporal Chamber; requirement the six Trinity Bridge Logos missions; 42,500 XP, 4,000
    /// credits, one of 3 Class VIII Standard Med Packs, 3 Class VIII Incendiary Grenades, 2 Class VIII
    /// Advanced Med Packs, 3 Class VIII EMP Grenades; "Activate the six conduits situated around Trinity
    /// Bridge, two on each bank and one on each of the side bridges. The portal is in the center of the
    /// middle bridge." TR MMO DB: level 30, 42,500 XP, 4,000 credits.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - The portal (map_link 49) stays open to all; the mission needs the conduits before Veryin talks.
    ///  - The conduits: six personal scene objects of UsableTwoStateElohHologramPlatform (20823), on the
    ///    bridge 3 m from each Logos sign towards the centre (at the navmesh's ground), each used once
    ///    (state 55 to 56, its effect 31282). A hidden tally (objective <see cref="Tally"/>) counts the six
    ///    and sets the player's flag <see cref="ConduitsFlag"/>; objective 1's talk to Veryin needs it.
    ///    The flag is kept on the character: one who abandons the mission after the six finds Veryin's
    ///    talk open at once when they take it again.
    ///  - The use argument 1 (the client's usabledata gives none for the class).
    ///  - Each Logos the player has is not checked at its conduit: the requirement holds all six.
    ///  - The items as the class VIII consumables of the server's other rewards (Consumable_Medpack_08B/08C,
    ///    Consumable_Grenade_Fire/EMP_08_40). One indicator on the bridge (the conduits and the portal).
    /// </summary>
    public static class PlateauArrayOfHopeV1
    {
        public const uint MissionId = 935;
        public const uint Level = 30;
        public const uint Hologram = PlateauVogrenNpcsV1.Hologram;
        public const uint Veryin = PlateauTemporalChamberNpcsV1.Veryin, VeryinPackage = PlateauTemporalChamberNpcsV1.VeryinPackage;
        public const uint Tally = 2;                    // hidden: six distinct conduits
        public const uint ConduitsFlag = 935001;
        public const uint ConduitClass = 20823;         // UsableTwoStateElohHologramPlatform
        public const uint UseArgument = 1, ReadyState = 55, UsedState = 56;

        public const uint Experience = 42500;
        public const uint Credits = 4000;
        public const uint StandardMedPack = 45064;      // Class VIII Standard Med Pack (Consumable_Medpack_08B_40)
        public const uint IncendiaryGrenade = 111039;   // Class VIII Incendiary Grenade (Consumable_Grenade_Fire_08_40)
        public const uint AdvancedMedPack = 45065;      // Class VIII Advanced Med Pack (Consumable_Medpack_08C_40)
        public const uint EmpGrenade = 111029;          // Class VIII EMP Grenade (Consumable_Grenade_EMP_08_40)

        /// <summary>The six missions of the Trinity Bridge Logos, all required.</summary>
        public static readonly uint[] Logos = { PlateauTrinityLogosPastV1.MissionId, 1047, 1048, 1049, 1051, 1057 };

        /// <summary>The conduits, by the six Logos signs, 3 m nearer the bridge's centre.</summary>
        public static readonly (string Sign, ScenePosition At)[] Conduits =
        {
            ("Past", new(-83.34f, 377.44f, 234.32f)),
            ("Future", new(-54f, 376.68f, 161f)),
            ("Dispersion", new(-83.34f, 377.48f, 87.68f)),
            ("Distance", new(-185f, 376.48f, 161f)),
            ("Far", new(-156.66f, 377.45f, 234.32f)),
            ("Logos", new(-156.66f, 377.24f, 87.68f)),
        };

        public static string ConduitRole(int index) => $"conduit-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Array Of Hope", 5030, Hologram, Veryin, Level);
            mission.Reward(Experience, Credits,
                new (uint, uint)[] { (StandardMedPack, 3), (IncendiaryGrenade, 3), (AdvancedMedPack, 2), (EmpGrenade, 3) },
                new (uint, uint)[0]);
            for (var i = 0; i < Logos.Length; i++)
                mission.Prerequisite(Logos[i], (uint)i + 1);

            mission.Objective(1, 9156, 9157, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 9156, 9157, 1, MissionObjectiveState.Incomplete);   // counts only the conduits
            mission.Scenario(1, "Six distinct Logos Conduits activated", (uint)Conduits.Length);

            // 1: Veryin, once the six conduits are on (ObjectiveRequirements).
            mission.Transition(1);
            mission.Conversation(1, VeryinPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            // The tally: six conduits, then the flag.
            mission.Transition(Tally);
            mission.Action(Tally, 1, MissionActionKind.SetPlayerFlag, playerFlag: ConduitsFlag, playerFlagValue: 1,
                comment: "The six Logos Conduits are on");

            mission.Indicator(1, 1, -120, 394, 161, 80);       // the conduits round the portal

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                HiddenObjectiveIds = new() { Tally }
            };
            scene.ObjectiveRequirements[1] = new FlagRequirement(ConduitsFlag, 1);
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Conduits.Length; index++)
            {
                var id = (uint)index + 1;
                var role = ConduitRole(index);
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, (uint)Conduits.Length, counter: 1, trigger: id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ConduitClass, Conduits[index].At,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Tally, id, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"activate-{role}", role, UsedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, 1, id) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/935",
                "Native1 'Activate Trinity Bridge.' 9156/9157, no counter; objectiveconversation 935,1,1199 text9158. Texts " +
                "5030-5033, 6292. No conduit in the client; Logos signs Past/Future/Dispersion/Distance/Far/Logos 68-86m round " +
                "the bridge centre (-120,394,161), portal map_link49 to the Temporal Chamber 2029.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Array_of_Hope",
                "Giver Vogren's Hologram, receiver the Eloh Hologram (Temporal Chamber); requirement the six Trinity Bridge " +
                "Logos; 42500XP, 4000 credits, one of 3 Class VIII Standard Med Packs/3 Incendiary Grenades/2 Advanced Med " +
                "Packs/3 EMP Grenades; six conduits, two on each bank, one on each side bridge. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauArrayOfHopeV1",
                "Level30. Portal open to all. Six personal conduits (class20823, use argument1) 3m inside the six signs; a " +
                "hidden tally sets flag935001, which Veryin's talk needs. Items 45064x3, 111039x3, 45065x2, 111029x3.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
