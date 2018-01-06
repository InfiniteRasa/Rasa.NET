namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeTitlePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeTitle;

        public int TitleId { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"ChangeTitle:\n{pr.ToString()}");
            pr.ReadTuple();
            if (pr.PeekType() == PythonType.Int)
                TitleId = pr.ReadInt();
            else
                pr.ReadNoneStruct();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(TitleId);
        }
    }
}
