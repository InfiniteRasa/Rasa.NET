using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Rasa.Voice
{
    using Config;

    /// <summary>
    /// A Talkback voice server for squad voice chat (see <see cref="Talkback"/> for the wire format).
    ///
    /// The client does everything audio: capture, push-to-talk, the codec (it asks for Speex), the
    /// mixing and playback. What it needs from a server is somebody to log in to and somebody to
    /// pass its voice packets on to the rest of the squad, and that is all this does. It never
    /// looks inside the audio.
    ///
    /// How a player gets here: the world server tells a squad VoiceChatAvailable(true); the client
    /// sends RequestJoinVoiceChannel; PartyManager asks <see cref="IssueTicket"/> for a token and
    /// answers VoiceChatConnectInfo("host:port", squad id, account id, token); the client connects
    /// over UDP and logs in to the group with those three numbers. One group per squad, one
    /// player id per account. Leaving the squad, the world or the voice channel ends it
    /// (<see cref="Leave"/>, <see cref="GroupDisbanded"/>).
    ///
    /// Threads: a receive thread of its own and a one-second housekeeping timer, with every piece
    /// of state behind one lock; the world server's calls in come from its main loop.
    /// </summary>
    public sealed class VoiceServer
    {
        public static VoiceServer Instance { get; } = new VoiceServer();

        /// <summary>shared/gameconstants.py MAX_PARTY_SIZE. Also the size of the client's per-member tables.</summary>
        public const int MaxMembers = 6;

        /// <summary>The client asks for codec 5 (Speex); what it asks for is what it gets.</summary>
        public const byte DefaultCodec = 5;

        private const int PingIntervalMs = 5000;
        private const int HandshakeTimeoutMs = 10000;

        private readonly object _lock = new object();
        private readonly Dictionary<IPEndPoint, Session> _sessions = new Dictionary<IPEndPoint, Session>();
        private readonly Dictionary<uint, Session[]> _groups = new Dictionary<uint, Session[]>();
        private readonly Dictionary<ulong, Ticket> _tickets = new Dictionary<ulong, Ticket>();
        private readonly byte[] _keyOffer = Talkback.KeyOffer();
        private readonly Random _random = new Random();

        private VoiceConfig _config = new VoiceConfig();
        private string _publicHost = "127.0.0.1";
        private Socket _socket;
        private Thread _receiveThread;
        private System.Threading.Timer _housekeeping;
        private IPEndPoint _boundTo;

        private VoiceServer()
        {
        }

        private enum SessionState
        {
            KeyOffered,
            KeyAccepted,
            Connected
        }

        private sealed class Session
        {
            public IPEndPoint EndPoint;
            public SessionState State;
            public long CreatedTick;
            public long LastHeardTick;
            public long LastPingTick;

            public bool LoggedIn;
            public uint GroupId;
            public ulong PlayerId;
            public byte LocalId;
            public byte Codec;

            public bool Speaking;
            public long SpeakingSinceTick;
            public ushort EndSeq;
        }

        private sealed class Ticket
        {
            public uint GroupId;
            public uint Token;
            public long ExpiresTick;
        }

        #region Configuration

        /// <summary>Whether squads can be offered voice: configured on and listening.</summary>
        public bool Available
        {
            get
            {
                lock (_lock)
                    return _config.Enabled && _socket != null;
            }
        }

        /// <summary>What clients are told to connect to, "host:port".</summary>
        public string ClientAddress
        {
            get
            {
                lock (_lock)
                    return $"{_publicHost}:{_boundTo?.Port ?? _config.Port}";
            }
        }

        /// <summary>
        /// Takes a (re)loaded VoiceConfig. Starts the listener when voice is on and it is not yet
        /// running, restarts it for a new address or port, and stops it - closing every voice
        /// connection - when voice is turned off. Returns false if it should be listening and
        /// could not bind.
        /// </summary>
        public bool Apply(VoiceConfig config, string worldPublicAddress)
        {
            config ??= new VoiceConfig();

            lock (_lock)
            {
                _config = config;
                _publicHost = string.IsNullOrWhiteSpace(config.PublicAddress)
                    ? (string.IsNullOrWhiteSpace(worldPublicAddress) ? "127.0.0.1" : worldPublicAddress.Trim())
                    : config.PublicAddress.Trim();
            }

            if (!config.Enabled)
            {
                if (IsListening)
                {
                    Stop();
                    Logger.WriteLog(LogType.Initialize, "Voice chat turned off; the voice server has stopped.");
                }

                return true;
            }

            var bind = BindEndPoint(config);

            if (bind == null)
            {
                Logger.WriteLog(LogType.Error, $"VoiceConfig.BindAddress '{config.BindAddress}' is not an IP address; voice chat stays off.");
                Stop();
                return false;
            }

            lock (_lock)
            {
                if (_socket != null && bind.Equals(_boundTo))
                    return true;
            }

            Stop();

            return Start(bind);
        }

        private static IPEndPoint BindEndPoint(VoiceConfig config)
        {
            if (config.Port <= 0 || config.Port > 65535)
                return null;

            var address = IPAddress.Any;

            if (!string.IsNullOrWhiteSpace(config.BindAddress) && !IPAddress.TryParse(config.BindAddress.Trim(), out address))
                return null;

            return new IPEndPoint(address, config.Port);
        }

        public bool IsListening
        {
            get
            {
                lock (_lock)
                    return _socket != null;
            }
        }

        private bool Start(IPEndPoint bind)
        {
            Socket socket;

            try
            {
                socket = new Socket(bind.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                // Windows reports an ICMP port-unreachable from an earlier send as a reset on the
                // next receive; a client closing its game would otherwise stop the listener.
                if (OperatingSystem.IsWindows())
                {
                    const int SIO_UDP_CONNRESET = -1744830452;
                    socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
                }

                socket.Bind(bind);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Voice server: unable to bind UDP {bind}: {e.Message}. Voice chat stays off.");
                return false;
            }

            lock (_lock)
            {
                _socket = socket;
                _boundTo = (IPEndPoint)socket.LocalEndPoint;
            }

            _receiveThread = new Thread(() => ReceiveLoop(socket)) { IsBackground = true, Name = "Voice server" };
            _receiveThread.Start();

            _housekeeping = new System.Threading.Timer(_ => Housekeeping(), null, 1000, 1000);

            Logger.WriteLog(LogType.Network, $"*** Voice chat listening on UDP port {_boundTo.Port}; clients are sent {ClientAddress}");

            return true;
        }

        /// <summary>Closes every voice connection and the socket.</summary>
        public void Stop()
        {
            Socket socket;

            lock (_lock)
            {
                socket = _socket;

                if (socket == null)
                    return;

                foreach (var session in _sessions.Values)
                    SendRaw(session.EndPoint, Talkback.FlagClose, ReadOnlySpan<byte>.Empty);

                _sessions.Clear();
                _groups.Clear();
                _tickets.Clear();
                _socket = null;
                _boundTo = null;
            }

            _housekeeping?.Dispose();
            _housekeeping = null;

            try
            {
                socket.Close();
            }
            catch (Exception)
            {
                // Closing is all that is wanted of it.
            }
        }

        #endregion

        #region World server side

        /// <summary>
        /// A login token for one player in one group, good for TokenLifetimeSeconds. A new ticket
        /// for the same player replaces the old one. Tokens stay below 2^31: they go to the client
        /// as a Python int, and the game hands them back to Talkback as an unsigned 32-bit value.
        /// </summary>
        public uint IssueTicket(uint groupId, ulong playerId)
        {
            lock (_lock)
            {
                var token = (uint)_random.Next(1, int.MaxValue);

                _tickets[playerId] = new Ticket
                {
                    GroupId = groupId,
                    Token = token,
                    ExpiresTick = Environment.TickCount64 + Math.Max(5, _config.TokenLifetimeSeconds) * 1000L
                };

                return token;
            }
        }

        /// <summary>A player leaving voice - the channel, their squad or the world. Closes their connection if they have one.</summary>
        public void Leave(ulong playerId)
        {
            lock (_lock)
            {
                _tickets.Remove(playerId);

                foreach (var session in _sessions.Values.Where(s => s.LoggedIn && s.PlayerId == playerId).ToList())
                    Drop(session, true, "left the squad or the voice channel");
            }
        }

        /// <summary>A squad that no longer exists: everyone in its group is disconnected.</summary>
        public void GroupDisbanded(uint groupId)
        {
            lock (_lock)
            {
                foreach (var ticket in _tickets.Where(t => t.Value.GroupId == groupId).Select(t => t.Key).ToList())
                    _tickets.Remove(ticket);

                if (!_groups.TryGetValue(groupId, out var members))
                    return;

                foreach (var session in members.Where(m => m != null).ToList())
                    Drop(session, true, "squad disbanded");
            }
        }

        /// <summary>For the console: one line per group.</summary>
        public List<string> Describe()
        {
            lock (_lock)
            {
                var lines = new List<string>
                {
                    _socket == null
                        ? $"Voice chat is {(_config.Enabled ? "on but not listening" : "off")}."
                        : $"Voice chat on UDP {_boundTo.Port}, clients sent {_publicHost}:{_boundTo.Port}. "
                          + $"{_sessions.Count} connection(s), {_groups.Count} group(s), {_tickets.Count} unused token(s)."
                };

                foreach (var (groupId, members) in _groups)
                    lines.Add($"  squad {groupId}: " + string.Join(", ", members.Where(m => m != null)
                        .Select(m => $"#{m.LocalId} account {m.PlayerId}{(m.Speaking ? " (talking)" : "")} {m.EndPoint}")));

                return lines;
            }
        }

        #endregion

        #region Receiving

        private void ReceiveLoop(Socket socket)
        {
            var buffer = new byte[2048];
            EndPoint from = new IPEndPoint(socket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

            while (true)
            {
                int length;

                try
                {
                    length = socket.ReceiveFrom(buffer, ref from);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    lock (_lock)
                        if (_socket != socket)
                            return;

                    // A reset from one client's vanished port is that client's problem, not the listener's.
                    if (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.MessageSize)
                        continue;

                    Logger.WriteLog(LogType.Error, $"Voice server receive failed: {e.Message}");
                    continue;
                }

                try
                {
                    Received((IPEndPoint)from, buffer.AsSpan(0, length));
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Voice server: error handling a datagram from {from}: {e}");
                }
            }
        }

        /// <summary>One datagram from one address. Internal so the tests can feed it directly.</summary>
        internal void Received(IPEndPoint from, ReadOnlySpan<byte> datagram)
        {
            if (!Talkback.TryParse(datagram, out var flags, out var payload))
                return;

            lock (_lock)
            {
                if (_socket == null)
                    return;

                var now = Environment.TickCount64;

                _sessions.TryGetValue(from, out var session);

                if (flags == Talkback.FlagHello)
                {
                    // A new connection, or a hello resent because our offer was lost. From an address
                    // that already had a connection, the client started over: drop the old one.
                    if (session != null && session.State != SessionState.KeyOffered)
                    {
                        Drop(session, false, "reconnected");
                        session = null;
                    }

                    if (session == null)
                    {
                        session = new Session { EndPoint = from, State = SessionState.KeyOffered, CreatedTick = now };
                        _sessions[from] = session;
                    }

                    session.LastHeardTick = now;
                    SendRaw(from, Talkback.FlagKeyOffer, _keyOffer);
                    return;
                }

                if (session == null)
                {
                    // Something from an address we have no connection with - most likely a client
                    // that outlived a restart of this server. Tell it the connection is gone.
                    if (flags != Talkback.FlagClose && flags != Talkback.FlagCloseAck)
                        SendRaw(from, Talkback.FlagClose, ReadOnlySpan<byte>.Empty);
                    else if (flags == Talkback.FlagClose)
                        SendRaw(from, Talkback.FlagCloseAck, ReadOnlySpan<byte>.Empty);

                    return;
                }

                session.LastHeardTick = now;

                switch (flags)
                {
                    case Talkback.FlagClientKey:
                        // The client's key goes nowhere: nothing is encrypted. Resent if our ack was lost.
                        if (session.State != SessionState.Connected)
                            session.State = SessionState.KeyAccepted;

                        SendRaw(from, Talkback.FlagAck, ReadOnlySpan<byte>.Empty);
                        return;

                    case Talkback.FlagPong:
                        if (session.State == SessionState.KeyAccepted)
                            session.State = SessionState.Connected;

                        return;

                    case Talkback.FlagAck:
                        // A ping of its own.
                        SendRaw(from, Talkback.FlagPong, ReadOnlySpan<byte>.Empty);
                        return;

                    case Talkback.FlagClose:
                        SendRaw(from, Talkback.FlagCloseAck, ReadOnlySpan<byte>.Empty);
                        Drop(session, false, "disconnected");
                        return;

                    case Talkback.FlagCloseAck:
                        Drop(session, false, "disconnected");
                        return;

                    case Talkback.FlagData:
                        // Our 06 may have been lost with the client already connected on its side.
                        if (session.State == SessionState.KeyAccepted)
                            session.State = SessionState.Connected;

                        if (session.State == SessionState.Connected && payload.Length > 0)
                            Message(session, payload);

                        return;
                }
            }
        }

        private void Message(Session session, ReadOnlySpan<byte> message)
        {
            switch (message[0])
            {
                case Talkback.Login:
                    Login(session, message);
                    return;

                case Talkback.VoiceFixed:
                case Talkback.VoiceVariable:
                    // The relay is one byte longer (the speaker's localId); one that would not fit a
                    // datagram is dropped. The client's own packets are a few hundred bytes at most.
                    if (session.LoggedIn && message.Length < Talkback.MaxPayload && Talkback.IsWellFormedVoice(message))
                    {
                        // Push-to-talk's StartTalking is one unacknowledged datagram; voice arriving
                        // from someone not marked as talking means it was lost.
                        if (!session.Speaking)
                            BeginSpeaking(session);

                        SendToGroup(session, Talkback.Relay(message, session.LocalId));
                    }

                    return;

                case Talkback.StartTalking:
                    if (session.LoggedIn && !session.Speaking)
                        BeginSpeaking(session);

                    return;

                case Talkback.StopTalking:
                    if (session.LoggedIn)
                        EndSpeaking(session);

                    return;

                case Talkback.Heartbeat:
                    return;

                case Talkback.Logout:
                    if (session.LoggedIn)
                        LeaveGroup(session, "logged out");

                    return;
            }
        }

        #endregion

        #region Groups

        private void Login(Session session, ReadOnlySpan<byte> message)
        {
            if (!Talkback.TryReadLogin(message, out var login))
            {
                SendMessage(session, Talkback.LoginReplyMessage(Talkback.ResultBadMessageType, 0, 0, MaxMembers, MaxMembers, DefaultCodec, 0f, 0f, false));
                return;
            }

            if (session.LoggedIn)
            {
                // A resend of a login whose reply was lost: say it again.
                if (session.PlayerId == login.PlayerId && session.GroupId == login.GroupId)
                    SendLoginReply(session);

                return;
            }

            if (!_tickets.TryGetValue(login.PlayerId, out var ticket) || ticket.Token != login.Token || ticket.GroupId != login.GroupId
                || ticket.ExpiresTick < Environment.TickCount64)
            {
                Log($"Voice: refused a login from {session.EndPoint} as account {login.PlayerId} to squad {login.GroupId}: "
                    + (ticket == null ? "no token issued" : ticket.ExpiresTick < Environment.TickCount64 ? "token expired" : "token or squad does not match"));

                SendMessage(session, Talkback.LoginReplyMessage(Talkback.ResultUnableToVerify, login.GroupId, 0, MaxMembers, MaxMembers, login.Codec, 0f, 0f, false));
                return;
            }

            if (Math.Abs(login.Version - Talkback.ClientVersion) > 0.001f)
                Log($"Voice: account {login.PlayerId} logs in with Talkback version {login.Version}, not {Talkback.ClientVersion}; letting it try.");

            _tickets.Remove(login.PlayerId);

            // The same player on another connection - a client that reconnected before its old
            // connection timed out. The new one takes over.
            foreach (var old in _sessions.Values.Where(s => s != session && s.LoggedIn && s.PlayerId == login.PlayerId).ToList())
                Drop(old, true, "replaced by a new connection");

            if (!_groups.TryGetValue(login.GroupId, out var members))
            {
                members = new Session[MaxMembers];
                _groups[login.GroupId] = members;
            }

            var slot = Array.IndexOf(members, null);

            if (slot < 0)
            {
                Log($"Voice: squad {login.GroupId} has no room for account {login.PlayerId}.");
                SendMessage(session, Talkback.LoginReplyMessage(Talkback.ResultUnableToJoin, login.GroupId, 0, MaxMembers, MaxMembers, login.Codec, 0f, 0f, false));
                return;
            }

            members[slot] = session;

            session.LoggedIn = true;
            session.GroupId = login.GroupId;
            session.PlayerId = login.PlayerId;
            session.LocalId = (byte)slot;
            session.Codec = login.Codec;

            SendLoginReply(session);

            // Who is already here, to the newcomer; and the newcomer, to them.
            foreach (var other in members)
            {
                if (other == null || other == session)
                    continue;

                SendMessage(session, Talkback.PlayerAddedMessage(other.GroupId, other.PlayerId, other.LocalId, other.Speaking));
                SendMessage(other, Talkback.PlayerAddedMessage(session.GroupId, session.PlayerId, session.LocalId, false));
            }

            Log($"Voice: account {login.PlayerId} joined squad {login.GroupId} as #{slot} from {session.EndPoint}.");
        }

        private void SendLoginReply(Session session)
        {
            SendMessage(session, Talkback.LoginReplyMessage(Talkback.ResultSuccess, session.GroupId, session.LocalId, MaxMembers, MaxMembers,
                session.Codec, Math.Max(1f, _config.MaxTalkTime), Math.Max(0f, _config.TalkTimeRegen), true));
        }

        private void BeginSpeaking(Session session)
        {
            session.Speaking = true;
            session.SpeakingSinceTick = Environment.TickCount64;

            // Not to the speaker: their client showed it the moment the key went down.
            SendToGroup(session, Talkback.BeginSpeakingMessage(session.LocalId));
        }

        private void EndSpeaking(Session session)
        {
            if (!session.Speaking)
                return;

            session.Speaking = false;

            var seconds = (Environment.TickCount64 - session.SpeakingSinceTick) / 1000f;

            SendToGroup(session, Talkback.EndSpeakingMessage(session.LocalId, seconds, ++session.EndSeq));
        }

        /// <summary>Takes a session out of its group and tells the rest.</summary>
        private void LeaveGroup(Session session, string why)
        {
            if (!session.LoggedIn)
                return;

            EndSpeaking(session);

            if (_groups.TryGetValue(session.GroupId, out var members))
            {
                if (members[session.LocalId] == session)
                    members[session.LocalId] = null;

                foreach (var other in members)
                    if (other != null)
                        SendMessage(other, Talkback.PlayerRemovedMessage(session.GroupId, session.PlayerId, session.LocalId));

                if (members.All(m => m == null))
                    _groups.Remove(session.GroupId);
            }

            session.LoggedIn = false;

            Log($"Voice: account {session.PlayerId} left squad {session.GroupId} ({why}).");
        }

        /// <summary>Ends a connection. Close tells the client, so it shows voice as disconnected straight away.</summary>
        private void Drop(Session session, bool close, string why)
        {
            LeaveGroup(session, why);

            if (_sessions.TryGetValue(session.EndPoint, out var current) && current == session)
                _sessions.Remove(session.EndPoint);

            if (close)
                SendRaw(session.EndPoint, Talkback.FlagClose, ReadOnlySpan<byte>.Empty);
        }

        private void Housekeeping()
        {
            try
            {
                lock (_lock)
                {
                    if (_socket == null)
                        return;

                    var now = Environment.TickCount64;
                    var timeout = Math.Max(5, _config.TimeoutSeconds) * 1000L;

                    foreach (var session in _sessions.Values.ToList())
                    {
                        if (session.State != SessionState.Connected)
                        {
                            if (now - session.CreatedTick > HandshakeTimeoutMs)
                                Drop(session, false, "never finished connecting");

                            continue;
                        }

                        if (now - session.LastHeardTick > timeout)
                        {
                            Drop(session, true, "timed out");
                            continue;
                        }

                        if (now - session.LastPingTick >= PingIntervalMs)
                        {
                            session.LastPingTick = now;
                            SendRaw(session.EndPoint, Talkback.FlagAck, ReadOnlySpan<byte>.Empty);
                        }
                    }

                    foreach (var player in _tickets.Where(t => t.Value.ExpiresTick < now).Select(t => t.Key).ToList())
                        _tickets.Remove(player);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Voice server housekeeping failed: {e}");
            }
        }

        #endregion

        #region Sending

        private void SendToGroup(Session from, byte[] message)
        {
            if (!_groups.TryGetValue(from.GroupId, out var members))
                return;

            var datagram = Talkback.Datagram(Talkback.FlagData, message);

            foreach (var other in members)
                if (other != null && other != from)
                    Send(other.EndPoint, datagram);
        }

        private void SendMessage(Session session, byte[] message) => SendRaw(session.EndPoint, Talkback.FlagData, message);

        private void SendRaw(IPEndPoint to, byte flags, ReadOnlySpan<byte> payload) => Send(to, Talkback.Datagram(flags, payload));

        private void Send(IPEndPoint to, byte[] datagram)
        {
            try
            {
                _socket?.SendTo(datagram, to);
            }
            catch (Exception e) when (e is SocketException || e is ObjectDisposedException)
            {
                // UDP: a send that fails is a packet lost.
            }
        }

        private void Log(string line)
        {
            if (_config.LogSessions)
                Logger.WriteLog(LogType.Network, line);
        }

        #endregion
    }
}
