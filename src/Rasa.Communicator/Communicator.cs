using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using Rasa.Packets;

namespace Rasa.Communicator
{
    using Memory;
    using Networking;
    using Packets;

    public enum CommunicatorType
    {
        Server,
        ServerClient,
        Client
    }

    public enum CommunicatorOpcode : byte
    {
        LoginRequest = 0,
        LoginResponse = 1,
        RedirectRequest = 2,
        RedirectResponse = 3,
        ServerInfoRequest = 4,
        ServerInfoResponse = 5
    }

    public enum CommunicatorActionResult : byte
    {
        Success = 0,
        Failure = 1
    }

    public class Communicator
    {
        public const SizeType CommunicatorHeaderLen = SizeType.Word;
        public const double ServerInfoUpdateIntervalMs = 30000.0d;

        public CommunicatorType Type { get; }
        public LengthedSocket Socket { get; private set; }
        public List<Communicator> Children { get; }
        public DateTime LastRequestTime { get; private set; }
        public ServerData ServerData { get; set; }

        public Action OnError { get; set; }
        public Action<ServerData> OnConnect { get; set; }
        public Func<ServerData, bool> OnLoginRequest { get; set; }
        public Action<CommunicatorActionResult> OnLoginResponse { get; set; }
        public Func<RedirectRequest, bool> OnRedirectRequest { get; set; }
        public Action<CommunicatorActionResult, uint> OnRedirectResponse { get; set; }
        public Action<ServerInfo> OnServerInfoRequest { get; set; }
        public Action<ServerInfo> OnServerInfoResponse { get; set; }

        public Communicator(CommunicatorType type, IPAddress address, int port, int backlog = 0)
        {
            if (type == CommunicatorType.ServerClient)
                throw new ArgumentOutOfRangeException(nameof(type));

            Type = type;

            Socket = new LengthedSocket(CommunicatorHeaderLen);
            Socket.OnError += OnSocketError;

            switch (Type)
            {
                case CommunicatorType.Server:
                    Children = new();

                    Socket.OnAccept += OnSocketAccept;

                    Socket.Bind(new IPEndPoint(address, port));
                    Socket.Listen(backlog);
                    Socket.AcceptAsync();
                    break;

                case CommunicatorType.Client:
                    Socket.OnReceive += OnSocketReceive;
                    Socket.OnConnect += OnSocketConnect;

                    Socket.ConnectAsync(new IPEndPoint(address, port));
                    break;
            }
        }

        public Communicator(LengthedSocket socket)
        {
            Type = CommunicatorType.ServerClient;

            Socket = socket;
            Socket.OnReceive += OnSocketReceive;

            Socket.ReceiveAsync();
        }

        #region Socketing
        private void OnSocketError(SocketAsyncEventArgs args)
        {
            Socket.Close();

            OnError?.Invoke();

            Logger.WriteLog(LogType.Communicator, $"Communicator(Type = {Type}) has encountered an error!");
        }

        private void OnSocketAccept(LengthedSocket socket)
        {
            Children.Add(new Communicator(socket));

            Logger.WriteLog(LogType.Communicator, $"New Communicator(Type = {Type}) client has connected! Remote: {socket.RemoteAddress}");

            Socket.AcceptAsync();
        }

        private void OnSocketReceive(BufferData data)
        {
            using var br = data.GetReader();

            var opcode = (CommunicatorOpcode)br.ReadByte();

            IOpcodedPacket<CommunicatorOpcode> packet = opcode switch
            {
                CommunicatorOpcode.LoginRequest       => new LoginRequestPacket(),
                CommunicatorOpcode.LoginResponse      => new LoginResponsePacket(),
                CommunicatorOpcode.RedirectRequest    => new RedirectRequestPacket(),
                CommunicatorOpcode.RedirectResponse   => new RedirectResponsePacket(),
                CommunicatorOpcode.ServerInfoRequest  => new ServerInfoRequestPacket(),
                CommunicatorOpcode.ServerInfoResponse => new ServerInfoResponsePacket(),

                _ => throw new Exception("Invalid opcode found in the Communicator's OnSocketReceive!")
            };

            packet.Read(br);

            switch (opcode)
            {
                case CommunicatorOpcode.LoginRequest:
                    MsgLoginRequest(packet as LoginRequestPacket);
                    break;

                case CommunicatorOpcode.LoginResponse:
                    MsgLoginResponse(packet as LoginResponsePacket);
                    break;

                case CommunicatorOpcode.RedirectRequest:
                    MsgRedirectRequest(packet as RedirectRequestPacket);
                    break;

                case CommunicatorOpcode.RedirectResponse:
                    MsgRedirectResponse(packet as RedirectResponsePacket);
                    break;

                case CommunicatorOpcode.ServerInfoRequest:
                    MsgServerInfoRequest(packet as ServerInfoRequestPacket);
                    break;

                case CommunicatorOpcode.ServerInfoResponse:
                    MsgServerInfoResponse(packet as ServerInfoResponsePacket);
                    break;
            }
        }

        private void OnSocketConnect(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                OnSocketError(args);
                return;
            }

            Logger.WriteLog(LogType.Communicator, $"Communicator(Type = {Type}) has connected to the Communicator Server!");

