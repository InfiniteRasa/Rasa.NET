using System.Collections.Generic;

namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py Recv_PartyMemberRoll(itemClassId, winnerUserId, rolls, isGreedRoll): how a
    /// squad roll for an item went, as lines in the chat window. rolls is {round: {userId: roll}},
    /// one round per throw - a tie is thrown again by those who tied, which is the next round, and
    /// the client prints "There's a tie" between them. A need roll with one round and one roller
    /// is printed as "X has looting rights to Y" with no dice. User ids are account ids, as in
    /// the squad member list the client looks the names up in; its own id reads as "You".
    /// </summary>
    public class PartyMemberRollPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyMemberRoll;

        public uint ItemClassId { get; }
        public uint WinnerUserId { get; }
        public bool IsGreedRoll { get; }

        /// <summary>Each round's throws, in order; each is account id to roll.</summary>
        public List<List<(uint UserId, int Roll)>> Rounds { get; }

        public PartyMemberRollPacket(uint itemClassId, uint winnerUserId, List<List<(uint UserId, int Roll)>> rounds, bool isGreedRoll)
        {
            ItemClassId = itemClassId;
            WinnerUserId = winnerUserId;
            Rounds = rounds;
            IsGreedRoll = isGreedRoll;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(ItemClassId);
            pw.WriteUInt(WinnerUserId);

            pw.WriteDictionary(Rounds.Count);

            for (var i = 0; i < Rounds.Count; i++)
            {
                pw.WriteInt(i + 1);
                pw.WriteDictionary(Rounds[i].Count);

                foreach (var (userId, roll) in Rounds[i])
                {
                    pw.WriteUInt(userId);
                    pw.WriteInt(roll);
                }
            }

            pw.WriteBool(IsGreedRoll);
        }
    }
}
