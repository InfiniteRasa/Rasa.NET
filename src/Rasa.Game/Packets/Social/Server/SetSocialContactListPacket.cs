using System.Collections.Generic;

namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SetSocialContactListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.SetSocialContactList;

        public List<Friend> FriendList = new List<Friend>();
        public List<IgnoredPlayer> IgnoreList = new List<IgnoredPlayer>();

        public SetSocialContactListPacket(List<Friend> friendList, List<IgnoredPlayer> ignoreList)
        {
            FriendList = friendList;
            IgnoreList = ignoreList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteList(FriendList.Count);
                foreach (var friend in FriendList)
                    pw.WriteStruct(friend);
            pw.WriteList(IgnoreList.Count);
                foreach (var ignored in IgnoreList)
                    pw.WriteStruct(ignored);
        }
    }
}
