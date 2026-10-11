"""Owned loopback fixture. Never serves gametools.config.js from disk or opens a game database."""
import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DOC = {
    "format": "rasa-weighted-loot-profiles", "version": 2,
    "profiles": [{"profileKey": "test", "description": "Editor fixture", "contentRevision": "test_v1",
                  "provenance": "Reconstructed", "evidenceNotes": "Reviewed fixture; not live balance"}],
    "groups": [{"profileKey": "test", "groupKey": "gear", "kind": 4, "chanceBasisPoints": 2500,
                "minimumRolls": 0, "maximumRolls": 2, "sortOrder": 0}],
    "items": [{"profileKey": "test", "groupKey": "gear", "itemTemplateId": 900 + i, "weight": w,
               "minimumQuantity": 1, "maximumQuantity": 1, "sortOrder": i} for i, w in enumerate([1, 3])],
    "bindings": [{"mapContextId": 10, "creatureId": 1, "profileKey": "test"}],
    "mapPolicies": [{"mapContextId": 10, "requireProfiles": True}],
}
STATE = {"saves": 0, "failNext": False, "payload": None}

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args): pass
    def reply(self, code, data, content_type="application/json"):
        encoded = data if isinstance(data, bytes) else json.dumps(data).encode()
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(encoded)))
        self.end_headers()
        self.wfile.write(encoded)
    def do_GET(self):
        name = self.path.split("?")[0]
        if name == "/__state": return self.reply(200, STATE)
        if name == "/lootpools": return self.reply(200, DOC)
        if name == "/gametools.config.js":
            config = "window.GAMETOOLS_CONFIG=" + json.dumps({
                "instance": {"name": "Owned mock fixture", "url": f"http://127.0.0.1:{self.server.server_port}"},
                "apiKey": "owned-fixture-not-a-real-credential"
            }) + ";"
            return self.reply(200, config.encode(), "text/javascript")
        allowed = {"/loot-table-editor.html", "/gametools.js", "/weighted-loot-project.js", "/weighted-loot-editor.js"}
        if name not in allowed: return self.reply(404, {"error": "fixture route not found"})
        file = ROOT / name.lstrip("/")
        if not file.exists(): return self.reply(404, {"error": "weighted editor file not implemented"})
        self.reply(200, file.read_bytes(), "text/html" if file.suffix == ".html" else "text/javascript")
    def do_POST(self):
        if self.path == "/__fail":
            STATE["failNext"] = True
            return self.reply(200, {"result": "armed"})
        if self.path != "/updatelootpools": return self.reply(404, {"error": "fixture route not found"})
        if self.headers.get("X-API-Key") != "owned-fixture-not-a-real-credential":
            return self.reply(401, {"error": "fixture key refused"})
        payload = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        if STATE["failNext"]:
            STATE["failNext"] = False
            return self.reply(500, {"error": "World database save failed; nothing changed",
                                    "problems": ["Owned injected failure; retain the draft and retry."]})
        STATE["saves"] += 1
        STATE["payload"] = payload
        DOC.update(payload)
        self.reply(200, {"result": "updated", **{k: len(payload[k]) for k in ["profiles", "groups", "items", "bindings", "mapPolicies"]}})

server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
print(f"READY http://127.0.0.1:{server.server_port}", flush=True)
server.serve_forever()
