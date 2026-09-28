using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Cryptography;
    using Rasa.Memory;
    using Rasa.Networking;
    using Rasa.Test.Memory;

    [TestClass]
    [DoNotParallelize]
    public class GameInboundOwnershipTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());

            BufferManager.Initialize(8192, 8, 8);
            LengthedSocket.InitializeEventArgsPool(64);
        }

        [TestMethod]
        public async Task ReceiveRacingDisconnectReturnsItsRentedChunk()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            using (var rentStarted = new ManualResetEventSlim())
            using (var releaseRent = new ManualResetEventSlim())
            {
                var transport = new LengthedSocket(accepted, SizeType.Dword, false)
                {
                    AutoReceive = false
                };
                var cryptData = new ClientCryptData();
                GameCryptManager.Initialize(cryptData, new byte[64]);
                var client = new Rasa.Game.Client(null, new Rasa.Game.Handlers.ClientPacketHandler());
                client.RegisterAtServer(null, transport, cryptData);

                var blocked = 0;
                using var buffers = new ArrayPoolTracker(
                    16,
                    currentThreadOnly: false,
                    onRent: _ =>
                    {
                        if (Interlocked.CompareExchange(ref blocked, 1, 0) != 0)
                            return;

                        rentStarted.Set();
                        Assert.IsTrue(releaseRent.Wait(TimeSpan.FromSeconds(5)));
                    });

                await sender.SendAsync(CreateEncryptedFrame(cryptData));
                Assert.IsTrue(rentStarted.Wait(TimeSpan.FromSeconds(5)));

                client.Close(false);
                releaseRent.Set();

                Assert.IsTrue(SpinWait.SpinUntil(
                    () => buffers.AllReturned,
                    TimeSpan.FromSeconds(5)));
                buffers.AssertReturned();
                Assert.AreEqual("Disconnected", client.State.ToString());
            }
        }

        private static byte[] CreateEncryptedFrame(ClientCryptData cryptData)
        {
            var payload = new byte[8];
            payload[0] = 1;
            var length = payload.Length;
            GameCryptManager.Encrypt(payload, 0, ref length, payload.Length, cryptData);
            var frame = new byte[4 + length];
            BitConverter.GetBytes(length).CopyTo(frame, 0);
            payload.AsSpan(0, length).CopyTo(frame.AsSpan(4));
            return frame;
        }

        private static async Task<(Socket Sender, Socket Accepted)> ConnectAsync()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await sender.ConnectAsync(listener.LocalEndPoint);
            return (sender, await listener.AcceptAsync());
        }
    }
}
