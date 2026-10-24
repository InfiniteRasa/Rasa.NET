using System.Text.RegularExpressions;

namespace Rasa.Managers
{
    using Data;
    using Game;

    /// <summary>
    /// What a chat line looks like when it reaches the client.
    ///
    /// The chat window shows rich text: a &lt;font color="..."&gt; tag colors what it holds
    /// (named colors such as Chat_Yellow, or #rrggbb), and the sender's name is wrapped in an
    /// &lt;a action="..."&gt; link. Say,
    /// whisper and squad chat hand the same line to the bubble over the speaker
    /// (UI_DISPLAY_CHAT_BUBBLE), which shows markup as it is, and there is no other way to put
    /// a bubble over a player. So:
    ///
    ///  - A game master's line (GameMaster or above) starts with [GM] in every kind of chat:
    ///    plain in say, whisper and squad chat, so it reads right in their bubble, and in shout,
    ///    channel and clan chat, which have no bubble, the whole line is yellow. Never in the
    ///    sender's name: markup there breaks its whisper link.
    ///  - Anything a player types that would read as a tag is broken up ("&lt;font" becomes
    ///    "&lt; font"), so nobody can color their text, fake a [GM] or slip in a link; neither the
    ///    client nor its text filter takes markup out. Breaking it up rather than escaping keeps
    ///    it readable in a bubble, which would show an escape as it is.
    ///
    /// Observers are not set apart. The chat log keeps the line as it was typed.
    /// </summary>
    public static class ChatMarkup
    {
        public const string GmOpen = "<font color=\"Chat_Yellow\">[GM] ";
        public const string GmClose = "</font>";
        public const string PlainGmTag = "[GM] ";

        private static readonly Regex TagStart = new Regex("<(?=[/!?A-Za-z])", RegexOptions.Compiled);

        public static bool IsGm(Client sender) => (sender?.AccountEntry?.Level ?? 0) >= (byte)GmLevel.GameMaster;

        /// <summary>A line for the chat window alone (shout, channels, clan): a GM's all yellow behind [GM].</summary>
        public static string Outgoing(Client sender, string message)
        {
            var text = Neutralize(message);

            return IsGm(sender) ? GmOpen + text + GmClose : text;
        }

        /// <summary>A line that is also a bubble (say, whisper, squad): a GM's behind a plain [GM].</summary>
        public static string OutgoingWithBubble(Client sender, string message)
        {
            var text = Neutralize(message);

            return IsGm(sender) ? PlainGmTag + text : text;
        }

        /// <summary>Whatever a player typed that would read as a tag, broken up so it shows as typed.</summary>
        public static string Neutralize(string message) => message == null ? null : TagStart.Replace(message, "< ");
    }
}
