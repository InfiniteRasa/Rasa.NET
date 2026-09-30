namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>client.augmentations.weapon.Weapon.OnBind: (self.entityId,), the weapon's entity id.</summary>
    public class RequestBindPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestBind;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
