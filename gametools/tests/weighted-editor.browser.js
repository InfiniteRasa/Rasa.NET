async (page) => {
  const failures = [], check = (ok, message) => { if (!ok) failures.push(message); };
  check(await page.locator('#p-provenance').count() > 0, 'Weighted profile provenance must be editable.');
  check(await page.locator('#g-chance').count() > 0, 'Group admission must use basis points.');
  check(await page.locator('#b-map').count() > 0, 'Bindings must include map context identity.');
  check(await page.locator('body').textContent().then(s => s.includes('Weights are conditional')), 'Weights must be explained separately from admission.');
  if (failures.length) return { red: failures };
  await page.getByRole('tab', { name: 'Save and export' }).click();
  await page.getByRole('button', { name: 'Load from server', exact: true }).click();
  await page.waitForFunction(() => document.getElementById('srv-msg').textContent.includes('Loaded'));
  await page.getByRole('tab', { name: 'Profiles and groups', exact: true }).click();
  check(await page.locator('#p-provenance').inputValue() === 'Reconstructed', 'GET preserves provenance.');
  await page.locator('#g-chance').fill('5000');
  await page.locator('#g-chance').dispatchEvent('change');
  await page.locator('input[data-field="weight"]').first().fill('3');
  await page.locator('input[data-field="weight"]').first().dispatchEvent('change');
  check(await page.locator('#g-foot').textContent().then(s => s.includes('50%')), 'Odds preview follows group admission.');
  await page.getByRole('tab', { name: 'Map and creature bindings', exact: true }).click();
  await page.locator('#b-map').fill('20'); await page.locator('#b-creature').fill('2');
  await page.getByRole('button', { name: 'Set binding', exact: true }).click();
  check(await page.locator('#binding-body').textContent().then(s => s.includes('20')), 'Map binding editor renders new row.');
  await page.getByRole('tab', { name: 'Save and export', exact: true }).click();
  const sql = await page.locator('#sql').inputValue();
  check(sql.includes('ordinary_loot_group') && !sql.includes('legacy_flat_'), 'SQL uses canonical tables.');
  await page.evaluate(() => fetch('/__fail', { method: 'POST' }));
  await page.getByRole('button', { name: 'Send to server', exact: true }).click();
  await page.getByRole('button', { name: 'Yes, send', exact: true }).click();
  await page.waitForFunction(() => document.getElementById('srv-msg').textContent.includes('Owned injected failure'));
  check(await page.locator('#status').textContent().then(s => s.includes('Changed')), 'Failed save must not mark draft synced.');
  await page.getByRole('button', { name: 'Send to server', exact: true }).click();
  await page.getByRole('button', { name: 'Yes, send', exact: true }).click();
  await page.waitForFunction(() => document.getElementById('srv-msg').textContent.includes('committed'));
  const state = await page.evaluate(() => fetch('/__state').then(r => r.json()));
  check(state.saves === 1 && state.payload.version === 2 && state.payload.format === 'rasa-weighted-loot-profiles', 'Only weighted successful saves reach the mock.');
  check(state.payload.items[0].weight === 3 && state.payload.groups[0].chanceBasisPoints === 5000, 'Units survive REST save.');
  check(state.payload.bindings.length === 2 && state.payload.profiles[0].evidenceNotes.includes('Reviewed'), 'Bindings and evidence are retained.');
  await page.reload();
  await page.getByRole('tab', { name: 'Profiles and groups', exact: true }).click();
  check(await page.locator('#g-chance').inputValue() === '5000', 'Browser draft survives reload.');
  await page.getByRole('tab', { name: 'Save and export', exact: true }).click();
  await page.locator('#x-file').setInputFiles({
    name: 'old-flat.json', mimeType: 'application/json',
    buffer: Buffer.from('{"format":"rasa-loot-tables","version":1,"groups":[],"assignments":[]}')
  });
  await page.waitForFunction(() => document.getElementById('x-msg').textContent.includes('percent'));
  check(await page.locator('#x-msg').textContent().then(s => s.includes('explicit')), 'Legacy import offers an authoring action.');
  await page.getByRole('tab', { name: 'Profiles and groups', exact: true }).click();
  check(await page.locator('#g-chance').inputValue() === '5000', 'Invalid file import preserves current draft.');
  return { passed: failures.length === 0, failures, savedRows: state.payload };
}
