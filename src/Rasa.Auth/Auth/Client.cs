using System;
using System.Net.Sockets;

namespace Rasa.Auth
{
    using Cryptography;
    using Data;
    using Database.Tables;
    using Extensions;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Client;
    using Packets.Server;
    using Structures;

    public class Client : INetworkClient
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public Server Server { get; }

        public uint OneTimeKey { get; }
        public uint SessionId1 { get; }
        public uint SessionId2 { get; }
        public AccountEntry Entry { get; private set; }
        public ClientState State { get; private set; }

        private static PacketRouter<Client, ClientOpcode> PacketRouter { get; } = new PacketRouter<Client, ClientOpcode>();

        public Client(LengthedSocket socket, Server server)
        {
            Socket = socket;
            Server = server;
            State = ClientState.Connected;

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;

            var rnd = new Random();

            OneTimeKey = rnd.NextUInt();
            SessionId1 = rnd.NextUInt();
            SessionId2 = rnd.NextUInt();

            // This packet must not be encrypted, so call Socket.Send instead of SendPacket
            Socket.Send(new ProtocolVersionPacket(OneTimeKey), null);

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);

            // Moved up before Sendpacket for debug purposes (it will get the first buffer part, easier to see values) can be moved back down after socketing is done
            Socket.ReceiveAsync();
        }
        
        public void Close(bool sendPacket = true)
        {
            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void SendPacket(IBasePacket packet)
        {
            Socket.Send(packet, AuthCryptManager.Instance);
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

                    Logger.WriteLog(LogType.Error, $"Account ({Entry.Username}, {Entry.Id}) couldn't be redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Success:
                    SendPacket(new HandoffToGamePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerIp = BitConverter.ToUInt32(info.Ip.GetAddressBytes(), 0),
                        ServerPort = info.GamePort,
                        UserId = Entry.Id
                    });

                    Logger.WriteLog(LogType.Debug, $"Account  ({Entry.Username}, {Entry.Id}) was redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Queue:
                    SendPacket(new HandoffToQueuePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerId = info.ServerId,
                        UserId = Entry.Id
                    });

                    Logger.WriteLog(LogType.Debug, $"Account  ({Entry.Username}, {Entry.Id}) was redirected to the queue of the server: {info.ServerId}!");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            AuthCryptManager.Instance.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.Length - data.Offset);

            var packetType = PacketRouter.GetPacketType((ClientOpcode) data.Buffer[data.BaseOffset + data.Offset++]);
            if (packetType == null)
                return;

            var packet = Activator.CreateInstance(packetType) as IBasePacket;
            if (packet == null)
                return;

            packet.Read(data.GetReader());

            Server.PacketQueue.EnqueueIncoming(this, packet);
        }

        #region Handlers
        // ReSharper disable once UnusedMember.Local - Used by reflection
        [PacketHandler(ClientOpcode.Login)]
        private void MsgLogin(LoginPacket packet)
        {

            Entry = AccountTable.GetAccount(packet.UserName);
            if (Entry == null)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close(false);
                Logger.WriteLog(LogType.Debug, $"User ({packet.UserName}) tried to log in with an invalid username!");
                return;
            }

            if (!Entry.CheckPassword(packet.Password))
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close(false);
                Logger.WriteLog(LogType.Debug, $"User ({Entry.Username}, {Entry.Id}) tried to log in with an invalid password!");
                return;
            }

            if (Entry.Locked)
            {
                SendPacket(new BlockedAccountPacket());
                Close(false);
                Logger.WriteLog(LogType.Debug, $"User ({Entry.Username}, {Entry.Id}) tried to log in, but he/she is locked.");
                return;
            }

            AccountTable.UpdateLoginData(Entry.Id, Socket.RemoteAddress);

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

            SendPacket(new SendServerListExtPacket(Server.ServerList, Entry.LastServerId));
        }

        // ReSharper disable once UnusedMember.Local - Used by reflection
        [PacketHandler(ClientOpcode.AboutToPlay)]
        private void MsgAboutToPlay(AboutToPlayPacket packet)
        {
            if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
            {
                Logger.WriteLog(LogType.Debug, $"Account ({Entry.Username}, {Entry.Id}) has sent an AboutToPlay packet with invalid session data!");
                return;
            }

            Server.RequestRedirection(this, packet.ServerId);
        }
        #endregion
    }
}
