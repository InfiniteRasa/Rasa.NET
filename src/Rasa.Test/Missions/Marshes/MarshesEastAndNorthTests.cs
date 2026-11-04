using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;
using Rasa.Packets;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesEscortsTests;
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// The Marshes set 12 (east and north) as the game server plays them: Bring Down the Kael (542: Toida, the prison caravan
    /// on the Long Walk), Bringing the Spirit Home (864: Bargas, the Forean Machina, the altars at the Temple of Paludos), Head
    /// East, Young Man (920: Figgins, Shaw's defence of the eastern guard station, Meeko) and Soyuz Salvage: Marshes (2009:
    /// Starling, the debris).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesEastAndNorthTests
    {
        private const uint Kael = MarshesBringDownTheKaelV1.MissionId;
        private const uint Spirit = MarshesBringingTheSpiritHomeV1.MissionId;
        private const uint HeadEast = MarshesHeadEastV2.MissionId;
        private const uint Soyuz = MarshesSoyuzSalvageV1.MissionId;
        private const uint Toida = MarshesWelcomeWagonV1.Toida;
        private const uint Starling = MarshesCannisterCollectionV1.Starling;
        private const uint Figgins = MarshesRetreadCityNpcsV1.Figgins;
        private const uint Bargas = MarshesEastAndNorthNpcsV1.Bargas;

        private static readonly Vector3 FalconHoldWaypoint = new(-143.27f, 219.05f, 581.54f);
        private static readonly Vector3 PaludosWaypoint = new(-304.35f, 218.3f, 97.36f);
        private static readonly Vector3 BaneAssertionCampWaypoint = new(130.77f, 218.3f, -312.8f);
        private static readonly Vector3 ResearchPointWaypoint = new(-343.45f, 216.15f, -279.6f);
        private static readonly Vector3 Road = new(-420f, 216.9f, 472.4f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Bargas on the temple's upper floor (a navmesh island, his altars walked to from him) and Shaw at the guard station
        /// stand with the client's names; the Machina pools are walked to from Falcon Hold; each mission is offered only by
        /// its giver, with no requirement.
        /// </summary>
        [TestMethod]
        public void TheGiversAndTheNewPeopleStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var bargas = Npc(marshes, Bargas);
            Assert.AreEqual((MarshesEastAndNorthNpcsV1.BargasName, MarshesEastAndNorthNpcsV1.BargasPackage), (bargas.NameId, bargas.Npc.NpcPackageId));
            Assert.AreNotEqual(TargetCategory.Hostile, bargas.TargetCategory);
            AssertOnGround(marshes, bargas.Position, "Bargas");
            Assert.IsFalse(marshes.NavMesh.FindPath(PaludosWaypoint, bargas.Position, out var fromPaludos) != null && fromPaludos,
                "the temple's upper floor is an island");
            foreach (var (_, _, at) in MarshesBraziersOnDryFloorV1.Moves)
            {
                AssertOnGround(marshes, ToVector(at), "an altar");
                Assert.IsTrue(marshes.NavMesh.FindPath(bargas.Position, ToVector(at), out var walked) != null && walked, "walked to from Bargas");
                Assert.IsLessThan(MarshesBringingTheSpiritHomeV1.AltarMarkerRadius,
                    Vector2.Distance(new(at.X, at.Z), new(MarshesBringingTheSpiritHomeV1.AltarMarker.X, MarshesBringingTheSpiritHomeV1.AltarMarker.Z)),
                    "inside the altar marker");
            }

            var shaw = Npc(marshes, MarshesEastAndNorthNpcsV1.Shaw);
            Assert.AreEqual((MarshesEastAndNorthNpcsV1.ShawName, MarshesEastAndNorthNpcsV1.ShawPackage), (shaw.NameId, shaw.Npc.NpcPackageId));
            AssertOnGround(marshes, shaw.Position, "Shaw");
            Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, shaw.Position, out var toShaw) != null && toShaw, "Shaw is walked to");

            Spawn(marshes, MarshesEastAndNorthNpcsV1.NorthMachinaPool, MarshesEastAndNorthNpcsV1.EastMachinaPool);
            foreach (var at in new[] { MarshesEastAndNorthNpcsV1.NorthMachinaAt, MarshesEastAndNorthNpcsV1.EastMachinaAt })
            {
                var pool = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
                var machinas = Alive(marshes).Where(creature => creature.DbId == MarshesEastAndNorthNpcsV1.Machina &&
                    Vector3.Distance(creature.Position, pool) < 45).ToArray();
                Assert.HasCount((int)MarshesRetreadArenaNpcsV1.Machinas, machinas, $"three at {pool}: " +
                    string.Join(" ", Alive(marshes).Where(creature => creature.DbId == MarshesEastAndNorthNpcsV1.Machina).Select(creature => creature.Position)));
                foreach (var machina in machinas)
                {
                    Assert.AreEqual(TargetCategory.Hostile, machina.TargetCategory);
                    AssertOnGround(marshes, machina.Position, "a Machina");
                }
                Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, pool, out var walked) != null && walked, $"{pool} walked to");
            }

            var toida = Npc(marshes, Toida);
            var starling = Npc(marshes, Starling);
            var figgins = Npc(marshes, Figgins);
            foreach (var (giver, mission) in new[] { (toida, Kael), (bargas, Spirit), (figgins, HeadEast), (starling, Soyuz) })
            {
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, giver, mission), $"{mission} needs nothing");
                foreach (var other in new[] { toida, bargas, figgins, starling }.Where(npc => npc != giver))
                    Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, other, mission), $"{mission} not {other.DbId}'s to give");
            }
        }

        /// <summary>
        /// 542: the caravan walks the Long Walk (the prisoner a friendly bystander, his four guards hostile on his tether); a
        /// guard's kill before the accept counts for nothing; four kills by the player complete 1 once; Toida pays; the
        /// prisoner walks on alone.
        /// </summary>
        [TestMethod]
        public void BringDownTheKaelKillsTheCaravansFourGuards()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, Toida);
            var (prisoner, guards) = Caravan(harness, marshes);
            Assert.AreEqual(MarshesEastAndNorthNpcsV1.PrisonerName, prisoner.NameId, "Forean Prisoner Leader");
            Assert.IsTrue(S.IsBystander(prisoner), "the prisoner takes up no fight");
            Assert.AreEqual(TargetCategory.Neutral, prisoner.TargetCategory, "no foe, and his guards' neither");
            Assert.IsEmpty(prisoner.Actions, "no attack");
            Assert.HasCount((int)MarshesEastAndNorthNpcsV1.Guards, guards);
            foreach (var guard in guards)
            {
                Assert.AreEqual(TargetCategory.Hostile, guard.TargetCategory);
                Assert.IsNotEmpty(guard.Actions, "a guard attacks");
                Assert.AreSame(prisoner, S.TetherOf(guard, out var tether), "tethered to the prisoner");
                Assert.AreEqual((float)MarshesEastAndNorthNpcsV1.Tether, tether);
            }
            var walk = MarshesEastAndNorthNpcsV1.LongWalk;
            Assert.AreEqual(S.PathMode.Back, S.Paths[MarshesEastAndNorthNpcsV1.Squad].Mode, "there and back");
            for (var step = 0; step < walk.Length; step++)
            {
                var at = new Vector3((float)walk[step].X, (float)walk[step].Y, (float)walk[step].Z);
                AssertOnGround(marshes, at, $"step {step}");
                if (step == 0)
                    Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, at, out var reached) != null && reached, "walked to");
                if (step == walk.Length - 1)
                    break;
                var next = new Vector3((float)walk[step + 1].X, (float)walk[step + 1].Y, (float)walk[step + 1].Z);
                Assert.IsTrue(marshes.NavMesh.FindPath(at, next, out var complete) != null && complete, $"step {step} to the next is walked");
            }
            var start = prisoner.Position;
            for (var tick = 0; tick < 120; tick++)
                PlateauGiantKillerSquadTests.Step(harness, marshes, 250);
            var live = S.On(marshes)[MarshesEastAndNorthNpcsV1.Squad];
            Assert.IsGreaterThan(10f, Vector3.Distance(start, prisoner.Position),
                $"the caravan walks: from {start} at {prisoner.Position}, phase {live.Phase}, index {live.PathIndex}, goal {prisoner.SquadMember.Goal}, " +
                $"speed {prisoner.SquadMember.Speed}, action {prisoner.Controller.CurrentAction}, guards at {string.Join(" ", guards.Select(g => g.Position))}");

            KillGuard(harness, marshes, guards[0]);
            var offer = WildernessRanjaGorgeTests.Offer(harness, toida, Kael);
            CollectionAssert.AreEqual(MarshesBringDownTheKaelV1.Rewards, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, toida, Kael);
            Assert.AreEqual(0U, harness.Client.Player.Missions[Kael].Objectives[1].Counters[0], "the kill before the accept does not count");

            var log = new List<PythonPacket>();
            for (var index = 1; index < guards.Length; index++)
            {
                log.AddRange(KillGuard(harness, marshes, guards[index]));
                Assert.AreEqual((uint)index, harness.Client.Player.Missions[Kael].Objectives[1].Counters[0], $"guard {index}");
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Kael, 1), "three of four");
            // The caravan sent again: its fourth guard.
            S.Worker(marshes);
            harness.Drain();
            var (_, again) = Caravan(harness, marshes, init: false);
            if (again.Length == 0)
            {
                S.On(marshes)[MarshesEastAndNorthNpcsV1.Squad].RespawnAt = 1;
                S.Worker(marshes);
                harness.Drain();
                (_, again) = Caravan(harness, marshes, init: false);
            }
            Assert.IsNotEmpty(again, "the caravan comes again");
            log.AddRange(KillGuard(harness, marshes, again[0]));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Kael, 1), "four guards");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Kael && packet.IsCompleteable),
                "MissionCompleteable once");

            FullBagWaits(harness, toida, Kael, 1, MarshesBringDownTheKaelV1.ReflectiveVest);
            FailedTurnInPaysNothing(harness, toida, Kael, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, toida, Kael, 1,
                MarshesBringDownTheKaelV1.Experience, MarshesBringDownTheKaelV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Kael), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, toida, Kael), "done once");
        }

        /// <summary>
        /// 864: the altars are the player's own and closed to a use before 1 opens; four Machina, each finished (the first
        /// death a self revive), give four icons and complete 2 (a reload keeps it); one altar takes the four icons, counts
        /// four shards, lights and closes both; Bargas pays.
        /// </summary>
        [TestMethod]
        public void BringingTheSpiritHomeTakesFourIconsToAnAltar()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var bargas = Npc(marshes, Bargas);
            Spawn(marshes, MarshesEastAndNorthNpcsV1.NorthMachinaPool, MarshesEastAndNorthNpcsV1.EastMachinaPool);
            Assert.IsFalse(Holds(KillMachina(harness, marshes), MarshesBringingTheSpiritHomeV1.IconTemplate), "no icon before the mission");
            harness.Drain();
            var offer = WildernessRanjaGorgeTests.Offer(harness, bargas, Spirit);
            CollectionAssert.AreEqual(MarshesBringingTheSpiritHomeV1.Rewards, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, bargas, Spirit);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Euthanize), "2 open");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Place), "1 waits");

            var altars = Altars(harness, marshes);
            harness.MoveTo(altars[0].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, altars[0], MarshesBringingTheSpiritHomeV1.UseArgument, recovers: false);

            var log = new List<PythonPacket>();
            for (var icon = 1U; icon <= MarshesBringingTheSpiritHomeV1.Icons; icon++)
            {
                var loot = KillMachina(harness, marshes);
                Assert.IsTrue(Holds(loot, MarshesBringingTheSpiritHomeV1.IconTemplate), $"Machina {icon} drops an icon");
                PickUp(harness, loot);
                log.AddRange(harness.Drain());
                Assert.AreEqual(icon, WildernessRanjaGorgeTests.Held(harness, MarshesBringingTheSpiritHomeV1.IconTemplate));
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Euthanize), "four icons");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Spirit &&
                packet.ObjectiveId == MarshesBringingTheSpiritHomeV1.Euthanize), "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Place), "1 opens");
            Assert.IsFalse(Holds(KillMachina(harness, marshes), MarshesBringingTheSpiritHomeV1.IconTemplate), "no fifth icon");
            harness.Drain();
            ReloadAndRecover(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Euthanize), "kept at a reload");

            altars = Altars(harness, marshes);
            harness.MoveTo(altars[1].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, altars[1], MarshesBringingTheSpiritHomeV1.UseArgument, recovers: true);
            log = harness.Drain().ToList();
            log.AddRange(Tick(harness, marshes, 250));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesBringingTheSpiritHomeV1.IconTemplate), "the icons in the urn");
            Assert.AreEqual(MarshesBringingTheSpiritHomeV1.Icons,
                harness.Client.Player.Missions[Spirit].Objectives[MarshesBringingTheSpiritHomeV1.Place].Counters[0], "four shards");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Spirit, MarshesBringingTheSpiritHomeV1.Place));
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Spirit && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual((UseObjectState)MarshesBringingTheSpiritHomeV1.LitState, altars[1].StateId, "lit");
            Assert.IsTrue(altars.All(altar => !altar.IsEnabled), "one use closes both");

            FullBagWaits(harness, bargas, Spirit, 1, MarshesBringingTheSpiritHomeV1.Rewards[1]);
            FailedTurnInPaysNothing(harness, bargas, Spirit, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bargas, Spirit, 1,
                MarshesBringingTheSpiritHomeV1.Experience, MarshesBringingTheSpiritHomeV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Spirit), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, bargas, Spirit), "done once");
        }

        /// <summary>
        /// 920: Shaw's stand-in and the station's two soldiers stand from the accept (the public Shaw hidden); his talk starts
        /// the defence; the waves come at 10 s and 90 s (the second with the Stalker); at 180 s 2 completes once, the station's
        /// men and the Thrax go, the public Shaw is back and Meeko comes (kept at a reload); his talk completes 3; Figgins
        /// takes nothing; Meeko cannot be killed; Meeko pays and goes.
        /// </summary>
        [TestMethod]
        public void HeadEastHoldsTheStationAndMeekoComes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var figgins = Npc(marshes, Figgins);
            var publicShaw = Npc(marshes, MarshesHeadEastV2.PublicShawPool);
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicShaw, harness.Client), "the public Shaw before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, figgins, HeadEast);
            CollectionAssert.AreEqual(MarshesHeadEastV2.Rewards, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Begin(harness, marshes, figgins, HeadEast);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, HeadEast, MarshesHeadEastV2.Defend), "2 hidden");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, HeadEast, MarshesHeadEastV2.Speak), "3 hidden");
            // To-fix 10: no station men while no player is near, the public Shaw there.
            for (var second = 0; second < 60; second++)
                Tick(harness, marshes, 1000);
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Leader), "no stand-in before the player comes near");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicShaw, harness.Client), "the public Shaw until then");
            Approach(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, HeadEast, MarshesHeadEastV2.Near), "the hidden approach");

            var shaw = SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Leader);
            Assert.IsNotNull(shaw, "the stand-in stands once the player is near");
            Assert.AreEqual((MarshesEastAndNorthNpcsV1.ShawStandIn, MarshesEastAndNorthNpcsV1.ShawName, MarshesEastAndNorthNpcsV1.ShawPackage),
                (shaw.DbId, shaw.NameId, shaw.Npc?.NpcPackageId));
            Assert.IsLessThan(0.5f, Vector3.Distance(shaw.Position, ToVector(MarshesHeadEastV2.ShawAt)), "at Shaw's place");
            Assert.AreEqual((int)MarshesGuardStationHoldsV1.ShawHitPoints, (int)shaw.Attributes[Attributes.Health].NormalMax, "Shaw's hit points");
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(publicShaw, harness.Client), "the public Shaw hidden");
            for (var member = 2; member <= MarshesHeadEastV2.SoldiersAt.Length + 1; member++)
            {
                var soldier = SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Ward(member));
                Assert.IsNotNull(soldier);
                Assert.AreEqual(MarshesEastAndNorthNpcsV1.StationSoldier, soldier.DbId);
                Assert.AreNotEqual(TargetCategory.Hostile, soldier.TargetCategory);
                Assert.IsTrue(BehaviorManager.NeverMoves(soldier), "a soldier stands still (review list 2026-11-04)");
                Assert.AreEqual((int)MarshesGuardStationHoldsV1.SoldierHitPoints, (int)soldier.Attributes[Attributes.Health].NormalMax, "a soldier's hit points");
                AssertOnGround(marshes, soldier.Position, "a soldier");
            }
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.AfterRole(1)), "no Meeko yet");
            Assert.IsFalse(Wave(harness, marshes, 1).Any(), "no Thrax before the talk");
            Assert.IsEmpty(Topics(harness, figgins, HeadEast), "Figgins hears nothing");

            Talk(harness, shaw, HeadEast, MarshesHeadEastV2.Report);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HeadEast, MarshesHeadEastV2.Defend));
            Tick(harness, marshes, 250);
            var log = new List<PythonPacket>();
            for (var wave = 1; wave <= MarshesGuardStationHoldsV1.Waves.Length; wave++)
            {
                var (after, members) = MarshesGuardStationHoldsV1.Waves[wave - 1];
                AdvanceTo(harness, marshes, after - 5000, log);
                Assert.IsFalse(Wave(harness, marshes, wave).Any(), $"wave {wave} not before {after - 5000} ms");
                AdvanceTo(harness, marshes, after + 1000, log);
                var thrax = Wave(harness, marshes, wave).ToArray();
                Assert.HasCount(members.Length, thrax, $"wave {wave}");
                for (var member = 0; member < thrax.Length; member++)
                {
                    Assert.AreEqual(members[member].Creature, thrax[member].DbId);
                    Assert.AreEqual(TargetCategory.Hostile, thrax[member].TargetCategory, "a foe");
                    Assert.IsTrue(thrax[member].SceneAttackPastLeash, $"wave {wave} member {member + 1} is sent at the station");
                    AssertOnGround(marshes, thrax[member].Position, $"wave {wave} member {member + 1}");
                    Assert.IsTrue(marshes.NavMesh.FindPath(thrax[member].Position, shaw.Position, out var reaches) != null && reaches,
                        $"wave {wave} member {member + 1} reaches the station");
                }
            }
            Assert.IsTrue(Wave(harness, marshes, 2).Any(creature => creature.DbId == MarshesEastAndNorthNpcsV1.Stalker), "a Stalker");
            AdvanceTo(harness, marshes, MarshesHeadEastV2.HoldMilliseconds - 5000, log);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HeadEast, MarshesHeadEastV2.Defend), "not before the hold");
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.AfterRole(1)), "no Meeko before the hold");
            AdvanceTo(harness, marshes, MarshesHeadEastV2.HoldMilliseconds + 2000, log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, HeadEast, MarshesHeadEastV2.Defend), "held");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == HeadEast &&
                packet.ObjectiveId == MarshesHeadEastV2.Defend), "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HeadEast, MarshesHeadEastV2.Speak), "3 opens");
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Leader), "the stand-in goes");
            for (var wave = 1; wave <= MarshesGuardStationHoldsV1.Waves.Length; wave++)
                Assert.IsFalse(Wave(harness, marshes, wave).Any(), $"wave {wave} goes");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicShaw, harness.Client), "the public Shaw is back");
            var meeko = Meeko(harness, marshes);

            ReloadAndRecover(harness, marshes);
            meeko = Meeko(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, HeadEast, MarshesHeadEastV2.Speak), "kept at a reload");

            // Meeko cannot be killed: the receiver stays.
            Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(meeko), "Meeko is invulnerable");

            Talk(harness, meeko, HeadEast, MarshesHeadEastV2.Speak);
            Assert.IsTrue(harness.Client.Player.Missions[HeadEast].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, figgins.EntityId, HeadEast, 1), "not Figgins' to take");
            FullBagWaits(harness, meeko, HeadEast, 1, MarshesHeadEastV2.Rewards[0]);
            FailedTurnInPaysNothing(harness, meeko, HeadEast, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, meeko, HeadEast, 1, MarshesHeadEastV2.Experience, MarshesHeadEastV2.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == HeadEast), "MissionRewarded once");
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.AfterRole(1)), "Meeko goes after the turn-in");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, figgins, HeadEast), "done once");
        }

        /// <summary>
        /// The defence's after-actors (MarshesDefenceScene): the hold's end sends the outcome, takes the station's men and the
        /// waves away and brings Meeko, the scene running on; after it a recovery brings Meeko back (a restart lost him), any
        /// other observation changes nothing, a cancel ends it. Without after-actors the hold's end ends the scene.
        /// </summary>
        [TestMethod]
        public void TheDefenceKeepsMeekoAfterTheHold()
        {
            var bindings = MarshesHeadEastV2.Scene().Bindings(MarshesMissionDataV1.Revision);
            SceneDecision Handle(string phase, SceneEventKind kind, uint sequence = 0, SceneBindings with = null) =>
                new MarshesDefenceScene().Handle(
                    new SceneContext(new SceneRun("run", MarshesMissionDataV1.Revision, "marshes.defence", 1, 1, 0,
                            System.Text.Json.JsonSerializer.Serialize(new MarshesDefenceScene.Checkpoint(phase, 2, System.Array.Empty<string>())),
                            SceneStatus.Running, 7, HeadEast),
                        with ?? bindings, new System.DateTime(2026, 11, 3, 0, 0, 0, System.DateTimeKind.Utc)),
                    new SceneObservation(kind, 1, SequenceId: sequence));
            string Phase(SceneDecision decision) =>
                System.Text.Json.JsonSerializer.Deserialize<MarshesDefenceScene.Checkpoint>(decision.Checkpoint).Phase;
            var meeko = $"ensure-{MarshesDefenceScene.AfterRole(1)}";

            // To-fix 10: with an approach the scene waits; the approach stands the men; a start heard first stands them too.
            var waiting = Handle("new", SceneEventKind.Started);
            Assert.AreEqual("waiting", Phase(waiting));
            Assert.IsEmpty(waiting.WorldIntents, "nobody stands before the approach");
            Assert.IsEmpty(Handle("waiting", SceneEventKind.Recovered).WorldIntents, "nor at a recovery");
            var near = Handle("waiting", SceneEventKind.Signal, MarshesHeadEastV2.Approach);
            Assert.AreEqual("standing", Phase(near));
            Assert.IsTrue(near.WorldIntents.Any(intent => intent is EnsureActorIntent { Role: MarshesDefenceScene.Leader }), "Shaw's stand-in stands");
            Assert.IsTrue(near.WorldIntents.Any(intent => intent is HideSpawnPoolsIntent), "the public Shaw hidden");
            var early = Handle("waiting", SceneEventKind.Signal, MarshesHeadEastV2.DefenceStarts);
            Assert.AreEqual("defending", Phase(early));
            Assert.IsTrue(early.WorldIntents.Any(intent => intent is EnsureActorIntent { Role: MarshesDefenceScene.Leader }), "the men stand at the start");
            Assert.IsNotEmpty(early.Timers, "the waves are timed");

            var held = Handle("defending", SceneEventKind.TimerElapsed, MarshesDefenceScene.HoldTimerSequence);
            Assert.AreEqual(("after", SceneStatus.Running), (Phase(held), held.Status), "the scene runs on");
            Assert.AreEqual(1, held.WorldIntents.Count(intent => intent.OperationKey == meeko), "Meeko comes");
            Assert.IsTrue(held.WorldIntents.Any(intent => intent is RemoveActorIntent { Role: MarshesDefenceScene.Leader }), "Shaw's stand-in goes");
            Assert.IsTrue(held.WorldIntents.Any(intent => intent is RemoveActorIntent remove && remove.Role == MarshesDefenceScene.WaveRole(2, 5)),
                "the Stalker goes");
            Assert.IsFalse(held.WorldIntents.Any(intent => intent is RemoveActorIntent remove && remove.Role.StartsWith("after-")), "Meeko stays");
            Assert.AreEqual((HeadEast, MarshesHeadEastV2.Held, 1U),
                (held.Signals.Single().MissionId, held.Signals.Single().SequenceId, held.Signals.Single().EventId), "held");

            var recovered = Handle("after", SceneEventKind.Recovered);
            CollectionAssert.AreEqual(new[] { meeko }, recovered.WorldIntents.Select(intent => intent.OperationKey).ToArray(), "Meeko back");
            Assert.AreEqual(SceneStatus.Running, recovered.Status);
            foreach (var (kind, sequence) in new[] { (SceneEventKind.TimerElapsed, MarshesDefenceScene.HoldTimerSequence),
                (SceneEventKind.Signal, MarshesHeadEastV2.DefenceStarts), (SceneEventKind.ActorDied, 0U) })
            {
                var unchanged = Handle("after", kind, sequence);
                Assert.IsEmpty(unchanged.WorldIntents, $"{kind} changes nothing");
                Assert.IsEmpty(unchanged.Signals, $"{kind} sends nothing");
            }
            Assert.IsTrue(bindings.Actors[MarshesDefenceScene.AfterRole(1)].GameplayPolicy?.Invulnerable == true, "Meeko cannot be killed");
            var cancelled = Handle("after", SceneEventKind.Cancelled);
            Assert.AreEqual(("cancelled", SceneStatus.Ended), (Phase(cancelled), cancelled.Status));

            var trapped = Handle("defending", SceneEventKind.TimerElapsed, MarshesDefenceScene.HoldTimerSequence,
                with: new SceneBindings(bindings.Release,
                    bindings.Actors.Where(entry => !entry.Key.StartsWith("after-")).ToDictionary(entry => entry.Key, entry => entry.Value),
                    bindings.Routes.ToDictionary(entry => entry.Key, entry => entry.Value),
                    bindings.Sequences.ToDictionary(entry => entry.Key, entry => entry.Value),
                    bindings.Names.ToDictionary(entry => entry.Key, entry => entry.Value),
                    bindings.DefeatSequences.ToDictionary(entry => entry.Key, entry => entry.Value), bindings.MapContextId));
            Assert.AreEqual(("done", SceneStatus.Ended), (Phase(trapped), trapped.Status), "no after-actors: the scene ends (Trapped!)");
        }

        /// <summary>The player 30 m west of Shaw, inside 920's approach (40 m): the station's men stand.</summary>
        private static void Approach(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            MarshesPreoDasTests.Enter(harness, ToVector(MarshesHeadEastV2.ShawAt) + new Vector3(-30, 0, 0));
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
        }

        /// <summary>Shaw killed before his talk fails 1; abandoned, the station's men go and the public Shaw is back.</summary>
        [TestMethod]
        public void HeadEastFailsWithShawDeadAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var figgins = Npc(marshes, Figgins);
            var publicShaw = Npc(marshes, MarshesHeadEastV2.PublicShawPool);
            Begin(harness, marshes, figgins, HeadEast);
            Approach(harness, marshes);
            var failed = Die(harness, marshes, SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Leader));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, HeadEast, MarshesHeadEastV2.Report), "lost before the report");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[HeadEast].State);
            Assert.AreEqual(1, failed.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == HeadEast), "ObjectiveFailed once");
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Ward(2)), "the soldiers go");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, HeadEast), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Begin(harness, marshes, figgins, HeadEast);
            Approach(harness, marshes);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, HeadEast), "abandoned with Shaw waiting");
            harness.Drain();
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.Leader), "the stand-in goes");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicShaw, harness.Client), "the public Shaw is back");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, figgins, HeadEast), "offered again");
        }

        /// <summary>
        /// 2009: five debris pieces, the player's own, at their places on the navmesh, walked to from their waypoints; the
        /// wrong use argument is refused; one piece issues the salvage and closes all; Starling keeps it and pays the rocket.
        /// </summary>
        [TestMethod]
        public void SoyuzSalvageTakesOnePieceOfFive()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var starling = Npc(marshes, Starling);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Soyuz), "no debris before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, starling, Soyuz);
            Assert.AreEqual(0, offer.SelectableReward.Count, "no choice: the model rocket");
            Begin(harness, marshes, starling, Soyuz);

            var pieces = MarshesSoyuzSalvageV1.Pieces.Select(piece => marshes.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Soyuz && obj.SceneActorRole == piece.Role && obj.IsInWorld)).ToArray();
            var from = new[] { Road, ResearchPointWaypoint, FalconHoldWaypoint, FalconHoldWaypoint, BaneAssertionCampWaypoint };
            for (var index = 0; index < pieces.Length; index++)
            {
                var piece = MarshesSoyuzSalvageV1.Pieces[index];
                Assert.AreEqual(piece.Class, (uint)pieces[index].EntityClassId);
                Assert.AreEqual(ToVector(piece.At), pieces[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, pieces[index].SceneOwnerCharacterId, "the player's own");
                AssertOnGround(marshes, pieces[index].Position, piece.Role);
                Assert.IsTrue(marshes.NavMesh.FindPath(from[index], pieces[index].Position, out var walked) != null && walked,
                    $"{piece.Role} is walked to");
            }

            harness.MoveTo(pieces[3].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, pieces[3], 3, recovers: false);
            Use(harness, marshes, pieces[3], MarshesSoyuzSalvageV1.UseArgument, recovers: true);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesSoyuzSalvageV1.DebrisTemplate));
            Assert.IsTrue(harness.Client.Player.Missions[Soyuz].Completeable);
            Assert.AreEqual((UseObjectState)MarshesSoyuzSalvageV1.TakenState, pieces[3].StateId);
            Assert.IsTrue(pieces.All(obj => !obj.IsEnabled), "one piece closes all five");
            harness.MoveTo(pieces[4].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, pieces[4], MarshesSoyuzSalvageV1.UseArgument, recovers: false);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesSoyuzSalvageV1.DebrisTemplate), "one piece");

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, starling, Soyuz, null,
                MarshesSoyuzSalvageV1.Experience, MarshesSoyuzSalvageV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesSoyuzSalvageV1.ModelRocket), "the model rocket");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesSoyuzSalvageV1.DebrisTemplate), "Starling keeps the piece");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, starling, Soyuz), "done once");
        }

        /// <summary>
        /// To-fix 30: the caravan is sent again on its time (300 s), not once at boot. Its four guards killed, the
        /// prisoner walks on alone and the caravan comes again 300 s after the last guard, not before (no wait for a
        /// corpse or for him to go). Its four guards living but lost from him - held far off, unable to move - count
        /// the same: 300 s on, the caravan is taken away, the lost guards too, and sent again in full.
        /// </summary>
        [TestMethod]
        public void TheCaravanIsSentAgainOnItsTimeWhenItsGuardsAreDeadOrLost()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var (prisoner, guards) = Caravan(harness, marshes);
            const long respawnMs = MarshesEastAndNorthNpcsV1.RespawnSeconds * 1000L;
            PlateauGiantKillerSquadTests.Step(harness, marshes, 10_000);
            var live = S.On(marshes)[MarshesEastAndNorthNpcsV1.Squad];

            foreach (var guard in guards)
                KillGuard(harness, marshes, guard);
            harness.MoveTo(new Vector3(-319f, 220f, -5f));
            var from = prisoner.Position;
            PlateauGiantKillerSquadTests.Step(harness, marshes, respawnMs - 2_000);
            Assert.AreSame(prisoner, live.Leader?.Creature, "he walks on alone until its time");
            Assert.IsGreaterThan(10f, Vector3.Distance(from, prisoner.Position), "he walks on");
            PlateauGiantKillerSquadTests.Step(harness, marshes, 3_000);
            var (again, guardsAgain) = Caravan(harness, marshes, init: false);
            Assert.IsNotNull(again, "the caravan is sent again 300 s after its last guard");
            Assert.AreNotSame(prisoner, again, "a new prisoner");
            Assert.IsNull(prisoner.SquadMember, "the old one taken away");
            Assert.HasCount((int)MarshesEastAndNorthNpcsV1.Guards, guardsAgain);

            // Lost: living, held 150 m off the prisoner, none able to come back to him.
            foreach (var guard in guardsAgain)
            {
                guard.WalkSpeed = guard.RunSpeed = 0;
                guard.Position = again.Position + new Vector3(150, 0, 0);
            }
            PlateauGiantKillerSquadTests.Step(harness, marshes, respawnMs - 2_000);
            Assert.AreSame(again, live.Leader?.Creature, "not before its time");
            Assert.AreEqual(5, live.Living.Count(), "the lost guards live");
            PlateauGiantKillerSquadTests.Step(harness, marshes, 3_000);
            var (third, guardsThird) = Caravan(harness, marshes, init: false);
            Assert.IsNotNull(third, "sent again with its guards lost");
            Assert.AreNotSame(again, third);
            Assert.IsTrue(guardsAgain.All(guard => guard.SquadMember == null), "the lost guards taken away");
            Assert.HasCount((int)MarshesEastAndNorthNpcsV1.Guards, guardsThird);
        }

        /// <summary>The caravan, sent as its squad (the squads' worker on the Marshes): the prisoner and his living guards.</summary>
        private static (Creature Prisoner, Creature[] Guards) Caravan(WildernessRuntimeTestHarness harness, MapChannel marshes, bool init = true)
        {
            if (init)
            {
                S.Init(harness);
                Assert.IsTrue(S.Spawns.ContainsKey(MarshesEastAndNorthNpcsV1.Squad), "the caravan is in the world database");
                S.Worker(marshes);
                harness.Drain();
            }
            var pack = S.On(marshes)[MarshesEastAndNorthNpcsV1.Squad].Members.Select(member => member.Creature)
                .Where(creature => creature.State != CharacterState.Dead).ToArray();
            return (pack.SingleOrDefault(creature => creature.DbId == MarshesEastAndNorthNpcsV1.Prisoner),
                pack.Where(creature => creature.DbId == MarshesEastAndNorthNpcsV1.Guard).ToArray());
        }

        /// <summary>The guard killed by the player; the scenes and the squads see it.</summary>
        private static IReadOnlyList<PythonPacket> KillGuard(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature guard)
        {
            harness.MoveTo(guard.Position);
            guard.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, guard, harness.Client.Player);
            harness.Manager.Scenes.Tick(marshes);
            return harness.Drain();
        }

        /// <summary>
        /// A living Machina killed as the player and finished (a crit kill: an ordinary first death is a self revive,
        /// CreatureSupport); its corpse. A pool is filled again when none is left.
        /// </summary>
        private static LootDispenser KillMachina(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            if (!Alive(marshes).Any(candidate => candidate.DbId == MarshesEastAndNorthNpcsV1.Machina))
                Respawn(marshes, MarshesEastAndNorthNpcsV1.NorthMachinaPool, MarshesEastAndNorthNpcsV1.EastMachinaPool);
            var machina = Alive(marshes).First(candidate => candidate.DbId == MarshesEastAndNorthNpcsV1.Machina);
            harness.MoveTo(machina.Position);
            machina.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, machina, harness.Client.Player, CritKill.Own);
            Assert.IsTrue(marshes.LootDispensers.TryGetValue(machina.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>The two altars: the player's own braziers, ready, at their places.</summary>
        private static DynamicObject[] Altars(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var altars = MarshesBringingTheSpiritHomeV1.Altars.Select(altar => marshes.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Spirit && obj.SceneActorRole == altar.Role && obj.IsInWorld)).ToArray();
            for (var index = 0; index < altars.Length; index++)
            {
                Assert.AreEqual(MarshesBringingTheSpiritHomeV1.BrazierClass, (uint)altars[index].EntityClassId);
                Assert.AreEqual(ToVector(MarshesBraziersOnDryFloorV1.Moves[index].Now), altars[index].Position, "on the temple floor, out of the fountain");
                Assert.AreEqual(harness.Client.Player.Id, altars[index].SceneOwnerCharacterId, "the player's own");
            }
            return altars;
        }

        /// <summary>Meeko: there, the player's own scene NPC, with the client's name and package, by Shaw.</summary>
        private static Creature Meeko(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var meeko = SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.AfterRole(1));
            Assert.IsNotNull(meeko, "Meeko comes");
            Assert.AreEqual((MarshesEastAndNorthNpcsV1.Meeko, MarshesEastAndNorthNpcsV1.MeekoName, MarshesEastAndNorthNpcsV1.MeekoPackage),
                (meeko.DbId, meeko.NameId, meeko.Npc?.NpcPackageId));
            Assert.AreNotEqual(TargetCategory.Hostile, meeko.TargetCategory);
            AssertOnGround(marshes, meeko.Position, "Meeko");
            Assert.IsLessThan(10f, Vector3.Distance(meeko.Position, ToVector(MarshesHeadEastV2.ShawAt)), "by Shaw");
            return meeko;
        }

        /// <summary>Wave <paramref name="wave"/>'s living creatures of this run, in member order.</summary>
        private static IEnumerable<Creature> Wave(WildernessRuntimeTestHarness harness, MapChannel marshes, int wave) =>
            Enumerable.Range(1, 8).Select(member => SceneCreature(harness, marshes, HeadEast, MarshesDefenceScene.WaveRole(wave, member)))
                .Where(creature => creature != null);

        /// <summary>The clock on to <paramref name="milliseconds"/> after the talk (counted from the first call), a second at a time.</summary>
        private void AdvanceTo(WildernessRuntimeTestHarness harness, MapChannel marshes, uint milliseconds, List<PythonPacket> log)
        {
            _talkedAt ??= harness.UtcNow;
            while ((harness.UtcNow - _talkedAt.Value).TotalMilliseconds < milliseconds)
            {
                // The attackers are held where they come in, the station kept up: only the scene's clock is under test.
                foreach (var creature in Enumerable.Range(1, MarshesGuardStationHoldsV1.Waves.Length).SelectMany(wave => Wave(harness, marshes, wave)))
                    creature.Attributes[Attributes.Health].Current = creature.Attributes[Attributes.Health].NormalMax;
                foreach (var role in new[] { MarshesDefenceScene.Leader, MarshesDefenceScene.Ward(2), MarshesDefenceScene.Ward(3) })
                    if (SceneCreature(harness, marshes, HeadEast, role) is { } ward)
                        ward.Attributes[Attributes.Health].Current = ward.Attributes[Attributes.Health].NormalMax;
                log.AddRange(Tick(harness, marshes, 1000));
            }
        }

        private System.DateTime? _talkedAt;
    }
}
