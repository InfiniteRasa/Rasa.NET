using System;
using System.Net.Sockets;

namespace Rasa.Auth
{
    using Cryptography;
    using Data;
    using Database.Tables.Auth;
    using Extensions;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Auth.Client;
    using Packets.Auth.Server;
    using Structures;
    using Timer;

    public class Client
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public Server Server { get; }

        public uint OneTimeKey { get; }
        public uint SessionId1 { get; }
        public uint SessionId2 { get; }
        public AuthAccountEntry AccountEntry { get; private set; }
        public ClientState State { get; private set; }
        public Timer Timer { get; }

        private PacketQueue _packetQueue = new PacketQueue();

        private static PacketRouter<Client, ClientOpcode> PacketRouter { get; } = new PacketRouter<Client, ClientOpcode>();

        public Client(LengthedSocket socket, Server server)
        {
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

            SendPacket(new ProtocolVersionPacket(OneTimeKey));

            // This is here (after ProtocolVersionPacket), so it won't get encrypted
            Socket.OnEncrypt += OnEncrypt;

            Timer.Add("timeout", Server.Config.AuthConfig.ClientTimeout * 1000, false, () =>
            {
                Logger.WriteLog(LogType.Network, "*** Client timed out! Ip: {0}", Socket.RemoteAddress);

                Close(true);
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
                HandlePacket(packet);

            while ((packet = _packetQueue.PopOutgoing()) != null)
                SendPacket(packet);
        }
        
        public void Close(bool sendPacket = true)
        {
            if (State == ClientState.Disconnected)
                return;

            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            Timer.Remove("timeout");

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void SendPacket(IBasePacket packet)
        {
            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            var authPacket = packet as IOpcodedPacket<ClientOpcode>;
            if (authPacket == null)
                return;

            PacketRouter.RoutePacket(this, authPacket);
        }

        public void RedirectionResult(RedirectResult result, ServerInfo info)
        {
            switch (result)
            {
                case RedirectResult.Fail:
                    SendPacket(new PlayFailPacket(FailReason.UnexpectedError));

                    Close(false);

                    Logger.WriteLog(LogType.Error, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) couldn't be redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Success:
                    SendPacket(new HandoffToQueuePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerId = info.ServerId,
                        AccountId = AccountEntry.Id
                    });

                    AccountTable.UpdateLastServer(AccountEntry.Id, info.ServerId);

                    Logger.WriteLog(LogType.Network, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) was redirected to the queue of the server: {info.ServerId}!");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
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

            var packetType = PacketRouter.GetPacketType((ClientOpcode) data.Buffer[data.BaseOffset + data.Offset++]);
            if (packetType == null)
                return;

            var packet = Activator.CreateInstance(packetType) as IBasePacket;
            if (packet == null)
                return;

            packet.Read(data.GetReader());

            _packetQueue.EnqueueIncoming(packet);
        }

        #region Handlers
        // ReSharper disable once UnusedMember.Local - Used by reflection
        [PacketHandler(ClientOpcode.Login)]
        private void MsgLogin(LoginPacket packet)
        {

            AccountEntry = AccountTable.GetAccount(packet.UserName);
            if (AccountEntry == null)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close(false);
                Logger.WriteLog(LogType.Security, $"User ({packet.UserName}) tried to log in with an invalid username!");
                return;
            }

            if (!AccountEntry.CheckPassword(packet.Password))
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close(false);
                Logger.WriteLog(LogType.Security, $"User ({AccountEntry.Username}, {AccountEntry.Id}) tried to log in with an invalid password!");
                return;
            }

            if (AccountEntry.Locked)
            {
                SendPacket(new BlockedAccountPacket());
                Close(false);
                Logger.WriteLog(LogType.Security, $"User ({AccountEntry.Username}, {AccountEntry.Id}) tried to log in, but he/she is locked.");
                return;
            }

            AccountTable.UpdateLoginData(AccountEntry.Id, Socket.RemoteAddress);

            State = ClientState.LoggedIn;

            SendPacket(new LoginOkPacket
            {
                SessionId1 = SessionId1,
                SessionId2 = SessionId2
            });

            Logger.WriteLog(LogType.Network, "*** Client logged in from {0}", Socket.RemoteAddress);
        }

        // ReSharper disable once UnusedMember.Local - Used by reflection
        // ReSharper disable once UnusedParameter.Local
        [PacketHandler(ClientOpcode.Logout)]
        private void MsgLogout(LogoutPacket packet)
        {
            Close(false);
        }

        // ReSharper disable once UnusedMember.Local - Used by reflection
        // ReSharper disable once UnusedParameter.Local
        /*[PacketHandler(ClientOpcode.SCCheck)]
        private void MsgSCCheck(SCCheckPacket packet)
        {
            // I'm not sure we have to handle this, seems unused by the client
        }*/

        // ReSharper disable once UnusedMember.Local - Used by reflection
        // ReSharper disable once UnusedParameter.Local
        [PacketHandler(ClientOpcode.ServerListExt)]
        private void MsgServerListExt(ServerListExtPacket packet)
        {
            State = ClientState.ServerList;

            SendPacket(new SendServerListExtPacket(Server.ServerList, AccountEntry.LastServerId));
        }

        // ReSharper disable once UnusedMember.Local - Used by reflection
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
        #endregion
    }
}
