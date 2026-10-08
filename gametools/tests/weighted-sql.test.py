import json
import re
import sqlite3
import subprocess
import unittest
from pathlib import Path

class CanonicalSqlTests(unittest.TestCase):
    def test_export_executes_with_foreign_keys_and_preserves_unnamed_content(self):
        self.assert_export_round_trip("sqlite")

    def test_mysql_hex_text_and_portable_statement_syntax_round_trip_offline(self):
        self.assert_export_round_trip("mysql")

    def export(self, doc, dialect):
        project = Path(__file__).resolve().parent.parent / "weighted-loot-project.js"
        sql = subprocess.run(["node", "-e",
            "const fs=require('node:fs');process.stdout.write(require(process.argv[1]).sql(JSON.parse(fs.readFileSync(0,'utf8')),process.argv[2]));",
            str(project), dialect], input=json.dumps(doc), encoding="utf-8", capture_output=True, check=True).stdout
        if dialect == "mysql":
            self.assertIn("-- SQL dialect: MySQL", sql)
            self.assertIn("NO_BACKSLASH_ESCAPES", sql)
            self.assertNotIn("\\", sql)
            # Only adapt the two provider-specific constructs, not the exported DML.
            # This checks the portable syntax/UTF-8 bytes in owned SQLite, not a MySQL engine.
            sql = re.sub(r"CONVERT\(X'([0-9a-f]*)' USING utf8mb4\)",
                         lambda match: "CAST(X'" + match[1] + "' AS TEXT)", sql)
            self.assertNotIn("CONVERT(", sql)
            sql = sql.replace("START TRANSACTION;", "BEGIN;")
        else:
            self.assertIn("-- SQL dialect: SQLite", sql)
        return sql

    def assert_export_round_trip(self, dialect):
        doc = {
            "format": "rasa-weighted-loot-profiles", "version": 2,
            "profiles": [{"profileKey": "test", "description": "A 'profile' \"quoted\" C:\\new\\review",
                          "contentRevision": "ending\\", "provenance": "中文 🙂 e\u0301 العربية",
                          "evidenceNotes": "SQLite fixture\nline\tend\\"}],
            "groups": [{"profileKey": "test", "groupKey": "gear", "kind": 4,
                        "chanceBasisPoints": 5000, "minimumRolls": 1, "maximumRolls": 2, "sortOrder": 0}],
            "items": [{"profileKey": "test", "groupKey": "gear", "itemTemplateId": 900, "weight": 7,
                       "minimumQuantity": 1, "maximumQuantity": 3, "sortOrder": 0}],
            "bindings": [{"mapContextId": 10, "creatureId": 1, "profileKey": "test"}],
            "mapPolicies": [{"mapContextId": 10, "requireProfiles": True}],
        }
        sql = self.export(doc, dialect)
        with sqlite3.connect(":memory:") as db:
            db.executescript("""
                PRAGMA foreign_keys=ON;
                CREATE TABLE loot_profile (profile_key TEXT PRIMARY KEY, description TEXT NOT NULL,
                  content_revision TEXT NOT NULL, provenance TEXT NOT NULL, evidence_notes TEXT NOT NULL);
                CREATE TABLE ordinary_loot_group (profile_key TEXT, group_key TEXT, kind INTEGER,
                  chance_basis_points INTEGER, minimum_rolls INTEGER, maximum_rolls INTEGER, sort_order INTEGER,
                  PRIMARY KEY(profile_key,group_key), FOREIGN KEY(profile_key) REFERENCES loot_profile(profile_key));
                CREATE TABLE loot_item (profile_key TEXT, group_key TEXT, item_template_id INTEGER, weight INTEGER,
                  minimum_quantity INTEGER, maximum_quantity INTEGER, sort_order INTEGER,
                  PRIMARY KEY(profile_key,group_key,item_template_id),
                  FOREIGN KEY(profile_key,group_key) REFERENCES ordinary_loot_group(profile_key,group_key));
                CREATE TABLE creature_loot_binding (map_context_id INTEGER, creature_id INTEGER, profile_key TEXT,
                  PRIMARY KEY(map_context_id,creature_id), FOREIGN KEY(profile_key) REFERENCES loot_profile(profile_key));
                CREATE TABLE loot_map_policy (map_context_id INTEGER PRIMARY KEY, require_profiles INTEGER);
                INSERT INTO loot_profile VALUES ('untouched','keep','v1','Native','Review');
                INSERT INTO creature_loot_binding VALUES (20,2,'untouched');
                INSERT INTO loot_map_policy VALUES (20,1);
                INSERT INTO loot_map_policy VALUES (10,1);
            """)
            db.executescript(sql)
            db.executescript(sql)
            self.assertEqual(tuple(doc["profiles"][0][name] for name in
                ["description", "contentRevision", "provenance", "evidenceNotes"]), db.execute(
                "SELECT description,content_revision,provenance,evidence_notes FROM loot_profile WHERE profile_key='test'").fetchone())
            self.assertEqual((5000, 1, 2), db.execute(
                "SELECT chance_basis_points,minimum_rolls,maximum_rolls FROM ordinary_loot_group WHERE profile_key='test'").fetchone())
            self.assertEqual((7, 1, 3), db.execute(
                "SELECT weight,minimum_quantity,maximum_quantity FROM loot_item WHERE item_template_id=900").fetchone())
            self.assertEqual(("untouched",), db.execute(
                "SELECT profile_key FROM creature_loot_binding WHERE map_context_id=20 AND creature_id=2").fetchone())
            self.assertEqual((1,), db.execute("SELECT require_profiles FROM loot_map_policy WHERE map_context_id=20").fetchone())
            self.assertEqual([], db.execute("PRAGMA foreign_key_check").fetchall())
            doc["mapPolicies"][0]["requireProfiles"] = False
            protected_sql = self.export(doc, dialect)
            db.executescript(protected_sql)
            self.assertEqual((1,), db.execute("SELECT require_profiles FROM loot_map_policy WHERE map_context_id=10").fetchone())

if __name__ == "__main__":
    unittest.main()
