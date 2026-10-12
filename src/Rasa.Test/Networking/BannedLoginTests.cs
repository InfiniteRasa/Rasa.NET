using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Memory;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Protocol;

    // A world login from an account that is banned (Server.IsBanned): the client's FatalError box,
    // "Login failed: Your account has been banned.", then the LoginResponse refusal as before.
    [TestClass]
    public class BannedLoginTests
    {
        [TestMethod]
        public void TheFatalErrorComesFirstThenTheRefusal()
        {
            var sent = Client.BannedLoginRefusal();

            Assert.HasCount(2, sent);

            var call = (CallMethodMessage)sent[0];
            Assert.AreEqual((ulong)SysEntity.ClientMethodId, call.EntityId);
            var fatal = (FatalErrorPacket)call.Packet;
            Assert.AreEqual(GameOpcode.FatalError, fatal.Opcode);
            Assert.AreEqual(PlayerMessage.PmLoginFailed, fatal.MsgId);
            Assert.AreEqual(13, (int)fatal.MsgId, "PM_LOGIN_FAILED, \"Login failed: %(reason)s\"");
            Assert.AreEqual("Your account has been banned.", fatal.Args["reason"]);
            Assert.HasCount(1, fatal.Args);

            var refusal = (LoginResponseMessage)sent[1];
            Assert.AreEqual(LoginResponseMessageSubtype.Failed, refusal.Subtype);
            Assert.AreEqual(LoginErrorCodes.AccountLocked, refusal.ErrorCode);
        }

        [TestMethod]
        public void TheFatalErrorIsWrittenAsRecvFatalErrorTakesIt()
        {
            var fatal = (FatalErrorPacket)((CallMethodMessage)Client.BannedLoginRefusal().First()).Packet;

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                fatal.Write(new PythonWriter(writer));
            stream.Position = 0;
            var reader = new PythonReader(new BinaryReader(stream));

            // Recv_FatalError(msgId, args={})
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(13U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual("reason", reader.ReadString());
            Assert.AreEqual("Your account has been banned.", reader.ReadString());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
