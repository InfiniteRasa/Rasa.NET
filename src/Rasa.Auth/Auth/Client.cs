using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Rasa.Auth
{
    using Cryptography;
    using Data;
    using Extensions;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Auth.Client;
    using Packets.Auth.Server;
    using Repositories;
    using Repositories.Auth.Account;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Auth;
    using Timer;

    public class Client
    {
        private readonly IAuthUnitOfWorkFactory _authUnitOfWorkFactory;

        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public Server Server { get; }

        public uint OneTimeKey { get; }
        public uint SessionId1 { get; }
        public uint SessionId2 { get; }
        public AuthAccountEntry AccountEntry { get; private set; }
        public ClientState State { get; private set; }
        public Timer Timer { get; }

        private PacketQueue _packetQueue = new();

        /// <summary>
        /// Close() is reached from the socket completion threads (OnError, OnDrop, and OnReceive's
        /// own catch) and from the main loop (the timeout timer, and Update's catch blocks), so
        /// two threads can be in it at once. The game server's client has had this guard for a
        /// while; this one had only the unsynchronized read of State at the top, which both
        /// threads pass before either writes it - and both then log the disconnect, remove the
        /// timer, shut the socket and queue the client for removal twice.
        /// </summary>
        private readonly object _clientLock = new object();

        /// <summary>Packets read off the socket but not yet handled by the main loop.</summary>
        private int _queuedPackets;
        private const int MaxQueuedPackets = 64;

        public Client(LengthedSocket socket, Server server, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
        {
            _authUnitOfWorkFactory = authUnitOfWorkFactory;

            Socket = socket;
            Server = server;
            State = ClientState.Connected;

            Timer = new Timer();

            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;
            Socket.OnReceive += OnReceive;
            Socket.OnDecrypt += OnDecrypt;

            Socket.ReceiveAsync();

            var rnd = new Random();

            OneTimeKey = rnd.NextUInt();
            SessionId1 = rnd.NextUInt();
            SessionId2 = rnd.NextUInt();

            SendPacket(new ProtocolVersionPacket(OneTimeKey));

            // This is here (after ProtocolVersionPacket), so it won't get encrypted
            Socket.OnEncrypt += OnEncrypt;

            Timer.Add("timeout", Server.Config.AuthConfig.ClientTimeout * 1000, false, () =>
            {
                Logger.WriteLog(LogType.Network, "*** Client timed out! Ip: {0}", Socket.RemoteAddress);

                Close();
            });

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
        }

        public void Update(long delta)
        {
            // The timeout timer's callback closes the connection, and Close() does real work.
            try
            {
                Timer.Update(delta);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error updating timers for {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
                return;
            }

            if (State == ClientState.Disconnected)
                return;

            IBasePacket packet;

            // This is the auth server's main loop thread, and nothing above it catches: an
            // exception out of a handler used to end the process. Now it ends the connection.
            while ((packet = _packetQueue.PopIncoming()) != null)
            {
                // Re-checked each time round, not just before the loop: a handler can close the
                // connection, and the packets queued behind it were handled anyway. A client
                // that pipelines a bad login and a server-list request in one segment got the
                // login refused and closed, then had the second packet rejected as unexpected
                // and closed again - one connection driving any number of teardowns and
                // Security log lines.
                if (State == ClientState.Disconnected)
                    return;

                Interlocked.Decrement(ref _queuedPackets);

                try
                {
                    HandlePacket(packet);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Error handling {packet.GetType().Name} from {Socket.RemoteAddress}, disconnecting client: {e}");
                    Close();
                    return;
                }
            }

            // Writing a packet can throw - serialization, or a socket that has gone since the
            // packet was queued - and the game server has guarded this half for a while. Auth
            // had not: an exception here reached the main loop with nothing above it.
            try
            {
                while ((packet = _packetQueue.PopOutgoing()) != null)
                    SendPacket(packet);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error sending queued packets to {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
            }
        }
        
        public void Close()
        {
            if (State == ClientState.Disconnected)
                return;

            lock (_clientLock)
            {
                // The check above is the cheap one; this is the one that decides. Without it
                // both threads get past the first and run the whole teardown.
                if (State == ClientState.Disconnected)
                    return;

                Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

                Timer.Remove("timeout");

                // Written before anything else in here, so a handler still running on the other
                // thread stops rather than carrying on against a socket that is about to go.
                State = ClientState.Disconnected;

                Socket.Close();

                Server.Disconnect(this);
            }
        }

        public void SendPacket(IBasePacket packet)
        {
            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            if (packet is not IOpcodedPacket<ClientOpcode> authPacket)
                return;

            // Each message belongs to a point in the conversation, and the handlers assume it:
            // everything after Login reads AccountEntry, which Login sets. A ServerListExt or
            // AboutToPlay sent first dereferenced null on the main loop thread. Login is the
            // only thing a fresh connection may say; once it has said it, it may not again.
            if (!IsExpected(authPacket.Opcode))
            {
                Logger.WriteLog(LogType.Security, $"Client {Socket.RemoteAddress} sent {authPacket.Opcode} in state {State}; disconnecting.");
                Close();
                return;
            }

            switch (authPacket.Opcode)
            {
                case ClientOpcode.Login:
                    MsgLogin(authPacket as LoginPacket);
                    break;

                case ClientOpcode.Logout:
                    MsgLogout(authPacket as LogoutPacket);
                    break;

                case ClientOpcode.AboutToPlay:
                    MsgAboutToPlay(authPacket as AboutToPlayPacket);
                    break;

                case ClientOpcode.ServerListExt:
                    MsgServerListExt(authPacket as ServerListExtPacket);
                    break;
            }
        }

        private bool IsExpected(ClientOpcode opcode)
        {
            switch (opcode)
            {
                case ClientOpcode.Login:
                    return State == ClientState.Connected;

                case ClientOpcode.ServerListExt:
                case ClientOpcode.AboutToPlay:
                    return AccountEntry != null && (State == ClientState.LoggedIn || State == ClientState.ServerList);

                default:
                    // Logout and SCCheck carry nothing the state has to be ready for.
                    return true;
            }
        }

        public void RedirectionResult(RedirectResult result, ServerInfo info)
        {
            switch (result)
            {
                case RedirectResult.Fail:
                    SendPacket(new PlayFailPacket(FailReason.UnexpectedError));

                    Close();

                    Logger.WriteLog(LogType.Error, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) couldn't be redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Success:
                    HandleSuccessfulRedirect(info);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private void HandleSuccessfulRedirect(ServerInfo info)
        {
            SendPacket(new HandoffToQueuePacket
            {
                OneTimeKey = OneTimeKey,
                ServerId = info.ServerId,
                AccountId = AccountEntry.Id
            });

            using var unitOfWork = _authUnitOfWorkFactory.Create();
            unitOfWork.AuthAccountRepository.UpdateLastServer(AccountEntry.Id, info.ServerId);
            unitOfWork.Complete();

            Logger.WriteLog(LogType.Network, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) was redirected to the queue of the server: {info.ServerId}!");
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close();
        }

        /// <summary>
        /// The socket has given up on this connection - a full send queue, a stream that stopped
        /// framing, or no buffers left to serve it. Nothing more can pass either way, so let it go.
        /// </summary>
        private void OnDrop(string reason)
        {
            Close();
        }

        private static void OnEncrypt(BufferData data, ref int length)
        {
            AuthCryptManager.Encrypt(data.Buffer, data.BaseOffset + data.Offset, ref length, data.RemainingLength);
        }

        private static bool OnDecrypt(BufferData data)
        {
            return AuthCryptManager.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.RemainingLength);
        }

        /// <summary>
        /// Socket completion thread. Nothing may escape: an opcode this server has no packet for
        /// throws out of CreatePacket, and a malformed body throws out of Read - and the caller
        /// is a socket callback, where an exception used to end the process. It costs this one
        /// connection instead, and says which opcode did it.
        /// </summary>
        private void OnReceive(BufferData data)
        {
            // Nullable, not a default: Login is 0x00, so a default would name it as the culprit
            // when the failure was reading the opcode byte itself.
            ClientOpcode? opcode = null;

            try
            {
                // Reset the timeout after every action
                Timer.ResetTimer("timeout");

                using var br = data.GetReader();

                opcode = (ClientOpcode)br.ReadByte();

                var packet = CreatePacket(opcode.Value);

                packet.Read(br);

                // Bounded, for the same reason the game client bounds its undrained input: this
                // runs at line speed on a socket thread while the main loop drains one packet
                // per handler per tick, and MsgLogin's handler is a synchronous database call.
                // A client that pipelines logins would otherwise queue them faster than they can
                // ever be answered. Nothing legitimate gets near this - the auth conversation is
                // a handful of packets - so the limit doubles as the flood check.
                if (Interlocked.Increment(ref _queuedPackets) > MaxQueuedPackets)
                {
                    Logger.WriteLog(LogType.Security, $"Client {Socket.RemoteAddress} has {_queuedPackets} unanswered packets queued (limit {MaxQueuedPackets}), disconnecting.");
                    Close();
                    return;
                }

                _packetQueue.EnqueueIncoming(packet);
            }
            catch (Exception e)
            {
                var what = opcode.HasValue ? $"a {opcode.Value} packet" : "a packet whose opcode could not be read";
                Logger.WriteLog(LogType.Error, $"Error reading {what} from {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
            }
        }

        private IBasePacket CreatePacket(ClientOpcode opcode)
        {
            return opcode switch
            {
                ClientOpcode.AboutToPlay   => new AboutToPlayPacket(),
                ClientOpcode.Login         => new LoginPacket(),
                ClientOpcode.Logout        => new LogoutPacket(),
                ClientOpcode.ServerListExt => new ServerListExtPacket(),
                ClientOpcode.SCCheck       => new SCCheckPacket(),

                _ => throw new InvalidDataException($"Unsupported auth opcode: {opcode}."),
            };
        }

        #region Handlers
        private void MsgLogin(LoginPacket packet)
        {
            using var unitOfWork = _authUnitOfWorkFactory.Create();

            try
            {
                AccountEntry = unitOfWork.AuthAccountRepository.GetByUserName(packet.UserName, packet.Password);
            }
            catch (EntityNotFoundException)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close();
                Logger.WriteLog(LogType.Security, $"User ({packet.UserName}) tried to log in with an invalid username!");
                return;
            }
            catch (PasswordCheckFailedException e)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close();
                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }
            catch (AccountLockedException e)
            {
                SendPacket(new BlockedAccountPacket());
                Close();
                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }

            unitOfWork.AuthAccountRepository.UpdateLoginData(AccountEntry.Id, Socket.RemoteAddress);
            unitOfWork.Complete();

            State = ClientState.LoggedIn;

            SendPacket(new LoginOkPacket
            {
                SessionId1 = SessionId1,
                SessionId2 = SessionId2
            });

            Logger.WriteLog(LogType.Network, "*** Client logged in from {0}", Socket.RemoteAddress);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void MsgLogout(LogoutPacket packet)
        {
            Close();
        }

        private void MsgServerListExt(ServerListExtPacket packet)
        {
            State = ClientState.ServerList;

            SendPacket(new SendServerListExtPacket(Server.GetServerListSnapshot(), AccountEntry.LastServerId));
        }
#pragma warning restore IDE0060 // Remove unused parameter

        private void MsgAboutToPlay(AboutToPlayPacket packet)
        {
            if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
            {
                Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an AboutToPlay packet with invalid session data!");
                return;
            }

            Server.RequestRedirection(this, packet.ServerId);
        }
        #endregion
    }
}
