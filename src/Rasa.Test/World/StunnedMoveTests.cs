using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A stunned player's client keeps sending Moves that go nowhere - settling them onto the
    /// ground, turning their head, flying a knockback's arc - and each was refused, put back and
    /// logged as a player trying to move while stunned (ManifestationManager.AcceptMove).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class StunnedMoveTests
    {
        [TestMethod]
        public void SettlingWhereTheStunFoundThemIsTakenAndWalkingOffIsNot()
        {
            // The line from the log: 0.4 m down and nothing across.
            var anchor = new Vector3(-86.08443f, 86.58565f, 82.7208f);
            Assert.AreEqual(ManifestationManager.StunnedMove.Settles, ManifestationManager.JudgeStunnedMove(anchor, new Vector3(-86.08203f, 86.21875f, 82.71875f)));

            Assert.AreEqual(ManifestationManager.StunnedMove.Settles, ManifestationManager.JudgeStunnedMove(anchor, anchor + new Vector3(0.4f, 0, 0)), "a few tenths across");
            Assert.AreEqual(ManifestationManager.StunnedMove.Settles, ManifestationManager.JudgeStunnedMove(anchor, anchor + new Vector3(0.2f, -12f, 0)), "a fall is no movement of theirs");
            Assert.AreEqual(ManifestationManager.StunnedMove.PutBack, ManifestationManager.JudgeStunnedMove(anchor, anchor + new Vector3(0, 0.8f, 0)), "a jump");
            Assert.AreEqual(ManifestationManager.StunnedMove.PutBack, ManifestationManager.JudgeStunnedMove(anchor, anchor + new Vector3(1.5f, 0, 0)), "a step from before the client knew");
            Assert.AreEqual(ManifestationManager.StunnedMove.Reported, ManifestationManager.JudgeStunnedMove(anchor, anchor + new Vector3(0, 0, 4f)), "walking off");
        }

        [TestMethod]
        public void AStunnedPlayerSettlesWithoutBeingPutBackAndCannotCreepAway()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var player = client.Player;
            var start = player.Position;
            Assert.IsTrue(PlayerCrowdControl.Stun(world.Map, player, null, 5000));
            WorldTestContext.Drain(client);

            // Onto the ground under them: taken, nothing sent back.
            Assert.IsTrue(ManifestationManager.Instance.AcceptMove(client, new Movement(start + new Vector3(0, -0.37f, 0), new Vector2(0.5f, 0))));
            Assert.IsEmpty(Corrections(client));

            // A little at a time adds up from where the stun found them, not from the last Move.
            Assert.IsTrue(ManifestationManager.Instance.AcceptMove(client, new Movement(start + new Vector3(0.4f, 0, 0), new Vector2(0.5f, 0))));
            player.Position = start + new Vector3(0.4f, 0, 0);
            Assert.IsFalse(ManifestationManager.Instance.AcceptMove(client, new Movement(start + new Vector3(0.8f, 0, 0), new Vector2(0.5f, 0))));
            Assert.HasCount(1, Corrections(client), "put back");

            // Over: the next Move is judged as any is, from where they are.
            foreach (var effect in player.ActiveEffects.Values.Where(e => e.IsStun))
                effect.ExpiresTick = System.Environment.TickCount64 - 1;
            player.MoveBudgetTick = System.Environment.TickCount64 - 2000;
            player.MoveBudget = 0;
            Assert.IsTrue(ManifestationManager.Instance.AcceptMove(client, new Movement(player.Position + new Vector3(1f, 0, 0), new Vector2(0.5f, 0))));
            Assert.IsNull(player.StunAnchor);
        }

        [TestMethod]
        public void WhatTheClientSaysOnAKnockbacksFlightIsSetAside()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 10, 0);
            var player = client.Player;
            var kicker = new Creature
            {
                Name = "Kicker",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
            };
            var from = player.Position;

            Assert.IsTrue(PlayerCrowdControl.Knockback(world.Map, player, kicker, 10f));
            var landing = player.Position;
            Assert.IsTrue(Vector3.Distance(from, landing) > 5f);
            WorldTestContext.Drain(client);

            // Half way along the arc: neither taken nor corrected.
            Assert.IsFalse(ManifestationManager.Instance.AcceptMove(client, new Movement((from + landing) / 2, new Vector2(0.5f, 0))));
            Assert.AreEqual(landing, player.Position);
            Assert.IsEmpty(Corrections(client));
        }

        private static MoveObjectMessage[] Corrections(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<MoveObjectMessage>()
                .Where(message => message.EntityId == client.Player.EntityId).ToArray();
    }
}
