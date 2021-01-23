using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Database.Tables.Character;
    using Handlers;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Protocol;
    using Structures;

    public class Client
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public ClientCryptData Data { get; }
        public Server Server { get; }
        public GameAccountEntry AccountEntry { get; private set; }
        public ClientState State { get; set; }
        public uint[] SendSequence { get; } = new uint[256];
        public uint[] ReceiveSequence { get; } = new uint[256];
        public uint LoadingMap { get; set; }
        public MapChannelClient MapClient { get; set; }
        public List<UserOptions> UserOptions = new List<UserOptions>();
        private readonly object _clientLock = new object();
        private readonly ClientPacketHandler _handler;
        private readonly PacketQueue _packetQueue = new PacketQueue();
        public MovementData MovementData { get; set; }


        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public static Type GetPacketType(GameOpcode opcode)
        {
            return PacketRouter.GetPacketType(opcode);
        }

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
            Socket.OnDisconnect += OnDisconnect;

            Socket.ReceiveAsync();

            for (var i = 0; i < 256; ++i)
                SendSequence[i] = 1;

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
        }

        public void Update(long delta)
        {
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

            lock (_clientLock)
            {
                if (State == ClientState.Disconnected)
                    return;

                Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

                State = ClientState.Disconnected;

                Socket.Close();

                Server.Disconnect(this);
            }
        }

        public void CallMethod(ulong entityId, PythonPacket packet)
        {
            SendMessage(new CallMethodMessage(entityId, packet));
        }

        public void CallMethod(SysEntity entityId, PythonPacket packet)
        {
            SendMessage(new CallMethodMessage((ulong)entityId, packet));
        }

        internal void MoveObject(ulong entityId, MovementData movementData)
        {
            SendMessage(new MoveObjectMessage(entityId, movementData), false, 1);
        }

        // Cell Domain
        public void CellCallMethod(Client client, ulong entityId, PythonPacket packet)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in client.MapClient.Player.Actor.Cells)
                clientList.AddRange(client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
                tempClient.CallMethod(entityId, packet);
        }

        // Cell Domain ignore self
        public void CellIgnoreSelfCallMethod(Client client, PythonPacket packet)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in client.MapClient.Player.Actor.Cells)
                clientList.AddRange(client.MapClient.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
            {
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(client.MapClient.Player.Actor.EntityId, packet);
            }
        }
        
        // Cell send movement
        internal void CellMoveObject(MapChannelClient mapClient, MoveObjectMessage moveObjectMessage, bool ignoreSelf)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in mapClient.Player.Actor.Cells)
                clientList.AddRange(mapClient.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
            {
                if (tempClient.MapClient == mapClient && ignoreSelf)
                        continue;

                tempClient.SendMessage(moveObjectMessage, false, 1);
            }
        }

        public void SendMessage(IClientMessage message, bool compress = false, byte channel = 0, bool delay = true)
        {
            var protocolPacket = new ProtocolPacket(message, message.Type, compress, channel);

            if (!delay)
                SendPacket(protocolPacket);
            else
                _packetQueue.EnqueueOutgoing(protocolPacket);
        }

        public void SendPacket(IBasePacket packet)
        {
            var pPacket = packet as ProtocolPacket;
            if (pPacket == null)
            {
                Debugger.Break();
                return;
            }

            if (pPacket.Channel != 0)
                pPacket.SequenceNumber = SendSequence[pPacket.Channel]++;

            Socket.Send(pPacket);
        }

        public void HandlePacket(IBasePacket packet)
        {
            var pPacket = packet as ProtocolPacket;
            if (pPacket == null)
                return;

            switch (pPacket.Type)
            {
                case ClientMessageOpcode.Login:
                    var loginMsg = pPacket.Message as LoginMessage;
                    if (loginMsg == null)
                    {
                        Close(true);
                        return;
                    }

                    if (loginMsg.Version.Length != 8 || loginMsg.Version != "1.16.5.0")
                    {
                        Logger.WriteLog(LogType.Error, $"Client version mismatch: Server: 1.16.5.0 | Client: {loginMsg.Version}");

                        SendMessage(new LoginResponseMessage
                        {
                            ErrorCode = LoginErrorCodes.VersionMismatch,
                            Subtype = LoginResponseMessageSubtype.Failed
                        }, delay: false);
                        return;
                    }

                    var loginEntry = Server.AuthenticateClient(this, loginMsg.AccountId, loginMsg.OneTimeKey);
                    if (loginEntry == null)
                    {
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in with invalid session data! User Id: {1} | OneTimeKey: {2}", Socket.RemoteAddress, loginMsg.AccountId, loginMsg.OneTimeKey);

                        SendMessage(new LoginResponseMessage
                        {
                            ErrorCode = LoginErrorCodes.AuthenticationFailed,
                            Subtype = LoginResponseMessageSubtype.Failed
                        }, delay: false);

                        return;
                    }

                    GameAccountTable.CreateAccountDataIfNeeded(loginEntry.Id, loginEntry.Name, loginEntry.Email);

                    if (Server.IsBanned(loginMsg.AccountId))
                    {
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in while the account is banned! User Id: {1}", Socket.RemoteAddress, loginMsg.AccountId);

                        SendMessage(new LoginResponseMessage
                        {
                            ErrorCode = LoginErrorCodes.AccountLocked,
                            Subtype = LoginResponseMessageSubtype.Failed
                        }, delay: false);

                        return;
                    }

                    if (Server.IsAlreadyLoggedIn(loginMsg.AccountId))
                    {
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in while the account is being played on! User Id: {1}", Socket.RemoteAddress, loginMsg.AccountId);

                        SendMessage(new LoginResponseMessage
                        {
                            ErrorCode = LoginErrorCodes.AlreadyLoggedIn,
                            Subtype = LoginResponseMessageSubtype.Failed
                        }, delay: false);

                        return;
                    }

                    AccountEntry = GameAccountTable.GetAccount(loginEntry.Id);
                    AccountEntry.LastIP = Socket.RemoteAddress.ToString();
                    AccountEntry.LastLogin = DateTime.Now;

                    GameAccountTable.UpdateAccount(AccountEntry);

                    SendMessage(new LoginResponseMessage
                    {
                        AccountId = loginMsg.AccountId,
                        Subtype = LoginResponseMessageSubtype.Success
                    });

                    State = ClientState.LoggedIn;

                    CharacterManager.Instance.StartCharacterSelection(this);
                    return;

                case ClientMessageOpcode.Move:
                    var movePacket = pPacket.Message as MoveMessage;
                    if (movePacket == null)
                    {
                        Close(true);
                        return;
                    }

                    MapClient.Player.Actor.Position = new Vector3(movePacket.MovementData.PosX, movePacket.MovementData.PosY, movePacket.MovementData.PosZ);
                    MapClient.Player.Actor.Orientation = movePacket.MovementData.ViewX;
                    MovementData = movePacket.MovementData;

                    // send your movement to other players in visibility range
                    var moveObjectMessage = new MoveObjectMessage(MapClient.Player.Actor.EntityId, movePacket.MovementData);
                    CellMoveObject(MapClient, moveObjectMessage, true);

                    break;

                case ClientMessageOpcode.CallServerMethod:
                    var csmPacket = pPacket.Message as CallServerMethodMessage;
                    if (csmPacket == null)
                    {
                        Close(true);
                        return;
                    }

                    if (!csmPacket.ReadPacket())
                    {
                        Close(true);
                        return;
                    }

                    PacketRouter.RoutePacket(_handler, csmPacket.Packet);
                    break;

                case ClientMessageOpcode.Ping:
                    var pingMessage = pPacket.Message as PingMessage;
                    if (pingMessage == null)
                    {
                        Close(true);
                        return;
                    }

                    SendMessage(pingMessage, delay: false);
                    break;
            }
        }

        public bool IsAuthenticated()
        {
            return State != ClientState.Connected && State != ClientState.Disconnected;
        }

        #region Socketing
        private void OnDisconnect()
        {
            foreach (var client in MapClient.MapChannel.ClientList)
                if (client.MapClient == MapClient)
                    MapChannelManager.Instance.RemovePlayer(client, false);
        }

        private void OnEncrypt(BufferData data, ref int length)
        {
            var paddingCount = (byte)(8 - length % 8);

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

                var startOffset = data.Offset;

                if (!DecodePacket(data, out ushort subsize))
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

                data.Offset += (int)br.BaseStream.Position;

                if (rawPacket.Type == ClientMessageOpcode.None)
                {
                    if (length != 4)
                        Debugger.Break(); // If it's not send timeout check, let's investigate...

                    return true;
                }

                _packetQueue.EnqueueIncoming(rawPacket);
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

        public Manifestation Player
        {
            get
            {
                return MapClient?.Player;
            }
        }
    }
}
