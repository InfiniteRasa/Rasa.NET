using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The REST API's petitions (Api.PetitionsEndpoint, Api.PetitionActionEndpoint): the open
    /// ones or all of them, read with every column and the names their ids stand for; a game
    /// master's answer, which is the petition's resolution, and its close; and the player told
    /// of either at once when in the world, or when they next arrive in it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PetitionApiTests
    {
        private readonly List<Client> _registered = new();

        [TestCleanup]
        public void Unregister()
        {
            lock (Server.Clients)
                foreach (var client in _registered)
                    Server.Clients.Remove(client);

            _registered.Clear();
        }

        #region The orders

        [TestMethod]
        public void AnAnswerIsAPetitionAnAdminAndAResponseAndACloseAPetitionAndAnAdminId()
        {
            var respond = new PetitionActionEndpoint(close: false);
            var answer = respond.Read(Post("{\"petitionId\":12,\"admin\":\" Atomsk \",\"response\":\" Fixed. \"}"), out var problem);

            Assert.IsNull(problem);
            Assert.AreEqual(12u, answer.PetitionId);
            Assert.AreEqual("Atomsk", answer.Admin);
            Assert.AreEqual("Fixed.", answer.Response);

            Assert.AreEqual(new string('r', 255), respond.Read(Post("{\"petitionId\":\"7\",\"admin\":\"a\",\"response\":\"" + new string('r', 255) + "\"}"), out problem).Response,
                "255 characters, and a number in a string");
            Assert.IsNull(problem);

            var close = new PetitionActionEndpoint(close: true);
            var order = close.Read(Post("{\"petitionId\":12,\"adminId\":\"Atomsk\"}"), out problem);

            Assert.IsNull(problem);
            Assert.AreEqual(12u, order.PetitionId);
            Assert.AreEqual("Atomsk", order.Admin);
            Assert.IsNull(order.Response, "a close takes no answer");
            Assert.AreEqual("42", close.Read(Post("{\"petitionId\":12,\"adminId\":42}"), out _).Admin, "an admin id may be a number");
        }

        [TestMethod]
        public void AnOrderThatWillNotDoIsAnsweredWithWhyAndNothingDone()
        {
            var respond = new Dictionary<string, string>
            {
                ["{\"admin\":\"a\",\"response\":\"r\"}"] = "petitionId must be a whole number above 0",
                ["{\"petitionId\":0,\"admin\":\"a\",\"response\":\"r\"}"] = "petitionId must be a whole number above 0",
                ["{\"petitionId\":-1,\"admin\":\"a\",\"response\":\"r\"}"] = "petitionId must be a whole number above 0",
                ["{\"petitionId\":\"x\",\"admin\":\"a\",\"response\":\"r\"}"] = "petitionId must be a whole number above 0",
                ["{\"petitionId\":1,\"response\":\"r\"}"] = "admin is required",
                ["{\"petitionId\":1,\"admin\":\" \",\"response\":\"r\"}"] = "admin is required",
                ["{\"petitionId\":1,\"admin\":\"" + new string('a', 65) + "\",\"response\":\"r\"}"] = "admin is longer than 64 characters",
                ["{\"petitionId\":1,\"admin\":\"a\"}"] = "response is required",
                ["{\"petitionId\":1,\"admin\":\"a\",\"response\":\"  \"}"] = "response is required",
                ["{\"petitionId\":1,\"admin\":\"a\",\"response\":5}"] = "response is required",
                ["{\"petitionId\":1,\"admin\":\"a\",\"response\":\"" + new string('r', 256) + "\"}"] = "response is longer than 255 characters",
                ["[1]"] = "the body must be a JSON object",
                ["{\"petitionId\":"] = "the body is not JSON"
            };

            var close = new Dictionary<string, string>
            {
                ["{\"adminId\":\"a\"}"] = "petitionId must be a whole number above 0",
                ["{\"petitionId\":1}"] = "adminId is required",
                ["{\"petitionId\":1,\"admin\":\"a\"}"] = "adminId is required",
                ["{\"petitionId\":1,\"adminId\":true}"] = "adminId is required"
            };

            var done = 0;

            foreach (var (endpoint, cases, verb) in new[] { (new PetitionActionEndpoint(false), respond, "responded"), (new PetitionActionEndpoint(true), close, "closed") })
            {
                endpoint.Perform = _ => { done++; return new PetitionResult(PetitionAnswer.Done); };

                foreach (var (body, error) in cases)
                {
                    var answer = endpoint.Handle(Post(body));

                    Assert.AreEqual(200, answer.Status, body);
                    Assert.AreEqual(Refusal(verb, error), answer.Json, body);
                }

                var form = Post("petitionId=1");
                form.Headers["Content-Type"] = "text/plain";
                Assert.AreEqual(Refusal(verb, "content type must be application/json"), endpoint.Handle(form).Json);
            }

            Assert.AreEqual(0, done);
        }

        [TestMethod]
        public void TheAnswerSaysWhatCameOfIt()
        {
            var respond = new PetitionActionEndpoint(close: false);
            const string body = "{\"petitionId\":12,\"admin\":\"Atomsk\",\"response\":\"Fixed.\"}";

            Assert.AreEqual(Refusal("responded", "the game server is not ready"), respond.Handle(Post(body)).Json);

            PetitionOrder seen = null;
            respond.Perform = order => { seen = order; return new PetitionResult(PetitionAnswer.Done, PetitionStatus.Open, told: false); };

            Assert.AreEqual("{\"responded\":true,\"petitionId\":12,\"status\":\"open\",\"playerTold\":false}", respond.Handle(Post(body)).Json);
            Assert.AreEqual("127.0.0.1", seen.From);

            respond.Perform = _ => null;
            Assert.AreEqual(Refusal("responded", "the game server did not answer in time"), respond.Handle(Post(body)).Json);

            var close = new PetitionActionEndpoint(close: true);
            const string closing = "{\"petitionId\":12,\"adminId\":\"Atomsk\"}";

            close.Perform = _ => new PetitionResult(PetitionAnswer.Done, PetitionStatus.Resolved, told: true);
            Assert.AreEqual("{\"closed\":true,\"petitionId\":12,\"status\":\"resolved\",\"playerTold\":true}", close.Handle(Post(closing)).Json);

            close.Perform = _ => new PetitionResult(PetitionAnswer.NoSuchPetition);
            Assert.AreEqual(Refusal("closed", "no such petition"), close.Handle(Post(closing)).Json);

            close.Perform = _ => new PetitionResult(PetitionAnswer.NotOpen, PetitionStatus.Cancelled);
            Assert.AreEqual(Refusal("closed", "the petition is cancelled, not open"), close.Handle(Post(closing)).Json);

            close.Perform = _ => new PetitionResult(PetitionAnswer.NotSaved);
            Assert.AreEqual(Refusal("closed", "the petition could not be saved"), close.Handle(Post(closing)).Json);
        }

        #endregion

        #region In game

        [TestMethod]
        public void AnAnswerIsThePetitionsResolutionAndThePlayerInTheWorldIsToldAtOnce()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var player = Online(context.Client);

            WithPetitions(context, petitions =>
            {
                var id = File(context, accountId: 1, characterId: 1);
                context.Drain();

                var result = petitions.Respond(id, "  The quest is fixed; try it again.  ");

                Assert.AreEqual(PetitionAnswer.Done, result.Answer);
                Assert.AreEqual(PetitionStatus.Open, result.Status, "an answer leaves it open");
                Assert.IsTrue(result.Told);

                var petition = petitions.Get(id);
                Assert.AreEqual("The quest is fixed; try it again.", petition.Resolution);
                Assert.AreEqual((byte)PetitionStatus.Open, petition.Status);
                Assert.IsFalse(petition.NoticePending);
                // In yellow, and as an AFS Message they can open later (GmMessages).
                var told = MissionTestContext.Drain(player);
                var notice = $"A game master has answered your petition #{id}: The quest is fixed; try it again.";
                Assert.AreEqual(notice, told.OfType<AdminMessagePacket>().Single().Message);
                Assert.AreEqual((uint)MsgFilterId.GameMaster, told.OfType<AdminMessagePacket>().Single().FilterId);
                Assert.AreEqual(notice, told.OfType<PreviewMOTDPacket>().Single().Motd);

                // A second answer takes the place of the first.
                petitions.Respond(id, "Second thoughts.");
                Assert.AreEqual("Second thoughts.", petitions.Get(id).Resolution);
                context.Drain();

                // Closed, with the answer it has.
                result = petitions.Close(id);

                Assert.AreEqual(PetitionAnswer.Done, result.Answer);
                Assert.AreEqual(PetitionStatus.Resolved, result.Status);
                Assert.AreEqual("Second thoughts.", petitions.Get(id).Resolution);
                Assert.AreEqual((byte)PetitionStatus.Resolved, petitions.Get(id).Status);
                Assert.AreEqual($"Your petition #{id} has been resolved: Second thoughts.", Told(player).Single());

                // Not open any more: neither answered nor closed again.
                Assert.AreEqual(PetitionAnswer.NotOpen, petitions.Respond(id, "Again.").Answer);
                Assert.AreEqual(PetitionStatus.Resolved, petitions.Close(id).Status);
                Assert.AreEqual("Second thoughts.", petitions.Get(id).Resolution);
                Assert.AreEqual(PetitionAnswer.NoSuchPetition, petitions.Close(id + 100).Answer);
                Assert.IsEmpty(Told(player));
            });
        }

        [TestMethod]
        public void APlayerAwayIsToldOnceWhenTheyNextArrive()
        {
            using var context = MissionTestContext.WithCompletableMission(429);

            WithPetitions(context, petitions =>
            {
                var answered = File(context, accountId: 1, characterId: 1);
                var closed = File(context, accountId: 1, characterId: 1);
                var bare = File(context, accountId: 1, characterId: 1);
                var withdrawn = File(context, accountId: 1, characterId: 1);

                // Not in the world: nobody to tell yet.
                Assert.IsFalse(petitions.Respond(answered, "Look again.").Told);
                petitions.Respond(closed, "Done.");
                Assert.IsFalse(petitions.Close(closed).Told);
                petitions.Close(bare);

                using (var unit = context.CreateChar())
                {
                    unit.Petitions.SetPetitionStatus(withdrawn, (byte)PetitionStatus.Cancelled, "");
                    unit.Petitions.SetNoticePending(withdrawn, true);
                }

                Assert.IsTrue(petitions.Get(answered).NoticePending);
                Assert.IsTrue(petitions.Get(closed).NoticePending);
                context.Drain();

                // Arriving in the world: once each, oldest first, as each petition is now.
                CommunicatorManager.Instance.LoginOk(context.Client);

                CollectionAssert.AreEqual(new[]
                {
                    $"A game master has answered your petition #{answered}: Look again.",
                    $"Your petition #{closed} has been resolved: Done.",
                    $"Your petition #{bare} has been resolved."
                }, Told(context.Client));

                foreach (var id in new[] { answered, closed, bare, withdrawn })
                    Assert.IsFalse(petitions.Get(id).NoticePending, $"#{id}");

                CommunicatorManager.Instance.LoginOk(context.Client);
                Assert.IsEmpty(Told(context.Client), "and not again");
            });
        }

        [TestMethod]
        public void TheConsolesResolveTellsThePlayerTooAndKeepsTheAnswerWhenGivenNone()
        {
            using var context = MissionTestContext.WithCompletableMission(429);

            WithPetitions(context, petitions =>
            {
                var id = File(context, accountId: 1, characterId: 1);
                petitions.Respond(id, "Answered.");

                Assert.IsTrue(petitions.Resolve(id, ""));
                Assert.AreEqual("Answered.", petitions.Get(id).Resolution);
                Assert.IsTrue(petitions.Get(id).NoticePending);

                var other = File(context, accountId: 1, characterId: 1);
                Online(context.Client);
                context.Drain();

                Assert.IsTrue(petitions.Resolve(other, "Reset your quest."));
                Assert.AreEqual("Reset your quest.", petitions.Get(other).Resolution);
                Assert.AreEqual($"Your petition #{other} has been resolved: Reset your quest.", Told(context.Client).Single());
                Assert.IsFalse(petitions.Resolve(other, "again"));
            });
        }

        #endregion

        #region The lists

        [TestMethod]
        public void TheListsHaveEveryColumnAndTheNamesNewestFirst()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            context.SeedCharacter(2, 0, 20);

            WithPetitions(context, petitions =>
            {
                var first = File(context, accountId: 1, characterId: 1);
                var second = File(context, accountId: 2, characterId: 20, type: PetitionType.BugReport);
                var gone = File(context, accountId: 9, characterId: 99);
                petitions.Respond(first, "Answered.");
                petitions.Close(first);

                var all = petitions.ListRecords(null);

                CollectionAssert.AreEqual(new[] { gone, second, first }, all.Select(record => record.Entry.Id).ToArray());
                CollectionAssert.AreEqual(new[] { gone, second }, petitions.ListRecords(PetitionStatus.Open).Select(record => record.Entry.Id).ToArray());

                using var json = JsonDocument.Parse(PetitionsEndpoint.Json(all));
                var root = json.RootElement;

                Assert.AreEqual(3, root.GetProperty("count").GetInt32());

                var bug = root.GetProperty("petitions")[1];
                Assert.AreEqual(second, bug.GetProperty("id").GetUInt32());
                Assert.AreEqual("bugreport", bug.GetProperty("type").GetString());
                Assert.AreEqual("open", bug.GetProperty("status").GetString());
                Assert.AreEqual(2u, bug.GetProperty("accountId").GetUInt32());
                Assert.AreEqual("Account 2", bug.GetProperty("accountName").GetString());
                Assert.AreEqual("Family 2", bug.GetProperty("familyName").GetString());
                Assert.AreEqual(20u, bug.GetProperty("characterId").GetUInt32());
                Assert.AreEqual("Character 20", bug.GetProperty("characterName").GetString());
                Assert.AreEqual("Stuck", bug.GetProperty("summary").GetString());
                Assert.AreEqual("Under the bridge.", bug.GetProperty("body").GetString());
                Assert.AreEqual(1220u, bug.GetProperty("mapContextId").GetUInt32());
                Assert.AreEqual(1.5, bug.GetProperty("position").GetProperty("x").GetDouble());
                Assert.AreEqual(-3.25, bug.GetProperty("position").GetProperty("z").GetDouble());
                Assert.AreEqual("", bug.GetProperty("resolution").GetString());
                Assert.IsFalse(bug.GetProperty("noticePending").GetBoolean());
                StringAssert.EndsWith(bug.GetProperty("createdAt").GetString(), "Z");

                var resolved = root.GetProperty("petitions")[2];
                Assert.AreEqual("resolved", resolved.GetProperty("status").GetString());
                Assert.AreEqual("Answered.", resolved.GetProperty("resolution").GetString());
                Assert.IsTrue(resolved.GetProperty("noticePending").GetBoolean(), "the player was away");

                var orphan = root.GetProperty("petitions")[0];
                Assert.AreEqual(JsonValueKind.Null, orphan.GetProperty("characterName").ValueKind, "a character no longer there");
                Assert.AreEqual(JsonValueKind.Null, orphan.GetProperty("familyName").ValueKind);

                // Through the endpoints, by the store Server gives them.
                var open = new PetitionsEndpoint(openOnly: true);
                Assert.AreEqual(503, open.Handle(new ApiRequest()).Status);

                open.Read = petitions.ListRecords;
                Assert.AreEqual(PetitionsEndpoint.Json(petitions.ListRecords(PetitionStatus.Open)), open.Handle(new ApiRequest()).Json);

                var everything = new PetitionsEndpoint(openOnly: false) { Read = petitions.ListRecords };
                Assert.AreEqual(PetitionsEndpoint.Json(all), everything.Handle(new ApiRequest()).Json);
            });
        }

        [TestMethod]
        public void TheFourAreOffUntilTheirOwnEntriesTurnThemOnAndNeverPublicByTheApiBeingSo()
        {
            const string Key = "petition-key";
            var config = new Rasa.Config.RestApiConfig { Public = true, ApiKey = Key };
            var host = new ApiHost(new ServerStatus());
            var api = host.Rest;

            host.OpenPetitions.Read = _ => new List<PetitionRecord>();
            host.AllPetitions.Read = _ => new List<PetitionRecord>();
            host.RespondPetition.Perform = _ => new PetitionResult(PetitionAnswer.NoSuchPetition);
            host.ClosePetition.Perform = _ => new PetitionResult(PetitionAnswer.NoSuchPetition);
            api.Apply(config);

            foreach (var (name, method) in new[] { ("getopenpetitions", "GET"), ("getallpetitions", "GET"), ("respondpetition", "POST"), ("closepetition", "POST") })
            {
                var request = method == "GET"
                    ? new ApiRequest { Path = "/" + name, Remote = System.Net.IPAddress.Loopback }
                    : Post("{\"petitionId\":1,\"admin\":\"a\",\"adminId\":\"a\",\"response\":\"r\"}");

                request.Path = "/" + name;
                request.Headers["X-API-Key"] = Key;

                CollectionAssert.Contains(api.Endpoints.ToList(), name);
                Assert.AreEqual(404, api.Respond(request).Status, $"{name}: no entry, off");

                config.Endpoints[name] = new Rasa.Config.ApiEndpointConfig { Enabled = false };
                Assert.AreEqual(404, api.Respond(request).Status, $"{name}: the shipped entry, off");

                config.Endpoints[name] = new Rasa.Config.ApiEndpointConfig();
                Assert.AreEqual(200, api.Respond(request).Status, name);

                request.Headers.Remove("X-API-Key");
                Assert.AreEqual(401, api.Respond(request).Status, $"{name}: the API being public is not this one being public");

                var wrong = new ApiRequest { Method = method == "GET" ? "POST" : "GET", Path = "/" + name, Remote = System.Net.IPAddress.Loopback };
                wrong.Headers["X-API-Key"] = Key;
                wrong.Headers["Content-Type"] = "application/json";
                wrong.Body = "{}";
                Assert.AreEqual(405, api.Respond(wrong).Status, $"{name}: {method} only");
            }
        }

        #endregion

        private Client Online(Client client)
        {
            lock (Server.Clients)
                Server.Clients.Add(client);

            _registered.Add(client);
            return client;
        }

        private static uint File(MissionTestContext context, uint accountId, uint characterId, PetitionType type = PetitionType.HelpRequest)
        {
            using var unit = context.CreateChar();

            return unit.Petitions.AddPetition(new PetitionEntry(accountId, characterId, (byte)type, "Stuck", "Under the bridge.", 1220, 1.5, 2, -3.25));
        }

        /// <summary>The yellow ADMIN MESSAGE lines a petition's notices go out as (GmMessages).</summary>
        private static List<string> Told(Client client) =>
            MissionTestContext.Drain(client).OfType<AdminMessagePacket>().Select(message => message.Message).ToList();

        /// <summary>A petition manager over the context's database, in place of the instance.</summary>
        private static void WithPetitions(MissionTestContext context, Action<PetitionManager> body)
        {
            var instance = typeof(PetitionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);

            try
            {
                var petitions = (PetitionManager)typeof(PetitionManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                instance.SetValue(null, petitions);
                body(petitions);
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }

        private static ApiRequest Post(string body)
        {
            var request = new ApiRequest { Method = "POST", Path = "/respondpetition", Body = body, Remote = System.Net.IPAddress.Loopback };
            request.Headers["Content-Type"] = "application/json";
            return request;
        }

        private static string Refusal(string verb, string error) => $"{{\"{verb}\":false,\"error\":\"{error}\"}}";
    }
}
