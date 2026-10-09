// The settings of the game tools: which game server they talk to, at which endpoints, with
// which keys.
//
// Copy this file to gametools.config.js, in this same folder, and fill it in. The editors
// read gametools.config.js when they are opened; without it they still open, and work on
// files as they always have. gametools.config.js holds keys, so it is not kept in git.
//
// What the game server needs set for this to work is in README.md.

window.GAMETOOLS_CONFIG = {
  // The game instance: the address of its REST API (ApiConfig.Rest in the server's
  // appsettings.json: http or https, the server's address, and Port), and a name for it that
  // the editors show so you can tell a test server from the live one.
  instance: {
    name: "Local test server",
    url: "http://127.0.0.1:8104"
  },

  // A key every endpoint below accepts (ApiConfig.Rest.ApiKey). Leave it empty if each
  // endpoint has a key of its own.
  apiKey: "",

  // The endpoints, by what the editors use them for: the path on the server, and the key of
  // that endpoint (its ApiKey under ApiConfig.Rest.Endpoints). An empty key means apiKey
  // above is used. The paths are the server's own names and only change if yours differ.
  endpoints: {
    monsterFlagsRead:  { path: "/monsterflags",       key: "" },
    monsterFlagsWrite: { path: "/updatemonsterflags", key: "" },
    lootPoolsRead:     { path: "/lootpools",          key: "" },
    lootPoolsWrite:    { path: "/updatelootpools",    key: "" }
  },

  // How long an editor waits for the server before it gives up, in milliseconds.
  timeoutMs: 15000
};
