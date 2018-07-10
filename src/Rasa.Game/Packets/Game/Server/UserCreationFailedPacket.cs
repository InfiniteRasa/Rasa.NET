namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class UserCreationFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UserCreationFailed;

        public CreateCharacterResult Result { get; set; }

        public UserCreationFailedPacket(CreateCharacterResult result)
        {
            Result = result;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int) Result);
        }
    }
}
