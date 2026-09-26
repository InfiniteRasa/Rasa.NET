using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Memory;
    using Rasa.Networking;

    [TestClass]
    [DoNotParallelize]
    public class LengthedSocketTests
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
        public async Task EveryFragmentBoundaryDeliversOneCompleteFrame()
        {
            var frame = new byte[] { 3, 0, 0, 0, 10, 20, 30 };

            for (var split = 1; split < frame.Length; split++)
            {
                var (sender, accepted) = await ConnectAsync();
                using (sender)
                using (accepted)
                {
                    var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
                    var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                    transport.OnReceive = data =>
                    {
                        var payload = new byte[data.RemainingLength];
                        Buffer.BlockCopy(data.Buffer, data.BaseOffset + data.Offset, payload, 0, payload.Length);
                        received.TrySetResult(payload);
                    };
                    transport.OnError = args => received.TrySetException(
                        new SocketException((int)args.SocketError));
                    transport.ReceiveAsync();

                    await sender.SendAsync(frame.AsMemory(0, split), SocketFlags.None);
                    await Task.Delay(10);
                    Assert.IsFalse(received.Task.IsCompleted, $"Split {split} delivered an incomplete frame.");
                    await sender.SendAsync(frame.AsMemory(split), SocketFlags.None);

                    CollectionAssert.AreEqual(new byte[] { 10, 20, 30 },
                        await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
                    transport.Close();
                }
            }
        }

        [TestMethod]
        public async Task MultipleFramesInOneReceiveAreDeliveredInOrder()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            using (accepted)
            {
                var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
                var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                var payload = new byte[2];
                var count = 0;
                transport.OnReceive = data =>
                {
                    payload[count++] = data[data.Offset];
                    if (count == payload.Length)
                        received.TrySetResult(payload);
                };
                transport.OnError = args => received.TrySetException(
                    new SocketException((int)args.SocketError));
                transport.ReceiveAsync();

                await sender.SendAsync(new byte[] { 1, 0, 0, 0, 10, 1, 0, 0, 0, 20 });

                CollectionAssert.AreEqual(new byte[] { 10, 20 },
                    await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
                transport.Close();
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(8193)]
        [DataRow(int.MaxValue)]
        [DataRow(-1)]
        public async Task InvalidTransportLengthDropsOnlyOnce(int length)
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            using (accepted)
            {
                var transport = new LengthedSocket(accepted, SizeType.Dword, true) { AutoReceive = false };
                var dropped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var dropCount = 0;
                transport.OnDrop = _ =>
                {
                    Interlocked.Increment(ref dropCount);
                    dropped.TrySetResult(true);
                };
                transport.OnReceive = _ => dropped.TrySetException(
                    new AssertFailedException("Invalid frame was delivered."));
                transport.ReceiveAsync();

                await sender.SendAsync(BitConverter.GetBytes(length));

                Assert.IsTrue(await dropped.Task.WaitAsync(TimeSpan.FromSeconds(5)));
                await Task.Delay(10);
                Assert.AreEqual(1, dropCount);
                transport.Close();
            }
        }

        [TestMethod]
        public async Task FailedDecryptionDoesNotDeliverPayload()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await sender.ConnectAsync(listener.LocalEndPoint);
            using var accepted = await listener.AcceptAsync();
            var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
            var outcome = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.OnDecrypt = _ => false;
            transport.OnReceive = _ => outcome.TrySetResult(false);
            transport.OnDrop = _ => outcome.TrySetResult(true);
            transport.ReceiveAsync();
            using var output = new NetworkStream(sender, false);
            await output.WriteAsync(new byte[] { 4, 0, 0, 0, 1, 2, 3, 4 });

            Assert.IsTrue(await outcome.Task.WaitAsync(TimeSpan.FromSeconds(5)),
                "Rejected ciphertext must raise a transport error, not invoke the payload handler.");
        }

        [TestMethod]
        [DataRow((byte)0, false)]
        [DataRow((byte)1, true)]
        [DataRow((byte)8, true)]
        [DataRow((byte)9, false)]
        [DataRow((byte)15, false)]
        [DataRow((byte)17, true)]
        public void GameCipherPaddingIsValidated(byte padding, bool accepted)
        {
            var key = new Rasa.Cryptography.ClientCryptData();
            Rasa.Cryptography.GameCryptManager.Initialize(key, new byte[64]);
            var data = BufferManager.RequestBuffer();
            try
            {
                data.Offset = 4;
                data.Length = 28;
                data[4] = padding;
                var length = data.RemainingLength;
                Rasa.Cryptography.GameCryptManager.Encrypt(data.Buffer, data.BaseOffset + data.Offset,
                    ref length, length, key);

                Assert.AreEqual(accepted, Rasa.Game.Client.DecryptFrame(data, key));
                Assert.AreEqual(accepted ? 4 + (padding & 15) : 4, data.Offset);
            }
            finally
            {
                BufferManager.FreeBuffer(data);
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(7)]
        [DataRow(9)]
        public void InvalidGameCipherLengthIsRejectedBeforeDecryption(int length)
        {
            var data = BufferManager.RequestBuffer();
            try
            {
                data.Length = length;

                Assert.IsFalse(Rasa.Game.Client.DecryptFrame(data, new Rasa.Cryptography.ClientCryptData()));
            }
            finally
            {
                BufferManager.FreeBuffer(data);
            }
        }

        [TestMethod]
        public void CloseIsIdempotent()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var transport = new LengthedSocket(socket, SizeType.Dword, false);
            var disconnects = 0;
            transport.OnDisconnect = () => Interlocked.Increment(ref disconnects);

            transport.Close();
            transport.Close();

            Assert.AreEqual(1, disconnects);
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
