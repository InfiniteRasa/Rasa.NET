namespace Rasa.Packets.Party.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py Recv_PartyMemberVoiceIds(memberList): replaces g_voiceIdToUserId with the
    /// (userId, voiceId) pairs. The client uses it to turn the voice player id Talkback reports for
    /// a speaker into the squad member whose portrait lights up. The voice id is the account id,
    /// so every pair is (id, id).
    /// </summary>
    public class PartyMemberVoiceIdsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyMemberVoiceIds;

        internal List<(uint UserId, uint VoiceId)> Members { get; }

        internal PartyMemberVoiceIdsPacket(List<(uint UserId, uint VoiceId)> members)
        {
            Members = members;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Members.Count);

            foreach (var (userId, voiceId) in Members)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(userId);
                pw.WriteUInt(voiceId);
            }
        }
    }
}
