using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Communicator.Server;

    /// <summary>
    /// A message from the game masters that a player can't miss, in the two ways the client
    /// shows what the server says without being asked:
    ///
    ///  - the yellow ADMIN MESSAGE line in chat (CommunicatorManager.AdminMessage), on a filter
    ///    no player can turn off; and
    ///  - the "AFS Message" notice: Recv_PreviewMOTD hands the text to the status updater,
    ///    which puts a flashing AFS
    ///    Message icon up until the player clicks it and reads the text in the Message of the
    ///    Day window. It is the message of the day's own slot, so a newer one of either replaces
    ///    an unread older one; the message of the day comes round again at the next login.
    ///
    /// An important one opens the client's modal dialog with an OK button, in place of the AFS Message
    /// (Recv_NonFatalError), which every player gets - it is not a GM-mode
    /// window - and which holds the UI until they press OK. Its text has to be one of the
    /// client's player messages; PM_MISSION_COUNTER_UPDATED_NO_TARGET (642) is nothing but
    /// "%(counterName)s: %(curValue)s", so it carries any title and text.
    ///
    /// Petition answers (PetitionManager), .message and POST /sendmessage go this way.
    /// </summary>
    public static class GmMessages
    {
        /// <summary>The longest message .message and /sendmessage take.</summary>
        public const int MaxLength = 1000;

        /// <summary>The longest title an important message's dialog takes.</summary>
        public const int MaxTitleLength = 64;

        public const string DefaultTitle = "Message from a game master";

        /// <summary>The line in chat, and the AFS Message notice with the whole message.</summary>
        public static void Send(Client client, string chatLine, string message)
        {
            if (client == null)
                return;

            CommunicatorManager.Instance.AdminMessage(client, chatLine);
            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket(message));
        }

        /// <summary>
        /// A note from a game master: in chat, and as an AFS Message - or, when it is important,
        /// in the modal dialog in place of the AFS Message, which would only say it again.
        /// </summary>
        public static void FromGm(Client client, string message, bool important = false, string title = null)
        {
            var chatLine = "A game master has sent you a message: " + message;

            if (!important)
            {
                Send(client, chatLine, "Message from a game master:\n\n" + message);
                return;
            }

            CommunicatorManager.Instance.AdminMessage(client, chatLine);
            Dialog(client, string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim(), message);
        }

        /// <summary>The client's modal "title: text" dialog with an OK button (PM 642, Recv_NonFatalError).</summary>
        public static void Dialog(Client client, string title, string text) =>
            client?.CallMethod(SysEntity.ClientMethodId, new NonFatalErrorPacket(PlayerMessage.PmMissionCounterUpdatedNoTarget,
                new Dictionary<string, string> { ["counterName"] = title, ["curValue"] = text }));

        /// <summary>Several notices at once, one chat line each and one AFS Message holding them all.</summary>
        public static void SendAll(Client client, IReadOnlyList<string> notices)
        {
            if (client == null || notices.Count == 0)
                return;

            foreach (var notice in notices)
                CommunicatorManager.Instance.AdminMessage(client, notice);

            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket(string.Join("\n\n", notices)));
        }
    }
}
