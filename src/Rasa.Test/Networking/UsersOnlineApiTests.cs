extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Api;
    using Rasa.Config;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Structures.Char;

    /// <summary>
    /// GET /usersonline (Rasa.Api.OnlineUsers): who is online as the world loop last read them,
    /// in what fields, read from which clients and how often, and who may ask.
    /// </summary>
    [TestClass]
    public class UsersOnlineApiTests
    {
        private const string Key = "players-key";

        [TestMethod]
        public void ItIsOffUntilItsOwnEntryTurnsItOnAndIsNeverPublicByTheApiBeingSo()
        {
            var config = new RestApiConfig { Public = true, ApiKey = Key };
            var api = Api(config, new OnlineUsers());

            Assert.AreEqual(404, api.Respond(Get(Key)).Status, "no entry: off");

            config.Endpoints["usersonline"] = new ApiEndpointConfig { Enabled = false };
            Assert.AreEqual(404, api.Respond(Get(Key)).Status, "the shipped entry: off");

            config.Endpoints["usersonline"] = new ApiEndpointConfig();
            Assert.AreEqual(401, api.Respond(Get(null)).Status, "the API being public is not this one being public");
            Assert.AreEqual(401, api.Respond(Get("not-the-key")).Status);
            Assert.AreEqual(200, api.Respond(Get(Key)).Status, "the global key opens it");

            config.Endpoints["usersonline"].ApiKey = "its-own";
            Assert.AreEqual(200, api.Respond(Get("its-own")).Status);

            config.Endpoints["usersonline"].Public = true;
            Assert.AreEqual(200, api.Respond(Get(null)).Status, "only a Public of its own makes it public");

            // A GET, and not a POST.
            var posted = api.Respond(new ApiRequest { Method = "POST", Path = "/usersonline", Remote = IPAddress.Loopback });

            Assert.AreEqual(405, posted.Status);
            Assert.AreEqual("GET, HEAD", posted.Allow);
        }

        [TestMethod]
        public void ItAnswersTheListTheWorldLoopLastReadWithACharactersFieldsOrNulls()
        {
            var users = new OnlineUsers { Now = () => 0 };
            var api = Api(Keyed(), users);

            // Before the world has been read: nobody.
            var empty = api.Respond(Get(Key));

            Assert.AreEqual(200, empty.Status);
            Assert.AreEqual("{\"count\":0,\"users\":[]}", empty.Json);

            users.Sample(() => new[]
            {
                new OnlineUser(3, ClientState.CharacterSelection),
                new OnlineUser(1, ClientState.Ingame, 12, "Ellie", "Hart", 2, 15),
                new OnlineUser(2, ClientState.Loading, 7, "Rook", "Vance", 10, 42)
            });

            var answer = api.Respond(Get(Key));

            Assert.AreEqual(200, answer.Status);
            Assert.AreEqual(
                "{\"count\":3,\"users\":[" +
                "{\"accountId\":1,\"characterId\":12,\"name\":\"Ellie\",\"familyName\":\"Hart\",\"classId\":2,\"className\":\"Soldier\",\"level\":15,\"state\":\"ingame\"}," +
                "{\"accountId\":2,\"characterId\":7,\"name\":\"Rook\",\"familyName\":\"Vance\",\"classId\":10,\"className\":\"Sniper\",\"level\":42,\"state\":\"loading\"}," +
                "{\"accountId\":3,\"characterId\":null,\"name\":null,\"familyName\":null,\"classId\":null,\"className\":null,\"level\":null,\"state\":\"characterselection\"}]}",
                answer.Json);

            // A class the server has no name for has its number and no name; a name that wants
            // escaping in JSON is escaped.
            users.Now = () => OnlineUsers.SampleMs;
            users.Sample(() => new[] { new OnlineUser(4, ClientState.Teleporting, 9, "Quote\"d", null, 99, 1) });

            using var document = JsonDocument.Parse(api.Respond(Get(Key)).Json);
            var user = document.RootElement.GetProperty("users")[0];

            Assert.AreEqual(1, document.RootElement.GetProperty("count").GetInt32());
            Assert.AreEqual("Quote\"d", user.GetProperty("name").GetString());
            Assert.AreEqual("", user.GetProperty("familyName").GetString(), "a character's family name is never null");
            Assert.AreEqual(99u, user.GetProperty("classId").GetUInt32());
            Assert.AreEqual(JsonValueKind.Null, user.GetProperty("className").ValueKind);
            Assert.AreEqual("teleporting", user.GetProperty("state").GetString());
        }

        [TestMethod]
        public void TheWorldIsReadOnceASecondAndTheListIsReplacedWhole()
        {
            var clock = 0L;
            var users = new OnlineUsers { Now = () => clock };
            var reads = 0;

            IEnumerable<OnlineUser> Read(params uint[] accounts)
            {
                reads++;
                return accounts.Select(account => new OnlineUser(account, ClientState.CharacterSelection));
            }

            users.Sample(() => Read(5, 6));
            Assert.AreEqual(1, reads);
            CollectionAssert.AreEqual(new uint[] { 5, 6 }, users.Current.Select(user => user.AccountId).ToList());

            var before = users.Current;

            // Every tick asks; only a second on is the world read again.
            clock = OnlineUsers.SampleMs - 1;
            users.Sample(() => Read(7));
            Assert.AreEqual(1, reads);
            Assert.AreSame(before, users.Current);

            clock = OnlineUsers.SampleMs;
            users.Sample(() => Read(7));
            Assert.AreEqual(2, reads);
            CollectionAssert.AreEqual(new uint[] { 7 }, users.Current.Select(user => user.AccountId).ToList());
            CollectionAssert.AreEqual(new uint[] { 5, 6 }, before.Select(user => user.AccountId).ToList(), "a list handed out is not changed under its reader");

            // Nobody, and a read that gives nothing at all.
            clock += OnlineUsers.SampleMs;
            users.Sample(() => null);
            Assert.AreEqual(0, users.Current.Count);
        }

        [TestMethod]
        public void OnlineIsEveryClientPastLoginWithTheCharacterItIsPlaying()
        {
            var clients = new List<Client>
            {
                Client(ClientState.Ingame, 10, 100, "Ellie", "Hart", 2, 15),
                Client(ClientState.CharacterSelection, 11, 101, "Left", "Behind", 4, 30),
                Client(ClientState.LoggedIn, 12),
                Client(ClientState.Loading, 13, 103, "Rook", "Vance", 10, 42),
                Client(ClientState.Teleporting, 14, 104, "Nomad", "Vance", 1, 3),
                Client(ClientState.Connected, 15),
                Client(ClientState.Disconnected, 16, 106, "Gone", "Away", 2, 9),
                Client(ClientState.Ingame, 17, 0),
                new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame },
                null
            };

            var users = OnlineUsers.Read(clients).ToDictionary(user => user.AccountId);

            CollectionAssert.AreEquivalent(new uint[] { 10, 11, 12, 13, 14, 17 }, users.Keys.ToList(),
                "past login: not one still connecting or one gone, and none without an account");

            var playing = users[10];

            Assert.AreEqual(100u, playing.CharacterId);
            Assert.AreEqual("Ellie", playing.Name);
            Assert.AreEqual("Hart", playing.FamilyName);
            Assert.AreEqual(2u, playing.ClassId);
            Assert.AreEqual("Soldier", playing.ClassName);
            Assert.AreEqual((byte)15, playing.Level);
            Assert.AreEqual(ClientState.Ingame, playing.State);

            Assert.AreEqual(103u, users[13].CharacterId, "loading its character");
            Assert.AreEqual(104u, users[14].CharacterId, "changing maps");

            // At the character screen there is no character, whatever was played before.
            foreach (var account in new uint[] { 11, 12, 17 })
            {
                Assert.IsNull(users[account].CharacterId, account.ToString());
                Assert.IsNull(users[account].Name);
                Assert.IsNull(users[account].FamilyName);
                Assert.IsNull(users[account].ClassId);
                Assert.IsNull(users[account].ClassName);
                Assert.IsNull(users[account].Level);
            }

            Assert.AreEqual(ClientState.CharacterSelection, users[11].State);
            Assert.AreEqual(ClientState.LoggedIn, users[12].State);
        }

        [TestMethod]
        public void TheServersApiHasItAndItIsOffAsShipped()
        {
            var host = new ApiHost(new ServerStatus());

            CollectionAssert.Contains(host.Rest.Endpoints.ToList(), "usersonline");
            Assert.IsNotNull(host.Users);

            host.Rest.Apply(new RestApiConfig { Public = true, ApiKey = Key });
            Assert.AreEqual(404, host.Rest.Respond(Get(Key)).Status);
        }

        private static Client Client(ClientState state, uint accountId, uint characterId = 0, string name = null,
            string familyName = null, uint classId = 0, byte level = 0)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = state };

            typeof(Client).GetProperty(nameof(Rasa.Game.Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = accountId });
            client.Player.Id = characterId;
            client.Player.Name = name;
            client.Player.FamilyName = familyName;
            client.Player.Class = classId;
            client.Player.Level = level;

            return client;
        }

        private static RestApiConfig Keyed()
        {
            var config = new RestApiConfig { ApiKey = Key };

            config.Endpoints["usersonline"] = new ApiEndpointConfig();

            return config;
        }

        private static ApiServer Api(RestApiConfig config, OnlineUsers users)
        {
            var api = new ApiServer();

            api.Register(new UsersOnlineEndpoint(users));
            api.Apply(config);

            return api;
        }

        private static ApiRequest Get(string key)
        {
            var request = new ApiRequest { Path = "/usersonline", Remote = IPAddress.Loopback };

            if (key != null)
                request.Headers["X-API-Key"] = key;

            return request;
        }
    }
}
