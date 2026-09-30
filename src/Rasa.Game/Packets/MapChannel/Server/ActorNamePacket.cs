namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorNamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorName;

        public string CharacterFamily { get; set; }

        /// <summary>
        /// Sent as a Python unicode string rather than a byte string. A creature with no
        /// creaturenamelanguage entry shows its actor name as it stands (creature.py GetName),
        /// and the overhead name and vendor window titles are set with SetText, which raises
        /// "argument must be unicode" for a byte string - the name went blank and the vendor
        /// window never opened. A player's family name only ever reaches the screen combined
        /// with a unicode string, so players keep the byte string they have always had.
        /// </summary>
        public bool Unicode { get; set; }

        public ActorNamePacket(string characterFamily, bool unicode = false)
        {
            CharacterFamily = characterFamily;
            Unicode = unicode;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);

            if (Unicode)
                pw.WriteUnicodeString(CharacterFamily);
            else
                pw.WriteString(CharacterFamily);
        }
    }
}