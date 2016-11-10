using System;
using System.Net.Sockets;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Database.Tables;
    using Memory;
    using Networking;
    using Packets;
    using Structures;

    public class Client : INetworkClient
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public Server Server { get; }
        public uint OneTimeKey { get; private set; }
        public AccountEntry Entry { get; private set; }
        public ClientState State { get; private set; }

        private static PacketRouter<Client, int> PacketRouter { get; } = new PacketRouter<Client, int>(); // TODO: type

        public Client(LengthedSocket socket, Server server)
        {
            Socket = socket;
            Server = server;

            State = ClientState.Connected;

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;

            // This packet must not be encrypted, so call Socket.Send instead of SendPacket
            //Socket.Send(new ProtocolVersionPacket(OneTimeKey), null); // TODO: some other packet

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);

            // Moved up before Sendpacket for debug purposes (it will get the first buffer part, easier to see values) can be moved back down after socketing is done
            Socket.ReceiveAsync();
        }

        public void AuthenticateClient(uint oneTimeKey, uint accountId)
        {
            OneTimeKey = oneTimeKey;
            Entry = AccountTable.GetAccount(accountId);
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
            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            var authPacket = packet as IOpcodedPacket<int>;
            if (authPacket == null)
                return;

            PacketRouter.RoutePacket(this, authPacket);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            //GameCryptManager.Instance.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.Length - data.Offset); // todo

            var opcode = (int) data.Buffer[data.BaseOffset + data.Offset++];

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
    }
}
