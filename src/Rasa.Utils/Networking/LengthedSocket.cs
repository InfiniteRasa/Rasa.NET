using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Rasa.Networking
{
    using Extensions;
    using Memory;
    using Packets;

    public enum SizeType : byte
    {
        None  = 0,
        Char  = 1,
        Word  = 2,
        Dword = 4
    }

    public class LengthedSocket
    {
        public delegate void AcceptHandler(LengthedSocket acceptedSocket);
        public delegate void AsyncHandler(SocketAsyncEventArgs args);
        public delegate void ReceiveHandler(BufferData data);
        public delegate void DisconnectHandler();
        public delegate void DropHandler(string reason);
        public delegate void EncryptDelegate(BufferData data, ref int length);
        public delegate bool DecryptDelegate(BufferData data);

        public SizeType SizeHeaderLength { get; }
        public bool CountSize { get; }
        public int LengthSize => (int) SizeHeaderLength;
        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        private IPAddress _remoteAddress;

        /// <summary>
        /// The other side's address, kept from the first time it is asked for so it can still
        /// be logged once the socket has been closed - which is when most of the asking happens.
        /// </summary>
        public IPAddress RemoteAddress
        {
            get
            {
                if (_remoteAddress != null)
                    return _remoteAddress;

                try
                {
                    _remoteAddress = ((IPEndPoint) Socket.RemoteEndPoint)?.Address;
                }
                catch (ObjectDisposedException)
                {
                    // Closed before anyone asked; there is nothing left to read it from.
                }

                return _remoteAddress ?? IPAddress.None;
            }
        }

        public bool AutoReceive { get; set; } = true;

        public AsyncHandler OnConnect;
        public DisconnectHandler OnDisconnect;
        public AcceptHandler OnAccept;
        public AsyncHandler OnSend;
        public ReceiveHandler OnReceive;
        public AsyncHandler OnError;
        public EncryptDelegate OnEncrypt;
        public DecryptDelegate OnDecrypt;

        public LengthedSocket(SizeType sizeHeaderLen, bool countSize = true)
           : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), sizeHeaderLen, countSize)
        {
        }

        public LengthedSocket(Socket s, SizeType sizeHeaderLen, bool countSize)
        {
            Socket = s;
            SizeHeaderLength = sizeHeaderLen;
            CountSize = countSize;
        }

        #region Sending

        /// <summary>
        /// One send may be in flight on a socket at a time; the rest wait here in the order they
        /// were written.
        ///
        /// Two things went wrong without this. A send that only transfers part of its buffer is
        /// resumed by issuing the remainder afterwards, so anything sent in between landed in the
        /// middle of it - and the other side is reading a length-prefixed stream, so a packet
        /// split around another packet's bytes is not a garbled message, it is a frame boundary
        /// in the wrong place and everything after it is misread. Partial sends happen when the
        /// socket's buffer is full, which is to say under load. Separately, overlapping sends have
        /// no ordering guarantee between them at all.
        ///
        /// The queue holds args that are already serialised and encrypted: the cipher is a stream
        /// and its state has to advance in the same order the bytes go out, so Send does that work
        /// under the same lock that fixes the order.
        /// </summary>
        private readonly object _sendLock = new object();
        private readonly Queue<SocketAsyncEventArgs> _sendQueue = new Queue<SocketAsyncEventArgs>();
        private bool _sending;
        private bool _sendClosed;

        /// <summary>
        /// Guards the one-receive-per-socket rule. <see cref="ReceiveAsync()"/> explains why it
        /// matters; these two say where this socket is: dispatching a buffer to its handlers, and
        /// whether one of those handlers asked for a receive while it was.
        /// </summary>
        private readonly object _receiveLock = new object();
        private bool _receiveDispatching;
        private bool _receiveDeferred;

        /// <summary>
        /// How far a client is allowed to fall behind before it is dropped, in pooled blocks. Each
        /// queued send holds a SocketAsyncEventArgs and a block from pools every connection shares,
        /// so one client that has stopped reading must not be able to starve the rest - the same
        /// reasoning as the inbound flood cap, in the other direction.
        ///
        /// This was 512, when every packet had a block to itself: 512 packets, however small, and
        /// twenty stalled connections were enough to empty a 10,240 block pool. Frames are packed
        /// into blocks now (<see cref="Send(IReadOnlyList{IBasePacket})"/>), so 128 is a megabyte
        /// behind at the default block size - a whole login's worth of inventory and entities on a
        /// slow link - and eighty stalled connections, not twenty, to empty the pool.
        /// </summary>
        private const int MaxQueuedSends = 128;

        /// <summary>
        /// How long a send may sit on the socket without completing, while more wait behind it,
        /// before the connection is taken for one whose peer has stopped reading. A queue under
        /// the cap used to be able to hold its blocks for as long as TCP kept retransmitting.
        /// </summary>
        private const long SendStallMs = 30000;

        /// <summary>When the send now on the socket was issued (Environment.TickCount64).</summary>
        private long _sendIssuedAt;

        #endregion

        #region SocketAsyncEventArgs
        private static Stack<SocketAsyncEventArgs> _socketAsyncEventArgsPool;

        private static readonly object ArgsInitLock = new object();

        public static void InitializeEventArgsPool(int eventArgsPoolCount)
        {
            if (_socketAsyncEventArgsPool != null)
                return;

            lock (ArgsInitLock)
            {
                if (_socketAsyncEventArgsPool != null)
                    return;

                _socketAsyncEventArgsPool = new Stack<SocketAsyncEventArgs>(eventArgsPoolCount);

                for (var i = 0; i < eventArgsPoolCount; ++i)
                    _socketAsyncEventArgsPool.Push(new SocketAsyncEventArgs());
            }
        }

        /// <summary>
        /// Takes an args, and a buffer for it where the operation needs one, or returns null if
        /// either pool is empty. Both pools are shared by every connection, so running dry is a
        /// load condition the caller has to answer for - by dropping the connection it was about
        /// to serve - rather than an error to throw on a socket thread that will not catch it.
        /// </summary>
        private SocketAsyncEventArgs SetupEventArgs(SocketAsyncOperation operation)
        {
            SocketAsyncEventArgs args;

            lock (_socketAsyncEventArgsPool)
                args = _socketAsyncEventArgsPool.Count > 0 ? _socketAsyncEventArgsPool.Pop() : null;

            if (args == null)
                return null;

            switch (operation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.Send:
                    var data = BufferManager.RequestBuffer();

                    if (data == null)
                    {
                        // Do not hold the args hostage to a buffer we could not get.
                        lock (_socketAsyncEventArgsPool)
                            _socketAsyncEventArgsPool.Push(args);

                        return null;
                    }

                    args.SetBuffer(BufferManager.Buffer, data.BaseOffset, data.MaxLength);
                    args.UserToken = data;
                    args.AcceptSocket = Socket;
                    break;

                case SocketAsyncOperation.Connect:
                    args.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
            }

            args.Completed += OperationCompleted;

            return args;
        }

        private void TeardownEventArgs(SocketAsyncEventArgs args)
        {
            // Free by what the args is actually holding rather than by LastOperation. An args
            // that was set up for a send and then discarded - the socket closed underneath it,
            // or the queue overflowed - never started an operation, so its LastOperation is
            // still whatever the previous user of that pool entry did, and its buffer would
            // have been handed back to the pool while a BufferData still pointed at it.
            var buffer = args.GetUserToken<BufferData>();

            if (buffer != null)
            {
                BufferManager.FreeBuffer(buffer);

                args.SetBuffer(null, 0, 0);
                args.UserToken = null;
            }

            if (args.LastOperation == SocketAsyncOperation.Connect)
                args.RemoteEndPoint = null;

            args.AcceptSocket = null;

            // The listener's accept args is this socket's own and is never pooled; see AcceptAsync.
            if (args == _acceptArgs)
                return;

            args.Completed -= OperationCompleted;

            lock (_socketAsyncEventArgsPool)
                _socketAsyncEventArgsPool.Push(args);
        }

        private enum Completion
        {
            /// <summary>The args has been re-armed and is in flight again; leave it be.</summary>
            Pending,

            /// <summary>The args is finished with and goes back to the pool.</summary>
            Released,

            /// <summary>As Released, and a whole send went out, so the next queued send may start.</summary>
            SendFinished
        }

        /// <summary>
        /// Runs on a socket completion thread. Nothing may get out of it: an exception that
        /// escapes here belongs to no connection, and the runtime ends the process. Everything
        /// the handlers do - decrypting, decoding, the key exchange, the queue and auth packets,
        /// the communicator - runs inside this, so until now a fault in any of them for one
        /// client's bytes took every other client down with it. Now it costs that client its
        /// connection: the fault is logged, the owner is told through OnError, and the args and
        /// buffer go back to their pools.
        /// </summary>
        private void OperationCompleted(object o, SocketAsyncEventArgs args)
        {
            Completion outcome;

            try
            {
                outcome = OperationCompletedCore(args);
            }
            catch (Exception e)
            {
                SafeLog($"Unhandled exception completing a socket {args.LastOperation} for {SafeRemoteAddress()}, closing the connection: {e}");

                // Whatever was in flight, nothing more is going out on this socket.
                DiscardQueuedSends();

                try
                {
                    OnError?.Invoke(args);
                }
                catch (Exception inner)
                {
                    SafeLog($"OnError handler threw for {SafeRemoteAddress()}: {inner}");
                }

                // The core only lets an exception out of a handler or a synchronous re-arm that
                // did not start, so the args is not in flight and has not been torn down.
                outcome = Completion.Released;
            }

            if (outcome == Completion.Pending)
                return;

            TeardownEventArgs(args);

            if (outcome == Completion.SendFinished)
                SendNext();
        }

        private Completion OperationCompletedCore(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success || (args.LastOperation == SocketAsyncOperation.Receive && args.BytesTransferred == 0))
            {
                // A failed send leaves the socket marked busy for ever and nothing would go out
                // again; the connection is finished either way, so let go of what was waiting.
                if (args.LastOperation == SocketAsyncOperation.Send)
                    DiscardQueuedSends();

                OnError?.Invoke(args);
                return Completion.Released;
            }

            var data = args.GetUserToken<BufferData>();

            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    data.ByteCount += args.BytesTransferred;

                    // We've transferred less bytes than we should have
                    if (data.Length > data.ByteCount)
                    {
                        args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                        // Sending the rest is safe now: the queue holds everything else back
                        // until this args reports the whole buffer gone.
                        SendAsync(args);
                        return Completion.Pending;
                    }

                    OnSend?.Invoke(args);
                    return Completion.SendFinished;

                case SocketAsyncOperation.Receive:
                    // This value may change in the middle of processing, causing odd behavior
                    var receiveAfter = AutoReceive;

                    InputResult result;

                    lock (_receiveLock)
                        _receiveDispatching = true;

                    try
                    {
                        result = ProcessInputBuffer(data, args);
                    }
                    finally
                    {
                        lock (_receiveLock)
                            _receiveDispatching = false;
                    }

                    // A receive is already armed on this args to finish a partial frame. Any
                    // deferred request keeps until that frame lands, which is the pass that
                    // decides whether to arm again.
                    if (result == InputResult.Reading)
                        return Completion.Pending;

                    bool deferred;

                    lock (_receiveLock)
                    {
                        deferred = _receiveDeferred;
                        _receiveDeferred = false;
                    }

                    // A broken stream gets its buffer back but no new receive: there is nothing
                    // left to read that could be made sense of. That beats a handler's request
                    // too - the handover it belonged to is moot on a connection being dropped.
                    if (result != InputResult.Consumed)
                        return Completion.Released;

                    // Otherwise a handler that took the socket over mid-dispatch and asked for a
                    // receive gets one, whatever AutoReceive says: AutoReceive was read before
                    // the handover and describes the owner this socket no longer has.
                    if (deferred || receiveAfter)
                        StartReceive();

                    return Completion.Released;

                case SocketAsyncOperation.Connect:
                    OnConnect?.Invoke(args);
                    return Completion.Released;

                case SocketAsyncOperation.Accept:
                    var accepted = args.AcceptSocket;

                    // Done with the args before the handler runs, because the handler's first
                    // move is to re-arm the accept - on this same args, on this same thread.
                    args.AcceptSocket = null;

                    var client = new LengthedSocket(accepted, SizeHeaderLength, CountSize);

                    client.EnableKeepAlive();
                    client.DisableNagle();

                    // The accept args is never torn down and is re-armed by the handler itself,
                    // so a fault here is dealt with on the spot: the connection that could not
                    // be set up is shut, and the listener carries on as if it had been refused.
                    try
                    {
                        OnAccept?.Invoke(client);
                    }
                    catch (Exception e)
                    {
                        SafeLog($"Unhandled exception accepting {client.SafeRemoteAddress()}, refusing the connection: {e}");
                        client.Close();
                    }

                    return Completion.Pending;
            }

            return Completion.Released;
        }

        /// <summary>The last resort must not itself be able to throw, whatever state the logger is in.</summary>
        private static void SafeLog(string message)
        {
            try
            {
                Logger.WriteLog(LogType.Error, message);
            }
            catch (Exception)
            {
                try
                {
                    Console.Error.WriteLine(message);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        /// <summary>
        /// The frame length the header at the start of <paramref name="data"/> announces,
        /// header included. Read unsigned: the header is a byte count, and reading it signed
        /// turned a Word of 0x8000 or a Dword of 0x80000000 and up into a negative length. The
        /// range check after this catches those either way; this just says what the other side
        /// wrote when it is logged.
        /// </summary>
        private long ReadSize(BufferData data)
        {
            var headerSize = !CountSize ? LengthSize : 0;

            switch (SizeHeaderLength)
            {
                case SizeType.Char:
                    return headerSize + data[0];

                case SizeType.Word:
                    return headerSize + BitConverter.ToUInt16(data.Buffer, data.BaseOffset);

                case SizeType.Dword:
                    return headerSize + BitConverter.ToUInt32(data.Buffer, data.BaseOffset);

                default:
                    throw new NotImplementedException($"Only 1, 2 and 4 byte headers are supported! {SizeHeaderLength} is not!");
            }
        }

        private enum InputResult
        {
            /// <summary>A receive is already running on this args; leave it be.</summary>
            Reading,

            /// <summary>Everything in the buffer has been handed on; it can be freed and read into again.</summary>
            Consumed,

            /// <summary>The stream cannot be framed any further. The connection is being dropped.</summary>
            Broken
        }

        private InputResult ProcessInputBuffer(BufferData data, SocketAsyncEventArgs args)
        {
            data.ByteCount += args.BytesTransferred;

            while (true)
            {
                // Whether the length is known is its own flag rather than a -1 in the length.
                // ReadSize adds the header to what the other side wrote, so a client that writes
                // -5 produces a length of -1 and used to be read as "the header has not arrived
                // yet" - which sent the receive off with the previous frame's length still set.
                var haveLength = data.ByteCount >= LengthSize;
                var length = haveLength ? ReadSize(data) : 0L;

                if (haveLength)
                {
                    // A length that no buffer could ever hold, or one with nothing after its own
                    // header, is not a packet we are behind on - it is a length field read out of
                    // step with the stream, or something speaking a different protocol at us.
                    // There is no way to resynchronise a length-prefixed stream once that happens,
                    // so the connection goes. This used to throw OutOfMemoryException on the socket
                    // thread, where nothing caught it.
                    if (length <= LengthSize || length > BufferManager.BlockSize)
                    {
                        Drop($"framing lost - a frame of {length} bytes, against a {BufferManager.BlockSize} byte buffer");
                        return InputResult.Broken;
                    }

                    // It fits in a buffer, just not in what is left of this one behind the frames
                    // already read out of it. Slide the unread bytes back to the front and carry
                    // on - this is an ordinary read that ended mid-frame, not a bad packet.
                    if (length > data.MaxLength)
                        Compact(data);

                    data.Length = (int) length;
                }

                if (!haveLength)
                {
                    // The length header itself is split across the end of the block. Slide the
                    // bytes that did arrive back to the front first, or the receive below gets
                    // handed whatever room is left behind the frames already read - which at the
                    // end of a full buffer is none at all, and a zero length receive completes
                    // immediately and reads as a closed connection.
                    Compact(data);

                    data.Length = data.MaxLength;
                }

                if (!haveLength || data.ByteCount < length)
                {
                    args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                    ReceiveAsync(args);
                    return InputResult.Reading;
                }

                data.Offset = LengthSize;
                data.Length = (int) length;

                // A decrypt that says no is a frame that did not come from the other side of
                // this cipher: the auth checksum failed, or the body is not a whole number of
                // cipher blocks. Its bytes are not a packet, and the stream behind it cannot be
                // trusted to frame any better. The result used to be discarded and the frame
                // handed on regardless.
                if (OnDecrypt != null && !OnDecrypt(data))
                {
                    Drop($"a {length} byte frame failed decryption or its integrity check");
                    return InputResult.Broken;
                }

                OnReceive?.Invoke(data);

                if (data.ByteCount == length)
                    break;

                data.ByteCount -= (int) length;
                data.BaseOffset += (int) length;
                data.Offset = 0;
                data.Length = data.MaxLength;
            }

            return InputResult.Consumed;
        }

        /// <summary>
        /// Moves the bytes that have not been read yet back to the start of the buffer, so the
        /// frame they belong to has the whole block to arrive into.
        /// </summary>
        private static void Compact(BufferData data)
        {
            if (data.BaseOffset == data.RealBaseOffset)
                return;

            Array.Copy(data.Buffer, data.BaseOffset, data.Buffer, data.RealBaseOffset, data.ByteCount);

            data.BaseOffset = data.RealBaseOffset;
            data.Offset = 0;
        }
        #endregion

        /// <summary>Idle time before the first keepalive probe, the gap between probes, and how many go unanswered before the connection is given up.</summary>
        public const int KeepAliveIdleSeconds = 30;
        public const int KeepAliveIntervalSeconds = 10;
        public const int KeepAliveProbes = 5;

        /// <summary>
        /// Turns on TCP keepalive, so a peer that has gone without a word - power cut, sleep, a
        /// NAT or Wi-Fi drop, a client that died on a loading screen - surfaces as a socket error
        /// within about a minute and a half (KeepAliveIdleSeconds + KeepAliveIntervalSeconds x
        /// KeepAliveProbes) instead of never.
        ///
        /// Without it the only thing that ever noticed a vanished peer was a send timing out,
        /// and a connection nothing is sent to - a player at the character screen, a queue
        /// connection that has been handed off, a world login that stalled mid-load - sat open
        /// for as long as the process ran, holding its pooled receive buffer and, on the world
        /// port, counting as the account being logged in. The probes only go out while the
        /// connection is idle, so a player in the world, whose client talks constantly, sends
        /// none of them.
        ///
        /// Set on every accepted socket. Each option is tried on its own and a refusal is
        /// ignored: the interval and probe count need a Windows 10 of 1703 or later, and a
        /// system without them keeps its own defaults for those, which only makes detection
        /// slower.
        /// </summary>
        public void EnableKeepAlive()
        {
            try
            {
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch (Exception)
            {
                return;
            }

            TrySetTcpOption(SocketOptionName.TcpKeepAliveTime, KeepAliveIdleSeconds);
            TrySetTcpOption(SocketOptionName.TcpKeepAliveInterval, KeepAliveIntervalSeconds);
            TrySetTcpOption(SocketOptionName.TcpKeepAliveRetryCount, KeepAliveProbes);
        }

        private void TrySetTcpOption(SocketOptionName option, int value)
        {
            try
            {
                Socket.SetSocketOption(SocketOptionLevel.Tcp, option, value);
            }
            catch (Exception)
            {
                // Not supported here; the system's own value stands.
            }
        }

        /// <summary>
        /// Sends go out as they are issued rather than held for an ACK. Frames are already packed
        /// into blocks by <see cref="Send(IReadOnlyList{IBasePacket})"/>, usually one a tick
        /// for a world client, so Nagle's algorithm has nothing left to coalesce and only adds its
        /// wait - up to a delayed ACK - to every movement update.
        /// </summary>
        public void DisableNagle()
        {
            try
            {
                Socket.NoDelay = true;
            }
            catch (Exception)
            {
                // Not a TCP socket, or already closed.
            }
        }

        public void Bind(EndPoint ep)
        {
            Socket.Bind(ep);
        }

        public void Listen(int backlog)
        {
            Socket.Listen(backlog);
        }

        /// <summary>
        /// This socket's own accept args, outside the pool.
        ///
        /// Accepting is how the server gets out of being short of args in the first place: a
        /// connection that arrives and goes away frees several, and the queue server exists to
        /// turn arrivals away politely. A listener that could not accept because the pool was
        /// empty would stop accepting for good, since nothing but an accept re-arms the accept.
        /// It carries no buffer and only ever has one accept outstanding, so one is enough.
        /// </summary>
        private SocketAsyncEventArgs _acceptArgs;

        public void AcceptAsync()
        {
            if (_acceptArgs == null)
            {
                _acceptArgs = new SocketAsyncEventArgs();
                _acceptArgs.Completed += OperationCompleted;
            }

            _acceptArgs.AcceptSocket = null;

            if (!Socket.AcceptAsync(_acceptArgs))
                OperationCompleted(Socket, _acceptArgs);
        }

        public void ConnectAsync(EndPoint remote)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Connect);

            if (args == null)
            {
                Drop("no connection slots left to connect with");
                return;
            }

            args.RemoteEndPoint = remote;

            if (!Socket.ConnectAsync(args))
                OperationCompleted(Socket, args);
        }

        /// <summary>
        /// Arms a receive, unless this socket is already inside one.
        ///
        /// One receive is in flight per socket, and everything downstream depends on it: bytes
        /// from a connection have to reach its owner in the order they arrived, and two
        /// completions on one socket can run on two IOCP threads at once. The completion path
        /// keeps to that by only re-arming after it has finished dispatching, but a *handler*
        /// can call this from inside that dispatch - the login exchange hands the socket to the
        /// game client, whose RegisterAtServer arms a receive of its own while the frame loop it
        /// was called from is still running. The new receive could then complete on a second
        /// thread while this one is still handing the rest of the buffer to the same client, and
        /// two producers would be feeding the client's hand-off queue at once. That queue is
        /// thread-safe; the byte order it is given is not something it can repair.
        ///
        /// So a request made mid-dispatch is remembered and honoured as the dispatch unwinds.
        /// </summary>
        public void ReceiveAsync()
        {
            lock (_receiveLock)
            {
                if (_receiveDispatching)
                {
                    _receiveDeferred = true;
                    return;
                }
            }

            StartReceive();
        }

        private void StartReceive()
        {
            var args = SetupEventArgs(SocketAsyncOperation.Receive);

            if (args == null)
            {
                // Nothing left to read into. Dropping this connection is what frees the buffers
                // the rest of them are waiting on.
                Drop("no buffers left to receive into");
                return;
            }

            ReceiveAsync(args);
        }

        private void ReceiveAsync(SocketAsyncEventArgs args)
        {
            bool pending;

            try
            {
                pending = Socket.ReceiveAsync(args);
            }
            catch (Exception e)
            {
                TeardownEventArgs(args);
                Drop($"receive failed to start: {e.Message}");
                return;
            }

            if (!pending)
                OperationCompleted(Socket, args);
        }

        public void Send(IBasePacket packet)
        {
            Send(new[] { packet });
        }

        /// <summary>
        /// Sends the packets in order, as many frames to a pooled block as fit, and one SendAsync
        /// per block.
        ///
        /// Every packet used to take a block and a SendAsync of its own: a 40-byte movement update
        /// held 8 KB of a pool every connection shares until it went out, and each one was a trip
        /// into the socket layer. The world sends a player one relayed packet for every move,
        /// shot and hit within a hundred metres, so both costs grew with the square of how many
        /// were standing together. The other side reads a length-prefixed stream and does not
        /// care where one send ends and the next begins - Nagle's algorithm was already gluing
        /// these together on the wire, a delayed ACK late - so frames are laid end to end in a
        /// block, and a block goes out when it is full or the packets are done.
        ///
        /// A packet that does not fit in what is left of a block starts the next one. One that
        /// does not fit in an empty block, or cannot be written at all, is logged and skipped as
        /// before: every frame stands on its own, so the stream is intact without it.
        ///
        /// Each block is written and queued under the send lock, as a single packet was, because
        /// the order the bytes are handed to the socket is the order they are framed in. The block
        /// is taken from the pool before the lock, never while holding it (see below).
        /// </summary>
        public void Send(IReadOnlyList<IBasePacket> packets)
        {
            if (packets == null)
                return;

            var next = 0;

            while (next < packets.Count)
            {
                if (_sendClosed)
                    return;

                // Taken before the send lock: TeardownEventArgs takes the pool lock too, and it
                // runs on the completion path which then takes the send lock to start the next
                // send - so holding the send lock while reaching for the pool would wait on a
                // thread that is waiting for this one.
                var args = SetupEventArgs(SocketAsyncOperation.Send);

                if (args == null)
                {
                    Drop("no buffers left to send with");
                    return;
                }

                var start = false;
                var discard = false;
                var overflowed = false;
                var stalled = false;

                lock (_sendLock)
                {
                    if (_sendClosed)
                    {
                        discard = true;
                        next = packets.Count;
                    }
                    else
                    {
                        var used = FillBlock(packets, ref next, args);

                        if (used == 0)
                        {
                            // Everything that was left was skipped.
                            discard = true;
                        }
                        else if (!_sending)
                        {
                            _sending = true;
                            start = true;
                        }
                        else if (Environment.TickCount64 - Interlocked.Read(ref _sendIssuedAt) > SendStallMs)
                        {
                            _sendClosed = true;
                            stalled = true;
                        }
                        else if (_sendQueue.Count < MaxQueuedSends)
                        {
                            _sendQueue.Enqueue(args);
                        }
                        else
                        {
                            // Too far behind to catch up. Drop the block and the connection with
                            // it - silently discarding frames out of a stream the other side is
                            // reading would desync it just as surely as interleaving would.
                            _sendClosed = true;
                            overflowed = true;
                        }
                    }
                }

                if (start)
                {
                    SendAsync(args);
                    continue;
                }

                if (!discard && !overflowed && !stalled)
                    continue;

                TeardownEventArgs(args);

                if (!overflowed && !stalled)
                    continue;

                // Hand the queued buffers back here rather than leaving it to whoever handles the
                // drop. Nothing else will ever go out on this socket, and the pool entries the
                // queue is sitting on belong to every other connection.
                DiscardQueuedSends();

                Drop(stalled
                    ? $"a send has not completed in {SendStallMs / 1000} s with more waiting behind it"
                    : $"send queue full at {MaxQueuedSends} blocks");

                return;
            }
        }

        private enum FrameResult
        {
            Written,

            /// <summary>Did not fit behind the frames already in the block; it goes in the next one.</summary>
            NoRoom,

            /// <summary>Could not be written into an empty block. Logged; the packet is skipped.</summary>
            Skipped
        }

        /// <summary>
        /// Lays frames from <paramref name="packets"/>, starting at <paramref name="next"/>, end to
        /// end in the args' block and sets the args up to send them. Advances next past every
        /// packet it wrote or skipped, and returns the bytes used.
        /// </summary>
        private int FillBlock(IReadOnlyList<IBasePacket> packets, ref int next, SocketAsyncEventArgs args)
        {
            var data = args.GetUserToken<BufferData>();
            var used = 0;

            while (next < packets.Count)
            {
                var result = TryWriteFrame(packets[next], data, used, out var frameLength);

                if (result == FrameResult.NoRoom)
                    break;

                if (result == FrameResult.Written)
                    used += frameLength;

                next++;
            }

            data.Offset = 0;
            data.Length = used;
            data.ByteCount = 0;

            if (used > 0)
                args.SetBuffer(data.BaseOffset, used);

            return used;
        }

        /// <summary>
        /// Serialises and encrypts one packet at <paramref name="at"/> in the block, with its
        /// length header in front.
        ///
        /// A packet that overruns the block - the writer's stream is the rest of the block, and
        /// is not expandable; the cipher's padding has to fit too - behind other frames is NoRoom
        /// and is written again at the start of the next block. The same failure in an empty
        /// block is a packet no block can hold, and any other throw out of the packet's Write or
        /// OnEncrypt is a packet that cannot be sent; both are logged and skipped. Before the
        /// send path was made safe, such a throw left the args and its buffer out of the pools
        /// for good. Nothing written for a frame that failed goes anywhere: the block ends where
        /// the last good frame did.
        /// </summary>
        private FrameResult TryWriteFrame(IBasePacket packet, BufferData data, int at, out int frameLength)
        {
            frameLength = 0;

            if (at + LengthSize >= data.MaxLength)
                return FrameResult.NoRoom;

            try
            {
                int length;

                // Room for the length header, then the packet in whatever the block has left.
                data.Offset = at + LengthSize;
                data.Length = data.MaxLength;

                using (var sw = data.CreateWriter())
                {
                    packet.Write(sw);

                    length = (int) sw.BaseStream.Position;
                }

                OnEncrypt?.Invoke(data, ref length);

                if (at + LengthSize + length > data.MaxLength)
                    throw new InvalidOperationException($"A {length} byte frame does not fit in the {data.MaxLength - at - LengthSize} bytes left in the block.");

                var sizeLen = CountSize ? length + LengthSize : length;

                for (var i = 0; i < LengthSize; ++i)
                    data[at + i] = (byte) ((sizeLen >> (i * 8)) & 0xFF);

                frameLength = length + LengthSize;

                return FrameResult.Written;
            }
            catch (Exception e)
            {
                if (at > 0)
                    return FrameResult.NoRoom;

                SafeLog($"Could not send a {packet?.GetType().Name ?? "null packet"} to {SafeRemoteAddress()}, skipping it: {e}");

                return FrameResult.Skipped;
            }
        }

        /// <summary>
        /// Raised when the connection cannot be carried on with: its send queue filled up, the
        /// inbound stream stopped framing, or there were no pooled buffers left to serve it. The
        /// reason has already been logged; the owner's job is to close the client.
        /// </summary>
        public DropHandler OnDrop;

        private int _dropped;
        private int _closed;

        /// <summary>
        /// Logs why this connection is going and tells its owner, once. Several paths can notice
        /// the same dying connection in the same moment, and a socket only dies once.
        /// </summary>
        private void Drop(string reason)
        {
            if (Interlocked.Exchange(ref _dropped, 1) != 0)
                return;

            Logger.WriteLog(LogType.Network, $"Dropping {SafeRemoteAddress()}: {reason}.");

            OnDrop?.Invoke(reason);
        }

        private string SafeRemoteAddress()
        {
            try
            {
                return RemoteAddress.ToString();
            }
            catch (System.Exception)
            {
                return "a closed socket";
            }
        }

        /// <summary>
        /// Sends whatever is next in the queue, or marks the socket idle.
        ///
        /// A send that completes synchronously runs OperationCompleted on this thread, which
        /// comes back here - so this recurses once per back-to-back synchronous completion,
        /// bounded by MaxQueuedSends. That only happens while the socket has room, which is the
        /// case where the queue is not deep.
        /// </summary>
        private void SendNext()
        {
            SocketAsyncEventArgs next;

            lock (_sendLock)
            {
                if (_sendQueue.Count == 0 || _sendClosed)
                {
                    _sending = false;
                    return;
                }

                next = _sendQueue.Dequeue();
            }

            SendAsync(next);
        }

        /// <summary>
        /// Throws away anything still waiting to go out and hands its buffers back. Called when
        /// the socket errors or closes: those packets have nowhere to go, and the pool entries
        /// they are holding belong to every other connection.
        /// </summary>
        private void DiscardQueuedSends()
        {
            List<SocketAsyncEventArgs> abandoned;

            lock (_sendLock)
            {
                _sendClosed = true;
                _sending = false;

                if (_sendQueue.Count == 0)
                    return;

                abandoned = new List<SocketAsyncEventArgs>(_sendQueue);
                _sendQueue.Clear();
            }

            foreach (var args in abandoned)
                TeardownEventArgs(args);
        }

        private void SendAsync(SocketAsyncEventArgs args)
        {
            bool pending;

            // Every issue counts as progress, including the rest of a partial send: a peer that is
            // taking bytes, however slowly, is not stalled.
            Interlocked.Exchange(ref _sendIssuedAt, Environment.TickCount64);

            try
            {
                pending = Socket.SendAsync(args);
            }
            catch (Exception e)
            {
                // The operation never started, so the args is ours to return; the socket is
                // finished, so the queue behind it goes too. Without this the args leaked and
                // _sending stayed set, which silenced the connection for good.
                TeardownEventArgs(args);
                DiscardQueuedSends();
                Drop($"send failed to start: {e.Message}");
                return;
            }

            if (!pending)
                OperationCompleted(Socket, args);
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0)
                return;

            DiscardQueuedSends();

            // Read it while it can still be read: the owner logs the address after this.
            _ = RemoteAddress;

            try
            {
                OnDisconnect?.Invoke();

                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // Already shut, never connected, or a listener: nothing to shut down.
            }

            // Shutdown ends the conversation; only Close returns the handle. Without it every
            // connection that ever ended kept its socket until the finalizer got round to it,
            // and a receive that was still armed on it stayed armed. Closing completes that
            // receive with OperationAborted, which the completion path treats as any other
            // error - the owner's OnError, then the args back to the pool.
            try
            {
                Socket.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
