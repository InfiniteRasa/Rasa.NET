(function (root) {
  'use strict';
  var FORMAT = 'rasa-weighted-loot-profiles', VERSION = 2, INT = 2147483647;
  var legacy = 'Legacy flat percent projects are not weights. Export and review the old configuration, then explicitly author weighted profiles, admission and map/creature bindings.';
  function empty() { return {format: FORMAT, version: VERSION, profiles: [], groups: [], items: [], bindings: [], mapPolicies: []}; }
  function parse(input) {
    if (!input || input.format !== FORMAT || input.version !== VERSION) {
      throw new Error(input && (input.format === 'rasa-loot-tables' || input.assignments) ? legacy : 'Expected weighted format ' + FORMAT + ', version 2. Load a current weighted project.');
    }
    var out = empty();
    ['profiles', 'groups', 'items', 'bindings', 'mapPolicies'].forEach(function (name) {
      if (!Array.isArray(input[name])) throw new Error(name + ' must be an array, even when empty.');
      out[name] = JSON.parse(JSON.stringify(input[name]));
    });
    var problems = validate(out);
    if (problems.length) throw new Error(problems.join('\n'));
    return out;
  }
  function validate(doc) {
    var errors = [], profiles = new Set(), groups = new Set(), items = new Set(), bindings = new Set(), policies = new Set();
    function bad(text) { if (errors.length < 200) errors.push(text); }
    function key(value, max, at) {
      if (typeof value !== 'string' || !new RegExp('^[a-z0-9_-]{1,' + max + '}$').test(value)) bad(at + ': key must be 1..' + max + ' lowercase ASCII letters, digits, underscores or hyphens.');
    }
    function integer(value, lo, hi, at) { if (!Number.isInteger(value) || value < lo || value > hi) bad(at + ': must be an integer in ' + lo + '..' + hi + '.'); }
    function unique(set, value, at) { if (set.has(value)) bad(at + ': duplicate key.'); set.add(value); }
    function rows(name, fn) {
      if (!Array.isArray(doc[name])) { bad(name + ' must be an array.'); return; }
      doc[name].forEach(function (row, index) {
        if (errors.length >= 200) return;
        if (!row || typeof row !== 'object' || Array.isArray(row)) bad(name + '[' + index + ']: expected an object.');
        else fn(row, name + '[' + index + ']');
      });
    }
    rows('profiles', function (r, at) {
      key(r.profileKey, 64, at); unique(profiles, r.profileKey, at);
      ['description', 'contentRevision', 'provenance', 'evidenceNotes'].forEach(function (field) {
        if (typeof r[field] !== 'string') bad(at + ': ' + field + ' must be a string.');
      });
      if (typeof r.contentRevision === 'string' && r.contentRevision.length > 32) bad(at + ': content revision is longer than 32 characters.');
    });
    rows('groups', function (r, at) {
      if ('id' in r || 'items' in r || 'chance' in r) bad(legacy);
      key(r.profileKey, 64, at); key(r.groupKey, 32, at);
      if (!profiles.has(r.profileKey)) bad(at + ': missing profile ' + r.profileKey + '.');
      unique(groups, r.profileKey + ':' + r.groupKey, at);
      integer(r.kind, 1, 4, at + ' kind');
      integer(r.chanceBasisPoints, 0, 10000, at + ' admission chanceBasisPoints');
      integer(r.minimumRolls, 0, 32, at + ' minimum rolls');
      integer(r.maximumRolls, r.minimumRolls, 32, at + ' maximum rolls');
      integer(r.sortOrder, -INT - 1, INT, at + ' sortOrder');
      if (r.kind === 1 && r.maximumRolls > 1) bad(at + ': Junk groups select at most one type.');
    });
    rows('items', function (r, at) {
      if ('chance' in r) bad(legacy);
      key(r.profileKey, 64, at); key(r.groupKey, 32, at);
      if (!groups.has(r.profileKey + ':' + r.groupKey)) bad(at + ': missing group.');
      unique(items, r.profileKey + ':' + r.groupKey + ':' + r.itemTemplateId, at);
      integer(r.itemTemplateId, 1, 4294967295, at + ' itemTemplateId');
      integer(r.weight, 1, INT, at + ' weight');
      integer(r.minimumQuantity, 1, INT - 1, at + ' minimum quantity');
      integer(r.maximumQuantity, r.minimumQuantity, INT - 1, at + ' maximum quantity');
      integer(r.sortOrder, -INT - 1, INT, at + ' sortOrder');
    });
    rows('bindings', function (r, at) {
      integer(r.mapContextId, 1, 4294967295, at + ' mapContextId');
      integer(r.creatureId, 1, 4294967295, at + ' creatureId');
      key(r.profileKey, 64, at);
      if (!profiles.has(r.profileKey)) bad(at + ': missing profile.');
      unique(bindings, r.mapContextId + ':' + r.creatureId, at);
    });
    rows('mapPolicies', function (r, at) {
      integer(r.mapContextId, 1, 4294967295, at + ' mapContextId');
      if (typeof r.requireProfiles !== 'boolean') bad(at + ': requireProfiles must be a boolean.');
      unique(policies, r.mapContextId, at);
    });
    (doc.groups || []).filter(Boolean).forEach(function (g) {
      var candidates = (doc.items || []).filter(function (r) { return r && r.profileKey === g.profileKey && r.groupKey === g.groupKey; });
      if (!candidates.length) bad(g.profileKey + '/' + g.groupKey + ': group has no candidates. Use an empty profile for no loot.');
      if (candidates.reduce(function (sum, r) { return sum + r.weight; }, 0) > INT) bad(g.profileKey + '/' + g.groupKey + ': total weight exceeds integer RNG range.');
    });
    profiles.forEach(function (profile) {
      var gs = (doc.groups || []).filter(function (g) { return g && g.profileKey === profile; });
      if (gs.reduce(function (sum, g) { return sum + g.maximumRolls; }, 0) > 32) bad(profile + ': aggregate maximum rolls exceed 32.');
      if (gs.filter(function (g) { return g.kind === 1; }).length > 1) bad(profile + ': multiple Junk groups bypass the one-type cap.');
    });
    return errors;
  }
  function odds(group, items) {
    var entries = items.filter(function (r) { return r.profileKey === group.profileKey && r.groupKey === group.groupKey; });
    var total = entries.reduce(function (sum, r) { return sum + r.weight; }, 0);
    var admission = group.chanceBasisPoints / 10000, counts = group.maximumRolls - group.minimumRolls + 1;
    var zero = group.minimumRolls === 0 ? 1 / counts : 0;
    return {
      admissionPercent: admission * 100,
      noSelectionPercent: (1 - admission + admission * zero) * 100,
      expectedSelections: admission * (group.minimumRolls + group.maximumRolls) / 2,
      candidates: entries.map(function (r) { return {itemTemplateId: r.itemTemplateId, perRollPercent: total > 0 ? r.weight / total * 100 : 0}; })
    };
  }
  function sql(input, dialect) {
    if (dialect !== 'sqlite' && dialect !== 'mysql') throw new Error('SQL dialect must explicitly be sqlite or mysql.');
    var doc = parse(input), lines = [
      dialect === 'mysql' ? '-- SQL dialect: MySQL (UTF-8 hex text; valid with or without NO_BACKSLASH_ESCAPES).' :
        '-- SQL dialect: SQLite (text literals; backslashes are preserved).',
      '-- Weighted ordinary loot. Validate native items and required-map coverage before applying.',
      '-- Offline authoring export; reload the validated catalog after applying. Roll back on any error.',
      dialect === 'mysql' ? 'START TRANSACTION;' : 'BEGIN;'];
    function quote(value) {
      if (dialect === 'sqlite') return "'" + value.replace(/'/g, "''") + "'";
      var hex = Array.from(new TextEncoder().encode(value), function (byte) { return byte.toString(16).padStart(2, '0'); }).join('');
      return "CONVERT(X'" + hex + "' USING utf8mb4)";
    }
    function upsert(table, columns, values, where, assignments, keyColumns) {
      // DISTINCT prevents MySQL from merging this derived source into the INSERT target.
      lines.push('INSERT INTO ' + table + ' (' + columns + ') SELECT ' + values +
        ' WHERE NOT EXISTS (SELECT 1 FROM (SELECT DISTINCT ' + keyColumns + ' FROM ' + table + ') AS saved_keys WHERE ' + where + ');');
      lines.push('UPDATE ' + table + ' SET ' + assignments + ' WHERE ' + where + ';');
    }
    doc.profiles.forEach(function (r) {
      var values = [r.profileKey, r.description, r.contentRevision, r.provenance, r.evidenceNotes].map(quote);
      upsert('loot_profile', 'profile_key, description, content_revision, provenance, evidence_notes', values.join(', '),
        'profile_key = ' + values[0], ['description', 'content_revision', 'provenance', 'evidence_notes'].map(function (c, i) { return c + ' = ' + values[i + 1]; }).join(', '), 'profile_key');
    });
    if (doc.profiles.length) {
      var scope = doc.profiles.map(function (r) { return quote(r.profileKey); }).join(', ');
      lines.push('DELETE FROM loot_item WHERE profile_key IN (' + scope + ');');
      lines.push('DELETE FROM ordinary_loot_group WHERE profile_key IN (' + scope + ');');
    }
    doc.groups.forEach(function (r) {
      lines.push('INSERT INTO ordinary_loot_group (profile_key, group_key, kind, chance_basis_points, minimum_rolls, maximum_rolls, sort_order) VALUES (' +
        [quote(r.profileKey), quote(r.groupKey), r.kind, r.chanceBasisPoints, r.minimumRolls, r.maximumRolls, r.sortOrder].join(', ') + ');');
    });
    doc.items.forEach(function (r) {
      lines.push('INSERT INTO loot_item (profile_key, group_key, item_template_id, weight, minimum_quantity, maximum_quantity, sort_order) VALUES (' +
        [quote(r.profileKey), quote(r.groupKey), r.itemTemplateId, r.weight, r.minimumQuantity, r.maximumQuantity, r.sortOrder].join(', ') + ');');
    });
    doc.bindings.forEach(function (r) {
      var where = 'map_context_id = ' + r.mapContextId + ' AND creature_id = ' + r.creatureId;
      upsert('creature_loot_binding', 'map_context_id, creature_id, profile_key', [r.mapContextId, r.creatureId, quote(r.profileKey)].join(', '), where, 'profile_key = ' + quote(r.profileKey), 'map_context_id, creature_id');
    });
    doc.mapPolicies.forEach(function (r) {
      upsert('loot_map_policy', 'map_context_id, require_profiles', r.mapContextId + ', ' + (r.requireProfiles ? 1 : 0),
        'map_context_id = ' + r.mapContextId, 'require_profiles = ' + (r.requireProfiles ? '1' : 'CASE WHEN require_profiles = 1 THEN 1 ELSE 0 END'), 'map_context_id');
    });
    lines.push('COMMIT;');
    return lines.join('\n') + '\n';
  }
  var api = {empty: empty, parse: parse, validate: validate, odds: odds, sql: sql};
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.WeightedLootProject = api;
})(typeof window === 'object' ? window : globalThis);
