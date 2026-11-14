using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Robotic Salvage Acquisitions (native mission 1267), Ligo Ashen Desert: Shinichiro Ito in Ashoka Settlement, the AFS
    /// logistician, fills a requisition order from Shadow's Edge after the AFS side of Formal Complaints: 15 salvage off the
    /// Boneyard's Nitroglazers and 15 off its Hominis Machina. The AFS twin of Latan's Robotic Salvage Recovery (1264).
    ///
    /// Native: texts 10158-10161, 10162 (missionconversation): log "Kill Bane Nitroglazers and Bane Hominis Machina and recover 15
    /// Intact Nitroglazer Salvage and 15 Intact Machina Salvage from them. Return to Shichiro Ito in Ashoka Settlement once you
    /// have filled the requisition order.", offer "Mr. Ashton thinks you can be trusted. ... everyone is talking about the way
    /// you handled yourself with Latan. ... We've already recovered as much salvage as we can from the Boneyard, so you'll have
    /// to kill some Nitros and Machina to get the parts. ...", turn-in "These parts are going to be vital in repairing the Mechs
    /// back at Shadow's Edge ...", reminder "The requisition order from command requires cybernetic and robotic parts. ...";
    /// objective 1 "Recover 15 Nitroglazer Salvage" 10185/10186, 2 "Recover 15 Machina Salvage" 10187/10188, no counters
    /// (missionobjective); no objective conversation. Items class 25444 Intact Nitroglazer Salvage (template 97465), 25445 Intact
    /// Machina Salvage (97466), the same as 1264's. Name 9272 Shinichiro Ito.
    ///
    /// Sourced: TaRapedia Robotic Salvage Acquisitions: giver and reward giver Shinichiro Ito; requirement "Dearly Departed ??";
    /// follow-up Weapon Salvage Acquisitions; 6,300 credits, XP "?"; one of Modification: Electric Resistance, Fire Resistance,
    /// Ice Resistance, Virulent Resistance. TaRapedia Dearly Departed (1285): leads to 1267. TaRapedia 1580: Scavenger (Latan's
    /// missions) or Logistician (Ito's), only one, by the side taken in Formal Complaints.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Offered after Dearly Departed (1285) and Formal Complaints (1254) are done and flag 251 (Formal Complaints' choice 2)
    ///    is at 1 (triage D1, as The Human Perspective 1258 and set 7's 1264): Dearly Departed is TaRapedia's requirement; the
    ///    flag is the side ("the way you handled yourself with Latan"); Formal Complaints done, as the flags stay at an abandon of
    ///    1254. In play the 251 chain (1258-1261-1285) already needs the flag and 1254: the three keep 1267 shut on any other
    ///    path too.
    ///  - Level 42 (D5: the chain comes after Dearly Departed, level 42, the Ashen chains' top). 95,000 XP (TaRapedia "?": as
    ///    Latan's twin 1264).
    ///  - The salvage as 1264's: a personal 75% drop off set 7's Nitroglazer 790240 or Hominis Machina 790241 on Ashen Desert
    ///    while its objective is open; 15 of each at most; Ito takes them at the turn-in; an abandon keeps them. The bindings are
    ///    this mission's own (the side is for good: a character holds 1264 or 1267, never both). A marker (r20) on each of set
    ///    7's four robot pools.
    ///  - "Modification" read as the grade [4] armour modules (as Fithik Infestation's Resist Fire): Armor Module: Resist Electric
    ///    [4] 123016, Resist Fire [4] 123026, Resist Ice [4] 123031, Resist Virulent [4] 123071 (client itemtemplatelanguage). One
    ///    to choose.
    /// </summary>
    public static class AshenRoboticAcquisitionsV1
    {
        public const uint MissionId = 1267;
        public const uint Level = 42;
        public const uint Ito = AshenFormalComplaintsNpcsV1.Ito;
        public const uint NitroSalvage = 1, MachinaSalvage = 2;
        public const uint Count = 15;
        public const int Chance = 75;
        public const uint NitroTemplate = AshenRoboticSalvageV1.NitroTemplate, NitroClass = AshenRoboticSalvageV1.NitroClass;
        public const uint MachinaTemplate = AshenRoboticSalvageV1.MachinaTemplate, MachinaClass = AshenRoboticSalvageV1.MachinaClass;
        public const string NitroKey = "ito-intact-nitroglazer-salvage", MachinaKey = "ito-intact-machina-salvage";
        public const double MarkerRadius = 20;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint ResistElectric = 123016;      // Armor Module: Resist Electric [4]
        public const uint ResistFire = 123026;          // Armor Module: Resist Fire [4]
        public const uint ResistIce = 123031;           // Armor Module: Resist Ice [4]
        public const uint ResistVirulent = 123071;      // Armor Module: Resist Virulent [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Robotic Salvage Acquisitions", 10158, Ito, Ito, Level);
            mission.Reward(Experience, Credits, new[] { ResistElectric, ResistFire, ResistIce, ResistVirulent });
            mission.Prerequisite(AshenDearlyDepartedV1.MissionId, 1);
            mission.Prerequisite(AshenFormalComplaintsV1.MissionId, 2);
            mission.FlagPrerequisite(AshenFormalComplaintsV1.YamamotoFlag, AshenFormalComplaintsV1.Chosen,
                "Formal Complaints choice 2, the AFS side (flag 251)", 3);

            mission.Objective(NitroSalvage, 10185, 10186, 0, MissionObjectiveState.Incomplete);
            mission.Transition(NitroSalvage);
            mission.Progress(NitroSalvage, MissionProgressEventKind.ItemAcquired, NitroClass, Count);
            mission.Action(NitroSalvage, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(MachinaSalvage, 10187, 10188, 1, MissionObjectiveState.Incomplete);
            mission.Transition(MachinaSalvage);
            mission.Progress(MachinaSalvage, MissionProgressEventKind.ItemAcquired, MachinaClass, Count);

            var marker = 1U;
            foreach (var (_, at) in AshenScavengerNpcsV1.NitroglazerPools)
                mission.Indicator(NitroSalvage, marker++, at.X, at.Y, at.Z, MarkerRadius);
            foreach (var (_, at) in AshenScavengerNpcsV1.MachinaPools)
                mission.Indicator(MachinaSalvage, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1267",
                "Native1 'Recover 15 Nitroglazer Salvage' 10185/10186, 2 'Recover 15 Machina Salvage' 10187/10188, no counters; no " +
                "objectiveconversation. Texts 10158-10162 (Ito's; the Boneyard's robots, 'the way you handled yourself with Latan'). " +
                "Items class25444 Intact Nitroglazer Salvage (template97465), class25445 Intact Machina Salvage (template97466). " +
                "Name 9272 Shinichiro Ito.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Robotic_Salvage_Acquisitions",
                "Giver/reward giver Shinichiro Ito (Ashoka); requirement 'Dearly Departed ??'; follow-up Weapon Salvage Acquisitions; " +
                "6300 credits, XP '?'; Modification: Electric Resistance, Fire Resistance, Ice Resistance, Virulent Resistance.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenRoboticAcquisitionsV1",
                "Offered after 1285 and 1254 done with flag251 at 1 (the AFS side). Level42, 95000XP estimated. Set 7's Nitroglazer " +
                "790240 and Hominis Machina 790241 (pools 790243-790246); a salvage a personal 75% drop, 15 of each, taken at the " +
                "turn-in, kept at an abandon; a marker r20 on each pool. Armour modules [4] 123016, 123026, 123031, 123071, one to " +
                "choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { NitroSalvage, MachinaSalvage },
                Items = new()
                {
                    new MissionItemBinding(NitroKey, NitroTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenScavengerNpcsV1.Nitroglazer }, NitroSalvage, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(MachinaKey, MachinaTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenScavengerNpcsV1.Machina }, MachinaSalvage, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
