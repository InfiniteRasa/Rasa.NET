using System;
using System.IO;
using System.Text;

namespace Rasa.Voice
{
    /// <summary>
    /// The wire format of Talkback, the voice chat built into tabula_rasa.exe (Talkback::ChatClient,
    /// ChatClientImpl). The game connects it with ChatClient::Connect(2, "host:port"), which opens
    /// a netUDPOutConnection: plain UDP datagrams, one Talkback message in each.
    ///
    /// Datagram (netBaseUdpConnection): an 8-byte header and up to 1004 bytes of payload.
    ///   u32 check  Adler-32 of every byte after this field (flags, reserved, payload)
    ///   u8  flags
    ///   u8  0
    ///   u16 0
    /// The client pairs a cryNullKeyExchanger with a cryPassThroughEncryptor, so the key exchange
    /// is a formality and nothing is encrypted; the Adler-32 is the only integrity check, and
    /// there are no sequence numbers, acknowledgements or resends. A lost voice packet is gone.
    ///
    /// Connection (netUdpOutConnection):
    ///   client 01  hello, 1004 zero bytes
    ///   server 07  key offer: u32 genLen (1..16), u32 primeLen (1..64), u32 pubLen (1..128),
    ///              then the generator, prime and public key bytes
    ///   client 04  its key: u32 length, the key, zero padding to 1004
    ///   server 06  key accepted
    ///   client 02  and it is connected
    /// Once connected, 00 is a Talkback message, 06 a ping the client answers with 02, 08 a close
    /// the other side acknowledges with 09.
    ///
    /// Talkback messages (ChatClientImpl; the exe also holds the encoders for the server's side,
    /// 0xa83410..0xa83ed0, which is where the server layouts below come from). The first byte is
    /// the type; fields are little-endian; a string is a u16 length and that many bytes.
    /// </summary>
    public static class Talkback
    {
        public const int HeaderSize = 8;
        public const int MaxPayload = 1004;

        #region Datagram flags

        public const byte FlagData = 0x00;
        public const byte FlagHello = 0x01;
        public const byte FlagPong = 0x02;
        public const byte FlagClientKey = 0x04;
        public const byte FlagAck = 0x06;
        public const byte FlagKeyOffer = 0x07;
        public const byte FlagClose = 0x08;
        public const byte FlagCloseAck = 0x09;

        #endregion

        #region Message types

        /// <summary>C→S: f32 version (1.3), u32 groupId, u64 playerId, u32 token, string, u8 codec, u8, string, string.</summary>
        public const byte Login = 10;

        /// <summary>C→S: u8 frameCount, u16 seq, audio (the rest). Codecs whose frames are all one size.</summary>
        public const byte VoiceFixed = 11;

        /// <summary>C→S: u8 localId. Push-to-talk released.</summary>
        public const byte StopTalking = 12;

        /// <summary>C→S: u8 localId, u16 counter. Push-to-talk pressed.</summary>
        public const byte StartTalking = 13;

        /// <summary>C→S: u8 localId. Sent every few seconds while connected.</summary>
        public const byte Heartbeat = 14;

        /// <summary>C→S: u8 frameCount, u16 seq, frameCount × u8 frame size, u16 length, audio. Variable-size frames (Speex).</summary>
        public const byte VoiceVariable = 15;

        /// <summary>C→S: nothing else. The client is leaving the group.</summary>
        public const byte Logout = 16;

        /// <summary>
        /// S→C: u8 result, u32 groupId, u8 localId, u8 maxMembers, u8 maxTalkers, u8 codec,
        /// f32 maxTalkTime, f32 talkTimeRegen, u8 mode, string, u8 canTalk. Result 0 is success,
        /// then 1 incorrect version, 2 unable to verify player information, 3 bad message type,
        /// 4 unable to join group. maxMembers sizes the client's per-member tables, so every localId
        /// has to be below it. Mode 4 is peer-to-peer (clients send voice to each other on port 8841);
        /// anything else relays through the server. canTalk 0 leaves the microphone off.
        /// </summary>
        public const byte LoginReply = 20;

        /// <summary>S→C: u8 speaker localId, then the VoiceFixed body.</summary>
        public const byte RelayFixed = 21;

        /// <summary>S→C: u32 groupId, u64 playerId, u8 localId, u8 speaking, u32 address, u32 port, string, u8.</summary>
        public const byte PlayerAdded = 22;

        /// <summary>S→C: u32 groupId, u64 playerId, u8 localId.</summary>
        public const byte PlayerRemoved = 23;

        /// <summary>S→C: u8 localId, f32 seconds talked, u16 seq. OnEndSpeaking.</summary>
        public const byte EndSpeaking = 24;

