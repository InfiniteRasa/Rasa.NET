using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

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
        public ClientCryptData Data { get; }
        public Server Server { get; }
        public uint OneTimeKey { get; private set; }
        public AccountEntry Entry { get; private set; }
        public ClientState State { get; private set; }

        private static PacketRouter<Client, int> PacketRouter { get; } = new PacketRouter<Client, int>(); // TODO: type

        public Client(LengthedSocket socket, ClientCryptData data, Server server)
        {
            Socket = socket;
            Data = data;
            Server = server;

            State = ClientState.Connected;

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;
            Socket.OnEncrypt += OnEncrypt;
            Socket.OnDecrypt += OnDecrypt;

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

        private void OnEncrypt(BufferData data, ref int length)
        {
            GameCryptManager.Instance.Encrypt(data.Buffer, data.RealOffset, ref length, data.RemainingLength, Data);
        }

        private bool OnDecrypt(BufferData data)
        {
            return GameCryptManager.Instance.Decrypt(data.Buffer, data.RealOffset, data.Length, Data);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            // TODO: find in the client and rethink subsize calculation
            var align = data[data.Offset] % 9;
            var size = data.Length - align;

            data.Offset += align;

            do
            {
                var subsize = data[data.Offset] | (data[data.Offset + 1] << 8);
                if (subsize == 43 && size == 12)
                {
                    
                }
                else
                {
                    if (subsize > 4000)
                    {
                        var zeroFound = 0;
                        var i = 0;

                        for (; i < subsize; ++i)
                        {
                            if (data[data.Offset + i] == 0 && data[data.Offset + i + 1] == 0)
                                ++zeroFound;

                            if (zeroFound != 2)
                                continue;

                            if (i - 2 < 0)
                                Console.WriteLine("FindSubsize::HOLYSHIT !!!");

                            subsize = i - 2;
                        }

                        subsize = i;
                    }

                    DecodePacket(data, subsize);
                }

                data.Offset += subsize;
                size -= subsize;
            }
            while (size > 0);


            /*var opcode = (int) data.Buffer[data.BaseOffset + data.Offset++];

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
                Logger.WriteLog(LogType.Error, $"Unhandled opcode: {opcode}");*/
        }

        private bool DecodePacket(BufferData data, int length)
        {
            // TODO: find in the client and rethink packet data reading
            if (length > 0xFFFF)
                throw new Exception("Message is too big!");

            if (length < 4)
                return false;

            if (length > 3000)
                throw new Exception("Big packet! Check it...");

            data.Offset += 2; // Skip subsize
            var majorOpcode = BitConverter.ToUInt16(data.Buffer, data.RealOffset); data.Offset += 2;
            if (majorOpcode != 0)
                return true;

            if (data[data.Offset++] != 2)
                Debugger.Break();

            var opcode = data[data.Offset++];

            if (data[data.Offset++] != 0)
                Debugger.Break();

            if (data[data.Offset++] != 3)
                Debugger.Break();

            if (data[data.Offset++] != 3)
                Debugger.Break();

            if (opcode == 2)
            {
                if (data[data.Offset++] != 0x29)
                    Debugger.Break();

                if (data[data.Offset++] != 3)
                    Debugger.Break();

                if (data[data.Offset++] != 1)
                    Debugger.Break();

                if (data[data.Offset++] != 7)
                    Debugger.Break();

                var userId = BitConverter.ToUInt32(data.Buffer, data.RealOffset); data.Offset += 4;

                if (data[data.Offset++] != 7)
                    Debugger.Break();

                var oneTimeKey = BitConverter.ToUInt32(data.Buffer, data.RealOffset); data.Offset += 4;

                if (data[data.Offset++] != 0xD)
                    Debugger.Break();

                if (data[data.Offset++] != 0xCB)
                    Debugger.Break();

                var versionLen = data[data.Offset++];
                var wrongVersion = versionLen != 8;

                if (wrongVersion || Encoding.UTF8.GetString(data.Buffer, data.RealOffset, versionLen) != "1.16.5.0")
                {
                    Logger.WriteLog(LogType.Error, "Client version mismatch: Server: 1.16.5.0 | Client: {0}", Encoding.UTF8.GetString(data.Buffer, data.RealOffset, versionLen));
                }

                data.Offset += versionLen;

                if (data[data.Offset] != 0x2A)
                    Debugger.Break();

                // todo: auth user

                return true;
            }

            if (opcode != 0x0C)
                return true;

            if (data[data.Offset] == 0)
                return true;

            if (data[data.Offset++] != 0x29)
                Debugger.Break();

            if (data[data.Offset++] != 3)
                Debugger.Break();

            if (data[data.Offset] == 0 || data[data.Offset++] > 0x10)
                Debugger.Break();

            if (data[data.Offset++] != 7)
                Debugger.Break();

            var methodId = BitConverter.ToUInt32(data.Buffer, data.RealOffset); data.Offset += 4;

            if (data[data.Offset++] != 1)
                Debugger.Break();

            if (data[data.Offset++] != 0xCB)
                Debugger.Break();

            var dataLen = 0U;
            var lenMask = (uint)data[data.Offset++];

            switch (lenMask >> 6)
            {
                case 0:
                    dataLen = lenMask & 0x3F;
                    break;

                case 1:
                    dataLen = lenMask & 0x3F;
                    dataLen |= (uint)data[data.Offset++] << 6;
                    break;

                default:
                    Debugger.Break();
                    break;
            }

            if (dataLen > 0 && data.Offset + dataLen < data.Length)
            {
                // todo: process python call
                //ProcessPythonMethodCall(methodId, data, off, dataLen);
            }
            else
            {
                Console.WriteLine($"Invalid data found in Python method call! Off: {data.Offset} | Len: {dataLen} | Array len: {data.Length}");
            }

            return true;
        }
    }
}
