namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py Recv_VoiceChatConnectInfo(serverAddr, groupId, playerId, token). The client
    /// initialises Talkback, connects to serverAddr ("host:port", split at the colon by the exe)
    /// and, once connected, logs in with LoginToVoiceGroup(groupId, playerId, token). playerId
    /// is also g_myPlayerId, the voice id the client recognises itself by in OnBeginSpeaking.
    ///
    /// All three numbers are Python ints: the game passes them to Talkback as u32, u64 and u32,
    /// and WriteUInt of anything past 2^31 would arrive negative, which is why squad ids, account
    /// ids and tokens all stay below it.
    /// </summary>
    public class VoiceChatConnectInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.VoiceChatConnectInfo;

        internal string ServerAddress { get; }
        internal uint GroupId { get; }
        internal uint PlayerId { get; }
        internal uint Token { get; }

        internal VoiceChatConnectInfoPacket(string serverAddress, uint groupId, uint playerId, uint token)
        {
            ServerAddress = serverAddress;
            GroupId = groupId;
            PlayerId = playerId;
            Token = token;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteString(ServerAddress);
            pw.WriteUInt(GroupId);
            pw.WriteUInt(PlayerId);
            pw.WriteUInt(Token);
        }
    }
}
