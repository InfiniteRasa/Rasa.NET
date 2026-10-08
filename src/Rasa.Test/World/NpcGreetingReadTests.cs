using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The speech bubble of an NPC whose line is marked important is for a character who has not
    // read the line (NpcGreetings): being shown it reads it, and that is kept for the character.
    [TestClass]
    [DoNotParallelize]
    public class NpcGreetingReadTests
    {
        private const uint Idle = 9078;

        [TestMethod]
        public void TheSpeechBubbleGoesWhenTheCharacterHasReadTheLineAndOnlyForThem()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var idle = Marked(context, Idle, 488);
            var other = context.CreateAdditionalClient(2);
            var npcs = new NpcManager(context, context.Manager);

            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(npcs, context.Client, idle));
            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(npcs, other, idle));
            Assert.IsTrue(NpcGreetings.IsUnread(context.Client.Player, idle));
            Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, idle));

            // Spoken to: the line as the important greeting, and then the NPC's status again -
            // the plain greeting, which takes the bubble down on the client.
            MissionTestContext.Drain(other);
            var first = Speak(npcs, context.Client, idle);

            Assert.AreEqual(2, first.Count);
            var said = ((ConversePacket)first[0]).ConvoDataDict;
            Assert.AreEqual(1, said.Count);
            Assert.AreEqual(488, said[ConversationType.ImportantGreering]);
            Assert.AreEqual(ConversationStatus.Greeting, ((NPCConversationStatusPacket)first[1]).ConvoStatusId);
            Assert.AreEqual(0, ((NPCConversationStatusPacket)first[1]).Data.Count);
            Assert.AreEqual(0, MissionTestContext.Drain(other).Count, "nobody else is told anything");

            Assert.IsTrue(NpcGreetings.HasRead(context.Client.Player, idle));
            Assert.IsFalse(NpcGreetings.IsUnread(context.Client.Player, idle));
            Assert.AreEqual(488u, context.Client.Player.GreetingsRead[Idle]);
            Assert.AreEqual((Idle, 488u), Kept(context, context.Client.Player.Id).Single());

            // From then on: no bubble, and the line as a plain greeting, with no status after it.
            Assert.AreEqual(ConversationStatus.Greeting, Status(npcs, context.Client, idle));

            var again = Speak(npcs, context.Client, idle);

            Assert.AreEqual(1, again.Count);
            Assert.AreEqual(488, ((ConversePacket)again[0]).ConvoDataDict[ConversationType.Greeting]);
            Assert.AreEqual(1, ((ConversePacket)again[0]).ConvoDataDict.Count);
            Assert.AreEqual((Idle, 488u), Kept(context, context.Client.Player.Id).Single());

            // The other character has read nothing: the bubble is theirs still, until they speak.
            Assert.AreEqual(0, Kept(context, other.Player.Id).Count);
            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(npcs, other, idle));
            Assert.AreEqual(488, ((ConversePacket)Speak(npcs, other, idle)[0]).ConvoDataDict[ConversationType.ImportantGreering]);
            Assert.AreEqual(ConversationStatus.Greeting, Status(npcs, other, idle));
            Assert.AreEqual((Idle, 488u), Kept(context, other.Player.Id).Single());
        }

        [TestMethod]
        public void AnotherLineIsUnreadAgainAndTheSameLineMarkedAgainIsNot()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var idle = Marked(context, Idle, 488);
            var npcs = new NpcManager(context, context.Manager);

            Speak(npcs, context.Client, idle);
            Assert.AreEqual(ConversationStatus.Greeting, Status(npcs, context.Client, idle));

            // Unmarked and marked again, the same line: they have read it.
            idle.Npc.GreetingImportant = false;
            Assert.AreEqual(ConversationStatus.Greeting, Status(npcs, context.Client, idle));
            idle.Npc.GreetingImportant = true;
            Assert.AreEqual(ConversationStatus.Greeting, Status(npcs, context.Client, idle));
            Assert.IsFalse(NpcGreetings.IsUnread(context.Client.Player, idle));

            // Another line: something new to say, and the bubble is back until that is read.
            idle.Npc.GreetingId = 1620;
            Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, idle));
            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(npcs, context.Client, idle));

            var packets = Speak(npcs, context.Client, idle);

            Assert.AreEqual(1620, ((ConversePacket)packets[0]).ConvoDataDict[ConversationType.ImportantGreering]);
            Assert.AreEqual(ConversationStatus.Greeting, ((NPCConversationStatusPacket)packets[1]).ConvoStatusId);
            Assert.AreEqual((Idle, 1620u), Kept(context, context.Client.Player.Id).Single(), "one row an NPC: the line read last");

            // Back to the first line: that is not the one they read last.
            idle.Npc.GreetingId = 488;
            Assert.AreEqual(ConversationStatus.ImportantGreeting, Status(npcs, context.Client, idle));
        }

        [TestMethod]
        public void OnlyBeingShownTheMarkedLineReadsIt()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npcs = new NpcManager(context, context.Manager);

            // A marked NPC with a mission to give: the mission is what it has, and what the
            // player is shown. The line is not read.
            var giver = Marked(context, 77, 488);

            Assert.AreEqual(ConversationStatus.Available, Status(npcs, context.Client, giver));

            var offer = Speak(npcs, context.Client, giver);

            Assert.AreEqual(1, offer.Count);
            Assert.IsTrue(((ConversePacket)offer[0]).ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
            Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, giver));

            // Nor is it under a topic list, which the line only heads.
            giver.Npc.Vendor = new Vendor(586);

            var listed = Speak(npcs, context.Client, giver);

            Assert.AreEqual(1, listed.Count);
            Assert.AreEqual(488, ((ConversePacket)listed[0]).ConvoDataDict[ConversationType.Greeting]);
            Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, giver));
            Assert.AreEqual(0, Kept(context, context.Client.Player.Id).Count);

            // A line that is not marked is not kept either: there is no bubble to take down.
            var plain = context.AddNpc(Idle);
            plain.Npc.GreetingId = 146;

            var spoken = Speak(npcs, context.Client, plain);

            Assert.AreEqual(1, spoken.Count);
            Assert.AreEqual(146, ((ConversePacket)spoken[0]).ConvoDataDict[ConversationType.Greeting]);
            Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, plain));
            Assert.AreEqual(0, Kept(context, context.Client.Player.Id).Count);

            // An NPC with no line of its own, and no NPC at all, have nothing to read.
            Assert.IsFalse(NpcGreetings.MarkRead(context.Client.Player, context.AddNpc(9079), context));
            Assert.IsFalse(NpcGreetings.MarkRead(context.Client.Player, null, context));
            Assert.IsFalse(NpcGreetings.MarkRead(null, plain, context));
            Assert.IsFalse(NpcGreetings.HasRead(null, plain));
            Assert.IsFalse(NpcGreetings.IsUnread(null, plain));
        }

        [TestMethod]
        public void WhatACharacterHasReadIsTheirsWhenTheyComeBack()
        {
            // A character loaded as a login loads it (CharacterManager), and loaded again.
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            var characterId = harness.Client.Player.Id;
            var general = new Creature { DbId = 510002, Npc = new Npc { GreetingId = 488, GreetingImportant = true } };
            var other = new Creature { DbId = 100, Npc = new Npc { GreetingId = 1620, GreetingImportant = true } };

            Assert.AreEqual(0, harness.Client.Player.GreetingsRead.Count);
            Assert.IsTrue(NpcGreetings.IsUnread(harness.Client.Player, general));

            Assert.IsTrue(NpcGreetings.MarkRead(harness.Client.Player, general, harness.Context));
            Assert.IsFalse(NpcGreetings.MarkRead(harness.Client.Player, general, harness.Context), "read already");

            harness.ReconnectFromSelection();

            Assert.AreEqual(characterId, harness.Client.Player.Id);
            Assert.AreEqual(488u, harness.Client.Player.GreetingsRead.Single(read => read.Key == 510002).Value);
            Assert.IsFalse(NpcGreetings.IsUnread(harness.Client.Player, general));
            Assert.IsTrue(NpcGreetings.IsUnread(harness.Client.Player, other), "and nothing else");

            // Forgotten, and gone when they come back again.
            Assert.IsTrue(NpcGreetings.Forget(harness.Client.Player, general, harness.Context));
            Assert.IsFalse(NpcGreetings.Forget(harness.Client.Player, general, harness.Context), "nothing to forget");
            Assert.IsTrue(NpcGreetings.IsUnread(harness.Client.Player, general));

            harness.ReconnectFromSelection();

            Assert.AreEqual(0, harness.Client.Player.GreetingsRead.Count);
            Assert.IsTrue(NpcGreetings.IsUnread(harness.Client.Player, general));
        }

        [TestMethod]
        public void TheRecordIsOneRowAnNpcACharacter()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var characterId = context.Client.Player.Id;

            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterGreetingReads.Get(characterId).Count);

                Assert.IsTrue(unit.CharacterGreetingReads.Set(characterId, 510002, 488));
                Assert.IsFalse(unit.CharacterGreetingReads.Set(characterId, 510002, 488), "recorded already");
                Assert.IsTrue(unit.CharacterGreetingReads.Set(characterId, 100, 1620));
                Assert.IsTrue(unit.CharacterGreetingReads.Set(characterId + 1000, 510002, 488), "another character's is their own");
                Assert.IsTrue(unit.CharacterGreetingReads.Set(characterId, 510002, 146), "another line of the same NPC, in place of the first");

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unit.CharacterGreetingReads.Set(0, 510002, 488));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unit.CharacterGreetingReads.Set(characterId, 0, 488));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unit.CharacterGreetingReads.Set(characterId, 510002, 0));
            }

            using (var unit = context.CreateChar())
            {
                CollectionAssert.AreEquivalent(
                    new[] { (510002u, 146u), (100u, 1620u) },
                    unit.CharacterGreetingReads.Get(characterId).Select(read => (read.Key, read.Value)).ToList());
                CollectionAssert.AreEqual(
                    new[] { (510002u, 488u) },
                    unit.CharacterGreetingReads.Get(characterId + 1000).Select(read => (read.Key, read.Value)).ToList());

                Assert.IsTrue(unit.CharacterGreetingReads.Remove(characterId, 510002));
                Assert.IsFalse(unit.CharacterGreetingReads.Remove(characterId, 510002), "it is gone");
                Assert.IsFalse(unit.CharacterGreetingReads.Remove(characterId, 4242), "never read");
            }

            using (var unit = context.CreateChar())
            {
                CollectionAssert.AreEqual(new[] { 100u }, unit.CharacterGreetingReads.Get(characterId).Keys.ToList());
                Assert.AreEqual(1, unit.CharacterGreetingReads.Get(characterId + 1000).Count, "and nobody else's with it");
            }
        }

        [TestMethod]
        public void AReadThatCannotBeSavedCountsUntilTheyLogOutAndOneThatCannotBeForgottenStays()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var idle = Marked(context, Idle, 488);
            var player = context.Client.Player;
            var broken = new NoDatabase();

            // The bubble they have just answered does not stay over the NPC.
            Assert.IsTrue(NpcGreetings.MarkRead(player, idle, broken));
            Assert.IsTrue(NpcGreetings.HasRead(player, idle));
            Assert.AreEqual(0, Kept(context, player.Id).Count, "nothing was kept");

            // What cannot be taken out of the database is not taken out of memory either.
            Assert.IsFalse(NpcGreetings.Forget(player, idle, broken));
            Assert.IsTrue(NpcGreetings.HasRead(player, idle));

            Assert.IsTrue(NpcGreetings.Forget(player, idle, context));
            Assert.IsFalse(NpcGreetings.HasRead(player, idle));
        }

        [TestMethod]
        public void TheGreetingCommandForgetsThatTheGameMasterHasReadTheTargetedNpc()
        {
            var factoryBefore = Server.GameUnitOfWorkFactory;

            try
            {
                using var context = MissionTestContext.WithDefinitions(321);
                var idle = Marked(context, Idle, 488);
                var other = context.CreateAdditionalClient(2);
                var npcs = new NpcManager(context, context.Manager);
                var commands = new ChatCommandsManager(npcs);

                commands.RegisterChatCommands();
                Server.GameUnitOfWorkFactory = context;
                context.Client.AccountEntry.Level = (byte)GmLevel.GameMaster;
                context.Client.Player.Target = idle.EntityId;

                List<PythonPacket> Run(string command)
                {
                    context.Drain();
                    commands.ProcessCommand(context.Client, command);

                    return context.Drain();
                }

                string Told(List<PythonPacket> packets) =>
                    string.Join(" | ", packets.OfType<SystemMessagePacket>().Select(message => message.TextMessage));

                StringAssert.Contains(Told(Run(".greeting")), "says greeting 488, marked important; you have not read it.");

                // Nothing to forget yet.
                var nothing = Run(".greeting unread");
                StringAssert.Contains(Told(nothing), "You have not read a marked line of Creature #9078.");
                Assert.AreEqual(0, nothing.OfType<NPCConversationStatusPacket>().Count());

                // Both read it; the game master forgets their own reading of it.
                Speak(npcs, context.Client, idle);
                Speak(npcs, other, idle);
                StringAssert.Contains(Told(Run(".greeting")), "says greeting 488, marked important; you have read it.");

                MissionTestContext.Drain(other);
                var forgotten = Run(".greeting unread");

                StringAssert.Contains(Told(forgotten), "it has the speech bubble for you again");
                Assert.AreEqual(ConversationStatus.ImportantGreeting, forgotten.OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);
                Assert.IsFalse(NpcGreetings.HasRead(context.Client.Player, idle));
                Assert.AreEqual(0, Kept(context, context.Client.Player.Id).Count);

                // The other character's is theirs: still read, and they are told nothing.
                Assert.AreEqual(0, MissionTestContext.Drain(other).Count);
                Assert.IsTrue(NpcGreetings.HasRead(other.Player, idle));
                Assert.AreEqual((Idle, 488u), Kept(context, other.Player.Id).Single());

                StringAssert.Contains(Told(Run(".greeting unread")), "You have not read a marked line");

                // Read, then unmarked: forgotten all the same, and no bubble to promise.
                Speak(npcs, context.Client, idle);
                idle.Npc.GreetingImportant = false;

                var plain = Run(".greeting unread");

                StringAssert.Contains(Told(plain), "It is not marked important now");
                Assert.AreEqual(ConversationStatus.Greeting, plain.OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);
                Assert.AreEqual(0, Kept(context, context.Client.Player.Id).Count);

                // More after the word is not understood.
                StringAssert.Contains(Told(Run(".greeting unread all")), "usage: .greeting");
            }
            finally
            {
                Server.GameUnitOfWorkFactory = factoryBefore;
            }
        }

        private static Creature Marked(MissionTestContext context, uint dbId, uint greetingId)
        {
            var npc = context.AddNpc(dbId);

            npc.Npc.GreetingId = greetingId;
            npc.Npc.GreetingImportant = true;

            return npc;
        }

        private static ConversationStatus Status(NpcManager npcs, Client client, Creature npc)
        {
            MissionTestContext.Drain(client);
            npcs.UpdateConversationStatus(client, npc);

            return MissionTestContext.Drain(client).OfType<NPCConversationStatusPacket>().Single().ConvoStatusId;
        }

        /// <summary>The player speaks to the NPC: what they are sent for it, in order.</summary>
        private static List<PythonPacket> Speak(NpcManager npcs, Client client, Creature npc)
        {
            MissionTestContext.Drain(client);
            npcs.RequestNpcConverse(client, new RequestNPCConversePacket { EntityId = npc.EntityId });

            var packets = MissionTestContext.Drain(client)
                .Where(packet => packet is ConversePacket || packet is NPCConversationStatusPacket).ToList();

            Assert.IsInstanceOfType(packets[0], typeof(ConversePacket), "the line first, then the status");
            Assert.IsTrue(MissionTestContext.Encode(packets[0]).Length > 0, "one the packet will write");

            return packets;
        }

        private static List<(uint, uint)> Kept(MissionTestContext context, uint characterId)
        {
            using var unit = context.CreateChar();

            return unit.CharacterGreetingReads.Get(characterId).Select(read => (read.Key, read.Value)).ToList();
        }

        /// <summary>A server whose databases cannot be reached.</summary>
        private sealed class NoDatabase : IGameUnitOfWorkFactory
        {
            public ICharUnitOfWork CreateChar() => throw new InvalidOperationException("The character database is away.");

            public IWorldUnitOfWork CreateWorld() => throw new InvalidOperationException("The world database is away.");
        }
    }
}
