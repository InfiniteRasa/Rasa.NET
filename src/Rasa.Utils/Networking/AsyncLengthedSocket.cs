using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace Rasa.Networking;

using Rasa.Memory;

public sealed class AsyncLengthedSocket : IDisposable
{
    public enum HeaderSizeType
    {
        Byte = 1,
        Word = 2,
        Dword = 4,
    }

    public const int MaxDataSize = 0x400;

    private Socket Socket { get; }
    private NonContiguousMemoryStream? ReceiveStream { get; set; }
    private NonContiguousMemoryStream? SendStream { get; set; }
    private Task? ListenTask { get; set; }
    private Task? ReceiveTask { get; set; }
    private Task? SendTask { get; set; }
    private Task? ConnectTask { get; set; }
    private SemaphoreSlim? SendDelaySemaphore { get; set; }
    private bool Running { get; set; }
    private CancellationTokenSource CloseCancellationTokenSource { get; } = new();
    private HeaderSizeType HeaderSize { get; }
    private bool CountHeaderSize { get; }
    private int HeaderSizeInBytes => (int)HeaderSize;
    private int MaxPacketSize => MaxDataSize + (int)HeaderSize;

    public Action? OnError { get; set; }
    public Action? OnDisconnect { get; set; }
    public Action? OnConnect { get; set; }
    public Action<AsyncLengthedSocket>? OnAccept { get; set; }
    public Action<NonContiguousMemoryStream, int>? OnReceive { get; set; }

    public EndPoint? RemoteAddress => Socket?.RemoteEndPoint;
    public bool Connected => Socket?.Connected ?? false;

