using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Divided We Fall (native mission 917), Valverde Marshes: Elder Toida in Paludos asks the player to find her four
    /// siblings among the Forean refugees and send them to Fort Defiance.
    ///
    /// Native: texts 4899-4902 and 7104 (missionconversation): log "Locate Ranger Alia, Shaman Siva, Warrior Veska, and
    /// Ranger Helka among the Forean refugee trains. Speak with them to arrange a meeting in Fort Defiance, then report back
    /// to Elder Toida in Paludos.", offer "my own family has been divided and separated. Please, locate my siblings among the
    /// refugees", turn-in "Helka's fate saddens me, yet I am happy to know that Alia at least is sensible in heading to Fort
    /// Defiance."; objectives 1 "Locate Ranger Alia" 4958/7105, 2 "Locate Shaman Siva" 4960/7106, 3 "Locate Warrior Veska"
    /// 4962/7107, 4 "Locate Ranger Helka" 4964/7108, no counters (missionobjective); objectiveconversation (917,1) package
    /// 985 text 4959 "Meet my family in Fort Defiance? I can manage that.", (917,2) package 986 text 4961 (Siva stays for
    /// the wounded), (917,3) package 984 text 4963 (Veska stays to fight); none for Helka. Item 21564 Forean Insignia
    /// (MisMarshesItemDividedInsignia) is in the client but named by no text of the mission.
    ///
    /// Sourced: TaRapedia Divided We Fall: giver and receiver Elder Toida (Paludos); level 35; 65,000 XP, 5,400 credits; one
    /// of Astra Hazmat Armor Boots lvl 34, Titan Motor Assist Armor Boots lvl 33, Prodigy Reflective Armor Boots lvl 34,
    /// Titan Stealth Armor Legs lvl 35; Alia on the road Plateau Point - Falcon Hold, Siva (-291, 218, 25), Veska (-38, 219,
    /// 49), Helka "a rotting Forean corpse" at (-564, 217, -53). TR MMO DB: level 33. The NPCs: MarshesPaludosNpcsV1.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Objectives 1-3 by the client's talks at the siblings' packages, any order.
    ///  - Objective 4 (no client talk; the user-approved triage Q17): Helka's body is the player's own, a Forean warrior
    ///    corpse model (21478 PropForeanWarriorCorpseV01, takes no target) with a pack on it (class 11096
    ///    UsableItemDisp_item_bag_hum_v01, as Pools 1315's bodies, mesh 17714); one use of the pack (the server's 10 s use)
    ///    finds her, closes the pack and completes 4. The Forean Insignia is not used.
    ///  - Level TaRapedia's. The items the lowest Uncommon templates of the bands that hold those levels (the manufacturer
    ///    names are not in the client tables), one to choose. Markers: Alia's whole beat, Siva, Veska, Helka.
    /// </summary>
    public static class MarshesDividedWeFallV1
    {
        public const uint MissionId = 917;
        public const uint Level = 35;
        public const uint Toida = MarshesWelcomeWagonV1.Toida;
        public const uint LocateAlia = 1, LocateSiva = 2, LocateVeska = 3, LocateHelka = 4;
        public const uint HelkaFound = 1, HelkaSequence = 1;    // the scenario and the pack's use sequence

        public const uint CorpseClass = 21478;          // PropForeanWarriorCorpseV01, no use
        public const uint PackClass = 11096;            // UsableItemDisp_item_bag_hum_v01, the use
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const string CorpseRole = "helka-corpse", PackRole = "helka-pack";
        public static readonly ScenePosition HelkaAt = new(-564f, 217.7f, -53f);
        public const double ObjectRadius = 8;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint HazmatBoots = 20335;          // Armor_T2_Hazmat_V02_UNC_Boots_34_to_38
        public const uint MotorAssistBoots = 28614;     // Armor_T1_MotorAssist_V04_UNC_Boots_33_to_37
        public const uint ReflectiveBoots = 35571;      // Armor_T2_Reflective_V02_UNC_Boots_34_to_38
        public const uint StealthLegs = 39191;          // Armor_T3_Stealth_V02_UNC_Legs_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Divided We Fall", 4899, Toida, Toida, Level);
            mission.Reward(Experience, Credits, new[] { HazmatBoots, MotorAssistBoots, ReflectiveBoots, StealthLegs });

            mission.Objective(LocateAlia, 4958, 7105, 0, MissionObjectiveState.Incomplete);
            mission.Objective(LocateSiva, 4960, 7106, 1, MissionObjectiveState.Incomplete);
            mission.Objective(LocateVeska, 4962, 7107, 2, MissionObjectiveState.Incomplete);
            mission.Objective(LocateHelka, 4964, 7108, 3, MissionObjectiveState.Incomplete);
            mission.Scenario(HelkaFound, "Ranger Helka's body found", 1);

            mission.Transition(LocateAlia);
            mission.Conversation(LocateAlia, MarshesPaludosNpcsV1.AliaPackage);
            mission.Action(LocateAlia, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(LocateSiva);
            mission.Conversation(LocateSiva, MarshesPaludosNpcsV1.SivaPackage);
            mission.Transition(LocateVeska);
            mission.Conversation(LocateVeska, MarshesPaludosNpcsV1.VeskaPackage);
            mission.Transition(LocateHelka);
            mission.Progress(LocateHelka, MissionProgressEventKind.ScenarioEvent, 1, null, counter: HelkaFound);

            var centre = MarshesPaludosNpcsV1.AliaBeatCentre;
            mission.Indicator(LocateAlia, 1, centre.X, centre.Y, centre.Z, MarshesPaludosNpcsV1.AliaBeatRadius);
            var siva = MarshesPaludosNpcsV1.SivaAt;
            mission.Indicator(LocateSiva, 2, siva.X, siva.Y, siva.Z, ObjectRadius);
            var veska = MarshesPaludosNpcsV1.VeskaAt;
            mission.Indicator(LocateVeska, 3, veska.X, veska.Y, veska.Z, ObjectRadius);
            mission.Indicator(LocateHelka, 4, HelkaAt.X, HelkaAt.Y, HelkaAt.Z, ObjectRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/917",
                "Native1 'Locate Ranger Alia' 4958/7105, 2 'Locate Shaman Siva' 4960/7106, 3 'Locate Warrior Veska' 4962/7107, 4 " +
                "'Locate Ranger Helka' 4964/7108, no counters; objectiveconversation 917,1 package985 text4959, 917,2 package986 " +
                "text4961, 917,3 package984 text4963, none for 4. Texts 4899-4902, 7104. Names 8601 Alia, 8602 Siva, 8600 Veska, " +
                "8603 Helka. Item 21564 Forean Insignia (no text names it).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Divided_We_Fall",
                "Giver/receiver Elder Toida (Paludos); level35; 65000XP, 5400 credits; one of Astra Hazmat Armor Boots34, Titan " +
                "Motor Assist Armor Boots33, Prodigy Reflective Armor Boots34, Titan Stealth Armor Legs35; Alia on the road Plateau " +
                "Point - Falcon Hold (history: -381, 671 map view), Siva -291, 218, 25, Veska -38, 219, 49, Helka a rotting Forean " +
                "corpse -564, 217, -53. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesDividedWeFallV1",
                "1-3 by the client talks, any order; Alia 760300 an NPC squad of one on 4 road corners round (-381, 511), Siva " +
                "760301, Veska 760302 (classes 7036/7035/7034, level35, estimated). 4 by one use of a personal pack (11096) on a " +
                "Forean warrior corpse model (21478) at TaRapedia's place; insignia not used. Items 20335, 28614, 35571, 39191 " +
                "(lowest Uncommon of each kind and band), one to choose.", 0.3);

            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = MarshesMissionDataV1.MapContextId };
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, HelkaAt,
                Orientation: 2.2, InitiallyInteractable: false);
            scene.Actors[PackRole] = new SceneActorDefinition(PackRole, SceneActorKind.Object, PackClass, HelkaAt,
                Orientation: 2.2, InitialObjectState: ReadyState,
                UseAction: new SceneObjectAction(MissionId, LocateHelka, HelkaSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("create-helka-corpse", CorpseRole), new EnsureActorIntent("create-helka-pack", PackRole) }
            };
            scene.Sequences[HelkaSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("helka-pack-searched", PackRole, TakenState),
                    new SetInteractionIntent("helka-pack-done", PackRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, HelkaFound, 1) }
            };

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
