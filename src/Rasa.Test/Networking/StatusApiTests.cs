extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Config;

    /// <summary>
    /// The status listeners (Rasa.Api): what the server status says and when it calls the server
    /// healthy, who the REST API answers and with which key, over HTTP and over TLS, the status
    /// port, the allow list, and the settings as they are read from the file.
    /// </summary>
    [TestClass]
    public class StatusApiTests
    {
        private const string HealthyBoth = "{\"game_server_status\":\"healthy\",\"app_server_status\":\"healthy\"}";

        #region The allow list

        [TestMethod]
        public void AnAllowListWithNothingOnItAllowsEveryAddress()
        {
            foreach (var configured in new[] { null, new string[0], new[] { "" }, new[] { "  ", null, "\t" } })
            {
                var list = IpAllowList.Parse(configured);

                Assert.IsTrue(list.AllowsAll);
                Assert.AreEqual(0, list.Count);
                Assert.AreEqual(0, list.Invalid.Count);
                Assert.IsTrue(list.Allows(IPAddress.Parse("198.51.100.20")));
                Assert.IsTrue(list.Allows(IPAddress.Parse("2001:db8::1")));
                Assert.IsTrue(list.Allows(null), "an address that could not be read, too");
            }

            Assert.IsTrue(IpAllowList.Open.AllowsAll);
        }

        [TestMethod]
        public void AnAllowListAllowsItsAddressesAndRangesAndNoOthers()
        {
            var list = IpAllowList.Parse(new[] { "203.0.113.7", "10.0.0.0/8", "192.168.1.128/25", "2001:db8::/32", "::1" });

            Assert.IsFalse(list.AllowsAll);
            Assert.AreEqual(5, list.Count);

            foreach (var allowed in new[] { "203.0.113.7", "10.0.0.0", "10.255.255.255", "192.168.1.128", "192.168.1.255", "2001:db8:ffff::9", "::1", "::ffff:10.1.2.3" })
                Assert.IsTrue(list.Allows(IPAddress.Parse(allowed)), allowed);

            foreach (var refused in new[] { "203.0.113.8", "11.0.0.0", "192.168.1.127", "192.168.2.200", "2001:db9::1", "::2", "127.0.0.1", "::ffff:11.1.2.3" })
                Assert.IsFalse(list.Allows(IPAddress.Parse(refused)), refused);

            Assert.IsFalse(list.Allows(null));

            // Several to an entry, and a range that is everything of its family.
            var loose = IpAllowList.Parse(new[] { "127.0.0.1, 127.0.0.2;127.0.0.3 127.0.0.4", "172.16.0.0/0" });

            Assert.AreEqual(5, loose.Count);
            Assert.IsTrue(loose.Allows(IPAddress.Parse("127.0.0.3")));
            Assert.IsTrue(loose.Allows(IPAddress.Parse("8.8.8.8")));
            Assert.IsFalse(loose.Allows(IPAddress.Parse("::1")), "of its family");

            // A mapped address is the IPv4 address, and its range counts in IPv4's bits.
            var mapped = IpAllowList.Parse(new[] { "::ffff:10.0.0.0/104" });

            Assert.AreEqual(0, mapped.Invalid.Count);
            Assert.IsTrue(mapped.Allows(IPAddress.Parse("10.200.0.1")));
            Assert.IsFalse(mapped.Allows(IPAddress.Parse("11.0.0.1")));
        }

        [TestMethod]
        public void AnEntryThatIsNoAddressAllowsNobodyAndDoesNotOpenTheList()
        {
            var list = IpAllowList.Parse(new[] { "office", "10.0.0.0/33", "10.0.0.1/x", "300.1.1.1", "::ffff:10.0.0.0/8" });

            Assert.IsFalse(list.AllowsAll, "a list of mistakes is not an empty list");
            Assert.AreEqual(0, list.Count);
            CollectionAssert.AreEqual(new[] { "office", "10.0.0.0/33", "10.0.0.1/x", "300.1.1.1", "::ffff:10.0.0.0/8" }, list.Invalid.ToArray());
            Assert.IsFalse(list.Allows(IPAddress.Parse("10.0.0.1")));
            Assert.IsFalse(list.Allows(IPAddress.Loopback));

            var mixed = IpAllowList.Parse(new[] { "office", "127.0.0.1" });

            Assert.IsTrue(mixed.Allows(IPAddress.Loopback));
            Assert.IsFalse(mixed.Allows(IPAddress.Parse("10.0.0.1")));
            Assert.AreEqual(1, mixed.Invalid.Count);
        }

        #endregion

        #region The status

        [TestMethod]
        public void TheStatusCountsUptimeAndThePlayersItIsToldOfOnceASecond()
        {
            long now = 50_000;
            var status = new ServerStatus { Now = () => now };
            var players = 3;
            var asked = 0;

            int Players()
            {
                asked++;
                return players;
            }

            Assert.AreEqual("{\"uptimeseconds\":0,\"currentconnections\":0,\"peakconnections\":0,\"maxconnections\":0}", status.StatusJson());

            status.Started();
            status.Beat(Players, 1024, true);

            Assert.AreEqual(3, status.CurrentConnections);
            Assert.AreEqual(3, status.PeakConnections);
            Assert.AreEqual(1024, status.MaxConnections);

            // Every tick is a beat; the count is read once a second.
            players = 9;
            now += 100;
            status.Beat(Players, 1024, true);
            Assert.AreEqual(3, status.CurrentConnections);
            Assert.AreEqual(1, asked);

            now += 900;
            status.Beat(Players, 1024, true);
            Assert.AreEqual(9, status.CurrentConnections);
            Assert.AreEqual(9, status.PeakConnections);

            // The peak stays; the limit is whatever the settings say now.
            players = 2;
            now += 1000;
            status.Beat(Players, 500, true);
            Assert.AreEqual(2, status.CurrentConnections);
            Assert.AreEqual(9, status.PeakConnections);
            Assert.AreEqual(500, status.MaxConnections);

            // Twelve and a half hours after the ports opened.
            now = 50_000 + 45_000_000;
            Assert.AreEqual(45000L, status.UptimeSeconds);
            Assert.AreEqual("{\"uptimeseconds\":45000,\"currentconnections\":2,\"peakconnections\":9,\"maxconnections\":500}", status.StatusJson());

            // Started is the first time it is said.
            status.Started();
            Assert.AreEqual(45000L, status.UptimeSeconds);

            now += 1999;
            Assert.AreEqual(45001L, status.UptimeSeconds, "whole seconds, the one begun not counted");
        }

        [TestMethod]
        public void TheGameServerIsHealthyWhileItIsReadyListeningAndItsLoopTicks()
        {
            long now = 1000;
            var linked = true;
            var status = new ServerStatus { Now = () => now, AuthLinked = () => linked, StallMs = 15000 };

            Assert.IsFalse(status.GameHealthy, "nothing has started");

            status.Started();
            status.Beat(() => 0, 10, true);
            Assert.IsFalse(status.GameHealthy, "still loading");

            status.Ready();
            Assert.IsTrue(status.GameHealthy);
            Assert.AreEqual(HealthyBoth, status.HealthJson());

            // The loop is late, and then too late.
            now += 15000;
            Assert.IsTrue(status.GameHealthy);
            now += 1;
            Assert.IsFalse(status.GameHealthy);
            Assert.AreEqual("{\"game_server_status\":\"unhealthy\",\"app_server_status\":\"healthy\"}", status.HealthJson());

            status.Beat(() => 0, 10, true);
            Assert.IsTrue(status.GameHealthy, "it ticked again");

            // The player port has gone.
            now += 100;
            status.Beat(() => 0, 10, false);
            Assert.IsFalse(status.GameHealthy);
            status.Beat(() => 0, 10, true);
            Assert.IsTrue(status.GameHealthy);

            // Shut down: neither is healthy, whatever the link says.
            status.Stopped();
            Assert.IsFalse(status.GameHealthy);
            Assert.IsFalse(status.AppHealthy);

            status.StallMs = 10;
            Assert.AreEqual(1000, status.StallMs, "no less than a second");
        }

        [TestMethod]
        public void TheAppServerIsHealthyWhileTheAuthLinkIsUp()
        {
            var linked = false;
            var status = new ServerStatus { AuthLinked = () => linked };

            status.Started();
            Assert.IsFalse(new ServerStatus().AppHealthy, "nothing says it is");
            Assert.IsFalse(status.AppHealthy);

            linked = true;
            Assert.IsTrue(status.AppHealthy);
            Assert.AreEqual("{\"game_server_status\":\"unhealthy\",\"app_server_status\":\"healthy\"}", status.HealthJson(), "each on its own");

            status.AuthLinked = () => throw new ObjectDisposedException("socket");
            Assert.IsFalse(status.AppHealthy, "a link that cannot be asked is not up");

            status.AuthLinked = null;
            Assert.IsFalse(status.AppHealthy);
        }

        [TestMethod]
        public void TheFullStatusIsBothInOneObject()
        {
            Assert.AreEqual(
                HealthyBoth.TrimEnd('}') + ",\"uptimeseconds\":3600,\"currentconnections\":4,\"peakconnections\":4,\"maxconnections\":64}",
                Healthy(4, 64, 3_600_000).FullJson());
        }

        #endregion

        #region Who the REST API answers

        [TestMethod]
        public void APublicApiAnswersWithoutAKey()
        {
            var api = Api(new RestApiConfig { Public = true });

            var health = api.Respond(Get("/healthcheck"));
            Assert.AreEqual(200, health.Status);
            Assert.AreEqual(HealthyBoth, health.Json);

            var status = api.Respond(Get("/serverstatus"));
            Assert.AreEqual(200, status.Status);
            Assert.AreEqual("{\"uptimeseconds\":0,\"currentconnections\":4,\"peakconnections\":4,\"maxconnections\":64}", status.Json);

            // Any case, a slash at the end, a query, a key nobody asked for.
            Assert.AreEqual(200, api.Respond(Get("/HealthCheck/")).Status);
            Assert.AreEqual(200, api.Respond(new ApiRequest { Path = "/serverstatus", Query = "pretty=1", Remote = IPAddress.Loopback }).Status);
            Assert.AreEqual(200, api.Respond(Get("/serverstatus", "whatever")).Status);
            Assert.AreEqual(200, api.Respond(new ApiRequest { Method = "HEAD", Path = "/healthcheck", Remote = IPAddress.Loopback }).Status);
        }

        [TestMethod]
        public void AnApiThatIsNotPublicWantsTheEndpointsKeyOrTheGlobalOne()
        {
            var config = new RestApiConfig { Public = false, ApiKey = "global-key" };

            config.Endpoints["healthcheck"] = new ApiEndpointConfig { ApiKey = "health-key" };
            config.Endpoints["serverstatus"] = new ApiEndpointConfig { ApiKey = "status-key" };

            var api = Api(config);

            Assert.AreEqual(401, api.Respond(Get("/healthcheck")).Status);
            Assert.AreEqual("{\"error\":\"unauthorized\"}", api.Respond(Get("/healthcheck")).Json);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "nonsense")).Status);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "health-ke")).Status);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "HEALTH-KEY")).Status, "to the letter");

            Assert.AreEqual(200, api.Respond(Get("/healthcheck", "health-key")).Status);
            Assert.AreEqual(200, api.Respond(Get("/serverstatus", "status-key")).Status);

            // Each endpoint's key is its own; the global one opens them all.
            Assert.AreEqual(401, api.Respond(Get("/serverstatus", "health-key")).Status);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "status-key")).Status);
            Assert.AreEqual(200, api.Respond(Get("/healthcheck", "global-key")).Status);
            Assert.AreEqual(200, api.Respond(Get("/serverstatus", "global-key")).Status);

            // The key as a bearer token.
            var bearer = Get("/serverstatus");
            bearer.Headers["authorization"] = "Bearer status-key";
            Assert.AreEqual(200, api.Respond(bearer).Status);

            bearer.Headers["authorization"] = "Basic status-key";
            Assert.AreEqual(401, api.Respond(bearer).Status);

            // No global key: only the endpoint's own.
            config.ApiKey = "";
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "global-key")).Status);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "")).Status);
            Assert.AreEqual(200, api.Respond(Get("/healthcheck", "health-key")).Status);
        }

        [TestMethod]
        public void WithNoKeySetAnEndpointThatIsNotPublicAnswersNobody()
        {
            var api = Api(new RestApiConfig { Public = false });

            Assert.AreEqual(401, api.Respond(Get("/healthcheck")).Status);
            Assert.AreEqual(401, api.Respond(Get("/healthcheck", "")).Status, "an empty key is no key");
            Assert.AreEqual(401, api.Respond(Get("/serverstatus", " ")).Status);
        }

        [TestMethod]
        public void AnEndpointCanBePublicOrNotOnItsOwnAndCanBeSwitchedOff()
        {
            var config = new RestApiConfig { Public = false, ApiKey = "global-key" };

            config.Endpoints["HealthCheck"] = new ApiEndpointConfig { Public = true };
            config.Endpoints["serverstatus"] = new ApiEndpointConfig();

            var api = Api(config);

            Assert.AreEqual(200, api.Respond(Get("/healthcheck")).Status, "its own Public, whatever the name's case in the file");
            Assert.AreEqual(401, api.Respond(Get("/serverstatus")).Status, "none of its own: the API's");

            // The other way about.
            config.Public = true;
            config.Endpoints["serverstatus"].Public = false;

            Assert.AreEqual(401, api.Respond(Get("/serverstatus")).Status);
            Assert.AreEqual(200, api.Respond(Get("/serverstatus", "global-key")).Status);

            // Switched off, it is not there, key or no key.
            config.Endpoints["serverstatus"].Enabled = false;

            var gone = api.Respond(Get("/serverstatus", "global-key"));
            Assert.AreEqual(404, gone.Status);
            Assert.AreEqual("{\"error\":\"not found\"}", gone.Json);
        }

        [TestMethod]
        public void TheApiRefusesOtherPathsMethodsAndAddresses()
        {
            var config = new RestApiConfig { Public = true, AllowedIps = new List<string> { "127.0.0.1", "10.0.0.0/8" } };
            var api = Api(config);

            Assert.AreEqual(404, api.Respond(Get("/")).Status);
            Assert.AreEqual(404, api.Respond(Get("/players")).Status);
            Assert.AreEqual(404, api.Respond(Get("/healthcheck/extra")).Status);

            foreach (var method in new[] { "POST", "PUT", "DELETE", "OPTIONS" })
            {
                var refused = api.Respond(new ApiRequest { Method = method, Path = "/healthcheck", Remote = IPAddress.Loopback });

                Assert.AreEqual(405, refused.Status, method);
                Assert.AreEqual("GET, HEAD", refused.Allow);
            }

            var outside = Get("/healthcheck");
            outside.Remote = IPAddress.Parse("198.51.100.20");

            var forbidden = api.Respond(outside);
            Assert.AreEqual(403, forbidden.Status);
            Assert.AreEqual("{\"error\":\"forbidden\"}", forbidden.Json);

            outside.Remote = IPAddress.Parse("10.4.4.4");
            Assert.AreEqual(200, api.Respond(outside).Status);

            // The list taken out again, with the next settings.
            config.AllowedIps.Clear();
            api.Apply(config);
            outside.Remote = IPAddress.Parse("198.51.100.20");
            Assert.AreEqual(200, api.Respond(outside).Status);
        }

        [TestMethod]
        public void TheHealthCheckIsA503WithTheSameBodyWhenEitherIsUnhealthy()
        {
            long now = 1000;
            var linked = true;
            var status = new ServerStatus { Now = () => now, AuthLinked = () => linked };
            var api = new ApiServer();

            api.Register(new HealthCheckEndpoint(status));
            api.Apply(new RestApiConfig { Public = true });

            status.Started();
            status.Ready();
            status.Beat(() => 1, 8, true);
            Assert.AreEqual(200, api.Respond(Get("/healthcheck")).Status);

            linked = false;

            var down = api.Respond(Get("/healthcheck"));
            Assert.AreEqual(503, down.Status);
            Assert.AreEqual("{\"game_server_status\":\"healthy\",\"app_server_status\":\"unhealthy\"}", down.Json);

            linked = true;
            now += 60000;

            var stalled = api.Respond(Get("/healthcheck"));
            Assert.AreEqual(503, stalled.Status);
            Assert.AreEqual("{\"game_server_status\":\"unhealthy\",\"app_server_status\":\"healthy\"}", stalled.Json);
        }

        [TestMethod]
        public void AnEndpointOfOnesOwnIsAnsweredLikeTheOthers()
        {
            var config = new RestApiConfig { Public = false, ApiKey = "global-key" };

            config.Endpoints["motd"] = new ApiEndpointConfig { ApiKey = "motd-key" };

            var api = Api(config);

            api.Register(new Fixed("Motd", "{\"text\":\"hello\"}"));
            api.Register(new Broken());

            CollectionAssert.AreEqual(new[] { "broken", "healthcheck", "motd", "serverstatus" }, api.Endpoints.ToArray());

            Assert.AreEqual(401, api.Respond(Get("/motd")).Status);
            Assert.AreEqual("{\"text\":\"hello\"}", api.Respond(Get("/motd", "motd-key")).Json);
            Assert.AreEqual(200, api.Respond(Get("/motd", "global-key")).Status);

            // One that throws is that request's 500 and nothing more.
            var broken = api.Respond(Get("/broken", "global-key"));
            Assert.AreEqual(500, broken.Status);
            Assert.AreEqual("{\"error\":\"internal error\"}", broken.Json);
            Assert.AreEqual(200, api.Respond(Get("/motd", "global-key")).Status);

            Assert.ThrowsExactly<ArgumentException>(() => api.Register(new Fixed(" ", "{}")));
        }

        #endregion

        #region HTTP

        [TestMethod]
        public void ARequestIsReadFromItsLineAndHeaders()
        {
            var request = ApiServer.Parse("get /ServerStatus/?a=1&b=2 HTTP/1.1\r\nHost: example\r\nx-api-key:  secret \r\nAccept: */*\r\n\r\nignored");

            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("/ServerStatus/", request.Path);
            Assert.AreEqual("a=1&b=2", request.Query);
            Assert.AreEqual("serverstatus", request.EndpointName);
            Assert.AreEqual("secret", request.ApiKey);
            Assert.AreEqual("example", request.Headers["HOST"]);

            Assert.IsNull(ApiServer.Parse("HEAD / HTTP/1.0\r\n\r\n").ApiKey);
            Assert.AreEqual("abc", ApiServer.Parse("GET / HTTP/1.1\r\nAuthorization: bearer abc\r\n\r\n").ApiKey);
            Assert.IsNull(ApiServer.Parse("GET / HTTP/1.1\r\nAuthorization: Bearer \r\n\r\n").ApiKey);

            foreach (var bad in new[]
                     {
                         null, "", "GET / HTTP/1.1\r\n", "GET /\r\n\r\n", "GET  / HTTP/1.1\r\n\r\n", "GET nowhere HTTP/1.1\r\n\r\n",
                         "GET / SPDY/3\r\n\r\n", "GET / HTTP/1.1\r\nno colon here\r\n\r\n", "\r\n\r\n", new string('A', 9000)
                     })
                Assert.IsNull(ApiServer.Parse(bad), bad ?? "null");
        }

        [TestMethod]
        public void AResponseIsWrittenWithItsLengthAndClosesTheConnection()
        {
            var text = Encoding.UTF8.GetString(ApiServer.Render(ApiResponse.Ok("{\"a\":\"é\"}"), headOnly: false));

            Assert.AreEqual(
                "HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: 10\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n{\"a\":\"é\"}",
                text);

            var head = Encoding.ASCII.GetString(ApiServer.Render(ApiResponse.Ok("{\"a\":1}"), headOnly: true));

            StringAssert.Contains(head, "Content-Length: 7\r\n");
            Assert.IsTrue(head.EndsWith("\r\n\r\n", StringComparison.Ordinal), "and no body");

            var refused = Encoding.ASCII.GetString(ApiServer.Render(new ApiResponse(405, "{}") { Allow = "GET, HEAD" }, headOnly: false));

            StringAssert.StartsWith(refused, "HTTP/1.1 405 Method Not Allowed\r\n");
            StringAssert.Contains(refused, "\r\nAllow: GET, HEAD\r\n");
            StringAssert.StartsWith(Encoding.ASCII.GetString(ApiServer.Render(ApiResponse.Error(503, "x"), false)), "HTTP/1.1 503 Service Unavailable\r\n");
        }

        [TestMethod]
        public void TheApiAnswersOverHttpOnItsPort()
        {
            var config = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, ApiKey = "global-key" };
            var api = Api(config, apply: false);

            Assert.IsFalse(api.Running);
            api.Apply(config);
            Assert.IsTrue(api.Running);

            try
            {
                var port = api.LocalEndPoint.Port;
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                // No key.
                using (var refused = Wait(http.GetAsync($"http://127.0.0.1:{port}/serverstatus")))
                {
                    Assert.AreEqual(HttpStatusCode.Unauthorized, refused.StatusCode);
                    Assert.AreEqual("{\"error\":\"unauthorized\"}", Wait(refused.Content.ReadAsStringAsync()));
                }

                // The key in its header.
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/serverstatus"))
                {
                    request.Headers.Add("X-API-Key", "global-key");

                    using var answered = Wait(http.SendAsync(request));

                    Assert.AreEqual(HttpStatusCode.OK, answered.StatusCode);
                    Assert.AreEqual("application/json", answered.Content.Headers.ContentType.MediaType);
                    Assert.AreEqual("{\"uptimeseconds\":0,\"currentconnections\":4,\"peakconnections\":4,\"maxconnections\":64}", Wait(answered.Content.ReadAsStringAsync()));
                }

                // HEAD: the headers of the same answer.
                using (var request = new HttpRequestMessage(HttpMethod.Head, $"http://127.0.0.1:{port}/healthcheck"))
                {
                    request.Headers.Add("X-API-Key", "global-key");

                    using var answered = Wait(http.SendAsync(request));

                    Assert.AreEqual(HttpStatusCode.OK, answered.StatusCode);
                    Assert.AreEqual((long?)HealthyBoth.Length, answered.Content.Headers.ContentLength);
                    Assert.AreEqual("", Wait(answered.Content.ReadAsStringAsync()));
                }

                // Something that is not HTTP.
                StringAssert.StartsWith(Exchange(port, "hello\r\n\r\n"), "HTTP/1.1 400 Bad Request\r\n");

                // A reload with the same address changes nothing of the listener; made public, it answers.
                config.Public = true;
                api.Apply(config);
                Assert.AreEqual(port, api.LocalEndPoint.Port);
                Assert.AreEqual(HealthyBoth, Wait(http.GetStringAsync($"http://127.0.0.1:{port}/healthcheck")));

                // Switched off by a reload.
                config.Enabled = false;
                api.Apply(config);
                Assert.IsFalse(api.Running);
                Assert.Throws<HttpRequestException>(() => Wait(http.GetStringAsync($"http://127.0.0.1:{port}/healthcheck")));
            }
            finally
            {
                api.Stop();
            }
        }

        #endregion

        #region TLS

        [TestMethod]
        public void TheMinimumProtocolIsReadInItsUsualSpellings()
        {
            foreach (var spelling in new[] { null, "", "Tls12", "tls12", "TLS 1.2", "TLSv1.2", "1.2" })
            {
                Assert.IsTrue(ApiTls.TryProtocols(spelling, out var protocols), spelling ?? "null");
                Assert.AreEqual(SslProtocols.Tls12 | SslProtocols.Tls13, protocols);
            }

            foreach (var spelling in new[] { "Tls13", "TLS 1.3", "TLSv1.3", "1.3" })
            {
                Assert.IsTrue(ApiTls.TryProtocols(spelling, out var protocols), spelling);
                Assert.AreEqual(SslProtocols.Tls13, protocols);
            }

            // Nothing older, and nothing that is no protocol: TLS 1.2 and up.
            foreach (var spelling in new[] { "Tls11", "1.0", "Ssl3", "newest" })
            {
                Assert.IsFalse(ApiTls.TryProtocols(spelling, out var protocols), spelling);
                Assert.AreEqual(SslProtocols.Tls12 | SslProtocols.Tls13, protocols);
            }
        }

        [TestMethod]
        public void WithTlsTheApiAnswersOverHttpsAndNothingElse()
        {
            using var files = new CertificateFiles();
            var certificate = files.Pfx("server.pfx", "pfx-password");
            var config = new RestApiConfig
            {
                Enabled = true,
                BindAddress = "127.0.0.1",
                Port = 0,
                ApiKey = "global-key",
                Tls = new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("server.pfx"), CertificatePassword = "pfx-password" }
            };
            var api = Api(config, apply: false);

            api.Apply(config);

            try
            {
                Assert.IsTrue(api.Running);
                Assert.AreEqual(certificate, api.Tls.Certificate.Thumbprint);
                Assert.AreEqual(SslProtocols.Tls12 | SslProtocols.Tls13, api.Tls.Protocols);

                var port = api.LocalEndPoint.Port;
                var answer = Https(port, "/serverstatus", "global-key");

                Assert.AreEqual(HttpStatusCode.OK, answer.Status);
                Assert.AreEqual("{\"uptimeseconds\":0,\"currentconnections\":4,\"peakconnections\":4,\"maxconnections\":64}", answer.Body);
                Assert.AreEqual(certificate, answer.Certificate, "the certificate of the file");

                // The key is still wanted.
                Assert.AreEqual(HttpStatusCode.Unauthorized, Https(port, "/serverstatus", null).Status);

                // Not a word in the clear.
                Assert.AreEqual("", Exchange(port, "GET /healthcheck HTTP/1.1\r\nX-API-Key: global-key\r\n\r\n"));

                using (var plain = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                    Assert.Throws<HttpRequestException>(() => Wait(plain.GetStringAsync($"http://127.0.0.1:{port}/healthcheck")));

                // And it still answers after that.
                Assert.AreEqual(HttpStatusCode.OK, Https(port, "/healthcheck", "global-key").Status);

                // An address that is not allowed does not get a handshake.
                config.AllowedIps = new List<string> { "10.0.0.0/8" };
                api.Apply(config);
                Assert.Throws<HttpRequestException>(() => Https(port, "/healthcheck", "global-key"));

                // Switched off by a reload, the same port is HTTP again.
                config.AllowedIps.Clear();
                config.Tls.Enabled = false;
                api.Apply(config);

                Assert.AreEqual(port, api.LocalEndPoint.Port);
                Assert.IsNull(api.Tls);
                StringAssert.StartsWith(Exchange(port, "GET /healthcheck HTTP/1.1\r\nX-API-Key: global-key\r\n\r\n"), "HTTP/1.1 200 OK\r\n");
            }
            finally
            {
                api.Stop();
            }
        }

        [TestMethod]
        public void TheCertificateCanBePemWithItsKeyBesideItInItOrEncrypted()
        {
            using var files = new CertificateFiles();

            var separate = files.Pem("separate.crt", "separate.key", keyPassword: null);
            var together = files.Pem("together.pem", null, keyPassword: null);
            var encrypted = files.Pem("encrypted.crt", "encrypted.key", keyPassword: "key-password");

            var cases = new[]
            {
                (Thumbprint: separate, Tls: new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("separate.crt"), KeyPath = files.Path("separate.key") }),
                (Thumbprint: together, Tls: new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("together.pem") }),
                (Thumbprint: encrypted, Tls: new ApiTlsConfig
                {
                    Enabled = true,
                    CertificatePath = files.Path("encrypted.crt"),
                    KeyPath = files.Path("encrypted.key"),
                    CertificatePassword = "key-password"
                })
            };

            foreach (var (thumbprint, tls) in cases)
            {
                var config = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, Public = true, Tls = tls };
                var api = Api(config, apply: false);

                api.Apply(config);

                try
                {
                    Assert.IsTrue(api.Running, tls.CertificatePath);

                    var answer = Https(api.LocalEndPoint.Port, "/healthcheck", null);

                    Assert.AreEqual(HealthyBoth, answer.Body);
                    Assert.AreEqual(thumbprint, answer.Certificate, tls.CertificatePath);
                }
                finally
                {
                    api.Stop();
                }
            }
        }

        [TestMethod]
        public void WithTlsOnAndNoCertificateThatLoadsTheApiStaysShut()
        {
            using var files = new CertificateFiles();

            files.Pfx("server.pfx", "pfx-password");
            files.Pem("alone.crt", "alone.key", keyPassword: null);
            File.WriteAllText(files.Path("garbage.pfx"), "this is no certificate");

            var broken = new[]
            {
                new ApiTlsConfig { Enabled = true },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("nowhere.pfx") },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("server.pfx"), CertificatePassword = "wrong" },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("server.pfx") },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("garbage.pfx") },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("alone.crt") },
                new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("alone.crt"), KeyPath = files.Path("nowhere.key") }
            };

            foreach (var tls in broken)
            {
                var config = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, Public = true, Tls = tls };
                var api = Api(config, apply: false);

                api.Apply(config);

                try
                {
                    Assert.IsNull(ApiTls.Load(tls, out var problem));
                    Assert.IsFalse(string.IsNullOrEmpty(problem));
                    Assert.IsFalse(api.Running, $"{tls.CertificatePath}: {problem}");
                    Assert.IsNull(api.Tls);
                }
                finally
                {
                    api.Stop();
                }
            }

            // Running in the clear, then told to use a certificate that is not there: closed, not left as it was.
            var settings = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, Public = true };
            var open = Api(settings, apply: false);

            open.Apply(settings);

            try
            {
                Assert.IsTrue(open.Running);

                settings.Tls = new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("nowhere.pfx") };
                open.Apply(settings);
                Assert.IsFalse(open.Running);

                // And with one that is, open again.
                settings.Tls = new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("server.pfx"), CertificatePassword = "pfx-password" };
                open.Apply(settings);
                Assert.IsTrue(open.Running);
                Assert.AreEqual(HealthyBoth, Https(open.LocalEndPoint.Port, "/healthcheck", null).Body);
            }
            finally
            {
                open.Stop();
            }
        }

        [TestMethod]
        public void ARenewedCertificateIsTakenUpWithoutARestartAndABrokenOneIsNot()
        {
            using var files = new CertificateFiles();
            var first = files.Pfx("server.pfx", "pfx-password");
            long now = 1_000_000;
            var config = new RestApiConfig
            {
                Enabled = true,
                BindAddress = "127.0.0.1",
                Port = 0,
                Public = true,
                Tls = new ApiTlsConfig { Enabled = true, CertificatePath = files.Path("server.pfx"), CertificatePassword = "pfx-password" }
            };
            var api = Api(config, apply: false);

            api.Now = () => now;
            api.Apply(config);

            try
            {
                var port = api.LocalEndPoint.Port;

                Assert.AreEqual(first, Https(port, "/healthcheck", null).Certificate);

                // The file is replaced, as a renewal replaces it.
                var second = files.Pfx("server.pfx", "pfx-password", writtenMinutesFromNow: 5);

                Assert.AreNotEqual(first, second);
                Assert.AreEqual(first, Https(port, "/healthcheck", null).Certificate, "not looked at on every connection");

                now += ApiServer.TlsRecheckMs;
                Assert.AreEqual(second, Https(port, "/healthcheck", null).Certificate, "looked at again after a minute");

                // A file that will not load: the one in use stays, and the API with it.
                File.WriteAllText(files.Path("server.pfx"), "half written");
                now += ApiServer.TlsRecheckMs;

                Assert.AreEqual(second, Https(port, "/healthcheck", null).Certificate);
                Assert.IsTrue(api.Running);

                // On a config reload too, without the wait.
                api.Apply(config);
                Assert.IsTrue(api.Running);
                Assert.AreEqual(second, api.Tls.Certificate.Thumbprint);

                var third = files.Pfx("server.pfx", "pfx-password", writtenMinutesFromNow: 10);

                api.Apply(config);
                Assert.AreEqual(third, api.Tls.Certificate.Thumbprint);
                Assert.AreEqual(third, Https(port, "/healthcheck", null).Certificate);
            }
            finally
            {
                api.Stop();
            }
        }

        [TestMethod]
        public void Tls13AloneRefusesAnOlderClient()
        {
            using var files = new CertificateFiles();

            files.Pfx("server.pfx", "pfx-password");

            var config = new RestApiConfig
            {
                Enabled = true,
                BindAddress = "127.0.0.1",
                Port = 0,
                Public = true,
                Tls = new ApiTlsConfig
                {
                    Enabled = true,
                    CertificatePath = files.Path("server.pfx"),
                    CertificatePassword = "pfx-password",
                    MinimumProtocol = "Tls13"
                }
            };
            var api = Api(config, apply: false);

            api.Apply(config);

            try
            {
                var port = api.LocalEndPoint.Port;

                Assert.AreEqual(SslProtocols.Tls13, api.Tls.Protocols);
                Assert.AreEqual(HttpStatusCode.OK, Https(port, "/healthcheck", null, SslProtocols.Tls13).Status);
                Assert.Throws<HttpRequestException>(() => Https(port, "/healthcheck", null, SslProtocols.Tls12));

                // Back to the usual: TLS 1.2 is taken again.
                config.Tls.MinimumProtocol = "Tls12";
                api.Apply(config);
                Assert.AreEqual(HttpStatusCode.OK, Https(port, "/healthcheck", null, SslProtocols.Tls12).Status);
            }
            finally
            {
                api.Stop();
            }
        }

        #endregion

        #region The status port

        [TestMethod]
        public void TheStatusPortAnswersAnythingSentWithTheFullStatusAndCloses()
        {
            var server = new StatusPortServer(Healthy(4, 64));

            server.Apply(new StatusPortConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0 });

            try
            {
                var port = server.LocalEndPoint.Port;
                var expected = HealthyBoth.TrimEnd('}') + ",\"uptimeseconds\":0,\"currentconnections\":4,\"peakconnections\":4,\"maxconnections\":64}\n";

                Assert.AreEqual(expected, Exchange(port, "?"));
                Assert.AreEqual(expected, Exchange(port, "\0"), "whatever it is");
                Assert.AreEqual(expected, Exchange(port, "GET / HTTP/1.1\r\n\r\n"));

                // Nothing is said until something has been sent.
                using var quiet = new TcpClient();

                quiet.Connect(IPAddress.Loopback, port);

                var stream = quiet.GetStream();
                var buffer = new byte[512];
                var early = stream.ReadAsync(buffer, 0, buffer.Length);

                Assert.IsFalse(early.Wait(300), "answered before it was asked");

                stream.Write(new byte[] { 1 }, 0, 1);

                Assert.IsTrue(early.Wait(5000));
                Assert.AreEqual(expected, Encoding.UTF8.GetString(buffer, 0, early.Result));
            }
            finally
            {
                server.Stop();
            }

            Assert.IsFalse(server.Running);
        }

        [TestMethod]
        public void TheStatusPortClosesOnAnAddressThatIsNotAllowed()
        {
            var server = new StatusPortServer(Healthy(1, 8));
            var config = new StatusPortConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0, AllowedIps = new List<string> { "10.0.0.0/8" } };

            server.Apply(config);

            try
            {
                var port = server.LocalEndPoint.Port;

                Assert.AreEqual("", Exchange(port, "?"), "closed without a word");

                // On the list from the next connection, with no new listener.
                config.AllowedIps.Add("127.0.0.1");
                server.Apply(config);
                Assert.AreEqual(port, server.LocalEndPoint.Port);
                StringAssert.Contains(Exchange(port, "?"), "\"maxconnections\":8");

                // An empty list is everybody.
                config.AllowedIps.Clear();
                server.Apply(config);
                StringAssert.Contains(Exchange(port, "?"), "\"currentconnections\":1");

                // A list of nothing but a mistake is nobody.
                config.AllowedIps.Add("localhost");
                server.Apply(config);
                Assert.AreEqual("", Exchange(port, "?"));
            }
            finally
            {
                server.Stop();
            }
        }

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void AListenerThatCannotOpenItsPortStaysOffAndTheHostCarriesOn()
        {
            var taken = new TcpListener(IPAddress.Loopback, 0);

            taken.Start();

            var host = new ApiHost(Healthy(0, 8));

            try
            {
                var port = ((IPEndPoint)taken.LocalEndpoint).Port;
                var config = new ApiConfig
                {
                    LoopStallSeconds = 40,
                    Rest = new RestApiConfig { Enabled = true, BindAddress = "127.0.0.1", Port = port },
                    StatusPort = new StatusPortConfig { Enabled = true, BindAddress = "127.0.0.1", Port = 0 }
                };

                host.Apply(config);

                Assert.IsFalse(host.Rest.Running, "the port is somebody else's");
                Assert.IsTrue(host.StatusPort.Running, "and the other listener is not held up by it");
                Assert.AreEqual(40000, host.Status.StallMs);

                TestContext.WriteLine(
                    "Registered REST endpoints: " +
                    string.Join(", ", host.Rest.Endpoints)
                );

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "addaccount", "healthcheck", "ingame/creature-actions",
                        "ingame/creatures", "ingame/creatures/{id}",
                        "ingame/items", "ingame/items/categories", "ingame/items/{id}", "ingame/session/exchange",
                        "kickuser", "lootpools", "monsterflags", "serverstatus", "updatelootpools", "updatemonsterflags", "usersonline"
                    },
                    host.Rest.Endpoints.ToArray());

                // An address that is none, a port that is none.
                config.StatusPort.BindAddress = "everywhere";
                host.Apply(config);
                Assert.IsFalse(host.StatusPort.Running);

                config.StatusPort.BindAddress = "";
                config.StatusPort.Port = 70000;
                host.Apply(config);
                Assert.IsFalse(host.StatusPort.Running);

                // No section at all: both off.
                host.Apply(null);
                Assert.IsFalse(host.Rest.Running);
                Assert.IsFalse(host.StatusPort.Running);
                Assert.AreEqual(15000, host.Status.StallMs);
            }
            finally
            {
                host.Stop();
                taken.Stop();
            }
        }

        #endregion

        #region The settings

        [TestMethod]
        public void TheSettingsAreReadFromTheFile()
        {
            const string json = @"{
              ""ApiConfig"": {
                ""LoopStallSeconds"": 30,
                ""Rest"": {
                  ""Enabled"": true,
                  ""BindAddress"": ""127.0.0.1"",
                  ""Port"": 9100,
                  ""Public"": false,
                  ""ApiKey"": ""global-key"",
                  ""AllowedIps"": [ ""203.0.113.7"", ""10.0.0.0/8"" ],
                  ""Endpoints"": {
                    ""healthcheck"": { ""Public"": true },
                    ""serverstatus"": { ""Enabled"": false, ""ApiKey"": ""status-key"" }
                  },
                  ""Tls"": {
                    ""Enabled"": true,
                    ""CertificatePath"": ""C:\\certs\\api.pfx"",
                    ""CertificatePassword"": ""pfx-password"",
                    ""MinimumProtocol"": ""Tls13""
                  }
                },
                ""StatusPort"": { ""Enabled"": true, ""Port"": 9101, ""AllowedIps"": [] }
              }
            }";

            var config = Bind(json).ApiConfig;

            Assert.AreEqual(30, config.LoopStallSeconds);
            Assert.IsTrue(config.Rest.Enabled);
            Assert.AreEqual("127.0.0.1", config.Rest.BindAddress);
            Assert.AreEqual(9100, config.Rest.Port);
            Assert.IsFalse(config.Rest.Public);
            Assert.AreEqual("global-key", config.Rest.ApiKey);
            CollectionAssert.AreEqual(new[] { "203.0.113.7", "10.0.0.0/8" }, config.Rest.AllowedIps);

            Assert.AreEqual(true, config.Rest.Endpoints["healthcheck"].Public);
            Assert.IsTrue(config.Rest.Endpoints["healthcheck"].Enabled, "on unless it says otherwise");
            Assert.AreEqual("", config.Rest.Endpoints["healthcheck"].ApiKey);
            Assert.IsNull(config.Rest.Endpoints["serverstatus"].Public, "left out: the API's");
            Assert.IsFalse(config.Rest.Endpoints["serverstatus"].Enabled);
            Assert.AreEqual("status-key", config.Rest.Endpoints["serverstatus"].ApiKey);

            Assert.IsTrue(config.Rest.Tls.Enabled);
            Assert.AreEqual(@"C:\certs\api.pfx", config.Rest.Tls.CertificatePath);
            Assert.AreEqual("pfx-password", config.Rest.Tls.CertificatePassword);
            Assert.AreEqual("", config.Rest.Tls.KeyPath);
            Assert.AreEqual("Tls13", config.Rest.Tls.MinimumProtocol);

            Assert.IsTrue(config.StatusPort.Enabled);
            Assert.AreEqual("0.0.0.0", config.StatusPort.BindAddress);
            Assert.AreEqual(9101, config.StatusPort.Port);
            Assert.IsTrue(IpAllowList.Parse(config.StatusPort.AllowedIps).AllowsAll);
        }

        [TestMethod]
        public void WithoutTheSectionBothListenersAreOff()
        {
            var config = Bind("{}").ApiConfig;

            Assert.IsFalse(config.Rest.Enabled);
            Assert.IsFalse(config.StatusPort.Enabled);
            Assert.AreEqual(8104, config.Rest.Port);
            Assert.AreEqual(8105, config.StatusPort.Port);
            Assert.AreEqual(15, config.LoopStallSeconds);
            Assert.IsFalse(config.Rest.Public, "and a key is wanted unless it says otherwise");
            Assert.IsFalse(config.Rest.Tls.Enabled);
            Assert.AreEqual("Tls12", config.Rest.Tls.MinimumProtocol);

            // A list left blank in the file is everybody, as one left out is.
            var blank = Bind(@"{ ""ApiConfig"": { ""StatusPort"": { ""Enabled"": true, ""AllowedIps"": """" }, ""Rest"": { ""AllowedIps"": [ """" ] } } }").ApiConfig;

            Assert.IsTrue(blank.StatusPort.Enabled);
            Assert.IsTrue(IpAllowList.Parse(blank.StatusPort.AllowedIps).AllowsAll);
            Assert.IsTrue(IpAllowList.Parse(blank.Rest.AllowedIps).AllowsAll);
        }

        #endregion

        private static RasaGame::Rasa.Config.Config Bind(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var config = new RasaGame::Rasa.Config.Config();

            new ConfigurationBuilder().AddJsonStream(stream).Build().Bind(config);

            return config;
        }

        /// <summary>A status that calls both servers healthy, with that many players of that many.</summary>
        private static ServerStatus Healthy(int players, int max, long uptimeMs = 0)
        {
            long now = 1000;
            var status = new ServerStatus { Now = () => now, AuthLinked = () => true };

            status.Started();
            status.Ready();
            now += uptimeMs;
            status.Beat(() => players, max, true);

            return status;
        }

        /// <summary>The API with the server's two endpoints over a healthy status of 4 players of 64.</summary>
        private static ApiServer Api(RestApiConfig config, bool apply = true)
        {
            var status = Healthy(4, 64);
            var api = new ApiServer();

            api.Register(new HealthCheckEndpoint(status));
            api.Register(new ServerStatusEndpoint(status));

            // Not listening: Respond is asked directly.
            if (apply)
                api.Apply(config);

            return api;
        }

        private static ApiRequest Get(string path, string key = null)
        {
            var request = new ApiRequest { Method = "GET", Path = path, Remote = IPAddress.Loopback };

            if (key != null)
                request.Headers["X-API-Key"] = key;

            return request;
        }

        /// <summary>
        /// A task's result, waited for here: the tests are plain methods, so that a runner that
        /// does not wait for a test's task still sees every assertion.
        /// </summary>
        private static T Wait<T>(Task<T> task)
        {
            if (Task.WaitAny(new Task[] { task }, TimeSpan.FromSeconds(15)) < 0)
                Assert.Fail("no answer in fifteen seconds");

            return task.GetAwaiter().GetResult();
        }

        /// <summary>Connects, sends, and reads until the other end closes.</summary>
        private static string Exchange(int port, string send)
        {
            using var client = new TcpClient { ReceiveTimeout = 10000, SendTimeout = 10000 };

            client.Connect(IPAddress.Loopback, port);

            var stream = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes(send);

            using var received = new MemoryStream();
            var buffer = new byte[1024];

            try
            {
                stream.Write(bytes, 0, bytes.Length);

                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    received.Write(buffer, 0, read);
            }
            catch (IOException)
            {
                // Closed on us before it was written to or read: nothing was said.
            }

            return Encoding.UTF8.GetString(received.ToArray());
        }

        /// <summary>One HTTPS request, any certificate taken: what was answered, and with which certificate.</summary>
        private static (HttpStatusCode Status, string Body, string Certificate) Https(int port, string path, string key, SslProtocols protocols = SslProtocols.None)
        {
            string certificate = null;

            using var handler = new SocketsHttpHandler();

            handler.SslOptions.EnabledSslProtocols = protocols;
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, presented, chain, errors) =>
            {
                certificate = presented == null ? null : new X509Certificate2(presented).Thumbprint;
                return true;
            };

            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{port}{path}");

            if (key != null)
                request.Headers.Add("X-API-Key", key);

            using var answered = Wait(http.SendAsync(request));

            return (answered.StatusCode, Wait(answered.Content.ReadAsStringAsync()), certificate);
        }

        /// <summary>Self-signed certificates written to a folder of the test's own, which goes with it.</summary>
        private sealed class CertificateFiles : IDisposable
        {
            private readonly string _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rasa-api-tls-" + Guid.NewGuid().ToString("N"));

            internal CertificateFiles() => Directory.CreateDirectory(_folder);

            internal string Path(string name) => System.IO.Path.Combine(_folder, name);

            private static X509Certificate2 Create(RSA key)
            {
                var request = new CertificateRequest("CN=rasa-api-test-" + Guid.NewGuid().ToString("N"), key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var names = new SubjectAlternativeNameBuilder();

                names.AddIpAddress(IPAddress.Loopback);
                request.CertificateExtensions.Add(names.Build());

                return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            }

            /// <summary>A new certificate as a PKCS#12 file; its thumbprint. The file's time can be put ahead, as a later write would.</summary>
            internal string Pfx(string name, string password, int writtenMinutesFromNow = 0)
            {
                using var key = RSA.Create(2048);
                using var certificate = Create(key);

                File.WriteAllBytes(Path(name), certificate.Export(X509ContentType.Pkcs12, password));
                File.SetLastWriteTimeUtc(Path(name), DateTime.UtcNow.AddMinutes(writtenMinutesFromNow));

                return certificate.Thumbprint;
            }

            /// <summary>A new certificate as PEM, its key in a file of its own or (no key file named) after it in the same one; its thumbprint.</summary>
            internal string Pem(string name, string keyName, string keyPassword)
            {
                using var key = RSA.Create(2048);
                using var certificate = Create(key);

                var keyPem = keyPassword == null
                    ? key.ExportPkcs8PrivateKeyPem()
                    : key.ExportEncryptedPkcs8PrivateKeyPem(keyPassword, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 10000));

                if (keyName == null)
                {
                    File.WriteAllText(Path(name), certificate.ExportCertificatePem() + "\n" + keyPem + "\n");
                }
                else
                {
                    File.WriteAllText(Path(name), certificate.ExportCertificatePem() + "\n");
                    File.WriteAllText(Path(keyName), keyPem + "\n");
                }

                return certificate.Thumbprint;
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(_folder, true);
                }
                catch (IOException)
                {
                }
            }
        }

        private sealed class Fixed : ApiEndpoint
        {
            private readonly string _json;

            internal Fixed(string name, string json)
            {
                Name = name;
                _json = json;
            }

            public override string Name { get; }

            public override ApiResponse Handle(ApiRequest request) => ApiResponse.Ok(_json);
        }

        private sealed class Broken : ApiEndpoint
        {
            public override string Name => "broken";

            public override ApiResponse Handle(ApiRequest request) => throw new InvalidOperationException("broken on purpose");
        }
    }
}
