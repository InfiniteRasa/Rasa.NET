using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Config;
    using Rasa.Managers;

    /// <summary>
    /// The REST API's side of the editors in the repository's gametools folder: that a web page
    /// may use an endpoint only where ApiConfig.Rest.AllowedOrigins says so and the endpoint
    /// wants a key, that an endpoint may take a body larger than the usual cap and is asked for
    /// its key before it reads one, and what the four endpoints take and give.
    /// </summary>
    [TestClass]
    public class GameToolsApiTests
    {
        private const string Key = "tools-key";

        #region Web pages

        [TestMethod]
        public void NoPageMayUseTheApiUntilItsOriginIsAllowed()
        {
            var config = Keyed();
            var api = Api(config, out _, out _);

            // A browser's question, and the request it would send, from a page opened from disk.
            var asked = api.Respond(Preflight("/monsterflags", "null", "GET"));

            Assert.AreEqual(403, asked.Status);
            Assert.IsEmpty(asked.Headers);

            var sent = api.Respond(Get("/monsterflags", Key, origin: "null"));

            Assert.AreEqual(200, sent.Status, "The request itself is answered as any other: it is the browser that keeps the answer from the page.");
            Assert.IsEmpty(sent.Headers);

            // Allowed: the question is answered with what the page may send, and the answer may be read.
            config.AllowedOrigins = new List<string> { "null" };
            asked = api.Respond(Preflight("/monsterflags", "null", "GET"));

            Assert.AreEqual(204, asked.Status);
            Assert.AreEqual("null", Header(asked, "Access-Control-Allow-Origin"));
            Assert.AreEqual("GET, HEAD", Header(asked, "Access-Control-Allow-Methods"));
            Assert.AreEqual("X-API-Key, Authorization, Content-Type", Header(asked, "Access-Control-Allow-Headers"));
            Assert.AreEqual("600", Header(asked, "Access-Control-Max-Age"));
            Assert.AreEqual("Origin", Header(asked, "Vary"));
            Assert.IsNull(Header(asked, "Access-Control-Allow-Private-Network"));

            Assert.AreEqual("POST", Header(api.Respond(Preflight("/updatemonsterflags", "null", "POST")), "Access-Control-Allow-Methods"));

            sent = api.Respond(Get("/monsterflags", Key, origin: "null"));

            Assert.AreEqual(200, sent.Status);
            Assert.AreEqual("null", Header(sent, "Access-Control-Allow-Origin"));
            Assert.AreEqual("Origin", Header(sent, "Vary"));

            // A wrong key is an answer the page may read too, so it can say what is wrong.
            var refused = api.Respond(Get("/monsterflags", "not-the-key", origin: "null"));

            Assert.AreEqual(401, refused.Status);
            Assert.AreEqual("null", Header(refused, "Access-Control-Allow-Origin"));

            // A request no page sent is answered as it always was.
            Assert.IsEmpty(api.Respond(Get("/monsterflags", Key)).Headers);
        }

        [TestMethod]
        public void APageIsAllowedByItsOriginAndOnlyForAnEndpointThatWantsAKey()
        {
            var config = Keyed();

            config.AllowedOrigins = new List<string> { "null", "https://Tools.Example/" };

            var api = Api(config, out _, out _);

            Assert.AreEqual(204, api.Respond(Preflight("/lootpools", "https://tools.example", "GET")).Status, "By name, whatever the case and a trailing slash.");
            Assert.AreEqual(403, api.Respond(Preflight("/lootpools", "https://other.example", "GET")).Status);
            Assert.AreEqual(403, api.Respond(Preflight("/lootpools", "http://tools.example", "GET")).Status, "http is another origin.");
            Assert.IsEmpty(api.Respond(Get("/lootpools", Key, origin: "https://other.example")).Headers);

            // The method the page means to use has to be the endpoint's.
            var wrong = api.Respond(Preflight("/lootpools", "null", "POST"));

            Assert.AreEqual(405, wrong.Status);
            Assert.AreEqual("GET, HEAD", wrong.Allow);

            // An endpoint that is off is not there, for a page as for anyone.
            config.Endpoints["lootpools"].Enabled = false;
            Assert.AreEqual(404, api.Respond(Preflight("/lootpools", "null", "GET")).Status);
            config.Endpoints["lootpools"].Enabled = true;

            // One that answers without a key is never opened to pages: any page a browser inside
            // the network loads could use it.
            config.Endpoints["lootpools"].Public = true;
            Assert.AreEqual(403, api.Respond(Preflight("/lootpools", "null", "GET")).Status);
            Assert.IsEmpty(api.Respond(Get("/lootpools", null, origin: "null")).Headers);
            Assert.AreEqual(200, api.Respond(Get("/lootpools", null)).Status);

            config.Public = true;
            Assert.AreEqual(403, api.Respond(Preflight("/healthcheck", "null", "GET")).Status, "Public by the API being so, the same.");
            Assert.IsEmpty(api.Respond(Get("/healthcheck", null, origin: "null")).Headers);

            // Any origin at all, by a star.
            config.Public = false;
            config.Endpoints["lootpools"].Public = null;
            config.AllowedOrigins = new List<string> { "*" };
            Assert.AreEqual("https://anywhere.example", Header(api.Respond(Preflight("/lootpools", "https://anywhere.example", "GET")), "Access-Control-Allow-Origin"));

            // A browser on a public page asking after a private address is told that too.
            var privately = Preflight("/lootpools", "https://anywhere.example", "GET");

            privately.Headers["Access-Control-Request-Private-Network"] = "true";
            Assert.AreEqual("true", Header(api.Respond(privately), "Access-Control-Allow-Private-Network"));
        }

        [TestMethod]
        public void AnAddressThatIsNotAnsweredIsNotToldWhatAPageMayUse()
        {
            var config = Keyed();

            config.AllowedOrigins = new List<string> { "null" };
            config.AllowedIps = new List<string> { "10.0.0.0/8" };

            var api = Api(config, out _, out _);

            // The same 403, and nothing else, whether the endpoint is one a page may use, one
            // that is off, or none at all: nothing to tell them apart by.
            foreach (var path in new[] { "/monsterflags", "/updatelootpools", "/addaccount", "/nothing" })
            {
                var sent = api.Respond(Get(path, Key, origin: "null"));

                Assert.AreEqual(403, sent.Status, path);
                Assert.IsEmpty(sent.Headers, path);

                var asked = api.Respond(Preflight(path, "null", "GET"));

                Assert.AreEqual(403, asked.Status, path);
                Assert.IsEmpty(asked.Headers, path);
            }
        }

        [TestMethod]
        public void AnEndpointThatGoesByACheckOfItsOwnIsNotOpenedToPagesAndTakesNoLargeBody()
        {
            var config = Keyed();

            config.Enabled = true;
            config.BindAddress = "127.0.0.1";
            config.Port = 0;
            config.AllowedOrigins = new List<string> { "null" };
            config.Endpoints["owncheck"] = new ApiEndpointConfig();
            config.Endpoints["ingame/items/{id}"] = new ApiEndpointConfig();

            var api = Api(config, out _, out _);

            api.Register(new OwnCheck());
            api.Register(new Rasa.Api.Ingame.IngameItemDetailsEndpoint(new Rasa.Api.Ingame.IngameSessionService()));

            try
            {
                // It answers without the API's key, as it did, and says nothing to a page.
                var sent = new ApiRequest { Method = "POST", Path = "/owncheck", Body = "{}", Remote = IPAddress.Loopback };

                sent.Headers["Origin"] = "null";

                var answer = api.Respond(sent);

                Assert.AreEqual(200, answer.Status);
                Assert.IsEmpty(answer.Headers);

                var asked = api.Respond(Preflight("/owncheck", "null", "POST"));

                Assert.AreEqual(403, asked.Status);
                Assert.IsEmpty(asked.Headers);

                // The in-game API's own, by a route with a number in it: found, and the same.
                asked = api.Respond(Preflight("/ingame/items/28", "null", "GET"));

                Assert.AreEqual(403, asked.Status, "Not 404: the route is the endpoint's.");
                Assert.IsEmpty(asked.Headers);

                var item = api.Respond(Get("/ingame/items/28", Key, origin: "null"));

                Assert.AreEqual(401, item.Status, "The API's key is not what it goes by.");
                Assert.IsEmpty(item.Headers);

                // It says it takes megabytes, and is let through with no key: the usual cap all the same.
                var port = api.LocalEndPoint.Port;
                var head = "POST /owncheck HTTP/1.1\r\nHost: test\r\nContent-Type: application/json\r\n";

                StringAssert.StartsWith(Exchange(port, head + $"Content-Length: {ApiServer.MaxBodyBytes + 1}\r\nExpect: 100-continue\r\n\r\n"), "HTTP/1.1 413 ");
                StringAssert.StartsWith(Exchange(port, head + "Content-Length: 2\r\n\r\n{}"), "HTTP/1.1 200 ");
            }
            finally
            {
                api.Stop();
            }
        }

        [TestMethod]
        public void AnOptionsRequestNoBrowserSentAndAnOriginThatIsNotPlainTextAreNothingNew()
        {
            var config = Keyed();

            config.AllowedOrigins = new List<string> { "*" };

            var api = Api(config, out _, out _);

            // No origin, or no word of the method meant: not a browser's question.
            var bare = api.Respond(new ApiRequest { Method = "OPTIONS", Path = "/lootpools", Remote = IPAddress.Loopback });

            Assert.AreEqual(405, bare.Status);
            Assert.AreEqual("GET, HEAD", bare.Allow);
            Assert.IsEmpty(bare.Headers);

            var half = new ApiRequest { Method = "OPTIONS", Path = "/lootpools", Remote = IPAddress.Loopback };

            half.Headers["Origin"] = "null";
            Assert.AreEqual(405, api.Respond(half).Status);

            // An origin goes back in a header of the answer, so one with a line break in it, or
            // anything else that is not plain text, is no origin.
            foreach (var origin in new[] { "https://a.example\nSet-Cookie: x=1", "https://a.example\rX: y", "https://a .example", "café", new string('a', 300) })
            {
                var request = Get("/lootpools", Key, origin: origin);

                Assert.IsNull(request.Origin, origin);
                Assert.IsEmpty(api.Respond(request).Headers, origin);
            }
        }

        [TestMethod]
        public void ABrowsersQuestionIsAnsweredOverHttpWithNothingInIt()
        {
            var config = Keyed();

            config.Enabled = true;
            config.BindAddress = "127.0.0.1";
            config.Port = 0;
            config.AllowedOrigins = new List<string> { "null" };

            var api = Api(config, out _, out _);

            try
            {
                var port = api.LocalEndPoint.Port;
                var answer = Exchange(port, "OPTIONS /updatelootpools HTTP/1.1\r\nHost: test\r\nOrigin: null\r\nAccess-Control-Request-Method: POST\r\nAccess-Control-Request-Headers: content-type,x-api-key\r\n\r\n");

                StringAssert.StartsWith(answer, "HTTP/1.1 204 No Content\r\n");
                StringAssert.Contains(answer, "\r\nContent-Length: 0\r\n");
                StringAssert.Contains(answer, "\r\nAccess-Control-Allow-Origin: null\r\n");
                StringAssert.Contains(answer, "\r\nAccess-Control-Allow-Methods: POST\r\n");
                StringAssert.Contains(answer, "\r\nAccess-Control-Allow-Headers: X-API-Key, Authorization, Content-Type\r\n");
                Assert.IsFalse(answer.Contains("Content-Type:"), "Nothing in it, so no type of it.");
                StringAssert.EndsWith(answer, "\r\n\r\n");

                // And the request that follows, as a browser sends it.
                var got = Exchange(port, $"GET /lootpools HTTP/1.1\r\nHost: test\r\nOrigin: null\r\nX-API-Key: {Key}\r\n\r\n");

                StringAssert.StartsWith(got, "HTTP/1.1 200 OK\r\n");
                StringAssert.Contains(got, "\r\nAccess-Control-Allow-Origin: null\r\n");
                StringAssert.Contains(got, "\r\nVary: Origin\r\n");
                StringAssert.Contains(got, "\"format\":\"rasa-loot-tables\"");
            }
            finally
            {
                api.Stop();
            }
        }

        #endregion

        #region Bodies

        [TestMethod]
        public void AnEndpointMayTakeALargeBodyAndIsAskedForItsKeyBeforeItReadsOne()
        {
            var config = Keyed();

            config.Enabled = true;
            config.BindAddress = "127.0.0.1";
            config.Port = 0;
            config.AllowedOrigins = new List<string> { "null" };

            var api = Api(config, out _, out var pools);

            try
            {
                var port = api.LocalEndPoint.Port;

                // Three thousand items in one group: some 200 KB, twenty-five times the usual cap.
                var items = string.Join(",", Enumerable.Range(1, 3000).Select(id =>
                    $"{{\"itemTemplateId\":{id},\"itemName\":\"A name for the reader of the file\",\"chance\":2.5,\"minQuantity\":1,\"maxQuantity\":2}}"));
                var body = "{\"format\":\"rasa-loot-tables\",\"version\":1,\"groups\":[{\"id\":1,\"name\":\"Everything\",\"note\":\"\",\"items\":[" + items + "]}],\"assignments\":[]}";

                Assert.IsGreaterThan(ApiServer.MaxBodyBytes * 20, body.Length);

                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port}/updatelootpools"))
                {
                    request.Headers.Add("X-API-Key", Key);
                    request.Headers.Add("Origin", "null");
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    using var answered = Wait(http.SendAsync(request));

                    Assert.AreEqual(HttpStatusCode.OK, answered.StatusCode);
                    Assert.AreEqual("{\"result\":\"updated\",\"groups\":1,\"items\":3000,\"assignments\":0}", Wait(answered.Content.ReadAsStringAsync()));
                    Assert.AreEqual("null", answered.Headers.GetValues("Access-Control-Allow-Origin").Single());
                }

                Assert.AreEqual(3000, pools.Set.ItemCount);

                var head = "POST /updatelootpools HTTP/1.1\r\nHost: test\r\nContent-Type: application/json\r\n";

                // Nobody without the key has the server wait for, or hold, a large body: the
                // answer comes with none of it sent.
                StringAssert.StartsWith(Exchange(port, head + "Content-Length: 200000\r\n\r\n"), "HTTP/1.1 401 ");
                StringAssert.StartsWith(Exchange(port, head + "X-API-Key: wrong\r\nContent-Length: 200000\r\n\r\n"), "HTTP/1.1 401 ");

                var toPage = Exchange(port, head + "Origin: null\r\nContent-Length: 200000\r\n\r\n");

                StringAssert.StartsWith(toPage, "HTTP/1.1 401 ");
                StringAssert.Contains(toPage, "\r\nAccess-Control-Allow-Origin: null\r\n", "A page is told as much, so it can say the key is wrong.");

                // A client that sends its body anyway - a browser does, key or no key - is
                // not cut off in the middle of it: it gets to the answer that turned it away.
                var megabyte = new string(' ', 1024 * 1024);
                var sentWhole = Exchange(port, head + $"Origin: null\r\nX-API-Key: wrong\r\nContent-Length: {megabyte.Length}\r\n\r\n", megabyte);

                StringAssert.StartsWith(sentWhole, "HTTP/1.1 401 ");
                StringAssert.Contains(sentWhole, "\r\nAccess-Control-Allow-Origin: null\r\n");
                StringAssert.EndsWith(sentWhole, "{\"error\":\"unauthorized\"}");
                Assert.AreEqual(3000, pools.Set.ItemCount, "And none of it reaches the endpoint.");

                // With the key, as much as the endpoint takes and no more.
                StringAssert.StartsWith(Exchange(port, head + $"X-API-Key: {Key}\r\nContent-Length: {GameToolsEndpoint.BodyBytes + 1}\r\n\r\n"), "HTTP/1.1 413 ");

                // An endpoint that does not say it takes more takes the usual 8 KB, key or no key.
                config.Endpoints["addaccount"] = new ApiEndpointConfig();
                api.Register(new AddAccountEndpoint());
                StringAssert.StartsWith(
                    Exchange(port, $"POST /addaccount HTTP/1.1\r\nHost: test\r\nX-API-Key: {Key}\r\nContent-Type: application/json\r\nContent-Length: {ApiServer.MaxBodyBytes + 1}\r\n\r\n"),
                    "HTTP/1.1 413 ");
            }
            finally
            {
                api.Stop();
            }
        }

        [TestMethod]
        public void ALargeBodyWithItsKeyHasLongerToArriveThanAConnectionHas()
        {
            var config = Keyed();

            config.Enabled = true;
            config.BindAddress = "127.0.0.1";
            config.Port = 0;

            var api = Api(config, out _, out var pools);

            try
            {
                var port = api.LocalEndPoint.Port;
                var items = string.Join(",", Enumerable.Range(1, 400).Select(id => $"{{\"itemTemplateId\":{id},\"chance\":2.5,\"minQuantity\":1,\"maxQuantity\":2}}"));
                var body = "{\"groups\":[{\"id\":1,\"name\":\"Slowly\",\"items\":[" + items + "]}]}";

                Assert.IsGreaterThan(ApiServer.MaxBodyBytes, body.Length);

                using var client = new TcpClient { ReceiveTimeout = 10000, SendTimeout = 10000, NoDelay = true };

                client.Connect(IPAddress.Loopback, port);

                var stream = client.GetStream();

                void Send(string text)
                {
                    var bytes = Encoding.UTF8.GetBytes(text);

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                Send($"POST /updatelootpools HTTP/1.1\r\nHost: test\r\nX-API-Key: {Key}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\n\r\n");
                Send(body.Substring(0, body.Length / 2));

                // Past the time any connection has, with half of it still to come.
                Thread.Sleep(TcpService.ExchangeTimeoutMs + 700);
                Send(body.Substring(body.Length / 2));

                using var reader = new StreamReader(stream, Encoding.UTF8);
                var answer = reader.ReadToEnd();

                StringAssert.StartsWith(answer, "HTTP/1.1 200 OK\r\n");
                StringAssert.EndsWith(answer, "{\"result\":\"updated\",\"groups\":1,\"items\":400,\"assignments\":0}");
                Assert.AreEqual(400, pools.Set.ItemCount);
            }
            finally
            {
                api.Stop();
            }
        }

        #endregion

        #region The four endpoints

        [TestMethod]
        public void TheFourAreOffUntilTheirOwnEntriesTurnThemOnAndAreNeverPublicByTheApiBeingSo()
        {
            var config = new RestApiConfig { Public = true, ApiKey = Key };
            var api = Api(config, out _, out _);

            foreach (var (name, method) in new[] { ("monsterflags", "GET"), ("updatemonsterflags", "POST"), ("lootpools", "GET"), ("updatelootpools", "POST") })
            {
                ApiRequest Asking(string key)
                {
                    var request = new ApiRequest { Method = method, Path = "/" + name, Body = "{}", Remote = IPAddress.Loopback };

                    request.Headers["Content-Type"] = "application/json";

                    if (key != null)
                        request.Headers["X-API-Key"] = key;

                    return request;
                }

                Assert.AreEqual(404, api.Respond(Asking(Key)).Status, name);

                config.Endpoints[name] = new ApiEndpointConfig();
                Assert.AreEqual(401, api.Respond(Asking(null)).Status, name + ": the API being public is not this one being public");
                Assert.AreNotEqual(401, api.Respond(Asking(Key)).Status, name);

                config.Endpoints[name].ApiKey = "its-own";
                Assert.AreNotEqual(401, api.Respond(Asking("its-own")).Status, name);

                // Each is a GET or a POST and not the other.
                var other = api.Respond(new ApiRequest { Method = method == "GET" ? "POST" : "GET", Path = "/" + name, Remote = IPAddress.Loopback });

                Assert.AreEqual(405, other.Status, name);
                Assert.AreEqual(method == "GET" ? "GET, HEAD" : "POST", other.Allow, name);
            }

            CollectionAssert.IsSubsetOf(new[] { "lootpools", "monsterflags", "updatelootpools", "updatemonsterflags" }, api.Endpoints.ToList());
        }

        [TestMethod]
        public void MonsterFlagsAreGivenAsTheEditorsFileAndTakenFromIt()
        {
            var api = Api(Keyed(), out var flags, out _);

            flags.Classes.Add(new MonsterFlags(6032, "Bane_Amoeboid_v1", new uint[] { 71, 5 }));
            flags.Classes.Add(new MonsterFlags(10215, "Ambient_Cavern_Creepa", null));

            var read = api.Respond(Get("/monsterflags", Key));

            Assert.AreEqual(200, read.Status);
            Assert.AreEqual(
                "{\"schema\":\"rasa.creature_flags/1\",\"source\":\"server\",\"creatures\":[" +
                "{\"class_id\":6032,\"class_name\":\"Bane_Amoeboid_v1\",\"flags\":[5,71]}," +
                "{\"class_id\":10215,\"class_name\":\"Ambient_Cavern_Creepa\",\"flags\":[]}]}",
                read.Json);

            // The editor's own file, with all it carries beside the flags: only class_id and flags count.
            var written = api.Respond(Post("/updatemonsterflags",
                "{\"schema\":\"rasa.creature_flags/1\",\"edited\":\"2026-10-07T12:00:00Z\",\"flags\":[{\"id\":5,\"name\":\"BIOLOGICAL\"}]," +
                "\"creatures\":[" +
                "{\"class_id\":6032,\"class_name\":\"Bane_Amoeboid_v1\",\"family\":\"Amoeboid\",\"flags\":[142,5,71,5],\"baseline\":[5,71],\"flag_names\":[\"BIOLOGICAL\"],\"changed\":true}," +
                "{\"CLASS_ID\":10215,\"Flags\":[]}]}"));

            Assert.AreEqual(200, written.Status, written.Json);
            Assert.AreEqual("{\"result\":\"updated\",\"classes\":2,\"flags\":3}", written.Json);

            var asked = flags.Replaced.Single();

            CollectionAssert.AreEqual(new uint[] { 6032, 10215 }, asked.Select(entry => entry.ClassId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 5, 71, 142 }, asked[0].Flags.ToArray(), "Each flag once, in order.");
            Assert.IsEmpty(asked[1].Flags, "No flags is an answer too: the class is left with none.");
        }

        [TestMethod]
        public void MonsterFlagsAreRefusedWholeWhenAnyOfThemWillNotDo()
        {
            var api = Api(Keyed(), out var flags, out _);

            ApiResponse Sent(string body) => api.Respond(Post("/updatemonsterflags", body));

            Assert.AreEqual(400, Sent("not json").Status);
            Assert.AreEqual(400, Sent("[]").Status);
            StringAssert.Contains(Sent("{}").Json, "creatures array");
            StringAssert.Contains(Sent("{\"creatures\":{}}").Json, "creatures array");

            var bad = Sent("{\"creatures\":[" +
                           "{\"flags\":[1]}," +
                           "{\"class_id\":0,\"flags\":[1]}," +
                           "{\"class_id\":\"12\",\"flags\":[1]}," +
                           "{\"class_id\":12}," +
                           "{\"class_id\":13,\"flags\":[1,-2]}," +
                           "{\"class_id\":14,\"flags\":[1.5]}," +
                           "{\"class_id\":15,\"flags\":[0]}," +
                           "{\"class_id\":16,\"flags\":[1]}," +
                           "{\"class_id\":16,\"flags\":[2]}]}");

            Assert.AreEqual(400, bad.Status);

            var problems = Problems(bad);

            Assert.HasCount(8, problems);
            StringAssert.Contains(problems[0], "creatures[0]: class_id");
            StringAssert.Contains(problems[3], "creatures[3] (class 12): flags must be an array");
            StringAssert.Contains(problems[4], "class 13");
            StringAssert.Contains(problems[7], "class 16 is named twice");
            Assert.IsEmpty(flags.Replaced, "Nothing reaches the store.");

            // What the store itself will not have: a class that is not a creature's, a flag there is not.
            flags.Refuse = "class 99 is not a class the server has";

            var refused = Sent("{\"creatures\":[{\"class_id\":99,\"flags\":[1]}]}");

            Assert.AreEqual(400, refused.Status);
            StringAssert.Contains(refused.Json, "nothing was changed");
            CollectionAssert.AreEqual(new[] { "class 99 is not a class the server has" }, Problems(refused));

            // Sent as JSON and as nothing else, as /addaccount is; and nothing to do before the server has its store.
            var form = Post("/updatemonsterflags", "{\"creatures\":[]}");

            form.Headers["Content-Type"] = "text/plain";
            Assert.AreEqual(415, api.Respond(form).Status);

            var early = new ApiServer();
            var config = Keyed();

            early.Register(new MonsterFlagsEndpoint());
            early.Register(new UpdateMonsterFlagsEndpoint());
            early.Register(new LootPoolsEndpoint());
            early.Register(new UpdateLootPoolsEndpoint());
            early.Apply(config);

            Assert.AreEqual(503, early.Respond(Get("/monsterflags", Key)).Status);
            Assert.AreEqual(503, early.Respond(Post("/updatemonsterflags", "{\"creatures\":[]}")).Status);
            Assert.AreEqual(503, early.Respond(Get("/lootpools", Key)).Status);
            Assert.AreEqual(503, early.Respond(Post("/updatelootpools", "{\"groups\":[]}")).Status);
        }

        [TestMethod]
        public void LootPoolsAreGivenAsTheEditorsProjectFileAndTakenFromIt()
        {
            var api = Api(Keyed(), out _, out var pools);

            pools.Set = new LootPoolSet(
                new[]
                {
                    new LootPool(2, "Level 30 gear", "", new[] { new LootPoolItem(1200, 0.25, 1, 1) }),
                    new LootPool(1, "Thrax \"junk\"", "what a Thrax carries", new[] { new LootPoolItem(28, 50, 1, 3), new LootPoolItem(40, 12.5, 2, 2) })
                },
                new (uint, uint)[] { (500, 2), (500, 1), (77, 1), (77, 9) });

            var read = api.Respond(Get("/lootpools", Key));

            Assert.AreEqual(200, read.Status);

            using (var document = JsonDocument.Parse(read.Json))
            {
                var root = document.RootElement;

                Assert.AreEqual("rasa-loot-tables", root.GetProperty("format").GetString());
                Assert.AreEqual(1, root.GetProperty("version").GetInt32());
                Assert.AreEqual("server", root.GetProperty("source").GetString());

                var groups = root.GetProperty("groups").EnumerateArray().ToList();

                Assert.HasCount(2, groups);
                Assert.AreEqual(1u, groups[0].GetProperty("id").GetUInt32(), "By id.");
                Assert.AreEqual("Thrax \"junk\"", groups[0].GetProperty("name").GetString());
                Assert.AreEqual("what a Thrax carries", groups[0].GetProperty("note").GetString());

                var first = groups[0].GetProperty("items").EnumerateArray().First();

                Assert.AreEqual(28u, first.GetProperty("itemTemplateId").GetUInt32());
                Assert.AreEqual(50d, first.GetProperty("chance").GetDouble());
                Assert.AreEqual(1u, first.GetProperty("minQuantity").GetUInt32());
                Assert.AreEqual(3u, first.GetProperty("maxQuantity").GetUInt32());
                Assert.AreEqual(0.25d, groups[1].GetProperty("items").EnumerateArray().Single().GetProperty("chance").GetDouble());

                // A pool that is not there is nobody's.
                CollectionAssert.AreEqual(
                    new[] { "77:1", "500:1", "500:2" },
                    root.GetProperty("assignments").EnumerateArray().Select(link => $"{link.GetProperty("creatureId").GetUInt32()}:{link.GetProperty("groupId").GetUInt32()}").ToArray());
            }

            // What was read can be sent back as it is, and what the editor saves, names and all.
            pools.Set = LootPoolSet.Empty;
            Assert.AreEqual(200, api.Respond(Post("/updatelootpools", read.Json)).Status);
            Assert.AreEqual(3, pools.Set.ItemCount);
            Assert.HasCount(3, pools.Set.Assignments);

            var written = api.Respond(Post("/updatelootpools",
                "{\"format\":\"rasa-loot-tables\",\"version\":1,\"savedAt\":\"2026-10-07T12:00:00Z\",\"groups\":[" +
                "{\"id\":4,\"name\":\"  Caretaker salves \",\"note\":\" as the text says \",\"items\":[" +
                "{\"itemTemplateId\":900,\"itemName\":\"Mafenide\",\"chance\":12.34567,\"minQuantity\":2,\"maxQuantity\":5}," +
                "{\"itemTemplateId\":901,\"chance\":100}," +
                "{\"itemTemplateId\":902,\"chance\":0,\"minQuantity\":4}]}," +
                "{\"id\":5,\"name\":\"Empty\",\"comment\":\"from the SQL column's name\"}]," +
                "\"assignments\":[{\"creatureId\":3000,\"creatureName\":\"Caretaker\",\"groupId\":4},{\"creatureId\":3000,\"groupId\":4},{\"creatureId\":3001,\"groupId\":5}]}"));

            Assert.AreEqual(200, written.Status, written.Json);
            Assert.AreEqual("{\"result\":\"updated\",\"groups\":2,\"items\":3,\"assignments\":2,\"warnings\":[\"group 5 has no items\"]}", written.Json);

            var salves = pools.Set.Pools[0];

            Assert.AreEqual("Caretaker salves", salves.Name);
            Assert.AreEqual("as the text says", salves.Note);
            Assert.AreEqual(12.3457, salves.Items[0].Chance, "A chance is kept to four places.");
            Assert.AreEqual((2u, 5u), (salves.Items[0].Minimum, salves.Items[0].Maximum));
            Assert.AreEqual((1u, 1u), (salves.Items[1].Minimum, salves.Items[1].Maximum), "One, when the file does not say how many.");
            Assert.AreEqual((4u, 4u), (salves.Items[2].Minimum, salves.Items[2].Maximum), "As many as the least, when the file does not say the most.");
            Assert.AreEqual("from the SQL column's name", pools.Set.Pools[1].Note);
            Assert.HasCount(1, pools.Set.For(3000), "A creature has a pool once.");

            // An empty project is no pools at all.
            Assert.AreEqual("{\"result\":\"updated\",\"groups\":0,\"items\":0,\"assignments\":0}", api.Respond(Post("/updatelootpools", "{\"groups\":[]}")).Json);
            Assert.IsEmpty(pools.Set.Pools);
        }

        [TestMethod]
        public void LootPoolsAreRefusedWholeWhenAnyOfThemWillNotDo()
        {
            var api = Api(Keyed(), out _, out var pools);
            var before = pools.Set = new LootPoolSet(new[] { new LootPool(1, "Kept", "", null) }, null);

            ApiResponse Sent(string body) => api.Respond(Post("/updatelootpools", body));

            Assert.AreEqual(400, Sent("{").Status);
            StringAssert.Contains(Sent("{\"assignments\":[]}").Json, "groups array");

            var bad = Sent("{\"groups\":[" +
                           "{\"name\":\"No id\"}," +
                           "{\"id\":1,\"name\":\"\"}," +
                           "{\"id\":1,\"name\":\"Again\"}," +
                           "{\"id\":2,\"name\":\"" + new string('n', 101) + "\",\"note\":\"" + new string('c', 201) + "\"}," +
                           "{\"id\":3,\"name\":\"Items\",\"items\":{}}," +
                           "{\"id\":4,\"name\":\"Rows\",\"items\":[" +
                           "{\"chance\":5}," +
                           "{\"itemTemplateId\":10,\"chance\":5},{\"itemTemplateId\":10,\"chance\":6}," +
                           "{\"itemTemplateId\":11}," +
                           "{\"itemTemplateId\":12,\"chance\":100.5}," +
                           "{\"itemTemplateId\":13,\"chance\":-1}," +
                           "{\"itemTemplateId\":14,\"chance\":\"5\"}," +
                           "{\"itemTemplateId\":15,\"chance\":5,\"minQuantity\":0}," +
                           "{\"itemTemplateId\":16,\"chance\":5,\"maxQuantity\":100001}," +
                           "{\"itemTemplateId\":17,\"chance\":5,\"minQuantity\":3,\"maxQuantity\":2}]}]," +
                           "\"assignments\":[{\"creatureId\":7},{\"creatureId\":7,\"groupId\":40},{\"creatureId\":0,\"groupId\":4}]}");

            Assert.AreEqual(400, bad.Status);

            var problems = Problems(bad);

            StringAssert.Contains(problems[0], "groups[0]: id");
            StringAssert.Contains(problems[1], "groups[1] (group 1): name must be 1 to 100 letters");
            StringAssert.Contains(problems[2], "group 1 is there twice");
            Assert.IsTrue(problems.Any(problem => problem.Contains("(group 2): note must be no more than 200")));
            Assert.IsTrue(problems.Any(problem => problem.Contains("(group 3): items must be an array")));
            Assert.IsTrue(problems.Any(problem => problem.Contains("items[0]: itemTemplateId")));
            Assert.IsTrue(problems.Any(problem => problem.Contains("item 10 is in the group twice")));

            foreach (var item in new[] { 11, 12, 13, 14 })
                Assert.IsTrue(problems.Any(problem => problem.Contains($"(item {item}): chance must be a percent from 0 to 100")), item.ToString());

            foreach (var item in new[] { 15, 16 })
                Assert.IsTrue(problems.Any(problem => problem.Contains($"(item {item}): minQuantity and maxQuantity")), item.ToString());

            Assert.IsTrue(problems.Any(problem => problem.Contains("(item 17): maxQuantity is less than minQuantity")));
            Assert.AreSame(before, pools.Set, "Nothing reaches the store.");

            // More problems than are worth a list are counted.
            var many = Sent("{\"groups\":[" + string.Join(",", Enumerable.Range(0, 30).Select(_ => "{\"id\":0}")) + "]}");
            var listed = Problems(many);

            Assert.HasCount(GameToolsEndpoint.ProblemsListed + 1, listed);
            Assert.AreEqual("and 10 more", listed[^1]);

            // And a body that is nothing but problems is not gone through to its end.
            foreach (var nothing in new[]
                     {
                         "{\"groups\":[" + string.Join(",", Enumerable.Repeat("0", 50000)) + "]}",
                         "{\"groups\":[{\"id\":1,\"name\":\"Rows\",\"items\":[" + string.Join(",", Enumerable.Repeat("0", 50000)) + "]}]}",
                         "{\"groups\":[],\"assignments\":[" + string.Join(",", Enumerable.Repeat("0", 50000)) + "]}"
                     })
            {
                listed = Problems(Sent(nothing));

                Assert.HasCount(GameToolsEndpoint.ProblemsListed + 1, listed);
                Assert.AreEqual($"and more: looking stopped at {StoreAnswer.MostKept}", listed[^1]);
            }

            // A group nobody gave: its own problem, with the creature's number.
            StringAssert.Contains(
                Problems(Sent("{\"groups\":[{\"id\":4,\"name\":\"Rows\"}],\"assignments\":[{\"creatureId\":7,\"groupId\":40}]}")).Single(),
                "creature 7 is given group 40, which is not in the file");

            // What the store itself will not have: an item or a creature the server has not got.
            pools.Refuse = "creature 7 is not a creature row the server has";

            var refused = Sent("{\"groups\":[{\"id\":4,\"name\":\"Rows\"}],\"assignments\":[{\"creatureId\":7,\"groupId\":4}]}");

            Assert.AreEqual(400, refused.Status);
            CollectionAssert.AreEqual(new[] { "creature 7 is not a creature row the server has" }, Problems(refused));
            Assert.AreSame(before, pools.Set);
        }

        #endregion

        #region Fixture

        /// <summary>The settings the editors want: every endpoint behind one key, the four of theirs on.</summary>
        private static RestApiConfig Keyed()
        {
            var config = new RestApiConfig { ApiKey = Key };

            foreach (var name in new[] { "monsterflags", "updatemonsterflags", "lootpools", "updatelootpools" })
                config.Endpoints[name] = new ApiEndpointConfig();

            return config;
        }

        /// <summary>The API with the four endpoints on stand-ins for their stores, and /healthcheck.</summary>
        private static ApiServer Api(RestApiConfig config, out FlagStore flags, out PoolStore pools)
        {
            var status = new ServerStatus { AuthLinked = () => true };
            var api = new ApiServer();

            status.Started();
            status.Ready();
            status.Beat(() => 1, 8, true);

            flags = new FlagStore();
            pools = new PoolStore();

            api.Register(new HealthCheckEndpoint(status));
            api.Register(new MonsterFlagsEndpoint { Store = flags });
            api.Register(new UpdateMonsterFlagsEndpoint { Store = flags });
            api.Register(new LootPoolsEndpoint { Store = pools });
            api.Register(new UpdateLootPoolsEndpoint { Store = pools });
            api.Apply(config);

            return api;
        }

        private sealed class FlagStore : IMonsterFlagStore
        {
            internal List<MonsterFlags> Classes { get; } = new List<MonsterFlags>();
            internal List<IReadOnlyList<MonsterFlags>> Replaced { get; } = new List<IReadOnlyList<MonsterFlags>>();
            internal string Refuse { get; set; }

            public IReadOnlyList<MonsterFlags> Read() => Classes;

            public StoreAnswer Replace(IReadOnlyList<MonsterFlags> classes)
            {
                var answer = new StoreAnswer();

                if (Refuse != null)
                    answer.Problems.Add(Refuse);
                else
                    Replaced.Add(classes);

                return answer;
            }
        }

        private sealed class PoolStore : ILootPoolStore
        {
            internal LootPoolSet Set { get; set; } = LootPoolSet.Empty;
            internal string Refuse { get; set; }

            public LootPoolSet Read() => Set;

            public StoreAnswer Replace(LootPoolSet set)
            {
                var answer = new StoreAnswer();

                if (Refuse != null)
                {
                    answer.Problems.Add(Refuse);
                    return answer;
                }

                foreach (var pool in set.Pools.Where(pool => pool.Items.Count == 0))
                    answer.Warnings.Add($"group {pool.Id} has no items");

                Set = set;

                return answer;
            }
        }

        /// <summary>An endpoint that is let through without the API's key and checks for itself, as the /ingame ones do.</summary>
        private sealed class OwnCheck : ApiEndpoint
        {
            public override string Name => "owncheck";
            public override string Method => "POST";
            public override bool Sensitive => true;
            public override bool RequiresApiKey => false;
            public override int MaxBodyBytes => GameToolsEndpoint.BodyBytes;

            public override ApiResponse Handle(ApiRequest request) => ApiResponse.Ok("{\"result\":\"ok\"}");
        }

        private static ApiRequest Get(string path, string key, string origin = null)
        {
            var request = new ApiRequest { Path = path, Remote = IPAddress.Loopback };

            if (key != null)
                request.Headers["X-API-Key"] = key;

            if (origin != null)
                request.Headers["Origin"] = origin;

            return request;
        }

        private static ApiRequest Post(string path, string body)
        {
            var request = new ApiRequest { Method = "POST", Path = path, Body = body, Remote = IPAddress.Loopback };

            request.Headers["Content-Type"] = "application/json";
            request.Headers["X-API-Key"] = Key;

            return request;
        }

        private static ApiRequest Preflight(string path, string origin, string method)
        {
            var request = new ApiRequest { Method = "OPTIONS", Path = path, Remote = IPAddress.Loopback };

            request.Headers["Origin"] = origin;
            request.Headers["Access-Control-Request-Method"] = method;
            request.Headers["Access-Control-Request-Headers"] = "x-api-key";

            return request;
        }

        private static string Header(ApiResponse response, string name) =>
            response.Headers.Where(header => header.Key == name).Select(header => header.Value).SingleOrDefault();

        private static List<string> Problems(ApiResponse response)
        {
            using var document = JsonDocument.Parse(response.Json);

            return document.RootElement.GetProperty("problems").EnumerateArray().Select(problem => problem.GetString()).ToList();
        }

        private static T Wait<T>(Task<T> task)
        {
            if (Task.WaitAny(new Task[] { task }, TimeSpan.FromSeconds(15)) < 0)
                Assert.Fail("no answer in fifteen seconds");

            return task.GetAwaiter().GetResult();
        }

        /// <summary>Connects, sends each part with a pause between them, and reads until the other end closes.</summary>
        private static string Exchange(int port, params string[] parts)
        {
            using var client = new TcpClient { ReceiveTimeout = 10000, SendTimeout = 10000, NoDelay = true };

            client.Connect(IPAddress.Loopback, port);

            var stream = client.GetStream();

            using var received = new MemoryStream();
            var buffer = new byte[1024];

            try
            {
                foreach (var part in parts)
                {
                    var bytes = Encoding.UTF8.GetBytes(part);

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                    Thread.Sleep(60);
                }

                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    received.Write(buffer, 0, read);
            }
            catch (IOException)
            {
                // Closed on us before it was written to or read.
            }

            return Encoding.UTF8.GetString(received.ToArray());
        }

        #endregion
    }
}
