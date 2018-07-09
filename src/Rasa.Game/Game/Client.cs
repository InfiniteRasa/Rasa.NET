using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;

namespace Rasa.Game
{
    using Cryptography;
    using Data;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Structures;

    public class Client : INetworkClient
    {
        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public ClientCryptData Data { get; }
        public Server Server { get; }
        public LoginAccountEntry Entry { get; private set; }
        public ClientState State { get; private set; }
        public int LoadingMap { get; set; }
        public uint LoadingSlot { get; set; }
        public MapChannelClient MapClient { get; set; }
        public List<UserOptions> UserOptions = new List<UserOptions>();
        private readonly ClientPacketHandler _handler;

        private static PacketRouter<ClientPacketHandler, GameOpcode> PacketRouter { get; } = new PacketRouter<ClientPacketHandler, GameOpcode>();

        public Client(LengthedSocket socket, ClientCryptData data, Server server)
        {
            _handler = new ClientPacketHandler(this);

            Socket = socket;
            Data = data;
            Server = server;

            State = ClientState.Connected;

            Socket.OnError += OnError;
            Socket.OnReceive += OnReceive;
            Socket.OnEncrypt += OnEncrypt;
            Socket.OnDecrypt += OnDecrypt;

            Socket.ReceiveAsync();

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
        }

        public void Close(bool sendPacket = true)
        {
            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void SendPacket(IBasePacket packet)
        {
            if (!(packet is PythonCallPacket))
                Debugger.Break(); // todo: handle outgoing queue packet sending from server (like in auth) (todo: maybe a delegate instead of an interface?)

            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            var authPacket = packet as PythonPacket;
            if (authPacket == null)
                return;

            PacketRouter.RoutePacket(_handler, authPacket);
        }

        #region Socketing
        private void OnEncrypt(BufferData data, ref int length)
        {
            GameCryptManager.Instance.Encrypt(data.Buffer, data.RealOffset, ref length, data.RemainingLength, Data);
        }

        private bool OnDecrypt(BufferData data)
        {
            return GameCryptManager.Instance.Decrypt(data.Buffer, data.RealOffset, data.Length, Data);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close(false);
        }

        private void OnReceive(BufferData data)
        {
            // TODO: find in the client and rethink subsize calculation
            var align = data[data.Offset] % 9;
            var size = data.Length - align;

            data.Offset += align;

            do
            {
                var subsize = data[data.Offset] | (data[data.Offset + 1] << 8);
                if (subsize == 43 && size == 12)
                    return;

                if (subsize > 4000)
                {
                    var zeroFound = 0;
                    var i = 0;

                    for (; i < subsize; ++i)
                    {
                        if (data[data.Offset + i] == 0 && data[data.Offset + i + 1] == 0)
                            ++zeroFound;

                        if (zeroFound != 2)
                            continue;

                        if (i - 2 < 0)
                            Console.WriteLine("FindSubsize::HOLYSHIT !!!");

                        subsize = i - 2;
                    }

                    subsize = i;
                }

                if (!DecodePacket(data, subsize))
                    return;

                data.Offset += subsize;
                size -= subsize;
            }
            while (size > 0);
        }

        private bool DecodePacket(BufferData data, int length)
        {
            var packet = new PythonCallPacket(length);
            using (var br = data.GetReader())
            {
                packet.Read(br);

                if (packet.Return.HasValue)
                    return packet.Return.Value;

                if (packet.Type == 2)
                {
                    State = ClientState.LoggedIn;
                    Entry = Server.AuthenticateClient(this, packet.AccountId, packet.OneTimeKey);
                    if (Entry == null)
                    {
                        Logger.WriteLog(LogType.Error, "Client with ip: {0} tried to log in with invalid session data! User Id: {1} | OneTimeKey: {2}", Socket.RemoteAddress, packet.AccountId, packet.OneTimeKey);
                        Close(false);
                        return false;
                    }

                    CharacterManager.Instance.StartCharacterSelection(this);
                    return true;
                }

                if (packet.DataSize > 0 && br.BaseStream.Position + packet.DataSize < br.BaseStream.Length)
                {
                    if (br.ReadByte() != 0x4F) // 'O' format
                        throw new Exception("Unsupported serialization format!");

                    var packetType = PacketRouter.GetPacketType(packet.Opcode);
                    if (packetType != null)
                    {
                        var pythonPacket = Activator.CreateInstance(packetType) as IBasePacket;
                        if (pythonPacket == null)
                            return false;

                        pythonPacket.Read(br);

                        Server.PacketQueue.EnqueueIncoming(this, pythonPacket);
                    }
                    else
                        Logger.WriteLog(LogType.Error, $"Unhandled game opcode:\n{packet.Opcode}\n");
                }
                else
                    Logger.WriteLog(LogType.Error, $"Invalid data found in Python method call! Off: {br.BaseStream.Position} | Len: {packet.DataSize} | Array len: {br.BaseStream.Length}");
            }

            return true;
        }

        // Cell Domain
        public void CellSendPacket(Client client, uint entityId, PythonPacket packet)
        {
            var mapCell = CellManager.Instance.GetCell(client.MapClient.MapChannel, client.MapClient.Player.Actor.CellLocation.CellPosX, client.MapClient.Player.Actor.CellLocation.CellPosY);
            var clientCount = mapCell.ClientNotifyList.Count;
            var clientList = mapCell.ClientNotifyList;

            if (clientList == null  || clientCount == 0)
                return;

            foreach (var tempClient in clientList)
                tempClient.SendPacket(entityId, packet);
        }

        // Cell Domain ignore self
        public void CellIgnoreSelfSendPacket(Client client, PythonPacket packet)
        {
            var mapCell = CellManager.Instance.GetCell(client.MapClient.MapChannel, client.MapClient.Player.Actor.CellLocation.CellPosX, client.MapClient.Player.Actor.CellLocation.CellPosY);
            var clientCount = mapCell.ClientNotifyList.Count;
            var clientList = mapCell.ClientNotifyList;

            if (clientList == null || clientCount == 0)
                return;

            foreach (var tempClient in clientList)
            {
                if (tempClient == client)
                    return;

                tempClient.SendPacket(client.MapClient.Player.Actor.EntityId, packet);
            }
        }

        /* MapChannel
         * we will see, will we neeed this, it's same as CellSendPacket
        public void MapChannelSendPacket(MapChannel mapChannel, Actor actor, uint entityId, PythonPacket packet)
        {
            var mapCell = CellManager.Instance.GetCell(mapChannel, actor.CellLocation.CellPosX, actor.CellLocation.CellPosY);
            var playerCount = mapCell.PlayerNotifyList.Count;
            var playerList = mapCell.PlayerNotifyList;

            if (playerList == null || playerCount == 0)
                return;

            foreach (var t in playerList)
                t.Player.Client.SendPacket(entityId, packet);
        }*/

        // Client
        public void SendPacket(uint entityId, PythonPacket packet)
        {
            SendPacket(new PythonCallPacket(packet, entityId));
        }

        // Clients
        public void SendPackets(List<Client> clientList, uint entityId, PythonPacket packet)
        {
            foreach (var client in clientList)
                client.SendPacket(entityId, packet);
        }
        
        // netMgr_sendEntityMovement    // ToDo
        public void SendMovement(uint entityId, NetCompressedMovement movement)
        {
            SendPacket(new MovementCallPacket(entityId, movement));
        }
        #endregion
    }
}
