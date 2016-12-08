namespace Rasa.Data
{
    public enum QueueOpcode
    {
        ServerKey = 0x00,
        ClientKey = 0x00,
        ClientKeyOk = 0x00,

        QueueLogin = 0x07,
        PositionInQueue = 0x0D,
        HandoffToGame = 0x0E

        // Internal opcodes, it can't be called through the socket
        //Unknown4      = 0xFC, // OnDisconnectedFromQueue
        //Unknown5      = 0xFD, // null, or in different vTable
        //Unknown6      = 0xFE, // OnConnectedToQueue
    }
}
