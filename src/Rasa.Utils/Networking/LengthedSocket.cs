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
        /// How far a client is allowed to fall behind before it is dropped. Each queued send holds
        /// a pooled SocketAsyncEventArgs and its buffer, and the pool is shared by every
        /// connection, so one client that has stopped reading must not be able to starve the rest.
        /// The same reasoning as the inbound flood cap, in the other direction.
        /// </summary>
        private const int MaxQueuedSends = 512;

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
            if (_sendClosed)
                return;

            // The args and its buffer are taken from the pool *before* the send lock, never
            // while holding it. TeardownEventArgs takes the pool lock too, and it runs on the
            // completion path which then takes the send lock to start the next send - so a Send
            // that held the send lock while reaching for the pool would be waiting for a lock
            // held by a thread waiting for this one.
            var args = SetupEventArgs(SocketAsyncOperation.Send);

            if (args == null)
            {
                Drop("no buffers left to send with");
                return;
            }

            var start = false;
            var discard = false;
            var overflowed = false;

            // Writing and encrypting happen under the send lock, not just the handing-off. The
            // encryption is a stream cipher, so its state has to advance in the same order the
            // bytes reach the socket; building two packets at once on two threads would advance
            // it twice and send both with the wrong keystream.
            lock (_sendLock)
            {
                if (_sendClosed)
                {
                    discard = true;
                }
                else if (!TryWriteFrame(packet, args))
                {
                    // Logged by TryWriteFrame. The packet is skipped; nothing of it reached the
                    // socket, and every frame stands on its own, so the stream is intact.
                    discard = true;
                }
                else
                {
                    if (!_sending)
                    {
                        _sending = true;
                        start = true;
                    }
                    else if (_sendQueue.Count < MaxQueuedSends)
                    {
                        _sendQueue.Enqueue(args);
                    }
                    else
                    {
                        // Too far behind to catch up. Drop the packet and the connection with it -
                        // silently discarding one packet out of a stream the other side is framing
                        // would desync it just as surely as interleaving would.
                        _sendClosed = true;
                        overflowed = true;
                    }
                }
            }

            if (start)
            {
                SendAsync(args);
                return;
            }

            if (!discard && !overflowed)
                return;

            TeardownEventArgs(args);

            if (!overflowed)
                return;

            // Hand the queued buffers back here rather than leaving it to whoever handles the
            // drop. Nothing else will ever go out on this socket, and the pool entries the queue
            // is sitting on belong to every other connection.
            DiscardQueuedSends();

            Drop($"send queue full at {MaxQueuedSends} packets");
        }

        /// <summary>
        /// Serialises and encrypts the packet into the args' buffer and sets the frame up to
        /// send, or returns false having logged why it could not. A throw out of the packet's
        /// Write - a body larger than the pool block, a null string, a writer bug - or out of
        /// OnEncrypt used to leave Send by way of the exception, with the args and its buffer
        /// never returned to their pools; every such packet cost the whole process one of each
        /// for good, and once they ran out nothing could be sent or received.
        /// </summary>
        private bool TryWriteFrame(IBasePacket packet, SocketAsyncEventArgs args)
        {
            var data = args.GetUserToken<BufferData>();

            try
            {
                int length;

                // Keep space for the length header
                data.Offset = LengthSize;

                // Write the packet data to the buffer
                using (var sw = data.CreateWriter())
                {
                    packet.Write(sw);

                    length = (int) sw.BaseStream.Position;
                }

                OnEncrypt?.Invoke(data, ref length);

                // Reset the offset to send everything (including the size header)
                data.Offset = 0;
                data.Length = length + LengthSize;

                var sizeLen = CountSize ? length + LengthSize : length;

                // Copy the size header into the buffer
                for (var i = 0; i < LengthSize; ++i)
                    data[i] = (byte) ((sizeLen >> (i * 8)) & 0xFF);

                args.SetBuffer(data.BaseOffset, data.Length);

                return true;
            }
            catch (Exception e)
            {
                SafeLog($"Could not send a {packet.GetType().Name} to {SafeRemoteAddress()}, skipping it: {e}");

                return false;
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
