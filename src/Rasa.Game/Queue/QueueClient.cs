using System;
using System.Net;

namespace Rasa.Queue;

using Rasa.Data;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets;
using Rasa.Packets.Queue.Client;
using Rasa.Packets.Queue.Server;
using System.Buffers;
using System.IO;
using System.Text;

public class QueueClient
{
    public const int SendBufferSize = 512;

    public QueueManager Manager { get; }
    public AsyncLengthedSocket Socket { get; }
    public QueueState State { get; private set; }
    public uint UserId { get; set; }
    public uint OneTimeKey { get; set; }
    public DateTime EnqueueTime { get; private set; }
    public DateTime DequeueTime { get; set; }

    public QueueClient(QueueManager manager, AsyncLengthedSocket socket)
    {
        Manager = manager;
        Socket = socket;
        Socket.OnReceive += OnReceive;
        Socket.OnError += OnError;

        Socket.Start();

        SendPacket(new ServerKeyPacket
        {
            PublicKey = Manager.Config.PublicKey,
            Prime = Manager.Config.Prime,
            Generator = Manager.Config.Generator
        });

        State = QueueState.Authenticating;
    }

    private void SendPacket(IOpcodedPacket<QueueOpcode> packet)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(SendBufferSize);
        var writer = new BinaryWriter(new MemoryStream(buffer, true));

        packet.Write(writer);

        Socket.Send(buffer, 0, (int)writer.BaseStream.Position);

        ArrayPool<byte>.Shared.Return(buffer);
    }

    private void OnReceive(NonContiguousMemoryStream incomingStream, int length)
    {
        var startPosition = incomingStream.Position;

        using var br = new BinaryReader(incomingStream, Encoding.UTF8, true);

        switch (State)
        {
            case QueueState.Authenticating:
                var keyPacket = new ClientKeyPacket();

                keyPacket.Read(br);

                if (keyPacket.PublicKey != Manager.Config.PublicKey)
                {
                    Close();
                    return;
                }

                SendPacket(new ClientKeyOkPacket());

                State = QueueState.Authenticated;

                break;

            case QueueState.Authenticated:
                if (br.ReadByte() != 7)
                    throw new Exception("Invalid opcode???");

                var loginPacket = new QueueLoginPacket();

                loginPacket.Read(br);

                UserId = loginPacket.UserId;
                OneTimeKey = loginPacket.OneTimeKey;
                State = QueueState.InQueue;

                Manager.Enqueue(this);
                EnqueueTime = DateTime.Now;
                break;

            default:
                throw new Exception("Received packet in a invalid queue state!");
        }
    }

    private void OnError() => Close();

    public void Close()
    {
        Socket.Close();

        State = QueueState.Disconnected;

        Manager.Disconnect(this);
    }

    public void Redirect(IPAddress ip, int port)
    {
        State = QueueState.Redirecting;

        SendPacket(new HandoffToGamePacket
        {
            OneTimeKey = OneTimeKey,
            ServerIp = ip,
            ServerPort = port,
            UserId = UserId
        });
    }

    public void SendPositionUpdate(int position, int estimatedTime)
    {
        SendPacket(new QueuePositionPacket
        {
            Position = position,
            EstimatedTime = estimatedTime
        });
    }
}
