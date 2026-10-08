using System;
using System.Collections;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// The line an NPC greets a player with: an id of the client's npcgreetinglanguage.
    ///
    /// The client asks for one in one place that every player sees. An NPC with more than one
    /// thing to talk about - two missions, a mission and a shop - opens the topic list
    /// (conversationwindow.py HandleShowConversationChoice), and the list is headed by the
    /// NPC's greeting, convoDataDict[CONVO_TYPE_GREETING]. Without it the client prints its own
    /// error text there instead: "ERROR: 7: No greeting".
    ///
    /// Which of its 1,699 lines an NPC spoke was the server's to know: the client has the texts
    /// and nothing that ties one to an NPC, an NPC package or a place. So an NPC's line is world
    /// data (npc_greeting, Npc.GreetingId): the ones the text itself gives away are there from
    /// the start (NpcGreetingSeed), and a game master gives any other NPC one (".greeting").
    /// An NPC with none says "Greetings." - the line the client has fourteen copies of (93 to
    /// 95 and 109 to 119), which reads as what an NPC said that had been given nothing of its
    /// own.
    ///
    /// An NPC with a line of its own can be spoken to for it. With nothing else to talk about it
    /// has the client's greeting status (CONVO_STATUS_GREETING: no pip over its head, and the
    /// converse action) and answers with the line alone, which the client shows in its
    /// conversation window. This is what most of the lines were for - the soldiers and
    /// villagers who stand about and say one thing. An NPC with no line of its own is as it
    /// was: nothing to say, and not to be spoken to.
    ///
    /// A line can be marked important (npc_greeting.important, Npc.GreetingImportant), and the
    /// NPC then has the client's other greeting status in the same place, with nothing else to
    /// talk about: CONVO_STATUS_IMPORTANT_GREETING, which overheadwindow.py draws as
    /// OVERHEAD_DIALOG_AVAILABLE - a grey speech bubble over its head, in the manner of the
    /// mission radios - and its conversation is the line under CONVO_TYPE_IMPORTANT_GREETING.
    /// npc.py shows that one ahead of an ambient objective, a mission reminder and a shop, so
    /// it is sent only where the plain greeting was: as the whole conversation. Which NPCs were
    /// marked was the server's to know as well; a game master marks one (".greeting important"),
    /// and one starts marked, by footage of the live game (NpcGreetingSeed.Marked).
    ///
    /// The bubble is for a line the player has not read. Footage of the live game shows it gone
    /// from an NPC once the player has spoken to it, and not back. The client does none of
    /// that - it draws the bubble for as long as the status says so - so the server keeps who
    /// has read what (character_greeting_read, Manifestation.GreetingsRead): the NPC's creature
    /// row and the line it was. Being shown a marked line as the conversation is reading it
    /// (NpcManager.OpenConversation); from then on that character has the plain greeting
    /// status and the plain greeting of that NPC, which still says the line when asked. The
    /// line is kept with the row so that an NPC given another one is unread again - it has
    /// something new to say.
    /// </summary>
    public static class NpcGreetings
    {
        /// <summary>npcgreetinglanguage 93: "Greetings."</summary>
        public const int Default = 93;

        /// <summary>The types of topic the client counts, each by the entries of its payload (npc.py Recv_Converse).</summary>
        private static readonly ConversationType[] Counted =
        {
            ConversationType.MissionComplete, ConversationType.ObjectiveComplete, ConversationType.MissionReward,
            ConversationType.MissionDispense, ConversationType.MissionReminder, ConversationType.ObjectiveChoice,
            ConversationType.ObjectiveAmbient, ConversationType.Vending, ConversationType.Auctioneer
        };

        /// <summary>The ids the client's npcgreetinglanguage has, as ranges: any other prints "Missing translation" where the line should be.</summary>
        private static readonly (uint First, uint Last)[] Lines =
        {
            (1, 1), (3, 360), (362, 436), (440, 571), (574, 600), (603, 1270), (1272, 1272), (1277, 1277), (1279, 1279),
            (1282, 1710), (10000001, 10000002), (20000003, 20000003), (20000006, 20000008)
        };

        /// <summary>The greeting of an NPC: its own, or the default.</summary>
        public static int For(Creature creature) => HasOwn(creature) ? (int)creature.Npc.GreetingId : Default;

        /// <summary>Whether an NPC has a line of its own, and so can be spoken to for it.</summary>
        public static bool HasOwn(Creature creature) => creature?.Npc != null && creature.Npc.GreetingId != 0;

        /// <summary>Whether an NPC's own line is marked important: the speech bubble over its head.</summary>
        public static bool IsImportant(Creature creature) => HasOwn(creature) && creature.Npc.GreetingImportant;

        /// <summary>Whether the character has read the NPC's own line - the one it has now.</summary>
        public static bool HasRead(Manifestation player, Creature creature)
        {
            if (player == null || !HasOwn(creature))
                return false;

            lock (player.GreetingsRead)
                return player.GreetingsRead.TryGetValue(creature.DbId, out var line) && line == creature.Npc.GreetingId;
        }

        /// <summary>
        /// Whether the NPC has the speech bubble for this character: its line is marked
        /// important and the character has not read it.
        /// </summary>
        public static bool IsUnread(Manifestation player, Creature creature) =>
            IsImportant(creature) && !HasRead(player, creature);

        /// <summary>
        /// The character has read the NPC's line: kept for the character
        /// (character_greeting_read), in place of any line of that NPC read before. False if
        /// they had read it already, or there is no line of the NPC's own to have read.
        ///
        /// A read that cannot be saved still counts until the character logs out: the bubble
        /// they have just answered does not stay over the NPC.
        /// </summary>
        public static bool MarkRead(Manifestation player, Creature creature, IGameUnitOfWorkFactory factory)
        {
            if (player == null || !HasOwn(creature) || creature.DbId == 0)
                return false;

            lock (player.GreetingsRead)
            {
                if (player.GreetingsRead.TryGetValue(creature.DbId, out var line) && line == creature.Npc.GreetingId)
                    return false;

                if (player.Id != 0)
                {
                    try
                    {
                        using var unitOfWork = factory.CreateChar();
                        unitOfWork.CharacterGreetingReads.Set(player.Id, creature.DbId, creature.Npc.GreetingId);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Character {player.Id} reading greeting {creature.Npc.GreetingId} of creature {creature.DbId} was not saved: {e.Message}");
                    }
                }

                player.GreetingsRead[creature.DbId] = creature.Npc.GreetingId;

                return true;
            }
        }

        /// <summary>
        /// Forgets that the character has read the NPC, whichever line of it that was: its
        /// marked line is unread for them again. False if they had read none or it could not
        /// be taken out.
        /// </summary>
        public static bool Forget(Manifestation player, Creature creature, IGameUnitOfWorkFactory factory)
        {
            if (player == null || creature == null || creature.DbId == 0)
                return false;

            lock (player.GreetingsRead)
            {
                if (!player.GreetingsRead.ContainsKey(creature.DbId))
                    return false;

                if (player.Id != 0)
                {
                    try
                    {
                        using var unitOfWork = factory.CreateChar();
                        unitOfWork.CharacterGreetingReads.Remove(player.Id, creature.DbId);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Character {player.Id} having read creature {creature.DbId} was not forgotten: {e.Message}");
                        return false;
                    }
                }

                return player.GreetingsRead.Remove(creature.DbId);
            }
        }

        /// <summary>Whether the client has a greeting of this id.</summary>
        public static bool IsLine(uint greetingId)
        {
            foreach (var (first, last) in Lines)
                if (greetingId >= first && greetingId <= last)
                    return true;

            return false;
        }

        /// <summary>
        /// Gives an NPC a line of its own, kept in the world database (npc_greeting) for its
        /// creature row: every NPC of that row has it, now and after a restart. False if the
        /// client has no such line, the creature is no NPC, or it could not be kept.
        /// </summary>
        public static bool Set(Creature creature, uint greetingId, IGameUnitOfWorkFactory factory)
        {
            if (creature?.Npc == null || !IsLine(greetingId))
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();
                unitOfWork.Creatures.SaveNpcGreeting(creature.DbId, greetingId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"The greeting of creature {creature.DbId} was not saved: {e.Message}");
                return false;
            }

            creature.Npc.GreetingId = greetingId;

            return true;
        }

        /// <summary>Takes an NPC's own line away, in the world database too: it says the default again. False if it had none or it could not be done.</summary>
        public static bool Clear(Creature creature, IGameUnitOfWorkFactory factory)
        {
            if (!HasOwn(creature))
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();
                unitOfWork.Creatures.DeleteNpcGreeting(creature.DbId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"The greeting of creature {creature.DbId} was not taken away: {e.Message}");
                return false;
            }

            creature.Npc.GreetingId = 0;
            creature.Npc.GreetingImportant = false;

            return true;
        }

        /// <summary>
        /// Marks an NPC's own line important, or plain again, kept in the world database with
        /// the line (npc_greeting.important). False if it has no line of its own - there is
        /// nothing to mark - or it could not be kept.
        /// </summary>
        public static bool SetImportant(Creature creature, bool important, IGameUnitOfWorkFactory factory)
        {
            if (!HasOwn(creature))
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();

                if (!unitOfWork.Creatures.SaveNpcGreetingImportant(creature.DbId, important))
                    return false;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"The greeting of creature {creature.DbId} was not marked: {e.Message}");
                return false;
            }

            creature.Npc.GreetingImportant = important;

            return true;
        }

        /// <summary>
        /// How many topics the client counts in a conversation: the missions it can complete,
        /// reward, give and remind of, the objectives it completes, chooses and talks about, its
        /// vendor packages, and one for an auctioneer. Training and the clan registrar are not
        /// counted - the client opens those from the NPC's status, with no list.
        /// </summary>
        public static int Topics(IReadOnlyDictionary<ConversationType, object> conversation)
        {
            if (conversation == null)
                return 0;

            var topics = 0;

            foreach (var type in Counted)
            {
                if (!conversation.TryGetValue(type, out var payload))
                    continue;

                topics += payload switch
                {
                    bool flag => flag ? 1 : 0,
                    ICollection collection => collection.Count,
                    _ => 0
                };
            }

            return topics;
        }

        /// <summary>
        /// Gives a conversation the NPC's greeting if the client will open the topic list for
        /// it: more than one topic. A conversation of one topic goes straight to it and reads no
        /// greeting, and is left as it is. Returns whether it was added.
        /// </summary>
        public static bool AddTo(Dictionary<ConversationType, object> conversation, Creature creature)
        {
            if (conversation == null || conversation.ContainsKey(ConversationType.Greeting) || Topics(conversation) < 2)
                return false;

            conversation.Add(ConversationType.Greeting, For(creature));

            return true;
        }
    }
}
