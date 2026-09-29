using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// UpdatePhysicalEntity (381), a client method (sent on SysEntity.ClientMethodId): a batch of
    /// entity methods for an entity the client already has. Wired so that it can be sent, and
    /// deliberately sent by nothing: <see cref="CreatePhysicalEntityPacket"/> does the same and
    /// more, and a live entity is changed here with its own methods, one CallMethod each.
    ///
    /// client/clientmethod.py Recv_UpdatePhysicalEntity(entityId, entityData):
    ///  - looks the entity up; one the client does not have is not created - it logs "Entity %s
    ///    not found or created yet!" and does nothing;
    ///  - takes the entity out of the world, if it is in it;
    ///  - runs the batch: each (methodId or name, args) becomes Recv_&lt;name&gt;(*args) on the
    ///    entity, the list CreatePhysicalEntity carries, written the same way. An entry whose
    ///    method the entity lacks is skipped; one that throws aborts the rest.
    ///
    /// Recv_CreatePhysicalEntity for an entity the client already has, of the same class, calls
    /// Recv_UpdatePhysicalEntity itself (a different class is destroyed and made anew). So a
    /// repeated Create is this, and also works when the entity is missing - which is why
    /// LootDispenserManager.RequestCorpseLooting re-sends Create for loot items the client may
    /// already hold.
    ///
    /// The catch: only Recv_WorldLocationDescriptor and Recv_WorldPlacementDescriptor put an
    /// entity back in the world. An update of a world entity - creature, object, player - that
    /// carries neither leaves it removed and invisible, an empty or None batch included. An
    /// inventory item is never in the world, so it is safe either way. Even with a location,
    /// every update runs the entity's remove and add callbacks (a usable's state effects
    /// restart), which separate CallMethods do not.
    ///
    /// Nothing in the retail client sends or calls it but its own Create path: only
    /// clientmethod.pyo in trpython.zip has the name, and tabula_rasa.exe does not. The C++
    /// server only listed the id.
    /// </summary>
    public class UpdatePhysicalEntityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdatePhysicalEntity;

        public ulong EntityId { get; set; }
        public List<PythonPacket> EntityData { get; } = new List<PythonPacket>();

        public UpdatePhysicalEntityPacket(ulong entityId, List<PythonPacket> entityData)
        {
            EntityId = entityId;
            EntityData = entityData ?? new List<PythonPacket>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(EntityId);

            // As CreatePhysicalEntity writes it: a list of (methodId, args), or None for no methods.
            // The client treats None as an empty batch - which still takes the entity out of the world.
            if (EntityData.Count > 0)
            {
                pw.WriteList(EntityData.Count);

                foreach (var packet in EntityData)
                {
                    pw.WriteTuple(2);
                    pw.WriteInt((int)packet.Opcode);

                    packet.Write(pw);
                }
            }
            else
                pw.WriteNoneStruct();
        }
    }
}