    public AsyncLengthedSocket(HeaderSizeType headerSize, bool countHeaderSize = true)
        : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), headerSize, countHeaderSize)
    {
    }

    public AsyncLengthedSocket(Socket socket, HeaderSizeType headerSize, bool countHeaderSize = true)
    {
        Socket = socket;
        HeaderSize = headerSize;
        CountHeaderSize = countHeaderSize;
    }

    public void StartListening(EndPoint endPoint, int backlog = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        try
        {
            Socket.Bind(endPoint);
            Socket.Listen(backlog);

            ListenTask = DoListen();
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Unable to start listening on socket {Socket.LocalEndPoint}! Exception: {ex}");
        }
    }

    private async Task DoListen()
    {
        Debug.Assert(OnAccept != null, "No callback is set to handle incoming socket connections!");

        try
        {
            while (true)
            {
                try
                {
                    var socket = await Socket.AcceptAsync(CloseCancellationTokenSource.Token);

                    OnAccept(new AsyncLengthedSocket(socket, HeaderSize));
                }
                catch (TaskCanceledException)
                {
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Error while listening on socket {Socket.LocalEndPoint}! Exception: {ex}");
        }
    }

    public void Start()
    {
        try
        {
            Running = true;
            ReceiveStream = new();
            SendStream = new();
            SendDelaySemaphore = new(1);

            ReceiveTask = DoReceive();
            SendTask = DoSend();
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Unable to start communicating on socket {Socket.RemoteEndPoint}! Exception: {ex}");
        }
    }

    public void ConnectAsync(EndPoint remote)
    {
        try
        {
            ConnectTask = DoConnect(remote);
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Unable to connect to socket at {remote}! Exception: {ex}");
        }
    }

    public void Send(byte[] data, int offset, int count)
    {
        Debug.Assert(SendStream != null, "The socket must be started before sending data to it!");
        Debug.Assert(SendDelaySemaphore != null, "The socket must be started before sending data to it!");

        var sizeToWrite = count;
        if (CountHeaderSize)
            sizeToWrite += HeaderSizeInBytes;

        var sizeHolder = ArrayPool<byte>.Shared.Rent(HeaderSizeInBytes);

        try
        {
            BitConverter.TryWriteBytes(sizeHolder, sizeToWrite);

            SendStream.Write(sizeHolder, 0, HeaderSizeInBytes);
            SendStream.Write(data, offset, count);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sizeHolder);
        }

        SendDelaySemaphore.Release();
    }

    private async Task DoConnect(EndPoint remote)
    {
        try
        {
            await Socket.ConnectAsync(remote, CloseCancellationTokenSource.Token);

            OnConnect?.Invoke();
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Error while connecting on address {remote}! Exception: {ex}");
        }
    }

    private async Task DoReceive()
    {
        Debug.Assert(ReceiveStream is not null, "The socket must be started before sending data to it!");

        var receiveBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);

        try
        {
            while (Running)
            {
                int received;

                try
                {
                    received = await Socket.ReceiveAsync(receiveBuffer, CloseCancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (received == 0)
                {
                    OnDisconnect?.Invoke();

                    Close();
                    return;
                }

                ReceiveStream.Write(receiveBuffer, 0, received);

                var dataSize = PeekIncomingPacketDataSize();
                if (dataSize == -1)
                    continue;

                if (ReceiveStream.AvailableBytesToRead >= dataSize + HeaderSizeInBytes)
                {
                    ReceiveStream.RemoveBytes(HeaderSizeInBytes);

                    OnReceive?.Invoke(ReceiveStream, dataSize);

                    ReceiveStream.RemoveBytes(dataSize);
                }

            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Error while receiving on socket {Socket.RemoteEndPoint}! Exception: {ex}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }
    }

    private int PeekIncomingPacketDataSize()
    {
        Debug.Assert(ReceiveStream is not null, "The socket must be started before sending data to it!");

        if (ReceiveStream.AvailableBytesToRead < HeaderSizeInBytes)
            return -1;

        var readPosition = ReceiveStream!.Position;
        var sizeHolder = ArrayPool<byte>.Shared.Rent(HeaderSizeInBytes);

        try
        {
            ReceiveStream.Read(sizeHolder, 0, HeaderSizeInBytes);

            var packetSize = HeaderSize switch
            {
                HeaderSizeType.Byte => sizeHolder[0],
                HeaderSizeType.Word => BitConverter.ToInt16(sizeHolder, 0),
                HeaderSizeType.Dword => BitConverter.ToInt32(sizeHolder, 0),
                _ => throw new Exception($"Unknown HeaderSizeType: {HeaderSize}!")
            };

            if (CountHeaderSize)
                packetSize -= HeaderSizeInBytes;

            return packetSize;
        }
        finally
        {
            ReceiveStream.Position = readPosition;

            ArrayPool<byte>.Shared.Return(sizeHolder);
        }
    }

    private async Task DoSend()
    {
        Debug.Assert(SendStream is not null, "The socket must be started before sending data to it!");
        Debug.Assert(SendDelaySemaphore is not null, "The socket must be started before sending data to it!");

        var sendBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);

        try
        {
            while (Running)
            {
                if (SendStream.AvailableBytesToRead > 0)
                {
                    var sizeToSend = Math.Min(MaxPacketSize, SendStream.AvailableBytesToRead);

                    SendStream.Read(sendBuffer, 0, sizeToSend);

                    int sentBytes;

                    try
                    {
                        sentBytes = await Socket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, sizeToSend), CloseCancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (sentBytes == 0)
                    {
                        OnError?.Invoke();

                        Close();
                        return;
                    }

                    SendStream.RemoveBytes(sentBytes);
                }
                else
                {
                    try
                    {
                        await SendDelaySemaphore.WaitAsync(CloseCancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke();

            Console.WriteLine($"Error while sending on socket {Socket.RemoteEndPoint}! Exception: {ex}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sendBuffer);
        }
    }

    public void Close()
    {
        Running = false;
        CloseCancellationTokenSource.Cancel();
        CloseCancellationTokenSource.Dispose();

        Socket.Close();
        ReceiveStream?.Dispose();
        SendStream?.Dispose();
    }

    public void Dispose() => Close();
}