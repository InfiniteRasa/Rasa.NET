extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// Taking a creature or a player out of the cells looks in the cell it is on (CellMembership,
    /// kept by the cells' lists, CellList) instead of every cell of the map. Checked against the
    /// search of every cell it replaced: the same world played twice, once with every cell searched
    /// as before (CellManager.SearchEveryCell), and the cells' lists and what each client was sent
    /// compared after every step; corpses and players put on a second list by hand among the steps.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CellMembershipTests
    {
        private static Creature Creature(WorldTestContext world, Vector3 position) => new()
        {
            Name = "Fixture",
            MapContextId = world.Map.MapInfo.MapContextId,
            Position = position,
            EntityClass = EntityClasses.HumanBaseMale,
            State = CharacterState.Idle,
            Level = 1,
            AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
        };

        private static Vector3 Somewhere(Random random) => new(random.Next(-150, 150), 0, random.Next(-150, 150));

        /// <summary>Every cell's lists, by seed, with each creature and client by its number in the run.</summary>
        private static string Snapshot(MapChannel map, List<Creature> creatures, List<Client> clients) =>
            string.Join(";", map.MapCellInfo.Cells.OrderBy(pair => pair.Key).Select(pair =>
                $"{pair.Key:X}:c[{string.Join(",", pair.Value.CreatureList.Select(c => creatures.IndexOf(c)))}]" +
                $"p[{string.Join(",", pair.Value.ClientList.Select(c => clients.IndexOf(c)))}]"));

        /// <summary>What each client was sent since the last look: the kind of each message and, for a call, the method.</summary>
        private static string Sent(List<Client> clients, List<Creature> creatures) =>
            string.Join("|", clients.Select(client => string.Join(",", WorldTestContext.Drain(client).Select(packet =>
                packet.Message is CallMethodMessage call
                    ? $"{call.MethodId}@{Who(call.EntityId, clients, creatures)}{About(call, clients, creatures)}"
                    : packet.Message.GetType().Name))));

        /// <summary>The entity a create or a destroy is of, which is in the packet rather than the call.</summary>
        private static string About(CallMethodMessage call, List<Client> clients, List<Creature> creatures) =>
            call.Packet?.GetType().GetProperty("EntityId")?.GetValue(call.Packet) is ulong entityId
                ? $"({Who(entityId, clients, creatures)})"
                : "";

        private static string Who(ulong entityId, List<Client> clients, List<Creature> creatures)
        {
            // A creature taken out of the world gives its id to the next one made: the newest with it is the one meant.
            var creature = creatures.FindLastIndex(c => c.EntityId == entityId);
            if (creature >= 0)
                return $"c{creature}";

            var client = clients.FindIndex(c => c.Player.EntityId == entityId);
            return client >= 0 ? $"p{client}" : "x";
        }

        /// <summary>One run of the creature scenario: what the cells held and what was sent, step by step.</summary>
        private static List<string> CreatureRun(bool search)
        {
            var steps = new List<string>();
            var random = new Random(11);

            using var world = new WorldTestContext();
            var clients = Enumerable.Range(0, 4).Select(i => PlayerDeathTests.Player(world, i * 40 - 60, 0)).ToList();
            var creatures = new List<Creature>();
            var inWorld = new List<Creature>();

            CellManager.SearchEveryCell = search;

            try
            {
                for (var step = 0; step < 600; step++)
                {
                    var roll = random.Next(0, 10);

                    if (roll < 3 || inWorld.Count == 0)
                    {
                        var creature = Creature(world, Somewhere(random));
                        creatures.Add(creature);
                        inWorld.Add(creature);
                        CellManager.Instance.AddToWorld(world.Map, creature);
                    }
                    else if (roll < 6)
                    {
                        // Walks: into the cell its new position is in, as a think moves it.
                        var creature = inWorld[random.Next(inWorld.Count)];
                        creature.Position = Somewhere(random);
                        CellManager.TryGetCellCoordinates(creature.Position, out var x, out var z);
                        CreatureManager.Instance.CellUpdateLocation(world.Map, creature, x, z);
                    }
                    else if (roll < 9)
                    {
                        var creature = inWorld[random.Next(inWorld.Count)];
                        inWorld.Remove(creature);
                        Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature));
                    }
                    else
                    {
                        // On a second cell's list as well, put there by hand: a duplicated corpse.
                        var creature = inWorld[random.Next(inWorld.Count)];
                        var cells = world.Map.MapCellInfo.Cells.Values.ToList();
                        cells[random.Next(cells.Count)].CreatureList.Add(creature);
                    }

                    steps.Add($"{step}: {Snapshot(world.Map, creatures, clients)} / {Sent(clients, creatures)}");
                }
            }
            finally
            {
                CellManager.SearchEveryCell = false;

                foreach (var creature in inWorld)
                    CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature);
            }

            return steps;
        }

        [TestMethod]
        public void CreaturesLeaveTheSameCellsAsWhenEveryCellWasSearched()
        {
            var known = CreatureRun(search: false);
            var searched = CreatureRun(search: true);

            // The clients were told of creatures coming and going, so the comparison covers it.
            Assert.IsTrue(known.Count(step => step.Contains("(c")) > 50, "creatures made and taken off the clients");

            Assert.HasCount(searched.Count, known);
            for (var i = 0; i < known.Count; i++)
                Assert.AreEqual(searched[i], known[i], $"step {i}");
        }

        /// <summary>One run of the player scenario: players walking, leaving maps and coming back.</summary>
        private static List<string> ClientRun(bool search)
        {
            var steps = new List<string>();
            var random = new Random(23);

            using var world = new WorldTestContext();
            var elsewhere = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>() };
            CellManager.Instance.CreateCellMatrix(elsewhere, (uint)CellManager.CellBias, (uint)CellManager.CellBias);

            var clients = Enumerable.Range(0, 12).Select(i => PlayerDeathTests.Player(world, i * 20 - 120, i * 5)).ToList();
            var creatures = new List<Creature>();

            foreach (var client in clients)
                WorldTestContext.Drain(client);

            CellManager.SearchEveryCell = search;

            try
            {
                for (var step = 0; step < 600; step++)
                {
                    var client = clients[random.Next(clients.Count)];
                    var roll = random.Next(0, 10);

                    if (roll < 5)
                    {
                        client.Player.Position = Somewhere(random);

                        // Back in the world when out of it, as a move does (UpdateVisibility).
                        CellManager.Instance.UpdateVisibility(client);
                    }
                    else if (roll < 7)
                        CellManager.Instance.DetachClient(world.Map, client);
                    else if (roll < 9)
                        CellManager.Instance.DetachClient(elsewhere, client);
                    else if (CellManager.Instance.IsInWorld(client))
                    {
                        // A second entry by hand: on its own cell's list, which UpdateVisibility tidies,
                        // or on another cell's, which only DetachClient takes it off.
                        var cells = world.Map.MapCellInfo.Cells.Values.ToList();
                        var cell = random.Next(0, 2) == 0 ? world.Map.MapCellInfo.Cells[client.Player.Cells[2, 2]] : cells[random.Next(cells.Count)];
                        cell.ClientList.Add(client);

                        CellManager.Instance.UpdateVisibility(client);
                    }

                    steps.Add($"{step}: {Snapshot(world.Map, creatures, clients)} / {Sent(clients, creatures)}");
                }
            }
            finally
            {
                CellManager.SearchEveryCell = false;
            }

            return steps;
        }

        [TestMethod]
        public void PlayersLeaveTheSameCellsAndAreToldTheSameAsWhenEveryCellWasSearched()
        {
            var known = ClientRun(search: false);
            var searched = ClientRun(search: true);

            Assert.IsTrue(known.Count(step => step.Contains("(p")) > 50, "players made and taken off each other's clients");

            Assert.HasCount(searched.Count, known);
            for (var i = 0; i < known.Count; i++)
                Assert.AreEqual(searched[i], known[i], $"step {i}");
        }

        [TestMethod]
        public void OneOnTwoListsIsTakenOffBoth()
        {
            using var world = new WorldTestContext();
            var creature = Creature(world, new Vector3(0, 0, 0));
            var second = CellManager.Instance.GetCell(world.Map, 40000, 40000);

            CellManager.Instance.AddToWorld(world.Map, creature);
            Assert.IsTrue(creature.CellMembership.TryGetCell(world.Map, out _));

            second.CreatureList.Add(creature);
            Assert.AreEqual(2, creature.CellMembership.Entries);
            Assert.IsFalse(creature.CellMembership.TryGetCell(world.Map, out _), "two cells: searched for");

            Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature));
            Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature)));
            Assert.AreEqual(0, creature.CellMembership.Entries);
        }

        [TestMethod]
        public void OnePutOnAListByHandIsKnownToBeThere()
        {
            using var world = new WorldTestContext();
            var creature = Creature(world, new Vector3(0, 0, 0));
            var cell = CellManager.Instance.GetCell(world.Map, 40000, 40000);

            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            creature.RuntimeMapChannel = world.Map;
            cell.CreatureList.Add(creature);

            Assert.IsTrue(creature.CellMembership.TryGetCell(world.Map, out var there));
            Assert.AreSame(cell, there);
            Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature));
            Assert.IsEmpty(cell.CreatureList);
        }

        /// <summary>
        /// Every way of changing a cell's list keeps what each thing on it knows: after each of a
        /// run of random changes, each one's entries are the times it is on the lists, and the cell
        /// it names is the one it is on.
        /// </summary>
        [TestMethod]
        public void EveryChangeToAListIsKnown()
        {
            using var world = new WorldTestContext();
            var cells = Enumerable.Range(0, 4).Select(i => CellManager.Instance.GetCell(world.Map, 30000, (uint)(30000 + i))).ToList();
            var creatures = Enumerable.Range(0, 6).Select(i => Creature(world, Vector3.Zero)).ToList();
            var random = new Random(31);

            for (var step = 0; step < 3000; step++)
            {
                var list = cells[random.Next(cells.Count)].CreatureList;
                var creature = creatures[random.Next(creatures.Count)];

                switch (random.Next(0, 11))
                {
                    case 0: list.Add(creature); break;
                    case 1: list.AddRange(creatures.Where(_ => random.Next(0, 3) == 0)); break;
                    case 2: list.Insert(random.Next(0, list.Count + 1), creature); break;
                    case 3: list.InsertRange(random.Next(0, list.Count + 1), new[] { creature, creatures[0] }); break;
                    case 4: list.Remove(creature); break;
                    case 5: if (list.Count > 0) list.RemoveAt(random.Next(0, list.Count)); break;
                    case 6: list.RemoveAll(c => c == creature); break;
                    case 7: if (list.Count > 1) list.RemoveRange(0, random.Next(1, list.Count)); break;
                    case 8: if (random.Next(0, 10) == 0) list.Clear(); break;
                    case 9: if (list.Count > 0) list[random.Next(0, list.Count)] = creature; break;
                    default: list.Add(creature); break;
                }

                foreach (var each in creatures)
                {
                    var times = cells.Sum(cell => cell.CreatureList.Count(c => c == each));
                    Assert.AreEqual(times, each.CellMembership?.Entries ?? 0, $"step {step}");

                    var holding = cells.Where(cell => cell.CreatureList.Contains(each)).ToList();
                    if (holding.Count <= 1)
                    {
                        Assert.IsTrue(each.CellMembership == null || each.CellMembership.TryGetCell(world.Map, out _), $"step {step}");
                        if (each.CellMembership != null)
                        {
                            each.CellMembership.TryGetCell(world.Map, out var named);
                            Assert.AreSame(holding.SingleOrDefault(), named, $"step {step}");
                        }
                    }
                    else
                        Assert.IsFalse(each.CellMembership.TryGetCell(world.Map, out _), $"step {step}");
                }
            }
        }

        [TestMethod]
        public void ACellOfAnotherMapIsNotTheAnswer()
        {
            using var world = new WorldTestContext();
            var other = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>() };
            var creature = Creature(world, new Vector3(0, 0, 0));

            CellManager.Instance.AddToWorld(world.Map, creature);

            // Same seeds, other cells.
            CellManager.Instance.CreateCellMatrix(other, creature.Cells[2, 2] & 0xFFFF, creature.Cells[2, 2] >> 16);

            Assert.IsTrue(creature.CellMembership.TryGetCell(other, out var there));
            Assert.IsNull(there);
            Assert.IsTrue(creature.CellMembership.TryGetCell(world.Map, out var here));
            Assert.AreSame(world.Map.MapCellInfo.Cells[creature.Cells[2, 2]], here);

            Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(world.Map, creature));
            Assert.IsTrue(creature.CellMembership.TryGetCell(world.Map, out here));
            Assert.IsNull(here);
        }
    }
}
