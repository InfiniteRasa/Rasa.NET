using System;
using System.IO;

namespace Rasa.Packets.Auth.Server
{
    using Data;

    public class BlockedAccountWithMsgPacket : IOpcodedPacket<ServerOpcode>
    {
        public ServerOpcode Opcode { get; } = ServerOpcode.BlockedAccountWithMessage;
        /// <summary>A server-to-client packet: the auth server writes it and never reads one.</summary>
        public void Read(BinaryReader reader)
        {
            throw new NotSupportedException("BlockedAccountWithMsgPacket is sent by the server, not read by it.");
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write((byte) 0); // TODO: when needed

            throw new NotImplementedException();
        }
    }
}
