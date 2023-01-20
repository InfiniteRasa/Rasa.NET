using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Handlers;
    using Managers;
    using Memory;
    using Models;
    using Networking;
    using Packets;
    using Packets.Protocol;
    using Rasa.Packets.Communicator.Both;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using System.Net.Mail;
    using System.Numerics;

    public class Client
    {
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        private readonly ICharacterManager _characterManager;

        public const int LengthSize = 2;

        public Server Server { get; private set; }
        public LengthedSocket Socket { get; private set; }
        public ClientCryptData Data { get; private set; }
        public GameAccountEntry AccountEntry { get; private set; }
        public uint LoadingMap { get; set; }
        public ClientState State { get; set; }
        public Manifestation Player { get; set; }
        public Movement Movement { get; set; }
        public uint[] SendSequence { get; } = new uint[256];
        public uint[] ReceiveSequence { get; } = new uint[256];
        public List<UserOptions> UserOptions = new();

        private readonly object _clientLock = new();
        private readonly ClientPacketHandler _handler;
        private readonly PacketQueue _packetQueue = new();
        private readonly NonContiguousMemoryStream _incomingDataQueue = new();


        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public static Type GetPacketType(GameOpcode opcode)
        {
            return PacketRouter.GetPacketType(opcode);
        }

        public Client(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            ClientPacketHandler handler)
            ClientPacketHandler handler)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _characterManager = characterManager;

            _handler = handler;
            _handler.RegisterClient(this);
        }

            _handler = handler;
            _handler.RegisterClient(this);
        }

        public void RegisterAtServer(Server server, LengthedSocket socket, ClientCryptData cryptData)
        {
            Socket = socket;
            Data = cryptData;
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

        public void Update(long delta)
        {
            foreach (var protocolPacket in DecodeIncomingPackets())
            {
                try
                {
                    HandleProtocolPacket(protocolPacket);
                }
                catch (InvalidClientMessageException)
                {
                    Close();
                }
            }

            IBasePacket packet;

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

                SaveCharacter();
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

        internal void MoveObject(ulong entityId, Movement movement)
        {
            SendMessage(new MoveObjectMessage(entityId, movement), false, 1);
        }

        // Cell Domain
        public void CellCallMethod(Client client, ulong entityId, PythonPacket packet)
        {
           var clientList = new List<Client>();

            foreach (var cellSeed in client.Player.Cells)
                clientList.AddRange(client.Player.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
                tempClient.CallMethod(entityId, packet);
        }

        // Cell Domain ignore self
        public void CellIgnoreSelfCallMethod(Client client, PythonPacket packet)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in client.Player.Cells)
                clientList.AddRange(client.Player.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
            {
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(client.Player.EntityId, packet);
            }
        }

        // Cell send movement
        internal void CellMoveObject(Client client, MoveObjectMessage moveObjectMessage, bool ignoreSelf)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in client.Player.Cells)
                clientList.AddRange(client.Player.MapChannel.MapCellInfo.Cells[cellSeed].ClientList);

            foreach (var tempClient in clientList)
            {
                if (tempClient == client && ignoreSelf)
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

        private void HandleProtocolPacket(ProtocolPacket protocolPacket)
        {
            switch (protocolPacket.Type)
            {
                case ClientMessageOpcode.Login:
                    var loginMsg = GetMessageAs<LoginMessage>(protocolPacket);

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

                    using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                    {
                        unitOfWork.GameAccounts.CreateOrUpdate(loginEntry.Id, loginEntry.Name, loginEntry.Email);

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

                    using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                    {
                        unitOfWork.GameAccounts.CreateOrUpdate(loginEntry.Id, loginEntry.Name, loginEntry.Email);
                        // for now set all account to GM status
                        unitOfWork.GameAccounts.UpdateAccountLevel(loginEntry.Id, 1);

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

                        LoadGameAccountEntry(unitOfWork, loginEntry.Id);

                        unitOfWork.GameAccounts.UpdateLoginData(loginEntry.Id, Socket.RemoteAddress);
                        unitOfWork.Complete();
                    }

                    SendMessage(new LoginResponseMessage
                    {
                        AccountId = loginMsg.AccountId,
                        Subtype = LoginResponseMessageSubtype.Success
                    });

                    State = ClientState.LoggedIn;

                    _characterManager.StartCharacterSelection(this);
                    break;

                case ClientMessageOpcode.Move:
                    if (Player == null)
                    {
                        return;
                    }

                    var moveMessage = GetMessageAs<MoveMessage>(protocolPacket);
                    if (moveMessage.Movement == null)
                    {
                        return;
                    }

                    Player.Position = moveMessage.Movement.Position;
                    Player.Rotation = moveMessage.Movement.ViewDirection.X;
                    Movement = moveMessage.Movement;

                    // send your movement to other players in visibility range
                    var moveObjectMessage = new MoveObjectMessage(Player.EntityId, moveMessage.Movement);
                    CellMoveObject(this, moveObjectMessage, true);

                    break;

                case ClientMessageOpcode.CallServerMethod:
                    var csmPacket = GetMessageAs<CallServerMethodMessage>(protocolPacket);

                    if (!csmPacket.ReadPacket())
                    {
                        Close(true);
                        return;
                    }

                    PacketRouter.RoutePacket(_handler, csmPacket.Packet);
                    break;

                case ClientMessageOpcode.Ping:
                    var pingMessage = GetMessageAs<PingMessage>(protocolPacket);

                    SendMessage(pingMessage, delay: false);
                    break;
            }
        }

        private T GetMessageAs<T>(ProtocolPacket protocolPacket)
            where T : class, IClientMessage
        {
            if (protocolPacket.Message is T message)
            {
                return message;
            }
            throw new InvalidClientMessageException();
        }

        public bool IsAuthenticated()
        {
            return State != ClientState.Connected && State != ClientState.Disconnected;
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
            _incomingDataQueue.CopyFromArray(data.Buffer, data.BaseOffset + data.Offset, data.RemainingLength);
        }

        private IEnumerable<ProtocolPacket> DecodeIncomingPackets()
        {
            ProtocolPacket packet;

            while ((packet = DecodeNextPacket()) != null)
                yield return packet;

            yield break;
        }

        private ProtocolPacket DecodeNextPacket()
        {
            // If there is not enough data to read the packet size at all, then stop processing
            if (_incomingDataQueue.Length < 2)
                return null;

            using var br = new BinaryReader(_incomingDataQueue, Encoding.UTF8, true);

            // Peek the packet size to determine if the whole packet has arrived
            var startPosition = _incomingDataQueue.Position;

            // Read the size of the next packet
            var packetSize = br.ReadUInt16();

            // Rewind the stream to the starting position
            _incomingDataQueue.Position = startPosition;

            // If the packet is fragmented and not all the fragments has arrived yet, then stop processing
            if (packetSize > _incomingDataQueue.Length)
                return null;

            // Construct and the packet
            var rawPacket = new ProtocolPacket();

            rawPacket.Read(br);

            // Check for overreading or underreading the packet
            if (_incomingDataQueue.Position != startPosition + packetSize)
                throw new Exception($"ProtocolPacket over or under read! Start position: {startPosition} | Packet size: {packetSize} | End position: {_incomingDataQueue.Position}!");

            // Advance the stream by removing the already processed data
            _incomingDataQueue.RemoveBytes(packetSize);

            // Throw away any packet that came out of order, if it came on a channel
            if (rawPacket.Channel != 0)
            {
                // If an out of sequence packet arrived, then throw it away
                if (rawPacket.SequenceNumber < ReceiveSequence[rawPacket.Channel])
                {
                    Debugger.Break(); // todo: test and remove later

                    return null;
                }

                // AddOrUpdate the receive sequence for the channel
                ReceiveSequence[rawPacket.Channel] = rawPacket.SequenceNumber;
            }

            // Some internal send timeout check, skip the packet
            if (rawPacket.Type == ClientMessageOpcode.None)
            {
                if (rawPacket.Size != 4)
                    Debugger.Break(); // If it's not send timeout check, let's investigate...

                return null;
            }
            
            return rawPacket;
        }
        #endregion

        public void SaveCharacter()
        {
            var player = Player;
            if (player == null)
            {
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Characters.SaveCharacter(player);
            unitOfWork.Complete();
        }

        public void ReloadGameAccountEntry()
        {
            if (AccountEntry == null)
            {
                throw new InvalidOperationException("Client must be initialized by handling a login packet first.");
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            LoadGameAccountEntry(unitOfWork, AccountEntry.Id);
        }

        private void LoadGameAccountEntry(ICharUnitOfWork unitOfWork, uint id)
        {
            AccountEntry = unitOfWork.GameAccounts.Get(id);
        }
    }
}
