using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Handlers;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Protocol;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public class Client
    {
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        private readonly ICharacterManager _characterManager;

        public const int LengthSize = 2;

        public Server Server { get; private set; }
        public LengthedSocket Socket { get; private set; }
        public ClientCryptData Data { get; private set; }
        public GameAccountEntry AccountEntry { get; private set; }
        public ClientState State { get; set; }
        public Manifestation Player { get; set; }
        public uint[] SendSequence { get; } = new uint[256];
        public uint[] ReceiveSequence { get; } = new uint[256];

        private readonly object _clientLock = new object();
        private readonly ClientPacketHandler _handler;
        private readonly PacketQueue _packetQueue = new PacketQueue();

        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public static Type GetPacketType(GameOpcode opcode)
        {
            return PacketRouter.GetPacketType(opcode);
        }

        public Client(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            ICharacterManager characterManager,
            ClientPacketHandler handler)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _characterManager = characterManager;

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

            try
            {
                HandleProtocolPacket(pPacket);
            }
            catch (InvalidClientMessageException)
            {
                Close();
            }
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
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in with invalid session data! User Id: {1} | OneTimeKey: {2}", Socket.RemoteAddress, loginMsg.AccountId,
                            loginMsg.OneTimeKey);

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
                    return;

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

                data.Offset += (int) br.BaseStream.Position;

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
