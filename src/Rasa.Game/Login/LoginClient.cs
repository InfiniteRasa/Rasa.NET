using System.Net.Sockets;

namespace Rasa.Login
{
    using Cryptography;
    using Memory;
    using Networking;
    using Packets.Login.Client;
    using Packets.Login.Server;

    public class LoginClient
    {
        public LengthedSocket Socket { get; }
        public LoginManager Manager { get; }
        public ClientCryptData Data { get; } = new ClientCryptData();

        // ReSharper disable once InconsistentNaming
        public BigNum PrivateKey { get; } = new BigNum();
        public BigNum PublicKey { get; } = new BigNum();
        public BigNum K { get; } = new BigNum();

        public LoginClient(LoginManager manager, LengthedSocket socket)
        {
            Manager = manager;

            Socket = socket;
            Socket.AutoReceive = false;
            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;

            DHKeyExchange.GeneratePrivateAndPublicA(PrivateKey, PublicKey);

            Socket.Send(new ServerKeyPacket
            {
                PublicKey = PublicKey,
                Prime = DHKeyExchange.ConstantPrime,
                Generator = DHKeyExchange.ConstantGenerator
            });

            Socket.OnEncrypt += OnEncrypt;
            Socket.ReceiveAsync();
        }

        private void OnEncrypt(BufferData data, ref int length)
        {
            GameCryptManager.Encrypt(BufferData.Buffer, data.BaseOffset + data.Offset, ref length, data.RemainingLength, Data);
        }

        private void OnReceive(BufferData data)
        {
            var packet = new ClientKeyPacket();
            packet.Read(data.GetReader());

            DHKeyExchange.GenerateServerK(PrivateKey, packet.B, K);

            var key = new byte[64];
            K.WriteToBigEndian(key, 0, key.Length);

            GameCryptManager.Initialize(Data, key);

            Socket.Send(new ClientKeyOkPacket());

            Cleanup();

            Manager.ExchangeDone(this);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Manager.Disconnect(this);

            Close();
        }

        private void Cleanup()
        {
            Socket.AutoReceive = true;
            Socket.OnReceive = null;
            Socket.OnError = null;
            Socket.OnEncrypt = null;
        }

        public void Close()
        {
            Socket.Close();
        }
    }
}
