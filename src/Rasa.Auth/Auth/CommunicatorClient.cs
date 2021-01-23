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
            using var br = data.GetReader();

            var packetType = _router.GetPacketType((CommOpcode)br.ReadByte());
            if (packetType == null)
                return;

            if (Activator.CreateInstance(packetType) is IOpcodedPacket<CommOpcode> packet)
            {
                packet.Read(br);

                _router.RoutePacket(this, packet);
            }
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

        #region Handlers
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter

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

        [PacketHandler(CommOpcode.ServerInfoResponse)]
        private void MsgGameInfoResponse(ServerInfoResponsePacket packet)
        {
            Server.UpdateServerInfo();
        }

        [PacketHandler(CommOpcode.RedirectResponse)]
        private void MsgRedirectResponse(RedirectResponsePacket packet)
        {
            Server.RedirectResponse(this, packet);
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
        #endregion
    }
}
