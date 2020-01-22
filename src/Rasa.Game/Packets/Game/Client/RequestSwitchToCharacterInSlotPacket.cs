namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestSwitchToCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public byte SlotNum { get; set; }
        public bool SkipBootCamp { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SlotNum = (byte)pr.ReadUInt();

            var skipBootCamp = pr.ReadUnkStruct();

            switch (skipBootCamp)
            {
                case PythonType.TrueStruct:
                    SkipBootCamp = true;
                    break;
                case PythonType.ZeroStruct:
                    SkipBootCamp = false;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"RequestSwitchToCharacterInSlotPacket:\nExpected TrueStruct or ZeroStruct, got {skipBootCamp}");
                    break;
            }
        }
    }
}