        /// <summary>S→C: u8 localId. OnBeginSpeaking.</summary>
        public const byte BeginSpeaking = 25;

        /// <summary>S→C: u8 speaker localId, then the VoiceVariable body.</summary>
        public const byte RelayVariable = 27;

        #endregion

        public const byte ResultSuccess = 0;
        public const byte ResultIncorrectVersion = 1;
        public const byte ResultUnableToVerify = 2;
        public const byte ResultBadMessageType = 3;
        public const byte ResultUnableToJoin = 4;

        /// <summary>The version the 1.16.5.0 client logs in with (float at 0xbc4d8c).</summary>
        public const float ClientVersion = 1.3f;

        #region Datagrams

        public static uint Adler32(ReadOnlySpan<byte> data)
        {
            const uint mod = 65521;
            uint a = 1, b = 0;

            foreach (var x in data)
            {
                a = (a + x) % mod;
                b = (b + a) % mod;
            }

            return (b << 16) | a;
        }

        /// <summary>One datagram: header, flags and payload.</summary>
        public static byte[] Datagram(byte flags, ReadOnlySpan<byte> payload)
        {
            if (payload.Length > MaxPayload)
                throw new ArgumentException($"Talkback payload of {payload.Length} bytes; the most is {MaxPayload}.");

            var buffer = new byte[HeaderSize + payload.Length];

            buffer[4] = flags;
            payload.CopyTo(buffer.AsSpan(HeaderSize));

            var check = Adler32(buffer.AsSpan(4));

            buffer[0] = (byte)check;
            buffer[1] = (byte)(check >> 8);
            buffer[2] = (byte)(check >> 16);
            buffer[3] = (byte)(check >> 24);

            return buffer;
        }

        /// <summary>Checks and splits a received datagram. False for anything short or corrupt.</summary>
        public static bool TryParse(ReadOnlySpan<byte> datagram, out byte flags, out ReadOnlySpan<byte> payload)
        {
            flags = 0;
            payload = default;

            if (datagram.Length < HeaderSize || datagram.Length > HeaderSize + MaxPayload)
                return false;

            var check = (uint)(datagram[0] | datagram[1] << 8 | datagram[2] << 16 | datagram[3] << 24);

            if (check != Adler32(datagram.Slice(4)))
                return false;

            flags = datagram[4];
            payload = datagram.Slice(HeaderSize);

            return true;
        }

        /// <summary>
        /// The server's half of the key exchange. The client's cryNullKeyExchanger takes any
        /// generator and prime and answers with a fixed key; these are the queue's values.
        /// </summary>
        public static byte[] KeyOffer()
        {
            var generator = Encoding.ASCII.GetBytes("A");
            var prime = Encoding.ASCII.GetBytes("FOOBAR");
            var publicKey = Encoding.ASCII.GetBytes("PUBKEY12");

            var w = new MessageWriter();

            w.U32((uint)generator.Length);
            w.U32((uint)prime.Length);
            w.U32((uint)publicKey.Length);
            w.Bytes(generator);
            w.Bytes(prime);
            w.Bytes(publicKey);

            return w.ToArray();
        }

        #endregion

        #region Server messages

        public static byte[] LoginReplyMessage(byte result, uint groupId, byte localId, byte maxMembers, byte maxTalkers,
            byte codec, float maxTalkTime, float talkTimeRegen, bool canTalk)
        {
            var w = new MessageWriter();

            w.U8(LoginReply);
            w.U8(result);
            w.U32(groupId);
            w.U8(localId);
            w.U8(maxMembers);
            w.U8(maxTalkers);
            w.U8(codec);
            w.F32(maxTalkTime);
            w.F32(talkTimeRegen);
            w.U8(0);        // relayed through the server, never peer-to-peer
            w.String("");
            w.U8(canTalk ? (byte)1 : (byte)0);

            return w.ToArray();
        }

        public static byte[] PlayerAddedMessage(uint groupId, ulong playerId, byte localId, bool speaking)
        {
            var w = new MessageWriter();

            w.U8(PlayerAdded);
            w.U32(groupId);
            w.U64(playerId);
            w.U8(localId);
            w.U8(speaking ? (byte)1 : (byte)0);
            w.U32(0);       // address and port: peer-to-peer mode only
            w.U32(0);
            w.String("");
            w.U8(0);

            return w.ToArray();
        }

        public static byte[] PlayerRemovedMessage(uint groupId, ulong playerId, byte localId)
        {
            var w = new MessageWriter();

            w.U8(PlayerRemoved);
            w.U32(groupId);
            w.U64(playerId);
            w.U8(localId);

            return w.ToArray();
        }

        public static byte[] BeginSpeakingMessage(byte localId) => new[] { BeginSpeaking, localId };

