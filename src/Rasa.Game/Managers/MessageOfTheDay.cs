using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets;
    using Packets.Communicator.Server;

    /// <summary>
    /// The message of the day, from appsettings.json's MessageOfTheDay (MessageOfTheDayConfig).
    ///
    /// It used to be one hard-coded line sent as PreviewMOTD - which the client shows whatever
    /// it has shown before - on every LoginOk, so on every login and again after every dropship
    /// ride. Now:
    ///  - it is sent once per connection, as the player first enters the world;
    ///  - by default as SendMOTD, a message per language that the client shows only when it
    ///    differs from the last one it showed (messageoftheday.py DisplayMessage keeps it in the
    ///    client's options) - so a player sees each new message once. ShowEveryLogin sends
    ///    PreviewMOTD instead, shown every time;
    ///  - an empty Text sends nothing;
    ///  - a change to the file goes to everyone in the world at once (Server.ConfigLoaded).
    /// </summary>
    public static class MessageOfTheDay
    {
        private static MessageOfTheDayConfig _current = new MessageOfTheDayConfig();

        public static MessageOfTheDayConfig Current => Volatile.Read(ref _current);

        /// <summary>Puts a newly loaded config in force. True when the message is not what it was.</summary>
        public static bool Apply(MessageOfTheDayConfig config)
        {
            config ??= new MessageOfTheDayConfig();

            var old = Interlocked.Exchange(ref _current, config);

            return Describe(old) != Describe(config);
        }

        private static string Describe(MessageOfTheDayConfig config) =>
            $"{config.ShowEveryLogin}|{config.Text}|" + string.Join("|", Messages(config).OrderBy(m => m.Key).Select(m => $"{m.Key}={m.Value}"));

        public static bool HasMessage(MessageOfTheDayConfig config) => !string.IsNullOrWhiteSpace(config?.Text);

        /// <summary>The message by client language id: English (1) is Text, the rest the translations given.</summary>
        public static Dictionary<int, string> Messages(MessageOfTheDayConfig config)
        {
            var messages = new Dictionary<int, string>();

            if (!HasMessage(config))
                return messages;

            foreach (var (languageId, text) in config.Translations ?? new Dictionary<int, string>())
                if (languageId != SendMOTDPacket.English && !string.IsNullOrWhiteSpace(text))
                    messages[languageId] = text;

            messages[SendMOTDPacket.English] = config.Text;
            return messages;
        }

        /// <summary>What to send for a config: null for no message.</summary>
        public static PythonPacket PacketFor(MessageOfTheDayConfig config)
        {
            if (!HasMessage(config))
                return null;

            return config.ShowEveryLogin
                ? new PreviewMOTDPacket(config.Text)
                : new SendMOTDPacket(Messages(config));
        }

        /// <summary>To a player entering the world: the message, once per connection.</summary>
        public static void SendOnLogin(Client client)
        {
            if (client == null || client.MotdSent)
                return;

            client.MotdSent = true;

            var packet = PacketFor(Current);

            if (packet != null)
                client.CallMethod(SysEntity.CommunicatorId, packet);
        }

        /// <summary>The message as it is, shown whatever the client has seen (.motd).</summary>
        public static bool Preview(Client client)
        {
            var config = Current;

            if (!HasMessage(config))
                return false;

            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket(config.Text));
            return true;
        }

        /// <summary>A changed message to everyone in the world. Returns how many were sent it.</summary>
        public static int SendToAll(IEnumerable<Client> clients)
        {
            var packet = PacketFor(Current);

            if (packet == null)
                return 0;

            var sent = 0;

            foreach (var client in clients.Where(c => c?.State == ClientState.Ingame))
            {
                try
                {
                    client.CallMethod(SysEntity.CommunicatorId, packet);
                    client.MotdSent = true;
                    sent++;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Could not send the message of the day: {e.Message}");
                }
            }

            return sent;
        }
    }
}
