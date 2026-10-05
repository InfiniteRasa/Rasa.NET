using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;

    /// <summary>Auth -> game: what became of the CreateAccountRequestPacket with that <see cref="RequestId"/>.</summary>
    public class CreateAccountResponsePacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.CreateAccountResponse;

        public uint RequestId { get; set; }
        public CreateAccountResult Result { get; set; }

        /// <summary>The new account's id; 0 unless it was created.</summary>
        public uint AccountId { get; set; }

        public void Read(BinaryReader br)
        {
            RequestId = br.ReadUInt32();
            Result = (CreateAccountResult) br.ReadByte();
            AccountId = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(RequestId);
            bw.Write((byte) Result);
            bw.Write(AccountId);
        }
    }
}
