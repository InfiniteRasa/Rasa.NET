namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// A map's name on the sub-region strip: <c>(contextID,)</c>.
    ///
    /// client/clientmethod.py's Recv_DisplayDestinationContextNotification looks the context up
    /// with clientlanguagemanager.GetGameContextName and posts it as UI_DISPLAY_SUB_REGION, the
    /// smaller line of bigtextwindow under the region name - where DisplayPlayerNotification of
    /// type DESTINATION puts a player message. It carries no text of its own: what shows is the
    /// localized name of a game context, or its class name with "(Needs localized name)" for one
    /// that has none. For an id the client's gamecontext table does not have, GetGameContextName
    /// leaves its result unassigned and the handler throws, so nothing shows.
    /// </summary>
    public class DisplayDestinationContextNotificationPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayDestinationContextNotification;

        public uint ContextId { get; set; }

        public DisplayDestinationContextNotificationPacket(uint contextId)
        {
            ContextId = contextId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ContextId);
        }
    }
}
