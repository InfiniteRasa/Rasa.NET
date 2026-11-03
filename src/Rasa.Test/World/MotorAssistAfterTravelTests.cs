extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;

    /// <summary>
    /// The armour skills' standing effects go back on at every map arrival (SyncSkillPassives,
    /// from AssignPlayer), while the client is still Teleporting. What they change that the cells
    /// tell - the movement modifier, the regeneration rates - reaches the player's own client as
    /// well (GameEffectManager.TellAround): Motor Assist showed in the buff list after a map link,
    /// a dropship or an operation portal, and the player ran at base speed until a sprint or an
    /// armour piece off and on worked the speed out again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MotorAssistAfterTravelTests
    {
        private const uint ArmorClass = 990612;

        [TestMethod]
        public void AfterAMapLinkTheMotorAssistSpeedReachesTheirOwnClient()
        {
            using var world = new WorldTestContext();
            var client = Wearing(world, ArmorSkills.MotorAssist, pump: 5);
            var speed = 1 + ArmorSkills.MovementPercent(5, 5) / 100.0;

            ManifestationManager.Instance.SyncSkillPassives(client);
            Assert.AreEqual(speed, client.Player.MovementSpeed, 1e-9, "on the map they leave");
            Assert.AreEqual(speed, Calls(client).OfType<MovementModChangePacket>().Single().MovementMod, 1e-9);

            var destination = Travel(world, client, out var maps);

            try
            {
                maps.MapLoaded(client);
                var sent = Calls(client);

                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.IsTrue(sent.OfType<GameEffectAttachedPacket>().Any(packet => packet.EffectTypeId == ArmorSkills.MotorAssistVisibleTypeId),
                    "the buff is shown");
                Assert.AreEqual(speed, client.Player.MovementSpeed, 1e-9, "what the move check measures against");
                Assert.AreEqual(speed, sent.OfType<MovementModChangePacket>().Single().MovementMod, 1e-9,
                    "and their own client runs at it, told once");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(client);
                destination.ClientList.Clear();
            }
        }

        [TestMethod]
        public void InTheWorldTheChangeIsSentToThemOnceAndToThoseAround()
        {
            using var world = new WorldTestContext();
            var client = Wearing(world, ArmorSkills.MotorAssist, pump: 5);
            var other = world.CreateClient(x: 3);
            CellManager.Instance.AddToWorld(other);
            Calls(other);

            try
            {
                ManifestationManager.Instance.SyncSkillPassives(client);
                var speed = 1 + ArmorSkills.MovementPercent(5, 5) / 100.0;

                Assert.AreEqual(speed, Calls(client).OfType<MovementModChangePacket>().Single().MovementMod, 1e-9, "once, not twice");
                Assert.AreEqual(speed, Calls(other).OfType<MovementModChangePacket>().Single().MovementMod, 1e-9, "as before, to those around");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(other);
            }
        }

        /// <summary>Graviton's armour regeneration, from the same arrival: their own client predicts the bar from it.</summary>
        [TestMethod]
        public void AfterAMapLinkTheGravitonRegenerationReachesTheirOwnClient()
        {
            using var world = new WorldTestContext();
            var client = Wearing(world, ArmorSkills.Graviton, pump: 3);
            var armor = client.Player.Attributes[Attributes.Armor];
            var regen = GameEffectManager.RegenAmount(client.Player, armor);

            ManifestationManager.Instance.SyncSkillPassives(client);
            Assert.AreEqual(GameEffectManager.RegenAmount(client.Player, armor), Calls(client).OfType<UpdateArmorPacket>().Last().Armor.RefreshAmount);
            Assert.IsGreaterThan(regen, GameEffectManager.RegenAmount(client.Player, armor), "Graviton raises it");

            var destination = Travel(world, client, out var maps);

            try
            {
                maps.MapLoaded(client);

                Assert.AreEqual(GameEffectManager.RegenAmount(client.Player, armor),
                    Calls(client).OfType<UpdateArmorPacket>().Last().Armor.RefreshAmount);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(client);
                destination.ClientList.Clear();
            }
        }

        #region Fixture

        /// <summary>A player in the world in five pieces of the armour, at that pump in its skill.</summary>
        private static Client Wearing(WorldTestContext world, int skillId, int pump)
        {
            var client = world.CreateClient(x: 10);
            CellManager.Instance.AddToWorld(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();

            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 10, 1000);

            client.Player.Skills[(SkillId)skillId] = new SkillsData((SkillId)skillId, 0, pump);

            // A class the arrival can send the worn pieces again with (InventoryManager.ResendForMap).
            world.AddClass((EntityClasses)ArmorClass);
            var classInfo = EntityClassManager.Instance.GetClassInfo((EntityClasses)ArmorClass);
            classInfo.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1, MaxHitPoints = 100 });
            classInfo.ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { RegenRate = 7 });

            foreach (var slot in ArmorSkills.ArmorSlots)
            {
                var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = ArmorClass, ItemClass = ArmorClass })
                {
                    EquipableInfo = new EquipableInfo(skillId, 1)
                };
                var item = new Item { ItemTemplate = template, ItemTemplateId = ArmorClass, StackSize = 1, CurrentHitPoints = 100 };
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                client.Player.Inventory.EquippedInventory[(int)slot] = item.EntityId;
            }

            Calls(client);
            return client;
        }

        /// <summary>
        /// Through a map link to another map, as far as MapLoaded: the arrival puts the standing
        /// effects back on as AssignPlayer does, at its end, while the client is Teleporting.
        /// </summary>
        private static MapChannel Travel(WorldTestContext world, Client client, out MapChannelManager maps)
        {
            var destination = DropshipTravelTests.CreateDestination();

            maps = new MapChannelManager(null, () => 1000,
                updateCharacter: (_, _, _) => { },
                refreshStats: (_, _) => { },
                assignPlayer: arriving =>
                {
                    Assert.AreEqual(ClientState.Teleporting, arriving.State, "the passives go on before the arrival is over");
                    ManifestationManager.Instance.SyncSkillPassives(arriving);
                },
                enterMapChannels: _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);

            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId, new Vector3(400, 5, 0), 0));
            Assert.AreEqual(1.0, client.Player.MovementSpeed, 1e-9, "the effects end with the map");
            Calls(client);
            return destination;
        }

        private static List<PythonPacket> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();

        #endregion
    }
}
