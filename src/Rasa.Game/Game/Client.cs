using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Protocol;
    using Structures;

    public class Client : INetworkClient
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public ClientCryptData Data { get; }
        public Server Server { get; }
        public LoginAccountEntry Entry { get; private set; }
        public ClientState State { get; set; }
        public uint[] SendSequence { get; } = new uint[256];
        public uint[] ReceiveSequence { get; } = new uint[256];

        private readonly ClientPacketHandler _handler;

        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public Client(LengthedSocket socket, ClientCryptData data, Server server)
        {
            _handler = new ClientPacketHandler(this);

            Socket = socket;
            Data = data;
            Server = server;

            State = ClientState.Connected;

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;
            Socket.OnEncrypt += OnEncrypt;
            Socket.OnDecrypt += OnDecrypt;

            Socket.ReceiveAsync();

            for (var i = 0; i < 256; ++i)
                SendSequence[i] = 1;

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
        }

        public void Close(bool sendPacket = true)
        {
            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void CallMethod(uint entityId, PythonPacket packet)
        {
            SendMessage(new CallMethodMessage(entityId, packet));
        }

        public void SendMessage(CallMethodMessage message)
        {
            SendPacket(new ProtocolPacket(message, ClientMessageOpcode.CallMethod, false, 0));
        }

        public void SendMessage(LoginResponseMessage message)
        {
            SendPacket(new ProtocolPacket(message, ClientMessageOpcode.LoginResponse, false, 0));
        }

        public void SendMessage(MoveObjectMessage message)
        {
            SendPacket(new ProtocolPacket(message, ClientMessageOpcode.MoveObject, false, 1));
        }

        public void SendMessage(NegotiateMoveChannelMessage message)
        {
            SendPacket(new ProtocolPacket(message, ClientMessageOpcode.NegotiateMoveChannel, false, 0));
        }

        public void SendMessage(PingMessage message)
        {
            SendPacket(new ProtocolPacket(message, ClientMessageOpcode.Ping, false, 0));
        }

        public void SendPacket(IBasePacket packet)
        {
            var pPacket = packet as ProtocolPacket;
            if (pPacket == null)
            {
                Debugger.Break(); // todo: handle outgoing queue packet sending from server (like in auth) (todo: maybe a delegate instead of an interface?)
                return;
            }

            if (pPacket.Channel != 0)
                pPacket.SequenceNumber = SendSequence[pPacket.Channel]++;

            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            var authPacket = packet as PythonPacket;
            if (authPacket == null)
                return;

            PacketRouter.RoutePacket(_handler, authPacket);
        }

        #region Socketing
        private void OnEncrypt(BufferData data, ref int length)
        {
            var paddingCount = (byte) (8 - length % 8);

            var tempArray = BufferManager.RequestBuffer();

            tempArray[0] = paddingCount;

            BufferData.Copy(data, data.Offset, tempArray, paddingCount, length);

            length += paddingCount;

            GameCryptManager.Encrypt(tempArray.Buffer, tempArray.BaseOffset, ref length, length, Data);

            BufferData.Copy(tempArray, 0, data, data.Offset, length);

            BufferManager.FreeBuffer(tempArray);
        }

        private bool OnDecrypt(BufferData data)
        {
            var result = GameCryptManager.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.RemainingLength, Data);
            if (!result)
                return false;

            var blowfishPadding = data[data.Offset] & 0xF;
            if (blowfishPadding > 8)
                throw new Exception("More than 8 bytes of blowfish padding was added to the packet?");

            data.Offset += blowfishPadding;

            return true;
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            do
            {
                if (data.RemainingLength == 0)
                    break;

                ushort subsize;
                var startOffset = data.Offset;

                if (!DecodePacket(data, out subsize))
                    return;

                if (data.Offset == startOffset + subsize)
                    continue;

                if (data.Offset < startOffset + subsize)
                    throw new Exception("ProtocolPacket underread!");

                if (data.Offset > startOffset + subsize)
                    throw new Exception("ProtocolPacket overread!");
            }
            while (true);
        }

        private bool DecodePacket(BufferData data, out ushort length)
        {
            using (var br = data.GetReader(data.Offset, data.RemainingLength))
            {
                var rawPacket = new ProtocolPacket();

                rawPacket.Read(br);

                if (rawPacket.Channel != 0)
                {
                    if (rawPacket.SequenceNumber < ReceiveSequence[rawPacket.Channel])
                    {
                        Debugger.Break();

                        length = rawPacket.Size; // throw away the packet
                        return true;
                    }

                    ReceiveSequence[rawPacket.Channel] = rawPacket.SequenceNumber;
                }

                length = rawPacket.Size;

                data.Offset += (int) br.BaseStream.Position;

                switch (rawPacket.Type)
                {
                    case ClientMessageOpcode.None: // Send timeout check
                        if (length != 4)
                            Debugger.Break(); // If it's not send timeout check, let's investigate...

                        return true;

                    case ClientMessageOpcode.Login:
                        var loginMsg = rawPacket.Message as LoginMessage;
                        if (loginMsg == null)
                        {
                            Close(false);
                            return false;
                        }


                        if (loginMsg.Version.Length != 8 || loginMsg.Version != "1.16.5.0")
                        {
                            Logger.WriteLog(LogType.Error, $"Client version mismatch: Server: 1.16.5.0 | Client: {loginMsg.Version}");

                            SendMessage(new LoginResponseMessage()
                            {
                                ErrorCode = LoginErrorCodes.VersionMismatch,
                                Subtype = LoginResponseMessageSubtype.Failed
                            });

                            Close(false);
                            return false;
                        }

                        Entry = Server.AuthenticateClient(this, loginMsg.AccountId, loginMsg.OneTimeKey); // TODO: implement ban system and check if the account is banned
                        if (Entry == null)
                        {
                            Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in with invalid session data! User Id: {1} | OneTimeKey: {2}", Socket.RemoteAddress, loginMsg.AccountId, loginMsg.OneTimeKey);

                            SendMessage(new LoginResponseMessage()
                            {
                                ErrorCode = LoginErrorCodes.AuthenticationFailed,
                                Subtype = LoginResponseMessageSubtype.Failed
                            });

                            Close(false);
                            return false;
                        }

                        SendMessage(new LoginResponseMessage()
                        {
                            AccountId = loginMsg.AccountId,
                            Subtype = LoginResponseMessageSubtype.Success
                        });

                        State = ClientState.LoggedIn;

                        CharacterManager.Instance.StartCharacterSelection(this);
                        return true;

                    case ClientMessageOpcode.Move:
                        break;

                    case ClientMessageOpcode.CallServerMethod:
                        break;

                    case ClientMessageOpcode.Ping:
                        break;
                }
            }
            /*var packet = new PythonCallPacket(length);
            using (var br = data.GetReader())
            {
                packet.Read(br);

                if (packet.Return.HasValue)
                    return packet.Return.Value;

                if (packet.Type == 2)
                {
                    State = ClientState.LoggedIn;
                    Entry = Server.AuthenticateClient(this, packet.AccountId, packet.OneTimeKey);
                    if (Entry == null)
                    {
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in with invalid session data! User Id: {1} | OneTimeKey: {2}", Socket.RemoteAddress, packet.AccountId, packet.OneTimeKey);
                        Close(false);
                        return false;
                    }

                    CharacterManager.Instance.StartCharacterSelection(this);
                    return true;
                }

                if (packet.DataSize > 0 && br.BaseStream.Position + packet.DataSize < br.BaseStream.Length)
                {
                    if (br.ReadByte() != 0x4F) // 'O' format
                        throw new Exception("Unsupported serialization format!");

                    var packetType = PacketRouter.GetPacketType(packet.Opcode);
                    if (packetType != null)
                    {
                        var pythonPacket = Activator.CreateInstance(packetType) as IBasePacket;
                        if (pythonPacket == null)
                            return false;

                        pythonPacket.Read(br);

                        Server.PacketQueue.EnqueueIncoming(this, pythonPacket);
                    }
                    else
                        Logger.WriteLog(LogType.Error, $"Unhandled game opcode: {packet.Opcode}");
                }
                else
                    Logger.WriteLog(LogType.Error, $"Invalid data found in Python method call! Off: {br.BaseStream.Position} | Len: {packet.DataSize} | Array len: {br.BaseStream.Length}");
            }*/

            return true;
        }
        #endregion
    }
}
