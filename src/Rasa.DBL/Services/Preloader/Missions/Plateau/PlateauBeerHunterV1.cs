using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Beer Hunter (native mission 855), Plateau: Field Lt. Peterson throws a bachelor party; Bartender
    /// McLaughlin at the Fort Defiance Tavern brews its beer from Saccharo Root and needs ten.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4429-4432 and 6293 (missionconversation); objectives
    /// 1 "Speak to Bartender McLaughlin." 4433/6294, 2 "Gather Saccharo Roots." 4435/6295 ("Gather ten
    /// Saccharo Roots from the plants growing around the perimeter of the Eloh Forum near Trinity
    /// Bridge."), 3 "Return to Bartender McLaughlin." 4436/6296, 4 "Inform Lt. Peterson." 4437/6297, no
    /// counters (missionobjective); McLaughlin's lines on package 629 for objectives 1 and 3 (4434, 4439),
    /// Peterson's on 385 for objective 4 (4438) (objectiveconversation, flag 1). The root is class 20787
    /// MisPlateauItemSaccharoRoot (template 42271); the plant class 20786 UsableItemDispSaccharoPlantDELETE
    /// "Saccharo Plant", an item dispenser (augmentation 34) with the root's own model. The client's map
    /// places no plant. McLaughlin is creature 510173, Peterson 510182.
    ///
    /// Sourced: TaRapedia The Beer Hunter (stub): giver and receiver Peterson, requirement A Bridge Too
    /// Far, 40,000 XP, 4,650 credits, one of lvl 30 Vextronics Pulse Disperser, lvl 27 ChiTech Cryogenic
    /// Injector Gun, lvl 29 Vextronics Incendiary Leech Gun, lvl 27 Animatics Incendiary Shotgun; "Saccharo
    /// Roots are little green circular plants that can be found north of Trinity Bridge. They take quite
    /// a while to loot". TR MMO DB: level 30. Ten Ton Hammer: "I found all mine near -136, 373, 436".
    ///
    /// Reconstructed (ESTIMATE): ten personal Saccharo Plants round Ten Ton Hammer's place, north of the
    /// Eloh Colossus, on the ground (each within 0.5 m of the navmesh), usable only while objective 2 is
    /// open; each use takes the server's 10 s object use, gives one root and closes that plant. The client
    /// has no counter text for objective 2, so a hidden tally (objective <see cref="Tally"/>, objective 2's
    /// texts) counts ten distinct plants, as Toxic Detox's pumps; objective 2 completes with it. Objective 2 shows the
    /// roots held as its item counter ("n/10" with the root's name, as Herbal Remedy's; Plateau to-fix #38).
    /// McLaughlin takes the ten roots at objective 3.
    /// The weapons as the Uncommon base templates of those bands, no manufacturer.
    /// </summary>
    public static class PlateauBeerHunterV1
    {
        public const uint MissionId = 855;
        public const uint Level = 30;
        public const uint Peterson = PlateauFortDefianceNpcsV1.Peterson, PetersonPackage = PlateauFortDefianceNpcsV1.PetersonPackage;
        public const uint McLaughlin = PlateauFortDefianceNpcsV1.McLaughlin, McLaughlinPackage = PlateauFortDefianceNpcsV1.McLaughlinPackage;
        public const uint RootTemplate = 42271, RootClass = 20787, Roots = 10;
        public const string RootKey = "saccharo-root";
        public const uint PlantClass = 20786;           // UsableItemDispSaccharoPlantDELETE
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public const uint Tally = 5;                    // hidden: ten distinct plants

        /// <summary>The plants, north of Trinity Bridge round (-136, 436).</summary>
        public static readonly ScenePosition[] Plants =
        {
            new(-170, 374.37f, 425),
            new(-160, 373.87f, 440),
            new(-150, 374.12f, 455),
            new(-136, 373.87f, 436),
            new(-140, 374.55f, 465),
            new(-130, 373.37f, 455),
            new(-110, 372.87f, 440),
            new(-100, 372.37f, 455),
            new(-90, 374.37f, 430),
            new(-170, 372.87f, 440),
        };

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint PulseDisperser = 17289;       // Weapon_Avatar_PropellantGun_EMP_UNC_30_to_31 "Pulse Disperser"
        public const uint CryogenicInjectorGun = 48100; // Weapon_Avatar_InjectionGun_Ice_UNC_27_to_31
        public const uint IncendiaryLeechGun = 47724;   // Weapon_Avatar_LeechGun_Fire_UNC_29_to_33
        public const uint IncendiaryShotgun = 17777;    // Weapon_Avatar_Shotgun_v1_Fire_UNC_27_to_31

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "The Beer Hunter", 4429, Peterson, Peterson, Level);
            mission.Reward(Experience, Credits, new[] { PulseDisperser, CryogenicInjectorGun, IncendiaryLeechGun, IncendiaryShotgun });
            mission.Prerequisite(PlateauBridgeTooFarV1.MissionId);

            mission.Objective(1, 4433, 6294, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 4435, 6295, 1, MissionObjectiveState.Inactive);
            mission.Objective(3, 4436, 6296, 2, MissionObjectiveState.Inactive);
            mission.Objective(4, 4437, 6297, 3, MissionObjectiveState.Inactive);
            mission.Objective(Tally, 4435, 6295, 4, MissionObjectiveState.Incomplete);   // counts only plants, usable at 2
            mission.Scenario(1, "Ten distinct Saccharo Plants harvested", (uint)Plants.Length);

            // 1: McLaughlin needs roots.
            mission.Transition(1);
            mission.Conversation(1, McLaughlinPackage);
            Open(mission, 1, 1, 2);

            // 2: ten roots, one from each plant, once the tally has them.
            mission.Transition(2);
            mission.ObjectiveState(2, Tally, MissionObjectiveState.Completed);
            Open(mission, 2, 1, 3);
            mission.Transition(Tally);

            // 3: McLaughlin brews with them.
            mission.Transition(3);
            mission.Conversation(3, McLaughlinPackage);
            mission.Action(3, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("mclaughlin-takes-roots", MissionId, RootKey, Roots, MissionItemScope.AssignmentIssued));
            Open(mission, 3, 2, 4);

            // 4: Peterson hears the party is on.
            mission.Transition(4);
            mission.Conversation(4, PetersonPackage);
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, 19, 399.59, 925);          // McLaughlin in the tavern
            mission.Indicator(2, 2, -135, 373.9, 445, 50);     // the plants
            mission.Indicator(3, 3, 19, 399.59, 925);
            mission.Indicator(4, 4, -66, 401.12, 931);         // Peterson at High Command

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(RootKey, RootTemplate, MissionItemScope.AssignmentIssued, Roots,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, CounterObjectiveId: 2)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Plants.Length; index++)
            {
                var id = (uint)index + 1;
                var role = $"plant-{id}";
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, Roots, counter: 1, trigger: id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PlantClass, Plants[index],
                    Orientation: index * 0.6283185307, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(MissionId, 2, id, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"harvest-{role}", role, TakenState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"root-from-{role}", MissionId, RootKey, RootTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, 1, id) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/855",
                "Native1 McLaughlin 4433/6294, 2 ten Saccharo Roots 4435/6295, 3 McLaughlin 4436/6296, 4 Peterson 4437/6297, no " +
                "counters; objectiveconversation 855,1 and 855,3 package629, 855,4 package385. Texts 4429-4432, 6293. Root " +
                "class20787/template42271; plant class20786 (augmentation34). No plant on the map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Beer_Hunter",
                "Stub. Giver/receiver Field Lt. Peterson; requirement A Bridge Too Far; 40000XP, 4650 credits; one of lvl30 " +
                "Vextronics Pulse Disperser, lvl27 ChiTech Cryogenic Injector Gun, lvl29 Vextronics Incendiary Leech Gun, lvl27 " +
                "Animatics Incendiary Shotgun; the plants north of Trinity Bridge, slow to loot. Ten Ton Hammer: near -136,373,436.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBeerHunterV1",
                "Ten personal plants (class20786) round -136,436 on the ground, usable while 2 is open, 10 s a use, one root " +
                "each; hidden tally5 counts ten distinct plants, 2 completes with it; McLaughlin takes the ten at 3. Weapons as Uncommon base templates: 17289, 48100, " +
                "47724, 17777. McLaughlin moved to the tavern (PlateauFortDefianceNpcsV1).", 0.3);

            mission.Enable(scene);
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PlateauMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
