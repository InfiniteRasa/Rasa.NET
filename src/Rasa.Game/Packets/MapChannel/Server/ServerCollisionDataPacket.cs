namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ServerCollisionData (360), a client method (sent on SysEntity.ClientMethodId): a batch of the
    /// server's collision shapes, for a developer to draw over the client's own. Wired so that it
    /// can be sent, and deliberately sent by nothing: the 1.16.5 client never asks for it and
    /// could not use it if it were sent, and this server has nothing to put in it.
    ///
    /// The client side, all of it marked "diagnostic only":
    ///  - client/world.py ToggleCollisionVisible() cycles the collision overlay (off, client,
    ///    server, both). The first time server shapes come on it posts "Requesting server
    ///    collision shapes." and sends PrivilegedCommand('getservercollisiondata', '').
    ///    GetServerCollisionData (280) is the server's name for that request; the client never
    ///    sends the opcode itself;
    ///  - client/clientmethod.py Recv_ServerCollisionData(collisionData) hands the batch to
    ///    world.InitServerCollisionData, which walks it as (entityId, skeletonData, stateData)
    ///    triples. The docstrings say (entityId, skeletonData, pos, quat), but the code unpacks
    ///    three. An entity the client does not know is created as class 1400 with collision role
    ///    5 (trigger collidee), and every entry goes to PhysicalEntity.Recv_ServerSkeleton, which
    ///    passes the serialized server physics skeleton and the transform blob to
    ///    body.SetServerSkeleton;
    ///  - an empty batch ends the transfer: "Finished receiving server collision shapes total:
    ///    ...". world.ClearServerCollision() drops the placeholder entities on a map change.
    ///
    /// Why the retail client cannot use it:
    ///  - nothing calls ToggleCollisionVisible. Only world.pyo in trpython.zip contains the name;
    ///    it was presumably bound in client_nca_internal.developerkeys, which clientmethod.py tries
    ///    to import and which is not shipped. Typing /getservercollisiondata sends the same
    ///    PrivilegedCommand (communicator.py passes any unknown slash command on), and this
    ///    server does not register it;
    ///  - tabula_rasa.exe has the overlay switches (SetCollisionVisible, ServerCollisionIsVisible
    ///    in the world's method table) but no SetServerSkeleton among the entity body's methods,
    ///    and "ServerSkeleton" is nowhere in it. A batch with any real entry raises
    ///    AttributeError on its first one;
    ///  - the skeleton is the retail server's serialized physics object, a format neither the
    ///    client nor anything else here documents.
    ///
    /// So the only batch the client can take is the empty one that ends a transfer, and that is
    /// all this writes. The C++ server never sent it either.
    /// </summary>
    public class ServerCollisionDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ServerCollisionData;

        public override void Write(PythonWriter pw)
        {
            // Recv_ServerCollisionData(collisionData): one argument, an empty tuple of entries.
            pw.WriteTuple(1);
            pw.WriteTuple(0);
        }
    }
}
