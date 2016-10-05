using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Communicator
{
    using Networking;

    public class Communicator
    {
        public enum CommunicatorType : byte
        {
            Auth = 0,
            Global = 1,
            Zone = 3
        }

        public CommunicatorType Type { get; }
        public CommunicatorConfig Config { get; }
        public LengthedSocket ConnectSocket { get; } = new LengthedSocket(SizeType.Word);
        public LengthedSocket Socket { get; } = new LengthedSocket(SizeType.Word);
        public List<CommunicatorClient> Clients { get; } = new List<CommunicatorClient>();

        public Communicator(CommunicatorType type, CommunicatorConfig config)
        {
            Type = type;
            Config = config;



            switch (type)
            {
                case CommunicatorType.Auth:
                    InitServer();
                    break;

                case CommunicatorType.Global:
                    InitClient();
                    InitServer();
                    break;

                case CommunicatorType.Zone:
                    InitClient();
                    break;
            }
        }

        private void InitServer()
        {
            Socket.OnAccept += OnAccept;
            Socket.Bind(new IPEndPoint(IPAddress.Any, Config.ListenPort));
            Socket.Listen(10);
        }

        private void OnAccept(LengthedSocket socket)
        {
            Clients.Add(new CommunicatorClient(socket));
        }

        private void InitClient()
        {
            ConnectSocket.OnConnect += OnConnect;
            ConnectSocket.ConnectAsync(new IPEndPoint(Config.ConnectAddress, Config.ConnectPort));
        }

        private void OnConnect(SocketAsyncEventArgs args)
        {
            // todo
        }
    }
}
