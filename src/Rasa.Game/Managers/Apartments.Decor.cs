using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// An apartment's items: put down as its copy opens, and placed, moved, turned, sized,
    /// copied, deleted and undone by those who may build there (Apartments.Commands).
    ///
    /// An item is a DynamicObject of its catalogue class, in the copy's cells as any object is,
    /// and shown to everyone in range. What the client is told of one is what a .map tells it of
    /// its own scenery - the class, then a position and an orientation - and that it may be
    /// picked out (IsTargetable), so a player can point at the item they mean; a scale other
    /// than 1 goes as BodyAttributes. Only the yaw is kept: the client is told an orientation
    /// about the up axis. A change to an item takes it out of the world and puts it back as a
    /// new entity: an object's cell is fixed when it goes in, and the client has no message for
    /// a server object's move but a new location, which leaves its cell behind.
    ///
    /// The rules of an edit, each refused with its reason in chat:
    ///  - the right to build (the owner, or a visitor invited with build);
    ///  - the item's box (decor.tsv's, turned and scaled) inside the plot's build box;
    ///  - no more items put down than the budget (the plot's, held to ApartmentConfig.ItemBudget)
    ///    and no more of a category than its cap (plot-caps.tsv, scaled down with the budget);
    ///  - a scale within the catalogue item's;
    ///  - no more than ApartmentConfig.EditBurst edits at once and EditsPerSecond after.
    /// </summary>
    public sealed partial class Apartments
    {
        /// <summary>A little room at the edges of the build box for an item's rough box.</summary>
        public const float BoxSlack = 0.05f;

        /// <summary>The item each character last chose: (apartment, number).</summary>
        private readonly Dictionary<uint, (uint ApartmentId, uint Number)> _selected = new Dictionary<uint, (uint, uint)>();

        /// <summary>What each character's last edit was, to undo it.</summary>
        private readonly Dictionary<uint, Undo> _undo = new Dictionary<uint, Undo>();

        /// <summary>The edits each character has left, and when that was worked out.</summary>
        private readonly Dictionary<uint, (double Tokens, long At)> _buckets = new Dictionary<uint, (double, long)>();

        private sealed class Undo
        {
            public uint ApartmentId;
            public string What;

            /// <summary>The item as it was before the edit; null when the edit placed it.</summary>
            public ApartmentItemEntry Before;

            /// <summary>The number the edit gave the item it placed.</summary>
            public uint Placed;
        }

        #region What the client is sent

        /// <summary>CreatePhysicalEntity's data for an apartment's item: targetable scenery, at its place and yaw, at its size.</summary>
        public static List<PythonPacket> EntityData(DynamicObject obj)
        {
            var data = new List<PythonPacket>
            {
                new IsTargetablePacket(true),
                new WorldLocationDescriptorPacket(obj.Position, obj.Rotation)
            };

            if (Math.Abs(obj.Scale - 1f) > 0.001f)
                data.Add(new BodyAttributesPacket(obj.Scale, null, BodyAttributesPacket.Collide, BodyAttributesPacket.Collide, null));

            return data;
        }

        #endregion

        #region Putting down and taking up

        private void PutDownItems(Live live, IEnumerable<ApartmentItemEntry> rows)
        {
            foreach (var row in rows.OrderBy(r => r.Number))
            {
                var decor = Catalogue.DecorOf(row.ClassId);

                // Packed, or of a class the catalogue no longer has: kept, not put down.
                if (row.Packed || decor == null)
                {
                    live.Packed[row.Number] = row;
                    continue;
                }

                live.Items[row.Number] = Spawn(live, row, decor);
            }
        }

        /// <summary>The item's entity in the copy: a footlocker the vault's, a station a crafting station, the rest scenery.</summary>
        private Placed Spawn(Live live, ApartmentItemEntry row, ApartmentCatalogue.Decor decor)
        {
            var obj = new DynamicObject
            {
                Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                Rotation = row.Rotation,
                MapContextId = live.Map.MapInfo.MapContextId,
                EntityClassId = (EntityClasses)row.ClassId,
                Scale = (float)row.Scale,
                Comment = $"apartment {row.ApartmentId} item {row.Number}",
                DynamicObjectType = DynamicObjectType.ApartmentItem
            };

            switch (decor.Function)
            {
                case DecorFunction.Storage:
                    // A world footlocker's way: its Use opens the player's own vault.
                    obj.DynamicObjectType = DynamicObjectType.Lockbox;
                    break;
                case DecorFunction.Crafting:
                    KraftwerksManager.Instance.PlaceApartmentStation(live.Map, obj, StationIdOf(row));
                    break;
            }

            CellManager.Instance.AddToWorld(live.Map, obj);

            return new Placed { Row = row, Decor = decor, Object = obj };
        }

        /// <summary>The kraftwerks id an apartment's crafting station keeps its jobs under.</summary>
        public static uint StationIdOf(ApartmentItemEntry row) => KraftwerksManager.ApartmentStationIdBase + row.Id;

        private void Despawn(Live live, Placed placed)
        {
            if (placed?.Object == null)
                return;

            if (placed.Decor.Function == DecorFunction.Crafting)
                KraftwerksManager.Instance.RemoveApartmentStation(live.Map, StationIdOf(placed.Row));

            if (placed.Object.RuntimeMapChannel != null)
                CellManager.Instance.RemoveFromWorld(live.Map, placed.Object);

            placed.Object = null;
        }

        /// <summary>A copy closed (its objects already taken out with it): the crafting stations off the table.</summary>
        private void ForgetItems(Live live)
        {
            foreach (var placed in live.Items.Values.Where(p => p.Decor.Function == DecorFunction.Crafting))
                KraftwerksManager.Instance.RemoveApartmentStation(live.Map, StationIdOf(placed.Row));

            live.Items.Clear();
            live.Packed.Clear();
        }

        #endregion

        #region The rules

        /// <summary>Why an item of a class may not stand there, so turned and sized, in the apartment; null when it may.</summary>
        public static string WhyNotThere(ApartmentCatalogue.Plot plot, ApartmentCatalogue.Decor decor, Vector3 position, double yaw, double scale)
        {
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) || !double.IsFinite(yaw) || !double.IsFinite(scale))
                return "That is not a place.";

            if (scale < decor.ScaleMin - 0.0001 || scale > decor.ScaleMax + 0.0001)
                return decor.ScaleMin == decor.ScaleMax
                    ? $"{decor.Name} only comes at scale {decor.ScaleMin:0.##}."
                    : $"{decor.Name} goes from scale {decor.ScaleMin:0.##} to {decor.ScaleMax:0.##}.";

            var (min, max) = decor.BoundsAt(position, yaw, (float)scale);

            if (!plot.Contains(min, BoxSlack) || !plot.Contains(max, BoxSlack))
                return $"That would put {decor.Name} outside the apartment's build box.";

            return null;
        }

        /// <summary>Why one more item of a class may not be put down in the apartment; null when it may.</summary>
        internal string WhyNotMore(Live live, ApartmentCatalogue.Decor decor)
        {
            var budget = ApartmentCatalogue.BudgetOf(live.Plot, Config.ItemBudget);

            if (live.Items.Count >= budget)
                return $"Your apartment holds {budget} items, and has them all. Delete one first.";

            var cap = Catalogue.CapOf(live.Plot, decor.Category, budget);

            if (cap != null && live.Items.Values.Count(p => string.Equals(p.Decor.Category, decor.Category, StringComparison.OrdinalIgnoreCase)) >= cap)
                return $"Your apartment holds {cap} {decor.Category} items, and has them all.";

            return null;
        }

        /// <summary>
        /// Which items an apartment's plot keeps put down: in the order they were placed, those
        /// whose box is inside the build box, up to the budget and each category's cap; the rest
        /// are packed. Returns whether each item, by row id, is packed.
        /// </summary>
        public static Dictionary<uint, bool> Repack(ApartmentCatalogue catalogue, ApartmentCatalogue.Plot plot, IEnumerable<ApartmentItemEntry> items, int serverBudget)
        {
            var budget = ApartmentCatalogue.BudgetOf(plot, serverBudget);
            var byCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var placed = 0;
            var packed = new Dictionary<uint, bool>();

            foreach (var item in items.OrderBy(i => i.PlacedAt).ThenBy(i => i.Number))
            {
                var decor = catalogue.DecorOf(item.ClassId);
                var fits = decor != null && placed < budget &&
                           WhyNotThere(plot, decor, new Vector3((float)item.PosX, (float)item.PosY, (float)item.PosZ), item.Rotation, item.Scale) == null;

                if (fits)
                {
                    var cap = catalogue.CapOf(plot, decor.Category, budget);
                    byCategory.TryGetValue(decor.Category, out var count);

                    if (cap != null && count >= cap)
                        fits = false;
                    else
                        byCategory[decor.Category] = count + 1;
                }

                if (fits)
                    placed++;

                packed[item.Id] = !fits;
            }

            return packed;
        }

        /// <summary>One edit taken from the character's allowance; false, and nothing taken, when it is used up.</summary>
        internal bool TakeEdit(uint characterId)
        {
            var now = Clock();
            var burst = Math.Max(1, Config.EditBurst);
            var rate = Math.Max(0.01, Config.EditsPerSecond);
            var (tokens, at) = _buckets.TryGetValue(characterId, out var bucket) ? bucket : (burst, now);

            tokens = Math.Min(burst, tokens + (now - at) * rate / 1000.0);

            if (tokens < 1)
            {
                _buckets[characterId] = (tokens, now);
                return false;
            }

            _buckets[characterId] = (tokens - 1, now);
            return true;
        }

        #endregion

        #region Which item

        /// <summary>
        /// The apartment a player is in and may build in; null, with them told why, when they are
        /// in none or may not build in it.
        /// </summary>
        internal Live BuildingIn(Client client)
        {
            var live = LiveOf(client?.Player?.MapChannel);

            if (live == null)
            {
                Say(client, "You are not in an apartment. .apartment takes you to yours.");
                return null;
            }

            if (AccessOf(live, client) < Access.Build)
            {
                Say(client, "You may not build in this apartment.");
                return null;
            }

            return live;
        }

        /// <summary>
        /// The item a player means: the one they target, if it is one of the apartment's; else the
        /// one they last chose. Null, with them told why, when there is none.
        /// </summary>
        internal Placed Current(Client client, Live live)
        {
            var player = client.Player;
            var targeted = player.Target == 0 ? null : live.Items.Values.FirstOrDefault(p => p.Object?.EntityId == player.Target);

            if (targeted != null)
            {
                _selected[player.Id] = (live.Row.Id, targeted.Row.Number);
                return targeted;
            }

            if (_selected.TryGetValue(player.Id, out var chosen) && chosen.ApartmentId == live.Row.Id && live.Items.TryGetValue(chosen.Number, out var placed))
                return placed;

            Say(client, "Choose an item first: target it, or .decor near and .decor sel <number>.");
            return null;
        }

        internal void Select(Client client, Live live, Placed placed) => _selected[client.Player.Id] = (live.Row.Id, placed.Row.Number);

        private static Vector3 PositionOf(ApartmentItemEntry row) => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);

        private static ApartmentItemEntry CopyOf(ApartmentItemEntry row) => new ApartmentItemEntry
        {
            Id = row.Id,
            ApartmentId = row.ApartmentId,
            Number = row.Number,
            ClassId = row.ClassId,
            PosX = row.PosX,
            PosY = row.PosY,
            PosZ = row.PosZ,
            Rotation = row.Rotation,
            Scale = row.Scale,
            Packed = row.Packed,
            PlacedBy = row.PlacedBy,
            PlacedAt = row.PlacedAt
        };

        /// <summary>A yaw within -pi..pi.</summary>
        public static double Wrap(double yaw)
        {
            yaw %= 2 * Math.PI;

            if (yaw > Math.PI)
                yaw -= 2 * Math.PI;
            else if (yaw < -Math.PI)
                yaw += 2 * Math.PI;

            return yaw;
        }

        /// <summary>The way a yaw faces, flat (Facing's convention).</summary>
        public static Vector3 Forward(double yaw) => new Vector3((float)-Math.Sin(yaw), 0f, (float)-Math.Cos(yaw));

        /// <summary>The yaw that faces from one point towards another.</summary>
        public static double YawTowards(Vector3 from, Vector3 to) => Math.Atan2(-(to.X - from.X), -(to.Z - from.Z));

        private string Describe(Live live, Placed placed) =>
            $"#{placed.Row.Number} {placed.Decor.Name}";

        private string Counts(Live live, ApartmentCatalogue.Decor decor)
        {
            var budget = ApartmentCatalogue.BudgetOf(live.Plot, Config.ItemBudget);
            var cap = Catalogue.CapOf(live.Plot, decor.Category, budget);
            var inCategory = live.Items.Values.Count(p => string.Equals(p.Decor.Category, decor.Category, StringComparison.OrdinalIgnoreCase));

            return cap == null
                ? $"{live.Items.Count}/{budget} items"
                : $"{decor.Category} {inCategory}/{cap}, {live.Items.Count}/{budget} items";
        }

        #endregion

        #region Edits

        /// <summary>A new item of a class, in front of the player, facing them. The text to tell them.</summary>
        internal string PlaceNew(Client client, Live live, ApartmentCatalogue.Decor decor, double distance)
        {
            var player = client.Player;

            if (!double.IsFinite(distance) || distance < 0 || distance > Config.Reach)
                return $"Place it within {Config.Reach:0} metres.";

            var position = player.Position + Forward(player.Rotation) * (float)distance;
            var yaw = Wrap(player.Rotation + Math.PI);
            var scale = Math.Clamp(1.0, decor.ScaleMin, decor.ScaleMax);

            return Add(client, live, decor, position, yaw, scale, "Placed");
        }

        /// <summary>A copy of an item, in front of the player, turned and sized as it is.</summary>
        internal string CopyItem(Client client, Live live, Placed placed, double distance)
        {
            var position = client.Player.Position + Forward(client.Player.Rotation) * (float)Math.Clamp(distance, 0, Config.Reach);

            return Add(client, live, placed.Decor, position, placed.Row.Rotation, placed.Row.Scale, "Copied as");
        }

        private string Add(Client client, Live live, ApartmentCatalogue.Decor decor, Vector3 position, double yaw, double scale, string verb)
        {
            var why = WhyNotMore(live, decor) ?? WhyNotThere(live.Plot, decor, position, yaw, scale);

            if (why != null)
                return why;

            if (!TakeEdit(client.Player.Id))
                return "Slow down a little.";

            var row = Store.With(repository => repository.AddItem(live.Row.Id, new ApartmentItemEntry
            {
                ClassId = decor.ClassId,
                PosX = position.X,
                PosY = position.Y,
                PosZ = position.Z,
                Rotation = yaw,
                Scale = scale,
                PlacedBy = client.Player.Id,
                PlacedAt = UtcNowMs()
            }));

            if (row == null)
                return "Your apartment could not be saved.";

            var placed = Spawn(live, row, decor);

            live.Items[row.Number] = placed;
            Select(client, live, placed);
            _undo[client.Player.Id] = new Undo { ApartmentId = live.Row.Id, What = $"placing {Describe(live, placed)}", Placed = row.Number };

            return $"{verb} {Describe(live, placed)} ({Counts(live, decor)}).";
        }

        /// <summary>
        /// An item moved, turned or sized: <paramref name="change"/> makes the new row from a copy
        /// of the old. The text to tell the player.
        /// </summary>
        internal string Change(Client client, Live live, Placed placed, Action<ApartmentItemEntry> change, string verb, bool remember = true)
        {
            var next = CopyOf(placed.Row);

            change(next);
            next.Rotation = Wrap(next.Rotation);

            var why = WhyNotThere(live.Plot, placed.Decor, PositionOf(next), next.Rotation, next.Scale);

            if (why != null)
                return why;

            if (!TakeEdit(client.Player.Id))
                return "Slow down a little.";

            if (!Store.With(repository => repository.UpdateItem(next)))
                return "That item is gone.";

            if (remember)
                _undo[client.Player.Id] = new Undo { ApartmentId = live.Row.Id, What = $"{verb} {Describe(live, placed)}", Before = CopyOf(placed.Row) };

            Despawn(live, placed);

            var again = Spawn(live, next, placed.Decor);

            live.Items[next.Number] = again;
            Select(client, live, again);

            return $"{Capitalised(verb)} {Describe(live, again)}.";
        }

        internal string Delete(Client client, Live live, Placed placed)
        {
            if (!TakeEdit(client.Player.Id))
                return "Slow down a little.";

            if (!Store.With(repository => repository.RemoveItem(placed.Row.Id)))
                return "That item is gone.";

            _undo[client.Player.Id] = new Undo { ApartmentId = live.Row.Id, What = $"deleting {Describe(live, placed)}", Before = CopyOf(placed.Row) };
            Despawn(live, placed);
            live.Items.Remove(placed.Row.Number);
            _selected.Remove(client.Player.Id);

            return $"Deleted {Describe(live, placed)} ({live.Items.Count}/{ApartmentCatalogue.BudgetOf(live.Plot, Config.ItemBudget)} items).";
        }

        /// <summary>The player's last edit in this apartment, undone.</summary>
        internal string UndoLast(Client client, Live live)
        {
            if (!_undo.TryGetValue(client.Player.Id, out var undo) || undo.ApartmentId != live.Row.Id)
                return "There is nothing of yours to undo here.";

            _undo.Remove(client.Player.Id);

            // A placing: the item goes again.
            if (undo.Before == null)
            {
                if (!live.Items.TryGetValue(undo.Placed, out var placed))
                    return "The item you placed is gone already.";

                var said = Delete(client, live, placed);
                _undo.Remove(client.Player.Id);
                return $"Undid {undo.What}: {said}";
            }

            // A move, turn or size: put back as it was.
            if (live.Items.TryGetValue(undo.Before.Number, out var moved))
            {
                var before = undo.Before;
                var said = Change(client, live, moved, row =>
                {
                    row.PosX = before.PosX;
                    row.PosY = before.PosY;
                    row.PosZ = before.PosZ;
                    row.Rotation = before.Rotation;
                    row.Scale = before.Scale;
                }, "put back", remember: false);

                return $"Undid {undo.What}: {said}";
            }

            // A delete: the item again, with a new number.
            var decor = Catalogue.DecorOf(undo.Before.ClassId);

            if (decor == null)
                return "That item is no longer in the catalogue.";

            var back = Add(client, live, decor, PositionOf(undo.Before), undo.Before.Rotation, undo.Before.Scale, "Put back");
            _undo.Remove(client.Player.Id);

            return $"Undid {undo.What}: {back}";
        }

        private static string Capitalised(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        #endregion

        #region Plots

        /// <summary>
        /// Moves an apartment to another plot: its items packed or put down again as the new plot
        /// takes them (<see cref="Repack"/>). On the same map, the copy's items are put down again
        /// and everyone in it is taken to the new entry; on another, everyone in it is sent out
        /// and the copy closes, and whoever moved it is taken into the new one once they are out.
        /// The text to tell the player.
        /// </summary>
        internal string MovePlot(Client client, ApartmentEntry row, ApartmentCatalogue.Plot plot)
        {
            if (plot == null || !plot.IsApartment)
                return "That is not an apartment plot. .apartment plot lists them.";

            if (row.PlotId == plot.Id)
                return $"Your apartment is on {plot.Name} already.";

            var live = LiveOf(row.Id);

            if (live?.Moving == true)
                return "Your apartment is moving already.";

            var items = Store.With(repository => repository.GetItems(row.Id));
            var packing = Repack(Catalogue, plot, items, Config.ItemBudget);

            Store.With(repository =>
            {
                repository.SetPacked(row.Id, packing);
                repository.Update(row.Id, entry => entry.PlotId = plot.Id);
                return 0;
            });
            row.PlotId = plot.Id;

            var kept = packing.Count(entry => !entry.Value);
            var packed = packing.Count - kept;
            var said = $"Your apartment is on {plot.Name} now: {kept} items put down, {packed} packed.";

            if (live == null)
                return said;

            foreach (var item in items)
                item.Packed = packing.TryGetValue(item.Id, out var isPacked) && isPacked;

            if (live.Plot.MapContextId == plot.MapContextId)
            {
                foreach (var placed in live.Items.Values.ToList())
                    Despawn(live, placed);

                live.Items.Clear();
                live.Packed.Clear();
                live.Row.PlotId = plot.Id;
                live.Plot = plot;
                PutDownItems(live, items);

                foreach (var inside in live.Map.ClientList.Where(c => c?.Player != null && c.State == ClientState.Ingame).ToList())
                    MoveWithin(inside, plot.Entry, plot.EntryYaw);

                return said;
            }

            // Another map: out, and in again on the new one.
            live.Moving = true;
            live.Row.PlotId = plot.Id;

            if (ReferenceEquals(client.Player.MapChannel, live.Map))
                _followMove.Add(client.Player.Id);

            return said + " You will be taken there.";
        }

        /// <summary>Somewhere else in the copy they are in, with no loading screen.</summary>
        private static void MoveWithin(Client client, Vector3 at, float yaw)
        {
            var player = client.Player;

            player.PlaceAt(at);
            player.Rotation = yaw;
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, new Movement(at, new Vector2(yaw, 0f))), false);
            CellManager.Instance.UpdateVisibility(client);
        }

        #endregion
    }
}
