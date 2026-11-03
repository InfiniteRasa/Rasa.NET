using System.Collections.Generic;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using static WarnetCavernsContent;

    /// <summary>
    /// The eight Warnet Caverns missions of client 1.16.5, in its own names, texts, objectives
    /// and conversation packages, as the Tabula Rasa wiki and Ten Ton Hammer tell them, on what
    /// WarnetCavernsContent places and the Game's WarnetCaverns runs:
    ///
    ///  - Reset the Research Stations (393), Information Spec. Sides: the way to the Research
    ///    Base Camp cleared, the camp cleared (Chelliak in it), then its three stations rebooted
    ///    (their conversations, packages 789-791: "REBOOT"). The specialists walk to the camp's
    ///    mouth and then to their posts in it as it goes (WarnetCaverns).
    ///  - Escort to Remote Station 3 (394), Brumley: "We're ready when you are" (package 143,
    ///    choice 2; choice 1 holds him), and he follows the player to Remote Station 3 at the
    ///    Dig Site through the Lower Passage.
    ///  - Create the Queen Lure (395), Brumley to Gooding: the metal container by the Dig Site's
    ///    sandbags, scent glands and digestive fluids off the caverns' warnets.
    ///  - Kill the Queen (396), Gooding to Brumley: the lure, given with the mission, placed at
    ///    the Nest's mouth calls the Queen and an attack; she dies, another attack, and the goo
    ///    covered object (the Eloh artifact) off her for each player on it.
    ///  - Kill the Light (1791), Captain LaFontaine at River-Base Krimm: Brazix's orders off him
    ///    in the Nest, radioed in. Kill the Light part II (1792) is offered by radio at that
    ///    reward: the Bane Bio-Bomb (a Bane pheromone canister) disarmed, back to LaFontaine.
    ///  - Scarecrow (1793), Sergeant Barrasso: five Miasma essences off the caverns' Miasma and
    ///    the pulsing meat off Joahg, the larger twin.
    ///  - Trophy Case (1794), Sergeant Mullen: fifteen trophies off Lake Elinor's warnets (the
    ///    Palisades'), fifteen off the caverns', Chelliak and Tozid killed.
    ///
    /// Rewards are Ten Ton Hammer's credits and kinds of gear (an injector gun, gloves or a helmet;
    /// legs, a helmet or a shotgun; a leech gun, boots or legs; a net gun, a leech gun or a pistol;
    /// a weapon or a healing disc; gloves; a weapon), the gear the quality-3 templates of the
    /// level band holding 18, a choice of one; experience as the Devil's Den's at the credits.
    /// </summary>
    public static class WarnetCavernsMissions
    {
        public const string Revision = "warnet_caverns_1_16_5";

        /// <summary>missioncategorylanguage: "Instance (Warnet Caverns)".</summary>
        public const uint Category = 10000043;

        public const uint ResetStations = 393, EscortToStation = 394, QueenLure = 395, KillTheQueen = 396;
        public const uint KillTheLight = 1791, KillTheLightII = 1792, Scarecrow = 1793, TrophyCase = 1794;
        public static readonly uint[] Missions = { ResetStations, EscortToStation, QueenLure, KillTheQueen, KillTheLight, KillTheLightII, Scarecrow, TrophyCase };

        /// <summary>The Krimm sergeants (MissionNpcCreaturePreloader).</summary>
        public const uint Barrasso = 510134, Mullen = 510135;

        /// <summary>The scenario events the Game raises, by mission: scenario 1 of each.</summary>
        public static class Events
        {
            public const uint Scenario = 1;
            public const uint WayCleared = 1, SiteCleared = 2;     // 393
            public const uint ArrivedAtStation = 1;                // 394
            public const uint LurePlaced = 1, QueenKilled = 2;     // 396
            public const uint BombDisarmed = 1;                    // 1792, the canister's use
        }

        /// <summary>How many of each the collectors want.</summary>
        public const uint GlandsWanted = 4, FluidsWanted = 3, EssenceWanted = 5, TrophiesWanted = 15;

        /// <summary>Drop chances, in percent.</summary>
        public const int GlandChance = 50, FluidChance = 40, EssenceChance = 50, TrophyChance = 60;

        /// <summary>The client's map marker names (missionobjectiveindicatorlanguage): Warnet Caverns Entrance, Research Station 3, Research Site, Warnet Queen, Bane Bio-Weapon, Miasma Emanations.</summary>
        public const uint EntranceMarker = 60, StationMarker = 61, ResearchSiteMarker = 69, QueenMarker = 351, BioWeaponMarker = 352, EmanationsMarker = 353;

        public static uint Unnamed(uint objective) => MissionIndicator.UnnamedFrom + objective;

        #region Rewards

        public const uint InjectionGun = 16673;     // Weapon_Avatar_InjectionGun_Electric_UNC_17_to_21
        public const uint MotorGloves = 28717;      // Armor_T1_MotorAssist_V04_UNC_Gloves_18_to_22
        public const uint MotorHelmet = 28870;      // Armor_T1_MotorAssist_V04_UNC_Helmet_18_to_22
        public const uint MotorLegs = 29023;        // Armor_T1_MotorAssist_V04_UNC_Legs_18_to_22
        public const uint MotorHelmetV05 = 29637;   // Armor_T1_MotorAssist_V05_UNC_Helmet_18_to_22
        public const uint EmpShotgun = 17720;       // Weapon_Avatar_Shotgun_v1_EMP_UNC_17_to_21
        public const uint PhysicalLeechGun = 47792; // Weapon_Avatar_LeechGun_Physical_UNC_14_to_18
        public const uint MotorBoots = 28564;       // Armor_T1_MotorAssist_V04_UNC_Boots_18_to_22
        public const uint MotorLegsV05 = 29791;     // Armor_T1_MotorAssist_V05_UNC_Legs_18_to_22
        public const uint NetGun = 16932;           // Weapon_Avatar_NetGun_Electric_UNC_16_to_20
        public const uint VirulentLeechGun = 15280; // Weapon_Avatar_LeechGun_Virulent_UNC_14_to_18
        public const uint EmpPistol = 13728;        // Weapon_Avatar_Pistol_EMP_UNC_15_to_19
        public const uint LaserShotgun = 17859;     // Weapon_Avatar_Shotgun_v1_Laser_UNC_17_to_21
        public const uint HealingDisc = 18313;      // Tool_Avatar_Healing_Disc_Direct_UNC_15_to_19
        public const uint MotorGlovesV05 = 29483;   // Armor_T1_MotorAssist_V05_UNC_Gloves_18_to_22
        public const uint LaserPistol = 13725;      // Weapon_Avatar_Pistol_Laser_UNC_15_to_19

        #endregion

        public static IEnumerable<string> Up()
        {
            var sql = new List<string>();
            ResetStationsRows(sql);
            EscortRows(sql);
            QueenLureRows(sql);
            KillTheQueenRows(sql);
            KillTheLightRows(sql);
            KillTheLightIIRows(sql);
            ScarecrowRows(sql);
            TrophyCaseRows(sql);
            return sql;
        }

        public static IEnumerable<string> Down() => InstanceMissionSql.Remove(Revision, Missions);

        private static InstanceMissionSql.RewardItem[] Choice(params uint[] templates)
        {
            var items = new InstanceMissionSql.RewardItem[templates.Length];
            for (var i = 0; i < templates.Length; i++)
                items[i] = new InstanceMissionSql.RewardItem(templates[i], 1);
            return items;
        }

        private static void OnEvent(InstanceMissionSql mission, uint objective, uint eventId, uint transition = 1) =>
            mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, eventId, counter: Events.Scenario, transition: transition);

        private static void Marker(InstanceMissionSql mission, MissionSceneDefinition scene, uint objective, uint id, Spot at, uint map, double radius)
        {
            mission.Indicator(objective, id, at.X, at.Y, at.Z, radius);
            scene.IndicatorMaps ??= new Dictionary<uint, Dictionary<uint, uint>>();
            if (!scene.IndicatorMaps.TryGetValue(objective, out var maps))
                scene.IndicatorMaps[objective] = maps = new Dictionary<uint, uint>();
            maps[id] = map;
        }

        private static MissionItemBinding Collected(string key, uint template, uint wanted, uint[] from, uint objective, uint map, int chance) =>
            new(key, template, MissionItemScope.CharacterOwned, wanted,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                TurnInQuantity: wanted, Drop: new MissionItemDropDefinition(from, objective, map, chance, 1));

        /// <summary>The way, the camp, the three stations; the stations' count is objective 1's.</summary>
        private static void ResetStationsRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ResetStations, "Reset the Research Stations", 534, Sides, Sides, 18, Category);
            mission.Reward(18000, 2700, selectableItems: Choice(InjectionGun, MotorGloves, MotorHelmet));
            mission.Objective(6, 6578, 6579, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 539, 6577, 1);
            mission.Objective(1, 538, 6580, 2, counterTextId: 1608);
            mission.Objective(3, 540, 6581, 3);
            mission.Objective(4, 541, 6582, 4);
            mission.Objective(5, 542, 6583, 5);
            mission.Scenario(Events.Scenario, "The way to the Research Base Camp and the camp cleared", Events.WayCleared, Events.SiteCleared);

            mission.Transition(6);
            OnEvent(mission, 6, Events.WayCleared);
            mission.RevealAndActivate(6, 2, 1);

            mission.Transition(2);
            OnEvent(mission, 2, Events.SiteCleared);
            mission.RevealAndActivate(2, 1, 1);
            mission.RevealAndActivate(2, 3, 3);
            mission.RevealAndActivate(2, 4, 5);
            mission.RevealAndActivate(2, 5, 7);

            var scene = Scene(events: true);
            scene.Dialogue = new List<MissionDialogueTopicDefinition>();
            scene.Sequences[0] = new SceneSequenceDefinition();

            for (uint i = 0; i < 3; i++)
            {
                var objective = 3 + i;
                var package = StationPackages[i];
                mission.Transition(objective);
                mission.Conversation(objective, package);
                if (i == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);

                scene.Dialogue.Add(new(objective, package, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = 1, [2] = 1 }));
                var role = $"station-{i + 1}";
                var at = Stations[i];
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, StationClass,
                    new ScenePosition((float)at.X, (float)at.Y, (float)at.Z), StationYaws[i],
                    Conversation: new SceneObjectConversation(ResetStations, objective, package, objective, 1, MissionDialogueKind.Choice));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                Marker(mission, scene, objective, Unnamed(objective), at, Map, 4);
            }

            scene.ObjectiveAggregations = new Dictionary<uint, MissionObjectiveAggregation>
            {
                [1] = new(new uint[] { 3, 4, 5 }, 3, 0)
            };

            Marker(mission, scene, 6, ResearchSiteMarker, SidesAtMouth, Map, 15);
            Marker(mission, scene, 2, ResearchSiteMarker, new Spot(55.0, 98.15, -60.0), Map, 40);

            Evidence(mission, "Information Spec. Sides (name2943) gives and takes it, at the entrance sandbags. Objective6 'Clear the way to the " +
                "Research Site' is event1 (the entrance tunnel's, first chamber's and camp mouth's warnets all down at once); objective2 " +
                "'Clear the Research Site' event2 (the big cavern's and Chelliak); then the three Scientific Remote Research Stations " +
                "(class7123) at the camp, their conversations packages789-791 (CHOICEBODY the station's error, CHOICE1 REBOOT), and " +
                "objective1 counts them as 'Research Stations Reset' (1608). The specialists walk to the camp's mouth and to their posts " +
                "(WarnetCaverns). Ten Ton Hammer: 2700 credits and a choice of injector gun, gloves or helmet.");
            mission.Enable(scene);
        }

        /// <summary>Brumley ready (choice 2), then following the player to Remote Station 3.</summary>
        private static void EscortRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, EscortToStation, "Escort to Remote Station 3", 543, Brumley, Brumley, 18, Category);
            mission.Reward(18000, 2700, selectableItems: Choice(MotorLegs, MotorHelmetV05, EmpShotgun));
            mission.Prerequisite(ResetStations);
            mission.Objective(2, 857, 6567, 0, MissionObjectiveState.Incomplete, counterTextId: 18357);
            mission.Objective(1, 547, 6566, 1);
            mission.Scenario(Events.Scenario, "Brumley at Remote Station 3", Events.ArrivedAtStation);

            // "We're ready when you are" (2) sets him going; "Hold on a moment" (1) leaves it open.
            mission.Transition(2, 1, sequence: 1);
            mission.Conversation(2, BrumleyPackage, transition: 1);
            mission.RevealAndActivate(2, 1, 1, 1);
            mission.Transition(2, 2, MissionObjectiveState.Incomplete, sequence: 2);
            mission.Conversation(2, BrumleyPackage, transition: 2);

            mission.Transition(1);
            OnEvent(mission, 1, Events.ArrivedAtStation);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = Scene(events: true);
            scene.Dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(2, BrumleyPackage, 1, MissionDialogueKind.Choice, choices: new Dictionary<int, uint> { [1] = 2, [2] = 1 })
            };
            Marker(mission, scene, 2, Unnamed(2), BrumleyAtPost, Map, 6);
            Marker(mission, scene, 1, StationMarker, RemoteStation, Map, 10);

            Evidence(mission, "Information Spec. Brumley (name2944, package143) gives and takes it. Objective2 'Talk to Brumley': CHOICE2 " +
                "'We're ready when you are.' completes it, CHOICE1 'Hold on a moment.' leaves it open. Objective1: he follows that player " +
                "(WarnetCaverns) from the camp through the Lower Passage to the Dig Site, and his arrival within 10 m of Remote Station 3 " +
                "(the client's Research Station 3 marker) is event1. The client's objective3 'tracker' is the server's and is left out. " +
                "Ten Ton Hammer: 2700 credits and a choice of legs, helmet or shotgun.");
            mission.Enable(scene);
        }

        /// <summary>The metal container from the Dig Site, glands and fluids off the warnets; to Gooding.</summary>
        private static void QueenLureRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, QueenLure, "Create the Queen Lure", 549, Brumley, Gooding, 18, Category);
            mission.Reward(18000, 2700, selectableItems: Choice(PhysicalLeechGun, MotorBoots, MotorLegsV05));
            mission.Prerequisite(EscortToStation);
            mission.Objective(1, 553, 6585, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 554, 6586, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 555, 6587, 2, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, MetalContainerClass, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, WarnetGlandsClass, target: GlandsWanted);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, DigestiveFluidsClass, target: FluidsWanted);

            var scene = Scene();
            scene.Items = new List<MissionItemBinding>
            {
                new("metal-container", MetalContainerTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, TurnInQuantity: 1),
                Collected("warnet-glands", WarnetGlandsTemplate, GlandsWanted, CavernsWarnets, 2, Map, GlandChance),
                Collected("digestive-fluids", DigestiveFluidsTemplate, FluidsWanted, CavernsWarnets, 3, Map, FluidChance)
            };
            Marker(mission, scene, 1, Unnamed(1), MetalContainerSpot, Map, 6);
            Marker(mission, scene, 2, Unnamed(2), new Spot(297.6, 48.95, 79.6), Map, 40);
            Marker(mission, scene, 3, Unnamed(3), new Spot(297.6, 48.95, 79.6), Map, 40);

            Evidence(mission, "Brumley gives it, Information Spec. Gooding (name2945; 'so she can make') takes it at the camp. Objective1 " +
                "'Collect a Metal Container': the metal container (UsableItemDispMetalcontainer class7130) by the Dig Site's sandbags gives " +
                $"the Shiney Metal Container (class7128, template473). Objectives2-3: {GlandsWanted} Scent Glands (class7125, template471) " +
                $"and {FluidsWanted} Warnet Digestive Fluids (class7127, template472) off the caverns' warnets, {GlandChance}% and " +
                $"{FluidChance}% a kill for each player on them. All go with the turn-in. Ten Ton Hammer: 2700 credits and a choice of " +
                "leech gun, boots or legs.");
            mission.Enable(scene);
        }

        /// <summary>The lure placed, the Queen killed, the goo covered object off her; to Brumley.</summary>
        private static void KillTheQueenRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, KillTheQueen, "Kill the Queen", 556, Gooding, Brumley, 19, Category);
            mission.Reward(25000, 3800, selectableItems: Choice(NetGun, VirulentLeechGun, EmpPistol));
            mission.Prerequisite(QueenLure);
            mission.Objective(2, 561, 6589, 0, MissionObjectiveState.Incomplete);
            mission.Objective(1, 560, 6590, 1);
            mission.Objective(4, 4446, 6591, 2);
            mission.Scenario(Events.Scenario, "The Queen Lure placed, the Queen killed", Events.LurePlaced, Events.QueenKilled);

            mission.Transition(2);
            OnEvent(mission, 2, Events.LurePlaced);
            mission.RevealAndActivate(2, 1, 1);
            mission.RevealAndActivate(2, 4, 3);

            mission.Transition(1);
            OnEvent(mission, 1, Events.QueenKilled);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.ItemAcquired, GooObjectClass, target: 1);

            var scene = Scene(events: true);
            scene.Items = new List<MissionItemBinding>
            {
                new("queen-lure", QueenLureTemplate, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                new("goo-covered-object", GooObjectTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1, Drop: new MissionItemDropDefinition(new[] { Queen }, 4, Map, 100, 1))
            };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("gooding-queen-lure", KillTheQueen, "queen-lure", QueenLureTemplate, 1)
            };
            Marker(mission, scene, 2, Unnamed(2), LureStand, Map, 5);
            Marker(mission, scene, 1, QueenMarker, QueenSpot, Map, 30);
            Marker(mission, scene, 4, QueenMarker, QueenSpot, Map, 30);

            Evidence(mission, "Gooding gives it with the Scented Lure for a Warnet Queen (class7129, template474); Brumley takes it. " +
                "Objective2 'Place the Lure': the Queen Lure stand (UsableItemAccQueenlure class7131) at the Nest's mouth, the wiki's red " +
                "staff, takes it (it stays with the mission) and is event1 for every player in the copy on it; the Queen Warnet Matriarch " +
                "(name8581, L19) comes, and an attack of level-19 warnets, as many as the squad in the cavern wants (WarnetCaverns). " +
                "Objective1 is event2, her death, for every player on it; another attack follows. Objective4 'Recover the Eloh Artifact': " +
                "the goo covered object (class7132, template475) off her for each player on it. Ten Ton Hammer: 3800 credits and a choice " +
                "of net gun, leech gun or pistol.");
            mission.Enable(scene);
        }

        /// <summary>Brazix's orders, radioed in.</summary>
        private static void KillTheLightRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, KillTheLight, "Kill the Light", 17916, LaFontaine, null, 19, Category, radioCompleteable: true);
            mission.Reward(18000, 2700);
            mission.Objective(1, 17921, 17922, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 17923, 17924, 1, required: false);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, OrdersClass, target: 1);
            mission.RevealAndActivate(1, 2, 1);
            mission.Action(1, 3, MissionActionKind.GrantReward, reward: 1);
            mission.Channels(MissionChannel.Npc, MissionChannel.Radio);

            var scene = Scene();
            scene.Items = new List<MissionItemBinding>
            {
                new("brazix-orders", OrdersTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1, Drop: new MissionItemDropDefinition(new[] { Brazix }, 1, Map, 100, 1))
            };
            Marker(mission, scene, 1, EntranceMarker, PalisadesMouth, Palisades, 15);
            Marker(mission, scene, 1, Unnamed(1), BrazixSpot, Map, 15);

            Evidence(mission, "Captain LaFontaine (name10219) at River-Base Krimm gives it; it is radioed in (17919 'Transmit the data you " +
                "found on the Lightbender immediately'). Objective1: Brazix's Orders (class28441, template118797) off Brazix (name10223, " +
                "L19) in the Nest; objective2 'Communicate with Captain LaFontaine' is the radio turn-in, shown and not required. Ten " +
                "Ton Hammer: Brazix at 236, 28, 255; 2700 credits, and Part 2 starts.");
            mission.Enable(scene);
        }

        /// <summary>The bio-bomb disarmed; offered by radio at Kill the Light's reward, back to LaFontaine.</summary>
        private static void KillTheLightIIRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, KillTheLightII, "Kill the Light part II", 17927, null, LaFontaine, 19, Category);
            mission.Reward(19000, 2850, selectableItems: Choice(LaserShotgun, HealingDisc));
            mission.Prerequisite(KillTheLight);
            mission.Objective(1, 17932, 17933, 0, MissionObjectiveState.Incomplete, counterTextId: 17934);
            mission.Scenario(Events.Scenario, "The Bane Bio-Bomb disarmed", Events.BombDisarmed);
            mission.Transition(1);
            OnEvent(mission, 1, Events.BombDisarmed);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Channels(MissionChannel.Radio, MissionChannel.Npc, MissionOfferSourceDefinition.MissionRewarded(KillTheLight));

            const string role = "bio-bomb";
            var scene = Scene(events: true);
            scene.MapContextId = Map;
            scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, BioBombClass,
                new ScenePosition((float)BioBomb.X, (float)BioBomb.Y, (float)BioBomb.Z), 0,
                InitialObjectState: IaActive, UseAction: new SceneObjectAction(KillTheLightII, 1, 1, 1));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("place-bio-bomb", role) } };
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("bio-bomb-disarmed", role, 121),
                    new SetInteractionIntent("bio-bomb-done", role, false)
                },
                Signals = { new SceneMissionSignal(KillTheLightII, Events.Scenario, Events.BombDisarmed) }
            };
            Marker(mission, scene, 1, BioWeaponMarker, BioBomb, Map, 8);

            Evidence(mission, "Offered by radio when Kill the Light is rewarded; LaFontaine takes it. Objective1 'Disarm the Bane Bio-Bomb' " +
                "(counter17934 'Bane Bio-Bomb disarmed'): a Bane Pheromone Canister (class24918) for each player at Ten Ton Hammer's " +
                "164, 33, 298 in the Nest, the client's Bane Bio-Weapon marker; a use disarms it. Ten Ton Hammer: 2850 credits and a " +
                "choice of weapon or healing disc.");
            mission.Enable(scene);
        }

        /// <summary>Five Miasma essences, Joahg's meat.</summary>
        private static void ScarecrowRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, Scarecrow, "Scarecrow", 17935, Barrasso, Barrasso, 19, Category);
            mission.Reward(19000, 2850, selectableItems: Choice(MotorGlovesV05));
            mission.Objective(1, 17952, 17953, 0, MissionObjectiveState.Incomplete);
            mission.Objective(3, 17954, 17955, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, MiasmaEssenceClass, target: EssenceWanted);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, MiasmaMeatClass, target: 1);

            var scene = Scene();
            scene.Items = new List<MissionItemBinding>
            {
                Collected("miasma-essence", MiasmaEssenceTemplate, EssenceWanted, CavernsMiasmas, 1, Map, EssenceChance),
                Collected("miasma-meat", MiasmaMeatTemplate, 1, new[] { Joahg }, 3, Map, 100)
            };
            Marker(mission, scene, 1, EntranceMarker, PalisadesMouth, Palisades, 15);
            Marker(mission, scene, 3, EmanationsMarker, JoahgSpot, Map, 12);

            Evidence(mission, "Sergeant Barrasso (name10220) at River-Base Krimm gives and takes it. Objective1: five Miasma Essence " +
                $"(class28444, template118800) off the caverns' Miasma, {EssenceChance}% a kill. Objective3: the Miasma meat " +
                "(class28445, template118801) off Joahg (name10224), the larger twin, where the client's Miasma Emanations marker is. " +
                "Ten Ton Hammer: Joahg at 148, 74, 117 (twins L18 and L19); 2850 credits and gloves.");
            mission.Enable(scene);
        }

        /// <summary>Fifteen Lake Elinor trophies, fifteen caverns trophies, Chelliak and Tozid.</summary>
        private static void TrophyCaseRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, TrophyCase, "Trophy Case", 17956, Mullen, Mullen, 19, Category);
            mission.Reward(19000, 1900, selectableItems: Choice(LaserPistol, PhysicalLeechGun, LaserShotgun));
            mission.Objective(1, 17961, 17962, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 17963, 17964, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 17965, 17966, 2, MissionObjectiveState.Incomplete, counterTextId: 17967);
            mission.Objective(4, 17968, 17969, 3, MissionObjectiveState.Incomplete, counterTextId: 17970);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, ElinorTrophyClass, target: TrophiesWanted);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, CavernsTrophyClass, target: TrophiesWanted);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.CreatureKilled, Chelliak, counter: 0, target: 1);
            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.CreatureKilled, Tozid, counter: 0, target: 1);

            var scene = Scene();
            scene.Items = new List<MissionItemBinding>
            {
                Collected("lake-elinor-trophies", ElinorTrophyTemplate, TrophiesWanted, new[] { PalisadesWarnet }, 1, Palisades, TrophyChance),
                Collected("caverns-trophies", CavernsTrophyTemplate, TrophiesWanted, CavernsWarnets, 2, Map, TrophyChance)
            };
            Marker(mission, scene, 2, EntranceMarker, PalisadesMouth, Palisades, 15);
            Marker(mission, scene, 3, Unnamed(3), new Spot(55.0, 98.15, -82.0), Map, 10);
            Marker(mission, scene, 4, Unnamed(4), new Spot(286.0, 49.66, 51.0), Map, 10);

            Evidence(mission, "Sergeant Mullen (name10221) at River-Base Krimm gives and takes it. Objective1: fifteen Lake Elinor Trophies " +
                $"(class28442, template118798) off the Palisades' warnets (creature{PalisadesWarnet}); objective2: fifteen caverns " +
                $"trophies (class28443, template118799) off the caverns' warnets, {TrophyChance}% a kill each; objectives3-4 Chelliak " +
                "(name10222) in the Research Base Camp and Tozid (name10111) in Bug City, out once the Hanging Hives are down. Ten Ton " +
                "Hammer: Chelliak L19 at 53, 98, -82, Tozid at 286, 49, 51; 1900 credits and a choice of weapon.");
            mission.Enable(scene);
        }

        /// <summary>A mission's binding, for the bootcamp's graduates; one whose events the Game raises has the data sequence.</summary>
        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://www.tentonhammer.com/guides/guide-to-warnet-caverns",
                "Ten Ton Hammer's Warnet Caverns and River-Base Krimm guides and the Tabula Rasa wiki's walkthrough: route, places, rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "WarnetCavernsMissions", reconstruction);
        }
    }
}
