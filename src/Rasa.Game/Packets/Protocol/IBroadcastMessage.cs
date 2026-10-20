namespace Rasa.Packets.Protocol
{
    /// <summary>
    /// A message that can be handed to several clients as the one object: a broadcast. What a
    /// frame carries after its channel header - the message, compressed or not - depends on the
    /// message alone, the channel's sequence number being in the header and the encryption being
    /// applied afterwards to each client's copy. So once more than one client has been handed the
    /// message (Client.SendMessage counts them), the first of them to be sent it encodes it and
    /// the rest are sent the same bytes (ProtocolPacket.Write).
    ///
    /// Every client used to encode it afresh: a hit, an effect or a move among forty players was
    /// marshalled forty times, each time the same.
    ///
    /// Once encoded, the message is not handed to anyone new (PythonPacket.MessageFor): the
    /// packet may have changed since, and whoever is handed it later is sent it as it is then.
    /// </summary>
    internal interface IBroadcastMessage
    {
        /// <summary>How many clients have been handed the message.</summary>
        int Recipients { get; }

        void AddRecipient();

        /// <summary>The frame after its header, once the message has gone to the first of several.</summary>
        byte[] Encoded { get; set; }
    }
}
