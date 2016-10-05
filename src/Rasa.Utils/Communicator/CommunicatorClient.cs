using System;

namespace Rasa.Communicator
{
    using Networking;
    using Packets;

    public class CommunicatorClient : INetworkClient
    {
        public LengthedSocket Socket { get; }

        private static PacketRouter<CommunicatorClient, int> PacketRouter { get; } = new PacketRouter<CommunicatorClient, int>();

        static CommunicatorClient()
        {
            PacketRouter.RegisterHandler(0, "MsgTest", null);// todo
        }

        public CommunicatorClient(LengthedSocket socket)
        {
            Socket = socket;
        }

        public void HandlePacket(IBasePacket packet)
        {
            throw new NotImplementedException();
        }

        public void SendPacket(IBasePacket packet)
        {
            throw new NotImplementedException();
        }
    }
}
