namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    /// <summary>client/party.py Recv_PartyMemberVoiceId(userId, voiceId): one entry of g_voiceIdToUserId. See PartyMemberVoiceIdsPacket.</summary>
    public class PartyMemberVoiceIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyMemberVoiceId;

        internal uint UserId { get; }
        internal uint VoiceId { get; }

        internal PartyMemberVoiceIdPacket(uint userId, uint voiceId)
        {
            UserId = userId;
            VoiceId = voiceId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(UserId);
            pw.WriteUInt(VoiceId);
        }
    }
}