            if (OnConnect == null)
            {
                return;
            }

            var info = new ServerData();

            OnConnect(info);

            Socket.Send(new LoginRequestPacket(info));
            Socket.ReceiveAsync();
        }
        #endregion

        #region Requests
        public void RequestServerInfo()
        {
            if (Type == CommunicatorType.Server)
            {
                foreach (var client in Children)
                {
                    if ((DateTime.Now - client.LastRequestTime).TotalMilliseconds > ServerInfoUpdateIntervalMs)
                        client.RequestServerInfo();
                }
            }
            else if (Type == CommunicatorType.ServerClient)
            {
                LastRequestTime = DateTime.Now;

                Socket.Send(new ServerInfoRequestPacket());
            }
        }

        public void RequestRedirection(RedirectRequest request)
        {
            if (Type != CommunicatorType.ServerClient)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not request redirection!");
                return;
            }

            Socket.Send(new RedirectRequestPacket(request));
        }
        #endregion

        #region Packet Handlers
        private void MsgLoginRequest(LoginRequestPacket packet)
        {
            if (Type != CommunicatorType.ServerClient)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle login requests!");
                return;
            }

            if (OnLoginRequest == null)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) has no OnLoginRequest callback!");
                return;
            }

            var result = OnLoginRequest(packet.Data);

            Socket.Send(new LoginResponsePacket
            {
                Result = result ? CommunicatorActionResult.Success : CommunicatorActionResult.Failure
            });

            if (!result)
                return;

            ServerData = packet.Data;

            RequestServerInfo();
        }

        private void MsgLoginResponse(LoginResponsePacket packet)
        {
            if (Type != CommunicatorType.Client)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle login responses!");
                return;
            }

            if (packet.Result == CommunicatorActionResult.Success)
            {
                Logger.WriteLog(LogType.Communicator, $"Communicator(Type = {Type}) successfully authenticated with the Communicator server!");
            }
            else
            {
                Socket?.Close();

                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) could not authenticate with the Communicator server!");
            }

            OnLoginResponse?.Invoke(packet.Result);
        }

        private void MsgRedirectRequest(RedirectRequestPacket packet)
        {
            if (Type != CommunicatorType.Client)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle redirect requests!");
                return;
            }

            if (OnRedirectRequest == null)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) has no OnRedirectRequest callback!");
                return;
            }

            var result = OnRedirectRequest(packet.Request);

            Socket.Send(new RedirectResponsePacket
            {
                AccountId = packet.Request.AccountId,
                Result = result ? CommunicatorActionResult.Success : CommunicatorActionResult.Failure
            });
        }

        private void MsgRedirectResponse(RedirectResponsePacket packet)
        {
            if (Type != CommunicatorType.ServerClient)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle redirect responses!");
                return;
            }

            if (OnRedirectResponse == null)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) has no OnRedirectResponse callback!");
                return;
            }

            OnRedirectResponse(packet.Result, packet.AccountId);
        }

        private void MsgServerInfoRequest(ServerInfoRequestPacket packet)
        {
            if (Type != CommunicatorType.Client)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle server info requests!");
                return;
            }

            if (OnServerInfoRequest == null)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) has no OnServerInfoRequest callback!");
                return;
            }

            var info = new ServerInfo();

            OnServerInfoRequest(info);

            Socket.Send(new ServerInfoResponsePacket(info));
        }

        private void MsgServerInfoResponse(ServerInfoResponsePacket packet)
        {
            if (Type != CommunicatorType.ServerClient)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) can not handle server info responses!");
                return;
            }

            if (OnServerInfoResponse == null)
            {
                Logger.WriteLog(LogType.Error, $"Communicator(Type = {Type}) has no OnServerInfoResponse callback!");
                return;
            }

            OnServerInfoResponse(packet.Info);
        }
        #endregion
    }

    public class ServerData
    {
        public byte Id { get; set; }
        public string Password { get; set; }
        public IPAddress Address { get; set; }
    }

    public class ServerInfo
    {
        public byte ServerId { get; set; }
        public IPAddress Ip { get; set; }
        public int QueuePort { get; set; }
        public int GamePort { get; set; }
        public byte AgeLimit { get; set; }
        public byte PKFlag { get; set; }
        public ushort CurrentPlayers { get; set; }
        public ushort MaxPlayers { get; set; }
        public byte Status { get; set; }

        public void Setup(IPAddress address, int queuePort, int gamePort, byte ageLimit, byte pkFlag, ushort currPlayers, ushort maxPlayers)
        {
            Ip = address;
            QueuePort = queuePort;
            GamePort = gamePort;
            AgeLimit = ageLimit;
            PKFlag = pkFlag;
            CurrentPlayers = currPlayers;
            MaxPlayers = maxPlayers;
            Status = 1;
        }
        public void Clear()
        {
            Ip = IPAddress.None;
            QueuePort = 0;
            GamePort = 0;
            AgeLimit = 0;
            PKFlag = 0;
            CurrentPlayers = 0;
            MaxPlayers = 0;
            Status = 0;
        }
    }

    public class RedirectRequest
    {
        public uint AccountId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public uint OneTimeKey { get; set; }
    }
}
