using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Showdown (native mission 876), Maligo Base (squad instance 1830): Colonel Bosley radios the player who
    /// comes into Maligo: a Bane officer carries a keypass to the second level; kill him, take it, and radio back.
    ///
    /// Native: texts 4591-4594 and 8876 (missionconversation): offer "some of the officers have access to that
    /// second entry by way of keypasses ... The pass provides entry to a local teleporter somewhere to the West",
    /// turn-in "With that keypass in hand you'll now be able to access the second level. Have you found the
    /// teleporter to the West on Level 1 yet?"; objectives (missionobjective) 1 "Bane Officer located! Track down
    /// and eliminate him" 4671/8877, 2 "ITEM - Obtain Level 1 Access Keypass" 4841/8878; no counters, no objective
    /// conversation. Item: MaligoLevel1Pass (class 21052, template 42414).
    ///
    /// Sourced: TaRapedia Showdown: giver Radio, reward from Radio, follow-up Coup; 17,500 XP, 1,200 credits;
    /// "Watchman Zyrutha spawns at 232, 106. Kill him for the key." TR MMO DB: level 30. Ten Ton Hammer (Maligo
    /// lower level, 2008-03): the radio call comes on entering Maligo; Zyrutha stands behind the first laser beam
    /// force field and runs off; "Showdown will update by radio transmission with a reward of 4500 credits and
    /// choice of new helmet".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23):
    ///  - Offered by radio on arriving in Maligo (map arrival), turned in by radio.
    ///  - 1 on killing Watchman Zyrutha (PlateauMaligoBaseV1, behind the Level 1 beams); 2 on picking up his
    ///    keypass, a personal 100% drop while 2 is open. CHANGE LATER: in 2008 it dropped for every player in
    ///    Maligo. The keypass stays in the bags (the turn-in: "with that keypass in hand").
    ///  - The keypass teleporter is a local teleporter pair open to everybody (PlateauMaligoBaseV1). CHANGE LATER:
    ///    gate it by the keypass.
    ///  - Rewards: TaRapedia's 17,500 XP and 1,200 credits (TTH says 4,500 credits); TTH's choice of helmet read as
    ///    the four Uncommon V04 helmets of the 30-34 band, as Clean Slate's. Level 30 (TR MMO DB).
    ///    No indicators: instance.
    /// </summary>
    public static class PlateauShowdownV1
    {
        public const uint MissionId = 876;
        public const uint Level = 30;
        public const uint Officer = 1, Keypass = 2;

        public const uint Pass = 42414, PassClass = 21052;
        public const string PassKey = "maligo-level-1-keypass";

        public const uint Experience = 17500;
        public const uint Credits = 1200;
        public const uint BioHelmet = PlateauCleanSlateV1.BioHelmet, GravitonHelmet = PlateauCleanSlateV1.GravitonHelmet,
            MechHelmet = PlateauCleanSlateV1.MechHelmet, StealthHelmet = PlateauCleanSlateV1.StealthHelmet;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Showdown", 4591, null, null, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, GravitonHelmet, MechHelmet, StealthHelmet });
            mission.RadioOfferAndCompletion(new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent,
                MissionOfferSourceDefinition.MapArrivalKey, PlateauMaligoBaseV1.MapContextId));

            mission.Objective(Officer, 4671, 8877, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Keypass, 4841, 8878, 1, MissionObjectiveState.Incomplete);

            mission.Transition(Officer);
            mission.Progress(Officer, MissionProgressEventKind.CreatureKilled, PlateauMaligoBaseV1.Zyrutha, null);

            // The keypass is on his corpse: the kill (1) always comes first.
            mission.Transition(Keypass);
            mission.Progress(Keypass, MissionProgressEventKind.ItemAcquired, PassClass, 1);
            mission.Action(Keypass, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauMaligoBaseV1.MapContextId,
                ExistingFactObjectiveIds = new() { Keypass },
                Items = new()
                {
                    new MissionItemBinding(PassKey, Pass, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { PlateauMaligoBaseV1.Zyrutha }, Keypass, PlateauMaligoBaseV1.MapContextId, 100, 1))
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/876",
                "Native1 'Bane Officer located! Track down and eliminate him' 4671/8877, 2 'ITEM - Obtain Level 1 Access Keypass' " +
                "4841/8878; no counters, no objective conversation. Texts 4591-4594, 8876 (a local teleporter to the West). " +
                "MaligoLevel1Pass class21052/template42414. Watchman Zyrutha name10295.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Showdown",
                "Giver Radio, reward from Radio, follow-up Coup; 17500XP, 1200 credits; Watchman Zyrutha spawns at 232,106. TR MMO DB " +
                "level30. Ten Ton Hammer guide-to-maligo-base-lower-level: radio on entering, Zyrutha behind the first laser beams, " +
                "4500 credits and a choice of helmet.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauShowdownV1",
                "Level30. Radio offer on arriving in Maligo1830, radio turn-in. 1 kill Zyrutha650088 (level32, 3x HP, behind the " +
                "Level 1 beams); 2 his keypass, a personal 100% drop while 2 is open, kept. CHANGE LATER: squad-wide drop; the " +
                "keypass teleporter (local teleporters984/985) is open to all. Helmets 46019, 25079, 34205, 40333. Places " +
                "are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
