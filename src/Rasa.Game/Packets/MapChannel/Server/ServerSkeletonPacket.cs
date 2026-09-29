namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ServerSkeleton (361), on an entity: the server's physics skeleton for it, for a developer to
    /// compare with the client's own collision. The answer to GetServerSkeleton (281). Wired so
    /// that it can be sent, and deliberately sent by nothing: the 1.16.5 client never asks for it
    /// and cannot load it, and this server has no skeletons to send.
    ///
    /// client/physicalentity.py Recv_ServerSkeleton(skeletonData, stateData) takes two raw
    /// strings - the server's serialized physics skeleton and a transform blob. With data it takes
    /// the entity out of the world, sets hasServerSkeleton (which nothing reads), passes both to
    /// body.SetServerSkeleton and puts the entity back; with an empty skeleton it does nothing.
    /// ServerCollisionData runs every entry of its batches through the same method (see
    /// <see cref="ServerCollisionDataPacket"/>).
    ///
    /// tabula_rasa.exe's entity body has no SetServerSkeleton - "ServerSkeleton" is nowhere in the
    /// exe; its phySkeleton loader is for the client's own skeleton files - so a skeleton with any
    /// data raises AttributeError after the entity has been taken out of the world, and leaves it
    /// out. The format is the retail server's physics serialization, which nothing here documents.
    /// So the only form the client can take is the empty one, and that is all this writes.
    ///
    /// The C++ server only listed the id.
    /// </summary>
    public class ServerSkeletonPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ServerSkeleton;

        public override void Write(PythonWriter pw)
        {
            // Recv_ServerSkeleton(skeletonData, stateData): two empty strings, which it ignores.
            pw.WriteTuple(2);
            pw.WriteString("");
            pw.WriteString("");
        }
    }
}
