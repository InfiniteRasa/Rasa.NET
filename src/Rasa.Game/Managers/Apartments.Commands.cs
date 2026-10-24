using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures.Char;

    /// <summary>
    /// The chat commands of apartments, for every player (ChatCommandsManager registers them at
    /// GmLevel.Player): .apartment, .decor and .leave, and the game masters' .aptadmin. The
    /// retail client has no window for any of it, so each answers in chat, short, and says how
    /// it is used when it is not.
    /// </summary>
    public sealed partial class Apartments
    {
        /// <summary>The most characters an apartment's owner may invite.</summary>
        public const int MaxVisitors = 100;

        /// <summary>Catalogue rows shown a page.</summary>
        public const int ListPage = 12;

        /// <summary>Items .decor near lists at most.</summary>
        public const int NearLines = 15;

        internal static readonly string[] ApartmentHelp =
        {
            "Apartment commands:",
            ".apartment - go to your apartment (made the first time). .leave - go back.",
            ".apartment visit <name> - go into someone's apartment you may enter. .apartment code <code> - go in with a passcode.",
            ".apartment list - apartments you may enter. .apartment info - about yours.",
            ".apartment invite <name> [build] / uninvite <name> / visitors - who may come in (build: may furnish it too).",
            ".apartment friends on/off - let your friends in. .apartment passcode <word>/off - a passcode for anyone.",
            ".apartment name <text>/off. .apartment plot [id] - list the plots, or move to one.",
            ".decor help - furnishing."
        };

        internal static readonly string[] DecorHelp =
        {
            "Decor commands (in an apartment you may build in):",
            ".decor cats - the categories. .decor list <category> [page]. .decor find <words>.",
            ".decor place <class id or name> [metres] - in front of you, facing you.",
            ".decor near [metres] - the items around you, by number. .decor sel <number> - choose one (or target it).",
            "On the chosen item: .decor move <dx> <dy> <dz> (map axes, metres), push <metres> (the way you face), here, turn <degrees>, face, scale <size>, copy [metres], del, info.",
            ".decor undo - your last edit. .decor packed - items packed by a plot change."
        };

        #region .apartment

        /// <summary>.apartment [what ...]</summary>
        public void ApartmentCommand(Client client, string[] parts)
        {
            if (client?.Player == null)
                return;

            var what = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
            var rest = parts.Skip(2).ToArray();

            switch (what)
            {
                case "":
                case "home":
                case "go":
                    GoHome(client);
                    return;
                case "help":
                case "?":
                    SayLines(client, ApartmentHelp);
                    return;
                case "leave":
                    Leave(client);
                    return;
            }

            if (!Config.Enabled || Store == null)
            {
                Say(client, "Apartments are not open on this server.");
                return;
            }

            switch (what)
            {
                case "visit":
                    Visit(client, string.Join(" ", rest));
                    return;
                case "code":
                    EnterByCode(client, rest.FirstOrDefault());
                    return;
                case "list":
                    ListEnterable(client);
                    return;
                case "info":
                    Info(client, OwnApartment(client, create: true), client);
                    return;
                case "invite":
                    Invite(client, rest);
                    return;
                case "uninvite":
                    Uninvite(client, string.Join(" ", rest));
                    return;
                case "visitors":
                    Visitors(client);
                    return;
                case "friends":
                    Friends(client, rest.FirstOrDefault());
                    return;
                case "passcode":
                    Passcode(client, rest.FirstOrDefault(), rest.Length);
                    return;
                case "name":
                    Rename(client, string.Join(" ", rest));
                    return;
                case "plot":
                case "plots":
                    Plot(client, rest.FirstOrDefault());
                    return;
                default:
                    Say(client, $"No such apartment command: {parts[1]}. .apartment help lists them.");
                    return;
            }
        }

        private void Visit(Client client, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Say(client, "usage: .apartment visit <character name>");
                return;
            }

            var why = WhyNotTravel(client);

            if (why != null)
            {
                Say(client, why);
                return;
            }

            var owner = FindCharacter(name);

            if (owner == null)
            {
                Say(client, $"There is no character called {name.Trim()}.");
                return;
            }

            if (owner.Value.Id == client.Player.Id)
            {
                GoHome(client);
                return;
            }

            var row = Store.With(repository => repository.FindByOwner(ApartmentEntry.OwnerKindCharacter, owner.Value.Id));

            if (row == null)
            {
                Say(client, $"{owner.Value.Name} has no apartment.");
                return;
            }

            // Asked before the copy is opened: nobody opens copies of apartments they may not enter.
            var live = LiveOf(row.Id);
            var access = live != null
                ? AccessOf(live, client)
                : AccessOf(row, Store.With(repository => repository.GetVisitors(row.Id)), owner.Value.AccountId, client);

            if (access == Access.None)
            {
                Say(client, $"{owner.Value.Name} hasn't invited you in.{(row.PasscodeHash != null ? " Their apartment takes a passcode: .apartment code <code>." : "")}");
                return;
            }

            GoInto(client, row);
        }

        private void EnterByCode(Client client, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Say(client, "usage: .apartment code <code> - the code is a word, a dash and a number, as the owner gave it.");
                return;
            }

            var why = WhyNotTravel(client);

            if (why != null)
            {
                Say(client, why);
                return;
            }

            var account = client.AccountEntry?.Id ?? 0;
            var now = Clock();
            (int Wrong, long LockedUntil, int Lockouts, long LastAt) tries = _codeTries.TryGetValue(account, out var known) ? known : (0, 0L, 0, now);

            // Lockouts in a row are forgiven an hour after the last wrong code.
            if (now - tries.LastAt > 3600_000)
                tries = (0, 0L, 0, now);

            if (now < tries.LockedUntil)
            {
                Say(client, $"Too many wrong passcodes. Try again in {(tries.LockedUntil - now + 999) / 1000} seconds.");
                return;
            }

            var dash = code.LastIndexOf('-');
            var row = dash > 0 && uint.TryParse(code.Substring(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? Store.With(repository => repository.Get(id))
                : null;
            var right = row?.PasscodeHash != null && string.Equals(row.PasscodeHash, PasscodeHash(row.Id, code.Substring(0, dash)), StringComparison.Ordinal);

            if (!right)
            {
                var wrong = tries.Wrong + 1;
                var lockouts = tries.Lockouts;
                var lockedUntil = 0L;

                if (wrong >= Math.Max(1, Config.PasscodeTries))
                {
                    var seconds = Math.Min(3600L, Math.Max(1, Config.PasscodeLockoutSeconds) * (1L << Math.Min(lockouts, 16)));

                    lockedUntil = now + seconds * 1000;
                    lockouts++;
                    wrong = 0;
                }

                _codeTries[account] = (wrong, lockedUntil, lockouts, now);
                Say(client, "That code does not open any apartment.");
                Logger.WriteLog(LogType.Security, $"Apartments: {client.Player.Name} (account {account}) gave a wrong apartment code.");
                return;
            }

            _codeTries[account] = (0, 0, tries.Lockouts, now);

            if (row.OwnerKind == ApartmentEntry.OwnerKindCharacter && row.OwnerId == client.Player.Id)
            {
                GoHome(client);
                return;
            }

            GoInto(client, row, viaCode: code);
        }

        private void ListEnterable(Client client)
        {
            var own = OwnApartment(client, create: false);
            var invited = Store.With(repository => repository.InvitedInto(client.Player.Id));
            var lines = new List<string> { "Apartments you may enter:" };

            lines.Add(own == null
                ? "  yours - not made yet: .apartment makes it"
                : $"  yours{(own.Name.Length > 0 ? $" \"{own.Name}\"" : "")} on {Catalogue.PlotOf(own.PlotId)?.Name ?? "?"}: .apartment");

            var visitors = invited.ToDictionary(row => row.Id, row => Store.With(repository => repository.GetVisitors(row.Id)).FirstOrDefault(v => v.CharacterId == client.Player.Id));

            foreach (var row in invited.Take(20))
            {
                var name = NameOf(row.OwnerId);
                var build = visitors.TryGetValue(row.Id, out var visitor) && visitor?.MayBuild == true ? ", you may build" : "";

                lines.Add($"  {name}'s{(row.Name.Length > 0 ? $" \"{row.Name}\"" : "")}{build}: .apartment visit {name}");
            }

            if (invited.Count > 20)
                lines.Add($"  ... and {invited.Count - 20} more.");

            lines.Add("A friend's apartment that lets friends in, or one with a passcode, is not listed: .apartment visit <name>, .apartment code <code>.");
            SayLines(client, lines);
        }

        /// <summary>What an apartment is: for its owner (.apartment info) or a game master (.aptadmin).</summary>
        internal void Info(Client client, ApartmentEntry row, Client asking)
        {
            if (row == null)
            {
                Say(asking, "There is no apartment.");
                return;
            }

            var plot = Catalogue.PlotOf(row.PlotId);
            var items = Store.With(repository => repository.GetItems(row.Id));
            var visitors = Store.With(repository => repository.GetVisitors(row.Id));
            var live = LiveOf(row.Id);
            var budget = ApartmentCatalogue.BudgetOf(plot, Config.ItemBudget);
            var inside = live?.Map.ClientList.Where(c => c?.Player != null).Select(c => c.Player.Name).ToList() ?? new List<string>();

            SayLines(asking, new[]
            {
                $"Apartment {row.Id} of {NameOf(row.OwnerId)}{(row.Name.Length > 0 ? $", \"{row.Name}\"" : "")}: plot {plot?.Id} {plot?.Name ?? "(gone)"}, map {plot?.MapContextId}.",
                $"Items: {items.Count(i => !i.Packed)}/{budget} put down, {items.Count(i => i.Packed)} packed. Visitors: {visitors.Count} ({visitors.Count(v => v.MayBuild)} may build). Friends: {(row.FriendsMayEnter ? "may enter" : "may not enter")}. Passcode: {(row.PasscodeHash != null ? $"on (code word-{row.Id})" : "off")}.",
                live == null ? "Not open now." : $"Open (instance {live.Map.InstanceId}), inside: {(inside.Count == 0 ? "nobody" : string.Join(", ", inside))}."
            });
        }

        /// <summary>The apartment a player manages: their own, made if need be. Null, with them told, when it cannot be made.</summary>
        private ApartmentEntry Managed(Client client)
        {
            var row = OwnApartment(client, create: true);

            if (row == null)
                Say(client, "Your apartment could not be made.");

            return row;
        }

        /// <summary>Changes the apartment's row, and the open copy's.</summary>
        private void UpdateRow(ApartmentEntry row, Action<ApartmentEntry> change)
        {
            Store.With(repository => repository.Update(row.Id, change));
            change(row);

            if (LiveOf(row.Id) is { } live && !ReferenceEquals(live.Row, row))
                change(live.Row);
        }

        private void Invite(Client client, string[] rest)
        {
            var build = rest.Length > 1 && string.Equals(rest[^1], "build", StringComparison.OrdinalIgnoreCase);
            var name = string.Join(" ", build ? rest.Take(rest.Length - 1) : rest);

            if (string.IsNullOrWhiteSpace(name))
            {
                Say(client, "usage: .apartment invite <character name> [build]");
                return;
            }

            var row = Managed(client);
            var guest = row == null ? null : FindCharacter(name);

            if (row == null)
                return;

            if (guest == null)
            {
                Say(client, $"There is no character called {name.Trim()}.");
                return;
            }

            if (guest.Value.Id == client.Player.Id)
            {
                Say(client, "It is your own apartment.");
                return;
            }

            var visitors = Store.With(repository => repository.GetVisitors(row.Id));

            if (visitors.Count >= MaxVisitors && visitors.All(v => v.CharacterId != guest.Value.Id))
            {
                Say(client, $"You have invited {MaxVisitors} characters already. .apartment uninvite one first.");
                return;
            }

            Store.With(repository => { repository.SetVisitor(row.Id, guest.Value.Id, build, UtcNowMs()); return 0; });
            RefreshVisitors(row.Id);
            _names[guest.Value.Id] = guest.Value.Name;

            Say(client, $"{guest.Value.Name} may come into your apartment{(build ? " and furnish it" : "")}.");

            var online = OnlineById(guest.Value.Id);

            if (online != null)
                Say(online, $"{client.Player.Name} has invited you into their apartment{(build ? ", and you may furnish it" : "")}: .apartment visit {client.Player.Name}");
        }

        private void Uninvite(Client client, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Say(client, "usage: .apartment uninvite <character name>");
                return;
            }

            var row = Managed(client);

            if (row == null)
                return;

            var guest = FindCharacter(name);

            if (guest == null || !Store.With(repository => repository.RemoveVisitor(row.Id, guest.Value.Id)))
            {
                Say(client, $"{guest?.Name ?? name.Trim()} is not invited.");
                return;
            }

            RefreshVisitors(row.Id);
            Say(client, $"{guest.Value.Name} may no longer come in.");
        }

        private void RefreshVisitors(uint apartmentId)
        {
            if (LiveOf(apartmentId) is { } live)
                live.Visitors = Store.With(repository => repository.GetVisitors(apartmentId));
        }

        private void Visitors(Client client)
        {
            var row = Managed(client);

            if (row == null)
                return;

            var visitors = Store.With(repository => repository.GetVisitors(row.Id));

            if (visitors.Count == 0)
            {
                Say(client, "You have invited nobody. .apartment invite <name> [build]");
                return;
            }

            SayLines(client, new[] { $"Invited ({visitors.Count}):" }
                .Concat(visitors.Take(30).Select(v => $"  {NameOf(v.CharacterId)}{(v.MayBuild ? " (may build)" : "")}"))
                .Concat(visitors.Count > 30 ? new[] { $"  ... and {visitors.Count - 30} more." } : Array.Empty<string>()));
        }

        private void Friends(Client client, string onOff)
        {
            var on = string.Equals(onOff, "on", StringComparison.OrdinalIgnoreCase);

            if (!on && !string.Equals(onOff, "off", StringComparison.OrdinalIgnoreCase))
            {
                Say(client, "usage: .apartment friends on/off");
                return;
            }

            var row = Managed(client);

            if (row == null)
                return;

            UpdateRow(row, entry => entry.FriendsMayEnter = on);
            LiveOf(row.Id)?.Friends.Clear();
            Say(client, on ? "Your friends may come into your apartment." : "Your friends may no longer come in uninvited.");
        }

        private void Passcode(Client client, string word, int words)
        {
            if (string.IsNullOrWhiteSpace(word) || words > 1)
            {
                Say(client, "usage: .apartment passcode <word>/off - a word of 4 to 32 letters and digits.");
                return;
            }

            var row = Managed(client);

            if (row == null)
                return;

            // Whoever came in by the old code has to have the new one.
            _passes.Remove(row.Id);

            if (string.Equals(word, "off", StringComparison.OrdinalIgnoreCase))
            {
                UpdateRow(row, entry => entry.PasscodeHash = null);
                Say(client, "Your apartment has no passcode now.");
                return;
            }

            if (!IsPasscodeWord(word))
            {
                Say(client, "A passcode is one word of 4 to 32 letters and digits.");
                return;
            }

            var hash = PasscodeHash(row.Id, word);

            UpdateRow(row, entry => entry.PasscodeHash = hash);
            Say(client, $"Passcode set. Anyone may come in with: .apartment code {word.ToLowerInvariant()}-{row.Id}");
        }

        private void Rename(Client client, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Say(client, $"usage: .apartment name <text>/off - up to {ApartmentEntry.NameLength} characters.");
                return;
            }

            var row = Managed(client);

            if (row == null)
                return;

            if (string.Equals(name.Trim(), "off", StringComparison.OrdinalIgnoreCase))
            {
                UpdateRow(row, entry => entry.Name = "");
                Say(client, "Your apartment goes by your name now.");
                return;
            }

            if (!IsName(name))
            {
                Say(client, $"A name is up to {ApartmentEntry.NameLength} characters, none of them control characters.");
                return;
            }

            var trimmed = name.Trim();

            UpdateRow(row, entry => entry.Name = trimmed);
            Say(client, $"Your apartment is \"{trimmed}\" now.");
        }

        private void Plot(Client client, string id)
        {
            var row = Managed(client);

            if (row == null)
                return;

            if (string.IsNullOrWhiteSpace(id))
            {
                SayLines(client, new[] { "Apartment plots (.apartment plot <id>):" }
                    .Concat(Catalogue.ApartmentPlots.Select(plot =>
                        $"  {plot.Id} {plot.Name} - {Maps().FindByContextId(plot.MapContextId)?.MapInfo.MapName ?? $"map {plot.MapContextId}"}, {ApartmentCatalogue.BudgetOf(plot, Config.ItemBudget)} items{(plot.Id == row.PlotId ? " (yours)" : "")}")));
                return;
            }

            if (!uint.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var plotId))
            {
                Say(client, "usage: .apartment plot [id]");
                return;
            }

            Say(client, MovePlot(client, row, Catalogue.PlotOf(plotId)));
        }

        #endregion

        #region .decor

        /// <summary>.decor what [...]</summary>
        public void DecorCommand(Client client, string[] parts)
        {
            if (client?.Player == null)
                return;

            var what = parts.Length > 1 ? parts[1].ToLowerInvariant() : "help";
            var rest = parts.Skip(2).ToArray();

            switch (what)
            {
                case "help":
                case "?":
                    SayLines(client, DecorHelp);
                    return;
                case "cats":
                case "categories":
                    SayLines(client, new[] { "Categories (.decor list <category>):" }
                        .Concat(Catalogue.Categories.Select(c => $"  {c.Category} ({c.Count})")));
                    return;
                case "list":
                    ListCategory(client, rest);
                    return;
                case "find":
                    FindItems(client, string.Join(" ", rest));
                    return;
            }

            var live = BuildingIn(client);

            if (live == null)
                return;

            switch (what)
            {
                case "place":
                case "add":
                    Place(client, live, rest);
                    return;
                case "near":
                    Near(client, live, rest.FirstOrDefault());
                    return;
                case "sel":
                case "select":
                    SelectByNumber(client, live, rest.FirstOrDefault());
                    return;
                case "undo":
                    Say(client, UndoLast(client, live));
                    return;
                case "packed":
                    Packed(client, live);
                    return;
            }

            var placed = Current(client, live);

            if (placed == null)
                return;

            switch (what)
            {
                case "info":
                    var row = placed.Row;
                    Say(client, $"{Describe(live, placed)}: class {row.ClassId}, {placed.Decor.Category}, at {row.PosX:0.##} {row.PosY:0.##} {row.PosZ:0.##}, turned {row.Rotation * 180 / Math.PI:0}, scale {row.Scale:0.##} ({placed.Decor.ScaleMin:0.##}-{placed.Decor.ScaleMax:0.##}), placed by {NameOf(row.PlacedBy)}.");
                    return;
                case "move":
                    if (rest.Length != 3 || !TryNumber(rest[0], out var dx) || !TryNumber(rest[1], out var dy) || !TryNumber(rest[2], out var dz) || !Within(Config.Reach, dx, dy, dz))
                    {
                        Say(client, $"usage: .decor move <dx> <dy> <dz> - metres along the map's axes, each within {Config.Reach:0}.");
                        return;
                    }

                    Say(client, Change(client, live, placed, r => { r.PosX += dx; r.PosY += dy; r.PosZ += dz; }, "moved"));
                    return;
                case "push":
                    if (rest.Length != 1 || !TryNumber(rest[0], out var metres) || !Within(Config.Reach, metres))
                    {
                        Say(client, $"usage: .decor push <metres> - the way you face (less than 0 towards you), within {Config.Reach:0}.");
                        return;
                    }

                    var along = Forward(client.Player.Rotation) * (float)metres;
                    Say(client, Change(client, live, placed, r => { r.PosX += along.X; r.PosZ += along.Z; }, "pushed"));
                    return;
                case "up":
                case "down":
                    if (rest.Length != 1 || !TryNumber(rest[0], out var height) || !Within(Config.Reach, height))
                    {
                        Say(client, $"usage: .decor {what} <metres>");
                        return;
                    }

                    var dh = what == "up" ? height : -height;
                    Say(client, Change(client, live, placed, r => r.PosY += dh, "moved"));
                    return;
                case "here":
                    var at = client.Player.Position;
                    Say(client, Change(client, live, placed, r => { r.PosX = at.X; r.PosY = at.Y; r.PosZ = at.Z; }, "moved"));
                    return;
                case "turn":
                    if (rest.Length != 1 || !TryNumber(rest[0], out var degrees) || !Within(3600, degrees))
                    {
                        Say(client, "usage: .decor turn <degrees>");
                        return;
                    }

                    Say(client, Change(client, live, placed, r => r.Rotation += degrees * Math.PI / 180, "turned"));
                    return;
                case "face":
                    var towards = YawTowards(PositionOf(placed.Row), client.Player.Position);
                    Say(client, Change(client, live, placed, r => r.Rotation = towards, "turned"));
                    return;
                case "scale":
                case "size":
                    if (rest.Length != 1 || !TryNumber(rest[0], out var scale) || scale <= 0)
                    {
                        Say(client, $"usage: .decor scale <size> - {placed.Decor.Name} goes from {placed.Decor.ScaleMin:0.##} to {placed.Decor.ScaleMax:0.##}.");
                        return;
                    }

                    Say(client, Change(client, live, placed, r => r.Scale = scale, "sized"));
                    return;
                case "del":
                case "delete":
                case "remove":
                    Say(client, Delete(client, live, placed));
                    return;
                case "copy":
                    var distance = Config.PlaceDistance;

                    if (rest.Length > 1 || rest.Length == 1 && !TryNumber(rest[0], out distance))
                    {
                        Say(client, "usage: .decor copy [metres]");
                        return;
                    }

                    Say(client, CopyItem(client, live, placed, distance));
                    return;
                default:
                    Say(client, $"No such decor command: {parts[1]}. .decor help lists them.");
                    return;
            }
        }

        private void ListCategory(Client client, string[] rest)
        {
            var page = 1;
            var words = rest.ToList();

            if (words.Count > 1 && int.TryParse(words[^1], out var asked))
            {
                page = asked;
                words.RemoveAt(words.Count - 1);
            }

            var category = string.Join(" ", words);
            var items = Catalogue.InCategory(category);

            if (items.Count == 0)
            {
                Say(client, string.IsNullOrWhiteSpace(category) ? "usage: .decor list <category> [page] - .decor cats lists the categories." : $"There is no category {category}. .decor cats lists them.");
                return;
            }

            var pages = (items.Count + ListPage - 1) / ListPage;

            page = Math.Clamp(page, 1, pages);

            SayLines(client, new[] { $"{items[0].Category}, page {page} of {pages} (.decor place <id>):" }
                .Concat(items.Skip((page - 1) * ListPage).Take(ListPage).Select(Line)));
        }

        private static string Line(ApartmentCatalogue.Decor decor)
        {
            var notes = new List<string>();

            if (decor.Surface != 1)
                notes.Add(decor.SurfaceName);

            if (decor.Function == DecorFunction.Storage)
                notes.Add("opens your vault");
            else if (decor.Function == DecorFunction.Crafting)
                notes.Add("crafting");
            else if (decor.Function == DecorFunction.Medbay)
                notes.Add("revive point");

            if (decor.ScaleMin != decor.ScaleMax)
                notes.Add($"scale {decor.ScaleMin:0.##}-{decor.ScaleMax:0.##}");

            return $"  {decor.ClassId} {decor.Name}{(notes.Count > 0 ? $" ({string.Join(", ", notes)})" : "")}";
        }

        private void FindItems(Client client, string words)
        {
            var found = Catalogue.Find(words);

            if (found.Count == 0)
            {
                Say(client, string.IsNullOrWhiteSpace(words) ? "usage: .decor find <words>" : $"Nothing in the catalogue is called {words.Trim()}.");
                return;
            }

            SayLines(client, new[] { $"{found.Count} found:" }.Concat(found.Take(ListPage).Select(d => Line(d) + $" - {d.Category}"))
                .Concat(found.Count > ListPage ? new[] { $"  ... and {found.Count - ListPage} more: say more of the name." } : Array.Empty<string>()));
        }

        private void Place(Client client, Live live, string[] rest)
        {
            var words = rest.ToList();
            var distance = Config.PlaceDistance;

            if (words.Count > 1 && TryNumber(words[^1], out var asked))
            {
                distance = asked;
                words.RemoveAt(words.Count - 1);
            }

            var name = string.Join(" ", words);
            var found = Catalogue.Find(name);

            if (found.Count == 0)
            {
                Say(client, string.IsNullOrWhiteSpace(name) ? "usage: .decor place <class id or name> [metres]" : $"Nothing in the catalogue is called {name.Trim()}. .decor find <words>");
                return;
            }

            if (found.Count > 1)
            {
                SayLines(client, new[] { $"{found.Count} items are called that; place one by its id:" }.Concat(found.Take(8).Select(Line)));
                return;
            }

            Say(client, PlaceNew(client, live, found[0], distance));
        }

        private void Near(Client client, Live live, string radiusText)
        {
            var radius = 10.0;

            if (radiusText != null && (!TryNumber(radiusText, out radius) || radius <= 0))
            {
                Say(client, "usage: .decor near [metres]");
                return;
            }

            var here = client.Player.Position;
            var near = live.Items.Values
                .Select(p => (Placed: p, Distance: System.Numerics.Vector3.Distance(here, PositionOf(p.Row))))
                .Where(p => p.Distance <= radius)
                .OrderBy(p => p.Distance)
                .ToList();

            if (near.Count == 0)
            {
                Say(client, $"No items within {radius:0.#} metres. ({live.Items.Count} in the apartment.)");
                return;
            }

            SayLines(client, new[] { $"Within {radius:0.#} m (.decor sel <number>):" }
                .Concat(near.Take(NearLines).Select(p => $"  {Describe(live, p.Placed)} {p.Distance:0.0} m"))
                .Concat(near.Count > NearLines ? new[] { $"  ... and {near.Count - NearLines} more further off." } : Array.Empty<string>()));
        }

        private void SelectByNumber(Client client, Live live, string number)
        {
            if (number == null)
            {
                var current = Current(client, live);

                if (current != null)
                    Say(client, $"Chosen: {Describe(live, current)}.");

                return;
            }

            if (!uint.TryParse(number.TrimStart('#'), NumberStyles.None, CultureInfo.InvariantCulture, out var n) || !live.Items.TryGetValue(n, out var placed))
            {
                Say(client, live.Packed.ContainsKey(n) ? $"#{n} is packed: move to a plot it fits on." : $"There is no item #{number.TrimStart('#')} here. .decor near lists them.");
                return;
            }

            Select(client, live, placed);
            Say(client, $"Chosen: {Describe(live, placed)}, {System.Numerics.Vector3.Distance(client.Player.Position, PositionOf(placed.Row)):0.0} m away.");
        }

        private void Packed(Client client, Live live)
        {
            if (live.Packed.Count == 0)
            {
                Say(client, "Nothing is packed.");
                return;
            }

            SayLines(client, new[] { $"Packed ({live.Packed.Count}) - kept, put down again on a plot they fit:" }
                .Concat(live.Packed.Values.OrderBy(r => r.Number).Take(20).Select(r => $"  #{r.Number} {Catalogue.DecorOf(r.ClassId)?.Name ?? $"class {r.ClassId}"}")));
        }

        #endregion

        #region .aptadmin

        /// <summary>.aptadmin &lt;name or #id&gt; [info | goto | kick | reset | plot &lt;id&gt;]: a game master's look at, and hand on, a character's apartment.</summary>
        public void AdminCommand(Client gm, string[] parts)
        {
            if (gm?.Player == null)
                return;

            if (parts.Length < 2)
            {
                Say(gm, "usage: .aptadmin <character name or #id> [info/goto/kick/reset/plot <id>]");
                return;
            }

            if (Store == null)
            {
                Say(gm, "Apartments are not open on this server.");
                return;
            }

            var owner = FindCharacter(parts[1]);
            var row = owner == null ? null : Store.With(repository => repository.FindByOwner(ApartmentEntry.OwnerKindCharacter, owner.Value.Id));

            if (owner == null)
            {
                Say(gm, $"There is no character {parts[1]}.");
                return;
            }

            if (row == null)
            {
                Say(gm, $"{owner.Value.Name} has no apartment.");
                return;
            }

            var what = parts.Length > 2 ? parts[2].ToLowerInvariant() : "info";
            var live = LiveOf(row.Id);

            switch (what)
            {
                case "info":
                    Info(gm, row, gm);
                    return;
                case "goto":
                    var why = WhyNotTravel(gm);

                    if (why != null)
                    {
                        Say(gm, why);
                        return;
                    }

                    // A game master comes in as a passcode holder does: until they leave.
                    if (!_passes.TryGetValue(row.Id, out var passes))
                        _passes[row.Id] = passes = new HashSet<uint>();

                    passes.Add(gm.Player.Id);
                    GoInto(gm, row);
                    return;
                case "kick":
                    var sent = 0;

                    if (live != null)
                        foreach (var inside in live.Map.ClientList.Where(c => c?.Player != null && c.Player.Id != row.OwnerId && c.State == ClientState.Ingame).ToList())
                            if (SendOut(inside, "A game master has sent you out of the apartment."))
                                sent++;

                    _passes.Remove(row.Id);
                    Say(gm, $"Sent {sent} visitor(s) out of {owner.Value.Name}'s apartment.");
                    return;
                case "reset":
                    // Out, everyone, and it closes; then as new: no items, nobody invited, no
                    // passcode or name, friends kept out, the first plot.
                    if (live != null)
                    {
                        live.Moving = true;

                        foreach (var inside in live.Map.ClientList.Where(c => c?.Player != null && c.State == ClientState.Ingame).ToList())
                            SendOut(inside, "Your apartment has been reset by a game master.");
                    }

                    var removed = Store.With(repository => repository.RemoveItems(row.Id));

                    foreach (var visitor in Store.With(repository => repository.GetVisitors(row.Id)))
                        Store.With(repository => repository.RemoveVisitor(row.Id, visitor.CharacterId));

                    var first = Catalogue.ApartmentPlots.FirstOrDefault();

                    UpdateRow(row, entry =>
                    {
                        entry.Name = "";
                        entry.PasscodeHash = null;
                        entry.FriendsMayEnter = false;
                        entry.PlotId = first?.Id ?? entry.PlotId;
                    });
                    _passes.Remove(row.Id);

                    Say(gm, $"{owner.Value.Name}'s apartment {row.Id} is reset: {removed} items deleted, invitations, passcode and name gone, plot {first?.Id}.");
                    Logger.WriteLog(LogType.Command, $"[.aptadmin] {gm.Player.Name} reset apartment {row.Id} of {owner.Value.Name} ({owner.Value.Id}): {removed} items deleted.");
                    return;
                case "plot":
                    if (parts.Length < 4 || !uint.TryParse(parts[3], out var plotId))
                    {
                        Say(gm, "usage: .aptadmin <name> plot <id>");
                        return;
                    }

                    Say(gm, MovePlot(gm, row, Catalogue.PlotOf(plotId)));
                    return;
                default:
                    Say(gm, "usage: .aptadmin <character name or #id> [info/goto/kick/reset/plot <id>]");
                    return;
            }
        }

        #endregion

        private void SayLines(Client client, IEnumerable<string> lines) => Say(client, string.Join("\n", lines));

        private static bool TryNumber(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

        private static bool Within(double limit, params double[] values) => values.All(v => Math.Abs(v) <= limit);
    }
}
