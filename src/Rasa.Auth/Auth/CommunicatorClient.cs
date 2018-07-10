using System;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Auth
{
    using Data;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Communicator;

    public class CommunicatorClient
    {
        public LengthedSocket Socket { get; }
        public Server Server { get; }
        public byte ServerId { get; set; }
        public int QueuePort { get; set; }
        public int GamePort { get; set; }
        public byte AgeLimit { get; set; }
        public byte PKFlag { get; set; }
        public ushort CurrentPlayers { get; set; }
        public ushort MaxPlayers { get; set; }
        public DateTime LastRequestTime { get; set; }
        public IPAddress PublicAddress { get; set; }

        private readonly PacketRouter<CommunicatorClient, CommOpcode> _router = new PacketRouter<CommunicatorClient, CommOpcode>();

        public bool Connected => Socket.Connected;

        public CommunicatorClient(LengthedSocket socket, Server server)
        {
            Server = server;
            Socket = socket;

            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;

            Socket.ReceiveAsync();
        }

        private void OnReceive(BufferData data)
        {
            var opcode = (CommOpcode) data.Buffer[data.BaseOffset + data.Offset++];

            var packetType = _router.GetPacketType(opcode);
            if (packetType == null)
                return;

            var packet = Activator.CreateInstance(packetType) as IOpcodedPacket<CommOpcode>;
            if (packet == null)
                return;

            packet.Read(data.GetReader());

            _router.RoutePacket(this, packet);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Socket.Close();

            Server.DisconnectCommunicator(this);
        }

        public void RequestServerInfo()
        {
            LastRequestTime = DateTime.Now;

            Socket.Send(new ServerInfoRequestPacket());
        }

        public void RequestRedirection(Client client)
        {
            Socket.Send(new RedirectRequestPacket
            {
                AccountId = client.AccountEntry.Id,
                Email = client.AccountEntry.Email,
                Username = client.AccountEntry.Username,
                OneTimeKey = client.OneTimeKey
            });
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.LoginRequest)]
        private void MsgLoginRequest(LoginRequestPacket packet)
        {
            if (!Server.AuthenticateGameServer(packet, this))
            {
                Socket.Send(new LoginResponsePacket
                {
                    Response = CommLoginReason.Failure
                });
                return;
            }

            Socket.Send(new LoginResponsePacket
            {
                Response = CommLoginReason.Success
            });

            ServerId = packet.ServerId;
            PublicAddress = packet.PublicAddress;

            RequestServerInfo();
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.ServerInfoResponse)]
        private void MsgGameInfoResponse(ServerInfoResponsePacket packet)
        {
            Server.UpdateServerInfo(this, packet);
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.RedirectResponse)]
        private void MsgRedirectResponse(RedirectResponsePacket packet)
        {
            Server.RedirectResponse(this, packet);
        }
    }
}