        public static byte[] EndSpeakingMessage(byte localId, float secondsTalked, ushort seq)
        {
            var w = new MessageWriter();

            w.U8(EndSpeaking);
            w.U8(localId);
            w.F32(secondsTalked);
            w.U16(seq);

            return w.ToArray();
        }

        /// <summary>
        /// A client's voice message as the rest of the group gets it: 11 becomes 21 and 15 becomes 27,
        /// with the speaker's localId after the type. The audio is passed on untouched - the server
        /// never decodes it, so it does not matter which codec the clients agreed on.
        /// </summary>
        public static byte[] Relay(ReadOnlySpan<byte> voice, byte speakerLocalId)
        {
            var relay = new byte[voice.Length + 1];

            relay[0] = voice[0] == VoiceFixed ? RelayFixed : RelayVariable;
            relay[1] = speakerLocalId;
            voice.Slice(1).CopyTo(relay.AsSpan(2));

            return relay;
        }

        #endregion

        #region Client messages

        public sealed class LoginRequest
        {
            public float Version;
            public uint GroupId;
            public ulong PlayerId;
            public uint Token;
            public byte Codec;
        }

        public static bool TryReadLogin(ReadOnlySpan<byte> message, out LoginRequest login)
        {
            login = null;

            var r = new MessageReader(message);

            if (!r.U8(out var type) || type != Login)
                return false;

            var request = new LoginRequest();

            if (!r.F32(out request.Version) || !r.U32(out request.GroupId) || !r.U64(out request.PlayerId) || !r.U32(out request.Token)
                || !r.SkipString() || !r.U8(out request.Codec))
                return false;

            // A u8 and two strings follow that nothing here needs.
            login = request;

            return true;
        }

        /// <summary>Whether a voice message is complete enough to pass on: its declared frames and audio are all there.</summary>
        public static bool IsWellFormedVoice(ReadOnlySpan<byte> message)
        {
            if (message.Length < 4)
                return false;

            if (message[0] == VoiceFixed)
                return true;

            if (message[0] != VoiceVariable)
                return false;

            var frames = message[1];
            var lengthAt = 4 + frames;

            if (message.Length < lengthAt + 2)
                return false;

            var length = message[lengthAt] | message[lengthAt + 1] << 8;

            return message.Length >= lengthAt + 2 + length;
        }

        #endregion

        public sealed class MessageWriter
        {
            private readonly MemoryStream _stream = new MemoryStream();
            private readonly BinaryWriter _writer;

            public MessageWriter()
            {
                _writer = new BinaryWriter(_stream);
            }

            public void U8(byte v) => _writer.Write(v);
            public void U16(ushort v) => _writer.Write(v);
            public void U32(uint v) => _writer.Write(v);
            public void U64(ulong v) => _writer.Write(v);
            public void F32(float v) => _writer.Write(v);
            public void Bytes(byte[] v) => _writer.Write(v);

            public void String(string v)
            {
                var bytes = Encoding.ASCII.GetBytes(v ?? "");

                _writer.Write((ushort)bytes.Length);
                _writer.Write(bytes);
            }

            public byte[] ToArray()
            {
                _writer.Flush();
                return _stream.ToArray();
            }
        }

        public ref struct MessageReader
        {
            private readonly ReadOnlySpan<byte> _data;
            private int _at;

            public MessageReader(ReadOnlySpan<byte> data)
            {
                _data = data;
                _at = 0;
            }

            public int Remaining => _data.Length - _at;

            private bool Take(int count, out ReadOnlySpan<byte> bytes)
            {
                if (Remaining < count)
                {
                    bytes = default;
                    return false;
                }

                bytes = _data.Slice(_at, count);
                _at += count;

                return true;
            }

            public bool U8(out byte v)
            {
                v = 0;

                if (!Take(1, out var b))
                    return false;

                v = b[0];
                return true;
            }

            public bool U16(out ushort v)
            {
                v = 0;

                if (!Take(2, out var b))
                    return false;

                v = (ushort)(b[0] | b[1] << 8);
                return true;
            }

            public bool U32(out uint v)
            {
                v = 0;

                if (!Take(4, out var b))
                    return false;

                v = (uint)(b[0] | b[1] << 8 | b[2] << 16 | b[3] << 24);
                return true;
            }

            public bool U64(out ulong v)
            {
                v = 0;

                if (!U32(out var lo) || !U32(out var hi))
                    return false;

                v = lo | (ulong)hi << 32;
                return true;
            }

            public bool F32(out float v)
            {
                v = 0;

                if (!U32(out var bits))
                    return false;

                v = BitConverter.Int32BitsToSingle((int)bits);
                return true;
            }

            public bool SkipString()
            {
                return U16(out var length) && length <= 1024 && Take(length, out _);
            }
        }
    }
}
