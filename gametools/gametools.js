// What the game tools share: the settings in gametools.config.js, and the way to the game
// server's REST API. Each editor loads gametools.config.js and then this file; it leaves one
// thing behind, window.GameTools.
//
//   GameTools.ready            whether there are settings that can be used
//   GameTools.problems         what is wrong with the settings, as sentences; empty when ready
//   GameTools.instance         { name, url } of the game instance
//   GameTools.has(name)        whether the endpoint of that name can be called
//   GameTools.get(name)        GET it; a promise of the answer's JSON
//   GameTools.post(name, obj)  POST obj to it as JSON; a promise of the answer's JSON
//
// A promise that fails does so with an Error whose message is fit to show: what went wrong
// and what to look at. Its .status is the HTTP status, or 0 when there was no answer, and its
// .problems the server's list of what it would not take, when it sent one.
(function (root) {
  "use strict";

  var DEFAULT_PATHS = {
    monsterFlagsRead: "/monsterflags",
    monsterFlagsWrite: "/updatemonsterflags",
    lootPoolsRead: "/lootpools",
    lootPoolsWrite: "/updatelootpools"
  };

  var raw = root.GAMETOOLS_CONFIG;
  var problems = [];
  var instance = { name: "", url: "" };
  var endpoints = {};
  var timeoutMs = 15000;

  function text(value) { return typeof value === "string" ? value.trim() : ""; }

  if (raw == null || typeof raw !== "object") {
    problems.push("There is no gametools.config.js beside this page, or it has a mistake in it. " +
      "Copy gametools.config.example.js to gametools.config.js and fill it in.");
  } else {
    var given = typeof raw.instance === "string" ? { url: raw.instance } : (raw.instance || {});
    var url = text(given.url).replace(/\/+$/, "");

    if (!/^https?:\/\/[^\s/?#]+$/i.test(url)) {
      problems.push("gametools.config.js: instance.url must be the address of the game server's REST API, " +
        "like http://127.0.0.1:8104, with nothing after the port.");
    }

    instance = { name: text(given.name) || url, url: url };

    var common = text(raw.apiKey);
    var listed = raw.endpoints && typeof raw.endpoints === "object" ? raw.endpoints : {};

    Object.keys(DEFAULT_PATHS).forEach(function (name) {
      var entry = listed[name];

      if (typeof entry === "string") entry = { path: entry };
      if (entry === false || entry === null) return;          // switched off in the settings
      entry = entry || {};

      var path = text(entry.path) || DEFAULT_PATHS[name];

      if (path.charAt(0) !== "/") path = "/" + path;
      endpoints[name] = { path: path, key: text(entry.key) || common };
    });

    if (typeof raw.timeoutMs === "number" && raw.timeoutMs >= 1000) timeoutMs = raw.timeoutMs;
  }

  var ready = problems.length === 0;

  function fail(message, status, list) {
    var error = new Error(message);

    error.status = status || 0;
    error.problems = Array.isArray(list) ? list : [];
    return error;
  }

  // The page's own origin as the server sees it: "null" for a page opened from disk.
  function origin() {
    return root.location && root.location.protocol !== "file:" && root.location.origin ? root.location.origin : "null";
  }

  function unreachable(endpoint) {
    return "No answer from " + instance.url + endpoint.path + " that this page may read. Check that the game server is " +
      "running and reachable at that address, that its appsettings.json has this endpoint Enabled with a key and not " +
      "Public, and that ApiConfig.Rest.AllowedOrigins lists \"" + origin() + "\".";
  }

  function refusal(status, endpoint, body) {
    var said = body && typeof body.error === "string" ? body.error : "";

    if (status === 400) return said || "The server would not take what was sent.";
    if (status === 401) return "The server refused the key for " + endpoint.path + ". Check apiKey and the endpoint's key in gametools.config.js against the server's appsettings.json.";
    if (status === 403) return "The server does not answer this computer (" + (said || "forbidden") + "). Check ApiConfig.Rest.AllowedIps on the server.";
    if (status === 404) return "The server has no " + endpoint.path + ", or it is switched off there.";
    if (status === 413) return "What was sent is more than the server takes in one request.";
    if (status === 503) return "The game server is not ready for this yet" + (said ? " (" + said + ")" : "") + ". Try again once it has finished starting.";
    return "The server answered " + status + (said ? ": " + said : "") + ".";
  }

  function call(name, payload) {
    if (!ready) return Promise.reject(fail(problems[0], 0));

    var endpoint = endpoints[name];

    if (!endpoint) return Promise.reject(fail("gametools.config.js has no endpoint named " + name + ".", 0));

    if (!endpoint.key) {
      return Promise.reject(fail("gametools.config.js gives no key for " + name + ": set apiKey, or the endpoint's own key. " +
        "The server only lets a page use an endpoint that wants a key.", 0));
    }

    var headers = { "X-API-Key": endpoint.key };
    var options = { method: "GET", headers: headers, mode: "cors", cache: "no-store", credentials: "omit" };

    if (payload !== undefined) {
      options.method = "POST";
      headers["Content-Type"] = "application/json";
      options.body = JSON.stringify(payload);
    }

    var timer = null;

    if (typeof AbortController === "function") {
      var stop = new AbortController();

      options.signal = stop.signal;
      timer = setTimeout(function () { stop.abort(); }, timeoutMs);
    }

    return fetch(instance.url + endpoint.path, options).then(function (response) {
      return response.text().then(function (body) {
        var parsed = null;

        try { parsed = body ? JSON.parse(body) : null; } catch (e) { parsed = null; }

        if (!response.ok) throw fail(refusal(response.status, endpoint, parsed), response.status, parsed && parsed.problems);
        if (parsed === null || typeof parsed !== "object") throw fail("The server's answer at " + endpoint.path + " was not JSON. Is that address the game server's REST API?", response.status);

        return parsed;
      });
    }, function (error) {
      if (error && error.name === "AbortError") {
        throw fail("The server at " + instance.url + " did not answer within " + Math.round(timeoutMs / 1000) + " seconds.", 0);
      }

      throw fail(unreachable(endpoint), 0);
    }).then(function (value) {
      clearTimeout(timer);
      return value;
    }, function (error) {
      clearTimeout(timer);
      throw error;
    });
  }

  root.GameTools = {
    ready: ready,
    problems: problems,
    instance: instance,
    origin: origin,
    has: function (name) { return ready && !!endpoints[name] && !!endpoints[name].key; },
    get: function (name) { return call(name); },
    post: function (name, payload) { return call(name, payload === undefined ? {} : payload); }
  };
})(window);
