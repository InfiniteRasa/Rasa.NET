using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Models;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// Rushing Blow's charge (AA_COMMANDO_RUSHING_BLOW 302, abilities.rushingblow).
    ///
    /// The client plays the charge and does not move anyone: RushingBlowAction.Windup sets the
    /// windup to the distance to the target over DEFAULT_PROJECTILE_VELOCITY (70 m/s) and plays
    /// ABILITY_RUSHING_BLOW_WINDUP (animation family 1099, FX by pump) scaled to it, with
    /// stopMovement and moveInterrupts off; nothing in the client changes the performer's
    /// position, and the animation data carries only blend times. The server makes the charge:
    ///
    /// - the windup is timed the client's way, distance / ChargeSpeed, rather than the data's
    ///   1598 ms, so the blow lands when the client's windup ends;
    /// - through the windup the performer is carried towards the target at ChargeSpeed, a step
    ///   a world tick, and every client in range is sent where they are (their own client too:
    ///   a player's position is otherwise the client's to say, so MoveObject puts it there);
    /// - they stop ChargeStopShort metres short of the target, facing it - the resolve animation
    ///   is a blow, not a collision - on walkable ground where the map has a navmesh;
    /// - the client's own Moves are set aside while it is carried, so a Move sent before it saw
    ///   the charge does not pull the character back or trip the movement check.
    ///
    /// The damage, knockback and stun are resolved as they were, once the charge has arrived. An
    /// interrupted or refused blow leaves the performer where the charge had got to. ChargeStopShort
    /// is a choice; nothing in the client says how close the charge ends.
    /// </summary>
    public partial class AbilityManager
    {
        public const string RushingBlowModule = "abilities.rushingblow";

        /// <summary>DEFAULT_PROJECTILE_VELOCITY (shared/gameconstants.py), which RushingBlowAction times its windup by.</summary>
        public const float ChargeSpeed = 70f;

        /// <summary>How far short of the target the charge ends, in metres.</summary>
        public const float ChargeStopShort = 2f;

        /// <summary>How far from the charge's end a navmesh point may be looked for.</summary>
        private const float ChargeWalkableRadius = 4f;

        private sealed class Charge
        {
            public MapChannel MapChannel;
            public Client Client;
            public Manifestation Player;
            public Actor Target;
            public ActionId ActionId;
            public Vector3 From;
            public long StartTick;
            public long ArriveTick;
        }

        private static readonly List<Charge> Charges = new List<Charge>();
        private static readonly object ChargesLock = new object();

        /// <summary>The windup the client plays for a charge over this distance: distance / ChargeSpeed, in ms.</summary>
        public static long ChargeWindupMs(float distance)
        {
            return distance <= 0 ? 0 : (long)Math.Round(distance / ChargeSpeed * 1000.0);
        }

        /// <summary>Where a charge from `from` at a target standing at `target` ends: ChargeStopShort short of it, or where it started if it is already that close.</summary>
        public static Vector3 ChargeStop(Vector3 from, Vector3 target)
        {
            var toTarget = target - from;
            var distance = toTarget.Length();

            if (distance <= ChargeStopShort)
                return from;

            return target - toTarget / distance * ChargeStopShort;
        }

        /// <summary>How far along a charge is at `now`, from 0 to 1.</summary>
        public static float ChargeProgress(long start, long arrive, long now)
        {
            if (arrive <= start || now >= arrive)
                return 1f;

            return now <= start ? 0f : (float)(now - start) / (arrive - start);
        }

        /// <summary>The yaw that faces from one point to another, in the convention creature movement uses (facing along (-sin yaw, -cos yaw)).</summary>
        public static float YawTowards(Vector3 from, Vector3 to)
        {
            return (float)Math.Atan2(-(to.X - from.X), -(to.Z - from.Z));
        }

        /// <summary>Whether the player is being carried by a charge, and their own Moves are to be set aside.</summary>
        public static bool IsCharging(Manifestation player)
        {
            lock (ChargesLock)
                return Charges.Any(c => c.Player == player);
        }

        /// <summary>Starts carrying the player at the target for the charge's windup.</summary>
        private static void StartCharge(MapChannel mapChannel, Client client, Manifestation player, Actor target, ActionId actionId, long windupMs)
        {
            var now = Environment.TickCount64;

            lock (ChargesLock)
            {
                Charges.RemoveAll(c => c.Player == player);
                Charges.Add(new Charge
                {
                    MapChannel = mapChannel,
                    Client = client,
                    Player = player,
                    Target = target,
                    ActionId = actionId,
                    From = player.Position,
                    StartTick = now,
                    ArriveTick = now + windupMs
                });
            }
        }

        /// <summary>
        /// Carries the charging players on this map a step on: to where they would be by now on
        /// the line to the target, as the target stands now. A charge whose blow is no longer
        /// pending - interrupted, or refused - stops where it is.
        /// </summary>
        internal void ChargeWorker(MapChannel mapChannel)
        {
            List<Charge> charges;

            lock (ChargesLock)
                charges = Charges.Where(c => c.MapChannel == mapChannel).ToList();

            if (charges.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var charge in charges)
            {
                var pending = mapChannel.PerformRecovery.Any(a => a.Actor == charge.Player && a.ActionId == charge.ActionId && !a.IsInrerrupted);

                if (!pending || charge.Player.State == CharacterState.Dead || charge.Target.MapContextId != charge.Player.MapContextId)
                {
                    EndCharge(charge);
                    continue;
                }

                var stop = ChargeStop(charge.From, charge.Target.Position);
                var at = Vector3.Lerp(charge.From, stop, ChargeProgress(charge.StartTick, charge.ArriveTick, now));

                CarryTo(charge, at);
            }
        }

        /// <summary>
        /// The blow is landing: the performer is put at the end of the charge, on walkable ground
        /// where the map has a navmesh, before its damage is resolved.
        /// </summary>
        private static void FinishCharge(Manifestation player)
        {
            Charge charge;

            lock (ChargesLock)
                charge = Charges.FirstOrDefault(c => c.Player == player);

            if (charge == null)
                return;

            var stop = ChargeStop(charge.From, charge.Target.Position);

            if (Vector3.DistanceSquared(stop, charge.From) > 0.01f)
                stop = NavMeshManager.NearestWalkable(charge.MapChannel, stop, ChargeWalkableRadius) ?? stop;

            CarryTo(charge, stop);
            EndCharge(charge);
        }

        private static void EndCharge(Charge charge)
        {
            lock (ChargesLock)
                Charges.Remove(charge);
        }

        /// <summary>Puts the charging player at `at`, facing the target, and tells their client and everyone in range.</summary>
        private static void CarryTo(Charge charge, Vector3 at)
        {
            var player = charge.Player;
            var client = charge.Client;
            var yaw = YawTowards(at, charge.Target.Position);
            var pitch = client.Movement?.ViewDirection.Y ?? 0f;
            var movement = new Movement(at, new Vector2(yaw, pitch));

            player.PlaceAt(at);
            player.Rotation = yaw;
            client.Movement = movement;

            client.MoveObject(player.EntityId, movement);

            if (player.MapChannel != null)
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), true);
        }
    }
}
