using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Repositories.Char.Apartment;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Bootcamp = Rasa.Missions.Content.Bootcamp.BootcampEntryDefinition;

    /// <summary>
    /// Where the apartments are kept (the character database's apartment tables): something done
    /// with their repository, saved as it is done.
    /// </summary>
    public interface IApartmentStore
    {
        T With<T>(Func<IApartmentRepository, T> action);
    }

    /// <summary>The server's store: a character unit of work for each thing done.</summary>
    internal sealed class DatabaseApartmentStore : IApartmentStore
    {
        private readonly IGameUnitOfWorkFactory _factory;

        internal DatabaseApartmentStore(IGameUnitOfWorkFactory factory) => _factory = factory;

        public T With<T>(Func<IApartmentRepository, T> action)
        {
            using var unitOfWork = _factory.CreateChar();

            return action(unitOfWork.Apartments);
        }
    }

    /// <summary>
    /// Players' apartments, for the unmodified retail client.
    ///
    /// The retail client's own housing was cut before 1.16.5 (see the handlers of
    /// RequestPlaceObject and RequestReturnItemToInventory): nothing in it can place a decoration
    /// or open an apartment window. Everything here is therefore done by the server with what the
    /// client does have - instances of retail maps, entities of retail classes, chat:
    ///
    ///  - An apartment stands on a plot (<see cref="ApartmentCatalogue"/>): a part of a retail
    ///    map with a build box and an entry. It is a copy of that map of its own (MapChannelManager's
    ///    apartment copies), blank - none of the map's creatures or objects - holding the
    ///    apartment's items. One copy an apartment, whoever goes in: visitors go into the owner's.
    ///    It opens as the first person goes in and closes once it has stood empty for
    ///    <see cref="ApartmentConfig.IdleCloseSeconds"/>; the items are put down again from the
    ///    database each time it opens.
    ///  - Every character has one, made on their first .apartment, on the plot of the lowest id.
    ///    The owner may move it to another plot: items outside the new build box, or over the new
    ///    plot's budget or caps, are packed - kept, not put down - and come back when a plot they
    ///    fit is chosen again.
    ///  - .apartment (or the retail SendMeToMyApartment) takes a player in from anywhere but a
    ///    private instance, unless they are dead, fighting or between maps; .leave takes them back
    ///    to where they came from (apartment_return, so a logout inside and a login later work:
    ///    back inside, or at that place if they may no longer go in). Dying inside, a player gets
    ///    up at the entry, or beside a medbay item if there is one (InstanceEntrances).
    ///  - The owner has every right. Others come in invited (with or without the right to build),
    ///    as the owner's friend while friends may, or with the passcode while the owner has one
    ///    (Homecoming's way: the word's hash is kept, the code given out is word-id). Wrong codes
    ///    lock an account out of codes for a while. Anyone inside who may no longer come in is
    ///    sent out within a second.
    ///  - Items are DynamicObjects of the catalogue's retail classes (Apartments.Decor), put down
    ///    in the copy only. Each has a number in its apartment, and is edited by number, by
    ///    being the one last chosen, or by being the player's target. Their yaw is kept; their
    ///    scale is sent as BodyAttributes, the one way the client sizes an entity the server made.
    ///    A few classes work: a footlocker opens the player's own vault, a crafting station the
    ///    crafting window, and a medbay item is where a player who dies inside gets up.
    ///  - Nothing costs anything.
    /// </summary>
    public sealed partial class Apartments
    {
        private static Apartments _instance;
        private static readonly object InstanceLock = new object();

        public static Apartments Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new Apartments();
                    }
                }

                return _instance;
            }
        }

        /// <summary>The values in force (appsettings.json's Apartments); replaced whole when the file changes.</summary>
        public static ApartmentConfig Config { get; set; } = new ApartmentConfig();

        internal Apartments()
        {
        }

        /// <summary>Puts a manager in the server's place, for tests; the one before is given back.</summary>
        internal static Apartments Swap(Apartments apartments)
        {
            var before = _instance;
            _instance = apartments;
            return before;
        }

        /// <summary>The plots and the decoration catalogue; empty until <see cref="Init"/> has read them.</summary>
        public ApartmentCatalogue Catalogue { get; set; } = new ApartmentCatalogue();

        /// <summary>Where the apartments are kept; null, as in tests that do not give one, keeps nothing and lets nobody in.</summary>
        public IApartmentStore Store { get; set; }

        /// <summary>The maps (MapChannelManager); replaceable for tests.</summary>
        public Func<MapChannelManager> Maps { get; set; } = () => MapChannelManager.Instance;

        /// <summary>The manager's clock, in milliseconds.</summary>
        public Func<long> Clock { get; set; } = () => Environment.TickCount64;

        /// <summary>The time on the wall, Unix milliseconds, for the rows.</summary>
        public Func<long> UtcNowMs { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>Whether the second account is on the first's friend list (SocialManager's rows); replaceable for tests.</summary>
        public Func<uint, uint, bool> IsFriendOf { get; set; } = (owner, visitor) => false;

        /// <summary>The players in the world, for finding one by name.</summary>
        public Func<IEnumerable<Client>> Online { get; set; } = () =>
        {
            lock (Server.Clients)
                return Server.Clients.Where(client => client?.State == ClientState.Ingame && client.Player != null).ToList();
        };

        /// <summary>A line in a player's chat.</summary>
        public Action<Client, string> Say { get; set; } = (client, text) => CommunicatorManager.Instance.SystemMessage(client, text);

        /// <summary>One of the client's own player messages (PmFailedToCreateApartment, PmFailedToLoadApartment).</summary>
        public Action<Client, PlayerMessage> Tell { get; set; } = (client, message) => CommunicatorManager.Instance.DisplaySystemMessage(client, message);

        /// <summary>An apartment's copy that stands, and what is in it.</summary>
        internal sealed class Live
        {
            public ApartmentEntry Row;
            public ApartmentCatalogue.Plot Plot;
            public MapChannel Map;
            public List<ApartmentVisitorEntry> Visitors = new List<ApartmentVisitorEntry>();
            public uint OwnerAccountId;
            public string OwnerName;

            /// <summary>The items put down, by number.</summary>
            public readonly Dictionary<uint, Placed> Items = new Dictionary<uint, Placed>();

            /// <summary>The packed items, by number: kept, not put down.</summary>
            public readonly Dictionary<uint, ApartmentItemEntry> Packed = new Dictionary<uint, ApartmentItemEntry>();

            /// <summary>The characters told where they are since they arrived.</summary>
            public readonly HashSet<uint> Greeted = new HashSet<uint>();

            /// <summary>Its plot has moved to another map: everyone is sent out, and it closes as it empties.</summary>
            public bool Moving;

            /// <summary>The friends of the owner looked up, and until when they count.</summary>
            public readonly Dictionary<uint, (bool Friend, long Until)> Friends = new Dictionary<uint, (bool, long)>();

            public string Title => Row.Name.Length > 0 ? Row.Name : $"{OwnerName}'s apartment";
        }

        /// <summary>An item put down: its row, its catalogue entry and its entity in the copy.</summary>
        internal sealed class Placed
        {
            public ApartmentItemEntry Row;
            public ApartmentCatalogue.Decor Decor;
            public DynamicObject Object;
        }

        /// <summary>What a character may do in an apartment.</summary>
        public enum Access
        {
            None = 0,
            Enter = 1,
            Build = 2,
            Owner = 3
        }

        private readonly Dictionary<uint, Live> _live = new Dictionary<uint, Live>();

        /// <summary>The characters who came in by the passcode, by apartment: until they leave, or it changes.</summary>
        private readonly Dictionary<uint, HashSet<uint>> _passes = new Dictionary<uint, HashSet<uint>>();

        /// <summary>Wrong passcodes by account: how many, until when they are locked out, and how many lockouts in a row.</summary>
        private readonly Dictionary<uint, (int Wrong, long LockedUntil, int Lockouts, long LastAt)> _codeTries = new Dictionary<uint, (int, long, int, long)>();

        /// <summary>The characters in the world inside an apartment, whose return point is kept: by character.</summary>
        private readonly Dictionary<uint, uint> _inside = new Dictionary<uint, uint>();

        /// <summary>Characters to be taken into their own apartment as soon as they have arrived outside it: its plot moved map.</summary>
        private readonly HashSet<uint> _followMove = new HashSet<uint>();

        /// <summary>When each open copy was last seen with nobody in it, by apartment.</summary>
        private readonly Dictionary<uint, long> _emptySince = new Dictionary<uint, long>();

        /// <summary>The names of characters looked up, by id.</summary>
        private readonly Dictionary<uint, string> _names = new Dictionary<uint, string>();

        /// <summary>Reads the plots and the catalogue and makes the store: once, after the maps and the entity classes are loaded.</summary>
        public void Init(IGameUnitOfWorkFactory factory)
        {
            Store = new DatabaseApartmentStore(factory);
            IsFriendOf = (owner, visitor) =>
            {
                using var unitOfWork = factory.CreateChar();
                return unitOfWork.Friends.GetFriends(owner).Contains(visitor);
            };

            var directory = Config.ContentDirectory ?? "apartments";

            if (!Path.IsPathRooted(directory))
                directory = Path.Combine(AppContext.BaseDirectory, directory);

            Catalogue = ApartmentCatalogue.Load(directory,
                map => MapChannelManager.Instance.MapChannelArray.ContainsKey(map),
                classId => EntityClassManager.Instance.LoadedEntityClasses.ContainsKey((EntityClasses)classId));

            foreach (var problem in Catalogue.Problems)
                Logger.WriteLog(LogType.Error, $"Apartments: {problem}");

            Logger.WriteLog(LogType.Initialize,
                $"Apartments: {Catalogue.ApartmentPlots.Count} apartment plots, {Catalogue.Items.Count} catalogue items in {Catalogue.Categories.Count} categories.");
        }

        #region Who may come in

        /// <summary>What a character may do in an apartment: the owner all, the invited enter or build, a friend or a passcode holder enter.</summary>
        internal Access AccessOf(Live live, Client client) =>
            live == null ? Access.None : AccessOf(live.Row, live.Visitors, live.OwnerAccountId, client, live);

        private Access AccessOf(ApartmentEntry row, List<ApartmentVisitorEntry> visitors, uint ownerAccountId, Client client, Live live = null)
        {
            var player = client?.Player;

            if (row == null || player == null || player.Id == 0)
                return Access.None;

            if (row.OwnerKind == ApartmentEntry.OwnerKindCharacter && row.OwnerId == player.Id)
                return Access.Owner;

            var visitor = visitors?.FirstOrDefault(v => v.CharacterId == player.Id);

            if (visitor != null)
                return visitor.MayBuild ? Access.Build : Access.Enter;

            if (_passes.TryGetValue(row.Id, out var passes) && passes.Contains(player.Id))
                return Access.Enter;

            if (row.FriendsMayEnter && client.AccountEntry != null && ownerAccountId != 0 && IsFriend(live, ownerAccountId, client.AccountEntry.Id))
                return Access.Enter;

            // A game master may be anywhere a player is: .goto and .summon bring one in, and is
            // not sent out again by the worker. They may not build.
            if (client.AccountEntry != null && client.AccountEntry.Level >= (byte)GmLevel.GameMaster)
                return Access.Enter;

            return Access.None;
        }

        /// <summary>Whether the owner's account has the visitor's on its friend list: asked again at most every 30 seconds for an open copy.</summary>
        private bool IsFriend(Live live, uint ownerAccountId, uint visitorAccountId)
        {
            var now = Clock();

            if (live != null && live.Friends.TryGetValue(visitorAccountId, out var known) && now < known.Until)
                return known.Friend;

            bool friend;

            try
            {
                friend = ownerAccountId != visitorAccountId && IsFriendOf(ownerAccountId, visitorAccountId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Apartments: the friends of account {ownerAccountId} could not be read: {e.Message}");
                friend = false;
            }

            if (live != null)
                live.Friends[visitorAccountId] = (friend, now + 30_000);

            return friend;
        }

        /// <summary>The passcode's hash, as kept: SHA-256 of "id:word", the word lower-cased, in hex.</summary>
        public static string PasscodeHash(uint apartmentId, string word) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{apartmentId}:{word.Trim().ToLowerInvariant()}"))).ToLowerInvariant();

        /// <summary>A word that may be a passcode: 4 to 32 letters and digits.</summary>
        public static bool IsPasscodeWord(string word) =>
            word != null && word.Length >= 4 && word.Length <= 32 && word.All(char.IsLetterOrDigit);

        /// <summary>A name an apartment may be given: 1 to 48 characters, none of them a control character, not blank.</summary>
        public static bool IsName(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= ApartmentEntry.NameLength && !name.Any(c => char.IsControl(c) || c == '|');

        #endregion

        #region Opening and closing

        internal Live LiveOf(uint apartmentId) => _live.TryGetValue(apartmentId, out var live) ? live : null;

        /// <summary>The open apartment a map channel is the copy of, or null.</summary>
        internal Live LiveOf(MapChannel map) => map != null && map.IsApartment ? LiveOf(map.ApartmentId) : null;

        /// <summary>The apartment's plot: its own, or - one that is gone from the tables - the first apartment plot, which it is moved to.</summary>
        private ApartmentCatalogue.Plot PlotFor(ApartmentEntry row)
        {
            var plot = Catalogue.PlotOf(row.PlotId);

            if (plot != null && plot.IsApartment)
                return plot;

            var first = Catalogue.ApartmentPlots.FirstOrDefault();

            if (first != null && Store != null)
            {
                Logger.WriteLog(LogType.Error, $"Apartments: apartment {row.Id} stood on plot {row.PlotId}, which is not an apartment plot now; moved to plot {first.Id}.");
                Store.With(repository => repository.Update(row.Id, entry => entry.PlotId = first.Id));
                row.PlotId = first.Id;
            }

            return first;
        }

        /// <summary>
        /// The apartment's copy, opened if it is not: its items put down from the database.
        /// Null, with the player told why if one is given, when it cannot be: no plot, the map
        /// not loaded, or as many copies open as may be.
        /// </summary>
        internal Live Open(ApartmentEntry row, Client forWhom = null)
        {
            if (row == null)
                return null;

            if (_live.TryGetValue(row.Id, out var open) && !open.Moving)
                return open;

            if (open != null)
            {
                forWhom?.Let(c => Say(c, "That apartment is moving to another plot. Try again in a moment."));
                return null;
            }

            var plot = PlotFor(row);
            var maps = Maps();

            if (plot == null)
            {
                forWhom?.Let(c => Tell(c, PlayerMessage.PmFailedToLoadApartment));
                return null;
            }

            if (maps.ApartmentCopies().Count >= Math.Max(1, Config.MaxOpenCopies))
            {
                forWhom?.Let(c =>
                {
                    Tell(c, PlayerMessage.PmFailedToCreateApartment);
                    Say(c, "Too many apartments are open right now. Try again in a few minutes.");
                });
                Logger.WriteLog(LogType.Debug, $"Apartments: {Config.MaxOpenCopies} copies are open; apartment {row.Id} was not opened.");
                return null;
            }

            var map = maps.OpenApartmentCopy(plot.MapContextId, row.Id);

            if (map == null)
            {
                forWhom?.Let(c => Tell(c, PlayerMessage.PmFailedToLoadApartment));
                Logger.WriteLog(LogType.Error, $"Apartments: map {plot.MapContextId} of plot {plot.Id} is not loaded; apartment {row.Id} was not opened.");
                return null;
            }

            var owner = Store?.With(repository => repository.GetCharacter(row.OwnerId));
            var live = new Live
            {
                Row = row,
                Plot = plot,
                Map = map,
                Visitors = Store?.With(repository => repository.GetVisitors(row.Id)) ?? new List<ApartmentVisitorEntry>(),
                OwnerAccountId = owner?.AccountId ?? 0,
                OwnerName = owner?.Name ?? $"#{row.OwnerId}"
            };

            _live[row.Id] = live;
            _emptySince.Remove(row.Id);
            PutDownItems(live, Store?.With(repository => repository.GetItems(row.Id)) ?? new List<ApartmentItemEntry>());

            Logger.WriteLog(LogType.Debug, $"Apartments: apartment {row.Id} opened on plot {plot.Id} (map {plot.MapContextId}, instance {map.InstanceId}), {live.Items.Count} items, {live.Packed.Count} packed.");

            return live;
        }

        /// <summary>Closes an apartment's copy that has nobody in it or on the way. False when it has.</summary>
        internal bool Close(Live live)
        {
            if (live == null || !Maps().CloseApartmentCopy(live.Map))
                return false;

            ForgetItems(live);
            _live.Remove(live.Row.Id);
            _emptySince.Remove(live.Row.Id);

            return true;
        }

        #endregion

        #region Going in and out

        /// <summary>Why a player may not travel to or from an apartment now, or null.</summary>
        private string WhyNotTravel(Client client)
        {
            var player = client?.Player;

            if (player == null || client.State != ClientState.Ingame || client.PendingTransfer != null || player.MapChannel == null)
                return "You can't travel right now.";

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return "You can't travel while dead.";

            if (player.InCombat)
                return "You can't travel during combat.";

            if (player.LogoutPending)
                return "You are logging out.";

            if (player.MapChannel.IsPrivateInstance)
                return "You can't go to an apartment from here.";

            return null;
        }

        /// <summary>.apartment and SendMeToMyApartment: into the player's own apartment, made the first time.</summary>
        public void GoHome(Client client)
        {
            if (client?.Player == null)
                return;

            if (!Config.Enabled || Store == null)
            {
                Say(client, "Apartments are not open on this server.");
                return;
            }

            var why = WhyNotTravel(client);

            if (why != null)
            {
                Say(client, why);
                return;
            }

            var row = OwnApartment(client, create: true);

            if (row == null)
            {
                Tell(client, PlayerMessage.PmFailedToCreateApartment);
                return;
            }

            GoInto(client, row);
        }

        /// <summary>The player's own apartment's row; made on the first apartment plot when they have none and <paramref name="create"/>.</summary>
        internal ApartmentEntry OwnApartment(Client client, bool create)
        {
            var id = client.Player.Id;
            var row = Store?.With(repository => repository.FindByOwner(ApartmentEntry.OwnerKindCharacter, id));

            if (row != null || !create || Store == null)
                return row;

            var plot = Catalogue.ApartmentPlots.FirstOrDefault();

            if (plot == null)
            {
                Logger.WriteLog(LogType.Error, "Apartments: there is no apartment plot on a loaded map; none can be made.");
                return null;
            }

            row = Store.With(repository => repository.Create(ApartmentEntry.OwnerKindCharacter, id, plot.Id, UtcNowMs()));
            Logger.WriteLog(LogType.Debug, $"Apartments: {client.Player.Name} ({id}) has an apartment now, {row.Id}, on plot {plot.Id}.");

            return row;
        }

        /// <summary>
        /// Into an apartment the player may enter, at its plot's entry: its copy opened if need be,
        /// where they stand kept as their way back unless they come from another apartment.
        /// </summary>
        internal bool GoInto(Client client, ApartmentEntry row, string viaCode = null)
        {
            var player = client.Player;
            var live = Open(row, client);

            if (live == null)
                return false;

            if (viaCode != null)
            {
                if (!_passes.TryGetValue(row.Id, out var passes))
                    _passes[row.Id] = passes = new HashSet<uint>();

                passes.Add(player.Id);
            }

            if (AccessOf(live, client) == Access.None)
            {
                Say(client, "You haven't been invited into that apartment.");
                return false;
            }

            if (ReferenceEquals(player.MapChannel, live.Map))
            {
                Say(client, $"You are in {live.Title}. .leave takes you back.");
                return false;
            }

            var here = player.MapChannel;
            var previous = Store.With(repository => repository.GetReturn(player.Id));

            // From outside, here is the way back; from another apartment, the way back is the one
            // they came in by.
            var back = here.IsApartment && previous != null
                ? previous
                : new ApartmentReturnEntry
                {
                    CharacterId = player.Id,
                    MapContextId = here.MapInfo.MapContextId,
                    PosX = player.Position.X,
                    PosY = player.Position.Y,
                    PosZ = player.Position.Z,
                    Rotation = player.Rotation
                };

            back.ApartmentId = row.Id;
            Store.With(repository => { repository.SetReturn(back); return 0; });

            if (!Maps().Send(client, live.Map, live.Plot.Entry, live.Plot.EntryYaw))
            {
                // Put back as it was.
                if (here.IsApartment && previous != null)
                    Store.With(repository => { repository.SetReturn(previous); return 0; });
                else
                    Store.With(repository => { repository.RemoveReturn(player.Id); return 0; });

                Say(client, "You can't travel right now.");
                return false;
            }

            _inside[player.Id] = row.Id;
            live.Greeted.Remove(player.Id);
            _emptySince.Remove(row.Id);

            return true;
        }

        /// <summary>.leave: out of the apartment the player is in, back to where they came from.</summary>
        public void Leave(Client client)
        {
            var map = client?.Player?.MapChannel;

            if (map == null || !map.IsApartment)
            {
                Say(client, "You are not in an apartment.");
                return;
            }

            var why = WhyNotTravel(client);

            // Out of a fight in their own apartment: there is nobody to fight there, so only the
            // dead and those between maps are held.
            if (why != null && client.Player.InCombat && client.Player.State != CharacterState.Dead)
                why = null;

            if (why != null)
            {
                Say(client, why);
                return;
            }

            SendOut(client, null);
        }

        /// <summary>
        /// Sends a player in an apartment back to where they came from, saying why if a reason is
        /// given. Where they came from is gone, or on a map that is not loaded: Alia Das.
        /// </summary>
        internal bool SendOut(Client client, string why)
        {
            var player = client.Player;
            var back = Store?.With(repository => repository.GetReturn(player.Id));
            var maps = Maps();
            var to = back != null && maps.MapChannelArray.ContainsKey(back.MapContextId)
                ? (back.MapContextId, new Vector3((float)back.PosX, (float)back.PosY, (float)back.PosZ), (float)back.Rotation)
                : (Bootcamp.BootcampArrivalMapContextId,
                    new Vector3((float)Bootcamp.BootcampArrivalCoordX, (float)Bootcamp.BootcampArrivalCoordY, (float)Bootcamp.BootcampArrivalCoordZ),
                    (float)Bootcamp.BootcampArrivalRotation);

            ForgetPass(player.MapChannel?.ApartmentId ?? 0, player.Id);

            if (!maps.ChangeMap(client, to.Item1, to.Item2, to.Item3))
            {
                Logger.WriteLog(LogType.Error, $"Apartments: {player.Name} could not be sent back to map {to.Item1}.");
                return false;
            }

            if (why != null)
                Say(client, why);

            return true;
        }

        private void ForgetPass(uint apartmentId, uint characterId)
        {
            if (_passes.TryGetValue(apartmentId, out var passes))
                passes.Remove(characterId);
        }

        /// <summary>
        /// A character entering the world on the map of an apartment they were in when they left
        /// it: back into its copy, opened again if need be, if they may still go in and it still
        /// stands on that map; otherwise at the place they came from, told why. Called between
        /// the character being loaded and the client being told where to (CharacterManager),
        /// before the map's own copies are seen to (MapChannelManager.PlaceLogin).
        /// </summary>
        public void PlaceLogin(Client client)
        {
            var player = client?.Player;

            if (player == null || Store == null || player.Id == 0)
                return;

            ApartmentReturnEntry back;

            try
            {
                back = Store.With(repository => repository.GetReturn(player.Id));
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Apartments: the return point of {player.Name} could not be read: {e.Message}");
                return;
            }

            if (back == null)
                return;

            // A return point is kept only while its character is in an apartment: it is let go
            // as soon as they are seen in the world anywhere else. So they left the world inside
            // this one.
            var row = Store.With(repository => repository.Get(back.ApartmentId));

            if (row == null)
            {
                PlaceAt(client, back, "The apartment you were in is gone.");
                return;
            }

            var plot = PlotFor(row);
            string why = null;

            if (!Config.Enabled)
                why = "Apartments are closed on this server.";
            else if (plot == null)
                why = "The apartment you were in could not be opened.";
            else if (AccessOf(row, Store.With(repository => repository.GetVisitors(row.Id)), Store.With(repository => repository.GetCharacter(row.OwnerId))?.AccountId ?? 0, client) == Access.None)
                why = "You may no longer go into the apartment you were in.";

            var live = why == null ? Open(row) : null;

            if (live == null)
            {
                PlaceAt(client, back, why ?? "The apartment you were in could not be opened.");
                return;
            }

            // Its plot moved meanwhile - to another map, or elsewhere on this one: at its entry.
            if (player.MapContextId != plot.MapContextId || !plot.Contains(player.Position, 4f))
            {
                player.MapContextId = plot.MapContextId;
                player.PlaceAt(plot.Entry);
                player.Rotation = plot.EntryYaw;
            }

            player.MapChannel = live.Map;
            _inside[player.Id] = row.Id;
            live.Greeted.Remove(player.Id);
            _emptySince.Remove(row.Id);
        }

        /// <summary>A login put at its return point instead of where its row has it, with why for when it has arrived.</summary>
        private void PlaceAt(Client client, ApartmentReturnEntry back, string why)
        {
            var player = client.Player;
            var maps = Maps();

            Store.With(repository => { repository.RemoveReturn(player.Id); return 0; });

            if (!maps.MapChannelArray.TryGetValue(back.MapContextId, out var map))
            {
                map = maps.FindByContextId(Bootcamp.BootcampArrivalMapContextId);

                if (map == null)
                    return;

                back = new ApartmentReturnEntry
                {
                    MapContextId = Bootcamp.BootcampArrivalMapContextId,
                    PosX = Bootcamp.BootcampArrivalCoordX,
                    PosY = Bootcamp.BootcampArrivalCoordY,
                    PosZ = Bootcamp.BootcampArrivalCoordZ,
                    Rotation = Bootcamp.BootcampArrivalRotation
                };
            }

            player.MapChannel = map;
            player.MapContextId = back.MapContextId;
            player.PlaceAt(new Vector3((float)back.PosX, (float)back.PosY, (float)back.PosZ));
            player.Rotation = back.Rotation;
            client.ArrivalNotice = why + " You are back where you were before.";
        }

        /// <summary>
        /// Where a player who dies in an apartment gets up: beside its first medbay item, or at
        /// the entry. Null on any other channel (InstanceEntrances).
        /// </summary>
        public static (Vector3 Position, float Rotation)? EntranceOf(MapChannel map)
        {
            var live = _instance?.LiveOf(map);

            if (live == null)
                return null;

            var medbay = live.Items.Values.Where(item => item.Decor.Function == DecorFunction.Medbay)
                .OrderBy(item => item.Row.Number)
                .FirstOrDefault();

            if (medbay != null)
            {
                var row = medbay.Row;
                var forward = new Vector3((float)-Math.Sin(row.Rotation), 0, (float)-Math.Cos(row.Rotation));
                var reach = Math.Max(medbay.Decor.HalfExtents.X, medbay.Decor.HalfExtents.Z) * (float)row.Scale + 1f;
                var beside = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ) + forward * reach;

                if (live.Plot.Contains(beside))
                    return (beside, (float)row.Rotation);
            }

            return (live.Plot.Entry, live.Plot.EntryYaw);
        }

        #endregion

        #region The worker

        /// <summary>
        /// Once a second, from the map channel worker: who has arrived in a copy is told where they
        /// are; anyone inside who may no longer be is sent out; a copy that has stood empty long
        /// enough is closed; the return point of someone who has left is let go; and someone whose
        /// apartment moved map is taken into it once they have arrived outside.
        /// </summary>
        public void Worker()
        {
            var maps = Maps();
            var now = Clock();

            foreach (var live in _live.Values.ToList())
            {
                foreach (var client in live.Map.ClientList.ToList())
                {
                    var player = client?.Player;

                    if (player == null || client.State != ClientState.Ingame || client.PendingTransfer != null || !ReferenceEquals(player.MapChannel, live.Map))
                        continue;

                    if (live.Moving)
                    {
                        SendOut(client, $"{live.Title} has moved to another plot.");
                        continue;
                    }

                    if (!Config.Enabled)
                    {
                        SendOut(client, "Apartments have been closed on this server.");
                        continue;
                    }

                    if (AccessOf(live, client) == Access.None)
                    {
                        SendOut(client, $"You may no longer be in {live.Title}.");
                        continue;
                    }

                    if (live.Greeted.Add(player.Id))
                        Greet(client, live);
                }

                if (maps.PopulationOf(live.Map) > 0)
                {
                    _emptySince.Remove(live.Row.Id);
                    continue;
                }

                if (!_emptySince.TryGetValue(live.Row.Id, out var since))
                {
                    _emptySince[live.Row.Id] = since = now;

                    if (!live.Moving)
                        continue;
                }

                if (live.Moving || now - since >= Math.Max(0, Config.IdleCloseSeconds) * 1000L)
                    Close(live);
            }

            // The return points of those who have left: once they are in the world somewhere else.
            foreach (var (characterId, apartmentId) in _inside.ToList())
            {
                var client = OnlineById(characterId);

                if (client == null)
                {
                    // Logged out inside: the point stays for their next login.
                    _inside.Remove(characterId);
                    continue;
                }

                if (client.State != ClientState.Ingame || client.PendingTransfer != null)
                    continue;

                var map = client.Player.MapChannel;

                if (map != null && map.IsApartment)
                {
                    _inside[characterId] = map.ApartmentId;
                    continue;
                }

                _inside.Remove(characterId);
                ForgetPass(apartmentId, characterId);
                Store?.With(repository => { repository.RemoveReturn(characterId); return 0; });

                if (_followMove.Remove(characterId))
                    GoHome(client);
            }
        }

        private Client OnlineById(uint characterId) =>
            Online().FirstOrDefault(client => client.Player?.Id == characterId)
            ?? _live.Values.SelectMany(live => live.Map.ClientList).FirstOrDefault(client => client?.Player?.Id == characterId);

        private void Greet(Client client, Live live)
        {
            var access = AccessOf(live, client);
            var budget = ApartmentCatalogue.BudgetOf(live.Plot, Config.ItemBudget);
            var text = access == Access.Owner
                ? $"Welcome home: {live.Title}, on {live.Plot.Name}. {live.Items.Count}/{budget} items{(live.Packed.Count > 0 ? $", {live.Packed.Count} packed" : "")}. .decor help to furnish it, .apartment help for the rest, .leave to go back."
                : $"You are in {live.Title}, on {live.Plot.Name}.{(access == Access.Build ? " You may build here: .decor help." : "")} .leave takes you back.";

            Say(client, text);
        }

        #endregion

        #region Finding characters

        /// <summary>
        /// A character by what a player typed: #id, a player in the world by first or family
        /// name, or any character by first name. (id, account, name), or null.
        /// </summary>
        internal (uint Id, uint AccountId, string Name)? FindCharacter(string text)
        {
            text = text?.Trim();

            if (string.IsNullOrEmpty(text))
                return null;

            if (text.StartsWith("#") && uint.TryParse(text.Substring(1), out var id))
                return Store?.With(repository => repository.GetCharacter(id));

            var online = Online().FirstOrDefault(client => string.Equals(client.Player.Name, text, StringComparison.OrdinalIgnoreCase))
                         ?? Online().FirstOrDefault(client => string.Equals(client.Player.FamilyName, text, StringComparison.OrdinalIgnoreCase));

            if (online != null)
                return (online.Player.Id, online.AccountEntry?.Id ?? 0, online.Player.Name);

            return Store?.With(repository => repository.FindCharacter(text));
        }

        /// <summary>A character's name, looked up once.</summary>
        internal string NameOf(uint characterId)
        {
            if (_names.TryGetValue(characterId, out var name))
                return name;

            name = Store?.With(repository => repository.GetCharacter(characterId))?.Name ?? $"#{characterId}";
            _names[characterId] = name;

            return name;
        }

        #endregion

        /// <summary>A character deleted: their apartment's copy, if it stands, is emptied and closed; the rows went with the character.</summary>
        public void CharacterDeleted(uint characterId, uint apartmentId)
        {
            _inside.Remove(characterId);
            _followMove.Remove(characterId);

            foreach (var passes in _passes.Values)
                passes.Remove(characterId);

            if (apartmentId == 0 || !_live.TryGetValue(apartmentId, out var live))
                return;

            live.Moving = true;
        }

        /// <summary>Forgets everything open; for tests.</summary>
        internal void Reset()
        {
            foreach (var live in _live.Values.ToList())
            {
                live.Map.ClientList.Clear();
                live.Map.QueuedClients.Clear();
                live.Map.Arriving.Clear();
                Close(live);
            }

            _live.Clear();
            _passes.Clear();
            _codeTries.Clear();
            _inside.Clear();
            _followMove.Clear();
            _emptySince.Clear();
            _names.Clear();
            _selected.Clear();
            _undo.Clear();
            _buckets.Clear();
        }
    }

    internal static class ApartmentExtensions
    {
        /// <summary>Runs an action on a value that is there.</summary>
        internal static void Let<T>(this T value, Action<T> action) where T : class
        {
            if (value != null)
                action(value);
        }
    }
}
