using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Config;
    using Rasa.Context.Auth;
    using Rasa.Data;
    using Rasa.Packets;
    using Rasa.Packets.Communicator;
    using Rasa.Repositories.Auth.Account;
    using Rasa.Services.Passwords;
    using Rasa.Services.Random;
    using Rasa.Structures.Auth;
    using Rasa.Test.Database;

    /// <summary>
    /// POST /addaccount, end to end in its parts: what details an account may have, the request
    /// and its answer on the link between the game server and Auth, Auth making the account, the
    /// game server waiting for the answer, and the REST endpoint that starts it all.
    /// </summary>
    [TestClass]
    public class AddAccountApiTests
    {
        private const string Json = "{\"email\":\"new@example.com\",\"username\":\"Newcomer\",\"password\":\"secret-pw\"}";

        #region The details

        [TestMethod]
        public void AnAccountsDetailsAreWhatTheGameClientCanSend()
        {
            Assert.IsNull(AccountRules.Problem("a@b.c", "x", "p"));
            Assert.IsNull(AccountRules.Problem("someone@example.com", "FourteenLetter", "sixteen-letters!"));
            Assert.IsNull(AccountRules.Problem("someone@example.com", "name", "a pass phrase"), "a space in a password is the player's business");

            StringAssert.Contains(AccountRules.Problem("a@b.c", null, "p"), "username is required");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "", "p"), "username is required");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "FifteenLetters!", "p"), "longer than 14");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "éééééééé", "p"), "longer than 14", "counted in the bytes the client sends");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "two words", "p"), "space");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "tab\there", "p"), "space");

            StringAssert.Contains(AccountRules.Problem("a@b.c", "x", null), "password is required");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "x", ""), "password is required");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "x", "seventeen-letters!"), "longer than 16");
            StringAssert.Contains(AccountRules.Problem("a@b.c", "x", "line\nbreak"), "control character");

            StringAssert.Contains(AccountRules.Problem(null, "x", "p"), "email is required");
            StringAssert.Contains(AccountRules.Problem(new string('a', 250) + "@b.com", "x", "p"), "longer than 255");

            foreach (var notAnAddress in new[] { "nobody", "@example.com", "nobody@", "a@b@c", "some body@example.com" })
                StringAssert.Contains(AccountRules.Problem(notAnAddress, "x", "p"), "not an address", notAnAddress);
        }

        #endregion

        #region The link

        [TestMethod]
        public void TheRequestAndItsAnswerCrossTheLinkWhole()
        {
            var request = Through(new CreateAccountRequestPacket { RequestId = 77, Email = "néw@example.com", Username = "Newcomer", Password = "päss word" });

            Assert.AreEqual(CommOpcode.CreateAccountRequest, request.Opcode);
            Assert.AreEqual(77u, request.RequestId);
            Assert.AreEqual("néw@example.com", request.Email);
            Assert.AreEqual("Newcomer", request.Username);
            Assert.AreEqual("päss word", request.Password);
            Assert.IsFalse(request.ToString().Contains("ss word"), "the password is not in what a log shows of it");
            StringAssert.Contains(request.ToString(), "Newcomer");

            var answer = Through(new CreateAccountResponsePacket { RequestId = 77, Result = CreateAccountResult.EmailTaken, AccountId = 0 });

            Assert.AreEqual(CommOpcode.CreateAccountResponse, answer.Opcode);
            Assert.AreEqual(77u, answer.RequestId);
            Assert.AreEqual(CreateAccountResult.EmailTaken, answer.Result);

            var created = Through(new CreateAccountResponsePacket { RequestId = uint.MaxValue, Result = CreateAccountResult.Created, AccountId = 4012 });

            Assert.AreEqual(uint.MaxValue, created.RequestId);
            Assert.AreEqual(CreateAccountResult.Created, created.Result);
            Assert.AreEqual(4012u, created.AccountId);

            // After the opcodes there were, and not in place of one.
            Assert.AreEqual(7, (int)CommOpcode.CreateAccountRequest);
            Assert.AreEqual(8, (int)CommOpcode.CreateAccountResponse);
        }

        #endregion

        #region Auth makes the account

        [TestMethod]
        public void AuthMakesTheAccountAndItCanBeLoggedInTo()
        {
            using var database = new AuthDatabase();
            var accounts = database.Accounts;

            Assert.AreEqual(CreateAccountResult.Created,
                Rasa.Auth.AccountCreation.Create(accounts, null, "new@example.com", "Newcomer", "secret-pw", out var id));
            Assert.AreNotEqual(0u, id);

            var entry = accounts.GetByUserName("Newcomer", "secret-pw");

            Assert.AreEqual(id, entry.Id);
            Assert.AreEqual("new@example.com", entry.Email);
            Assert.AreNotEqual("secret-pw", entry.Password, "hashed");
            Assert.IsFalse(entry.Locked);
            Assert.ThrowsExactly<PasswordCheckFailedException>(() => accounts.GetByUserName("Newcomer", "wrong"));

            // A second, with its own id.
            Assert.AreEqual(CreateAccountResult.Created,
                Rasa.Auth.AccountCreation.Create(accounts, null, "other@example.com", "Other", "secret-pw", out var second));
            Assert.AreNotEqual(id, second);
            Assert.AreNotEqual(0u, second);
        }

        [TestMethod]
        public void ANameOrAnAddressInUseIsSaidToBeAndNothingIsMade()
        {
            using var database = new AuthDatabase();
            var accounts = database.Accounts;
            var before = database.Context.AuthAccountEntries.Count();
            var completed = 0;

            Assert.AreEqual(CreateAccountResult.Created,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "new@example.com", "Newcomer", "secret-pw", out _));
            Assert.AreEqual(1, completed);

            // In any case: "newcomer" beside "Newcomer" is two accounts to Sqlite and one to the client's player.
            Assert.AreEqual(CreateAccountResult.UsernameTaken,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "else@example.com", "NEWCOMER", "secret-pw", out var id));
            Assert.AreEqual(0u, id);

            Assert.AreEqual(CreateAccountResult.EmailTaken,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "New@Example.COM", "Somebody", "secret-pw", out id));
            Assert.AreEqual(0u, id);

            // Both taken: the name is what is said.
            Assert.AreEqual(CreateAccountResult.UsernameTaken,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "new@example.com", "newcomer", "secret-pw", out _));

            Assert.AreEqual(CreateAccountResult.Invalid,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "else@example.com", "a name with spaces", "secret-pw", out _));
            Assert.AreEqual(CreateAccountResult.Invalid,
                Rasa.Auth.AccountCreation.Create(accounts, () => completed++, "else@example.com", "Somebody", "a password that is too long", out _));

            Assert.AreEqual(1, completed);
            Assert.AreEqual(before + 1, database.Context.AuthAccountEntries.Count(), "the one, beside whatever a new database starts with");
            Assert.IsNull(accounts.FindByUserNameOrEmail("Somebody", "else@example.com"));
            Assert.AreEqual("Newcomer", accounts.FindByUserNameOrEmail("nobody", "NEW@example.com").Username);
        }

        [TestMethod]
        public void ADatabaseThatRefusesIsAFailureAndALostRaceIsANameTaken()
        {
            var broken = new FakeAccounts { CreateThrows = new InvalidOperationException("the database is away") };

            Assert.AreEqual(CreateAccountResult.Failed,
                Rasa.Auth.AccountCreation.Create(broken, null, "new@example.com", "Newcomer", "secret-pw", out var id));
            Assert.AreEqual(0u, id);

            // Somebody else's insert landed between the look and the insert: the unique index
            // refuses this one, and the look after it finds theirs.
            var raced = new FakeAccounts { CreateThrows = new InvalidOperationException("UNIQUE constraint failed: account.username") };

            raced.FoundAfterCreate = new AuthAccountEntry { Id = 9, Username = "newcomer", Email = "theirs@example.com" };

            Assert.AreEqual(CreateAccountResult.UsernameTaken,
                Rasa.Auth.AccountCreation.Create(raced, null, "new@example.com", "Newcomer", "secret-pw", out _));

            // Even the look fails.
            var gone = new FakeAccounts { FindThrows = new InvalidOperationException("the database is away") };

            Assert.AreEqual(CreateAccountResult.Failed,
                Rasa.Auth.AccountCreation.Create(gone, null, "new@example.com", "Newcomer", "secret-pw", out _));
        }

        #endregion

        #region The game server asks and waits

        [TestMethod]
        public void TheRelayHandsOnTheRequestAndReturnsTheAnswerWithItsNumber()
        {
            var sent = new List<CreateAccountRequestPacket>();
            AccountRelay relay = null;

            relay = new AccountRelay
            {
                Connected = () => true,
                Send = request =>
                {
                    lock (sent)
                        sent.Add(request);

                    // The answer comes on another thread, as it does from the link.
                    Task.Run(() =>
                    {
                        // One for somebody else first: not this request's.
                        relay.Answer(new CreateAccountResponsePacket { RequestId = request.RequestId + 1000, Result = CreateAccountResult.Failed });
                        relay.Answer(new CreateAccountResponsePacket
                        {
                            RequestId = request.RequestId,
                            Result = request.Username == "Taken" ? CreateAccountResult.UsernameTaken : CreateAccountResult.Created,
                            AccountId = request.Username == "Taken" ? 0u : 500 + request.RequestId
                        });
                    });
                }
            };

            var first = relay.Create("new@example.com", "Newcomer", "secret-pw");

            Assert.IsTrue(first.Asked);
            Assert.IsTrue(first.Answered);
            Assert.AreEqual(CreateAccountResult.Created, first.Result);
            Assert.AreEqual(500 + sent[0].RequestId, first.AccountId);
            Assert.AreEqual("new@example.com", sent[0].Email);
            Assert.AreEqual("Newcomer", sent[0].Username);
            Assert.AreEqual("secret-pw", sent[0].Password);

            var second = relay.Create("else@example.com", "Taken", "secret-pw");

            Assert.AreEqual(CreateAccountResult.UsernameTaken, second.Result);
            Assert.AreEqual(0u, second.AccountId);
            Assert.AreNotEqual(sent[0].RequestId, sent[1].RequestId, "every request has a number of its own");
            Assert.AreEqual(0, relay.Waiting);
        }

        [TestMethod]
        public void TheRelaySaysSoWhenAuthIsNotThereOrDoesNotAnswer()
        {
            var sent = 0;
            var connected = false;
            CreateAccountRequestPacket last = null;
            var relay = new AccountRelay
            {
                TimeoutMs = 100,
                Connected = () => connected,
                Send = request =>
                {
                    sent++;
                    last = request;
                }
            };

            // The link is down: nothing goes out.
            var down = relay.Create("new@example.com", "Newcomer", "secret-pw");

            Assert.IsFalse(down.Asked);
            Assert.IsFalse(down.Answered);
            Assert.AreEqual(0, sent);

            // Up, and no answer: an Auth server from before this message ignores it.
            connected = true;

            var silent = relay.Create("new@example.com", "Newcomer", "secret-pw");

            Assert.IsTrue(silent.Asked);
            Assert.IsFalse(silent.Answered);
            Assert.AreEqual(1, sent);
            Assert.AreEqual(0, relay.Waiting, "nothing is kept of a request that ran out");

            // The answer, too late, is nobody's.
            relay.Answer(new CreateAccountResponsePacket { RequestId = last.RequestId, Result = CreateAccountResult.Created, AccountId = 3 });
            relay.Answer(null);
            Assert.AreEqual(0, relay.Waiting);

            // The link fails as the request is put on it.
            relay.Send = _ => throw new InvalidOperationException("the link to the Auth server is down");

            var failed = relay.Create("new@example.com", "Newcomer", "secret-pw");

            Assert.IsFalse(failed.Asked);
            Assert.AreEqual(0, relay.Waiting);

            // Nothing to send with at all.
            relay.Send = null;
            Assert.IsFalse(relay.Create("new@example.com", "Newcomer", "secret-pw").Asked);
            Assert.IsFalse(new AccountRelay().Create("new@example.com", "Newcomer", "secret-pw").Asked);
        }

        #endregion

        #region The endpoint

        [TestMethod]
        public void TheEndpointIsOffUntilItsOwnEntryTurnsItOnAndIsNeverPublicByTheApiBeingSo()
        {
            var config = new RestApiConfig { Public = true, ApiKey = "global-key" };
            var api = Api(config, out var asked);

            // No entry of its own: not there, though the API is public and its other endpoints answer.
            Assert.AreEqual(200, api.Respond(new ApiRequest { Path = "/healthcheck", Remote = IPAddress.Loopback }).Status);
            Assert.AreEqual(404, api.Respond(Post(Json, "global-key")).Status);

            // An entry that says nothing of Enabled is one that turns it on, as for any endpoint.
            config.Endpoints["addaccount"] = new ApiEndpointConfig { Enabled = false };
            Assert.AreEqual(404, api.Respond(Post(Json, "global-key")).Status);

            config.Endpoints["addaccount"] = new ApiEndpointConfig();
            Assert.AreEqual(401, api.Respond(Post(Json, null)).Status, "the API being public is not this one being public");
            Assert.AreEqual(401, api.Respond(Post(Json, "wrong")).Status);
            Assert.AreEqual(0, asked.Count, "nothing reaches Auth without the key");

            // The global key opens it, and its own.
            Assert.AreEqual(201, api.Respond(Post(Json, "global-key")).Status);

            config.Endpoints["addaccount"].ApiKey = "account-key";
            Assert.AreEqual(201, api.Respond(Post(Json, "account-key")).Status);
            Assert.AreEqual(2, asked.Count);

            // Public by its own word only.
            config.Endpoints["addaccount"].Public = true;
            Assert.AreEqual(201, api.Respond(Post(Json, null)).Status);

            // It is a POST and nothing else, and the others are not.
            foreach (var method in new[] { "GET", "HEAD", "PUT", "DELETE" })
            {
                var refused = api.Respond(new ApiRequest { Method = method, Path = "/addaccount", Remote = IPAddress.Loopback });

                Assert.AreEqual(405, refused.Status, method);
                Assert.AreEqual("POST", refused.Allow);
            }

            var post = api.Respond(new ApiRequest { Method = "POST", Path = "/healthcheck", Remote = IPAddress.Loopback });

            Assert.AreEqual(405, post.Status);
            Assert.AreEqual("GET, HEAD", post.Allow);
        }

        [TestMethod]
        public void TheEndpointAsksAuthForTheAccountAndAnswersWithWhatAuthSaid()
        {
            var config = new RestApiConfig { ApiKey = "global-key" };

            config.Endpoints["addaccount"] = new ApiEndpointConfig();

            var api = Api(config, out var asked, out var endpoint);

            var created = api.Respond(Post("{ \"Email\": \" new@example.com \", \"USERNAME\": \" Newcomer \", \"password\": \" pw \", \"extra\": 1 }", "global-key"));

            Assert.AreEqual(201, created.Status);
            Assert.AreEqual("{\"result\":\"created\",\"username\":\"Newcomer\",\"account_id\":41}", created.Json);
            Assert.AreEqual(("new@example.com", "Newcomer", " pw "), asked.Single(), "the names in any case, the e-mail and the name trimmed, the password as it came");

            ApiResponse With(AccountAnswer answer)
            {
                endpoint.Create = (email, userName, password) => answer;
                return api.Respond(Post(Json, "global-key"));
            }

            Assert.AreEqual(409, With(AccountAnswer.Of(CreateAccountResult.UsernameTaken, 0)).Status);
            Assert.AreEqual("{\"error\":\"username taken\"}", With(AccountAnswer.Of(CreateAccountResult.UsernameTaken, 0)).Json);
            Assert.AreEqual("{\"error\":\"email taken\"}", With(AccountAnswer.Of(CreateAccountResult.EmailTaken, 0)).Json);
            Assert.AreEqual(400, With(AccountAnswer.Of(CreateAccountResult.Invalid, 0)).Status);
            Assert.AreEqual(500, With(AccountAnswer.Of(CreateAccountResult.Failed, 0)).Status);
            Assert.AreEqual(503, With(AccountAnswer.NotConnected).Status);
            Assert.AreEqual(504, With(AccountAnswer.TimedOut).Status);
            StringAssert.Contains(With(AccountAnswer.TimedOut).Json, "may or may not");

            endpoint.Create = null;
            Assert.AreEqual(503, api.Respond(Post(Json, "global-key")).Status);
        }

        [TestMethod]
        public void TheEndpointRefusesABodyThatIsNotTheDetails()
        {
            var config = new RestApiConfig { ApiKey = "global-key" };

            config.Endpoints["addaccount"] = new ApiEndpointConfig();

            var api = Api(config, out var asked);

            (int Status, string Json) Answer(string body, string contentType = "application/json")
            {
                var request = Post(body, "global-key");

                if (contentType == null)
                    request.Headers.Remove("Content-Type");
                else
                    request.Headers["Content-Type"] = contentType;

                var response = api.Respond(request);

                return (response.Status, response.Json);
            }

            // Sent as something else: a form, text, nothing.
            Assert.AreEqual(415, Answer(Json, "application/x-www-form-urlencoded").Status);
            Assert.AreEqual(415, Answer(Json, "text/plain").Status);
            Assert.AreEqual(415, Answer(Json, null).Status);
            Assert.AreEqual(201, Answer(Json, "Application/JSON; charset=utf-8").Status);

            Assert.AreEqual((400, "{\"error\":\"the body is not JSON\"}"), Answer(""));
            Assert.AreEqual((400, "{\"error\":\"the body is not JSON\"}"), Answer("email=a@b.c&username=x"));
            Assert.AreEqual(400, Answer("[1, 2]").Status);
            Assert.AreEqual(400, Answer("\"text\"").Status);

            Assert.AreEqual((400, "{\"error\":\"username is required\"}"), Answer("{\"email\":\"a@b.c\",\"password\":\"p\"}"));
            Assert.AreEqual((400, "{\"error\":\"password is required\"}"), Answer("{\"email\":\"a@b.c\",\"username\":\"x\"}"));
            Assert.AreEqual((400, "{\"error\":\"email is required\"}"), Answer("{\"username\":\"x\",\"password\":\"p\"}"));
            Assert.AreEqual((400, "{\"error\":\"password must be text\"}"), Answer("{\"email\":\"a@b.c\",\"username\":\"x\",\"password\":12345}"));
            Assert.AreEqual((400, "{\"error\":\"username must be text\"}"), Answer("{\"email\":\"a@b.c\",\"username\":null,\"password\":\"p\"}"));
            StringAssert.Contains(Answer("{\"email\":\"a@b.c\",\"username\":\"FifteenLetters!\",\"password\":\"p\"}").Json, "longer than 14");
            StringAssert.Contains(Answer("{\"email\":\"nobody\",\"username\":\"x\",\"password\":\"p\"}").Json, "not an address");

            Assert.AreEqual(1, asked.Count, "only the one that was right reached Auth");
        }

        [TestMethod]
        public void TheEndpointTakesItsBodyOverHttp()
        {
            var config = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, ApiKey = "global-key" };

            config.Endpoints["addaccount"] = new ApiEndpointConfig();

            var api = Api(config, out var asked);

            try
            {
                var port = api.LocalEndPoint.Port;

                // As a client library sends it.
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port}/addaccount"))
                {
                    request.Headers.Add("X-API-Key", "global-key");
                    request.Content = new StringContent(Json, Encoding.UTF8, "application/json");

                    using var answered = Wait(http.SendAsync(request));

                    Assert.AreEqual(HttpStatusCode.Created, answered.StatusCode);
                    Assert.AreEqual("{\"result\":\"created\",\"username\":\"Newcomer\",\"account_id\":41}", Wait(answered.Content.ReadAsStringAsync()));
                }

                Assert.AreEqual(("new@example.com", "Newcomer", "secret-pw"), asked.Single());

                var head = "POST /addaccount HTTP/1.1\r\nHost: test\r\nX-API-Key: global-key\r\nContent-Type: application/json\r\n";

                // The body in a packet of its own, after the headers, and in two halves.
                StringAssert.StartsWith(
                    Exchange(port, head + $"Content-Length: {Json.Length}\r\n\r\n", Json.Substring(0, 20), Json.Substring(20)),
                    "HTTP/1.1 201 Created\r\n");

                // A client that asks before it sends the body is told to go on.
                var waited = Exchange(port, head + $"Content-Length: {Json.Length}\r\nExpect: 100-continue\r\n\r\n", Json);

                StringAssert.StartsWith(waited, "HTTP/1.1 100 Continue\r\n\r\nHTTP/1.1 201 Created\r\n");
                Assert.AreEqual(3, asked.Count);

                // Too much, sent in chunks, a length that is no number, no body at all.
                StringAssert.StartsWith(Exchange(port, head + $"Content-Length: {ApiServer.MaxBodyBytes + 1}\r\n\r\n"), "HTTP/1.1 413 ");
                StringAssert.StartsWith(Exchange(port, head + "Transfer-Encoding: chunked\r\n\r\n"), "HTTP/1.1 411 ");
                StringAssert.StartsWith(Exchange(port, head + "Content-Length: lots\r\n\r\n"), "HTTP/1.1 400 ");
                StringAssert.StartsWith(Exchange(port, head + "\r\n"), "HTTP/1.1 400 ");
                Assert.AreEqual(3, asked.Count);

                // The others still take no body and are not harmed by one.
                StringAssert.StartsWith(Exchange(port, "GET /addaccount HTTP/1.1\r\nX-API-Key: global-key\r\n\r\n"), "HTTP/1.1 405 Method Not Allowed\r\n");
                StringAssert.Contains(Exchange(port, "GET /addaccount HTTP/1.1\r\nX-API-Key: global-key\r\n\r\n"), "\r\nAllow: POST\r\n");
                StringAssert.StartsWith(Exchange(port, "GET /healthcheck HTTP/1.1\r\nX-API-Key: global-key\r\nContent-Length: 4\r\n\r\nbody"), "HTTP/1.1 200 OK\r\n");
            }
            finally
            {
                api.Stop();
            }
        }

        #endregion

        private static T Through<T>(T packet) where T : IOpcodedPacket<CommOpcode>, new()
        {
            using var stream = new MemoryStream();

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                packet.Write(writer);

            stream.Position = 0;

            using var reader = new BinaryReader(stream);

            Assert.AreEqual((byte)packet.Opcode, reader.ReadByte(), "the opcode first, as the link reads it");

            var read = new T();

            read.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position, "all of it read");

            return read;
        }

        /// <summary>The API with /addaccount answered by a stand-in for Auth that creates account 41 and notes what it was asked.</summary>
        private static ApiServer Api(RestApiConfig config, out List<(string Email, string UserName, string Password)> asked) => Api(config, out asked, out _);

        private static ApiServer Api(RestApiConfig config, out List<(string Email, string UserName, string Password)> asked, out AddAccountEndpoint endpoint)
        {
            var status = new ServerStatus { AuthLinked = () => true };
            var noted = new List<(string, string, string)>();
            var api = new ApiServer();

            status.Started();
            status.Ready();
            status.Beat(() => 1, 8, true);

            endpoint = new AddAccountEndpoint
            {
                Create = (email, userName, password) =>
                {
                    noted.Add((email, userName, password));
                    return AccountAnswer.Of(CreateAccountResult.Created, 41);
                }
            };

            api.Register(new HealthCheckEndpoint(status));
            api.Register(endpoint);
            api.Apply(config);

            asked = noted;

            return api;
        }

        private static ApiRequest Post(string body, string key)
        {
            var request = new ApiRequest { Method = "POST", Path = "/addaccount", Body = body, Remote = IPAddress.Loopback };

            request.Headers["Content-Type"] = "application/json";

            if (key != null)
                request.Headers["X-API-Key"] = key;

            return request;
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

        /// <summary>A Sqlite auth database of the test's own, migrated, with the repository Auth uses on it.</summary>
        private sealed class AuthDatabase : IDisposable
        {
            private readonly string _folder = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            private readonly RandomNumberService _random = new RandomNumberService();

            internal AuthContext Context { get; }
            internal AuthAccountRepository Accounts { get; }

            internal AuthDatabase()
            {
                Directory.CreateDirectory(_folder);

                Context = (AuthContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteAuthContext), Path.Combine(_folder, "database"));
                Context.Database.Migrate();
                Accounts = new AuthAccountRepository(Context, _random, new HashSettings());
            }

            public void Dispose()
            {
                Context.Dispose();
                _random.Dispose();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                try
                {
                    Directory.Delete(_folder, true);
                }
                catch (IOException)
                {
                }
            }
        }

        private sealed class HashSettings : IPasswordHashSettings
        {
            public string Pepper => "";
            public int Iterations => PasswordHasher.MinimumIterations;
        }

        /// <summary>A repository that fails where it is told to.</summary>
        private sealed class FakeAccounts : IAuthAccountRepository
        {
            private bool _created;

            internal Exception CreateThrows { get; set; }
            internal Exception FindThrows { get; set; }
            internal AuthAccountEntry FoundAfterCreate { get; set; }

            public void Create(string email, string userName, string password)
            {
                _created = true;

                if (CreateThrows != null)
                    throw CreateThrows;
            }

            public AuthAccountEntry FindByUserNameOrEmail(string userName, string email)
            {
                if (FindThrows != null)
                    throw FindThrows;

                return _created ? FoundAfterCreate : null;
            }

            public AuthAccountEntry GetByUserName(string name, string password) => throw new NotSupportedException();
            public void UpdateLoginData(uint id, IPAddress remoteAddress) => throw new NotSupportedException();
            public void UpdateLastServer(uint id, byte lastServerId) => throw new NotSupportedException();
            public AuthAccountEntry SetLocked(string userName, bool locked) => throw new NotSupportedException();
        }
    }
}
