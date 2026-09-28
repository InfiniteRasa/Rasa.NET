using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Config;
    using Rasa.Cryptography;
    using Rasa.Data;
    using Rasa.Login;
    using Rasa.Memory;
    using Rasa.Networking;
    using Rasa.Queue;

    [TestClass]
    [DoNotParallelize]
    public class HandshakeValidationTests
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
        [DataRow(0, false)]
        [DataRow(2, false)]
        [DataRow(4, false)]
        [DataRow(100, false)]
        [DataRow(0, true)]
        [DataRow(2, true)]
        [DataRow(4, true)]
        [DataRow(100, true)]
        public void AuthChecksumCoversTheFrameAtEveryBufferOffset(int offset, bool corrupt)
        {
            var data = new byte[offset + 64];
            for (var i = 0; i < offset; i++)
                data[i] = 0xA5;
            for (var i = 0; i < 13; i++)
                data[offset + i] = (byte)(i + 1);
            var length = 13;
            AuthCryptManager.Encrypt(data, offset, ref length, 64);
            if (corrupt)
                data[offset] ^= 128;

            Assert.AreEqual(!corrupt, AuthCryptManager.Decrypt(data, offset, length));
            for (var i = 0; i < offset; i++)
                Assert.AreEqual((byte)0xA5, data[i]);
        }

        [TestMethod]
        public void TruncatedAuthCredentialsFailBeforeBlockDecryption()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[8]));

            Assert.ThrowsExactly<EndOfStreamException>(() =>
                new Rasa.Packets.Auth.Client.LoginPacket().Read(reader));
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(65)]
        public void GameKeyLengthUsesTheExisting64ByteBound(int length)
        {
            using var reader = new BinaryReader(new MemoryStream(BitConverter.GetBytes(length)));

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new Rasa.Packets.Login.Client.ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void TruncatedGameKeyFailsBeforeBigNumberParsing()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[] { 4, 0, 0, 0, 1 }));

            Assert.ThrowsExactly<EndOfStreamException>(() =>
                new Rasa.Packets.Login.Client.ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void TruncatedQueueKeyIsNotAcceptedAsAnEmptyString()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[] { 10, 0, 0, 0 }));

            Assert.ThrowsExactly<EndOfStreamException>(() =>
                new Rasa.Packets.Queue.Client.ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void AuthLoginRejectsTrailingBytes()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[37]));

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new Rasa.Packets.Auth.Client.LoginPacket().Read(reader));
        }

        [TestMethod]
        public void GameKeyRejectsTrailingBytes()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[] { 1, 0, 0, 0, 1, 0xAA }));

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new Rasa.Packets.Login.Client.ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void QueueKeyRejectsTrailingBytes()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[] { 1, 0, 0, 0, (byte)'x', 0xAA }));

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new Rasa.Packets.Queue.Client.ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void QueueLoginRejectsTrailingBytes()
        {
            using var reader = new BinaryReader(new MemoryStream(new byte[9]));

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new Rasa.Packets.Queue.Client.QueueLoginPacket().Read(reader));
        }

        [TestMethod]
        public async Task ClosedLoginCannotCompleteItsExchange()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            {
                var manager = new LoginManager();
                var completed = 0;
                manager.OnLogin = _ => completed++;
                manager.LoginSocket(new LengthedSocket(accepted, SizeType.Dword, false));
                var client = manager.Clients.Single();

                client.Close();
                manager.ExchangeDone(client);

                Assert.AreEqual(0, completed);
                Assert.AreEqual(0, manager.Clients.Count);
            }
        }

        [TestMethod]
        public async Task PreloadedLoginSuccessCompletesDuringStartOutsideTheClientListLock()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            {
                var handshake = CreateLoginHandshake(1, new byte[] { 1 });
                await sender.SendAsync(handshake);
                Assert.IsTrue(SpinWait.SpinUntil(
                    () => accepted.Available == handshake.Length,
                    TimeSpan.FromSeconds(5)));

                var manager = new LoginManager();
                LoginClient completedClient = null;
                var callbacks = 0;
                var callbackUnderLock = true;
                var loginSocketReturned = false;
                var callbackBeforeReturn = false;
                manager.OnLogin = client =>
                {
                    callbacks++;
                    completedClient = client;
                    callbackUnderLock = Monitor.IsEntered(manager.Clients);
                    callbackBeforeReturn = !loginSocketReturned;
                };

                manager.LoginSocket(new LengthedSocket(accepted, SizeType.Dword, false));
                loginSocketReturned = true;

                Assert.AreEqual(1, callbacks);
                Assert.IsTrue(callbackBeforeReturn);
                Assert.IsFalse(callbackUnderLock);
                Assert.AreEqual(0, manager.Clients.Count);

                completedClient.Socket.Close();
            }
        }

        [TestMethod]
        public async Task PreloadedMalformedLoginFailsDuringStartOutsideTheClientListLock()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            {
                var handshake = CreateLoginHandshake(0, Array.Empty<byte>());
                await sender.SendAsync(handshake);
                Assert.IsTrue(SpinWait.SpinUntil(
                    () => accepted.Available == handshake.Length,
                    TimeSpan.FromSeconds(5)));

                var manager = new LoginManager();
                LoginClient failedClient = null;
                var callbacks = 0;
                var callbackUnderLock = true;
                var loginSocketReturned = false;
                var callbackBeforeReturn = false;
                manager.OnFail = client =>
                {
                    callbacks++;
                    failedClient = client;
                    callbackUnderLock = Monitor.IsEntered(manager.Clients);
                    callbackBeforeReturn = !loginSocketReturned;
                };

                manager.LoginSocket(new LengthedSocket(accepted, SizeType.Dword, false));
                loginSocketReturned = true;

                Assert.AreEqual(1, callbacks);
                Assert.IsTrue(callbackBeforeReturn);
                Assert.IsFalse(callbackUnderLock);
                Assert.AreEqual(0, manager.Clients.Count);
                Assert.IsTrue(failedClient.Socket.Socket.SafeHandle.IsClosed);

                failedClient.Close();
                Assert.AreEqual(1, callbacks);
            }
        }

        [TestMethod]
        public async Task QueueDisconnectWinningAdmissionRaceDoesNotEnqueue()
        {
            var (sender, accepted) = await ConnectAsync();
            using (sender)
            using (accepted)
            using (var admissionStarted = new ManualResetEventSlim())
            using (var releaseAdmission = new ManualResetEventSlim())
            {
                var config = new QueueConfig
                {
                    PublicKey = "queue-test-key",
                    Prime = "queue-test-prime",
                    Generator = "queue-test-generator"
                };
                var manager = new QueueManager(
                    config,
                    () => true,
                    IPAddress.Loopback,
                    8001,
                    () =>
                    {
                        admissionStarted.Set();
                        Assert.IsTrue(releaseAdmission.Wait(TimeSpan.FromSeconds(5)));
                    });
                var client = new QueueClient(manager, new LengthedSocket(accepted, SizeType.Dword, false));
                lock (manager.Clients)
                    manager.Clients.Add(client);

                using (var keyPayload = new MemoryStream())
                using (var keyWriter = new BinaryWriter(keyPayload))
                {
                    new Rasa.Packets.Queue.Client.ClientKeyPacket
                    {
                        PublicKey = config.PublicKey
                    }.Write(keyWriter);
                    keyPayload.Position = 0;
                    using var keyReader = new BinaryReader(keyPayload);
                    client.HandleReceive(keyReader);
                }

                using var loginPayload = new MemoryStream();
                using (var loginWriter = new BinaryWriter(loginPayload, System.Text.Encoding.UTF8, true))
                {
                    new Rasa.Packets.Queue.Client.QueueLoginPacket
                    {
                        UserId = 123,
                        OneTimeKey = 456
                    }.Write(loginWriter);
                }
                loginPayload.Position = 0;

                var admission = Task.Run(() =>
                {
                    using var loginReader = new BinaryReader(loginPayload);
                    client.HandleReceive(loginReader);
                });
                Assert.IsTrue(admissionStarted.Wait(TimeSpan.FromSeconds(5)));

                client.Close();
                var stateAfterClose = client.State;
                var clientsAfterClose = manager.Clients.Count;
                releaseAdmission.Set();
                await admission.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.AreEqual(QueueState.Disconnected, stateAfterClose);
                Assert.AreEqual(0, clientsAfterClose);
                Assert.AreEqual(0, manager.QueuedClients);
            }
        }

        [TestMethod]
        public async Task ConcurrentAcceptedClientsReserveOnlyOneAvailableSlot()
        {
            var firstConnection = await ConnectAsync();
            var secondConnection = await ConnectAsync();
            using (firstConnection.Sender)
            using (firstConnection.Accepted)
            using (secondConnection.Sender)
            using (secondConnection.Accepted)
            using (var admissions = new CountdownEvent(2))
            {
                var config = CreateQueueConfig();
                QueueManager manager = null;
                manager = new QueueManager(
                    config,
                    () => manager.RedirectingClients >= 1,
                    IPAddress.Loopback,
                    8001,
                    () =>
                    {
                        admissions.Signal();
                        Assert.IsTrue(admissions.Wait(TimeSpan.FromSeconds(5)));
                    });

                await firstConnection.Sender.SendAsync(CreateQueueHandshake(config, 101, 1001));
                await secondConnection.Sender.SendAsync(CreateQueueHandshake(config, 102, 1002));

                var firstAccept = Task.Run(() => manager.AcceptClient(
                    new LengthedSocket(firstConnection.Accepted, SizeType.Dword, false)));
                var secondAccept = Task.Run(() => manager.AcceptClient(
                    new LengthedSocket(secondConnection.Accepted, SizeType.Dword, false)));

                await Task.WhenAll(firstAccept, secondAccept).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsTrue(SpinWait.SpinUntil(
                    () => manager.RedirectingClients + manager.QueuedClients == 2,
                    TimeSpan.FromSeconds(5)));

                Assert.AreEqual(1, manager.RedirectingClients);
                Assert.AreEqual(1, manager.QueuedClients);

                QueueClient[] clients;
                lock (manager.Clients)
                    clients = manager.Clients.ToArray();
                foreach (var client in clients)
                    client.Close();
            }
        }

        [TestMethod]
        public async Task FullQueueUpdateRemovesDisconnectedClientBehindLiveClient()
        {
            var firstConnection = await ConnectAsync();
            var secondConnection = await ConnectAsync();
            using (firstConnection.Sender)
            using (firstConnection.Accepted)
            using (secondConnection.Sender)
            using (secondConnection.Accepted)
            {
                var config = CreateQueueConfig();
                var manager = new QueueManager(
                    config,
                    () => true,
                    IPAddress.Loopback,
                    8001,
                    null);
                var first = new QueueClient(
                    manager,
                    new LengthedSocket(firstConnection.Accepted, SizeType.Dword, false));
                var second = new QueueClient(
                    manager,
                    new LengthedSocket(secondConnection.Accepted, SizeType.Dword, false));
                lock (manager.Clients)
                {
                    manager.Clients.Add(first);
                    manager.Clients.Add(second);
                }

                AuthenticateAndLogin(first, config, 201, 2001);
                AuthenticateAndLogin(second, config, 202, 2002);
                second.Close();

                manager.Update(0);

                Assert.AreEqual(1, manager.QueuedClients);
                Assert.AreEqual(QueueState.InQueue, first.State);
                first.Close();
            }
        }

        private static QueueConfig CreateQueueConfig()
        {
            return new QueueConfig
            {
                PublicKey = "queue-test-key",
                Prime = "queue-test-prime",
                Generator = "queue-test-generator"
            };
        }

        private static byte[] CreateQueueHandshake(QueueConfig config, uint userId, uint oneTimeKey)
        {
            using var output = new MemoryStream();
            WriteFrame(output, writer => new Rasa.Packets.Queue.Client.ClientKeyPacket
            {
                PublicKey = config.PublicKey
            }.Write(writer));
            WriteFrame(output, writer => new Rasa.Packets.Queue.Client.QueueLoginPacket
            {
                UserId = userId,
                OneTimeKey = oneTimeKey
            }.Write(writer));
            return output.ToArray();
        }

        private static byte[] CreateLoginHandshake(int keyLength, byte[] key)
        {
            using var output = new MemoryStream();
            WriteFrame(output, writer =>
            {
                writer.Write(keyLength);
                writer.Write(key);
            });
            return output.ToArray();
        }

        private static void WriteFrame(Stream output, Action<BinaryWriter> writePayload)
        {
            using var payload = new MemoryStream();
            using (var writer = new BinaryWriter(payload, System.Text.Encoding.UTF8, true))
                writePayload(writer);

            using var outputWriter = new BinaryWriter(output, System.Text.Encoding.UTF8, true);
            outputWriter.Write((int)payload.Length);
            outputWriter.Write(payload.GetBuffer(), 0, (int)payload.Length);
        }

        private static void AuthenticateAndLogin(
            QueueClient client,
            QueueConfig config,
            uint userId,
            uint oneTimeKey)
        {
            using (var keyPayload = new MemoryStream())
            using (var keyWriter = new BinaryWriter(keyPayload))
            {
                new Rasa.Packets.Queue.Client.ClientKeyPacket
                {
                    PublicKey = config.PublicKey
                }.Write(keyWriter);
                keyPayload.Position = 0;
                using var keyReader = new BinaryReader(keyPayload);
                client.HandleReceive(keyReader);
            }

            using var loginPayload = new MemoryStream();
            using (var loginWriter = new BinaryWriter(loginPayload, System.Text.Encoding.UTF8, true))
            {
                new Rasa.Packets.Queue.Client.QueueLoginPacket
                {
                    UserId = userId,
                    OneTimeKey = oneTimeKey
                }.Write(loginWriter);
            }
            loginPayload.Position = 0;
            using var loginReader = new BinaryReader(loginPayload);
            client.HandleReceive(loginReader);
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
