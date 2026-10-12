using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Rasa.Api
{
    using Data;
    using Game;

    /// <summary>
    /// An account that is online: logged in to this game server, at the character screen or
    /// with a character. The character's part is null while it has none - at the character
    /// screen - and is the character being loaded, in the world or changing maps otherwise.
    /// </summary>
    public sealed class OnlineUser
    {
        public OnlineUser(uint accountId, ClientState state, uint? characterId = null, string name = null,
            string familyName = null, uint? classId = null, byte? level = null)
        {
            AccountId = accountId;
            State = state;
            CharacterId = characterId;
            Name = characterId.HasValue ? name ?? "" : null;
            FamilyName = characterId.HasValue ? familyName ?? "" : null;
            ClassId = characterId.HasValue ? classId : null;
            Level = characterId.HasValue ? level : null;
        }

        public uint AccountId { get; }
        public ClientState State { get; }
        public uint? CharacterId { get; }

        /// <summary>The character's own name, and the family name every character of the account has.</summary>
        public string Name { get; }
        public string FamilyName { get; }

        /// <summary>The class, a CharacterClass.</summary>
        public uint? ClassId { get; }

        public byte? Level { get; }

        /// <summary>The class by its name in CharacterClass ("Soldier"); null for a class that has none there.</summary>
        public string ClassName =>
            ClassId is { } id && Enum.IsDefined(typeof(CharacterClass), id) ? ((CharacterClass)id).ToString() : null;
    }

    /// <summary>
    /// The accounts online, as the world loop last saw them, for GET /usersonline.
    ///
    /// Like <see cref="ServerStatus"/>, it is written by the world loop and only read by the
    /// listeners' threads: once a second the loop reads the clients (<see cref="Read"/>) and
    /// puts the list here whole (<see cref="Sample"/>), and a request is answered from the list
    /// there is, at most a second old. A listener's thread never touches a client or the
    /// world, and takes no lock the loop holds.
    ///
    /// Online is what Server.CurrentPlayers counts of the world's own connections: every one
    /// past login (Client.IsAuthenticated). The queue's players on their way to the world port
    /// are counted there too, and are not here: they have not logged in to this server yet, so
    /// there is no account to name.
    /// </summary>
    public sealed class OnlineUsers
    {
        /// <summary>How often the clients are read, in milliseconds.</summary>
        public const int SampleMs = 1000;

        /// <summary>The clock, in milliseconds; replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        private IReadOnlyList<OnlineUser> _users = Array.Empty<OnlineUser>();
        private long _lastSample = -1;

        /// <summary>The list as it was last read: by account, then character.</summary>
        public IReadOnlyList<OnlineUser> Current => Volatile.Read(ref _users);

        /// <summary>
        /// From the world loop, every tick; once a second <paramref name="read"/> is asked who is
        /// online, and that is the list from then on.
        /// </summary>
        public void Sample(Func<IEnumerable<OnlineUser>> read)
        {
            var now = Now();
            var last = Volatile.Read(ref _lastSample);

            if (last >= 0 && now - last < SampleMs)
                return;

            Volatile.Write(ref _lastSample, now);

            var users = (read?.Invoke() ?? Enumerable.Empty<OnlineUser>())
                .Where(user => user != null)
                .OrderBy(user => user.AccountId).ThenBy(user => user.CharacterId ?? 0)
                .ToList();

            Volatile.Write(ref _users, users);
        }

        /// <summary>
        /// Who is online among these clients - the server's list, locked while it is copied - on
        /// the world loop, which the clients' characters belong to.
        /// </summary>
        public static List<OnlineUser> Read(List<Client> clients)
        {
            Client[] copy;

            lock (clients)
                copy = clients.ToArray();

            var users = new List<OnlineUser>();

            foreach (var client in copy)
            {
                var account = client?.AccountEntry;

                if (account == null || !client.IsAuthenticated())
                    continue;

                var player = client.Player;
                var playing = client.State == ClientState.Loading || client.State == ClientState.Ingame
                              || client.State == ClientState.Teleporting;

                users.Add(playing && player != null && player.Id != 0
                    ? new OnlineUser(account.Id, client.State, player.Id, player.Name, player.FamilyName, player.Class, player.Level)
                    : new OnlineUser(account.Id, client.State));
            }

            return users;
        }

        /// <summary>
        /// {"count":2,"users":[{"accountId":1,"characterId":12,"name":"Ellie","familyName":"Hart",
        /// "classId":2,"className":"Soldier","level":15,"state":"ingame"},{"accountId":3,
        /// "characterId":null,...,"state":"characterselection"}]}
        /// </summary>
        public string Json()
        {
            var users = Current;

            return ServerStatus.Json(writer =>
            {
                writer.WriteNumber("count", users.Count);
                writer.WriteStartArray("users");

                foreach (var user in users)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("accountId", user.AccountId);
                    Number(writer, "characterId", user.CharacterId);
                    writer.WriteString("name", user.Name);
                    writer.WriteString("familyName", user.FamilyName);
                    Number(writer, "classId", user.ClassId);
                    writer.WriteString("className", user.ClassName);
                    Number(writer, "level", user.Level);
                    writer.WriteString("state", user.State.ToString().ToLowerInvariant());
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            });
        }

        private static void Number(Utf8JsonWriter writer, string name, uint? value)
        {
            if (value.HasValue)
                writer.WriteNumber(name, value.Value);
            else
                writer.WriteNull(name);
        }

        private static void Number(Utf8JsonWriter writer, string name, byte? value) =>
            Number(writer, name, value.HasValue ? value.Value : (uint?)null);
    }

    /// <summary>
    /// GET /usersonline: every account online now, with the character it is playing - see
    /// <see cref="OnlineUsers"/>. It names accounts and characters, so it is
    /// <see cref="ApiEndpoint.Sensitive"/>: off until its own entry in ApiConfig.Rest.Endpoints
    /// turns it on, and public only by a Public of its own.
    /// </summary>
    public sealed class UsersOnlineEndpoint : ApiEndpoint
    {
        private readonly OnlineUsers _users;

        public UsersOnlineEndpoint(OnlineUsers users) => _users = users;

        public override string Name => "usersonline";

        public override bool Sensitive => true;

        public override ApiResponse Handle(ApiRequest request) => ApiResponse.Ok(_users.Json());
    }
}
