const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const target = path.join(__dirname, '..', 'weighted-loot-project.js');
function project() {
  assert.ok(fs.existsSync(target), 'The editor needs a weighted project protocol, not flat percentage tidying.');
  return require(target);
}
function fixture() {
  return {
    format: 'rasa-weighted-loot-profiles', version: 2,
    profiles: [{ profileKey: 'test', description: "A 'profile'", contentRevision: 'test_v1', provenance: 'Reconstructed', evidenceNotes: 'Reviewed' }],
    groups: [{ profileKey: 'test', groupKey: 'gear', kind: 4, chanceBasisPoints: 2500, minimumRolls: 0, maximumRolls: 2, sortOrder: 0 }],
    items: [
      { profileKey: 'test', groupKey: 'gear', itemTemplateId: 900, weight: 1, minimumQuantity: 1, maximumQuantity: 3, sortOrder: 0 },
      { profileKey: 'test', groupKey: 'gear', itemTemplateId: 901, weight: 3, minimumQuantity: 1, maximumQuantity: 1, sortOrder: 1 }
    ],
    bindings: [{ mapContextId: 10, creatureId: 1, profileKey: 'test' }],
    mapPolicies: [{ mapContextId: 10, requireProfiles: true }]
  };
}
test('weighted parsing copies every row and preserves provenance and native IDs', () => {
  const input = fixture();
  const copy = project().parse(input);
  assert.deepEqual(copy, input);
  input.items[0].weight = 999;
  input.profiles[0].provenance = 'mutated';
  assert.equal(copy.items[0].weight, 1);
  assert.equal(copy.profiles[0].provenance, 'Reconstructed');
});
test('legacy percentages are rejected actionably without automatic conversion', () => {
  assert.throws(() => project().parse({ format: 'rasa-loot-tables', version: 1, groups: [], assignments: [] }), /percent.*weight.*explicit/i);
});
test('all five arrays and an explicit weighted header are required', () => {
  const input = fixture();
  delete input.bindings;
  assert.throws(() => project().parse(input), /bindings/);
  assert.throws(() => project().parse({ groups: [] }), /format|weighted/);
});
for (const collection of ['profiles', 'groups', 'items', 'bindings', 'mapPolicies']) {
  for (const [field, value] of Object.entries(fixture()[collection][0])) {
    test(`weighted JSON requires explicit typed ${collection}.${field} without changing the payload`, () => {
      for (const fault of ['omitted', 'null', 'wrong type']) {
        const input = fixture();
        if (fault === 'omitted') delete input[collection][0][field];
        else input[collection][0][field] = fault === 'null' ? null : typeof value === 'string' ? 1 : '1';
        const before = JSON.stringify(input);
        assert.throws(() => project().parse(input), undefined, `${collection}.${field}: ${fault}`);
        assert.equal(JSON.stringify(input), before);
      }
    });
  }
}
test('weighted JSON round trips explicit zero admission rolls orders and false policies exactly', () => {
  const input = fixture();
  Object.assign(input.groups[0], { chanceBasisPoints: 0, minimumRolls: 0, maximumRolls: 0, sortOrder: 0 });
  input.mapPolicies[0].requireProfiles = false;
  input.profiles[0].description = '';
  assert.deepEqual(project().parse(JSON.parse(JSON.stringify(input))), input);
});
for (const [field, mutate, error] of [
  ['fractional weight', x => x.items[0].weight = 1.5, /weight/],
  ['zero weight', x => x.items[0].weight = 0, /weight/],
  ['unsafe total weight', x => x.items[0].weight = 2147483647, /total weight/],
  ['quantity ordering', x => x.items[0].minimumQuantity = 4, /quantity/],
  ['chance bounds', x => x.groups[0].chanceBasisPoints = 10001, /admission|chance/],
  ['key', x => x.profiles[0].profileKey = 'Upper Case', /key/],
  ['roll cap', x => x.groups[0].maximumRolls = 33, /roll/],
  ['duplicate bindings', x => x.bindings.push({ ...x.bindings[0] }), /duplicate/],
  ['Junk cap', x => x.groups[0].kind = 1, /Junk|junk/],
  ['missing group', x => x.items[0].groupKey = 'missing', /group/],
  ['metadata length', x => x.profiles[0].contentRevision = 'x'.repeat(33), /revision/],
  ['percent field in weighted rows', x => x.items[0].chance = 5, /percent|chance/]
]) test(`invalid ${field} is refused without normalization`, () => {
  const input = fixture(); mutate(input);
  assert.throws(() => project().parse(input), error);
});
test('odds distinguish admission, conditional selection and zero rolls', () => {
  const p = project(), input = fixture(), group = input.groups[0];
  const odds = p.odds(group, input.items);
  assert.equal(odds.admissionPercent, 25);
  assert.equal(odds.candidates[0].perRollPercent, 25);
  assert.equal(odds.candidates[1].perRollPercent, 75);
  assert.ok(Math.abs(odds.noSelectionPercent - 83.33333333333333) < 0.001);
  group.chanceBasisPoints = 0;
  assert.equal(p.odds(group, input.items).noSelectionPercent, 100);
});
test('SQL exports only the five canonical tables in FK-safe order and escapes metadata', () => {
  const sql = project().sql(fixture(), 'sqlite');
  assert.doesNotMatch(sql, /legacy_flat_|creature_loot_group|loot_group_item|CREATE TABLE|DELETE FROM creature_loot_binding/);
  assert.match(sql, /BEGIN|START TRANSACTION/);
  assert.match(sql, /COMMIT/);
  assert.match(sql, /A ''profile''/);
  const tables = ['loot_profile', 'ordinary_loot_group', 'loot_item', 'creature_loot_binding', 'loot_map_policy'];
  const inserts = tables.map(t => sql.indexOf('INSERT INTO ' + t + ' '));
  assert.ok(inserts.every(i => i >= 0));
  assert.deepEqual(inserts, inserts.slice().sort((a, b) => a - b));
  assert.ok(sql.indexOf('DELETE FROM loot_item') < sql.indexOf('DELETE FROM ordinary_loot_group'));
});
test('SQL key checks use a materialized derived source instead of a MySQL target-table subquery', () => {
  const sql = project().sql(fixture(), 'mysql');
  assert.match(sql, /FROM \(SELECT DISTINCT profile_key FROM loot_profile\) AS saved_keys/);
  assert.match(sql, /FROM \(SELECT DISTINCT map_context_id, creature_id FROM creature_loot_binding\) AS saved_keys/);
  assert.match(sql, /FROM \(SELECT DISTINCT map_context_id FROM loot_map_policy\) AS saved_keys/);
});
test('SQL requires an explicit supported provider instead of ambiguous generic SQL', () => {
  for (const dialect of [undefined, '', 'postgresql', 'MYSQL'])
    assert.throws(() => project().sql(fixture(), dialect), /dialect.*sqlite.*mysql/i);
  assert.match(project().sql(fixture(), 'sqlite'), /-- SQL dialect: SQLite/);
  assert.match(project().sql(fixture(), 'mysql'), /-- SQL dialect: MySQL/);
});
test('MySQL exports UTF-8 hex text expressions independent of backslash SQL mode', () => {
  const input = fixture();
  Object.assign(input.profiles[0], {
    description: 'C:\\new\\review',
    contentRevision: 'ending\\',
    provenance: "O'Brien \"quoted\"",
    evidenceNotes: '中文 🙂 e\u0301 العربية\nline\tend\\'
  });
  const sql = project().sql(input, 'mysql');
  assert.match(sql, /CONVERT\(X'433a5c6e65775c726576696577' USING utf8mb4\)/);
  assert.match(sql, /CONVERT\(X'656e64696e675c' USING utf8mb4\)/);
  assert.doesNotMatch(sql, /SET\s+.*sql_mode|C:\\new\\review|O'Brien|中文|\\/i);
  // The only quoted tokens are hex digits: neither MySQL string-escape mode can reinterpret them.
  for (const mode of ['normal backslash escapes', 'NO_BACKSLASH_ESCAPES']) {
    const quoted = [...sql.matchAll(/'([^']*)'/g)].map(match => match[1]);
    assert.ok(quoted.length > 0, mode);
    assert.ok(quoted.every(text => /^(?:[0-9a-f]{2})*$/.test(text)), mode);
    const decoded = [...sql.matchAll(/CONVERT\(X'([0-9a-f]*)' USING utf8mb4\)/g)]
      .map(match => Buffer.from(match[1], 'hex').toString('utf8'));
    assert.deepEqual(decoded.slice(0, 5), Object.values(input.profiles[0]), mode);
    assert.deepEqual(decoded.slice(5, 10), [
      input.profiles[0].profileKey, input.profiles[0].description, input.profiles[0].contentRevision,
      input.profiles[0].provenance, input.profiles[0].evidenceNotes
    ], mode);
  }
  assert.deepEqual(project().parse(JSON.parse(JSON.stringify(input))), input, 'Provider export cannot alter the JSON payload.');
  assert.ok(sql.indexOf('DELETE FROM loot_item') < sql.indexOf('DELETE FROM ordinary_loot_group'));
  assert.match(sql, /FROM \(SELECT DISTINCT profile_key FROM loot_profile\) AS saved_keys/);
  assert.doesNotMatch(sql, /DELETE FROM creature_loot_binding|DELETE FROM loot_map_policy|legacy_flat_/);
  input.profiles[0].description = '';
  assert.match(project().sql(input, 'mysql'), /CONVERT\(X'' USING utf8mb4\)/);
});
