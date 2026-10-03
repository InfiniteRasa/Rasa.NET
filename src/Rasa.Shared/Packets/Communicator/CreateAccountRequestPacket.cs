using System.IO;

namespace Rasa.Packets.Communicator
{
    using Data;
    using Extensions;

    /// <summary>
    /// Game -> Auth: make an account. The accounts are Auth's - its database, its password
    /// hashing and pepper - so a game server that is asked for one (its REST API's /addaccount)
    /// asks here, and is answered with a CreateAccountResponsePacket carrying the same
    /// <see cref="RequestId"/>. Auth takes it only from a game server that has logged in.
    ///
    /// The password crosses this link as it is, as the game server's own password does at its
    /// login: the link is for a network the operator trusts.
    /// </summary>
    public class CreateAccountRequestPacket : IOpcodedPacket<CommOpcode>
    {
        public CommOpcode Opcode { get; } = CommOpcode.CreateAccountRequest;

        /// <summary>The asker's number for this request, echoed in the answer.</summary>
        public uint RequestId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void Read(BinaryReader br)
        {
            RequestId = br.ReadUInt32();
            Email = br.ReadLengthedString();
            Username = br.ReadLengthedString();
            Password = br.ReadLengthedString();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(RequestId);
            bw.WriteLengthedString(Email);
            bw.WriteLengthedString(Username);
            bw.WriteLengthedString(Password);
        }

        public override string ToString()
        {
            // Never the password: this is what a log line or a debugger shows of the packet.
            return $"CreateAccountRequestPacket({RequestId}, \"{Email}\", \"{Username}\", \"***\")";
        }
    }
}
