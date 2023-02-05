using System;
using System.Buffers;
using System.IO;
using System.Net;

namespace Rasa.Auth;

using Rasa.Communicator;
using Rasa.Cryptography;
using Rasa.Data;
using Rasa.Extensions;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets;
using Rasa.Packets.Auth.Client;
using Rasa.Packets.Auth.Server;
using Rasa.Repositories;
using Rasa.Repositories.Auth.Account;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures.Auth;
using Rasa.Timer;

public class Client
{
    public const int SendBufferSize = 512;
    public const int SendBufferCryptoPadding = 8;
    public const int SendBufferChecksumPadding = 8;

    private readonly IAuthUnitOfWorkFactory _authUnitOfWorkFactory;

    public AsyncLengthedSocket Socket { get; }
    public Server Server { get; }

    public uint OneTimeKey { get; }
    public uint SessionId1 { get; }
    public uint SessionId2 { get; }
    public AuthAccountEntry AccountEntry { get; private set; }
    public ClientState State { get; private set; }
    public Timer Timer { get; }

    private readonly PacketQueue _packetQueue = new();

    public Client(AsyncLengthedSocket socket, Server server, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
    {
        _authUnitOfWorkFactory = authUnitOfWorkFactory;

        Server = server;
        State = ClientState.Connected;

        Timer = new Timer();

        Socket = socket;
        Socket.OnError += OnError;
        Socket.OnReceive += OnReceive;
        Socket.Start();

        var rnd = new Random();

        OneTimeKey = rnd.NextUInt();
        SessionId1 = rnd.NextUInt();
        SessionId2 = rnd.NextUInt();

        SendPacket(new ProtocolVersionPacket(OneTimeKey));

        Timer.Add("timeout", Server.Config.AuthConfig.ClientTimeout * 1000, false, () =>
        {
            Logger.WriteLog(LogType.Network, "*** Client timed out! Ip: {0}", Socket.RemoteAddress);

            Close();
        });

        Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
    }

    public void Update(long delta)
    {
        Timer.Update(delta);

        if (State == ClientState.Disconnected)
            return;

        IBasePacket packet;

        while ((packet = _packetQueue.PopIncoming()) != null)
            HandlePacket(packet);

        while ((packet = _packetQueue.PopOutgoing()) != null)
            SendPacket(packet);
    }
    
    public void Close()
    {
        if (State == ClientState.Disconnected)
            return;

        Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

        Timer.Remove("timeout");

        State = ClientState.Disconnected;

        Socket.Close();

        Server.Disconnect(this);
    }

    public void SendPacket(IBasePacket packet)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(SendBufferSize + SendBufferCryptoPadding + SendBufferChecksumPadding);
        var writer = new BinaryWriter(new MemoryStream(buffer, true));

        packet.Write(writer);

        var length = (int)writer.BaseStream.Position;
        if (packet is not ProtocolVersionPacket)
            AuthCryptManager.Encrypt(buffer, 0, ref length, buffer.Length);

        Socket.Send(buffer, 0, length);

        ArrayPool<byte>.Shared.Return(buffer);
    }

    public void HandlePacket(IBasePacket packet)
    {
        if (packet is not IOpcodedPacket<ClientOpcode> authPacket)
            return;

        switch (authPacket.Opcode)
        {
            case ClientOpcode.Login:
                MsgLogin(authPacket as LoginPacket);
                break;

            case ClientOpcode.Logout:
                MsgLogout(authPacket as LogoutPacket);
                break;

            case ClientOpcode.AboutToPlay:
                MsgAboutToPlay(authPacket as AboutToPlayPacket);
                break;

            case ClientOpcode.ServerListExt:
                MsgServerListExt(authPacket as ServerListExtPacket);
                break;
        }
    }

