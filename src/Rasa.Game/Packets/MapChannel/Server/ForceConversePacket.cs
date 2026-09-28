namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Forces the conversation window open on the client with no NPC involved - client.py's
    /// Manifestation.Recv_ForceConverse reads only convoDataDict[CONVO_TYPE_GREETING] and an
    /// optional speaker name text id, then shows it exactly like an NPC greeting. Sent to the
    /// player's own entity, so no world entity has to exist for a scripted "vision" line like
    /// the bootcamp Eloh hologram to talk to the player.
    /// </summary>
    public class ForceConversePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceConverse;

        public int GreetingId { get; set; }
        public uint? NpcNameId { get; set; }

        public ForceConversePacket(int greetingId, uint? npcNameId = null)
        {
            GreetingId = greetingId;
            NpcNameId = npcNameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);

            pw.WriteDictionary(1);
            pw.WriteInt((int)ConversationType.Greeting);
            pw.WriteInt(GreetingId);

            if (NpcNameId.HasValue)
                pw.WriteUInt(NpcNameId.Value);
            else
                pw.WriteNoneStruct();
        }
    }
}
