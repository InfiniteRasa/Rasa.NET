(function () {
  'use strict';
  var P = window.WeightedLootProject, GT = window.GameTools;
  var KEY = 'rasa-weighted-loot-profiles-v2', SYNC = KEY + '-synced';
  var state = P.empty(), selected = null, groupKey = null, undo = null, stored = true, busy = false, asking = false;
  var tab = 'profiles', timer = null, serverMessage = '', serverError = false, clearAsked = false;
  function $(id) { return document.getElementById(id); }
  function esc(text) { return String(text).replace(/[&<>"']/g, function (c) { return {'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[c]; }); }
  function profile() { return state.profiles.find(function (r) { return r.profileKey === selected; }); }
  function group() { return state.groups.find(function (r) { return r.profileKey === selected && r.groupKey === groupKey; }); }
  function currentGroups() { return state.groups.filter(function (r) { return r.profileKey === selected; }); }
  function candidates() { return state.items.filter(function (r) { return r.profileKey === selected && r.groupKey === groupKey; }); }
  function numeric(input) { return input.value.trim() === '' ? NaN : Number(input.value); }
  function mark(doc) { return JSON.stringify(doc); }
  function remember(doc) {
    try { localStorage.setItem(SYNC, JSON.stringify({url: GT.instance.url, mark: mark(doc)})); }
    catch (_) { stored = false; }
  }
  function message(text, error) {
    $('edit-msg').textContent = text;
    $('edit-msg').className = error ? 'bad' : '';
  }
  function checkpoint(text) {
    undo = JSON.stringify(state);
    $('toast-text').textContent = text;
    $('toast').classList.add('on');
    clearTimeout(timer);
    timer = setTimeout(function () { $('toast').classList.remove('on'); }, 9000);
  }
  function persist() {
    try { localStorage.setItem(KEY, JSON.stringify(state)); stored = true; }
    catch (_) { stored = false; }
  }
  function changed(render) {
    asking = false; clearAsked = false;
    persist(); status(); checks(); odds();
    if (render) renderAll();
    else renderServer();
  }
  function selectFirst() {
    selected = state.profiles.length ? state.profiles[0].profileKey : null;
    groupKey = currentGroups().length ? currentGroups()[0].groupKey : null;
  }
  function status() {
    var text = state.profiles.length + ' profiles, ' + state.groups.length + ' groups, ' + state.bindings.length + ' map/creature bindings. ';
    text += stored ? 'Draft saved in this browser.' : 'Browser storage unavailable. Save a project file.';
    if (GT && GT.ready) {
      try {
        var last = JSON.parse(localStorage.getItem(SYNC) || 'null');
        text += last && last.url === GT.instance.url ?
          (last.mark === mark(state) ? ' Last loaded or committed draft.' : ' Changed since last load or commit.') : ' Not yet compared with this server.';
      } catch (_) { text += ' Sync status unavailable.'; }
    }
    $('status').textContent = text;
  }
  function fill(id, value, disabled) {
    $(id).value = value == null ? '' : value;
    $(id).disabled = disabled || busy;
  }
  function renderProfileList() {
    var query = $('p-search').value.toLowerCase();
    $('p-list').innerHTML = state.profiles.filter(function (r) { return (r.profileKey + ' ' + r.description).toLowerCase().includes(query); })
      .map(function (r) {
        return '<li><button class="pick' + (r.profileKey === selected ? ' on' : '') + '" data-profile="' + esc(r.profileKey) + '">' +
          '<strong>' + esc(r.profileKey || '(empty key)') + '</strong><small>' + esc(r.description || 'No description') + '</small></button></li>';
      }).join('') || '<li class="empty">No profiles shown. Load from server or a project file, or create a profile.</li>';
  }
  function renderGroupChoices() {
    var gs = currentGroups();
    $('g-select').innerHTML = gs.map(function (row) { return '<option value="' + esc(row.groupKey) + '">' + esc(row.groupKey) + '</option>'; }).join('') || '<option value="">No groups (empty profile)</option>';
    fill('g-select', groupKey || '', !gs.length);
  }
  function renderProfiles() {
    renderProfileList();
    var p = profile(), g = group();
    ['key', 'description', 'revision', 'provenance', 'evidence'].forEach(function (name, index) {
      fill('p-' + name, p ? p[['profileKey', 'description', 'contentRevision', 'provenance', 'evidenceNotes'][index]] : '', !p);
    });
    $('p-copy').disabled = !p || busy;
    $('g-new').disabled = !p || busy;
    renderGroupChoices();
    ['key', 'kind', 'chance', 'min', 'max', 'order'].forEach(function (name, index) {
      fill('g-' + name, g ? g[['groupKey', 'kind', 'chanceBasisPoints', 'minimumRolls', 'maximumRolls', 'sortOrder'][index]] : '', !g);
    });
    $('g-remove').disabled = !g || busy;
    $('i-add').disabled = !g || busy;
    $('i-template').disabled = !g || busy; $('i-weight').disabled = !g || busy;
    renderItems();
  }
  function renderItems() {
    var rows = candidates();
    $('i-body').innerHTML = rows.length ? '<table><thead><tr><th>Template ID</th><th>Weight</th><th>Per roll</th><th>Min qty</th><th>Max qty</th><th>Order</th><th></th></tr></thead><tbody>' +
      rows.map(function (row) {
        var total = rows.reduce(function (sum, r) { return sum + r.weight; }, 0);
        return '<tr data-item="' + row.itemTemplateId + '"><td class="mono">' + esc(row.itemTemplateId) + '</td>' +
          ['weight', 'minimumQuantity', 'maximumQuantity', 'sortOrder'].map(function (field, index) {
            var cell = '<td><input type="number" step="1" data-field="' + field + '" value="' + esc(row[field]) +
              '" aria-label="' + field + ' for template ' + row.itemTemplateId + '"' + (field === 'sortOrder' ? '' : ' min="1"') +
              (busy ? ' disabled' : '') + '></td>';
            if (index === 0) cell += '<td class="num" data-odds>' + (Number.isFinite(total) && total > 0 ? (row.weight / total * 100).toFixed(2) + '%' : 'Invalid') + '</td>';
            return cell;
          }).join('') + '<td><button class="btn danger" data-remove="' + row.itemTemplateId + '"' + (busy ? ' disabled' : '') +
          ' aria-label="Remove template ' + row.itemTemplateId + '">Remove</button></td></tr>';
      }).join('') + '</tbody></table>' : '<div class="empty">' + (group() ? 'No candidates yet. Add a native template ID above. A group must have candidates before saving.' : 'Select a group to edit its weighted candidates.') + '</div>';
    odds();
  }
  function odds() {
    var g = group();
    if (!g) { $('g-foot').textContent = 'An empty profile gives no ordinary items. Credits and mission drops are separate.'; return; }
    if (P.validate(state).length) { $('g-foot').textContent = 'Fix the draft checks before using this odds preview.'; return; }
    var result = P.odds(g, state.items);
    $('g-foot').textContent = result.admissionPercent.toFixed(2).replace(/\.00$/, '') + '% group admission. ' +
      g.minimumRolls + '..' + g.maximumRolls + ' rolls, uniformly chosen after admission. ' +
      result.noSelectionPercent.toFixed(2) + '% chance of no selection (includes zero rolls); ' +
      result.expectedSelections.toFixed(2) + ' expected selections per kill. Per-roll percentages are conditional, not independent item drop chances.';
  }
  function options() {
    $('b-profile').innerHTML = state.profiles.map(function (p) { return '<option value="' + esc(p.profileKey) + '">' + esc(p.profileKey) + '</option>'; }).join('');
    $('b-profile').disabled = !state.profiles.length || busy;
    if (selected) $('b-profile').value = selected;
  }
  function renderBindings() {
    options();
    $('binding-body').innerHTML = state.bindings.length ? '<table><thead><tr><th>Map</th><th>Creature</th><th>Profile</th><th></th></tr></thead><tbody>' +
      state.bindings.map(function (r, i) { return '<tr><td>' + r.mapContextId + '</td><td>' + r.creatureId + '</td><td>' + esc(r.profileKey) +
        '</td><td><button class="btn" data-bind="' + i + '">Edit</button></td></tr>'; }).join('') + '</tbody></table>' :
      '<div class="empty">No bindings in this draft. Bind a native map and creature to a profile.</div>';
    $('policy-body').innerHTML = state.mapPolicies.length ? '<table><thead><tr><th>Map</th><th>Require profiles</th></tr></thead><tbody>' +
      state.mapPolicies.map(function (r) { return '<tr><td>' + r.mapContextId + '</td><td>' + (r.requireProfiles ? 'Required' : 'Optional') + '</td></tr>'; }).join('') + '</tbody></table>' : '<p class="muted">No map policies in this draft. Existing server policies are retained.</p>';
  }
  function checks() {
    var problems = P.validate(state);
    $('x-checks').innerHTML = problems.length ? problems.map(function (s) { return '<li class="bad">' + esc(s) + '</li>'; }).join('') :
      '<li>Structure and numeric bounds pass. Native items and required-map coverage still need server validation.</li>';
    if (tab === 'export') {
      $('sql').value = problems.length ? '' : P.sql(state, $('x-sql-dialect').value);
      $('x-sql-dialect').disabled = busy;
      ['x-save', 'x-sql-save', 'x-sql-copy'].forEach(function (id) { $(id).disabled = busy || problems.length > 0; });
    }
  }
  function renderServer() {
    if (!GT || !GT.ready) {
      $('srv-where').textContent = (GT ? GT.problems[0] : 'gametools.js is missing.') + ' File editing is still available.';
      $('srv-row').innerHTML = '';
      return;
    }
    $('srv-where').textContent = GT.instance.name + ' (' + GT.instance.url + '). Named profiles replace their groups/candidates. Unnamed profiles, bindings and policies remain. Native validation and one World transaction precede publication.';
    $('srv-row').innerHTML = asking ?
      '<span>Commit this weighted draft to ' + esc(GT.instance.name) + '?</span><button class="btn primary" id="srv-yes">Yes, send</button><button class="btn" id="srv-no">Not now</button>' :
      '<button class="btn" id="srv-load"' + (busy || !GT.has('lootPoolsRead') ? ' disabled' : '') + '>Load from server</button>' +
      '<button class="btn primary" id="srv-send"' + (busy || !GT.has('lootPoolsWrite') || P.validate(state).length ? ' disabled' : '') + '>Send to server</button>';
    $('srv-msg').textContent = serverMessage;
    $('srv-msg').className = serverError ? 'bad' : '';
  }
  function renderAll() {
    renderProfiles(); renderBindings(); status(); checks(); renderServer();
  }
  function setTab(name, focus) {
    tab = name;
    document.querySelectorAll('.tab').forEach(function (button) {
      var on = button.dataset.tab === name;
      button.setAttribute('aria-selected', on); button.tabIndex = on ? 0 : -1;
      $('view-' + button.dataset.tab).classList.toggle('on', on);
      if (on && focus) button.focus();
    });
    renderAll();
  }
  function unique(prefix, rows, field) {
    var n = 1;
    while (rows.some(function (r) { return r[field] === prefix + n; })) n++;
    return prefix + n;
  }
  document.querySelector('.tabs').addEventListener('click', function (e) {
    var button = e.target.closest('.tab');
    if (button) setTab(button.dataset.tab, false);
  });
  document.querySelector('.tabs').addEventListener('keydown', function (e) {
    var names = ['profiles', 'bindings', 'export'], index = names.indexOf(tab);
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) return;
    e.preventDefault();
    setTab(e.key === 'Home' ? names[0] : e.key === 'End' ? names[2] : names[(index + (e.key === 'ArrowLeft' ? 2 : 1)) % 3], true);
  });
  $('p-search').addEventListener('input', renderProfileList);
  $('p-list').addEventListener('click', function (e) {
    var button = e.target.closest('[data-profile]');
    if (!button || busy) return;
    selected = button.dataset.profile;
    groupKey = currentGroups().length ? currentGroups()[0].groupKey : null;
    renderAll();
  });
  $('p-new').addEventListener('click', function () {
    if (busy) return;
    checkpoint('Created a profile.');
    selected = unique('profile_', state.profiles, 'profileKey'); groupKey = null;
    state.profiles.push({profileKey: selected, description: '', contentRevision: '', provenance: '', evidenceNotes: ''});
    changed(true); $('p-key').focus();
  });
  $('p-copy').addEventListener('click', function () {
    if (!profile() || busy) return;
    checkpoint('Duplicated a profile without changing any bindings.');
    var old = selected, copy = JSON.parse(JSON.stringify(profile()));
    selected = unique('profile_', state.profiles, 'profileKey'); copy.profileKey = selected;
    state.profiles.push(copy);
    ['groups', 'items'].forEach(function (table) {
      state[table].filter(function (r) { return r.profileKey === old; }).forEach(function (r) {
        var next = JSON.parse(JSON.stringify(r)); next.profileKey = selected; state[table].push(next);
      });
    });
    changed(true);
  });
  ['key', 'description', 'revision', 'provenance', 'evidence'].forEach(function (name, index) {
    $('p-' + name).addEventListener('change', function () {
      if (!profile() || busy) return;
      var field = ['profileKey', 'description', 'contentRevision', 'provenance', 'evidenceNotes'][index], old = selected;
      profile()[field] = this.value;
      if (field === 'profileKey') {
        selected = this.value;
        ['groups', 'items', 'bindings'].forEach(function (table) {
          state[table].forEach(function (r) { if (r.profileKey === old) r.profileKey = selected; });
        });
      }
      changed(false); renderProfileList(); options();
    });
  });
  $('g-select').addEventListener('change', function () { groupKey = this.value; renderProfiles(); });
  $('g-new').addEventListener('click', function () {
    if (!profile() || busy) return;
    checkpoint('Created a group.');
    groupKey = unique('group_', currentGroups(), 'groupKey');
    state.groups.push({profileKey: selected, groupKey: groupKey, kind: 4, chanceBasisPoints: 0, minimumRolls: 1, maximumRolls: 1, sortOrder: currentGroups().length});
    changed(true); $('g-key').focus();
  });
  $('g-remove').addEventListener('click', function () {
    if (!group() || busy) return;
    checkpoint('Removed group and its candidates from the draft.');
    var old = groupKey;
    ['groups', 'items'].forEach(function (table) { state[table] = state[table].filter(function (r) { return r.profileKey !== selected || r.groupKey !== old; }); });
    groupKey = currentGroups().length ? currentGroups()[0].groupKey : null;
    changed(true);
  });
  ['key', 'kind', 'chance', 'min', 'max', 'order'].forEach(function (name, index) {
    $('g-' + name).addEventListener('change', function () {
      if (!group() || busy) return;
      var field = ['groupKey', 'kind', 'chanceBasisPoints', 'minimumRolls', 'maximumRolls', 'sortOrder'][index], old = groupKey;
      group()[field] = name === 'key' ? this.value : numeric(this);
      if (name === 'key') {
        groupKey = this.value;
        state.items.forEach(function (r) { if (r.profileKey === selected && r.groupKey === old) r.groupKey = groupKey; });
      }
      changed(false); renderGroupChoices();
    });
  });
  $('item-add').addEventListener('submit', function (e) {
    e.preventDefault();
    if (!group() || busy) return;
    var id = numeric($('i-template')), weight = numeric($('i-weight'));
    if (candidates().some(function (r) { return r.itemTemplateId === id; })) { message('That template is already a candidate in this group. Edit its weight below.', true); return; }
    checkpoint('Added a candidate.');
    state.items.push({profileKey: selected, groupKey: groupKey, itemTemplateId: id, weight: weight, minimumQuantity: 1, maximumQuantity: 1, sortOrder: candidates().length});
    message('', false); changed(true);
  });
  $('i-body').addEventListener('change', function (e) {
    if (!e.target.dataset.field || busy) return;
    var id = Number(e.target.closest('[data-item]').dataset.item);
    var row = candidates().find(function (r) { return r.itemTemplateId === id; });
    row[e.target.dataset.field] = numeric(e.target);
    changed(false);
    var rows = candidates(), total = rows.reduce(function (sum, r) { return sum + r.weight; }, 0);
    $('i-body').querySelectorAll('[data-item]').forEach(function (element) {
      var candidate = rows.find(function (r) { return r.itemTemplateId === Number(element.dataset.item); });
      element.querySelector('[data-odds]').textContent = Number.isFinite(total) && total > 0 ?
        (candidate.weight / total * 100).toFixed(2) + '%' : 'Invalid';
    });
  });
  $('i-body').addEventListener('click', function (e) {
    var button = e.target.closest('[data-remove]');
    if (!button || busy) return;
    checkpoint('Removed a candidate.');
    var id = Number(button.dataset.remove);
    state.items = state.items.filter(function (r) { return r.profileKey !== selected || r.groupKey !== groupKey || r.itemTemplateId !== id; });
    changed(true);
  });
  $('binding-form').addEventListener('submit', function (e) {
    e.preventDefault();
    if (busy || !$('b-profile').value) return;
    checkpoint('Set a map/creature binding.');
    var map = numeric($('b-map')), creature = numeric($('b-creature'));
    var row = state.bindings.find(function (r) { return r.mapContextId === map && r.creatureId === creature; });
    if (row) row.profileKey = $('b-profile').value;
    else state.bindings.push({mapContextId: map, creatureId: creature, profileKey: $('b-profile').value});
    changed(true);
  });
  $('binding-body').addEventListener('click', function (e) {
    var button = e.target.closest('[data-bind]');
    if (!button || busy) return;
    var row = state.bindings[Number(button.dataset.bind)];
    $('b-map').value = row.mapContextId; $('b-creature').value = row.creatureId; $('b-profile').value = row.profileKey;
    $('b-map').focus();
  });
  $('policy-form').addEventListener('submit', function (e) {
    e.preventDefault(); if (busy) return;
    var map = numeric($('policy-map')), required = $('policy-required').checked;
    var row = state.mapPolicies.find(function (r) { return r.mapContextId === map; });
    if (row && row.requireProfiles && !required) { $('binding-msg').textContent = 'A required-map policy cannot be disabled here. Retain coverage and use an empty profile for no loot.'; return; }
    checkpoint('Set a map policy.');
    if (row) row.requireProfiles = required;
    else state.mapPolicies.push({mapContextId: map, requireProfiles: required});
    $('binding-msg').textContent = ''; changed(true);
  });
  function errorText(error) { return error.message + (error.problems && error.problems.length ? '\n' + error.problems.join('\n') : ''); }
  function run(operation) {
    if (busy) return;
    busy = true; asking = false; serverError = false; renderAll();
    Promise.resolve().then(operation).catch(function (e) { serverError = true; serverMessage = errorText(e); })
      .then(function () { busy = false; renderAll(); });
  }
  $('srv-row').addEventListener('click', function (e) {
    if (e.target.id === 'srv-no') { asking = false; renderServer(); }
    else if (e.target.id === 'srv-send') { asking = true; renderServer(); }
    else if (e.target.id === 'srv-load') {
      serverMessage = 'Loading weighted content...';
      run(function () { return GT.get('lootPoolsRead').then(function (doc) {
        var next = P.parse(doc);
        checkpoint('Loaded from server. Undo restores the previous browser draft.');
        state = next; selectFirst(); persist(); remember(state); serverMessage = 'Loaded validated-contract weighted content from ' + GT.instance.name + '.';
      }); });
    } else if (e.target.id === 'srv-yes') {
      var payload;
      try { payload = P.parse(state); } catch (error) { serverError = true; serverMessage = errorText(error); asking = false; renderServer(); return; }
      serverMessage = 'Validating and committing weighted content...';
      run(function () { return GT.post('lootPoolsWrite', payload).then(function () {
        remember(payload); serverMessage = 'Weighted edit committed. The next new ordinary roll uses the validated catalog. Existing captured profiles remain unchanged.';
      }); });
    }
  });
  function download(name, content, type) {
    var a = document.createElement('a'), url = URL.createObjectURL(new Blob([content], {type: type}));
    a.href = url; a.download = name; document.body.appendChild(a); a.click();
    setTimeout(function () { URL.revokeObjectURL(url); a.remove(); }, 500);
  }
  $('x-save').addEventListener('click', function () {
    try { download('weighted-loot-profiles.json', JSON.stringify(P.parse(state), null, 2), 'application/json'); }
    catch (e) { $('x-msg').textContent = e.message; }
  });
  $('x-load').addEventListener('click', function () { if (!busy) $('x-file').click(); });
  $('x-file').addEventListener('change', function () {
    var file = this.files[0]; this.value = '';
    if (!file || busy) return;
    if (file.size > 8 * 1024 * 1024) { $('x-msg').textContent = 'File is larger than 8 MB. Split and review it before loading.'; return; }
    file.text().then(function (text) {
      var next = P.parse(JSON.parse(text));
      checkpoint('Loaded project file.'); state = next; selectFirst(); changed(true);
      $('x-msg').textContent = 'Loaded weighted project file. Native validation is still required before commit.';
    }).catch(function (e) { $('x-msg').textContent = e.message + '\nCurrent draft was retained.'; });
  });
  $('x-reset').addEventListener('click', function () {
    if (busy) return;
    if (!clearAsked) { clearAsked = true; this.textContent = 'Yes, clear browser draft'; return; }
    checkpoint('Cleared browser draft only. Server content was not deleted.');
    state = P.empty(); selectFirst(); this.textContent = 'Clear browser draft'; changed(true);
  });
  $('x-sql-dialect').addEventListener('change', function () { $('x-sql-msg').textContent = ''; checks(); });
  $('x-sql-save').addEventListener('click', function () {
    var dialect = $('x-sql-dialect').value;
    download('weighted-loot-profiles-' + dialect + '.sql', P.sql(state, dialect), 'text/plain');
  });
  $('x-sql-copy').addEventListener('click', function () {
    if (!navigator.clipboard) { $('x-sql-msg').textContent = 'Clipboard unavailable. Select the SQL below or download it.'; return; }
    navigator.clipboard.writeText(P.sql(state, $('x-sql-dialect').value)).then(function () { $('x-sql-msg').textContent = 'Copied SQL.'; },
      function () { $('x-sql-msg').textContent = 'Clipboard permission refused. Select the SQL below or download it.'; });
  });
  $('toast-undo').addEventListener('click', function () {
    if (!undo || busy) return;
    state = JSON.parse(undo); undo = null; $('toast').classList.remove('on'); selectFirst(); changed(true);
  });
  try {
    var raw = localStorage.getItem(KEY);
    if (raw) {
      var draft = JSON.parse(raw);
      if (draft.format !== 'rasa-weighted-loot-profiles' || draft.version !== 2 ||
          !['profiles', 'groups', 'items', 'bindings', 'mapPolicies'].every(function (name) { return Array.isArray(draft[name]); }))
        throw new Error('Stored draft has an incompatible contract.');
      state = draft;
    } else if (localStorage.getItem('rasa-loot-tables-v1')) {
      message('An old flat project is retained in browser storage. It was not activated or converted. Export it with the old editor, review it, and explicitly author weighted content.', true);
    }
  } catch (e) { stored = false; message('Could not load browser draft: ' + e.message + ' Load a saved weighted project file. The stored copy was not overwritten.', true); }
  selectFirst(); renderAll();
})();