    public void RedirectionResult(CommunicatorActionResult result, ServerInfo info)
    {
        switch (result)
        {
            case CommunicatorActionResult.Failure:
                SendPacket(new PlayFailPacket(FailReason.UnexpectedError));

                Close();

                Logger.WriteLog(LogType.Error, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) couldn't be redirected to server: {info.ServerId}!");
                break;

            case CommunicatorActionResult.Success:
                HandleSuccessfulRedirect(info);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(result));
        }
    }

    private void HandleSuccessfulRedirect(ServerInfo info)
    {
        SendPacket(new HandoffToQueuePacket
        {
            OneTimeKey = OneTimeKey,
            ServerId = info.ServerId,
            AccountId = AccountEntry.Id
        });

        using var unitOfWork = _authUnitOfWorkFactory.Create();
        unitOfWork.AuthAccountRepository.UpdateLastServer(AccountEntry.Id, info.ServerId);
        unitOfWork.Complete();

        Logger.WriteLog(LogType.Network, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) was redirected to the queue of the server: {info.ServerId}!");
    }

    private void OnError() => Close();

    private void OnReceive(NonContiguousMemoryStream incomingStream, int length)
    {
        var data = ArrayPool<byte>.Shared.Rent(length);

        incomingStream.Read(data, 0, length);

        AuthCryptManager.Decrypt(data, 0, length);

        using var br = new BinaryReader(new MemoryStream(data, 0, length, false));

        var packet = CreatePacket((ClientOpcode)br.ReadByte());

        packet.Read(br);

        ArrayPool<byte>.Shared.Return(data);

        _packetQueue.EnqueueIncoming(packet);

        // Reset the timeout after every action
        Timer.ResetTimer("timeout");
    }

    private static IBasePacket CreatePacket(ClientOpcode opcode)
    {
        return opcode switch
        {
            ClientOpcode.AboutToPlay   => new AboutToPlayPacket(),
            ClientOpcode.Login         => new LoginPacket(),
            ClientOpcode.Logout        => new LogoutPacket(),
            ClientOpcode.ServerListExt => new ServerListExtPacket(),
            ClientOpcode.SCCheck       => new SCCheckPacket(),
            _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
        };
    }

    private void MsgLogin(LoginPacket packet)
    {
        using var unitOfWork = _authUnitOfWorkFactory.Create();

        try
        {
            AccountEntry = unitOfWork.AuthAccountRepository.GetByUserName(packet.UserName, packet.Password);
        }
        catch (EntityNotFoundException)
        {
            SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
            Close();
            Logger.WriteLog(LogType.Security, $"User ({packet.UserName}) tried to log in with an invalid username!");
            return;
        }
        catch (PasswordCheckFailedException e)
        {
            SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
            Close();
            Logger.WriteLog(LogType.Security, e.Message);
            return;
        }
        catch (AccountLockedException e)
        {
            SendPacket(new BlockedAccountPacket());
            Close();
            Logger.WriteLog(LogType.Security, e.Message);
            return;
        }

        unitOfWork.AuthAccountRepository.UpdateLoginData(AccountEntry.Id, (Socket.RemoteAddress as IPEndPoint).Address);
        unitOfWork.Complete();

        State = ClientState.LoggedIn;

        SendPacket(new LoginOkPacket
        {
            SessionId1 = SessionId1,
            SessionId2 = SessionId2
        });

        Logger.WriteLog(LogType.Network, "*** Client logged in from {0}", Socket.RemoteAddress);
    }

    private void MsgLogout(LogoutPacket packet)
    {
        if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
        {
            Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an LogoutPacket with invalid session data!");
            return;
        }

        Close();
    }

    private void MsgServerListExt(ServerListExtPacket packet)
    {
        if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
        {
            Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an ServerListExtPacket with invalid session data!");
            return;
        }

        State = ClientState.ServerList;

        SendPacket(new SendServerListExtPacket(Server.ServerList, AccountEntry.LastServerId));
    }

    private void MsgAboutToPlay(AboutToPlayPacket packet)
    {
        if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
        {
            Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an AboutToPlay packet with invalid session data!");
            return;
        }

        Server.RequestRedirection(this, packet.ServerId);
    }
}
