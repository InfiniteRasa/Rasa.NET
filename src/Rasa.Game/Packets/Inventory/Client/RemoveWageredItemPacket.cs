namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>RemoveWageredItem (10000093), client/prestige.py RemoveWageredItem(): the right click on the wagered item, back to the backpack. No arguments.</summary>
    public class RemoveWageredItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveWageredItem;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
