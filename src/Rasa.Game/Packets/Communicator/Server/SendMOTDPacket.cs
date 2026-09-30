using System.Collections.Generic;

namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SendMOTD (the message of the day): client/communicator.py Recv_SendMOTD(MOTDDict) takes a
    /// dictionary of languageId -> text (generated.client.languageidentifiers: 1 English, 5 French,
    /// 6 German, ...) and shows the entry for the client's language, else the English one, else
    /// nothing. This wrote a bare string, which the client's MOTDDict.keys() would have thrown on;
    /// nothing sends it today (the message of the day goes out as PreviewMOTD).
    /// </summary>
    public class SendMOTDPacket : ServerPythonPacket
    {
        public const int English = 1;

        public override GameOpcode Opcode { get; } = GameOpcode.SendMOTD;

        /// <summary>The message, by client language id.</summary>
        public Dictionary<int, string> Messages { get; }

        /// <summary>One message for every client: filed as English, which every client falls back to.</summary>
        public SendMOTDPacket(string motd)
        {
            Messages = new Dictionary<int, string> { [English] = motd ?? "" };
        }

        public SendMOTDPacket(Dictionary<int, string> messages)
        {
            Messages = messages ?? new Dictionary<int, string>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(Messages.Count);

            foreach (var (languageId, text) in Messages)
            {
                pw.WriteInt(languageId);
                pw.WriteUnicodeString(text ?? "");
            }
        }
    }
}
