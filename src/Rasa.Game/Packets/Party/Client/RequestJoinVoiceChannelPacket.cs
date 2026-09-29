namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py _RequestJoinVoiceChannel - SendChatMsg('RequestJoinVoiceChannel', ()). Sent
    /// when the squad state says VoiceChatAvailable(true), or when the player turns voice back on
    /// in the options, unless the client is already connected. Answered with VoiceChatConnectInfo.
    /// </summary>
    public class RequestJoinVoiceChannelPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestJoinVoiceChannel;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
