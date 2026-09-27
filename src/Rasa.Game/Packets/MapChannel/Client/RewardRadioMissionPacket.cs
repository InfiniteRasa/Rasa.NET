using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// RewardRadioMission (541), a user method: (missionId, selectionIdx, rating), the fields
    /// CompleteRadioMission has. client/missionlog.py RewardRadioMission, called only from
    /// conversationwindow.py OnAcceptRewardBtn when the window has no actor. The 1.16.5 client never
    /// gets there: the reward screen (UI_SHOW_CONVERSATION_MISSION_REWARD) is opened only by an NPC's
    /// Recv_Converse, always with that NPC's entity id, so the NPC branch - RewardNPCMission - is
    /// taken. Radio turn-ins, a reward still owed on a mission already succeeded included, all go
    /// through the completion screen and CompleteRadioMission. Handled so that a client that does
    /// send it is not disconnected, and does what the name says (MissionApplication.TryRewardRadioMission).
    /// </summary>
    public sealed class RewardRadioMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.RewardRadioMission;
        public uint MissionId { get; set; }
        public int? SelectionIdx { get; set; }
        public int? Rating { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 3)
                throw new InvalidDataException("Radio mission reward requires mission, selection and rating fields.");
            MissionId = pr.ReadUInt();
            SelectionIdx = pr.ReadNullableInt();
            Rating = pr.ReadNullableInt();
        }
    }
}
