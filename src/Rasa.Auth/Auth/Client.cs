using System;
using System.Net.Sockets;

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

        private readonly PacketQueue _packetQueue = new PacketQueue();

        private static PacketRouter<Client, ClientOpcode> PacketRouter { get; } = new PacketRouter<Client, ClientOpcode>();

        public Client(LengthedSocket socket, Server server, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
        {
            _authUnitOfWorkFactory = authUnitOfWorkFactory;

            Socket = socket;
            Server = server;
            State = ClientState.Connected;

            Timer = new Timer();

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;
            Socket.OnDecrypt += OnDecrypt;

            Socket.ReceiveAsync();

            var rnd = new Random();

            OneTimeKey = rnd.NextUInt();
            SessionId1 = rnd.NextUInt();
            SessionId2 = rnd.NextUInt();

            Socket.Send(new ProtocolVersionPacket(OneTimeKey));

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
            Timer.Update(delta);

            if (State == ClientState.Disconnected)
                return;

            IBasePacket packet;

            while ((packet = _packetQueue.PopIncoming()) != null)
            {
                if (packet is IOpcodedPacket<ClientOpcode> authPacket)
                    PacketRouter.RoutePacket(this, authPacket);
            }

            while ((packet = _packetQueue.PopOutgoing()) != null)
                Socket.Send(packet);
        }
        
        public void Close()
        {
            if (State == ClientState.Disconnected)
                return;

            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            Timer.Remove("timeout");

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void RedirectionResult(RedirectResult result, ServerInfo info)
        {
            switch (result)
            {
                case RedirectResult.Fail:
                    Socket.Send(new PlayFailPacket(FailReason.UnexpectedError));

                    Close();

                    Logger.WriteLog(LogType.Error, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) couldn't be redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Success:
                    Socket.Send(new HandoffToQueuePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerId = info.ServerId,
                        AccountId = AccountEntry.Id
                    });

                    using (var unitOfWork = _authUnitOfWorkFactory.Create())
                    {
                        unitOfWork.AuthAccountRepository.UpdateLastServer(AccountEntry.Id, info.ServerId);
                        unitOfWork.Complete();
                    }

                    Logger.WriteLog(LogType.Network, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) was redirected to the queue of the server: {info.ServerId}!");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        public void UpdateServerList()
        {
            if (State == ClientState.ServerList)
                Socket.Send(new SendServerListExtPacket(Server.ServerList, AccountEntry.LastServerId));
        }

        private void OnError(SocketAsyncEventArgs args)
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

        private void OnReceive(BufferData data)
        {
            // Reset the timeout after every action
            Timer.ResetTimer("timeout");

            using var br = data.GetReader();

            var packetType = PacketRouter.GetPacketType((ClientOpcode)br.ReadByte());
            if (packetType == null)
                return;

            if (Activator.CreateInstance(packetType) is IBasePacket packet)
            {
                packet.Read(br);

                _packetQueue.EnqueueIncoming(packet);
            }
        }

        #region Handlers
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter

        [PacketHandler(ClientOpcode.Login)]
        private void MsgLogin(LoginPacket packet)
        {
            using var unitOfWork = _authUnitOfWorkFactory.Create();
            try
            {
                AccountEntry = unitOfWork.AuthAccountRepository.GetByUserName(packet.UserName, packet.Password);
            }
            catch (EntityNotFoundException)
            {
                Socket.Send(new LoginFailPacket(FailReason.UserNameOrPassword));

                Close();

                Logger.WriteLog(LogType.Security, $"User ({packet.UserName}) tried to log in with an invalid username!");
                return;
            }
            catch (PasswordCheckFailedException e)
            {
                Socket.Send(new LoginFailPacket(FailReason.UserNameOrPassword));

                Close();

                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }
            catch (AccountLockedException e)
            {
                Socket.Send(new BlockedAccountPacket());

                Close();

                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }

            unitOfWork.AuthAccountRepository.UpdateLoginData(AccountEntry.Id, Socket.RemoteAddress);
            unitOfWork.Complete();

            State = ClientState.LoggedIn;

            Socket.Send(new LoginOkPacket
            {
                SessionId1 = SessionId1,
                SessionId2 = SessionId2
            });

            Logger.WriteLog(LogType.Network, "*** Client logged in from {0}", Socket.RemoteAddress);
        }

        [PacketHandler(ClientOpcode.Logout)]
        private void MsgLogout(LogoutPacket packet)
        {
            Close();
        }

        [PacketHandler(ClientOpcode.ServerListExt)]
        private void MsgServerListExt(ServerListExtPacket packet)
        {
            State = ClientState.ServerList;

            UpdateServerList();
        }

        [PacketHandler(ClientOpcode.AboutToPlay)]
        private void MsgAboutToPlay(AboutToPlayPacket packet)
        {
            if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
            {
                Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an AboutToPlay packet with invalid session data!");
                return;
            }

            Server.RequestRedirection(this, packet.ServerId);
        }

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
        #endregion
    }
}
