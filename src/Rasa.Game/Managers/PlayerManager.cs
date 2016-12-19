namespace Rasa.Managers
{
    using Structures;
    using Packets.MapChannel.Server;

    public class PlayerManager
    {
        public static void AssignPlayer(MapChannel mapChannel, MapChannelClient owner, PlayerData player)
        {
            owner.Client.SendPacket(5, new SetControlledActorIdPacket { EntetyId = owner.Player.Actor.EntityId});

            owner.Client.SendPacket(7, new SetSkyTimePacket {RunningTime = 6666666});   // ToDo add actual time how long map is running

            owner.Client.SendPacket(5, new SetCurrentContextIdPacket {MapContextId = 1220});    // ToDo

            // ToDo To enter world we need only send those 3 packet's, later we will inplement others

            /*owner.Client.SendPacket(owner.Player.Actor.EntityId, new UpdateRegionsPacket { RegionIdList = 0});        // ToDo

            owner.Client.SendPacket(owner.Player.Actor.EntityId, new AllCreditsPacket { Credits = 0, Prestige = 0});  // ToDo

            owner.Client.SendPacket(owner.Player.Actor.EntityId, new AdvancementStatsPacket                           // ToDo
                {
                Level = 1,
                Experience = 0,
                Attributes = 4,
                TrainPts = 0,       // trainPoints (are not used by the client??)
                SkillPts = 4
            } );
            
            owner.Client.SendPacket(owner.Player.Actor.EntityId, new SkillsPacket());        // ToDo

            owner.Client.SendPacket(owner.Player.Actor.EntityId, new AbilitiesPacket());   // ToDo

            owner.Client.SendPacket(owner.Player.Actor.EntityId, new AbilityDrawerPacket {AbilityId = 0, PumpLevel = 0});    //ToDo*/
        }
    }
}
