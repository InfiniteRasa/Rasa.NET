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
    /// The client's action plays the charge and does not move anyone: RushingBlowAction.Windup
    /// sets the windup to the distance to the target over DEFAULT_PROJECTILE_VELOCITY (70 m/s)
    /// and plays ABILITY_RUSHING_BLOW_WINDUP (animation family 1099, FX by pump) scaled to it,
    /// with stopMovement and moveInterrupts off, and the animation data carries only blend
    /// times. What moves the performer is a movement: a MoveObject of MovementType.Rush puts
    /// their controller into the client's RushingMoveState, which runs them to the position at
    /// the velocity with their own input blocked, on their own client and on everyone's who
    /// sees them. So the server starts the charge and the clients make it:
    ///
    /// - the windup is timed the client's way, distance / ChargeSpeed, rather than the data's
    ///   1598 ms, so the blow lands when the client's windup ends;
    /// - as it starts, every client in range (their own too) is sent the run: to
    ///   ChargeStopShort metres short of the target - the resolve animation is a blow, not a
    ///   collision - on walkable ground where the map has a navmesh, at the speed that gets
    ///   them there as the windup ends;
    /// - through the windup the server keeps its own place for them along the line, a step a
    ///   world tick, as the target stands now, and sends nothing: a client is in its rush state
    ///   until the run is done and looks at nothing else sent for the performer before then;
    /// - if the target has moved by the time the blow lands, the clients are sent a second run,
    ///   from where the first ends to where the charge ends now;
    /// - the client's own Moves are set aside while they are carried, so a Move sent before it
    ///   saw the charge does not pull the character back or trip the movement check.
    ///
    /// The damage, knockback and stun are resolved as they were, once the charge has arrived. An
    /// interrupted or refused blow leaves the performer where the charge had got to on the
    /// server, and the clients, who cannot be stopped part-way, are told so: they put the
    /// performer back there when their run is over. ChargeStopShort is a choice; nothing in the
    /// client says how close the charge ends.
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

        /// <summary>How far the charge's end may have moved from where the clients were sent before they are sent on, in metres.</summary>
        public const float ChargeResendDistance = 0.5f;

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

            /// <summary>Where the clients were last sent running to, and how fast; null if they were not sent anywhere.</summary>
            public Vector3? SentTo;
            public float Speed;
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

        /// <summary>
        /// Starts the charge: the player's client and everyone in range are sent the run to where
        /// it ends, there as the windup ends, and the server carries them along it meanwhile.
        /// </summary>
        internal static void StartCharge(MapChannel mapChannel, Client client, Manifestation player, Actor target, ActionId actionId, long windupMs)
        {
            var now = Environment.TickCount64;
            var charge = new Charge
            {
                MapChannel = mapChannel,
                Client = client,
                Player = player,
                Target = target,
                ActionId = actionId,
                From = player.Position,
                StartTick = now,
                ArriveTick = now + windupMs
            };

            lock (ChargesLock)
            {
                Charges.RemoveAll(c => c.Player == player);
                Charges.Add(charge);
            }

            var end = ChargeEnd(charge);
            var run = Vector3.Distance(charge.From, end);

            if (run > 0.1f && windupMs > 0)
                SendRush(charge, end, run / (windupMs / 1000f));
        }

        /// <summary>Where the charge ends as the target stands now: ChargeStop, on walkable ground where the map has a navmesh.</summary>
        private static Vector3 ChargeEnd(Charge charge)
        {
            var stop = ChargeStop(charge.From, charge.Target.Position);

            if (Vector3.DistanceSquared(stop, charge.From) > 0.01f)
                stop = NavMeshManager.NearestWalkable(charge.MapChannel, stop, ChargeWalkableRadius) ?? stop;

            return stop;
        }

        /// <summary>
        /// Carries the charging players on this map a step on, on the server alone: to where they
        /// would be by now on the line to the target, as the target stands now. A charge whose
        /// blow is no longer pending - interrupted, or refused - stops where it is, and the
        /// clients are told where that is.
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
                    CutShort(charge);
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
        internal static void FinishCharge(Manifestation player)
        {
            Charge charge;

            lock (ChargesLock)
                charge = Charges.FirstOrDefault(c => c.Player == player);

            if (charge == null)
                return;

            var stop = ChargeEnd(charge);

            CarryTo(charge, stop);

            // The target has moved since the clients were sent, or they were sent nowhere because
            // it stood too close then: on from where they have the performer.
            if (Vector3.Distance(charge.SentTo ?? charge.From, stop) > ChargeResendDistance)
                SendRush(charge, stop, charge.Speed > 0 ? charge.Speed : ChargeSpeed);

            EndCharge(charge);
        }

        /// <summary>
        /// The charge is over before the blow: the server has the performer where the carry had
        /// got to, and the clients, running them to the end, are told so. They put the performer
        /// there when their run is done. Not for one who has left the map.
        /// </summary>
        private static void CutShort(Charge charge)
        {
            var player = charge.Player;
            var client = charge.Client;

            if (player.MapChannel != charge.MapChannel)
                return;

            // Never sent anywhere and never carried anywhere: they are where their client has them.
            if (charge.SentTo == null && Vector3.DistanceSquared(player.Position, charge.From) <= 0.0001f)
                return;

            var movement = new Movement(player.Position, client.Movement?.ViewDirection ?? new Vector2((float)player.Rotation, 0f));

            client.MoveObject(player.EntityId, movement);
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), true);
        }

        private static void EndCharge(Charge charge)
        {
            lock (ChargesLock)
                Charges.Remove(charge);
        }

        /// <summary>Has the charging player at `at`, facing the target, on the server. The clients are making the run and are not told.</summary>
        private static void CarryTo(Charge charge, Vector3 at)
        {
            var player = charge.Player;
            var client = charge.Client;
            var yaw = YawTowards(at, charge.Target.Position);
            var pitch = client.Movement?.ViewDirection.Y ?? 0f;

            player.PlaceAt(at);
            player.Rotation = yaw;
            client.Movement = new Movement(at, new Vector2(yaw, pitch));
        }

        /// <summary>
        /// Sends the charging player running to `to` at this speed (MovementType.Rush), on their
        /// own client and everyone's in range. The speed is held to what a movement can carry.
        /// </summary>
        private static void SendRush(Charge charge, Vector3 to, float speed)
        {
            var player = charge.Player;
            var client = charge.Client;
            var pitch = client.Movement?.ViewDirection.Y ?? 0f;

            speed = Math.Clamp(speed, 1f, Movement.MaxVelocity);

            var movement = Movement.Rush(to, speed, new Vector2(YawTowards(to, charge.Target.Position), pitch));

            charge.SentTo = to;
            charge.Speed = speed;

            client.MoveObject(player.EntityId, movement);

            if (player.MapChannel != null)
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), true);
        }
    }
}
