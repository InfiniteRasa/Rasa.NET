extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Api;
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Networking;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    /// <summary>
    /// POST /kickuser (Api.KickUserEndpoint): what it takes, whom it kicks and what it answers;
    /// and the REST API's log (Api.ApiAudit): what a row holds, the secrets it does not, and
    /// that every request a listener answers is on it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KickUserApiTests
    {
        private const string Key = "kick-key";

        private readonly List<Client> _registered = new();

        [TestCleanup]
        public void Unregister()
        {
            lock (Server.Clients)
                foreach (var client in _registered)
                    Server.Clients.Remove(client);
        }

        #region The order

        [TestMethod]
        public void TheOrderIsAnAccountByIdOrFamilyNameAReasonAndAnAdmin()
        {
            var byId = KickUserEndpoint.Read(Post("{\"accountId\":12,\"reason\":\" griefing \",\"admin\":\" Atomsk \"}"), out var problem);

            Assert.IsNull(problem);
            Assert.AreEqual(12u, byId.AccountId);
            Assert.IsNull(byId.FamilyName);
            Assert.AreEqual("griefing", byId.Reason);
            Assert.AreEqual("Atomsk", byId.Admin);

            var byName = KickUserEndpoint.Read(Post("{\"familyName\":\"Hart\",\"admin\":\"Atomsk\"}"), out problem);

            Assert.IsNull(problem);
            Assert.IsNull(byName.AccountId);
            Assert.AreEqual("Hart", byName.FamilyName);
            Assert.IsNull(byName.Reason, "a reason may be left out");

            Assert.AreEqual(7u, KickUserEndpoint.Read(Post("{\"accountId\":\"7\",\"admin\":\"a\",\"reason\":\"\",\"familyName\":null}"), out problem).AccountId,
                "a number in a string; an empty reason and a null name are none");
            Assert.IsNull(problem);
        }

        [TestMethod]
        public void AnOrderThatWillNotDoIsAnsweredWithWhyAndNoKick()
        {
            var cases = new Dictionary<string, string>
            {
                ["{\"admin\":\"a\"}"] = "accountId or familyName is required",
                ["{\"accountId\":1,\"familyName\":\"Hart\",\"admin\":\"a\"}"] = "give accountId or familyName, not both",
                ["{\"accountId\":0,\"admin\":\"a\"}"] = "accountId must be a whole number above 0",
                ["{\"accountId\":-3,\"admin\":\"a\"}"] = "accountId must be a whole number above 0",
                ["{\"accountId\":1.5,\"admin\":\"a\"}"] = "accountId must be a whole number above 0",
                ["{\"accountId\":\"twelve\",\"admin\":\"a\"}"] = "accountId must be a whole number above 0",
                ["{\"familyName\":\" \",\"admin\":\"a\"}"] = "familyName must be a name",
                ["{\"familyName\":5,\"admin\":\"a\"}"] = "familyName must be a name",
                ["{\"familyName\":\"Hart\"}"] = "admin is required",
                ["{\"familyName\":\"Hart\",\"admin\":\"  \"}"] = "admin is required",
                ["{\"familyName\":\"Hart\",\"admin\":\"" + new string('a', 65) + "\"}"] = "admin is longer than 64 characters",
                ["{\"familyName\":\"Hart\",\"admin\":\"a\",\"reason\":3}"] = "reason must be text",
                ["{\"familyName\":\"Hart\",\"admin\":\"a\",\"reason\":\"" + new string('r', 257) + "\"}"] = "reason is longer than 256 characters",
                ["[1,2]"] = "the body must be a JSON object",
                ["{\"familyName\":"] = "the body is not JSON",
                [""] = "the body is not JSON"
            };

            var kicks = 0;
            var endpoint = new KickUserEndpoint { Kick = order => { kicks++; return KickOutcome.Done(1, "x"); } };

            foreach (var (body, error) in cases)
            {
                var answer = endpoint.Handle(Post(body));

                Assert.AreEqual(200, answer.Status, body);
                Assert.AreEqual(Json(false, error), answer.Json, body);
            }

            var form = Post("familyName=Hart&admin=a");
            form.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            Assert.AreEqual(Json(false, "content type must be application/json"), endpoint.Handle(form).Json);
            Assert.AreEqual(0, kicks);
        }

        #endregion

        #region The kick

        [TestMethod]
        public void AnAccountOnlineIsKickedByIdOrFamilyNameEveryConnectionOfItAndTold()
        {
            using var world = new WorldTestContext();
            var trouble = Online(world, 2, "Trouble");
            var other = Online(world, 3, "Bystander");

            var kicked = KickUserEndpoint.Perform(new KickOrder { AccountId = 2, Reason = "griefing", Admin = "Atomsk", From = "127.0.0.1" });

            Assert.IsTrue(kicked.Kicked);
            Assert.AreEqual(2u, kicked.AccountId);
            Assert.AreEqual("Trouble", kicked.FamilyName);
            Assert.AreEqual(ClientState.Disconnected, trouble.State, "no server timer here: closed at once");
            Assert.IsTrue(trouble.SkipCombatLinger);
            Assert.AreEqual("You have been disconnected by a game master: griefing",
                MissionTestContext.Drain(trouble).OfType<SystemMessagePacket>().Single().TextMessage);
            Assert.AreEqual(ClientState.Ingame, other.State, "nobody else");

            // Gone: not online, by id or by name.
            Assert.AreEqual("not online", KickUserEndpoint.Perform(new KickOrder { AccountId = 2, Admin = "Atomsk" }).Error);
            Assert.AreEqual("not online", KickUserEndpoint.Perform(new KickOrder { FamilyName = "Trouble", Admin = "Atomsk" }).Error);
            Assert.AreEqual("not online", KickUserEndpoint.Perform(new KickOrder { AccountId = 99, Admin = "Atomsk" }).Error);

            // By family name, its case aside; both of an account's connections; a game master's
            // account too - the API may kick anyone, as the console may.
            var first = Online(world, 4, "Warden", GmLevel.Admin);
            var second = Online(world, 4, "Warden", GmLevel.Admin);
            second.State = ClientState.CharacterSelection;

            var gm = KickUserEndpoint.Perform(new KickOrder { FamilyName = "warden", Admin = "Atomsk" });

            Assert.IsTrue(gm.Kicked);
            Assert.AreEqual("Warden", gm.FamilyName);
            Assert.AreEqual(ClientState.Disconnected, first.State);
            Assert.AreEqual(ClientState.Disconnected, second.State);
            Assert.AreEqual("You have been disconnected by a game master.",
                MissionTestContext.Drain(first).OfType<SystemMessagePacket>().Single().TextMessage, "no reason given");
        }

        [TestMethod]
        public void TheEndpointAnswersTheKickOrWhyThereWasNone()
        {
            using var world = new WorldTestContext();
            Online(world, 2, "Trouble");
            KickOrder asked = null;
            var endpoint = new KickUserEndpoint();

            // Before the server has started: not ready.
            Assert.AreEqual(Json(false, "the game server is not ready"), endpoint.Handle(Post("{\"familyName\":\"Trouble\",\"admin\":\"a\"}")).Json);

            endpoint.Kick = order => KickUserEndpoint.Perform(asked = order);

            var request = Post("{\"familyName\":\"Trouble\",\"reason\":\"griefing\",\"admin\":\"Atomsk\"}");
            request.Remote = IPAddress.Parse("::ffff:10.1.2.3");

            var answer = endpoint.Handle(request);

            Assert.AreEqual(200, answer.Status);
            Assert.AreEqual("{\"kicked\":true,\"accountId\":2,\"familyName\":\"Trouble\"}", answer.Json);
            Assert.AreEqual("10.1.2.3", asked.From, "who asked, for the server log");

            answer = endpoint.Handle(Post("{\"accountId\":2,\"admin\":\"Atomsk\"}"));

            Assert.AreEqual(200, answer.Status);
            Assert.AreEqual("{\"kicked\":false,\"error\":\"not online\"}", answer.Json);

            // A world loop that did not get to it.
            endpoint.Kick = order => null;
            Assert.AreEqual(Json(false, "the game server did not answer in time"), endpoint.Handle(Post("{\"accountId\":2,\"admin\":\"a\"}")).Json);
        }

        [TestMethod]
        public void ItIsOffUntilItsOwnEntryTurnsItOnIsAPostAndIsNeverPublicByTheApiBeingSo()
        {
            var config = new RestApiConfig { Public = true, ApiKey = Key };
            var api = new ApiServer();

            api.Register(new KickUserEndpoint { Kick = order => KickOutcome.Failed("not online") });
            api.Apply(config);

            var request = Post("{\"accountId\":2,\"admin\":\"a\"}");

            Assert.AreEqual(404, api.Respond(request).Status, "no entry: off");

            config.Endpoints["kickuser"] = new ApiEndpointConfig { Enabled = false };
            Assert.AreEqual(404, api.Respond(request).Status, "the shipped entry: off");

            config.Endpoints["kickuser"] = new ApiEndpointConfig();
            request.Headers.Remove("X-API-Key");
            Assert.AreEqual(401, api.Respond(request).Status, "the API being public is not this one being public");

            request.Headers["X-API-Key"] = Key;
            Assert.AreEqual(200, api.Respond(request).Status);

            var get = api.Respond(new ApiRequest { Path = "/kickuser", Remote = IPAddress.Loopback });

            Assert.AreEqual(405, get.Status);
            Assert.AreEqual("POST", get.Allow);

            var host = new ApiHost(new ServerStatus());

            CollectionAssert.Contains(host.Rest.Endpoints.ToList(), "kickuser");
            Assert.AreSame(host.Audit, host.Rest.Audit, "the server's API logs to the server's log");
        }

        #endregion

        #region The log

        [TestMethod]
        public void ASecretInABodyOrAnAnswerIsNotKept()
        {
            Assert.AreEqual("{\"email\":\"a@b.c\",\"username\":\"test\",\"password\":\"***\"}",
                ApiAudit.Redact("{\"email\":\"a@b.c\",\"username\":\"test\",\"password\":\"hunter2\"}"));
            Assert.AreEqual("{\"exchangeCode\":\"***\"}", ApiAudit.Redact("{ \"exchangeCode\": \"8F3K-22\" }"));
            Assert.AreEqual("{\"token\":\"***\"}", ApiAudit.Redact("{\"token\":\"eyJhbGciOi.x.y\"}"), "the exchange's answer");

            // At any depth, whatever the value, and a null left as it is.
            Assert.AreEqual("{\"user\":{\"Pass_Word\":\"***\",\"keys\":[{\"apiKey\":\"***\"}],\"pin_code\":\"***\",\"secret\":null}}",
                ApiAudit.Redact("{\"user\":{\"Pass_Word\":12345,\"keys\":[{\"apiKey\":\"k\"}],\"pin_code\":[1,2],\"secret\":null}}"));

            // Nothing secret: kept exactly as it was sent.
            const string kick = "{ \"familyName\" : \"Hart\", \"reason\": \"griefing\", \"admin\": \"Atomsk\" }";
            Assert.AreSame(kick, ApiAudit.Redact(kick));

            // Not JSON that reads, and a form: found by name all the same.
            Assert.AreEqual("{\"username\":\"test\",\"password\":\"***\", \"x\":", ApiAudit.Redact("{\"username\":\"test\",\"password\":\"hunter2\", \"x\":"));
            Assert.AreEqual("{\"password\": \"***\"", ApiAudit.Redact("{\"password\": hunter2"));
            Assert.AreEqual("username=test&password=***&exchange%5Fcode=***", ApiAudit.Redact("username=test&password=hunter2&exchange%5Fcode=X1"));

            Assert.AreEqual("", ApiAudit.Redact(null));
            Assert.IsFalse(ApiAudit.IsSecret("familyName"));
            Assert.IsFalse(ApiAudit.IsSecret("itemTemplateId"));
            Assert.IsTrue(ApiAudit.IsSecret("PASSWORD"));
        }

        [TestMethod]
        public void ARowHoldsTheRequestAndItsAnswerCutToTheTable()
        {
            var store = new Store();
            var audit = new ApiAudit { UtcNow = () => new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc) };

            Assert.AreEqual(0u, audit.Record(Post("{}"), IPAddress.Loopback, ApiResponse.Ok("{}")), "no store, no row");

            audit.Load(store);

            var post = Post("{\"username\":\"test\",\"password\":\"hunter2\"}", "/addaccount");
            post.Query = "x=1";

            Assert.AreEqual(1u, audit.Record(post, IPAddress.Parse("::ffff:192.0.2.7"), ApiResponse.Error(409, "username taken")));

            var row = store.Rows.Single();

            Assert.AreEqual(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc), row.CreatedAt);
            Assert.AreEqual("192.0.2.7", row.Address);
            Assert.AreEqual("POST", row.Method);
            Assert.AreEqual("/addaccount", row.Path);
            Assert.AreEqual("x=1", row.Query);
            Assert.AreEqual(409, row.Status);
            Assert.AreEqual("{\"username\":\"test\",\"password\":\"***\"}", row.Body);
            Assert.AreEqual(post.Body.Length, row.BodyLength);
            Assert.AreEqual("{\"error\":\"username taken\"}", row.Response);

            // A GET has no body; a request that could not be read has nothing but where it came from.
            audit.Record(new ApiRequest { Path = "/serverstatus", Body = "ignored", Remote = IPAddress.IPv6Loopback }, null, ApiResponse.Error(401, "unauthorized"));

            Assert.AreEqual("", store.Rows[1].Body);
            Assert.AreEqual("::1", store.Rows[1].Address, "the request's own address when none is given");
            Assert.AreEqual("GET", store.Rows[1].Method);

            audit.Record(null, IPAddress.Loopback, ApiResponse.Error(400, "bad request"));

            Assert.AreEqual(("127.0.0.1", "", "", 400), (store.Rows[2].Address, store.Rows[2].Method, store.Rows[2].Path, store.Rows[2].Status));

            // What is longer than the table takes is cut, and the body's whole length kept.
            var big = "{\"items\":\"" + new string('x', 20000) + "\"}";
            audit.Record(Post(big, "/updatelootpools" + new string('/', 300)), IPAddress.Loopback, ApiResponse.Ok("{\"r\":\"" + new string('y', 3000) + "\"}"));

            var cut = store.Rows[3];

            Assert.AreEqual(ApiLogEntry.MaxBodyLength, cut.Body.Length);
            Assert.AreEqual(big.Length, cut.BodyLength);
            Assert.AreEqual(ApiLogEntry.MaxPathLength, cut.Path.Length);
            Assert.AreEqual(ApiLogEntry.MaxResponseLength, cut.Response.Length);

            // A store that fails costs the row and nothing else.
            store.Fail = true;
            Assert.AreEqual(0u, audit.Record(post, IPAddress.Loopback, ApiResponse.Ok("{}")));
        }

        [TestMethod]
        public void EveryRequestAListenerAnswersIsOnTheLogRefusedOrNot()
        {
            var store = new Store();
            var audit = new ApiAudit();
            var status = new ServerStatus { AuthLinked = () => true };
            var api = new ApiServer { Audit = audit };

            audit.Load(store);
            status.Started();
            status.Ready();
            status.Beat(() => 1, 8, true);

            var config = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, ApiKey = Key };
            config.Endpoints["kickuser"] = new ApiEndpointConfig();

            api.Register(new HealthCheckEndpoint(status));
            api.Register(new KickUserEndpoint { Kick = order => KickOutcome.Failed("not online") });
            api.Apply(config);

            try
            {
                var port = api.LocalEndPoint.Port;
                var kick = "{\"familyName\":\"Hart\",\"reason\":\"griefing\",\"admin\":\"Atomsk\",\"password\":\"oops\"}";

                StringAssert.StartsWith(Exchange(port, "GET /healthcheck HTTP/1.1\r\nHost: test\r\n\r\n"), "HTTP/1.1 401");
                StringAssert.StartsWith(Exchange(port, "GET /healthcheck?full=1 HTTP/1.1\r\nHost: test\r\nX-API-Key: " + Key + "\r\n\r\n"), "HTTP/1.1 200");
                StringAssert.StartsWith(Exchange(port, "GET /nothing HTTP/1.1\r\nHost: test\r\n\r\n"), "HTTP/1.1 404");
                StringAssert.StartsWith(Exchange(port, "POST /kickuser HTTP/1.1\r\nHost: test\r\nX-API-Key: " + Key + "\r\nContent-Type: application/json\r\nContent-Length: "
                    + Encoding.UTF8.GetByteCount(kick) + "\r\n\r\n" + kick), "HTTP/1.1 200");
                StringAssert.StartsWith(Exchange(port, "this is no request\r\n\r\n"), "HTTP/1.1 400");

                var rows = store.Rows;

                Assert.AreEqual(5, rows.Count);
                CollectionAssert.AreEqual(new[] { 401, 200, 404, 200, 400 }, rows.Select(row => row.Status).ToArray());
                CollectionAssert.AreEqual(new[] { "/healthcheck", "/healthcheck", "/nothing", "/kickuser", "" }, rows.Select(row => row.Path).ToArray());
                Assert.AreEqual("full=1", rows[1].Query);
                Assert.IsTrue(rows.All(row => row.Address == "127.0.0.1"));
                Assert.AreEqual("{\"familyName\":\"Hart\",\"reason\":\"griefing\",\"admin\":\"Atomsk\",\"password\":\"***\"}", rows[3].Body);
                Assert.AreEqual("{\"kicked\":false,\"error\":\"not online\"}", rows[3].Response);
                Assert.IsFalse(rows.Any(row => row.Body.Contains(Key) || row.Response.Contains(Key)), "a key is never kept");
            }
            finally
            {
                api.Stop();
            }
        }

        [TestMethod]
        public void TheRowsAreKeptInTheCharacterDatabase()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var audit = new ApiAudit();

            audit.Load(new ApiAudit.ServerStore(context));

            var first = audit.Record(Post("{\"admin\":\"Atomsk\",\"familyName\":\"Hart\"}"), IPAddress.Loopback, ApiResponse.Ok("{\"kicked\":true}"));
            var second = audit.Record(new ApiRequest { Path = "/usersonline", Remote = IPAddress.Loopback }, null, ApiResponse.Ok("{\"count\":0,\"users\":[]}"));

            Assert.IsTrue(first > 0);
            Assert.IsTrue(second > first);

            using var unit = context.CreateChar();
            var rows = unit.ApiLogs.GetRecent(10);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("/usersonline", rows[0].Path, "newest first");
            Assert.AreEqual("/kickuser", rows[1].Path);
            Assert.AreEqual("{\"admin\":\"Atomsk\",\"familyName\":\"Hart\"}", rows[1].Body);
            Assert.AreEqual("{\"kicked\":true}", rows[1].Response);
            Assert.AreEqual(1, unit.ApiLogs.GetRecent(1).Count);
            Assert.AreEqual(0, unit.ApiLogs.GetRecent(-1).Count);
        }

        #endregion

        private sealed class Store : ApiAudit.IStore
        {
            internal List<ApiLogEntry> Rows { get; } = new();
            internal bool Fail { get; set; }

            public uint Add(ApiLogEntry entry)
            {
                if (Fail)
                    throw new InvalidOperationException("The log is away.");

                lock (Rows)
                {
                    Rows.Add(entry);
                    return (uint)Rows.Count;
                }
            }
        }

        private Client Online(WorldTestContext world, uint accountId, string familyName, GmLevel level = GmLevel.Player)
        {
            var client = world.CreateClient();

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry
            {
                Id = accountId,
                FamilyName = familyName,
                Level = (byte)level,
                Characters = new List<CharacterEntry>()
            });
            typeof(Client).GetProperty(nameof(Client.Socket), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(client, new LengthedSocket(SizeType.Dword, false));

            lock (Server.Clients)
                Server.Clients.Add(client);

            _registered.Add(client);
            return client;
        }

        private static ApiRequest Post(string body, string path = "/kickuser")
        {
            var request = new ApiRequest { Method = "POST", Path = path, Body = body, Remote = IPAddress.Loopback };

            request.Headers["Content-Type"] = "application/json";
            request.Headers["X-API-Key"] = Key;

            return request;
        }

        private static string Json(bool kicked, string error) =>
            "{\"kicked\":" + (kicked ? "true" : "false") + ",\"error\":" + JsonSerializer.Serialize(error) + "}";

        /// <summary>Connects, sends the request, and reads until the other end closes.</summary>
        private static string Exchange(int port, string text)
        {
            using var client = new TcpClient { ReceiveTimeout = 10000, SendTimeout = 10000, NoDelay = true };

            client.Connect(IPAddress.Loopback, port);

            var stream = client.GetStream();
            var bytes = Encoding.UTF8.GetBytes(text);

            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();

            using var received = new MemoryStream();
            var buffer = new byte[1024];

            try
            {
                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    received.Write(buffer, 0, read);
            }
            catch (IOException)
            {
            }

            return Encoding.UTF8.GetString(received.ToArray());
        }
    }
}
