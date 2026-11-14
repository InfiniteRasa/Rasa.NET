using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Blast the Biolab (native mission 1498), the Bane Conscription Facility (squad instance 1988): the Bane CommLink
    /// Terminal's voice sends the player for bomb materials and a Bane teleporter to bring an operative in; Slocombe makes the
    /// bomb, and the bomb destroys the Atta Laboratory.
    ///
    /// Native: texts 13384-13388 (missionconversation): title "Blast the Biolab", log "Gather bomb materials and secure a way to
    /// rendezvous with an operative.", offer "The security database shows that there are signs of Atta manipulation going on
    /// here. ... disable the Atta Laboratory. ... Search out explosive materials and secure a way for an operative to rendezvous
    /// with you.", turn-in "There is massive Atta activity being detected in the eastern wing of the facility. I'll hack into the
    /// system to see if I can find any details."; objectives (missionobjective) 1 "Gather Bio Materials" 14064/14065, 2 "Gather
    /// Mechanical Parts" 14066/14067, 3 "Activate Bane Teleporter" 14068/14069, 4 "Give Bomb Materials to Slocombe"
    /// 14070/14071, 5 "Use Bomb to Destroy Atta Lab" 14073/14074, 6 "Hidden - gathering stuff counter" 14061/14062 counter
    /// 14063 "hidden counter"; objective conversation (1498,4) package 1933 (Slocombe) variant 1, 14072 "Do you have the
    /// pieces? Hand 'em over and I'll cook you up a little something special...". Items class 26462 "Bane Bio Material"
    /// (template 111198, stack 5), 26461 "Bane Machine Parts" (111197, stack 5), 26459 "Detonator" (111195). Names 9745
    /// Slocombe. Indicator name 287 "Conscription Transport Terminal".
    ///
    /// Sourced: TaRapedia Blast the Biolab (0.5): giver Bane CommLink Terminal; requires Raid the Conscription Facility;
    /// follow-ups Into the Lion's Den, What Makes Them Tick; 95,000 XP, 8,400 credits; Vextronics Incendiary Staff, Cryogenic
    /// Net Gun, ChiTech Leech Gun, Teleract Laser Pistol; "most enemies can be bypassed, as many of the Atta and Bane are
    /// fighting each other" (the old revision, 01-2008).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; offered by radio (an object can not give a mission) as soon as Raid the Conscription Facility is rewarded,
    ///    and on arrival in the facility (as Crucible 1218's), each member on his own reward; turned in by radio (the turn-in
    ///    text is the terminal's voice); bound to the squad's copy. Requires 1497.
    ///  - 1 and 2 five of each (D16, no source gives counts): a personal drop while each is open, in the facility only, at 50 %
    ///    (as 1243's): Bio Material from the Caretakers (531011) and Linkers (531014), Machine Parts from the Thrax Soldiers,
    ///    Grenadiers and Technicians (531008-531010); held ones count (existing facts). 6 (hidden, the client's counter) counts
    ///    1 and 2; 7 (hidden, ours) opens 3 when 6 is done and puts the member's teleporter there, usable (none before 3).
    ///  - 3 the player's own Bane teleporter (UsableTwoStateBaneTeleporterV01 8044, dark 55, running 56; argument 1) on the
    ///    north side of the HQ's round hall (the public pad 921 to the lab's wing on its east side, AshenConscriptionEntryNpcsV1):
    ///    the use sends 3's event; 3 turns it on and brings the member's own Slocombe (OwnerOnly, invulnerable; no public one)
    ///    through, beside it.
    ///  - 4 the talk with Slocombe: 4 takes each member's five and five, then gives him the bomb (the Detonator, his own);
    ///    a member short of either is refused (his 4 stays open).
    ///  - 5 the player's own Explosive Charge (7870, argument 1) at the Atta Laboratory's door (the lab room's raised floor has
    ///    cracks at its edge the client floor checks do not pass), usable once 5 is open: the use sends
    ///    5's event; 5 takes the bomb, then sets the charge; the reward. No lab object is destroyed (the client map's lab has
    ///    none of the Bane's destructibles: docs/ashen-playtest.md).
    ///  - Markers: 1 on the Linkers' pool 531064 by the lab, 2 on the Thrax pool 531066 in the lab's chunnel, 3 the teleporter
    ///    (287), 4 Slocombe, 5 the charge. 6 and 7 hidden, none.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 43 (client entityclass names): Staff_Fire
    ///    49496 (40-44), NetGun_Ice 12129 (41-45), LeechGun_Physical 45763 (39-43), Pistol_Laser 14492 (40-44); one to choose.
    /// </summary>
    public static class AshenBlastTheBiolabV1
    {
        public const uint MissionId = 1498;
        public const uint Level = 43;
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint Slocombe = AshenConscriptionEntryNpcsV1.Slocombe, SlocombePackage = AshenConscriptionEntryNpcsV1.SlocombePackage;
        public const uint Variant = 1;

        public const uint BioMaterials = 1, MechanicalParts = 2, Teleporter = 3, GiveToSlocombe = 4, DestroyTheLab = 5, Gathered = 6,
            Opener = 7;

        public const uint BioTemplate = 111198, BioClass = 26462, PartsTemplate = 111197, PartsClass = 26461;
        public const uint BombTemplate = 111195, BombClass = 26459;
        public const uint EachCount = 5;               // D16, ESTIMATE
        public const int DropChance = 50;              // ESTIMATE
        public const string BioKey = "bcf-bio-material", PartsKey = "bcf-machine-parts", BombKey = "bcf-lab-bomb";

        /// <summary>The Caretaker and the Linker drop Bio Material; the Thrax Soldier, Grenadier and Technician Machine Parts.</summary>
        public static readonly uint[] BioCreatures = { 531011, 531014 };
        public static readonly uint[] PartsCreatures = { 531008, 531009, 531010 };
        public const uint BioPool = 531064, PartsPool = 531066;
        public static readonly (double X, double Y, double Z) BioPoolAt = (-167.8, 105, -146.2), PartsPoolAt = (-233.6, 103.9, -205.3);
        public const double BioPoolRadius = 30, PartsPoolRadius = 21.3;

        public const uint TeleporterClass = 8044;      // UsableTwoStateBaneTeleporterV01: dark 55, running 56
        public const uint TeleporterOff = 55, TeleporterOn = 56;
        public const uint ChargeClass = 7870;          // UsableBombHumV01 "Explosive Charge"
        public const uint UseArgument = 1;             // usabledata lookup[8044], [7870]: None
        public const string TeleporterRole = "bcf-bane-teleporter", SlocombeRole = "slocombe", ChargeRole = "bcf-lab-charge";

        /// <summary>Scenarios: the teleporter used (3's event), the charge used (5's event), Slocombe through (3 done), the teleporter
        /// usable (3 open), the charge usable (5 open), the charge set (5 done).</summary>
        public const uint TeleporterUsed = 1, ChargeUsed = 2, SlocombeComes = 3, TeleporterReady = 4, ChargeReady = 5, LabBlown = 6;
        public const uint TeleporterSequence = 11, ChargeSequence = 12;

        public const uint TeleporterIndicator = 287;
        public const double PoolMarkerPadding = 5, ObjectMarkerRadius = 6, NpcMarkerRadius = 8;    // ESTIMATE

        public static readonly ScenePosition TeleporterAt = new(7f, 135.94f, -358f);
        public static readonly ScenePosition SlocombeAt = new(1.5f, 135.94f, -359f);
        public static readonly ScenePosition ChargeAt = new(-216f, 96.01f, -160f);

        public const uint Experience = 95000;
        public const uint Credits = 8400;
        public const uint IncendiaryStaff = 49496;     // Weapon_Avatar_Staff_Fire_UNC_40_to_44
        public const uint CryogenicNetGun = 12129;     // Weapon_Avatar_NetGun_Ice_UNC_41_to_45
        public const uint LeechGun = 45763;            // Weapon_Avatar_LeechGun_Physical_UNC_39_to_43
        public const uint LaserPistol = 14492;         // Weapon_Avatar_Pistol_Laser_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Blast the Biolab", 13384, null, null, Level);
            mission.RadioOfferAndCompletion(MissionOfferSourceDefinition.MissionRewarded(AshenRaidTheConscriptionFacilityV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Facility));
            mission.Reward(Experience, Credits, new[] { IncendiaryStaff, CryogenicNetGun, LeechGun, LaserPistol });
            mission.Prerequisite(AshenRaidTheConscriptionFacilityV1.MissionId);
            mission.Scenario(TeleporterUsed, "The Bane teleporter used", 1);
            mission.Scenario(ChargeUsed, "The lab's charge used with the bomb", 1);
            mission.Scenario(SlocombeComes, "The teleporter on: Slocombe through (3 done)", 0);
            mission.Scenario(TeleporterReady, "The teleporter usable (3 open)", 0);
            mission.Scenario(ChargeReady, "The lab's charge usable (5 open)", 0);
            mission.Scenario(LabBlown, "The lab's charge set (5 done)", 0);

            mission.Objective(BioMaterials, 14064, 14065, 0, MissionObjectiveState.Incomplete);
            mission.Objective(MechanicalParts, 14066, 14067, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Teleporter, 14068, 14069, 2, MissionObjectiveState.Inactive);
            mission.Objective(GiveToSlocombe, 14070, 14071, 3, MissionObjectiveState.Inactive);
            mission.Objective(DestroyTheLab, 14073, 14074, 4, MissionObjectiveState.Inactive);
            mission.Objective(Gathered, 14061, 14062, 5, MissionObjectiveState.Incomplete, counterTextId: 14063, required: false);     // hidden
            mission.Objective(Opener, 14061, 14062, 6, MissionObjectiveState.Incomplete, required: false);                             // hidden

            // 1, 2: five of each held (drops while each is open; held ones count).
            mission.Transition(BioMaterials);
            mission.Progress(BioMaterials, MissionProgressEventKind.ItemAcquired, BioClass, EachCount);
            mission.Transition(MechanicalParts);
            mission.Progress(MechanicalParts, MissionProgressEventKind.ItemAcquired, PartsClass, EachCount);

            // 6 counts 1 and 2 (the scene's aggregation); 7, hidden: 6 done opens 3 and makes the teleporter usable.
            mission.Transition(Opener);
            mission.ObjectiveState(Opener, Gathered, MissionObjectiveState.Completed);
            AshenRaidTheConscriptionFacilityV1.Open(mission, Opener, 1, Teleporter);
            mission.Action(Opener, 3, MissionActionKind.StartScenario, scenario: TeleporterReady);

            // 3: the teleporter used: it comes on, the member's Slocombe comes through.
            mission.Transition(Teleporter);
            mission.Progress(Teleporter, MissionProgressEventKind.ScenarioEvent, 1, null, counter: TeleporterUsed);
            mission.Action(Teleporter, 1, MissionActionKind.StartScenario, scenario: SlocombeComes);
            AshenRaidTheConscriptionFacilityV1.Open(mission, Teleporter, 2, GiveToSlocombe);

            // 4: Slocombe's talk: he takes the five and five, then makes the bomb; 5 opens, the lab's charge usable.
            mission.Transition(GiveToSlocombe);
            mission.Conversation(GiveToSlocombe, SlocombePackage, Variant);
            mission.Action(GiveToSlocombe, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("slocombe-takes-the-bio-material", MissionId, BioKey, EachCount, MissionItemScope.CharacterOwned));
            mission.Action(GiveToSlocombe, 2, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("slocombe-takes-the-machine-parts", MissionId, PartsKey, EachCount, MissionItemScope.CharacterOwned));
            mission.Action(GiveToSlocombe, 3, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("slocombe-gives-the-bomb", MissionId, BombKey, BombTemplate, 1));
            AshenRaidTheConscriptionFacilityV1.Open(mission, GiveToSlocombe, 4, DestroyTheLab);
            mission.Action(GiveToSlocombe, 6, MissionActionKind.StartScenario, scenario: ChargeReady);

            // 5: the charge used: 5 takes the bomb, then sets the charge; the reward (the turn-in by radio).
            mission.Transition(DestroyTheLab);
            mission.Progress(DestroyTheLab, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ChargeUsed);
            mission.Action(DestroyTheLab, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("the-lab-charge-takes-the-bomb", MissionId, BombKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(DestroyTheLab, 2, MissionActionKind.StartScenario, scenario: LabBlown);
            mission.Action(DestroyTheLab, 3, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(BioMaterials, 1, BioPoolAt.X, BioPoolAt.Y, BioPoolAt.Z, BioPoolRadius + PoolMarkerPadding, Facility);
            mission.Indicator(MechanicalParts, 2, PartsPoolAt.X, PartsPoolAt.Y, PartsPoolAt.Z, PartsPoolRadius + PoolMarkerPadding, Facility);
            mission.NamedIndicator(Teleporter, TeleporterIndicator, TeleporterAt.X, TeleporterAt.Y, TeleporterAt.Z, ObjectMarkerRadius, Facility);
            mission.Indicator(GiveToSlocombe, 3, SlocombeAt.X, SlocombeAt.Y, SlocombeAt.Z, NpcMarkerRadius, Facility);
            mission.Indicator(DestroyTheLab, 4, ChargeAt.X, ChargeAt.Y, ChargeAt.Z, ObjectMarkerRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1498",
                "Native1 'Gather Bio Materials' 14064/14065, 2 'Gather Mechanical Parts' 14066/14067, 3 'Activate Bane Teleporter' " +
                "14068/14069, 4 'Give Bomb Materials to Slocombe' 14070/14071, 5 'Use Bomb to Destroy Atta Lab' 14073/14074, 6 'Hidden " +
                "- gathering stuff counter' 14061/14062 c14063; objectiveconversation 1498,4 pkg1933 14072. Texts 13384-13388. Items " +
                "Bane Bio Material 26462 (111198), Bane Machine Parts 26461 (111197), Detonator 26459 (111195). Name 9745 Slocombe. " +
                "Indicator 287.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Blast_the_Biolab",
                "Bane CommLink Terminal gives; requires Raid the Conscription Facility; follow-ups Into the Lion's Den, What Makes " +
                "Them Tick; 95000XP, 8400 credits; Vextronics Incendiary Staff, Cryogenic Net Gun, ChiTech Leech Gun, Teleract Laser " +
                "Pistol.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenBlastTheBiolabV1",
                "Level43; radio offer (1497 rewarded, arrival in 1988) and radio completion; bound to the squad's copy. 1/2 five " +
                "each, personal 50% drops in 1988 (bio: 531011, 531014; parts: 531008-531010), existing facts; hidden 6 counts 1 " +
                "and 2, hidden 7 opens 3. 3 a personal teleporter 8044 (55 -> 56) on the HQ hall's north side (7,135.94,-358): " +
                "Slocombe 790660 (OwnerOnly) comes; 4 his talk (pkg1933) takes 5+5 and gives the bomb 111195; 5 a personal charge " +
                "7870 at the Atta Laboratory's door (-216,96.01,-160) takes the bomb; the reward. Pads 921/922 (the HQ hall, the " +
                "lab's service room). Items 49496, 12129, 45763, 14492.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The drops and the bomb, the player's own teleporter, Slocombe and charge, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                HiddenObjectiveIds = new() { Gathered, Opener },
                ExistingFactObjectiveIds = new() { BioMaterials, MechanicalParts },
                ObjectiveAggregations = new()
                {
                    [Gathered] = new MissionObjectiveAggregation(new[] { BioMaterials, MechanicalParts }, 2)
                },
                Items = new()
                {
                    new MissionItemBinding(BioKey, BioTemplate, MissionItemScope.CharacterOwned, EachCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(BioCreatures, BioMaterials, Facility, DropChance, 1)),
                    new MissionItemBinding(PartsKey, PartsTemplate, MissionItemScope.CharacterOwned, EachCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(PartsCreatures, MechanicalParts, Facility, DropChance, 1)),
                    new MissionItemBinding(BombKey, BombTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Actors[TeleporterRole] = new SceneActorDefinition(TeleporterRole, SceneActorKind.Object, TeleporterClass, TeleporterAt,
                InitiallyInteractable: false, InitialObjectState: TeleporterOff,
                UseAction: new SceneObjectAction(MissionId, Teleporter, TeleporterSequence, UseArgument));
            scene.Actors[SlocombeRole] = new SceneActorDefinition(SlocombeRole, SceneActorKind.Creature, Slocombe, SlocombeAt,
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true }, OwnerOnly: true);
            scene.Actors[ChargeRole] = new SceneActorDefinition(ChargeRole, SceneActorKind.Object, ChargeClass, ChargeAt,
                InitiallyInteractable: false,
                UseAction: new SceneObjectAction(MissionId, DestroyTheLab, ChargeSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent($"create-{ChargeRole}", ChargeRole)
                }
            };
            // The uses: the events only (the world changes after the objective's own steps, the set 15 door's order).
            scene.Sequences[TeleporterSequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, TeleporterUsed, 1) } };
            scene.Sequences[ChargeSequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, ChargeUsed, 1) } };
            // The teleporter is made only here, when 3 opens (not in sequence 0): at an accept with five and five held, 7 opens 3
            // before sequence 0 runs, and a second Ensure journaled after this SetInteraction would make a rebuild (restart,
            // fresh or squad copy) drop it, the teleporter back unusable (audit; the validator forbids one key in two
            // sequences). AshenConscriptionEntryTests: an abandon, a new accept and a rebuild in a fresh copy.
            scene.Sequences[TeleporterReady] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent($"create-{TeleporterRole}", TeleporterRole),
                    new SetInteractionIntent("teleporter-usable", TeleporterRole, true)
                }
            };
            scene.Sequences[SlocombeComes] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("teleporter-on", TeleporterRole, TeleporterOn),
                    new SetInteractionIntent("teleporter-done", TeleporterRole, false),
                    new EnsureActorIntent("slocombe-comes-through", SlocombeRole)
                }
            };
            scene.Sequences[ChargeReady] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("lab-charge-usable", ChargeRole, true) }
            };
            scene.Sequences[LabBlown] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("lab-charge-set", ChargeRole, false) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
