namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Whether a player's helmet is drawn: client/augmentations/manifestation.py
    /// Recv_ShowHelmetChanged(bShowHelmet), called on the player's entity. It calls SetShowHelmet,
    /// which leaves the HELMET slot out of the appearance (actor.py _ProcessSwapsets) and redraws.
    /// AppearanceData still lists the helmet; this is what hides it.
    /// </summary>
    public class ShowHelmetChangedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ShowHelmetChanged;

        public bool ShowHelmet { get; set; }

        public ShowHelmetChangedPacket(bool showHelmet)
        {
            ShowHelmet = showHelmet;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(ShowHelmet);
        }
    }
}
