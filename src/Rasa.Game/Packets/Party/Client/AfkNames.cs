namespace Rasa.Packets.Party.Client
{
    /// <summary>
    /// The name a squad request dialog sends back, as the server knows it.
    ///
    /// The client shows an AFK player's name with the "(AFK)" element appended (commonuiutil.py
    /// GetAfkUserName: the name, then uielement 4648, with nothing between), and the revoke
    /// dialog it opens from the pending-request indicator sends that shown name back in
    /// CancelSquadInviteRequest and CancelSquadJoinRequest - so a request sent to someone who
    /// was AFK came back as "Name(AFK)", which matched no request on the server. The element is
    /// "(AFK)" in English and German, "(Absent)" in French and "(離席中)" in Japanese; a family
    /// name itself (RequestCreateCharacterInSlotPacket.ValidateName) has no parentheses, so a
    /// trailing parenthesised group is always the client's decoration.
    /// </summary>
    internal static class AfkNames
    {
        /// <summary>The name without a trailing "(...)" decoration and the whitespace around it; null stays null.</summary>
        public static string Strip(string shown)
        {
            if (shown == null)
                return null;

            var name = shown.TrimEnd();

            if (name.EndsWith(')'))
            {
                var open = name.LastIndexOf('(');

                if (open >= 0)
                    name = name.Substring(0, open);
            }

            return name.Trim();
        }
    }
}
