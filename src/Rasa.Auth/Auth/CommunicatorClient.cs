using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

        /// <summary>When the connection was accepted; one that has not logged in within Server.CommunicatorLoginTimeout is closed.</summary>
        public DateTime ConnectedTime { get; } = DateTime.UtcNow;
        public IPAddress PublicAddress { get; set; }

        private readonly PacketRouter<CommunicatorClient, CommOpcode> _router = new PacketRouter<CommunicatorClient, CommOpcode>();

        public bool Connected => Socket.Connected;

        public CommunicatorClient(LengthedSocket socket, Server server)
        {
            Server = server;
            Socket = socket;

            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;

            Socket.ReceiveAsync();
        }

        /// <summary>
        /// Socket completion thread, carrying messages from a game server. A malformed body or a
        /// throwing handler must not end the auth process, and it must not silently cost the link
        /// to that game server either - which is what happened when the exception was left to the
        /// socket layer's catch-all, whose log line names the socket operation rather than the
        /// message that could not be handled.
        /// </summary>
        private void OnReceive(BufferData data)
        {
            CommOpcode? opcode = null;

            try
            {
                opcode = (CommOpcode) data.Buffer[data.BaseOffset + data.Offset++];

                var packetType = _router.GetPacketType(opcode.Value);
                if (packetType == null)
                    return;

                var packet = Activator.CreateInstance(packetType) as IOpcodedPacket<CommOpcode>;
                if (packet == null)
                    return;

                packet.Read(data.GetReader());

                _router.RoutePacket(this, packet);
            }
            catch (Exception e)
            {
                var what = opcode.HasValue ? $"a {opcode.Value} message" : "a message whose opcode could not be read";
                Logger.WriteLog(LogType.Error, $"Error handling {what} from game server {ServerId}: {e}");
            }
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Disconnect();
        }

        /// <summary>
        /// The socket layer gave up on this link without a socket error - no buffer to re-arm the
        /// receive with, a full send queue, a frame that would not decode. The socket stays open
        /// and Connected after that, and with only OnError handled the game server stayed
        /// registered and listed as up while nothing it sent was read and nothing reached it.
        /// It is let go the same way a socket error lets it go; the game server reconnects.
        /// </summary>
        private void OnDrop(string reason)
        {
            Disconnect();
        }

        private int _disconnected;

        /// <summary>Whether this link has been let go (Disconnect has run).</summary>
        internal bool IsDisconnected => Volatile.Read(ref _disconnected) != 0;

        /// <summary>
        /// Once per link: a drop closes the socket, and the close completes the receive that
        /// was still armed with an error, which comes back through OnError.
        /// </summary>
        internal void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0)
                return;

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
            // ServerId and PublicAddress are assigned by AuthenticateGameServer, under the
            // GameServers lock and in the same step that claims the slot, so a socket that dies
            // right after is still given its slot back by DisconnectCommunicator. They used to be
            // taken from the packet here, before the password was checked: a rejected login then
            // carried the id of the server it had failed to be, and DisconnectCommunicator removed
            // GameServers[id] - the real server's entry - for it.
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

            RequestServerInfo();
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.ServerInfoResponse)]
        private void MsgGameInfoResponse(ServerInfoResponsePacket packet)
        {
            // Only from a connection that has logged in as a game server.
            if (ServerId == 0)
                return;

            Server.UpdateServerInfo(this, packet);
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.RedirectResponse)]
        private void MsgRedirectResponse(RedirectResponsePacket packet)
        {
            // Only from a connection that has logged in as a game server.
            if (ServerId == 0)
                return;

            Server.RedirectResponse(this, packet);
        }
    }
}
