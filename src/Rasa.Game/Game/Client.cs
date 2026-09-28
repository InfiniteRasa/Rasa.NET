using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Handlers;
    using Managers;
    using Missions.Integration;
    using Missions.Protocol;
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

        public const int LengthSize = 2;

        public Server Server { get; private set; }
        public LengthedSocket Socket { get; private set; }
        public ClientCryptData Data { get; private set; }
        public GameAccountEntry AccountEntry { get; private set; }
        public uint LoadingMap { get; set; }

        /// <summary>
        /// A Wonkavate has gone to this client and the MapLoaded that answers it has not come
        /// back. The client sends MapLoaded once per load, when its loading screen ends
        /// (wonkavator.py HandleLoadingScreenEnd), so each Wonkavate allows exactly one. Set by
        /// everything that sends one - PassClientToMapInstance, ChangeMap, a departing dropship -
        /// and cleared by MapChannelManager.MapLoaded.
        /// </summary>
        internal bool AwaitingMapLoaded { get; set; }

        private ClientState _state;
        public ClientState State
        {
            get => _state;
            set
            {
                if (value != ClientState.Ingame)
                    InvalidateMissionSession();
                _state = value;
            }
        }
        private Manifestation _player = new();
        public Manifestation Player
        {
            get => _player;
            set
            {
                if (!ReferenceEquals(_player, value))
                    InvalidateMissionSession();
                _player = value;
            }
        }
        public Movement Movement { get; set; }
        public uint[] SendSequence { get; } = new uint[256];
        public uint[] ReceiveSequence { get; } = new uint[256];
        public List<UserOptions> UserOptions = new();
        internal MissionAreaService MissionAreaService { get; set; } = MissionAreaService.Instance;
        internal CharacterFlagProjection FlagProjection { get; } = new();

        private readonly object _clientLock = new();
        internal object SyncRoot => _clientLock;
        private PlayerTransfer _pendingTransfer;
        internal PlayerTransfer PendingTransfer
        {
            get => _pendingTransfer;
            set
            {
                if (value != null)
                    InvalidateMissionSession();
                _pendingTransfer = value;
            }
        }
        internal MissionConversationSession MissionConversation { get; set; }
        internal Guid MissionSessionId { get; private set; } = Guid.NewGuid();
        internal void InvalidateMissionSession()
        {
            MissionConversation = null;
            MissionSessionId = Guid.NewGuid();
        }

        private readonly ClientPacketHandler _handler;
        private readonly PacketQueue _packetQueue = new();
        private readonly bool[] _receivedSequence = new bool[256];

        // Inbound byte stream. Owned exclusively by the MainLoop thread: it is only ever
        // touched from Update()/TryDecodeNextPacket() and (after disconnect) Close().
        private readonly NonContiguousMemoryStream _incomingDataQueue = new();

        // Hand-off from socket completion threads to the MainLoop. OnReceive() runs on an
        // IOCP thread and must not mutate _incomingDataQueue (its backing List<> is not
        // thread-safe; the MainLoop enumerates and RemoveRange()s it while decoding). It
        // rents a pooled array, copies the chunk in, and enqueues it here; Update() drains
        // the queue into the stream on the MainLoop. Receives are serialized per socket,
        // so there is exactly one producer per client and byte order is preserved.
        private readonly ConcurrentQueue<(byte[] Buffer, int Length)> _pendingChunks = new();
        private readonly object _pendingChunksLock = new();

        // Bytes enqueued but not yet drained. Bounded so a client that floods faster than
        // the MainLoop consumes cannot grow memory without limit. Legitimate traffic is a
        // few KB/s and a single frame is capped at 8 KB by LengthedSocket, so this only
        // trips on a misbehaving client or a MainLoop stalled for many seconds.
        private int _pendingBytes;
        private const int MaxPendingBytes = 512 * 1024;


        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public static Type GetPacketType(GameOpcode opcode)
        {
            return PacketRouter.GetPacketType(opcode);
        }

        public Client(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            ClientPacketHandler handler)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;

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
            Socket.OnDrop += OnDrop;
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
            // Nothing in this method may throw: Update() is driven by the single MainLoop
            // thread that services every client and every manager. An escaping exception
            // takes the whole world down, not just this connection.
            try
            {
                DrainPendingChunks();

                foreach (var protocolPacket in DecodeIncomingPackets())
                {
                    try
                    {
                        HandleProtocolPacket(protocolPacket);
                    }
                    catch (InvalidClientMessageException)
                    {
                        Close();
                        return;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Error handling {protocolPacket.Type} from {Socket.RemoteAddress}, disconnecting client: {e}");
                        Close();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                // DecodeIncomingPackets() can throw while advancing the iterator (desynced
                // or malformed stream), which the inner try above would never see.
                Logger.WriteLog(LogType.Error, $"Error decoding packet stream from {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
                return;
            }

            try
            {
                IBasePacket packet;

                while ((packet = _packetQueue.PopOutgoing()) != null)
                    SendPacket(packet);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error sending queued packets to {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
            }
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

                Server?.Disconnect(this);

                // A dropped connection (Alt+F4, crash, network loss) never runs the /logout
                // flow, and that flow was the only thing that set RemoveFromMap - so the
                // character stayed in its map cell as a frozen copy, visible to everyone
                // including the same player on their next login. Flag it for the
                // MapChannelWorker instead of calling RemovePlayer here: Close() is also
                // reached from socket completion threads, and RemovePlayer walks the cell
                // and entity tables the MainLoop owns. Disconected goes first so the
                // worker skips handing a dead socket back to character selection, and so
                // the visibility and trigger passes stop treating the player as present.
                RestoreTransferOrigin();
                if (Player != null && Player.MapChannel != null)
                {
                    Player.Disconected = true;
                    Player.RemoveFromMap = true;
                }

                DiscardPendingChunks();

                try
                {
                    SaveCharacterOnDisconnect();
                }
                catch (Exception e)
                {
                    // Close() is reached from socket completion threads (OnError) as well as
                    // from the MainLoop. A throw here used to terminate the process on every
                    // disconnect that happened before a character was loaded.
                    Logger.WriteLog(LogType.Error, $"Failed to save character on disconnect: {e}");
                }
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
                if (client.Player.MapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    clientList.AddRange(cell.ClientList);

            foreach (var tempClient in clientList)
                tempClient.CallMethod(entityId, packet);
        }

        // Cell Domain ignore self
        public void CellIgnoreSelfCallMethod(Client client, PythonPacket packet)
        {
            var clientList = new List<Client>();

            foreach (var cellSeed in client.Player.Cells)
                if (client.Player.MapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    clientList.AddRange(cell.ClientList);

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
            foreach (var tempClient in CellManager.Instance.GetClientsInCells(client.Player.MapChannel,
                         client.Player.Cells, ignoreSelf ? client : null))
                tempClient.SendMessage(moveObjectMessage, false, 1);
        }

        public void SendMessage(IClientMessage message, bool compress = false, byte channel = 0, bool delay = true)
        {
            var protocolPacket = new ProtocolPacket(message, message.Type, compress, channel);

            if (!delay)
                SendPacket(protocolPacket);
            else
                _packetQueue.EnqueueOutgoing(protocolPacket);
        }

        internal IBasePacket DequeueOutgoingPacket()
        {
            return _packetQueue.PopOutgoing();
        }

        public void SendPacket(IBasePacket packet)
        {
            var pPacket = packet as ProtocolPacket;
            if (pPacket == null)
            {
                Logger.WriteLog(LogType.Error, $"SendPacket() called with a non-ProtocolPacket ({packet?.GetType().Name ?? "null"}), dropping it.");
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
                        // A new account is created at level 0, an ordinary player. Logging in used
                        // to set every account to level 1, which made the GM check in front of the
                        // dot commands true for everyone who could reach it. Levels are handed out
                        // from the Game console now: gm <familyName> <level>.
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

                    CharacterManager.Instance.StartCharacterSelection(this);
                    break;

                case ClientMessageOpcode.Move:
                    var moveMessage = GetMessageAs<MoveMessage>(protocolPacket);
                    HandleMovement(moveMessage.Movement);
                    break;

                case ClientMessageOpcode.CallServerMethod:
                    var csmPacket = GetMessageAs<CallServerMethodMessage>(protocolPacket);

                    if (!csmPacket.ReadPacket())
                    {
                        Close(true);
                        return;
                    }

                    // Nothing that acts on the world runs for a connection that is not in it.
                    if (!IsExpected(csmPacket.MethodId))
                        return;

                    // MethodId, not Packet.Opcode: an opcode with no handler leaves Packet null.
                    ManifestationManager.Instance.NotifyPlayerActivity(this, csmPacket.MethodId);

                    PacketRouter.RoutePacket(_handler, csmPacket.Packet);
                    break;

                case ClientMessageOpcode.Ping:
                    var pingMessage = GetMessageAs<PingMessage>(protocolPacket);

                    SendMessage(pingMessage, delay: false);
                    break;
            }
        }

        internal bool HandleMovement(Movement movement)
        {
            lock (_clientLock)
            {
                if (State != ClientState.Ingame || Player?.MapChannel == null || Player.Id == 0 ||
                    PendingTransfer != null || Player.Disconected || Player.RemoveFromMap ||
                    !CellManager.Instance.IsInWorld(this))
                {
                    Logger.WriteLog(LogType.Network, $"Ignored movement outside the active world state: {State}.");
                    return false;
                }

                if (movement == null ||
                    !CellManager.TryGetCellCoordinates(movement.Position, out _, out _) ||
                    !float.IsFinite(movement.Velocity) || movement.Velocity < 0 ||
                    !float.IsFinite(movement.ViewDirection.X) || !float.IsFinite(movement.ViewDirection.Y))
                {
                    Logger.WriteLog(LogType.Network, "Rejected movement with invalid coordinates or motion values.");
                    return false;
                }

                if (!ManifestationManager.Instance.AcceptMove(this, movement))
                    return false;

                var previousPosition = Player.Position;
                Player.Position = movement.Position;
                MissionInteractionPolicy.InvalidateIfUnavailable(this);
                Player.Rotation = movement.ViewDirection.X;
                Movement = movement;
                MissionAreaService?.RecordAcceptedMovement(
                    this,
                    previousPosition,
                    movement.Position);
                ManifestationManager.Instance.NotifyPlayerActivity(this);
                CellManager.Instance.UpdateVisibility(this);
                CellMoveObject(this, new MoveObjectMessage(Player.EntityId, movement), true);
                return true;
            }
        }

        internal void SetWorldPosition(Vector3 position, double rotation)
        {
            Player.Position = position;
            MissionInteractionPolicy.InvalidateIfUnavailable(this);
            Player.Rotation = rotation;
            Movement = new Movement(position, new Vector2((float)rotation, 0));
        }

        internal void RestoreTransferOrigin()
        {
            var transfer = PendingTransfer;
            if (transfer == null || Player == null)
                return;

            Player.MapChannel = transfer.OriginMap;
            Player.MapContextId = transfer.OriginMap.MapInfo.MapContextId;
            SetWorldPosition(transfer.OriginPosition, transfer.OriginRotation);
            LoadingMap = transfer.OriginMap.MapInfo.MapContextId;
            AwaitingMapLoaded = false;
            PendingTransfer = null;
        }

        /// <summary>
        /// The character is in the world: registered with the EntityManager, standing in a map's
        /// cells, and holding inventory lists that name live entities. Teleporting counts - a
        /// dropship ride keeps the manifestation and everything registered with it - but Loading
        /// does not, because a map change tears all of that down and MapLoaded builds it again.
        /// </summary>
        public bool IsInWorld => State == ClientState.Ingame || State == ClientState.Teleporting;

        /// <summary>
        /// The methods the real client sends while it is not in the world, taken from the module
        /// each one is sent from: character creation and selection
        /// (client/inputstate/charactercreation.py, characterselection.py), the loading screen's
        /// MapLoaded (wonkavator.py), the ping it keeps up throughout (game.py), and the
        /// account-wide options it can save from anywhere (clientmethod.py).
        ///
        /// Everything else names an entity, a map, a character or a clan, and only means
        /// anything while the character is in the world. SaveCharacterOptions is deliberately
        /// absent: it writes rows keyed on the character id, which is 0 until one is chosen.
        /// </summary>
        private static readonly HashSet<GameOpcode> WorldlessMethods = new()
        {
            GameOpcode.RequestCharacterName,
            GameOpcode.RequestFamilyName,
            GameOpcode.RequestCreateCharacterInSlot,
            GameOpcode.RequestCloneCharacterToSlot,
            GameOpcode.RequestDeleteCharacterInSlot,
            GameOpcode.RequestSwitchToCharacterInSlot,
            GameOpcode.StoreUserClientInformation,
            GameOpcode.MapLoaded,
            GameOpcode.Ping,
            GameOpcode.SaveUserOptions
        };

        /// <summary>
        /// Whether this connection may call that method now. There was no such check: every one
        /// of the handlers was reachable in any state, which is what made the stale inventory
        /// lists of a logged-out or mid-zone client worth anything to whoever kept them.
        /// </summary>
        private bool IsExpected(GameOpcode methodId)
        {
            if (IsInWorld || WorldlessMethods.Contains(methodId))
                return true;

            ReportOutOfState(methodId);

            return false;
        }

        /// <summary>How long this client's refusals stay quiet after one has been logged.</summary>
        private const long RefusalLogQuietMs = 5000;

        private bool _refusalLogged;
        private long _refusalsSinceLog;
        private long _nextRefusalLogTick;

        /// <summary>
        /// The first refusal in full, then at most one every RefusalLogQuietMs saying how many
        /// stood behind it. A client can send these as fast as the wire allows and the log writes
        /// synchronously on the loop thread, so a line each would be the denial of service the
        /// refusal is there to prevent. Refusing costs the packet, not the connection: the real
        /// client has a few of its own to send as it crosses in and out of the world.
        /// </summary>
        private void ReportOutOfState(GameOpcode methodId)
        {
            _refusalsSinceLog++;

            var now = Environment.TickCount64;

            if (_refusalLogged && now < _nextRefusalLogTick)
                return;

            var repeat = _refusalsSinceLog > 1 ? $" ({_refusalsSinceLog} refused since the last of these)" : "";

            Logger.WriteLog(LogType.Security,
                $"Client {Socket.RemoteAddress} sent {methodId} in state {State}; ignored{repeat}.");

            _refusalLogged = true;
            _refusalsSinceLog = 0;
            _nextRefusalLogTick = now + RefusalLogQuietMs;
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
            // The frame body is one byte of padding count, that many bytes of padding (the
            // count byte itself is the first of them), then the packet - so the packet moves
            // right by the count and the cipher runs over the lot. This used to be done through
            // a second pool buffer per send, which doubled what every send took from a pool the
            // whole server shares, and dereferenced the null it gets when that pool is empty.
            var paddingCount = (byte) (8 - length % 8);
            var start = data.BaseOffset + data.Offset;

            if (data.Offset + length + paddingCount > data.MaxLength)
                throw new InvalidOperationException($"A {length} byte packet leaves no room for its {paddingCount} bytes of padding.");

            Array.Copy(data.Buffer, start, data.Buffer, start + paddingCount, length);
            Array.Clear(data.Buffer, start, paddingCount);
            data.Buffer[start] = paddingCount;

            length += paddingCount;

            GameCryptManager.Encrypt(data.Buffer, start, ref length, length, Data);
        }

        private bool OnDecrypt(BufferData data)
        {
            return DecryptFrame(data, Data);
        }

        internal static bool DecryptFrame(BufferData data, ClientCryptData cryptData)
        {
            var length = data.RemainingLength;
            if (length < 8 || length % 8 != 0)
                return false;

            var result = GameCryptManager.Decrypt(data.Buffer, data.BaseOffset + data.Offset, length, cryptData);
            if (!result)
                return false;

            var blowfishPadding = data[data.Offset] & 0xF;
            if (blowfishPadding < 1 || blowfishPadding > 8 || blowfishPadding > data.RemainingLength)
                return false;

            data.Offset += blowfishPadding;

            return true;
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        /// <summary>
        /// The socket has given up on this connection - a full send queue, a stream that stopped
        /// framing, or no buffers left to serve it. Close without trying to send anything: either
        /// nothing can reach them, or nothing they send can be read.
        /// </summary>
        private void OnDrop(string reason)
        {
            Close(false);
        }
		
        private void OnReceive(BufferData data)
        {
            var count = data.RemainingLength;
            if (count <= 0)
                return;

            var chunk = ArrayPool<byte>.Shared.Rent(count);
            Buffer.BlockCopy(data.Buffer, data.BaseOffset + data.Offset, chunk, 0, count);

            var overflowed = false;
            var pending = 0;

            lock (_pendingChunksLock)
            {
                if (State == ClientState.Disconnected)
                {
                    ArrayPool<byte>.Shared.Return(chunk);
                    return;
                }

                pending = _pendingBytes + count;
                if (pending > MaxPendingBytes)
                {
                    overflowed = true;
                    ArrayPool<byte>.Shared.Return(chunk);
                }
                else
                {
                    _pendingBytes = pending;
                    _pendingChunks.Enqueue((chunk, count));
                }
            }

            if (!overflowed)
                return;

            Logger.WriteLog(LogType.Security, $"Client {Socket.RemoteAddress} has {pending} bytes of undrained input (limit {MaxPendingBytes}), disconnecting.");
            Close(false);
        }

        // MainLoop thread. Moves everything the socket thread has handed off into the
        // stream. AddSharedPoolArray takes ownership of the pooled array; RemoveBytes
        // returns it to the pool once it has been consumed, exactly as before.
        private void DrainPendingChunks()
        {
            lock (_pendingChunksLock)
            {
                while (_pendingChunks.TryDequeue(out var chunk))
                {
                    _pendingBytes -= chunk.Length;
                    _incomingDataQueue.AddSharedPoolArray(chunk.Buffer, chunk.Length);
                }
            }
        }

        // State is set before this is called. The shared lock makes an OnReceive already in
        // progress either enqueue before this drain or observe Disconnected and return its rent.
        private void DiscardPendingChunks()
        {
            lock (_pendingChunksLock)
            {
                while (_pendingChunks.TryDequeue(out var chunk))
                {
                    _pendingBytes -= chunk.Length;
                    ArrayPool<byte>.Shared.Return(chunk.Buffer);
                }
            }
        }

        /// <summary>
        /// Hands the inbound stream's pooled arrays back. Anything drained into it but not yet
        /// decoded - a partial frame, which is what every Alt+F4 leaves behind - is held by
        /// arrays only Dispose returns, so the shared pool lost a block on each of those
        /// disconnects. DiscardPendingChunks covers the other half, the chunks not yet drained.
        ///
        /// Called by the MainLoop when it finally drops the client, not by Close(): Close() runs
        /// on socket threads too, and this stream belongs to the MainLoop. Disposing it from
        /// under a tick that is mid-decode is the same class of bug as the one the hand-off
        /// queue exists to prevent.
        /// </summary>
        internal void ReleaseInboundBuffers()
        {
            _incomingDataQueue.Dispose();
        }

        private IEnumerable<ProtocolPacket> DecodeIncomingPackets()
        {
            // A skipped packet (out of order, or the untyped send-timeout check) used to come
            // back as null too, which read as "no more data" and ended the loop for the tick;
            // everything queued behind it waited for the next one, 100 ms later, and a stream
            // with a skipped packet in every tick fell further behind on each. Skips now
            // continue and only an incomplete frame stops.
            while (TryDecodeNextPacket(out var packet))
                if (packet != null)
                    yield return packet;
        }

        /// <returns>
        /// false when the queue holds no complete frame; true otherwise, with the packet, or
        /// null for a frame that was consumed and dropped.
        /// </returns>
        private bool TryDecodeNextPacket(out ProtocolPacket packet)
        {
            packet = null;

            // If there is not enough data to read the packet size at all, then stop processing
            if (_incomingDataQueue.Length < 2)
                return false;

            using var br = new BinaryReader(_incomingDataQueue, Encoding.UTF8, true);

            // Peek the packet size to determine if the whole packet has arrived
            var startPosition = _incomingDataQueue.Position;

            // Read the size of the next packet
            var packetSize = br.ReadUInt16();

            // Rewind the stream to the starting position
            _incomingDataQueue.Position = startPosition;

            // If the packet is fragmented and not all the fragments has arrived yet, then stop processing
            if (packetSize > _incomingDataQueue.Length)
                return false;

            // Construct and the packet
            var rawPacket = new ProtocolPacket();

            rawPacket.Read(br);

            // Check for overreading or underreading the packet
            if (_incomingDataQueue.Position != startPosition + packetSize)
                throw new Exception($"ProtocolPacket over or under read! Start position: {startPosition} | Packet size: {packetSize} | End position: {_incomingDataQueue.Position}!");

            // Advance the stream by removing the already processed data
            _incomingDataQueue.RemoveBytes(packetSize);

            if (rawPacket.Channel != 0 && !TryAcceptSequence(rawPacket.Channel, rawPacket.SequenceNumber))
            {
                Logger.WriteLog(LogType.Debug,
                    $"Dropped out-of-order packet on channel {rawPacket.Channel} (seq {rawPacket.SequenceNumber}, last {ReceiveSequence[rawPacket.Channel]}) from {Socket.RemoteAddress}.");
                return true;
            }

            // Some internal send timeout check, skip the packet
            if (rawPacket.Type == ClientMessageOpcode.None)
            {
                if (rawPacket.Size != 4)
                    Logger.WriteLog(LogType.Debug, $"Skipped an untyped packet of size {rawPacket.Size} (expected the 4-byte send-timeout check) from {Socket.RemoteAddress}.");

                return true;
            }

            packet = rawPacket;

            return true;
        }

        internal bool TryAcceptSequence(byte channel, uint sequence)
        {
            if (channel == 0)
                return true;

            if (_receivedSequence[channel] &&
                unchecked((int)(sequence - ReceiveSequence[channel])) <= 0)
                return false;

            _receivedSequence[channel] = true;
            ReceiveSequence[channel] = sequence;
            return true;
        }
        #endregion

        public void SaveCharacter()
        {
            var player = Player;

            // Player is field-initialized to an empty Manifestation, so a null check alone
            // never fires. A client that disconnects before entering the world (character
            // selection, failed login, idle timeout) still has Id == 0, and looking that up
            // throws EntityNotFoundException.
            if (player == null || player.Id == 0)
            {
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Characters.SaveCharacter(player);
            unitOfWork.Complete();
        }

        internal bool SaveCharacterOnDisconnect()
        {
            try
            {
                SaveCharacter();
                return true;
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error, $"Unable to save character {Player?.Id} on disconnect: {error.Message}");
                return false;
            }
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
