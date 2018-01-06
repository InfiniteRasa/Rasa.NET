namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorControllerInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorControllerInfo;

        public bool IsPlayer { get; set; }

        public ActorControllerInfoPacket(bool isPlayer)
        {
            IsPlayer = IsPlayer;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsPlayer);
        }
    }
}
