using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Structures;

    /// <summary>
    /// Scriptable client events: the server asks the client to report a player action
    /// (StartTrackingScriptableClientEvent), the client sends ScriptableClientEvent each time the
    /// player does it, and StopTrackingScriptableClientEvent ends that. Retail used them for tutorial
    /// objectives (WASD, crouch, open the mission log, equip the gear...); the scripts that asked
    /// for them are gone, so here they are a hook: <see cref="Start"/> and <see cref="Stop"/> from
    /// anywhere, <see cref="Occurred"/> to hear back, and .clientevent to drive them by hand.
    ///
    /// The server's list lives on the Manifestation and the client's until character select, so
    /// both last the same session: a map change keeps them, a relog clears them.
    /// </summary>
    public static class ScriptableClientEvents
    {
        /// <summary>A tracked event came back from the player's client. Runs on that client's packet thread.</summary>
        public static event Action<Client, ScriptableClientEvent> Occurred;

        public static bool IsDefined(uint eventId) => Enum.IsDefined(typeof(ScriptableClientEvent), eventId);

        /// <summary>Asks the client to report the event; whether it was sent. Sent again when already tracked (the client ignores the repeat).</summary>
        public static bool Start(Client client, ScriptableClientEvent eventId)
        {
            var player = client?.Player;

            if (player == null || !IsDefined((uint)eventId))
                return false;

            lock (player.TrackedClientEvents)
                player.TrackedClientEvents.Add(eventId);

            client.CallMethod(SysEntity.ClientMethodId, new StartTrackingScriptableClientEventPacket(eventId));
            return true;
        }

        /// <summary>Asks the client to stop reporting the event; whether it was sent.</summary>
        public static bool Stop(Client client, ScriptableClientEvent eventId)
        {
            var player = client?.Player;

            if (player == null || !IsDefined((uint)eventId))
                return false;

            lock (player.TrackedClientEvents)
                player.TrackedClientEvents.Remove(eventId);

            client.CallMethod(SysEntity.ClientMethodId, new StopTrackingScriptableClientEventPacket(eventId));
            return true;
        }

        /// <summary>Stops every event the player's client is tracking; how many.</summary>
        public static int StopAll(Client client)
        {
            var tracked = Tracked(client?.Player);

            foreach (var eventId in tracked)
                Stop(client, eventId);

            return tracked.Count;
        }

        public static bool IsTracking(Manifestation player, ScriptableClientEvent eventId)
        {
            if (player == null)
                return false;

            lock (player.TrackedClientEvents)
                return player.TrackedClientEvents.Contains(eventId);
        }

        /// <summary>The events the player's client is tracking, in id order.</summary>
        public static IReadOnlyList<ScriptableClientEvent> Tracked(Manifestation player)
        {
            if (player == null)
                return Array.Empty<ScriptableClientEvent>();

            lock (player.TrackedClientEvents)
                return player.TrackedClientEvents.OrderBy(eventId => eventId).ToList();
        }

        /// <summary>ScriptableClientEvent from the client. Unknown or untracked ids are logged and dropped.</summary>
        public static void Received(Client client, uint eventId)
        {
            var player = client?.Player;

            if (player == null)
                return;

            if (!IsDefined(eventId))
            {
                Logger.WriteLog(LogType.Debug, $"ScriptableClientEvent: {player.Name} sent unknown event {eventId}");
                return;
            }

            var scriptableEvent = (ScriptableClientEvent)eventId;

            if (!IsTracking(player, scriptableEvent))
            {
                Logger.WriteLog(LogType.Debug, $"ScriptableClientEvent: {player.Name} sent {scriptableEvent}, which is not tracked");
                return;
            }

            if (player.EchoClientEvents)
                CommunicatorManager.Instance.SystemMessage(client, $"Client event {eventId} {scriptableEvent}.");

            var handlers = Occurred;

            if (handlers == null)
                return;

            foreach (Action<Client, ScriptableClientEvent> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(client, scriptableEvent);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"ScriptableClientEvent: a handler of {scriptableEvent} for {player.Name} threw: {e}");
                }
            }
        }

        /// <summary>An event by id or by name, with or without underscores (8, Crouched, CROUCHED, move_forward).</summary>
        public static bool TryParse(string text, out ScriptableClientEvent eventId)
        {
            eventId = default;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (uint.TryParse(text, out var id))
            {
                eventId = (ScriptableClientEvent)id;
                return IsDefined(id);
            }

            var name = text.Replace("_", "");

            foreach (ScriptableClientEvent value in Enum.GetValues(typeof(ScriptableClientEvent)))
            {
                if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    eventId = value;
                    return true;
                }
            }

            return false;
        }
    }
}
