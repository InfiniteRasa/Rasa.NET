namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class SendJoinRequestToSquadLeaderPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SendJoinRequestToSquadLeader;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"SendJoinRequestToSquadLeaderPacket: {pr.ToString()}");
        }
    }
}
