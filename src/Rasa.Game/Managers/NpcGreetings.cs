using System.Collections;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
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
    /// and nothing that ties one to an NPC, an NPC package or a place. So every NPC is given the
    /// same one, "Greetings." - the line the client has fourteen copies of (93 to 95 and 109 to
    /// 119), which reads as what an NPC said that had been given nothing of its own.
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

        /// <summary>The greeting of an NPC.</summary>
        public static int For(Creature creature) => Default;

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
