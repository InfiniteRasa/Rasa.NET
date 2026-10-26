using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    /// <summary>
    /// The GM commands that name an actor by entity id (.actorstate, .track, .vamp, .effect; .cover, which needs
    /// a map's cover file, is scoped the same way) find it only on the GM's own copy of the map (MapInstanceScope), not on another copy of the
    /// same map (a private instance, a battleground), which shares its map context id.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GmCommandMapScopeTests
    {
        [TestMethod]
        public void AnActorOnAnotherCopyOfTheMapIsNotFound()
        {
            using var world = new WorldTestContext();
            var gm = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(gm, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            gm.Player.State = CharacterState.Normal;
            gm.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 2000, 2000, 2000, 0, 0);
            CellManager.Instance.AddToWorld(gm);

            var copy = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>() };
            var here = Creature(world.Map, new Vector3(2, 0, 2));
            var there = Creature(copy, new Vector3(2, 0, 2));
            Assert.AreEqual(here.MapContextId, there.MapContextId, "the same map, by context id");

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(gm);
                commands.ProcessCommand(gm, command);
                return WorldTestContext.Drain(gm).Select(packet => packet.Message).OfType<CallMethodMessage>()
                    .Select(message => message.Packet).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            foreach (var command in new[] { ".effect list #{0}", ".vamp health 10 #{0}", ".actorstate idle #{0}", ".track #{0} me" })
            {
                CollectionAssert.Contains(Say(string.Format(command, there.EntityId)), $"No creature or player #{there.EntityId} on this map.", command);
                CollectionAssert.DoesNotContain(Say(string.Format(command, here.EntityId)), $"No creature or player #{here.EntityId} on this map.", command);
            }
        }

        private static Creature Creature(MapChannel map, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            return creature;
        }
    }
}
