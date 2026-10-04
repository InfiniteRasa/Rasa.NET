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
