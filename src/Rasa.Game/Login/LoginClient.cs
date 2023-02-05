using System.Net.Sockets;

namespace Rasa.Login;

using Rasa.Cryptography;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets;
using Rasa.Packets.Login.Client;
using Rasa.Packets.Login.Server;
using System;
using System.Buffers;
using System.IO;
using System.Text;

public class LoginClient
{
    public const int SendBufferSize = 512;
    public const int SendBufferCryptoPadding = 8;
    public const int SendBufferChecksumPadding = 8;

    public AsyncLengthedSocket Socket { get; }
    public LoginManager Manager { get; }
    public ClientCryptData Data { get; } = new ClientCryptData();

    public BigNum PrivateKey { get; } = new BigNum();
    public BigNum PublicKey { get; } = new BigNum();
    public BigNum K { get; } = new BigNum();

    public LoginClient(LoginManager manager, AsyncLengthedSocket socket)
    {
        Manager = manager;

        Socket = socket;
        Socket.OnReceive += OnReceive;
        Socket.OnError += OnError;

        DHKeyExchange.GeneratePrivateAndPublicA(PrivateKey, PublicKey);

        SendPacket(new ServerKeyPacket
        {
            PublicKey = PublicKey,
            Prime = DHKeyExchange.ConstantPrime,
            Generator = DHKeyExchange.ConstantGenerator
        });

        Socket.Start();
    }

    public void SendPacket(IBasePacket packet)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(SendBufferSize + SendBufferCryptoPadding + SendBufferChecksumPadding);
        var writer = new BinaryWriter(new MemoryStream(buffer, true));

        packet.Write(writer);

        var length = (int)writer.BaseStream.Position;

        if (packet is not ServerKeyPacket)
        {
            GameCryptManager.Encrypt(buffer, 0, ref length, buffer.Length, Data);
        }

        Socket.Send(buffer, 0, length);

        ArrayPool<byte>.Shared.Return(buffer);
    }

    private void OnReceive(NonContiguousMemoryStream incomingStream, int length)
    {
        var startPosition = incomingStream.Position;

        using var br = new BinaryReader(incomingStream, Encoding.UTF8, true);

        var packet = new ClientKeyPacket();
        packet.Read(br);

        if (startPosition + length != incomingStream.Position)
            throw new Exception($"Over or under read of the incoming packet! Start position: {startPosition} | Length: {length} | Ending position: {incomingStream.Position}");

        DHKeyExchange.GenerateServerK(PrivateKey, packet.B, K);

        var key = new byte[64];
        K.WriteToBigEndian(key, 0, key.Length);

        GameCryptManager.Initialize(Data, key);

        SendPacket(new ClientKeyOkPacket());

        Cleanup();

        Manager.ExchangeDone(this);
    }

    private void OnError()
    {
        Manager.Disconnect(this);

        Close();
    }

    private void Cleanup()
    {
        Socket.OnReceive = null;
        Socket.OnError = null;
    }

    public void Close() => Socket.Close();
}
