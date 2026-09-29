namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py - SendChatMsg('RequestLeaveVoiceChannel', ()). Sent whenever the client's
    /// voice connection ends (OnVoiceServerDisconnected), fails to open (OnVoiceServerConnectFailed)
    /// or cannot be set up at all, including after the server itself closed it.
    /// </summary>
    public class RequestLeaveVoiceChannelPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLeaveVoiceChannel;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
