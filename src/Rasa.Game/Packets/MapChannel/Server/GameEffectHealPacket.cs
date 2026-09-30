namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'Heal', (healAmount,)) on a Xanx holding XANX_FORTIFY
    /// (gameeffectdata 302): the Xanx healed itself. XanxFortifyEffect.Recv_Heal, for an amount
    /// above zero, calls the holder's AnnounceHealing(holder, amount) - "+amount" floated over it,
    /// the healed message in the combat log (COMBAT_REGEN_HEALTH) with the Xanx as its own healer,
    /// and its health display refreshed. It changes no health: the UpdateHealth before it does.
    ///
    /// Needed because that UpdateHealth announces nothing: the client announces an attribute
    /// change only when its whoId is an entity it does not have, and a Xanx healing itself is
    /// one it has.
    ///
    /// Heal is a method of the effect, not of the entity: the Heal (711) opcode names it in the
    /// client's method table, but no entity class has a Recv_Heal to answer one sent to it
    /// directly. XanxFortifyEffect's is the only one in the client. The C++ server only listed
    /// the id. Sent by CreatureHabits when a Xanx devours a corpse.
    /// </summary>
    public class GameEffectHealPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public int Amount { get; }

        public GameEffectHealPacket(int effectId, int amount)
        {
            EffectId = effectId;
            Amount = amount;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("Heal");
            pw.WriteTuple(1);                   // args = (healAmount,)
            pw.WriteInt(Amount);
        }
    }
}
