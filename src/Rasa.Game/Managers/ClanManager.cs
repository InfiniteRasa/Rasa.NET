namespace Rasa.Managers
{
    using Game;
    using Packets.MapChannel.Client;

    public class ClanManager
    {
        /*   Clan Packets:
         * - DisplayClanMessage
         * - InviteToClan
         * - SetClanData
         * - SetClanMemberData
         * - ClanMembersRosterBegin
         * - ClanMembersRosterEnd
         *  - PlayerJoinedClan
         *  - PlayerLeftClan
         *  - ClanDisbanded
         *  - ClanDeleted
         *  - DisplayClanLeaderInfo
         *  - DisplayClanMemberInfoHeader
         *  - DisplayClanMemberInfo
         *  - ClanCreated
         *  - GetPvPClanStatus
         *  
         *    Clan Handlers:
         *  - GetPvPClanMembershipStatus
         *  - CreateClan
         */

        private static ClanManager _instance;
        private static readonly object InstanceLock = new object();
        public static ClanManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ClanManager();
                    }
                }

                return _instance;
            }
        }

        private ClanManager()
        {
        }

        #region Handlers
        public void GetPvPClanMembershipStatus(Client client)
        {
            // ToDo
        }

        public void CreateClan(Client client, CreateClanPacket packet)
        {
            // ToDo
        }

        #endregion

        #region Helper Functions
        #endregion
    }
}
