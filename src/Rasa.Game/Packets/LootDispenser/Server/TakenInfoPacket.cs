using System.Collections.Generic;

namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;
    using Structures;

    public class TakenInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TakenInfo;

        private readonly ulong[] _itemEntityIds;

        public TakenInfoPacket(ulong actorId, List<LootItem> lootItems)
        {
            _itemEntityIds = lootItems?.ConvertAll(item => item.EntityId).ToArray()
                             ?? System.Array.Empty<ulong>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(_itemEntityIds.Length);
            foreach (var entityId in _itemEntityIds)
            {
                pw.WriteULong(entityId);
                pw.WriteBool(true);
            }
        }
    }
}
