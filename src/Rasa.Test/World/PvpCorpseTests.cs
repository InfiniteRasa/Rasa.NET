using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // The corpse abilities on a dead enemy player (Pvp, PlayerDeath): Reanimation raises their
    // clone, Hortimonculus grows from them, and either sends them to their nearest hospital;
    // Cadaver Immolation goes off early when they get up. And a Hominis Machina's Self Revive.
    [TestClass]
    [DoNotParallelize]
    public class PvpCorpseTests
    {
        private const uint MapId = 1220;
        private const uint RedClanId = 900031;
        private const uint BlueClanId = 900032;

        private Func<IEnumerable<(uint, uint, Vector3, string)>> _source;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;

        [TestInitialize]
        public void UseTestHospitals()
        {
            _source = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;

            Hospitals.Source = () => new[] { (105u, MapId, new Vector3(-300, 0, 0), "Hospital: Twin Pillars") };
            Hospitals.IsSafeZone = id => id == 105;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();
        }

        [TestCleanup]
        public void RestoreHospitals()
        {
            Hospitals.Source = _source;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            Hospitals.Reset();
        }

        [TestMethod]
        public void OnlyADeadEnemyPlayersBodyIsACorpseToUse()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, 0);
            var redMate = Fighter(world, RedClanId, 3);
            var blue = Fighter(world, BlueClanId, 6);

            WithFeud(world, () =>
            {
                Assert.IsFalse(AbilityManager.IsUsableCorpse(blue.Player, red.Player), "alive");

                Kill(world, blue);
                Kill(world, redMate);

                Assert.IsTrue(AbilityManager.IsUsableCorpse(blue.Player, red.Player));
                Assert.IsFalse(AbilityManager.IsUsableCorpse(redMate.Player, red.Player), "a friend's");
                Assert.IsFalse(AbilityManager.IsUsableCorpse(blue.Player), "nobody using it");
            });
        }

        [TestMethod]
        public void ReanimationRaisesTheEnemysCloneAndSendsThemToTheirHospital()
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)AbilityManager.CloneMaleClassId);
            var red = Fighter(world, RedClanId, 0);
            var blue = Fighter(world, BlueClanId, 6);
            blue.Player.AppearanceData[EquipmentData.Helmet] = new AppearanceData { SlotId = EquipmentData.Helmet, Class = 1234 };

            WithFeud(world, () =>
            {
                Kill(world, blue);

                var info = Level(ActionId.AaExobiologistReanimation, 1);
                info.Properties[AbilityProperty.Duration] = 120;
                Invoke("Reanimate", world.Map, red.Player, info, new ActionData(red.Player, ActionId.AaExobiologistReanimation, 1, blue.Player.EntityId, 0));

                var clone = world.Map.MapCellInfo.Cells.Values.SelectMany(c => c.CreatureList).Distinct().Single(c => c.MasterEntityId == red.Player.EntityId);

                try
                {
                    Assert.AreEqual($"Clone of {blue.Player.Name}", clone.Name);
                    Assert.AreEqual(TargetCategory.Friendly, clone.TargetCategory);
                    Assert.AreEqual(1234u, clone.AppearanceData[EquipmentData.Helmet].Class, "in their look");
                    Assert.AreEqual(1000, clone.Attributes[Attributes.Health].CurrentMax, "at their health");
                    Assert.AreEqual(6f, clone.Position.X, 0.01f, "where they lay");
                    Assert.IsTrue(AbilityManager.IsReanimated(clone));

                    Assert.AreNotEqual(CharacterState.Dead, blue.Player.State, "sent to their hospital");
                    Assert.AreEqual(-300f, blue.Player.Position.X, 0.01f);
                }
                finally
                {
                    foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                        cell.CreatureList.Remove(clone);
                }
            });
        }

        [TestMethod]
        public void HortimonculusGrowsFromTheEnemyAndSendsThemToTheirHospital()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, 0);
            var blue = Fighter(world, BlueClanId, 6);

            WithFeud(world, () =>
            {
                Kill(world, blue);

                var info = Level(ActionId.AaExobiologistHortimunculus, 1);
                Invoke("GrowHortimonculus", world.Map, red.Player, info, new ActionData(red.Player, ActionId.AaExobiologistHortimunculus, 1, blue.Player.EntityId, 0));

                var plant = world.Map.MapCellInfo.Cells.Values.SelectMany(c => c.DynamicObjectList).Distinct()
                    .Single(o => o.DynamicObjectType == DynamicObjectType.Hortimonculus);

                Assert.AreEqual(6f, plant.Position.X, 0.01f, "where they lay");
                Assert.AreNotEqual(CharacterState.Dead, blue.Player.State, "sent to their hospital");
                Assert.AreEqual(-300f, blue.Player.Position.X, 0.01f);

                Kill(world, blue);
                Assert.IsTrue(AbilityManager.IsUsableCorpse(blue.Player, red.Player), "the plant keeps no body of theirs");
            });
        }

        [TestMethod]
        public void CadaverImmolationOnAnEnemyGoesOffEarlyWhenTheyGetUp()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId, 0);
            var blue = Fighter(world, BlueClanId, 6);
            var blueMate = Fighter(world, BlueClanId, 8);

            WithFeud(world, () =>
            {
                Kill(world, blue);

                var info = Level(ActionId.AaExobiologistCadaverImmolation, 1);
                info.Properties[AbilityProperty.DamageAmountMin] = 100;
                info.Properties[AbilityProperty.DamageAmountMax] = 100;
                info.Properties[AbilityProperty.EffectRadius] = 12;
                info.Properties[AbilityProperty.Delay] = 60;
                Invoke("ImmolateCorpse", world.Map, red.Player, blue.Player, info);

                Assert.IsTrue(AbilityManager.IsCorpseInUse(blue.Player));
                Assert.AreEqual(1000, blueMate.Player.Attributes[Attributes.Health].Current, "not yet");

                PlayerDeath.ReviveMe(blue, null);

                Assert.IsFalse(AbilityManager.IsCorpseInUse(blue.Player));
                Assert.IsTrue(blueMate.Player.Attributes[Attributes.Health].Current < 1000, "it went off where the body lay");
                Assert.AreEqual(1000, blue.Player.Attributes[Attributes.Health].Current, "after the blast, at the hospital");
            });
        }

        [TestMethod]
        public void AHominisMachinaKeepsItsMorphInDeathAndGetsUpOnce()
        {
            using var world = new WorldTestContext();
            var client = Fighter(world, 0, 20);
            var player = client.Player;

            Morph(world, client);

            Kill(world, client);
            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == AbilityManager.PolymorphTypeId), "the morph stays, with its button");

            Assert.IsTrue(PlayerDeath.SelfRevive(client));
            Assert.AreNotEqual(CharacterState.Dead, player.State);
            Assert.AreEqual(1000, player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(20f, player.Position.X, 0.01f, "where they fell");
            Assert.IsFalse(AbilityManager.CanSelfRevive(player), "useable once");

            Kill(world, client);
            Assert.IsFalse(PlayerDeath.SelfRevive(client));
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => e.TypeId == AbilityManager.PolymorphTypeId), "spent: the morph ends with them");
        }

        [TestMethod]
        public void AMachinaWhoGoesToTheHospitalLeavesTheMorphBehind()
        {
            using var world = new WorldTestContext();
            var client = Fighter(world, 0, 20);

            Morph(world, client);
            Kill(world, client);
            PlayerDeath.ReviveMe(client, null);

            Assert.IsFalse(client.Player.ActiveEffects.Values.Any(e => e.TypeId == AbilityManager.PolymorphTypeId));
            Assert.IsFalse(AbilityManager.CanSelfRevive(client.Player));
        }

        private static void Morph(WorldTestContext world, Client client)
        {
            var player = client.Player;

            player.MorphWeapon = new MorphWeaponItem();
            player.MorphAbilities = new List<(ActionId, uint)> { (ActionId.PolySelfRes, 1u) };

            var morph = new GameEffect
            {
                TypeId = AbilityManager.PolymorphTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                Source = player,
                SourceId = player.EntityId,
                IsBuff = true,
                ServerOnly = true,
                ExpiresTick = Environment.TickCount64 + 120000,
                OnDetached = (map, actor, e) =>
                {
                    player.MorphWeapon = null;
                    player.MorphAbilities = new List<(ActionId, uint)>();
                }
            };

            GameEffectManager.Instance.Attach(world.Map, player, morph);
            Assert.IsTrue(AbilityManager.CanSelfRevive(player));
        }

        private static void Kill(WorldTestContext world, Client client)
        {
            client.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsTrue(PlayerDeath.AtZero(world.Map, client.Player, null));
        }

        private static ActionLevelInfo Level(ActionId actionId, uint level) => new ActionLevelInfo { ActionId = actionId, Level = level, MaxRange = 40 };

        private static object Invoke(string name, params object[] args)
        {
            var method = typeof(AbilityManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!;
            return method.Invoke(method.IsStatic ? null : Abilities(), args);
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static Client Fighter(WorldTestContext world, uint clanId, float x)
        {
            var client = PlayerDeathTests.Player(world, x, 0);
            client.Player.ClanId = clanId;
            return client;
        }

        private static void WithFeud(WorldTestContext world, Action body)
        {
            var online = world.Map.ClientList.ToList();

            lock (Server.Clients)
                Server.Clients.AddRange(online);

            ClanFeuds.Feud feud = null;

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = RedClanId, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = BlueClanId, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                body();
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in online)
                        Server.Clients.Remove(client);
            }
        }
    }
}
