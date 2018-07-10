using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

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
        public delegate void EncryptDelegate(BufferData data, ref int length);
        public delegate bool DecryptDelegate(BufferData data);

        public SizeType SizeHeaderLength { get; }
        public bool CountSize { get; }
        public int LengthSize => (int) SizeHeaderLength;
        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        public IPAddress RemoteAddress => ((IPEndPoint)Socket.RemoteEndPoint).Address;

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

        private SocketAsyncEventArgs SetupEventArgs(SocketAsyncOperation operation)
        {
            SocketAsyncEventArgs args;

            lock (_socketAsyncEventArgsPool)
                args = _socketAsyncEventArgsPool.Count > 0 ? _socketAsyncEventArgsPool.Pop() : null;

            if (args == null)
                throw new OutOfMemoryException("All of the SocketAsyncEventArgs are being used!");

            switch (operation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.Send:
                    var data = BufferManager.RequestBuffer();

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
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.Send:
                    BufferManager.FreeBuffer(args.GetUserToken<BufferData>());

                    args.SetBuffer(null, 0, 0);
                    args.UserToken = null;
                    break;

                case SocketAsyncOperation.Connect:
                    args.RemoteEndPoint = null;
                    break;
            }

            args.AcceptSocket = null;
            args.Completed -= OperationCompleted;

            lock (_socketAsyncEventArgsPool)
                _socketAsyncEventArgsPool.Push(args);
        }

        private void OperationCompleted(object o, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success || (args.LastOperation == SocketAsyncOperation.Receive && args.BytesTransferred == 0))
                OnError?.Invoke(args);
            else
            {
                var data = args.GetUserToken<BufferData>();

                switch (args.LastOperation)
                {
                    case SocketAsyncOperation.Send:
                        data.ByteCount += args.BytesTransferred;

                        // We've transferred less bytes than we should have
                        if (data.Length > data.ByteCount)
                        {
                            args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                            // TODO: test if multiple send operations will collide (if the packet was only sent partially, and another packet is getting sent between the two parts)
                            // TODO: create a queue for sending async, if it collides?
                            SendAsync(args);
                            return;
                        }

                        OnSend?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Receive:
                        if (ProcessInputBuffer(data, args))
                            return;

                        if (AutoReceive)
                            ReceiveAsync();

                        break;

                    case SocketAsyncOperation.Connect:
                        OnConnect?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Accept:
                        OnAccept?.Invoke(new LengthedSocket(args.AcceptSocket, SizeHeaderLength, CountSize));
                        break;
                }
            }

            TeardownEventArgs(args);
        }

        private int ReadSize(BufferData data)
        {
            var headerSize = !CountSize ? LengthSize : 0;

            switch (SizeHeaderLength)
            {
                case SizeType.Char:
                    return headerSize + data[0];

                case SizeType.Word:
                    return headerSize + BitConverter.ToInt16(data.Buffer, data.BaseOffset);

                case SizeType.Dword:
                    return headerSize + BitConverter.ToInt32(data.Buffer, data.BaseOffset);

                default:
                    throw new NotImplementedException($"Only 1, 2 and 4 byte headers are supported! {SizeHeaderLength} is not!");
            }
        }

        private bool ProcessInputBuffer(BufferData data, SocketAsyncEventArgs args)
        {
            data.ByteCount += args.BytesTransferred;

            while (true)
            {
                var length = -1;

                if (data.ByteCount >= LengthSize)
                    length = ReadSize(data);

                if (length != -1)
                {
                    data.Length = length;

                    if (data.Length > data.MaxLength)
                        throw new OutOfMemoryException($"Packet is bigger than the max packet size! Packet size: {length} | Max buffer size: {data.MaxLength}");
                }

                if (length == -1 || data.ByteCount < length)
                {
                    args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                    ReceiveAsync(args);
                    return true;
                }

                data.Offset = LengthSize;
                data.Length = length;

                OnDecrypt?.Invoke(data);
                OnReceive?.Invoke(data);

                if (data.ByteCount == length)
                    break;

                data.ByteCount -= length;
                data.BaseOffset += length;
                data.Offset = 0;
                data.Length = data.MaxLength;
            }

            return false;
        }

        private void CopyToOtherBuffer(BufferData source, BufferData desti)
        {
            
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

        public void AcceptAsync()
        {
            var args = SetupEventArgs(SocketAsyncOperation.Accept);

            if (!Socket.AcceptAsync(args))
                OperationCompleted(Socket, args);
        }

        public void ConnectAsync(EndPoint remote)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Connect);

            args.RemoteEndPoint = remote;

            if (!Socket.ConnectAsync(args))
                OperationCompleted(Socket, args);
        }

        public void ReceiveAsync()
        {
            ReceiveAsync(SetupEventArgs(SocketAsyncOperation.Receive));
        }

        private void ReceiveAsync(SocketAsyncEventArgs args)
        {
            if (!Socket.ReceiveAsync(args))
                OperationCompleted(Socket, args);
        }

        public void Send(IBasePacket packet)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Send);
            var data = args.GetUserToken<BufferData>();

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

            SendAsync(args);
        }

        public void Send(MovementPacket packet)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Send);
            var data = args.GetUserToken<BufferData>();

            int length;

            // Keep space for the length header
            data.Offset = LengthSize;

            // Write the packet data to the buffer
            using (var sw = data.CreateWriter())
            {
                packet.WriteMovement(sw);

                length = (int)sw.BaseStream.Position;
            }

            OnEncrypt?.Invoke(data, ref length);

            // Reset the offset to send everything (including the size header)
            data.Offset = 0;
            data.Length = length + LengthSize;

            var sizeLen = CountSize ? data.Length : length;

            // Copy the size header into the buffer
            for (var i = 0; i < LengthSize; ++i)
                args.Buffer[data.BaseOffset + i] = (byte)((sizeLen >> (i * 8)) & 0xFF);

            args.SetBuffer(data.BaseOffset, data.Length);

            SendAsync(args);
        }

        private void SendAsync(SocketAsyncEventArgs args)
        {
            if (!Socket.SendAsync(args))
                OperationCompleted(Socket, args);
        }

        public void Close()
        {
            try
            {
                OnDisconnect?.Invoke();

                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
