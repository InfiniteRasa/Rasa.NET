namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_UpdateCredits(creditTypeId, amount, delta): the new total of one currency, and what
    /// it changed by. The client posts the delta with PLAYER_CREDITS_UPDATE; for prestige a
    /// delta above 0 floats "+N Prestige" over the player (UI_FLOAT_PRESTIGE) and adds to the
    /// prestige window's "this session" count. A spend is a negative delta, which both ignore.
    /// </summary>
    public class UpdateCreditsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateCredits;

        public CurencyType Type { get; set; }
        public int Amount { get; set; }
        public int Delta { get; set; }

        public UpdateCreditsPacket(CurencyType type, int amount, int delta)
        {
            Type = type;
            Amount = amount;
            Delta = delta;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteInt(Amount);
            pw.WriteInt(Delta);
        }
    }
}
