namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A slash command the client does not handle itself: <c>(command, arg)</c>.
    ///
    /// client/communicator.py's ProcessSlashCommand sends what it has no local handler for as
    /// unicode, the arg u'' when there is none. The client's own pickers send byte strings:
    /// inputstate/gotomap.py ('gotomap', '&lt;mapId&gt; &lt;startGroup&gt;'), killmap.py, selectmap.py,
    /// clientmethod.py's givemission and world.py's getservercollisiondata. Either reads as a
    /// string, None as null; reading only unicode threw on the byte strings, and a throw while
    /// reading a packet closes the connection.
    /// </summary>
    public class PrivilegedCommandPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PrivilegedCommand;

        public string Command { get; set; }
        public string Args { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Command = ReadText(pr);
            Args = ReadText(pr);
        }

        /// <summary>A unicode or byte string, or null for None or anything else (which is skipped).</summary>
        public static string ReadText(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.UnicodeString:
                    return pr.ReadUnicodeString();

                case PythonType.String:
                    return pr.ReadString();

                default:
                    pr.SkipValue();
                    return null;
            }
        }
    }
}
