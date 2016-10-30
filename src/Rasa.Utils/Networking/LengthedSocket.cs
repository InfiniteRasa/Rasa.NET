using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Networking
{
    using Cryptography;
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

        public SizeType SizeHeaderLength { get; }
        public int LengthSize => (int) SizeHeaderLength;
        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        public IPAddress RemoteAddress => ((IPEndPoint)Socket.RemoteEndPoint).Address;

        public AsyncHandler OnConnect;
        public DisconnectHandler OnDisconnect;
        public AcceptHandler OnAccept;
        public AsyncHandler OnSend;
        public ReceiveHandler OnReceive;
        public AsyncHandler OnError;

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

                    args.SetBuffer(BufferManager.Buffer, data.BaseOffset, data.Length);
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
                    var data = args.GetUserToken<BufferData>();
                    if (data != null)
                        BufferManager.FreeBuffer(data);

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
                        if (data.Missing > 0)
                        {
                            // TODO: Needs testing!
                            args.SetBuffer(data.BaseOffset + data.ByteCount, data.Missing);

                            // TODO: test if multiple send operations will collide (if the packet was only sent partially, and another packet is getting sent between the two parts)
                            // TODO: create a queue for sending async, if it collides?
                            SendAsync(args);
                            return;
                        }

                        OnSend?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Receive:
                        data.ByteCount += args.BytesTransferred;
                        data.Offset = 0;

                        while (data.Remaining > 0)
                        {
                            var length = -1;

                            if (data.Remaining >= LengthSize)
                                length = ReadSize(data);

                            if (length > data.MaxLength)
                                throw new OutOfMemoryException($"Packet is bigger than the max packet size! Packet size: {length} | Max size: {data.MaxLength}");

                            if (length != -1)
                                data.Length = length;

                            if (length == -1 || data.Remaining < length)
                            {
                                // First packet, no need to move
                                if (data.Offset == 0)
                                    args.SetBuffer(data.ByteOffset, data.Missing);
                                else
                                {
                                    var args2 = SetupEventArgs(SocketAsyncOperation.Receive);
                                    var newData = args2.GetUserToken<BufferData>();

                                    newData.ByteCount = data.Remaining;

                                    if (length != -1)
                                        newData.Length = length;

                                    args2.SetBuffer(newData.ByteOffset, newData.Missing);

                                    data.MoveTo(newData, data.Offset, newData.ByteCount);

                                    TeardownEventArgs(args);

                                    args = args2;
                                }

                                ReceiveAsync(args);
                                return;
                            }

                            var currentOff = data.Offset;

                            data.Offset += LengthSize;
                            data.Length = length;
                            //data.Length = length - LengthSize;

                            OnReceive?.Invoke(data);

                            data.Offset = currentOff + length;
                        }

                        ReceiveAsync();
                        break;

                    case SocketAsyncOperation.Connect:
                        OnConnect?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Accept:
                        OnAccept?.Invoke(new LengthedSocket(args.AcceptSocket, SizeHeaderLength));
                        break;
                }
            }

            TeardownEventArgs(args);
        }

        private int ReadSize(BufferData data)
        {
            switch (SizeHeaderLength)
            {
                case SizeType.Char:
                    return BufferManager.Buffer[data.BaseOffset + data.Offset];

                case SizeType.Word:
                    return BitConverter.ToUInt16(BufferManager.Buffer, data.BaseOffset + data.Offset);

                case SizeType.Dword:
                    return BitConverter.ToInt32(BufferManager.Buffer, data.BaseOffset + data.Offset);

                default:
                    throw new NotImplementedException($"Only 1, 2 and 4 byte headers are supported! {SizeHeaderLength} is not!");
            }
        }
        #endregion

        public LengthedSocket(SizeType sizeHeaderLen)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), sizeHeaderLen)
        {
        }

        public LengthedSocket(Socket s, SizeType sizeHeaderLen)
        {
            Socket = s;
            SizeHeaderLength = sizeHeaderLen;
        }

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

        public void Send(IBasePacket packet, ICryptoManager cryptManager)
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

            cryptManager?.Encrypt(args.Buffer, data.BaseOffset + data.Offset, ref length, data.Length - data.Offset);

            length += LengthSize;

            if (length > data.MaxLength)
                throw new OutOfMemoryException("Tried to send a bigger packet than the max packet length!");

            // Reset the offset to send everything (including the size header)
            data.Offset = 0;
            data.Length = length;

            // Copy the size header into the buffer
            var size = new List<byte>();

            switch (SizeHeaderLength)
            {
                case SizeType.Char:
                    size.Add((byte) length);
                    break;

                case SizeType.Word:
                    size.AddRange(BitConverter.GetBytes((ushort) length));
                    break;

                case SizeType.Dword:
                    size.AddRange(BitConverter.GetBytes((uint) length));
                    break;

                default:
                    throw new NotImplementedException($"Only 1, 2 and 4 byte headers are supported! {SizeHeaderLength} is not!");
            }

            Array.Copy(size.ToArray(), 0, args.Buffer, data.BaseOffset, LengthSize);

            args.SetBuffer(data.BaseOffset, length);

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
