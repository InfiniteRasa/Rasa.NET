using System.Collections.Generic;

namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's MessageOfTheDay: the message the client shows in its message-of-the-day
    /// window after logging in (Managers.MessageOfTheDay). Changes to the file apply at once,
    /// and players already in the world are shown the new message.
    /// </summary>
    public class MessageOfTheDayConfig
    {
        public const string DefaultText = "Welcome to the Infinite Rasa server.";

        /// <summary>The message, in English - the one every client falls back to. Empty for none.</summary>
        public string Text { get; set; } = DefaultText;

        /// <summary>
        /// False: each player sees a message once, at their first login after it changes (the
        /// client remembers the last one it showed). True: every login shows it.
        /// </summary>
        public bool ShowEveryLogin { get; set; }

        /// <summary>
        /// The message in other languages, by the client's language id (generated.client.
        /// languageidentifiers: 5 French, 6 German, ...). A client set to a language not here gets
        /// Text. Only used when ShowEveryLogin is false.
        /// </summary>
        public Dictionary<int, string> Translations { get; set; } = new();
    }
}
