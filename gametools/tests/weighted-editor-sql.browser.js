async (page) => {
  const failures = [], check = (ok, message) => { if (!ok) failures.push(message); };
  if (await page.locator('#x-sql-dialect').count() === 0)
    return { passed: false, failures: ['SQL export needs a labeled, explicit provider control.'] };
  await page.getByRole('tab', { name: 'Profiles and groups', exact: true }).click();
  const metadata = {
    'p-description': "O'Brien \"quoted\" C:\\new\\review",
    'p-revision': 'ending\\',
    'p-provenance': '中文 🙂 e\u0301 العربية',
    'p-evidence': 'line\nnext\tend\\'
  };
  for (const [id, value] of Object.entries(metadata)) {
    await page.locator('#' + id).fill(value);
    await page.locator('#' + id).dispatchEvent('change');
  }
  const payloadBefore = await page.evaluate(() => localStorage.getItem('rasa-weighted-loot-profiles-v2'));
  await page.getByRole('tab', { name: 'Save and export', exact: true }).click();
  check(await page.getByLabel('SQL dialect').inputValue() === 'sqlite', 'Default provider is explicitly SQLite.');
  await page.evaluate(() => {
    window.__sqlDownloads = [];
    window.__sqlCopies = [];
    const createObjectURL = URL.createObjectURL.bind(URL);
    URL.createObjectURL = blob => {
      window.__sqlDownloads.push(blob.text());
      return createObjectURL(blob);
    };
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async text => { window.__sqlCopies.push(text); }
    } });
    document.addEventListener('click', event => {
      const anchor = event.target.closest('a[download]');
      if (anchor) { window.__sqlFilename = anchor.download; event.preventDefault(); }
    });
  });
  const exports = [];
  for (const dialect of ['mysql', 'sqlite']) {
    await page.getByLabel('SQL dialect').selectOption(dialect);
    const preview = await page.locator('#sql').inputValue();
    check(preview.includes('-- SQL dialect: ' + (dialect === 'mysql' ? 'MySQL' : 'SQLite')), 'Preview names ' + dialect + '.');
    check(dialect === 'mysql' ? preview.includes("CONVERT(X'") && !preview.includes("O'Brien") :
      preview.includes("O''Brien") && preview.includes('C:\\new\\review') && !preview.includes('CONVERT('),
      'Provider-specific metadata encoding in ' + dialect + '.');
    await page.getByRole('button', { name: 'Download .sql', exact: true }).click();
    await page.getByRole('button', { name: 'Copy SQL', exact: true }).click();
    const result = await page.evaluate(async () => ({
      downloaded: await window.__sqlDownloads[window.__sqlDownloads.length - 1],
      copied: window.__sqlCopies[window.__sqlCopies.length - 1],
      filename: window.__sqlFilename
    }));
    check(result.downloaded === preview && result.copied === preview, 'Download/copy exactly match selected ' + dialect + ' preview.');
    check(result.filename === 'weighted-loot-profiles-' + dialect + '.sql', 'Download filename identifies ' + dialect + '.');
    exports.push({ dialect, filename: result.filename, previewMatchesDownloadAndCopy: result.downloaded === preview && result.copied === preview });
  }
  const payloadAfter = await page.evaluate(() => localStorage.getItem('rasa-weighted-loot-profiles-v2'));
  check(payloadAfter === payloadBefore, 'Provider selection must not change the weighted JSON payload.');
  const payload = JSON.parse(payloadAfter);
  check(payload.profiles[0].description === metadata['p-description'] &&
    payload.profiles[0].contentRevision === metadata['p-revision'] &&
    payload.profiles[0].provenance === metadata['p-provenance'] &&
    payload.profiles[0].evidenceNotes === metadata['p-evidence'], 'Metadata stays exact in the browser draft.');
  const receipt = { passed: failures.length === 0, failures, exports, profile: payload.profiles[0] };
  await page.evaluate(receipt => { window.__weightedSqlReceipt = receipt; }, receipt);
  return receipt;
}
