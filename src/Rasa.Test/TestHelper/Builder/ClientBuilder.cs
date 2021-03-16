using System;
using System.Linq;
using System.Net.Sockets;
using JetBrains.Annotations;
using Moq;

namespace Rasa.Test.TestHelper.Builder
{
    using System.Net;
    using Cryptography;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Protocol;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Repositories.Char;
    using Repositories.Char.Character;
    using Repositories.Char.GameAccount;
    using Repositories.UnitOfWork;
    using Structures;

    public class ClientBuilder
    {
        private readonly Mock<ICharacterManager> _characterManager = new Mock<ICharacterManager>();
        private readonly Mock<IMapChannelManager> _mapChannelManager = new Mock<IMapChannelManager>();
        private readonly Mock<IManifestationManager> _manifestationManager = new Mock<IManifestationManager>();

        private readonly Mock<IGameUnitOfWorkFactory> _gameUnitOfWorkFactory = new Mock<IGameUnitOfWorkFactory>();
        private readonly Mock<ICharUnitOfWork> _charUnitOfWork = new Mock<ICharUnitOfWork>();
        private readonly Mock<IGameAccountRepository> _gameAccountRepository = new Mock<IGameAccountRepository>();
        private readonly Mock<ICharacterRepository> _characterRepository = new Mock<ICharacterRepository>();

        private readonly Mock<IRasaGameServer> _rasaGameServer = new Mock<IRasaGameServer>();
        private readonly Mock<IClientSocket> _clientSocket = new Mock<IClientSocket>();
        
        public ClientBuilder WithServerAuthenticateClient(LoginAccountEntry loginAccountEntry)
        {
            _rasaGameServer.Setup(m => m.AuthenticateClient(It.IsAny<Client>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(loginAccountEntry);
            return this;
        }

        public ClientBuilder WithClientIsBanned()
        {
            _rasaGameServer.Setup(m => m.IsBanned(It.IsAny<uint>()))
                .Returns(true);
            return this;
        }

        public ClientBuilder WithClientIsAlreadyLoggedIn()
        {
            _rasaGameServer.Setup(m => m.IsAlreadyLoggedIn(It.IsAny<uint>()))
                .Returns(true);
            return this;
        }

        public ClientBuilder WithRemoteAddress(IPAddress remoteAddress)
        {
            _clientSocket.Setup(m => m.RemoteAddress)
                .Returns(remoteAddress);
            return this;
        }

        public Client Build()
        {
            _charUnitOfWork.Setup(m => m.GameAccounts)
                .Returns(_gameAccountRepository.Object);
            _charUnitOfWork.Setup(m => m.Characters)
                .Returns(_characterRepository.Object);

            _gameUnitOfWorkFactory.Setup(m => m.CreateChar())
                .Returns(_charUnitOfWork.Object);

            var packetHandler = new ClientPacketHandler(
                _characterManager.Object,
                _mapChannelManager.Object,
                _manifestationManager.Object
            );
            var client = new Client(
                _gameUnitOfWorkFactory.Object,
                _characterManager.Object,
                packetHandler
            );

            client.RegisterAtServer(_rasaGameServer.Object, _clientSocket.Object, new ClientCryptData());

            return client;
        }

        public void VerifySocketCallbacksRegistered()
        {
            _clientSocket.Verify(m => m.AddOnErrorCallback(It.IsAny<Action<SocketAsyncEventArgs>>()));
            _clientSocket.Verify(m => m.AddOnReceiveCallback(It.IsAny<Action<BufferData>>()));
            _clientSocket.Verify(m => m.AddOnEncryptCallback(It.IsAny<Action<IEncryptData>>()));
            _clientSocket.Verify(m => m.AddOnDecryptCallback(It.IsAny<Func<BufferData, bool>>()));
        }

        public void VerifyReceiveAsyncAtSocketCalled()
        {
            _clientSocket.Verify(m => m.ReceiveAsync());
        }

        public void VerifyMessageSent([NotNull]IClientMessage expectedMessage)
        {
            _clientSocket.Verify(m => m.Send(It.Is<ProtocolPacket>(sentPacket => IsMatch(sentPacket, expectedMessage))));
        }

        public void VerifyNoMessagesSent()
        {
            _clientSocket.Verify(m => m.Send(It.IsAny<IBasePacket>()), Times.Never);
        }

        public void VerifyUnitOfWorkCompleted(int times)
        {
            _gameUnitOfWorkFactory.Verify(m => m.CreateChar(), Times.AtLeastOnce);
            _charUnitOfWork.Verify(m => m.Complete(), Times.Exactly(times));
        }

        public void VerifyGameAccountCreated(uint accountId, string name, string email)
        {
            _gameAccountRepository.Verify(m => m.CreateOrUpdate(accountId, name, email));
        }

        public void VerifyGameAccountForLoginLoaded(uint accountId, IPAddress expectedIpAddress)
        {
            _gameAccountRepository.Verify(m => m.Get(accountId));
            _gameAccountRepository.Verify(m => m.UpdateLoginData(accountId, expectedIpAddress));
        }

        public void VerifyStartCharacterSelectionCalled(Client expectedClient)
        {
            _characterManager.Verify(m => m.StartCharacterSelection(expectedClient));
        }

        public void VerifyCharacterSaved(Manifestation expectedManifestation)
        {
            _characterRepository.Verify(m => m.SaveCharacter(expectedManifestation));
            _charUnitOfWork.Verify(m => m.Complete());
        }

        public void VerifyClientLoggedOutFromMap(Client expectedClient)
        {
            _mapChannelManager.Verify(m => m.CharacterLogout(expectedClient));
        }

        public void VerifyMapLoaded(Client expectedClient)
        {
            _mapChannelManager.Verify(m => m.MapLoaded(expectedClient));
        }

        private bool IsMatch(ProtocolPacket sentPacket, [NotNull] IClientMessage expectedMessage)
        {
            return sentPacket?.Message != null && AreEqual(expectedMessage, sentPacket.Message);
        }

        private bool AreEqual([NotNull] IClientMessage expectedMessage, [NotNull] IClientMessage actualMessage)
        {
            var expectedType = expectedMessage.GetType();
            if (expectedType != actualMessage.GetType())
            {
                return false;
            }

            var properties = expectedType.GetProperties()
                .Where(p => p.GetMethod != null);

            foreach (var propertyInfo in properties)
            {
                var expectedValue = propertyInfo.GetValue(expectedMessage);
                var actualValue = propertyInfo.GetValue(actualMessage);
                if (expectedValue == null)
                {
                    if (actualValue != null)
                    {
                        return false;
                    }
                    continue;
                }

                var expectedValueType = expectedValue.GetType();
                if (expectedValueType != typeof(string) && !expectedValueType.IsValueType)
                {
                    // for complex types, we just match the type
                    if (expectedValueType != actualValue?.GetType())
                    {
                        return false;
                    }
                    continue;
                }

                if (!expectedValue.Equals(actualValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}