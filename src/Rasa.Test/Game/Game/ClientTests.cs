using System.Linq;
using Xunit;

namespace Rasa.Test.Game
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Numerics;
    using Data;
    using Models;
    using Packets.Communicator;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Packets.Protocol;
    using Structures;
    using TestHelper.Builder;

    public class ClientTests
    {
        [Fact]
        public void RegisterAtServer_ShouldRegisterCallbacks()
        {
            var builder = new ClientBuilder();
            builder.Build();

            builder.VerifySocketCallbacksRegistered();
        }

        [Fact]
        public void RegisterAtServer_ShouldStartReceive()
        {
            var builder = new ClientBuilder();
            builder.Build();

            builder.VerifyReceiveAsyncAtSocketCalled();
        }

        [Fact]
        public void RegisterAtServer_ShouldSetupSendSequence()
        {
            var sut = new ClientBuilder()
                .Build();

            var expectedSendSequence = Enumerable.Repeat(1u, 256);

            Assert.Equal(expectedSendSequence, sut.SendSequence);
        }

        [Theory]
        [InlineData("1.16.1")] // too short
        [InlineData("1.16.5.10")] // too long
        [InlineData("1.16.3.0")] // just wrong
        public void HandlePacket_LoginPacket_WrongVersion_ShouldFailFast(string version)
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();

            var message = CreateLoginMessage(version);
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMessageSent(new LoginResponseMessage
            {
                ErrorCode = LoginErrorCodes.VersionMismatch,
                Subtype = LoginResponseMessageSubtype.Failed
            });
        }

        [Fact]
        public void HandlePacket_LoginPacket_WithAuthenticationFails_ShouldFailFast()
        {
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(null);
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMessageSent(new LoginResponseMessage
            {
                ErrorCode = LoginErrorCodes.AuthenticationFailed,
                Subtype = LoginResponseMessageSubtype.Failed
            });
        }

        [Fact]
        public void HandlePacket_LoginPacket_WithClientIsBanned_ShouldFailFast()
        {
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(CreateLoginAccountEntry())
                .WithClientIsBanned();
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMessageSent(new LoginResponseMessage
            {
                ErrorCode = LoginErrorCodes.AccountLocked,
                Subtype = LoginResponseMessageSubtype.Failed
            });

            builder.VerifyUnitOfWorkCompleted(0);
        }

        [Fact]
        public void HandlePacket_LoginPacket_WithClientIsAlreadyLoggedIn_ShouldFailFast()
        {
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(CreateLoginAccountEntry())
                .WithClientIsAlreadyLoggedIn();
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMessageSent(new LoginResponseMessage
            {
                ErrorCode = LoginErrorCodes.AlreadyLoggedIn,
                Subtype = LoginResponseMessageSubtype.Failed
            });

            builder.VerifyUnitOfWorkCompleted(0);
        }

        [Fact]
        public void HandlePacket_LoginPacket_LoginSuccessful_ShouldSucceedDelayed()
        {
            var accountEntry = CreateLoginAccountEntry();
            var remoteAddress = new IPAddress(1);
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(accountEntry)
                .WithRemoteAddress(remoteAddress);
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyNoMessagesSent();

            // send delayed packages
            sut.Update(0);

            builder.VerifyMessageSent(new LoginResponseMessage
            {
                AccountId = message.AccountId,
                Subtype = LoginResponseMessageSubtype.Success
            });

            builder.VerifyGameAccountCreated(accountEntry.Id, accountEntry.Name, accountEntry.Email);
            builder.VerifyGameAccountForLoginLoaded(accountEntry.Id, remoteAddress);
            builder.VerifyUnitOfWorkCompleted(1);
        }

        [Fact]
        public void HandlePacket_LoginPacket_LoginSuccessful_ShouldSetState()
        {
            var accountEntry = CreateLoginAccountEntry();
            var remoteAddress = new IPAddress(1);
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(accountEntry)
                .WithRemoteAddress(remoteAddress);
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            Assert.Equal(ClientState.LoggedIn, sut.State);
        }

        [Fact]
        public void HandlePacket_LoginPacket_LoginSuccessful_ShouldHandOverToCharacterSelection()
        {
            var accountEntry = CreateLoginAccountEntry();
            var remoteAddress = new IPAddress(1);
            var builder = new ClientBuilder()
                .WithServerAuthenticateClient(accountEntry)
                .WithRemoteAddress(remoteAddress);
            var sut = builder.Build();

            var message = CreateLoginMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyStartCharacterSelectionCalled(sut);
        }

        [Fact]
        public void HandlePacket_MovePacket_ShouldUpdatePositionAndRotation()
        {
            var newPosition = new Vector3(10, 10, 10);
            var newRotation = 100f;

            var builder = new ClientBuilder();
            var sut = builder.Build();
            sut.Player = new Manifestation
            {
                Position = new Vector3(5, 5, 5),
                Rotation = 100_000
            };

            var message = new MoveMessage
            {
                Type = ClientMessageOpcode.Move,
                Movement = new Movement(newPosition, 1, 0, new Vector2(newRotation, 500))
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            Assert.Equal(newPosition, sut.Player.Position);
            Assert.Equal(newRotation, sut.Player.Rotation);
        }

        [Fact]
        public void HandlePacket_PingPacket_ShouldSendPongFast()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();
            sut.Player = new Manifestation
            {
                Position = new Vector3(5, 5, 5),
                Rotation = 100_000
            };

            var message = new PingMessage();
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMessageSent(message);
        }

        [Fact]
        public void HandlePacket_CallServerMethodPacket_MapLoaded_ShouldHandOverToMapChannelManager()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new MapLoadedPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyMapLoaded(sut);
        }

        [Fact]
        public void HandlePacket_CallServerMethodPacket_Ping_ShouldAcknowledgePingDelayed()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new PingPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyNoMessagesSent();

            sut.Update(0);

            builder.VerifyMessageSent(new CallMethodMessage((ulong)SysEntity.ClientMethodId, new AckPingPacket(0)));
        }

        [Fact]
        public void HandlePacket_CallServerMethodPacket_RequestLogout_ShouldStartLogoutTimeDelayed()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new RequestLogoutPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyNoMessagesSent();

            sut.Update(0);

            builder.VerifyMessageSent(new CallMethodMessage((ulong)SysEntity.ClientMethodId, new LogoutTimeRemainingPacket()));
        }

        [Fact]
        public void HandlePacket_CallServerMethodPacket_CharacterLogout_ShouldSaveCharacter()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();
            sut.Player = new Manifestation();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new CharacterLogoutPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyCharacterSaved(sut.Player);
        }

        [Fact]
        public void HandlePacket_CallServerMethod_Close_ShouldRemoveClientFromMap()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();
            sut.Player = new Manifestation();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new CharacterLogoutPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyClientLoggedOutFromMap(sut);
        }

        [Fact]
        public void HandlePacket_CallServerMethod_Close_ShouldHandOverToCharacterSelection()
        {
            var builder = new ClientBuilder();
            var sut = builder.Build();
            sut.Player = new Manifestation();

            var message = new CallServerMethodMessage
            {
                Type = ClientMessageOpcode.CallServerMethod,
                Packet = new CharacterLogoutPacket()
            };
            var packet = CreatePacket(message);
            sut.HandlePacket(packet);

            builder.VerifyStartCharacterSelectionCalled(sut);
        }

        private LoginMessage CreateLoginMessage(string version = "1.16.5.0")
        {
            return new()
            {
                AccountId = 50,
                Version = version
            };
        }

        private LoginAccountEntry CreateLoginAccountEntry()
        {
            return new(new RedirectRequestPacket())
            {
                Id = 100,
                Name = "some name",
                Email = "some email"
            };
        }

        private ProtocolPacket CreatePacket(IClientMessage message)
        {
            var type = GetMatchingOpCode(message);
            return new ProtocolPacket(message, type, false, 0);
        }

        private ClientMessageOpcode GetMatchingOpCode(IClientMessage message)
        {
            return message switch
            {
                LoginMessage => ClientMessageOpcode.Login,
                MoveMessage => ClientMessageOpcode.Move,
                CallServerMethodMessage => ClientMessageOpcode.CallServerMethod,
                PingMessage => ClientMessageOpcode.Ping,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}