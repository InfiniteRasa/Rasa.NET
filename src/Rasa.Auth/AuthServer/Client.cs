using System;
using System.Net.Sockets;

namespace Rasa.AuthServer
{
    using AuthData;
    using AuthPackets.Client;
    using AuthPackets.Server;
    using AuthStructures;
    using Cryptography;
    using Database.Tables;
    using Extensions;
    using Memory;
    using Networking;
    using Packets;
    using Structures;

    public class Client : PacketRouter<Client, ClientOpcode>, INetworkClient
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

        static Client()
        {
            // Create target-less delegates of the packet handler functions for opcode routing
            // Note: Because it's target-less and the function is instanced (not static) we need to give 2 paramteres: The Client (the target) and the BinaryReader (the parameter)
            PacketRouter.RegisterHandler(ClientOpcode.Login, "MsgLogin", typeof(LoginPacket));
            PacketRouter.RegisterHandler(ClientOpcode.Logout, "MsgLogout", typeof(LogoutPacket));
            PacketRouter.RegisterHandler(ClientOpcode.AboutToPlay, "MsgAboutToPlay", typeof(AboutToPlayPacket));
            PacketRouter.RegisterHandler(ClientOpcode.SCCheck, "MsgSCCheck", typeof(SCCheckPacket));
            PacketRouter.RegisterHandler(ClientOpcode.ServerListExt, "MsgServerListExt", typeof(ServerListExtPacket));
        }

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

            PacketRouter.RoutePacket(this, authPacket.Opcode, authPacket);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            AuthCryptManager.Instance.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.Length - data.Offset);

            var opcode = (ClientOpcode) data.Buffer[data.BaseOffset + data.Offset++];

            var packetType = PacketRouter.GetPacketType(opcode);
            if (packetType != null)
            {
                var packet = Activator.CreateInstance(packetType) as IBasePacket;
                if (packet == null)
                    return;

                packet.Read(data.GetReader());

                Server.PacketQueue.EnqueueIncoming(this, packet);
            }
            else
                Logger.WriteLog(LogType.Error, $"Unhandled opcode: {opcode}");
        }

        #region Handlers
        // ReSharper disable once UnusedMember.Local
        // Used by reflection
        private void MsgLogin(IOpcodedPacket<ClientOpcode> packet)
        {
            var loginPacket = packet as LoginPacket;
            if (loginPacket == null)
            {
                Close();
                return;
            }

            Entry = AccountTable.GetAccount(loginPacket.UserName);
            if (Entry == null)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close(false);
                Logger.WriteLog(LogType.Debug, $"User ({loginPacket.UserName}) tried to log in with an invalid username!");
                return;
            }

            if (!Entry.CheckPassword(loginPacket.Password))
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

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        // Used by reflection
        private void MsgLogout(IOpcodedPacket<ClientOpcode> packet)
        {
            Close(false);
        }

        // ReSharper disable once UnusedMember.Local
        // Used by reflection
        private void MsgSCCheck(IOpcodedPacket<ClientOpcode> packet)
        {
            var scCheckPacket = packet as SCCheckPacket;
            if (scCheckPacket == null)
            {
                Close();
                return;
            }

            // I'm not sure we have to handle this, seems unused by the client
        }

        // ReSharper disable once UnusedMember.Local
        // Used by reflection
        private void MsgServerListExt(IOpcodedPacket<ClientOpcode> packet)
        {
            var serverListPacket = packet as ServerListExtPacket;
            if (serverListPacket == null)
            {
                Close();
                return;
            }

            State = ClientState.ServerList;

            SendPacket(new SendServerListExtPacket(Server.ServerList, Entry.LastServerId));
        }

        // ReSharper disable once UnusedMember.Local
        // Used by reflection
        private void MsgAboutToPlay(IOpcodedPacket<ClientOpcode> packet)
        {
            var atpPacket = packet as AboutToPlayPacket;
            if (atpPacket == null)
            {
                Close();
                return;
            }

            if (SessionId1 != atpPacket.SessionId1 || SessionId2 != atpPacket.SessionId2)
            {
                Logger.WriteLog(LogType.Debug, $"Account ({Entry.Username}, {Entry.Id}) sent an AboutToPlay packet with invalid session data!");
                return;
            }

            ServerInfo info;

            switch (Server.RedirectToGlobal(this, atpPacket.ServerId, out info))
            {
                case RedirectResult.Fail:
                    SendPacket(new PlayFailPacket(FailReason.UnexpectedError));

                    Close(false);

                    Logger.WriteLog(LogType.Error, $"Account ({Entry.Username}, {Entry.Id}) couldn't be redirected to server: {atpPacket.ServerId}!");
                    break;

                case RedirectResult.Success:
                    SendPacket(new HandoffToGamePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerIp = BitConverter.ToUInt32(info.Ip.GetAddressBytes(), 0),
                        ServerPort = info.GamePort,
                        UserId = Entry.Id
                    });

                    Logger.WriteLog(LogType.Debug, $"Account  ({Entry.Username}, {Entry.Id}) was redirected to server: {atpPacket.ServerId}!");
                    break;

                case RedirectResult.Queue:
                    SendPacket(new HandoffToQueuePacket
                    {
                        OneTimeKey = OneTimeKey,
                        ServerId = info.ServerId,
                        UserId = Entry.Id
                    });

                    Logger.WriteLog(LogType.Debug, $"Account  ({Entry.Username}, {Entry.Id}) was redirected to the queue of the server: {atpPacket.ServerId}!");
                    break;
            }
        }
        #endregion
    }
}
