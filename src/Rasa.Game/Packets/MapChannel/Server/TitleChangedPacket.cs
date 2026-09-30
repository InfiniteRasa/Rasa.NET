namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_TitleChanged(curTitleId) on a manifestation: the title it wears, to its own client
    /// and to everyone who can see it (overheadwindow.py draws it over the name). None for no
    /// title: the client keeps whatever it is given, and the overhead window looks a 0 up in
    /// titledata and fails on the miss.
    /// </summary>
    public class TitleChangedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TitleChanged;

        public uint TitleId { get; set; }

        public TitleChangedPacket(uint titleId)
        {
            TitleId = titleId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);

            if (TitleId == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteUInt(TitleId);
        }
    }
}
