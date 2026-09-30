namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// WorldPlacementDescriptor (244), on a physical entity: puts it in the world plugged into a
    /// socket of another entity, where WorldLocationDescriptor (243) puts it at a position. The
    /// placing half of apartment decorating, which was cut: the answer RequestPlaceObject (344)
    /// would have had is, by its arguments, the item created with this. Wired so that it can be
    /// sent, and deliberately sent by nothing; see ClientPacketHandler.RequestReturnItemToInventory
    /// for the rest of decorating and why none of it can be reached.
    ///
    /// client/physicalentity.py Recv_WorldPlacementDescriptor(plugId, destEntityId, socketId):
    ///  - looks destEntityId up and calls body.AttachToEntity(plugId, dest.body, socketId);
    ///  - adds the entity to the world, as WorldLocationDescriptor does - these two are the only
    ///    methods that do, which is why an UpdatePhysicalEntity has to carry one of them;
    ///  - posts OBJECT_PLACED_ON_OBJECT, which nothing handles.
    /// A destEntityId the client does not have throws on dest.body before the add, and leaves the
    /// entity out of the world.
    ///
    /// The ids are the bodies' own: decoration.py takes an item's first plug from
    /// body.GetDecorationPointPlugs() and the sockets from GetDecorationPointSockets(), both as
    /// (trId, palId), and places by the pal ids. They come from the meshes, which this server does
    /// not have; RequestPlaceObject carries only the socket, the plug being the item's own. The
    /// destinations are the static map entities gamemap.py registers when a body has sockets,
    /// with ids the client gives them as it loads the .map. The client itself calls this to put
    /// its half-transparent "put it here?" proxies on every free socket an item fits
    /// (Decoration.OnDesignateCurrentItem).
    ///
    /// tabula_rasa.exe has the natives - AttachToEntity, DetachFromEntity, Has/GetDecorationPoint-
    /// Plugs and -Sockets, GetAttachedAtEntityId, GetAttachedToEntityId, GetCompatiblePlug - so a
    /// placement with real ids would work. The C++ server only listed the id.
    /// </summary>
    public class WorldPlacementDescriptorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WorldPlacementDescriptor;

        /// <summary>The placed entity's plug: its body's pal plug id.</summary>
        public int PlugId { get; set; }

        /// <summary>The entity whose socket it goes in; the client must have it.</summary>
        public ulong DestEntityId { get; set; }

        /// <summary>The destination's pal socket id.</summary>
        public int SocketId { get; set; }

        public WorldPlacementDescriptorPacket(int plugId, ulong destEntityId, int socketId)
        {
            PlugId = plugId;
            DestEntityId = destEntityId;
            SocketId = socketId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(PlugId);
            pw.WriteULong(DestEntityId);
            pw.WriteInt(SocketId);
        }
    }
}
