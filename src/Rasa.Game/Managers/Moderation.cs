using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// Game master tools that act on other players: announcements to the whole server, kicking
    /// a player off, and silencing one's chat. In game as .announce, .kick, .mute and .unmute
    /// (GameMaster level), and on the console as announce, kick, mute and unmute.
    ///
    /// Silencing uses the client's own messages for it (PM_GM_YOU_ARE_SILENCED and the rest), and
    /// is kept on the account (account.muted_until), so logging out, switching character or a
    /// restart does not lift it; it can be put on an account that is not online. A silenced
    /// player cannot say, shout, emote, or use squad, clan, channel or whisper chat - except a
    /// whisper to a game master. Dot commands and petitions still work. When the time is up the
    /// player is told so.
    ///
    /// A game master in game cannot kick or silence an account of their own level or above; the
    /// console can.
    /// </summary>
    public static class Moderation
    {
        /// <summary>The longest silence: a year.</summary>
        public const int MaxMuteMinutes = 525_600;

        /// <summary>How long after the notice a kicked connection is closed, so the notice gets there.</summary>
        public const long KickDelayMs = 1000;

        public static Func<long> UnixNow { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>An account that is not online, by family name; replaced in tests.</summary>
        public static Func<string, GameAccountEntry> FindStoredAccount { get; set; } = familyName =>
        {
            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateChar();
            return unitOfWork.GameAccounts.FindByFamilyName(familyName);
        };

        /// <summary>Writes an account's muted_until; replaced in tests.</summary>
        public static Action<uint, long> PersistMutedUntil { get; set; } = (accountId, mutedUntil) =>
        {
            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateChar();
            unitOfWork.GameAccounts.UpdateMutedUntil(accountId, mutedUntil);
        };

        /// <summary>
        /// What a command reports back: whether it did anything, a line for whoever asked, and
        /// whether a game master who asked has been told already in the client's own words.
        /// </summary>
        public readonly struct Result
        {
            public Result(bool done, string text, bool told = false)
            {
                Done = done;
                Text = text;
                Told = told;
            }

            public bool Done { get; }
            public string Text { get; }
            public bool Told { get; }
        }

        #region Announcements

        public const string AnnouncementPrefix = "[Announcement] ";

        /// <summary>A line in chat for everyone in the world, or on their way into it. Returns how many.</summary>
        public static int Announce(string text)
        {
            text = text?.Trim();

            if (string.IsNullOrEmpty(text))
                return 0;

            var message = AnnouncementPrefix + text;
            var listeners = Connected().Where(c => c.State == ClientState.Ingame || c.State == ClientState.Loading || c.State == ClientState.Teleporting).ToList();

            foreach (var client in listeners)
                Send(client, () => CommunicatorManager.Instance.SystemMessage(client, message));

            Logger.WriteLog(LogType.Command, $"Announced to {listeners.Count} player(s): {text}");
            return listeners.Count;
        }

        #endregion

        #region Kicking

        /// <summary>
        /// Disconnects every connection of the account with this family name - in the world or at
        /// character selection - after telling the player why. The character leaves as it would on
        /// a dropped connection, but never lingers in a fight (CombatLogout).
        /// </summary>
        public static Result Kick(string familyName, string reason, Client by)
        {
            var targets = Online(familyName);

            if (targets.Count == 0)
                return new Result(false, $"{familyName} is not online.");

            var account = targets[0].AccountEntry;

            if (!MayActOn(by, account))
                return new Result(false, $"You cannot kick {account.FamilyName}: their account level is not below yours.");

            return Kick(targets, reason, Who(by));
        }

        /// <summary>
        /// Disconnects these connections - all of one account's, from <see cref="Online"/> or
        /// <see cref="OnlineAccount"/> - after telling the player why; <paramref name="who"/> is
        /// whoever did it, for the server log. Whether they may is the caller's to have asked.
        /// </summary>
        public static Result Kick(List<Client> targets, string reason, string who)
        {
            if (targets == null || targets.Count == 0)
                return new Result(false, "Nobody is online to kick.");

            var account = targets[0].AccountEntry;

            reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            var notice = "You have been disconnected by a game master" + (reason == null ? "." : $": {reason}");

            foreach (var target in targets)
            {
                target.SkipCombatLinger = true;
                Send(target, () => CommunicatorManager.Instance.SystemMessage(target, notice));
                CloseSoon(target);
            }

            Logger.WriteLog(LogType.Security,
                $"{who} kicked {account.FamilyName} (account {account.Id})" + (reason == null ? "." : $": {reason}"));

            return new Result(true, $"Kicked {account.FamilyName}.");
        }

        private static void CloseSoon(Client target)
        {
            var timer = target.Server?.Timer;

            if (timer == null)
            {
                target.Close();
                return;
            }

            timer.Add($"Kick:{target.AccountEntry?.Id}:{Environment.TickCount64}:{target.GetHashCode()}", KickDelayMs, false, () => target.Close());
        }

        #endregion

        #region Silencing

        public static bool IsMuted(GameAccountEntry account, long nowUnixMs) => account != null && account.MutedUntil > nowUnixMs;

        /// <summary>Whole minutes left of a silence, rounded up.</summary>
        public static long MinutesLeft(long mutedUntil, long nowUnixMs) => Math.Max(0, (mutedUntil - nowUnixMs + 59_999) / 60_000);

        /// <summary>
        /// For the chat handlers: true, with the player told they are silenced, when this player
        /// may not chat; false when they may.
        /// </summary>
        public static bool RefuseIfMuted(Client client)
        {
            if (!IsMuted(client?.AccountEntry, UnixNow()))
                return false;

            Tell(client, PlayerMessage.PmGmYouAreStillSilenced, new Dictionary<string, string>());
            return true;
        }

        /// <summary>Silences the account with this family name for so many minutes, online or not.</summary>
        public static Result Mute(string familyName, int minutes, string reason, Client by)
        {
            if (minutes < 1 || minutes > MaxMuteMinutes)
                return new Result(false, $"Minutes must be 1 to {MaxMuteMinutes}.");

            var online = Online(familyName);
            var account = online.FirstOrDefault()?.AccountEntry ?? FindStoredAccount(familyName);

            if (account == null)
                return new Result(false, $"There is no family named {familyName}.");

            if (!MayActOn(by, account))
                return new Result(false, $"You cannot silence {account.FamilyName}: their account level is not below yours.");

            var until = UnixNow() + minutes * 60_000L;

            try
            {
                PersistMutedUntil(account.Id, until);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not save the silence of {account.FamilyName}: {e.Message}");
                return new Result(false, $"Could not silence {account.FamilyName}: the database refused it.");
            }

            reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            var duration = new Dictionary<string, string> { ["duration"] = minutes.ToString() };

            foreach (var client in online)
            {
                client.AccountEntry.MutedUntil = until;
                Tell(client, PlayerMessage.PmGmYouAreSilenced, duration);

                if (reason != null)
                    Send(client, () => CommunicatorManager.Instance.SystemMessage(client, $"Reason: {reason}"));
            }

            if (by != null)
                Tell(by, PlayerMessage.PmGmUserSilenced, new Dictionary<string, string> { ["username"] = account.FamilyName, ["duration"] = minutes.ToString() });

            Logger.WriteLog(LogType.Security,
                $"{Who(by)} silenced {account.FamilyName} (account {account.Id}) for {minutes} minute(s)" + (reason == null ? "." : $": {reason}"));

            return new Result(true, $"{account.FamilyName} is silenced for {minutes} minute(s).", by != null);
        }

        /// <summary>Lifts a silence from the account with this family name, online or not.</summary>
        public static Result Unmute(string familyName, Client by)
        {
            var online = Online(familyName);
            var account = online.FirstOrDefault()?.AccountEntry ?? FindStoredAccount(familyName);

            if (account == null)
                return new Result(false, $"There is no family named {familyName}.");

            if (!IsMuted(account, UnixNow()))
            {
                if (by != null)
                    Tell(by, PlayerMessage.PmGmUserNotInSilencedList, new Dictionary<string, string> { ["username"] = account.FamilyName });

                return new Result(false, $"{account.FamilyName} is not silenced.", by != null);
            }

            try
            {
                PersistMutedUntil(account.Id, 0);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not save lifting the silence of {account.FamilyName}: {e.Message}");
                return new Result(false, $"Could not lift {account.FamilyName}'s silence: the database refused it.");
            }

            foreach (var client in online)
            {
                client.AccountEntry.MutedUntil = 0;
                Tell(client, PlayerMessage.PmGmYouAreUnsilenced, new Dictionary<string, string>());
            }

            if (by != null)
                Tell(by, PlayerMessage.PmGmUserUnsilenced, new Dictionary<string, string> { ["username"] = account.FamilyName });

            Logger.WriteLog(LogType.Security, $"{Who(by)} lifted the silence of {account.FamilyName} (account {account.Id}).");

            return new Result(true, $"{account.FamilyName} is no longer silenced.", by != null);
        }

        /// <summary>
        /// Every second, per map: a player whose silence has run out is told so. The stored value is
        /// left as it is - a time in the past is no silence.
        /// </summary>
        public static int Worker(MapChannel mapChannel)
        {
            if (mapChannel?.ClientList == null)
                return 0;

            var now = UnixNow();
            var lifted = 0;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var account = client?.AccountEntry;

                if (account == null || account.MutedUntil == 0 || account.MutedUntil > now || client.State != ClientState.Ingame)
                    continue;

                account.MutedUntil = 0;
                Tell(client, PlayerMessage.PmGmYouAreUnsilenced, new Dictionary<string, string>());
                lifted++;
            }

            return lifted;
        }

        /// <summary>On entering the world: a silenced player is reminded they are.</summary>
        public static void OnEnteredWorld(Client client)
        {
            if (IsMuted(client?.AccountEntry, UnixNow()))
                Tell(client, PlayerMessage.PmGmYouAreStillSilenced, new Dictionary<string, string>());
        }

        #endregion

        /// <summary>Every connection past login of the account with this family name (any case).</summary>
        public static List<Client> Online(string familyName)
        {
            if (string.IsNullOrWhiteSpace(familyName))
                return new List<Client>();

            familyName = familyName.Trim();

            var matches = Connected().Where(c => string.Equals(c.AccountEntry.FamilyName, familyName, StringComparison.OrdinalIgnoreCase)).ToList();

            // One account is meant; an exact spelling wins over an older account differing only in case.
            var exact = matches.Where(c => string.Equals(c.AccountEntry.FamilyName, familyName, StringComparison.Ordinal)).ToList();
            var chosen = exact.Count > 0 ? exact : matches;
            var accountId = chosen.FirstOrDefault()?.AccountEntry.Id;

            return chosen.Where(c => c.AccountEntry.Id == accountId).ToList();
        }

        /// <summary>The connections of the account with this id that are online.</summary>
        public static List<Client> OnlineAccount(uint accountId) =>
            Connected().Where(c => c.AccountEntry.Id == accountId).ToList();

        private static List<Client> Connected()
        {
            lock (Server.Clients)
                return Server.Clients.Where(c => c?.AccountEntry != null && c.State != ClientState.Disconnected).ToList();
        }

        /// <summary>The console may act on anyone; a game master only on accounts below their own level.</summary>
        private static bool MayActOn(Client by, GameAccountEntry target) =>
            by == null || (by.AccountEntry != null && target.Level < by.AccountEntry.Level);

        private static string Who(Client by) => by == null ? "The console" : $"{by.AccountEntry?.FamilyName} (account {by.AccountEntry?.Id})";

        private static void Tell(Client client, PlayerMessage message, Dictionary<string, string> args) =>
            Send(client, () => client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, args, MsgFilterId.GeneralSystemMessages)));

        private static void Send(Client client, Action send)
        {
            try
            {
                send();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not send a moderation notice to {client?.AccountEntry?.FamilyName}: {e.Message}");
            }
        }
    }
}
