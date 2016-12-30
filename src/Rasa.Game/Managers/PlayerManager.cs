using System;

namespace Rasa.Managers
{
    using Structures;
    using Packets.MapChannel.Server;
    using Packets.Game.Server;

    public class PlayerManager
    {
        public static void AssignPlayer(MapChannel mapChannel, MapChannelClient mapClient)
        {
            var player = mapClient.Player;
            var actor = mapClient.Player.Actor;
            player.Client.SendPacket(5, new SetControlledActorIdPacket { EntetyId = actor.EntityId});

            player.Client.SendPacket(7, new SetSkyTimePacket {RunningTime = 6666666});   // ToDo add actual time how long map is running

            player.Client.SendPacket(5, new SetCurrentContextIdPacket {MapContextId = actor.MapContextId});

            player.Client.SendPacket(actor.EntityId, new UpdateRegionsPacket { RegionIdList = mapClient.MapChannel.MapInfo.BaseRegionId});  // ToDo this should be some list of regions

            player.Client.SendPacket(actor.EntityId, new AllCreditsPacket { Credits = player.Credits, Prestige = player.Prestige});

            player.Client.SendPacket(actor.EntityId, new AdvancementStatsPacket                           // ToDo
                {
                Level = actor.Stats.Level,
                Experience = player.Experience,
                Attributes = 4,
                TrainPts = 0,       // trainPoints (are not used by the client??)
                SkillPts = 5
            } );

            player.Client.SendPacket(actor.EntityId, new SkillsPacket());        // ToDo

            player.Client.SendPacket(actor.EntityId, new AbilitiesPacket());   // ToDo

            player.Client.SendPacket(actor.EntityId, new AbilityDrawerPacket {AbilityId = 0, PumpLevel = 0});    // ToDo
        }

        public static void CellDiscardClientToPlayers(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].ClientEntityId == client.ClientEntityId)
                    continue;
                client.Client.SendPacket(5, new DestroyPhysicalEntityPacket{ EntityID = client.Player.Actor.EntityId });

            }
        }

        public static void CellDiscardPlayersToClient(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].Player == null)
                    continue;
                if (mapChannel.PlayerList[i].ClientEntityId == client.ClientEntityId)
                    continue;
                client.Client.SendPacket(5, new DestroyPhysicalEntityPacket { EntityID = mapChannel.PlayerList[i].Player.Actor.EntityId });
            }

        }

        public static void CellIntroduceClientToPlayers(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(5, new CreatePyhsicalEntityPacket( (int)client.Player.Actor.EntityId, (int)client.Player.Actor.EntityClassId));
            }

            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new AttributeInfoPacket());  // ToDo
            }
            
            // PreloadData
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new PreloadDataPacket());
            }
            // Recv_AppearanceData
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new AppearanceDataPacket{ AppearanceData = client.Player.AppearanceData});
            }
            // set controller (Recv_ActorControllerInfo )
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new ActorControllerInfoPacket{ IsPlayer = true });
            }
            // set level
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new LevelPacket{ Level = client.Player.Actor.Stats.Level});
            }
            // set class
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new CharacterClassPacket{ CharacterClass = client.Player.ClassId });
            }
            // set charname (name)
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new CharacterNamePacket{ CharacterName = client.Player.Actor.Name });
            }
            // set actor name
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new ActorNamePacket{ CharacterFamily = client.Player.Actor.FamilyName });
            }
            // set running
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new IsRunningPacket{ IsRunning = client.Player.Actor.IsRunning });
            }
            // set logos tabula
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new LogosStoneTabulaPacket());       // ToDo
            }
            // Recv_Abilities (id: 10, desc: must only be sent for the local manifestation)
            // We dont need to send ability data to every client, but only the owner (which is done in manifestation_assignPlayer)
            // Skills -> Everything that the player can learn via the skills menu (Sprint, Firearms...) Abilities -> Every skill gained by logos?
            // Recv_WorldLocationDescriptor
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    PosX = client.Player.Actor.PosX,
                    PosY = client.Player.Actor.PosY,
                    PosZ = client.Player.Actor.PosZ,
                    RotationX = 0.0f,
                    RotationY = 0.0f,
                    RotationZ = 0.0f,
                    Unknwon = 1.0f          // Camera poss?
                });
            }
            // set target category
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new TargetCategoryPacket { TargetCategory = 0 });    // 0 frendly
            }
            // player flags
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new PlayerFlagsPacket());
            }
            // send inital movement packet
            //netCompressedMovement_t netMovement = { 0 };
            //netMovement.entityId = client->player->actor->entityId;
            //netMovement.posX24b = client->player->actor->posX * 256.0f;
            //netMovement.posY24b = client->player->actor->posY * 256.0f;
            //netMovement.posZ24b = client->player->actor->posZ * 256.0f;
            //for (sint32 i = 0; i < playerCount; i++)
            //{
            //    netMgr_sendEntityMovement(playerList[i]->cgm, &netMovement);
            //}*/
        }

        public static void CellIntroducePlayersToClient(MapChannel mapChannel, MapChannelClient mapClient, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].ClientEntityId == mapClient.ClientEntityId)
                    continue;

                var tempClient = mapChannel.PlayerList[i];

                mapClient.Client.SendPacket(5, new CreatePyhsicalEntityPacket((int)tempClient.Player.Actor.EntityId, (int)tempClient.Player.Actor.EntityClassId));

                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new AttributeInfoPacket());
                // doesnt seem important (its only for loading gfx early?)
                //PreloadData
                //client.Client.SendPacket(tempClient.Player.Actor.EntityId, new PreloadDataPacket());
                // Recv_AppearanceData
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new AppearanceDataPacket());
                // set controller
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorControllerInfoPacket{IsPlayer = true});
                // set level
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new LevelPacket{ Level = 1 });
                // set class
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterClassPacket{ CharacterClass = tempClient.Player.ClassId });
                // set charname (name)
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterNamePacket{ CharacterName = tempClient.Player.Actor.Name });
                // set actor name (familyName)
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorNamePacket{ CharacterFamily = tempClient.Player.Actor.FamilyName });
                // set running
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new IsRunningPacket{ IsRunning = tempClient.Player.Actor.IsRunning });
                // Recv_WorldLocationDescriptor
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    PosX = tempClient.Player.Actor.PosX,
                    PosY = tempClient.Player.Actor.PosY,
                    PosZ = tempClient.Player.Actor.PosZ,
                    RotationX = 0.0f,
                    RotationY = 0.0f,
                    RotationZ = 0.0f,
                    Unknwon = 1.0f          // Camera poss?
                });
                // set target category
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new TargetCategoryPacket{ TargetCategory = 0 });  // 0 frendly
                // send inital movement packet
                //netCompressedMovement_t netMovement = { 0 };
                //netMovement.entityId = tempClient->player->actor->entityId;
                //netMovement.posX24b = tempClient->player->actor->posX * 256.0f;
                //netMovement.posY24b = tempClient->player->actor->posY * 256.0f;
                //netMovement.posZ24b = tempClient->player->actor->posZ * 256.0f;
                //netMgr_sendEntityMovement(client->cgm, &netMovement);
            }
        }

        public static void RemovePlayerCharacter(MapChannel mapChannel, MapChannelClient mapClient)
        {
            // ToDo do we need remove something, or it's done already 
        }
    }
}
